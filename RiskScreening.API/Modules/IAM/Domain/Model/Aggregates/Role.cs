using RiskScreening.API.Shared.Domain.Model.Aggregates;

namespace RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;

/// <summary>
///     Role aggregate. System roles (e.g. ADMIN) cannot be deleted.
///     Custom roles can be created and assigned to users.
/// </summary>
public class Role : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsSystemRole { get; private set; }

    private Role() { }
}
