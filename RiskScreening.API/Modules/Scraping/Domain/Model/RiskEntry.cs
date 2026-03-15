namespace RiskScreening.API.Modules.Scraping.Domain.Model;

/// <summary>
///     Unified risk entry from any sanctions/debarment source.
///     Fields not applicable to a given source are <c>null</c>.
/// </summary>
public record RiskEntry(
    string ListSource,
    string? Name,
    string? Address,
    string? Type,
    string? List,
    string[]? Programs,
    double? Score,
    string? Country,
    string? FromDate,
    string? ToDate,
    string? Grounds,
    string? Jurisdiction,
    string? LinkedTo,
    string? DataFrom);