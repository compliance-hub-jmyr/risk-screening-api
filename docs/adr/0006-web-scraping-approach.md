# ADR-0006: Web Scraping Strategy — On-Demand with Cache (Phase 1)

## Status
`Accepted`

## Date
2026-03-13

## Context

The Scraping module requires obtaining data from three external sources:
1. **OFAC SDN** — US Treasury, available as a public XML feed
2. **World Bank Debarred Firms** — Web page with a paginated HTML table
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
- Assessment reference: `https://sanctionssearch.ofac.treas.gov/` (web-only form, no REST API)
- Programmatic source: `https://sdn.ofac.treas.gov/SDN_XML.zip` (public ZIP containing SDN XML)
- Method: Download ZIP, decompress in-memory, parse XML with `System.Xml.Linq`
- Cache key: `scraping:ofac:{normalizedQuery}`
- TTL: **10 minutes**

#### World Bank Debarred Firms
- Source: `https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms?srchTerm={query}`
- Method: HTTP GET + HTML table parsing with `HtmlAgilityPack`
- Cache key: `scraping:worldbank:{normalizedQuery}`
- TTL: **10 minutes**

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
- First request for a new query term incurs live fetch latency (OFAC XML download can take 1–3 s)
- If the external source is unavailable at query time, the request fails (no pre-cached fallback)

**Mitigation:**
- Use `Polly` retry + timeout policies on all HTTP clients to reduce transient failure impact
- **Phase 2 (future):** Replace on-demand fetching for OFAC and World Bank with a `BackgroundService` that pre-populates cache on startup and refreshes periodically. ICIJ remains on-demand permanently (dataset size, public API design). This improvement is intentionally deferred to avoid over-engineering Phase 1.

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `HtmlAgilityPack` | 1.11.x | HTML parsing for World Bank |
| `System.Xml.Linq` | .NET 10 | XML parsing for OFAC SDN |
| `Microsoft.Extensions.Http` | .NET 10 | `HttpClientFactory` for typed clients |
| `Polly` | 8.x | Retry and timeout policies for HTTP requests |

## References
- [OFAC SDN List Downloads](https://www.treasury.gov/resource-center/sanctions/SDN-List/Pages/default.aspx)
- [ICIJ Offshore Leaks API](https://offshoreleaks.icij.org/api)
- [HtmlAgilityPack](https://html-agility-pack.net/)
- [Polly — .NET Resilience Library](https://github.com/App-vNext/Polly)
- ADR-0008 — Cache technology: IMemoryCache (Phase 1)
