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
    private readonly IScrapingSource _worldBankSource = Substitute.For<IScrapingSource>();
    private readonly IScrapingSource _icijSource = Substitute.For<IScrapingSource>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private readonly ILogger<SearchRiskListsQueryHandler> _logger =
        Substitute.For<ILogger<SearchRiskListsQueryHandler>>();

    private readonly SearchRiskListsQueryHandler _sut;

    public SearchRiskListsQueryHandlerTests()
    {
        _ofacSource.SourceName.Returns("OFAC");
        _worldBankSource.SourceName.Returns("WORLD_BANK");
        _icijSource.SourceName.Returns("ICIJ");
        _sut = new SearchRiskListsQueryHandler(
            [_ofacSource, _worldBankSource, _icijSource], _cache, _logger);
    }

    // Source selection 

    [Fact]
    public async Task Handle_NoSourceFilter_QueriesAllSources()
    {
        // Arrange
        var ofacResult = SearchResultMother.WithOfacEntries(3);
        var wbResult = SearchResultMother.WithWorldBankEntries(2);
        var icijResult = SearchResultMother.WithIcijEntries(1);
        _ofacSource.SearchAsync("term", Arg.Any<CancellationToken>()).Returns(ofacResult);
        _worldBankSource.SearchAsync("term", Arg.Any<CancellationToken>()).Returns(wbResult);
        _icijSource.SearchAsync("term", Arg.Any<CancellationToken>()).Returns(icijResult);

        var query = new SearchRiskListsQuery("term");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Hits.Should().Be(6);
        result.Entries.Should().HaveCount(6);
        await _ofacSource.Received(1).SearchAsync("term", Arg.Any<CancellationToken>());
        await _worldBankSource.Received(1).SearchAsync("term", Arg.Any<CancellationToken>());
        await _icijSource.Received(1).SearchAsync("term", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NullSourceNames_QueriesAllSources()
    {
        // Arrange
        _ofacSource.SearchAsync("term", Arg.Any<CancellationToken>()).Returns(SearchResult.Empty);
        _worldBankSource.SearchAsync("term", Arg.Any<CancellationToken>()).Returns(SearchResult.Empty);
        _icijSource.SearchAsync("term", Arg.Any<CancellationToken>()).Returns(SearchResult.Empty);

        var query = new SearchRiskListsQuery("term");

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _ofacSource.Received(1).SearchAsync("term", Arg.Any<CancellationToken>());
        await _worldBankSource.Received(1).SearchAsync("term", Arg.Any<CancellationToken>());
        await _icijSource.Received(1).SearchAsync("term", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptySourceNames_QueriesAllSources()
    {
        // Arrange
        _ofacSource.SearchAsync("term", Arg.Any<CancellationToken>()).Returns(SearchResult.Empty);
        _worldBankSource.SearchAsync("term", Arg.Any<CancellationToken>()).Returns(SearchResult.Empty);
        _icijSource.SearchAsync("term", Arg.Any<CancellationToken>()).Returns(SearchResult.Empty);

        var query = new SearchRiskListsQuery("term", []);

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _ofacSource.Received(1).SearchAsync("term", Arg.Any<CancellationToken>());
        await _worldBankSource.Received(1).SearchAsync("term", Arg.Any<CancellationToken>());
        await _icijSource.Received(1).SearchAsync("term", Arg.Any<CancellationToken>());
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
        await _worldBankSource.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _icijSource.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonMatchingSourceFilter_ReturnsEmpty()
    {
        // Arrange — "unknown" does not match any registered source
        var query = new SearchRiskListsQuery("term", ["unknown"]);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Hits.Should().Be(0);
        result.Entries.Should().BeEmpty();
        await _ofacSource.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _worldBankSource.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _icijSource.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithWorldBankSourceFilter_QueriesOnlyWorldBank()
    {
        // Arrange
        var expected = SearchResultMother.WithWorldBankEntries(2);
        _worldBankSource.SearchAsync("acme", Arg.Any<CancellationToken>()).Returns(expected);

        var query = new SearchRiskListsQuery("acme", ["world_bank"]);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Hits.Should().Be(2);
        result.Entries.Should().HaveCount(2);
        result.Entries.Should().AllSatisfy(e => e.ListSource.Should().Be("WORLD_BANK"));
        await _worldBankSource.Received(1).SearchAsync("acme", Arg.Any<CancellationToken>());
        await _ofacSource.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _icijSource.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithIcijSourceFilter_QueriesOnlyIcij()
    {
        // Arrange
        var expected = SearchResultMother.WithIcijEntries(2);
        _icijSource.SearchAsync("appleby", Arg.Any<CancellationToken>()).Returns(expected);

        var query = new SearchRiskListsQuery("appleby", ["icij"]);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Hits.Should().Be(2);
        result.Entries.Should().HaveCount(2);
        result.Entries.Should().AllSatisfy(e => e.ListSource.Should().Be("ICIJ"));
        await _icijSource.Received(1).SearchAsync("appleby", Arg.Any<CancellationToken>());
        await _ofacSource.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _worldBankSource.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
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
        await _worldBankSource.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _icijSource.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // Caching

    [Fact]
    public async Task Handle_CachesResult_SecondCallDoesNotHitSource()
    {
        // Arrange
        _ofacSource.SearchAsync("john", Arg.Any<CancellationToken>()).Returns(SearchResultMother.WithOfacEntries());
        _worldBankSource.SearchAsync("john", Arg.Any<CancellationToken>()).Returns(SearchResultMother.WithWorldBankEntries());
        _icijSource.SearchAsync("john", Arg.Any<CancellationToken>()).Returns(SearchResultMother.WithIcijEntries());

        var query = new SearchRiskListsQuery("john");

        // Act
        await _sut.Handle(query, CancellationToken.None);
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Hits.Should().Be(3);
        await _ofacSource.Received(1).SearchAsync("john", Arg.Any<CancellationToken>());
        await _worldBankSource.Received(1).SearchAsync("john", Arg.Any<CancellationToken>());
        await _icijSource.Received(1).SearchAsync("john", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DifferentTerms_CallsSourceForEach()
    {
        // Arrange
        _ofacSource.SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(SearchResult.Empty);
        _worldBankSource.SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(SearchResult.Empty);
        _icijSource.SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(SearchResult.Empty);

        // Act
        await _sut.Handle(new SearchRiskListsQuery("john"), CancellationToken.None);
        await _sut.Handle(new SearchRiskListsQuery("jane"), CancellationToken.None);

        // Assert
        await _ofacSource.Received(1).SearchAsync("john", Arg.Any<CancellationToken>());
        await _ofacSource.Received(1).SearchAsync("jane", Arg.Any<CancellationToken>());
        await _worldBankSource.Received(1).SearchAsync("john", Arg.Any<CancellationToken>());
        await _worldBankSource.Received(1).SearchAsync("jane", Arg.Any<CancellationToken>());
        await _icijSource.Received(1).SearchAsync("john", Arg.Any<CancellationToken>());
        await _icijSource.Received(1).SearchAsync("jane", Arg.Any<CancellationToken>());
    }

    // Result merging

    [Fact]
    public async Task Handle_MergesAllThreeSourceResults()
    {
        // Arrange — each source returns different entry counts
        _ofacSource.SearchAsync("term", Arg.Any<CancellationToken>())
            .Returns(SearchResultMother.WithOfacEntries());
        _worldBankSource.SearchAsync("term", Arg.Any<CancellationToken>())
            .Returns(SearchResultMother.WithWorldBankEntries(2));
        _icijSource.SearchAsync("term", Arg.Any<CancellationToken>())
            .Returns(SearchResultMother.WithIcijEntries(3));

        var query = new SearchRiskListsQuery("term");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Hits.Should().Be(6);
        result.Entries.Should().HaveCount(6);
        result.Entries.Select(e => e.ListSource).Distinct()
            .Should().BeEquivalentTo(["OFAC", "WORLD_BANK", "ICIJ"]);
    }
}
