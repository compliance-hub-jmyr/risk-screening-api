using System.Linq.Expressions;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Modules.Suppliers.Domain.Model.ValueObjects;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;
using RiskScreening.API.Shared.Infrastructure.Persistence.Query.Sort;

namespace RiskScreening.API.Modules.Suppliers.Infrastructure.Persistence.Query;

/// <summary>
///     Sort configuration for suppliers.
///     Whitelists allowed sort fields and defines the default sort (updatedAt DESC).
/// </summary>
/// <remarks>
///     Value object properties (e.g. <see cref="LegalName"/>) are referenced directly
///     (not via <c>.Value</c>) so EF Core applies its value converter for SQL translation.
///     Each value object implements <see cref="IComparable{T}"/> to support in-memory
///     sorting in unit tests with MockQueryable.
/// </remarks>
public class SupplierSortConfiguration : SortConfiguration<Supplier>
{
    protected override IReadOnlyDictionary<string, LambdaExpression> AllowedSortFields =>
        new Dictionary<string, LambdaExpression>
        {
            ["legalName"] = (Expression<Func<Supplier, LegalName>>)(x => x.LegalName),
            ["commercialName"] = (Expression<Func<Supplier, CommercialName>>)(x => x.CommercialName),
            ["taxId"] = (Expression<Func<Supplier, TaxId>>)(x => x.TaxId),
            ["country"] = (Expression<Func<Supplier, CountryCode>>)(x => x.Country),
            ["status"] = (Expression<Func<Supplier, SupplierStatus>>)(x => x.Status),
            ["riskLevel"] = (Expression<Func<Supplier, RiskLevel>>)(x => x.RiskLevel),
            ["createdAt"] = (Expression<Func<Supplier, DateTime>>)(x => x.CreatedAt),
            ["updatedAt"] = (Expression<Func<Supplier, DateTime>>)(x => x.UpdatedAt)
        };

    protected override string DefaultSortField => "updatedAt";

    protected override LambdaExpression TiebreakerField =>
        (Expression<Func<Supplier, string>>)(x => x.Id);
}