# MVC / Web API Split Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split the single `Project498.WebApi` project into `Project498.Mvc` (frontend + app-DB controllers + proxy) and a slimmed `Project498.WebApi` (comics/characters backend protected by a hardcoded API key).

**Architecture:** The MVC project owns `AppDbContext` (users, checkouts) and serves static HTML from `wwwroot`. It exposes a catch-all `ProxyController` that forwards `/api/comics` and `/api/characters` requests to the backend, injecting a hardcoded service API key. The WebApi project owns `ComicsDbContext` exclusively and validates all inbound requests against that same hardcoded key via `ApiKeyMiddleware`.

**Tech Stack:** ASP.NET Core 10 MVC, ASP.NET Core 10 Web API, EF Core 10 + Npgsql, BCrypt.Net-Next, Microsoft.AspNetCore.Authentication.JwtBearer, System.Net.Http.Json (inbox), Docker/Compose.

**Design spec:** `docs/superpowers/specs/2026-04-28-mvc-api-split-design.md`

---

## File Map

### Created (Project498.Mvc — new project)
| File | Purpose |
|---|---|
| `src/Project498.Mvc/Project498.Mvc.csproj` | Project file with all required packages |
| `src/Project498.Mvc/Properties/launchSettings.json` | Dev launch on port 5031 |
| `src/Project498.Mvc/Constants/ApiKeyConstants.cs` | Hardcoded service-to-service API key |
| `src/Project498.Mvc/Models/ErrorResponse.cs` | Shared error response record |
| `src/Project498.Mvc/Models/User.cs` | User entity |
| `src/Project498.Mvc/Models/Checkout.cs` | Checkout entity |
| `src/Project498.Mvc/Models/DTOs/ChangePasswordDto.cs` | Change password DTO |
| `src/Project498.Mvc/Models/DTOs/UpdateUserDto.cs` | Update user DTO |
| `src/Project498.Mvc/Data/AppDbContext.cs` | EF context for users + checkouts |
| `src/Project498.Mvc/Data/DbSeeder.cs` | Seeds demo user on dev startup |
| `src/Project498.Mvc/Controllers/AuthController.cs` | `/api/auth` — register + login + JWT issuance |
| `src/Project498.Mvc/Controllers/UsersController.cs` | `/api/users` — CRUD + checks + password change |
| `src/Project498.Mvc/Controllers/CheckoutsController.cs` | `/api/checkouts` — checkout/return, calls backend via HttpClient for comic status |
| `src/Project498.Mvc/Controllers/ProxyController.cs` | `/api/{**path}` catch-all proxy |
| `src/Project498.Mvc/Program.cs` | MVC pipeline, JWT auth, HttpClient, DI |
| `src/Project498.Mvc/appsettings.json` | AppDb conn string, Jwt config, BackendApi:BaseUrl |
| `src/Project498.Mvc/appsettings.Development.json` | Dev overrides (localhost DB ports, backend URL) |
| `src/Project498.Mvc/Dockerfile` | Multi-stage build for MVC |
| `src/Project498.Mvc/wwwroot/**` | Static HTML/CSS/JS (moved from WebApi) |

### Created (Project498.WebApi — additions)
| File | Purpose |
|---|---|
| `src/Project498.WebApi/Constants/ApiKeyConstants.cs` | Same hardcoded API key as MVC |
| `src/Project498.WebApi/Middleware/ApiKeyMiddleware.cs` | Validates every inbound Bearer token against the constant |

### Modified (Project498.WebApi)
| File | Change |
|---|---|
| `src/Project498.WebApi/Project498.WebApi.csproj` | Remove BCrypt, JwtBearer, IdentityModel packages |
| `src/Project498.WebApi/Program.cs` | Remove AppDbContext, JWT auth, static files, seeding of app DB; add ApiKeyMiddleware |
| `src/Project498.WebApi/appsettings.json` | Remove AppConnection and Jwt sections |
| `src/Project498.WebApi/appsettings.Development.json` | Remove AppConnection |
| `src/Project498.WebApi/Controllers/ComicsController.cs` | Remove `[Authorize]` attributes (middleware protects all routes) |
| `src/Project498.WebApi/Controllers/CharactersController.cs` | Remove `[Authorize]` attributes |
| `src/compose.yaml` | Add `project498.mvc` service; strip host port from `project498.webapi` |

### Deleted (Project498.WebApi — files moving to Mvc)
- `src/Project498.WebApi/Controllers/AuthController.cs`
- `src/Project498.WebApi/Controllers/UsersController.cs`
- `src/Project498.WebApi/Controllers/CheckoutsController.cs`
- `src/Project498.WebApi/Data/AppDbContext.cs`
- `src/Project498.WebApi/Models/User.cs`
- `src/Project498.WebApi/Models/Checkout.cs`
- `src/Project498.WebApi/Models/DTOs/ChangePasswordDto.cs`
- `src/Project498.WebApi/Models/DTOs/UpdateUserDto.cs`
- `src/Project498.WebApi/wwwroot/` (entire directory)

### Modified (DbSeeder — split responsibilities)
- `src/Project498.WebApi/Data/DbSeeder.cs` — remove `SeedAppAsync` method (users stay in MVC)

### Modified (Solution)
- `src/Project498.sln` — add `Project498.Mvc` project entry

### Modified (Tests)
- `src/Project498.WebApi.Tests/Project498.WebApi.Tests.csproj` — add reference to `Project498.Mvc`
- `src/Project498.WebApi.Tests/UnitTests/UserControllerTests.cs` — update `using` to `Project498.Mvc.*`
- `src/Project498.WebApi.Tests/IntegrationTests/AuthControllerTests.cs` — update `using` to `Project498.Mvc.*`

