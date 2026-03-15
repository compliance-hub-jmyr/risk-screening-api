# User Stories — Scraping Module

> **Format:** Title / Description / Deliverable / Dependencies / Acceptance Criteria (BDD Given/When/Then).
> **Task tags:** `[BE-DOMAIN]` `[BE-APP]` `[BE-INFRA]` `[BE-INTERFACES]` `[BE-TEST]` `[DOCS]`
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
Endpoint `GET /api/lists/search?q={term}&sources=ofac` that performs real web scraping of the OFAC Sanctions List Search website by:
1. Fetching the initial ASP.NET form page to extract ViewState and form fields
2. Submitting a POST request with the search term
3. Parsing the HTML results table to extract Name, Address, Type, List, Programs, and **Score** (match confidence percentage)
Results are cached for 10 minutes per term.

**Technical Note:**
The OFAC SDN XML feed (`https://www.treasury.gov/ofac/downloads/sdn.xml`) does not include the **Score** field required by the technical assessment. The Score (match confidence percentage) is only available through the web search interface at `https://sanctionssearch.ofac.treas.gov/`, which requires form submission and HTML parsing. This implementation uses `HtmlAgilityPack` for robust HTML parsing and handles ASP.NET ViewState management.

**Dependencies:**
- `US-IAM-001`: JWT authentication
- `ScrapingModuleExtensions.AddScrapingModule()` registered — HTTP client with 45s timeout and `IMemoryCache` configured
- `HtmlAgilityPack` 1.11.71 for HTML parsing

**Priority:** High | **Estimate:** 5 SP | **Status:** Updated (v0.6.0 - Web Scraping + Ports & Adapters)

#### Tasks

- `[BE-DOMAIN]` `RiskEntry` record (`Domain/Model/ValueObjects/`) — unified type for all three sources:
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
- `[BE-DOMAIN]` `SearchResult` record (`Domain/Model/ValueObjects/`) with `Hits`, `Entries` (IReadOnlyList<RiskEntry>), static `Empty`, and factory `Merge`
- `[BE-DOMAIN]` `SearchRiskListsQuery` record (`Domain/Model/Queries/`) — CQRS query implementing `IRequest<SearchResult>` with `Term` and optional `SourceNames` filter
- `[BE-APP]` `IScrapingSource` port (`Application/Ports/`) — interface with `SourceName` and `SearchAsync(term, ct)` — defines the contract for scraping source adapters
- `[BE-APP]` `SearchRiskListsQueryHandler` (`Application/Search/`) — MediatR `IRequestHandler<SearchRiskListsQuery, SearchResult>` that orchestrates source calls with `IMemoryCache` caching and parallel execution via `Task.WhenAll`
- `[BE-INFRA]` `OfacScrapingSource` (`Infrastructure/Sources/`) — adapter implementing `IScrapingSource`; orchestrates the GET → POST HTTP flow against `https://sanctionssearch.ofac.treas.gov/`
- `[BE-INFRA]` `OfacHtmlParser` (`Infrastructure/Sources/`) — static helper that extracts ASP.NET form data from the initial page and parses the HTML results table into `RiskEntry` records
- `[BE-APP]` `SearchRiskListsQueryValidator` (`Application/Search/`) — FluentValidation validator auto-executed by `ValidationPipelineBehavior`; validates `q` is not empty and each `sources` value is whitelisted (ofac, worldbank, icij)
- `[BE-INTERFACES]` `ListsController.Search` — thin controller: creates `SearchRiskListsQuery`, dispatches via MediatR, maps response with `ScrapingResponseMapper`; validation handled by `ValidationPipelineBehavior`
- `[BE-TEST]` `OfacScrapingSourceTests` (16 tests) — uses `OfacHtmlMother` for HTML fixtures and `FakeHttpMessageHandler` for HTTP simulation
- `[BE-TEST]` `SearchRiskListsQueryHandlerTests` (10 tests) — uses `SearchResultMother` and `RiskEntryMother` for test data; covers source selection, caching, and result merging
- `[BE-TEST]` `SearchResultTests` (5 tests) — `Empty`, `Merge` factory behavior

