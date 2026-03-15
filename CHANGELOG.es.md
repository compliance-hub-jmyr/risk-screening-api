# Changelog

Todos los cambios notables de este proyecto se documentan aquí.

Formato: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
Versionado: [Semantic Versioning](https://semver.org/spec/v2.0.0.html)

---

## [Unreleased]

### Agregado

#### Infraestructura Compartida

- `[INFRA]` Modelo de dominio base: `AggregateRoot`, `Entity`, `ValueObject`, `IAuditableEntity`, `IDomainEvent`
- `[INFRA]` Value objects compartidos: `Email`
- `[INFRA]` Patrón repositorio base: `BaseRepository`, `IBaseRepository`, `IUnitOfWork`
- `[INFRA]` `AppDbContext` con auditoría automática (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`)
- `[INFRA]` `DatabaseMigrator` para migraciones SQL con DbUp al iniciar
- `[INFRA]` Comportamientos de pipeline MediatR: `LoggingPipelineBehavior`, `ValidationPipelineBehavior`
- `[INFRA]` Manejadores centralizados de excepciones: `DomainExceptionHandler`, `ValidationExceptionHandler`, `GlobalExceptionHandler`
- `[INFRA]` Recursos API compartidos: `ErrorResponse`, `PageResponse<T>` con `PageMetadata`
- `[INFRA]` Infraestructura de consultas: `SpecificationComposer`, `SortConfiguration`, `PageableExtensions`, `PageRequest`
- `[INFRA]` Documentación OpenAPI: esquema de seguridad JWT, `StandardResponsesOperationFilter`, `ErrorResponseDocumentFilter`
- `[INFRA]` Versionado de API mediante header `Api-Version`
- `[INFRA]` Política CORS con orígenes permitidos configurables
- `[INFRA]` Logging estructurado con Serilog y `CorrelationIdMiddleware` para trazabilidad de requests
- `[INFRA]` Rate limiting por IP via `AspNetCoreRateLimit` con reglas escalonadas: sign-in (5 req/min), listas de scraping (20 req/min), API general (100 req/min)
- `[INFRA]` `RateLimitResponseMiddleware` — reescribe respuestas 429 al formato estándar `ErrorResponse` (RFC 7807) con código de error `RATE_LIMIT_EXCEEDED`
- `[INFRA]` Docker Compose con servicios API + SQL Server
- `[INFRA]` Convención snake_case para mapeo de entidades en EF Core

#### Módulo IAM

- `[IAM]` Esquema de base de datos: tablas `roles`, `users`, `user_roles` con scripts de migración
- `[IAM]` Modelo de dominio: agregados `User` y `Role`, value objects `AccountStatus`, `Password`, `Username`
- `[IAM]` `POST /api/authentication/sign-in` — autenticación JWT con verificación de contraseña BCrypt
- `[IAM]` `GET /api/authentication/me` — obtener perfil del usuario autenticado
- `[IAM]` Seeder de datos para roles por defecto y usuario administrador
- `[IAM]` Especificación OpenAPI (`openapi-iam.yaml`)
- `[IAM]` Tests unitarios: `SignInCommandHandler`, `SignInCommandValidator`, `GetCurrentUserQueryHandler`

#### Módulo Scraping

- `[SCR]` Tres registros de `HttpClient` nombrados via `IHttpClientFactory` (OFAC, World Bank, ICIJ) con timeout y headers User-Agent
- `[SCR]` Registro de `IMemoryCache` para cache bajo demanda de resultados de scraping (TTL 10 min por fuente por término)
- `[SCR]` Modelo de dominio: value objects `RiskEntry`, `SearchResult` (`Domain/Model/ValueObjects/`), query CQRS `SearchRiskListsQuery` (`Domain/Model/Queries/`)
- `[SCR]` Puerto de aplicación `IScrapingSource` (`Application/Ports/`) — interfaz extensible para adaptadores de fuentes de scraping
- `[SCR]` `SearchRiskListsQueryHandler` (`Application/Search/`) — handler CQRS de MediatR con cache `IMemoryCache` y ejecución paralela via `Task.WhenAll`
- `[SCR]` `OfacScrapingSource` (`Infrastructure/Sources/`) — adaptador para web scraping real de `https://sanctionssearch.ofac.treas.gov/` (extracción de ViewState ASP.NET + POST de formulario)
- `[SCR]` `OfacHtmlParser` (`Infrastructure/Sources/`) — helper estático para extracción de datos del formulario HTML de OFAC y parsing de tabla de resultados con `HtmlAgilityPack`
- `[SCR]` `GET /api/lists/search?q={term}&sources=ofac&sources=worldbank` — endpoint unificado para búsquedas en listas de riesgo; parámetro `sources` opcional (query params repetidos: ofac, worldbank, icij); cuando se omite, consulta todas las fuentes
- `[SCR]` `SearchRiskListsQueryValidator` (`Application/Search/`) — validador FluentValidation ejecutado automáticamente por `ValidationPipelineBehavior`; valida que `q` no esté vacío y que los valores de `sources` estén en la whitelist
- `[SCR]` `ScrapingResponseMapper` (`Interfaces/REST/Mappers/Response/`) — mapea objetos de dominio `SearchResult` a DTOs `ScrapingResponse`
- `[SCR]` DTOs de respuesta: `ScrapingResponse`, `RiskEntryResponse` con anotaciones Swagger
- `[SCR]` Agrupación de API en Swagger: "Lists Module" agregado al dropdown
- `[SCR]` Especificación OpenAPI (`openapi-lists.yaml`)
- `[SCR]` Archivo HTTP de pruebas (`RiskScreening.API.http`) — requests de búsqueda para todas las fuentes, fuentes individuales, casos de error
- `[SCR]` Tests unitarios: `OfacScrapingSourceTests` (16 tests), `SearchRiskListsQueryHandlerTests` (10 tests), `SearchResultTests` (5 tests)
- `[SCR]` Infraestructura de tests: `RiskEntryMother`, `SearchResultMother`, `OfacHtmlMother`, `FakeHttpMessageHandler`

#### Módulo Suppliers

- `[SUP]` Esquema de base de datos: tablas `suppliers` y `screening_results` con scripts de migración
- `[SUP]` Modelo de dominio: agregados `Supplier` y `ScreeningResult`
- `[SUP]` Value objects: `LegalName`, `CommercialName`, `TaxId`, `CountryCode`, `PhoneNumber`, `WebsiteUrl`, `SupplierAddress`, `AnnualBilling`, `RiskLevel`, `SupplierStatus`, `SupplierId`
- `[SUP]` `POST /api/suppliers` — crear proveedor con validación completa y verificación de unicidad de TaxId
- `[SUP]` `GET /api/suppliers` — listar proveedores con paginación, filtros (legalName, commercialName, taxId, country, status, riskLevel) y ordenamiento configurable
- `[SUP]` `GET /api/suppliers/{id}` — obtener proveedor por ID
- `[SUP]` `PUT /api/suppliers/{id}` — actualizar proveedor con validación completa y verificación de unicidad de TaxId
- `[SUP]` `DELETE /api/suppliers/{id}` — eliminación lógica (soft-delete) de proveedor
- `[SUP]` `SupplierFilterComposer` para predicados de consulta componibles en EF Core
- `[SUP]` `SupplierSortConfiguration` con campos de ordenamiento permitidos (default: `updatedAt DESC`)
- `[SUP]` Especificación OpenAPI (`openapi-suppliers.yaml`)
- `[SUP]` Agrupación de API en Swagger por módulo (All, IAM, Suppliers) con dropdown en Swagger UI
- `[SUP]` `SchemaExamplesFilter` para valores de ejemplo realistas en los schemas de Swagger
- `[SUP]` Tests unitarios: `CreateSupplierCommandHandler`, `CreateSupplierCommandValidator`, `UpdateSupplierCommandHandler`, `UpdateSupplierCommandValidator`, `GetAllSuppliersQueryHandler`, `GetSupplierByIdQueryHandler`
- `[SUP]` Infraestructura de tests: `SupplierBuilder`, `SupplierMother`, `CreateSupplierCommandMother`, `UpdateSupplierCommandMother` con Bogus

### Cambiado

- `[INFRA]` Claim del actor en `AppDbContext` cambiado de `ClaimTypes.Name` a `ClaimTypes.NameIdentifier` (almacena ID del usuario en vez del username)
- `[SUP]` `CountryCode.ValidCodes` hecho público para uso compartido entre código de producción y tests
- `[SUP]` `SupplierResponse` ahora incluye campos de auditoría `createdBy` y `updatedBy`

#### Documentación

- `[DOCS]` ADR-0001: Decisión de arquitectura Modular Monolith (EN + ES)
- `[DOCS]` ADR-0002: CQRS con MediatR y pipeline behaviors (EN + ES)
- `[DOCS]` ADR-0003: Autenticación JWT — enfoque API Key evaluado y descartado (EN + ES)
- `[DOCS]` ADR-0004: Scripts SQL versionados con DbUp, nomenclatura estilo Flyway (EN + ES)
- `[DOCS]` ADR-0005: Rate limiting con AspNetCoreRateLimit por IP del cliente (EN + ES)
- `[DOCS]` ADR-0006: Web scraping — bajo demanda con IMemoryCache, worker en background en Fase 2 (EN + ES)
- `[DOCS]` ADR-0007: Framework frontend — Angular 21 + PrimeNG (EN + ES)
- `[DOCS]` ADR-0008: Tecnología de cache — IMemoryCache Fase 1, ruta de migración a Redis (EN + ES)
- `[DOCS]` ADR-0009: Estrategia de paginación — offset-based con wrapper PageResponse (EN + ES)
- `[DOCS]` ADR-0010: Manejo centralizado de errores — cadena IExceptionHandler + RFC 7807 (EN + ES)
- `[DOCS]` ADR-0011: Campos de auditoría — IAuditableEntity + intercepción de SaveChangesAsync (EN + ES)
- `[DOCS]` ADR-0012: Estrategia de versionado de API — header-based con header `Api-Version` (EN + ES)
- `[DOCS]` ADR-0013: Logging estructurado con Serilog — templates optimizados para Loki (EN + ES)
- `[DOCS]` Diagramas de Arquitectura C4 — L1 Context, L2 Container, L3 Component, L4 Code (EN + ES)
- `[DOCS]` README — descripción del proyecto, stack tecnológico, tabla de ADRs (EN + ES)
- `[DOCS]` CONTRIBUTING — estrategia de ramas, convenciones de commits, flujo de PRs (EN + ES)

---

[Unreleased]: https://github.com/compliance-hub-jmyr/risk-screening-api/commits/main
