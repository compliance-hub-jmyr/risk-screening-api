using RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Scraping.Application.Ports;

/// <summary>
///     Port for a risk list data source (OFAC, World Bank, ICIJ, etc.).
///     <para>
///         Each implementation is registered in DI and consumed by the
///         <c>SearchRiskListsQueryHandler</c> in the Application layer.
///     </para>
/// </summary>
public interface IScrapingSource
{
    /// <summary>Source identifier (e.g. <c>"OFAC"</c>, <c>"WORLD_BANK"</c>, <c>"ICIJ"</c>).</summary>
    string SourceName { get; }

    /// <summary>
    ///     Searches the source for the given term.
    ///     <para>
    ///         Implementations must be fault-tolerant and return
    ///         <see cref="SearchResult.Empty"/> on any failure.
    ///     </para>
    /// </summary>
    Task<SearchResult> SearchAsync(string term, CancellationToken ct = default);
}