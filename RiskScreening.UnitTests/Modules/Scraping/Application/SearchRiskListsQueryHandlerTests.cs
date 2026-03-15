using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RiskScreening.API.Modules.Scraping.Application.Ports;
using RiskScreening.API.Modules.Scraping.Application.Search;
using RiskScreening.API.Modules.Scraping.Domain.Model.Queries;
using RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;
using RiskScreening.UnitTests.Modules.Scraping.Mothers;
using Xunit;

namespace RiskScreening.UnitTests.Modules.Scraping.Application;

/// <summary>
///     Unit tests for <see cref="SearchRiskListsQueryHandler"/>.
///     All scraping sources are substituted — no real HTTP is involved.
///     Uses <see cref="SearchResultMother"/> and <see cref="RiskEntryMother"/>
///     for test data construction.
/// </summary>
public class SearchRiskListsQueryHandlerTests
{
    private readonly IScrapingSource _ofacSource = Substitute.For<IScrapingSource>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private readonly ILogger<SearchRiskListsQueryHandler> _logger =
        Substitute.For<ILogger<SearchRiskListsQueryHandler>>();

    private readonly SearchRiskListsQueryHandler _sut;

    public SearchRiskListsQueryHandlerTests()
    {
        _ofacSource.SourceName.Returns("OFAC");
        _sut = new SearchRiskListsQueryHandler([_ofacSource], _cache, _logger);
    }

    // Source selection 

    [Fact]
    public async Task Handle_NoSourceFilter_QueriesAllSources()
    {
        // Arrange
        var expected = SearchResultMother.WithOfacEntries(3);
        _ofacSource.SearchAsync("term", Arg.Any<CancellationToken>()).Returns(expected);

        var query = new SearchRiskListsQuery("term");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Hits.Should().Be(3);
        result.Entries.Should().HaveCount(3);
        await _ofacSource.Received(1).SearchAsync("term", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NullSourceNames_QueriesAllSources()
    {
        // Arrange
        _ofacSource.SearchAsync("term", Arg.Any<CancellationToken>()).Returns(SearchResult.Empty);

        var query = new SearchRiskListsQuery("term");

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _ofacSource.Received(1).SearchAsync("term", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptySourceNames_QueriesAllSources()
    {
        // Arrange
        _ofacSource.SearchAsync("term", Arg.Any<CancellationToken>()).Returns(SearchResult.Empty);

        var query = new SearchRiskListsQuery("term", []);

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _ofacSource.Received(1).SearchAsync("term", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMatchingSourceFilter_QueriesOnlySelected()
    {
        // Arrange
        var expected = SearchResultMother.WithOfacEntries();
        _ofacSource.SearchAsync("term", Arg.Any<CancellationToken>()).Returns(expected);

        var query = new SearchRiskListsQuery("term", ["ofac"]);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Hits.Should().Be(1);
        await _ofacSource.Received(1).SearchAsync("term", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonMatchingSourceFilter_ReturnsEmpty()
    {
        // Arrange
        var query = new SearchRiskListsQuery("term", ["worldbank"]);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Hits.Should().Be(0);
        result.Entries.Should().BeEmpty();
        await _ofacSource.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CaseInsensitiveSourceName_MatchesCorrectly()
    {
        // Arrange
        _ofacSource.SearchAsync("john", Arg.Any<CancellationToken>()).Returns(SearchResult.Empty);

        var query = new SearchRiskListsQuery("john", ["OFAC"]);

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _ofacSource.Received(1).SearchAsync("john", Arg.Any<CancellationToken>());
    }

    // Caching

    [Fact]
    public async Task Handle_CachesResult_SecondCallDoesNotHitSource()
    {
        // Arrange
        var expected = SearchResultMother.WithOfacEntries();
        _ofacSource.SearchAsync("john", Arg.Any<CancellationToken>()).Returns(expected);

        var query = new SearchRiskListsQuery("john");

        // Act
        await _sut.Handle(query, CancellationToken.None);
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Hits.Should().Be(1);
        await _ofacSource.Received(1).SearchAsync("john", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DifferentTerms_CallsSourceForEach()
    {
        // Arrange
        _ofacSource.SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(SearchResult.Empty);

        // Act
        await _sut.Handle(new SearchRiskListsQuery("john"), CancellationToken.None);
        await _sut.Handle(new SearchRiskListsQuery("jane"), CancellationToken.None);

        // Assert
        await _ofacSource.Received(1).SearchAsync("john", Arg.Any<CancellationToken>());
        await _ofacSource.Received(1).SearchAsync("jane", Arg.Any<CancellationToken>());
    }

    // Result merging

    [Fact]
    public async Task Handle_MergesMultipleSourceResults()
    {
        // Arrange — two sources returning different entries
        var worldBankSource = Substitute.For<IScrapingSource>();
        worldBankSource.SourceName.Returns("WORLD_BANK");

        var ofacResult = SearchResultMother.WithOfacEntries();
        var wbResult = SearchResultMother.WithWorldBankEntries(2);

        _ofacSource.SearchAsync("term", Arg.Any<CancellationToken>()).Returns(ofacResult);
        worldBankSource.SearchAsync("term", Arg.Any<CancellationToken>()).Returns(wbResult);

        var sut = new SearchRiskListsQueryHandler(
            [_ofacSource, worldBankSource], _cache, _logger);

        var query = new SearchRiskListsQuery("term");

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Hits.Should().Be(3);
        result.Entries.Should().HaveCount(3);
    }
}