#### Acceptance Criteria

**Scenario 1: Matches found**
- Given I am authenticated
- And I send `GET /api/lists/search?q=john doe&sources=ofac`
- When the request is processed
- Then I receive HTTP 200 with `{ hits: N, entries: [...] }`
- And each entry includes `listSource = "OFAC"`, `name`, `address`, `type`, `list`, `programs`, **`score` (double)**

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
- Given the OFAC website is unreachable
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
Endpoint `GET /api/lists/search?q={term}&sources=worldbank` that scrapes the World Bank Debarred Firms page using `HtmlAgilityPack` to extract the API config from embedded JavaScript, then fetches the JSON API and filters firms client-side (OR logic across name, address, city, state, country, grounds). Returns a `ScrapingResponse`. Results are cached for 10 minutes per term.

**Dependencies:**
- `US-SCR-001` (same infrastructure, same pattern)

**Priority:** High | **Estimate:** 3 SP | **Status:** Updated (v0.6.0)

#### Tasks

- `[BE-INFRA]` `WorldBankScrapingSource` — adapter implementing `IScrapingSource`; two-step web scraping flow: (1) GET HTML page → extract API URL + key from JavaScript via `WorldBankHtmlParser.ExtractApiConfig()`, (2) GET JSON API → filter + map via `WorldBankHtmlParser.ParseResults()`; maps to `RiskEntry` with `ListSource = "WORLD_BANK"`; `ToDate` shows "Ongoing"/"Permanent" label when applicable; returns `SearchResult.Empty` on failure
- `[BE-INFRA]` `WorldBankHtmlParser` — unified static helper (mirrors `OfacHtmlParser` pattern): `ExtractApiConfig()` parses `<script>` tags with `HtmlAgilityPack` to extract API URL + key using `GeneratedRegex`; `ParseResults()` deserializes `response.ZPROCSUPP` JSON, filters firms by search term (multi-field OR, case-insensitive contains), combines address components, and maps `INELIGIBLY_STATUS` to `ToDate` when status is "Permanent" or "Ongoing"
- `[BE-APP]` `SearchRiskListsQueryHandler` caches result by `scraping:worldbank:{term}` for 10 min (shared handler — no per-source orchestrator needed)
- `[BE-INTERFACES]` `ListsController.Search` with `sources=worldbank` — requires `q`; subject to rate limiting
- `[BE-TEST]` `WorldBankScrapingSourceTests` (18 tests) — uses `WorldBankJsonMother` for JSON fixtures; covers multi-field search (name, address, country, grounds), "Ongoing"/"Permanent" status mapping, address combination, error scenarios (HTTP error, invalid JSON, timeout)

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
- Given the World Bank API is unreachable or returns invalid JSON
- When I send the request
- Then I receive HTTP 200 with `{ hits: 0, entries: [] }` (fault-tolerant)

**Scenario 4: Rate limit exceeded**
- Given I have exceeded 20 requests per minute from the same IP
- When I send an additional request
- Then I receive HTTP 429 Too Many Requests

---

### US-SCR-003: Search the ICIJ Offshore Leaks database

**Title:** Scrape the ICIJ Offshore Leaks search page

**Description:**
As a compliance officer, I want to search the ICIJ Offshore Leaks database, so that I can identify suppliers linked to offshore entities named in the Panama Papers, Paradise Papers, or similar investigations.

**Deliverable:**
Endpoint `GET /api/lists/search?q={term}&sources=icij` that scrapes the ICIJ Offshore Leaks search page using `Microsoft.Playwright` (headless Chromium with anti-detection stealth flags) to render the JavaScript SPA, then parses the HTML results table with `HtmlAgilityPack`, and returns a `ScrapingResponse`. Results are cached for 10 minutes per term.

**Dependencies:**
- `US-SCR-001` (same infrastructure, same pattern)

**Priority:** High | **Estimate:** 2 SP | **Status:** Implemented (v0.7.0)

#### Tasks

