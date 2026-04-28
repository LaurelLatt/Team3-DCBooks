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
/// Registration: call <c>app.UseMiddleware&lt;ApiKeyMiddleware&gt;()</c> in <c>Program.cs</c> before
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
