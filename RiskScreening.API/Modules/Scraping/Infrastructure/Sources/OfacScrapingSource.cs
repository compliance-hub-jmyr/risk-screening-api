using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using RiskScreening.API.Modules.Scraping.Domain.Model;

namespace RiskScreening.API.Modules.Scraping.Infrastructure.Sources;

/// <summary>
///     Scrapes the OFAC Sanctions List Search website by performing a POST request
///     with ASP.NET form data and parsing the HTML results table.
///     Returns <see cref="SearchResult.Empty"/> on any failure (fault-tolerant).
/// </summary>
public sealed class OfacScrapingSource(
    IHttpClientFactory httpClientFactory,
    ILogger<OfacScrapingSource> logger) : IScrapingSource
{

    public string SourceName => "OFAC";

    public async Task<SearchResult> SearchAsync(string term, CancellationToken ct = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Ofac");

            // Step 1: GET the initial page to extract ViewState and other form fields
            var initialResponse = await client.GetAsync("", ct); // Empty string uses BaseAddress
            initialResponse.EnsureSuccessStatusCode();

            var htmlContent = await initialResponse.Content.ReadAsStringAsync(ct);
            var formData = ExtractFormData(htmlContent, term);

            // Step 2: POST the search form
            var content = new FormUrlEncodedContent(formData);
            var searchResponse = await client.PostAsync("", content, ct); // Empty string uses BaseAddress
            searchResponse.EnsureSuccessStatusCode();

            var resultHtml = await searchResponse.Content.ReadAsStringAsync(ct);
            var entries = ParseResults(resultHtml, logger);

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

    private static Dictionary<string, string> ExtractFormData(string html, string searchTerm)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var formData = new Dictionary<string, string>();

        // Extract ASP.NET ViewState and other hidden fields
        var hiddenFields = doc.DocumentNode.SelectNodes("//input[@type='hidden']");
        if (hiddenFields != null)
            foreach (var field in hiddenFields)
            {
                var name = field.GetAttributeValue("name", "");
                var value = WebUtility.HtmlDecode(field.GetAttributeValue("value", ""));
                if (!string.IsNullOrEmpty(name)) formData[name] = value;
            }

        // Add search parameters
        formData["ctl00$MainContent$txtLastName"] = searchTerm;
        formData["ctl00$MainContent$ddlType"] = ""; // All types
        formData["ctl00$MainContent$lstPrograms"] = ""; // All programs
        formData["ctl00$MainContent$txtAddress"] = "";
        formData["ctl00$MainContent$txtCity"] = "";
        formData["ctl00$MainContent$txtState"] = "";
        formData["ctl00$MainContent$txtID"] = "";
        formData["ctl00$MainContent$ddlCountry"] = ""; // All countries
        formData["ctl00$MainContent$ddlList"] = ""; // All lists (SDN + Non-SDN)
        formData["ctl00$MainContent$Slider1"] = "100";
        formData["ctl00$MainContent$Slider1_Boundcontrol"] = "100"; // Minimum score 100%
        formData["ctl00$MainContent$btnSearch"] = "Search";

        return formData;
    }

    private static List<RiskEntry> ParseResults(string html, ILogger logger)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var entries = new List<RiskEntry>();

        // Results are in: <div id="scrollResults"><div><table>...</table></div></div>
        var scrollResults = doc.DocumentNode.SelectSingleNode("//div[@id='scrollResults']");
        if (scrollResults == null)
        {
            logger.LogWarning("OFAC: scrollResults div not found");
            return entries;
        }

        var tables = scrollResults.SelectNodes(".//table");
        if (tables == null || tables.Count == 0)
        {
            logger.LogWarning("OFAC: No tables found inside scrollResults");
            return entries;
        }

        // Get the results table (usually the only table, or the last one)
        var resultsTable = tables.Last();
        var rows = resultsTable.SelectNodes(".//tr");

        if (rows == null || rows.Count == 0)
        {
            logger.LogDebug("OFAC: No results found");
            return entries;
        }

        // Process all data rows
        foreach (var row in rows)
        {
            var cells = row.SelectNodes(".//td");

            if (cells == null || cells.Count < 6) continue; // Skip rows that don't have all required columns

            // Extract data from cells: Name, Address, Type, Programs, List, Score
            var name = CleanText(cells[0].InnerText);
            var address = CleanText(cells[1].InnerText);
            var type = CleanText(cells[2].InnerText);
            var programs = CleanText(cells[3].InnerText);
            var list = CleanText(cells[4].InnerText);
            var scoreText = CleanText(cells[5].InnerText);

            // Parse score as double
            double? score = null;
            if (!string.IsNullOrWhiteSpace(scoreText) &&
                double.TryParse(scoreText, out var parsedScore))
                score = parsedScore;

            // Split programs by semicolon or comma
            string[]? programsArray = null;
            if (!string.IsNullOrWhiteSpace(programs))
                programsArray = programs
                    .Split([';', ','], StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToArray();

            entries.Add(new RiskEntry(
                "OFAC",
                string.IsNullOrWhiteSpace(name) ? null : name,
                string.IsNullOrWhiteSpace(address) ? null : address,
                string.IsNullOrWhiteSpace(type) ? null : type,
                string.IsNullOrWhiteSpace(list) ? null : list,
                programsArray,
                score,
                null,
                null,
                null,
                null,
                null,
                null,
                null));
        }

        return entries;
    }

    private static string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Decode HTML entities
        text = WebUtility.HtmlDecode(text);

        // Remove extra whitespace
        text = Regex.Replace(text, @"\s+", " ");

        return text.Trim();
    }
}