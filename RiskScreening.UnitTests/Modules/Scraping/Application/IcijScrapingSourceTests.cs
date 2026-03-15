using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RiskScreening.API.Modules.Scraping.Infrastructure.Sources;
using RiskScreening.UnitTests.Modules.Scraping.Mothers;
using Xunit;

namespace RiskScreening.UnitTests.Modules.Scraping.Application;

/// <summary>
///     Unit tests for <see cref="IcijHtmlParser"/>.
///     <para>
///         Since <see cref="IcijScrapingSource"/> now uses Microsoft.Playwright (headless Chromium)
///         to render the ICIJ JavaScript SPA, the HTTP-level tests are no longer applicable.
///         These tests focus on the HTML parsing logic, which remains unchanged — the parser
///         receives rendered HTML from Playwright and extracts the search results table.
///     </para>
///     Uses <see cref="IcijHtmlMother"/> for HTML fixtures.
/// </summary>
public class IcijScrapingSourceTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    //  Success paths

    [Fact]
    public void ParseResults_MatchingEntities_ReturnsResults()
    {
        // Arrange
        var html = IcijHtmlMother.ResultsPage(
            ("Mossack Fonseca & Co.", "Panama", "Panama", "Panama Papers"));

        // Act
        var entries = IcijHtmlParser.ParseResults(html, _logger);

        // Assert
        entries.Should().HaveCount(1);

        var entry = entries[0];
        entry.ListSource.Should().Be("ICIJ");
        entry.Name.Should().Be("Mossack Fonseca & Co.");
        entry.Jurisdiction.Should().Be("Panama");
        entry.LinkedTo.Should().Be("Panama");
        entry.DataFrom.Should().Be("Panama Papers");
    }

    [Fact]
    public void ParseResults_MultipleEntities_ReturnsAll()
    {
        // Arrange
        var html = IcijHtmlMother.ResultsPage(
            ("Mossack Fonseca & Co.", "Panama", "Panama", "Panama Papers"),
            ("Mossack Fonseca Limited", "Nevada", "United States", "Panama Papers"),
            ("Mossack Fonseca (Singapore)", "Singapore", "Singapore", "Pandora Papers"));

        // Act
        var entries = IcijHtmlParser.ParseResults(html, _logger);

        // Assert
        entries.Should().HaveCount(3);
    }

    [Fact]
    public void ParseResults_EntityFieldMapping_MapsAllFields()
    {
        // Arrange
        var html = IcijHtmlMother.ResultsPage(
            ("Portcullis TrustNet", "Samoa", "Cook Islands", "Paradise Papers"));

        // Act
        var entries = IcijHtmlParser.ParseResults(html, _logger);

        // Assert
        var entry = entries[0];
        entry.Name.Should().Be("Portcullis TrustNet");
        entry.Jurisdiction.Should().Be("Samoa");
        entry.LinkedTo.Should().Be("Cook Islands");
        entry.DataFrom.Should().Be("Paradise Papers");
    }

    [Fact]
    public void ParseResults_IcijSpecificFields_OtherSourceFieldsAreNull()
    {
        // Arrange
        var html = IcijHtmlMother.ResultsPage(
            ("Mossack Fonseca", "Panama", "Panama", "Panama Papers"));

        // Act
        var entries = IcijHtmlParser.ParseResults(html, _logger);

        // Assert — OFAC fields
        var entry = entries[0];
        entry.Address.Should().BeNull();
        entry.Type.Should().BeNull();
        entry.List.Should().BeNull();
        entry.Programs.Should().BeNull();
        entry.Score.Should().BeNull();

        // Assert — World Bank fields
        entry.Country.Should().BeNull();
        entry.FromDate.Should().BeNull();
        entry.ToDate.Should().BeNull();
        entry.Grounds.Should().BeNull();
    }

    [Fact]
    public void ParseResults_HtmlEntities_DecodesCorrectly()
    {
        // Arrange
        var html = IcijHtmlMother.ResultsPage(
            ("Mossack Fonseca &amp; Co.", "Panama", "Panama", "Panama Papers"));

        // Act
        var entries = IcijHtmlParser.ParseResults(html, _logger);

        // Assert
        entries[0].Name.Should().Be("Mossack Fonseca & Co.");
    }

    [Fact]
    public void ParseResults_WhitespaceInFields_TrimsCorrectly()
    {
        // Arrange — inject extra whitespace
        var html = """
            <html><body>
            <table class="table"><tbody>
                <tr>
                    <td>  Mossack Fonseca  </td>
                    <td>  Panama  </td>
                    <td>  Panama  </td>
                    <td>  Panama Papers  </td>
                </tr>
            </tbody></table>
            </body></html>
            """;

        // Act
        var entries = IcijHtmlParser.ParseResults(html, _logger);

        // Assert
        var entry = entries[0];
        entry.Name.Should().Be("Mossack Fonseca");
        entry.Jurisdiction.Should().Be("Panama");
    }

    [Fact]
    public void ParseResults_EmptyFieldValues_MapsToNull()
    {
        // Arrange — entity with empty jurisdiction and linkedTo
        var html = """
            <html><body>
            <table class="table"><tbody>
                <tr>
                    <td>Unknown Entity</td>
                    <td></td>
                    <td></td>
                    <td>Panama Papers</td>
                </tr>
            </tbody></table>
            </body></html>
            """;

        // Act
        var entries = IcijHtmlParser.ParseResults(html, _logger);

        // Assert
        var entry = entries[0];
        entry.Name.Should().Be("Unknown Entity");
        entry.Jurisdiction.Should().BeNull();
        entry.LinkedTo.Should().BeNull();
        entry.DataFrom.Should().Be("Panama Papers");
    }

    // Empty / no results 

    [Fact]
    public void ParseResults_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        var html = IcijHtmlMother.EmptyResultsPage();

        // Act
        var entries = IcijHtmlParser.ParseResults(html, _logger);

        // Assert
        entries.Should().BeEmpty();
    }

    [Fact]
    public void ParseResults_EmptyTable_ReturnsEmptyList()
    {
        // Arrange — table exists but no rows
        var html = """
            <html><body>
            <table class="table"><thead><tr><th>Entity</th></tr></thead><tbody></tbody></table>
            </body></html>
            """;

        // Act
        var entries = IcijHtmlParser.ParseResults(html, _logger);

        // Assert
        entries.Should().BeEmpty();
    }

    // WAF / invalid HTML

    [Fact]
    public void ParseResults_WafChallengePage_ReturnsEmptyList()
    {
        // Arrange — AWS WAF challenge page (no results table)
        var html = IcijHtmlMother.WafChallengePage();

        // Act
        var entries = IcijHtmlParser.ParseResults(html, _logger);

        // Assert
        entries.Should().BeEmpty();
    }

    [Fact]
    public void ParseResults_InvalidHtml_ReturnsEmptyList()
    {
        // Arrange
        var html = "invalid html <<<<";

        // Act
        var entries = IcijHtmlParser.ParseResults(html, _logger);

        // Assert
        entries.Should().BeEmpty();
    }

    // Rendered SPA structure (as produced by Playwright)

    [Fact]
    public void ParseResults_PlaywrightRenderedHtml_ParsesCorrectly()
    {
        // Arrange — HTML structure matching what Playwright renders from the ICIJ SPA
        var html = """
            <html><body>
            <table class="table table-sm table-striped search__results__table">
                <thead class="search__results__table__head thead-light">
                    <tr>
                        <th class="text-nowrap">Entity</th>
                        <th class="jurisdiction text-nowrap">Jurisdiction</th>
                        <th class="country text-nowrap">Linked to</th>
                        <th class="source text-nowrap">Data from</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td><a href="/nodes/82030452" class="font-weight-bold text-dark">Appleby</a></td>
                        <td class="jurisdiction">Isle of Man</td>
                        <td class="country">Isle of Man</td>
                        <td class="source text-nowrap"><a title="Paradise Papers - Appleby" href="https://www.icij.org/investigations/paradise-papers">Paradise Papers</a></td>
                    </tr>
                    <tr>
                        <td><a href="/nodes/10018414" class="font-weight-bold text-dark">APPLEBY LTD.</a></td>
                        <td class="jurisdiction">Seychelles</td>
                        <td class="country">Monaco</td>
                        <td class="source text-nowrap"><a title="Panama Papers" href="https://www.icij.org/investigations/panama-papers">Panama Papers</a></td>
                    </tr>
                </tbody>
            </table>
            </body></html>
            """;

        // Act
        var entries = IcijHtmlParser.ParseResults(html, _logger);

        // Assert
        entries.Should().HaveCount(2);

        entries[0].Name.Should().Be("Appleby");
        entries[0].Jurisdiction.Should().Be("Isle of Man");
        entries[0].LinkedTo.Should().Be("Isle of Man");
        entries[0].DataFrom.Should().Be("Paradise Papers");

        entries[1].Name.Should().Be("APPLEBY LTD.");
        entries[1].Jurisdiction.Should().Be("Seychelles");
        entries[1].LinkedTo.Should().Be("Monaco");
        entries[1].DataFrom.Should().Be("Panama Papers");
    }

    [Fact]
    public void ParseResults_RowWithFewerThanFourCells_SkipsRow()
    {
        // Arrange — row with only 3 cells
        var html = """
            <html><body>
            <table class="table"><tbody>
                <tr>
                    <td>Valid Entity</td>
                    <td>Panama</td>
                    <td>Panama</td>
                    <td>Panama Papers</td>
                </tr>
                <tr>
                    <td>Incomplete</td>
                    <td>Panama</td>
                    <td>Panama</td>
                </tr>
            </tbody></table>
            </body></html>
            """;

        // Act
        var entries = IcijHtmlParser.ParseResults(html, _logger);

        // Assert — only the valid row is parsed
        entries.Should().HaveCount(1);
        entries[0].Name.Should().Be("Valid Entity");
    }
}
