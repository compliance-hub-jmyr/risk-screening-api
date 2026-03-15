namespace RiskScreening.API.Modules.Scraping.Domain.Model;

/// <summary>
///     Aggregated search result from one or more risk list sources.
/// </summary>
public record SearchResult(int Hits, IReadOnlyList<RiskEntry> Entries)
{
    /// <summary>Empty result — zero hits, no entries.</summary>
    public static readonly SearchResult Empty = new(0, []);

    /// <summary>
    ///     Merges multiple search results into one.
    ///     Sums <see cref="Hits"/> and concatenates <see cref="Entries"/>.
    ///     No deduplication — an entity present in multiple lists is counted multiple times.
    /// </summary>
    public static SearchResult Merge(IEnumerable<SearchResult> results)
    {
        var all = results.ToList();
        var totalHits = all.Sum(r => r.Hits);
        var allEntries = all.SelectMany(r => r.Entries).ToList();
        return new SearchResult(totalHits, allEntries);
    }
}