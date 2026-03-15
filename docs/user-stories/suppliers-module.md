# User Stories — Suppliers Module

> **Format:** Title / Description / Deliverable / Dependencies / Acceptance Criteria (BDD Given/When/Then).
> **Task tags:** `[BE-DOMAIN]` `[BE-APP]` `[BE-INFRA]` `[BE-INTERFACES]` `[BE-DB]` `[BE-TEST]` `[DOCS]`

---

## Technical Story: Suppliers Module Bootstrap

---

### TS-SUP-000: Suppliers Module Initial Setup

**Title:** Database scripts, EF Core configuration, and module registration

**Description:**
As a developer, I need to set up the foundational infrastructure of the Suppliers module — database tables, EF Core entity configurations, repository implementations, and DI registration — so that all Suppliers user stories have a stable base to build upon.

**Deliverable:**
SQL migration scripts V005–V006, EF Core configurations for `Supplier` and `ScreeningResult`, `SupplierRepository`, `ScreeningResultRepository`, and `SuppliersModuleExtensions` registered in `Program.cs`.

**Dependencies:**
- `TS-IAM-000`: IAM module initial setup (shared kernel, `AppDbContext`, `BaseRepository`)

**Priority:** Critical | **Estimate:** 3 SP | **Status:** Updated (v0.4.1)

#### Tasks

- `[BE-DB]` Script `V005__create_suppliers_table.sql` — columns:
  - `id` NVARCHAR(36) PK
  - `legal_name` NVARCHAR(200) NOT NULL *(razón social)*
  - `commercial_name` NVARCHAR(200) NOT NULL *(nombre comercial)*
  - `tax_id` CHAR(11) NOT NULL UNIQUE *(identificación tributaria — exactamente 11 dígitos numéricos)*
  - `contact_phone` NVARCHAR(50) NULL
  - `contact_email` NVARCHAR(255) NULL
  - `website` NVARCHAR(500) NULL *(sitio web)*
  - `address` NVARCHAR(500) NULL *(dirección física)*
  - `country` NVARCHAR(100) NOT NULL
  - `annual_billing_usd` DECIMAL(18, 2) NULL *(facturación anual en dólares)*
  - `risk_level` NVARCHAR(10) NOT NULL DEFAULT 'NONE' CHECK (NONE/LOW/MEDIUM/HIGH)
  - `status` NVARCHAR(20) NOT NULL DEFAULT 'PENDING' CHECK (PENDING/APPROVED/REJECTED/UNDER_REVIEW)
  - `is_deleted` BIT NOT NULL DEFAULT 0 *(soft-delete flag — independiente del status de negocio)*
  - `notes` NVARCHAR(MAX) NULL
  - `created_at` DATETIME2, `updated_at` DATETIME2, `created_by` NVARCHAR(255), `updated_by` NVARCHAR(255)
  - Indexes: `IX_suppliers_risk_level`, `IX_suppliers_status`, `IX_suppliers_country`, `IX_suppliers_is_deleted`
  - CHECK constraint on `tax_id`: `LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'`

- `[BE-DB]` Script `V006__create_screening_results_table.sql` — columns:
  - `id` NVARCHAR(36) PK
  - `supplier_id` NVARCHAR(36) NOT NULL FK → `suppliers(id)` ON DELETE CASCADE
  - `sources_queried` NVARCHAR(200) NOT NULL *(CSV de fuentes consultadas, e.g. "OFAC,WORLD_BANK,ICIJ")*
  - `screened_at` DATETIME2 NOT NULL DEFAULT GETUTCDATE()
  - `risk_level` NVARCHAR(10) NOT NULL DEFAULT 'NONE' CHECK (NONE/LOW/MEDIUM/HIGH)
  - `total_matches` INT NOT NULL DEFAULT 0
  - `entries_json` NVARCHAR(MAX) NULL *(serialización JSON de los RiskEntry coincidentes)*
  - `created_at` DATETIME2 NOT NULL DEFAULT GETUTCDATE()
  - Indexes: `IX_screening_results_supplier_id`, `IX_screening_results_screened_at DESC`, `IX_screening_results_risk_level`
  - **Sin `updated_at`** — los resultados de screening son inmutables tras su creación

