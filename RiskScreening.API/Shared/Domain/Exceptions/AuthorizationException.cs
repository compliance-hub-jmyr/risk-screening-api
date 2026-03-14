namespace RiskScreening.API.Shared.Domain.Exceptions;

/// <summary>
///     Base exception for authorization failures at the domain level.
///     Maps to HTTP 403 Forbidden.
/// </summary>
/// <remarks>
///     Use this for domain-level access control violations such as
///     insufficient permissions to perform a business operation
///     or attempting to modify a system-owned resource.
/// </remarks>
public abstract class AuthorizationException : DomainException
{
    protected AuthorizationException(string message, int errorNumber, string errorCode)
        : base(message, errorNumber, errorCode)
    {
    }
}