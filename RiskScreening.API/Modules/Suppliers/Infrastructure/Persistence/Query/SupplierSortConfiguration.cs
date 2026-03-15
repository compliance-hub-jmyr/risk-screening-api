using System.Linq.Expressions;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Shared.Infrastructure.Persistence.Query.Sort;

namespace RiskScreening.API.Modules.Suppliers.Infrastructure.Persistence.Query;

/// <summary>
///     Sort configuration for suppliers.
///     Whitelists allowed sort fields and defines the default sort (updatedAt DESC).
/// </summary>
public class SupplierSortConfiguration : SortConfiguration<Supplier>
{
    protected override IReadOnlyDictionary<string, Expression<Func<Supplier, object>>> AllowedSortFields =>
        new Dictionary<string, Expression<Func<Supplier, object>>>
        {
            ["legalName"] = x => x.LegalName.Value,
            ["commercialName"] = x => x.CommercialName.Value,
            ["taxId"] = x => x.TaxId.Value,
            ["country"] = x => x.Country.Value,
            ["status"] = x => x.Status,
            ["riskLevel"] = x => x.RiskLevel,
            ["createdAt"] = x => x.CreatedAt,
            ["updatedAt"] = x => x.UpdatedAt
        };

    protected override string DefaultSortField => "updatedAt";
}
