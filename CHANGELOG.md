# Changelog

All notable changes to this project are documented here.

Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
Versioning: [Semantic Versioning](https://semver.org/spec/v2.0.0.html)

---

## [Unreleased]

### Added

#### Shared Infrastructure

- `[INFRA]` Base domain model: `AggregateRoot`, `Entity`, `ValueObject`, `IAuditableEntity`, `IDomainEvent`
- `[INFRA]` Shared value objects: `Email`
- `[INFRA]` Base repository pattern: `BaseRepository`, `IBaseRepository`, `IUnitOfWork`
- `[INFRA]` `AppDbContext` with automatic audit stamping (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`)
- `[INFRA]` `DatabaseMigrator` for DbUp SQL migrations at startup
- `[INFRA]` MediatR pipeline behaviors: `LoggingPipelineBehavior`, `ValidationPipelineBehavior`
- `[INFRA]` Centralized exception handlers: `DomainExceptionHandler`, `ValidationExceptionHandler`, `GlobalExceptionHandler`
- `[INFRA]` Shared API resources: `ErrorResponse`, `PageResponse<T>` with `PageMetadata`
- `[INFRA]` Query infrastructure: `SpecificationComposer`, `SortConfiguration`, `PageableExtensions`, `PageRequest`
- `[INFRA]` OpenAPI documentation: JWT security scheme, `StandardResponsesOperationFilter`, `ErrorResponseDocumentFilter`
- `[INFRA]` API versioning via `Api-Version` header
- `[INFRA]` CORS policy with configuration-based allowed origins
- `[INFRA]` Serilog structured logging with `CorrelationIdMiddleware` for request tracing
- `[INFRA]` IP-based rate limiting via `AspNetCoreRateLimit` with tiered rules: sign-in (5 req/min), scraping lists (20 req/min), general API (100 req/min)
- `[INFRA]` `RateLimitResponseMiddleware` — rewrites 429 responses to standard `ErrorResponse` (RFC 7807) format with `RATE_LIMIT_EXCEEDED` error code
- `[INFRA]` Docker Compose setup with API + SQL Server services
- `[INFRA]` Snake-case naming convention for EF Core entity mappings

#### IAM Module

- `[IAM]` Database schema: `roles`, `users`, `user_roles` tables with migration scripts
- `[IAM]` Domain model: `User` and `Role` aggregates, `AccountStatus`, `Password`, `Username` value objects
- `[IAM]` `POST /api/authentication/sign-in` — JWT authentication with BCrypt password verification
- `[IAM]` `GET /api/authentication/me` — retrieve current authenticated user profile
- `[IAM]` Data seeder for default roles and admin user
- `[IAM]` OpenAPI specification (`openapi-iam.yaml`)
- `[IAM]` Unit tests: `SignInCommandHandler`, `SignInCommandValidator`, `GetCurrentUserQueryHandler`

#### Scraping Module

- `[SCR]` Three named `HttpClient` registrations via `IHttpClientFactory` (OFAC, World Bank, ICIJ) with timeout and User-Agent headers
- `[SCR]` `IMemoryCache` registration for on-demand scraping result caching (10-min TTL per source per term)
- `[SCR]` Domain model: `RiskEntry`, `SearchResult` value objects (`Domain/Model/ValueObjects/`), `SearchRiskListsQuery` CQRS query (`Domain/Model/Queries/`)
- `[SCR]` `IScrapingSource` application port (`Application/Ports/`) — extensible interface for scraping source adapters
- `[SCR]` `SearchRiskListsQueryHandler` (`Application/Search/`) — MediatR CQRS handler with `IMemoryCache` caching and parallel execution via `Task.WhenAll`
- `[SCR]` `OfacScrapingSource` (`Infrastructure/Sources/`) — adapter for real web scraping of `https://sanctionssearch.ofac.treas.gov/` (ASP.NET ViewState extraction + form POST)
- `[SCR]` `OfacHtmlParser` (`Infrastructure/Sources/`) — static helper for OFAC HTML form data extraction and results table parsing with `HtmlAgilityPack`
- `[SCR]` `GET /api/lists/search?q={term}&sources=ofac&sources=worldbank` — unified endpoint for all risk list searches; `sources` parameter optional (repeated query params: ofac, worldbank, icij); when omitted, queries all sources
- `[SCR]` `SearchRiskListsQueryValidator` (`Application/Search/`) — FluentValidation validator auto-executed by `ValidationPipelineBehavior`; validates `q` is not empty and `sources` values are whitelisted
- `[SCR]` `ScrapingResponseMapper` (`Interfaces/REST/Mappers/Response/`) — maps `SearchResult` domain objects to `ScrapingResponse` DTOs
- `[SCR]` Response DTOs: `ScrapingResponse`, `RiskEntryResponse` with Swagger annotations
- `[SCR]` Swagger API grouping: "Lists Module" added to dropdown
- `[SCR]` OpenAPI specification (`openapi-lists.yaml`)
- `[SCR]` HTTP test file (`RiskScreening.API.http`) — search requests for all sources, individual sources, error cases
- `[SCR]` Unit tests: `OfacScrapingSourceTests` (16 tests), `SearchRiskListsQueryHandlerTests` (10 tests), `SearchResultTests` (5 tests)
- `[SCR]` Test infrastructure: `RiskEntryMother`, `SearchResultMother`, `OfacHtmlMother`, `FakeHttpMessageHandler`

#### Suppliers Module

- `[SUP]` Database schema: `suppliers` and `screening_results` tables with migration scripts
- `[SUP]` Domain model: `Supplier` and `ScreeningResult` aggregates
- `[SUP]` Value objects: `LegalName`, `CommercialName`, `TaxId`, `CountryCode`, `PhoneNumber`, `WebsiteUrl`, `SupplierAddress`, `AnnualBilling`, `RiskLevel`, `SupplierStatus`, `SupplierId`
- `[SUP]` `POST /api/suppliers` — create supplier with full field validation and TaxId uniqueness check
- `[SUP]` `GET /api/suppliers` — list suppliers with pagination, filtering (legalName, commercialName, taxId, country, status, riskLevel), and configurable sorting
- `[SUP]` `GET /api/suppliers/{id}` — retrieve supplier by ID
- `[SUP]` `PUT /api/suppliers/{id}` — update supplier with full field validation and TaxId uniqueness check
- `[SUP]` `DELETE /api/suppliers/{id}` — soft-delete supplier
- `[SUP]` `SupplierFilterComposer` for composable EF Core query predicates
- `[SUP]` `SupplierSortConfiguration` with whitelisted sort fields (default: `updatedAt DESC`)
- `[SUP]` OpenAPI specification (`openapi-suppliers.yaml`)
- `[SUP]` Swagger API grouping by module (All, IAM, Suppliers) with dropdown in Swagger UI
- `[SUP]` `SchemaExamplesFilter` for realistic example values in Swagger schemas
- `[SUP]` Unit tests: `CreateSupplierCommandHandler`, `CreateSupplierCommandValidator`, `UpdateSupplierCommandHandler`, `UpdateSupplierCommandValidator`, `GetAllSuppliersQueryHandler`, `GetSupplierByIdQueryHandler`
- `[SUP]` Test infrastructure: `SupplierBuilder`, `SupplierMother`, `CreateSupplierCommandMother`, `UpdateSupplierCommandMother` with Bogus

### Changed

- `[INFRA]` `AppDbContext` actor claim changed from `ClaimTypes.Name` to `ClaimTypes.NameIdentifier` (stores user ID instead of username)
- `[SUP]` `CountryCode.ValidCodes` made public for shared use between production code and tests
- `[SUP]` `SupplierResponse` now includes `createdBy` and `updatedBy` audit fields

#### Documentation

- `[DOCS]` ADR-0001: Modular Monolith architecture decision (EN + ES)
- `[DOCS]` ADR-0002: CQRS with MediatR and pipeline behaviors (EN + ES)
- `[DOCS]` ADR-0003: JWT authentication — API Key approach evaluated and discarded (EN + ES)
- `[DOCS]` ADR-0004: Versioned SQL scripts with DbUp, Flyway-style naming (EN + ES)
- `[DOCS]` ADR-0005: Rate limiting with AspNetCoreRateLimit per client IP (EN + ES)
- `[DOCS]` ADR-0006: Web scraping — on-demand with IMemoryCache, Phase 2 background worker (EN + ES)
- `[DOCS]` ADR-0007: Frontend framework — Angular 21 + PrimeNG (EN + ES)
- `[DOCS]` ADR-0008: Cache technology — IMemoryCache Phase 1, Redis migration path (EN + ES)
- `[DOCS]` ADR-0009: Pagination strategy — offset-based with PageResponse wrapper (EN + ES)
- `[DOCS]` ADR-0010: Centralized error handling — IExceptionHandler chain + RFC 7807 (EN + ES)
- `[DOCS]` ADR-0011: Audit fields — IAuditableEntity + SaveChangesAsync interception (EN + ES)
- `[DOCS]` ADR-0012: API versioning strategy — header-based with `Api-Version` header (EN + ES)
- `[DOCS]` ADR-0013: Structured logging with Serilog — Loki-optimized templates (EN + ES)
- `[DOCS]` C4 Architecture Diagrams — L1 Context, L2 Container, L3 Component, L4 Code (EN + ES)
- `[DOCS]` README — project overview, tech stack, ADR table (EN + ES)
- `[DOCS]` CONTRIBUTING — branch strategy, commit conventions, PR workflow (EN + ES)

---

[Unreleased]: https://github.com/compliance-hub-jmyr/risk-screening-api/commits/main
