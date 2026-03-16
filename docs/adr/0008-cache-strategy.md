# ADR-0008: Cache Strategy — IMemoryCache (Phase 1)

## Status
`Accepted`

## Date
2026-03-13

## Context

The Scraping module fetches data from three external sources (OFAC SDN, World Bank Debarred Firms, ICIJ Offshore Leaks). These sources are updated infrequently (daily to weekly), but individual queries may be repeated frequently. Fetching them live on every request would add 1–3 s of latency and risk hitting rate limits or triggering scraping detection.

A caching layer is required that:

- Reduces latency for repeated list data lookups.
- Prevents repeated live fetches of external sources on every request.
- Is simple to implement and operate in Phase 1 (single-instance deployment).
- Can be evolved to a distributed cache (Redis) in a future milestone without significant rework.

> How and when the cache is populated (on-demand vs background worker) is documented in **ADR-0006**.

---

## Decision

**Use `IMemoryCache` (in-process memory cache) as the Phase 1 cache layer.**

All cache interactions are encapsulated behind a `ScrapingCacheService` so that the backing implementation can be swapped without touching application logic.

### Cache key convention

```
{module}:{resource}:{qualifier}
```

Examples:

| Key | TTL | Description |
|-----|-----|-------------|
| `scraping:ofac:{query}` | 10 min | OFAC SDN result for a given query term |
| `scraping:worldbank:{query}` | 10 min | World Bank result for a given query term |
| `scraping:icij:{query}` | 10 min | ICIJ Offshore Leaks result for a given query term |
| `rate_limit:{clientIp}` | 60 s (sliding) | Rate-limiting window per client IP |

### TTL strategy

- Cache entries are written on first cache miss (on-demand population per ADR-0006 Phase 1).
- TTL expiry ensures stale data is evicted automatically; the next request after expiry triggers a fresh live fetch.
- Rate-limiting keys use a sliding-window TTL managed directly via `IMemoryCache` with explicit timestamp tracking.

---

## Evaluated Options

### Option A — No cache (always fetch on demand)

| Pros | Cons |
|------|------|
| No complexity | High latency (1–3 s per external call) |
| Always fresh data | Risk of IP banning / scraping detection |
| | Not viable at > 1 req/s |

### Option B — `IMemoryCache` Selected

| Pros | Cons |
|------|------|
| Zero infrastructure overhead | In-process: lost on restart |
| Native .NET 10 — no extra packages | Not shared across multiple instances |
| Thread-safe, low-latency | Memory bounded per process |
| Simple to test (injectable) | Requires graceful TTL design |

### Option C — `IDistributedCache` + Redis

| Pros | Cons |
|------|------|
| Survives process restarts | Requires Redis infrastructure |
| Shared across instances | Added operational complexity |
| Scales horizontally | Over-engineered for Phase 1 (single instance) |

---

## Consequences

### Positive
- Sub-millisecond repeated lookups for all scraping queries.
- Zero infrastructure dependencies in Phase 1.
- `IMemoryCache` is fully injectable — cache layer is unit-testable via mock.
- `ScrapingCacheService` interface isolates the implementation — swapping to Redis requires no changes to application logic.

### Negative / Mitigations

| Risk | Mitigation |
|------|-----------|
| Cache lost on process restart | All entries are repopulated on next request (on-demand strategy per ADR-0006) |
| Multi-instance deployment (future) | Replace `IMemoryCache` with Redis-backed `IDistributedCache` — `ScrapingCacheService` interface isolates the swap |
| Memory growth | Set `SizeLimit` on `MemoryCacheOptions` and use `SetSize(1)` per entry to cap total memory |

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.Extensions.Caching.Memory` | Included in .NET 10 SDK | `IMemoryCache` implementation |

---

## Future Work

- **Phase 2:** Replace `IMemoryCache` with Redis (`StackExchange.Redis` + `IDistributedCache`) for multi-instance support.
- **Phase 2:** Add cache hit/miss metrics via `ILogger` structured logging or OpenTelemetry counters.
- **Phase 2:** Externalize TTL values to `appsettings.json` under `Cache:Scraping:OfacTtlMinutes`, etc.
