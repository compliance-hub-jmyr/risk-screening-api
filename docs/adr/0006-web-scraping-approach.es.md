# ADR-0006: Estrategia de Web Scraping — Bajo Demanda con Cache (Fase 1)

## Estado
`Aceptado`

## Fecha
2026-03-13

## Contexto

El módulo Scraping requiere obtener datos de tres fuentes externas:
1. **OFAC SDN** — US Treasury, disponible como feed XML público
2. **World Bank Debarred Firms** — Página web con tabla HTML paginada
3. **ICIJ Offshore Leaks** — API REST pública (búsqueda por consulta; sin descarga completa del dataset)

Se evaluaron dos estrategias de obtención de datos:

| Estrategia | Descripción | Latencia | Riesgo |
|------------|-------------|----------|--------|
| **Scraping bajo demanda** | Scraping en vivo cuando llega la solicitud; resultado cacheado con TTL | Moderada en primera llamada; casi nula en cache hit | Pequeña demora en cache miss; la fuente externa debe estar disponible |
| **Refresh en background + cache** | `IHostedService` periódico pre-puebla el cache; todas las solicitudes se sirven desde cache | Casi nula (< 10 ms siempre) | Datos pueden tener hasta N minutos de antigüedad; requiere calentamiento al inicio |

## Decisión

**Fase 1 (actual):** Scraping bajo demanda con cache de resultados en `IMemoryCache`.

Cuando llega una solicitud de búsqueda:
1. Consultar `IMemoryCache` por un resultado cacheado (clave = fuente + término de búsqueda).
2. **Cache hit** → retornar inmediatamente (submilisegundo).
3. **Cache miss** → obtener en vivo desde la fuente externa, guardar resultado en cache con TTL y retornar.

Este enfoque es más simple de implementar y operar en la Fase 1, evita la complejidad de un worker en background, y aún así entrega respuestas rápidas para consultas repetidas.

> La elección de tecnología de cache (IMemoryCache vs Redis) está documentada en **ADR-0008**.

### Estrategia por fuente

#### OFAC SDN
- Referencia del assessment: `https://sanctionssearch.ofac.treas.gov/` (formulario web, sin API REST)
- Fuente programática: `https://sdn.ofac.treas.gov/SDN_XML.zip` (ZIP público con XML SDN)
- Método: Descarga ZIP, descompresión en memoria, parsing XML con `System.Xml.Linq`
- Clave de cache: `scraping:ofac:{consultaNormalizada}`
- TTL: **10 minutos**

#### World Bank Debarred Firms
- Fuente: `https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms?srchTerm={consulta}`
- Método: HTTP GET + parsing de tabla HTML con `HtmlAgilityPack`
- Clave de cache: `scraping:worldbank:{consultaNormalizada}`
- TTL: **10 minutos**

#### ICIJ Offshore Leaks
- Fuente: `https://offshoreleaks.icij.org/api/nodes?q={consulta}` (API REST pública)
- Método: HTTP GET — búsqueda en tiempo real por consulta; la API pública soporta búsqueda por consulta de forma nativa
- Clave de cache: `scraping:icij:{consultaNormalizada}`
- TTL: **10 minutos**

### Manejo de errores (tolerante a fallos)

```
Si un fetch en vivo falla (timeout, error HTTP, error de parsing):
  - Registrar error en nivel WARNING
  - Retornar SearchResult.Empty (hits: 0, entries: []) — NO un error HTTP
  - NO escribir un resultado fallido en cache
  - La próxima solicitud reintentará el fetch en vivo
  - Al buscar en todas las fuentes (GET /api/lists/search?q=term), el fallo de una fuente
    no impide que las otras retornen resultados
```

## Consecuencias

**Positivas:**
- Simple de implementar — sin worker en background, sin complejidad de calentamiento al inicio
- Las consultas repetidas para el mismo término responden en tiempo submilisegundo (cache hit)
- Menor carga sobre las fuentes externas comparado con hacer scraping en cada solicitud individual
- Sin riesgo de datos obsoletos por un worker que falló al refrescar

**Negativas:**
- La primera solicitud para un término nuevo incurre en latencia de fetch en vivo (la descarga XML de OFAC puede tardar 1–3 s)
- Si la fuente externa no está disponible al momento de la consulta, la solicitud falla (sin fallback pre-cacheado)

**Mitigación:**
- Usar políticas de retry y timeout de `Polly` en todos los clientes HTTP para reducir el impacto de fallos transitorios
- **Fase 2 (futuro):** Reemplazar el fetch bajo demanda para OFAC y World Bank con un `BackgroundService` que pre-pueble el cache al inicio y se refresque periódicamente. ICIJ permanece siempre bajo demanda (tamaño del dataset, diseño de la API pública). Esta mejora se difiere intencionalmente para evitar sobre-ingeniería en la Fase 1.

## Dependencias

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `HtmlAgilityPack` | 1.11.x | Parsing HTML para World Bank |
| `System.Xml.Linq` | .NET 10 | Parsing XML para OFAC SDN |
| `Microsoft.Extensions.Http` | .NET 10 | `HttpClientFactory` para clientes tipados |
| `Polly` | 8.x | Políticas de retry y timeout para solicitudes HTTP |

## Referencias
- [Descargas de la Lista SDN de OFAC](https://www.treasury.gov/resource-center/sanctions/SDN-List/Pages/default.aspx)
- [API de ICIJ Offshore Leaks](https://offshoreleaks.icij.org/api)
- [HtmlAgilityPack](https://html-agility-pack.net/)
- [Polly — Librería de Resiliencia .NET](https://github.com/App-vNext/Polly)
- ADR-0008 — Tecnología de cache: IMemoryCache (Fase 1)
