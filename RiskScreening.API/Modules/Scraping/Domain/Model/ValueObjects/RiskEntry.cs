namespace RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;

/// <summary>
///     Unified risk entry from any sanctions/debarment source.
///     <para>
///         Each source populates only its relevant fields — remaining fields are <c>null</c>.
///     </para>
///     <list type="bullet">
///         <item><b>OFAC:</b> Name, Address, Type, List, Programs, Score</item>
///         <item><b>World Bank:</b> Name, Address, Country, FromDate, ToDate, Grounds</item>
///         <item><b>ICIJ:</b> Name, Jurisdiction, LinkedTo, DataFrom</item>
///     </list>
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
