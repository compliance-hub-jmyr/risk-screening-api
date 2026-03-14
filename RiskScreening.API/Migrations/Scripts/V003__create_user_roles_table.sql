-- =============================================================
-- V003 - Create user_roles table
-- RiskScreening Platform - IAM Module
-- =============================================================
-- Join table : many-to-many between users and roles
-- FKs        : users(id), roles(id)
-- Note       : EF Core maps this via HasMany().WithMany().UsingEntity()
--              Column names match EF Core convention: user_id, roles_id
-- =============================================================

CREATE TABLE user_roles
(
    user_id  NVARCHAR(36)    NOT NULL,
    roles_id NVARCHAR(36)    NOT NULL,

    CONSTRAINT PK_user_roles PRIMARY KEY (user_id, roles_id),
    CONSTRAINT FK_user_roles_users FOREIGN KEY (user_id) REFERENCES users (id),
    CONSTRAINT FK_user_roles_roles FOREIGN KEY (roles_id) REFERENCES roles (id)
);

CREATE INDEX IX_user_roles_user_id ON user_roles (user_id);
CREATE INDEX IX_user_roles_roles_id ON user_roles (roles_id);
