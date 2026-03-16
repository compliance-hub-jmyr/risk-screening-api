# Changelog

Todos los cambios notables de este proyecto se documentan aquí.

Formato: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
Versionado: [Semantic Versioning](https://semver.org/spec/v2.0.0.html)

---

## [1.1.0] — 2026-03-16

### Agregado

- `[DB]` Script de migración `V007__seed_sample_suppliers.sql` — inserta proveedores de ejemplo para entornos de desarrollo y demo, cubriendo distintos niveles de riesgo, estados y países para ejercitar todos los escenarios de screening y filtrado.
- `[DOCS]` `CONTRIBUTING.md` — agregada sección de Configuración para Desarrollo Local con prerrequisitos, arranque de base de datos, configuración de entorno, instalación del browser de Playwright y comando para correr la API.

### Corregido

- `[SCR]` `IcijScrapingSource` — el término de búsqueda se trunca a 50 caracteres antes de construir la URL de ICIJ. El buscador de ICIJ tiene un límite de 50 caracteres en el input del browser; enviar términos más largos devolvía 0 resultados porque el SPA truncaba la consulta a mitad de palabra. Nombres de proveedores con más de 50 chars (ej. "Oceania International Consultants (BVI) Company Limited") ahora retornan correctamente sus coincidencias.
- `[SCR]` Browser Chromium de Playwright faltante tras actualización del paquete — `IcijScrapingSource` lanzaba `PlaywrightException: Executable doesn't exist` al iniciar. Causa raíz: el paquete NuGet `Microsoft.Playwright` fija una revisión específica del browser (`chromium-1208`) que debe descargarse manualmente en desarrollo local tras cada actualización. La imagen Docker de producción no se ve afectada (el browser se instala en tiempo de build).
- `[DOCS]` `README.md` — tabla de ADRs actualizada (agregados ADR-0013 a ADR-0018), estructura de directorios actualizada (agregados `docs/deployment/`, `docs/user-stories/`, `docs/api/`), y tabla de infraestructura actualizada (agregados Azure Container Apps, Docker Hub, GitHub Actions, Playwright).

---

## [1.0.0] — 2026-03-16

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
- `[INFRA]` Zona horaria configurable via `App:TimeZone` (`appsettings.json`) para timestamps de auditoría
- `[INFRA]` Dockerfile con build multi-stage, dependencias Chromium/Playwright, usuario non-root

#### Módulo IAM

- `[IAM]` Esquema de base de datos: tablas `roles`, `users`, `user_roles` con scripts de migración
- `[IAM]` Modelo de dominio: agregados `User` y `Role`, value objects `AccountStatus`, `Password`, `Username`
- `[IAM]` `POST /api/authentication/sign-in` — autenticación JWT con verificación de contraseña BCrypt
- `[IAM]` `GET /api/authentication/me` — obtener perfil del usuario autenticado
- `[IAM]` Seeder de datos para roles por defecto y usuario administrador
- `[IAM]` Especificación OpenAPI (`openapi-iam.yaml`)
- `[IAM]` Tests unitarios: `SignInCommandHandler`, `SignInCommandValidator`, `GetCurrentUserQueryHandler`

#### Módulo Scraping

- `[SCR]` Dos registros de `HttpClient` nombrados via `IHttpClientFactory` (OFAC, World Bank) con timeout y headers User-Agent; ICIJ usa `Microsoft.Playwright` (Chromium headless) en su lugar
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
- `[SCR]` `WorldBankScrapingSource` (`Infrastructure/Sources/`) — adaptador para Firmas Excluidas del Banco Mundial; web scraping en dos pasos: GET página HTML → extraer config del API del JavaScript → GET API JSON → filtrar y mapear
- `[SCR]` `WorldBankHtmlParser` (`Infrastructure/Sources/`) — parser unificado: `ExtractApiConfig()` scrapea tags `<script>` con `HtmlAgilityPack` para extraer URL del API + key; `ParseResults()` deserializa JSON `response.ZPROCSUPP`, filtra firmas por término de búsqueda (lógica OR multi-campo en nombre, dirección, ciudad, estado, país, motivos), combina componentes de dirección, mapea estado de inelegibilidad "Ongoing"/"Permanent" a `toDate`
- `[SCR]` `IcijScrapingSource` (`Infrastructure/Sources/`) — adaptador para ICIJ Offshore Leaks; scraping con headless browser via `Microsoft.Playwright` (Chromium) con flags stealth anti-detección para bypasear CloudFront WAF; renderiza la SPA JavaScript y parsea la tabla HTML de resultados
- `[SCR]` `IcijHtmlParser` (`Infrastructure/Sources/`) — parsea tabla HTML de resultados de búsqueda ICIJ (renderizada por Playwright) con `HtmlAgilityPack`; extrae Entity (→ Name), Jurisdiction, Linked To (→ LinkedTo), Data From (→ DataFrom)
- `[SCR]` Tests unitarios: `OfacScrapingSourceTests` (16 tests), `WorldBankScrapingSourceTests` (18 tests), `IcijScrapingSourceTests` (14 tests), `SearchRiskListsQueryHandlerTests` (10 tests), `SearchResultTests` (5 tests)
- `[SCR]` Infraestructura de tests: `RiskEntryMother`, `SearchResultMother`, `OfacHtmlMother`, `WorldBankJsonMother`, `IcijHtmlMother`, `FakeHttpMessageHandler`

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
- `[INFRA]` `SortConfiguration<T>` — `AllowedSortFields` cambió de `Expression<Func<T, object>>` a `LambdaExpression` para preservar el `TKey` real y evitar nodos `Convert()` de boxing que EF Core no puede traducir a SQL
- `[INFRA]` `SortConfiguration<T>` — `OrderBy`/`OrderByDescending` ahora se invocan via reflexión (`MethodInfo.MakeGenericMethod`) para pasar la expresión con el tipo correcto
- `[INFRA]` `SortConfiguration<T>` — agregada propiedad opcional `TiebreakerField` (añadida como `ThenBy ASC`) para paginación offset determinista
- `[SUP]` `SupplierSortConfiguration` — las expresiones de ordenamiento ahora referencian las propiedades value object directamente (ej. `x => x.LegalName`) en lugar de `.Value`; EF Core usa el converter `HasConversion` para SQL mientras `IComparable<T>` se usa en memoria (MockQueryable)
- `[SUP]` `SupplierSortConfiguration` — agregado `TiebreakerField = x => x.Id` para paginación estable
- `[SUP]` `LegalName`, `CommercialName`, `TaxId` — implementan `IComparable<T>` (delega a `string.Compare` sobre `.Value`) para soportar ordenamiento en memoria en tests unitarios
- `[SUP]` `CountryCode` — implementa `IComparable<CountryCode>` por la misma razón
- `[SUP]` `SupplierFilterComposer` — filtros de string cambiados de `EF.Functions.Like(x.LegalName.Value, ...)` / `EF.Property<string>(...)` a `EF.Functions.Like((string)x.LegalName, ...)`; el cast explícito invoca `implicit operator string` en memoria mientras EF Core traduce la propiedad directamente a la columna subyacente
- `[SUP]` `SupplierFilterComposer` — filtros de enum (`status`, `riskLevel`) ahora se parsean con `Enum.TryParse` antes de construir el árbol de expresión; valores inválidos se ignoran silenciosamente (no se aplica filtro)
- `[SUP]` `StringValueObjectConverter<T>` — `ConvertToProvider` ahora acepta tanto el tipo value object como `string` para corregir el `InvalidCastException` de EF Core 10 en la sanitización de filtros LIKE

### Corregido

- `[SUP]` `GET /api/suppliers?sortBy=legalName` retornaba HTTP 500 — EF Core no podía traducir `OrderBy` sobre una propiedad value object mapeada con `HasConversion` porque la expresión contenía un nodo `Convert()` de boxing. Corregido usando `LambdaExpression` y reflexión tipada para `Queryable.OrderBy<T, TKey>`.
- `[SUP]` `GET /api/suppliers?sortBy=status` retornaba HTTP 500 — misma causa raíz (boxing de enum). Corregido con el mismo enfoque de `LambdaExpression` + reflexión.
- `[SUP]` `GET /api/suppliers?legalName=Acme` retornaba HTTP 500 — EF Core no podía traducir `EF.Property<string>(x, "LegalName")` para predicados de filtro y también fallaba en la navegación `.Value` dentro de árboles de expresión. Corregido usando el cast explícito `(string)x.LegalName` que EF Core reduce al nombre de columna y MockQueryable resuelve via `implicit operator string`.
- `[SUP]` `GET /api/suppliers?status=approved` (minúsculas) no retornaba resultados — el filtro de enum comparaba `x.Status.ToString() == v` que EF Core no puede traducir. Corregido pre-parseando con `Enum.TryParse(ignoreCase: true)` y comparando `x.Status == valorParsed` directamente.

#### Despliegue & CI/CD

- `[DEPLOY]` Despliegue en Azure Container Apps con ingress externo y dominios HTTPS auto-provisionados
- `[DEPLOY]` Azure SQL Database (PaaS, tier Basic) con regla de firewall `AllowAzureServices`
- `[DEPLOY]` Guía de despliegue con comandos Azure CLI paso a paso (EN + ES)
- `[CI/CD]` GitHub Actions CI workflow (`ci.yml`) — build y test en push/PR a `main`/`develop`
- `[CI/CD]` GitHub Actions CD workflow (`cd.yml`) — Docker build, push a Docker Hub, deploy a Azure Container Apps via patrón `workflow_run`
- `[CI/CD]` GitHub environment `production` con gate de aprobación manual

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
- `[DOCS]` ADR-0014: Plataforma de contenedores — Azure Container Apps (EN + ES)
- `[DOCS]` ADR-0015: Registro de contenedores — Docker Hub (EN + ES)
- `[DOCS]` ADR-0016: Arquitectura de dos dominios — URLs públicas separadas (EN + ES)
- `[DOCS]` ADR-0017: Pipeline CI/CD — GitHub Actions con patrón workflow_run (EN + ES)
- `[DOCS]` ADR-0018: Estrategia de base de datos — Azure SQL Database PaaS (EN + ES)
- `[DOCS]` Diagramas de Arquitectura C4 — L1 Context, L2 Container, L3 Component, L4 Code, Despliegue (EN + ES)
- `[DOCS]` README — descripción del proyecto, stack tecnológico, tabla de ADRs (EN + ES)
- `[DOCS]` CONTRIBUTING — estrategia de ramas, convenciones de commits, flujo de PRs (EN + ES)

---

[1.1.0]: https://github.com/compliance-hub-jmyr/risk-screening-api/releases/tag/v1.1.0
[1.0.0]: https://github.com/compliance-hub-jmyr/risk-screening-api/releases/tag/v1.0.0
