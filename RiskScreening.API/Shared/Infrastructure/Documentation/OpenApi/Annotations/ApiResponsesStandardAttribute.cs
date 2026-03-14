namespace RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Annotations;

/// <summary>
///     Composite attribute that documents all standard error responses in one annotation.
///     Applies: 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found.
/// </summary>
/// <remarks>
///     Detected by <c>StandardResponsesOperationFilter</c>, which adds all four
///     standard error responses to the Swagger operation automatically.
/// </remarks>
/// <example>
/// <code>
/// [HttpGet("{id}")]
/// [ApiResponsesStandard]
/// [ProducesResponseType(typeof(RiskScreeningResource), StatusCodes.Status200OK)]
/// public async Task&lt;IActionResult&gt; GetById(Guid id) { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ApiResponsesStandardAttribute : Attribute;