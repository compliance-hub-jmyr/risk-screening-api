# ADR-0011: Estrategia de Campos de Auditoría — IAuditableEntity + AggregateRoot + AppDbContext

## Estado
`Accepted`

## Fecha
2026-03-13

## Contexto

Cualquier API de producción debe registrar *quién* creó o modificó un registro y *cuándo*. Sin una estrategia centralizada de auditoría, cada desarrollador agregaría `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` de forma ad-hoc a cada entidad — generando inconsistencias, campos olvidados y ningún punto de enforcement único.

La plataforma persiste dos aggregates principales (`Supplier`, `User`) y entidades hijo (`ScreeningResult`). Todos necesitan al menos auditoría de timestamps. `CreatedBy`/`UpdatedBy` son deseables pero están sujetos a la disponibilidad de la identidad del usuario autenticado en el momento de escritura.

Requisitos:
- `CreatedAt` y `UpdatedAt` deben poblarse **automáticamente** — sin asignación manual en cada handler.
- `CreatedBy` y `UpdatedBy` deben estar **disponibles en el modelo** para que los handlers puedan poblarlos opcionalmente desde claims JWT.
- El mecanismo debe ser **transparente** para el dominio — los aggregates y entidades no deben depender de EF Core directamente.
- Debe cubrir todos los aggregates y entidades sin requerir boilerplate por clase.

---

## Decisión

**Usar un enfoque de tres capas: interfaz `IAuditableEntity` → clases base `AggregateRoot` / `Entity<TId>` → override de `AppDbContext.SaveChangesAsync`.**

### Capa 1 — `IAuditableEntity` (interfaz, `Shared/Domain/Model/`)

Marca un objeto de dominio como auditable. Declara los cuatro campos de auditoría para que cualquier código que tenga una referencia `IAuditableEntity` pueda leer el historial de auditoría completo sin hacer cast al tipo concreto:

```csharp
public interface IAuditableEntity
{
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
    string?  CreatedBy { get; }
    string?  UpdatedBy { get; }
}
```

`CreatedAt`/`UpdatedAt` se pueblan automáticamente por `AppDbContext`.
`CreatedBy`/`UpdatedBy` son poblados por los handlers de aplicación que tienen acceso a claims JWT; las escrituras del sistema los dejan en `null`.

### Capa 2 — Clases base (`Shared/Domain/Model/`)

Tanto `AggregateRoot` como `Entity<TId>` implementan `IAuditableEntity`:

```
AggregateRoot : IAuditableEntity
  + string  Id          (UUID v4, auto-generado)
  + DateTime CreatedAt  (internal set — poblado por AppDbContext)
  + DateTime UpdatedAt  (internal set — poblado por AppDbContext)
  + string? CreatedBy   (internal set — poblado por handlers de aplicación)
  + string? UpdatedBy   (internal set — poblado por handlers de aplicación)
  + gestión de domain events (RaiseDomainEvent / PopDomainEvents)

Entity<TId> : IAuditableEntity
  + TId     Id
  + DateTime CreatedAt  (private set — poblado por AppDbContext)
  + DateTime UpdatedAt  (private set — poblado por AppDbContext)
  + string? CreatedBy   (protected set — poblado por handlers de aplicación)
  + string? UpdatedBy   (protected set — poblado por handlers de aplicación)
```

Todos los aggregates y entidades de dominio heredan estos campos con cero boilerplate. Un nuevo aggregate solo necesita extender `AggregateRoot`.

### Capa 3 — Override de `AppDbContext.SaveChangesAsync` (`Shared/Infrastructure/Persistence/`)

El contexto de EF Core recibe un `IHttpContextAccessor` opcional vía inyección de constructor (registrado en `AddSharedInfrastructure()`). En cada guardado itera el `ChangeTracker` y sella automáticamente los cuatro campos de auditoría:

```csharp
public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
    : base(options)
{
    _httpContextAccessor = httpContextAccessor;
}

public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var now   = DateTime.UtcNow;
    var actor = _httpContextAccessor?.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
    {
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
    }
    return base.SaveChangesAsync(cancellationToken);
}
```

`actor` se resuelve desde `ClaimTypes.Name` — el claim que `JwtTokenService` embebe como el username del usuario autenticado. `IHttpContextAccessor` es opcional para que el contexto funcione correctamente en seeders, migraciones y background jobs donde no hay una solicitud HTTP activa; esas escrituras dejan `CreatedBy`/`UpdatedBy` como `null`.

