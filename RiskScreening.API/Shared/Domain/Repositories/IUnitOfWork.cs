namespace RiskScreening.API.Shared.Domain.Repositories;

/// <summary>
/// Represents the Unit of Work pattern for managing database transactions.
/// Call <see cref="CompleteAsync"/> after all repository operations to persist changes atomically.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    ///     Asynchronously persists all pending changes to the database.
    ///     Pass the request's <see cref="CancellationToken"/> so the operation
    ///     aborts if the client disconnects or the request times out.
    /// </summary>
    Task CompleteAsync(CancellationToken cancellationToken = default);
}
