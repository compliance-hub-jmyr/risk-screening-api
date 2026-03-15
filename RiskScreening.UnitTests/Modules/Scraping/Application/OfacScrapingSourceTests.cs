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
///     Unit tests for <see cref="OfacScrapingSource"/>.
///     Uses <see cref="OfacHtmlMother"/> for HTML fixtures and
///     <see cref="FakeHttpMessageHandler"/> to simulate the GET → POST flow.
/// </summary>
public class OfacScrapingSourceTests
{
    private readonly ILogger<OfacScrapingSource> _logger =
        Substitute.For<ILogger<OfacScrapingSource>>();

    /// <summary>Creates a <see cref="OfacScrapingSource"/> backed by fake HTTP responses.</summary>
    private OfacScrapingSource CreateSource(string initialHtml, string resultsHtml)
    {
        var handler = new FakeHttpMessageHandler(initialHtml, resultsHtml);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://sanctionssearch.ofac.treas.gov/")
        };

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("Ofac").Returns(httpClient);

        return new OfacScrapingSource(factory, _logger);
    }

    // Success paths

    [Fact]
    public async Task SearchAsync_MatchingEntries_ReturnsResults()
    {
        // Arrange
        var sut = CreateSource(
            OfacHtmlMother.InitialPage(),
            OfacHtmlMother.ResultsPage(
                ("LAZARUS GROUP", "Potonggang District", "Entity", "DPRK3", "SDN", "100")));

        // Act
        var result = await sut.SearchAsync("Lazarus Group");

        // Assert
        result.Hits.Should().Be(1);
        result.Entries.Should().HaveCount(1);

        var entry = result.Entries[0];
        entry.ListSource.Should().Be("OFAC");
        entry.Name.Should().Be("LAZARUS GROUP");
        entry.Address.Should().Be("Potonggang District");
        entry.Type.Should().Be("Entity");
        entry.Programs.Should().Contain("DPRK3");
        entry.List.Should().Be("SDN");
        entry.Score.Should().Be(100);
    }

    [Fact]
    public async Task SearchAsync_CaseInsensitive_MatchesRegardlessOfCase()
    {
        // Arrange
        var sut = CreateSource(
            OfacHtmlMother.InitialPage(),
            OfacHtmlMother.ResultsPage(
                ("JOHN DOE", "123 Main St", "Individual", "SDGT", "SDN", "100")));

        // Act
        var result = await sut.SearchAsync("john doe");

        // Assert
        result.Hits.Should().Be(1);
        result.Entries[0].Name.Should().Be("JOHN DOE");
    }

    [Fact]
    public async Task SearchAsync_PartialMatch_ReturnsMatchingEntries()
    {
        // Arrange
        var sut = CreateSource(
            OfacHtmlMother.InitialPage(),
            OfacHtmlMother.ResultsPage(
                ("John Doe", "123 Main St", "Individual", "SDGT", "SDN", "95"),
                ("Johnny Doe-Smith", "456 Oak Ave", "Individual", "IRAN", "SDN", "85")));

        // Act
        var result = await sut.SearchAsync("Doe");

        // Assert
        result.Hits.Should().Be(2);
    }

    [Fact]
    public async Task SearchAsync_MultiplePrograms_ReturnsAllPrograms()
    {
        // Arrange
        var sut = CreateSource(
            OfacHtmlMother.InitialPage(),
            OfacHtmlMother.ResultsPage(
                ("John Doe", "123 Main St", "Individual", "SDGT; IRAN; CUBA", "SDN", "100")));

        // Act
        var result = await sut.SearchAsync("John Doe");

        // Assert
        result.Entries[0].Programs.Should().HaveCount(3);
        result.Entries[0].Programs.Should().Contain("SDGT");
        result.Entries[0].Programs.Should().Contain("IRAN");
        result.Entries[0].Programs.Should().Contain("CUBA");
    }

    [Fact]
    public async Task SearchAsync_EntryWithAddress_MapsAddressFields()
    {
        // Arrange
        var sut = CreateSource(
            OfacHtmlMother.InitialPage(),
            OfacHtmlMother.ResultsPage(
                ("John Doe", "123 Main St, Kabul, Afghanistan", "Individual", "SDGT", "SDN", "100")));

        // Act
        var result = await sut.SearchAsync("John Doe");

        // Assert
        result.Entries[0].Address.Should().Contain("123 Main St");
        result.Entries[0].Address.Should().Contain("Kabul");
        result.Entries[0].Address.Should().Contain("Afghanistan");
    }

    [Fact]
    public async Task SearchAsync_EntryWithoutAddress_AddressIsNull()
    {
        // Arrange
        var sut = CreateSource(
            OfacHtmlMother.InitialPage(),
            OfacHtmlMother.ResultsPage(
                ("John Doe", "", "Individual", "SDGT", "SDN", "100")));

        // Act
        var result = await sut.SearchAsync("John Doe");

        // Assert
        result.Entries[0].Address.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_OfacSpecificFields_OtherSourceFieldsAreNull()
    {
        // Arrange
        var sut = CreateSource(
            OfacHtmlMother.InitialPage(),
            OfacHtmlMother.ResultsPage(
                ("John Doe", "123 Main St", "Individual", "SDGT", "SDN", "100")));

        // Act
        var result = await sut.SearchAsync("John Doe");

        // Assert — World Bank fields
        result.Entries[0].Country.Should().BeNull();
        result.Entries[0].FromDate.Should().BeNull();
        result.Entries[0].ToDate.Should().BeNull();
        result.Entries[0].Grounds.Should().BeNull();

        // Assert — ICIJ fields
        result.Entries[0].Jurisdiction.Should().BeNull();
        result.Entries[0].LinkedTo.Should().BeNull();
        result.Entries[0].DataFrom.Should().BeNull();

        // Assert — Score IS set by HTML scraping
        result.Entries[0].Score.Should().Be(100);
    }

    [Fact]
    public async Task SearchAsync_ReversedWordOrder_MatchesRegardless()
    {
        // Arrange
        var sut = CreateSource(
            OfacHtmlMother.InitialPage(),
            OfacHtmlMother.ResultsPage(
                ("BOUT, Viktor", "Moscow", "Individual", "SDGT", "SDN", "100")));

        // Act — user searches "bout viktor" (different order)
        var result = await sut.SearchAsync("bout viktor");

        // Assert
        result.Hits.Should().Be(1);
        result.Entries[0].Name.Should().Be("BOUT, Viktor");
    }

    [Fact]
    public async Task SearchAsync_AliasMatch_ReturnsEntry()
    {
        // Arrange — OFAC returns the primary name when alias matches
        var sut = CreateSource(
            OfacHtmlMother.InitialPage(),
            OfacHtmlMother.ResultsPage(
                ("LAZARUS GROUP", "Potonggang District", "Entity", "DPRK3", "SDN", "100")));

        // Act — search by alias
        var result = await sut.SearchAsync("HIDDEN COBRA");

        // Assert
        result.Hits.Should().Be(1);
        result.Entries[0].Name.Should().Be("LAZARUS GROUP");
    }

    [Fact]
    public async Task SearchAsync_SingleSearchWord_MatchesPartially()
    {
        // Arrange
        var sut = CreateSource(
            OfacHtmlMother.InitialPage(),
            OfacHtmlMother.ResultsPage(
                ("BOUT, Viktor Anatolyevich", "Moscow", "Individual", "SDGT", "SDN", "95")));

        // Act
        var result = await sut.SearchAsync("bout");

        // Assert
        result.Hits.Should().Be(1);
    }

    [Fact]
    public async Task SearchAsync_ScoreFieldExtracted_ReturnsScore()
    {
        // Arrange
        var sut = CreateSource(
            OfacHtmlMother.InitialPage(),
            OfacHtmlMother.ResultsPage(
                ("LAZARUS GROUP", "Potonggang District", "Entity", "DPRK3", "SDN", "100")));

        // Act
        var result = await sut.SearchAsync("Lazarus Group");

        // Assert
        result.Entries[0].Score.Should().Be(100);
    }

    [Fact]
    public async Task SearchAsync_NoMatches_ReturnsEmptyResult()
    {
        // Arrange
        var sut = CreateSource(
            OfacHtmlMother.InitialPage(),
            OfacHtmlMother.EmptyResultsPage());

        // Act
        var result = await sut.SearchAsync("unknown entity xyz");

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
            BaseAddress = new Uri("https://sanctionssearch.ofac.treas.gov/")
        };

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("Ofac").Returns(httpClient);
        var sut = new OfacScrapingSource(factory, _logger);

        // Act
        var result = await sut.SearchAsync("John Doe");

        // Assert
        result.Should().Be(SearchResult.Empty);
    }

    [Fact]
    public async Task SearchAsync_InvalidHtml_ReturnsEmptyResult()
    {
        // Arrange
        var sut = CreateSource(OfacHtmlMother.InitialPage(), "invalid html <<<<");

        // Act
        var result = await sut.SearchAsync("John Doe");

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
            BaseAddress = new Uri("https://sanctionssearch.ofac.treas.gov/")
        };

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("Ofac").Returns(httpClient);
        var sut = new OfacScrapingSource(factory, _logger);

        // Act
        var result = await sut.SearchAsync("John Doe");

        // Assert
        result.Should().Be(SearchResult.Empty);
    }

    [Fact]
    public void SourceName_ReturnsOfac()
    {
        // Arrange
        var sut = CreateSource(OfacHtmlMother.InitialPage(), OfacHtmlMother.EmptyResultsPage());

        // Assert
        sut.SourceName.Should().Be("OFAC");
    }
}
