# ADR-0012: API Versioning Strategy — Header-Based with `Api-Version`

## Status
`Accepted`

## Date
2026-03-14

## Context

The API requires a versioning strategy to allow future evolution of contracts without breaking existing clients (the Angular SPA and Postman collections). Three placement strategies were evaluated:

| Strategy | Example | Pros | Cons |
|----------|---------|------|------|
| **URL segment** | `GET /api/v1/suppliers` | Visible, bookmarkable, easy to test in browser | Version becomes part of the resource identifier — violates REST semantics; URLs must change on every major version |
| **Header-based** | `GET /api/suppliers` + `Api-Version: 1` | Clean URLs; version is request metadata, not resource identity | Not directly testable in a browser address bar |
| **Query string** | `GET /api/suppliers?api-version=1` | No URL change | Pollutes query string; caching may be affected |

## Decision

Use **header-based versioning** via a custom `Api-Version` request header, implemented with the `Asp.Versioning.Http` NuGet package.

### Configuration

```csharp
// WebApplicationBuilderExtensions.cs
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion               = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions               = true;
    options.ApiVersionReader               = new HeaderApiVersionReader("Api-Version");
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat           = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

### Constants

```csharp
// Shared/Infrastructure/Configuration/ApiVersioning.cs
public static class ApiVersioning
{
    public const string Base   = "/api";
    public const string V1     = "1.0";
    public const string V2     = "2.0";   // reserved
    public const string Header = "Api-Version";
}
```

### Client usage

```http
GET /api/suppliers
Api-Version: 1
Authorization: Bearer eyJ...
```

### Default version behavior

`AssumeDefaultVersionWhenUnspecified = true` means clients that omit the header receive version `1.0`. This ensures backward compatibility and allows the Postman collection and the Angular SPA to work without requiring the header explicitly in Phase 1.

### Controller declaration

Each controller declares the versions it supports via `[ApiVersion]`:

```csharp
[ApiController]
[Route("api/[controller]")]
[ApiVersion(ApiVersioning.V1)]
public class SuppliersController : ControllerBase { ... }
```

When a breaking change requires a V2, only the affected controller gets a new class tagged `[ApiVersion(ApiVersioning.V2)]`; all other controllers remain unchanged.

### Response headers

With `ReportApiVersions = true`, every response includes:

```http
api-supported-versions: 1.0
```

This lets clients discover available versions programmatically.

## Consequences

**Positive:**
- URLs are clean and stable across versions — `/api/suppliers` never changes
- Version is correctly treated as request metadata, not resource identity
- `AssumeDefaultVersionWhenUnspecified = true` ensures zero friction for existing clients
- Adding V2 to a single endpoint does not affect any other controller
- Swagger UI groups endpoints by version via `AddApiExplorer`

**Negative:**
- Not directly testable by pasting a URL in a browser — requires a client that can set headers (Postman, curl, Angular `HttpClient`)
- Less discoverable than URL versioning for developers unfamiliar with the API

**Mitigation:**
- Postman collection documents the `Api-Version: 1` header in every request
- Swagger UI exposes the version header as a parameter on every operation
- The default version fallback (`AssumeDefaultVersionWhenUnspecified`) means casual testing without the header still works

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Asp.Versioning.Http` | 8.x | Header-based API versioning for ASP.NET Core |
| `Asp.Versioning.Mvc.ApiExplorer` | 8.x | Swagger/OpenAPI integration for versioned endpoints |

## References
- [Asp.Versioning on GitHub](https://github.com/dotnet/aspnet-api-versioning)
- [API Versioning Best Practices — Troy Hunt](https://www.troyhunt.com/your-api-versioning-is-wrong-which-is/)
- [REST API Versioning — Microsoft REST API Guidelines](https://github.com/microsoft/api-guidelines/blob/vNext/azure/Guidelines.md#api-versioning)
