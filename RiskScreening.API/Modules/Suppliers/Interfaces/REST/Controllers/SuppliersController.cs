using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Documentation;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Mappers.Request;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Mappers.Response;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Resources.Requests;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Resources.Responses;
using RiskScreening.API.Shared.Infrastructure.Configuration;
using RiskScreening.API.Shared.Infrastructure.Documentation.OpenApi.Annotations;

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
        // TODO: Change nameof(Create) to nameof(GetById) when the GetById method is implemented.
        return CreatedAtAction(nameof(Create), new { id = supplier.Id }, response);
    }
}