# ADR-0005: Rate Limiting with AspNetCoreRateLimit

## Status
`Accepted`

## Date
2026-03-13

## Context

The platform specification requires: **"Maximum number of calls per minute: 20"** as a requirement for the scraping API.

A rate limiting strategy is needed that:
- Is precise (avoids bursts at the start of each minute)
- Does not require external infrastructure for the development environment
- Is easy to configure and monitor
- Is partitioned per client IP (since all endpoints are JWT-protected, there is no API key to partition by)

The following options were evaluated:

| Option | Description | Precision | Infrastructure |
|--------|-------------|-----------|----------------|
| **Native .NET `SlidingWindowRateLimiter`** | Built-in `System.Threading.RateLimiting` API (ASP.NET Core 7+) | High — per-segment sliding window | None |
| **AspNetCoreRateLimit** | Mature NuGet package — declarative policy config in `appsettings.json` | High — configurable per endpoint/client | None (in-memory) |
| **Redis + custom counter** | Distributed counter using `INCR` + `EXPIRE` | High | Redis |

## Decision

Use **`AspNetCoreRateLimit`** (`AspNetCoreRateLimit 5.0.0` NuGet package) configured as client-based rate limiting, partitioned by client IP address.

`AspNetCoreRateLimit` was chosen over the native `SlidingWindowRateLimiter` because:
- Policy rules are declarative in `appsettings.json` — no code change needed to adjust limits
- Supports client-specific overrides (`ClientRateLimitPolicies`) out of the box
- Mature package with extensive real-world usage in production ASP.NET APIs

> **Note:** The initial design partitioned by `X-Api-Key` header. Since API Key authentication was removed (see [ADR-0003](./0003-jwt-authentication.md)), the partition is now by client IP (`ClientIdHeader: "X-Forwarded-For"`).

### Tiered rate limiting strategy

Rules are evaluated from most specific to most general:

| Endpoint | Limit | Rationale |
|----------|-------|-----------|
| `POST /api/authentication/sign-in` | 5 req/min | Public endpoint — brute-force protection |
| `GET /api/lists/*` | 20 req/min | External source protection (spec requirement) |
| `*:/api/*` | 100 req/min | General fallback for authenticated CRUD endpoints |

### Configuration in `appsettings.json`

```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Forwarded-For",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "POST:/api/authentication/sign-in",
        "Period":   "1m",
        "Limit":    5
      },
      {
        "Endpoint": "GET:/api/lists/*",
        "Period":   "1m",
        "Limit":    20
      },
      {
        "Endpoint": "*:/api/*",
        "Period":   "1m",
        "Limit":    100
      }
    ]
  }
}
```

### Service registration in `Program.cs`

```csharp
builder.Services.AddMemoryCache();
builder.Services.Configure<ClientRateLimitOptions>(
    builder.Configuration.GetSection("ClientRateLimiting"));
builder.Services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddClientRateLimiting();

// ...

app.UseClientRateLimiting();
```

### HTTP response when the limit is exceeded

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 30
Content-Type: application/json

{
  "message": "API calls quota exceeded. Maximum 20 requests per 1m."
}
```

## Consequences

**Positive:**
- Rules are fully declarative in `appsettings.json` — tunable without recompiling
- `EnableEndpointRateLimiting: true` enables per-endpoint rules — tiered limits protect sign-in, scraping, and general API independently
- Standard `Retry-After` header (RFC 6585) in the 429 response
- In-memory counters — no Redis required for Phase 1

**Negative:**
- In-memory state — if multiple API instances run behind a load balancer, each instance has its own counter (limit is not shared across instances)
- Counters are lost on API restart
- IP-based partitioning can be spoofed via proxies (acceptable for Phase 1)

**Mitigation for production with multiple instances:**
- Replace `MemoryCacheRateLimitCounterStore` with a Redis-backed implementation
- `AspNetCoreRateLimit` supports a Redis store via the `AspNetCoreRateLimit.Redis` package — the configuration stays identical, only the store registration changes

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `AspNetCoreRateLimit` | 5.0.0 | Client-based rate limiting middleware with per-IP quotas |

## References
- [AspNetCoreRateLimit on GitHub](https://github.com/stefanprodan/AspNetCoreRateLimit)
- [AspNetCoreRateLimit on NuGet](https://www.nuget.org/packages/AspNetCoreRateLimit)
- [RFC 6585 — HTTP 429 Too Many Requests](https://tools.ietf.org/html/rfc6585)
- [ASP.NET Core Rate Limiting (native) — Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
