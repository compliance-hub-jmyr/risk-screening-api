namespace RiskScreening.API.Shared.Domain.Exceptions;

/// <summary>
///     Exception thrown when a Value Object receives an invalid value.
///     Value Objects are self-validating and throw this exception when their
///     invariants are violated during construction.
///     Maps to HTTP 400 Bad Request.
/// </summary>
/// <example>
/// <code>
/// public record Email
/// {
///     public string Value { get; }
///     public Email (string value)
///     {
///         if (!IsValid(value))
///             throw new InvalidValueException("Email", value, "must be a valid email address");
///         Value = value;
///     }
/// }
/// </code>
/// </example>
public class InvalidValueException : DomainException
{
    public string ValueObjectName { get; }
    public string? InvalidValue { get; }
    public string Reason { get; }

    public InvalidValueException(string valueObjectName, string? invalidValue, string reason)
        : base(BuildMessage(valueObjectName, invalidValue, reason), ErrorCodes.InvalidValue,
            BuildErrorCode(valueObjectName))
    {
        ValueObjectName = valueObjectName;
        InvalidValue = invalidValue;
        Reason = reason;
    }

    public InvalidValueException(string valueObjectName, string reason)
        : this(valueObjectName, null, reason)
    {
    }

    /// <summary>Constructor with a custom error number for specific Value Object types.</summary>
    public InvalidValueException(string valueObjectName, string? invalidValue, string reason, int errorNumber,
        string errorCode)
        : base(BuildMessage(valueObjectName, invalidValue, reason), errorNumber, errorCode)
    {
        ValueObjectName = valueObjectName;
        InvalidValue = invalidValue;
        Reason = reason;
    }

    private static string BuildMessage(string name, string? value, string reason)
    {
        return string.IsNullOrWhiteSpace(value)
            ? $"Invalid {name}: {reason}"
            : $"Invalid {name} '{value}': {reason}";
    }

    // e.g. "EmailAddress" → "INVALID_EMAIL_ADDRESS"
    private static string BuildErrorCode(string valueObjectName)
    {
        return "INVALID_" + System.Text.RegularExpressions.Regex
            .Replace(valueObjectName, "([a-z])([A-Z])", "$1_$2")
            .ToUpperInvariant();
    }
}