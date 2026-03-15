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
Endpoint `GET /api/lists/ofac?q={termino}` que consulta el feed XML de la lista SDN de OFAC, filtra entradas cuyo nombre contiene el termino de busqueda (sin distincion de mayusculas), y retorna un `ScrapingResponse` con el conteo de coincidencias y las entradas encontradas. Los resultados se cachean por 10 minutos por termino de busqueda.

**Dependencias:**
- `US-IAM-001`: autenticacion JWT
- `ScrapingModuleExtensions.AddScrapingModule()` registrado — cliente HTTP e `IMemoryCache` configurados

**Prioridad:** Alta | **Estimacion:** 3 SP | **Estado:** Updated (v0.5.1)

#### Tareas

- `[BE-DOMAIN]` Record `RiskEntry` — tipo unificado para las tres fuentes:
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
- `[BE-DOMAIN]` Record `SearchResult` con `Hits`, `Entries`, estatico `Empty` y fabrica `Merge`
- `[BE-INFRA]` Interfaz `IScrapingSource` con `SourceName` y `SearchAsync(term, ct)`
- `[BE-INFRA]` `OfacScrapingSource` — descarga el ZIP SDN de `https://sdn.ofac.treas.gov/SDN_XML.zip`, descomprime en memoria, parsea XML con `XDocument`, busqueda de nombre case-insensitive; mapea a `RiskEntry` con `ListSource = "OFAC"`, `Name`, `Address`, `Type`, `List`, `Programs`, `Score`; retorna `SearchResult.Empty` ante cualquier fallo
- `[BE-INFRA]` `ScrapingOrchestrationService.SearchSourceAsync("ofac", term)` — cachea resultado por `scraping:ofac:{term}` por 10 min
- `[BE-INTERFACES]` `ListsController.SearchOfac` — requiere parametro `q`; delega a `SearchSourceAsync`; sujeto a rate limiting (20 req/min por IP)
- `[BE-TEST]` Unit test: entradas coincidentes retornadas con todos los campos OFAC, sin coincidencias retorna resultado vacio, `q` ausente retorna 400

#### Criterios de Aceptacion

**Escenario 1: Coincidencias encontradas**
- Given que estoy autenticado
- And envio `GET /api/lists/ofac?q=john doe`
- When la peticion se procesa
- Then recibo HTTP 200 con `{ hits: N, entries: [...] }`
- And cada entrada incluye `listSource = "OFAC"`, `name`, `address`, `type`, `list`, `programs`, `score`

**Escenario 2: Sin coincidencias**
- Given que el termino de busqueda no aparece en la lista SDN de OFAC
- When envio `GET /api/lists/ofac?q=entidad desconocida xyz`
- Then recibo HTTP 200 con `{ hits: 0, entries: [] }`

**Escenario 3: Parametro de busqueda ausente**
- Given que envio `GET /api/lists/ofac` sin el parametro `q`
- When la peticion llega al controlador
- Then recibo HTTP 400 Bad Request

**Escenario 4: Rate limit excedido**
- Given que he enviado mas de 20 peticiones en un minuto desde la misma IP
- When envio una peticion adicional
- Then recibo HTTP 429 Too Many Requests

**Escenario 5: Fuente OFAC no disponible**
- Given que el endpoint ZIP de OFAC no es accesible
- When envio la peticion
- Then recibo HTTP 200 con `{ hits: 0, entries: [] }` (tolerante a fallos — se retorna `SearchResult.Empty`)

**Escenario 6: Cache hit**
- Given que una peticion previa para el mismo termino se realizo en los ultimos 10 minutos
- When envio `GET /api/lists/ofac?q=john doe` nuevamente
- Then recibo HTTP 200 con el resultado en cache (sin llamada HTTP externa)

---

### US-SCR-002: Buscar en la lista de firmas excluidas del Banco Mundial

**Titulo:** Consultar el registro de Firmas Excluidas del Banco Mundial

