# Risk Screening API

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)](./compose.yaml)

> Backend full-stack para **debida diligencia automatizada de proveedores** y cruce contra listas internacionales de alto riesgo — OFAC SDN, World Bank Debarred Firms, ICIJ Offshore Leaks.

---

## Tabla de Contenido

- [Descripción General](#descripción-general)
- [Arquitectura](#arquitectura)
- [Estructura del Repositorio](#estructura-del-repositorio)
- [Stack Tecnológico](#stack-tecnológico)
- [Decisiones de Arquitectura (ADRs)](#decisiones-de-arquitectura-adrs)
- [Fuentes y Referencias](#fuentes-y-referencias)

---

## Descripción General

### Módulo Scraping — API de Búsqueda en Listas de Alto Riesgo

API RESTful que extrae y agrega datos de compliance desde listas internacionales de alto riesgo:

| Fuente | Lista | Atributos |
|--------|-------|-----------|
| [US Treasury / OFAC](https://sanctionssearch.ofac.treas.gov/) | SDN List | Name, Address, Type, Program(s), List, Score |
| [World Bank](https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms) | Debarred Firms | Firm Name, Address, Country, From Date, To Date, Grounds |
| [ICIJ](https://offshoreleaks.icij.org) | Offshore Leaks DB | Entity, Jurisdiction, Linked To, Data From |

**Características:**
- Autenticación via JWT Bearer token (`Authorization: Bearer <token>`)
- Rate limiting: **20 requests/minuto** por IP del cliente (`AspNetCoreRateLimit`)
- Respuestas JSON paginadas con conteo de hits
- Obtención bajo demanda con cache de resultados en `IMemoryCache` (TTL por fuente)

### Módulo Suppliers — SPA de Debida Diligencia de Proveedores

Aplicación SPA full-stack para que oficiales de compliance puedan:
- Registrar, editar y eliminar proveedores con validación completa de campos
- Ejecutar screening contra una o más listas desde un modal emergente
- Ver resultados en tabla con nivel de riesgo clasificado
- Mantener historial de screenings con audit trail

---

## Arquitectura

El sistema sigue una arquitectura de **Modular Monolith** en el backend, con separación por capas dentro de cada módulo (`Domain -> Application -> Infrastructure -> Interfaces`). La comunicación interna usa el patrón **CQRS** mediado por MediatR con un pipeline de behaviors para logging y validación.

| Documento | Descripción |
|-----------|-------------|
| [docs/architecture/c4-diagrams.md](./docs/architecture/c4-diagrams.md) | Diagramas C4 — Context, Container, Component, Code (L1–L4) |
| [docs/architecture/database-schema.md](./docs/architecture/database-schema.md) | Esquema de base de datos — ERD y definición de tablas |

---

## Estructura del Repositorio

> El proyecto está distribuido en **dos repositorios separados** bajo la misma organización GitHub:
> - `risk-screening-api` — Backend .NET 10 (este repositorio)
> - `risk-screening-app` — Frontend Angular 21

```
risk-screening-api/
|-- docs/
|   |-- adr/                                  # Architecture Decision Records
|   |   |-- 0001-modular-monolith.md
|   |   |-- 0002-cqrs-mediatr.md
|   |   |-- 0003-jwt-authentication.md
|   |   |-- 0004-sql-migration-scripts.md
|   |   |-- 0005-rate-limiting-strategy.md
|   |   |-- 0006-web-scraping-approach.md
|   |   |-- 0007-angular-frontend.md
|   |   |-- 0008-cache-strategy.md
|   |   |-- 0009-pagination-strategy.md
|   |   |-- 0010-error-handling.md
|   |   |-- 0011-auditing.md
|   |   `-- 0012-api-versioning.md
|   `-- architecture/
|       |-- c4-diagrams.md                    # Diagramas C4 (L1 a L4)
|       `-- database-schema.md                # ERD y definición de tablas
|
|-- RiskScreening.API/                        # Backend .NET 10
|   |-- Migrations/
|   |   `-- Scripts/                          # Scripts SQL versionados (estilo Flyway, ejecutados por DbUp)
|   |-- Modules/
|   |   |-- IAM/                              # Módulo IAM (Domain/App/Infra/Interfaces)
|   |   |-- Scraping/                         # Módulo Scraping
|   |   `-- Suppliers/                        # Módulo Suppliers + Screening
|   `-- Shared/                               # Shared Kernel
|
|-- RiskScreening.UnitTests/                  # Tests unitarios
|
|-- compose.yaml                              # Docker Compose (API + SQL Server)
|-- .env.example                              # Variables de entorno de ejemplo
|-- CHANGELOG.md
|-- CONTRIBUTING.md
`-- README.md
```

---

## Stack Tecnológico

### Backend

| Capa | Tecnología | Versión | Propósito |
|------|-----------|---------|-----------|
| Framework | ASP.NET Core | .NET 10 | Host de la Web API |
| Versionado | Asp.Versioning.Http | 8.x | Versionado de API por header (`Api-Version`) |
| CQRS | MediatR | 14.1.0 | Mediador de commands/queries |
| Validación | FluentValidation | 12.1.1 | Validación en pipeline MediatR |
| ORM | Entity Framework Core | 10.0.3 | Acceso a datos |
| Auth | JWT Bearer / BCrypt.Net-Next | 10.0.3 / 4.0.3 | Autenticación y hasheo de contraseñas |
| Docs | Swashbuckle (`Swashbuckle.AspNetCore`) | 10.1.4 | Swagger UI — OpenAPI 3.1 |
| Rate Limiting | AspNetCoreRateLimit | 5.0.0 | Cuotas de requests por IP del cliente en endpoints de scraping |
| Testing | xUnit + FluentAssertions | — | Tests unitarios |
| Migraciones | DbUp (`dbup-sqlserver`) | 7.2.0 | Runner de scripts SQL versionados |

### Frontend (SPA Angular)

| Capa | Tecnología | Versión |
|------|-----------|---------|
| Framework | Angular | 21.2.x |
| Lenguaje | TypeScript | 5.x |
| UI | PrimeNG | 21.1.3 |
| Formularios | Angular Reactive Forms | — |
| HTTP | Angular HttpClient | — |
| Testing | Jest + Angular Testing Library | — |

### Infraestructura

| Componente | Tecnología |
|-----------|-----------|
| Base de datos | SQL Server 2022 |
| Contenedores | Docker + Docker Compose |

---

## Decisiones de Arquitectura (ADRs)

| ADR | Decisión | Estado |
|-----|----------|--------|
| [ADR-0001](./docs/adr/0001-modular-monolith.md) | Arquitectura Modular Monolith | Accepted |
| [ADR-0002](./docs/adr/0002-cqrs-mediatr.md) | CQRS con MediatR y pipeline behaviors | Accepted |
| [ADR-0003](./docs/adr/0003-jwt-authentication.md) | Autenticación JWT Bearer (API Key eliminado) | Accepted |
| [ADR-0004](./docs/adr/0004-sql-migration-scripts.md) | Scripts SQL versionados con DbUp (nomenclatura estilo Flyway) | Accepted |
| [ADR-0005](./docs/adr/0005-rate-limiting-strategy.md) | Rate limiting con AspNetCoreRateLimit (por IP del cliente) | Accepted |
| [ADR-0006](./docs/adr/0006-web-scraping-approach.md) | Web scraping: bajo demanda con IMemoryCache (Fase 1) | Accepted |
| [ADR-0007](./docs/adr/0007-angular-frontend.md) | Framework frontend: Angular 21.2 + PrimeNG 21 | Accepted |
| [ADR-0008](./docs/adr/0008-cache-strategy.md) | Tecnología de cache: IMemoryCache (Fase 1) | Accepted |
| [ADR-0009](./docs/adr/0009-pagination-strategy.md) | Estrategia de paginación: offset-based con wrapper PageResponse | Accepted |
| [ADR-0010](./docs/adr/0010-error-handling.md) | Manejo centralizado de errores con GlobalExceptionHandler | Accepted |
| [ADR-0011](./docs/adr/0011-auditing.md) | Timestamps de auditoría via intercepción de SaveChanges en EF Core | Accepted |
| [ADR-0012](./docs/adr/0012-api-versioning.md) | Estrategia de versionado de API: header-based con `Api-Version` | Accepted |

---

## Fuentes y Referencias

### Listas de Alto Riesgo (Fuentes de Datos)
- **OFAC SDN Search** — [sanctionssearch.ofac.treas.gov](https://sanctionssearch.ofac.treas.gov/)
- **OFAC SDN XML Feed** — [treasury.gov/ofac/downloads/sdn.xml](https://www.treasury.gov/ofac/downloads/sdn.xml)
- **World Bank Debarred Firms** — [projects.worldbank.org/en/projects-operations/procurement/debarred-firms](https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms)
- **ICIJ Offshore Leaks Database** — [offshoreleaks.icij.org](https://offshoreleaks.icij.org)
- **ICIJ Data API Docs** — [offshoreleaks.icij.org/api](https://offshoreleaks.icij.org/api)

### Estándares de Documentación
- **C4 Model** — Simon Brown — [c4model.com](https://c4model.com/)
- **Architecture Decision Records** — Michael Nygard — [cognitect.com/blog/2011/11/15/documenting-architecture-decisions](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
- **OpenAPI 3.1 Specification** — [spec.openapis.org/oas/v3.1.0](https://spec.openapis.org/oas/v3.1.0)
- **Keep a Changelog** — [keepachangelog.com](https://keepachangelog.com/en/1.0.0/)
- **Semantic Versioning** — [semver.org](https://semver.org/)
- **Mermaid Diagrams** — [mermaid.js.org](https://mermaid.js.org/)

### Frameworks y Librerías
- **ASP.NET Core Documentation** — [learn.microsoft.com/aspnet/core](https://learn.microsoft.com/en-us/aspnet/core/)
- **MediatR** — [github.com/jbogard/MediatR](https://github.com/jbogard/MediatR)
- **FluentValidation** — [docs.fluentvalidation.net](https://docs.fluentvalidation.net/)
- **Entity Framework Core** — [learn.microsoft.com/ef/core](https://learn.microsoft.com/en-us/ef/core/)
- **BCrypt.Net-Next** — [github.com/BcryptNet/bcrypt.net](https://github.com/BcryptNet/bcrypt.net)
- **DbUp** — [dbup.readthedocs.io](https://dbup.readthedocs.io/)
- **Swashbuckle** — [github.com/domaindrivendev/Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- **AspNetCoreRateLimit** — [github.com/stefanprodan/AspNetCoreRateLimit](https://github.com/stefanprodan/AspNetCoreRateLimit)
- **Angular** — [angular.dev](https://angular.dev/)
- **PrimeNG** — [primeng.org](https://primeng.org/)

### Patrones y Buenas Prácticas
- **Domain-Driven Design** — Eric Evans — [domainlanguage.com/ddd](https://www.domainlanguage.com/ddd/)
- **Clean Architecture** — Robert C. Martin — [blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- **CQRS Pattern** — Martin Fowler — [martinfowler.com/bliki/CQRS.html](https://martinfowler.com/bliki/CQRS.html)
- **REST API Best Practices** — [restfulapi.net](https://restfulapi.net/)
- **OWASP Top 10** — [owasp.org/www-project-top-ten](https://owasp.org/www-project-top-ten/)
- **JWT Best Practices (RFC 8725)** — [tools.ietf.org/html/rfc8725](https://tools.ietf.org/html/rfc8725)
- **HTTP 429 Too Many Requests (RFC 6585)** — [tools.ietf.org/html/rfc6585](https://tools.ietf.org/html/rfc6585)
- **BCrypt Password Hashing** — [auth0.com/blog/hashing-in-action-understanding-bcrypt](https://auth0.com/blog/hashing-in-action-understanding-bcrypt/)

---

<p align="center">Desarrollado como entregable para la Prueba Técnica EY — .NET Developer 2026</p>
