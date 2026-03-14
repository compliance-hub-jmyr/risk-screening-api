using Bogus;
using RiskScreening.API.Modules.IAM.Domain.Model.Commands;

namespace RiskScreening.UnitTests.Modules.IAM.Mothers;

/// <summary>
///     Object Mother for <see cref="SignInCommand"/> — named business scenarios with random valid data.
/// </summary>
public static class SignInCommandMother
{
    private static readonly Faker Faker = new();

    /// <summary>A valid sign-in command for the given user email.</summary>
    public static SignInCommand ValidFor(string email)
    {
        return new SignInCommand(email, ValidPassword());
    }

    /// <summary>A command for a user that does not exist in the system.</summary>
    public static SignInCommand WithNonExistentUser()
    {
        return new SignInCommand(Faker.Internet.Email().ToLower(), ValidPassword());
    }

    /// <summary>A command with a wrong password for the given email.</summary>
    public static SignInCommand WithWrongPassword(string email)
    {
        return new SignInCommand(email, ValidPassword());
    }

    /// <summary>
    ///     A syntactically valid password string accepted at the HTTP layer.
    ///     Meets the domain strength rule: ≥8 chars, 1 uppercase, 1 lowercase, 1 digit.
    /// </summary>
    public static string ValidPassword()
    {
        return $"Aa1{Faker.Random.String2(8)}";
    }
}