# ADR-0003: Autenticación — JWT Bearer

## Estado
`Accepted` — Implementado en v0.2.0

## Fecha
2026-03-13

## Contexto

La plataforma sirve a dos tipos de consumidores:

1. **Módulo Scraping (API de listas):** Acceso programático para consultar listas de alto riesgo. Inicialmente se consideró un esquema de API Key para este módulo; sin embargo, la especificación no requiere API keys — todos los endpoints de la plataforma son consumidos por usuarios autenticados o por la SPA.

2. **Módulo Suppliers / SPA:** Usuarios humanos que hacen login con email/contraseña y mantienen una sesión activa en el navegador.

Se necesita una estrategia de autenticación que soporte ambos casos sin complejidad de infraestructura externa (servidor OAuth2, Keycloak, etc.).

## Decisión

Usar un **único esquema de autenticación: JWT Bearer** para todos los endpoints de la API, incluyendo el módulo Scraping.

El enfoque de API Key fue evaluado y se construyó una implementación inicial (`ApiKeyAuthHandler`, tabla `api_keys`, `V004__create_api_keys_table.sql`) pero fue **descartado** porque:
- La especificación no requiere un mecanismo de autenticación machine-to-machine independiente.
- Todos los endpoints de scraping son consumidos por la SPA (que ya posee un JWT) o por desarrolladores probando via Swagger (que soporta Bearer auth de forma nativa).
- Un segundo esquema de autenticación añade complejidad innecesaria y una tabla en base de datos sin beneficio funcional real.

### Autenticación JWT Bearer

- El usuario se autentica via `POST /api/authentication/sign-in`
- El servidor genera un JWT HS256 firmado con la clave secreta configurada
- El token incluye claims: `sub` (userId), `email`, `name`, roles
- Expiración: configurable via `Jwt:ExpirationHours` (por defecto: 24h)
- **Todos** los endpoints de los módulos (IAM, Scraping, Suppliers) usan `[Authorize]` (Bearer scheme)

```csharp
// JWT Claims incluidos
new Claim(ClaimTypes.NameIdentifier, user.Id)
new Claim(ClaimTypes.Email, user.Email.Value)
new Claim(ClaimTypes.Name, user.Username.Value)
new Claim(ClaimTypes.Role, role.Name)  // uno por rol
```

### Seguridad de Contraseñas

- Las contraseñas se hashean con **BCrypt (cost factor 12)** antes de almacenarse
- Nunca se almacena la contraseña en texto plano
- En producción, incrementar el cost factor a 13–14

## Consecuencias

**Positivo:**
- Mecanismo de autenticación único y uniforme — menos código, menor carga cognitiva
- JWT es stateless — el servidor no necesita almacenar sesiones
- Swagger UI puede probar todos los endpoints con un único token Bearer
- BCrypt es el estándar para hash de contraseñas — resistente a rainbow tables

**Negativo:**
- JWT no puede ser revocado sin implementar una blacklist (aceptable para este proyecto)
- Todos los endpoints requieren un JWT válido — no hay acceso anónimo ni via API key

**Mitigación:**
- En producción configurar `UseHttpsRedirection()` y certificado SSL
- Para revocación de JWT: implementar blacklist en cache si se requiere en el futuro

## Configuración

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
