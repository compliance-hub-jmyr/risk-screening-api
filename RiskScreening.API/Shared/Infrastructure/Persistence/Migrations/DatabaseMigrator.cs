using System.Reflection;
using DbUp;
using DbUp.Engine;

namespace RiskScreening.API.Shared.Infrastructure.Persistence.Migrations;

/// <summary>
///     Runs pending SQL migration scripts against the database on application startup.
/// </summary>
/// <remarks>
///     Scripts are embedded as resources in the assembly under <c>Migrations/Scripts/</c>.
///     DbUp tracks executed scripts in a <c>SchemaVersions</c> table — only new scripts run.
///     Scripts execute in alphabetical order, so prefix with a number: <c>0001_</c>, <c>0002_</c>, etc.
/// </remarks>
public static class DatabaseMigrator
{
    /// <summary>
    ///     Ensures the database exists, then applies all pending migration scripts.
    ///     Throws if any migration fails — the application will not start.
    /// </summary>
    /// <param name="connectionString">SQL Server connection string.</param>
    /// <exception cref="InvalidOperationException">Thrown when a migration script fails.</exception>
    public static void Migrate(string connectionString)
    {
        // Create the database if it does not exist yet (useful for local dev / CI)
        EnsureDatabase.For.SqlDatabase(connectionString);

        var upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                // Only pick scripts inside the Migrations/Scripts folder
                scriptName => scriptName.Contains(".Migrations.Scripts."))
            .WithTransactionPerScript()   // each script runs in its own transaction
            .LogToConsole()
            .Build();

        if (!upgrader.IsUpgradeRequired()) return;
        var result = upgrader.PerformUpgrade();
        ThrowIfFailed(result);
    }

    private static void ThrowIfFailed(DatabaseUpgradeResult result)
    {
        if (result.Successful) return;

        throw new InvalidOperationException(
            $"Migration '{result.ErrorScript?.Name}' failed: {result.Error?.Message}",
            result.Error);
    }
}
