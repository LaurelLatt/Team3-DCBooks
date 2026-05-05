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
