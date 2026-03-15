using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Scraping.Infrastructure.Sources;

/// <summary>
///     Parses the World Bank Debarred Firms HTML page and JSON API response.
///     <list type="bullet">
///         <item>
///             <see cref="ExtractApiConfig"/>: scrapes <c>&lt;script&gt;</c> tags with
///             <see cref="HtmlAgilityPack"/> to extract the API URL and API key
///             from JavaScript variables (<c>prodtabApi</c>, <c>propApiKey</c>).
///         </item>
///         <item>
///             <see cref="ParseResults"/>: deserializes the JSON payload from
///             <c>response.ZPROCSUPP</c>, performs case-insensitive multi-field matching
///             (firm name, address, city, state, country, grounds — OR logic),
///             and returns matching <see cref="RiskEntry"/> records.
///         </item>
///     </list>
/// </summary>
internal static partial class WorldBankHtmlParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Step 1: HTML scraping

    /// <summary>
    ///     Extracts the API URL and API key from the World Bank page's
    ///     embedded JavaScript using <see cref="HtmlAgilityPack"/>.
    /// </summary>
    /// <param name="html">Raw HTML from the debarred firms page.</param>
    /// <returns>
    ///     A tuple with the API URL and API key, or <c>(null, null)</c>
    ///     if the JavaScript variables could not be found.
    /// </returns>
    public static (string? ApiUrl, string? ApiKey) ExtractApiConfig(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var scripts = doc.DocumentNode.SelectNodes("//script");
        if (scripts is null) return (null, null);

        string? apiUrl = null;
        string? apiKey = null;

        foreach (var script in scripts)
        {
            var text = script.InnerText;
            if (string.IsNullOrWhiteSpace(text)) continue;

            if (apiUrl is null)
            {
                var urlMatch = ApiUrlRegex().Match(text);
                if (urlMatch.Success)
                    apiUrl = urlMatch.Groups[1].Value;
            }

            if (apiKey is null)
            {
                var keyMatch = ApiKeyRegex().Match(text);
                if (keyMatch.Success)
                    apiKey = keyMatch.Groups[1].Value;
            }

            if (apiUrl is not null && apiKey is not null)
                break;
        }

        return (apiUrl, apiKey);
    }

    // Step 2: JSON parsing

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
    public static List<RiskEntry> ParseResults(string json, string searchTerm, ILogger logger)
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

        var firms = response?.Response?.Zprocsupp;
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

    // Private helpers

    /// <summary>
    ///     Matches the search term against all searchable fields (OR logic),
    ///     mirroring the World Bank website's client-side filter.
    /// </summary>
    private static bool MatchesTerm(WorldBankFirmDto firm, string term) =>
        Contains(firm.SuppName, term) ||
        Contains(firm.SuppAddr, term) ||
        Contains(firm.SuppCity, term) ||
        Contains(firm.SuppStateCode, term) ||
        Contains(firm.CountryName, term) ||
        Contains(firm.DebarReason, term);

    private static bool Contains(string? field, string term) =>
        !string.IsNullOrEmpty(field) && field.Contains(term, StringComparison.OrdinalIgnoreCase);

    /// <summary>Maps a single World Bank firm DTO to a <see cref="RiskEntry"/>.</summary>
    private static RiskEntry MapToRiskEntry(WorldBankFirmDto firm)
    {
        // When INELIGIBLY_STATUS is "Permanent" or "Ongoing", show that label
        // instead of the sentinel date (e.g. 2999-12-31) stored in DEBAR_TO_DATE
        var toDate = firm.IneligiblyStatus is "Permanent" or "Ongoing"
            ? firm.IneligiblyStatus
            : NullIfEmpty(firm.DebarToDate);

        return new RiskEntry(
            ListSource: "WORLD_BANK",
            Name: NullIfEmpty(firm.SuppName),
            Address: BuildAddress(firm),
            Type: null,
            List: null,
            Programs: null,
            Score: null,
            Country: NullIfEmpty(firm.CountryName),
            FromDate: NullIfEmpty(firm.DebarFromDate),
            ToDate: toDate,
            Grounds: NullIfEmpty(firm.DebarReason),
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
        var parts = new[] { firm.SuppAddr, firm.SuppCity, firm.SuppStateCode, firm.SuppZipCode }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim());

        var address = string.Join(", ", parts);
        return string.IsNullOrEmpty(address) ? null : address;
    }

    /// <summary>Returns <c>null</c> when the string is empty or whitespace-only.</summary>
    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Matches <c>var prodtabApi = "..."</c> in JavaScript.</summary>
    [GeneratedRegex("""var\s+prodtabApi\s*=\s*"([^"]+)"\s*;""")]
    private static partial Regex ApiUrlRegex();

    /// <summary>Matches <c>var propApiKey = "..."</c> in JavaScript.</summary>
    [GeneratedRegex("""var\s+propApiKey\s*=\s*"([^"]+)"\s*;""")]
    private static partial Regex ApiKeyRegex();
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
    public List<WorldBankFirmDto>? Zprocsupp { get; init; }
}

/// <summary>
///     Single firm record from the World Bank Debarred Firms API.
///     <para>Field names mirror the API response (SCREAMING_SNAKE_CASE).</para>
/// </summary>
internal sealed class WorldBankFirmDto
{
    [JsonPropertyName("SUPP_NAME")]
    public string? SuppName { get; init; }

    [JsonPropertyName("ADD_SUPP_INFO")]
    public string? AddSuppInfo { get; init; }

    [JsonPropertyName("SUPP_ADDR")]
    public string? SuppAddr { get; init; }

    [JsonPropertyName("SUPP_CITY")]
    public string? SuppCity { get; init; }

    [JsonPropertyName("SUPP_STATE_CODE")]
    public string? SuppStateCode { get; init; }

    [JsonPropertyName("SUPP_ZIP_CODE")]
    public string? SuppZipCode { get; init; }

    [JsonPropertyName("COUNTRY_NAME")]
    public string? CountryName { get; init; }

    [JsonPropertyName("DEBAR_FROM_DATE")]
    public string? DebarFromDate { get; init; }

    [JsonPropertyName("DEBAR_TO_DATE")]
    public string? DebarToDate { get; init; }

    [JsonPropertyName("DEBAR_REASON")]
    public string? DebarReason { get; init; }

    [JsonPropertyName("INELIGIBLY_STATUS")]
    public string? IneligiblyStatus { get; init; }
}
