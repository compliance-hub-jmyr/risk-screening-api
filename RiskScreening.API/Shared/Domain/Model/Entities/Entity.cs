using RiskScreening.API.Shared.Domain.Model.Aggregates;

namespace RiskScreening.API.Shared.Domain.Model.Entities;

/// <summary>
/// Base class for Entities that belong to an Aggregate in Domain-Driven Design.
/// Entities have identity but are not aggregate roots —
/// they must be accessed through their parent aggregate.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class Entity<TId> : IAuditableEntity
{
    /// <summary>Gets the unique identifier of this entity.</summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>Gets the UTC timestamp when this entity was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Gets the UTC timestamp when this entity was last modified.</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Gets the identifier of the user who created this entity.</summary>
    public string? CreatedBy { get; protected set; }

    /// <summary>Gets the identifier of the user who last modified this entity.</summary>
    public string? UpdatedBy { get; protected set; }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id!.Equals(other.Id);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return GetType().GetHashCode();
    }
}