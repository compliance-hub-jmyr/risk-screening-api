# User Stories — Modulo Scraping (Consulta de Listas de Riesgo)

> **Formato:** Titulo / Descripcion / Entregable / Dependencias / Criterios de Aceptacion (BDD Given/When/Then).
> **Tags de tareas:** `[BE-DOMAIN]` `[BE-APP]` `[BE-INFRA]` `[BE-INTERFACES]` `[BE-DB]` `[BE-TEST]` `[DOCS]`
>
> **Naturaleza del modulo:** El modulo Scraping es **sin estado (stateless)** — sin tablas SQL, sin entidades EF Core. Todos los datos se obtienen en tiempo real desde fuentes externas y se cachean opcionalmente en `IMemoryCache`. No existe historia tecnica `TS-SCR-000` de bootstrap de base de datos porque el modulo solo requiere registro de clientes HTTP y cache.

---

## Epica: Busqueda en Listas de Riesgo (API Directa)

---

### US-SCR-001: Buscar en la lista SDN de OFAC

**Titulo:** Consultar la lista de Nacionales Especialmente Designados de OFAC

**Descripcion:**
Como oficial de compliance, quiero buscar en la lista de Nacionales Especialmente Designados (SDN) de OFAC por nombre, para verificar rapidamente si una persona o entidad aparece en las sanciones del Departamento del Tesoro de EE.UU.

**Entregable:**
Endpoint `GET /api/lists/search?q={termino}&sources=ofac` que realiza web scraping real del sitio web OFAC Sanctions List Search mediante:
1. Obtención de la página inicial del formulario ASP.NET para extraer ViewState y campos del formulario
2. Envío de una petición POST con el término de búsqueda
3. Parsing de la tabla HTML de resultados para extraer Name, Address, Type, List, Programs, y **Score** (porcentaje de confianza de coincidencia)
Los resultados se cachean por 10 minutos por término de búsqueda.

**Nota Técnica:**
El feed XML de OFAC SDN (`https://www.treasury.gov/ofac/downloads/sdn.xml`) no incluye el campo **Score** requerido por el assessment técnico. El Score (porcentaje de confianza de coincidencia) solo está disponible a través de la interfaz web de búsqueda en `https://sanctionssearch.ofac.treas.gov/`, la cual requiere envío de formulario y parsing HTML. Esta implementación usa `HtmlAgilityPack` para parsing HTML robusto y maneja la gestión de ViewState de ASP.NET.

**Dependencias:**
- `US-IAM-001`: autenticacion JWT
- `ScrapingModuleExtensions.AddScrapingModule()` registrado — cliente HTTP con timeout de 45s e `IMemoryCache` configurados
- `HtmlAgilityPack` 1.11.71 para parsing HTML

**Prioridad:** Alta | **Estimacion:** 5 SP | **Estado:** Actualizado (v0.6.0 - Web Scraping + Ports & Adapters)

#### Tareas

- `[BE-DOMAIN]` Record `RiskEntry` (`Domain/Model/ValueObjects/`) — tipo unificado para las tres fuentes:
  - `ListSource` string NOT NULL — discriminador de fuente (`"OFAC"`, `"WORLD_BANK"`, `"ICIJ"`)
  - `Name` string? — nombre de la entidad (nombre OFAC, nombre de firma del Banco Mundial, caption del nodo ICIJ)
  - `Address` string? — direccion fisica (OFAC, Banco Mundial)
  - `Type` string? — tipo de entidad (OFAC)
  - `List` string? — nombre de la lista de sanciones (OFAC)
  - `Programs` string[]? — programas de sanciones (OFAC)
  - `Score` double? — puntuacion de confianza de coincidencia (OFAC)
  - `Country` string? — pais (Banco Mundial)
  - `FromDate` string? — fecha de inicio de inhabilitacion (Banco Mundial)
  - `ToDate` string? — fecha de fin de inhabilitacion (Banco Mundial)
  - `Grounds` string? — motivos de inhabilitacion (Banco Mundial)
  - `Jurisdiction` string? — jurisdiccion legal (ICIJ)
  - `LinkedTo` string? — entidades vinculadas (ICIJ)
  - `DataFrom` string? — nombre del dataset de origen (ICIJ)
  - Los campos no aplicables a una fuente se dejan como `null`
