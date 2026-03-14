# ADR-0001: Modular Monolith Architecture

## Status
`Accepted`

## Date
2026-03-13

## Context

The platform requires developing two independent modules:
1. A scraping API for risk lists with JWT authentication
2. A supplier management SPA with JWT authentication

Both must coexist within the same .NET backend. Three options were evaluated:

| Option | Description | Pros | Cons |
|--------|-------------|------|------|
| **Simple Monolith** | Everything in one project with no module separation | Fast to implement | Hard to scale, high coupling |
| **Modular Monolith** | Single process, multiple modules with clear boundaries | Balance between simplicity and organization | Requires discipline to enforce separation |
| **Microservices** | Independent services per module | Independent scalability | Excessive infrastructure overhead |

## Decision

Use a **Modular Monolith**: a single ASP.NET Core process with independent modules, each with its own `Domain / Application / Infrastructure / Interfaces` layer stack.

The module structure is:
```
Modules/
  IAM/           -- Identity & Access Management
  Scraping/      -- Scraping Module: search in high-risk lists
  Suppliers/     -- Suppliers Module: supplier management and screening
Shared/          -- Shared Kernel (DbContext, pipelines, exceptions)
```

Each module has strict boundaries:
- Modules do not reference each other directly
- Inter-module communication happens via domain events or through the `Shared` kernel
- Each module registers its own dependencies via `IServiceCollection` extension methods

## Consequences

**Positive:**
- Clear separation of responsibilities — each module is independent
- Single deployment — Docker Compose with one API container
- Easy to understand for reviewers
- Modules are cohesive — all IAM artifacts live under `Modules/IAM/`

**Negative:**
- The shared DbContext creates an infrastructure dependency between modules
- Scaling a single module independently would require extracting it to a microservice

**Mitigation:**
- Each module has its own EF configuration class (`IEntityTypeConfiguration<T>`)
- `AppDbContext` only applies configurations via `ApplyConfigurationsFromAssembly` — it does not know entities directly

## References
- [Modular Monolith: A Primer — Kamil Grzybek](https://www.kamilgrzybek.com/design/modular-monolith-primer/)
- [Clean Architecture — Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
