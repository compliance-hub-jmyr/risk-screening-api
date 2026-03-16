# Diagramas de Arquitectura C4

> El modelo C4 (Context, Container, Component, Code) provee cuatro niveles de abstracción
> para documentar la arquitectura de software — desde el contexto del negocio hasta el código.
>
> **Referencias:**
> - C4 Model — Simon Brown: [c4model.com](https://c4model.com/)
> - Soporte Mermaid C4: [mermaid.js.org/syntax/c4](https://mermaid.js.org/syntax/c4.html)

---

## Nivel 1 — System Context Diagram

> Muestra el sistema desde la perspectiva del negocio: quiénes son los usuarios y con qué sistemas externos interactúa.

```mermaid
C4Context
    title System Context - Risk Screening API

    Person(officer, "Compliance Officer", "Personal EY que realiza debida diligencia de proveedores")
    Person(admin, "Platform Admin", "Administrador que gestiona usuarios y roles del sistema")

    System(platform, "Risk Screening API", "Provee búsqueda en listas de alto riesgo y una SPA de screening de proveedores")

    System_Ext(ofac, "OFAC SDN List", "US Treasury - Specially Designated Nationals and Blocked Persons List")
    System_Ext(worldBank, "World Bank Debarment", "Lista de firmas inhabilitadas en proyectos del Banco Mundial")
    System_Ext(icij, "ICIJ Offshore Leaks", "Base de datos financiera offshore (Panama Papers, Pandora Papers, etc.)")

    Rel(officer, platform, "Gestiona proveedores, ejecuta screening", "HTTPS + Bearer JWT")
    Rel(admin, platform, "Gestiona usuarios y roles", "HTTPS + Bearer JWT")
    Rel(platform, ofac, "Fetch bajo demanda de datos de sanciones", "HTTPS")
    Rel(platform, worldBank, "Fetch bajo demanda de datos de inhabilitaciones", "HTTPS")
    Rel(platform, icij, "Consulta bajo demanda de entidades offshore", "HTTPS")

    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
```

---

## Nivel 2 — Container Diagram

> Muestra los contenedores (procesos, bases de datos, frontends) que componen el sistema y cómo se comunican.

```mermaid
C4Container
    title Container Diagram - Risk Screening API

    Person(officer, "Compliance Officer")
    Person(admin, "Platform Admin")

    Container_Boundary(platform, "Risk Screening API") {
        Container(spa, "Angular SPA", "Angular 21, TypeScript, PrimeNG", "UI para gestión de proveedores, screening y administración")
        Container(api, ".NET Web API", ".NET 10, ASP.NET Core, MediatR, EF Core", "REST API: lógica de negocio, scraping, screening, IAM, CQRS")
        ContainerDb(db, "SQL Server 2022", "Relational Database", "Almacena usuarios, roles, proveedores, resultados de screening y audit logs")
        Container(cache, "In-Memory Cache", "Microsoft.Extensions.Caching.Memory", "Cachea resultados de scraping bajo demanda (OFAC, World Bank, ICIJ) y contadores de rate limiting")
    }

    System_Ext(ofac, "OFAC SDN")
    System_Ext(wb, "World Bank")
    System_Ext(icij, "ICIJ Offshore Leaks")

    Rel(officer, spa, "Usa", "HTTPS :443")
    Rel(admin, spa, "Administra usuarios", "HTTPS :443")
    Rel(spa, api, "GET/POST/PUT/DELETE /api/*", "HTTPS + Bearer JWT")
    Rel(api, db, "Lee y escribe entidades via EF Core", "SQL TCP :1433")
    Rel(api, cache, "Lee/escribe resultados de scraping y contadores de rate limit", "In-process")
    Rel(api, ofac, "HTTP GET bajo demanda — descarga SDN XML", "HTTPS")
    Rel(api, wb, "HTTP GET bajo demanda — parsea tabla HTML paginada", "HTTPS")
    Rel(api, icij, "Scraping con headless browser bajo demanda — renderiza SPA + parsea HTML", "HTTPS")
```

> **Nota:** La Fase 1 usa scraping bajo demanda (sin worker en background). Un worker `BackgroundService` de pre-población está planificado para la Fase 2 (solo OFAC y World Bank; ICIJ permanece bajo demanda de forma permanente).

---

## Nivel 3 — Component Diagram (Web API)

> Muestra los componentes internos del contenedor principal (.NET Web API) y sus responsabilidades.

```mermaid
C4Component
    title Component Diagram - .NET Web API (Modular Monolith)

    Container_Boundary(api, ".NET 10 Web API") {

        Boundary(iam, "IAM Module") {
            Component(authCtrl, "AuthenticationController", "ASP.NET Controller", "POST /sign-in — Autentica usuario y devuelve JWT. GET /me — Perfil del usuario.")
            Component(usersCtrl, "UsersController", "ASP.NET Controller", "CRUD de usuarios. Solo ADMIN. Activar, suspender, asignar/revocar roles.")
            Component(rolesCtrl, "RolesController", "ASP.NET Controller", "CRUD de roles. Solo ADMIN. Crear roles, listar, obtener por id.")
            Component(jwtSvc, "JwtTokenService", "Infrastructure Service", "Genera tokens HS256. Lee claims de IConfiguration.")
            Component(bcrypt, "BCryptPasswordHasher", "Infrastructure Service", "Hashea passwords con cost factor 12. Verifica hash en login.")
            Component(userRepo, "UserRepository", "EF Core Repository", "Persistencia del agregado User. Carga roles via Include.")
            Component(roleRepo, "RoleRepository", "EF Core Repository", "Persistencia del agregado Role.")
        }

        Boundary(scraping, "Scraping Module") {
            Component(listCtrl, "ListsController", "ASP.NET Controller", "GET /lists/ofac — /lists/worldbank — /lists/icij — /lists/all. Requiere Bearer JWT.")
            Component(rateLimitMW, "RateLimiterMiddleware", "ASP.NET Middleware", "Sliding window 20 req/min particionado por IP del cliente. Retorna 429 si se excede.")
            Component(orchSvc, "ScrapingOrchestrationService", "Infrastructure Service", "SearchSourceAsync(source, term) — fuente individual. SearchAllAsync(term) — paralelo en todas las fuentes. Resultados cacheados en IMemoryCache.")
            Component(ofacSrc, "OfacScrapingSource", "IScrapingSource", "Web scraping del formulario OFAC en sanctionssearch.ofac.treas.gov con HtmlAgilityPack (GET pagina + POST formulario). SourceName = OFAC.")
            Component(wbSrc, "WorldBankScrapingSource", "IScrapingSource", "Scrapea pagina HTML de World Bank con HtmlAgilityPack para extraer config del API, luego obtiene API JSON. SourceName = WORLD_BANK.")
            Component(icijSrc, "IcijScrapingSource", "IScrapingSource", "Scrapea pagina de busqueda ICIJ en offshoreleaks.icij.org usando Playwright headless Chromium (renderizado SPA + bypass WAF CloudFront) + parsing HTML con HtmlAgilityPack. SourceName = ICIJ.")
        }

        Boundary(suppliers, "Suppliers Module") {
            Component(supplierCtrl, "SuppliersController", "ASP.NET Controller", "CRUD completo de proveedores. Validación via FluentValidation.")
            Component(screenCtrl, "ScreeningsController", "ASP.NET Controller", "POST /screenings/run — Ejecuta screening. GET /screenings/{id} — Resultado por id. GET /screenings/supplier/{id} — Historial.")
            Component(supplierRepo, "SupplierRepository", "EF Core Repository", "CRUD de Supplier. Paginación, ordenamiento, filtrado.")
            Component(screenRepo, "ScreeningResultRepository", "EF Core Repository", "Persiste el agregado ScreeningResult. Audit trail.")
        }

        Boundary(shared, "Shared Kernel") {
            Component(pipeline, "MediatR Pipeline", "Behaviors", "Cadena: LoggingBehavior -> ValidationBehavior -> Handler")
            Component(exHandler, "GlobalExceptionHandler", "ASP.NET IExceptionHandler", "Mapea: ValidationException->400, DomainExceptions->409/404/401, Exception->500")
            Component(migrator, "DbUp Runner", "Startup Service", "Al inicio: ejecuta scripts embebidos V00N__.sql ordenados por versión. Idempotente.")
            Component(dbCtx, "AppDbContext", "EF Core DbContext", "Contexto central. ApplyConfigurationsFromAssembly. SnakeCaseNaming. Audit timestamps.")
            Component(unitOfWork, "UnitOfWork", "Infrastructure", "Envuelve SaveChangesAsync. Permite commits transaccionales entre repositorios.")
        }
    }

    ContainerDb(db, "SQL Server 2022")
    Container(cache, "IMemoryCache")

    Rel(authCtrl, pipeline, "Send(SignInCommand | GetCurrentUserQuery)")
    Rel(usersCtrl, pipeline, "Send(User commands/queries)")
    Rel(rolesCtrl, pipeline, "Send(Role commands/queries)")
    Rel(pipeline, jwtSvc, "Usa en SignInCommandHandler")
    Rel(pipeline, bcrypt, "Usa en SignInCommandHandler")
    Rel(pipeline, userRepo, "Usa en User handlers")
    Rel(pipeline, roleRepo, "Usa en Role handlers")
    Rel(listCtrl, rateLimitMW, "Protegido por")
    Rel(listCtrl, orchSvc, "Delega búsqueda a")
    Rel(orchSvc, ofacSrc, "Despacha a")
    Rel(orchSvc, wbSrc, "Despacha a")
    Rel(orchSvc, icijSrc, "Despacha a")
    Rel(orchSvc, cache, "Lee/escribe resultados")
    Rel(screenCtrl, pipeline, "Send(RunScreeningCommand | GetScreeningResult queries)")
    Rel(pipeline, supplierRepo, "Usa en supplier handlers")
    Rel(pipeline, screenRepo, "Usa en screening handlers")
    Rel(pipeline, orchSvc, "Usa en RunScreeningCommandHandler")
    Rel(supplierCtrl, pipeline, "Send(Supplier commands/queries)")
    Rel(userRepo, dbCtx, "Usa")
    Rel(roleRepo, dbCtx, "Usa")
    Rel(supplierRepo, dbCtx, "Usa")
    Rel(screenRepo, dbCtx, "Usa")
    Rel(dbCtx, db, "Genera SQL para")
    Rel(migrator, db, "Ejecuta scripts al startup")
```

---

## Nivel 4 — Code Diagram (Modelo de Dominio — Shared Kernel)

> Muestra todas las clases fundacionales del Shared Kernel (`Shared/Domain`, `Shared/Application`, `Shared/Infrastructure`, `Shared/Interfaces`) reutilizadas por todos los módulos.
> Dividido en seis sub-secciones: Modelo de Dominio, Repositorios, Excepciones, Aplicación, Infraestructura e Interfaces.

---

### Shared Kernel — Modelo de Dominio

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

### Shared Kernel — Repositorios de Dominio

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

### Shared Kernel — Excepciones de Dominio

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

### Shared Kernel — Aplicación

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

    note for IEventListener~TEvent~ "Envuelve INotificationHandler de MediatR.\nDistingue semánticamente los listeners\nde los command/query handlers."
    note for IDomainEventNotification "Puente: IDomainEvent + INotification de MediatR.\nVive en Shared/Infrastructure/Events."

    IEventListener~TEvent~ --> IDomainEventNotification : maneja
```

---

### Shared Kernel — Infraestructura

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

    AppDbContext ..> IHttpContextAccessor : inyección opcional

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

    BaseRepository~TEntity_TId~ --> AppDbContext : usa
    UnitOfWork --> AppDbContext : usa
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

## Nivel 4 — Code Diagram (Modelo de Dominio IAM)

> Muestra las clases principales del modelo de dominio del módulo IAM.
> `AggregateRoot` y `Email` viven en `Shared/Domain` y son reutilizados por todos los módulos.
> `Username`, `Password` y `AccountStatus` son Value Objects propios del módulo IAM.

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

## Nivel 4 — Code Diagram (Modelo de Dominio — Módulo Scraping)

> Muestra las clases clave de las capas de dominio e infraestructura del módulo Scraping.
> El módulo Scraping es **stateless** — nunca escribe en la base de datos. Los resultados se sirven únicamente desde `IMemoryCache`.
> `PageResponse<T>` y `PageMetadata` viven en `Shared/Interfaces`. `PageRequest` vive en `Shared/Infrastructure`.

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
    ScrapingOrchestrationService --> "0..*" IScrapingSource : despacha a
    SearchResult *-- "0..*" RiskEntry
```

> **`[Scraping/Domain]`**: `SearchResult`, `RiskEntry`
> **`[Scraping/Infrastructure]`**: `IScrapingSource`, `OfacScrapingSource`, `WorldBankScrapingSource`, `IcijScrapingSource`, `ScrapingOrchestrationService`

---

## Nivel 4 — Code Diagram (Modelo de Dominio — Módulo Suppliers)

> Muestra las clases clave del modelo de dominio del módulo Suppliers.
> `AggregateRoot` proviene de `Shared/Domain`.
> `Supplier` y `ScreeningResult` son `AggregateRoot`s independientes.

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

## Diagrama de Despliegue — Azure Container Apps

> Muestra la topologia fisica de despliegue en Azure: Container Apps para frontend y backend, Azure SQL Database para persistencia, Docker Hub como registro de contenedores.
>
> Para instrucciones paso a paso, ver [Guia de Despliegue Azure](../deployment/azure-deployment.es.md).

```mermaid
C4Deployment
    title Deployment Diagram - Risk Screening (Azure Container Apps)

    Deployment_Node(github, "GitHub", "Source Control & CI/CD") {
        Deployment_Node(actions, "GitHub Actions", "CI/CD Pipeline") {
            Container(ci, "CI Workflow", "Build & Test", "Se activa en push/PR a main")
            Container(cd, "CD Workflow", "Docker Push + Deploy", "Se activa cuando CI pasa en main")
        }
    }

    Deployment_Node(dockerHub, "Docker Hub", "Container Registry") {
        Container(apiImage, "riskscreening-api", "Docker Image", "jhosepmyr/riskscreening-api:latest")
        Container(webImage, "riskscreening-web", "Docker Image", "jhosepmyr/riskscreening-web:latest")
    }

    Deployment_Node(azure, "Azure", "Cloud Platform") {
        Deployment_Node(rg, "rg-riskscreening-prod", "Resource Group") {
            Deployment_Node(env, "riskscreening-env", "Container Apps Environment") {
                Container(api, "riskscreening-api", "Azure Container App", ".NET 10 Web API — 1 vCPU, 2 GB — ingress externo :8080")
                Container(web, "riskscreening-web", "Azure Container App", "Angular SPA en nginx — 0.5 vCPU, 1 GB — ingress externo :8080")
            }
            Deployment_Node(sqlNode, "Azure SQL", "Base de Datos PaaS") {
                ContainerDb(db, "RiskScreeningDb", "Azure SQL Database", "Tier Basic — riskscreening-sqlserver.database.windows.net")
            }
        }
    }

    Rel(cd, apiImage, "Sube imagen")
    Rel(cd, webImage, "Sube imagen")
    Rel(api, apiImage, "Pull desde", "Docker Hub")
    Rel(web, webImage, "Pull desde", "Docker Hub")
    Rel(cd, api, "az containerapp update")
    Rel(cd, web, "az containerapp update")
    Rel(web, api, "HTTPS cross-origin", "CORS")
    Rel(api, db, "SQL TCP :1433", "EF Core + DbUp")
```

> **Puntos clave:**
> - Ambos contenedores tienen `--ingress external` con dominios HTTPS separados `*.azurecontainerapps.io`
> - CORS se configura en el backend via la variable de entorno `Cors__AllowedOrigins__0`
> - CI/CD usa el patron `workflow_run`: CD se activa solo despues de que CI pasa en `main`

---

## Notas sobre el Modelo C4

| Nivel | Audiencia | Herramienta |
|-------|----------|-------------|
| L1 Context | Stakeholders de negocio | Mermaid (en README y este archivo) |
| L2 Container | Arquitectos, Tech Leads | Mermaid (en README y este archivo) |
| L3 Component | Desarrolladores | Mermaid (en este archivo) |
| L4 Code | Desarrolladores | Mermaid classDiagram / IDE |

**Herramientas alternativas para diagramas C4:**
- [Structurizr Lite](https://structurizr.com/help/lite) — DSL propio, genera todos los niveles
- [C4-PlantUML](https://github.com/plantuml-stdlib/C4-PlantUML) — para equipos que usan PlantUML
- [draw.io / diagrams.net](https://draw.io) — con la shape library de C4
