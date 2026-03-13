# ADR-0003: Dual Authentication — JWT Bearer + API Key

## Status
`Accepted` — Implemented in v0.2.0

## Date
2026-03-13

## Context

The platform has two types of consumers with different needs:

1. **Scraping Module (lists API):** API consumers that need programmatic access without a user session. Requires authentication via API Key in the `X-Api-Key` header.

2. **Suppliers Module / SPA:** Human users who log in with email/password and maintain an active session in the browser.

An authentication strategy is needed that supports both cases without external infrastructure complexity (OAuth2 server, Keycloak, etc.).

## Decision

Implement **two independent authentication schemes in ASP.NET Core**:

### Scheme 1: JWT Bearer (SPA)

- The user logs in via `POST /api/authentication/sign-in`
- The server generates an HS256 JWT signed with the configured secret key
- The token includes claims: `sub` (userId), `email`, `name`, roles
- Expiration: configurable via `Jwt:ExpirationHours` (default: 24h)
- Suppliers module endpoints use `[Authorize]` (Bearer scheme)

```csharp
// JWT Claims included
new Claim(ClaimTypes.NameIdentifier, user.Id)
new Claim(ClaimTypes.Email, user.Email.Value)
new Claim(ClaimTypes.Name, user.Username.Value)
new Claim(ClaimTypes.Role, role.Name)  // one per role
```

### Scheme 2: API Key (Scraping)

- API keys are generated and stored as hashes in the database (`api_keys` table)
- The consumer sends the key in the `X-Api-Key` header
- Implemented as an ASP.NET Core `AuthenticationHandler`
- Scraping module endpoints use `[Authorize(AuthenticationSchemes = "ApiKey")]`

```http
GET /api/lists/ofac?query=John HTTP/1.1
X-Api-Key: ey-test-key-abc123
```

### Password Security

- Passwords are hashed with **BCrypt (cost factor 12)** before storage
- Plaintext passwords are never stored
- In production, increase the cost factor to 13–14

## Consequences

**Positive:**
- Clean separation of the two authentication types without collision
- JWT is stateless — the server does not need to store sessions
- API Keys are simple to use for programmatic integrations
- BCrypt is the standard for password hashing — resistant to rainbow tables

**Negative:**
- JWT cannot be revoked without implementing a blacklist (acceptable for this project)
- API Keys in plaintext in the header can be intercepted if HTTPS is not used (use HTTPS in production)

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
