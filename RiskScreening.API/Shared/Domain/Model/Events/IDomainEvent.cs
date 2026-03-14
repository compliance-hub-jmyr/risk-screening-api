namespace RiskScreening.API.Shared.Domain.Model.Events;

/// <summary>
/// Marker interface for Domain Events.
/// Domain Events represent something significant that happened in the domain.
/// Implement this interface using immutable records named in past tense.
/// </summary>
/// <example>
/// <code>
/// public record SupplierCreated(
///     string AggregateId,
///     DateTime OccurredAt,
///     string AggregateType,
///     string Name
/// ) : IDomainEvent;
/// </code>
/// </example>
public interface IDomainEvent
{
    /// <summary>Gets the identifier of the aggregate that raised this event.</summary>
    string AggregateId { get; }

    /// <summary>Gets the UTC timestamp when this event occurred.</summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// Gets the type name of the aggregate that raised this event.
    /// Used for logging and event routing (e.g., "Supplier", "Screening").
    /// </summary>
    string AggregateType { get; }
}