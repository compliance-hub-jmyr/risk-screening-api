namespace RiskScreening.UnitTests.Modules.Scraping.Mothers;

/// <summary>
///     Named factory for World Bank test fixtures — both the HTML page
///     (with embedded JavaScript containing the API config) and the JSON
///     API response (<c>response.ZPROCSUPP</c> array).
/// </summary>
public static class WorldBankJsonMother
{
    /// <summary>
    ///     Generates a World Bank debarred firms HTML page with embedded
    ///     JavaScript containing the API URL and API key variables.
    ///     This is the page that <c>WorldBankHtmlParser.ExtractApiConfig</c> scrapes.
    /// </summary>
    public static string DebarredFirmsPage(
        string apiUrl = "https://apigwext.worldbank.org/dvsvc/v1.0/json/APPLICATION/ADOBE_EXPRNCE_MGR/FIRM/SANCTIONED_FIRM",
        string apiKey = "test-api-key-12345") =>
        $$"""
        <!DOCTYPE html>
        <html>
        <head><title>World Bank - Debarred Firms</title></head>
        <body>
        <div id="k-debarred-firms"></div>
        <script type="text/javascript">
            var defined = 'defined';
            var prodtabApi = "{{apiUrl}}";
            var propApiKey = "{{apiKey}}";
            var qaTabApi = "https://apigwqaext.worldbank.org/dvsvcqa/v1.0/json/APPLICATION/ADOBE_EXPRNCE_MGR/FIRM/SANCTIONED_FIRM";
            var qaApiKey = "TFpD0mDR9NqcmxNCU1zmvm2wGBfQgkxg";
        </script>
        </body>
        </html>
        """;


    /// <summary>Generates an API response with the specified firm entries.</summary>
    public static string ApiResponse(
        params (string Name, string Address, string City, string Country,
                string FromDate, string ToDate, string Grounds)[] firms)
    {
        var firmsJson = string.Join(",\n", firms.Select(f => $$"""
                {
                    "SUPP_NAME": "{{f.Name}}",
                    "ADD_SUPP_INFO": "",
                    "SUPP_ADDR": "{{f.Address}}",
                    "SUPP_CITY": "{{f.City}}",
                    "SUPP_STATE_CODE": "",
                    "SUPP_ZIP_CODE": "",
                    "COUNTRY_NAME": "{{f.Country}}",
                    "DEBAR_FROM_DATE": "{{f.FromDate}}",
                    "DEBAR_TO_DATE": "{{f.ToDate}}",
                    "DEBAR_REASON": "{{f.Grounds}}",
                    "INELIGIBLY_STATUS": ""
                }
            """));

        return $$"""
            {
                "response": {
                    "ZPROCSUPP": [
                        {{firmsJson}}
                    ]
                }
            }
            """;
    }

    /// <summary>Generates an API response with permanent debarment (no end date).</summary>
    public static string PermanentDebarmentResponse(
        string name, string country, string fromDate, string grounds)
    {
        return $$"""
            {
                "response": {
                    "ZPROCSUPP": [
                        {
                            "SUPP_NAME": "{{name}}",
                            "ADD_SUPP_INFO": "",
                            "SUPP_ADDR": "",
                            "SUPP_CITY": "",
                            "SUPP_STATE_CODE": "",
                            "SUPP_ZIP_CODE": "",
                            "COUNTRY_NAME": "{{country}}",
                            "DEBAR_FROM_DATE": "{{fromDate}}",
                            "DEBAR_TO_DATE": "",
                            "DEBAR_REASON": "{{grounds}}",
                            "INELIGIBLY_STATUS": "Permanent"
                        }
                    ]
                }
            }
            """;
    }

    /// <summary>
    ///     Generates an API response with an "Ongoing" debarment — the API stores
    ///     a sentinel date (<c>2999-12-31</c>) in <c>DEBAR_TO_DATE</c> but the
    ///     display should show <c>INELIGIBLY_STATUS</c> = "Ongoing".
    /// </summary>
    public static string OngoingDebarmentResponse(
        string name, string city, string country, string fromDate, string grounds)
    {
        return $$"""
            {
                "response": {
                    "ZPROCSUPP": [
                        {
                            "SUPP_NAME": "{{name}}",
                            "ADD_SUPP_INFO": "",
                            "SUPP_ADDR": "",
                            "SUPP_CITY": "{{city}}",
                            "SUPP_STATE_CODE": "",
                            "SUPP_ZIP_CODE": "",
                            "COUNTRY_NAME": "{{country}}",
                            "DEBAR_FROM_DATE": "{{fromDate}}",
                            "DEBAR_TO_DATE": "2999-12-31",
                            "DEBAR_REASON": "{{grounds}}",
                            "INELIGIBLY_STATUS": "Ongoing"
                        }
                    ]
                }
            }
            """;
    }

    /// <summary>Generates an API response with no firms.</summary>
    public static string EmptyApiResponse() => """
        {
            "response": {
                "ZPROCSUPP": []
            }
        }
        """;
}
