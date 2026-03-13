# ADR-0010: Error Handling Strategy — GlobalExceptionHandler + ProblemDetails

## Status
`Accepted`

## Date
2026-03-13

## Context

The API must translate exceptions thrown anywhere in the request pipeline — domain logic, validation, infrastructure — into structured, consistent HTTP responses. Without a centralized strategy, each controller or service would handle errors independently, leading to:

- Inconsistent response bodies (some return plain strings, others return JSON objects).
- Duplicate try/catch blocks across controllers.
- Frontend clients that cannot rely on a stable error contract.

Three cross-cutting error types must be handled:

| Source | Example | Expected HTTP |
|--------|---------|---------------|
| FluentValidation pipeline behavior | Required field missing, format invalid | `400 Bad Request` |
| Domain exceptions | Duplicate `TaxId`, user not found, supplier already deleted | `409 Conflict`, `404 Not Found`, `401 Unauthorized` |
| Unhandled / infrastructure exceptions | DB connection failure, null reference | `500 Internal Server Error` |

Additionally, the error response body shape must:
- Be **machine-readable** — the Angular SPA must be able to extract field-level validation errors for form display.
- Be **standard** — avoid inventing a proprietary error envelope.
- Not leak internal stack traces or implementation details to clients.

---

## Decision

**Use a chain of `IExceptionHandler` implementations (ASP.NET Core .NET 10) with a custom `ErrorResponse` record that extends RFC 7807 Problem Details.**

### Mechanism: `IExceptionHandler` chain

`IExceptionHandler` (introduced in .NET 8, available in .NET 10) is the preferred ASP.NET Core extension point for centralized exception handling. Three handlers are registered in priority order:

```
ValidationExceptionHandler → DomainExceptionHandler → GlobalExceptionHandler
```

Each handler:
- Is registered in `Program.cs` via `builder.Services.AddExceptionHandler<T>()`.
- Is invoked by `UseExceptionHandler()` middleware in registration order.
- Returns `true` to short-circuit the chain, or `false` to pass to the next handler.
- Is fully injectable via constructor DI and testable in isolation.

### Response shape: RFC 7807 extended with machine-readable codes

