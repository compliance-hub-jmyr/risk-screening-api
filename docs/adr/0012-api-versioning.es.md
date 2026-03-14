# ADR-0012: Estrategia de Versionado de API — Header-Based con `Api-Version`

## Estado
`Accepted`

## Fecha
2026-03-14

## Contexto

La API requiere una estrategia de versionado para permitir la evolución futura de contratos sin romper los clientes existentes (la SPA Angular y las colecciones de Postman). Se evaluaron tres estrategias de ubicación de la versión:

| Estrategia | Ejemplo | Ventajas | Desventajas |
|------------|---------|----------|-------------|
| **Segmento en URL** | `GET /api/v1/suppliers` | Visible, bookmarkable, fácil de probar en el browser | La versión pasa a ser parte del identificador del recurso — viola la semántica REST; las URLs deben cambiar en cada versión mayor |
| **Header** | `GET /api/suppliers` + `Api-Version: 1` | URLs limpias; la versión es metadata de la solicitud, no identidad del recurso | No se puede probar directamente pegando la URL en el browser |
| **Query string** | `GET /api/suppliers?api-version=1` | Sin cambio de URL | Contamina el query string; puede afectar el caché |

## Decisión

Usar **versionado basado en header** mediante un header de solicitud personalizado `Api-Version`, implementado con el paquete NuGet `Asp.Versioning.Http`.

### Configuración

```csharp
// WebApplicationBuilderExtensions.cs
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion                   = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions                   = true;
    options.ApiVersionReader                    = new HeaderApiVersionReader("Api-Version");
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat           = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

### Constantes

```csharp
// Shared/Infrastructure/Configuration/ApiVersioning.cs
public static class ApiVersioning
{
    public const string Base   = "/api";
    public const string V1     = "1.0";
    public const string V2     = "2.0";   // reservado
    public const string Header = "Api-Version";
}
```

### Uso por parte del cliente

```http
GET /api/suppliers
Api-Version: 1
Authorization: Bearer eyJ...
```

### Comportamiento de versión por defecto

`AssumeDefaultVersionWhenUnspecified = true` significa que los clientes que omitan el header reciben la versión `1.0`. Esto garantiza compatibilidad hacia atrás y permite que la colección de Postman y la SPA Angular funcionen sin necesidad de enviar el header explícitamente en la Fase 1.

### Declaración en controllers

Cada controller declara las versiones que soporta mediante `[ApiVersion]`:

```csharp
[ApiController]
[Route("api/[controller]")]
[ApiVersion(ApiVersioning.V1)]
public class SuppliersController : ControllerBase { ... }
```

Cuando un cambio breaking requiera una V2, solo el controller afectado recibe una nueva clase etiquetada `[ApiVersion(ApiVersioning.V2)]`; el resto de los controllers permanece sin cambios.

### Headers de respuesta

Con `ReportApiVersions = true`, cada respuesta incluye:

```http
api-supported-versions: 1.0
```

Esto permite a los clientes descubrir las versiones disponibles de forma programática.

## Consecuencias

**Positivo:**
- Las URLs son limpias y estables entre versiones — `/api/suppliers` nunca cambia
- La versión se trata correctamente como metadata de la solicitud, no como identidad del recurso
- `AssumeDefaultVersionWhenUnspecified = true` garantiza cero fricción para los clientes existentes
- Agregar V2 a un endpoint no afecta ningún otro controller
- Swagger UI agrupa endpoints por versión via `AddApiExplorer`

**Negativo:**
- No se puede probar directamente pegando una URL en el browser — requiere un cliente que pueda enviar headers (Postman, curl, Angular `HttpClient`)
- Menos descubrible que el versionado en URL para desarrolladores no familiarizados con la API

**Mitigación:**
- La colección de Postman documenta el header `Api-Version: 1` en cada solicitud
- Swagger UI expone el header de versión como parámetro en cada operación
- El fallback a la versión por defecto (`AssumeDefaultVersionWhenUnspecified`) permite pruebas ocasionales sin el header

## Dependencias

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `Asp.Versioning.Http` | 8.x | Versionado de API por header para ASP.NET Core |
| `Asp.Versioning.Mvc.ApiExplorer` | 8.x | Integración con Swagger/OpenAPI para endpoints versionados |

## Referencias
- [Asp.Versioning en GitHub](https://github.com/dotnet/aspnet-api-versioning)
- [API Versioning Best Practices — Troy Hunt](https://www.troyhunt.com/your-api-versioning-is-wrong-which-is/)
- [REST API Versioning — Microsoft REST API Guidelines](https://github.com/microsoft/api-guidelines/blob/vNext/azure/Guidelines.md#api-versioning)
