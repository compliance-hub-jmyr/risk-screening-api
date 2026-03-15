using RiskScreening.API.Modules.Suppliers.Domain.Model.ValueObjects;
using RiskScreening.API.Shared.Domain.Model.Aggregates;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.Suppliers.Domain.Model.Aggregates;

/// <summary>
///     Immutable record of a single screening run for a supplier.
/// </summary>
public class ScreeningResult : AggregateRoot
{
    public SupplierId SupplierId { get; private set; } = null!;
    public string SourcesQueried { get; private set; } = null!;
    public DateTime ScreenedAt { get; private set; }
    public RiskLevel RiskLevel { get; private set; }
    public int TotalMatches { get; private set; }
    public string? EntriesJson { get; private set; }

    private ScreeningResult()
    {
    }
}