- `[BE-INFRA]` EF Core `SupplierConfiguration` — mapea todos los campos incluyendo `LegalName`, `CommercialName`, `Website`, `AnnualBillingUsd`, `IsDeleted`
- `[BE-INFRA]` EF Core `ScreeningResultConfiguration` — mapea `SourcesQueried`, `EntriesJson`; sin `updated_at`
- `[BE-INFRA]` `SupplierRepository` y `ScreeningResultRepository` implementando `BaseRepository`
- `[BE-INFRA]` `SuppliersModuleExtensions.AddSuppliersModule()` — registra ambos repositorios como scoped

#### Acceptance Criteria

- Given the application starts against a fresh database
- When the DbUp migration sequence completes
- Then the `suppliers` table exists with `legal_name`, `commercial_name`, `tax_id` CHAR(11), `website`, `annual_billing_usd`, `is_deleted`, y los constraints correspondientes
- And the `screening_results` table exists con `sources_queried` y `entries_json`, sin `updated_at`, con FK a `suppliers` CASCADE DELETE
- And subsequent startups are idempotent (DbUp does not re-run applied scripts)

---

## Epic: Supplier Management

---

### US-SUP-001: Register a new supplier

**Title:** Create a supplier record

**Description:**
As a compliance officer or administrator, I want to register a new supplier in the platform, so that I can track its risk profile and run screening checks against international risk lists.

**Deliverable:**
Endpoint `POST /api/suppliers` que valida la solicitud, verifica duplicado de `TaxId`, crea el `Supplier` aggregate con `status = PENDING`, `riskLevel = NONE`, `isDeleted = false`, y retorna `201 Created` con el nuevo Id.

**Dependencies:**
- `TS-SUP-000`: module bootstrap
- `US-IAM-001`: JWT authentication

**Priority:** High | **Estimate:** 3 SP | **Status:** Updated (v0.4.1)

#### Tasks

- `[BE-DOMAIN]` `Supplier` aggregate con factory method `Create(legalName, commercialName, taxId, country, contactPhone?, contactEmail?, website?, address?, annualBillingUsd?, notes?)` — inicializa `Status = PENDING`, `RiskLevel = NONE`, `IsDeleted = false`
- `[BE-DOMAIN]` Value rules en `Create`:
  - `LegalName` ≤ 200, required
  - `CommercialName` ≤ 200, required
  - `TaxId` exactamente 11 dígitos numéricos (`^\d{11}$`)
  - `ContactEmail` formato válido si se provee
  - `Website` formato URL válido si se provee
  - `AnnualBillingUsd` ≥ 0 si se provee
- `[BE-DOMAIN]` `SupplierTaxIdAlreadyExistsException` (extends `BusinessRuleViolationException`)
- `[BE-APP]` `CreateSupplierCommand` + `CreateSupplierCommandHandler` — verifica `ExistsByTaxIdAsync`, llama `Supplier.Create`, persiste, retorna nuevo Id
- `[BE-APP]` `CreateSupplierCommandValidator` (FluentValidation) con todas las reglas de campo
- `[BE-INTERFACES]` `SuppliersController.Create` — mapea `CreateSupplierRequest` → `CreateSupplierCommand`; retorna `201 Created` con `Location` header
- `[BE-TEST]` Unit test: creación exitosa, TaxId duplicado retorna 409, TaxId con formato inválido retorna 400, campos requeridos faltantes retornan 400

#### Acceptance Criteria

**Scenario 1: Successful creation**
- Given I am an authenticated user
- And I send `POST /api/suppliers` with valid `legalName`, `commercialName`, `taxId` (11 digits), and `country`
- When the request is processed
- Then I receive HTTP 201 Created
- And the response body contains the new supplier Id
- And the `Location` header points to `GET /api/suppliers/{id}`
- And the supplier has `status = PENDING`, `riskLevel = NONE`, `isDeleted = false`

**Scenario 2: Duplicate TaxId**
- Given a supplier with the same `taxId` already exists
- When I send `POST /api/suppliers` with that `taxId`
- Then I receive HTTP 409 Conflict with a descriptive message

**Scenario 3: Invalid TaxId format**
- Given I send `taxId` with fewer or more than 11 digits, or containing non-numeric characters
- When the request reaches the validation pipeline
- Then I receive HTTP 400 Bad Request with a field-level error for `taxId`