- `[BE-INFRA]` `IcijScrapingSource` — adapter implementing `IScrapingSource`; headless browser scraping via `Microsoft.Playwright` (Chromium): launches browser with anti-detection flags (`--disable-blink-features=AutomationControlled`, custom User-Agent, `navigator.webdriver = false`), navigates to search page, waits for table selector, extracts rendered HTML → parses via `IcijHtmlParser`; maps to `RiskEntry` with:
  - `ListSource = "ICIJ"`
  - `Name` (entity name from table cell)
  - `Jurisdiction`, `LinkedTo`, `DataFrom`
  - All OFAC/World Bank fields remain `null`
  - Returns `SearchResult.Empty` on failure (browser launch error, timeout, CloudFront block)
- `[BE-INFRA]` `IcijHtmlParser` — static helper that parses the rendered ICIJ HTML (from Playwright) with `HtmlAgilityPack`; extracts 4 columns: Entity (→ Name), Jurisdiction, Linked To (→ LinkedTo), Data From (→ DataFrom); decodes HTML entities and normalizes whitespace
- `[BE-APP]` `SearchRiskListsQueryHandler` caches result by `scraping:icij:{term}` for 10 min (shared handler)
- `[BE-INTERFACES]` `ListsController.Search` with `sources=icij` — requires `q`; subject to rate limiting
- `[BE-TEST]` `IcijScrapingSourceTests` (14 tests) — uses `IcijHtmlMother` for HTML fixtures; covers entity field mapping, multiple results, HTML entity decoding, whitespace normalization, empty fields, WAF challenge, error scenarios (HTTP error, timeout, invalid HTML)

#### Acceptance Criteria

**Scenario 1: Matches found**
- Given I am authenticated
- And I send `GET /api/lists/search?q=mossack fonseca&sources=icij`
- When the request is processed
- Then I receive HTTP 200 with `{ hits: N, entries: [...] }`
- And each entry includes `listSource = "ICIJ"`, `name`, `jurisdiction`, `linkedTo`, `dataFrom`

**Scenario 2: No matches**
- Given the search term returns no matches in the ICIJ search results table
- When I send the request
- Then I receive HTTP 200 with `{ hits: 0, entries: [] }`

**Scenario 3: ICIJ API unavailable**
- Given the ICIJ website is unreachable or returns a WAF challenge
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

**Priority:** Critical | **Estimate:** 2 SP | **Status:** Implemented (no separate branch — built into `SearchRiskListsQueryHandler` from US-SCR-001)

**Implementation Note:**
This story does not require a dedicated `feature/us-scr-004-search-all-lists` branch. The "search all" behavior is inherent to the `SearchRiskListsQueryHandler` design: when the `sources` parameter is omitted or empty, the handler queries all registered `IScrapingSource` instances in parallel via `Task.WhenAll`. The handler, merge logic, validator, and controller were all implemented as part of `US-SCR-001`.

#### Tasks

- `[BE-APP]` `SearchRiskListsQueryHandler.Handle(SearchRiskListsQuery, CancellationToken)` — selects sources by `SourceNames` filter (or all if null/empty), calls `IScrapingSource` instances in parallel via `Task.WhenAll`, merges with `SearchResult.Merge(results)`, caches each source result independently ✅ *(implemented in US-SCR-001)*
- `[BE-DOMAIN]` `SearchResult.Merge(IEnumerable<SearchResult>)` — sums `Hits` and concatenates `Entries` lists from all sources; no deduplication (an entity present in multiple lists is counted multiple times — known limitation) ✅ *(implemented in US-SCR-001)*
- `[BE-APP]` `SearchRiskListsQueryValidator` — FluentValidation rules: `Term` not empty, each `SourceNames` value in whitelist; auto-executed by `ValidationPipelineBehavior` before handler ✅ *(implemented in US-SCR-001)*
- `[BE-INTERFACES]` `ListsController.Search` — thin controller: creates `SearchRiskListsQuery(q, sources)`, dispatches via `IMediator`, maps response with `ScrapingResponseMapper`; subject to rate limiting ✅ *(implemented in US-SCR-001)*
- `[BE-TEST]` Unit test: results from all three sources merged correctly; one source failing does not prevent the other two from returning results ✅ *(covered by `SearchRiskListsQueryHandlerTests`)*

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
As a developer, I need to configure the scraping module's infrastructure — typed HTTP clients with timeout and User-Agent headers, in-memory caching, IP-based rate limiting, and DI registration of scraping sources and MediatR handlers — so that all scraping user stories have a reliable, protected foundation.