### Modified (Frontend JS — fix hardcoded base URLs)
- `src/Project498.Mvc/wwwroot/js/home.js`
- `src/Project498.Mvc/wwwroot/js/comic-detail.js`
- `src/Project498.Mvc/wwwroot/js/checkout.js`

---

## Task 1: Scaffold Project498.Mvc project

**Files:**
- Create: `src/Project498.Mvc/Project498.Mvc.csproj`
- Create: `src/Project498.Mvc/Properties/launchSettings.json`

- [ ] **Step 1: Create the MVC project directory**

```bash
mkdir -p src/Project498.Mvc/Properties
```

- [ ] **Step 2: Create the project file**

Create `src/Project498.Mvc/Project498.Mvc.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
        <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.1" />
        <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.3">
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
            <PrivateAssets>all</PrivateAssets>
        </PackageReference>
        <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
        <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.9.0" />
    </ItemGroup>

</Project>
```

- [ ] **Step 3: Create launchSettings.json**

Create `src/Project498.Mvc/Properties/launchSettings.json`:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "",
      "applicationUrl": "http://localhost:5031",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:7035;http://localhost:5031",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

- [ ] **Step 4: Commit**

```bash
git add src/Project498.Mvc/
git commit -m "scaffold: add Project498.Mvc project file and launch settings"
```

---

## Task 2: Register MVC project in the solution

**Files:**
- Modify: `src/Project498.sln`

- [ ] **Step 1: Add the project to the solution**

Run from `src/`:

```bash
cd src && dotnet sln Project498.sln add Project498.Mvc/Project498.Mvc.csproj
```

Expected output:
```
Project `Project498.Mvc/Project498.Mvc.csproj` added to the solution.
```

- [ ] **Step 2: Verify the solution lists all three projects**

```bash
dotnet sln Project498.sln list
```

Expected output (order may vary):
```
Project(s)
----------
Project498.WebApi/Project498.WebApi.csproj
Project498.WebApi.Tests/Project498.WebApi.Tests.csproj
Project498.Mvc/Project498.Mvc.csproj
```

- [ ] **Step 3: Commit**

```bash
git add src/Project498.sln
git commit -m "scaffold: register Project498.Mvc in solution"
```

---

## Task 3: Add ApiKeyConstants to both projects

**Files:**
- Create: `src/Project498.Mvc/Constants/ApiKeyConstants.cs`
- Create: `src/Project498.WebApi/Constants/ApiKeyConstants.cs`

The API key is a hardcoded shared secret used for service-to-service authentication between the MVC frontend and the backend API. Both projects must contain this file with **exactly the same `ServiceApiKey` value**.

To rotate the key: update `ServiceApiKey` to the same new value in both files and redeploy both services.

- [ ] **Step 1: Create Constants directory in both projects**

```bash
mkdir -p src/Project498.Mvc/Constants src/Project498.WebApi/Constants
```

- [ ] **Step 2: Create ApiKeyConstants in the MVC project**

Create `src/Project498.Mvc/Constants/ApiKeyConstants.cs`:

```csharp
namespace Project498.Mvc.Constants;

/// <summary>
/// Holds the hardcoded service-to-service API key used by the MVC frontend
/// when forwarding requests to the backend Web API.
///
/// This key is attached as <c>Authorization: Bearer &lt;ServiceApiKey&gt;</c>
/// on every outbound request from <see cref="Controllers.ProxyController"/>
/// and <see cref="Controllers.CheckoutsController"/>, replacing any user JWT
/// that arrived from the browser. The backend's ApiKeyMiddleware validates it.
///
/// Security model: the backend API is not exposed on a public port in Docker,
/// so only the MVC project can reach it. This key is a second line of defense.
///
/// To rotate: set <see cref="ServiceApiKey"/> to the same new value in both
/// Project498.Mvc and Project498.WebApi, then redeploy both services.
/// </summary>
public static class ApiKeyConstants
{
    /// <summary>
    /// The shared secret that authorizes the MVC project to call the backend API.
    /// Must match <c>Project498.WebApi.Constants.ApiKeyConstants.ServiceApiKey</c> exactly.
    /// </summary>
    public const string ServiceApiKey = "project498-internal-service-key-2026";
}
```

- [ ] **Step 3: Create ApiKeyConstants in the WebApi project**

Create `src/Project498.WebApi/Constants/ApiKeyConstants.cs`:

```csharp
namespace Project498.WebApi.Constants;

/// <summary>
/// Holds the hardcoded service-to-service API key that the backend Web API
/// uses to authenticate inbound requests from the MVC frontend.
///
/// Every request to this API must include the header:
/// <c>Authorization: Bearer &lt;ServiceApiKey&gt;</c>
/// This is validated by <see cref="Middleware.ApiKeyMiddleware"/> before any
/// controller sees the request.
///
/// The backend does not issue or validate user JWTs — user authentication is
/// handled entirely by the MVC frontend. Only the MVC project is trusted here.
///
/// To rotate: set <see cref="ServiceApiKey"/> to the same new value in both
/// Project498.WebApi and Project498.Mvc, then redeploy both services.
/// </summary>
public static class ApiKeyConstants
{
    /// <summary>
    /// The shared secret that must appear as the Bearer token on every inbound request.
    /// Must match <c>Project498.Mvc.Constants.ApiKeyConstants.ServiceApiKey</c> exactly.
    /// </summary>
    public const string ServiceApiKey = "project498-internal-service-key-2026";
}
```

- [ ] **Step 4: Commit**

```bash
git add src/Project498.Mvc/Constants/ src/Project498.WebApi/Constants/
git commit -m "auth: add hardcoded service API key constant to both projects"
```

---

## Task 4: Create ApiKeyMiddleware in WebApi

**Files:**
- Create: `src/Project498.WebApi/Middleware/ApiKeyMiddleware.cs`

- [ ] **Step 1: Create the Middleware directory**