**Scenario 4: Missing required fields**
- Given I send the request without `legalName`, `commercialName`, or with an invalid `contactEmail` format
- When the request reaches the validation pipeline
- Then I receive HTTP 400 Bad Request with a list of field-level validation errors

**Scenario 5: Unauthenticated**
- Given I do not include a JWT Bearer token
- When I send `POST /api/suppliers`
- Then I receive HTTP 401 Unauthorized

---

### US-SUP-002: List all suppliers (paginated)

**Title:** Paginated listing of suppliers

**Description:**
As a compliance officer or administrator, I want to see the paginated list of all registered suppliers ordered by last edit date, to monitor their statuses and risk levels from the main dashboard.

**Deliverable:**
Endpoint `GET /api/suppliers` retornando lista paginada y ordenada de proveedores activos (`isDeleted = false`), con soporte para `page`, `size`, `sortBy`, `sortDirection`.

**Dependencies:**
- `US-SUP-001`

**Priority:** High | **Estimate:** 2 SP | **Status:** Updated (v0.4.1)

#### Tasks

- `[BE-APP]` `GetAllSuppliersQuery` (page, size, sortBy, sortDirection) + `GetAllSuppliersQueryHandler` — filtra `IsDeleted = false`, ordena por `UpdatedAt` desc por defecto
- `[BE-INFRA]` `ISupplierRepository.Query()` retornando `IQueryable<Supplier>` para composición LINQ
- `[BE-INTERFACES]` `SuppliersController.GetAll` — mapea query params a `GetAllSuppliersQuery`; retorna `PageResponse<SupplierResponse>`
- `[BE-TEST]` Unit test: listado paginado, sorting, resultado vacío

#### Acceptance Criteria

**Scenario 1: Successful listing**
- Given I am authenticated and suppliers exist
- When I call `GET /api/suppliers?page=0&size=10`
- Then I receive HTTP 200 with `{ content: [...], page: { number, size, totalElements, totalPages } }`
- And each entry includes `{ id, legalName, commercialName, taxId, country, contactEmail, contactPhone, website, annualBillingUsd, riskLevel, status, updatedAt, createdAt }`
- And soft-deleted suppliers (`isDeleted = true`) are NOT included
- And results are ordered by `updatedAt` descending by default

**Scenario 2: Rejected suppliers remain visible**
- Given a supplier has `status = REJECTED` but `isDeleted = false`
- When I call `GET /api/suppliers`
- Then the rejected supplier IS included in the listing (compliance rejected ≠ deleted)

**Scenario 3: Sorting**
- Given I call `GET /api/suppliers?sortBy=legalName&sortDirection=asc`
- When the request is processed
- Then I receive suppliers sorted by `legalName` ascending

**Scenario 4: Empty result**
- Given no suppliers have been registered (or all are soft-deleted)
- When I call `GET /api/suppliers`
- Then I receive HTTP 200 with `{ content: [], page: { totalElements: 0 } }`

---

### US-SUP-003: Get supplier by ID

**Title:** Query a supplier's full profile

**Description:**
As a compliance officer or administrator, I want to see the full profile of a specific supplier, to review its details, current risk level, and status before taking action.

**Deliverable:**
Endpoint `GET /api/suppliers/{supplierId}` retornando el perfil completo del proveedor.

**Dependencies:**
- `US-SUP-002`

**Priority:** High | **Estimate:** 1 SP | **Status:** Updated (v0.4.1)

#### Tasks

- `[BE-APP]` `GetSupplierByIdQuery` + `GetSupplierByIdQueryHandler` — lanza `SupplierNotFoundException` si no existe o si `IsDeleted = true`
- `[BE-INTERFACES]` `SuppliersController.GetById` — mapea 404 via `GlobalExceptionHandler`
- `[BE-TEST]` Unit test: supplier encontrado, supplier no encontrado retorna 404, supplier eliminado retorna 404

#### Acceptance Criteria

**Scenario 1: Supplier found**
- Given I am authenticated and the supplierId exists and `isDeleted = false`
- When I call `GET /api/suppliers/{supplierId}`
- Then I receive HTTP 200 with the full supplier profile including `legalName`, `commercialName`, `taxId`, `website`, `annualBillingUsd`, and all other fields

**Scenario 2: Supplier not found or soft-deleted**
- Given the supplierId does not exist or `isDeleted = true`
- When I call `GET /api/suppliers/{supplierId}`
- Then I receive HTTP 404 Not Found with a descriptive error message

