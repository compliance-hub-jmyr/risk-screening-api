using RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;

namespace RiskScreening.UnitTests.Modules.Scraping.Mothers;

/// <summary>
///     Named factory for <see cref="RiskEntry"/> test instances.
/// </summary>
public static class RiskEntryMother
{
    public static RiskEntry Ofac(
        string name = "LAZARUS GROUP",
        string? address = "Potonggang District",
        string type = "Entity",
        string list = "SDN",
        string[]? programs = null,
        double? score = 100) =>
        new(
            ListSource: "OFAC",
            Name: name,
            Address: address,
            Type: type,
            List: list,
            Programs: programs ?? ["DPRK3"],
            Score: score,
            Country: null,
            FromDate: null,
            ToDate: null,
            Grounds: null,
            Jurisdiction: null,
            LinkedTo: null,
            DataFrom: null);

    public static RiskEntry WorldBank(
        string name = "Acme Corp",
        string? address = "123 Business Ave",
        string country = "GB",
        string fromDate = "2020-01-15",
        string toDate = "2025-01-15",
        string grounds = "Fraudulent practice") =>
        new(
            ListSource: "WORLD_BANK",
            Name: name,
            Address: address,
            Type: null,
            List: null,
            Programs: null,
            Score: null,
            Country: country,
            FromDate: fromDate,
            ToDate: toDate,
            Grounds: grounds,
            Jurisdiction: null,
            LinkedTo: null,
            DataFrom: null);

    public static RiskEntry Icij(
        string name = "Mossack Fonseca",
        string jurisdiction = "Panama",
        string linkedTo = "Panama Papers",
        string dataFrom = "Panama Papers") =>
        new(
            ListSource: "ICIJ",
            Name: name,
            Address: null,
            Type: null,
            List: null,
            Programs: null,
            Score: null,
            Country: null,
            FromDate: null,
            ToDate: null,
            Grounds: null,
            Jurisdiction: jurisdiction,
            LinkedTo: linkedTo,
            DataFrom: dataFrom);
}
