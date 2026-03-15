# ADR-0006: Web Scraping Strategy — On-Demand with Cache (Phase 1)

## Status
`Accepted`

## Date
2026-03-15 (Updated)

## Context

The Scraping module requires obtaining data from three external sources:
1. **OFAC SDN** — US Treasury Sanctions List Search (web form at `https://sanctionssearch.ofac.treas.gov/`)
2. **World Bank Debarred Firms** — Kendo UI grid at `https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms` (loads data dynamically via AJAX; client-side filtering)
3. **ICIJ Offshore Leaks** — Public REST API (per-query search; no full dataset download)

Two data retrieval strategies were evaluated:

| Strategy | Description | Latency | Risk |
|----------|-------------|---------|------|
| **On-demand scraping** | Scrape live when the request arrives; cache the result with a TTL | Moderate on first call; near-zero on cache hit | Slight delay on cache miss; external site must be reachable |
| **Background refresh + cache** | Periodic `IHostedService` pre-populates cache; all requests served from cache | Near-zero (< 10 ms always) | Data may be up to N minutes old; startup warm-up required |

## Decision

**Phase 1 (current):** On-demand scraping with `IMemoryCache` result caching.

When a search request arrives:
1. Check `IMemoryCache` for a cached result (keyed by source + query term).
2. **Cache hit** → return immediately (sub-millisecond).
3. **Cache miss** → fetch live from the external source, store result in cache with a TTL, then return.

This approach is simpler to implement and operate in Phase 1, avoids the complexity of a background worker, and still delivers fast responses for repeated queries.

> Cache technology choice (IMemoryCache vs Redis) is documented in **ADR-0008**.

### Strategy by source

#### OFAC SDN
- **Assessment reference**: `https://sanctionssearch.ofac.treas.gov/` (ASP.NET web form with search functionality)
- **Method**: Real web scraping via `HtmlAgilityPack`
  1. `OfacScrapingSource` (adapter) orchestrates the HTTP flow: GET initial page → POST search form
  2. `OfacHtmlParser` (static helper) handles HTML extraction:
     - `ExtractFormData()` — parses ASP.NET ViewState and hidden fields from the initial page
     - `ParseResults()` — locates the `#scrollResults` table and converts rows into `RiskEntry` records
  3. Extracted columns: Name, Address, Type, Program(s), List, **Score** (match confidence percentage)
- **Architecture**: Ports & Adapters — `IScrapingSource` port in `Application/Ports/`, `OfacScrapingSource` adapter in `Infrastructure/Sources/`, orchestration via `SearchRiskListsQueryHandler` (MediatR CQRS handler in `Application/Search/`)
- **Why not XML?** The XML feed (`https://www.treasury.gov/ofac/downloads/sdn.xml`) does not include the **Score** field, which is a critical requirement for the technical assessment. The Score represents the match confidence percentage calculated by OFAC's search algorithm.
- **Cache key**: `scraping:ofac:{normalizedQuery}`
- **TTL**: **10 minutes**
- **HTTP Client timeout**: 45 seconds (increased to handle form submission + parsing)

