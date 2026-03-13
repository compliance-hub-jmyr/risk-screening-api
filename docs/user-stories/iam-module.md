# User Stories — IAM Module (Identity & Access Management)

> **Format:** Title / Description / Deliverable / Dependencies / Acceptance Criteria (BDD Given/When/Then).
> **Task tags:** `[BE-DOMAIN]` `[BE-APP]` `[BE-INFRA]` `[BE-INTERFACES]` `[BE-DB]` `[BE-TEST]` `[DOCS]`

---

## Technical Story: IAM Module Bootstrap

---

### TS-IAM-000: IAM Module Initial Setup

**Title:** Database scripts, EF Core configuration and IAM seeder

**Description:**
As a developer, I need to set up the foundational infrastructure of the IAM module — database tables, EF Core entity configurations, shared kernel abstractions, and the startup seeder — so that all IAM user stories have a stable base to build upon.

**Deliverable:**
SQL migration scripts V001–V003, EF Core configurations for `User`, `Role`, and `UserRole`, shared kernel base classes, and an `IamSeeder` that seeds the `ADMIN` and `ANALYST` system roles plus a default admin user on application startup.

**Dependencies:**
- None — this is the foundational task for the IAM module.

**Priority:** Critical | **Estimate:** 3 SP | **Status:** Implemented (v0.2.0)

#### Tasks

- `[BE-DB]` Script `V001__create_roles_table.sql` — columns: `id`, `name`, `description`, `is_system_role`, `created_at`
- `[BE-DB]` Script `V002__create_users_table.sql` — columns: `id`, `email`, `username`, `password_hash`, `account_status`, `failed_login_attempts`, `created_at`, `last_login_at`
- `[BE-DB]` Script `V003__create_user_roles_table.sql` — join table: `user_id`, `role_id`
- `[BE-INFRA]` EF Core `IEntityTypeConfiguration<User>`, `IEntityTypeConfiguration<Role>`, `IEntityTypeConfiguration<UserRole>`
- `[BE-INFRA]` `IamSeeder` — seeds `ADMIN` role, `ANALYST` role, and a default admin user with hashed password on `IHostedService.StartAsync`
- `[BE-APP]` Shared kernel: `AggregateRoot`, `ValueObject`, `DomainException` base classes; `IUnitOfWork` interface

#### Acceptance Criteria

- Given that the application starts for the first time against an empty database
- When the startup sequence completes
- Then the `roles` table contains exactly two system roles: `ADMIN` and `ANALYST`, both with `is_system_role = true`
- And the `users` table contains a default admin user with `account_status = ACTIVE`
- And subsequent startups are idempotent (seeder does not create duplicates)

---

## Epic: Authentication

---

### US-IAM-001: Sign in with email and password

**Title:** User authentication with credentials

**Description:**
As a platform user (compliance officer or administrator), I want to sign in with my email and password, to access the platform with my personalized session and obtain a JWT Bearer access token.

**Deliverable:**
Endpoint `POST /api/authentication/sign-in` that validates credentials, records failed attempts, locks the account after 5 consecutive failures, and returns a JWT Bearer token with configurable duration.

**Dependencies:**
- `TS-IAM-000`: IAM module initial setup (tables, shared kernel, EF Core)

**Priority:** High | **Estimate:** 3 SP | **Status:** Implemented (v0.2.0)

#### Tasks

- `[BE-DOMAIN]` `User` aggregate with methods `EnsureCanLogin()`, `RecordFailedLogin()`, `RecordSuccessfulLogin()`
- `[BE-DOMAIN]` Value objects: `Email`, `Username`, `Password` (hash + verify), `AccountStatus`
- `[BE-DOMAIN]` Domain exceptions: `InvalidCredentialsException`, `AccountLockedException`
- `[BE-APP]` `SignInCommand` + `SignInCommandHandler` (MediatR)
- `[BE-APP]` `SignInCommandValidator` (FluentValidation — email format, password required)
- `[BE-INFRA]` `JwtTokenService.GenerateToken(User)` — claims: sub, email, name, roles
- `[BE-INFRA]` `BCryptPasswordHasher.Verify(plain, hash)`
- `[BE-INFRA]` `UserRepository.FindByEmailAsync(email)` with roles included
- `[BE-INTERFACES]` `AuthenticationController.SignIn` — maps `SignInRequest` → `SignInCommand`
- `[BE-TEST]` Unit test: `SignInCommandHandlerTests` — successful login, wrong credentials, locked account, suspended account

#### Acceptance Criteria

**Scenario 1: Successful login**
- Given I am a registered user with an active account
- And I send `POST /api/authentication/sign-in` with correct email and password
- When the request is processed
- Then I receive HTTP 200 with `{ token, email, username, roles }`
- And the JWT token is valid for 24 hours

