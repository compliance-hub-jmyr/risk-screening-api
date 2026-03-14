using System.Net.Mail;
using RiskScreening.API.Shared.Domain.Exceptions;

namespace RiskScreening.API.Shared.Domain.Model.ValueObjects;

/// <summary>Valid, normalized email address.</summary>
public record Email : ValueObject
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidValueException("Email", value, "must not be blank",
                ErrorCodes.InvalidEmail, ErrorCodes.InvalidEmailCode);

        var normalized = value.Trim().ToLowerInvariant();

        if (!IsValid(normalized))
            throw new InvalidValueException("Email", value, "must be a valid email address",
                ErrorCodes.InvalidEmail, ErrorCodes.InvalidEmailCode); // 1101

        Value = normalized;
    }

    private static bool IsValid(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
