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
  - `legal_name` NVARCHAR(200) NOT NULL *(trade name / razón social)*
  - `commercial_name` NVARCHAR(200) NOT NULL *(commercial name / nombre comercial)*
  - `tax_id` CHAR(11) NOT NULL UNIQUE *(tax identifier — exactly 11 numeric digits)*
  - `contact_phone` NVARCHAR(50) NULL
  - `contact_email` NVARCHAR(255) NULL
  - `website` NVARCHAR(500) NULL
  - `address` NVARCHAR(500) NULL
  - `country` NVARCHAR(100) NOT NULL
  - `annual_billing_usd` DECIMAL(18, 2) NULL *(annual billing in USD)*
  - `risk_level` NVARCHAR(10) NOT NULL DEFAULT 'NONE' CHECK (NONE/LOW/MEDIUM/HIGH)
  - `status` NVARCHAR(20) NOT NULL DEFAULT 'PENDING' CHECK (PENDING/APPROVED/REJECTED/UNDER_REVIEW)
  - `is_deleted` BIT NOT NULL DEFAULT 0 *(soft-delete flag — independent of business status)*
  - `notes` NVARCHAR(MAX) NULL
  - `created_at` DATETIME2, `updated_at` DATETIME2, `created_by` NVARCHAR(255), `updated_by` NVARCHAR(255)
  - Indexes: `IX_suppliers_risk_level`, `IX_suppliers_status`, `IX_suppliers_country`, `IX_suppliers_is_deleted`
  - CHECK constraint on `tax_id`: `LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'`

- `[BE-DB]` Script `V006__create_screening_results_table.sql` — columns:
  - `id` NVARCHAR(36) PK
  - `supplier_id` NVARCHAR(36) NOT NULL FK → `suppliers(id)` ON DELETE CASCADE
  - `sources_queried` NVARCHAR(200) NOT NULL *(CSV of queried sources, e.g. "OFAC,WORLD_BANK,ICIJ")*
  - `screened_at` DATETIME2 NOT NULL DEFAULT GETUTCDATE()
  - `risk_level` NVARCHAR(10) NOT NULL DEFAULT 'NONE' CHECK (NONE/LOW/MEDIUM/HIGH)
  - `total_matches` INT NOT NULL DEFAULT 0
  - `entries_json` NVARCHAR(MAX) NULL *(JSON serialization of matched RiskEntry objects)*
  - `created_at` DATETIME2 NOT NULL DEFAULT GETUTCDATE()
  - Indexes: `IX_screening_results_supplier_id`, `IX_screening_results_screened_at DESC`, `IX_screening_results_risk_level`
  - **No `updated_at`** — screening results are immutable after creation

- `[BE-INFRA]` EF Core `SupplierConfiguration` — maps all fields including `LegalName`, `CommercialName`, `Website`, `AnnualBillingUsd`, `IsDeleted`
- `[BE-INFRA]` EF Core `ScreeningResultConfiguration` — maps `SourcesQueried`, `EntriesJson`; no `updated_at`
- `[BE-INFRA]` `SupplierRepository` and `ScreeningResultRepository` implementing `BaseRepository`
- `[BE-INFRA]` `SuppliersModuleExtensions.AddSuppliersModule()` — registers both repositories as scoped

#### Acceptance Criteria

- Given the application starts against a fresh database
- When the DbUp migration sequence completes
- Then the `suppliers` table exists with `legal_name`, `commercial_name`, `tax_id` CHAR(11), `website`, `annual_billing_usd`, `is_deleted`, and the corresponding constraints
- And the `screening_results` table exists with `sources_queried` and `entries_json`, no `updated_at`, with FK to `suppliers` CASCADE DELETE
- And subsequent startups are idempotent (DbUp does not re-run applied scripts)

---

## Epic: Supplier Management

---

### US-SUP-001: Register a new supplier

**Title:** Create a supplier record

**Description:**
As a compliance officer or administrator, I want to register a new supplier in the platform, so that I can track its risk profile and run screening checks against international risk lists.

**Deliverable:**
Endpoint `POST /api/suppliers` that validates the request, checks for `TaxId` duplicates, creates the `Supplier` aggregate with `status = PENDING`, `riskLevel = NONE`, `isDeleted = false`, and returns `201 Created` with the new Id.

**Dependencies:**
- `TS-SUP-000`: module bootstrap
- `US-IAM-001`: JWT authentication

**Priority:** High | **Estimate:** 3 SP | **Status:** Updated (v0.4.1)

#### Tasks