- `[BE-DOMAIN]` Record `SearchResult` (`Domain/Model/ValueObjects/`) con `Hits`, `Entries`, estatico `Empty` y fabrica `Merge`
- `[BE-DOMAIN]` Record `SearchRiskListsQuery` (`Domain/Model/Queries/`) — query CQRS implementando `IRequest<SearchResult>` con `Term` y filtro opcional `SourceNames`
- `[BE-APP]` Puerto `IScrapingSource` (`Application/Ports/`) — interfaz con `SourceName` y `SearchAsync(term, ct)` — define el contrato para los adaptadores de fuentes de scraping
- `[BE-APP]` `SearchRiskListsQueryHandler` (`Application/Search/`) — `IRequestHandler<SearchRiskListsQuery, SearchResult>` de MediatR que orquesta llamadas a fuentes con cache `IMemoryCache` y ejecución paralela via `Task.WhenAll`
- `[BE-INFRA]` `OfacScrapingSource` (`Infrastructure/Sources/`) — adaptador que implementa `IScrapingSource`; orquesta el flujo HTTP GET → POST contra `https://sanctionssearch.ofac.treas.gov/`
- `[BE-INFRA]` `OfacHtmlParser` (`Infrastructure/Sources/`) — helper estático que extrae datos del formulario ASP.NET de la página inicial y parsea la tabla HTML de resultados en records `RiskEntry`
- `[BE-APP]` `SearchRiskListsQueryValidator` (`Application/Search/`) — validador FluentValidation ejecutado automáticamente por `ValidationPipelineBehavior`; valida que `q` no esté vacío y que cada valor de `sources` esté en la whitelist (ofac, worldbank, icij)
- `[BE-INTERFACES]` `ListsController.Search` — controlador thin: crea `SearchRiskListsQuery`, despacha via MediatR, mapea respuesta con `ScrapingResponseMapper`; validación manejada por `ValidationPipelineBehavior`
- `[BE-TEST]` `OfacScrapingSourceTests` (16 tests) — usa `OfacHtmlMother` para fixtures HTML y `FakeHttpMessageHandler` para simulación HTTP
- `[BE-TEST]` `SearchRiskListsQueryHandlerTests` (10 tests) — usa `SearchResultMother` y `RiskEntryMother` para datos de prueba; cubre selección de fuentes, cache y merge de resultados
- `[BE-TEST]` `SearchResultTests` (5 tests) — `Empty`, comportamiento de fábrica `Merge`

#### Criterios de Aceptacion

**Escenario 1: Coincidencias encontradas**
- Given que estoy autenticado
- And envio `GET /api/lists/search?q=john doe&sources=ofac`
- When la peticion se procesa
- Then recibo HTTP 200 con `{ hits: N, entries: [...] }`
- And cada entrada incluye `listSource = "OFAC"`, `name`, `address`, `type`, `list`, `programs`, `score`

**Escenario 2: Sin coincidencias**
- Given que el termino de busqueda no aparece en la lista SDN de OFAC
- When envio `GET /api/lists/search?q=entidad desconocida xyz&sources=ofac`
- Then recibo HTTP 200 con `{ hits: 0, entries: [] }`

**Escenario 3: Parametro de busqueda ausente**
- Given que envio `GET /api/lists/search?sources=ofac` sin el parametro `q`
- When la peticion llega al controlador
- Then recibo HTTP 400 Bad Request

**Escenario 4: Rate limit excedido**
- Given que he enviado mas de 20 peticiones en un minuto desde la misma IP
- When envio una peticion adicional
- Then recibo HTTP 429 Too Many Requests

**Escenario 5: Fuente OFAC no disponible**
- Given que el sitio web de OFAC no es accesible
- When envio la peticion
- Then recibo HTTP 200 con `{ hits: 0, entries: [] }` (tolerante a fallos — se retorna `SearchResult.Empty`)

