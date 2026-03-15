using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiskScreening.API.Modules.Scraping.Domain.Model.Queries;
using RiskScreening.API.Modules.Scraping.Interfaces.REST.Documentation;
using RiskScreening.API.Modules.Scraping.Interfaces.REST.Mappers.Response;
using RiskScreening.API.Shared.Infrastructure.Configuration;

namespace RiskScreening.API.Modules.Scraping.Interfaces.REST.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiVersion(ApiVersioning.V1)]
[Produces("application/json")]
[Authorize]
public class ListsController(IMediator mediator) : ControllerBase, IListsController
{
    /// <inheritdoc />
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] List<string>? sources,
        CancellationToken ct)
    {
        var query = new SearchRiskListsQuery(q, sources);
        var result = await mediator.Send(query, ct);
        return Ok(ScrapingResponseMapper.ToResponse(result));
    }
}