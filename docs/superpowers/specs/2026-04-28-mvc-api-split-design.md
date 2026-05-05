# Design: Split Web API into MVC Frontend + Web API Backend

**Date:** 2026-04-28
**Branch:** mvc2
**Status:** Approved

---

## Overview

The existing `Project498.WebApi` project is a single ASP.NET Core 10 application that serves both a static HTML/JS frontend and a REST API. This design splits it into two projects within the same solution:

1. **`Project498.Mvc`** — An ASP.NET Core MVC project that serves the static frontend, handles user-facing actions (auth, users, checkouts), and proxies content-related API calls to the backend.
2. **`Project498.WebApi`** — A slimmed-down ASP.NET Core Web API project that hosts only content-related endpoints (comics, characters) and requires a hardcoded bearer token to access.

The split is driven by database ownership: the MVC project exclusively uses `AppDbContext` (users, checkouts) and the Web API project exclusively uses `ComicsDbContext` (comics, characters).

---

## Section 1: Solution Structure

The existing `Project498.sln` is updated to reference both projects. No shared library project is introduced — the two projects share no code.

```
src/
├── Project498.sln                            # references all three projects
│
├── Project498.Mvc/                           # NEW — MVC frontend project
│   ├── Project498.Mvc.csproj
│   ├── Program.cs                            # MVC pipeline, JWT auth, HttpClient, DI
│   ├── Controllers/
│   │   ├── AuthController.cs                 # moved from WebApi — uses AppDbContext
│   │   ├── UsersController.cs                # moved from WebApi — uses AppDbContext
│   │   ├── CheckoutsController.cs            # moved from WebApi — uses AppDbContext
│   │   └── ProxyController.cs                # NEW — generic catch-all proxy
│   ├── Constants/
│   │   └── ApiKeyConstants.cs                # NEW — hardcoded service-to-service API key
│   ├── wwwroot/                              # moved from WebApi
│   │   ├── index.html
│   │   ├── login.html
│   │   ├── createAccount.html
│   │   ├── userInfo.html
│   │   ├── checkout.html
│   │   ├── comic-detail.html
│   │   ├── css/styles.css
│   │   └── js/
│   │       ├── home.js
│   │       ├── login.js
│   │       ├── createAccount.js
│   │       ├── userInfo.js
│   │       ├── checkout.js
│   │       ├── comic-detail.js
│   │       └── mockData.js
│   ├── appsettings.json                      # BackendApi:BaseUrl, Jwt config, connection string
│   ├── appsettings.Development.json
│   └── Properties/launchSettings.json
│
├── Project498.WebApi/                        # MODIFIED — backend API only
│   ├── Project498.WebApi.csproj
│   ├── Program.cs                            # API pipeline, ApiKey middleware, DI
│   ├── Controllers/
│   │   ├── ComicsController.cs               # unchanged
│   │   └── CharactersController.cs           # unchanged
│   ├── Constants/
│   │   └── ApiKeyConstants.cs                # NEW — same hardcoded key as MVC project
│   ├── Middleware/
│   │   └── ApiKeyMiddleware.cs               # NEW — validates incoming bearer token
│   ├── appsettings.json                      # ComicsDb connection string only
│   ├── appsettings.Development.json
│   ├── Dockerfile                            # updated
│   └── Properties/launchSettings.json
│
└── Project498.WebApi.Tests/                  # unchanged
    └── ...
```

### What moves vs. what stays

| Artifact | From | To |
|---|---|---|
| `AuthController` | WebApi | Mvc |
| `UsersController` | WebApi | Mvc |
| `CheckoutsController` | WebApi | Mvc |
| `AppDbContext` + models | WebApi | Mvc |
| `wwwroot/` (all static files) | WebApi | Mvc |
| `ComicsController` | WebApi | WebApi (stays) |
| `CharactersController` | WebApi | WebApi (stays) |
| `ComicsDbContext` + models | WebApi | WebApi (stays) |
| JWT auth configuration | WebApi | Mvc |
| Swagger/OpenAPI | WebApi | WebApi (stays) |

---

## Section 2: Authentication & Security

### User-facing authentication (MVC project)

