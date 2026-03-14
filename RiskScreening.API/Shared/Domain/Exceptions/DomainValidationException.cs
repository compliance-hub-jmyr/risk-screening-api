namespace RiskScreening.API.Shared.Domain.Exceptions;

/// <summary>
///     Domain exception that carries field-level validation errors.
///     Use when domain logic detects multiple invalid fields at once
///     (e.g., a Value Object or aggregate that validates several properties together).
///     Maps to HTTP 400 Bad Request with <c>fieldErrors[]</c>.
/// </summary>
/// <example>
/// <code>
/// // In a Value Object or aggregate:
/// var errors = new List&lt;DomainFieldError&gt;();
///
/// if (string.IsNullOrWhiteSpace(name))
///     errors.Add (new ("name", "must not be blank", name));
///
/// if (documentNumber.Length != 11)
///     errors.Add (new("documentNumber", "must be 11 digits", documentNumber));
///
/// if (errors.Count > 0)
///     throw new DomainValidationException(errors);
/// </code>
/// </example>
public class DomainValidationException : DomainException
{
    public IReadOnlyList<DomainFieldError> FieldErrors { get; }

    public DomainValidationException(IReadOnlyList<DomainFieldError> fieldErrors)
        : base("Domain validation failed. Check field errors for details.",
            ErrorCodes.ValidationFailed,
            ErrorCodes.ValidationFailedCode)
    {
        FieldErrors = fieldErrors;
    }

    public DomainValidationException(string field, string message, object? rejectedValue = null)
        : this([new DomainFieldError(field, message, rejectedValue)])
    {
    }
}

/// <summary>Field-level error emitted by a <see cref="DomainValidationException"/>.</summary>
/// <param name="Field">Name of the invalid field (e.g. "documentNumber").</param>
/// <param name="Message">Why the value was rejected (e.g. "must be 11 digits").</param>
/// <param name="RejectedValue">The value that was rejected. Can be <c>null</c>.</param>
public record DomainFieldError(string Field, string Message, object? RejectedValue = null);