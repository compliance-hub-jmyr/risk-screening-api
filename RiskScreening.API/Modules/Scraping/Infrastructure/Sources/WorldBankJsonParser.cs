using System.Text.Json;
using System.Text.Json.Serialization;
using RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Scraping.Infrastructure.Sources;

/// <summary>
///     Deserializes the World Bank Debarred Firms JSON API response,
///     filters firms by search term, and maps them to <see cref="RiskEntry"/> records.
///     <list type="bullet">
///         <item>
///             <see cref="ParseAndFilter"/>: deserializes the JSON payload from
///             <c>response.ZPROCSUPP</c>, performs case-insensitive multi-field matching
///             (firm name, address, city, state, country, grounds — OR logic),
///             and returns matching <see cref="RiskEntry"/> records.
///         </item>
///     </list>
///     <para>
///         The World Bank API returns <b>all</b> debarred firms in a single response
///         (no server-side search), so filtering is done client-side.
///     </para>
/// </summary>
internal static class WorldBankJsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     Deserializes the World Bank API JSON and filters firms where the
    ///     <paramref name="searchTerm"/> matches any searchable field (OR logic,
    ///     case-insensitive contains): firm name, address, city, state, country, or grounds.
    ///     This mirrors the client-side filter behavior of the World Bank website.
    /// </summary>
    /// <param name="json">Raw JSON from the World Bank API response.</param>
    /// <param name="searchTerm">The name/entity to search for.</param>
    /// <param name="logger">Logger for diagnostic messages on parse failures.</param>
    /// <returns>Matching risk entries; empty list if no matches or the JSON is invalid.</returns>
    public static List<RiskEntry> ParseAndFilter(string json, string searchTerm, ILogger logger)
    {
        WorldBankApiResponse? response;

        try
        {
            response = JsonSerializer.Deserialize<WorldBankApiResponse>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "World Bank: failed to deserialize JSON response");
            return [];
        }

        var firms = response?.Response?.ZPROCSUPP;
        if (firms is null or { Count: 0 })
        {
            logger.LogDebug("World Bank: no firms in API response");
            return [];
        }

        var term = searchTerm.Trim();

        return firms
            .Where(f => MatchesTerm(f, term))
            .Select(MapToRiskEntry)
            .ToList();
    }

    /// <summary>
    ///     Matches the search term against all searchable fields (OR logic),
    ///     mirroring the World Bank website's client-side filter.
    /// </summary>
    private static bool MatchesTerm(WorldBankFirmDto firm, string term) =>
        Contains(firm.SUPP_NAME, term) ||
        Contains(firm.SUPP_ADDR, term) ||
        Contains(firm.SUPP_CITY, term) ||
        Contains(firm.SUPP_STATE_CODE, term) ||
        Contains(firm.COUNTRY_NAME, term) ||
        Contains(firm.DEBAR_REASON, term);

    private static bool Contains(string? field, string term) =>
        !string.IsNullOrEmpty(field) && field.Contains(term, StringComparison.OrdinalIgnoreCase);

    /// <summary>Maps a single World Bank firm DTO to a <see cref="RiskEntry"/>.</summary>
    private static RiskEntry MapToRiskEntry(WorldBankFirmDto firm)
    {
        // When INELIGIBLY_STATUS is "Permanent" or "Ongoing", show that label
        // instead of the sentinel date (e.g. 2999-12-31) stored in DEBAR_TO_DATE
        var toDate = firm.INELIGIBLY_STATUS is "Permanent" or "Ongoing"
            ? firm.INELIGIBLY_STATUS
            : NullIfEmpty(firm.DEBAR_TO_DATE);

        return new RiskEntry(
            ListSource: "WORLD_BANK",
            Name: NullIfEmpty(firm.SUPP_NAME),
            Address: BuildAddress(firm),
            Type: null,
            List: null,
            Programs: null,
            Score: null,
            Country: NullIfEmpty(firm.COUNTRY_NAME),
            FromDate: NullIfEmpty(firm.DEBAR_FROM_DATE),
            ToDate: toDate,
            Grounds: NullIfEmpty(firm.DEBAR_REASON),
            Jurisdiction: null,
            LinkedTo: null,
            DataFrom: null);
    }

    /// <summary>
    ///     Combines address components (<c>SUPP_ADDR</c>, <c>SUPP_CITY</c>,
    ///     <c>SUPP_STATE_CODE</c>, <c>SUPP_ZIP_CODE</c>) into a single string.
    /// </summary>
    private static string? BuildAddress(WorldBankFirmDto firm)
    {
        var parts = new[] { firm.SUPP_ADDR, firm.SUPP_CITY, firm.SUPP_STATE_CODE, firm.SUPP_ZIP_CODE }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim());

        var address = string.Join(", ", parts);
        return string.IsNullOrEmpty(address) ? null : address;
    }

    /// <summary>Returns <c>null</c> when the string is empty or whitespace-only.</summary>
    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

// JSON DTOs

/// <summary>Top-level envelope of the World Bank Debarred Firms API.</summary>
internal sealed class WorldBankApiResponse
{
    [JsonPropertyName("response")]
    public WorldBankResponseData? Response { get; init; }
}

/// <summary>Contains the <c>ZPROCSUPP</c> array of sanctioned firms.</summary>
internal sealed class WorldBankResponseData
{
    [JsonPropertyName("ZPROCSUPP")]
    public List<WorldBankFirmDto>? ZPROCSUPP { get; init; }
}

/// <summary>
///     Single firm record from the World Bank Debarred Firms API.
///     <para>Field names mirror the API response (SCREAMING_SNAKE_CASE).</para>
/// </summary>
internal sealed class WorldBankFirmDto
{
    [JsonPropertyName("SUPP_NAME")]
    public string? SUPP_NAME { get; init; }

    [JsonPropertyName("ADD_SUPP_INFO")]
    public string? ADD_SUPP_INFO { get; init; }

    [JsonPropertyName("SUPP_ADDR")]
    public string? SUPP_ADDR { get; init; }

    [JsonPropertyName("SUPP_CITY")]
    public string? SUPP_CITY { get; init; }

    [JsonPropertyName("SUPP_STATE_CODE")]
    public string? SUPP_STATE_CODE { get; init; }

    [JsonPropertyName("SUPP_ZIP_CODE")]
    public string? SUPP_ZIP_CODE { get; init; }

    [JsonPropertyName("COUNTRY_NAME")]
    public string? COUNTRY_NAME { get; init; }

    [JsonPropertyName("DEBAR_FROM_DATE")]
    public string? DEBAR_FROM_DATE { get; init; }

    [JsonPropertyName("DEBAR_TO_DATE")]
    public string? DEBAR_TO_DATE { get; init; }

    [JsonPropertyName("DEBAR_REASON")]
    public string? DEBAR_REASON { get; init; }

    [JsonPropertyName("INELIGIBLY_STATUS")]
    public string? INELIGIBLY_STATUS { get; init; }
}
