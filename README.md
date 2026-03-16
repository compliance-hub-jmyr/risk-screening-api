# Risk Screening API

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)](./compose.yaml)

> Full-stack backend for **automated supplier due diligence** and cross-referencing against international high-risk lists — OFAC SDN, World Bank Debarred Firms, ICIJ Offshore Leaks.

---

## Table of Contents

- [Overview](#overview)
- [Postman Collection](#postman-collection)
- [Architecture](#architecture)
- [Repository Structure](#repository-structure)
- [Technology Stack](#technology-stack)
- [Architecture Decision Records (ADRs)](#architecture-decision-records-adrs)
- [Sources and References](#sources-and-references)

---

## Overview

### Scraping Module — High-Risk List Search API

RESTful API that fetches and aggregates compliance data from international high-risk lists:

| Source | List | Attributes |
|--------|------|------------|
| [US Treasury / OFAC](https://sanctionssearch.ofac.treas.gov/) | SDN List | Name, Address, Type, Program(s), List, Score |
| [World Bank](https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms) | Debarred Firms | Firm Name, Address, Country, From Date, To Date, Grounds |
| [ICIJ](https://offshoreleaks.icij.org) | Offshore Leaks DB | Entity, Jurisdiction, Linked To, Data From |

**Features:**
- Authentication via JWT Bearer token (`Authorization: Bearer <token>`)
- Rate limiting: **20 requests/minute** per client IP (`AspNetCoreRateLimit`)
- Paginated JSON responses with hit count
- On-demand fetching with `IMemoryCache` result caching (TTL per source)

### Suppliers Module — Supplier Due Diligence SPA

Full-stack SPA for compliance officers to:
- Register, edit, and delete suppliers with full field validation
- Run screening against one or more lists from a modal dialog
- View results in a table with classified risk level
- Maintain a screening history with audit trail

---

## Postman Collection

A ready-to-use Postman collection is included in [`postman/`](./postman/):

| File | Description |
|------|-------------|
| `Risk-Screening-API.postman_collection.json` | Full collection — all endpoints, example responses, and auto-token script |
| `Risk-Screening-API.postman_environment.json` | Local environment (`http://localhost:5215`) |
| `Risk-Screening-API.postman_environment.production.json` | Production environment (Azure Container Apps URL) |

**Quick start:**
1. Import the collection and the local environment into Postman
2. Run **Authentication → Sign In** — the JWT token is saved automatically as a collection variable
3. All subsequent requests use the saved token via Bearer authentication

---

## Architecture

The system follows a **Modular Monolith** architecture on the backend, with layer separation within each module (`Domain -> Application -> Infrastructure -> Interfaces`). Internal communication uses the **CQRS** pattern mediated by MediatR with a pipeline of behaviors for logging and validation.

| Document | Description |
|----------|-------------|
| [docs/architecture/c4-diagrams.md](./docs/architecture/c4-diagrams.md) | C4 Diagrams — Context, Container, Component, Code (L1–L4) |
| [docs/architecture/database-schema.md](./docs/architecture/database-schema.md) | Database Schema — ERD and table definitions |

---

## Repository Structure

> The project is distributed across **two separate repositories** under the same GitHub organization:
> - `risk-screening-api` — .NET 10 Backend (this repository)
> - `risk-screening-app` — Angular 21 Frontend

```
risk-screening-api/
|-- docs/
|   |-- adr/                                  # Architecture Decision Records (0001–0018)
|   |-- api/                                  # OpenAPI specifications per module
|   |   |-- openapi-iam.yaml
|   |   |-- openapi-lists.yaml
|   |   `-- openapi-suppliers.yaml
|   |-- architecture/
|   |   |-- c4-diagrams.md                    # C4 Diagrams (L1 to L4 + Deployment)
|   |   `-- database-schema.md                # ERD and table definitions
|   |-- deployment/
|   |   `-- azure-deployment.md               # Step-by-step Azure Container Apps guide
|   `-- user-stories/
|       |-- iam-module.md
|       |-- scraping-module.md
|       `-- suppliers-module.md
|
|-- RiskScreening.API/                        # .NET 10 Backend
|   |-- Migrations/
|   |   `-- Scripts/                          # Versioned SQL scripts (Flyway-style, run by DbUp)
|   |-- Modules/
|   |   |-- IAM/                              # IAM Module (Domain/App/Infra/Interfaces)
|   |   |-- Scraping/                         # Scraping Module (OFAC, World Bank, ICIJ via Playwright)
|   |   `-- Suppliers/                        # Suppliers + Screening Module
|   `-- Shared/                               # Shared Kernel
|
|-- RiskScreening.UnitTests/                  # Unit tests
|
|-- postman/                                  # Postman collection and environments
|   |-- Risk-Screening-API.postman_collection.json
|   |-- Risk-Screening-API.postman_environment.json
|   `-- Risk-Screening-API.postman_environment.production.json
|
|-- compose.yaml                              # Docker Compose (API + SQL Server)
|-- .env.example                              # Example environment variables
|-- CHANGELOG.md
|-- CONTRIBUTING.md
`-- README.md
```

---

## Technology Stack

### Backend

| Layer | Technology | Version | Purpose |
|-------|-----------|---------|---------|
| Framework | ASP.NET Core | .NET 10 | Web API host |
| Versioning | Asp.Versioning.Http | 8.x | Header-based API versioning (`Api-Version` header) |
| CQRS | MediatR | 14.1.0 | Command/query mediator |
| Validation | FluentValidation | 12.1.1 | Validation in MediatR pipeline |
| ORM | Entity Framework Core | 10.0.3 | Data access |
| Auth | JWT Bearer / BCrypt.Net-Next | 10.0.3 / 4.0.3 | Authentication and password hashing |
| Docs | Swashbuckle (`Swashbuckle.AspNetCore`) | 10.1.4 | Swagger UI — OpenAPI 3.1 |
| Rate Limiting | AspNetCoreRateLimit | 5.0.0 | Per-IP request quotas on scraping endpoints |
| Testing | xUnit + FluentAssertions | — | Unit tests |
| Migrations | DbUp (`dbup-sqlserver`) | 7.2.0 | Versioned SQL schema runner |

### Frontend (Angular SPA)

| Layer | Technology | Version |
|-------|-----------|---------|
| Framework | Angular | 21.2.x |
| Language | TypeScript | 5.x |
| UI | PrimeNG | 21.1.3 |
| Forms | Angular Reactive Forms | — |
| HTTP | Angular HttpClient | — |
| Testing | Jest + Angular Testing Library | — |

### Infrastructure

| Component | Technology |
|-----------|-----------|
| Database | SQL Server 2022 (local) / Azure SQL Database (production) |
| Containers | Docker + Docker Compose (local) / Azure Container Apps (production) |
| Container Registry | Docker Hub (`jhosepmyr/riskscreening-api`) |
| CI/CD | GitHub Actions (`ci.yml` build + test, `cd.yml` deploy) |
| Browser Automation | Microsoft Playwright (headless Chromium — ICIJ scraping) |

---

## Architecture Decision Records (ADRs)

| ADR | Decision | Status |
|-----|----------|--------|
| [ADR-0001](./docs/adr/0001-modular-monolith.md) | Modular Monolith architecture | Accepted |
| [ADR-0002](./docs/adr/0002-cqrs-mediatr.md) | CQRS with MediatR and pipeline behaviors | Accepted |
| [ADR-0003](./docs/adr/0003-jwt-authentication.md) | JWT Bearer authentication (API Key removed) | Accepted |
| [ADR-0004](./docs/adr/0004-sql-migration-scripts.md) | Versioned SQL scripts with DbUp (Flyway-style naming) | Accepted |
| [ADR-0005](./docs/adr/0005-rate-limiting-strategy.md) | Rate limiting with AspNetCoreRateLimit (per client IP) | Accepted |
| [ADR-0006](./docs/adr/0006-web-scraping-approach.md) | Web scraping: on-demand with IMemoryCache (Phase 1) | Accepted |
| [ADR-0007](./docs/adr/0007-angular-frontend.md) | Frontend framework: Angular 21.2 + PrimeNG 21 | Accepted |
| [ADR-0008](./docs/adr/0008-cache-strategy.md) | Cache technology: IMemoryCache (Phase 1) | Accepted |
| [ADR-0009](./docs/adr/0009-pagination-strategy.md) | Pagination strategy: offset-based with PageResponse wrapper | Accepted |
| [ADR-0010](./docs/adr/0010-error-handling.md) | Centralized error handling with GlobalExceptionHandler | Accepted |
| [ADR-0011](./docs/adr/0011-auditing.md) | Audit timestamps via EF Core SaveChanges interception | Accepted |
| [ADR-0012](./docs/adr/0012-api-versioning.md) | API versioning strategy: header-based with `Api-Version` | Accepted |
| [ADR-0013](./docs/adr/0013-structured-logging.md) | Structured logging with Serilog (Loki-optimized templates) | Accepted |
| [ADR-0014](./docs/adr/0014-container-platform.md) | Container platform: Azure Container Apps | Accepted |
| [ADR-0015](./docs/adr/0015-container-registry.md) | Container registry: Docker Hub | Accepted |
| [ADR-0016](./docs/adr/0016-two-domain-architecture.md) | Two-domain architecture: separate public URLs per service | Accepted |
| [ADR-0017](./docs/adr/0017-cicd-pipeline.md) | CI/CD pipeline: GitHub Actions with `workflow_run` pattern | Accepted |
| [ADR-0018](./docs/adr/0018-database-strategy.md) | Database strategy: Azure SQL Database PaaS | Accepted |

---

## Sources and References

### High-Risk Lists (Data Sources)
- **OFAC SDN Search** — [sanctionssearch.ofac.treas.gov](https://sanctionssearch.ofac.treas.gov/)
- **OFAC SDN XML Feed** — [treasury.gov/ofac/downloads/sdn.xml](https://www.treasury.gov/ofac/downloads/sdn.xml)
- **World Bank Debarred Firms** — [projects.worldbank.org/en/projects-operations/procurement/debarred-firms](https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms)
- **ICIJ Offshore Leaks Database** — [offshoreleaks.icij.org](https://offshoreleaks.icij.org)
- **ICIJ Data API Docs** — [offshoreleaks.icij.org/api](https://offshoreleaks.icij.org/api)

### Documentation Standards
- **C4 Model** — Simon Brown — [c4model.com](https://c4model.com/)
- **Architecture Decision Records** — Michael Nygard — [cognitect.com/blog/2011/11/15/documenting-architecture-decisions](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
- **OpenAPI 3.1 Specification** — [spec.openapis.org/oas/v3.1.0](https://spec.openapis.org/oas/v3.1.0)
- **Keep a Changelog** — [keepachangelog.com](https://keepachangelog.com/en/1.0.0/)
- **Semantic Versioning** — [semver.org](https://semver.org/)
- **Mermaid Diagrams** — [mermaid.js.org](https://mermaid.js.org/)

### Frameworks and Libraries
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

### Patterns and Best Practices
- **Domain-Driven Design** — Eric Evans — [domainlanguage.com/ddd](https://www.domainlanguage.com/ddd/)
- **Clean Architecture** — Robert C. Martin — [blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- **CQRS Pattern** — Martin Fowler — [martinfowler.com/bliki/CQRS.html](https://martinfowler.com/bliki/CQRS.html)
- **REST API Best Practices** — [restfulapi.net](https://restfulapi.net/)
- **OWASP Top 10** — [owasp.org/www-project-top-ten](https://owasp.org/www-project-top-ten/)
- **JWT Best Practices (RFC 8725)** — [tools.ietf.org/html/rfc8725](https://tools.ietf.org/html/rfc8725)
- **HTTP 429 Too Many Requests (RFC 6585)** — [tools.ietf.org/html/rfc6585](https://tools.ietf.org/html/rfc6585)
- **BCrypt Password Hashing** — [auth0.com/blog/hashing-in-action-understanding-bcrypt](https://auth0.com/blog/hashing-in-action-understanding-bcrypt/)

---

<p align="center">Developed as a deliverable for the EY Technical Assessment — .NET Developer 2026</p>