- `[BE-DOMAIN]` `Supplier` aggregate with factory method `Create(legalName, commercialName, taxId, country, contactPhone?, contactEmail?, website?, address?, annualBillingUsd?, notes?)` — initializes `Status = PENDING`, `RiskLevel = NONE`, `IsDeleted = false`
- `[BE-DOMAIN]` Value rules in `Create`:
  - `LegalName` ≤ 200, required
  - `CommercialName` ≤ 200, required
  - `TaxId` exactly 11 numeric digits (`^\d{11}$`)
  - `ContactEmail` valid format if provided
  - `Website` valid URL format if provided
  - `AnnualBillingUsd` ≥ 0 if provided
- `[BE-DOMAIN]` `SupplierTaxIdAlreadyExistsException` (extends `BusinessRuleViolationException`)
- `[BE-APP]` `CreateSupplierCommand` + `CreateSupplierCommandHandler` — checks `ExistsByTaxIdAsync`, calls `Supplier.Create`, persists, returns new Id
- `[BE-APP]` `CreateSupplierCommandValidator` (FluentValidation) with all field rules
- `[BE-INTERFACES]` `SuppliersController.Create` — maps `CreateSupplierRequest` → `CreateSupplierCommand`; returns `201 Created` with `Location` header
- `[BE-TEST]` Unit test: successful creation, duplicate TaxId returns 409, invalid TaxId format returns 400, missing required fields return 400

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
Endpoint `GET /api/suppliers` returning a paginated and sorted list of active suppliers (`isDeleted = false`), with support for `page`, `size`, `sortBy`, `sortDirection`.

**Dependencies:**
- `US-SUP-001`

**Priority:** High | **Estimate:** 2 SP | **Status:** Updated (v0.4.1)

#### Tasks

- `[BE-APP]` `GetAllSuppliersQuery` (page, size, sortBy, sortDirection) + `GetAllSuppliersQueryHandler` — filters `IsDeleted = false`, sorts by `UpdatedAt` desc by default
- `[BE-INFRA]` `ISupplierRepository.Query()` returning `IQueryable<Supplier>` for LINQ composition
- `[BE-INTERFACES]` `SuppliersController.GetAll` — maps query params to `GetAllSuppliersQuery`; returns `PageResponse<SupplierResponse>`
- `[BE-TEST]` Unit test: paginated listing, sorting, empty result

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
Endpoint `GET /api/suppliers/{supplierId}` returning the supplier's full profile.

**Dependencies:**
- `US-SUP-002`

**Priority:** High | **Estimate:** 1 SP | **Status:** Updated (v0.4.1)

#### Tasks

- `[BE-APP]` `GetSupplierByIdQuery` + `GetSupplierByIdQueryHandler` — throws `SupplierNotFoundException` if not found or `IsDeleted = true`
- `[BE-INTERFACES]` `SuppliersController.GetById` — maps 404 via `GlobalExceptionHandler`
- `[BE-TEST]` Unit test: supplier found, supplier not found returns 404, soft-deleted supplier returns 404

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
Endpoint `PUT /api/suppliers/{supplierId}` that updates all mutable fields. `TaxId` is intentionally excluded — it cannot be changed after creation.

**Dependencies:**
- `US-SUP-003`

**Priority:** High | **Estimate:** 2 SP | **Status:** Updated (v0.4.1)

#### Tasks

- `[BE-DOMAIN]` `Supplier.Update(legalName, commercialName, country, contactPhone?, contactEmail?, website?, address?, annualBillingUsd?, notes?)` method — applies new values, `EnsureNotDeleted()` guard checks `IsDeleted = false`
- `[BE-APP]` `UpdateSupplierCommand` + `UpdateSupplierCommandHandler` — loads supplier, calls `Update`, commits
- `[BE-APP]` `UpdateSupplierCommandValidator` (FluentValidation — same rules as Create, plus Id required)
- `[BE-INTERFACES]` `SuppliersController.Update` — returns 204 No Content
- `[BE-TEST]` Unit test: successful update, supplier not found returns 404, soft-deleted supplier returns 422

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
Endpoint `DELETE /api/suppliers/{supplierId}` that sets `IsDeleted = true`. **Does not modify `Status`** — the business state (PENDING/APPROVED/REJECTED/UNDER_REVIEW) is preserved for audit. The record remains in the database and is excluded from all listings.

**Dependencies:**
- `US-SUP-003`

**Priority:** High | **Estimate:** 1 SP | **Status:** Updated (v0.4.1)

#### Tasks

- `[BE-DOMAIN]` `Supplier.Delete()` method — sets `IsDeleted = true`; `EnsureNotDeleted()` guard throws `SupplierAlreadyDeletedException` if `IsDeleted` is already `true`
- `[BE-APP]` `DeleteSupplierCommand` + `DeleteSupplierCommandHandler`
- `[BE-INTERFACES]` `SuppliersController.Delete` — returns 204 No Content
- `[BE-TEST]` Unit test: successful deletion, already deleted returns 404, supplier not found returns 404

