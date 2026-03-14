# ADR-0013: Estrategia de Logging Estructurado — ILogger con Message Templates optimizados para Loki

## Estado
`Accepted`

## Fecha
2026-03-14

## Contexto

La plataforma requiere una estrategia de logging que proporcione trazabilidad completa de las solicitudes entre módulos, sin saturar Grafana Loki (el backend centralizado de agregación de logs). Se evaluaron dos enfoques principales:

| Enfoque | Descripción | Ventajas | Desventajas |
|---------|-------------|----------|-------------|
| **Interpolación de strings** | `_logger.LogInformation($"Usuario {userId} inició sesión")` | Familiar, fácil de escribir | Pierde las propiedades estructuradas — Loki almacena texto plano, no puede filtrar por campo |
| **Message templates estructurados** | `_logger.LogInformation("Usuario {UserId} inició sesión", userId)` | Las propiedades nombradas son indexadas por Loki como labels/campos consultables | Ligeramente menos familiar para desarrolladores nuevos en logging estructurado |

Una segunda preocupación es el **volumen de logs**: loguear en `Debug` o `Trace` en producción, o loguear cada evento interno del framework, puede inundar Loki y hacer la relación señal/ruido inmanejable.

Una tercera preocupación es la **fuga de datos sensibles**: contraseñas, tokens JWT y PII nunca deben aparecer en la salida de logs.

## Decisión

### 1. Siempre usar message templates estructurados

Usar placeholders nombrados — nunca interpolación de strings ni `string.Format` — para que Loki pueda indexar y consultar propiedades individuales.

```csharp
// CORRECTO — UserId y Email son campos consultables en Loki
_logger.LogInformation("Sign-in attempt for Email={Email}", command.Email);

// INCORRECTO — produce texto plano; Loki no puede filtrar por campo
_logger.LogInformation($"Sign-in attempt for {command.Email}");
```

### 2. Convención de formato del mensaje

```
"[Verbo] [resultado/estado] — Property={Value}, Property={Value}"
```

Ejemplos:

```csharp
_logger.LogInformation("Sign-in attempt for Email={Email}", command.Email);
_logger.LogWarning("Sign-in failed — user not found for Email={Email}", command.Email);
_logger.LogWarning("Sign-in failed — invalid password for UserId={UserId}, FailedAttempts={FailedAttempts}", user.Id, user.FailedLoginAttempts);
_logger.LogInformation("Sign-in succeeded for UserId={UserId}", user.Id);
```

### 3. Guía de niveles de log

| Nivel | Cuándo usarlo |
|-------|---------------|
| `Trace` | Detalle extremo paso a paso de la ejecución. Deshabilitado en todos los entornos. |
| `Debug` | Investigación de desarrollo: valores de variables, decisiones de branches. Solo desarrollo/staging. |
| `Information` | Hitos normales del flujo de negocio: solicitud recibida, recurso creado, usuario autenticado. |
| `Warning` | Fallos esperados o estado degradado: credenciales inválidas, cuenta suspendida, reintento. |
| `Error` | Excepción inesperada — operación fallida pero el servicio sigue funcionando. Requiere investigación. |
| `Critical` | El servicio no puede continuar: base de datos inaccesible, crash no controlado. Acción inmediata requerida. |

**Regla:** si representa un comportamiento esperado → `Information` o inferior. Si requiere que un humano reaccione → `Warning` o superior.

### 4. Nivel mínimo por entorno

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

Esto elimina el ruido interno de ASP.NET Core y EF Core de Loki en producción.

### 5. Middleware de Correlation ID

Un `CorrelationIdMiddleware` inserta un `CorrelationId` en el `LogContext` de Serilog para cada solicitud. Todas las entradas de log dentro de una solicitud llevan esta propiedad automáticamente sin necesidad de pasarla manualmente por los métodos.

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

### 6. Qué nunca debe loguearse

| Categoría | Ejemplos |
|-----------|----------|
| Credenciales de autenticación | Contraseñas, PINs |
| Tokens | Tokens JWT de acceso, refresh tokens, API keys |
| PII más allá de identificadores de auditoría | Nombres completos combinados con DNI/CI, datos financieros |

```csharp
// INCORRECTO — loguea el valor del token
_logger.LogInformation("JWT issued: {Token}", jwtToken);

// CORRECTO — loguear solo metadata
_logger.LogInformation("Sign-in succeeded for UserId={UserId}", user.Id);
```

### 7. Patrón de inyección de ILogger

Usar `ILogger<T>` mediante inyección en primary constructor. El parámetro genérico delimita automáticamente la categoría del log al nombre de la clase, visible en Loki.

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

## Consecuencias

**Positivo:**
- Las entradas de log son consultables en Loki/Grafana por campo (e.g., `{UserId="42"}`) sin parsing con regex
- `CorrelationId` vincula todas las entradas de log de una sola solicitud, habilitando trazabilidad completa
- Sobreescribir los namespaces del framework a `Warning` en producción reduce drásticamente el volumen de escritura en Loki
- La convención de message templates hace que el escaneo de logs sea predecible entre módulos
- La protección de datos sensibles es explícita y se aplica por convención

**Negativo:**
- Los desarrolladores deben aprender la sintaxis de message templates (placeholders nombrados, no interpolación)
- `ILogger<T>` debe agregarse a cada handler/servicio que necesite logging — incrementa marginalmente el tamaño del constructor

**Mitigación:**
- La convención está documentada aquí y se aplica mediante code review
- La sintaxis de primary constructor mantiene la inyección concisa

## Referencias
- [Serilog Best Practices — Ben Foster](https://benfoster.io/blog/serilog-best-practices/)
- [5 Serilog Best Practices — Milan Jovanovic](https://www.milanjovanovic.tech/blog/5-serilog-best-practices-for-better-structured-logging)
- [serilog-sinks-grafana-loki — GitHub](https://github.com/serilog-contrib/serilog-sinks-grafana-loki)
- [Logging Best Practices in ASP.NET Core — Anton Dev Tips](https://antondevtips.com/blog/logging-best-practices-in-asp-net-core)
- [Sensitive Data in Logs — Better Stack](https://betterstack.com/community/guides/logging/sensitive-data/)