The existing JWT flow is preserved unchanged:

- Users authenticate via `POST /api/auth/login`, which returns a signed JWT
- The frontend JS stores the token in `localStorage` and sends it as `Authorization: Bearer <token>` on protected requests
- The MVC project validates the token using `Microsoft.AspNetCore.Authentication.JwtBearer` configured with `Jwt:Secret` from `appsettings.json`
- Protected MVC controllers (`CheckoutsController`, etc.) use `[Authorize]` as today

### Service-to-service authentication (MVC → Backend API)

A hardcoded API key secures the internal channel. It is **not** a JWT — it is a simple static string that acts as a shared secret between the two projects.

**The key is defined in both projects as:**
```csharp
// Project498.Mvc/Constants/ApiKeyConstants.cs
// Project498.WebApi/Constants/ApiKeyConstants.cs
public static class ApiKeyConstants
{
    public const string ServiceApiKey = "...same literal value in both projects...";
}
```

**Trust model:**
- The MVC `ProxyController` always attaches `Authorization: Bearer <ServiceApiKey>` to outbound requests, **replacing** any user JWT that arrived from the browser
- The backend `ApiKeyMiddleware` checks every request's `Authorization` header against `ApiKeyConstants.ServiceApiKey` and returns `401` if it does not match
- The backend has no knowledge of user JWTs and performs no user-level authorization — it trusts the MVC project to enforce user auth before proxying
- The backend's port is not exposed on the Docker host, making it unreachable from outside the Docker network

**Why user JWT is stripped at the proxy:**
The backend API must never receive user tokens directly. The MVC project is the sole authorized caller. Replacing the header at the proxy enforces this boundary at the transport layer regardless of what the browser sends.

**Documentation surfaces for the API key:**
1. `ApiKeyConstants.cs` in both projects — XML doc on the class and the constant
2. `ApiKeyMiddleware.cs` — XML doc on the class and inline comments on the validation logic
3. `ProxyController.cs` — inline comment on the header replacement step
4. This design document
5. `README.md` — a section explaining the two-project auth model

**Rotating the key:**
Update `ApiKeyConstants.ServiceApiKey` to the same new value in both projects and redeploy both. There is no external secret store — this is intentional for simplicity at this stage.

---

## Section 3: The Generic Proxy Controller

`ProxyController` is the bridge between the browser-facing MVC project and the backend API. It is a single ASP.NET Core controller with one catch-all action method.

### Route

```csharp
[Route("api/{**path}")]
```

This route matches any request under `/api/` that is not claimed by a more specific controller. Because `AuthController`, `UsersController`, and `CheckoutsController` each register concrete routes (`/api/auth`, `/api/users`, `/api/checkouts`), ASP.NET Core's routing system prefers them. The proxy fires only for `/api/comics` and `/api/characters` paths.

### Request lifecycle (per incoming request)

1. **Extract path and query** — read `{path}` from the route and `Request.QueryString` from the incoming request
2. **Construct backend URL** — combine `BackendApi:BaseUrl` (from config) + `/api/` + `{path}` + query string
3. **Build outbound request** — create `HttpRequestMessage` with the same HTTP method (`GET`, `POST`, `PUT`, `DELETE`) and body stream
4. **Copy safe headers** — forward `Content-Type` from the incoming request; deliberately skip `Authorization` and any hop-by-hop headers
5. **Inject service token** — set `Authorization: Bearer <ApiKeyConstants.ServiceApiKey>` on the outbound request
6. **Send** — dispatch via the named `HttpClient` (`"backend"`) managed by `IHttpClientFactory`
7. **Stream response** — copy the backend's status code, `Content-Type` response header, and body stream directly back to the caller without buffering

### What the proxy does NOT do

- It does not inspect, validate, or transform the request body
- It does not cache responses
- It does not retry on failure — a non-2xx from the backend is returned to the caller as-is
- It does not log request bodies (only method + path for debugging)

### HttpClient registration

The named `"backend"` client is registered in `Program.cs`:

```csharp
builder.Services.AddHttpClient("backend", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BackendApi:BaseUrl"]!);
});
```