**Escenario 6: Cache hit**
- Given que una peticion previa para el mismo termino se realizo en los ultimos 10 minutos
- When envio `GET /api/lists/search?q=john doe&sources=ofac` nuevamente
- Then recibo HTTP 200 con el resultado en cache (sin llamada HTTP externa)

---

### US-SCR-002: Buscar en la lista de firmas excluidas del Banco Mundial

**Titulo:** Consultar el registro de Firmas Excluidas del Banco Mundial

**Descripcion:**
Como oficial de compliance, quiero buscar en la lista de firmas excluidas y co-excluidas del Banco Mundial, para identificar proveedores que hayan sido sancionados de participar en proyectos financiados por el Banco Mundial.

**Entregable:**
Endpoint `GET /api/lists/search?q={termino}&sources=worldbank` que realiza web scraping de la página de Firmas Excluidas del Banco Mundial usando `HtmlAgilityPack` para extraer la configuración del API del JavaScript embebido, luego consulta la API JSON y filtra firmas client-side (lógica OR en nombre, dirección, ciudad, estado, país, motivos). Retorna un `ScrapingResponse`. Los resultados se cachean por 10 minutos por término de búsqueda.

**Dependencias:**
- `US-SCR-001` (misma infraestructura, mismo patron)

**Prioridad:** Alta | **Estimacion:** 3 SP | **Estado:** Actualizado (v0.6.0)

#### Tareas

- `[BE-INFRA]` `WorldBankScrapingSource` — adaptador que implementa `IScrapingSource`; flujo de web scraping en dos pasos: (1) GET página HTML → extraer URL del API + key del JavaScript via `WorldBankHtmlParser.ExtractApiConfig()`, (2) GET API JSON → filtrar + mapear via `WorldBankHtmlParser.ParseResults()`; mapea a `RiskEntry` con `ListSource = "WORLD_BANK"`; `ToDate` muestra etiqueta "Ongoing"/"Permanent" cuando aplica; retorna `SearchResult.Empty` ante fallos
- `[BE-INFRA]` `WorldBankHtmlParser` — helper estático unificado (mismo patrón que `OfacHtmlParser`): `ExtractApiConfig()` parsea tags `<script>` con `HtmlAgilityPack` para extraer URL del API + key usando `GeneratedRegex`; `ParseResults()` deserializa JSON `response.ZPROCSUPP`, filtra firmas por término de búsqueda (OR multi-campo, contains case-insensitive), combina componentes de dirección, y mapea `INELIGIBLY_STATUS` a `ToDate` cuando el estado es "Permanent" u "Ongoing"
- `[BE-APP]` `SearchRiskListsQueryHandler` cachea resultado por `scraping:worldbank:{term}` por 10 min (handler compartido — no se necesita orquestador por fuente)
- `[BE-INTERFACES]` `ListsController.Search` con `sources=worldbank` — requiere `q`; sujeto a rate limiting
- `[BE-TEST]` `WorldBankScrapingSourceTests` (18 tests) — usa `WorldBankJsonMother` para fixtures JSON; cubre búsqueda multi-campo (nombre, dirección, país, motivos), mapeo de estado "Ongoing"/"Permanent", combinación de dirección, escenarios de error (error HTTP, JSON inválido, timeout)

#### Criterios de Aceptacion

**Escenario 1: Coincidencias encontradas**
- Given que estoy autenticado
- And envio `GET /api/lists/search?q=acme corp&sources=worldbank`
- When la peticion se procesa
- Then recibo HTTP 200 con `{ hits: N, entries: [...] }`
- And cada entrada incluye `listSource = "WORLD_BANK"`, `name`, `address`, `country`, `fromDate`, `toDate`, `grounds`

**Escenario 2: Sin coincidencias**
- Given que el termino de busqueda no aparece en la tabla de firmas excluidas del Banco Mundial
- When envio `GET /api/lists/search?q=entidad desconocida xyz&sources=worldbank`
- Then recibo HTTP 200 con `{ hits: 0, entries: [] }`

