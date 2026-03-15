using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RiskScreening.API.Shared.Domain.Model.Aggregates;
using RiskScreening.API.Shared.Infrastructure.Persistence.Extensions;

namespace RiskScreening.API.Shared.Infrastructure.Persistence;

/// <summary>
/// Central database context for the application.
/// Manages entity configurations, automatic audit timestamps, and audit actor stamping.
/// </summary>
public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly TimeZoneInfo _timeZone;

    /// <summary>
    /// Initializes the context with the provided options and an optional HTTP context accessor.
    /// The accessor is used to resolve the current authenticated user for <see cref="IAuditableEntity.CreatedBy"/>
    /// and <see cref="IAuditableEntity.UpdatedBy"/>. It is optional, so the context still works in migrations,
    /// seeders, and background jobs where no HTTP context is present.
    /// </summary>
    /// <param name="options">EF Core options.</param>
    /// <param name="configuration">App configuration — reads <c>App:TimeZone</c> (IANA id, e.g. "America/Lima"). Defaults to UTC.</param>
    /// <param name="httpContextAccessor">Optional accessor for JWT claims.</param>
    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IConfiguration? configuration = null,
        IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;

        var tzId = configuration?["App:TimeZone"];
        _timeZone = string.IsNullOrWhiteSpace(tzId)
            ? TimeZoneInfo.Utc
            : TimeZoneInfo.FindSystemTimeZoneById(tzId);
    }

    /// <summary>
    /// Configures entity mappings by scanning all
    /// <see cref="IEntityTypeConfiguration{TEntity}"/> implementations in the assembly.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.UseSnakeCaseNamingConvention();
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Persists all pending changes to the database.
    /// <list type="bullet">
    ///   <item><see cref="IAuditableEntity.CreatedAt"/> and <see cref="IAuditableEntity.UpdatedAt"/> are set automatically from <see cref="DateTime.UtcNow"/>.</item>
    ///   <item><see cref="IAuditableEntity.CreatedBy"/> and <see cref="IAuditableEntity.UpdatedBy"/> are set from the authenticated user's <c>ClaimTypes.Name</c> claim when an HTTP context is available. <c>null</c> for system writes (seeders, background jobs).</item>
    /// </list>
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
        var actor = _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.CurrentValues[nameof(IAuditableEntity.CreatedAt)] = now;
                    entry.CurrentValues[nameof(IAuditableEntity.UpdatedAt)] = now;
                    if (actor is not null)
                    {
                        entry.CurrentValues[nameof(IAuditableEntity.CreatedBy)] = actor;
                        entry.CurrentValues[nameof(IAuditableEntity.UpdatedBy)] = actor;
                    }

                    break;
                case EntityState.Modified:
                    entry.CurrentValues[nameof(IAuditableEntity.UpdatedAt)] = now;
                    if (actor is not null)
                        entry.CurrentValues[nameof(IAuditableEntity.UpdatedBy)] = actor;
                    break;
            }

        return base.SaveChangesAsync(cancellationToken);
    }
}