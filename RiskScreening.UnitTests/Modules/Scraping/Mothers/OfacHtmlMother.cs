using System.Text;

namespace RiskScreening.UnitTests.Modules.Scraping.Mothers;

/// <summary>
///     Named factory for OFAC HTML test pages (initial form + results).
/// </summary>
public static class OfacHtmlMother
{
    /// <summary>Generates an ASP.NET form page with ViewState hidden fields.</summary>
    public static string InitialPage() =>
        """
        <!DOCTYPE html>
        <html>
        <head><title>OFAC Search</title></head>
        <body>
        <form method="post" action="./"  id="aspnetForm">
        <input type="hidden" name="__VIEWSTATE" id="__VIEWSTATE" value="test-viewstate" />
        <input type="hidden" name="__VIEWSTATEGENERATOR" id="__VIEWSTATEGENERATOR" value="CA0B0334" />
        <input type="hidden" name="__EVENTVALIDATION" id="__EVENTVALIDATION" value="test-validation" />
        </form>
        </body>
        </html>
        """;

    /// <summary>Generates a results page with matching entries in the scrollResults table.</summary>
    public static string ResultsPage(
        params (string Name, string Address, string Type, string Programs, string List, string Score)[] entries)
    {
        var rows = new StringBuilder();
        foreach (var entry in entries)
        {
            rows.AppendLine($"""
                        <tr>
                            <td><a href="Details.aspx?id=123">{entry.Name}</a></td>
                            <td>{entry.Address}</td>
                            <td>{entry.Type}</td>
                            <td>{entry.Programs}</td>
                            <td>{entry.List}</td>
                            <td>{entry.Score}</td>
                        </tr>
            """);
        }

        return $"""
            <!DOCTYPE html>
            <html>
            <head><title>OFAC Results</title></head>
            <body>
            <form method="post" action="./"  id="aspnetForm">
            <input type="hidden" name="__VIEWSTATE" id="__VIEWSTATE" value="test-viewstate" />
            <div id="scrollResults" class="ResultsDiv">
                <div>
                    <table cellspacing="0" id="gvSearchResults">
                        {rows}
                    </table>
                </div>
            </div>
            </form>
            </body>
            </html>
            """;
    }

    /// <summary>Generates a results page with no matching entries.</summary>
    public static string EmptyResultsPage() =>
        """
        <!DOCTYPE html>
        <html>
        <head><title>OFAC Results</title></head>
        <body>
        <form method="post" action="./"  id="aspnetForm">
        <input type="hidden" name="__VIEWSTATE" id="__VIEWSTATE" value="test-viewstate" />
        <div id="scrollResults" class="ResultsDiv">
            <div>
                <table cellspacing="0" id="gvSearchResults">
                </table>
            </div>
        </div>
        </form>
        </body>
        </html>
        """;
}
