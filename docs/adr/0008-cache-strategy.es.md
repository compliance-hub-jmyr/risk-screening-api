# ADR-0008: Estrategia de Cache — IMemoryCache (Fase 1)

## Estado
`Aceptado`

## Fecha
2026-03-13

## Contexto

El módulo Scraping obtiene datos de tres fuentes externas (OFAC SDN, World Bank Debarred Firms, ICIJ Offshore Leaks). Estas fuentes se actualizan con poca frecuencia (diariamente a semanalmente), pero las consultas individuales pueden repetirse con frecuencia. Obtenerlas en vivo en cada solicitud añadiría 1–3 s de latencia y riesgo de alcanzar límites de tasa o activar detección de scraping.

Se requiere una capa de cache que:

- Reduzca la latencia para búsquedas repetidas de datos de listas.
- Evite fetches en vivo repetidos de fuentes externas en cada solicitud.
- Sea simple de implementar y operar en la Fase 1 (despliegue de instancia única).
- Pueda evolucionar a un cache distribuido (Redis) en un hito futuro sin refactorización significativa.

> Cómo y cuándo se puebla el cache (bajo demanda vs worker en background) está documentado en **ADR-0006**.

---

## Decisión

**Usar `IMemoryCache` (cache en memoria en proceso) como capa de cache en la Fase 1.**

Todas las interacciones con el cache están encapsuladas en un `ScrapingCacheService`, de modo que la implementación subyacente puede intercambiarse sin modificar la lógica de aplicación.

### Convención de claves de cache

```
{módulo}:{recurso}:{calificador}
```

Ejemplos:

| Clave | TTL | Descripción |
|-------|-----|-------------|
| `scraping:ofac:{consulta}` | 60 min | Resultado OFAC SDN para un término de búsqueda dado |
| `scraping:worldbank:{consulta}` | 120 min | Resultado World Bank para un término de búsqueda dado |
| `scraping:icij:{consulta}` | 15 min | Resultado ICIJ Offshore Leaks para un término de búsqueda dado |
| `rate_limit:{apiKey}` | 60 s (deslizante) | Ventana de rate limiting por API key |

### Estrategia de TTL

- Las entradas de cache se escriben en el primer cache miss (población bajo demanda según ADR-0006 Fase 1).
- La expiración del TTL asegura que los datos obsoletos se eliminen automáticamente; la próxima solicitud tras la expiración desencadena un nuevo fetch en vivo.
- Las claves de rate limiting usan un TTL de ventana deslizante gestionado directamente vía `IMemoryCache` con seguimiento explícito de timestamp.

---

## Opciones Evaluadas

### Opción A — Sin cache (siempre fetch bajo demanda)

| Ventajas | Desventajas |
|----------|-------------|
| Sin complejidad | Alta latencia (1–3 s por llamada externa) |
| Datos siempre frescos | Riesgo de bloqueo por IP / detección de scraping |
| | No viable a > 1 req/s |

### Opción B — `IMemoryCache` Seleccionada

| Ventajas | Desventajas |
|----------|-------------|
| Sin overhead de infraestructura | En proceso: se pierde al reiniciar |
| Nativo .NET 10 — sin paquetes adicionales | No compartido entre múltiples instancias |
| Thread-safe, baja latencia | Memoria acotada por proceso |
| Simple de testear (inyectable) | Requiere diseño cuidadoso de TTL |

### Opción C — `IDistributedCache` + Redis

| Ventajas | Desventajas |
|----------|-------------|
| Sobrevive reinicios de proceso | Requiere infraestructura Redis |
| Compartido entre instancias | Complejidad operacional adicional |
| Escala horizontalmente | Sobre-ingeniería para Fase 1 (instancia única) |

---

## Consecuencias

### Positivas
- Búsquedas repetidas en sub-milisegundo para todas las consultas de scraping.
- Sin dependencias de infraestructura en la Fase 1.
- `IMemoryCache` es completamente inyectable — la capa de cache es testeable con mocks.
- La interfaz `ScrapingCacheService` aísla la implementación — migrar a Redis no requiere cambios en la lógica de aplicación.

### Negativas / Mitigaciones

| Riesgo | Mitigación |
|--------|-----------|
| Cache perdido al reiniciar el proceso | Todas las entradas se repueblan en la siguiente solicitud (estrategia bajo demanda según ADR-0006) |
| Despliegue multi-instancia (futuro) | Reemplazar `IMemoryCache` con `IDistributedCache` respaldado por Redis — la interfaz `ScrapingCacheService` aísla el intercambio |
| Crecimiento de memoria | Configurar `SizeLimit` en `MemoryCacheOptions` y usar `SetSize(1)` por entrada para limitar la memoria total |

---

## Dependencias

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `Microsoft.Extensions.Caching.Memory` | Incluido en .NET 10 SDK | Implementación de `IMemoryCache` |

---

## Trabajo Futuro

- **Fase 2:** Reemplazar `IMemoryCache` con Redis (`StackExchange.Redis` + `IDistributedCache`) para soporte multi-instancia.
- **Fase 2:** Agregar métricas de cache hit/miss vía logging estructurado con `ILogger` o contadores OpenTelemetry.
- **Fase 2:** Externalizar valores de TTL a `appsettings.json` bajo `Cache:Scraping:OfacTtlMinutes`, etc.
