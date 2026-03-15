using System.Xml.Linq;
using RiskScreening.API.Modules.Scraping.Domain.Model;

namespace RiskScreening.API.Modules.Scraping.Infrastructure.Sources;

/// <summary>
///     Downloads the OFAC SDN XML from the US Treasury website,
///     parses with <see cref="XDocument"/>, and matches entries by name.
///     Returns <see cref="SearchResult.Empty"/> on any failure (fault-tolerant).
/// </summary>
public sealed class OfacScrapingSource(
    IHttpClientFactory httpClientFactory,
    ILogger<OfacScrapingSource> logger) : IScrapingSource
{
    private const string XmlUrl = "ofac/downloads/sdn.xml";

    public string SourceName => "OFAC";

    public async Task<SearchResult> SearchAsync(string term, CancellationToken ct = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Ofac");
            await using var stream = await client.GetStreamAsync(XmlUrl, ct);

            var doc = await XDocument.LoadAsync(stream, LoadOptions.None, ct);
            var entries = ParseEntries(doc, term);

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

    private static List<RiskEntry> ParseEntries(XDocument doc, string term)
    {
        var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
        var entries = new List<RiskEntry>();
        var searchWords = term.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var sdnEntries = doc.Descendants(ns + "sdnEntry");

        foreach (var sdnEntry in sdnEntries)
        {
            var firstName = sdnEntry.Element(ns + "firstName")?.Value.Trim() ?? string.Empty;
            var lastName = sdnEntry.Element(ns + "lastName")?.Value.Trim() ?? string.Empty;
            var fullName = $"{firstName} {lastName}".Trim();

            // Also check aliases (akaList/aka)
            var aliases = sdnEntry
                .Elements(ns + "akaList")
                .SelectMany(al => al.Elements(ns + "aka"))
                .Select(aka =>
                {
                    var akaFirst = aka.Element(ns + "firstName")?.Value.Trim() ?? string.Empty;
                    var akaLast = aka.Element(ns + "lastName")?.Value.Trim() ?? string.Empty;
                    return $"{akaFirst} {akaLast}".Trim();
                })
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            // Match if all search words appear in the name or any alias (order-independent)
            var matchedName = MatchesAllWords(fullName, searchWords)
                ? fullName
                : aliases.FirstOrDefault(a => MatchesAllWords(a, searchWords));

            if (string.IsNullOrEmpty(fullName) && matchedName is null)
                continue;

            if (matchedName is null)
                continue;

            var address = ParseAddress(sdnEntry, ns);
            var type = sdnEntry.Element(ns + "sdnType")?.Value;
            var programs = sdnEntry
                .Elements(ns + "programList")
                .SelectMany(pl => pl.Elements(ns + "program"))
                .Select(p => p.Value)
                .ToArray();

            var list = programs.Length > 0 ? string.Join(", ", programs) : null;

            entries.Add(new RiskEntry(
                ListSource: "OFAC",
                Name: fullName,
                Address: address,
                Type: type,
                List: list,
                Programs: programs.Length > 0 ? programs : null,
                Score: null,
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

    private static bool MatchesAllWords(string text, string[] words)
    {
        if (string.IsNullOrEmpty(text)) return false;
        return words.All(w => text.Contains(w, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ParseAddress(XElement sdnEntry, XNamespace ns)
    {
        var addressNode = sdnEntry
            .Elements(ns + "addressList")
            .SelectMany(al => al.Elements(ns + "address"))
            .FirstOrDefault();

        if (addressNode is null) return null;

        var parts = new[]
        {
            addressNode.Element(ns + "address1")?.Value,
            addressNode.Element(ns + "city")?.Value,
            addressNode.Element(ns + "stateOrProvince")?.Value,
            addressNode.Element(ns + "country")?.Value
        };

        var address = string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        return string.IsNullOrEmpty(address) ? null : address;
    }
}
