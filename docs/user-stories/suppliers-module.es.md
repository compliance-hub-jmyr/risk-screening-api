# User Stories — Modulo Suppliers (Gestión de Proveedores)

> **Formato:** Titulo / Descripcion / Entregable / Dependencias / Criterios de Aceptacion (BDD Given/When/Then).
> **Tags de tareas:** `[BE-DOMAIN]` `[BE-APP]` `[BE-INFRA]` `[BE-INTERFACES]` `[BE-DB]` `[BE-TEST]` `[DOCS]`

---

## Historia Tecnica: Bootstrapping del Modulo Suppliers

---

### TS-SUP-000: Setup Inicial del Modulo Suppliers

**Titulo:** Scripts de base de datos, configuracion EF Core y registro del modulo

**Descripcion:**
Como desarrollador, necesito configurar la infraestructura fundamental del modulo Suppliers — tablas de base de datos, configuraciones de entidad EF Core, implementaciones de repositorios y registro en el contenedor de dependencias — para que todas las historias de usuario de Suppliers tengan una base estable sobre la que construir.

**Entregable:**
Scripts de migracion SQL V005–V006, configuraciones EF Core para `Supplier` y `ScreeningResult`, `SupplierRepository`, `ScreeningResultRepository` y `SuppliersModuleExtensions` registrado en `Program.cs`.

**Dependencias:**
- `TS-IAM-000`: Setup inicial del modulo IAM (shared kernel, `AppDbContext`, `BaseRepository`)

**Prioridad:** Critica | **Estimacion:** 3 SP | **Estado:** Implementado (v0.4.0)

#### Tareas

- `[BE-DB]` Script `V005__create_suppliers_table.sql` — columnas: `id`, `name`, `tax_id` (unico), `country`, `contact_email`, `contact_phone`, `address`, `notes`, `risk_level` (CHECK: NONE/LOW/MEDIUM/HIGH), `status` (CHECK: PENDING/APPROVED/REJECTED/UNDER_REVIEW), `created_at`, `updated_at`, `created_by`, `updated_by`; indices: `IX_suppliers_risk_level`, `IX_suppliers_status`, `IX_suppliers_country`
- `[BE-DB]` Script `V006__create_screening_results_table.sql` — columnas: `id`, `supplier_id` (FK → `suppliers(id)` ON DELETE CASCADE), `screened_at`, `risk_level` (CHECK), `total_matches`, `created_at`; indices: `IX_screening_results_supplier_id`, `IX_screening_results_screened_at DESC`, `IX_screening_results_risk_level`
- `[BE-INFRA]` EF Core `SupplierConfiguration` y `ScreeningResultConfiguration`
- `[BE-INFRA]` `SupplierRepository` y `ScreeningResultRepository` implementando `BaseRepository`
- `[BE-INFRA]` `SuppliersModuleExtensions.AddSuppliersModule()` — registra ambos repositorios como scoped

#### Criterios de Aceptacion

- Given que la aplicacion arranca contra una base de datos nueva
- When la secuencia de migracion DbUp se completa
- Then la tabla `suppliers` existe con todas las columnas, PK, restriccion unica en `tax_id`, CHECK constraints y tres indices
- And la tabla `screening_results` existe con FK a `suppliers`, CASCADE DELETE y tres indices
- And arranques posteriores son idempotentes (DbUp no re-ejecuta scripts ya aplicados)

---

## Epica: Gestion de Proveedores

---

### US-SUP-001: Registrar un nuevo proveedor

**Titulo:** Crear un registro de proveedor

**Descripcion:**
Como oficial de compliance o administrador, quiero registrar un nuevo proveedor en la plataforma, para poder hacer seguimiento de su perfil de riesgo y ejecutar verificaciones de screening contra listas internacionales de riesgo.

**Entregable:**
Endpoint `POST /api/suppliers` que valida la solicitud, verifica duplicados de `TaxId`, crea el agregado `Supplier` con estado `PENDING` y nivel de riesgo `NONE`, y retorna `201 Created` con el Id del nuevo proveedor.

**Dependencias:**
- `TS-SUP-000`: bootstrapping del modulo (tablas, EF Core, DI)
- `US-IAM-001`: autenticacion JWT

