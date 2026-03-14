using Microsoft.EntityFrameworkCore;
using RiskScreening.API.Shared.Infrastructure.Extensions;

namespace RiskScreening.API.Shared.Infrastructure.Persistence.Extensions;

/// <summary>
///     Provides extension methods for <see cref="ModelBuilder"/> to apply
///     database naming conventions across all entity types.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Applies snake_case naming convention to all tables, columns, keys,
    ///     foreign keys, and indexes in the model.
    /// </summary>
    /// <remarks>
    ///     This method intentionally does not pluralize table names. Entity configurations
    ///     (for example, <c>builder.ToTable("roles")</c>) remain the source of truth.
    /// </remarks>
    /// <param name="builder">The model builder instance to configure.</param>
    /// <example>
    ///     <code>
    ///         // AppDbContext.OnModelCreating:
    ///         modelBuilder.UseSnakeCaseNamingConvention();
    ///         modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    ///     </code>
    /// </example>
    public static void UseSnakeCaseNamingConvention(this ModelBuilder builder)
    {
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
                entity.SetTableName(tableName.ToSnakeCase());

            foreach (var property in entity.GetProperties())
                property.SetColumnName(property.GetColumnName().ToSnakeCase());

            foreach (var key in entity.GetKeys())
            {
                var keyName = key.GetName();
                if (!string.IsNullOrEmpty(keyName))
                    key.SetName(keyName.ToSnakeCase());
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var constraintName = foreignKey.GetConstraintName();
                if (!string.IsNullOrEmpty(constraintName))
                    foreignKey.SetConstraintName(constraintName.ToSnakeCase());
            }

            foreach (var index in entity.GetIndexes())
            {
                var indexName = index.GetDatabaseName();
                if (!string.IsNullOrEmpty(indexName))
                    index.SetDatabaseName(indexName.ToSnakeCase());
            }
        }
    }
}