> **Design note:** The soft-delete (`IsDeleted = true`) is independent of the compliance rejection (`Status = REJECTED`). A rejected supplier remains visible in listings until explicitly deleted. Both operations can coexist.

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
As a compliance officer, I want to trigger a risk screening for a supplier against international sanctions and debarment lists — selecting one or more sources — to evaluate its risk level before approving it as a vendor.

**Deliverable:**
Endpoint `POST /api/suppliers/{supplierId}/screenings` that accepts an optional list of sources to query (OFAC, WORLD_BANK, ICIJ; all by default), executes the queries in parallel, computes a `RiskLevel` with per-source logic, creates a `ScreeningResult` storing the matched entries serialized as JSON, updates the supplier's `RiskLevel`, and auto-transitions the supplier to `UNDER_REVIEW` if the result is `HIGH`. Returns `201 Created` with the new screening result Id.

**Dependencies:**
- `US-SUP-003`
- Scraping module (`ScrapingOrchestrationService`) operacional

**Priority:** Critical | **Estimate:** 5 SP | **Status:** Updated (v0.5.1)

#### Tasks

- `[BE-DOMAIN]` `Supplier.ApplyScreeningResult(RiskLevel)` method — updates `RiskLevel`, sets `Status = UNDER_REVIEW` if `riskLevel = HIGH`
- `[BE-DOMAIN]` `ScreeningResult` aggregate with factory `ScreeningResult.Create(supplierId, sourcesQueried, riskLevel, totalMatches, entries)` — immutable after creation; `entries` is serialized as JSON in `EntriesJson`
- `[BE-DOMAIN]` `ScreeningResultNotFoundException` (extends `EntityNotFoundException`)
- `[BE-APP]` `RunScreeningCommand` — fields: `SupplierId`, `Sources` (optional `IReadOnlyList<string>?`; valid values: `"ofac"`, `"worldbank"`, `"icij"`; if null → all)
- `[BE-APP]` `RunScreeningCommandHandler`:
  1. Loads supplier; throws `SupplierNotFoundException` if not found or `IsDeleted = true`
  2. Determines sources to query (`Sources` param or all by default)
  3. Calls `ScrapingOrchestrationService` for the selected sources in parallel using `LegalName` as the search term
  4. Computes `RiskLevel` with **per-source logic**:
     - **OFAC**: score ≥ 0.85 → HIGH | score ≥ 0.60 → MEDIUM | any match → LOW
     - **World Bank**: any match → HIGH *(formally debarred company)*
     - **ICIJ**: any match → LOW *(journalistic leak, not an official sanction)*
     - Final `RiskLevel` = maximum across all sources
  5. Creates `ScreeningResult.Create(supplierId, sourcesQueried, riskLevel, totalMatches, allEntries)`
  6. Calls `supplier.ApplyScreeningResult(riskLevel)`
  7. Persists both, commits, returns new result Id
- `[BE-INFRA]` `IScreeningResultRepository` + `ScreeningResultRepository`
- `[BE-INTERFACES]` `SuppliersController.RunScreening` — accepts optional body `{ "sources": ["ofac", "worldbank"] }`; returns `201 Created` with `Location` header
- `[BE-TEST]` Unit test: screening creates result and updates supplier; World Bank match → HIGH; HIGH → UNDER_REVIEW; entries persisted; supplier not found returns 404

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
Endpoint `GET /api/screenings?supplierId={supplierId}` returning a paginated list of all screening results. The **matched entries** (`entries`) are omitted from the listing for lightweight responses — they are retrieved via `GET /api/screenings/{id}`.

**Dependencies:**
- `US-SUP-006`

**Priority:** High | **Estimate:** 2 SP | **Status:** Updated (v0.5.1)

#### Tasks

- `[BE-APP]` `GetScreeningResultsBySupplierQuery` (supplierId, page, size) + handler — queries via `QueryBySupplierId`, orders by `ScreenedAt` desc
- `[BE-INTERFACES]` `ScreeningsController.GetBySupplierId` — requires `supplierId` query param; returns `PageResponse<ScreeningResultSummaryResponse>` *(without `entries` field)*
- `[BE-TEST]` Unit test: paginated results for valid supplier, empty list when no screenings exist

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
Endpoint `GET /api/screenings/{screeningId}` returning the full screening result with the list of matched entries deserialized from `entriesJson`.

**Dependencies:**
- `US-SUP-007`

**Priority:** Medium | **Estimate:** 1 SP | **Status:** Updated (v0.5.1)

#### Tasks

