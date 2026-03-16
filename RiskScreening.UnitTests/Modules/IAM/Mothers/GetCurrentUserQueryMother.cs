using Bogus;
using RiskScreening.API.Modules.IAM.Domain.Model.Queries;

namespace RiskScreening.UnitTests.Modules.IAM.Mothers;

/// <summary>
///     Object Mother for <see cref="GetCurrentUserQuery"/> — named business scenarios.
/// </summary>
public static class GetCurrentUserQueryMother
{
    private static readonly Faker Faker = new();

    /// <summary>A query for an existing authenticated user.</summary>
    public static GetCurrentUserQuery ForExistingUser(string email)
    {
        return new GetCurrentUserQuery(email);
    }

    /// <summary>A query with a random email that does not belong to any user.</summary>
    public static GetCurrentUserQuery ForNonExistentUser()
    {
        return new GetCurrentUserQuery(Faker.Internet.Email().ToLower());
    }
}