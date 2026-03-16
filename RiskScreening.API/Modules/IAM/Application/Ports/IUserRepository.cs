using RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;

namespace RiskScreening.API.Modules.IAM.Application.Ports;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    IQueryable<User> Query();
    Task AddAsync(User user);
    void Update(User user);
}