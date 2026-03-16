using System.Text.RegularExpressions;
using RiskScreening.API.Shared.Domain.Exceptions;

namespace RiskScreening.API.Shared.Domain.Model.ValueObjects;

/// <summary>Phone number. Accepts digits, spaces, dashes, parentheses, and optional leading '+'.</summary>
public partial record PhoneNumber : ValueObject
{
    public string Value { get; }

    public PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidValueException("PhoneNumber", value, "must not be blank",
                ErrorCodes.InvalidPhoneNumber, ErrorCodes.InvalidPhoneNumberCode);

        var trimmed = value.Trim();

        if (!PhonePattern().IsMatch(trimmed))
            throw new InvalidValueException("PhoneNumber", value,
                "must be a valid phone number",
                ErrorCodes.InvalidPhoneNumber, ErrorCodes.InvalidPhoneNumberCode);

        Value = trimmed;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(PhoneNumber phone)
    {
        return phone.Value;
    }

    [GeneratedRegex(@"^\+?[\d\s\-().]{7,20}$")]
    private static partial Regex PhonePattern();
}