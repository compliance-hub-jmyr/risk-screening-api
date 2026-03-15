using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiskScreening.API.Modules.Scraping.Infrastructure.Services;
using RiskScreening.API.Modules.Scraping.Interfaces.REST.Documentation;
using RiskScreening.API.Modules.Scraping.Interfaces.REST.Mappers.Response;
using RiskScreening.API.Shared.Infrastructure.Configuration;

namespace RiskScreening.API.Modules.Scraping.Interfaces.REST.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiVersion(ApiVersioning.V1)]
[Produces("application/json")]
[Authorize]
public class ListsController(ScrapingOrchestrationService orchestrationService)
    : ControllerBase, IListsController
{
    private static readonly HashSet<string> ValidSources = new(StringComparer.OrdinalIgnoreCase)
    {
        "ofac", "worldbank", "icij"
    };

    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string? sources,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Query parameter 'q' is required." });

        List<string>? sourceList = null;

        if (!string.IsNullOrWhiteSpace(sources))
        {
            sourceList = sources.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => ValidSources.Contains(s))
                .ToList();

            if (sourceList.Count == 0)
                return BadRequest(new { message = "No valid sources specified. Valid values: ofac, worldbank, icij." });
        }

        var result = await orchestrationService.SearchAllAsync(q.Trim(), sourceList, ct);
        return Ok(ScrapingResponseMapper.ToResponse(result));
    }
}
