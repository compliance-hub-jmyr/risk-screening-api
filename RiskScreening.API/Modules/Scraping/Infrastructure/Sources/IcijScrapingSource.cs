using Microsoft.Playwright;
using RiskScreening.API.Modules.Scraping.Application.Ports;
using RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Scraping.Infrastructure.Sources;

/// <summary>
///     <see cref="IScrapingSource"/> adapter for the
///     <a href="https://offshoreleaks.icij.org/">ICIJ Offshore Leaks Database</a>.
///     <para>
///         The ICIJ website is a JavaScript SPA protected by CloudFront.
///         This adapter uses <b>Playwright</b> (headless Chromium) to render the page
///         and extract the search results table with all four columns:
///         Entity, Jurisdiction, Linked To, Data From.
///     </para>
///     <para>Returns <see cref="SearchResult.Empty"/> on any failure (fault-tolerant).</para>
/// </summary>
public sealed class IcijScrapingSource(
    ILogger<IcijScrapingSource> logger) : IScrapingSource
{
    private const string BaseUrl = "https://offshoreleaks.icij.org";

    /// <inheritdoc />
    public string SourceName => "ICIJ";

    /// <inheritdoc />
    public async Task<SearchResult> SearchAsync(string term, CancellationToken ct = default)
    {
        try
        {
            var url = $"{BaseUrl}/search?q={Uri.EscapeDataString(term)}&c=&j=&d=";

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Channel = "chromium",
                Args =
                [
                    "--disable-blink-features=AutomationControlled",
                    "--no-sandbox",
                    "--disable-gpu",
                    "--disable-dev-shm-usage",
                    "--disable-setuid-sandbox"
                ]
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                Locale = "en-US",
                TimezoneId = "America/New_York"
            });

            var page = await context.NewPageAsync();

            // Remove automation flags to bypass CloudFront bot detection
            await page.AddInitScriptAsync(@"
                Object.defineProperty(navigator, 'webdriver', { get: () => false });
                Object.defineProperty(navigator, 'plugins',   { get: () => [1, 2, 3] });
                Object.defineProperty(navigator, 'languages', { get: () => ['en-US', 'en'] });
                window.chrome = { runtime: {} };
            ");

            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30_000
            });

            // Wait for the results table to appear in the DOM
            await page.WaitForSelectorAsync("table.table tbody tr", new PageWaitForSelectorOptions
            {
                Timeout = 10_000
            });

            var html = await page.ContentAsync();
            var entries = IcijHtmlParser.ParseResults(html, logger);

            logger.LogDebug(
                "ICIJ search completed — Term={Term}, Hits={Hits}",
                term, entries.Count);

            return new SearchResult(entries.Count, entries);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "ICIJ search failed — Term={Term}, returning empty result",
                term);

            return SearchResult.Empty;
        }
    }
}