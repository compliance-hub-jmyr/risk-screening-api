using Microsoft.EntityFrameworkCore;
using RiskScreening.API.Modules.Suppliers.Application.Ports;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Shared.Infrastructure.Persistence;
using RiskScreening.API.Shared.Infrastructure.Persistence.Repositories;

namespace RiskScreening.API.Modules.Suppliers.Infrastructure.Persistence.Repositories;

public class ScreeningResultRepository(AppDbContext context)
    : BaseRepository<ScreeningResult, string>(context), IScreeningResultRepository
{
    public new async Task AddAsync(ScreeningResult result)
    {
        await Context.Set<ScreeningResult>().AddAsync(result);
    }

    public async Task<ScreeningResult?> FindByIdAsync(string id, CancellationToken ct = default)
    {
        return await Context.Set<ScreeningResult>()
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public IQueryable<ScreeningResult> Query()
    {
        return Context.Set<ScreeningResult>();
    }
}