```bash
mkdir -p src/Project498.WebApi/Middleware
```

- [ ] **Step 2: Create ApiKeyMiddleware**

Create `src/Project498.WebApi/Middleware/ApiKeyMiddleware.cs`:

```csharp
using Project498.WebApi.Constants;

namespace Project498.WebApi.Middleware;

/// <summary>
/// ASP.NET Core middleware that enforces service-to-service authentication on every
/// request to the backend Web API.
///
/// <para>
/// This middleware is the sole authentication mechanism for the backend. It expects
/// every inbound HTTP request to carry the header:
/// <c>Authorization: Bearer {ApiKeyConstants.ServiceApiKey}</c>
/// </para>
///
/// <para>
/// The backend has no concept of user identity. User authentication is handled by
/// the MVC frontend (<c>Project498.Mvc</c>). By the time a request reaches this API,
/// the MVC proxy has already stripped the user's JWT and replaced it with this key.
/// </para>
///
/// <para>
/// If the header is absent or the token does not match <see cref="ApiKeyConstants.ServiceApiKey"/>,
/// the middleware short-circuits the pipeline and returns HTTP 401 with a JSON error body.
/// No controller code is invoked.
/// </para>
///
/// Registration: call <c>app.UseApiKeyAuthentication()</c> in <c>Program.cs</c> before
/// <c>app.MapControllers()</c>.
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Validates the incoming Authorization header against the hardcoded service API key.
    /// Passes the request to the next middleware only when the key matches.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Read the Authorization header. Format must be: "Bearer <key>"
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        // Check that the header is present and starts with "Bearer "
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await WriteUnauthorized(context, "MISSING_API_KEY", "Authorization header with Bearer token is required.");
            return;
        }

        // Extract the token value after "Bearer "
        var token = authHeader["Bearer ".Length..].Trim();

        // Compare the token to the hardcoded service key.
        // StringComparison.Ordinal ensures an exact byte-for-byte match — no culture or case folding.
        if (!string.Equals(token, ApiKeyConstants.ServiceApiKey, StringComparison.Ordinal))
        {
            await WriteUnauthorized(context, "INVALID_API_KEY", "The provided API key is not valid.");
            return;
        }

        // Key is valid — pass the request to the next middleware/controller.
        await _next(context);
    }

    /// <summary>
    /// Short-circuits the request pipeline with a 401 response and a JSON error body.
    /// </summary>
    private static async Task WriteUnauthorized(HttpContext context, string code, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { code, message });
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/Project498.WebApi/Middleware/
git commit -m "auth: add ApiKeyMiddleware to backend Web API"
```

---

## Task 5: Slim down the WebApi project

Remove all app-DB concerns and the moved controllers. Update `Program.cs`, the `.csproj`, and config files. Remove `[Authorize]` from the remaining controllers (the middleware now protects all routes). Register `ApiKeyMiddleware`.

**Files:**
- Delete: `src/Project498.WebApi/Controllers/AuthController.cs`
- Delete: `src/Project498.WebApi/Controllers/UsersController.cs`
- Delete: `src/Project498.WebApi/Controllers/CheckoutsController.cs`
- Delete: `src/Project498.WebApi/Data/AppDbContext.cs`
- Delete: `src/Project498.WebApi/Models/User.cs`
- Delete: `src/Project498.WebApi/Models/Checkout.cs`
- Delete: `src/Project498.WebApi/Models/DTOs/ChangePasswordDto.cs`
- Delete: `src/Project498.WebApi/Models/DTOs/UpdateUserDto.cs`
- Modify: `src/Project498.WebApi/Data/DbSeeder.cs`
- Modify: `src/Project498.WebApi/Controllers/ComicsController.cs`
- Modify: `src/Project498.WebApi/Controllers/CharactersController.cs`
- Modify: `src/Project498.WebApi/Program.cs`
- Modify: `src/Project498.WebApi/Project498.WebApi.csproj`
- Modify: `src/Project498.WebApi/appsettings.json`
- Modify: `src/Project498.WebApi/appsettings.Development.json`

- [ ] **Step 1: Delete moved controller files**

```bash
rm src/Project498.WebApi/Controllers/AuthController.cs \
   src/Project498.WebApi/Controllers/UsersController.cs \
   src/Project498.WebApi/Controllers/CheckoutsController.cs
```

- [ ] **Step 2: Delete moved model and data files**

```bash
rm src/Project498.WebApi/Data/AppDbContext.cs \
   src/Project498.WebApi/Models/User.cs \
   src/Project498.WebApi/Models/Checkout.cs \
   src/Project498.WebApi/Models/DTOs/ChangePasswordDto.cs \
   src/Project498.WebApi/Models/DTOs/UpdateUserDto.cs
```

- [ ] **Step 3: Remove SeedAppAsync from DbSeeder — only comics seeding belongs in WebApi**

Replace the entire contents of `src/Project498.WebApi/Data/DbSeeder.cs`:

