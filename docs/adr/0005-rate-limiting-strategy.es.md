# ADR-0005: Rate Limiting con AspNetCoreRateLimit

## Estado
`Accepted`

## Fecha
2026-03-13

## Contexto

El requerimiento de la plataforma especifica: **"Número máximo de llamadas por minuto: 20"** para la API de scraping.

Se necesita una estrategia de rate limiting que:
- Sea precisa (evite bursts al inicio de cada minuto)
- No requiera infraestructura externa para el entorno de desarrollo
- Sea fácil de configurar y monitorear
- Esté particionada por IP del cliente (dado que todos los endpoints están protegidos por JWT, no existe una API key para particionar)

Se evaluaron las siguientes opciones:

| Opción | Descripción | Precisión | Infraestructura |
|--------|-------------|-----------|----------------|
| **`SlidingWindowRateLimiter` nativo de .NET** | API `System.Threading.RateLimiting` incorporada (ASP.NET Core 7+) | Alta — ventana deslizante por segmentos | Ninguna |
| **AspNetCoreRateLimit** | Paquete NuGet maduro — configuración declarativa de políticas en `appsettings.json` | Alta — configurable por endpoint/cliente | Ninguna (en memoria) |
| **Redis + contador custom** | Contador distribuido usando `INCR` + `EXPIRE` | Alta | Redis |

## Decisión

Usar **`AspNetCoreRateLimit`** (paquete NuGet `AspNetCoreRateLimit 5.0.0`) configurado como rate limiting basado en cliente, particionado por dirección IP del cliente.

Se eligió `AspNetCoreRateLimit` sobre el `SlidingWindowRateLimiter` nativo porque:
- Las reglas de política son declarativas en `appsettings.json` — no se requiere cambio de código para ajustar los límites
- Soporta overrides por cliente (`ClientRateLimitPolicies`) de forma nativa
- Paquete maduro con uso extensivo en producción en APIs ASP.NET

> **Nota:** El diseño inicial particionaba por el header `X-Api-Key`. Dado que la autenticación por API Key fue eliminada (ver [ADR-0003](./0003-jwt-authentication.es.md)), la partición se realiza ahora por IP del cliente (`ClientIdHeader: "X-Forwarded-For"`).

### Estrategia de rate limiting escalonado

Las reglas se evalúan de la más específica a la más general:

| Endpoint | Límite | Justificación |
|----------|--------|---------------|
| `POST /api/authentication/sign-in` | 5 req/min | Endpoint público — protección contra fuerza bruta |
| `GET /api/lists/*` | 20 req/min | Protección de fuentes externas (requerimiento de spec) |
| `*:/api/*` | 100 req/min | Fallback general para endpoints CRUD autenticados |

### Configuración en `appsettings.json`

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

### Registro de servicios en `Program.cs`

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

### Respuesta HTTP cuando se supera el límite

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 30
Content-Type: application/json

{
  "message": "API calls quota exceeded. Maximum 20 requests per 1m."
}
```

## Consecuencias

**Positivo:**
- Las reglas son completamente declarativas en `appsettings.json` — ajustables sin recompilar
- `EnableEndpointRateLimiting: true` habilita reglas por endpoint — límites escalonados protegen sign-in, scraping y API general de forma independiente
- Header `Retry-After` estándar (RFC 6585) en la respuesta 429
- Contadores en memoria — no se requiere Redis para la Fase 1

**Negativo:**
- Estado en memoria — si múltiples instancias de la API corren detrás de un load balancer, cada instancia tiene su propio contador (el límite no se comparte entre instancias)
- Los contadores se pierden al reiniciar la API
- La partición por IP puede ser evadida via proxies (aceptable para la Fase 1)

**Mitigación para producción con múltiples instancias:**
- Reemplazar `MemoryCacheRateLimitCounterStore` con una implementación respaldada por Redis
- `AspNetCoreRateLimit` soporta un store Redis via el paquete `AspNetCoreRateLimit.Redis` — la configuración permanece idéntica, solo cambia el registro del store

## Dependencias

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `AspNetCoreRateLimit` | 5.0.0 | Middleware de rate limiting basado en cliente con cuotas por IP |

## Referencias
- [AspNetCoreRateLimit en GitHub](https://github.com/stefanprodan/AspNetCoreRateLimit)
- [AspNetCoreRateLimit en NuGet](https://www.nuget.org/packages/AspNetCoreRateLimit)
- [RFC 6585 — HTTP 429 Too Many Requests](https://tools.ietf.org/html/rfc6585)
- [ASP.NET Core Rate Limiting (nativo) — Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
