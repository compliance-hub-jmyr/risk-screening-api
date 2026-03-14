# ADR-0009: Estrategia de Paginación — Offset-Based con PageResponse&lt;T&gt;

## Estado
`Accepted`

## Fecha
2026-03-13

## Contexto

Múltiples endpoints en los módulos Suppliers y Scraping devuelven colecciones de tamaño potencialmente grande (lista de proveedores, historial de screening, resultados de listas de riesgo). Se requiere un contrato de paginación consistente que:

- Prevenga consultas sin límite que agoten la base de datos o la memoria.
- Proporcione al cliente suficientes metadatos para renderizar una UI paginada (números de página, botones siguiente/anterior) sin cálculos adicionales.
- Sea reutilizable en todos los módulos sin duplicar la forma de la respuesta.
- Sea suficientemente simple para la Fase 1 y no imponga infraestructura innecesaria (ej. cursores con estado).

Se evaluaron dos enfoques fundamentales: **paginación offset-based** y **paginación cursor-based**.

---

## Decisión

**Usar paginación offset-based con un contrato compartido `PageResponse<T>` / `PageMetadata` / `PageRequest` ubicado en `Shared/Domain`.**

### Entrada: `PageRequest`

Todos los endpoints paginados aceptan un objeto `PageRequest` (parámetros de query string):

| Parámetro | Tipo | Default | Máximo | Descripción |
|-----------|------|---------|--------|-------------|
| `page` | `int` | `0` | — | Índice de página base-cero |
| `size` | `int` | `20` | `100` | Número de ítems por página |
| `sortBy` | `string?` | `null` | — | Nombre del campo por el que ordenar |
| `sortDirection` | `string?` | `asc` | — | `asc` o `desc` |

Un tamaño máximo de página de `100` es aplicado por FluentValidation para prevenir solicitudes sin límite.

### Salida: `PageResponse<T>`

Todos los endpoints paginados devuelven un envelope `PageResponse<T>`:

```json
{
  "content": [...],
  "meta": {
    "number": 0,
    "size": 20,
    "totalElements": 143,
    "totalPages": 8,
    "first": true,
    "last": false,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

Los campos de `PageMetadata` son pre-calculados en el servidor para que el cliente nunca necesite derivar `hasNext`, `hasPrevious` ni `totalPages` por su cuenta.

### Ubicación en Shared

`PageResponse<T>`, `PageMetadata` y `PageRequest` viven en `Shared/Domain` y son usados por:
- **Módulo Suppliers** — `GET /api/suppliers`
- **Módulo Scraping** — los tres endpoints de listas (`/lists/ofac`, `/lists/worldbank`, `/lists/icij`)

---

## Opciones Evaluadas

### Opción A — Paginación offset-based ✅ Seleccionada

| Ventajas | Desventajas |
|----------|-------------|
| Simple de implementar con EF Core (`.Skip().Take()`) | El rendimiento degrada en offsets muy altos (no es una preocupación en este dominio) |
| Soporta navegación de acceso aleatorio (ir a la página N) | La consulta de conteo agrega un round trip extra a la DB |
| El cliente puede renderizar UI con números de página completos | Los resultados pueden desplazarse entre páginas si los datos se mutan durante la navegación |
| Sin estado — no requiere cursor en el servidor | |

### Opción B — Paginación cursor-based

| Ventajas | Desventajas |
|----------|-------------|
| Estable ante inserciones/eliminaciones concurrentes | No permite saltar a una página arbitraria — solo siguiente/anterior |
| Escala a offsets arbitrariamente grandes | Más compleja de implementar y testear |
| | Requiere un token de cursor opaco en la respuesta |
| | Incompatible con UI de números de página (PrimeNG Paginator) |

La paginación cursor-based es más adecuada para feeds de alto volumen en tiempo real (ej. timelines de redes sociales). Los datos de proveedores y listas de riesgo en esta plataforma son de bajo volumen y se actualizan con poca frecuencia, lo que hace que la paginación offset sea la elección correcta.

### Opción C — Sin paginación (devolver todos los resultados)

| Ventajas | Desventajas |
|----------|-------------|
| Implementación más simple | Tamaño de respuesta ilimitado |
| | Rompe el renderizado de la UI con datasets grandes |
| | No aceptable para producción |

---

## Consecuencias

### Positivas
- Forma de respuesta uniforme en todos los endpoints de colección — los clientes solo necesitan aprender un patrón.
- Los booleanos de `PageMetadata` (`hasNext`, `hasPrevious`, `first`, `last`) simplifican la lógica de paginación en el frontend.
- La validación de `PageRequest` (tamaño máximo 100) se aplica via FluentValidation — respuesta de error consistente ante violaciones.
- Cambiar la implementación subyacente (ej. Dapper en lugar de EF Core) no modifica el contrato de la API.

### Negativas / Mitigaciones

| Riesgo | Mitigación |
|--------|-----------|
| Overhead de la consulta de conteo en cada solicitud paginada | Aceptable en Fase 1. La Fase 2 puede agregar caché del conteo o usar split queries de EF Core si el profiling lo justifica |
| Desplazamiento de páginas ante escrituras concurrentes | Aceptable para flujos de trabajo de compliance — los proveedores no se insertan con alta frecuencia |
| El cliente solicita `size=10000` | Máximo de `100` aplicado via FluentValidation — retorna HTTP 400 ante violaciones |

---

## Dependencias

No se requieren paquetes adicionales. La paginación offset se implementa mediante los métodos nativos de EF Core: `.Skip()` / `.Take()` / `.CountAsync()`.

---

## Trabajo Futuro

- **Fase 2:** Exponer el header `X-Total-Count` para clientes que prefieren metadatos en headers en lugar de envelope.
- **Fase 2:** Agregar tamaños de página por defecto y máximo configurables via `appsettings.json` bajo `Pagination:DefaultSize` y `Pagination:MaxSize`.
- **Fase 2:** Evaluar paginación cursor-based para endpoints de audit log si el volumen supera las 100k filas.
