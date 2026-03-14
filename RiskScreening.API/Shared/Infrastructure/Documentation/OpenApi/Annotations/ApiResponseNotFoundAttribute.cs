namespace RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Annotations;

/// <summary>
///     Documents a 404 Not Found response on a controller action or class.
///     Use this on endpoints that fetch resources by ID.
/// </summary>
/// <example>
/// <code>
/// [HttpGet("{id}")]
/// [ApiResponseNotFound]
/// public async Task&lt;IActionResult&gt; GetById(Guid id) { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ApiResponseNotFoundAttribute : Attribute;