**Prioridad:** Alta | **Estimacion:** 3 SP | **Estado:** Implementado (v0.4.0)

#### Tareas

- `[BE-DOMAIN]` Agregado `Supplier` con metodo fabrica `Create(name, taxId, country, contactEmail?, contactPhone?, address?, notes?)` — inicializa `Status = PENDING`, `RiskLevel = NONE`
- `[BE-DOMAIN]` Reglas de valor en `Create`: `Name` ≤ 200, `TaxId` ≤ 50, `Country` ≤ 100, `ContactEmail` formato valido
- `[BE-DOMAIN]` `SupplierTaxIdAlreadyExistsException` (extiende `BusinessRuleViolationException`)
- `[BE-APP]` `CreateSupplierCommand` + `CreateSupplierCommandHandler` — verifica `ExistsByTaxIdAsync`, llama `Supplier.Create`, persiste, hace commit, retorna el nuevo Id
- `[BE-APP]` `CreateSupplierCommandValidator` (FluentValidation)
- `[BE-INTERFACES]` `SuppliersController.Create` — mapea `CreateSupplierRequest` → `CreateSupplierCommand`; retorna `201 Created` con header `Location`
- `[BE-TEST]` Unit test: creacion exitosa, TaxId duplicado retorna 409, errores de validacion retornan 400

#### Criterios de Aceptacion

**Escenario 1: Creacion exitosa**
- Given que soy un usuario autenticado
- And envio `POST /api/suppliers` con `name`, `taxId` y `country` validos
- When la peticion se procesa
- Then recibo HTTP 201 Created
- And el cuerpo de la respuesta contiene el Id del nuevo proveedor
- And el header `Location` apunta a `GET /api/suppliers/{id}`
- And el proveedor tiene `status = PENDING` y `riskLevel = NONE`

**Escenario 2: TaxId duplicado**
- Given que ya existe un proveedor con el mismo `taxId`
- When envio `POST /api/suppliers` con ese `taxId`
- Then recibo HTTP 409 Conflict con un mensaje descriptivo

**Escenario 3: Errores de validacion**
- Given que envio la solicitud sin `name` o con formato de `contactEmail` invalido
- When la peticion llega al pipeline de validacion
- Then recibo HTTP 400 Bad Request con listado de errores de validacion por campo

**Escenario 4: Sin autenticacion**
- Given que no incluyo token JWT Bearer
- When envio `POST /api/suppliers`
- Then recibo HTTP 401 Unauthorized

---

### US-SUP-002: Listar todos los proveedores (paginado)

**Titulo:** Listado paginado de proveedores

**Descripcion:**
Como oficial de compliance o administrador, quiero ver el listado paginado de todos los proveedores registrados, para monitorear sus estados y niveles de riesgo desde el panel principal.

**Entregable:**
Endpoint `GET /api/suppliers` que retorna una lista paginada y ordenada de proveedores no eliminados, con soporte para los parametros `page`, `size`, `sortBy` y `sortDirection`.

**Dependencias:**
- `US-SUP-001`

**Prioridad:** Alta | **Estimacion:** 2 SP | **Estado:** Implementado (v0.4.0)

#### Tareas

- `[BE-APP]` `GetAllSuppliersQuery` (page, size, sortBy, sortDirection) + `GetAllSuppliersQueryHandler` — filtra `Status = REJECTED`, ordena por `UpdatedAt` desc
- `[BE-INFRA]` `ISupplierRepository.Query()` retornando `IQueryable<Supplier>` para composicion LINQ
- `[BE-INTERFACES]` `SuppliersController.GetAll` — mapea parametros de query a `GetAllSuppliersQuery`; retorna `PageResponse<SupplierResponse>`
- `[BE-TEST]` Unit test: listado paginado, ordenamiento, resultado vacio

#### Criterios de Aceptacion

**Escenario 1: Listado exitoso**
- Given que estoy autenticado y existen proveedores
- When llamo `GET /api/suppliers?page=0&size=10`
- Then recibo HTTP 200 con `{ content: [...], page: { number, size, totalElements, totalPages } }`
- And cada entrada incluye `{ id, name, taxId, country, riskLevel, status, createdAt, updatedAt }`
- And los proveedores eliminados (`status = REJECTED`) no estan incluidos

