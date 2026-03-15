using Microsoft.AspNetCore.Mvc;
using RiskScreening.API.Modules.Scraping.Interfaces.REST.Resources.Responses;
using Swashbuckle.AspNetCore.Annotations;

namespace RiskScreening.API.Modules.Scraping.Interfaces.REST.Documentation;

/// <summary>
/// OpenAPI contract for risk list search endpoints.
/// <para>
/// Separates the documentation concern from the implementation.
/// Implementation lives in <see cref="Controllers.ListsController"/>.
/// </para>
/// </summary>
public interface IListsController
{
    /// <summary> Search risk lists by term and optional sources.</summary>
    [SwaggerOperation(Summary = "Search risk lists by selected sources", Tags = ["Lists"])]
    [SwaggerResponse(StatusCodes.Status200OK, "Search results.", typeof(ScrapingResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Missing search term or invalid sources.")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Authentication required.")]
    [SwaggerResponse(StatusCodes.Status429TooManyRequests, "Rate limit exceeded.")]
    Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] List<string>? sources,
        CancellationToken ct);
}