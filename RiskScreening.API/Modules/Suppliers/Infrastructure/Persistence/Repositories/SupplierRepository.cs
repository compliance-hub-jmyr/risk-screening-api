using Microsoft.EntityFrameworkCore;
using RiskScreening.API.Modules.Suppliers.Application.Ports;
using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;
using RiskScreening.API.Shared.Infrastructure.Persistence;
using RiskScreening.API.Shared.Infrastructure.Persistence.Repositories;

namespace RiskScreening.API.Modules.Suppliers.Infrastructure.Persistence.Repositories;

public class SupplierRepository(AppDbContext context)
    : BaseRepository<Supplier, string>(context), ISupplierRepository
{
    public async Task<Supplier?> FindByIdAsync(string id, CancellationToken ct = default)
    {
        return await Context.Set<Supplier>()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<bool> ExistsByTaxIdAsync(string taxId, CancellationToken ct = default)
    {
        return await Context.Set<Supplier>()
            .AnyAsync(s => s.TaxId == taxId, ct);
    }

    public IQueryable<Supplier> Query()
    {
        return Context.Set<Supplier>();
    }

    public new async Task AddAsync(Supplier supplier)
    {
        await Context.Set<Supplier>().AddAsync(supplier);
    }

    public new void Update(Supplier supplier)
    {
        Context.Set<Supplier>().Update(supplier);
    }

    public new void Remove(Supplier supplier)
    {
        Context.Set<Supplier>().Remove(supplier);
    }
}