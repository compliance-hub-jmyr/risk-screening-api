using Microsoft.Extensions.Caching.Memory;
using RiskScreening.API.Modules.Scraping.Domain.Model;
using RiskScreening.API.Modules.Scraping.Infrastructure.Sources;

namespace RiskScreening.API.Modules.Scraping.Infrastructure.Services;

/// <summary>
///     Orchestrates scraping source calls with <see cref="IMemoryCache"/> caching.
///     Each source result is cached independently by <c>scraping:{source}:{term}</c> for 10 minutes.
/// </summary>
public sealed class ScrapingOrchestrationService(
    IEnumerable<IScrapingSource> sources,
    IMemoryCache cache,
    ILogger<ScrapingOrchestrationService> logger)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    /// <summary>
    ///     Searches a single source by name, with caching.
    /// </summary>
    public async Task<SearchResult> SearchSourceAsync(
        string sourceName, string term, CancellationToken ct = default)
    {
        var cacheKey = $"scraping:{sourceName.ToLowerInvariant()}:{term.ToLowerInvariant()}";

        if (cache.TryGetValue(cacheKey, out SearchResult? cached) && cached is not null)
        {
            logger.LogDebug(
                "Cache hit — Source={Source}, Term={Term}, Hits={Hits}",
                sourceName, term, cached.Hits);
            return cached;
        }

        var source = sources.FirstOrDefault(s =>
            s.SourceName.Equals(sourceName, StringComparison.OrdinalIgnoreCase));

        if (source is null)
        {
            logger.LogWarning("Unknown scraping source requested — Source={Source}", sourceName);
            return SearchResult.Empty;
        }

        var result = await source.SearchAsync(term, ct);
        cache.Set(cacheKey, result, CacheTtl);

        logger.LogInformation(
            "Scraping completed — Source={Source}, Term={Term}, Hits={Hits}",
            sourceName, term, result.Hits);

        return result;
    }

    /// <summary>
    ///     Searches selected sources in parallel, merges results.
    ///     When <paramref name="sourceNames"/> is null or empty, all registered sources are queried.
    /// </summary>
    public async Task<SearchResult> SearchAllAsync(
        string term, IReadOnlyList<string>? sourceNames = null, CancellationToken ct = default)
    {
        var selected = sourceNames is { Count: > 0 }
            ? sources.Where(s => sourceNames.Any(n =>
                n.Equals(s.SourceName, StringComparison.OrdinalIgnoreCase)))
            : sources;

        var tasks = selected.Select(s => SearchSourceAsync(s.SourceName, term, ct));
        var results = await Task.WhenAll(tasks);
        return SearchResult.Merge(results);
    }
}
