using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Commands;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Queries;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Documentation;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Mappers.Request;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Mappers.Response;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Resources.Requests;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Resources.Responses;
using RiskScreening.API.Shared.Infrastructure.Configuration;
using RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Annotations;
using RiskScreening.API.Shared.Interfaces.REST.Resources;

namespace RiskScreening.API.Modules.Suppliers.Interfaces.REST.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiVersion(ApiVersioning.V1)]
[Produces("application/json")]
[Authorize]
public class SuppliersController(IMediator mediator)
    : ControllerBase, ISuppliersController
{
    /// <inheritdoc/>
    [HttpPost]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status201Created)]
    [ApiResponseBadRequest]
    [ApiResponseConflict]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request, CancellationToken ct)
    {
        var command = CreateSupplierRequestMapper.ToCommand(request);
        var supplier = await mediator.Send(command, ct);
        var response = SupplierResponseMapper.ToResponse(supplier);
        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, response);
    }

    /// <inheritdoc/>
    [HttpGet]
    [ProducesResponseType(typeof(PageResponse<SupplierResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? legalName,
        [FromQuery] string? commercialName,
        [FromQuery] string? taxId,
        [FromQuery] string? country,
        [FromQuery] string? status,
        [FromQuery] string? riskLevel,
        [FromQuery] int? page,
        [FromQuery] int? size,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        CancellationToken ct)
    {
        var query = new GetAllSuppliersQuery(legalName, commercialName, taxId, country, status, riskLevel, page, size,
            sortBy, sortDirection);
        var result = await mediator.Send(query, ct);
        return Ok(SupplierResponseMapper.ToPageResponse(result));
    }

    /// <inheritdoc/>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status200OK)]
    [ApiResponseNotFound]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var query = new GetSupplierByIdQuery(id);
        var supplier = await mediator.Send(query, ct);
        return Ok(SupplierResponseMapper.ToResponse(supplier));
    }

    /// <inheritdoc/>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status200OK)]
    [ApiResponseBadRequest]
    [ApiResponseNotFound]
    [ApiResponseConflict]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateSupplierRequest request, CancellationToken ct)
    {
        var command = UpdateSupplierRequestMapper.ToCommand(id, request);
        var supplier = await mediator.Send(command, ct);
        return Ok(SupplierResponseMapper.ToResponse(supplier));
    }

    /// <inheritdoc/>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ApiResponseNotFound]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await mediator.Send(new DeleteSupplierCommand(id), ct);
        return NoContent();
    }
}