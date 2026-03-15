# ADR-0006: Estrategia de Web Scraping — Bajo Demanda con Cache (Fase 1)

## Estado
`Aceptado`

## Fecha
2026-03-15 (Actualizado)

## Contexto

El módulo Scraping requiere obtener datos de tres fuentes externas:
1. **OFAC SDN** — US Treasury Sanctions List Search (formulario web en `https://sanctionssearch.ofac.treas.gov/`)
2. **World Bank Debarred Firms** — Grid Kendo UI en `https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms` (carga datos dinámicamente via AJAX; filtrado client-side)
3. **ICIJ Offshore Leaks** — SPA JavaScript en `https://offshoreleaks.icij.org/` protegida por AWS CloudFront WAF; requiere renderizado con headless browser

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
- **Referencia del assessment**: `https://sanctionssearch.ofac.treas.gov/` (formulario web ASP.NET con funcionalidad de búsqueda)
- **Método**: Web scraping real mediante `HtmlAgilityPack`
  1. `OfacScrapingSource` (adaptador) orquesta el flujo HTTP: GET página inicial → POST formulario de búsqueda
  2. `OfacHtmlParser` (helper estático) maneja la extracción HTML:
     - `ExtractFormData()` — parsea ViewState de ASP.NET y campos ocultos de la página inicial
     - `ParseResults()` — localiza la tabla `#scrollResults` y convierte filas en records `RiskEntry`
  3. Columnas extraídas: Name, Address, Type, Program(s), List, **Score** (porcentaje de confianza de coincidencia)
- **Arquitectura**: Ports & Adapters — puerto `IScrapingSource` en `Application/Ports/`, adaptador `OfacScrapingSource` en `Infrastructure/Sources/`, orquestación via `SearchRiskListsQueryHandler` (handler CQRS de MediatR en `Application/Search/`)
- **¿Por qué no XML?** El feed XML (`https://www.treasury.gov/ofac/downloads/sdn.xml`) no incluye el campo **Score**, que es un requerimiento crítico del assessment técnico. El Score representa el porcentaje de confianza de coincidencia calculado por el algoritmo de búsqueda de OFAC.
- **Clave de cache**: `scraping:ofac:{consultaNormalizada}`
- **TTL**: **10 minutos**
- **Timeout del cliente HTTP**: 45 segundos (aumentado para manejar envío de formulario + parsing)

#### World Bank Debarred Firms
- **Referencia del assessment**: `https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms` (grid Kendo UI con filtrado client-side)
- **API real**: `https://apigwext.worldbank.org/dvsvc/v1.0/json/APPLICATION/ADOBE_EXPRNCE_MGR/FIRM/SANCTIONED_FIRM` (API JSON usada por la página web; requiere API key público en header `apikey`)
- **Método**: Web scraping en dos pasos via `HtmlAgilityPack` + `System.Text.Json` (mismo patrón GET → GET que el GET → POST de OFAC)
  1. `WorldBankScrapingSource` (adaptador) orquesta el flujo HTTP en dos pasos:
     - **Paso 1 (scrape):** GET la página HTML → `WorldBankHtmlParser.ExtractApiConfig()` parsea tags `<script>` con `HtmlAgilityPack` para extraer las variables JavaScript `prodtabApi` (URL del API) y `propApiKey` (API key)
     - **Paso 2 (fetch):** GET la API JSON usando URL y key extraídos (mismo request que el browser hace via AJAX) → `WorldBankHtmlParser.ParseResults()` deserializa y filtra
  2. `WorldBankHtmlParser` (helper estático unificado — mismo patrón que `OfacHtmlParser`) maneja ambos pasos:
     - `ExtractApiConfig()`: Parsea tags `<script>` con `HtmlAgilityPack`, extrae `var prodtabApi = "..."` y `var propApiKey = "..."` usando `GeneratedRegex`
     - `ParseResults()`: Procesamiento JSON:
     - Deserializa array `response.ZPROCSUPP` de DTOs de firmas
     - Filtra firmas donde el término de búsqueda coincide con cualquier campo (contains case-insensitive, lógica OR en nombre, dirección, ciudad, estado, país, motivos)
     - Mapea `SUPP_NAME`, `SUPP_ADDR`/`SUPP_CITY`/`SUPP_STATE_CODE`/`SUPP_ZIP_CODE` (combinados), `COUNTRY_NAME`, `DEBAR_FROM_DATE`, `DEBAR_TO_DATE`, `DEBAR_REASON` a `RiskEntry`
     - Cuando `INELIGIBLY_STATUS` es "Permanent" u "Ongoing", usa esa etiqueta para `ToDate` en lugar de la fecha centinela (`2999-12-31`)
  4. Campos extraídos: Firm Name (→ `Name`), Address (componentes combinados), Country, FromDate, ToDate (o Ineligibility Status), Grounds
