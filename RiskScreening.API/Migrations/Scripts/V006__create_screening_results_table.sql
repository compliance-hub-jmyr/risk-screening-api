-- =============================================================
-- V006 - Create screening_results table
-- RiskScreening Platform - Suppliers Module
-- =============================================================
-- Records each screening run for a supplier.
-- Immutable after creation — no updated_at column.
-- risk_level values: NONE, LOW, MEDIUM, HIGH
-- entries_json: serialized JSON array of matched RiskEntry objects
-- sources_queried: CSV of queried sources, e.g. "OFAC, WORLD_BANK,ICIJ"
-- =============================================================

CREATE TABLE screening_results
(
    id              NVARCHAR(36)    NOT NULL DEFAULT NEWID(),
    supplier_id     NVARCHAR(36)    NOT NULL,
    sources_queried NVARCHAR(200)   NOT NULL,
    screened_at     DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    risk_level      NVARCHAR(10)    NOT NULL DEFAULT 'NONE',
    total_matches   INT       NOT NULL DEFAULT 0,
    entries_json    NVARCHAR(MAX)   NULL,
    created_at      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    created_by      NVARCHAR(255)   NULL,

    CONSTRAINT PK_screening_results PRIMARY KEY (id),
    CONSTRAINT FK_screening_results_suppliers FOREIGN KEY (supplier_id) REFERENCES suppliers (id) ON DELETE CASCADE,
    CONSTRAINT CK_screening_results_risk_level CHECK (risk_level IN ('NONE', 'LOW', 'MEDIUM', 'HIGH'))
);

CREATE INDEX IX_screening_results_supplier_id ON screening_results (supplier_id);
CREATE INDEX IX_screening_results_screened_at ON screening_results (screened_at DESC);
CREATE INDEX IX_screening_results_risk_level ON screening_results (risk_level);
