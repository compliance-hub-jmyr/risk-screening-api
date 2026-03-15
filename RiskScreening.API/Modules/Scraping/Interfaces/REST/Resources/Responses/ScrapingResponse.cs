using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace RiskScreening.API.Modules.Scraping.Interfaces.REST.Resources.Responses;

/// <summary>
///     Response DTO for scraping search results.
/// </summary>
public record ScrapingResponse(
    [property: SwaggerSchema(
        Description = "Total number of matching entries across the searched source(s).",
        Nullable = false)]
    [property: JsonPropertyName("hits")]
    int Hits,
    [property: SwaggerSchema(
        Description = "List of matching risk entries.",
        Nullable = false)]
    [property: JsonPropertyName("entries")]
    IReadOnlyList<RiskEntryResponse> Entries);

/// <summary>
///     Single risk entry in the scraping response.
///     Fields not applicable to a given source are <c>null</c>.
/// </summary>
public record RiskEntryResponse(
    [property: SwaggerSchema(Description = "Source list identifier (OFAC, WORLD_BANK, ICIJ).", Nullable = false)]
    [property: JsonPropertyName("listSource")]
    string ListSource,
    [property: SwaggerSchema(Description = "Entity name.")]
    [property: JsonPropertyName("name")]
    string? Name,
    [property: SwaggerSchema(Description = "Physical address (OFAC, World Bank).")]
    [property: JsonPropertyName("address")]
    string? Address,
    [property: SwaggerSchema(Description = "Entity type (OFAC).")]
    [property: JsonPropertyName("type")]
    string? Type,
    [property: SwaggerSchema(Description = "Sanctions list name (OFAC).")]
    [property: JsonPropertyName("list")]
    string? List,
    [property: SwaggerSchema(Description = "Sanctions programs (OFAC).")]
    [property: JsonPropertyName("programs")]
    string[]? Programs,
    [property: SwaggerSchema(Description = "Match confidence score (OFAC).", Format = "double")]
    [property: JsonPropertyName("score")]
    double? Score,
    [property: SwaggerSchema(Description = "Country (World Bank).")]
    [property: JsonPropertyName("country")]
    string? Country,
    [property: SwaggerSchema(Description = "Ineligibility start date (World Bank).")]
    [property: JsonPropertyName("fromDate")]
    string? FromDate,
    [property: SwaggerSchema(Description = "Ineligibility end date (World Bank).")]
    [property: JsonPropertyName("toDate")]
    string? ToDate,
    [property: SwaggerSchema(Description = "Debarment grounds (World Bank).")]
    [property: JsonPropertyName("grounds")]
    string? Grounds,
    [property: SwaggerSchema(Description = "Legal jurisdiction (ICIJ).")]
    [property: JsonPropertyName("jurisdiction")]
    string? Jurisdiction,
    [property: SwaggerSchema(Description = "Linked entities (ICIJ).")]
    [property: JsonPropertyName("linkedTo")]
    string? LinkedTo,
    [property: SwaggerSchema(Description = "Source dataset name (ICIJ).")]
    [property: JsonPropertyName("dataFrom")]
    string? DataFrom);