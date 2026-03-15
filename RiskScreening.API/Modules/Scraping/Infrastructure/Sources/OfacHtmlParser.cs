using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Scraping.Infrastructure.Sources;

/// <summary>
///     Parses OFAC Sanctions List Search HTML pages.
///     <list type="bullet">
///         <item>
///             <see cref="ExtractFormData"/>: extracts ASP.NET ViewState and hidden fields
///             from the initial GET page, then appends the search parameters for the POST.
///         </item>
///         <item>
///             <see cref="ParseResults"/>: locates the <c>#scrollResults</c> table in the
///             POST response and converts each <c>&lt;tr&gt;</c> into a <see cref="RiskEntry"/>.
///         </item>
///     </list>
/// </summary>
internal static partial class OfacHtmlParser
{
    /// <summary>
    ///     Extracts hidden ASP.NET form fields (ViewState, EventValidation, etc.)
    ///     from the initial page HTML and adds the OFAC search parameters.
    /// </summary>
    /// <param name="html">Raw HTML from the initial GET request.</param>
    /// <param name="searchTerm">The name/entity to search for.</param>
    /// <returns>
    ///     A dictionary ready to be sent as <see cref="FormUrlEncodedContent"/>.
    /// </returns>
    public static Dictionary<string, string> ExtractFormData(string html, string searchTerm)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var formData = new Dictionary<string, string>();

        // ASP.NET hidden fields (ViewState, EventValidation, etc.) 
        var hiddenFields = doc.DocumentNode.SelectNodes("//input[@type='hidden']");
        if (hiddenFields != null)
            foreach (var field in hiddenFields)
            {
                var name = field.GetAttributeValue("name", "");
                var value = WebUtility.HtmlDecode(field.GetAttributeValue("value", ""));
                if (!string.IsNullOrEmpty(name)) formData[name] = value;
            }

        // OFAC search form parameters
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

    /// <summary>
    ///     Parses the OFAC results HTML table into a list of <see cref="RiskEntry"/>.
    ///     <para>
    ///         Expected HTML structure:
    ///         <c>&lt;div id="scrollResults"&gt; → &lt;table&gt; → &lt;tr&gt;</c>
    ///         with 6 columns: Name, Address, Type, Programs, List, Score.
    ///     </para>
    /// </summary>
    /// <param name="html">Raw HTML from the POST search response.</param>
    /// <param name="logger">Logger for diagnostic messages when structure is unexpected.</param>
    /// <returns>Parsed risk entries; empty list if the structure is invalid or no matches.</returns>
    public static List<RiskEntry> ParseResults(string html, ILogger logger)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var entries = new List<RiskEntry>();

        // Locate the results container
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

        // Iterate result rows
        var resultsTable = tables.Last();
        var rows = resultsTable.SelectNodes(".//tr");

        if (rows == null || rows.Count == 0)
        {
            logger.LogDebug("OFAC: No results found");
            return entries;
        }

        foreach (var row in rows)
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 6) continue;

            // Columns: Name | Address | Type | Programs | List | Score
            var name = CleanText(cells[0].InnerText);
            var address = CleanText(cells[1].InnerText);
            var type = CleanText(cells[2].InnerText);
            var programs = CleanText(cells[3].InnerText);
            var list = CleanText(cells[4].InnerText);
            var scoreText = CleanText(cells[5].InnerText);

            // Parse score
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
                ListSource: "OFAC",
                Name: NullIfEmpty(name),
                Address: NullIfEmpty(address),
                Type: NullIfEmpty(type),
                List: NullIfEmpty(list),
                Programs: programsArray,
                Score: score,
                Country: null,
                FromDate: null,
                ToDate: null,
                Grounds: null,
                Jurisdiction: null,
                LinkedTo: null,
                DataFrom: null));
        }

        return entries;
    }

    /// <summary>Decodes HTML entities and normalizes whitespace.</summary>
    private static string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = WebUtility.HtmlDecode(text);
        text = WhitespaceRegex().Replace(text, " ");

        return text.Trim();
    }

    /// <summary>Returns <c>null</c> when the string is empty or whitespace-only.</summary>
    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