**Escenario 3: Fuente Banco Mundial no disponible**
- Given que la API del Banco Mundial no es accesible o retorna JSON inválido
- When envio la peticion
- Then recibo HTTP 200 con `{ hits: 0, entries: [] }` (tolerante a fallos)

**Escenario 4: Rate limit excedido**
- Given que he excedido 20 peticiones por minuto desde la misma IP
- When envio una peticion adicional
- Then recibo HTTP 429 Too Many Requests

---

### US-SCR-003: Buscar en la base de datos Offshore Leaks de ICIJ

**Titulo:** Consultar la API JSON de Offshore Leaks de ICIJ

**Descripcion:**
Como oficial de compliance, quiero buscar en la base de datos Offshore Leaks del ICIJ, para identificar proveedores vinculados a entidades offshore mencionadas en Panama Papers, Paradise Papers u otras investigaciones similares.

**Entregable:**
Endpoint `GET /api/lists/search?q={termino}&sources=icij` que consulta la API JSON publica del ICIJ, deserializa el array `nodes` y retorna un `ScrapingResponse`. Los resultados se cachean por 10 minutos por termino de busqueda.

**Dependencias:**
- `US-SCR-001` (misma infraestructura, mismo patron)

**Prioridad:** Alta | **Estimacion:** 2 SP | **Estado:** Actualizado (v0.6.0)

#### Tareas

- `[BE-INFRA]` `IcijScrapingSource` — adaptador que implementa `IScrapingSource`; obtiene `https://offshoreleaks.icij.org/api/nodes?q={term}`, deserializa array `nodes` con `System.Text.Json` en DTOs internos `IcijNode`; mapea a `RiskEntry` con `ListSource = "ICIJ"`, `Name` (caption del nodo con fallback a name), `Jurisdiction`, `LinkedTo`, `DataFrom`; los campos OFAC/Banco Mundial quedan en `null`; retorna `SearchResult.Empty` ante fallos
- `[BE-APP]` `SearchRiskListsQueryHandler` cachea resultado por `scraping:icij:{term}` por 10 min (handler compartido)
- `[BE-INTERFACES]` `ListsController.Search` con `sources=icij` — requiere `q`; sujeto a rate limiting
- `[BE-TEST]` Unit test: nodos deserializados correctamente, array `nodes` vacio retorna resultado vacio

#### Criterios de Aceptacion

**Escenario 1: Coincidencias encontradas**
- Given que estoy autenticado
- And envio `GET /api/lists/search?q=mossack fonseca&sources=icij`
- When la peticion se procesa
- Then recibo HTTP 200 con `{ hits: N, entries: [...] }`
- And cada entrada incluye `listSource = "ICIJ"`, `name`, `jurisdiction`, `linkedTo`, `dataFrom`

**Escenario 2: Sin coincidencias**
- Given que el termino de busqueda retorna un array `nodes` vacio desde la API de ICIJ
- When envio la peticion
- Then recibo HTTP 200 con `{ hits: 0, entries: [] }`

**Escenario 3: API de ICIJ no disponible**
- Given que la API de ICIJ no es accesible
- When envio la peticion
- Then recibo HTTP 200 con `{ hits: 0, entries: [] }` (tolerante a fallos)

**Escenario 4: Rate limit excedido**
- Given que he excedido 20 peticiones por minuto desde la misma IP
- When envio una peticion adicional
- Then recibo HTTP 429 Too Many Requests

---

### US-SCR-004: Buscar en todas las listas de riesgo simultaneamente

**Titulo:** Consulta paralela en OFAC, Banco Mundial e ICIJ

**Descripcion:**
Como oficial de compliance, quiero buscar en las tres listas de riesgo con una sola peticion, para obtener una vista consolidada del riesgo en todas las fuentes sin realizar tres llamadas API separadas.

