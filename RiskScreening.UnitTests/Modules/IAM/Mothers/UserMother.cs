using RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;

namespace RiskScreening.UnitTests.Modules.IAM.Mothers;

/// <summary>
///     Object Mother for <see cref="User"/> — provides named business scenarios.
///     Returns a <see cref="UserBuilder"/> so the test can further customize if needed.
/// </summary>
public static class UserMother
{
    /// <summary>A standard active user with default credentials.</summary>
    public static UserBuilder Active()
    {
        return UserBuilder.AUser();
    }

    /// <summary>A user that has been locked due to too many failed login attempts.</summary>
    public static UserBuilder Locked()
    {
        return UserBuilder.AUser().Locked();
    }

    /// <summary>A user that has been manually suspended by an admin.</summary>
    public static UserBuilder Suspended()
    {
        return UserBuilder.AUser().Suspended();
    }

    /// <summary>An active user with a specific email — useful for sign-in tests.</summary>
    public static UserBuilder WithEmail(string email)
    {
        return UserBuilder.AUser().WithEmail(email);
    }
}