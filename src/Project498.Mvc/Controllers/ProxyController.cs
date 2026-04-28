using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Project498.Mvc.Constants;

namespace Project498.Mvc.Controllers;

/// <summary>
/// Generic catch-all proxy that forwards browser-initiated API requests to the backend
/// Web API (<c>Project498.WebApi</c>).
///
/// <para>
/// <b>Which routes this handles:</b> Any request matching <c>/api/{**path}</c> that is NOT
/// claimed by a more specific controller. The concrete routes registered by
/// <see cref="AuthController"/> (<c>/api/auth</c>), <see cref="UsersController"/> (<c>/api/users</c>),
/// and <see cref="CheckoutsController"/> (<c>/api/checkouts</c>) take priority via ASP.NET Core's
/// route specificity rules. This proxy fires only for <c>/api/comics</c> and <c>/api/characters</c>.
/// </para>
///
/// <para>
/// <b>Security — token substitution:</b> The incoming <c>Authorization</c> header (which may
/// contain the user's JWT or nothing at all) is intentionally stripped before the request is
/// forwarded. It is replaced with <c>Authorization: Bearer {ApiKeyConstants.ServiceApiKey}</c>,
/// the hardcoded service-to-service key. This ensures the backend never receives a user token
/// and that only the MVC project can authorize calls to the backend.
/// </para>
///
/// <para>
/// <b>Adding new backend controllers:</b> No changes to this file are needed. Any new route
/// registered in <c>Project498.WebApi</c> under <c>/api/*</c> will automatically be proxied
/// as long as the MVC project does not register a controller at the same path.
/// </para>
///
/// <para>
/// <b>What this proxy does NOT do:</b> inspect/transform request bodies, cache responses,
/// retry on failure, or log request bodies. A non-2xx from the backend is returned as-is.
/// </para>
/// </summary>
[ApiController]
[Route("api/{**path}")]
public class ProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProxyController> _logger;

    public ProxyController(IHttpClientFactory httpClientFactory, ILogger<ProxyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Accepts any HTTP method at <c>/api/{**path}</c> and forwards it to the backend API.
    ///
    /// <para>Request lifecycle:</para>
    /// <list type="number">
    ///   <item>Extract <c>{path}</c> from the route and the incoming query string.</item>
    ///   <item>Construct the full backend URL: <c>{BackendBaseUrl}/api/{path}?{query}</c>.</item>
    ///   <item>Build an outbound <see cref="HttpRequestMessage"/> with the same HTTP method and body.</item>
    ///   <item>Copy <c>Content-Type</c> from the incoming request; skip <c>Authorization</c> and hop-by-hop headers.</item>
    ///   <item>Inject <c>Authorization: Bearer {ApiKeyConstants.ServiceApiKey}</c> — replacing the user's token.</item>
    ///   <item>Dispatch via the named <c>"backend"</c> <see cref="HttpClient"/> (base address set in Program.cs).</item>
    ///   <item>Stream the backend's status code, <c>Content-Type</c> header, and body back to the caller unchanged.</item>
    /// </list>
    /// </summary>
    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpDelete]
    [HttpPatch]
    public async Task<IActionResult> ProxyRequest(string? path)
    {
        // Reconstruct the full path including query string.
        var queryString = Request.QueryString.Value ?? string.Empty;
        var targetPath = $"api/{path}{queryString}";

        _logger.LogDebug("Proxying {Method} {Path} to backend", Request.Method, targetPath);

        // Build the outbound request with the same method and body.
        // using ensures disposal of the request and response, including any held sockets and stream resources.
        using var outboundRequest = new HttpRequestMessage(
            new HttpMethod(Request.Method),
            targetPath
        );

        // Copy the request body for methods that typically carry one.
        // Guarding by method verb is more reliable than checking ContentLength (which can be null
        // even when a body is present, e.g., POST with no Content-Length header).
        if (HttpMethods.IsPost(Request.Method) || HttpMethods.IsPut(Request.Method) || HttpMethods.IsPatch(Request.Method))
        {
            outboundRequest.Content = new StreamContent(Request.Body);

            // Forward Content-Type so the backend can parse the body correctly.
            if (Request.ContentType is not null)
            {
                outboundRequest.Content.Headers.ContentType =
                    MediaTypeHeaderValue.Parse(Request.ContentType);
            }
        }

        // Inject the service API key, replacing whatever Authorization header the browser sent.
        // The backend's ApiKeyMiddleware validates this on every request.
        // This ensures the backend never receives a user JWT and cannot be called directly by browsers.
        outboundRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiKeyConstants.ServiceApiKey);

        // Dispatch to the backend via the named HttpClient (base URL configured in Program.cs).
        var client = _httpClientFactory.CreateClient("backend");
        HttpResponseMessage backendResponse;
        try
        {
            backendResponse = await client.SendAsync(outboundRequest, HttpCompletionOption.ResponseHeadersRead);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Backend unreachable while proxying {Method} {Path}", Request.Method, targetPath);
            return StatusCode(502);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Backend timeout while proxying {Method} {Path}", Request.Method, targetPath);
            return StatusCode(504);
        }

        using (backendResponse)
        {
            // Stream the response back to the caller without buffering.
            Response.StatusCode = (int)backendResponse.StatusCode;

            if (backendResponse.Content.Headers.ContentType is not null)
            {
                Response.ContentType = backendResponse.Content.Headers.ContentType.ToString();
            }

            await backendResponse.Content.CopyToAsync(Response.Body);
        }

        return new EmptyResult();
    }
}
