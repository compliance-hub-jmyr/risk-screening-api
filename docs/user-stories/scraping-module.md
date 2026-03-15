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
Endpoint `GET /api/lists/search?q={term}&sources=ofac` that downloads the SDN XML feed, filters entries whose name contains the term (case-insensitive), and returns a `ScrapingResponse` with the match count and matching entries. Results are cached for 10 minutes per term.

**Dependencies:**
- `US-IAM-001`: JWT authentication
- `ScrapingModuleExtensions.AddScrapingModule()` registered — HTTP client and `IMemoryCache` configured

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
- `[BE-DOMAIN]` `SearchResult` record with `Hits`, `Entries` (IReadOnlyList<RiskEntry>), static `Empty`, and factory `Merge`
- `[BE-INFRA]` `IScrapingSource` interface with `SourceName` and `SearchAsync(term, ct)`
- `[BE-INFRA]` `OfacScrapingSource` — downloads SDN ZIP from `https://sdn.ofac.treas.gov/SDN_XML.zip` (programmatic data source; the assessment references `https://sanctionssearch.ofac.treas.gov/` which is a web-only form with no REST API), decompresses in memory, parses XML with `XDocument`, case-insensitive name match; maps to `RiskEntry` with `ListSource = "OFAC"`, `Name`, `Address`, `Type`, `List`, `Programs`, `Score`; returns `SearchResult.Empty` on any failure
- `[BE-INFRA]` `ScrapingOrchestrationService.SearchSourceAsync("ofac", term)` — caches by `scraping:ofac:{term}` for 10 min
- `[BE-INTERFACES]` `ListsController.Search` with `sources=ofac` — requires `q`; dispatches to `SearchAllAsync` with source filter; subject to rate limiting (20 req/min per IP)
- `[BE-TEST]` Unit test: matching entries returned with all OFAC fields, no matches returns empty, missing `q` returns 400

#### Acceptance Criteria

**Scenario 1: Matches found**
- Given I am authenticated
- And I send `GET /api/lists/search?q=john doe&sources=ofac`
- When the request is processed
- Then I receive HTTP 200 with `{ hits: N, entries: [...] }`
- And each entry includes `listSource = "OFAC"`, `name`, `address`, `type`, `list`, `programs`, `score`

**Scenario 2: No matches**
- Given the search term does not appear in the OFAC SDN list
- When I send `GET /api/lists/search?q=unknown entity xyz&sources=ofac`
- Then I receive HTTP 200 with `{ hits: 0, entries: [] }`

**Scenario 3: Missing search term**
- Given I send `GET /api/lists/search?sources=ofac` without the `q` parameter
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
- When I send `GET /api/lists/search?q=john doe&sources=ofac` again
- Then I receive HTTP 200 with the cached result (no external HTTP call made)

---

### US-SCR-002: Search the World Bank debarred firms list

**Title:** Query the World Bank Debarred Firms registry

**Description:**
As a compliance officer, I want to search the World Bank's list of debarred and cross-debarred firms, so that I can identify suppliers that have been sanctioned from participating in World Bank-funded projects.

**Deliverable:**
Endpoint `GET /api/lists/search?q={term}&sources=worldbank` that scrapes the World Bank HTML page using `HtmlAgilityPack`, extracts matching table rows, and returns a `ScrapingResponse`. Results are cached for 10 minutes per term.

**Dependencies:**
- `US-SCR-001` (same infrastructure, same pattern)

**Priority:** High | **Estimate:** 3 SP | **Status:** Updated (v0.5.1)

#### Tasks

- `[BE-INFRA]` `WorldBankScrapingSource` — fetches `https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms?srchTerm={term}`, parses HTML table with `HtmlAgilityPack`, extracts firm name (mapped to `Name`), `Address`, `Country`, `FromDate`, `ToDate`, `Grounds`; maps to `RiskEntry` with `ListSource = "WORLD_BANK"`; returns `SearchResult.Empty` on failure
- `[BE-INFRA]` `ScrapingOrchestrationService.SearchSourceAsync("worldbank", term)` — caches by `scraping:worldbank:{term}` for 10 min
- `[BE-INTERFACES]` `ListsController.Search` with `sources=worldbank` — requires `q`; subject to rate limiting
- `[BE-TEST]` Unit test: matching rows returned with all World Bank fields, HTML parse failure returns empty

#### Acceptance Criteria

