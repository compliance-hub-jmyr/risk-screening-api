using MediatR;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;

namespace RiskScreening.API.Modules.Suppliers.Domain.Model.Commands;

public record UpdateSupplierCommand(
    string Id,
    string LegalName,
    string CommercialName,
    string TaxId,
    string Country,
    string? ContactPhone,
    string? ContactEmail,
    string? Website,
    string? Address,
    decimal? AnnualBillingUsd,
    string? Notes
) : IRequest<Supplier>;