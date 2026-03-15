using Microsoft.AspNetCore.Mvc;
using RiskScreening.API.Modules.Scraping.Interfaces.REST.Resources.Responses;
using Swashbuckle.AspNetCore.Annotations;

namespace RiskScreening.API.Modules.Scraping.Interfaces.REST.Documentation;

/// <summary>
///     Swagger documentation for the Lists (scraping) controller.
/// </summary>
public interface IListsController
{
    /// <summary>
    ///     Searches selected risk list sources in parallel and returns merged results.
    ///     <para>
    ///         The <c>sources</c> parameter accepts a comma-separated list of source names
    ///         (ofac, worldbank, icij). Minimum 1, maximum 3.
    ///         When omitted, all registered sources are queried.
    ///         Each source result is cached independently for 10 minutes.
    ///     </para>
    /// </summary>
    [SwaggerOperation(Summary = "Search risk lists by selected sources", Tags = ["Lists"])]
    [SwaggerResponse(StatusCodes.Status200OK, "Search results.", typeof(ScrapingResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Missing search term or invalid sources.")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Authentication required.")]
    [SwaggerResponse(StatusCodes.Status429TooManyRequests, "Rate limit exceeded.")]
    Task<IActionResult> Search([FromQuery] string q, [FromQuery] string? sources, CancellationToken ct);
}