**Descripcion:**
Como oficial de compliance, quiero buscar en la lista de firmas excluidas y co-excluidas del Banco Mundial, para identificar proveedores que hayan sido sancionados de participar en proyectos financiados por el Banco Mundial.

**Entregable:**
Endpoint `GET /api/lists/worldbank?q={termino}` que realiza scraping de la pagina HTML de firmas excluidas del Banco Mundial usando `HtmlAgilityPack`, extrae las filas de tabla coincidentes y retorna un `ScrapingResponse`. Los resultados se cachean por 10 minutos por termino de busqueda.

**Dependencias:**
- `US-SCR-001` (misma infraestructura, mismo patron)

**Prioridad:** Alta | **Estimacion:** 3 SP | **Estado:** Updated (v0.5.1)

#### Tareas

- `[BE-INFRA]` `WorldBankScrapingSource` — obtiene `https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms?srchTerm={term}`, parsea tabla HTML con `HtmlAgilityPack`, extrae nombre de firma (mapeado a `Name`), `Address`, `Country`, `FromDate`, `ToDate`, `Grounds`; mapea a `RiskEntry` con `ListSource = "WORLD_BANK"`; retorna `SearchResult.Empty` ante fallos
- `[BE-INFRA]` `ScrapingOrchestrationService.SearchSourceAsync("worldbank", term)` — cachea resultado por `scraping:worldbank:{term}` por 10 min
- `[BE-INTERFACES]` `ListsController.SearchWorldBank` — requiere `q`; sujeto a rate limiting
- `[BE-TEST]` Unit test: filas coincidentes retornadas, fallo de parse HTML retorna resultado vacio

#### Criterios de Aceptacion

**Escenario 1: Coincidencias encontradas**
- Given que estoy autenticado
- And envio `GET /api/lists/worldbank?q=acme corp`
- When la peticion se procesa
- Then recibo HTTP 200 con `{ hits: N, entries: [...] }`
- And cada entrada incluye `listSource = "WORLD_BANK"`, `name`, `address`, `country`, `fromDate`, `toDate`, `grounds`

**Escenario 2: Sin coincidencias**
- Given que el termino de busqueda no aparece en la tabla de firmas excluidas del Banco Mundial
- When envio `GET /api/lists/worldbank?q=entidad desconocida xyz`
- Then recibo HTTP 200 con `{ hits: 0, entries: [] }`

**Escenario 3: Fuente Banco Mundial no disponible**
- Given que la pagina del Banco Mundial no es accesible o retorna HTML inesperado
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
Endpoint `GET /api/lists/icij?q={termino}` que consulta la API JSON publica del ICIJ, deserializa el array `nodes` y retorna un `ScrapingResponse`. Los resultados se cachean por 10 minutos por termino de busqueda.

**Dependencias:**
- `US-SCR-001` (misma infraestructura, mismo patron)

**Prioridad:** Alta | **Estimacion:** 2 SP | **Estado:** Updated (v0.5.1)

#### Tareas

- `[BE-INFRA]` `IcijScrapingSource` — obtiene `https://offshoreleaks.icij.org/api/nodes?q={term}`, deserializa array `nodes` con `System.Text.Json` en DTOs internos `IcijNode`; mapea a `RiskEntry` con `ListSource = "ICIJ"`, `Name` (caption del nodo con fallback a name), `Jurisdiction`, `LinkedTo`, `DataFrom`; los campos OFAC/Banco Mundial quedan en `null`; retorna `SearchResult.Empty` ante fallos
- `[BE-INFRA]` `ScrapingOrchestrationService.SearchSourceAsync("icij", term)` — cachea resultado por `scraping:icij:{term}` por 10 min
- `[BE-INTERFACES]` `ListsController.SearchIcij` — requiere `q`; sujeto a rate limiting
- `[BE-TEST]` Unit test: nodos deserializados correctamente, array `nodes` vacio retorna resultado vacio

#### Criterios de Aceptacion