**Escenario 2: Ordenamiento**
- Given que llamo `GET /api/suppliers?sortBy=name&sortDirection=asc`
- When la peticion se procesa
- Then recibo los proveedores ordenados por nombre ascendente

**Escenario 3: Resultado vacio**
- Given que no hay proveedores registrados aun
- When llamo `GET /api/suppliers`
- Then recibo HTTP 200 con `{ content: [], page: { totalElements: 0 } }`

---

### US-SUP-003: Obtener proveedor por ID

**Titulo:** Consultar el perfil completo de un proveedor

**Descripcion:**
Como oficial de compliance o administrador, quiero ver el perfil completo de un proveedor especifico, para revisar sus detalles, nivel de riesgo actual y estado antes de tomar una decision.

**Entregable:**
Endpoint `GET /api/suppliers/{supplierId}` que retorna el perfil completo del proveedor.

**Dependencias:**
- `US-SUP-002`

**Prioridad:** Alta | **Estimacion:** 1 SP | **Estado:** Implementado (v0.4.0)

#### Tareas

- `[BE-APP]` `GetSupplierByIdQuery` + `GetSupplierByIdQueryHandler` — lanza `SupplierNotFoundException` si no se encuentra
- `[BE-INTERFACES]` `SuppliersController.GetById` — mapea 404 via `GlobalExceptionHandler`
- `[BE-TEST]` Unit test: proveedor encontrado, proveedor no encontrado retorna 404

#### Criterios de Aceptacion

**Escenario 1: Proveedor encontrado**
- Given que estoy autenticado y el supplierId existe
- When llamo `GET /api/suppliers/{supplierId}`
- Then recibo HTTP 200 con el perfil completo del proveedor incluyendo todos los campos opcionales

**Escenario 2: Proveedor no encontrado**
- Given que el supplierId no existe en la base de datos
- When llamo `GET /api/suppliers/{supplierId}`
- Then recibo HTTP 404 Not Found con un mensaje de error descriptivo

---

### US-SUP-004: Actualizar informacion del proveedor

**Titulo:** Editar los datos de un proveedor

**Descripcion:**
Como oficial de compliance o administrador, quiero actualizar la informacion de un proveedor, para que el registro se mantenga preciso cuando cambien los datos de contacto o comerciales.

**Entregable:**
Endpoint `PUT /api/suppliers/{supplierId}` que actualiza todos los campos mutables. `TaxId` esta intencionalmente excluido — no puede modificarse despues de la creacion.

**Dependencias:**
- `US-SUP-003`

**Prioridad:** Alta | **Estimacion:** 2 SP | **Estado:** Implementado (v0.4.0)

#### Tareas

- `[BE-DOMAIN]` Metodo `Supplier.Update(name, country, contactEmail?, contactPhone?, address?, notes?)` — aplica nuevos valores, enforces guard `EnsureNotDeleted()`
- `[BE-APP]` `UpdateSupplierCommand` + `UpdateSupplierCommandHandler` — carga proveedor, llama `Update`, hace commit
- `[BE-APP]` `UpdateSupplierCommandValidator` (FluentValidation — mismas reglas que Create, mas Id requerido)
- `[BE-INTERFACES]` `SuppliersController.Update` — retorna 204 No Content
- `[BE-TEST]` Unit test: actualizacion exitosa, proveedor no encontrado retorna 404, proveedor eliminado retorna 422

#### Criterios de Aceptacion

**Escenario 1: Actualizacion exitosa**
- Given que estoy autenticado y el proveedor existe y no esta eliminado
- When llamo `PUT /api/suppliers/{supplierId}` con los campos actualizados validos
- Then recibo HTTP 204 No Content
- And un `GET /api/suppliers/{supplierId}` posterior refleja los nuevos valores

**Escenario 2: Proveedor no encontrado**
- Given que el supplierId no existe
- When llamo `PUT /api/suppliers/{supplierId}`
- Then recibo HTTP 404 Not Found

**Escenario 3: Proveedor ya eliminado**
- Given que el proveedor tiene `status = REJECTED`
- When llamo `PUT /api/suppliers/{supplierId}`
- Then recibo HTTP 422 Unprocessable Entity con mensaje de violacion de regla de negocio

