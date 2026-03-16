using RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;

namespace RiskScreening.UnitTests.Modules.Scraping.Mothers;

/// <summary>
///     Named factory for <see cref="SearchResult"/> test instances.
/// </summary>
public static class SearchResultMother
{
    public static SearchResult Empty() => SearchResult.Empty;

    public static SearchResult WithOfacEntries(int count = 1) =>
        new(count, Enumerable.Range(0, count)
            .Select(_ => RiskEntryMother.Ofac())
            .ToList());

    public static SearchResult WithWorldBankEntries(int count = 1) =>
        new(count, Enumerable.Range(0, count)
            .Select(_ => RiskEntryMother.WorldBank())
            .ToList());

    public static SearchResult WithIcijEntries(int count = 1) =>
        new(count, Enumerable.Range(0, count)
            .Select(_ => RiskEntryMother.Icij())
            .ToList());

    public static SearchResult WithEntries(params RiskEntry[] entries) =>
        new(entries.Length, entries.ToList());
}