**Scenario 2: Wrong credentials**
- Given I send a valid email but wrong password
- When the request is processed
- Then I receive HTTP 401 Unauthorized
- And the failed login attempt counter is incremented by 1

**Scenario 3: Account locked after failed attempts**
- Given the user has failed 5 consecutive login attempts
- When they attempt to log in again
- Then I receive HTTP 401 with an account locked message
- And the account remains in `LOCKED` state until unlocked by an ADMIN

**Scenario 4: Suspended account**
- Given the administrator has suspended the user's account
- When the user attempts to log in
- Then I receive HTTP 401 with an account suspended message

**Scenario 5: Empty or invalid fields**
- Given I send the request without the `email` field or with an invalid email format
- When the request reaches the validation pipeline
- Then I receive HTTP 400 Bad Request with a list of validation errors

**Scenario 6: User not found**
- Given I send an email that does not exist in the database
- When the request is processed
- Then I receive HTTP 401 Unauthorized (without revealing whether the email exists, for security)

---

### US-IAM-002: Get authenticated user profile

**Title:** Query the current session user profile

**Description:**
As an authenticated user, I want to query my current profile information, so that the SPA can display my name, roles, and session data in the navigation header.

**Deliverable:**
Endpoint `GET /api/authentication/me` protected by JWT Bearer that returns the user profile extracted from the token.

**Dependencies:**
- US-IAM-001 (sign-in implemented and JWT valid)

**Priority:** High | **Estimate:** 1 SP | **Status:** Implemented (v0.2.0)

#### Tasks

- `[BE-APP]` `GetCurrentUserQuery` + `GetCurrentUserQueryHandler` — extracts userId from `sub` claim
- `[BE-INFRA]` `UserRepository.FindByIdAsync(userId)` with roles
- `[BE-INTERFACES]` `AuthenticationController.Me` — `[Authorize]`, extracts claim and delegates to handler
- `[BE-TEST]` Unit test: valid token returns profile, missing token returns 401

#### Acceptance Criteria

**Scenario 1: Valid token**
- Given I have an active JWT Bearer token in the `Authorization` header
- When I call `GET /api/authentication/me`
- Then I receive HTTP 200 with my profile `{ email, username, roles, token }`

**Scenario 2: Missing or expired token**
- Given I do not include the `Authorization` header or the token is expired
- When I make the request
- Then I receive HTTP 401 Unauthorized

---

## Epic: User Management (ADMIN only)

---

### US-IAM-003: List all users

**Title:** Paginated listing of system users

**Description:**
As an administrator, I want to see the paginated list of all registered users, to manage their accounts and statuses from the administration panel.

**Deliverable:**
Paginated endpoint `GET /api/users` with support for field and direction sorting, restricted to the `ADMIN` role.

**Dependencies:**
- US-IAM-001 (auth implemented)
- SQL scripts V001–V003

**Priority:** Medium | **Estimate:** 2 SP | **Status:** Implemented (v0.3.0)

#### Tasks

- `[BE-APP]` `GetUsersQuery` (page, size, sortBy, sortDirection) + `GetUsersQueryHandler`
- `[BE-INFRA]` `UserRepository.FindAllAsync(query)` with EF Core pagination
- `[BE-INTERFACES]` `UsersController.GetAll` — `[Authorize(Roles = "ADMIN")]`
- `[BE-TEST]` Unit test: paginated listing, sorting, access without ADMIN returns 403

#### Acceptance Criteria

**Scenario 1: Successful listing with pagination**
- Given I am an authenticated ADMIN
- When I call `GET /api/users?page=1&size=10`
- Then I receive HTTP 200 with `{ content: [...], page: { number, size, totalElements, totalPages } }`
- And each user includes `{ id, email, username, status, roles, createdAt, lastLoginAt }`

**Scenario 2: Filtering and sorting**
- Given I call `GET /api/users?sortBy=email&sortDirection=asc`
- When the request is processed
- Then I receive users sorted by email ascending

**Scenario 3: Without ADMIN permissions**
- Given I am an authenticated user with the `ANALYST` role
- When I call `GET /api/users`
- Then I receive HTTP 403 Forbidden

**Scenario 4: Without authentication**
- Given I do not include a JWT token
- When I call `GET /api/users`
- Then I receive HTTP 401 Unauthorized

---

### US-IAM-004: Get user by ID

**Title:** Query the full profile of a user by ID

**Description:**
As an administrator, I want to see the full profile of a specific user, to review their account status, assigned roles, and access history.

**Deliverable:**
Endpoint `GET /api/users/{id}` that returns the full user profile.

**Dependencies:**
- US-IAM-003

**Priority:** Medium | **Estimate:** 1 SP | **Status:** Implemented (v0.3.0)

