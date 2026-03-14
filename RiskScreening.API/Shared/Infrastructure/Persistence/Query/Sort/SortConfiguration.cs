using System.Linq.Expressions;

namespace RiskScreening.API.Shared.Infrastructure.Persistence.Query.Sort;

/// <summary>
///     Abstract base class for entity-specific sort configuration.
///     Defines the allowed sort fields and default sorting behavior for one entity type.
///     Prevents SQL injection by only allowing whitelisted fields.
/// </summary>
/// <typeparam name="T">The entity type being sorted.</typeparam>
/// <example>
/// <code>
/// public class RiskScreeningSortConfiguration : SortConfiguration&lt;RiskScreening&gt;
/// {
///     protected override IReadOnlyDictionary&lt;string, Expression&lt;Func&lt;RiskScreening, object&gt;&gt;&gt; AllowedSortFields =&gt;
///         new Dictionary&lt;string, Expression&lt;Func&lt;RiskScreening, object&gt;&gt;&gt;
///         {
///             ["createdAt"]   = x =&gt; x.CreatedAt,
///             ["updatedAt"]   = x =&gt; x.UpdatedAt,
///             ["entityName"]  = x =&gt; x.EntityName,
///             ["status"]      = x =&gt; x.Status
///         };
///
///     protected override string DefaultSortField =&gt; "createdAt";
/// }
/// </code>
/// </example>
public abstract class SortConfiguration<T>
{
    /// <summary>
    ///     Map of allowed sort field names (from the request) to their EF Core expressions.
    ///     Keys are case-sensitive and must match what the client sends.
    /// </summary>
    protected abstract IReadOnlyDictionary<string, Expression<Func<T, object>>> AllowedSortFields { get; }

    /// <summary>Default field to sort by when none is provided or the requested field is invalid.</summary>
    protected abstract string DefaultSortField { get; }

    /// <summary>Default sort direction. Override to change from DESC to ASC.</summary>
    protected virtual bool DefaultDescending => true;

    /// <summary>
    ///     Applies validated sorting to the query.
    ///     Falls back to defaults for invalid or missing values.
    /// </summary>
    /// <param name="query">The base query.</param>
    /// <param name="sortBy">Field name from the request (can be null).</param>
    /// <param name="sortDirection">"ASC" or "DESC" from the request (can be null).</param>
    /// <returns>Ordered query.</returns>
    public IOrderedQueryable<T> ApplySort(IQueryable<T> query, string? sortBy, string? sortDirection)
    {
        var field = sortBy is not null && AllowedSortFields.ContainsKey(sortBy)
            ? sortBy
            : DefaultSortField;

        var descending = sortDirection?.ToUpperInvariant() switch
        {
            "ASC" => false,
            "DESC" => true,
            _ => DefaultDescending
        };

        var expression = AllowedSortFields[field];

        return descending
            ? query.OrderByDescending(expression)
            : query.OrderBy(expression);
    }

    /// <summary>Applies the default sort configuration to the query.</summary>
    public IOrderedQueryable<T> DefaultSort(IQueryable<T> query) =>
        ApplySort(query, null, null);
}
