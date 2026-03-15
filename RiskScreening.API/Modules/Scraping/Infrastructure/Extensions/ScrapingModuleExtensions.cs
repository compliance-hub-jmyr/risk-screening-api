using RiskScreening.API.Modules.Scraping.Infrastructure.Services;
using RiskScreening.API.Modules.Scraping.Infrastructure.Sources;

namespace RiskScreening.API.Modules.Scraping.Infrastructure.Extensions;

/// <summary>
///     Registers Scraping module infrastructure:
///     <list type="bullet">
///         <item>Three typed <c>HttpClient</c> instances (OFAC, World Bank, ICIJ) via <c>IHttpClientFactory</c></item>
///         <item><c>IMemoryCache</c> for on-demand scraping result caching</item>
///         <item>Scraping sources and orchestration service</item>
///     </list>
/// </summary>
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
            client.BaseAddress = new Uri("https://www.treasury.gov/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        });

        builder.Services.AddHttpClient("WorldBank", client =>
        {
            client.BaseAddress = new Uri("https://projects.worldbank.org/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        });

        builder.Services.AddHttpClient("Icij", client =>
        {
            client.BaseAddress = new Uri("https://offshoreleaks.icij.org/");
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        });

        // Caching

        builder.Services.AddMemoryCache();

        // Scraping sources

        builder.Services.AddScoped<IScrapingSource, OfacScrapingSource>();

        // Orchestration

        builder.Services.AddScoped<ScrapingOrchestrationService>();
    }
}
