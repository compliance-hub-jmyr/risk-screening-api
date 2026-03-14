namespace RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Annotations;

/// <summary>
///     Documents a 401 Unauthorized response on a controller action or class.
///     Use this on endpoints that require a valid JWT Bearer token.
/// </summary>
/// <example>
/// <code>
/// [HttpGet("{id}")]
/// [ApiResponseUnauthorized]
/// public async Task&lt;IActionResult&gt; GetById(Guid id) { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ApiResponseUnauthorizedAttribute : Attribute;
