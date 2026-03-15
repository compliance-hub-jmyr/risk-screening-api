using RiskScreening.API.Modules.Suppliers.Domain.Model.Commands;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Resources.Requests;

namespace RiskScreening.API.Modules.Suppliers.Interfaces.REST.Mappers.Request;

/// <summary>Maps a <see cref="UpdateSupplierRequest"/> DTO to a <see cref="UpdateSupplierCommand"/>.</summary>
public static class UpdateSupplierRequestMapper
{
    public static UpdateSupplierCommand ToCommand(string id, UpdateSupplierRequest request)
    {
        return new UpdateSupplierCommand(
            id,
            request.LegalName,
            request.CommercialName,
            request.TaxId,
            request.Country,
            request.ContactPhone,
            request.ContactEmail,
            request.Website,
            request.Address,
            request.AnnualBillingUsd,
            request.Notes);
    }
}