using Microsoft.EntityFrameworkCore;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Shared.Infrastructure.Persistence.Query.Specification;

namespace RiskScreening.API.Modules.Suppliers.Infrastructure.Persistence.Query;

/// <summary>
///     Composes EF Core predicates for filtering suppliers.
///     Null/blank values are ignored — no filter is applied for that field.
/// </summary>
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
        return ApplyAndFilters(query,
            ToSpec(legalName, v =>
                x => EF.Functions.Like(x.LegalName.Value, $"%{v}%")),
            ToSpec(commercialName, v =>
                x => EF.Functions.Like(x.CommercialName.Value, $"%{v}%")),
            ToSpec(taxId, v =>
                x => x.TaxId.Value == v),
            ToSpec(country, v =>
                x => x.Country.Value == v.ToUpperInvariant()),
            ToSpec(status, v =>
                x => x.Status.ToString() == v),
            ToSpec(riskLevel, v =>
                x => x.RiskLevel.ToString() == v)
        );
    }
}