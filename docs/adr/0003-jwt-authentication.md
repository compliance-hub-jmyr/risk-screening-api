# ADR-0003: Authentication — JWT Bearer

## Status
`Accepted` — Implemented in v0.2.0

## Date
2026-03-13

## Context

The platform serves two types of consumers:

1. **Scraping Module (lists API):** Programmatic access to query high-risk lists. Initially considered an API Key scheme for this module; however, the specification does not require API keys — all endpoints on the platform are consumed by authenticated users or the SPA.

2. **Suppliers Module / SPA:** Human users who log in with email/password and maintain an active session in the browser.

An authentication strategy is needed that supports both cases without external infrastructure complexity (OAuth2 server, Keycloak, etc.).

## Decision

Use a **single authentication scheme: JWT Bearer** for all API endpoints, including the Scraping module.

The API Key approach was considered and an initial implementation was built (`ApiKeyAuthHandler`, `api_keys` table, `V004__create_api_keys_table.sql`) but **discarded** because:
- The spec does not require a separate machine-to-machine auth mechanism.
- All scraping endpoints are consumed either by the SPA (which already holds a JWT) or by developers testing via Swagger (which supports Bearer auth natively).
- A second auth scheme adds unnecessary complexity and a DB table with no functional benefit.

### JWT Bearer Authentication

- The user authenticates via `POST /api/authentication/sign-in`
- The server generates an HS256 JWT signed with the configured secret key
- The token includes claims: `sub` (userId), `email`, `name`, roles
- Expiration: configurable via `Jwt:ExpirationHours` (default: 24h)
- **All** module endpoints (IAM, Scraping, Suppliers) use `[Authorize]` (Bearer scheme)

```csharp
// JWT Claims included
new Claim(ClaimTypes.NameIdentifier, user.Id)
new Claim(ClaimTypes.Email, user.Email.Value)
new Claim(ClaimTypes.Name, user.Username.Value)
new Claim(ClaimTypes.Role, role.Name)  // one per role
```

### Password Security

- Passwords are hashed with **BCrypt (cost factor 12)** before storage
- Plaintext passwords are never stored
- In production, increase the cost factor to 13–14

## Consequences

**Positive:**
- Single, unified auth mechanism — less code, less cognitive overhead
- JWT is stateless — the server does not need to store sessions
- Swagger UI can test all endpoints with a single Bearer token
- BCrypt is the standard for password hashing — resistant to rainbow tables

**Negative:**
- JWT cannot be revoked without implementing a blacklist (acceptable for this project)
- All endpoints require a valid JWT — no anonymous or API-key-based access

**Mitigation:**
- In production, configure `UseHttpsRedirection()` and an SSL certificate
- For JWT revocation: implement a cache-based blacklist if required in the future

## Configuration

```json
// appsettings.json
{
  "Jwt": {
    "Key": "your-super-secret-key-at-least-32-chars",
    "Issuer": "RiskScreening",
    "Audience": "RiskScreening",
    "ExpirationHours": 24
  }
}
```

## References
- [JWT Best Practices — RFC 8725](https://tools.ietf.org/html/rfc8725)
- [ASP.NET Core JWT Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [BCrypt Password Hashing](https://auth0.com/blog/hashing-in-action-understanding-bcrypt/)
- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
