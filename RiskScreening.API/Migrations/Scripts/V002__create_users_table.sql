-- =============================================================
-- V002 - Create users table
-- RiskScreening Platform - IAM Module
-- =============================================================
-- PKs      : UUID stored as NVARCHAR(36)
-- Audit    : created_at, updated_at, created_by, updated_by
-- Dates    : DATETIME2
-- Password : BCrypt hash stored as NVARCHAR(72) (BCrypt max length)
-- Status   : enum stored as string (Active/Locked/Suspended/Deleted)
-- =============================================================

CREATE TABLE users
(
    id                    NVARCHAR(36)    NOT NULL,
    email                 NVARCHAR(256)   NOT NULL,
    username              NVARCHAR(50)    NOT NULL,
    password              NVARCHAR(72)    NOT NULL,
    status                NVARCHAR(20)    NOT NULL,
    failed_login_attempts INT       NOT NULL DEFAULT 0,
    last_login_at         DATETIME2 NULL,
    locked_at             DATETIME2 NULL,
    created_at            DATETIME2 NOT NULL,
    updated_at            DATETIME2 NOT NULL,
    created_by            NVARCHAR(256)   NULL,
    updated_by            NVARCHAR(256)   NULL,

    CONSTRAINT PK_users PRIMARY KEY (id),
    CONSTRAINT UQ_users_email UNIQUE (email),
    CONSTRAINT CK_users_status CHECK (status IN ('Active', 'Locked', 'Suspended', 'Deleted'))
);

CREATE INDEX IX_users_email ON users (email);
CREATE INDEX IX_users_username ON users (username);
CREATE INDEX IX_users_status ON users (status);