#### Tasks

- `[BE-APP]` `GetUserByIdQuery` + `GetUserByIdQueryHandler` — throws `UserNotFoundException` if not found
- `[BE-INTERFACES]` `UsersController.GetById` — maps 404 via `GlobalExceptionHandler`
- `[BE-TEST]` Unit test: user found, user not found returns 404

#### Acceptance Criteria

**Scenario 1: User found**
- Given I am an ADMIN and the userId exists
- When I call `GET /api/users/{userId}`
- Then I receive HTTP 200 with the full user profile

**Scenario 2: User not found**
- Given the userId does not exist in the database
- When I call `GET /api/users/{userId}`
- Then I receive HTTP 404 Not Found with a descriptive message

---

### US-IAM-005: Delete user (soft delete)

**Title:** Logical deletion of a user

**Description:**
As an administrator, I want to logically delete a user account, so that former employees or unauthorized users can no longer access the platform without losing the audit record.

**Deliverable:**
Endpoint `DELETE /api/users/{id}` that changes the user's `AccountStatus` to `DELETED` (soft delete).

**Dependencies:**
- US-IAM-003

**Priority:** High | **Estimate:** 2 SP | **Status:** Implemented (v0.3.0)

#### Tasks

- `[BE-DOMAIN]` Method `User.Delete()` — changes status to `DELETED`, validates it is not already deleted
- `[BE-APP]` `DeleteUserCommand` + `DeleteUserCommandHandler`
- `[BE-INTERFACES]` `UsersController.Delete` — returns 204
- `[BE-TEST]` Unit test: successful deletion, already deleted user returns 404

#### Acceptance Criteria

**Scenario 1: Successful deletion**
- Given I am an ADMIN and the user with that userId exists and is not deleted
- When I call `DELETE /api/users/{userId}`
- Then I receive HTTP 204 No Content
- And the user's `account_status` field is changed to `DELETED` (soft delete)
- And the user cannot log in

**Scenario 2: User already deleted**
- Given the user already has status `DELETED`
- When I call `DELETE /api/users/{userId}`
- Then I receive HTTP 404 Not Found

**Scenario 3: User not found**
- Given the userId does not exist
- When I call `DELETE /api/users/{userId}`
- Then I receive HTTP 404 Not Found

---

### US-IAM-006: Activate suspended or locked user

**Title:** Reactivation of a user account

**Description:**
As an administrator, I want to reactivate the account of a suspended or locked user, so that legitimate users can regain access to the platform.

**Deliverable:**
Endpoint `PATCH /api/users/{id}/activate` that changes the `AccountStatus` to `ACTIVE` and resets the failed login attempt counter.

**Dependencies:**
- US-IAM-005

**Priority:** High | **Estimate:** 1 SP | **Status:** Implemented (v0.3.0)

#### Tasks

- `[BE-DOMAIN]` Method `User.Activate()` — changes status to `ACTIVE`, resets `FailedLoginAttempts`
- `[BE-APP]` `ActivateUserCommand` + `ActivateUserCommandHandler`
- `[BE-INTERFACES]` `UsersController.Activate`
- `[BE-TEST]` Unit test: successful activation, already active user is idempotent (returns 204 without error)

#### Acceptance Criteria

**Scenario 1: Successful activation**
- Given I am an ADMIN and the user has status `SUSPENDED` or `LOCKED`
- When I call `PATCH /api/users/{userId}/activate`
- Then I receive HTTP 204 No Content
- And the user can log in again

**Scenario 2: User already active (idempotent)**
- Given the user already has status `ACTIVE`
- When I call `PATCH /api/users/{userId}/activate`
- Then I receive HTTP 204 No Content

---

### US-IAM-007: Suspend active user

**Title:** Temporary suspension of a user account

**Description:**
As an administrator, I want to temporarily suspend an active user account, to restrict their access without permanently deleting the account.

**Deliverable:**
Endpoint `PATCH /api/users/{id}/suspend` that changes the `AccountStatus` to `SUSPENDED`.

**Dependencies:**
- US-IAM-006

**Priority:** High | **Estimate:** 1 SP | **Status:** Implemented (v0.3.0)

#### Tasks

- `[BE-DOMAIN]` Method `User.Suspend()` — changes status to `SUSPENDED`
- `[BE-APP]` `SuspendUserCommand` + `SuspendUserCommandHandler`
- `[BE-INTERFACES]` `UsersController.Suspend`
- `[BE-TEST]` Unit test: successful suspension, user not found returns 404

#### Acceptance Criteria

**Scenario 1: Successful suspension**
- Given I am an ADMIN and the user has status `ACTIVE`
- When I call `PATCH /api/users/{userId}/suspend`
- Then I receive HTTP 204 No Content
- And the user cannot log in (receives 401)

