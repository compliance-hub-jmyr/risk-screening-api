using RiskScreening.API.Modules.Scraping.Application.Ports;
using RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Scraping.Infrastructure.Sources;

/// <summary>
///     <see cref="IScrapingSource"/> adapter for the
///     <a href="https://sanctionssearch.ofac.treas.gov/">OFAC Sanctions List Search</a>.
///     <para>
///         The OFAC website is an ASP.NET WebForms application, so the scraping
///         follows a two-step flow:
///     </para>
///     <list type="number">
///         <item><b>GET</b> — fetches the initial page to extract ViewState and
///         other hidden form fields required for a valid POST.</item>
///         <item><b>POST</b> — submits the search form with the term and parses
///         the HTML results table into <see cref="RiskEntry"/> records.</item>
///     </list>
///     <para>
///         HTML extraction is delegated to <see cref="OfacHtmlParser"/>.
///         Returns <see cref="SearchResult.Empty"/> on any failure (fault-tolerant).
///     </para>
/// </summary>
public sealed class OfacScrapingSource(
    IHttpClientFactory httpClientFactory,
    ILogger<OfacScrapingSource> logger) : IScrapingSource
{
    /// <inheritdoc />
    public string SourceName => "OFAC";

    /// <inheritdoc />
    public async Task<SearchResult> SearchAsync(string term, CancellationToken ct = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Ofac");

            // Step 1: GET initial page → extract ASP.NET form fields
            var initialResponse = await client.GetAsync("", ct);
            initialResponse.EnsureSuccessStatusCode();

            var initialHtml = await initialResponse.Content.ReadAsStringAsync(ct);
            var formData = OfacHtmlParser.ExtractFormData(initialHtml, term);

            // Step 2: POST search form → parse HTML results table
            var content = new FormUrlEncodedContent(formData);
            var searchResponse = await client.PostAsync("", content, ct);
            searchResponse.EnsureSuccessStatusCode();

            var resultHtml = await searchResponse.Content.ReadAsStringAsync(ct);
            var entries = OfacHtmlParser.ParseResults(resultHtml, logger);

            logger.LogDebug(
                "OFAC search completed — Term={Term}, Hits={Hits}",
                term, entries.Count);

            return new SearchResult(entries.Count, entries);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "OFAC search failed — Term={Term}, returning empty result",
                term);

            return SearchResult.Empty;
        }
    }
}