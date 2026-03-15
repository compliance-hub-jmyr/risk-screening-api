# User Stories — Scraping Module

> **Format:** Title / Description / Deliverable / Dependencies / Acceptance Criteria (BDD Given/When/Then).
> **Task tags:** `[BE-DOMAIN]` `[BE-APP]` `[BE-INFRA]` `[BE-INTERFACES]` `[BE-DB]` `[BE-TEST]` `[DOCS]`
>
> **Module nature:** The Scraping module is **stateless** — no database tables, no EF Core entities. All data is fetched live from external sources and optionally cached in `IMemoryCache`. There is no `TS-SCR-000` bootstrap story because the module only requires HTTP clients and cache registration.

---

## Epic: Risk List Search (Direct API)

---

### US-SCR-001: Search the OFAC SDN list

**Title:** Query the OFAC Specially Designated Nationals list

**Description:**
As a compliance officer, I want to search the OFAC Specially Designated Nationals (SDN) list by name, so that I can quickly check whether a person or entity appears on US Treasury sanctions.

**Deliverable:**
Endpoint `GET /api/lists/ofac?q={term}` que descarga el feed XML del SDN, filtra entradas cuyo nombre contiene el término (case-insensitive), y retorna un `ScrapingResponse` con el conteo de matches y las entradas coincidentes. Resultados cacheados 10 minutos por término.

**Dependencies:**
- `US-IAM-001`: JWT authentication
- `ScrapingModuleExtensions.AddScrapingModule()` registrado — HTTP client e `IMemoryCache` configurados

**Priority:** High | **Estimate:** 3 SP | **Status:** Updated (v0.5.1)

#### Tasks

- `[BE-DOMAIN]` `RiskEntry` record — unified type for all three sources:
  - `ListSource` string NOT NULL — source discriminator (`"OFAC"`, `"WORLD_BANK"`, `"ICIJ"`)
  - `Name` string? — entity name (OFAC name, World Bank firm name, ICIJ caption)
  - `Address` string? — physical address (OFAC, World Bank)
  - `Type` string? — entity type (OFAC)
  - `List` string? — sanctions list name (OFAC)
  - `Programs` string[]? — sanctions programs (OFAC)
  - `Score` double? — match confidence score (OFAC)
  - `Country` string? — country (World Bank)
  - `FromDate` string? — ineligibility start date (World Bank)
  - `ToDate` string? — ineligibility end date (World Bank)
  - `Grounds` string? — debarment grounds (World Bank)
  - `Jurisdiction` string? — legal jurisdiction (ICIJ)
  - `LinkedTo` string? — linked entities (ICIJ)
  - `DataFrom` string? — source dataset name (ICIJ)
  - Fields not applicable to a source are left as `null`
- `[BE-DOMAIN]` `SearchResult` record con `Hits`, `Entries` (IReadOnlyList<RiskEntry>), static `Empty`, y factory `Merge`
- `[BE-INFRA]` `IScrapingSource` interface con `SourceName` y `SearchAsync(term, ct)`
- `[BE-INFRA]` `OfacScrapingSource` — downloads SDN ZIP from `https://sdn.ofac.treas.gov/SDN_XML.zip`, decompresses in memory, parses XML with `XDocument`, case-insensitive name match; maps to `RiskEntry` with `ListSource = "OFAC"`, `Name`, `Address`, `Type`, `List`, `Programs`, `Score`; returns `SearchResult.Empty` on any failure
- `[BE-INFRA]` `ScrapingOrchestrationService.SearchSourceAsync("ofac", term)` — cachea por `scraping:ofac:{term}` por 10 min
- `[BE-INTERFACES]` `ListsController.SearchOfac` — requiere `q`; despacha a `SearchSourceAsync`; sujeto a rate limiting (20 req/min por IP)
- `[BE-TEST]` Unit test: matching entries returned with all OFAC fields, no matches returns empty, missing `q` returns 400

#### Acceptance Criteria

**Scenario 1: Matches found**
- Given I am authenticated
- And I send `GET /api/lists/ofac?q=john doe`
- When the request is processed
- Then I receive HTTP 200 with `{ hits: N, entries: [...] }`
- And each entry includes `listSource = "OFAC"`, `name`, `address`, `type`, `list`, `programs`, `score`

**Scenario 2: No matches**
- Given the search term does not appear in the OFAC SDN list
- When I send `GET /api/lists/ofac?q=unknown entity xyz`
- Then I receive HTTP 200 with `{ hits: 0, entries: [] }`

**Scenario 3: Missing search term**
- Given I send `GET /api/lists/ofac` without the `q` parameter
- When the request reaches the controller
- Then I receive HTTP 400 Bad Request