**Escenario 4: Errores de validacion**
- Given que envio la solicitud con formato de `contactEmail` invalido
- When la peticion llega al pipeline de validacion
- Then recibo HTTP 400 Bad Request

---

### US-SUP-005: Eliminar proveedor (soft delete)

**Titulo:** Eliminacion logica de un proveedor

**Descripcion:**
Como administrador, quiero eliminar logicamente un registro de proveedor, para que proveedores dados de baja o fraudulentos ya no aparezcan en los listados activos sin perder el historial de auditoria.

**Entregable:**
Endpoint `DELETE /api/suppliers/{supplierId}` que cambia el `Status` del proveedor a `REJECTED` (soft delete). El registro permanece en la base de datos y se filtra de todos los listados.

**Dependencias:**
- `US-SUP-003`

**Prioridad:** Alta | **Estimacion:** 1 SP | **Estado:** Implementado (v0.4.0)

#### Tareas

- `[BE-DOMAIN]` Metodo `Supplier.Delete()` — establece `Status = REJECTED`; guard `EnsureNotDeleted()` lanza `SupplierAlreadyDeletedException` si ya esta eliminado
- `[BE-APP]` `DeleteSupplierCommand` + `DeleteSupplierCommandHandler`
- `[BE-INTERFACES]` `SuppliersController.Delete` — retorna 204 No Content
- `[BE-TEST]` Unit test: eliminacion exitosa, ya eliminado retorna 404, proveedor no encontrado retorna 404

#### Criterios de Aceptacion

**Escenario 1: Eliminacion exitosa**
- Given que soy ADMIN autenticado y el proveedor existe y no esta eliminado
- When llamo `DELETE /api/suppliers/{supplierId}`
- Then recibo HTTP 204 No Content
- And el proveedor ya no aparece en `GET /api/suppliers`
- And el `status` del proveedor es `REJECTED` en la base de datos (soft delete — registro no eliminado)

**Escenario 2: Ya eliminado**
- Given que el proveedor ya tiene `status = REJECTED`
- When llamo `DELETE /api/suppliers/{supplierId}`
- Then recibo HTTP 404 Not Found

**Escenario 3: Proveedor no encontrado**
- Given que el supplierId no existe
- When llamo `DELETE /api/suppliers/{supplierId}`
- Then recibo HTTP 404 Not Found

---

## Epica: Screening de Proveedores

---

### US-SUP-006: Ejecutar screening para un proveedor

**Titulo:** Disparar una verificacion de riesgo en tiempo real

**Descripcion:**
Como oficial de compliance, quiero disparar un screening de riesgo para un proveedor contra listas internacionales de sanciones y exclusiones, para evaluar su nivel de riesgo antes de aprobarlo como vendedor.

**Entregable:**
Endpoint `POST /api/suppliers/{supplierId}/screenings` que consulta OFAC, World Bank e ICIJ en paralelo, calcula un `RiskLevel`, crea un registro `ScreeningResult`, actualiza el `RiskLevel` del proveedor, y transiciona automaticamente el proveedor a `UNDER_REVIEW` si el resultado es `HIGH`. Retorna `201 Created` con el Id del nuevo resultado de screening.

**Dependencias:**
- `US-SUP-003`
- Modulo Scraping (`ScrapingOrchestrationService`) operacional

**Prioridad:** Critica | **Estimacion:** 5 SP | **Estado:** Implementado (v0.5.0)

#### Tareas

- `[BE-DOMAIN]` Metodo `Supplier.ApplyScreeningResult(RiskLevel)` — actualiza `RiskLevel`, establece `Status = UNDER_REVIEW` si `riskLevel >= HIGH`
- `[BE-DOMAIN]` Agregado `ScreeningResult` con fabrica `ScreeningResult.Create(supplierId, riskLevel, totalMatches)` — inmutable tras la creacion
- `[BE-DOMAIN]` `ScreeningResultNotFoundException` (extiende `EntityNotFoundException`)
- `[BE-APP]` `RunScreeningCommand` + `RunScreeningCommandHandler` — carga proveedor, llama `ScrapingOrchestrationService.SearchAllAsync(supplier.Name)`, calcula `RiskLevel` desde el score maximo (≥ 0.85 → HIGH, ≥ 0.60 → MEDIUM, cualquier hit → LOW, 0 → NONE), crea `ScreeningResult`, llama `ApplyScreeningResult`, persiste ambos, hace commit, retorna el nuevo Id
- `[BE-INFRA]` `IScreeningResultRepository` + `ScreeningResultRepository`
- `[BE-INTERFACES]` `SuppliersController.RunScreening` — despacha `RunScreeningCommand`; retorna `201 Created` con header `Location` apuntando a `GET /api/screenings/{screeningId}`
- `[BE-TEST]` Unit test: el screening crea el resultado y actualiza el riesgo del proveedor; riesgo HIGH establece status UNDER_REVIEW; proveedor no encontrado retorna 404

