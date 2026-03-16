using RiskScreening.API.Modules.Suppliers.Application.Ports;
using RiskScreening.API.Modules.Suppliers.Infrastructure.Persistence.Repositories;

namespace RiskScreening.API.Modules.Suppliers.Infrastructure.Extensions;

public static class SuppliersModuleExtensions
{
    /// <summary>
    ///     Registers all Suppliers module services: repositories.
    /// </summary>
    public static void AddSuppliersModule(this WebApplicationBuilder builder)
    {
        // Repositories
        builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
        builder.Services.AddScoped<IScreeningResultRepository, ScreeningResultRepository>();
    }
}