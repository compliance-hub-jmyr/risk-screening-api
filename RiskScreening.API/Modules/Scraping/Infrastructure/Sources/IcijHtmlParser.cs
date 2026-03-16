using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Scraping.Infrastructure.Sources;

/// <summary>
///     Parses the ICIJ Offshore Leaks search results HTML page.
///     <list type="bullet">
///         <item>
///             <see cref="ParseResults"/>: locates the search results table in
///             the HTML and converts each row into a <see cref="RiskEntry"/>
///             with fields: Name, Jurisdiction, LinkedTo (countries), DataFrom (source investigation).
///         </item>
///     </list>
///     <para>
///         The ICIJ search page is a JavaScript SPA rendered via Playwright.
///         The resulting DOM contains a <c>&lt;table class="table"&gt;</c> with columns:
///         Entity, Jurisdiction, Linked To, Data From.
///     </para>
/// </summary>
internal static partial class IcijHtmlParser
{
    /// <summary>
    ///     Parses the ICIJ search results HTML into a list of <see cref="RiskEntry"/>.
    ///     <para>
    ///         Expected HTML structure:
    ///         <c>&lt;table class="table"&gt; → &lt;tbody&gt; → &lt;tr&gt;</c>
    ///         with columns: Entity (name + link), Jurisdiction, Linked To, Data From.
    ///     </para>
    /// </summary>
    /// <param name="html">Raw HTML from the ICIJ search results page.</param>
    /// <param name="logger">Logger for diagnostic messages when structure is unexpected.</param>
    /// <returns>Parsed risk entries; empty list if the structure is invalid or no matches.</returns>
    public static List<RiskEntry> ParseResults(string html, ILogger logger)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var entries = new List<RiskEntry>();

        // Locate the search results table
        var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class,'table')]");
        if (table is null)
        {
            logger.LogDebug("ICIJ: results table not found - page may still be loading or no results available");
            return entries;
        }

        var rows = table.SelectNodes(".//tbody/tr");
        if (rows is null or { Count: 0 })
        {
            logger.LogDebug("ICIJ: no result rows found - search completed with 0 matches");
            return entries;
        }

        foreach (var row in rows)
        {
            var cells = row.SelectNodes(".//td");
            if (cells is null || cells.Count < 4) 
            {
                logger.LogDebug("ICIJ: Skipping row with insufficient columns (expected 4, got {Count})", cells?.Count ?? 0);
                continue;
            }

            // Columns: Entity | Jurisdiction | Linked To | Data From
            var name = CleanText(cells[0].InnerText);
            var jurisdiction = CleanText(cells[1].InnerText);
            var linkedTo = CleanText(cells[2].InnerText);
            var dataFrom = CleanText(cells[3].InnerText);

            entries.Add(new RiskEntry(
                "ICIJ",
                NullIfEmpty(name),
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                NullIfEmpty(jurisdiction),
                NullIfEmpty(linkedTo),
                NullIfEmpty(dataFrom)));
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
    private static string? NullIfEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}