```csharp
using Project498.WebApi.Models;

namespace Project498.WebApi.Data;

/// <summary>
/// Seeds the comics database with initial characters, comics, and character links
/// for development and first-run environments.
/// App DB seeding (users) is handled by Project498.Mvc.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedComicsAsync(ComicsDbContext comicsDb)
    {
        if (!comicsDb.Characters.Any())
        {
            comicsDb.Characters.AddRange(
                new Character { CharacterId = 1, Name = "Bruce Wayne", Alias = "Batman", Description = "The Dark Knight of Gotham City." },
                new Character { CharacterId = 2, Name = "Clark Kent", Alias = "Superman", Description = "The Man of Steel from Krypton." },
                new Character { CharacterId = 3, Name = "Diana Prince", Alias = "Wonder Woman", Description = "Amazonian warrior and princess." },
                new Character { CharacterId = 4, Name = "Barry Allen", Alias = "The Flash", Description = "The fastest man alive." },
                new Character { CharacterId = 5, Name = "Hal Jordan", Alias = "Green Lantern", Description = "Fearless member of the Green Lantern Corps." }
            );
            await comicsDb.SaveChangesAsync();
        }

        if (!comicsDb.Comics.Any())
        {
            comicsDb.Comics.AddRange(
                new Comic { ComicId = 1, Title = "Batman: Year One", IssueNumber = 1, YearPublished = 1987, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 2, Title = "Superman: Man of Steel", IssueNumber = 1, YearPublished = 1986, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 3, Title = "Wonder Woman: Gods and Mortals", IssueNumber = 1, YearPublished = 1987, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 4, Title = "The Flash: Born to Run", IssueNumber = 1, YearPublished = 1994, Publisher = "DC Comics", Status = "available", CheckedOutBy = null },
                new Comic { ComicId = 5, Title = "Green Lantern: Emerald Dawn", IssueNumber = 1, YearPublished = 1989, Publisher = "DC Comics", Status = "available", CheckedOutBy = null }
            );
            await comicsDb.SaveChangesAsync();
        }

        if (!comicsDb.ComicCharacters.Any())
        {
            comicsDb.ComicCharacters.AddRange(
                new ComicCharacter { ComicId = 1, CharacterId = 1 },
                new ComicCharacter { ComicId = 2, CharacterId = 2 },
                new ComicCharacter { ComicId = 3, CharacterId = 3 },
                new ComicCharacter { ComicId = 4, CharacterId = 4 },
                new ComicCharacter { ComicId = 5, CharacterId = 5 },
                new ComicCharacter { ComicId = 1, CharacterId = 4 },
                new ComicCharacter { ComicId = 2, CharacterId = 1 }
            );
            await comicsDb.SaveChangesAsync();
        }
    }
}
```

- [ ] **Step 4: Remove [Authorize] from ComicsController**

The middleware now enforces auth on all routes. Replace `src/Project498.WebApi/Controllers/ComicsController.cs` with the identical file but without the three `[Authorize]` attributes on `AddComic`, `EditComic`, and `DeleteComic`. Remove the `using Microsoft.AspNetCore.Authorization;` line too.

The three method signatures become:

```csharp
[HttpPost]
public async Task<IActionResult> AddComic([FromBody] ComicUpsertRequest request)

[HttpPut("{id}")]
public async Task<IActionResult> EditComic(int id, [FromBody] ComicUpsertRequest request)

[HttpDelete("{id}")]
public async Task<IActionResult> DeleteComic(int id)
```

Remove from the top of the file:
```csharp
using Microsoft.AspNetCore.Authorization;
```

- [ ] **Step 5: Remove [Authorize] from CharactersController**

Same pattern. Remove the three `[Authorize]` attributes on `AddCharacter`, `EditCharacter`, `DeleteCharacter` and remove `using Microsoft.AspNetCore.Authorization;` from the top.

The three method signatures become:

```csharp
[HttpPost]
public async Task<IActionResult> AddCharacter([FromBody] Character request)

[HttpPut("{id}")]
public async Task<IActionResult> EditCharacter(int id, [FromBody] Character request)

[HttpDelete("{id}")]
public async Task<IActionResult> DeleteCharacter(int id)
```

- [ ] **Step 6: Replace Program.cs — remove AppDb, JWT, static files; add ApiKeyMiddleware**

Replace the full contents of `src/Project498.WebApi/Program.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Project498.WebApi.Data;
using Project498.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

// ComicsDbContext is the only database this service owns.
// AppDbContext (users, checkouts) lives in Project498.Mvc.
builder.Services.AddDbContext<ComicsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ComicsConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var comicsDb = scope.ServiceProvider.GetRequiredService<ComicsDbContext>();
    await comicsDb.Database.EnsureCreatedAsync();
    await DbSeeder.SeedComicsAsync(comicsDb);

    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Project498 Backend API");
    });
}

app.UseHttpsRedirection();

// Validate the service-to-service API key on every inbound request.
// This replaces user JWT authentication — the backend has no concept of user identity.
// Only Project498.Mvc is trusted to call this API.
// See: Project498.WebApi.Middleware.ApiKeyMiddleware
// See: Project498.WebApi.Constants.ApiKeyConstants
app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();

app.Run();
```

- [ ] **Step 7: Update the WebApi .csproj — remove packages no longer needed**

Replace `src/Project498.WebApi/Project498.WebApi.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.1" />
        <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.3">
          <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
          <PrivateAssets>all</PrivateAssets>
        </PackageReference>
        <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
        <PackageReference Include="Swashbuckle.AspNetCore.SwaggerUI" Version="10.1.2" />
    </ItemGroup>

    <ItemGroup>
      <Content Include="..\.dockerignore">
        <Link>.dockerignore</Link>
      </Content>
    </ItemGroup>

</Project>
```

Packages removed: `BCrypt.Net-Next`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`.

- [ ] **Step 8: Update appsettings.json — remove AppDb and Jwt sections**

Replace `src/Project498.WebApi/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "ComicsConnection": "Host=localhost;Database=project498_comics;Username=postgres;Password=postgres"
  }
}
```

- [ ] **Step 9: Update appsettings.Development.json — remove AppConnection**

Replace `src/Project498.WebApi/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "ComicsConnection": "Host=localhost;Port=5433;Database=project498_comics;Username=postgres;Password=postgres"
  }
}
```

- [ ] **Step 10: Verify WebApi still builds**

```bash
cd src && dotnet build Project498.WebApi/Project498.WebApi.csproj
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 11: Commit**

```bash
git add src/Project498.WebApi/
git commit -m "refactor: slim down WebApi to comics/characters + ApiKeyMiddleware only"
```

---

## Task 6: Create MVC data layer

