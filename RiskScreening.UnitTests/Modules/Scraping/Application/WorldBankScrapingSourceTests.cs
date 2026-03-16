using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;
using RiskScreening.API.Modules.Scraping.Infrastructure.Sources;
using RiskScreening.UnitTests.Modules.Scraping.Mothers;
using Xunit;

namespace RiskScreening.UnitTests.Modules.Scraping.Application;

/// <summary>
///     Unit tests for <see cref="WorldBankScrapingSource"/>.
///     Uses <see cref="WorldBankJsonMother"/> for HTML + JSON fixtures and
///     <see cref="FakeHttpMessageHandler"/> to simulate the two-step scraping flow:
///     first call → GET HTML page, second call → GET JSON API.
/// </summary>
public class WorldBankScrapingSourceTests
{
    private readonly ILogger<WorldBankScrapingSource> _logger =
        Substitute.For<ILogger<WorldBankScrapingSource>>();

    /// <summary>
    ///     Creates a <see cref="WorldBankScrapingSource"/> backed by fake HTTP responses.
    ///     The first call returns the HTML page (with embedded API config),
    ///     the second call returns the JSON API response.
    /// </summary>
    private WorldBankScrapingSource CreateSource(string pageHtml, string apiJson)
    {
        var handler = new FakeHttpMessageHandler(pageHtml, apiJson);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://projects.worldbank.org/")
        };

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("WorldBank").Returns(httpClient);