**Escenario 1: Coincidencias encontradas**
- Given que estoy autenticado
- And envio `GET /api/lists/icij?q=mossack fonseca`
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
Endpoint `GET /api/lists/all?q={termino}` que ejecuta las tres consultas a fuentes en paralelo via `Task.WhenAll`, combina los resultados con `SearchResult.Merge` y retorna un unico `ScrapingResponse` con el conteo total de `hits` y la lista unificada de entradas. Cada resultado de fuente individual se cachea independientemente.

**Dependencias:**
- `US-SCR-001`, `US-SCR-002`, `US-SCR-003`

**Prioridad:** Critica | **Estimacion:** 2 SP | **Estado:** Implementado (v0.5.0)

#### Tareas

- `[BE-INFRA]` `ScrapingOrchestrationService.SearchAllAsync(term)` — llama todas las instancias `IScrapingSource` registradas en paralelo via `Task.WhenAll`, combina con `SearchResult.Merge(results)`
- `[BE-DOMAIN]` `SearchResult.Merge(IEnumerable<SearchResult>)` — suma `Hits` y concatena listas `Entries` de todas las fuentes
- `[BE-INTERFACES]` `ListsController.SearchAll` — requiere `q`; delega a `SearchAllAsync`; sujeto a rate limiting
- `[BE-TEST]` Unit test: resultados de las tres fuentes combinados correctamente; fallo de una fuente no impide que las otras dos retornen resultados

#### Criterios de Aceptacion

**Escenario 1: Coincidencias en multiples fuentes**
- Given que estoy autenticado
- And envio `GET /api/lists/all?q=global corp`
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
Como desarrollador, necesito configurar la infraestructura del modulo de scraping — clientes HTTP tipados con timeout y headers User-Agent, cache en memoria, rate limiting por IP y registro del servicio de orquestacion en el contenedor de dependencias — para que todas las historias de usuario de scraping tengan una base confiable y protegida.

**Entregable:**
`ScrapingModuleExtensions.AddScrapingModule()` registra clientes HTTP y cache. Rate limiting movido a infraestructura compartida (`AddRateLimiting()` / `UseRateLimiting()`) ya que protege endpoints de todos los modulos. `RateLimitResponseMiddleware` reescribe respuestas 429 al formato estandar `ErrorResponse`.

**Dependencias:**
- Ninguna — sin dependencia de IAM; el modulo registra sus propios servicios independientemente

**Prioridad:** Critica | **Estimacion:** 2 SP | **Estado:** Implementado (v0.5.0)

#### Tareas

- `[BE-INFRA]` Tres registros de `HttpClient` tipados — cada uno con `Timeout` y header `User-Agent` configurados para su fuente objetivo
- `[BE-INFRA]` Registro de `IMemoryCache` (si no esta ya registrado por Shared)
- `[BE-INFRA]` `ScrapingOrchestrationService` registrado como scoped; recibe `IEnumerable<IScrapingSource>` (las tres fuentes inyectadas via DI)
- `[BE-INFRA]` Rate limiting por IP via `AspNetCoreRateLimit` (movido a infraestructura compartida) con reglas escalonadas: `POST /api/authentication/sign-in` (5 req/min — proteccion contra fuerza bruta), `GET /api/lists/*` (20 req/min — proteccion de fuentes externas), `*:/api/*` (100 req/min — fallback general)
- `[BE-INFRA]` `RateLimitResponseMiddleware` — intercepta respuestas 429 y reescribe al formato estandar `ErrorResponse` (RFC 7807) con codigo de error `RATE_LIMIT_EXCEEDED` (7000)
- `[BE-INFRA]` `UseRateLimiting(app)` conecta el middleware `app.UseIpRateLimiting()` (infraestructura compartida)
- `[BE-TEST]` Integration test: el rate limiter rechaza la peticion numero 21 por minuto con 429 en formato `ErrorResponse`

#### Criterios de Aceptacion

