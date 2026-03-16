using RiskScreening.API.Modules.Scraping.Application.Ports;
using RiskScreening.API.Modules.Scraping.Infrastructure.Sources;

namespace RiskScreening.API.Modules.Scraping.Infrastructure.Extensions;

/// <summary>
///     Registers Scraping module infrastructure:
///     <list type="bullet">
///         <item>Typed <c>HttpClient</c> instances (OFAC, World Bank) via <c>IHttpClientFactory</c></item>
///         <item><c>IMemoryCache</c> for on-demand scraping result caching</item>
///         <item>Scraping source adapters (<see cref="IScrapingSource"/> implementations)</item>
///     </list>
/// </summary>
/// <remarks>
///     The <see cref="Application.Search.SearchRiskListsQueryHandler"/> is auto-discovered
///     by MediatR assembly scanning — no explicit registration needed.
/// </remarks>
public static class ScrapingModuleExtensions
{
    private const string UserAgent = "RiskScreeningPlatform/1.0 (+compliance-screening)";

    /// <summary>
    ///     Registers Scraping module services into the DI container.
    /// </summary>
    public static void AddScrapingModule(this WebApplicationBuilder builder)
    {
        // Typed HTTP Clients

        builder.Services.AddHttpClient("Ofac", client =>
        {
            client.BaseAddress = new Uri("https://sanctionssearch.ofac.treas.gov/");
            client.Timeout = TimeSpan.FromSeconds(45);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            client.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        });

        builder.Services.AddHttpClient("WorldBank", client =>
        {
            client.BaseAddress = new Uri("https://projects.worldbank.org/");
            client.Timeout = TimeSpan.FromSeconds(45);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            client.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        });

        // ICIJ uses Playwright (headless Chromium) instead of HttpClient
        // because the search page is a JavaScript SPA protected by CloudFront.

        // Caching

        builder.Services.AddMemoryCache();

        // Scraping sources

        builder.Services.AddScoped<IScrapingSource, OfacScrapingSource>();
        builder.Services.AddScoped<IScrapingSource, WorldBankScrapingSource>();
        builder.Services.AddScoped<IScrapingSource, IcijScrapingSource>();
    }
}