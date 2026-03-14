using RiskScreening.API.Shared.Domain.Model.Entities;

namespace RiskScreening.API.Shared.Domain.Model.Aggregates;

/// <summary>
/// Marks a domain object as auditable.
/// Implemented by <see cref="AggregateRoot"/> and <see cref="Entity{TId}"/>.
/// Audit timestamps (<see cref="CreatedAt"/>, <see cref="UpdatedAt"/>) are set automatically
/// by the infrastructure layer (<c>AppDbContext.SaveChangesAsync</c>).
/// Actor fields (<see cref="CreatedBy"/>, <see cref="UpdatedBy"/>) are populated by application
/// handlers that have access to JWT claims; system writes leave them <c>null</c>.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>Gets the UTC timestamp when this object was created.</summary>
    DateTime CreatedAt { get; }

    /// <summary>Gets the UTC timestamp when this object was last modified.</summary>
    DateTime UpdatedAt { get; }

    /// <summary>
    /// Gets the identifier (e.g., username or user ID) of the actor who created this object.
    /// <c>null</c> for system-initiated writes (seeders, background jobs).
    /// </summary>
    string? CreatedBy { get; }

    /// <summary>
    /// Gets the identifier of the actor who last modified this object.
    /// <c>null</c> for system-initiated writes.
    /// </summary>
    string? UpdatedBy { get; }
}