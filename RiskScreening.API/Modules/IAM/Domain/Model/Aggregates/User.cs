using RiskScreening.API.Modules.IAM.Domain.Exceptions;
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

    private User()
    {
    }

    public static User Create(Email email, Username username, Password password)
    {
        return new User
        {
            Email = email,
            Username = username,
            Password = password,
            Status = AccountStatus.Active
        };
    }

    public void RecordSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        LastLoginAt = DateTime.UtcNow;
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= MaxFailedLoginAttempts)
            Lock();
    }

    public void Activate()
    {
        if (Status == AccountStatus.Active) return;
        Status = AccountStatus.Active;
    }

    public void Suspend()
    {
        if (Status == AccountStatus.Suspended) return;
        Status = AccountStatus.Suspended;
    }

    /// <summary> Softly delete it — permanent, cannot be reactivated.</summary>
    public void Delete()
    {
        if (Status == AccountStatus.Deleted) return;
        Status = AccountStatus.Deleted;
    }

    public void Unlock()
    {
        if (Status != AccountStatus.Locked) return;
        Status = AccountStatus.Active;
        FailedLoginAttempts = 0;
        LockedAt = null;
    }

    private void Lock()
    {
        Status = AccountStatus.Locked;
        LockedAt = DateTime.UtcNow;
    }

    public void AssignRole(Role role)
    {
        if (_roles.Any(r => r.Id == role.Id)) return;
        _roles.Add(role);
    }

    public void RevokeRole(string roleId)
    {
        var role = _roles.FirstOrDefault(r => r.Id == roleId);
        if (role is null) return;
        _roles.Remove(role);
    }

    public bool HasRole(string roleName)
    {
        return _roles.Any(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsActive()
    {
        return Status == AccountStatus.Active;
    }

    public bool IsLocked()
    {
        return Status == AccountStatus.Locked;
    }

    public bool IsSuspended()
    {
        return Status == AccountStatus.Suspended;
    }

    public void EnsureCanLogin()
    {
        if (Status == AccountStatus.Locked) throw new AccountLockedException();
        if (Status == AccountStatus.Suspended) throw new AccountLockedException();
    }
}