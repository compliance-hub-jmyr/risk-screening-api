# ADR-0010: Estrategia de Manejo de Errores — GlobalExceptionHandler + ProblemDetails

## Estado
`Accepted`

## Fecha
2026-03-13

## Contexto

La API debe traducir las excepciones lanzadas en cualquier punto del pipeline de solicitud — lógica de dominio, validación, infraestructura — en respuestas HTTP estructuradas y consistentes. Sin una estrategia centralizada, cada controlador o servicio manejaría los errores de forma independiente, resultando en:

- Cuerpos de respuesta inconsistentes (algunos devuelven strings planos, otros objetos JSON).
- Bloques try/catch duplicados en todos los controladores.
- Clientes frontend que no pueden depender de un contrato de error estable.

Tres tipos de errores transversales deben ser manejados:

| Origen | Ejemplo | HTTP esperado |
|--------|---------|---------------|
| Behavior de validación FluentValidation | Campo requerido faltante, formato inválido | `400 Bad Request` |
| Excepciones de dominio | `TaxId` duplicado, usuario no encontrado, proveedor ya eliminado | `409 Conflict`, `404 Not Found`, `401 Unauthorized` |
| Excepciones no manejadas / infraestructura | Fallo de conexión DB, referencia nula | `500 Internal Server Error` |

Además, la forma del cuerpo de respuesta de error debe:
- Ser **legible por máquina** — la SPA Angular debe poder extraer errores de validación a nivel de campo para mostrar en formularios.
- Ser **estándar** — evitar inventar un envelope de error propio.
- No filtrar stack traces ni detalles de implementación a los clientes.

---

## Decisión

**Usar una cadena de implementaciones de `IExceptionHandler` (ASP.NET Core .NET 10) con un record personalizado `ErrorResponse` que extiende RFC 7807 Problem Details.**

### Mecanismo: cadena de `IExceptionHandler`

`IExceptionHandler` (introducido en .NET 8, disponible en .NET 10) es el punto de extensión preferido de ASP.NET Core para el manejo centralizado de excepciones. Se registran tres handlers en orden de prioridad:

```
ValidationExceptionHandler → DomainExceptionHandler → GlobalExceptionHandler
```

Cada handler:
- Se registra en `Program.cs` via `builder.Services.AddExceptionHandler<T>()`.
- Es invocado por el middleware `UseExceptionHandler()` en orden de registro.
- Retorna `true` para cortocircuitar la cadena, o `false` para pasar al siguiente handler.
- Es completamente inyectable via constructor DI y testeable en aislamiento.

### Forma de respuesta: RFC 7807 extendido con códigos legibles por máquina

