using MediatR;
using RiskScreening.API.Shared.Domain.Model.Events;

namespace RiskScreening.API.Shared.Infrastructure.Events;

/// <summary>
/// Bridge interface that connects domain events with MediatR's notification pipeline.
/// Concrete domain events that need to be dispatched via MediatR should implement this interface.
/// </summary>
/// <example>
/// <code>
/// public record SupplierCreated(
///     string AggregateId,
///     DateTime OccurredAt,
///     string AggregateType,
///     string Name
/// ) : IDomainEventNotification;
/// </code>
/// </example>
public interface IDomainEventNotification : IDomainEvent, INotification;
