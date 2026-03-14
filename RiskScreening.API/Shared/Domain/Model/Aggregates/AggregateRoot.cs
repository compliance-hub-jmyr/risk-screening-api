using System.Collections.ObjectModel;
using RiskScreening.API.Shared.Domain.Model.Events;

namespace RiskScreening.API.Shared.Domain.Model.Aggregates;

/// <summary>
///     Base class for Aggregate Roots in Domain-Driven Design.
///     The identifier type is <c>string</c> and is auto-generated as a UUID v4
///     on construction, so aggregates always have a valid ID from birth.
///     Manages identity, audit timestamps, and domain event publishing.
/// </summary>
public abstract class AggregateRoot : IAuditableEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    ///     Unique identifier of this aggregate. Auto-generated as a UUID v4 string
    ///     (e.g. <c>"550e8400-e29b-41d4-a716-446655440000"</c>).
    /// </summary>
    public string Id { get; protected set; } = Guid.NewGuid().ToString();

    /// <summary>Gets the UTC timestamp when this aggregate was created.</summary>
    public DateTime CreatedAt { get; internal set; }

    /// <summary>Gets the UTC timestamp when this aggregate was last modified.</summary>
    public DateTime UpdatedAt { get; internal set; }

    /// <summary>Gets the identifier of the user who created this aggregate.</summary>
    public string? CreatedBy { get; internal set; }

    /// <summary>Gets the identifier of the user who last modified this aggregate.</summary>
    public string? UpdatedBy { get; internal set; }

    /// <summary>
    ///     Registers a domain event to be dispatched after the aggregate is persisted.
    /// </summary>
    /// <param name="domainEvent">The domain event to register.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    ///     Returns all pending domain events and clears the internal collection.
    ///     Called by the infrastructure layer after successfully persisting the aggregate.
    /// </summary>
    /// <returns>A read-only list of pending domain events.</returns>
    public IReadOnlyList<IDomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return new ReadOnlyCollection<IDomainEvent>(events);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not AggregateRoot other) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}