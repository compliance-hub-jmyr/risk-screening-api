using Microsoft.EntityFrameworkCore;
using RiskScreening.API.Modules.IAM.Application.Ports;
using RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;
using RiskScreening.API.Shared.Infrastructure.Persistence;
using RiskScreening.API.Shared.Infrastructure.Persistence.Repositories;

namespace RiskScreening.API.Modules.IAM.Infrastructure.Persistence.Repositories;

public class RoleRepository(AppDbContext context)
    : BaseRepository<Role, string>(context), IRoleRepository
{
    public async Task<Role?> FindByIdAsync(string id, CancellationToken ct = default)
    {
        return await Context.Set<Role>().FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<Role?> FindByNameAsync(string name, CancellationToken ct = default)
    {
        return await Context.Set<Role>()
            .FirstOrDefaultAsync(r => r.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase), ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
    {
        return await Context.Set<Role>()
            .AnyAsync(r => r.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase), ct);
    }

    public async Task<List<Role>> GetAllAsync(CancellationToken ct = default)
    {
        return await Context.Set<Role>()
            .OrderBy(r => r.Name)
            .ToListAsync(ct);
    }

    public new async Task AddAsync(Role role)
    {
        await Context.Set<Role>().AddAsync(role);
    }
}