-- =============================================================
-- V005 - Create suppliers table
-- RiskScreening Platform - Suppliers Module
-- =============================================================
-- Stores supplier records for due diligence management.
-- risk_level values: NONE, LOW, MEDIUM, HIGH
-- status values: PENDING, APPROVED, REJECTED, UNDER_REVIEW
-- is_deleted: soft-delete flag, independent of business status
-- =============================================================

CREATE TABLE suppliers
(
    id                 NVARCHAR(36)    NOT NULL DEFAULT NEWID(),
    legal_name         NVARCHAR(200)   NOT NULL,
    commercial_name    NVARCHAR(200)   NOT NULL,
    tax_id             CHAR(11)  NOT NULL,
    contact_phone      NVARCHAR(50)    NULL,
    contact_email      NVARCHAR(255)   NULL,
    website            NVARCHAR(500)   NULL,
    address            NVARCHAR(500)   NULL,
    country            NVARCHAR(100)   NOT NULL,
    annual_billing_usd DECIMAL(18, 2) NULL,
    risk_level         NVARCHAR(10)    NOT NULL DEFAULT 'NONE',
    status             NVARCHAR(20)    NOT NULL DEFAULT 'PENDING',
    is_deleted         BIT       NOT NULL DEFAULT 0,
    notes              NVARCHAR(MAX)   NULL,
    created_at         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    created_by         NVARCHAR(255)   NULL,
    updated_by         NVARCHAR(255)   NULL,

    CONSTRAINT PK_suppliers PRIMARY KEY (id),
    CONSTRAINT UQ_suppliers_tax_id UNIQUE (tax_id),
    CONSTRAINT CK_suppliers_tax_id CHECK (tax_id LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT CK_suppliers_risk_level CHECK (risk_level IN ('NONE', 'LOW', 'MEDIUM', 'HIGH')),
    CONSTRAINT CK_suppliers_status CHECK (status IN ('PENDING', 'APPROVED', 'REJECTED', 'UNDER_REVIEW')),
    CONSTRAINT CK_suppliers_billing CHECK (annual_billing_usd IS NULL OR annual_billing_usd >= 0)
);

CREATE INDEX IX_suppliers_risk_level ON suppliers (risk_level);
CREATE INDEX IX_suppliers_status ON suppliers (status);
CREATE INDEX IX_suppliers_country ON suppliers (country);
CREATE INDEX IX_suppliers_is_deleted ON suppliers (is_deleted);
