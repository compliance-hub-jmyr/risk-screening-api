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
- Is partitioned per API key (each consumer has its own quota)

The following options were evaluated:

| Option | Description | Precision | Infrastructure |
|--------|-------------|-----------|----------------|
| **Native .NET `SlidingWindowRateLimiter`** | Built-in `System.Threading.RateLimiting` API (ASP.NET Core 7+) | High — per-segment sliding window | None |
| **AspNetCoreRateLimit** | Mature NuGet package — declarative policy config in `appsettings.json` | High — configurable per endpoint/client | None (in-memory) |
| **Redis + custom counter** | Distributed counter using `INCR` + `EXPIRE` | High | Redis |

## Decision

Use **`AspNetCoreRateLimit`** (`AspNetCoreRateLimit 5.0.0` NuGet package) configured as client-based rate limiting, partitioned by the `X-Api-Key` header.

`AspNetCoreRateLimit` was chosen over the native `SlidingWindowRateLimiter` because:
- Policy rules are declarative in `appsettings.json` — no code change needed to adjust limits
- Supports client-specific overrides (`ClientRateLimitPolicies`) out of the box
- Mature package with extensive real-world usage in production ASP.NET APIs

### Configuration in `appsettings.json`

```json
{
  "ClientRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "ClientIdHeader": "X-Api-Key",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "GET:/api/lists/*",
        "Period":   "1m",
        "Limit":    20
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
  "message": "API calls quota exceeded. Maximum 20 requests per 1m per API key."
}
```

## Consequences

**Positive:**
- Rules are fully declarative in `appsettings.json` — tunable without recompiling
- `ClientIdHeader: "X-Api-Key"` partitions each consumer's quota automatically
- `EnableEndpointRateLimiting: true` scopes the limit to `/api/lists/*` only — other endpoints are unaffected
- Standard `Retry-After` header (RFC 6585) in the 429 response
- In-memory counters — no Redis required for Phase 1

**Negative:**
- In-memory state — if multiple API instances run behind a load balancer, each instance has its own counter (limit is not shared across instances)
- Counters are lost on API restart

**Mitigation for production with multiple instances:**
- Replace `MemoryCacheRateLimitCounterStore` with a Redis-backed implementation
- `AspNetCoreRateLimit` supports a Redis store via the `AspNetCoreRateLimit.Redis` package — the configuration stays identical, only the store registration changes

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `AspNetCoreRateLimit` | 5.0.0 | Client-based rate limiting middleware with per-key quotas |

## References
- [AspNetCoreRateLimit on GitHub](https://github.com/stefanprodan/AspNetCoreRateLimit)
- [AspNetCoreRateLimit on NuGet](https://www.nuget.org/packages/AspNetCoreRateLimit)
- [RFC 6585 — HTTP 429 Too Many Requests](https://tools.ietf.org/html/rfc6585)
- [ASP.NET Core Rate Limiting (native) — Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
