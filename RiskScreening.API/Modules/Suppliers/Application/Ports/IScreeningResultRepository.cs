using RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;

namespace RiskScreening.API.Modules.Suppliers.Application.Ports;

public interface IScreeningResultRepository
{
    Task AddAsync(ScreeningResult result);
    Task<ScreeningResult?> FindByIdAsync(string id, CancellationToken ct = default);
    IQueryable<ScreeningResult> Query();
}