#### World Bank Debarred Firms
- **Assessment reference**: `https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms` (Kendo UI grid with client-side filtering)
- **Actual API**: `https://apigwext.worldbank.org/dvsvc/v1.0/json/APPLICATION/ADOBE_EXPRNCE_MGR/FIRM/SANCTIONED_FIRM` (JSON API used by the web page; public API key required in `apikey` header)
- **Method**: Two-step web scraping via `HtmlAgilityPack` + `System.Text.Json` (same GET → GET pattern as OFAC's GET → POST)
  1. `WorldBankScrapingSource` (adapter) orchestrates the two-step HTTP flow:
     - **Step 1 (scrape):** GET the HTML page → `WorldBankHtmlParser.ExtractApiConfig()` parses `<script>` tags with `HtmlAgilityPack` to extract the JavaScript variables `prodtabApi` (API URL) and `propApiKey` (API key)
     - **Step 2 (fetch):** GET the JSON API using the extracted URL and key (same request the browser makes via AJAX) → `WorldBankHtmlParser.ParseResults()` deserializes and filters
  2. `WorldBankHtmlParser` (unified static helper — mirrors `OfacHtmlParser` pattern) handles both steps:
     - `ExtractApiConfig()`: Parses `<script>` tags with `HtmlAgilityPack`, extracts `var prodtabApi = "..."` and `var propApiKey = "..."` using `GeneratedRegex`
     - `ParseResults()`: JSON processing:
     - Deserializes `response.ZPROCSUPP` array of firm DTOs
     - Filters firms where the search term matches any field (case-insensitive contains, OR logic across name, address, city, state, country, grounds)
     - Maps `SUPP_NAME`, `SUPP_ADDR`/`SUPP_CITY`/`SUPP_STATE_CODE`/`SUPP_ZIP_CODE` (combined), `COUNTRY_NAME`, `DEBAR_FROM_DATE`, `DEBAR_TO_DATE`, `DEBAR_REASON` to `RiskEntry`
     - When `INELIGIBLY_STATUS` is "Permanent" or "Ongoing", uses that label for `ToDate` instead of the sentinel date (`2999-12-31`)
  4. Extracted fields: Firm Name (→ `Name`), Address (combined components), Country, FromDate, ToDate (or Ineligibility Status), Grounds
- **Why two-step scraping?** The World Bank page is a Kendo UI grid that loads data dynamically via AJAX — the initial HTML does not contain firm data. The scraper extracts the API endpoint and key from the page's JavaScript, then replicates the same AJAX request the browser makes. This ensures the adapter automatically adapts if the API key is rotated.
- **Architecture**: Same Ports & Adapters pattern as OFAC — `IScrapingSource` port, `WorldBankScrapingSource` adapter in `Infrastructure/Sources/`
- **Cache key**: `scraping:worldbank:{normalizedQuery}`
- **TTL**: **10 minutes**
- **HTTP Client timeout**: 45 seconds (increased — two HTTP requests)

#### ICIJ Offshore Leaks
- Source: `https://offshoreleaks.icij.org/api/nodes?q={query}` (public REST API)
- Method: HTTP GET — real-time per-query search; the public API supports per-query search natively
- Cache key: `scraping:icij:{normalizedQuery}`
- TTL: **10 minutes**

### Error handling (fault-tolerant)

```
If a live fetch fails (timeout, HTTP error, parse error):
  - Log error at WARNING level
  - Return SearchResult.Empty (hits: 0, entries: []) — NOT an HTTP error
  - Do NOT write a failed result to cache
  - The next request will retry the live fetch
  - When searching all sources (GET /api/lists/search?q=term), a single source failure
    does not prevent other sources from returning results
```

## Consequences

**Positive:**
- Simple to implement — no background worker, no startup warm-up complexity
- Repeated queries for the same term respond in sub-millisecond time (cache hit)
- Lower load on external sites compared to scraping on every single request
- No stale data risk from a worker that failed to refresh

**Negative:**
- First request for a new query term incurs live fetch latency (OFAC form submission + HTML parsing can take 2–5 s)
- If the external source is unavailable at query time, the request fails (no pre-cached fallback)
- OFAC scraping is more fragile than XML parsing — changes to the ASP.NET form structure or HTML table format will break the scraper
- World Bank JSON API requires a public API key embedded in the page's JavaScript — if the key is rotated, the adapter must be updated

**Mitigation:**
- Use `Polly` retry + timeout policies on all HTTP clients to reduce transient failure impact
- **Phase 2 (future):** Replace on-demand fetching for OFAC and World Bank with a `BackgroundService` that pre-populates cache on startup and refreshes periodically. ICIJ remains on-demand permanently (dataset size, public API design). This improvement is intentionally deferred to avoid over-engineering Phase 1.

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `HtmlAgilityPack` | 1.11.71 | HTML parsing for OFAC (form + results table) and World Bank (JavaScript extraction) |
| `System.Text.Json` | .NET 10 | JSON deserialization for World Bank API responses |
| `Microsoft.Extensions.Http` | .NET 10 | `HttpClientFactory` for typed clients |
| `Polly` | 8.x (future) | Retry and timeout policies for HTTP requests |

## References
- [OFAC SDN List Downloads](https://www.treasury.gov/resource-center/sanctions/SDN-List/Pages/default.aspx)
- [ICIJ Offshore Leaks API](https://offshoreleaks.icij.org/api)
- [HtmlAgilityPack](https://html-agility-pack.net/)
- [Polly — .NET Resilience Library](https://github.com/App-vNext/Polly)
- ADR-0008 — Cache technology: IMemoryCache (Phase 1)