**Deliverable:**
`ScrapingModuleExtensions.AddScrapingModule()` registers HTTP clients, `IMemoryCache`, and `IScrapingSource` adapter implementations. The `SearchRiskListsQueryHandler` is auto-discovered by MediatR assembly scanning — no explicit registration needed. Rate limiting moved to shared infrastructure (`AddRateLimiting()` / `UseRateLimiting()`) since it protects endpoints across all modules. `RateLimitResponseMiddleware` rewrites 429 responses to standard `ErrorResponse` format.

**Dependencies:**
- None — no IAM dependency; the module registers its own services independently

**Priority:** Critical | **Estimate:** 2 SP | **Status:** Updated (v0.6.0 - Ports & Adapters)

#### Tasks

- `[BE-INFRA]` Two typed `HttpClient` registrations (OFAC, World Bank) — each with `Timeout` and `User-Agent` header configured for its target source; ICIJ uses `Microsoft.Playwright` instead of `HttpClient`
- `[BE-INFRA]` `IMemoryCache` registration (if not already registered by Shared)
- `[BE-INFRA]` `IScrapingSource` adapter implementations registered as scoped (`OfacScrapingSource`, future: `WorldBankScrapingSource`, `IcijScrapingSource`)
- `[BE-APP]` `SearchRiskListsQueryHandler` auto-discovered by MediatR assembly scanning; receives `IEnumerable<IScrapingSource>` (all sources injected via DI) and `IMemoryCache`
- `[BE-INFRA]` `AspNetCoreRateLimit` IP-based rate limiting with tiered rules (moved to shared infrastructure): `POST /api/authentication/sign-in` (5 req/min — brute-force protection), `GET /api/lists/*` (20 req/min — external source protection), `*:/api/*` (100 req/min — general fallback)
- `[BE-INFRA]` `RateLimitResponseMiddleware` — intercepts 429 responses and rewrites to standard `ErrorResponse` (RFC 7807) with `RATE_LIMIT_EXCEEDED` error code (7000)
- `[BE-INFRA]` `UseRateLimiting(app)` adds `app.UseIpRateLimiting()` middleware (shared infrastructure)
- `[BE-TEST]` Integration test: rate limiter rejects the 21st request per minute with 429 in `ErrorResponse` format

#### Acceptance Criteria

- Given the application starts
- When `AddScrapingModule()`, `AddRateLimiting()` and `UseRateLimiting()` are called
- Then all `IScrapingSource` adapter implementations are resolvable from DI
- And `SearchRiskListsQueryHandler` is resolvable and receives all sources via `IEnumerable<IScrapingSource>`
- And the rate limiter is active with tiered rules: sign-in (5/min), lists (20/min), general API (100/min)
- And requests that exceed the limit receive HTTP 429 in `ErrorResponse` format with `errorCode: "RATE_LIMIT_EXCEEDED"` and `Retry-After` header

---

## Out of Scope — v1.0

### US-SCR-005: Persist scraping history *(deferred)*

Store `SearchResult` records in a dedicated `scraping_results` table for auditing and trend analysis. Deferred: the cache (10-min TTL) is sufficient for v1.0 use cases, and the `ScreeningResult` in the Suppliers module already records the aggregated risk result along with matching entries in `entries_json`.

### US-SCR-006: Configurable cache TTL *(deferred)*

Expose the cache TTL as a configurable value in `appsettings.json`. Currently hardcoded to 10 minutes in `SearchRiskListsQueryHandler`.

### US-SCR-007: Additional risk list sources *(deferred)*

