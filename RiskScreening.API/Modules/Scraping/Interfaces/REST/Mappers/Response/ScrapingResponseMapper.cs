using RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;
using RiskScreening.API.Modules.Scraping.Interfaces.REST.Resources.Responses;

namespace RiskScreening.API.Modules.Scraping.Interfaces.REST.Mappers.Response;

/// <summary>
///     Maps <see cref="SearchResult"/> domain objects to <see cref="ScrapingResponse"/> DTOs
///     for the REST API layer.
/// </summary>
public static class ScrapingResponseMapper
{
    /// <summary>Converts a <see cref="SearchResult"/> into its REST representation.</summary>
    public static ScrapingResponse ToResponse(SearchResult result)
    {
        var entries = result.Entries.Select(ToEntryResponse).ToList();
        return new ScrapingResponse(result.Hits, entries);
    }

    /// <summary>Maps a single <see cref="RiskEntry"/> to its API response DTO.</summary>
    private static RiskEntryResponse ToEntryResponse(RiskEntry entry)
    {
        return new RiskEntryResponse(
            entry.ListSource,
            entry.Name,
            entry.Address,
            entry.Type,
            entry.List,
            entry.Programs,
            entry.Score,
            entry.Country,
            entry.FromDate,
            entry.ToDate,
            entry.Grounds,
            entry.Jurisdiction,
            entry.LinkedTo,
            entry.DataFrom);
    }
}