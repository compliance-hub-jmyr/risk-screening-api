# ADR-0004: Scripts SQL Versionados con DbUp

## Estado
`Aceptado`

## Fecha
2026-03-13

## Contexto

El proyecto necesita una estrategia para versionar y ejecutar cambios en el esquema de base de datos de forma reproducible y automática.

Se evaluaron las siguientes opciones:

| Opción | Descripción | Ventajas | Desventajas |
|--------|-------------|----------|-------------|
| **EF Core Migrations** | `dotnet ef migrations add` genera clases C# de migración | Integrado con EF Core, no requiere SQL manual | Genera C# verboso, difícil de revisar, acoplado al ORM |
| **Scripts SQL versionados + DbUp** | Archivos `.sql` numerados ejecutados en orden por DbUp | SQL puro y legible, portable, fácil de revisar en PRs | Requiere el paquete NuGet `dbup-sqlserver` |
| **Flyway / Liquibase** | Herramientas externas de migración | Robustas con historial en BD | Dependencia de un proceso externo adicional |

## Decisión

Usar **scripts SQL versionados** ejecutados por **DbUp** (paquete NuGet `dbup-sqlserver`), siguiendo las convenciones de nomenclatura estilo Flyway.

### Convención de nomenclatura

```
Migrations/Scripts/
  V001__create_roles_table.sql
  V002__create_users_table.sql
  V003__create_user_roles_table.sql
  V005__create_suppliers_table.sql
  V006__create_screening_results_table.sql
```

Formato: `V{número:000}__{descripción_snake_case}.sql`

### Comportamiento del runner

DbUp se invoca al inicio de la API en `Program.cs`:

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

Comportamiento de DbUp:
1. Crea la tabla `schemaversions` automáticamente si no existe
2. Lee todos los scripts SQL embebidos en el assembly, ordenados por nombre (es decir, por número de versión)
3. Ejecuta únicamente los scripts cuyo nombre NO esté ya registrado en `schemaversions`
4. Registra cada script ejecutado en `schemaversions` con un timestamp

### Embebido como recurso

Los scripts se embeben en el assembly como `EmbeddedResource` en el `.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Migrations\Scripts\*.sql" />
</ItemGroup>
```

## Consecuencias

**Positivas:**
- Los scripts SQL son directamente legibles en PRs — los revisores ven el SQL exacto
- Reproducible: en cualquier entorno limpio, iniciar la API crea el esquema completo
- Sin dependencia del CLI `dotnet ef` en producción
- Los scripts son portables — pueden ejecutarse en cualquier herramienta SQL
- Docker Compose inicia y migra automáticamente sin pasos manuales
- DbUp gestiona la tabla `schemaversions` automáticamente — sin bootstrapping manual

**Negativas:**
- Sin rollback automático (los scripts son solo `up`, sin `down`)
- Si un script falla a mitad de ejecución, el esquema puede quedar en estado inconsistente (mitigado con `WithTransaction()`)

**Mitigación:**
- `WithTransaction()` envuelve cada script en una transacción — si falla, el script completo se revierte
- Para rollback manual: agregar un nuevo script `V00N__rollback_descripcion.sql` (procedimiento documentado)
- Los scripts están diseñados para ser idempotentes donde sea posible (guardas `IF NOT EXISTS`)

## Dependencias

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `dbup-sqlserver` | 7.2.0 | Runner de migraciones SQL Server — ejecuta scripts SQL embebidos versionados |

## Referencias
- [Documentación de DbUp](https://dbup.readthedocs.io/)
- [dbup-sqlserver en NuGet](https://www.nuget.org/packages/dbup-sqlserver)
- [Convención de nomenclatura de Flyway](https://www.red-gate.com/blog/database-devops/flyway-naming-patterns-matter/)
- [EF Core Migrations vs SQL Scripts](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