`BaseUrl` is set to:
- `http://localhost:5100` in `appsettings.Development.json`
- `http://project498.webapi:8080` in Docker (via environment variable override)

### Documentation in code

Every non-obvious step in `ProxyController` has an inline comment. The class itself has a full XML doc block covering:
- What the proxy is and why it exists
- Which routes it handles and which it defers to specific controllers
- The security model (token replacement)
- How to add a new backend controller without modifying the proxy

---

## Section 4: Containerization

### Dockerfiles

Both projects follow the same multi-stage build pattern as the existing `Project498.WebApi/Dockerfile`.

**`Project498.Mvc/Dockerfile`:**
- Build stage: `mcr.microsoft.com/dotnet/sdk:10.0` — restores and publishes `Project498.Mvc`
- Runtime stage: `mcr.microsoft.com/dotnet/aspnet:10.0` — copies publish output
- Exposes port `8080`
- `ENTRYPOINT ["dotnet", "Project498.Mvc.dll"]`

**`Project498.WebApi/Dockerfile`** (updated):
- Same pattern, now publishes only `Project498.WebApi`
- Exposes port `8080`
- `ENTRYPOINT ["dotnet", "Project498.WebApi.dll"]`

### `compose.yaml` — five services

```
project498.mvc        → public-facing, port 8080:8080
project498.webapi     → internal only, NO host port mapping
db                    → PostgreSQL (app DB), port 5432:5432
db_comics             → PostgreSQL (comics DB), port 5433:5432
```

**`project498.mvc` environment variables:**
- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection=<app db connection string>`
- `Jwt__Secret=<jwt signing secret>`
- `Jwt__ExpiresInHours=1`
- `BackendApi__BaseUrl=http://project498.webapi:8080`

**`project498.webapi` environment variables:**
- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__ComicsConnection=<comics db connection string>`

**`depends_on` / health checks:**
- `project498.mvc` depends on `db` (healthy) and `project498.webapi` (started)
- `project498.webapi` depends on `db_comics` (healthy)
- Both database services retain existing `pg_isready` health checks

**Why `project498.webapi` has no host port mapping:**
Removing the host port binding means the backend API is only accessible within the Docker network. External HTTP clients (browsers, curl, other services) cannot reach it directly. The MVC project is the sole entry point for content-related API calls. This enforces the API key boundary at the network layer.

### Local development (without Docker)

Both projects can be run simultaneously via `dotnet run` or IDE multi-project launch:
- `Project498.Mvc` on `http://localhost:5031`
- `Project498.WebApi` on `http://localhost:5100`
- `BackendApi:BaseUrl` in `Project498.Mvc/appsettings.Development.json` set to `http://localhost:5100`

---

## Data Flow Summary

```
Browser
  │
  │  GET /api/comics  (no auth header, or user JWT)
  ▼
Project498.Mvc  :8080
  │  ProxyController catches /api/{**path}
  │  Strips incoming Authorization header
  │  Adds Authorization: Bearer <ApiKeyConstants.ServiceApiKey>
  │  Forwards to http://project498.webapi:8080/api/comics
  ▼
Project498.WebApi  :8080  (internal only)
  │  ApiKeyMiddleware validates Bearer token == ApiKeyConstants.ServiceApiKey
  │  ComicsController handles request, queries ComicsDbContext
  │  Returns JSON response
  ▼
Project498.Mvc
  │  Streams status code + body back to caller unchanged
  ▼
Browser
```

---

## Frontend JS Base URL

The current JS files define `const API_BASE_URL = 'http://localhost:8080'` and prefix all fetch calls with it. Since the MVC project serves both the static files and the proxy from the same origin, all API calls should be updated to use **relative paths** (e.g., `/api/comics` instead of `http://localhost:8080/api/comics`). This removes the hardcoded hostname entirely and works in both local dev and Docker without any per-environment config.

Each JS file that defines `API_BASE_URL` or uses an absolute URL for API calls must be updated as part of this refactor.

---

## Out of Scope

- Converting static HTML files to Razor views — HTML stays in `wwwroot` as-is
- Moving the API key to an external secret store — hardcoded constant is intentional for this stage
- Adding retry/circuit-breaker logic to the proxy
