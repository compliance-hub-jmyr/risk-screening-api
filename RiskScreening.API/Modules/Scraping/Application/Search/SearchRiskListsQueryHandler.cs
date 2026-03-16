using MediatR;
using Microsoft.Extensions.Caching.Memory;
using RiskScreening.API.Modules.Scraping.Application.Ports;
using RiskScreening.API.Modules.Scraping.Domain.Model.Queries;
using RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Scraping.Application.Search;

/// <summary>
///     Handles <see cref="SearchRiskListsQuery"/> by orchestrating scraping source
///     calls with <see cref="IMemoryCache"/> caching and parallel execution.
///     <para>
///         Each source result is cached independently by <c>scraping:{source}:{term}</c>
///         for 10 minutes. When <see cref="SearchRiskListsQuery.SourceNames"/> is
///         <c>null</c> or empty, all registered sources are queried.
///     </para>
/// </summary>
/// <remarks>
///     This handler follows the CQRS query pattern used across all modules.
///     It consumes <see cref="IScrapingSource"/> ports and delegates caching
///     to <see cref="IMemoryCache"/>.
/// </remarks>
public class SearchRiskListsQueryHandler(
    IEnumerable<IScrapingSource> sources,
    IMemoryCache cache,
    ILogger<SearchRiskListsQueryHandler> logger)
    : IRequestHandler<SearchRiskListsQuery, SearchResult>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    /// <summary>
    ///     Processes the search query by selecting sources, executing them in
    ///     parallel via <c>Task.WhenAll</c>, and merging results.
    /// </summary>
    /// <param name="query">The search query with term and optional source filter.</param>
    /// <param name="ct">Token to cancel the asynchronous operation.</param>
    /// <returns>
    ///     A merged <see cref="SearchResult"/> from all queried sources.
    /// </returns>
    public async Task<SearchResult> Handle(SearchRiskListsQuery query, CancellationToken ct)
    {
        var term = query.Term.Trim();

        var selected = query.SourceNames is { Count: > 0 }
            ? sources.Where(s => query.SourceNames.Any(n =>
                n.Equals(s.SourceName, StringComparison.OrdinalIgnoreCase)))
            : sources;

        var tasks = selected.Select(s => SearchSourceAsync(s, term, ct));
        var results = await Task.WhenAll(tasks);

        return SearchResult.Merge(results);
    }

    /// <summary>
    ///     Searches a single source with caching. On cache miss, fetches from
    ///     the source and stores the result for <see cref="CacheTtl"/>.
    /// </summary>
    private async Task<SearchResult> SearchSourceAsync(
        IScrapingSource source, string term, CancellationToken ct)
    {
        var cacheKey = $"scraping:{source.SourceName.ToLowerInvariant()}:{term.ToLowerInvariant()}";

        if (cache.TryGetValue(cacheKey, out SearchResult? cached) && cached is not null)
        {
            logger.LogDebug(
                "Cache hit — Source={Source}, Term={Term}, Hits={Hits}",
                source.SourceName, term, cached.Hits);
            return cached;
        }

        var result = await source.SearchAsync(term, ct);
        cache.Set(cacheKey, result, CacheTtl);

        logger.LogInformation(
            "Scraping completed — Source={Source}, Term={Term}, Hits={Hits}",
            source.SourceName, term, result.Hits);

        return result;
    }
}