**Entregable:**
Endpoint `GET /api/lists/search?q={termino}` (sin parametro `sources`, o `sources=ofac,worldbank,icij`) que ejecuta las tres consultas a fuentes en paralelo via `Task.WhenAll`, combina los resultados con `SearchResult.Merge` y retorna un unico `ScrapingResponse` con el conteo total de `hits` y la lista unificada de entradas. Cada resultado de fuente individual se cachea independientemente. El parametro `sources` acepta un subconjunto separado por comas para consultar fuentes especificas; cuando se omite, se consultan todas las fuentes registradas.

**Dependencias:**
- `US-SCR-001`, `US-SCR-002`, `US-SCR-003`

**Prioridad:** Critica | **Estimacion:** 2 SP | **Estado:** Actualizado (v0.6.0 - Handler CQRS)

#### Tareas

- `[BE-APP]` `SearchRiskListsQueryHandler.Handle(SearchRiskListsQuery, CancellationToken)` — selecciona fuentes por filtro `SourceNames` (o todas si null/vacío), llama instancias `IScrapingSource` en paralelo via `Task.WhenAll`, combina con `SearchResult.Merge(results)`, cachea cada resultado de fuente independientemente
- `[BE-DOMAIN]` `SearchResult.Merge(IEnumerable<SearchResult>)` — suma `Hits` y concatena listas `Entries` de todas las fuentes; sin deduplicación (una entidad presente en múltiples listas se cuenta múltiples veces — limitación conocida)
- `[BE-APP]` `SearchRiskListsQueryValidator` — reglas FluentValidation: `Term` no vacío, cada valor de `SourceNames` en whitelist; ejecutado automáticamente por `ValidationPipelineBehavior` antes del handler
- `[BE-INTERFACES]` `ListsController.Search` — controlador thin: crea `SearchRiskListsQuery(q, sources)`, despacha via `IMediator`, mapea respuesta con `ScrapingResponseMapper`; sujeto a rate limiting
- `[BE-TEST]` Unit test: resultados de las tres fuentes combinados correctamente; fallo de una fuente no impide que las otras dos retornen resultados

#### Criterios de Aceptacion

**Escenario 1: Coincidencias en multiples fuentes**
- Given que estoy autenticado
- And envio `GET /api/lists/search?q=global corp` (sin parametro `sources` — consulta todas)
- When la peticion se procesa
- Then recibo HTTP 200 con `{ hits: N, entries: [...] }`
- And `hits` es igual a la suma de los conteos de cada fuente
- And cada entrada incluye `listSource` para identificar su origen
- And las entradas de OFAC, Banco Mundial e ICIJ estan todas incluidas en la respuesta

**Escenario 2: Solo una fuente retorna coincidencias**
- Given que solo OFAC retorna coincidencias para el termino de busqueda
- When la peticion se procesa
- Then recibo las entradas de OFAC con el conteo correcto
- And Banco Mundial e ICIJ contribuyen 0 hits y entradas vacias (sin error)

**Escenario 3: Todas las fuentes no disponibles**
- Given que las tres fuentes externas no son accesibles
- When la peticion se procesa
- Then recibo HTTP 200 con `{ hits: 0, entries: [] }` (cada fuente retorna `SearchResult.Empty`)

**Escenario 4: Fallo parcial de fuente**
- Given que OFAC no es accesible pero Banco Mundial e ICIJ estan disponibles
- When la peticion se procesa
- Then recibo HTTP 200 con resultados de Banco Mundial e ICIJ solamente
- And el total de `hits` refleja solo las fuentes disponibles

**Escenario 5: Rate limit excedido**
- Given que he excedido 20 peticiones por minuto desde la misma IP
- When envio una peticion adicional
- Then recibo HTTP 429 Too Many Requests

---

## Epica: Infraestructura de Scraping

---

### TS-SCR-000: Bootstrapping del Modulo Scraping

**Titulo:** Clientes HTTP, caching, rate limiting y registro en DI

**Descripcion:**
Como desarrollador, necesito configurar la infraestructura del modulo de scraping — clientes HTTP tipados con timeout y headers User-Agent, cache en memoria, rate limiting por IP y registro de fuentes de scraping y handlers MediatR en el contenedor de dependencias — para que todas las historias de usuario de scraping tengan una base confiable y protegida.

