# ADR-0011: Audit Fields Strategy — IAuditableEntity + AggregateRoot + AppDbContext

## Status
`Accepted`

## Date
2026-03-13

## Context

Any production-grade API must record *who* created or modified a record and *when*. Without a centralized audit strategy, individual developers would add `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` ad-hoc to each entity — leading to inconsistencies, forgotten fields, and no single enforcement point.

The platform persists two main aggregates (`Supplier`, `User`) and child entities (`ScreeningResult`). All of them need at minimum timestamp auditing. `CreatedBy`/`UpdatedBy` are desirable but subject to availability of the authenticated user identity at write time.

Requirements:
- `CreatedAt` and `UpdatedAt` must be populated **automatically** — no manual assignment in every handler.
- `CreatedBy` and `UpdatedBy` must be **available on the model** so handlers can optionally populate them from JWT claims.
- The mechanism must be **transparent** to the domain — aggregates and entities must not depend on EF Core directly.
- Must cover all aggregates and entities without requiring per-class boilerplate.

---

## Decision

**Use a three-layer approach: `IAuditableEntity` interface → `AggregateRoot` / `Entity<TId>` base classes → `AppDbContext.SaveChangesAsync` override.**

### Layer 1 — `IAuditableEntity` (interface, `Shared/Domain/Model/`)

Marks a domain object as auditable. Declares all four audit fields so any code that holds an `IAuditableEntity` reference can read the full audit trail without casting to a concrete type:

```csharp
public interface IAuditableEntity
{
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
    string?  CreatedBy { get; }
    string?  UpdatedBy { get; }
}
```

`CreatedAt`/`UpdatedAt` are populated automatically by `AppDbContext`.
`CreatedBy`/`UpdatedBy` are populated by application handlers that have JWT claims context; system writes leave them `null`.

### Layer 2 — Base classes (`Shared/Domain/Model/`)

Both `AggregateRoot` and `Entity<TId>` implement `IAuditableEntity`:

```
AggregateRoot : IAuditableEntity
  + string  Id          (UUID v4, auto-generated)
  + DateTime CreatedAt  (internal set — populated by AppDbContext)
  + DateTime UpdatedAt  (internal set — populated by AppDbContext)
  + string? CreatedBy   (internal set — populated by application handlers)
  + string? UpdatedBy   (internal set — populated by application handlers)
  + domain event management (RaiseDomainEvent / PopDomainEvents)

Entity<TId> : IAuditableEntity
  + TId     Id
  + DateTime CreatedAt  (private set — populated by AppDbContext)
  + DateTime UpdatedAt  (private set — populated by AppDbContext)
  + string? CreatedBy   (protected set — populated by application handlers)
  + string? UpdatedBy   (protected set — populated by application handlers)
```

All domain aggregates and entities inherit these fields with zero boilerplate. A new aggregate only needs to extend `AggregateRoot`.

### Layer 3 — `AppDbContext.SaveChangesAsync` override (`Shared/Infrastructure/Persistence/`)

The EF Core context iterates `ChangeTracker` on every save and populates `CreatedAt`/`UpdatedAt` based on entity state:

```csharp
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var now = DateTime.UtcNow;
    foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.CurrentValues[nameof(IAuditableEntity.CreatedAt)] = now;
                entry.CurrentValues[nameof(IAuditableEntity.UpdatedAt)] = now;
                break;
            case EntityState.Modified:
                entry.CurrentValues[nameof(IAuditableEntity.UpdatedAt)] = now;
                break;
        }
    }
    return base.SaveChangesAsync(cancellationToken);
}
```

### `CreatedBy` / `UpdatedBy` — manual assignment from application layer

`AppDbContext` does **not** populate `CreatedBy`/`UpdatedBy` because:
1. The DB context has no access to `IHttpContextAccessor` or JWT claims by design — mixing infrastructure concerns.
2. Not all writes have an authenticated user (e.g., the IAM seeder runs at startup).

Instead, MediatR command handlers that have access to the current user identity (via `ICurrentUserService` or `ClaimsPrincipal` injected from `IHttpContextAccessor`) assign these fields explicitly on the aggregate before calling `SaveChangesAsync`.

**Known limitation:** `CreatedBy`/`UpdatedBy` are `null` for system-initiated writes (seed data, background jobs). This is intentional and acceptable.

---

## Evaluated Options

### Option A — Base class + `SaveChangesAsync` override Selected

| Pros | Cons |
|------|------|
| Zero boilerplate per entity | Requires inheriting from base class (minor coupling) |
| Single enforcement point in `AppDbContext` | `CreatedBy`/`UpdatedBy` not auto-populated by `AppDbContext` (by design — requires handler context) |
| Full audit contract on `IAuditableEntity` — all four fields readable without casting | |
| Works for all entities/aggregates automatically | |

### Option B — EF Core shadow properties only

| Pros | Cons |
|------|------|
| No C# properties on domain classes | Fields not visible in domain code — cannot be mapped to response DTOs without EF |
| | Harder to test without a real DbContext |
| | `CreatedBy`/`UpdatedBy` would require `IHttpContextAccessor` in `AppDbContext` |

### Option C — Per-entity manual assignment

| Pros | Cons |
|------|------|
| Explicit per entity | Massive duplication — every handler must remember to set all four fields |
| | Inconsistent — developers forget fields |
| | No enforcement point |

### Option D — Interceptors (`ISaveChangesInterceptor`)

| Pros | Cons |
|------|------|
| Pluggable — can inject `IHttpContextAccessor` | More complex than `SaveChangesAsync` override for simple timestamps |
| Can handle `CreatedBy`/`UpdatedBy` automatically | Requires DI-aware interceptor registration |
| | Over-engineered for the current scale |

---

## Consequences

### Positive
- Every new aggregate and entity gets `CreatedAt`/`UpdatedAt` for free by extending `AggregateRoot` or `Entity<TId>`.
- `IAuditableEntity` keeps the domain model decoupled from EF Core.
- `AppDbContext` is the single authoritative source for timestamp stamping — no scattered assignments across handlers.
- `CreatedBy`/`UpdatedBy` are present on all models and can be populated selectively by handlers with user context.

### Negative / Mitigations

| Risk | Mitigation |
|------|-----------|
| `CreatedBy`/`UpdatedBy` are `null` for system writes | Documented as intentional; support tooling should treat `null` as "system" |
| `internal set` on `AggregateRoot` audit fields means EF Core sets them via reflection | EF Core's `ChangeTracker` uses `CurrentValues[]` dictionary access, bypassing access modifiers — works correctly |
| All timestamps are UTC only | `DateTime.UtcNow` is used consistently; UI layer is responsible for timezone conversion |

---

## Dependencies

No additional packages. `IAuditableEntity`, `AggregateRoot`, `Entity<TId>`, and `AppDbContext` are all part of the Shared kernel in `RiskScreening.API/Shared/`.

---

## Related Decisions

- **ADR-0001** — Architecture: `AggregateRoot` and `Entity<TId>` are part of the Shared Domain kernel, consumed by all modules.
- **ADR-0003** — Persistence: `AppDbContext` is the central EF Core context; the `SaveChangesAsync` override lives there.