- Given que la aplicacion arranca
- When se llaman `AddScrapingModule()`, `AddRateLimiting()` y `UseRateLimiting()`
- Then las tres implementaciones de `IScrapingSource` son resolvibles desde DI
- And `ScrapingOrchestrationService` es resolvible y recibe las tres fuentes
- And el rate limiter esta activo con reglas escalonadas: sign-in (5/min), lists (20/min), API general (100/min)
- And las peticiones que exceden el limite reciben HTTP 429 en formato `ErrorResponse` con `errorCode: "RATE_LIMIT_EXCEEDED"` y header `Retry-After`

---

## Fuera de Alcance — v1.0

### US-SCR-005: Persistir historial de scraping *(diferido)*

Almacenar registros `SearchResult` en una tabla dedicada `scraping_results` para auditoria y analisis de tendencias. Diferido: el cache (TTL de 10 min) es suficiente para los casos de uso de v1.0 y el `ScreeningResult` en el modulo Suppliers ya registra el resultado de riesgo agregado junto con las entradas coincidentes en `entries_json`.

### US-SCR-006: TTL de cache configurable *(diferido)*

Exponer el TTL del cache como un valor configurable en `appsettings.json`. Actualmente hardcodeado en 10 minutos en `ScrapingOrchestrationService`.

### US-SCR-007: Fuentes de listas de riesgo adicionales *(diferido)*

Integracion de fuentes adicionales (Sanciones UE, Consejo de Seguridad ONU, INTERPOL). La interfaz `IScrapingSource` esta disenada para incorporar nuevas fuentes sin cambios en el orquestador ni en el controlador.

---

## Notas de Implementacion

| Aspecto | Implementacion |
|---------|---------------|
| Diseno sin estado | Sin tablas SQL; sin entidades EF Core. Todos los datos viven en respuestas HTTP e `IMemoryCache` |
| Formato `RiskEntry` | Tipo unificado para las tres fuentes; `ListSource` discrimina el origen; campos no aplicables son `null` |
| Campos OFAC | `listSource`, `name`, `address`, `type`, `list`, `programs` (string[]), `score` (double?) |
| Campos Banco Mundial | `listSource`, `name` (nombre de firma mapeado aqui), `address`, `country`, `fromDate`, `toDate`, `grounds` |
| Campos ICIJ | `listSource`, `name` (caption del nodo), `jurisdiction`, `linkedTo`, `dataFrom` |
| Formato de clave de cache | `scraping:{FUENTE}:{termino}` — por fuente, por termino; TTL de 10 minutos |
| Tolerancia a fallos | Cada `IScrapingSource` envuelve toda su implementacion en try/catch y retorna `SearchResult.Empty` ante cualquier error — el orquestador nunca propaga excepciones a nivel de fuente |
| Rate limiting | Basado en IP via `AspNetCoreRateLimit` (infraestructura compartida) con reglas escalonadas: sign-in (5/min), lists (20/min), API general (100/min). `RateLimitResponseMiddleware` reescribe 429 al formato estandar `ErrorResponse` con `RATE_LIMIT_EXCEEDED` (7000) y header `Retry-After` |
| Ejecucion paralela | `SearchAllAsync` usa `Task.WhenAll` — las tres fuentes se consultan concurrentemente; la latencia total es la de la fuente mas lenta, no la suma |
| Parseo OFAC | Descarga y descomprime el ZIP SDN completo en memoria en cada cache miss; sin escrituras en disco local |
| Parseo Banco Mundial | `HtmlAgilityPack` se usa para parseo robusto de tabla HTML |
| Integracion ICIJ | API REST JSON; deserializacion con `System.Text.Json`; caption del nodo mapeado al campo `name` |
| Sin deduplicacion | `SearchResult.Merge` suma hits y concatena entradas; una entidad presente en multiples listas se cuenta multiples veces — limitacion conocida de v1.0 |
| Uso entre modulos | `ScrapingOrchestrationService` es consumido directamente por `RunScreeningCommandHandler` en el modulo Suppliers; el modulo Scraping no tiene dependencia de Suppliers |
| Estado de implementacion | Todas las historias US-SCR y TS-SCR-000 implementadas en v0.5.0 |
