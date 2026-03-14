namespace RiskScreening.API.Shared.Infrastructure.Exceptions;

/// <summary>
///     Base exception for infrastructure-level technical errors.
///     These represent failures in external systems, databases, or deployment configuration —
///     not business rule violations.
///     Maps to HTTP 500 Internal Server Error or 502 Bad Gateway.
/// </summary>
public abstract class InfrastructureException : Exception
{
    public int    ErrorNumber { get; }
    public string ErrorCode   { get; }

    protected InfrastructureException(string message, int errorNumber, string errorCode)
        : base(message)
    {
        ErrorNumber = errorNumber;
        ErrorCode   = errorCode;
    }

    protected InfrastructureException(string message, int errorNumber, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorNumber = errorNumber;
        ErrorCode   = errorCode;
    }
}
