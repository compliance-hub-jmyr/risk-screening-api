using Microsoft.EntityFrameworkCore;
using RiskScreening.API.Shared.Interfaces.REST.Resources;

namespace RiskScreening.API.Shared.Infrastructure.Persistence.Query.Pagination;

/// <summary>
///     Extension methods for applying pagination to <see cref="IQueryable{T}"/>
///     and projecting the result into a <see cref="PageResponse{T}"/>.
/// </summary>
public static class PageableExtensions
{
    /// <summary>
    ///     Executes the query with pagination and returns a <see cref="PageResponse{T}"/>.
    ///     Issues two SQL queries: one for COUNT and one for the page data.
    /// </summary>
    /// <typeparam name="T">The entity or projection type.</typeparam>
    /// <param name="query">The base query (with filters and sorting already applied).</param>
    /// <param name="request">Pagination parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated response with content and metadata.</returns>
    /// <example>
    /// <code>
    /// var query = _context.RiskScreenings
    ///     .Where(x => x.Status == status)
    ///     .OrderByDescending(x => x.CreatedAt);
    ///
    /// return await query.ToPageResponseAsync(pageRequest, ct);
    /// </code>
    /// </example>
    public static async Task<PageResponse<T>> ToPageResponseAsync<T>(
        this IQueryable<T> query,
        PageRequest request,
        CancellationToken ct = default)
    {
        var totalElements = await query.LongCountAsync(ct);
        var totalPages = (int)Math.Ceiling((double)totalElements / request.Size);

        var content = await query
            .Skip(request.Page * request.Size)
            .Take(request.Size)
            .ToListAsync(ct);

        return new PageResponse<T>(
            content,
            new PageResponse<T>.PageMetadata(
                request.Page,
                request.Size,
                totalElements,
                totalPages,
                request.Page == 0,
                request.Page >= totalPages - 1,
                request.Page < totalPages - 1,
                request.Page > 0
            )
        );
    }
}