using Microsoft.AspNetCore.Mvc;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Resources.Requests;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Resources.Responses;
using RiskScreening.API.Shared.Interfaces.REST.Resources;
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

    /// <summary>Get all suppliers with optional filters, sorting, and pagination.</summary>
    [SwaggerOperation(Summary = "List suppliers", Tags = ["Suppliers"])]
    [SwaggerResponse(StatusCodes.Status200OK, "Paginated list of suppliers.",
        typeof(PageResponse<SupplierResponse>))]
    Task<IActionResult> GetAll(
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
        CancellationToken ct);

    /// <summary>Get a supplier by ID.</summary>
    [SwaggerOperation(Summary = "Get supplier by ID", Tags = ["Suppliers"])]
    [SwaggerResponse(StatusCodes.Status200OK, "Supplier found.", typeof(SupplierResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Supplier not found.")]
    Task<IActionResult> GetById(string id, CancellationToken ct);

    /// <summary>Soft-delete a supplier.</summary>
    [SwaggerOperation(Summary = "Delete supplier", Tags = ["Suppliers"])]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Supplier deleted.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Supplier not found.")]
    Task<IActionResult> Delete(string id, CancellationToken ct);
}