**Scenario 4: Rate limit exceeded**
- Given I have sent more than 20 requests in a single minute from the same IP
- When I send an additional request
- Then I receive HTTP 429 Too Many Requests

**Scenario 5: OFAC source unavailable**
- Given the OFAC ZIP endpoint is unreachable
- When I send the request
- Then I receive HTTP 200 with `{ hits: 0, entries: [] }` (fault-tolerant — `SearchResult.Empty` returned)

**Scenario 6: Cache hit**
- Given a previous request for the same term was made within the last 10 minutes
- When I send `GET /api/lists/ofac?q=john doe` again
- Then I receive HTTP 200 with the cached result (no external HTTP call made)

---

### US-SCR-002: Search the World Bank debarred firms list

**Title:** Query the World Bank Debarred Firms registry

**Description:**
As a compliance officer, I want to search the World Bank's list of debarred and cross-debarred firms, so that I can identify suppliers that have been sanctioned from participating in World Bank-funded projects.

**Deliverable:**
Endpoint `GET /api/lists/worldbank?q={term}` que hace scraping de la página HTML del World Bank usando `HtmlAgilityPack`, extrae filas coincidentes de la tabla, y retorna un `ScrapingResponse`. Resultados cacheados 10 minutos por término.

**Dependencies:**
- `US-SCR-001` (misma infraestructura, mismo patrón)

**Priority:** High | **Estimate:** 3 SP | **Status:** Updated (v0.5.1)

#### Tasks

- `[BE-INFRA]` `WorldBankScrapingSource` — fetches `https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms?srchTerm={term}`, parses HTML table with `HtmlAgilityPack`, extracts firm name (mapped to `Name`), `Address`, `Country`, `FromDate`, `ToDate`, `Grounds`; maps to `RiskEntry` with `ListSource = "WORLD_BANK"`; returns `SearchResult.Empty` on failure
- `[BE-INFRA]` `ScrapingOrchestrationService.SearchSourceAsync("worldbank", term)` — cachea por `scraping:worldbank:{term}` por 10 min
- `[BE-INTERFACES]` `ListsController.SearchWorldBank` — requiere `q`; sujeto a rate limiting
- `[BE-TEST]` Unit test: matching rows returned with all World Bank fields, HTML parse failure returns empty

#### Acceptance Criteria

**Scenario 1: Matches found**
- Given I am authenticated
- And I send `GET /api/lists/worldbank?q=acme corp`
- When the request is processed
- Then I receive HTTP 200 with `{ hits: N, entries: [...] }`
- And each entry includes `listSource = "WORLD_BANK"`, `name`, `address`, `country`, `fromDate`, `toDate`, `grounds`

**Scenario 2: No matches**
- Given the search term does not appear in the World Bank debarred firms table
- When I send `GET /api/lists/worldbank?q=unknown entity xyz`
- Then I receive HTTP 200 with `{ hits: 0, entries: [] }`

**Scenario 3: World Bank source unavailable**
- Given the World Bank page is unreachable or returns unexpected HTML
- When I send the request
- Then I receive HTTP 200 with `{ hits: 0, entries: [] }` (fault-tolerant)

**Scenario 4: Rate limit exceeded**
- Given I have exceeded 20 requests per minute from the same IP
- When I send an additional request
- Then I receive HTTP 429 Too Many Requests

---

### US-SCR-003: Search the ICIJ Offshore Leaks database

**Title:** Query the ICIJ Offshore Leaks JSON API

**Description:**
As a compliance officer, I want to search the ICIJ Offshore Leaks database, so that I can identify suppliers linked to offshore entities named in the Panama Papers, Paradise Papers, or similar investigations.

**Deliverable:**
Endpoint `GET /api/lists/icij?q={term}` que consulta la API JSON pública de ICIJ, deserializa el array `nodes`, y retorna un `ScrapingResponse`. Resultados cacheados 10 minutos por término.

**Dependencies:**
- `US-SCR-001` (misma infraestructura, mismo patrón)

**Priority:** High | **Estimate:** 2 SP | **Status:** Updated (v0.5.1)

#### Tasks

- `[BE-INFRA]` `IcijScrapingSource` — fetches `https://offshoreleaks.icij.org/api/nodes?q={term}`, deserializes `nodes` array with `System.Text.Json` into internal `IcijNode` DTOs; maps to `RiskEntry` with:
  - `ListSource = "ICIJ"`
  - `Name` (node caption, fallback to name field)
  - `Jurisdiction`, `LinkedTo`, `DataFrom`
  - All OFAC/World Bank fields remain `null`
  - Returns `SearchResult.Empty` on failure