**Los handlers no necesitan asignar los campos de actor manualmente** — `AppDbContext` lo maneja de forma transparente para todos los módulos.

**Limitación conocida:** `CreatedBy`/`UpdatedBy` son `null` para escrituras iniciadas por el sistema (datos seed, background jobs). Esto es intencional y aceptable.

---

## Opciones Evaluadas

### Opción A — Clase base + override de `SaveChangesAsync` Seleccionada

| Ventajas | Desventajas |
|----------|-------------|
| Cero boilerplate por entidad — timestamps y actor sellados automáticamente | Requiere heredar de la clase base (acoplamiento menor) |
| Punto de enforcement único en `AppDbContext` | |
| Contrato de auditoría completo en `IAuditableEntity` — los cuatro campos accesibles sin cast | |
| Funciona para todas las entidades/aggregates automáticamente | |
| `CreatedBy`/`UpdatedBy` sellados automáticamente via `IHttpContextAccessor` — cero boilerplate en handlers | |

### Opción B — Solo shadow properties de EF Core

| Ventajas | Desventajas |
|----------|-------------|
| Sin propiedades C# en las clases de dominio | Los campos no son visibles en el código de dominio — no se pueden mapear a DTOs de respuesta sin EF |
| | Más difícil de testear sin un DbContext real |
| | `CreatedBy`/`UpdatedBy` requerirían `IHttpContextAccessor` en `AppDbContext` |

### Opción C — Asignación manual por entidad

| Ventajas | Desventajas |
|----------|-------------|
| Explícito por entidad | Duplicación masiva — cada handler debe recordar asignar los cuatro campos |
| | Inconsistente — los desarrolladores olvidan campos |
| | Sin punto de enforcement |

### Opción D — Interceptores (`ISaveChangesInterceptor`)

| Ventajas | Desventajas |
|----------|-------------|
| Pluggable — puede inyectar `IHttpContextAccessor` | Más complejo que el override de `SaveChangesAsync` para timestamps simples |
| Puede manejar `CreatedBy`/`UpdatedBy` automáticamente | Requiere registro de interceptor con DI |
| | Sobredimensionado para la escala actual |

---

## Consecuencias

### Positivas
- Cada nuevo aggregate y entidad obtiene `CreatedAt`/`UpdatedAt` gratis al extender `AggregateRoot` o `Entity<TId>`.
- `IAuditableEntity` mantiene el modelo de dominio desacoplado de EF Core.
- `AppDbContext` es la fuente autoritativa única para los cuatro campos de auditoría — sin asignaciones dispersas en handlers.
- `CreatedBy`/`UpdatedBy` se sellan automáticamente desde el claim JWT `ClaimTypes.Name`; los handlers no requieren ningún boilerplate.
- Las escrituras del sistema (seeders, background jobs) dejan `CreatedBy`/`UpdatedBy` como `null` de forma segura sin ningún manejo especial.

### Negativas / Mitigaciones

| Riesgo | Mitigación |
|--------|-----------|
| `CreatedBy`/`UpdatedBy` son `null` para escrituras del sistema | Documentado como intencional; las herramientas de soporte deben tratar `null` como "sistema" |
| `internal set` en los campos de auditoría de `AggregateRoot` significa que EF Core los asigna via reflexión | El `ChangeTracker` de EF Core usa acceso por diccionario `CurrentValues[]`, evitando modificadores de acceso — funciona correctamente |
| Todos los timestamps son UTC únicamente | Se usa `DateTime.UtcNow` de forma consistente; la capa UI es responsable de la conversión de zona horaria |

---

## Dependencias

Sin paquetes adicionales. `IAuditableEntity`, `AggregateRoot`, `Entity<TId>` y `AppDbContext` forman parte del kernel Shared en `RiskScreening.API/Shared/`. `IHttpContextAccessor` es proporcionado por `Microsoft.AspNetCore.Http` (ya parte del SDK de ASP.NET Core) y registrado via `builder.Services.AddHttpContextAccessor()` en `AddSharedInfrastructure()`.

---

## Decisiones Relacionadas

- **ADR-0001** — Arquitectura: `AggregateRoot` y `Entity<TId>` son parte del kernel de Dominio Shared, consumido por todos los módulos.
- **ADR-0003** — Persistencia: `AppDbContext` es el contexto central de EF Core; el override de `SaveChangesAsync` vive ahí.
