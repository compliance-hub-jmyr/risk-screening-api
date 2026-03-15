using RiskScreening.API.Modules.Scraping.Domain.Model;

namespace RiskScreening.API.Modules.Scraping.Infrastructure.Sources;

/// <summary>
///     Contract for a risk list data source (OFAC, World Bank, ICIJ, etc.).
///     Each implementation is registered in DI and consumed by
///     <see cref="Services.ScrapingOrchestrationService"/>.
/// </summary>
public interface IScrapingSource
{
    /// <summary>Source identifier (e.g. <c>"OFAC"</c>, <c>"WORLD_BANK"</c>, <c>"ICIJ"</c>).</summary>
    string SourceName { get; }

    /// <summary>
    ///     Searches the source for the given term.
    ///     Implementations must be fault-tolerant and return
    ///     <see cref="SearchResult.Empty"/> on any failure.
    /// </summary>
    Task<SearchResult> SearchAsync(string term, CancellationToken ct = default);
}
