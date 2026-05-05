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