**Scenario 1: Matches found**
- Given I am authenticated
- And I send `GET /api/lists/search?q=acme corp&sources=worldbank`
- When the request is processed
- Then I receive HTTP 200 with `{ hits: N, entries: [...] }`
- And each entry includes `listSource = "WORLD_BANK"`, `name`, `address`, `country`, `fromDate`, `toDate`, `grounds`

**Scenario 2: No matches**
- Given the search term does not appear in the World Bank debarred firms table
- When I send `GET /api/lists/search?q=unknown entity xyz&sources=worldbank`
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
Endpoint `GET /api/lists/search?q={term}&sources=icij` that queries the ICIJ public JSON API, deserializes the `nodes` array, and returns a `ScrapingResponse`. Results are cached for 10 minutes per term.

**Dependencies:**
- `US-SCR-001` (same infrastructure, same pattern)

**Priority:** High | **Estimate:** 2 SP | **Status:** Updated (v0.5.1)

#### Tasks

- `[BE-INFRA]` `IcijScrapingSource` — fetches `https://offshoreleaks.icij.org/api/nodes?q={term}`, deserializes `nodes` array with `System.Text.Json` into internal `IcijNode` DTOs; maps to `RiskEntry` with:
  - `ListSource = "ICIJ"`
  - `Name` (node caption, fallback to name field)
  - `Jurisdiction`, `LinkedTo`, `DataFrom`
  - All OFAC/World Bank fields remain `null`
  - Returns `SearchResult.Empty` on failure
- `[BE-INFRA]` `ScrapingOrchestrationService.SearchSourceAsync("icij", term)` — caches by `scraping:icij:{term}` for 10 min
- `[BE-INTERFACES]` `ListsController.Search` with `sources=icij` — requires `q`; subject to rate limiting
- `[BE-TEST]` Unit test: nodes deserialized correctly with `name` field, empty `nodes` array returns empty

#### Acceptance Criteria

**Scenario 1: Matches found**
- Given I am authenticated
- And I send `GET /api/lists/search?q=mossack fonseca&sources=icij`
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
Endpoint `GET /api/lists/search?q={term}` (no `sources` parameter, or `sources=ofac,worldbank,icij`) that executes all three source queries in parallel via `Task.WhenAll`, merges results with `SearchResult.Merge`, and returns a single `ScrapingResponse` with the total `hits` and unified entry list. Each per-source result is cached independently. The `sources` query parameter accepts a comma-separated subset to query specific sources only; when omitted, all registered sources are queried.

**Dependencies:**
- `US-SCR-001`, `US-SCR-002`, `US-SCR-003`

**Priority:** Critical | **Estimate:** 2 SP | **Status:** Implemented (v0.5.0)

#### Tasks

- `[BE-INFRA]` `ScrapingOrchestrationService.SearchAllAsync(term, sourceNames?)` — calls selected (or all) `IScrapingSource` instances in parallel via `Task.WhenAll`, merges with `SearchResult.Merge(results)`
- `[BE-DOMAIN]` `SearchResult.Merge(IEnumerable<SearchResult>)` — sums `Hits` and concatenates `Entries` lists from all sources; no deduplication (an entity present in multiple lists is counted multiple times — known limitation)
- `[BE-INTERFACES]` `ListsController.Search` — unified endpoint `GET /api/lists/search?q={term}&sources={csv}`; requires `q`; optional `sources` (comma-separated: ofac, worldbank, icij); validates source names against whitelist; delegates to `SearchAllAsync`; subject to rate limiting
- `[BE-TEST]` Unit test: results from all three sources merged correctly; one source failing does not prevent the other two from returning results

#### Acceptance Criteria

**Scenario 1: Matches across multiple sources**
- Given I am authenticated
- And I send `GET /api/lists/search?q=global corp` (no `sources` parameter — queries all)
- When the request is processed
- Then I receive HTTP 200 with `{ hits: N, entries: [...] }`
- And `hits` equals the sum of individual source hit counts
- And each entry includes `listSource` to identify its origin
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
As a developer, I need to configure the scraping module's infrastructure — typed HTTP clients with timeout and User-Agent headers, in-memory caching, IP-based rate limiting, and DI registration of the orchestration service — so that all scraping user stories have a reliable, protected foundation.

**Deliverable:**
`ScrapingModuleExtensions.AddScrapingModule()` registers HTTP clients and cache. Rate limiting moved to shared infrastructure (`AddRateLimiting()` / `UseRateLimiting()`) since it protects endpoints across all modules. `RateLimitResponseMiddleware` rewrites 429 responses to standard `ErrorResponse` format.