#### Criterios de Aceptacion

**Escenario 1: Screening exitoso — sin coincidencias**
- Given que estoy autenticado y el proveedor existe
- And no se encuentran coincidencias en OFAC, World Bank ni ICIJ
- When llamo `POST /api/suppliers/{supplierId}/screenings`
- Then recibo HTTP 201 Created
- And el cuerpo de la respuesta contiene el Id del nuevo resultado de screening
- And el `riskLevel` del proveedor se actualiza a `NONE`
- And el `status` del proveedor permanece sin cambios

**Escenario 2: Screening con coincidencias de riesgo HIGH**
- Given que el nombre del proveedor coincide con una entrada con score ≥ 0.85 en alguna lista
- When llamo `POST /api/suppliers/{supplierId}/screenings`
- Then recibo HTTP 201 Created
- And el `ScreeningResult` tiene `riskLevel = HIGH`
- And el `status` del proveedor cambia automaticamente a `UNDER_REVIEW`

**Escenario 3: Umbrales de scoring**
- Given que los resultados de busqueda contienen coincidencias con los siguientes scores maximos:
  - Score ≥ 0.85 → `riskLevel = HIGH`
  - Score ≥ 0.60 → `riskLevel = MEDIUM`
  - Cualquier coincidencia → `riskLevel = LOW`
  - Sin coincidencias → `riskLevel = NONE`
- When el screening se completa
- Then el `ScreeningResult.riskLevel` refleja la clasificacion correcta

**Escenario 4: Proveedor no encontrado**
- Given que el supplierId no existe
- When llamo `POST /api/suppliers/{supplierId}/screenings`
- Then recibo HTTP 404 Not Found

**Escenario 5: Fuente externa no disponible**
- Given que una de las fuentes externas (OFAC, World Bank, ICIJ) no es accesible
- When el screening se ejecuta
- Then el orquestador retorna `SearchResult.Empty` para la fuente que fallo (tolerante a fallos)
- And el screening se completa usando los resultados de las fuentes disponibles restantes

---

### US-SUP-007: Listar resultados de screening de un proveedor

**Titulo:** Listado paginado del historial de screenings

**Descripcion:**
Como oficial de compliance o administrador, quiero ver el historial completo de screenings de un proveedor, ordenado del mas reciente al mas antiguo, para hacer seguimiento de como ha evolucionado su perfil de riesgo.

**Entregable:**
Endpoint `GET /api/screenings?supplierId={supplierId}` que retorna una lista paginada y ordenada de todos los resultados de screening para el proveedor indicado.

**Dependencias:**
- `US-SUP-006`

**Prioridad:** Alta | **Estimacion:** 2 SP | **Estado:** Implementado (v0.5.0)

#### Tareas

- `[BE-APP]` `GetScreeningResultsBySupplierQuery` (supplierId, page, size) + `GetScreeningResultsBySupplierQueryHandler` — consulta via `QueryBySupplierId`, ordena por `ScreenedAt` descendente
- `[BE-INTERFACES]` `ScreeningsController.GetBySupplierId` — requiere parametro `supplierId`; retorna `PageResponse<ScreeningResultResponse>`
- `[BE-TEST]` Unit test: resultados paginados para proveedor valido, lista vacia cuando no se han ejecutado screenings

#### Criterios de Aceptacion

**Escenario 1: Listado exitoso**
- Given que estoy autenticado y se ha ejecutado al menos un screening para el proveedor
- When llamo `GET /api/screenings?supplierId={supplierId}&page=0&size=10`
- Then recibo HTTP 200 con una lista paginada ordenada por `screenedAt` descendente
- And cada entrada incluye `{ id, supplierId, screenedAt, riskLevel, totalMatches, createdAt }`

