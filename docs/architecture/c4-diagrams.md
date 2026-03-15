# C4 Architecture Diagrams

> The C4 model (Context, Container, Component, Code) provides four levels of abstraction
> for documenting software architecture — from business context down to code.
>
> **References:**
> - C4 Model — Simon Brown: [c4model.com](https://c4model.com/)
> - Mermaid C4 support: [mermaid.js.org/syntax/c4](https://mermaid.js.org/syntax/c4.html)

---

## Level 1 — System Context Diagram

> Shows the system from a business perspective: who the users are and which external systems it interacts with.

```mermaid
C4Context
    title System Context - Risk Screening API

    Person(officer, "Compliance Officer", "EY staff performing supplier due diligence")
    Person(admin, "Platform Admin", "Administrator managing system users and roles")

    System(platform, "Risk Screening API", "Provides high-risk list search and a supplier screening SPA")

    System_Ext(ofac, "OFAC SDN List", "US Treasury - Specially Designated Nationals and Blocked Persons List")
    System_Ext(worldBank, "World Bank Debarment", "List of firms debarred from World Bank projects")
    System_Ext(icij, "ICIJ Offshore Leaks", "Offshore financial database (Panama Papers, Pandora Papers, etc.)")

    Rel(officer, platform, "Manages suppliers, runs screening", "HTTPS + Bearer JWT")
    Rel(admin, platform, "Manages users and roles", "HTTPS + Bearer JWT")
    Rel(platform, ofac, "On-demand fetch of sanctions data", "HTTPS")
    Rel(platform, worldBank, "On-demand fetch of debarment data", "HTTPS")
    Rel(platform, icij, "On-demand query of offshore entities", "HTTPS")

    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
```

---

## Level 2 — Container Diagram

> Shows the containers (processes, databases, frontends) that make up the system and how they communicate.

```mermaid
C4Container
    title Container Diagram - Risk Screening API

    Person(officer, "Compliance Officer")
    Person(admin, "Platform Admin")

    Container_Boundary(platform, "Risk Screening API") {
        Container(spa, "Angular SPA", "Angular 21, TypeScript, PrimeNG", "UI for supplier management, screening, and administration")
        Container(api, ".NET Web API", ".NET 10, ASP.NET Core, MediatR, EF Core", "REST API: business logic, scraping, screening, IAM, CQRS")
        ContainerDb(db, "SQL Server 2022", "Relational Database", "Stores users, roles, suppliers, screening results and audit logs")
        Container(cache, "In-Memory Cache", "Microsoft.Extensions.Caching.Memory", "Caches on-demand scraping results (OFAC, World Bank, ICIJ) and rate limiting counters")
    }

    System_Ext(ofac, "OFAC SDN")
    System_Ext(wb, "World Bank")
    System_Ext(icij, "ICIJ Offshore Leaks")

    Rel(officer, spa, "Uses", "HTTPS :443")
    Rel(admin, spa, "Manages users", "HTTPS :443")
    Rel(spa, api, "GET/POST/PUT/DELETE /api/*", "HTTPS + Bearer JWT")
    Rel(api, db, "Reads and writes entities via EF Core", "SQL TCP :1433")
    Rel(api, cache, "Reads/writes scraping results and rate limit counters", "In-process")
    Rel(api, ofac, "On-demand HTTP GET — downloads SDN XML", "HTTPS")
    Rel(api, wb, "On-demand HTTP GET — parses paginated HTML table", "HTTPS")
    Rel(api, icij, "On-demand HTTP GET — queries public REST API", "HTTPS")
```

> **Note:** Phase 1 uses on-demand scraping (no background worker). A `BackgroundService` pre-population worker is planned for Phase 2 (OFAC and World Bank only; ICIJ remains on-demand permanently).

---

## Level 3 — Component Diagram (Web API)

> Shows the internal components of the main container (.NET Web API) and their responsibilities.

```mermaid
C4Component
    title Component Diagram - .NET Web API (Modular Monolith)

    Container_Boundary(api, ".NET 10 Web API") {

        Boundary(iam, "IAM Module") {
            Component(authCtrl, "AuthenticationController", "ASP.NET Controller", "POST /sign-in — Authenticates user and returns JWT. GET /me — User profile.")
            Component(usersCtrl, "UsersController", "ASP.NET Controller", "User CRUD. ADMIN only. Activate, suspend, assign/revoke roles.")
            Component(rolesCtrl, "RolesController", "ASP.NET Controller", "Role CRUD. ADMIN only. Create roles, list, get by id.")
            Component(jwtSvc, "JwtTokenService", "Infrastructure Service", "Generates HS256 tokens. Reads claims from IConfiguration.")
            Component(bcrypt, "BCryptPasswordHasher", "Infrastructure Service", "Hashes passwords with cost factor 12. Verifies hash on login.")
            Component(userRepo, "UserRepository", "EF Core Repository", "Persistence for the User aggregate. Loads roles via Include.")
            Component(roleRepo, "RoleRepository", "EF Core Repository", "Persistence for the Role aggregate.")
        }

        Boundary(scraping, "Scraping Module") {
            Component(listCtrl, "ListsController", "ASP.NET Controller", "GET /lists/ofac — /lists/worldbank — /lists/icij — /lists/all. Requires Bearer JWT.")
            Component(rateLimitMW, "RateLimiterMiddleware", "ASP.NET Middleware", "Sliding window 20 req/min partitioned by client IP. Returns 429 if exceeded.")
            Component(orchSvc, "ScrapingOrchestrationService", "Infrastructure Service", "SearchSourceAsync(source, term) — single source. SearchAllAsync(term) — parallel across all sources. Results cached in IMemoryCache.")
            Component(ofacSrc, "OfacScrapingSource", "IScrapingSource", "Downloads and parses sdn.xml from treasury.gov. SourceName = OFAC.")
            Component(wbSrc, "WorldBankScrapingSource", "IScrapingSource", "Scrapes World Bank HTML table with HtmlAgilityPack. SourceName = WORLD_BANK.")
            Component(icijSrc, "IcijScrapingSource", "IScrapingSource", "Queries /api/nodes at offshoreleaks.icij.org. SourceName = ICIJ.")
        }

        Boundary(suppliers, "Suppliers Module") {
            Component(supplierCtrl, "SuppliersController", "ASP.NET Controller", "Full supplier CRUD. Validation via FluentValidation.")
            Component(screenCtrl, "ScreeningsController", "ASP.NET Controller", "POST /screenings/run — Runs screening. GET /screenings/{id} — Result by id. GET /screenings/supplier/{id} — History.")
            Component(supplierRepo, "SupplierRepository", "EF Core Repository", "Supplier CRUD. Pagination, sorting, filtering.")
            Component(screenRepo, "ScreeningResultRepository", "EF Core Repository", "Persists ScreeningResult aggregate. Audit trail.")
        }

        Boundary(shared, "Shared Kernel") {
            Component(pipeline, "MediatR Pipeline", "Behaviors", "Chain: LoggingBehavior -> ValidationBehavior -> Handler")
            Component(exHandler, "GlobalExceptionHandler", "ASP.NET IExceptionHandler", "Maps: ValidationException->400, DomainExceptions->409/404/401, Exception->500")
            Component(migrator, "DbUp Runner", "Startup Service", "On startup: executes embedded V00N__.sql scripts ordered by version. Idempotent.")
            Component(dbCtx, "AppDbContext", "EF Core DbContext", "Central context. ApplyConfigurationsFromAssembly. SnakeCaseNaming. Audit timestamps.")
            Component(unitOfWork, "UnitOfWork", "Infrastructure", "Wraps SaveChangesAsync. Allows transactional commits across repositories.")
        }
    }

    ContainerDb(db, "SQL Server 2022")
    Container(cache, "IMemoryCache")

    Rel(authCtrl, pipeline, "Send(SignInCommand | GetCurrentUserQuery)")
    Rel(usersCtrl, pipeline, "Send(User commands/queries)")
    Rel(rolesCtrl, pipeline, "Send(Role commands/queries)")
    Rel(pipeline, jwtSvc, "Used in SignInCommandHandler")
    Rel(pipeline, bcrypt, "Used in SignInCommandHandler")
    Rel(pipeline, userRepo, "Used in User handlers")
    Rel(pipeline, roleRepo, "Used in Role handlers")
    Rel(listCtrl, rateLimitMW, "Protected by")
    Rel(listCtrl, orchSvc, "Delegates search to")
    Rel(orchSvc, ofacSrc, "Dispatches to")
    Rel(orchSvc, wbSrc, "Dispatches to")
    Rel(orchSvc, icijSrc, "Dispatches to")
    Rel(orchSvc, cache, "Reads/writes results")
    Rel(screenCtrl, pipeline, "Send(RunScreeningCommand | GetScreeningResult queries)")
    Rel(pipeline, supplierRepo, "Used in supplier handlers")
    Rel(pipeline, screenRepo, "Used in screening handlers")
    Rel(pipeline, orchSvc, "Used in RunScreeningCommandHandler")
    Rel(supplierCtrl, pipeline, "Send(Supplier commands/queries)")
    Rel(userRepo, dbCtx, "Uses")
    Rel(roleRepo, dbCtx, "Uses")
    Rel(supplierRepo, dbCtx, "Uses")
    Rel(screenRepo, dbCtx, "Uses")
    Rel(dbCtx, db, "Generates SQL for")
    Rel(migrator, db, "Executes scripts at startup")
```

---

## Level 4 — Code Diagram (Shared Kernel Domain Model)

> Shows all foundational classes of the Shared Kernel (`Shared/Domain`, `Shared/Application`, `Shared/Infrastructure`, `Shared/Interfaces`) reused across all modules.
> Divided into six sub-sections: Domain Model, Repositories, Exceptions, Application, Infrastructure, and Interfaces.

---

### Shared Kernel — Domain Model

```mermaid
classDiagram
    class IAuditableEntity {
        <<interface>>
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +string? CreatedBy
        +string? UpdatedBy
    }

    class IDomainEvent {
        <<interface>>
        +string AggregateId
        +DateTime OccurredAt
        +string AggregateType
    }

    class AggregateRoot {
        +string Id
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +string? CreatedBy
        +string? UpdatedBy
        #RaiseDomainEvent(event) void
        +PopDomainEvents() IReadOnlyList~IDomainEvent~
    }

    class Entity~TId~ {
        +TId Id
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +string? CreatedBy
        +string? UpdatedBy
    }

    class ValueObject {
        <<abstract record>>
    }

    class Email {
        +string Value
        +Create(value) Email
    }

    IAuditableEntity <|.. AggregateRoot
    IAuditableEntity <|.. Entity~TId~
    ValueObject <|-- Email
```

---

### Shared Kernel — Domain Repositories

```mermaid
classDiagram
    class IBaseRepository~TEntity_TId~ {
        <<interface>>
        +AddAsync(entity) Task
        +FindByIdAsync(id) Task~TEntity~
        +Update(entity) void
        +Remove(entity) void
        +ListAsync() Task~IEnumerable~TEntity~~
    }

    class IUnitOfWork {
        <<interface>>
        +CompleteAsync(ct) Task
    }
```

---

### Shared Kernel — Domain Exceptions

```mermaid
classDiagram
    class DomainException {
        <<abstract>>
        +int ErrorNumber
        +string ErrorCode
    }

    class AuthenticationException {
        <<abstract>>
    }

    class AuthorizationException {
        <<abstract>>
    }

    class BusinessRuleViolationException {
        <<abstract>>
    }

    class EntityNotFoundException {
        <<abstract>>
        +string EntityName
        +string Field
        +string Value
    }

    class DomainValidationException {
        +IReadOnlyList~DomainFieldError~ FieldErrors
    }

    class DomainFieldError {
        +string Field
        +string Message
        +object? RejectedValue
    }

    class InvalidValueException {
        +string ValueObjectName
        +string? InvalidValue
        +string Reason
    }

    class ErrorCodes {
        <<static>>
        +ValidationFailed = 1000
        +InvalidValue = 1100
        +AuthenticationFailed = 2000
        +AuthorizationFailed = 3000
        +EntityNotFound = 4000
        +BusinessRuleViolation = 5000
        +InfrastructureError = 6000
    }

    DomainException <|-- AuthenticationException
    DomainException <|-- AuthorizationException
    DomainException <|-- BusinessRuleViolationException
    DomainException <|-- EntityNotFoundException
    DomainException <|-- DomainValidationException
    DomainException <|-- InvalidValueException
    DomainValidationException --> DomainFieldError
```

---

### Shared Kernel — Application

```mermaid
classDiagram
    class IEventListener~TEvent~ {
        <<interface>>
    }

    class IDomainEventNotification {
        <<interface>>
        +string AggregateId
        +DateTime OccurredAt
        +string AggregateType
    }

    note for IEventListener~TEvent~ "Wraps MediatR INotificationHandler.\nSemanticaly distinguishes event listeners\nfrom command/query handlers."
    note for IDomainEventNotification "Bridge: IDomainEvent + MediatR INotification.\nLives in Shared/Infrastructure/Events."

    IEventListener~TEvent~ --> IDomainEventNotification : handles
```

---

### Shared Kernel — Infrastructure

```mermaid
classDiagram
    class AppDbContext {
        -IHttpContextAccessor? _httpContextAccessor
        +SaveChangesAsync(ct) Task~int~
        #OnModelCreating(builder) void
    }

    class IHttpContextAccessor {
        <<interface>>
        +HttpContext? HttpContext
    }

    AppDbContext ..> IHttpContextAccessor : optional inject

    class BaseRepository~TEntity_TId~ {
        #AppDbContext Context
        +AddAsync(entity) Task
        +FindByIdAsync(id) Task~TEntity~
        +Update(entity) void
        +Remove(entity) void
        +ListAsync() Task~IEnumerable~TEntity~~
    }

    class UnitOfWork {
        +CompleteAsync(ct) Task
    }

    class PageRequest {
        +int Page
        +int Size
        +string? SortBy
        +string? SortDirection
        +DefaultPage = 0
        +DefaultSize = 20
        +MaxSize = 100
    }

    class SortConfiguration~T~ {
        <<abstract>>
        #AllowedSortFields IReadOnlyDictionary
        #DefaultSortField string
        #DefaultDescending bool
        +ApplySort(query, sortBy, sortDir) IOrderedQueryable~T~
        +DefaultSort(query) IOrderedQueryable~T~
    }

    class SpecificationComposer~T~ {
        <<abstract>>
        #ToSpec(value, mapper) Expression
        #ApplyAndFilters(query, specs) IQueryable~T~
        #ApplyOrFilters(query, specs) IQueryable~T~
    }

    class LoggingPipelineBehavior~TRequest_TResponse~ {
        +Handle(request, next, ct) Task~TResponse~
    }

    class ValidationPipelineBehavior~TRequest_TResponse~ {
        +Handle(request, next, ct) Task~TResponse~
    }

    class InfrastructureException {
        <<abstract>>
        +int ErrorNumber
        +string ErrorCode
    }

    class RequiredSeedDataMissingException {
        +string MissingData
    }

    BaseRepository~TEntity_TId~ --> AppDbContext : uses
    UnitOfWork --> AppDbContext : uses
    InfrastructureException <|-- RequiredSeedDataMissingException
```

---

### Shared Kernel — Interfaces (REST)

```mermaid
classDiagram
    class PageResponse~T~ {
        +List~T~ Content
        +PageMetadata Page
    }

    class PageMetadata {
        +int Number
        +int Size
        +long TotalElements
        +int TotalPages
        +bool First
        +bool Last
        +bool HasNext
        +bool HasPrevious
    }

    class ErrorResponse {
        +string Type
        +string Title
        +int Status
        +string? Instance
        +int ErrorNumber
        +string ErrorCode
        +string Message
        +DateTime Timestamp
        +IReadOnlyList~FieldError~? FieldErrors
    }

    class FieldError {
        +string Field
        +string Message
        +object? RejectedValue
    }

    PageResponse~T~ --> PageMetadata : page
    ErrorResponse --> FieldError
```

> **`[Shared/Domain]`**: `IAuditableEntity`, `IDomainEvent`, `AggregateRoot`, `Entity<TId>`, `ValueObject`, `Email`, `IBaseRepository<T,TId>`, `IUnitOfWork`, `DomainException` hierarchy, `ErrorCodes`
> **`[Shared/Application]`**: `IEventListener<TEvent>`
> **`[Shared/Infrastructure]`**: `IDomainEventNotification`, `AppDbContext`, `BaseRepository<T,TId>`, `UnitOfWork`, `PageRequest`, `SortConfiguration<T>`, `SpecificationComposer<T>`, `LoggingPipelineBehavior`, `ValidationPipelineBehavior`, `InfrastructureException` hierarchy
> **`[Shared/Interfaces]`**: `PageResponse<T>`, `PageMetadata`, `ErrorResponse`, `FieldError`

---

## Level 4 — Code Diagram (IAM Domain Model)

> Shows the main classes of the IAM module's domain model.
> `AggregateRoot` and `Email` live in `Shared/Domain` and are reused across modules.
> `Username`, `Password`, and `AccountStatus` are IAM-specific Value Objects.

```mermaid
classDiagram
    class AggregateRoot {
        +string Id
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +string? CreatedBy
        +string? UpdatedBy
    }

    class User {
        +Email Email
        +Username Username
        +Password Password
        +AccountStatus Status
        +int FailedLoginAttempts
        +DateTime? LastLoginAt
        +DateTime? LockedAt
        -List~Role~ _roles
        +IReadOnlyList~Role~ Roles
        +MaxFailedLoginAttempts = 5
        +Create(email, username, password) User
        +RecordSuccessfulLogin() void
        +RecordFailedLogin() void
        +Activate() void
        +Suspend() void
        +Delete() void
        -Lock() void
        +Unlock() void
        +AssignRole(role) void
        +RevokeRole(roleId) void
        +HasRole(roleName) bool
        +IsActive() bool
        +IsLocked() bool
        +IsSuspended() bool
        +EnsureCanLogin() void
    }

    class Role {
        +string Name
        +string Description
        +bool IsSystemRole
        +Create(name, description, isSystemRole) Role
        +UpdateDescription(description) void
    }

    class Username {
        +string Value
        +Create(value) Username
    }

    class Password {
        +string Hash
        +Create(hash) Password
    }

    class AccountStatus {
        <<enumeration>>
        Active
        Suspended
        Locked
        Deleted
    }

    AggregateRoot <|-- User
    AggregateRoot <|-- Role
    User "1" *-- "0..*" Role : has
    User --> Email
    User --> Username
    User --> Password
    User --> AccountStatus
```

> **`[Shared/Domain]`**: `AggregateRoot`, `Email`
> **`[IAM/Domain]`**: `Username`, `Password`, `AccountStatus` (Value Objects)

---

## Level 4 — Code Diagram (Scraping Module Domain Model)

> Shows the key classes of the Scraping module's domain and infrastructure layers.
> The Scraping module is **stateless** — it never writes to the database. Results are served from `IMemoryCache` only.
> `PageResponse<T>` and `PageMetadata` live in `Shared/Interfaces`. `PageRequest` lives in `Shared/Infrastructure`.

```mermaid
classDiagram
    class SearchResult {
        +int Hits
        +IReadOnlyList~RiskEntry~ Entries
        +Empty$ SearchResult
        +Merge(results)$ SearchResult
    }

    class RiskEntry {
        +string ListSource
        +string? Name
        +string? Address
        +string? Type
        +string? List
        +string[]? Programs
        +double? Score
        +string? Country
        +string? FromDate
        +string? ToDate
        +string? Grounds
        +string? Jurisdiction
        +string? LinkedTo
        +string? DataFrom
    }

    class IScrapingSource {
        <<interface>>
        +string SourceName
        +SearchAsync(term, ct) Task~SearchResult~
    }

    class OfacScrapingSource {
        +SourceName = "OFAC"
        +SearchAsync(term, ct) Task~SearchResult~
        -DownloadSdnXml() Task~XDocument~
        -ParseSdnEntries(doc, term) IEnumerable~RiskEntry~
    }

    class WorldBankScrapingSource {
        +SourceName = "WORLD_BANK"
        +SearchAsync(term, ct) Task~SearchResult~
        -ScrapeHtmlTable(html, term) IEnumerable~RiskEntry~
    }

    class IcijScrapingSource {
        +SourceName = "ICIJ"
        +SearchAsync(term, ct) Task~SearchResult~
    }

    class ScrapingOrchestrationService {
        -IEnumerable~IScrapingSource~ _sources
        -IMemoryCache _cache
        +SearchSourceAsync(sourceName, term) Task~SearchResult~
        +SearchAllAsync(term) Task~SearchResult~
    }

    IScrapingSource <|.. OfacScrapingSource
    IScrapingSource <|.. WorldBankScrapingSource
    IScrapingSource <|.. IcijScrapingSource
    ScrapingOrchestrationService --> "0..*" IScrapingSource : dispatches to
    SearchResult *-- "0..*" RiskEntry
```

> **`[Scraping/Domain]`**: `SearchResult`, `RiskEntry`
> **`[Scraping/Infrastructure]`**: `IScrapingSource`, `OfacScrapingSource`, `WorldBankScrapingSource`, `IcijScrapingSource`, `ScrapingOrchestrationService`

---

## Level 4 — Code Diagram (Suppliers Module Domain Model)

> Shows the key classes of the Suppliers module's domain model.
> `AggregateRoot` comes from `Shared/Domain`.
> `Supplier` and `ScreeningResult` are independent `AggregateRoot`s.

```mermaid
classDiagram
    class Supplier {
        +string LegalName
        +string CommercialName
        +string TaxId
        +string Country
        +string? ContactEmail
        +string? ContactPhone
        +string? Website
        +string? Address
        +decimal? AnnualBillingUsd
        +bool IsDeleted
        +RiskLevel RiskLevel
        +SupplierStatus Status
        +string? Notes
        +Create(legalName, commercialName, taxId, country, ...) Supplier
        +Update(legalName, commercialName, country, ...) void
        +ApplyScreeningResult(riskLevel) void
        +Approve() void
        +Reject() void
        +MarkUnderReview() void
        +Delete() void
        +IsActive() bool
    }

    class ScreeningResult {
        +string SupplierId
        +string SourcesQueried
        +DateTime ScreenedAt
        +RiskLevel RiskLevel
        +int TotalMatches
        +string? EntriesJson
        +Create(supplierId, sourcesQueried, riskLevel, totalMatches, entries) ScreeningResult
    }

    class RiskLevel {
        <<enumeration>>
        NONE
        LOW
        MEDIUM
        HIGH
    }

    class SupplierStatus {
        <<enumeration>>
        PENDING
        APPROVED
        REJECTED
        UNDER_REVIEW
    }

    AggregateRoot <|-- Supplier
    AggregateRoot <|-- ScreeningResult
    Supplier --> RiskLevel
    Supplier --> SupplierStatus
    ScreeningResult --> RiskLevel
```

> **`[Shared/Domain]`**: `AggregateRoot`

---

## C4 Model Notes

| Level | Audience | Tool |
|-------|----------|------|
| L1 Context | Business stakeholders | Mermaid (in README and this file) |
| L2 Container | Architects, Tech Leads | Mermaid (in README and this file) |
| L3 Component | Developers | Mermaid (in this file) |
| L4 Code | Developers | Mermaid classDiagram / IDE |

**Alternative tools for C4 diagrams:**
- [Structurizr Lite](https://structurizr.com/help/lite) — proprietary DSL, generates all levels
- [C4-PlantUML](https://github.com/plantuml-stdlib/C4-PlantUML) — for teams using PlantUML
- [draw.io / diagrams.net](https://draw.io) — with the C4 shape library
