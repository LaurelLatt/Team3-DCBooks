using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project498.Mvc.Data;
using Project498.Mvc.Controllers;
using Project498.WebApi.Data;
using Project498.Mvc.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Project498.WebApi.Models;

namespace Project498.WebApi.Tests.UnitTests;

public class CheckoutsControllerTests
{
    private ComicsDbContext CreateFreshDbContext()
    {
        var options = new DbContextOptionsBuilder<ComicsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ComicsDbContext(options);
    }
    
    private AppDbContext CreateFreshAppDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
    
    private HttpClient CreateHttpClient()
    {
        return new HttpClient
        {
            BaseAddress = new Uri("http://localhost")
        };
    }
    
    private CheckoutsController CreateController(
        AppDbContext? appDb = null,
        HttpClient? httpClient = null)
    {
        appDb ??= CreateFreshAppDbContext();
        httpClient ??= CreateHttpClient();

        var httpFactory = new FakeHttpClientFactory(httpClient);
        var logger = NullLogger<CheckoutsController>.Instance;

        return new CheckoutsController(appDb, httpFactory, logger);
    }
    
    private void SetUser(ControllerBase controller, int? userId)
    {
        var claims = new List<Claim>();

        if (userId != null)
        {
            claims.Add(new Claim("user_id", userId.ToString()));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }
    
    [Fact]
    public async Task GetCheckouts_ReturnsAllOrdered()
    {
        
        var appDb = CreateFreshAppDbContext();

        var controller = CreateController(appDb);

        appDb.Checkouts.AddRange(
            new Checkout { CheckoutId = 1, CheckoutDate = DateTime.UtcNow.AddDays(-1) },
            new Checkout { CheckoutId = 2, CheckoutDate = DateTime.UtcNow }
        );
        await appDb.SaveChangesAsync();

        var result = await controller.GetCheckouts();

        var ok = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<List<Checkout>>(ok.Value);

        Assert.Equal(2, data.Count);
        Assert.Equal(2, data[0].CheckoutId); // newest first
    }
    
    [Fact]
    public async Task GetCheckout_ReturnsCheckout_WhenFound()
    {
        var appDb = CreateFreshAppDbContext();

        appDb.Checkouts.Add(new Checkout { CheckoutId = 1 });
        await appDb.SaveChangesAsync();

        var controller = CreateController(appDb);

        var result = await controller.GetCheckout(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }
    
    [Fact]
    public async Task GetCheckout_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController();

        var result = await controller.GetCheckout(999);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
    }
    
    [Fact]
    public async Task Checkout_ReturnsUnauthorized_WhenNoUser()
    {
        var controller = CreateController();

        SetUser(controller, null);

        var result = await controller.Checkout(new CheckoutRequest(1));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
    
    [Fact]
    public async Task Checkout_ReturnsUserNotFound()
    {
        var appDb = CreateFreshAppDbContext();

        var controller = CreateController(appDb);
        
        SetUser(controller, 1);

        var result = await controller.Checkout(new CheckoutRequest(1));

        Assert.IsType<NotFoundObjectResult>(result);
    }
    
    [Fact]
    public async Task Checkout_ReturnsComicNotFound()
    {
        var appDb = CreateFreshAppDbContext();

        appDb.Users.Add(new User { UserId = 1 });
        await appDb.SaveChangesAsync();

        var controller = CreateController(appDb);
        SetUser(controller, 1);

        var result = await controller.Checkout(new CheckoutRequest(1));

        Assert.IsType<NotFoundObjectResult>(result);
    }
    
    [Fact]
    public async Task Checkout_ReturnsConflict_WhenComicUnavailable()
    {
        var appDb = CreateFreshAppDbContext();
        var comicsDb = CreateFreshDbContext();

        appDb.Users.Add(new User { UserId = 1 });
        comicsDb.Comics.Add(new Comic { ComicId = 1, Status = "checked_out" });

        await appDb.SaveChangesAsync();
        await comicsDb.SaveChangesAsync();

        var controller = CreateController(appDb);
        SetUser(controller, 1);

        var result = await controller.Checkout(new CheckoutRequest(1));

        Assert.IsType<ConflictObjectResult>(result);
    }
    
    [Fact]
    public async Task Checkout_CreatesCheckout_WhenValid()
    {
        var appDb = CreateFreshAppDbContext();
        var comicsDb = CreateFreshDbContext();

        appDb.Users.Add(new User { UserId = 1 });
        comicsDb.Comics.Add(new Comic { ComicId = 1, Status = "available" });

        await appDb.SaveChangesAsync();
        await comicsDb.SaveChangesAsync();

        var controller = CreateController(appDb);
        SetUser(controller, 1);

        var result = await controller.Checkout(new CheckoutRequest(1));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var checkout = Assert.IsType<Checkout>(created.Value);

        Assert.Equal(1, checkout.UserId);

        var comic = await comicsDb.Comics.FindAsync(1);
        Assert.Equal("checked_out", comic.Status);
    }
    
    [Fact]
    public async Task Return_ReturnsUnauthorized_WhenNoUser()
    {
        var controller = CreateController();
        SetUser(controller, null);

        var result = await controller.Return(1);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
    
    [Fact]
    public async Task Return_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController();
        SetUser(controller, 1);

        var result = await controller.Return(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }
    
    [Fact]
    public async Task Return_ReturnsForbid_WhenWrongUser()
    {
        var appDb = CreateFreshAppDbContext();
        var comicsDb = CreateFreshDbContext();

        appDb.Checkouts.Add(new Checkout { CheckoutId = 1, UserId = 2 });
        await appDb.SaveChangesAsync();

        var controller = CreateController(appDb);
        SetUser(controller, 1);

        var result = await controller.Return(1);

        Assert.IsType<ForbidResult>(result);
    }
    
    [Fact]
    public async Task Return_ReturnsConflict_WhenAlreadyReturned()
    {
        var appDb = CreateFreshAppDbContext();
        var comicsDb = CreateFreshDbContext();

        appDb.Checkouts.Add(new Checkout
        {
            CheckoutId = 1,
            UserId = 1,
            Status = "returned",
            ReturnDate = DateTime.UtcNow
        });

        await appDb.SaveChangesAsync();

        var controller = CreateController(appDb);
        SetUser(controller, 1);

        var result = await controller.Return(1);

        Assert.IsType<ConflictObjectResult>(result);
    }
    
    [Fact]
    public async Task Return_SuccessfullyReturnsComic()
    {
        var appDb = CreateFreshAppDbContext();
        var comicsDb = CreateFreshDbContext();

        appDb.Checkouts.Add(new Checkout
        {
            CheckoutId = 1,
            UserId = 1,
            ComicId = 1,
            Status = "checked_out"
        });

        comicsDb.Comics.Add(new Comic
        {
            ComicId = 1,
            Status = "checked_out",
            CheckedOutBy = 1
        });

        await appDb.SaveChangesAsync();
        await comicsDb.SaveChangesAsync();

        var controller = CreateController(appDb);
        SetUser(controller, 1);

        var result = await controller.Return(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var checkout = Assert.IsType<Checkout>(ok.Value);

        Assert.Equal("returned", checkout.Status);

        var comic = await comicsDb.Comics.FindAsync(1);
        Assert.Equal("available", comic.Status);
    }
}

class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _client;

    public FakeHttpClientFactory(HttpClient client)
    {
        _client = client;
    }

    public HttpClient CreateClient(string name)
    {
        return _client;
    }
}