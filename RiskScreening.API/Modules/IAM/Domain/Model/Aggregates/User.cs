using RiskScreening.API.Modules.IAM.Domain.Model.ValueObjects;
using RiskScreening.API.Shared.Domain.Model.Aggregates;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;
using AccountStatus = RiskScreening.API.Modules.IAM.Domain.Model.ValueObjects.AccountStatus;

namespace RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;

/// <summary>
///     User aggregate root. Manages authentication state, account status,
///     login attempts, and role assignments.
/// </summary>
public class User : AggregateRoot
{
    public const int MaxFailedLoginAttempts = 5;

    public Email Email { get; private set; } = null!;
    public Username Username { get; private set; } = null!;
    public Password Password { get; private set; } = null!;

    public AccountStatus Status { get; private set; } = AccountStatus.Active;
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? LockedAt { get; private set; }

    private readonly List<Role> _roles = [];
    public IReadOnlyList<Role> Roles => _roles.AsReadOnly();

    private User() { }
}

