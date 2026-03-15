using System.Linq.Expressions;
using System.Reflection;

namespace RiskScreening.API.Shared.Infrastructure.Persistence.Query.Sort;

/// <summary>
///     Abstract base class for entity-specific sort configuration.
///     Defines the allowed sort fields and default sorting behavior for one entity type.
///     Prevents SQL injection by only allowing whitelisted fields.
/// </summary>
/// <typeparam name="T">The entity type being sorted.</typeparam>
/// <remarks>
///     Sort expressions use <see cref="LambdaExpression"/> instead of
///     <c>Expression&lt;Func&lt;T, object&gt;&gt;</c> to avoid boxing value types
///     (DateTime, enums, decimals). Boxing produces <c>Convert()</c> nodes that
///     EF Core cannot translate to SQL. Using untyped <see cref="LambdaExpression"/>
///     preserves the original return type and allows EF Core to generate correct
///     <c>ORDER BY</c> clauses for columns mapped with value converters.
/// </remarks>
/// <example>
/// <code>
/// public class RiskScreeningSortConfiguration : SortConfiguration&lt;RiskScreening&gt;
/// {
///     protected override IReadOnlyDictionary&lt;string, LambdaExpression&gt; AllowedSortFields =&gt;
///         new Dictionary&lt;string, LambdaExpression&gt;
///         {
///             ["createdAt"]   = (Expression&lt;Func&lt;RiskScreening, DateTime&gt;&gt;)(x =&gt; x.CreatedAt),
///             ["updatedAt"]   = (Expression&lt;Func&lt;RiskScreening, DateTime&gt;&gt;)(x =&gt; x.UpdatedAt),
///             ["entityName"]  = (Expression&lt;Func&lt;RiskScreening, string&gt;&gt;)(x =&gt; x.EntityName),
///             ["status"]      = (Expression&lt;Func&lt;RiskScreening, Status&gt;&gt;)(x =&gt; x.Status)
///         };
///
///     protected override string DefaultSortField =&gt; "createdAt";
/// }
/// </code>
/// </example>
public abstract class SortConfiguration<T>
{
    private static readonly MethodInfo OrderByMethod =
        typeof(Queryable).GetMethods()
            .First(m => m.Name == nameof(Queryable.OrderBy) && m.GetParameters().Length == 2);

    private static readonly MethodInfo OrderByDescendingMethod =
        typeof(Queryable).GetMethods()
            .First(m => m.Name == nameof(Queryable.OrderByDescending) && m.GetParameters().Length == 2);

    private static readonly MethodInfo ThenByMethod =
        typeof(Queryable).GetMethods()
            .First(m => m.Name == nameof(Queryable.ThenBy) && m.GetParameters().Length == 2);

    /// <summary>
    ///     Map of allowed sort field names (from the request) to their EF Core expressions.
    ///     Keys are case-sensitive and must match what the client sends.
    ///     Each value must be a strongly-typed <c>Expression&lt;Func&lt;T, TKey&gt;&gt;</c>
    ///     cast to <see cref="LambdaExpression"/> to preserve the return type.
    /// </summary>
    protected abstract IReadOnlyDictionary<string, LambdaExpression> AllowedSortFields { get; }

    /// <summary>Default field to sort by when none is provided or the requested field is invalid.</summary>
    protected abstract string DefaultSortField { get; }

    /// <summary>Default sort direction. Override to change from DESC to ASC.</summary>
    protected virtual bool DefaultDescending => true;

    /// <summary>
    ///     Optional tiebreaker expression appended as <c>ThenBy</c> (always ascending)
    ///     to guarantee deterministic pagination when the primary sort field has duplicates.
    ///     Override in derived classes to specify the entity's unique key (typically the primary key).
    ///     When <c>null</c>, no tiebreaker is applied.
    /// </summary>
    protected virtual LambdaExpression? TiebreakerField => null;

    /// <summary>
    ///     Applies validated sorting to the query.
    ///     Falls back to defaults for invalid or missing values.
    ///     Appends a tiebreaker (<see cref="TiebreakerField"/>) to ensure deterministic
    ///     ordering for offset-based pagination.
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

        // Build Queryable.OrderBy<T, TKey>(query, expression) via reflection
        // so the generic TKey matches the expression's actual return type.
        var keyType = expression.ReturnType;
        var method = (descending ? OrderByDescendingMethod : OrderByMethod)
            .MakeGenericMethod(typeof(T), keyType);

        var ordered = (IOrderedQueryable<T>)method.Invoke(null, [query, expression])!;

        // Append tiebreaker for deterministic pagination (always ascending).
        if (TiebreakerField is not null)
        {
            var thenBy = ThenByMethod.MakeGenericMethod(typeof(T), TiebreakerField.ReturnType);
            ordered = (IOrderedQueryable<T>)thenBy.Invoke(null, [ordered, TiebreakerField])!;
        }

        return ordered;
    }

    /// <summary>Applies the default sort configuration to the query.</summary>
    public IOrderedQueryable<T> DefaultSort(IQueryable<T> query)
    {
        return ApplySort(query, null, null);
    }
}