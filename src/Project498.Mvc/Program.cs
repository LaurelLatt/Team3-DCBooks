using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Project498.Mvc.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// App database — users and checkouts.
// Comics/characters live in Project498.WebApi and are accessed via HttpClient.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AppConnection")));

// JWT authentication for user-facing requests.
// Tokens are issued by AuthController and validated here on protected MVC endpoints.
// Note: the service-to-service API key (for outbound calls to the backend) is handled
// separately via ApiKeyConstants — it is NOT related to this JWT configuration.
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();

// Named HttpClient for all outbound calls to the backend Web API.
// Base address points to Project498.WebApi (localhost in dev, Docker service name in compose).
// The Authorization header is set per-request by ProxyController and CheckoutsController,
// not here, so each request gets the service key independently.
builder.Services.AddHttpClient("backend", client =>
{
    var baseUrl = builder.Configuration["BackendApi:BaseUrl"]
        ?? throw new InvalidOperationException("BackendApi:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

// Marvel catalog service (same contract as the browser: GET /api/comics, GET /api/comics/{id}).
builder.Services.AddHttpClient("marvel", client =>
{
    var baseUrl = builder.Configuration["MarvelApi:BaseUrl"]
        ?? throw new InvalidOperationException("MarvelApi:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    // EnsureCreated does not alter existing tables; add comic_source when upgrading a dev DB.
    if (db.Database.IsRelational())
    {
        await db.Database.ExecuteSqlRawAsync(
            """ALTER TABLE "Checkouts" ADD COLUMN IF NOT EXISTS comic_source character varying(20) NOT NULL DEFAULT 'dc';""");
    }

    await DbSeeder.SeedAppAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Serve static HTML/CSS/JS from wwwroot. Default document for "/" is login first, then home.
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "login.html", "index.html" }
});
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
