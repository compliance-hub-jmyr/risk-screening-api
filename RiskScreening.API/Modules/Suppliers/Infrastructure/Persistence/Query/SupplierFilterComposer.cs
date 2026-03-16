using Microsoft.EntityFrameworkCore;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Modules.Suppliers.Domain.Model.ValueObjects;
using RiskScreening.API.Shared.Infrastructure.Persistence.Query.Specification;

namespace RiskScreening.API.Modules.Suppliers.Infrastructure.Persistence.Query;

/// <summary>
/// Composes EF Core predicates for supplier query filters.
/// </summary>
/// <remarks>
/// Empty inputs are ignored. Enum filters (<see cref="SupplierStatus"/>, <see cref="RiskLevel"/>)
/// are parsed before predicate composition to keep expressions provider-translatable.
/// </remarks>
public class SupplierFilterComposer : SpecificationComposer<Supplier>
{
    public IQueryable<Supplier> Apply(
        IQueryable<Supplier> query,
        string? legalName,
        string? commercialName,
        string? taxId,
        string? country,
        string? status,
        string? riskLevel)
    {
        // Normalize raw inputs up front so expression trees only capture constants.
        var legalNamePattern = legalName is not null ? $"%{legalName.Trim()}%" : null;
        var commercialPattern = commercialName is not null ? $"%{commercialName.Trim()}%" : null;
        var taxIdNorm = taxId?.Trim();
        var countryNorm = country?.Trim().ToUpperInvariant();

        // Parse enums once and compare typed values in predicates.
        var statusEnum = Enum.TryParse<SupplierStatus>(status?.Trim(), ignoreCase: true, out var s) ? s : (SupplierStatus?)null;
        var riskEnum = Enum.TryParse<RiskLevel>(riskLevel?.Trim(), ignoreCase: true, out var r) ? r : (RiskLevel?)null;

        // String casts keep in-memory tests consistent with mapped EF Core columns.
        return ApplyAndFilters(query,
            ToSpec(legalNamePattern, v => x => EF.Functions.Like((string)x.LegalName, v)),
            ToSpec(commercialPattern, v => x => EF.Functions.Like((string)x.CommercialName, v)),
            ToSpec(taxIdNorm, v => x => (string)x.TaxId == v),
            ToSpec(countryNorm, v => x => (string)x.Country == v),
            ToSpec(statusEnum, v => x => x.Status == v),
            ToSpec(riskEnum, v => x => x.RiskLevel == v)
        );
    }
}