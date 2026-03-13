# ADR-0003: Autenticacion Dual — JWT Bearer + API Key

## Estado
`Accepted` — Implementado en v0.2.0

## Fecha
2026-03-13

## Contexto

La plataforma tiene dos tipos de consumidores con necesidades distintas:

1. **Modulo Scraping (API de listas):** Consumidores de API que necesitan acceso programatico sin sesion de usuario. Requiere autenticacion via API Key en el header `X-Api-Key`.

2. **Modulo Suppliers / SPA:** Usuarios humanos que hacen login con email/password y mantienen sesion activa en el navegador.

Se necesita una estrategia de autenticacion que soporte ambos casos sin complejidad de infraestructura externa (OAuth2 server, Keycloak, etc.).

## Decision

Implementar **dos esquemas de autenticacion independientes en ASP.NET Core**:

### Esquema 1: JWT Bearer (SPA)

- El usuario hace login via `POST /api/authentication/sign-in`
- El servidor genera un JWT HS256 firmado con la clave secreta configurada
- El token incluye claims: `sub` (userId), `email`, `name`, roles
- Expiracion: configurable via `Jwt:ExpirationHours` (default: 24h)
- Los endpoints del modulo Suppliers usan `[Authorize]` (Bearer scheme)

```csharp
// JWT Claims incluidos
new Claim(ClaimTypes.NameIdentifier, user.Id)
new Claim(ClaimTypes.Email, user.Email.Value)
new Claim(ClaimTypes.Name, user.Username.Value)
new Claim(ClaimTypes.Role, role.Name)  // uno por rol
```

### Esquema 2: API Key (Scraping)

- API keys se generan y almacenan con hash en la base de datos (tabla `api_keys`)
- El consumidor envia la key en el header `X-Api-Key`
- Se implementa como un `AuthenticationHandler` de ASP.NET Core
- Los endpoints del modulo Scraping usan `[Authorize(AuthenticationSchemes = "ApiKey")]`

```http
GET /api/lists/ofac?query=John HTTP/1.1
X-Api-Key: ey-test-key-abc123
```

### Seguridad de Passwords

- Passwords se hashean con **BCrypt (cost factor 12)** antes de almacenar
- Nunca se almacena el password en texto plano
- En produccion, incrementar el cost factor a 13-14

## Consecuencias

**Positivo:**
- Separacion limpia de los dos tipos de autenticacion sin colision
- JWT es stateless — el servidor no necesita almacenar sesiones
- API Keys son simples de usar para integraciones programaticas
- BCrypt es el estandar para password hashing — resistente a rainbow tables

**Negativo:**
- JWT no puede ser revocado sin implementar una blacklist (para este proyecto es aceptable)
- API Keys en texto plano en el header pueden ser interceptadas si no se usa HTTPS (usar HTTPS en produccion)

**Mitigacion:**
- En produccion configurar `UseHttpsRedirection()` y certificado SSL
- Para revocacion de JWT: implementar blacklist en cache si se requiere en el futuro

## Configuracion

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

## Referencias
- [JWT Best Practices — RFC 8725](https://tools.ietf.org/html/rfc8725)
- [ASP.NET Core JWT Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [BCrypt Password Hashing](https://auth0.com/blog/hashing-in-action-understanding-bcrypt/)
- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
