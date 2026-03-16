using Microsoft.EntityFrameworkCore;
using RiskScreening.API.Modules.IAM.Application.Ports;
using RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;
using RiskScreening.API.Shared.Infrastructure.Persistence;
using RiskScreening.API.Shared.Infrastructure.Persistence.Repositories;

namespace RiskScreening.API.Modules.IAM.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext context)
    : BaseRepository<User, string>(context), IUserRepository
{
    public async Task<User?> FindByIdAsync(string id, CancellationToken ct = default)
    {
        return await Context.Set<User>()
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        return await Context.Set<User>()
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == new Email(email), ct);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        return await Context.Set<User>()
            .AnyAsync(u => u.Email == new Email(email), ct);
    }

    public IQueryable<User> Query()
    {
        return Context.Set<User>().Include(u => u.Roles);
    }

    public new async Task AddAsync(User user)
    {
        await Context.Set<User>().AddAsync(user);
    }

    public new void Update(User user)
    {
        Context.Set<User>().Update(user);
    }
}