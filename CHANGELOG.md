# Changelog

All notable changes to this project are documented here.

Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
Versioning: [Semantic Versioning](https://semver.org/spec/v2.0.0.html)

---

## [1.1.0] — 2026-03-16

### Added

- `[DB]` Migration script `V007__seed_sample_suppliers.sql` — seeds sample supplier records for development and demo environments, covering a range of risk levels, statuses, and countries to exercise all screening and filtering scenarios.
- `[DOCS]` `CONTRIBUTING.md` — added Local Development Setup section documenting prerequisites, database startup, environment configuration, Playwright browser installation, and API run command.
- `[DOCS]` Postman collection (`postman/`) — ready-to-use collection with all endpoints (Authentication, Lists, Suppliers), example responses for all status codes, and auto-token script that saves the JWT after Sign In.

### Fixed

- `[SCR]` `IcijScrapingSource` — truncate search term to 50 characters before building the ICIJ URL. ICIJ's search input has a 50-character browser limit; sending longer terms caused 0 results because the SPA truncated the query mid-word. Supplier names longer than 50 chars (e.g. "Oceania International Consultants (BVI) Company Limited") now correctly return matches.
- `[SCR]` Playwright Chromium browser missing after package update — `IcijScrapingSource` threw `PlaywrightException: Executable doesn't exist` on startup. Root cause: the `Microsoft.Playwright` NuGet package pins a specific browser revision (`chromium-1208`) which must be downloaded manually in local development after each update. Production Docker image is unaffected (browser installed at build time).
- `[DOCS]` `README.md` — updated ADR table (added ADR-0013 through ADR-0018), updated directory structure (added `docs/deployment/`, `docs/user-stories/`, `docs/api/`), and updated infrastructure table (added Azure Container Apps, Docker Hub, GitHub Actions, Playwright).

---

## [1.0.0] — 2026-03-16

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
- `[INFRA]` Configurable timezone via `App:TimeZone` (`appsettings.json`) for audit timestamps
- `[INFRA]` Dockerfile with multi-stage build, Chromium/Playwright dependencies, non-root user

#### IAM Module

- `[IAM]` Database schema: `roles`, `users`, `user_roles` tables with migration scripts
- `[IAM]` Domain model: `User` and `Role` aggregates, `AccountStatus`, `Password`, `Username` value objects
- `[IAM]` `POST /api/authentication/sign-in` — JWT authentication with BCrypt password verification
- `[IAM]` `GET /api/authentication/me` — retrieve current authenticated user profile
- `[IAM]` Data seeder for default roles and admin user
- `[IAM]` OpenAPI specification (`openapi-iam.yaml`)
- `[IAM]` Unit tests: `SignInCommandHandler`, `SignInCommandValidator`, `GetCurrentUserQueryHandler`

#### Scraping Module

- `[SCR]` Two named `HttpClient` registrations via `IHttpClientFactory` (OFAC, World Bank) with timeout and User-Agent headers; ICIJ uses `Microsoft.Playwright` (headless Chromium) instead
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
- `[SCR]` `WorldBankScrapingSource` (`Infrastructure/Sources/`) — adapter for World Bank Debarred Firms; two-step web scraping: GET HTML page → extract API config from JavaScript → GET JSON API → filter and map
- `[SCR]` `WorldBankHtmlParser` (`Infrastructure/Sources/`) — unified parser: `ExtractApiConfig()` scrapes `<script>` tags with `HtmlAgilityPack` to extract API URL + key; `ParseResults()` deserializes `response.ZPROCSUPP` JSON, filters firms by search term (multi-field OR logic across name, address, city, state, country, grounds), combines address components, maps "Ongoing"/"Permanent" ineligibility status to `toDate`
- `[SCR]` `IcijScrapingSource` (`Infrastructure/Sources/`) — adapter for ICIJ Offshore Leaks; headless browser scraping via `Microsoft.Playwright` (Chromium) with anti-detection stealth flags to bypass CloudFront WAF; renders the JavaScript SPA and parses the HTML results table
- `[SCR]` `IcijHtmlParser` (`Infrastructure/Sources/`) — parses ICIJ search results HTML (rendered by Playwright) with `HtmlAgilityPack`; extracts Entity (→ Name), Jurisdiction, Linked To (→ LinkedTo), Data From (→ DataFrom)
- `[SCR]` Unit tests: `OfacScrapingSourceTests` (16 tests), `WorldBankScrapingSourceTests` (18 tests), `IcijScrapingSourceTests` (14 tests), `SearchRiskListsQueryHandlerTests` (10 tests), `SearchResultTests` (5 tests)
- `[SCR]` Test infrastructure: `RiskEntryMother`, `SearchResultMother`, `OfacHtmlMother`, `WorldBankJsonMother`, `IcijHtmlMother`, `FakeHttpMessageHandler`

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
- `[INFRA]` `SortConfiguration<T>` — `AllowedSortFields` changed from `Expression<Func<T, object>>` to `LambdaExpression` to preserve the actual `TKey` and avoid boxing `Convert()` nodes that EF Core cannot translate to SQL
- `[INFRA]` `SortConfiguration<T>` — `OrderBy`/`OrderByDescending` now invoked via reflection (`MethodInfo.MakeGenericMethod`) to pass the correctly-typed expression
- `[INFRA]` `SortConfiguration<T>` — added optional `TiebreakerField` property (appended as `ThenBy ASC`) for deterministic offset-based pagination
- `[SUP]` `SupplierSortConfiguration` — sort expressions now reference value object properties directly (e.g. `x => x.LegalName`) instead of `.Value`; EF Core uses the `HasConversion` converter for SQL while `IComparable<T>` is used for in-memory (MockQueryable)
- `[SUP]` `SupplierSortConfiguration` — added `TiebreakerField = x => x.Id` for stable pagination
- `[SUP]` `LegalName`, `CommercialName`, `TaxId` — implement `IComparable<T>` (delegates to `string.Compare` on `.Value`) to support in-memory sorting in unit tests
- `[SUP]` `CountryCode` — implements `IComparable<CountryCode>` for the same reason
- `[SUP]` `SupplierFilterComposer` — string filters changed from `EF.Functions.Like(x.LegalName.Value, ...)` / `EF.Property<string>(...)` to `EF.Functions.Like((string)x.LegalName, ...)`; explicit cast invokes `implicit operator string` in-memory while EF Core translates the property directly to the underlying column
- `[SUP]` `SupplierFilterComposer` — enum filters (`status`, `riskLevel`) now parsed with `Enum.TryParse` before building the expression tree; invalid values are silently ignored (no filter applied)
- `[SUP]` `StringValueObjectConverter<T>` — `ConvertToProvider` now accepts both the value object type and raw `string` to fix EF Core 10 `InvalidCastException` in LIKE filter sanitization

### Fixed

- `[SUP]` `GET /api/suppliers?sortBy=legalName` returned HTTP 500 — EF Core could not translate `OrderBy` on a `HasConversion`-mapped value object property because the expression contained a `Convert()` boxing node. Fixed by using `LambdaExpression` and typed reflection for `Queryable.OrderBy<T, TKey>`.
- `[SUP]` `GET /api/suppliers?sortBy=status` returned HTTP 500 — same root cause (enum boxing). Fixed by the same `LambdaExpression` + reflection approach.
- `[SUP]` `GET /api/suppliers?legalName=Acme` returned HTTP 500 — EF Core could not translate `EF.Property<string>(x, "LegalName")` for filter predicates and also failed on `.Value` navigation inside expression trees. Fixed by using explicit cast `(string)x.LegalName` which EF Core strips to the column name and MockQueryable resolves via `implicit operator string`.
- `[SUP]` `GET /api/suppliers?status=approved` (lowercase) returned no results — enum filter compared `x.Status.ToString() == v` which EF Core cannot translate. Fixed by pre-parsing with `Enum.TryParse(ignoreCase: true)` and comparing `x.Status == parsedValue` directly.

#### Deployment & CI/CD

- `[DEPLOY]` Azure Container Apps deployment with external ingress and auto-provisioned HTTPS domains
- `[DEPLOY]` Azure SQL Database (PaaS, Basic tier) with `AllowAzureServices` firewall rule
- `[DEPLOY]` Deployment guide with step-by-step Azure CLI commands (EN + ES)
- `[CI/CD]` GitHub Actions CI workflow (`ci.yml`) — build and test on push/PR to `main`/`develop`
- `[CI/CD]` GitHub Actions CD workflow (`cd.yml`) — Docker build, push to Docker Hub, deploy to Azure Container Apps via `workflow_run` pattern
- `[CI/CD]` GitHub environment `production` with manual approval gate

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
- `[DOCS]` ADR-0014: Container platform — Azure Container Apps (EN + ES)
- `[DOCS]` ADR-0015: Container registry — Docker Hub (EN + ES)
- `[DOCS]` ADR-0016: Two-domain architecture — separate public URLs (EN + ES)
- `[DOCS]` ADR-0017: CI/CD pipeline — GitHub Actions with workflow_run pattern (EN + ES)
- `[DOCS]` ADR-0018: Database strategy — Azure SQL Database PaaS (EN + ES)
- `[DOCS]` C4 Architecture Diagrams — L1 Context, L2 Container, L3 Component, L4 Code, Deployment (EN + ES)
- `[DOCS]` README — project overview, tech stack, ADR table (EN + ES)
- `[DOCS]` CONTRIBUTING — branch strategy, commit conventions, PR workflow (EN + ES)

---

[1.1.0]: https://github.com/compliance-hub-jmyr/risk-screening-api/releases/tag/v1.1.0
[1.0.0]: https://github.com/compliance-hub-jmyr/risk-screening-api/releases/tag/v1.0.0
