using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Resources.Responses;

namespace RiskScreening.API.Modules.Suppliers.Interfaces.REST.Mappers.Response;
/// <summary>
/// Maps supplier domain objects to <see cref="SupplierResponse"/> DTOs.
/// </summary>
public static class SupplierResponseMapper
{
    /// <summary> Maps a <see cref="Supplier"/> to a <see cref="SupplierResponse"/>.</summary>
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
}