using Microsoft.Playwright;
using RiskScreening.API.Modules.Scraping.Application.Ports;
using RiskScreening.API.Modules.Scraping.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Scraping.Infrastructure.Sources;

/// <summary>
///     <see cref="IScrapingSource"/> implementation for integration with the
///     <a href="https://offshoreleaks.icij.org/">ICIJ Offshore Leaks Database</a>.
/// </summary>
/// <remarks>
///     <para>
///         <strong>Technical Features:</strong>
///         <list type="bullet">
///             <item>Uses Playwright (headless Chromium) to render dynamic JavaScript content</item>
///             <item>Implements bypass strategies to evade CloudFront automation detection mechanisms</item>
///             <item>Extracts search results table with four columns: Entity, Jurisdiction, Linked To, Data From</item>
///             <item>Provides fault-tolerance: returns <see cref="SearchResult.Empty"/> on any exception</item>
///         </list>
///     </para>
///     <para>
///         <strong>Error Handling Behavior:</strong> All exceptions are caught, logged, 
///         and handled internally. The method always returns a valid result (never throws exceptions).
///     </para>
/// </remarks>
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

            // Injects an initialization script to mask automation indicators
            // This is necessary to evade CloudFront bot detection mechanisms
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

            // Waits for the search results table to appear in the DOM
            // Implements a fallback strategy to detect searches with no results
            try
            {
                // Attempts to wait for the table with result rows
                await page.WaitForSelectorAsync("table.table tbody tr", new PageWaitForSelectorOptions
                {
                    Timeout = 20_000
                });
            }
            catch (TimeoutException)
            {
                // If no rows are found, verifies whether the search completed but returned no results
                // Validates the presence of the table to confirm the page loaded correctly
                try
                {
                    await page.WaitForSelectorAsync("table.table", new PageWaitForSelectorOptions
                    {
                        Timeout = 5_000
                    });
                    
                    logger.LogDebug("ICIJ: Search completed with no results for term: {Term}", term);
                }
                catch (TimeoutException innerEx)
                {
                    // If even the table header does not appear, indicates a page load failure
                    logger.LogWarning("ICIJ: Failed to load search page for term: {Term}", term);
                    throw new TimeoutException($"ICIJ search page failed to load for term: {term}", innerEx);
                }
            }

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