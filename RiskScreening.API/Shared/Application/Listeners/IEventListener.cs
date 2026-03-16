using MediatR;
using RiskScreening.API.Shared.Infrastructure.Events;

namespace RiskScreening.API.Shared.Application.Listeners;

/// <summary>
///     Marker interface for domain event listeners in the application layer.
///     Wraps MediatR's <see cref="INotificationHandler{TNotification}"/> to semantically
///     distinguish event listeners from command/query handlers (<c>IRequestHandler</c>).
/// </summary>
/// <typeparam name="TEvent">
///     The domain event type, must implement <see cref="IDomainEventNotification"/>.
/// </typeparam>
/// <example>
/// <code>
/// public class RiskScreeningApprovedListener: IEventListener&lt;RiskScreeningApproved&gt;
/// {
///     public Task Handle(RiskScreeningApproved notification, CancellationToken ct)
///     {
///         // react to the event — send email, update read model, etc.
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface IEventListener<in TEvent> : INotificationHandler<TEvent>
    where TEvent : IDomainEventNotification;