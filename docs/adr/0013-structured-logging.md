# ADR-0013: Structured Logging Strategy — ILogger with Loki-Optimized Message Templates

## Status
`Accepted`

## Date
2026-03-14

## Context

The platform requires a logging strategy that provides full request traceability across modules while avoiding log saturation in Grafana Loki (the centralized log aggregation backend). Two broad approaches were evaluated:

| Approach | Description | Pros | Cons |
|----------|-------------|------|------|
| **String interpolation** | `_logger.LogInformation($"User {userId} signed in")` | Familiar, easy to write | Loses structured properties — Loki stores plain text, cannot filter by field |
| **Structured message templates** | `_logger.LogInformation("User {UserId} signed in", userId)` | Named properties are indexed by Loki as queryable labels/fields | Slightly less familiar to developers new to structured logging |

A secondary concern is **log volume**: logging at `Debug` or `Trace` in production, or logging every framework-internal event, can flood Loki and make signal-to-noise ratio unworkable.

A third concern is **sensitive data leakage**: passwords, JWT tokens, and PII must never appear in log output.

## Decision

### 1. Always use structured message templates

Use named placeholders — never string interpolation or `string.Format` — so that Loki can index and query individual properties.

```csharp
// CORRECT — UserId and Email are queryable fields in Loki
_logger.LogInformation("Sign-in attempt for Email={Email}", command.Email);

// WRONG — produces plain text; Loki cannot filter by field
_logger.LogInformation($"Sign-in attempt for {command.Email}");
```

### 2. Message format convention

```
"[Verb] [outcome/state] — Property={Value}, Property={Value}"
```

Examples:

```csharp
_logger.LogInformation("Sign-in attempt for Email={Email}", command.Email);
_logger.LogWarning("Sign-in failed — user not found for Email={Email}", command.Email);
_logger.LogWarning("Sign-in failed — invalid password for UserId={UserId}, FailedAttempts={FailedAttempts}", user.Id, user.FailedLoginAttempts);
_logger.LogInformation("Sign-in succeeded for UserId={UserId}", user.Id);
```

### 3. Log level guidelines

| Level | When to use |
|-------|-------------|
| `Trace` | Extremely detailed step-by-step execution. Disabled in all environments. |
| `Debug` | Developer investigation: variable values, branch decisions. Development/staging only. |
| `Information` | Normal business flow milestones: request received, resource created, user authenticated. |
| `Warning` | Expected failures or degraded state: invalid credentials, suspended account, retry attempt. |
| `Error` | Unexpected exception — operation failed but service is still running. Requires investigation. |
| `Critical` | Service cannot continue: database unreachable, unhandled crash. Immediate action required. |

**Rule:** if it represents normal expected behavior → `Information` or below. If a human needs to react → `Warning` or above.

### 4. Minimum level per environment

```json
// appsettings.Production.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

```json
// appsettings.Development.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
    }
  }
}
```

This eliminates ASP.NET Core and EF Core internal noise from production Loki.

### 5. Correlation ID middleware

A `CorrelationIdMiddleware` pushes a `CorrelationId` into the Serilog `LogContext` for every request. All log entries within a request automatically carry this property without manual threading through method calls.

```csharp
public async Task InvokeAsync(HttpContext context)
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                        ?? Activity.Current?.TraceId.ToString()
                        ?? Guid.NewGuid().ToString("N");

    context.Response.Headers["X-Correlation-ID"] = correlationId;

    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await _next(context);
    }
}
```

### 6. What must never be logged

| Category | Examples |
|----------|----------|
| Authentication credentials | Passwords, PINs |
| Tokens | JWT access tokens, refresh tokens, API keys |
| PII beyond audit identifiers | Full names combined with national IDs, financial data |

```csharp
// WRONG — logs the token value
_logger.LogInformation("JWT issued: {Token}", jwtToken);

// CORRECT — log only metadata
_logger.LogInformation("Sign-in succeeded for UserId={UserId}", user.Id);
```

### 7. ILogger injection pattern

Use `ILogger<T>` via primary constructor injection. The generic parameter automatically scopes the log category to the class name, visible in Loki.

```csharp
public class SignInCommandHandler(
    IUserRepository userRepository,
    ILogger<SignInCommandHandler> logger
) : IRequestHandler<SignInCommand, SignInResult>
{
    public async Task<SignInResult> Handle(SignInCommand command, CancellationToken ct)
    {
        logger.LogInformation("Sign-in attempt for Email={Email}", command.Email);
        // ...
    }
}
```

## Consequences

**Positive:**
- Log entries are queryable in Loki/Grafana by field (e.g., `{UserId="42"}`) without regex parsing
- `CorrelationId` links all log entries for a single request, enabling full traceability
- Overriding framework namespaces to `Warning` in production drastically reduces write volume to Loki
- Consistent message template convention makes log scanning predictable across modules
- Sensitive data protection is explicit and enforced by convention

**Negative:**
- Developers must learn the message template syntax (named placeholders, not interpolation)
- `ILogger<T>` must be added to every handler/service that needs logging — marginally increases constructor size

**Mitigation:**
- The convention is documented here and enforced via code review
- Primary constructor syntax keeps injection concise

## References
- [Serilog Best Practices — Ben Foster](https://benfoster.io/blog/serilog-best-practices/)
- [5 Serilog Best Practices — Milan Jovanovic](https://www.milanjovanovic.tech/blog/5-serilog-best-practices-for-better-structured-logging)
- [serilog-sinks-grafana-loki — GitHub](https://github.com/serilog-contrib/serilog-sinks-grafana-loki)
- [Logging Best Practices in ASP.NET Core — Anton Dev Tips](https://antondevtips.com/blog/logging-best-practices-in-asp-net-core)
- [Sensitive Data in Logs — Better Stack](https://betterstack.com/community/guides/logging/sensitive-data/)
