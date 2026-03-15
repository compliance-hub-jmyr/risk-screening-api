using RiskScreening.API.Modules.Suppliers.Domain.Model.Commands;
using RiskScreening.API.Modules.Suppliers.Interfaces.REST.Resources.Requests;

namespace RiskScreening.API.Modules.Suppliers.Interfaces.REST.Mappers.Request;

/// <summary>Maps a <see cref="CreateSupplierRequest"/> DTO to a <see cref="CreateSupplierCommand"/>.</summary>
public static class CreateSupplierRequestMapper
{
    public static CreateSupplierCommand ToCommand(CreateSupplierRequest request)
    {
        return new CreateSupplierCommand(
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