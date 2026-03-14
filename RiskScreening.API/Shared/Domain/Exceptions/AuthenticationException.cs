namespace RiskScreening.API.Shared.Domain.Exceptions;

/// <summary>
///     Base exception for authentication failures at the domain level.
///     Maps to HTTP 401 Unauthorized.
/// </summary>
/// <remarks>
///     Use this for business-level authentication violations such as
///     invalid credentials, locked accounts, pending verification.
///     JWT token validation is handled by ASP.NET authentication middleware — not here.
/// </remarks>
public abstract class AuthenticationException : DomainException
{
    protected AuthenticationException(string message, int errorNumber, string errorCode)
        : base(message, errorNumber, errorCode) { }
}
