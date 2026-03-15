using RiskScreening.API.Modules.Scraping.Domain.Model;
using RiskScreening.API.Modules.Scraping.Interfaces.REST.Resources.Responses;

namespace RiskScreening.API.Modules.Scraping.Interfaces.REST.Mappers.Response;

public static class ScrapingResponseMapper
{
    public static ScrapingResponse ToResponse(SearchResult result)
    {
        var entries = result.Entries.Select(ToEntryResponse).ToList();
        return new ScrapingResponse(result.Hits, entries);
    }

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