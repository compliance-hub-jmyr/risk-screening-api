using Microsoft.AspNetCore.Mvc;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Resources.Requests;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Resources.Responses;
using Swashbuckle.AspNetCore.Annotations;

namespace RiskScreening.API.Modules.Suppliers.Interfaces.REST.Documentation;

/// <summary>
/// OpenAPI contract for supplier management endpoints.
/// <para>
/// Separates the documentation concern from the implementation.
/// Implementations live in <see cref="Controllers.SuppliersController"/>.
/// </para>
/// </summary>
public interface ISuppliersController
{
    /// <summary>Create a new supplier.</summary>
    [SwaggerOperation(Summary = "Create supplier", Tags = ["Suppliers"])]
    [SwaggerResponse(StatusCodes.Status201Created, "Supplier created.", typeof(SupplierResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation error.")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Tax ID already exists.")]
    Task<IActionResult> Create([FromBody] CreateSupplierRequest request, CancellationToken ct);
}