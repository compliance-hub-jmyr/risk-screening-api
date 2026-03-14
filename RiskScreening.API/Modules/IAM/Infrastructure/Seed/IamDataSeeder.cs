using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RiskScreening.API.Modules.IAM.Application.Ports;
using RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;
using RiskScreening.API.Modules.IAM.Domain.Model.ValueObjects;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;
using RiskScreening.API.Shared.Infrastructure.Persistence;

namespace RiskScreening.API.Modules.IAM.Infrastructure.Seed;

/// <summary>
///     Seeds required IAM data on application startup.
///     Creates system roles and the default admin user if they don't exist.
/// </summary>
public class IamDataSeeder(AppDbContext context, IPasswordHasher passwordHasher, IOptions<IamSeedSettings> options)
{
    private const string AdminRole = "ADMIN";
    private const string AnalystRole = "ANALYST";
    private readonly IamSeedSettings _settings = options.Value;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedRolesAsync(ct);
        await SeedAdminUserAsync(ct);
    }

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        if (!await context.Set<Role>().AnyAsync(r => r.Name == AdminRole, ct))
        {
            var admin = Role.Create(AdminRole, "Full access — system role", true);
            await context.Set<Role>().AddAsync(admin, ct);
        }

        if (!await context.Set<Role>().AnyAsync(r => r.Name == AnalystRole, ct))
        {
            var analyst = Role.Create(AnalystRole, "Read, write and screening access — system role", true);
            await context.Set<Role>().AddAsync(analyst, ct);
        }

        await context.SaveChangesAsync(ct);
    }

    private async Task SeedAdminUserAsync(CancellationToken ct)
    {
        var emailVo = new Email(_settings.AdminEmail);

        if (await context.Set<User>().AnyAsync(u => u.Email == emailVo, ct))
            return;

        var adminRole = await context.Set<Role>()
            .FirstAsync(r => r.Name == AdminRole, ct);

        var user = User.Create(
            emailVo,
            new Username("admin"),
            Password.FromPlainText(_settings.AdminPassword, passwordHasher.Hash));

        user.AssignRole(adminRole);

        await context.Set<User>().AddAsync(user, ct);
        await context.SaveChangesAsync(ct);
    }
}