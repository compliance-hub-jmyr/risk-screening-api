# ADR-0004: Versioned SQL Migration Scripts with DbUp

## Status
`Accepted`

## Date
2026-03-13

## Context

The project needs a strategy to version and execute database schema changes in a reproducible and automatic way.

The following options were evaluated:

| Option | Description | Pros | Cons |
|--------|-------------|------|------|
| **EF Core Migrations** | `dotnet ef migrations add` generates C# migration classes | Integrated with EF Core, no manual SQL | Generates verbose C#, hard to review, tightly coupled to ORM |
| **Versioned SQL scripts + DbUp** | Numbered `.sql` files executed in order by DbUp | Pure and readable SQL, portable, easy to review in PRs | Requires the `dbup-sqlserver` NuGet package |
| **Flyway / Liquibase** | External migration tools | Robust with DB history tracking | Additional external process dependency |

## Decision

Use **versioned SQL scripts** executed by **DbUp** (`dbup-sqlserver` NuGet package), following Flyway-style naming conventions.

### Naming convention

```
Migrations/Scripts/
  V001__create_roles_table.sql
  V002__create_users_table.sql
  V003__create_user_roles_table.sql
  V005__create_suppliers_table.sql
  V006__create_screening_results_table.sql
```

Format: `V{number:000}__{snake_case_description}.sql`

### Runner behavior

DbUp is invoked at API startup in `Program.cs`:

```csharp
var upgradeResult = DeployChanges.To
    .SqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(
        Assembly.GetExecutingAssembly(),
        s => s.Contains("Migrations/Scripts"))
    .WithTransaction()
    .LogToConsole()
    .Build()
    .PerformUpgrade();

if (!upgradeResult.Successful)
    throw new Exception("Database migration failed", upgradeResult.Error);
```

DbUp behavior:
1. Creates the `schemaversions` table automatically if it does not exist
2. Reads all SQL scripts embedded in the assembly, ordered by name (i.e., version number)
3. Executes only the scripts whose name is NOT already registered in `schemaversions`
4. Records each executed script in `schemaversions` with a timestamp

### Embedded as a resource

Scripts are embedded in the assembly as `EmbeddedResource` in the `.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Migrations\Scripts\*.sql" />
</ItemGroup>
```

## Consequences

**Positive:**
- SQL scripts are directly readable in PRs — reviewers see the exact SQL
- Reproducible: in any clean environment, starting the API creates the full schema
- No dependency on `dotnet ef` CLI in production
- Scripts are portable — they can be run in any SQL tool
- Docker Compose starts and migrates automatically without manual steps
- DbUp handles the `schemaversions` table automatically — no manual bootstrapping

**Negative:**
- No automatic rollback (scripts are `up` only, no `down`)
- If a script fails mid-execution, the schema may be left in an inconsistent state (mitigated by `WithTransaction()`)

**Mitigation:**
- `WithTransaction()` wraps each script in a transaction — if it fails, the entire script is rolled back
- For manual rollback: add a new `V00N__rollback_description.sql` script (documented procedure)
- Scripts are designed to be idempotent where possible (`IF NOT EXISTS` guards)

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `dbup-sqlserver` | 7.2.0 | SQL Server migration runner — executes versioned embedded SQL scripts |

## References
- [DbUp Documentation](https://dbup.readthedocs.io/)
- [dbup-sqlserver on NuGet](https://www.nuget.org/packages/dbup-sqlserver)
- [Flyway Migration Naming Convention](https://www.red-gate.com/blog/database-devops/flyway-naming-patterns-matter/)
- [EF Core Migrations vs SQL Scripts](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
