using RiskScreening.API.Shared.Domain.Exceptions;

namespace RiskScreening.API.Shared.Infrastructure.Exceptions;

/// <summary>
///     Exception thrown when required seed data is missing from the database at startup.
///     This is an <b>infrastructure</b> exception — it represents a deployment or
///     configuration failure, not a business rule violation.
///     Maps to HTTP 500 Internal Server Error.
/// </summary>
/// <remarks>
///     Throw during application initialization or database seeding.
///     Do NOT throw during normal request processing.
/// </remarks>
/// <example>
/// <code>
/// var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
/// if (adminRole is null)
///     throw new RequiredSeedDataMissingException("Admin role", "cannot create super admin user");
/// </code>
/// </example>
public class RequiredSeedDataMissingException : InfrastructureException
{
    public string MissingData { get; }

    public RequiredSeedDataMissingException(string missingData, string context)
        : base(
            $"Required seed data '{missingData}' is missing: {context}",
            ErrorCodes.RequiredSeedDataMissing,
            ErrorCodes.RequiredSeedDataMissingCode)
    {
        MissingData = missingData;
    }
}