---

### US-SUP-004: Update supplier information

**Title:** Edit supplier details

**Description:**
As a compliance officer or administrator, I want to update a supplier's information, so that the record stays accurate when the supplier's contact details or commercial data change.

**Deliverable:**
Endpoint `PUT /api/suppliers/{supplierId}` que actualiza todos los campos mutables. `TaxId` es intencionalmente excluido — no puede cambiarse tras la creación.

**Dependencies:**
- `US-SUP-003`

**Priority:** High | **Estimate:** 2 SP | **Status:** Updated (v0.4.1)

#### Tasks

- `[BE-DOMAIN]` Método `Supplier.Update(legalName, commercialName, country, contactPhone?, contactEmail?, website?, address?, annualBillingUsd?, notes?)` — aplica nuevos valores, guard `EnsureNotDeleted()` verifica `IsDeleted = false`
- `[BE-APP]` `UpdateSupplierCommand` + `UpdateSupplierCommandHandler` — carga supplier, llama `Update`, commits
- `[BE-APP]` `UpdateSupplierCommandValidator` (FluentValidation — mismas reglas que Create, más Id required)
- `[BE-INTERFACES]` `SuppliersController.Update` — retorna 204 No Content
- `[BE-TEST]` Unit test: actualización exitosa, supplier no encontrado retorna 404, supplier eliminado retorna 422

#### Acceptance Criteria

**Scenario 1: Successful update**
- Given I am authenticated and the supplier exists and `isDeleted = false`
- When I call `PUT /api/suppliers/{supplierId}` with valid updated fields including `commercialName`, `website`, `annualBillingUsd`
- Then I receive HTTP 204 No Content
- And subsequent `GET /api/suppliers/{supplierId}` reflects the new values

**Scenario 2: Supplier not found**
- Given the supplierId does not exist
- When I call `PUT /api/suppliers/{supplierId}`
- Then I receive HTTP 404 Not Found

**Scenario 3: Supplier is soft-deleted**
- Given the supplier has `isDeleted = true`
- When I call `PUT /api/suppliers/{supplierId}`
- Then I receive HTTP 422 Unprocessable Entity with a business rule violation message

**Scenario 4: Validation errors**
- Given I send the request with an invalid `contactEmail` format or a negative `annualBillingUsd`
- When the request reaches the validation pipeline
- Then I receive HTTP 400 Bad Request

---

### US-SUP-005: Delete supplier (soft delete)

**Title:** Logical deletion of a supplier

**Description:**
As an administrator, I want to logically delete a supplier record, so that decommissioned suppliers no longer appear in active listings without losing audit history.

**Deliverable:**
Endpoint `DELETE /api/suppliers/{supplierId}` que establece `IsDeleted = true`. **No modifica `Status`** — el estado de negocio (PENDING/APPROVED/REJECTED/UNDER_REVIEW) se preserva para auditoría. El registro permanece en la base de datos y queda excluido de todos los listados.

**Dependencies:**
- `US-SUP-003`

**Priority:** High | **Estimate:** 1 SP | **Status:** Updated (v0.4.1)

#### Tasks

- `[BE-DOMAIN]` Método `Supplier.Delete()` — establece `IsDeleted = true`; guard `EnsureNotDeleted()` lanza `SupplierAlreadyDeletedException` si `IsDeleted` ya es `true`
- `[BE-APP]` `DeleteSupplierCommand` + `DeleteSupplierCommandHandler`
- `[BE-INTERFACES]` `SuppliersController.Delete` — retorna 204 No Content
- `[BE-TEST]` Unit test: eliminación exitosa, ya eliminado retorna 404, supplier no encontrado retorna 404

> **Nota de diseño:** El soft-delete (`IsDeleted = true`) es independiente del rechazo de compliance (`Status = REJECTED`). Un proveedor rechazado sigue visible en listados hasta que sea explícitamente eliminado. Ambas operaciones pueden coexistir.

#### Acceptance Criteria

**Scenario 1: Successful deletion**
- Given I am an authenticated ADMIN and the supplier exists and `isDeleted = false`
- When I call `DELETE /api/suppliers/{supplierId}`
- Then I receive HTTP 204 No Content
- And the supplier no longer appears in `GET /api/suppliers`
- And `isDeleted = true` in the database; the record is NOT physically removed
- And the supplier's `status` is **unchanged** (preserved for audit)