All error responses follow [RFC 7807 Problem Details for HTTP APIs](https://datatracker.ietf.org/doc/html/rfc7807) using the standard fields (`type`, `title`, `status`, `instance`) extended with additional members as permitted by RFC 7807 3.2.

The concrete type is `ErrorResponse` — a custom record in `Shared/Interfaces/REST/Resources/`.

**400 Validation error:**
```json
{
  "type":        "https://tools.ietf.org/html/rfc7807",
  "title":       "Validation failed",
  "status":      400,
  "instance":    "/api/v1/suppliers",
  "errorNumber": 1000,
  "errorCode":   "VALIDATION_FAILED",
  "message":     "Request validation failed. Check field errors for details.",
  "timestamp":   "2026-03-13T10:15:30Z",
  "fieldErrors": [
    { "field": "taxId", "message": "TaxId must be exactly 11 digits.", "rejectedValue": "123" },
    { "field": "legalName", "message": "LegalName is required.", "rejectedValue": "" }
  ]
}
```

**404 Not Found:**
```json
{
  "type":        "https://tools.ietf.org/html/rfc7807",
  "title":       "Resource not found",
  "status":      404,
  "instance":    "/api/v1/suppliers/3fa85f64",
  "errorNumber": 4000,
  "errorCode":   "ENTITY_NOT_FOUND",
  "message":     "Supplier not found with id: 3fa85f64",
  "timestamp":   "2026-03-13T10:15:30Z"
}
```

The `fieldErrors` extension property is populated only for `400 Bad Request` responses from `ValidationExceptionHandler`.

**Why extend RFC 7807 instead of using it as-is?**

RFC 7807 alone provides `type`, `title`, `status`, and `instance` — sufficient for humans but not for programmatic client handling. The Angular SPA needs to identify the specific error type to display the correct message or highlight the correct form field. `errorCode` (a string constant like `"ENTITY_NOT_FOUND"`) enables reliable `switch`/`case` handling in Angular interceptors without fragile string-parsing of `title` or `detail`. `errorNumber` provides a stable integer for support log correlation.

### Exception-to-HTTP mapping

| Exception type | HTTP status | `title` | `errorCode` |
|----------------|-------------|---------|-------------|
| `ValidationException` (FluentValidation) | `400 Bad Request` | `Validation failed` | `VALIDATION_FAILED` |
| `EntityNotFoundException` (DomainException) | `404 Not Found` | `Resource not found` | *(set by the exception)* |
| `BusinessRuleViolationException` (DomainException) | `409 Conflict` | `Business rule violation` | *(set by the exception)* |
| `AuthenticationException` (DomainException) | `401 Unauthorized` | `Unauthorized` | *(set by the exception)* |
| `AuthorizationException` (DomainException) | `403 Forbidden` | `Forbidden` | *(set by the exception)* |
| `Exception` (catch-all) | `500 Internal Server Error` | `An unexpected error occurred` | `INTERNAL_SERVER_ERROR` |

### Domain exception hierarchy

All domain exceptions extend `DomainException` in `Shared/Domain/Exceptions/` and carry their own `ErrorNumber` and `ErrorCode`:

```
DomainException (abstract)
├── EntityNotFoundException
├── BusinessRuleViolationException
├── AuthenticationException
├── AuthorizationException
├── DomainValidationException
└── InvalidValueException
```

`DomainExceptionHandler` matches by type and sets `title` and HTTP status accordingly.

### Security: no stack trace leakage

- `500` responses never include `exception.Message` or stack traces in the response body.
- In development (`IWebHostEnvironment.IsDevelopment()`), `message` contains the exception message to aid debugging.
- In production, `message` is always the generic string `"An unexpected error occurred. Please try again later."`.
- The full exception is logged via `ILogger<GlobalExceptionHandler>` at `Error` level.

---

## Evaluated Options

### Option A — `IExceptionHandler` (ASP.NET Core) Selected

| Pros | Cons |
|------|------|
| Native .NET 10 — no extra packages | Requires `UseExceptionHandler()` middleware to be registered |
| Injectable via DI | |
| Can short-circuit (return `true`) or delegate to next handler | |
| Works alongside `UseStatusCodePages` | |

### Option B — Custom exception-handling middleware

| Pros | Cons |
|------|------|
| Full control over the pipeline | Boilerplate: must manually read `context.Response`, set content type, serialize JSON |
| | Not the idiomatic .NET 10 approach |
| | Harder to test without `WebApplicationFactory` |

### Option C — Exception filters (`IExceptionFilter`)

| Pros | Cons |
|------|------|
| Works at MVC action level | Does not catch exceptions outside MVC (middleware, model binding) |
| Easy to scope per controller | |
| | Incomplete coverage — not suitable as the sole strategy |

### Option D — Per-controller try/catch

| Pros | Cons |
|------|------|
| Explicit | Massive code duplication |
| | Inconsistent error shapes per developer |
| | Untestable at scale |

---

## Consequences

### Positive
- Single place to modify error handling behavior — no scattered try/catch across controllers.
- RFC 7807 `ProblemDetails` is a well-known standard — Angular HTTP interceptors can parse it reliably.
- Field-level validation errors in the `errors` extension property enable direct form binding in PrimeNG reactive forms.
- `DomainException` hierarchy decouples module-specific errors from HTTP semantics.
- `traceId` in every error response enables log correlation without exposing internals.

### Negative / Mitigations

| Risk | Mitigation |
|------|-----------|
| New exception type not mapped — falls through to 500 | `DomainException` base class is caught before the catch-all; adding a new subclass only requires updating the mapping table in `GlobalExceptionHandler` |
| Verbose `ProblemDetails` shape may feel over-engineered for simple errors | RFC 7807 is the industry standard; Angular clients benefit from the predictability |

---

## Dependencies

No additional packages required. `ProblemDetails` and `IExceptionHandler` are part of the ASP.NET Core .NET 10 SDK.

---

## Related Decisions

- **ADR-0002** — CQRS + MediatR: `ValidationBehavior` in the MediatR pipeline throws `ValidationException`, which is caught here and mapped to `400`.
- **ADR-0009** — Pagination: `PageRequest` size violations are raised as `ValidationException` and handled by this ADR.