**Escenario 2: Sin screenings aun**
- Given que el proveedor existe pero no tiene historial de screenings
- When llamo `GET /api/screenings?supplierId={supplierId}`
- Then recibo HTTP 200 con `{ content: [], page: { totalElements: 0 } }`

---

### US-SUP-008: Obtener resultado de screening por ID

**Titulo:** Consultar un resultado de screening especifico

**Descripcion:**
Como oficial de compliance, quiero recuperar los detalles de un resultado de screening especifico, para revisar el nivel de riesgo exacto y el conteo de coincidencias de una ejecucion en particular.

**Entregable:**
Endpoint `GET /api/screenings/{screeningId}` que retorna el resultado de screening completo.

**Dependencias:**
- `US-SUP-007`

**Prioridad:** Media | **Estimacion:** 1 SP | **Estado:** Implementado (v0.5.0)

#### Tareas

- `[BE-APP]` `GetScreeningResultByIdQuery` + `GetScreeningResultByIdQueryHandler` — lanza `ScreeningResultNotFoundException` si no se encuentra
- `[BE-INTERFACES]` `ScreeningsController.GetById` — mapea 404 via `GlobalExceptionHandler`
- `[BE-TEST]` Unit test: resultado encontrado, resultado no encontrado retorna 404

#### Criterios de Aceptacion

**Escenario 1: Resultado encontrado**
- Given que estoy autenticado y el screeningId existe
- When llamo `GET /api/screenings/{screeningId}`
- Then recibo HTTP 200 con el resultado completo `{ id, supplierId, screenedAt, riskLevel, totalMatches, createdAt }`

**Escenario 2: Resultado no encontrado**
- Given que el screeningId no existe
- When llamo `GET /api/screenings/{screeningId}`
- Then recibo HTTP 404 Not Found con un mensaje de error descriptivo

---

## Epica: Flujo de Trabajo de Proveedores

---

### US-SUP-009: Aprobar un proveedor

**Titulo:** Marcar un proveedor como aprobado

**Descripcion:**
Como administrador, quiero aprobar un proveedor que ha sido revisado, para que pueda ser marcado como vendedor de confianza y excluido de las revisiones pendientes.

**Entregable:**
Endpoint `PATCH /api/suppliers/{supplierId}/approve` que transiciona el `Status` del proveedor a `APPROVED`.

**Dependencias:**
- `US-SUP-006`

**Prioridad:** Alta | **Estimacion:** 1 SP | **Estado:** Pendiente

#### Tareas

- `[BE-DOMAIN]` Metodo `Supplier.Approve()` — establece `Status = APPROVED`; guard `EnsureNotDeleted()`
- `[BE-APP]` `ApproveSupplierCommand` + `ApproveSupplierCommandHandler`
- `[BE-INTERFACES]` `SuppliersController.Approve` — retorna 204 No Content
- `[BE-TEST]` Unit test: aprobacion exitosa, proveedor eliminado retorna 404

#### Criterios de Aceptacion

**Escenario 1: Aprobacion exitosa**
- Given que soy ADMIN autenticado y el proveedor tiene status `PENDING` o `UNDER_REVIEW`
- When llamo `PATCH /api/suppliers/{supplierId}/approve`
- Then recibo HTTP 204 No Content
- And el `status` del proveedor es `APPROVED`

**Escenario 2: Proveedor no encontrado**
- Given que el supplierId no existe
- When llamo `PATCH /api/suppliers/{supplierId}/approve`
- Then recibo HTTP 404 Not Found

---

### US-SUP-010: Rechazar un proveedor

**Titulo:** Marcar un proveedor como rechazado

**Descripcion:**
Como administrador, quiero rechazar un proveedor que no paso la revision de compliance, para que sea excluido de las operaciones y quede claramente marcado como no conforme.

**Entregable:**
Endpoint `PATCH /api/suppliers/{supplierId}/reject` que transiciona el `Status` del proveedor a `REJECTED`.

**Dependencias:**
- `US-SUP-009`

**Prioridad:** Alta | **Estimacion:** 1 SP | **Estado:** Pendiente

#### Tareas

