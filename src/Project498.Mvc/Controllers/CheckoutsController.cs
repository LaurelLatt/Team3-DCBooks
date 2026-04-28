using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project498.Mvc.Constants;
using Project498.Mvc.Data;
using Project498.Mvc.Models;

namespace Project498.Mvc.Controllers;

/// <summary>
/// Manages comic checkouts and returns.
///
/// <para>
/// This controller owns the <c>Checkouts</c> and <c>Users</c> tables in the app database
/// (<see cref="AppDbContext"/>). It does NOT hold a direct reference to <c>ComicsDbContext</c>.
/// Instead, operations that read or mutate comic status (checkout, return) call the backend
/// Web API (<c>Project498.WebApi</c>) through a named <c>HttpClient</c> injected via
/// <see cref="IHttpClientFactory"/>. Those calls carry the service-to-service API key from
/// <see cref="ApiKeyConstants.ServiceApiKey"/>.
/// </para>
///
/// <para>
/// Cross-service consistency note: creating a checkout and updating the comic's status are
/// two separate HTTP operations and are not wrapped in a distributed transaction. If the
/// comic-status PUT to the backend fails after the checkout row is created, the records
/// will be inconsistent. This is an accepted trade-off of the split architecture.
/// </para>
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CheckoutsController : ControllerBase
{
    private readonly AppDbContext _appDb;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CheckoutsController> _logger;

    public CheckoutsController(AppDbContext appDb, IHttpClientFactory httpClientFactory, ILogger<CheckoutsController> logger)
    {
        _appDb = appDb;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetCheckouts()
    {
        var checkouts = await _appDb.Checkouts
            .OrderByDescending(c => c.CheckoutDate)
            .ToListAsync();
        return Ok(checkouts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCheckout(int id)
    {
        var checkout = await _appDb.Checkouts.FindAsync(id);
        return checkout is null
            ? NotFound(new ErrorResponse("CHECKOUT_NOT_FOUND", $"Checkout {id} was not found."))
            : Ok(checkout);
    }

    /// <summary>
    /// Returns active (unreturned) checkouts for a given user.
    /// Comic titles are not included in the response — the caller should fetch
    /// individual comic details from <c>GET /api/comics/{id}</c> if titles are needed.
    /// </summary>
    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetUserCheckouts(int userId)
    {
        var checkouts = await _appDb.Checkouts
            .Where(c => c.UserId == userId && c.ReturnDate == null)
            .OrderByDescending(c => c.CheckoutDate)
            .Select(c => new {
                c.CheckoutId,
                c.ComicId,
                c.DueDate,
                c.CheckoutDate
            })
            .ToListAsync();
        return Ok(checkouts);
    }

    /// <summary>
    /// Checks out a comic for the authenticated user.
    ///
    /// <para>Flow:</para>
    /// <list type="number">
    ///   <item>Validate the requesting user exists in the app DB.</item>
    ///   <item>Call <c>GET /api/comics/{comicId}</c> on the backend to confirm the comic
    ///         exists and is available. The backend API key is injected automatically.</item>
    ///   <item>Create the checkout record in the app DB.</item>
    ///   <item>Call <c>PUT /api/comics/{comicId}</c> on the backend to mark the comic as
    ///         checked_out. <c>CharacterIds</c> is null so the backend preserves existing
    ///         character links.</item>
    /// </list>
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        var userId = GetUserIdFromToken();
        if (userId is null)
        {
            return Unauthorized(new ErrorResponse("UNAUTHORIZED", "A valid user token is required."));
        }

        if (request.ComicId <= 0)
        {
            return BadRequest(new ErrorResponse("INVALID_COMIC_ID", "ComicId must be a positive integer."));
        }

        var userExists = await _appDb.Users.AnyAsync(u => u.UserId == userId.Value);
        if (!userExists)
        {
            return NotFound(new ErrorResponse("USER_NOT_FOUND", $"User {userId.Value} was not found."));
        }

        // Call the backend to validate the comic exists and is available.
        var backend = CreateBackendClient();
        var comicResponse = await backend.GetAsync($"api/comics/{request.ComicId}");

        if (comicResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound(new ErrorResponse("COMIC_NOT_FOUND", $"Comic {request.ComicId} was not found."));
        }

        if (!comicResponse.IsSuccessStatusCode)
        {
            return StatusCode(502, new ErrorResponse("BACKEND_ERROR", "Could not retrieve comic from backend."));
        }

        var comic = await comicResponse.Content.ReadFromJsonAsync<BackendComicDto>();

        if (!string.Equals(comic!.Status, "available", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new ErrorResponse("COMIC_UNAVAILABLE", "Comic is already checked out."));
        }

        // Create the checkout record in the app DB.
        var now = DateTime.UtcNow;
        var checkout = new Checkout
        {
            UserId = userId.Value,
            ComicId = request.ComicId,
            CheckoutDate = now,
            DueDate = now.AddDays(14),
            Status = "checked_out"
        };

        _appDb.Checkouts.Add(checkout);
        await _appDb.SaveChangesAsync();

        // Update comic status in the backend.
        // CharacterIds = null tells the backend to leave character links unchanged.
        var statusUpdate = new ComicStatusUpdateRequest(
            comic.Title, comic.IssueNumber, comic.YearPublished,
            comic.Publisher, "checked_out", userId.Value, null
        );
        await backend.PutAsJsonAsync($"api/comics/{request.ComicId}", statusUpdate);

        return CreatedAtAction(nameof(GetCheckout), new { id = checkout.CheckoutId }, checkout);
    }

    [Authorize]
    [HttpPut("{id:int}/return")]
    public Task<IActionResult> Return(int id) => ReturnInternal(id);

    [Authorize]
    [HttpPut("{id:int}")]
    public Task<IActionResult> ReturnCompat(int id) => ReturnInternal(id);

    /// <summary>
    /// Returns a checked-out comic.
    ///
    /// <para>Flow:</para>
    /// <list type="number">
    ///   <item>Load the checkout from the app DB and verify ownership.</item>
    ///   <item>Mark the checkout as returned in the app DB.</item>
    ///   <item>Fetch the current comic from the backend to get its full field set.</item>
    ///   <item>Call <c>PUT /api/comics/{comicId}</c> to mark the comic as available again.</item>
    /// </list>
    /// </summary>
    private async Task<IActionResult> ReturnInternal(int id)
    {
        var userId = GetUserIdFromToken();
        if (userId is null)
        {
            return Unauthorized(new ErrorResponse("UNAUTHORIZED", "A valid user token is required."));
        }

        var checkout = await _appDb.Checkouts.FindAsync(id);
        if (checkout is null)
        {
            return NotFound(new ErrorResponse("CHECKOUT_NOT_FOUND", $"Checkout {id} was not found."));
        }

        if (checkout.UserId != userId.Value)
        {
            return Forbid();
        }

        if (checkout.ReturnDate is not null || string.Equals(checkout.Status, "returned", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new ErrorResponse("CHECKOUT_ALREADY_RETURNED", "Checkout has already been returned."));
        }

        // Update checkout record in app DB.
        checkout.ReturnDate = DateTime.UtcNow;
        checkout.Status = "returned";
        await _appDb.SaveChangesAsync();

        // Fetch current comic data from backend to reconstruct the full update request.
        var backend = CreateBackendClient();
        var comicResponse = await backend.GetAsync($"api/comics/{checkout.ComicId}");

        if (comicResponse.IsSuccessStatusCode)
        {
            var comic = await comicResponse.Content.ReadFromJsonAsync<BackendComicDto>();

            // Mark comic as available again.
            // CharacterIds = null tells the backend to leave character links unchanged.
            var statusUpdate = new ComicStatusUpdateRequest(
                comic!.Title, comic.IssueNumber, comic.YearPublished,
                comic.Publisher, "available", null, null
            );
            var updateResponse = await backend.PutAsJsonAsync($"api/comics/{checkout.ComicId}", statusUpdate);
            if (!updateResponse.IsSuccessStatusCode)
            {
                // Log but do not fail the return — the checkout row is already committed.
                // The comic status will remain "checked_out" until manually corrected.
                _logger.LogError(
                    "Failed to mark comic {ComicId} as available after checkout {CheckoutId} was returned. Backend status: {StatusCode}",
                    checkout.ComicId, checkout.CheckoutId, updateResponse.StatusCode);
            }
        }
        else
        {
            _logger.LogError(
                "Could not fetch comic {ComicId} from backend during return of checkout {CheckoutId}. Backend status: {StatusCode}",
                checkout.ComicId, id, comicResponse.StatusCode);
        }

        return Ok(checkout);
    }

    /// <summary>
    /// Creates an HttpClient configured with the service-to-service API key.
    /// The named "backend" client has the base address set in Program.cs.
    /// The Authorization header is set per-call to avoid sharing state between requests.
    /// </summary>
    private HttpClient CreateBackendClient()
    {
        // IHttpClientFactory.CreateClient returns a new HttpClient wrapper each call,
        // so mutating DefaultRequestHeaders is safe — each call gets its own instance.
        // The underlying HttpMessageHandler is pooled, but the HttpClient (and its
        // DefaultRequestHeaders) is not shared across calls.
        var client = _httpClientFactory.CreateClient("backend");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ApiKeyConstants.ServiceApiKey);
        return client;
    }

    private int? GetUserIdFromToken()
    {
        var claim = User.FindFirst("user_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var userId) ? userId : null;
    }
}

/// <summary>Request body for creating a new checkout.</summary>
file record CheckoutRequest(int ComicId);

/// <summary>
/// Local DTO for deserializing comic data from the backend API (<c>GET /api/comics/{id}</c>).
/// Mirrors <c>Project498.WebApi.Controllers.ComicResponse</c>.
/// </summary>
file record BackendComicDto(
    int ComicId,
    string Title,
    int IssueNumber,
    int YearPublished,
    string Publisher,
    string Status,
    int? CheckedOutBy,
    List<int> CharacterIds,
    List<string> CharacterNames
);

/// <summary>
/// Local DTO for sending comic status updates to the backend API (<c>PUT /api/comics/{id}</c>).
/// Mirrors <c>Project498.WebApi.Controllers.ComicUpsertRequest</c>.
/// <para>Pass <c>CharacterIds = null</c> to preserve existing character links on the backend.</para>
/// </summary>
file record ComicStatusUpdateRequest(
    string Title,
    int IssueNumber,
    int YearPublished,
    string Publisher,
    string Status,
    int? CheckedOutBy,
    List<int>? CharacterIds
);
