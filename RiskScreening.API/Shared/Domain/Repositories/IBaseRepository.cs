namespace RiskScreening.API.Shared.Domain.Repositories;

/// <summary>
/// Defines a base repository interface for performing CRUD operations on entities.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public interface IBaseRepository<TEntity, TId>
{
    /// <summary>
    /// Asynchronously adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    Task AddAsync(TEntity entity);

    /// <summary>
    /// Asynchronously finds an entity by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the entity.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    Task<TEntity?> FindByIdAsync(TId id);

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    void Update(TEntity entity);

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    void Remove(TEntity entity);

    /// <summary>
    /// Asynchronously retrieves all entities.
    /// </summary>
    /// <returns>An enumerable of all entities.</returns>
    Task<IEnumerable<TEntity>> ListAsync();
}