**Scenario 2: User not found**
- Given the userId does not exist
- When I call `PATCH /api/users/{userId}/suspend`
- Then I receive HTTP 404 Not Found

---

### US-IAM-008: Assign role to user

**Title:** Role assignment to a user account

**Description:**
As an administrator, I want to assign a role to a user, to grant them the permissions associated with that role within the platform.

**Deliverable:**
Endpoint `POST /api/users/{id}/roles` that creates the `user_roles` relationship between the user and the indicated role.

**Dependencies:**
- US-IAM-003
- Roles are seeded at startup via `TS-IAM-000` — no dynamic role creation required.

**Priority:** High | **Estimate:** 2 SP | **Status:** Implemented (v0.3.0)

#### Tasks

- `[BE-APP]` `AssignRoleCommand` + `AssignRoleCommandHandler` — validates role exists, is idempotent
- `[BE-APP]` `AssignRoleCommandValidator` (FluentValidation — roleName required, not empty)
- `[BE-INFRA]` `RoleRepository.FindByNameAsync(name)`
- `[BE-INTERFACES]` `UsersController.AssignRole`
- `[BE-TEST]` Unit test: successful assignment, role already assigned is idempotent, role not found returns 404

#### Acceptance Criteria

**Scenario 1: Successful assignment**
- Given I am an ADMIN and I send `POST /api/users/{userId}/roles` with `{ "roleName": "ANALYST" }`
- When the request is processed
- Then I receive HTTP 204 No Content
- And the user has the `ANALYST` role in their profile

**Scenario 2: Role already assigned (idempotent)**
- Given the user already has the `ANALYST` role
- When I attempt to assign it again
- Then I receive HTTP 204 No Content (without error)

**Scenario 3: Role not found**
- Given I send a roleName that does not exist in the system
- When the request is processed
- Then I receive HTTP 404 Not Found with message "Role not found"

**Scenario 4: Field validation**
- Given I send the body without the `roleName` field or with an empty value
- When the request reaches the validation pipeline
- Then I receive HTTP 400 Bad Request

---

### US-IAM-009: Revoke role from user

**Title:** Role revocation from a user account

**Description:**
As an administrator, I want to revoke a role from a user, to restrict their permissions when their responsibilities change.

**Deliverable:**
Endpoint `DELETE /api/users/{id}/roles/{roleName}` that removes the `user_roles` relationship between the user and the role.

**Dependencies:**
- US-IAM-008

**Priority:** High | **Estimate:** 1 SP | **Status:** Implemented (v0.3.0)

#### Tasks

- `[BE-APP]` `RevokeRoleCommand` + `RevokeRoleCommandHandler`
- `[BE-APP]` `RevokeRoleCommandValidator` (FluentValidation — roleName required)
- `[BE-INTERFACES]` `UsersController.RevokeRole`
- `[BE-TEST]` Unit test: successful revocation, user not found returns 404

#### Acceptance Criteria

**Scenario 1: Successful revocation**
- Given I am an ADMIN and the user has the `ANALYST` role
- When I call `DELETE /api/users/{userId}/roles/ANALYST`
- Then I receive HTTP 204 No Content
- And the user no longer has the `ANALYST` role

**Scenario 2: User not found**
- Given the userId does not exist
- When I call `DELETE /api/users/{userId}/roles/ANALYST`
- Then I receive HTTP 404 Not Found

---

## Out of Scope — v1.0

The following stories are **not in scope for v1.0**. Roles are fixed system roles (`ADMIN`, `ANALYST`) seeded at startup. Dynamic role management is deferred to a future milestone.

### US-IAM-010: List all roles *(deferred)*

Endpoint `GET /api/roles` returning the role catalog. Not required in v1.0 since roles are seeded and known at design time.

### US-IAM-011: Create custom role *(deferred)*

Endpoint `POST /api/roles` for creating custom roles with arbitrary names. Not required in v1.0; only `ADMIN` and `ANALYST` are supported.

---

## Implementation Notes

| Aspect | Implementation |
|--------|---------------|
| Account lockout | Automatic on the 5th consecutive failed login (`User.MaxFailedLoginAttempts = 5`) |
| Soft delete | Changes `AccountStatus` to `DELETED`, never physically removes the record |
| System roles | `ADMIN` and `ANALYST` are `IsSystemRole = true`, seeded on startup via `TS-IAM-000` |
| Idempotency | `AssignRole` and `Activate` are idempotent — calling them multiple times does not cause errors |
| Normalization | Role names are normalized to UPPERCASE in the domain |
| Implementation status | All US-IAM stories implemented in v0.2.0 (auth) and v0.3.0 (users/roles) |