        return new WorldBankScrapingSource(factory, _logger);
    }

    /// <summary>Shorthand: creates a source with the default HTML page and the given JSON.</summary>
    private WorldBankScrapingSource CreateSource(string apiJson) =>
        CreateSource(WorldBankJsonMother.DebarredFirmsPage(), apiJson);

    // success paths

    [Fact]
    public async Task SearchAsync_MatchingEntries_ReturnsResults()
    {
        // Arrange
        var sut = CreateSource(
            WorldBankJsonMother.ApiResponse(
                ("Acme Corp", "123 Business Ave", "London", "GB",
                 "2020-01-15", "2025-01-15", "Fraudulent practice")));

        // Act
        var result = await sut.SearchAsync("Acme Corp");

        // Assert
        result.Hits.Should().Be(1);
        result.Entries.Should().HaveCount(1);

        var entry = result.Entries[0];
        entry.ListSource.Should().Be("WORLD_BANK");
        entry.Name.Should().Be("Acme Corp");
        entry.Address.Should().Be("123 Business Ave, London");
        entry.Country.Should().Be("GB");
        entry.FromDate.Should().Be("2020-01-15");
        entry.ToDate.Should().Be("2025-01-15");
        entry.Grounds.Should().Be("Fraudulent practice");
    }

    [Fact]
    public async Task SearchAsync_CaseInsensitive_MatchesRegardlessOfCase()
    {
        // Arrange
        var sut = CreateSource(
            WorldBankJsonMother.ApiResponse(
                ("Acme Corp", "", "", "GB", "2020-01-15", "2025-01-15", "Fraud")));

        // Act
        var result = await sut.SearchAsync("acme corp");

        // Assert
        result.Hits.Should().Be(1);
        result.Entries[0].Name.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task SearchAsync_PartialMatch_ReturnsMatchingEntries()
    {
        // Arrange
        var sut = CreateSource(
            WorldBankJsonMother.ApiResponse(
                ("Acme Corp", "", "", "GB", "2020-01-15", "2025-01-15", "Fraud"),
                ("Acme Industries", "", "", "US", "2021-03-01", "2026-03-01", "Corruption"),
                ("Other Company", "", "", "FR", "2019-06-15", "2024-06-15", "Collusion")));

        // Act
        var result = await sut.SearchAsync("Acme");

        // Assert
        result.Hits.Should().Be(2);
        result.Entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_WorldBankSpecificFields_OtherSourceFieldsAreNull()
    {
        // Arrange
        var sut = CreateSource(
            WorldBankJsonMother.ApiResponse(
                ("Acme Corp", "123 Main St", "London", "GB",
                 "2020-01-15", "2025-01-15", "Fraudulent practice")));

        // Act
        var result = await sut.SearchAsync("Acme Corp");

        // Assert — OFAC fields
        result.Entries[0].Type.Should().BeNull();
        result.Entries[0].List.Should().BeNull();
        result.Entries[0].Programs.Should().BeNull();
        result.Entries[0].Score.Should().BeNull();

        // Assert — ICIJ fields
        result.Entries[0].Jurisdiction.Should().BeNull();
        result.Entries[0].LinkedTo.Should().BeNull();
        result.Entries[0].DataFrom.Should().BeNull();

        // Assert — World Bank fields ARE set
        result.Entries[0].Country.Should().Be("GB");
        result.Entries[0].FromDate.Should().Be("2020-01-15");
        result.Entries[0].ToDate.Should().Be("2025-01-15");
        result.Entries[0].Grounds.Should().Be("Fraudulent practice");
    }

    [Fact]
    public async Task SearchAsync_AddressComponents_CombinedIntoSingleField()
    {
        // Arrange
        var sut = CreateSource(
            WorldBankJsonMother.ApiResponse(
                ("Acme Corp", "456 Oak Ave", "New York", "US",
                 "2020-01-15", "2025-01-15", "Fraud")));

        // Act
        var result = await sut.SearchAsync("Acme Corp");

        // Assert — address and city combined with comma separator
        result.Entries[0].Address.Should().Be("456 Oak Ave, New York");
    }

    [Fact]
    public async Task SearchAsync_NoAddressComponents_AddressIsNull()
    {
        // Arrange
        var sut = CreateSource(
            WorldBankJsonMother.ApiResponse(
                ("Acme Corp", "", "", "GB", "2020-01-15", "2025-01-15", "Fraud")));

        // Act
        var result = await sut.SearchAsync("Acme Corp");

        // Assert
        result.Entries[0].Address.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_OngoingDebarment_ShowsOngoingNotSentinelDate()
    {
        // Arrange — firm with "Ongoing" status (DEBAR_TO_DATE has sentinel 2999-12-31)
        var sut = CreateSource(
            WorldBankJsonMother.OngoingDebarmentResponse(
                "NATIONAL BIO-MEDICAL PVT. LTD.", "KATHMANDU", "Nepal",
                "2018-04-10", "Procurement Guidelines, 1.14(a)(i)-(ii)"));

        // Act
        var result = await sut.SearchAsync("NATIONAL BIO-MEDICAL");

        // Assert — ToDate shows "Ongoing", NOT the sentinel date
        result.Entries[0].ToDate.Should().Be("Ongoing");
        result.Entries[0].FromDate.Should().Be("2018-04-10");
    }

    [Fact]
    public async Task SearchAsync_PermanentStatus_ShowsPermanent()
    {
        // Arrange
        var sut = CreateSource(
            WorldBankJsonMother.PermanentDebarmentResponse(
                "Acme Corp", "GB", "2018-05-01", "Fraudulent practice"));

        // Act
        var result = await sut.SearchAsync("Acme Corp");

        // Assert
        result.Entries[0].ToDate.Should().Be("Permanent");
    }

    // Multi-field search (OR logic)

    [Fact]
    public async Task SearchAsync_MatchByAddress_ReturnsEntry()
    {
        // Arrange — search term matches address, not firm name
        var sut = CreateSource(
            WorldBankJsonMother.ApiResponse(
                ("MR. NABARAJ BASNET", "NATIONAL BIO-MEDICAL PVT. LTD., TRIPURAPATH", "KATHMANDU", "Nepal",
                 "2018-04-10", "2999-12-31", "Procurement Guidelines, 1.14(a)(i)")));

        // Act — search by text that appears in the address
        var result = await sut.SearchAsync("NATIONAL BIO-MEDICAL");

        // Assert — matched via the address field
        result.Hits.Should().Be(1);
        result.Entries[0].Name.Should().Be("MR. NABARAJ BASNET");
    }

    [Fact]
    public async Task SearchAsync_MatchByCountry_ReturnsEntry()
    {
        // Arrange
        var sut = CreateSource(
            WorldBankJsonMother.ApiResponse(
                ("Acme Corp", "", "", "Nepal", "2020-01-15", "2025-01-15", "Fraud")));

        // Act — search by country name
        var result = await sut.SearchAsync("Nepal");

        // Assert
        result.Hits.Should().Be(1);
        result.Entries[0].Name.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task SearchAsync_MatchByGrounds_ReturnsEntry()
    {
        // Arrange
        var sut = CreateSource(
            WorldBankJsonMother.ApiResponse(
                ("Acme Corp", "", "", "GB", "2020-01-15", "2025-01-15", "Fraudulent practice")));

        // Act — search by grounds text
        var result = await sut.SearchAsync("Fraudulent");

        // Assert
        result.Hits.Should().Be(1);
        result.Entries[0].Grounds.Should().Be("Fraudulent practice");
    }

    [Fact]
    public async Task SearchAsync_MultiFieldOrLogic_MatchesAcrossFields()
    {
        // Arrange — like the real World Bank case: "NATIONAL BIO-MEDICAL" matches
        // both by firm name (first entry) and by address (second entry)
        var sut = CreateSource(
            WorldBankJsonMother.ApiResponse(
                ("NATIONAL BIO-MEDICAL PVT. LTD.", "TRIPURAPATH, TRIPURESHWOR", "KATHMANDU", "Nepal",
                 "2018-04-10", "2999-12-31", "Procurement Guidelines, 1.14(a)(i)-(ii)"),
                ("MR. NABARAJ BASNET", "NATIONAL BIO-MEDICAL PVT. LTD., TRIPURAPATH", "KATHMANDU", "Nepal",
                 "2018-04-10", "2999-12-31", "Procurement Guidelines, 1.14(a)(i)"),
                ("Other Company", "", "", "GB", "2020-01-15", "2025-01-15", "Collusion")));

        // Act
        var result = await sut.SearchAsync("NATIONAL BIO-MEDICAL");

        // Assert — 2 hits: one by name, one by address
        result.Hits.Should().Be(2);
    }

    // Web scraping step (HTML parsing)

    [Fact]
    public async Task SearchAsync_ExtractsApiConfigFromHtml_ThenQueriesApi()
    {
        // Arrange — explicit HTML page with custom API URL + key
        var sut = CreateSource(
            WorldBankJsonMother.DebarredFirmsPage(
                apiUrl: "https://custom-api.example.com/firms",
                apiKey: "custom-key-999"),
            WorldBankJsonMother.ApiResponse(
                ("Acme Corp", "", "", "GB", "2020-01-15", "2025-01-15", "Fraud")));

        // Act — the source scrapes the HTML, extracts config, then queries the API
        var result = await sut.SearchAsync("Acme Corp");

        // Assert — confirms the two-step flow works end-to-end
        result.Hits.Should().Be(1);
        result.Entries[0].Name.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task SearchAsync_HtmlWithoutApiConfig_ReturnsEmptyResult()
    {
        // Arrange — HTML page missing the JavaScript variables
        var sut = CreateSource(
            "<html><body><p>No script tags here</p></body></html>",
            WorldBankJsonMother.EmptyApiResponse());

        // Act
        var result = await sut.SearchAsync("Acme Corp");

        // Assert — cannot extract API config, returns empty
        result.Should().Be(SearchResult.Empty);
    }

    // Empty / error paths

    [Fact]
    public async Task SearchAsync_NoMatches_ReturnsEmptyResult()
    {
        // Arrange
        var sut = CreateSource(
            WorldBankJsonMother.ApiResponse(
                ("Acme Corp", "", "", "GB", "2020-01-15", "2025-01-15", "Fraud")));

        // Act
        var result = await sut.SearchAsync("unknown entity xyz");

        // Assert
        result.Hits.Should().Be(0);
        result.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_EmptyApiResponse_ReturnsEmptyResult()
    {
        // Arrange
        var sut = CreateSource(WorldBankJsonMother.EmptyApiResponse());

        // Act
        var result = await sut.SearchAsync("Acme Corp");

        // Assert
        result.Hits.Should().Be(0);
        result.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_HttpError_ReturnsEmptyResult()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            initialResponse: new HttpResponseMessage(HttpStatusCode.InternalServerError),
            resultsResponse: new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://projects.worldbank.org/")
        };

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("WorldBank").Returns(httpClient);
        var sut = new WorldBankScrapingSource(factory, _logger);

        // Act
        var result = await sut.SearchAsync("Acme Corp");

        // Assert
        result.Should().Be(SearchResult.Empty);
    }

    [Fact]
    public async Task SearchAsync_InvalidJson_ReturnsEmptyResult()
    {
        // Arrange — valid HTML page but invalid JSON from API
        var sut = CreateSource(
            WorldBankJsonMother.DebarredFirmsPage(),
            "{ invalid json <<<");

        // Act
        var result = await sut.SearchAsync("Acme Corp");

        // Assert
        result.Hits.Should().Be(0);
        result.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_Timeout_ReturnsEmptyResult()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(
            initialResponse: new HttpResponseMessage(HttpStatusCode.OK),
            resultsResponse: new HttpResponseMessage(HttpStatusCode.OK),
            throwOnSend: true);

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://projects.worldbank.org/")
        };

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("WorldBank").Returns(httpClient);
        var sut = new WorldBankScrapingSource(factory, _logger);

        // Act
        var result = await sut.SearchAsync("Acme Corp");

        // Assert
        result.Should().Be(SearchResult.Empty);
    }

    [Fact]
    public void SourceName_ReturnsWorldBank()
    {
        // Arrange
        var sut = CreateSource(WorldBankJsonMother.EmptyApiResponse());

        // Assert
        sut.SourceName.Should().Be("WORLD_BANK");
    }
}
