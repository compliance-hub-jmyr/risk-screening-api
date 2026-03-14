using System.Linq.Expressions;

namespace RiskScreening.API.Shared.Infrastructure.Persistence.Query.Specification;

/// <summary>
///     Abstract base class for composing EF Core query predicates into complex filters.
/// </summary>
/// <remarks>
///     In EF Core, <see cref="Expression{TDelegate}"/> predicates are compiled directly
///     Null and blank values are automatically ignored (no filter applied).
/// </remarks>
/// <typeparam name="T">The entity type being filtered.</typeparam>
/// <example>
/// <code>
/// public class RiskScreeningFilterComposer : SpecificationComposer&lt;RiskScreening&gt;
/// {
///     public IQueryable&lt;RiskScreening&gt; Apply(
///         IQueryable&lt;RiskScreening&gt; query,
///         string? entityName,
///         string? status,
///         DateTime? createdFrom)
///     {
///         return ApplyAndFilters(query,
///             ToSpec(entityName, v =&gt;
///                 x =&gt; EF.Functions.Like(x.EntityName, $"%{v}%")),
///             ToSpec(status, v =&gt;
///                 x =&gt; x.Status.ToString() == v),
///             ToSpec(createdFrom, v =&gt;
///                 x =&gt; x.CreatedAt &gt;= v)
///         );
///     }
/// }
/// </code>
/// </example>
public abstract class SpecificationComposer<T>
{
    /// <summary>
    ///     Converts a nullable value into an optional EF Core predicate.
    ///     Returns <c>null</c> (no filter) if the value is null or a blank string.
    /// </summary>
    /// <typeparam name="V">The filter value type.</typeparam>
    /// <param name="value">The filter value from the request (can be null).</param>
    /// <param name="mapper">Function that builds the predicate from the value.</param>
    /// <returns>The predicate, or <c>null</c> if the value is absent.</returns>
    protected static Expression<Func<T, bool>>? ToSpec<V>(
        V?                                    value,
        Func<V, Expression<Func<T, bool>>>    mapper)
    {
        if (value is null) return null;
        if (value is string str && string.IsNullOrWhiteSpace(str)) return null;
        return mapper(value);
    }

    /// <summary>
    ///     Applies multiple predicates to a query using AND logic.
    ///     Null predicates are skipped — they do not add a WHERE clause.
    ///     Returns the original query unchanged if all predicates are null.
    /// </summary>
    /// <param name="query">The base query.</param>
    /// <param name="specs">Predicates to combine (nulls are ignored).</param>
    /// <returns>Query with all non-null predicates applied as AND conditions.</returns>
    protected static IQueryable<T> ApplyAndFilters(
        IQueryable<T>                     query,
        params Expression<Func<T, bool>>?[] specs)
    {
        return specs
            .Where(s => s is not null)
            .Aggregate(query, (q, spec) => q.Where(spec!));
    }

    /// <summary>
    ///     Applies multiple predicates to a query using OR logic.
    ///     Null predicates are skipped.
    ///     Returns the original query unchanged if all predicates are null.
    /// </summary>
    protected static IQueryable<T> ApplyOrFilters(
        IQueryable<T>                     query,
        params Expression<Func<T, bool>>?[] specs)
    {
        var valid = specs.Where(s => s is not null).ToList();
        if (valid.Count == 0) return query;

        // Build combined OR expression: spec1.Or(spec2).Or(spec3)...
        var combined = valid[0]!;
        foreach (var spec in valid.Skip(1))
            combined = CombineOr(combined, spec!);

        return query.Where(combined);
    }

    // Combines two expressions with OR using expression tree rewriting
    private static Expression<Func<T, bool>> CombineOr(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var param= Expression.Parameter(typeof(T));
        var leftBody= Expression.Invoke(left, param);
        var rightBody= Expression.Invoke(right, param);
        return Expression.Lambda<Func<T, bool>>(
            Expression.OrElse(leftBody, rightBody), param);
    }
}
