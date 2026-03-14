using System.Text.RegularExpressions;
using RiskScreening.API.Shared.Domain.Exceptions;
using RiskScreening.API.Shared.Domain.Model.ValueObjects;

namespace RiskScreening.API.Modules.IAM.Domain.Model.ValueObjects;

/// <summary>3–50 characters. Letters, numbers, dots, hyphens, underscores.</summary>
public partial record Username : ValueObject
{
    [GeneratedRegex(@"^[a-zA-Z0-9._\-]{3,50}$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();

    private static readonly Regex ValidPattern = MyRegex();

    public string Value { get; }

    public Username(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidValueException("Username", value, "must not be blank",
                ErrorCodes.InvalidUsername, ErrorCodes.InvalidUsernameCode);

        if (!ValidPattern.IsMatch(value))
            throw new InvalidValueException("Username", value,
                "must be 3–50 characters and contain only letters, numbers, dots, hyphens or underscores",
                ErrorCodes.InvalidUsername, ErrorCodes.InvalidUsernameCode);

        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(Username username)
    {
        return username.Value;
    }
}