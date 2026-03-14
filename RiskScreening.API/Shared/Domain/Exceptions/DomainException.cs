namespace RiskScreening.API.Shared.Domain.Exceptions;

/// <summary>
///     Base exception for all domain-level errors.
///     Domain exceptions represent business rule violations, invariants, or domain logic failures
///     and are part of the Ubiquitous Language.
/// </summary>
/// <remarks>
///     Error numbering scheme:
///     <list type="table">
///         <item><term>1000–1999</term><description>Validation &amp; Value Object errors</description></item>
///         <item><term>2000–2999</term><description>Authentication errors</description></item>
///         <item><term>3000–3999</term><description>Authorization errors</description></item>
///         <item><term>4000–4999</term><description>Entity didn't found errors</description></item>
///         <item><term>5000–5999</term><description>Business rule violations</description></item>
///     </list>
/// </remarks>
public abstract class DomainException : Exception
{
    /// <summary>Numeric code for programmatic handling (e.g. 4001).</summary>
    public int ErrorNumber { get; }

    /// <summary>Machine-readable text code (e.g. "USER_NOT_FOUND").</summary>
    public string ErrorCode { get; }

    protected DomainException(string message, int errorNumber, string errorCode)
        : base(message)
    {
        ErrorNumber = errorNumber;
        ErrorCode = errorCode;
    }

    protected DomainException(string message, int errorNumber, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorNumber = errorNumber;
        ErrorCode = errorCode;
    }
}