**Entregable:**
`ScrapingModuleExtensions.AddScrapingModule()` registra clientes HTTP, `IMemoryCache` e implementaciones de adaptadores `IScrapingSource`. El `SearchRiskListsQueryHandler` es auto-descubierto por el assembly scanning de MediatR — no se necesita registro explícito. Rate limiting movido a infraestructura compartida (`AddRateLimiting()` / `UseRateLimiting()`) ya que protege endpoints de todos los modulos. `RateLimitResponseMiddleware` reescribe respuestas 429 al formato estandar `ErrorResponse`.

**Dependencias:**
- Ninguna — sin dependencia de IAM; el modulo registra sus propios servicios independientemente

**Prioridad:** Critica | **Estimacion:** 2 SP | **Estado:** Actualizado (v0.6.0 - Ports & Adapters)

#### Tareas

- `[BE-INFRA]` Tres registros de `HttpClient` tipados — cada uno con `Timeout` y header `User-Agent` configurados para su fuente objetivo
- `[BE-INFRA]` Registro de `IMemoryCache` (si no esta ya registrado por Shared)
- `[BE-INFRA]` Implementaciones de adaptadores `IScrapingSource` registrados como scoped (`OfacScrapingSource`, futuro: `WorldBankScrapingSource`, `IcijScrapingSource`)
- `[BE-APP]` `SearchRiskListsQueryHandler` auto-descubierto por assembly scanning de MediatR; recibe `IEnumerable<IScrapingSource>` (todas las fuentes inyectadas via DI) e `IMemoryCache`
- `[BE-INFRA]` Rate limiting por IP via `AspNetCoreRateLimit` (movido a infraestructura compartida) con reglas escalonadas: `POST /api/authentication/sign-in` (5 req/min — proteccion contra fuerza bruta), `GET /api/lists/*` (20 req/min — proteccion de fuentes externas), `*:/api/*` (100 req/min — fallback general)
- `[BE-INFRA]` `RateLimitResponseMiddleware` — intercepta respuestas 429 y reescribe al formato estandar `ErrorResponse` (RFC 7807) con codigo de error `RATE_LIMIT_EXCEEDED` (7000)
- `[BE-INFRA]` `UseRateLimiting(app)` conecta el middleware `app.UseIpRateLimiting()` (infraestructura compartida)
- `[BE-TEST]` Integration test: el rate limiter rechaza la peticion numero 21 por minuto con 429 en formato `ErrorResponse`

#### Criterios de Aceptacion

- Given que la aplicacion arranca
- When se llaman `AddScrapingModule()`, `AddRateLimiting()` y `UseRateLimiting()`
- Then todas las implementaciones de adaptadores `IScrapingSource` son resolvibles desde DI
- And `SearchRiskListsQueryHandler` es resolvible y recibe todas las fuentes via `IEnumerable<IScrapingSource>`
- And el rate limiter esta activo con reglas escalonadas: sign-in (5/min), lists (20/min), API general (100/min)
- And las peticiones que exceden el limite reciben HTTP 429 en formato `ErrorResponse` con `errorCode: "RATE_LIMIT_EXCEEDED"` y header `Retry-After`

---

## Fuera de Alcance — v1.0

### US-SCR-005: Persistir historial de scraping *(diferido)*

Almacenar registros `SearchResult` en una tabla dedicada `scraping_results` para auditoria y analisis de tendencias. Diferido: el cache (TTL de 10 min) es suficiente para los casos de uso de v1.0 y el `ScreeningResult` en el modulo Suppliers ya registra el resultado de riesgo agregado junto con las entradas coincidentes en `entries_json`.

### US-SCR-006: TTL de cache configurable *(diferido)*

Exponer el TTL del cache como un valor configurable en `appsettings.json`. Actualmente hardcodeado en 10 minutos en `SearchRiskListsQueryHandler`.

### US-SCR-007: Fuentes de listas de riesgo adicionales *(diferido)*

Integracion de fuentes adicionales (Sanciones UE, Consejo de Seguridad ONU, INTERPOL). El puerto `IScrapingSource` esta disenado para incorporar nuevas implementaciones de adaptadores sin cambios en el handler ni en el controlador.