Integrate additional sources (EU Sanctions, UN Security Council, INTERPOL). The `IScrapingSource` port interface is designed to accommodate new adapter implementations without changes to the handler or controller.

---

## Implementation Notes

| Aspect | Implementation |
|--------|---------------|
| Architecture | Ports & Adapters (Hexagonal): `IScrapingSource` port in `Application/Ports/`, adapter implementations in `Infrastructure/Sources/`, CQRS query handler in `Application/Search/` |
| Stateless design | No SQL tables; no EF Core entities. All data lives in HTTP responses and `IMemoryCache` |
| `RiskEntry` shape | Unified record in `Domain/Model/ValueObjects/`; `ListSource` discriminates origin; inapplicable fields are `null` |
| OFAC fields | `listSource`, `name`, `address`, `type`, `list`, `programs` (string[]), `score` (double?) |
| World Bank fields | `listSource`, `name` (firm name), `address`, `country`, `fromDate`, `toDate`, `grounds` |
| ICIJ fields | `listSource`, `name` (node caption), `jurisdiction`, `linkedTo`, `dataFrom` |
| CQRS pattern | `SearchRiskListsQuery` → `SearchRiskListsQueryHandler` via MediatR (same pattern as IAM and Suppliers modules) |
| Cache key format | `scraping:{SOURCE}:{term}` — per source, per term; TTL 10 minutes |
| Fault tolerance | Each `IScrapingSource` adapter wraps its implementation in try/catch and returns `SearchResult.Empty` on any error — the handler never propagates source-level exceptions |
| Rate limiting | IP-based via `AspNetCoreRateLimit` (shared infrastructure) with tiered rules: sign-in (5/min), lists (20/min), general API (100/min). `RateLimitResponseMiddleware` rewrites 429 to standard `ErrorResponse` with `RATE_LIMIT_EXCEEDED` (7000) and `Retry-After` header |
| Unified endpoint | Single `GET /api/lists/search?q={term}&sources=ofac&sources=worldbank` — the `sources` parameter is optional (repeated query params: ofac, worldbank, icij); when omitted, all sources are queried. `SearchRiskListsQueryValidator` (FluentValidation) validates input before the handler executes |
| Parallel execution | `SearchRiskListsQueryHandler` uses `Task.WhenAll` — all selected sources queried concurrently; total latency = slowest source, not the sum |
| OFAC scraping | `OfacScrapingSource` orchestrates GET → POST flow; `OfacHtmlParser` extracts form data and parses HTML results table with `HtmlAgilityPack` |
| World Bank scraping | Two-step web scraping via unified `WorldBankHtmlParser`: (1) `ExtractApiConfig()` scrapes `<script>` tags with `HtmlAgilityPack` to extract API URL + key, (2) `ParseResults()` deserializes JSON and filters client-side (OR logic across name, address, city, state, country, grounds); maps "Ongoing"/"Permanent" to `toDate` |
| ICIJ scraping | `IcijScrapingSource` uses `Microsoft.Playwright` (headless Chromium) with stealth flags to render the ICIJ JavaScript SPA; `IcijHtmlParser` parses the rendered HTML with `HtmlAgilityPack` extracting Entity, Jurisdiction, Linked To, Data From columns |
| No deduplication | `SearchResult.Merge` sums hits and concatenates entries; an entity present in multiple lists is counted multiple times — known v1.0 limitation |
| Cross-module usage | `SearchRiskListsQueryHandler` is consumed via MediatR by `RunScreeningCommandHandler` in the Suppliers module; the Scraping module has no dependency on Suppliers |
| Test infrastructure | Mother pattern: `RiskEntryMother`, `SearchResultMother`, `OfacHtmlMother`, `WorldBankJsonMother`, `IcijHtmlMother`; `FakeHttpMessageHandler` for HTTP simulation |
| Implementation status | All US-SCR stories and TS-SCR-000 implemented. US-SCR-004 does not require a separate branch — "search all" is built into `SearchRiskListsQueryHandler` (queries all sources when `sources` is omitted) |
