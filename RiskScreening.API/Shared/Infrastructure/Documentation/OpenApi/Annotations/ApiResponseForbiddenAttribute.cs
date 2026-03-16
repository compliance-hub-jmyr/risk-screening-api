namespace RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Annotations;

/// <summary>
///     Documents a 403 Forbidden response on a controller action or class.
///     Use this on endpoints that require specific roles or permissions.
/// </summary>
/// <example>
/// <code>
/// [HttpDelete("{id}")]
/// [ApiResponseForbidden]
/// public async Task&lt;IActionResult&gt; Delete(Guid id) { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ApiResponseForbiddenAttribute : Attribute;