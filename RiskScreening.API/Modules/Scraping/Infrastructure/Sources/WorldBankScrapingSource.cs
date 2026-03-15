using RiskScreening.API.Modules.Scraping.Application.Ports;
using RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Scraping.Infrastructure.Sources;

/// <summary>
///     <see cref="IScrapingSource"/> adapter for the
///     <a href="https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms">
///     World Bank Debarred Firms</a> list.
///     <para>
///         Unlike OFAC (HTML scraping), the World Bank exposes a JSON API that returns
///         <b>all</b> sanctioned firms in a single GET request. Filtering is performed
///         client-side by <see cref="WorldBankJsonParser.ParseAndFilter"/>.
///     </para>
///     <list type="number">
///         <item><b>GET</b> — fetches the full debarred firms JSON from the World Bank API.</item>
///         <item><b>Filter</b> — matches firms by name (case-insensitive contains).</item>
///     </list>
///     <para>Returns <see cref="SearchResult.Empty"/> on any failure (fault-tolerant).</para>
/// </summary>
public sealed class WorldBankScrapingSource(
    IHttpClientFactory httpClientFactory,
    ILogger<WorldBankScrapingSource> logger) : IScrapingSource
{
    private const string Endpoint =
        "dvsvc/v1.0/json/APPLICATION/ADOBE_EXPRNCE_MGR/FIRM/SANCTIONED_FIRM";

    /// <inheritdoc />
    public string SourceName => "WORLD_BANK";

    /// <inheritdoc />
    public async Task<SearchResult> SearchAsync(string term, CancellationToken ct = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("WorldBank");

            var response = await client.GetAsync(Endpoint, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var entries = WorldBankJsonParser.ParseAndFilter(json, term, logger);

            logger.LogDebug(
                "World Bank search completed — Term={Term}, Hits={Hits}",
                term, entries.Count);

            return new SearchResult(entries.Count, entries);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "World Bank search failed — Term={Term}, returning empty result",
                term);

            return SearchResult.Empty;
        }
    }
}