- `[BE-INFRA]` `ScrapingOrchestrationService.SearchSourceAsync("icij", term)` — caches by `scraping:icij:{term}` for 10 min
- `[BE-INTERFACES]` `ListsController.SearchIcij` — requires `q`; subject to rate limiting
- `[BE-TEST]` Unit test: nodes deserialized correctly with `name` field, empty `nodes` array returns empty

#### Acceptance Criteria

**Scenario 1: Matches found**
- Given I am authenticated
- And I send `GET /api/lists/icij?q=mossack fonseca`
- When the request is processed
- Then I receive HTTP 200 with `{ hits: N, entries: [...] }`
- And each entry includes `listSource = "ICIJ"`, `name`, `jurisdiction`, `linkedTo`, `dataFrom`

**Scenario 2: No matches**
- Given the search term returns an empty `nodes` array from the ICIJ API
- When I send the request
- Then I receive HTTP 200 with `{ hits: 0, entries: [] }`

**Scenario 3: ICIJ API unavailable**
- Given the ICIJ API is unreachable
- When I send the request
- Then I receive HTTP 200 with `{ hits: 0, entries: [] }` (fault-tolerant)

**Scenario 4: Rate limit exceeded**
- Given I have exceeded 20 requests per minute from the same IP
- When I send an additional request
- Then I receive HTTP 429 Too Many Requests

---

### US-SCR-004: Search all risk lists simultaneously

**Title:** Parallel query across OFAC, World Bank, and ICIJ

**Description:**
As a compliance officer, I want to search all three risk lists with a single request, so that I get a consolidated view of risk across all sources without making three separate API calls.

**Deliverable:**
Endpoint `GET /api/lists/all?q={term}` que ejecuta las tres consultas en paralelo via `Task.WhenAll`, mergea los resultados con `SearchResult.Merge`, y retorna un único `ScrapingResponse` con el total de `hits` y la lista unificada de entradas. Cada resultado por fuente se cachea independientemente.

**Dependencies:**
- `US-SCR-001`, `US-SCR-002`, `US-SCR-003`

**Priority:** Critical | **Estimate:** 2 SP | **Status:** Implemented (v0.5.0)

#### Tasks

- `[BE-INFRA]` `ScrapingOrchestrationService.SearchAllAsync(term)` — llama todas las instancias `IScrapingSource` registradas en paralelo via `Task.WhenAll`, mergea con `SearchResult.Merge(results)`
- `[BE-DOMAIN]` `SearchResult.Merge(IEnumerable<SearchResult>)` — suma `Hits` y concatena listas `Entries` de todas las fuentes; sin deduplicación (una entidad en múltiples listas se cuenta múltiples veces — limitación conocida)
- `[BE-INTERFACES]` `ListsController.SearchAll` — requiere `q`; delega a `SearchAllAsync`; sujeto a rate limiting
- `[BE-TEST]` Unit test: resultados de las tres fuentes mergeados correctamente; una fuente fallando no impide que las otras dos retornen resultados

#### Acceptance Criteria

**Scenario 1: Matches across multiple sources**
- Given I am authenticated
- And I send `GET /api/lists/all?q=global corp`
- When the request is processed
- Then I receive HTTP 200 with `{ hits: N, entries: [...] }`
- And `hits` equals the sum of individual source hit counts
- And each entry includes `listSource` para identificar su origen
- And entries from OFAC, World Bank, and ICIJ are all included in the response

**Scenario 2: Only one source returns matches**
- Given only OFAC returns matches for the search term
- When the request is processed
- Then I receive the OFAC entries with the correct hit count
- And World Bank and ICIJ contribute 0 hits and empty entries (no error)

**Scenario 3: All sources unavailable**
- Given all three external sources are unreachable
- When the request is processed
- Then I receive HTTP 200 with `{ hits: 0, entries: [] }`

**Scenario 4: Partial source failure**
- Given OFAC is unreachable but World Bank and ICIJ are available
- When the request is processed
- Then I receive HTTP 200 with results from World Bank and ICIJ only
- And the total `hits` reflects only the available sources

**Scenario 5: Rate limit exceeded**
- Given I have exceeded 20 requests per minute from the same IP
- When I send an additional request
- Then I receive HTTP 429 Too Many Requests

---

## Epic: Scraping Infrastructure

---

### TS-SCR-000: Scraping Module Bootstrap

**Title:** HTTP clients, caching, rate limiting, and DI registration

**Description:**
As a developer, I need to configure the scraping module's infrastructure — typed HTTP clients con timeout y User-Agent headers, in-memory caching, IP-based rate limiting, y DI registration del orchestration service — so that all scraping user stories have a reliable, protected foundation.

**Deliverable:**
`ScrapingModuleExtensions.AddScrapingModule()` y `UseScrapingModule()` registrados en `Program.cs`, con configuración completa de HTTP clients, cache, rate limiter y `ScrapingOrchestrationService`.