**Files:**
- Create: `src/Project498.Mvc/Models/ErrorResponse.cs`
- Create: `src/Project498.Mvc/Models/User.cs`
- Create: `src/Project498.Mvc/Models/Checkout.cs`
- Create: `src/Project498.Mvc/Models/DTOs/ChangePasswordDto.cs`
- Create: `src/Project498.Mvc/Models/DTOs/UpdateUserDto.cs`
- Create: `src/Project498.Mvc/Data/AppDbContext.cs`
- Create: `src/Project498.Mvc/Data/DbSeeder.cs`

- [ ] **Step 1: Create directory structure**

```bash
mkdir -p src/Project498.Mvc/Models/DTOs src/Project498.Mvc/Data
```

- [ ] **Step 2: Create ErrorResponse.cs**

Create `src/Project498.Mvc/Models/ErrorResponse.cs`:

```csharp
namespace Project498.Mvc.Models;

/// <summary>
/// Standard error response body returned by all API endpoints on validation
/// or domain failures. Consumers can use <see cref="Code"/> for programmatic
/// handling and <see cref="Message"/> for display.
/// </summary>
public record ErrorResponse(string Code, string Message);
```

- [ ] **Step 3: Create User.cs**

Create `src/Project498.Mvc/Models/User.cs`:

```csharp
namespace Project498.Mvc.Models;

public class User
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public ICollection<Checkout> Checkouts { get; set; } = new List<Checkout>();
}
```

- [ ] **Step 4: Create Checkout.cs**

Create `src/Project498.Mvc/Models/Checkout.cs`:

```csharp
namespace Project498.Mvc.Models;

public class Checkout
{
    public int CheckoutId { get; set; }
    public int UserId { get; set; }
    public int ComicId { get; set; }
    public DateTime CheckoutDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}
```

- [ ] **Step 5: Create ChangePasswordDto.cs**

Create `src/Project498.Mvc/Models/DTOs/ChangePasswordDto.cs`:

```csharp
namespace Project498.Mvc.Models.DTOs;

public class ChangePasswordDto
{
    public int UserId { get; set; }
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

- [ ] **Step 6: Create UpdateUserDto.cs**

Create `src/Project498.Mvc/Models/DTOs/UpdateUserDto.cs`:

```csharp
namespace Project498.Mvc.Models.DTOs;

public class UpdateUserDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
}
```

- [ ] **Step 7: Create AppDbContext.cs**

Create `src/Project498.Mvc/Data/AppDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Project498.Mvc.Models;

namespace Project498.Mvc.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Checkout> Checkouts => Set<Checkout>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100).HasColumnName("first_name");
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100).HasColumnName("last_name");
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100).HasColumnName("username");
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100).HasColumnName("email");
            entity.Property(e => e.Password).IsRequired().HasColumnName("password");
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Checkout>(entity =>
        {
            entity.ToTable("Checkouts");
            entity.HasKey(e => e.CheckoutId);
            entity.Property(e => e.CheckoutId).HasColumnName("checkout_id");
            entity.Property(e => e.UserId).IsRequired().HasColumnName("user_id");
            entity.Property(e => e.ComicId).IsRequired().HasColumnName("comic_id");
            entity.Property(e => e.CheckoutDate).IsRequired().HasColumnName("checkout_date");
            entity.Property(e => e.DueDate).IsRequired().HasColumnName("due_date");
            entity.Property(e => e.ReturnDate).HasColumnName("return_date");
            entity.Property(e => e.Status).IsRequired().HasColumnName("status");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Checkouts)
                  .HasForeignKey(e => e.UserId);
        });
    }
}
```

- [ ] **Step 8: Create DbSeeder.cs**

Create `src/Project498.Mvc/Data/DbSeeder.cs`:

```csharp
using Project498.Mvc.Models;

namespace Project498.Mvc.Data;

/// <summary>
/// Seeds the app database (users) with a demo account on first run.
/// Comics seeding is handled by Project498.WebApi.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAppAsync(AppDbContext db)
    {
        if (!db.Users.Any())
        {
            db.Users.Add(new User
            {
                FirstName = "Demo",
                LastName = "Demoson",
                Username = "demo",
                Email = "demo@demo.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Demo123")
            });

            await db.SaveChangesAsync();
        }
    }
}
```

- [ ] **Step 9: Commit**

```bash
git add src/Project498.Mvc/Models/ src/Project498.Mvc/Data/
git commit -m "feat(mvc): add models, DTOs, AppDbContext, and DbSeeder"
```

---

## Task 7: Create MVC AuthController and UsersController

**Files:**
- Create: `src/Project498.Mvc/Controllers/AuthController.cs`
- Create: `src/Project498.Mvc/Controllers/UsersController.cs`

- [ ] **Step 1: Create the Controllers directory**

```bash
mkdir -p src/Project498.Mvc/Controllers
```

- [ ] **Step 2: Create AuthController.cs**

Create `src/Project498.Mvc/Controllers/AuthController.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Project498.Mvc.Data;
using Project498.Mvc.Models;