- `[BE-DOMAIN]` Metodo `Supplier.Reject()` — establece `Status = REJECTED`; guard `EnsureNotDeleted()`
- `[BE-APP]` `RejectSupplierCommand` + `RejectSupplierCommandHandler`
- `[BE-INTERFACES]` `SuppliersController.Reject` — retorna 204 No Content
- `[BE-TEST]` Unit test: rechazo exitoso, ya rechazado retorna 422

#### Criterios de Aceptacion

**Escenario 1: Rechazo exitoso**
- Given que soy ADMIN autenticado y el proveedor existe y no esta ya rechazado
- When llamo `PATCH /api/suppliers/{supplierId}/reject`
- Then recibo HTTP 204 No Content
- And el `status` del proveedor es `REJECTED`

**Escenario 2: Proveedor ya rechazado**
- Given que el proveedor ya tiene `status = REJECTED`
- When llamo `PATCH /api/suppliers/{supplierId}/reject`
- Then recibo HTTP 422 Unprocessable Entity

---

### US-SUP-011: Marcar un proveedor en revision

**Titulo:** Marcar manualmente un proveedor para revision

**Descripcion:**
Como oficial de compliance, quiero poner manualmente un proveedor en revision, para poder marcarlo para investigacion adicional antes de aprobarlo o rechazarlo — independientemente de los disparadores automaticos de screening.

**Entregable:**
Endpoint `PATCH /api/suppliers/{supplierId}/under-review` que transiciona el `Status` del proveedor a `UNDER_REVIEW`.

**Dependencias:**
- `US-SUP-009`

**Prioridad:** Media | **Estimacion:** 1 SP | **Estado:** Pendiente

#### Tareas

- `[BE-DOMAIN]` Metodo `Supplier.MarkUnderReview()` — establece `Status = UNDER_REVIEW`; guard `EnsureNotDeleted()`
- `[BE-APP]` `MarkUnderReviewCommand` + `MarkUnderReviewCommandHandler`
- `[BE-INTERFACES]` `SuppliersController.MarkUnderReview` — retorna 204 No Content
- `[BE-TEST]` Unit test: transicion exitosa, proveedor eliminado retorna 404

#### Criterios de Aceptacion

**Escenario 1: Transicion exitosa**
- Given que estoy autenticado y el proveedor existe y no esta eliminado
- When llamo `PATCH /api/suppliers/{supplierId}/under-review`
- Then recibo HTTP 204 No Content
- And el `status` del proveedor es `UNDER_REVIEW`

**Escenario 2: Proveedor no encontrado**
- Given que el supplierId no existe
- When llamo `PATCH /api/suppliers/{supplierId}/under-review`
- Then recibo HTTP 404 Not Found

---

## Notas de Implementacion

| Aspecto | Implementacion |
|---------|---------------|
| Soft delete | `Supplier.Delete()` establece `Status = REJECTED`; sin eliminacion fisica; filtrado de todos los listados |
| Scoring de riesgo | ≥ 0.85 → HIGH, ≥ 0.60 → MEDIUM, cualquier hit → LOW, 0 hits → NONE; score proviene de OFAC/World Bank; entradas ICIJ cuentan como LOW |
| Auto-revision | `Supplier.ApplyScreeningResult(riskLevel)` automaticamente establece `Status = UNDER_REVIEW` cuando `riskLevel = HIGH` |
| Cascade delete | FK `screening_results.supplier_id` tiene `ON DELETE CASCADE` — eliminar fisicamente un proveedor en la DB tambien elimina su historial de screenings |
| Inmutabilidad de TaxId | `TaxId` no puede modificarse despues de la creacion; `UpdateSupplierCommand` lo omite intencionalmente |
| Tolerancia a fallos en scraping | Cada `IScrapingSource` retorna `SearchResult.Empty` ante cualquier excepcion HTTP o de parsing; el screening siempre completa |
| Caching | `ScrapingOrchestrationService` cachea resultados por `(fuente, termino)` por 10 minutos; screenings repetidos del mismo nombre dentro de esa ventana reutilizan datos en cache |
| Estado de implementacion | TS-SUP-000 a US-SUP-008 implementados (v0.4.0–v0.5.0); US-SUP-009–011 (transiciones de flujo) pendientes |