---

## Notas de Implementacion

| Aspecto | Implementacion |
|---------|---------------|
| Arquitectura | Ports & Adapters (Hexagonal): puerto `IScrapingSource` en `Application/Ports/`, implementaciones de adaptadores en `Infrastructure/Sources/`, query handler CQRS en `Application/Search/` |
| Diseno sin estado | Sin tablas SQL; sin entidades EF Core. Todos los datos viven en respuestas HTTP e `IMemoryCache` |
| Formato `RiskEntry` | Record unificado en `Domain/Model/ValueObjects/`; `ListSource` discrimina el origen; campos no aplicables son `null` |
| Campos OFAC | `listSource`, `name`, `address`, `type`, `list`, `programs` (string[]), `score` (double?) |
| Campos Banco Mundial | `listSource`, `name` (nombre de firma mapeado aqui), `address`, `country`, `fromDate`, `toDate`, `grounds` |
| Campos ICIJ | `listSource`, `name` (caption del nodo), `jurisdiction`, `linkedTo`, `dataFrom` |
| Patron CQRS | `SearchRiskListsQuery` → `SearchRiskListsQueryHandler` via MediatR (mismo patron que módulos IAM y Suppliers) |
| Formato de clave de cache | `scraping:{FUENTE}:{termino}` — por fuente, por termino; TTL de 10 minutos |
| Tolerancia a fallos | Cada adaptador `IScrapingSource` envuelve toda su implementacion en try/catch y retorna `SearchResult.Empty` ante cualquier error — el handler nunca propaga excepciones a nivel de fuente |
| Rate limiting | Basado en IP via `AspNetCoreRateLimit` (infraestructura compartida) con reglas escalonadas: sign-in (5/min), lists (20/min), API general (100/min). `RateLimitResponseMiddleware` reescribe 429 al formato estandar `ErrorResponse` con `RATE_LIMIT_EXCEEDED` (7000) y header `Retry-After` |
| Endpoint unificado | Un solo `GET /api/lists/search?q={term}&sources=ofac&sources=worldbank` — el parametro `sources` es opcional (query params repetidos: ofac, worldbank, icij); cuando se omite, se consultan todas las fuentes. `SearchRiskListsQueryValidator` (FluentValidation) valida la entrada antes de que el handler se ejecute |
| Ejecucion paralela | `SearchRiskListsQueryHandler` usa `Task.WhenAll` — todas las fuentes seleccionadas se consultan concurrentemente; la latencia total es la de la fuente mas lenta, no la suma |
| Scraping OFAC | `OfacScrapingSource` orquesta el flujo GET → POST; `OfacHtmlParser` extrae datos del formulario y parsea la tabla HTML de resultados con `HtmlAgilityPack` |
| Scraping Banco Mundial | Web scraping en dos pasos via `WorldBankHtmlParser` unificado: (1) `ExtractApiConfig()` scrapea tags `<script>` con `HtmlAgilityPack` para extraer URL del API + key, (2) `ParseResults()` deserializa JSON y filtra client-side (lógica OR en nombre, dirección, ciudad, estado, país, motivos); mapea "Ongoing"/"Permanent" a `toDate` |
| Integracion ICIJ | API REST JSON; deserializacion con `System.Text.Json`; caption del nodo mapeado al campo `name` |
| Sin deduplicacion | `SearchResult.Merge` suma hits y concatena entradas; una entidad presente en multiples listas se cuenta multiples veces — limitacion conocida de v1.0 |
| Uso entre modulos | `SearchRiskListsQueryHandler` es consumido via MediatR por `RunScreeningCommandHandler` en el modulo Suppliers; el modulo Scraping no tiene dependencia de Suppliers |
| Infraestructura de tests | Patron Mother: `RiskEntryMother`, `SearchResultMother`, `OfacHtmlMother`; `FakeHttpMessageHandler` para simulación HTTP |
| Estado de implementacion | Todas las historias US-SCR y TS-SCR-000 implementadas en v0.6.0 |
