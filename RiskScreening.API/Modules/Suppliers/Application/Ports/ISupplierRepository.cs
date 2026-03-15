using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;

namespace RiskScreening.API.Modules.Suppliers.Application.Ports;

public interface ISupplierRepository
{
    Task<Supplier?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<bool> ExistsByTaxIdAsync(string taxId, CancellationToken ct = default);
    IQueryable<Supplier> Query();
    Task AddAsync(Supplier supplier);
    void Update(Supplier supplier);
    void Remove(Supplier supplier);
}