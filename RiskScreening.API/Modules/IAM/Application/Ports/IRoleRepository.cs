using RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;

namespace RiskScreening.API.Modules.IAM.Application.Ports;

public interface IRoleRepository
{
    Task<Role?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<Role?> FindByNameAsync(string name, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<List<Role>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Role role);
}