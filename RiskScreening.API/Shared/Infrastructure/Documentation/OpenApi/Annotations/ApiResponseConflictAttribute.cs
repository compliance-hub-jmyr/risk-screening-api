namespace RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Annotations;

/// <summary>
///     Documents a 409 Conflict response on a controller action or class.
///     Use this on endpoints where a resource may already exist (e.g. duplicate creation).
/// </summary>
/// <example>
/// <code>
/// [HttpPost]
/// [ApiResponseConflict]
/// public async Task&lt;IActionResult&gt; Create([FromBody] CreateRiskScreeningCommand command) { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ApiResponseConflictAttribute : Attribute;