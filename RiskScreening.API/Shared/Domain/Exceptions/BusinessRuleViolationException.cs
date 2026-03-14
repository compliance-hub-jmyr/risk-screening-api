namespace RiskScreening.API.Shared.Domain.Exceptions;

/// <summary>
///     Base exception for domain business rule violations and invariant breaches.
///     Maps to HTTP 409 Conflict.
/// </summary>
/// <example>
/// <code>
/// public class DuplicateScreeningException: BusinessRuleViolationException
/// {
///     public DuplicateScreeningException(string entityName)
///         : base($"{entityName} already exists", ErrorCodes.DuplicateEntry, ErrorCodes.DuplicateEntryCode) { }
/// }
/// </code>
/// </example>
public abstract class BusinessRuleViolationException : DomainException
{
    protected BusinessRuleViolationException(string message, int errorNumber, string errorCode)
        : base(message, errorNumber, errorCode) { }
}
