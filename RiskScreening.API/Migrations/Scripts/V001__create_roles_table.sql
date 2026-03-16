-- =============================================================
-- V001 - Create roles table
-- RiskScreening Platform - IAM Module
-- =============================================================
-- PKs   : UUID stored as NVARCHAR(36)
-- Audit : created_at, updated_at, created_by, updated_by
-- Dates : DATETIME2
-- =============================================================

CREATE TABLE roles
(
    id             NVARCHAR(36)    NOT NULL,
    name           NVARCHAR(50)    NOT NULL,
    description    NVARCHAR(200)   NULL,
    is_system_role BIT       NOT NULL,
    created_at     DATETIME2 NOT NULL,
    updated_at     DATETIME2 NOT NULL,
    created_by     NVARCHAR(256)   NULL,
    updated_by     NVARCHAR(256)   NULL,

    CONSTRAINT PK_roles PRIMARY KEY (id),
    CONSTRAINT UQ_roles_name UNIQUE (name),
    CONSTRAINT CK_roles_name CHECK (LEN(LTRIM(RTRIM(name))) > 0)
);

CREATE INDEX IX_roles_name ON roles (name);
CREATE INDEX IX_roles_is_system ON roles (is_system_role);
