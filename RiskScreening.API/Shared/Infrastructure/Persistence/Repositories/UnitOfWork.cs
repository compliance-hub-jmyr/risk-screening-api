using MediatR;
using RiskScreening.API.Shared.Domain.Model.Aggregates;
using RiskScreening.API.Shared.Domain.Repositories;
using RiskScreening.API.Shared.Infrastructure.Events;

namespace RiskScreening.API.Shared.Infrastructure.Persistence.Repositories;

/// <summary>
///     EF Core implementation of the Unit of Work pattern.
///     Coordinates persistence and domain event dispatching in a single operation.
/// </summary>
/// <remarks>
///     Domain events are collected from all tracked <see cref="AggregateRoot"/> instances
///     before saving, then dispatched via MediatR after successful persistence.
///     Only events implementing <see cref="IDomainEventNotification"/> are dispatched.
/// </remarks>
public class UnitOfWork(AppDbContext context, IPublisher publisher) : IUnitOfWork
{
    /// <summary>
    ///     Persists all pending changes to the database and dispatches collected domain events.
    ///     Domain events are always dispatched after a successful save to guarantee consistency.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        // Collect events before saving — PopDomainEvents clears the internal list
        var events = context.ChangeTracker
            .Entries<AggregateRoot>()
            .SelectMany(e => e.Entity.PopDomainEvents())
            .OfType<IDomainEventNotification>()
            .ToList();

        await context.SaveChangesAsync(cancellationToken);

        // Dispatch only after successful persistence to guarantee consistency
        foreach (var notification in events)
            await publisher.Publish(notification, cancellationToken);
    }
}