**Dependencies:**
- None — no IAM dependency; el módulo registra sus propios servicios independientemente

**Priority:** Critical | **Estimate:** 2 SP | **Status:** Implemented (v0.5.0)

#### Tasks

- `[BE-INFRA]` Tres registros de `HttpClient` tipados — cada uno con `Timeout` y `User-Agent` header configurados para su fuente destino
- `[BE-INFRA]` `IMemoryCache` registration (si no está ya registrado por Shared)
- `[BE-INFRA]` `ScrapingOrchestrationService` registrado como scoped; recibe `IEnumerable<IScrapingSource>` (las tres fuentes inyectadas via DI)
- `[BE-INFRA]` `AspNetCoreRateLimit` IP-based rate limiting with tiered rules: `POST /api/authentication/sign-in` (5 req/min — brute-force protection), `GET /api/lists/*` (20 req/min — external source protection), `*:/api/*` (100 req/min — general fallback)
- `[BE-INFRA]` `UseScrapingModule(app)` agrega `app.UseIpRateLimiting()` middleware
- `[BE-TEST]` Integration test: rate limiter rechaza la 21a solicitud por minuto con 429

#### Acceptance Criteria

- Given the application starts
- When `AddScrapingModule()` y `UseScrapingModule()` son llamados
- Then las tres implementaciones de `IScrapingSource` son resolvibles desde DI
- And `ScrapingOrchestrationService` es resolvible y recibe las tres fuentes
- And el rate limiter está activo con reglas escalonadas: sign-in (5/min), lists (20/min), general API (100/min)
- And solicitudes que exceden el límite reciben HTTP 429 con header `Retry-After`

---

## Out of Scope — v1.0

### US-SCR-005: Persist scraping history *(deferred)*

Almacenar registros `SearchResult` en una tabla dedicada `scraping_results` para auditoría y análisis de tendencias. Diferido: el cache (10-min TTL) es suficiente para los casos de uso de v1.0, y el `ScreeningResult` en el módulo de Suppliers ya registra el resultado agregado de riesgo junto con las entradas coincidentes en `entries_json`.

### US-SCR-006: Configurable cache TTL *(deferred)*

Exponer el TTL del cache como valor configurable en `appsettings.json`. Actualmente hardcodeado a 10 minutos en `ScrapingOrchestrationService`.

### US-SCR-007: Additional risk list sources *(deferred)*

Integrar fuentes adicionales (EU Sanctions, UN Security Council, INTERPOL). La interfaz `IScrapingSource` está diseñada para acomodar nuevas fuentes sin cambios en el orchestrator ni en el controller.

---

## Implementation Notes

| Aspect | Implementation |
|--------|---------------|
| Stateless design | No SQL tables; no EF Core entities. All data lives in HTTP responses and `IMemoryCache` |
| `RiskEntry` shape | Unified type for all three sources; `ListSource` discriminates origin; inapplicable fields are `null` |
| OFAC fields | `listSource`, `name`, `address`, `type`, `list`, `programs` (string[]), `score` (double?) |
| World Bank fields | `listSource`, `name` (firm name), `address`, `country`, `fromDate`, `toDate`, `grounds` |
| ICIJ fields | `listSource`, `name` (node caption), `jurisdiction`, `linkedTo`, `dataFrom` |
| Cache key format | `scraping:{SOURCE}:{term}` — per source, per term; TTL 10 minutes |
| Fault tolerance | Each `IScrapingSource` wraps its implementation in try/catch and returns `SearchResult.Empty` on any error — the orchestrator never propagates source-level exceptions |
| Rate limiting | IP-based via `AspNetCoreRateLimit` with tiered rules: `POST /api/authentication/sign-in` (5 req/min — brute-force protection), `GET /api/lists/*` (20 req/min — external source protection), `*:/api/*` (100 req/min — general fallback); returns 429 with `Retry-After` when exceeded |
| Parallel execution | `SearchAllAsync` uses `Task.WhenAll` — all three sources queried concurrently; total latency = slowest source, not the sum |
| OFAC parsing | Downloads and decompresses the full SDN ZIP in memory on each cache miss; no disk writes |
| World Bank parsing | `HtmlAgilityPack` for robust HTML table parsing |
| ICIJ integration | REST JSON API; deserialization with `System.Text.Json`; entity caption mapped to `name` field |
| No deduplication | `SearchResult.Merge` sums hits and concatenates entries; an entity present in multiple lists is counted multiple times — known v1.0 limitation |
| Cross-module usage | `ScrapingOrchestrationService` is consumed directly by `RunScreeningCommandHandler` in the Suppliers module; the Scraping module has no dependency on Suppliers |
| Implementation status | All US-SCR stories and TS-SCR-000 implemented in v0.5.0 |
