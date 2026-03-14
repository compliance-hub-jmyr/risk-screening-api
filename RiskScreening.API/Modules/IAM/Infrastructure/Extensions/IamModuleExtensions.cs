using RiskScreening.API.Modules.IAM.Application.Ports;
using RiskScreening.API.Modules.IAM.Infrastructure.Hashing;
using RiskScreening.API.Modules.IAM.Infrastructure.Persistence.Repositories;
using RiskScreening.API.Modules.IAM.Infrastructure.Security;
using RiskScreening.API.Modules.IAM.Infrastructure.Seed;
using RiskScreening.API.Modules.IAM.Infrastructure.Tokens;

namespace RiskScreening.API.Modules.IAM.Infrastructure.Extensions;

public static class IamModuleExtensions
{
    /// <summary>
    ///     Registers all IAM module services: repositories, hashing, JWT, and seeder.
    /// </summary>
    public static WebApplicationBuilder AddIamModule(this WebApplicationBuilder builder)
    {
        // Bind JWT settings from configuration
        builder.Services.Configure<JwtSettings>(
            builder.Configuration.GetSection("Jwt"));

        // Bind IAM seed settings from configuration
        builder.Services.Configure<IamSeedSettings>(
            builder.Configuration.GetSection("IamSeed"));

        // Repositories
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IRoleRepository, RoleRepository>();

        // Infrastructure services
        builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Seeder
        builder.Services.AddScoped<IamDataSeeder>();

        // JWT Bearer authentication — separated into its own file
        builder.Services.AddJwtBearerAuthentication(builder.Configuration);

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