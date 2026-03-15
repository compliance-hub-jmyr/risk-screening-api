using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Resources.Responses;
using RiskScreening.API.Shared.Interfaces.REST.Resources;

namespace RiskScreening.API.Modules.Suppliers.Interfaces.REST.Mappers.Response;

/// <summary>
/// Maps supplier domain objects to <see cref="SupplierResponse"/> DTOs.
/// </summary>
public static class SupplierResponseMapper
{
    /// <summary>Maps a <see cref="Supplier"/> to a <see cref="SupplierResponse"/>.</summary>
    public static SupplierResponse ToResponse(Supplier supplier)
    {
        return new SupplierResponse(
            supplier.Id,
            supplier.LegalName.Value,
            supplier.CommercialName.Value,
            supplier.TaxId.Value,
            supplier.ContactPhone?.Value,
            supplier.ContactEmail?.Value,
            supplier.Website?.Value,
            supplier.Address?.Value,
            supplier.Country.Value,
            supplier.AnnualBillingUsd?.Value,
            supplier.RiskLevel.ToString(),
            supplier.Status.ToString(),
            supplier.IsDeleted,
            supplier.Notes,
            supplier.CreatedAt,
            supplier.UpdatedAt);
    }

    /// <summary>Maps a paginated domain result to a paginated response DTO.</summary>
    public static PageResponse<SupplierResponse> ToPageResponse(PageResponse<Supplier> source)
    {
        var meta = source.Page;
        return new PageResponse<SupplierResponse>(
            source.Content.Select(ToResponse).ToList(),
            new PageResponse<SupplierResponse>.PageMetadata(
                meta.Number, meta.Size, meta.TotalElements, meta.TotalPages,
                meta.First, meta.Last, meta.HasNext, meta.HasPrevious));
    }
}