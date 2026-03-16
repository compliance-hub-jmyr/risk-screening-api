using Bogus;
using NSubstitute;
using RiskScreening.API.Modules.IAM.Application.Ports;
using RiskScreening.API.Modules.IAM.Domain.Model.Aggregates;
using RiskScreening.API.Modules.IAM.Domain.Model.ValueObjects;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;

namespace RiskScreening.UnitTests.Modules.IAM.Mothers;

/// <summary>
///     Fluent builder for creating <see cref="User"/> instances in tests.
///     Uses Bogus to generate random valid data — only specify values relevant to the test.
/// </summary>
public class UserBuilder
{
    private static readonly Faker Faker = new();

    private string _email = Faker.Internet.Email().ToLower();
    private string _username = Faker.Random.String2(Faker.Random.Int(5, 20), "abcdefghijklmnopqrstuvwxyz0123456789_");
    private string _passwordHash = Faker.Random.Hash();
    private bool _locked;
    private bool _suspended;

    public static UserBuilder AUser()
    {
        return new UserBuilder();
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public UserBuilder WithPasswordHash(string hash)
    {
        _passwordHash = hash;
        return this;
    }

    /// <summary>Simulates a user that has reached max failed login attempts.</summary>
    public UserBuilder Locked()
    {
        _locked = true;
        return this;
    }

    /// <summary>Simulates a user that has been suspended by an admin.</summary>
    public UserBuilder Suspended()
    {
        _suspended = true;
        return this;
    }

    public User Build()
    {
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Hash(Arg.Any<string>()).Returns(_passwordHash);

        var user = User.Create(
            new Email(_email),
            new Username(_username),
            Password.FromPlainText("PlainPassword123!", hasher.Hash));

        if (_locked)
            // Drive the domain into Locked state by exhausting failed attempts
            for (var i = 0; i < User.MaxFailedLoginAttempts; i++)
                user.RecordFailedLogin();

        if (_suspended)
            user.Suspend();

        return user;
    }
}