**Scenario 2: Already deleted**
- Given the supplier already has `isDeleted = true`
- When I call `DELETE /api/suppliers/{supplierId}`
- Then I receive HTTP 404 Not Found

**Scenario 3: Supplier not found**
- Given the supplierId does not exist
- When I call `DELETE /api/suppliers/{supplierId}`
- Then I receive HTTP 404 Not Found

---

## Epic: Supplier Screening

---

### US-SUP-006: Run screening for a supplier

**Title:** Trigger a live risk screening run

**Description:**
As a compliance officer, I want to trigger a risk screening for a supplier against international sanctions and debarment lists — pudiendo seleccionar una o más fuentes — para evaluar su nivel de riesgo antes de aprobarlo como proveedor.

**Deliverable:**
Endpoint `POST /api/suppliers/{supplierId}/screenings` que acepta un listado opcional de fuentes a consultar (OFAC, WORLD_BANK, ICIJ; por defecto todas), ejecuta las consultas en paralelo, computa un `RiskLevel` con lógica por fuente, crea un `ScreeningResult` almacenando las entradas coincidentes serializadas en JSON, actualiza el `RiskLevel` del proveedor, y auto-transiciona el proveedor a `UNDER_REVIEW` si el resultado es `HIGH`. Retorna `201 Created` con el Id del nuevo screening result.

**Dependencies:**
- `US-SUP-003`
- Scraping module (`ScrapingOrchestrationService`) operacional

**Priority:** Critical | **Estimate:** 5 SP | **Status:** Updated (v0.5.1)

#### Tasks

- `[BE-DOMAIN]` Método `Supplier.ApplyScreeningResult(RiskLevel)` — actualiza `RiskLevel`, establece `Status = UNDER_REVIEW` si `riskLevel = HIGH`
- `[BE-DOMAIN]` `ScreeningResult` aggregate con factory `ScreeningResult.Create(supplierId, sourcesQueried, riskLevel, totalMatches, entries)` — inmutable tras creación; `entries` se serializa como JSON en `EntriesJson`
- `[BE-DOMAIN]` `ScreeningResultNotFoundException` (extends `EntityNotFoundException`)
- `[BE-APP]` `RunScreeningCommand` — campos: `SupplierId`, `Sources` (opcional `IReadOnlyList<string>?`; valores válidos: `"ofac"`, `"worldbank"`, `"icij"`; si null → todas)
- `[BE-APP]` `RunScreeningCommandHandler`:
  1. Carga supplier; lanza `SupplierNotFoundException` si no existe o `IsDeleted = true`
  2. Determina fuentes a consultar (param `Sources` o todas por defecto)
  3. Llama `ScrapingOrchestrationService` para las fuentes seleccionadas en paralelo usando `LegalName` como término
  4. Computa `RiskLevel` con **lógica por fuente**:
     - **OFAC**: score ≥ 0.85 → HIGH | score ≥ 0.60 → MEDIUM | cualquier match → LOW
     - **World Bank**: cualquier match → HIGH *(empresa formalmente inhabilitada)*
     - **ICIJ**: cualquier match → LOW *(leak periodístico, no sanción oficial)*
     - `RiskLevel` final = máximo entre todas las fuentes
  5. Crea `ScreeningResult.Create(supplierId, sourcesQueried, riskLevel, totalMatches, allEntries)`
  6. Llama `supplier.ApplyScreeningResult(riskLevel)`
  7. Persiste ambos, commits, retorna nuevo result Id
- `[BE-INFRA]` `IScreeningResultRepository` + `ScreeningResultRepository`
- `[BE-INTERFACES]` `SuppliersController.RunScreening` — acepta body opcional `{ "sources": ["ofac", "worldbank"] }`; retorna `201 Created` con `Location` header
- `[BE-TEST]` Unit test: screening crea result y actualiza supplier; World Bank match → HIGH; HIGH → UNDER_REVIEW; entries guardadas; supplier no encontrado retorna 404

#### Acceptance Criteria

**Scenario 1: Successful screening — no matches**
- Given I am authenticated and the supplier exists
- And no matches are found across the queried sources
- When I call `POST /api/suppliers/{supplierId}/screenings`
- Then I receive HTTP 201 Created
- And the ScreeningResult has `riskLevel = NONE`, `totalMatches = 0`, `entriesJson = "[]"`
- And the supplier's `riskLevel` is updated to `NONE`