namespace Project498.Mvc.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password) ||
            string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.FirstName) ||
            string.IsNullOrWhiteSpace(user.LastName))
        {
            return BadRequest(new ErrorResponse("VALIDATION_ERROR", "All registration fields are required."));
        }

        if (await _context.Users.AnyAsync(u => u.Username == user.Username))
        {
            return BadRequest(new ErrorResponse("USERNAME_EXISTS", "Username already exists."));
        }

        if (await _context.Users.AnyAsync(u => u.Email == user.Email))
        {
            return BadRequest(new ErrorResponse("EMAIL_EXISTS", "Email already exists."));
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Ok(new { message = "User registered successfully." });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var user = _context.Users.SingleOrDefault(u => u.Username == request.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            return Unauthorized(new ErrorResponse("INVALID_CREDENTIALS", "Invalid username or password."));

        var token = GenerateToken(user);
        return Ok(new { access_token = token, token });
    }

    private string GenerateToken(User user)
    {
        var secret = _configuration["Jwt:Secret"]!;
        var expiresInHours = int.Parse(_configuration["Jwt:ExpiresInHours"]!);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("user_id", user.UserId.ToString()),
            new Claim("username", user.Username)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiresInHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

- [ ] **Step 3: Create UsersController.cs**

Create `src/Project498.Mvc/Controllers/UsersController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project498.Mvc.Data;
using Project498.Mvc.Models;
using Project498.Mvc.Models.DTOs;

namespace Project498.Mvc.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await _context.Users.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new ErrorResponse("USER_NOT_FOUND", $"User {id} was not found."));
        }

        return user;
    }

    [HttpPost]
    public async Task<ActionResult<User>> AddUser(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Username) ||
            string.IsNullOrWhiteSpace(user.Password))
        {
            return BadRequest(new ErrorResponse("VALIDATION_ERROR", "Username and password are required."));
        }

        if (await _context.Users.AnyAsync(u => u.Username == user.Username))
        {
            return Conflict(new ErrorResponse("USERNAME_EXISTS", "Username already exists."));
        }

        if (await _context.Users.AnyAsync(u => u.Email == user.Email))
        {
            return Conflict(new ErrorResponse("EMAIL_EXISTS", "Email already exists."));
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> EditUser(int id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new ErrorResponse("USER_NOT_FOUND", $"User {id} was not found."));
        }

        user.FirstName = dto.FirstName ?? user.FirstName;
        user.LastName = dto.LastName ?? user.LastName;
        user.Username = dto.Username ?? user.Username;
        user.Email = dto.Email ?? user.Email;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new ErrorResponse("USER_NOT_FOUND", $"User {id} was not found."));
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("check-username")]
    public async Task<IActionResult> CheckUsername(string username)
    {
        bool exists = await _context.Users.AnyAsync(u => u.Username == username);
        return Ok(new { exists });
    }

    [HttpGet("check-email")]
    public async Task<IActionResult> CheckEmail(string email)
    {
        bool exists = await _context.Users.AnyAsync(u => u.Email == email);
        return Ok(new { exists });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);

        if (user == null)
            return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.Password))
            return BadRequest("Incorrect old password");

        if (dto.NewPassword != dto.ConfirmPassword)
            return BadRequest("Passwords do not match");

        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();

        return Ok("Password updated");
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add src/Project498.Mvc/Controllers/AuthController.cs \
        src/Project498.Mvc/Controllers/UsersController.cs
git commit -m "feat(mvc): add AuthController and UsersController"
```

---

## Task 8: Create MVC CheckoutsController

The checkout controller lives in the MVC project and owns only `AppDbContext`. Operations that need to read or update comic status (checkout, return) call the backend API via a named `HttpClient` injected through `IHttpClientFactory`. This keeps all comic-DB logic inside `Project498.WebApi`.

**Files:**
- Create: `src/Project498.Mvc/Controllers/CheckoutsController.cs`

- [ ] **Step 1: Create CheckoutsController.cs**

Create `src/Project498.Mvc/Controllers/CheckoutsController.cs`:

```csharp
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

    public CheckoutsController(AppDbContext appDb, IHttpClientFactory httpClientFactory)
    {
        _appDb = appDb;
        _httpClientFactory = httpClientFactory;
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
            await backend.PutAsJsonAsync($"api/comics/{checkout.ComicId}", statusUpdate);
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
        var client = _httpClientFactory.CreateClient("backend");
        // Replace any existing Authorization header with the service API key.
        // The backend's ApiKeyMiddleware validates this on every request.
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
public record CheckoutRequest(int ComicId);

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
```

- [ ] **Step 2: Commit**

```bash
git add src/Project498.Mvc/Controllers/CheckoutsController.cs
git commit -m "feat(mvc): add CheckoutsController with HttpClient-based comic status updates"
```

---

## Task 9: Create MVC ProxyController

**Files:**
- Create: `src/Project498.Mvc/Controllers/ProxyController.cs`

- [ ] **Step 1: Create ProxyController.cs**

Create `src/Project498.Mvc/Controllers/ProxyController.cs`:

```csharp
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
        var outboundRequest = new HttpRequestMessage(
            new HttpMethod(Request.Method),
            targetPath
        );

        // Copy the request body for methods that carry one (POST, PUT, PATCH).
        if (Request.ContentLength > 0 || Request.Headers.ContainsKey("Transfer-Encoding"))
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
        var backendResponse = await client.SendAsync(
            outboundRequest,
            HttpCompletionOption.ResponseHeadersRead
        );

        // Stream the response back to the caller without buffering.
        Response.StatusCode = (int)backendResponse.StatusCode;

        if (backendResponse.Content.Headers.ContentType is not null)
        {
            Response.ContentType = backendResponse.Content.Headers.ContentType.ToString();
        }

        await backendResponse.Content.CopyToAsync(Response.Body);

        return new EmptyResult();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Project498.Mvc/Controllers/ProxyController.cs
git commit -m "feat(mvc): add generic catch-all ProxyController"
```

---

## Task 10: Create MVC Program.cs and configuration files

**Files:**
- Create: `src/Project498.Mvc/Program.cs`
- Create: `src/Project498.Mvc/appsettings.json`
- Create: `src/Project498.Mvc/appsettings.Development.json`

- [ ] **Step 1: Create Program.cs**

Create `src/Project498.Mvc/Program.cs`:

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Project498.Mvc.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// App database — users and checkouts.
// Comics/characters live in Project498.WebApi and are accessed via HttpClient.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AppConnection")));

// JWT authentication for user-facing requests.
// Tokens are issued by AuthController and validated here on protected MVC endpoints.
// Note: the service-to-service API key (for outbound calls to the backend) is handled
// separately via ApiKeyConstants — it is NOT related to this JWT configuration.
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
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
    client.BaseAddress = new Uri(builder.Configuration["BackendApi:BaseUrl"]!);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DbSeeder.SeedAppAsync(db);
}

app.UseHttpsRedirection();

// Serve static HTML/CSS/JS from wwwroot (index.html, login.html, etc.).
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

- [ ] **Step 2: Create appsettings.json**

Create `src/Project498.Mvc/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "AppConnection": "Host=localhost;Database=project498_app;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "your-super-secret-key-change-this-in-production-min-32-chars",
    "ExpiresInHours": 1
  },
  "BackendApi": {
    "BaseUrl": "http://localhost:5100"
  }
}
```

- [ ] **Step 3: Create appsettings.Development.json**

Create `src/Project498.Mvc/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "AppConnection": "Host=localhost;Port=5432;Database=project498_app;Username=postgres;Password=postgres"
  },
  "BackendApi": {
    "BaseUrl": "http://localhost:5100"
  }
}
```

- [ ] **Step 4: Update WebApi launchSettings to use port 5100**

The WebApi backend must run on port 5100 locally so the MVC project can reach it at `http://localhost:5100`. Replace the http profile's `applicationUrl` in `src/Project498.WebApi/Properties/launchSettings.json`:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5100",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:7100;http://localhost:5100",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

- [ ] **Step 5: Verify MVC project builds**

```bash
cd src && dotnet build Project498.Mvc/Project498.Mvc.csproj
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 6: Commit**

```bash
git add src/Project498.Mvc/Program.cs \
        src/Project498.Mvc/appsettings.json \
        src/Project498.Mvc/appsettings.Development.json \
        src/Project498.WebApi/Properties/launchSettings.json
git commit -m "feat(mvc): add Program.cs and configuration files"
```

---

## Task 11: Move wwwroot and fix JS base URLs

Move the static frontend assets from the WebApi project to the MVC project. Update the three JS files that use hardcoded `http://localhost:8080` URLs to use relative paths — since the MVC project serves both the static files and the proxy from the same origin, relative paths work in all environments without configuration.

**Files:**
- Move: `src/Project498.WebApi/wwwroot/` → `src/Project498.Mvc/wwwroot/`
- Modify: `src/Project498.Mvc/wwwroot/js/home.js`
- Modify: `src/Project498.Mvc/wwwroot/js/comic-detail.js`
- Modify: `src/Project498.Mvc/wwwroot/js/checkout.js`

- [ ] **Step 1: Move the entire wwwroot directory**

```bash
mv src/Project498.WebApi/wwwroot src/Project498.Mvc/wwwroot
```

- [ ] **Step 2: Fix home.js — replace hardcoded URL with relative path**

In `src/Project498.Mvc/wwwroot/js/home.js`, change line 11:

```js
// Before:
const response = await fetch("http://localhost:8080/api/comics");

// After:
const response = await fetch("/api/comics");
```

- [ ] **Step 3: Fix comic-detail.js — replace two hardcoded URLs**

In `src/Project498.Mvc/wwwroot/js/comic-detail.js`:

Line 55 — change:
```js
// Before:
const response = await fetch(`http://localhost:8080/api/comics/${comicId}`);

// After:
const response = await fetch(`/api/comics/${comicId}`);
```

Line 111 — change:
```js
// Before:
const response = await fetch("http://localhost:8080/api/checkouts", {

// After:
const response = await fetch("/api/checkouts", {
```

- [ ] **Step 4: Fix checkout.js — replace four hardcoded URLs**

In `src/Project498.Mvc/wwwroot/js/checkout.js`:

Line 41:
```js
// Before:
const userResponse = await fetch(`http://localhost:8080/api/checkouts/user/${userId}`, {
// After:
const userResponse = await fetch(`/api/checkouts/user/${userId}`, {
```

Line 53:
```js
// Before:
const allResponse = await fetch(`http://localhost:8080/api/checkouts`, {
// After:
const allResponse = await fetch(`/api/checkouts`, {
```

Line 88:
```js
// Before:
const comicResponse = await fetch(`http://localhost:8080/api/comics/${c.comicId}`);
// After:
const comicResponse = await fetch(`/api/comics/${c.comicId}`);
```

Line 131:
```js
// Before:
const response = await fetch(`http://localhost:8080/api/checkouts/${checkoutId}`, {
// After:
const response = await fetch(`/api/checkouts/${checkoutId}`, {
```

- [ ] **Step 5: Commit**

```bash
git add src/Project498.Mvc/wwwroot/ src/Project498.WebApi/
git commit -m "refactor: move wwwroot to MVC project, fix hardcoded JS base URLs to relative paths"
```

---

## Task 12: Create MVC Dockerfile and update compose.yaml

**Files:**
- Create: `src/Project498.Mvc/Dockerfile`
- Modify: `src/compose.yaml`

- [ ] **Step 1: Create MVC Dockerfile**

Create `src/Project498.Mvc/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Project498.Mvc/Project498.Mvc.csproj", "Project498.Mvc/"]
RUN dotnet restore "Project498.Mvc/Project498.Mvc.csproj"
COPY . .
WORKDIR "/src/Project498.Mvc"
RUN dotnet build "./Project498.Mvc.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Project498.Mvc.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Project498.Mvc.dll"]
```

- [ ] **Step 2: Replace compose.yaml with the updated five-service file**

Replace the entire contents of `src/compose.yaml`:

```yaml
services:
  # Public-facing MVC frontend.
  # Serves static HTML from wwwroot and proxies /api/comics and /api/characters
  # to project498.webapi. All user auth (JWT) is handled here.
  project498.mvc:
    image: project498.mvc
    build:
      context: .
      dockerfile: Project498.Mvc/Dockerfile
    environment:
      - ConnectionStrings__AppConnection=Host=db;Database=project498_app;Username=postgres;Password=postgres
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://0.0.0.0:8080
      - Jwt__Secret=your-super-secret-key-change-this-in-production-min-32-chars
      - Jwt__ExpiresInHours=1
      # Tell the MVC proxy where to find the backend API inside the Docker network.
      - BackendApi__BaseUrl=http://project498.webapi:8080
    depends_on:
      db:
        condition: service_healthy
      project498.webapi:
        condition: service_started
    ports:
      - "8080:8080"

  # Internal backend Web API — comics and characters only.
  # NOT exposed on a host port: only reachable from within the Docker network.
  # Every inbound request must carry Authorization: Bearer <ApiKeyConstants.ServiceApiKey>.
  project498.webapi:
    image: project498.webapi
    build:
      context: .
      dockerfile: Project498.WebApi/Dockerfile
    environment:
      - ConnectionStrings__ComicsConnection=Host=db_comics;Database=project498_comics;Username=postgres;Password=postgres
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://0.0.0.0:8080
    depends_on:
      db_comics:
        condition: service_healthy

  db:
    image: postgres:latest
    environment:
      POSTGRES_DB: project498_app
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 2s
      timeout: 3s
      retries: 30
      start_period: 5s

  db_comics:
    image: postgres:latest
    environment:
      POSTGRES_DB: project498_comics
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5433:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 2s
      timeout: 3s
      retries: 30
      start_period: 5s
```

- [ ] **Step 3: Commit**

```bash
git add src/Project498.Mvc/Dockerfile src/compose.yaml
git commit -m "infra: add MVC Dockerfile and update compose.yaml for two-service split"
```

---

## Task 13: Update test project for moved controllers

The `Project498.WebApi.Tests` project references `Project498.WebApi`. The `UserControllerTests` and `AuthControllerTests` test controllers that have moved to `Project498.Mvc`. These test files need to reference the MVC project instead.

**Files:**
- Modify: `src/Project498.WebApi.Tests/Project498.WebApi.Tests.csproj`
- Modify: `src/Project498.WebApi.Tests/UnitTests/UserControllerTests.cs`
- Modify: `src/Project498.WebApi.Tests/IntegrationTests/AuthControllerTests.cs`

- [ ] **Step 1: Add a project reference to Project498.Mvc in the test csproj**

In `src/Project498.WebApi.Tests/Project498.WebApi.Tests.csproj`, add inside the existing `<ItemGroup>` that has `PackageReference` entries (or create a new `<ItemGroup>`):

```xml
<ItemGroup>
  <ProjectReference Include="..\Project498.WebApi\Project498.WebApi.csproj" />
  <ProjectReference Include="..\Project498.Mvc\Project498.Mvc.csproj" />
</ItemGroup>
```

- [ ] **Step 2: Update using directives in UserControllerTests.cs**

Open `src/Project498.WebApi.Tests/UnitTests/UserControllerTests.cs`. Replace all occurrences of:
- `Project498.WebApi.Controllers` → `Project498.Mvc.Controllers`
- `Project498.WebApi.Data` → `Project498.Mvc.Data`
- `Project498.WebApi.Models` → `Project498.Mvc.Models`

- [ ] **Step 3: Update using directives in AuthControllerTests.cs**

Open `src/Project498.WebApi.Tests/IntegrationTests/AuthControllerTests.cs`. Replace all occurrences of:
- `Project498.WebApi.Controllers` → `Project498.Mvc.Controllers`
- `Project498.WebApi.Data` → `Project498.Mvc.Data`
- `Project498.WebApi.Models` → `Project498.Mvc.Models`

- [ ] **Step 4: Run all tests**

```bash
cd src && dotnet test Project498.WebApi.Tests/Project498.WebApi.Tests.csproj --verbosity normal
```

Expected: all tests pass. If any test fails due to a missing type or namespace, trace it to the rename above and fix it.

- [ ] **Step 5: Commit**

```bash
git add src/Project498.WebApi.Tests/
git commit -m "test: update test project references and namespaces for moved controllers"
```

---

## Task 14: Full solution build and smoke-test verification

- [ ] **Step 1: Restore all packages**

```bash
cd src && dotnet restore Project498.sln
```

Expected: no errors.

- [ ] **Step 2: Build the entire solution**

```bash
cd src && dotnet build Project498.sln
```

Expected: `Build succeeded.` for all three projects, 0 errors.

- [ ] **Step 3: Run all tests**

```bash
cd src && dotnet test Project498.sln --verbosity normal
```

Expected: all tests pass.

- [ ] **Step 4: Verify WebApi's launchSettings now point to port 5100**

```bash
grep applicationUrl src/Project498.WebApi/Properties/launchSettings.json
```

Expected: `http://localhost:5100`.

- [ ] **Step 5: Verify no remaining hardcoded localhost:8080 in JS files**

```bash
grep -r "localhost:8080" src/Project498.Mvc/wwwroot/
```

Expected: no output (zero matches).

- [ ] **Step 6: Verify wwwroot no longer exists in WebApi**

```bash
ls src/Project498.WebApi/wwwroot 2>&1
```

Expected: `No such file or directory`.

- [ ] **Step 7: Final commit**

```bash
git add -A
git commit -m "chore: final verification — MVC/API split complete"
```

---

## Local Dev Quick-Start (post-refactor)

Run both projects simultaneously (two terminals):

```bash
# Terminal 1 — backend API on port 5100
cd src && dotnet run --project Project498.WebApi

# Terminal 2 — MVC frontend on port 5031
cd src && dotnet run --project Project498.Mvc
```

Open `http://localhost:5031` in a browser.
