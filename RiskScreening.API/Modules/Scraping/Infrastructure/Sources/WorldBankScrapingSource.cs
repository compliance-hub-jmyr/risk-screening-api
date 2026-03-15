using RiskScreening.API.Modules.Scraping.Application.Ports;
using RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Scraping.Infrastructure.Sources;

/// <summary>
///     <see cref="IScrapingSource"/> adapter for the
///     <a href="https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms">
///     World Bank Debarred Firms</a> list.
///     <para>
///         The World Bank page is a Kendo UI grid that loads data dynamically via AJAX.
///         This adapter follows a two-step web scraping flow (similar to OFAC):
///     </para>
///     <list type="number">
///         <item><b>GET</b> — fetches the HTML page and extracts the API URL and API key
///         from embedded <c>&lt;script&gt;</c> tags using <see cref="WorldBankHtmlParser"/>.</item>
///         <item><b>GET</b> — fetches the JSON API (same request the browser makes) and
///         filters firms client-side using <see cref="WorldBankHtmlParser"/>.</item>
///     </list>
///     <para>Returns <see cref="SearchResult.Empty"/> on any failure (fault-tolerant).</para>
/// </summary>
public sealed class WorldBankScrapingSource(
    IHttpClientFactory httpClientFactory,
    ILogger<WorldBankScrapingSource> logger) : IScrapingSource
{
    private const string PagePath =
        "en/projects-operations/procurement/debarred-firms";

    /// <inheritdoc />
    public string SourceName => "WORLD_BANK";

    /// <inheritdoc />
    public async Task<SearchResult> SearchAsync(string term, CancellationToken ct = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("WorldBank");

            // Step 1: GET the HTML page → extract API config from JavaScript
            var pageResponse = await client.GetAsync(PagePath, ct);
            pageResponse.EnsureSuccessStatusCode();

            var html = await pageResponse.Content.ReadAsStringAsync(ct);
            var (apiUrl, apiKey) = WorldBankHtmlParser.ExtractApiConfig(html);

            if (apiUrl is null || apiKey is null)
            {
                logger.LogWarning(
                    "World Bank: could not extract API config from page HTML");
                return SearchResult.Empty;
            }

            // Step 2: GET the JSON API (the same request the browser makes via AJAX)
            var apiRequest = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            apiRequest.Headers.Add("apikey", apiKey);

            var apiResponse = await client.SendAsync(apiRequest, ct);
            apiResponse.EnsureSuccessStatusCode();

            var json = await apiResponse.Content.ReadAsStringAsync(ct);
            var entries = WorldBankHtmlParser.ParseResults(json, term, logger);

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