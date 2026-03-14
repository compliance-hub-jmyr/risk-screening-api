using Microsoft.EntityFrameworkCore;
using RiskScreening.API.Shared.Domain.Repositories;

namespace RiskScreening.API.Shared.Infrastructure.Persistence.Repositories;

/// <summary>
///     Base EF Core repository providing CRUD operations for any entity.
///     Concrete repositories should extend this class and expose domain-specific query methods.
/// </summary>
/// <typeparam name="TEntity">The entity type managed by this repository.</typeparam>
/// <typeparam name="TId">The type of the entity's primary key identifier.</typeparam>
public abstract class BaseRepository<TEntity, TId>(AppDbContext context)
    : IBaseRepository<TEntity, TId> where TEntity : class
{
    /// <summary>
    ///     The database context used for all data operations.
    ///     Available to derived repositories for custom queries.
    /// </summary>
    protected readonly AppDbContext Context = context;

    /// <summary>
    ///     Asynchronously adds a new entity to the database set.
    ///     Changes are not persisted until <c>IUnitOfWork.CompleteAsync</c> is called.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    public async Task AddAsync(TEntity entity)
    {
        await Context.Set<TEntity>().AddAsync(entity);
    }

    /// <summary>
    ///     Asynchronously finds an entity by its primary key identifier.
    ///     Returns <c>null</c> if no entity with the given identifier exists.
    /// </summary>
    /// <param name="id">The primary key identifier of the entity.</param>
    /// <returns>The entity if found; otherwise, <c>null</c>.</returns>
    public async Task<TEntity?> FindByIdAsync(TId id)
    {
        return await Context.Set<TEntity>().FindAsync(id);
    }

    /// <summary>
    ///     Marks an existing entity as modified in the change tracker.
    ///     Changes are not persisted until <c>IUnitOfWork.CompleteAsync</c> is called.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    public void Update(TEntity entity)
    {
        Context.Set<TEntity>().Update(entity);
    }

    /// <summary>
    ///     Marks an entity for deletion from the database.
    ///     Changes are not persisted until <c>IUnitOfWork.CompleteAsync</c> is called.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    public void Remove(TEntity entity)
    {
        Context.Set<TEntity>().Remove(entity);
    }

    /// <summary>
    ///     Asynchronously retrieves all entities of <typeparamref name="TEntity"/> from the database.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous operation,
    ///     containing an enumerable of all entities.
    /// </returns>
    public async Task<IEnumerable<TEntity>> ListAsync()
    {
        return await Context.Set<TEntity>().ToListAsync();
    }
}