Todas las respuestas de error siguen el estándar [RFC 7807 Problem Details for HTTP APIs](https://datatracker.ietf.org/doc/html/rfc7807) usando los campos estándar (`type`, `title`, `status`, `instance`) extendidos con miembros adicionales según lo permitido por RFC 7807 3.2.

El tipo concreto es `ErrorResponse` — un record personalizado en `Shared/Interfaces/REST/Resources/`.

**Error de validación 400:**
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

La propiedad de extensión `fieldErrors` se popula únicamente en respuestas `400 Bad Request` provenientes de `ValidationExceptionHandler`. Es un **array** de objetos `{ field, message, rejectedValue }`, no un diccionario.

**¿Por qué extender RFC 7807 en lugar de usarlo tal cual?**

RFC 7807 por sí solo provee `type`, `title`, `status` e `instance` — suficiente para humanos, pero no para manejo programático del cliente. La SPA Angular necesita identificar el tipo de error específico para mostrar el mensaje correcto o resaltar el campo del formulario adecuado. `errorCode` (una constante string como `"ENTITY_NOT_FOUND"`) permite manejo confiable con `switch`/`case` en interceptores Angular sin parseo frágil de `title` o `detail`. `errorNumber` provee un entero estable para correlación en logs de soporte.

### Mapeo excepción → HTTP

| Tipo de excepción | HTTP status | `title` | `errorCode` |
|-------------------|-------------|---------|-------------|
| `ValidationException` (FluentValidation) | `400 Bad Request` | `Validation failed` | `VALIDATION_FAILED` |
| `EntityNotFoundException` (DomainException) | `404 Not Found` | `Resource not found` | *(definido por la excepción)* |
| `BusinessRuleViolationException` (DomainException) | `409 Conflict` | `Business rule violation` | *(definido por la excepción)* |
| `AuthenticationException` (DomainException) | `401 Unauthorized` | `Unauthorized` | *(definido por la excepción)* |
| `AuthorizationException` (DomainException) | `403 Forbidden` | `Forbidden` | *(definido por la excepción)* |
| `Exception` (catch-all) | `500 Internal Server Error` | `An unexpected error occurred` | `INTERNAL_SERVER_ERROR` |

### Jerarquía de excepciones de dominio

Todas las excepciones de dominio extienden `DomainException` en `Shared/Domain/Exceptions/` y llevan su propio `ErrorNumber` y `ErrorCode`:

```
DomainException (abstract)
├── EntityNotFoundException
├── BusinessRuleViolationException
├── AuthenticationException
├── AuthorizationException
├── DomainValidationException
└── InvalidValueException
```

`DomainExceptionHandler` hace matching por tipo y asigna `title` y HTTP status correspondientes.

### Seguridad: sin filtración de stack traces

- Las respuestas `500` nunca incluyen `exception.Message` ni stack traces en el cuerpo de la respuesta.
- En desarrollo (`IWebHostEnvironment.IsDevelopment()`), `message` contiene el mensaje de la excepción para facilitar el debugging.
- En producción, `message` es siempre el string genérico `"An unexpected error occurred. Please try again later."`.
- La excepción completa se loguea via `ILogger<GlobalExceptionHandler>` al nivel `Error`.

---

## Opciones Evaluadas

### Opción A — `IExceptionHandler` (ASP.NET Core) Seleccionada

| Ventajas | Desventajas |
|----------|-------------|
| Nativo .NET 10 — sin paquetes adicionales | Requiere que el middleware `UseExceptionHandler()` esté registrado |
| Inyectable via DI | |
| Puede cortocircuitar (retornar `true`) o delegar al siguiente handler | |
| Funciona junto con `UseStatusCodePages` | |

### Opción B — Middleware de manejo de excepciones personalizado

| Ventajas | Desventajas |
|----------|-------------|
| Control total sobre el pipeline | Boilerplate: debe leer manualmente `context.Response`, configurar el content type, serializar JSON |
| | No es el enfoque idiomático en .NET 10 |
| | Más difícil de testear sin `WebApplicationFactory` |

### Opción C — Filtros de excepción (`IExceptionFilter`)

| Ventajas | Desventajas |
|----------|-------------|
| Funciona a nivel de acción MVC | No captura excepciones fuera de MVC (middleware, model binding) |
| Fácil de delimitar por controlador | |
| | Cobertura incompleta — no es adecuado como única estrategia |

### Opción D — Try/catch por controlador

| Ventajas | Desventajas |
|----------|-------------|
| Explícito | Duplicación masiva de código |
| | Formas de error inconsistentes por desarrollador |
| | No testeable a escala |

---

## Consecuencias

### Positivas
- Un único lugar para modificar el comportamiento del manejo de errores — sin try/catch dispersos en los controladores.
- RFC 7807 `ProblemDetails` es un estándar conocido — los interceptores HTTP de Angular pueden parsearlo de forma confiable.
- Los errores de validación a nivel de campo en la propiedad de extensión `fieldErrors` (array) habilitan el binding directo en formularios reactivos de PrimeNG.
- La jerarquía `DomainException` desacopla los errores específicos de módulo de la semántica HTTP.
- `errorCode` en cada respuesta de error permite manejo programático en Angular sin parsear strings.

### Negativas / Mitigaciones

| Riesgo | Mitigación |
|--------|-----------|
| Nuevo tipo de excepción no mapeado — cae en el 500 | La clase base `DomainException` es capturada antes del catch-all; agregar una nueva subclase solo requiere actualizar la tabla de mapeo en `DomainExceptionHandler` |
| La forma verbosa puede parecer sobredimensionada para errores simples | RFC 7807 es el estándar de la industria; los clientes Angular se benefician de la predictibilidad |

---

## Dependencias

No se requieren paquetes adicionales. `IExceptionHandler` forma parte del SDK de ASP.NET Core .NET 10.

---

## Decisiones Relacionadas

- **ADR-0002** — CQRS + MediatR: `ValidationBehavior` en el pipeline de MediatR lanza `ValidationException`, que es capturada aquí y mapeada a `400`.
- **ADR-0009** — Paginación: las violaciones del tamaño de `PageRequest` se lanzan como `ValidationException` y son manejadas por esta ADR.