**Scenario 2: Source selection**
- Given I send `{ "sources": ["ofac", "icij"] }` in the request body
- When the screening runs
- Then only OFAC and ICIJ are queried
- And `sourcesQueried = "OFAC,ICIJ"` in the ScreeningResult

**Scenario 3: Default — all sources**
- Given I send the request with no body (or `sources: null`)
- When the screening runs
- Then OFAC, World Bank, and ICIJ are all queried

**Scenario 4: World Bank match → HIGH risk**
- Given the supplier name appears in the World Bank debarred firms list
- When the screening completes
- Then `ScreeningResult.riskLevel = HIGH`
- And the supplier's `status` is automatically changed to `UNDER_REVIEW`

**Scenario 5: OFAC scoring thresholds**
- Given OFAC returns entries with the following max score:
  - Score ≥ 0.85 → `riskLevel = HIGH`
  - Score ≥ 0.60 → `riskLevel = MEDIUM`
  - Score < 0.60 → `riskLevel = LOW`
- When the screening completes
- Then the `ScreeningResult.riskLevel` reflects the correct classification

**Scenario 6: Entries persisted in result**
- Given any source returns matches
- When the screening completes
- Then `ScreeningResult.entriesJson` contains the serialized list of all matched `RiskEntry` objects
- And subsequent `GET /api/screenings/{screeningId}` returns those entries deserialized

**Scenario 7: Supplier not found**
- Given the supplierId does not exist or `isDeleted = true`
- When I call `POST /api/suppliers/{supplierId}/screenings`
- Then I receive HTTP 404 Not Found

**Scenario 8: External source unavailable**
- Given one of the selected sources is unreachable
- When the screening runs
- Then the orchestrator returns `SearchResult.Empty` for the failed source
- And the screening completes using results from the remaining available sources

---

### US-SUP-007: List screening results for a supplier

**Title:** Paginated listing of screening history

**Description:**
As a compliance officer or administrator, I want to see the full screening history for a supplier, ordered from most recent to oldest.

**Deliverable:**
Endpoint `GET /api/screenings?supplierId={supplierId}` retornando lista paginada de todos los screening results. Las **entradas coincidentes** (`entries`) se omiten en el listado para respuestas ligeras — se obtienen via `GET /api/screenings/{id}`.

**Dependencies:**
- `US-SUP-006`

**Priority:** High | **Estimate:** 2 SP | **Status:** Updated (v0.5.1)

#### Tasks

- `[BE-APP]` `GetScreeningResultsBySupplierQuery` (supplierId, page, size) + handler — consulta via `QueryBySupplierId`, ordena por `ScreenedAt` desc
- `[BE-INTERFACES]` `ScreeningsController.GetBySupplierId` — requiere `supplierId` query param; retorna `PageResponse<ScreeningResultSummaryResponse>` *(sin campo `entries`)*
- `[BE-TEST]` Unit test: resultados paginados para proveedor válido, lista vacía cuando no hay screenings

#### Acceptance Criteria

**Scenario 1: Successful listing**
- Given I am authenticated and at least one screening has been run for the supplier
- When I call `GET /api/screenings?supplierId={supplierId}&page=0&size=10`
- Then I receive HTTP 200 with a paginated list ordered by `screenedAt` descending
- And each summary entry includes `{ id, supplierId, sourcesQueried, screenedAt, riskLevel, totalMatches, createdAt }` **without** `entries`

**Scenario 2: No screenings yet**
- Given the supplier exists but has no screening history
- When I call `GET /api/screenings?supplierId={supplierId}`
- Then I receive HTTP 200 with `{ content: [], page: { totalElements: 0 } }`

---

### US-SUP-008: Get screening result by ID

**Title:** Query a single screening result with full matched entries

**Description:**
As a compliance officer, I want to retrieve the details of a specific screening result including all matched entries, so that I can review the exact records that determined the risk level.

**Deliverable:**
Endpoint `GET /api/screenings/{screeningId}` retornando el screening result completo con la lista de entradas coincidentes deserializadas desde `entriesJson`.

**Dependencies:**
- `US-SUP-007`

