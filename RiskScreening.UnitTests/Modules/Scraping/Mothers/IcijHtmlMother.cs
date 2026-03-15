namespace RiskScreening.UnitTests.Modules.Scraping.Mothers;

/// <summary>
///     Named factory for ICIJ Offshore Leaks HTML fixtures.
///     Generates realistic HTML matching the ICIJ search results page structure.
/// </summary>
public static class IcijHtmlMother
{
    /// <summary>
    ///     Generates ICIJ search results HTML with the given entity tuples.
    ///     Each tuple: (Entity name, Jurisdiction, Linked To, Data From).
    /// </summary>
    public static string ResultsPage(
        params (string Entity, string Jurisdiction, string LinkedTo, string DataFrom)[] entries)
    {
        var rows = string.Join("\n", entries.Select(e => $"""
                <tr>
                    <td><a href="/nodes/10000001">{e.Entity}</a></td>
                    <td>{e.Jurisdiction}</td>
                    <td>{e.LinkedTo}</td>
                    <td>{e.DataFrom}</td>
                </tr>
            """));

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head><title>Search | ICIJ Offshore Leaks Database</title></head>
            <body>
                <div class="search-results">
                    <table class="table table-striped">
                        <thead>
                            <tr>
                                <th>Entity</th>
                                <th>Jurisdiction</th>
                                <th>Linked To</th>
                                <th>Data From</th>
                            </tr>
                        </thead>
                        <tbody>
            {{rows}}
                        </tbody>
                    </table>
                </div>
            </body>
            </html>
            """;
    }

    /// <summary>Returns an ICIJ search results page with no matches.</summary>
    public static string EmptyResultsPage() => """
        <!DOCTYPE html>
        <html lang="en">
        <head><title>Search | ICIJ Offshore Leaks Database</title></head>
        <body>
            <div class="search-results">
                <p>No matches for "unknown entity xyz"</p>
            </div>
        </body>
        </html>
        """;

    /// <summary>Returns an AWS WAF challenge page (simulates WAF blocking).</summary>
    public static string WafChallengePage() => """
        <!DOCTYPE html>
        <html lang="en">
        <head><title></title>
            <script type="text/javascript">
                window.awsWafCookieDomainList = [];
            </script>
            <script src="https://challenge.awswaf.com/challenge.js"></script>
        </head>
        <body>
            <div id="challenge-container"></div>
            <noscript>
                <h1>JavaScript is disabled</h1>
                In order to continue, we need to verify that you're not a robot.
            </noscript>
        </body>
        </html>
        """;
}
