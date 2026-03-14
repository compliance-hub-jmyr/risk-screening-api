using RiskScreening.API.Modules.IAM.Application.Ports;
using RiskScreening.API.Modules.IAM.Infrastructure.Persistence.Repositories;
using RiskScreening.API.Modules.IAM.Infrastructure.Seed;

namespace RiskScreening.API.Modules.IAM.Infrastructure.Extensions;

public static class IamModuleExtensions
{
    /// <summary>
    ///     Registers all IAM module services: repositories, hashing, JWT, and seeder.
    /// </summary>
    public static WebApplicationBuilder AddIamModule(this WebApplicationBuilder builder)
    {

        // Bind IAM seed settings from configuration
        builder.Services.Configure<IamSeedSettings>(
            builder.Configuration.GetSection("IamSeed"));

        // Repositories
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IRoleRepository, RoleRepository>();

        // Seeder
        builder.Services.AddScoped<IamDataSeeder>();

        return builder;
    }

    /// <summary>
    ///     Runs IAM seed data — creates system roles and default admin user.
    /// </summary>
    public static async Task UseIamModuleAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IamDataSeeder>();
        await seeder.SeedAsync();
    }
}