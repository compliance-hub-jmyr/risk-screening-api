namespace RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;

/// <summary>
///     Aggregated search result from one or more risk list sources.
///     <para>
///         Immutable value object that holds a hit count and the list of
///         <see cref="RiskEntry"/> records returned by the scraping operation.
///     </para>
/// </summary>
public record SearchResult(int Hits, IReadOnlyList<RiskEntry> Entries)
{
    /// <summary>Sentinel value for an empty result — zero hits, no entries.</summary>
    public static readonly SearchResult Empty = new(0, []);

    /// <summary>
    ///     Merges multiple <see cref="SearchResult"/> instances into one.
    ///     <para>
    ///         Sums <see cref="Hits"/> and concatenates <see cref="Entries"/>.
    ///         No deduplication — an entity present in multiple lists appears multiple times.
    ///     </para>
    /// </summary>
    public static SearchResult Merge(IEnumerable<SearchResult> results)
    {
        var all = results.ToList();
        var totalHits = all.Sum(r => r.Hits);
        var allEntries = all.SelectMany(r => r.Entries).ToList();
        return new SearchResult(totalHits, allEntries);
    }
}