namespace Project498.Mvc.Models;

/// <summary>
/// Standard error response body returned by all API endpoints on validation
/// or domain failures. Consumers can use <see cref="Code"/> for programmatic
/// handling and <see cref="Message"/> for display.
/// </summary>
public record ErrorResponse(string Code, string Message);
