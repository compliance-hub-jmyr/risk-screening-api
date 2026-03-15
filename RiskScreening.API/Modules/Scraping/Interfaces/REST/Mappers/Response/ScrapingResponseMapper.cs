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

    private static RiskEntryResponse ToEntryResponse(RiskEntry entry) =>
        new(
            ListSource: entry.ListSource,
            Name: entry.Name,
            Address: entry.Address,
            Type: entry.Type,
            List: entry.List,
            Programs: entry.Programs,
            Score: entry.Score,
            Country: entry.Country,
            FromDate: entry.FromDate,
            ToDate: entry.ToDate,
            Grounds: entry.Grounds,
            Jurisdiction: entry.Jurisdiction,
            LinkedTo: entry.LinkedTo,
            DataFrom: entry.DataFrom);
}