- `[BE-APP]` `GetScreeningResultByIdQuery` + handler — throws `ScreeningResultNotFoundException` if not found; deserializes `EntriesJson` → `List<RiskEntry>`
- `[BE-INTERFACES]` `ScreeningsController.GetById` — returns `ScreeningResultDetailResponse` including the `entries` field
- `[BE-TEST]` Unit test: result found with deserialized entries, result not found returns 404

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
Endpoint `PATCH /api/suppliers/{supplierId}/approve` that transitions `Status` to `APPROVED`.

**Dependencies:**
- `US-SUP-006`

**Priority:** High | **Estimate:** 1 SP | **Status:** Pending

#### Tasks

- `[BE-DOMAIN]` `Supplier.Approve()` method — sets `Status = APPROVED`; `EnsureNotDeleted()` guard checks `IsDeleted = false`
- `[BE-APP]` `ApproveSupplierCommand` + `ApproveSupplierCommandHandler`
- `[BE-INTERFACES]` `SuppliersController.Approve` — returns 204 No Content
- `[BE-TEST]` Unit test: successful approval, deleted supplier returns 404

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
As an administrator, I want to reject a supplier that did not pass the compliance review. **This rejection is a business decision** and is independent of the logical deletion of the record. A rejected supplier remains visible in listings for audit purposes.

**Deliverable:**
Endpoint `PATCH /api/suppliers/{supplierId}/reject` that transitions `Status` to `REJECTED`.

**Dependencies:**
- `US-SUP-009`

**Priority:** High | **Estimate:** 1 SP | **Status:** Pending

#### Tasks

- `[BE-DOMAIN]` `Supplier.Reject()` method — sets `Status = REJECTED`; `EnsureNotDeleted()` guard checks `IsDeleted = false`; throws `InvalidSupplierStateException` if already in `REJECTED`
- `[BE-APP]` `RejectSupplierCommand` + `RejectSupplierCommandHandler`
- `[BE-INTERFACES]` `SuppliersController.Reject` — returns 204 No Content
- `[BE-TEST]` Unit test: successful rejection, already rejected returns 422, deleted supplier returns 404

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
Endpoint `PATCH /api/suppliers/{supplierId}/under-review` that transitions `Status` to `UNDER_REVIEW`.

**Dependencies:**
- `US-SUP-009`

**Priority:** Medium | **Estimate:** 1 SP | **Status:** Pending

#### Tasks

- `[BE-DOMAIN]` `Supplier.MarkUnderReview()` method — sets `Status = UNDER_REVIEW`; `EnsureNotDeleted()` guard checks `IsDeleted = false`
- `[BE-APP]` `MarkUnderReviewCommand` + `MarkUnderReviewCommandHandler`
- `[BE-INTERFACES]` `SuppliersController.MarkUnderReview` — returns 204 No Content
- `[BE-TEST]` Unit test: successful transition, deleted supplier returns 404

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
| Soft delete | `Supplier.Delete()` sets `IsDeleted = true`; **does not modify `Status`**; filtered from all listings with `WHERE is_deleted = 0`; `GetById` also returns 404 for deleted records |
| Business rejection | `Supplier.Reject()` sets `Status = REJECTED`; record **remains visible** in listings (`isDeleted = false`); it is a compliance decision, not a deletion |
| TaxId | Exactly 11 numeric digits; validated with FluentValidation (`^\d{11}$`) and a DB CHECK constraint (`CHAR(11)`); immutable after creation |
| Required supplier fields | `legalName` (razón social), `commercialName` (nombre comercial), `taxId`, `country` — required; `website`, `annualBillingUsd` DECIMAL(18,2), `contactPhone`, `contactEmail`, `address` — optional |
| Risk scoring per source | OFAC: score ≥ 0.85 → HIGH, ≥ 0.60 → MEDIUM, any match → LOW; World Bank: any match → HIGH (formally debarred); ICIJ: any match → LOW (journalistic leak, not an official sanction) |
| Final risk level | `RiskLevel` = maximum across all queried sources |
| Auto-review | `Supplier.ApplyScreeningResult(HIGH)` automatically sets `Status = UNDER_REVIEW` |
| Entries in ScreeningResult | Serialized as JSON in `entries_json` (NVARCHAR MAX); `GET /api/screenings/{id}` deserializes and returns them; the listing (`GET /api/screenings?supplierId=`) omits them for lightweight responses |
| ScreeningResult immutability | No `updated_at` in DB or EF Core config; an existing result is never updated |
| Source selection | `RunScreeningCommand.Sources` (optional); if null = all sources; validated against ["ofac", "worldbank", "icij"] |
| Cascade delete | `screening_results.supplier_id` FK with `ON DELETE CASCADE` — physically deleting a supplier at the DB level also removes its screening history |
| Caching | `ScrapingOrchestrationService` caches results by `(source, term)` for 10 minutes |
| Implementation status | TS-SUP-000 through US-SUP-008 updated (v0.4.1–v0.5.1); US-SUP-009–011 pending |