- **¿Por qué scraping en dos pasos?** La página del Banco Mundial es un grid Kendo UI que carga datos dinámicamente via AJAX — el HTML inicial no contiene datos de firmas. El scraper extrae el endpoint de la API y la key del JavaScript de la página, luego replica el mismo request AJAX que hace el browser. Esto asegura que el adaptador se adapta automáticamente si la API key se rota.
- **Arquitectura**: Mismo patrón Ports & Adapters que OFAC — puerto `IScrapingSource`, adaptador `WorldBankScrapingSource` en `Infrastructure/Sources/`
- **Clave de cache**: `scraping:worldbank:{consultaNormalizada}`
- **TTL**: **10 minutos**
- **Timeout del cliente HTTP**: 45 segundos (aumentado — dos requests HTTP)

#### ICIJ Offshore Leaks
- **Referencia del assessment**: `https://offshoreleaks.icij.org/` (SPA JavaScript — resultados renderizados client-side, protegida por AWS CloudFront WAF)
- **Método**: Scraping con headless browser via `Microsoft.Playwright` (Chromium) + `HtmlAgilityPack` para parsing HTML
  1. `IcijScrapingSource` (adaptador) lanza una instancia headless de Chromium con flags anti-detección (`--disable-blink-features=AutomationControlled`, User-Agent personalizado, `navigator.webdriver = false`) para bypasear la protección de bots de CloudFront
  2. Navega a `/search?q={term}&c=&j=&d=` y espera `NetworkIdle` + el selector de la tabla de resultados (`table.table tbody tr`)
  3. Extrae el HTML completamente renderizado y lo pasa a `IcijHtmlParser`
  4. `IcijHtmlParser` (helper estático) parsea la tabla HTML de resultados con `HtmlAgilityPack`:
     - Localiza `<table class="table">` → `<tbody>` → `<tr>` filas
     - Extrae 4 columnas: Entity (→ `Name`), Jurisdiction, Linked To (→ `LinkedTo`), Data From (→ `DataFrom`)
     - Decodifica entidades HTML y normaliza espacios en blanco
  5. Campos extraídos: Name, Jurisdiction, LinkedTo, DataFrom
- **¿Por qué Playwright?** El sitio web de ICIJ migró a una SPA JavaScript que retorna HTTP 202 con body vacío para requests del lado del servidor. `HtmlAgilityPack` solo no puede ejecutar JavaScript. Además, CloudFront WAF bloquea firmas estándar de headless browsers — Playwright con configuración stealth bypasea esta protección.
- **Arquitectura**: Mismo patrón Ports & Adapters que OFAC — puerto `IScrapingSource`, adaptador `IcijScrapingSource` en `Infrastructure/Sources/`
- **Nota**: A diferencia de OFAC y World Bank, ICIJ no usa `IHttpClientFactory` — Playwright gestiona su propio ciclo de vida del browser dentro de cada llamada a `SearchAsync`.
- **Clave de cache**: `scraping:icij:{consultaNormalizada}`
- **TTL**: **10 minutos**
- **Timeout del browser**: 30 segundos (navegación de página) + 10 segundos (espera del selector de tabla)

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
- La primera solicitud para un término nuevo incurre en latencia de fetch en vivo (envío de formulario OFAC + parsing HTML puede tardar 2–5 s)
- Si la fuente externa no está disponible al momento de la consulta, la solicitud falla (sin fallback pre-cacheado)
- El scraping de OFAC es más frágil que el parsing XML — cambios en la estructura del formulario ASP.NET o formato de la tabla HTML romperán el scraper
- La API JSON de World Bank requiere un API key público embebido en el JavaScript de la página — si la clave se rota, el adaptador debe actualizarse

**Mitigación:**
- Usar políticas de retry y timeout de `Polly` en todos los clientes HTTP para reducir el impacto de fallos transitorios
- **Fase 2 (futuro):** Reemplazar el fetch bajo demanda para OFAC y World Bank con un `BackgroundService` que pre-pueble el cache al inicio y se refresque periódicamente. ICIJ permanece siempre bajo demanda (tamaño del dataset, diseño de la API pública). Esta mejora se difiere intencionalmente para evitar sobre-ingeniería en la Fase 1.

## Dependencias

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `HtmlAgilityPack` | 1.11.71 | Parsing HTML para OFAC (formulario + tabla de resultados), World Bank (extracción de JavaScript) e ICIJ (parsing del DOM renderizado) |
| `Microsoft.Playwright` | 1.58.0 | Chromium headless para renderizado de SPA ICIJ (bypasea detección de bots de CloudFront WAF) |
| `System.Text.Json` | .NET 10 | Deserialización JSON para respuestas de la API de World Bank |
| `Microsoft.Extensions.Http` | .NET 10 | `HttpClientFactory` para clientes tipados |
| `Polly` | 8.x (futuro) | Políticas de retry y timeout para solicitudes HTTP |

## Referencias
- [Descargas de la Lista SDN de OFAC](https://www.treasury.gov/resource-center/sanctions/SDN-List/Pages/default.aspx)
- [API de ICIJ Offshore Leaks](https://offshoreleaks.icij.org/api)
- [HtmlAgilityPack](https://html-agility-pack.net/)
- [Microsoft.Playwright para .NET](https://playwright.dev/dotnet/)
- [Polly — Librería de Resiliencia .NET](https://github.com/App-vNext/Polly)
- ADR-0008 — Tecnología de cache: IMemoryCache (Fase 1)