**Dependencies:**
- None — no IAM dependency; the module registers its own services independently

**Priority:** Critical | **Estimate:** 2 SP | **Status:** Implemented (v0.5.0)

#### Tasks

- `[BE-INFRA]` Three typed `HttpClient` registrations — each with `Timeout` and `User-Agent` header configured for its target source
- `[BE-INFRA]` `IMemoryCache` registration (if not already registered by Shared)
- `[BE-INFRA]` `ScrapingOrchestrationService` registered as scoped; receives `IEnumerable<IScrapingSource>` (all three sources injected via DI)
- `[BE-INFRA]` `AspNetCoreRateLimit` IP-based rate limiting with tiered rules (moved to shared infrastructure): `POST /api/authentication/sign-in` (5 req/min — brute-force protection), `GET /api/lists/*` (20 req/min — external source protection), `*:/api/*` (100 req/min — general fallback)
- `[BE-INFRA]` `RateLimitResponseMiddleware` — intercepts 429 responses and rewrites to standard `ErrorResponse` (RFC 7807) with `RATE_LIMIT_EXCEEDED` error code (7000)
- `[BE-INFRA]` `UseRateLimiting(app)` adds `app.UseIpRateLimiting()` middleware (shared infrastructure)
- `[BE-TEST]` Integration test: rate limiter rejects the 21st request per minute with 429 in `ErrorResponse` format

#### Acceptance Criteria

- Given the application starts
- When `AddScrapingModule()`, `AddRateLimiting()` and `UseRateLimiting()` are called
- Then all three `IScrapingSource` implementations are resolvable from DI
- And `ScrapingOrchestrationService` is resolvable and receives all three sources
- And the rate limiter is active with tiered rules: sign-in (5/min), lists (20/min), general API (100/min)
- And requests that exceed the limit receive HTTP 429 in `ErrorResponse` format with `errorCode: "RATE_LIMIT_EXCEEDED"` and `Retry-After` header

---

## Out of Scope — v1.0

### US-SCR-005: Persist scraping history *(deferred)*

Store `SearchResult` records in a dedicated `scraping_results` table for auditing and trend analysis. Deferred: the cache (10-min TTL) is sufficient for v1.0 use cases, and the `ScreeningResult` in the Suppliers module already records the aggregated risk result along with matching entries in `entries_json`.

### US-SCR-006: Configurable cache TTL *(deferred)*

Expose the cache TTL as a configurable value in `appsettings.json`. Currently hardcoded to 10 minutes in `ScrapingOrchestrationService`.

### US-SCR-007: Additional risk list sources *(deferred)*

Integrate additional sources (EU Sanctions, UN Security Council, INTERPOL). The `IScrapingSource` interface is designed to accommodate new sources without changes to the orchestrator or controller.

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
| Rate limiting | IP-based via `AspNetCoreRateLimit` (shared infrastructure) with tiered rules: sign-in (5/min), lists (20/min), general API (100/min). `RateLimitResponseMiddleware` rewrites 429 to standard `ErrorResponse` with `RATE_LIMIT_EXCEEDED` (7000) and `Retry-After` header |
| Unified endpoint | Single `GET /api/lists/search?q={term}&sources={csv}` — the `sources` parameter is optional (comma-separated: ofac, worldbank, icij); when omitted, all sources are queried. Controller validates source names against a whitelist. No individual per-source endpoints |
| Parallel execution | `SearchAllAsync` uses `Task.WhenAll` — all selected sources queried concurrently; total latency = slowest source, not the sum |
| OFAC parsing | Downloads SDN_XML.zip from `sdn.ofac.treas.gov` (the assessment URL `sanctionssearch.ofac.treas.gov` is a web form with no REST API); decompresses in memory on each cache miss; no disk writes |
| World Bank parsing | `HtmlAgilityPack` for robust HTML table parsing |
| ICIJ integration | REST JSON API; deserialization with `System.Text.Json`; entity caption mapped to `name` field |
| No deduplication | `SearchResult.Merge` sums hits and concatenates entries; an entity present in multiple lists is counted multiple times — known v1.0 limitation |
| Cross-module usage | `ScrapingOrchestrationService` is consumed directly by `RunScreeningCommandHandler` in the Suppliers module; the Scraping module has no dependency on Suppliers |
| Implementation status | All US-SCR stories and TS-SCR-000 implemented in v0.5.0 |