**Priority:** Medium | **Estimate:** 1 SP | **Status:** Updated (v0.5.1)

#### Tasks

- `[BE-APP]` `GetScreeningResultByIdQuery` + handler — lanza `ScreeningResultNotFoundException` si no existe; deserializa `EntriesJson` → `List<RiskEntry>`
- `[BE-INTERFACES]` `ScreeningsController.GetById` — retorna `ScreeningResultDetailResponse` que incluye el campo `entries`
- `[BE-TEST]` Unit test: result encontrado con entries deserializadas, result no encontrado retorna 404

#### Acceptance Criteria

**Scenario 1: Result found with entries**
- Given I am authenticated and the screeningId exists
- When I call `GET /api/screenings/{screeningId}`
- Then I receive HTTP 200 with `{ id, supplierId, sourcesQueried, screenedAt, riskLevel, totalMatches, createdAt, entries: [...] }`
- And `entries` is the deserialized list of `RiskEntry` objects with their source-specific fields

**Scenario 2: Result not found**
- Given the screeningId does not exist
- When I call `GET /api/screenings/{screeningId}`
- Then I receive HTTP 404 Not Found with a descriptive error message

---

## Epic: Supplier Workflow

---

### US-SUP-009: Approve a supplier

**Title:** Mark a supplier as approved

**Description:**
As an administrator, I want to approve a supplier that has been reviewed, so that it can be marked as a trusted vendor.

**Deliverable:**
Endpoint `PATCH /api/suppliers/{supplierId}/approve` que transiciona `Status` a `APPROVED`.

**Dependencies:**
- `US-SUP-006`

**Priority:** High | **Estimate:** 1 SP | **Status:** Pending

#### Tasks

- `[BE-DOMAIN]` Método `Supplier.Approve()` — establece `Status = APPROVED`; guard `EnsureNotDeleted()` verifica `IsDeleted = false`
- `[BE-APP]` `ApproveSupplierCommand` + `ApproveSupplierCommandHandler`
- `[BE-INTERFACES]` `SuppliersController.Approve` — retorna 204 No Content
- `[BE-TEST]` Unit test: aprobación exitosa, supplier eliminado retorna 404

#### Acceptance Criteria

**Scenario 1: Successful approval**
- Given I am an authenticated ADMIN and the supplier exists and `isDeleted = false`
- When I call `PATCH /api/suppliers/{supplierId}/approve`
- Then I receive HTTP 204 No Content
- And the supplier's `status` is `APPROVED`

**Scenario 2: Supplier not found or deleted**
- Given the supplierId does not exist or `isDeleted = true`
- When I call `PATCH /api/suppliers/{supplierId}/approve`
- Then I receive HTTP 404 Not Found

---

### US-SUP-010: Reject a supplier

**Title:** Mark a supplier as rejected by compliance

**Description:**
As an administrator, I want to reject a supplier that did not pass the compliance review. **Este rechazo es una decisión de negocio** y es independiente de la eliminación lógica del registro. Un proveedor rechazado sigue visible en listados para auditoría.

**Deliverable:**
Endpoint `PATCH /api/suppliers/{supplierId}/reject` que transiciona `Status` a `REJECTED`.

**Dependencies:**
- `US-SUP-009`

**Priority:** High | **Estimate:** 1 SP | **Status:** Pending

#### Tasks

- `[BE-DOMAIN]` Método `Supplier.Reject()` — establece `Status = REJECTED`; guard `EnsureNotDeleted()` verifica `IsDeleted = false`; lanza `InvalidSupplierStateException` si ya está en `REJECTED`
- `[BE-APP]` `RejectSupplierCommand` + `RejectSupplierCommandHandler`
- `[BE-INTERFACES]` `SuppliersController.Reject` — retorna 204 No Content
- `[BE-TEST]` Unit test: rechazo exitoso, ya rechazado retorna 422, supplier eliminado retorna 404

#### Acceptance Criteria

**Scenario 1: Successful rejection**
- Given I am an authenticated ADMIN and the supplier exists, `isDeleted = false`, and `status != REJECTED`
- When I call `PATCH /api/suppliers/{supplierId}/reject`
- Then I receive HTTP 204 No Content
- And the supplier's `status` is `REJECTED`
- And the supplier **still appears** in `GET /api/suppliers` (visible for audit)

