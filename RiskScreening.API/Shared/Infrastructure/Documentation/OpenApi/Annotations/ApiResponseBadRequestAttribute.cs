namespace RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Annotations;

/// <summary>
///     Documents a 400 Bad Request response on a controller action or class.
///     Use this on endpoints that accept request bodies with validation.
/// </summary>
/// <example>
/// <code>
/// [HttpPost]
/// [ApiResponseBadRequest]
/// public async Task&lt;IActionResult&gt; Create([FromBody] CreateRiskScreeningCommand command) { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ApiResponseBadRequestAttribute : Attribute;