**Scenario 2: Already rejected**
- Given the supplier already has `status = REJECTED`
- When I call `PATCH /api/suppliers/{supplierId}/reject`
- Then I receive HTTP 422 Unprocessable Entity

**Scenario 3: Supplier deleted**
- Given `isDeleted = true`
- When I call `PATCH /api/suppliers/{supplierId}/reject`
- Then I receive HTTP 404 Not Found

---

### US-SUP-011: Mark a supplier as under review

**Title:** Manually flag a supplier for review

**Description:**
As a compliance officer, I want to manually place a supplier under review, so that I can flag it for additional investigation before approving or rejecting it — independently of automated screening triggers.

**Deliverable:**
Endpoint `PATCH /api/suppliers/{supplierId}/under-review` que transiciona `Status` a `UNDER_REVIEW`.

**Dependencies:**
- `US-SUP-009`

**Priority:** Medium | **Estimate:** 1 SP | **Status:** Pending

#### Tasks

- `[BE-DOMAIN]` Método `Supplier.MarkUnderReview()` — establece `Status = UNDER_REVIEW`; guard `EnsureNotDeleted()` verifica `IsDeleted = false`
- `[BE-APP]` `MarkUnderReviewCommand` + `MarkUnderReviewCommandHandler`
- `[BE-INTERFACES]` `SuppliersController.MarkUnderReview` — retorna 204 No Content
- `[BE-TEST]` Unit test: transición exitosa, supplier eliminado retorna 404

#### Acceptance Criteria

**Scenario 1: Successful transition**
- Given I am authenticated and the supplier exists and `isDeleted = false`
- When I call `PATCH /api/suppliers/{supplierId}/under-review`
- Then I receive HTTP 204 No Content
- And the supplier's `status` is `UNDER_REVIEW`

**Scenario 2: Supplier not found or deleted**
- Given the supplierId does not exist or `isDeleted = true`
- When I call `PATCH /api/suppliers/{supplierId}/under-review`
- Then I receive HTTP 404 Not Found

---

## Implementation Notes

| Aspect | Implementation |
|--------|---------------|
| Soft delete | `Supplier.Delete()` establece `IsDeleted = true`; **no modifica `Status`**; filtrado en listados con `WHERE is_deleted = 0`; `GetById` también retorna 404 para eliminados |
| Rechazo de negocio | `Supplier.Reject()` establece `Status = REJECTED`; el registro **sigue visible** en listados (`isDeleted = false`); es una decisión de compliance, no eliminación |
| TaxId | Exactamente 11 dígitos numéricos; validado con FluentValidation (`^\d{11}$`) y CHECK constraint en DB (`CHAR(11)`); inmutable tras creación |
| Campos requeridos del proveedor | `legalName` (razón social), `commercialName` (nombre comercial), `taxId`, `country` — required; `website`, `annualBillingUsd` DECIMAL(18,2), `contactPhone`, `contactEmail`, `address` — opcionales |
| Risk scoring por fuente | OFAC: score ≥ 0.85 → HIGH, ≥ 0.60 → MEDIUM, any match → LOW; World Bank: any match → HIGH (inhabilitación oficial); ICIJ: any match → LOW (leak, no sanción) |
| Risk scoring final | `RiskLevel` = máximo entre todas las fuentes consultadas |
| Auto-review | `Supplier.ApplyScreeningResult(HIGH)` automáticamente establece `Status = UNDER_REVIEW` |
| Entries en ScreeningResult | Serializadas como JSON en `entries_json` (NVARCHAR MAX); `GET /api/screenings/{id}` las deserializa y retorna; el listado (`GET /api/screenings?supplierId=`) las omite |
| ScreeningResult inmutabilidad | Sin `updated_at` en DB ni en EF Core config; nunca se actualiza un resultado existente |
| Selección de fuentes | `RunScreeningCommand.Sources` (opcional); si null = todas las fuentes; validado contra ["ofac", "worldbank", "icij"] |
| Cascade delete | `screening_results.supplier_id` FK con `ON DELETE CASCADE` — eliminar físicamente un supplier a nivel DB también elimina su historial |
| Caching | `ScrapingOrchestrationService` cachea resultados por `(source, term)` por 10 minutos |
| Implementation status | TS-SUP-000 a US-SUP-008 actualizados (v0.4.1–v0.5.1); US-SUP-009–011 pendientes |
