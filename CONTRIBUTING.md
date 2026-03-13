# Contributing Guide

## Repositories

This project is composed of two independent repositories:

| Repository | Stack | Description |
|------------|-------|-------------|
| `risk-screening-api` | .NET 10 / ASP.NET Core | Backend REST API (this repository) |
| `risk-screening-app` | Angular 21 / TypeScript | Frontend SPA (separate repository) |

Each repository has its own `CHANGELOG.md`, `CONTRIBUTING.md`, and versioning cycle. This guide covers the **backend API** repository only.

---

## Gitflow

This project follows the standard **Gitflow** model:

```
main          <- stable releases only, tagged (v1.0.0, v1.1.0)
  |
develop       <- integration branch — all features merge here first
  |
  |-- feature/us-iam-001-sign-in          <- one branch per User Story
  |-- feature/us-scr-001-ofac-search
  |-- feature/us-sup-001-create-supplier
  |-- bugfix/fix-jwt-expiry-claim
  |-- release/1.0.0                       <- release preparation branch
  `-- hotfix/critical-auth-bypass         <- from main only, merge to main AND develop
```

### Branch types (standard Gitflow)

| Type | Pattern | Origin | Merge into | When to use |
|------|---------|--------|------------|-------------|
| `feature/` | `feature/us-XXX-description` | `develop` | `develop` | New functionality (User Story or Technical Story) |
| `bugfix/` | `bugfix/short-description` | `develop` | `develop` | Bug found during development |
| `release/` | `release/X.Y.Z` | `develop` | `main` + `develop` | Prepare a release — bug fixes only, no new features |
| `hotfix/` | `hotfix/short-description` | `main` | `main` + `develop` | Critical production bug that cannot wait |

> Do not use `chore/` or `test/` as branch names — those are **commit** types, not branch types.
> A test-only branch goes as `feature/test-iam-coverage` or directly as part of the feature branch that generates the tests.

---

## Conventional Commits

All commits must follow the [Conventional Commits v1.0.0](https://www.conventionalcommits.org/en/v1.0.0/) standard:

```
<type>(<scope>): <description>

[optional body]

[optional footer: BREAKING CHANGE, Closes #issue]
```

### Allowed commit types

| Type | When to use | Appears in CHANGELOG |
|------|-------------|----------------------|
| `feat` | New functionality | Yes — under `Added` |
| `fix` | Bug fix | Yes — under `Fixed` |
| `refactor` | Code change without new feature or fix | No |
| `test` | Add or fix tests | No |
| `docs` | Documentation only (README, ADRs, user stories) | No |
| `chore` | Maintenance, config, dependencies | No |
| `perf` | Performance improvement | Yes — under `Changed` |
| `ci` | CI/CD pipeline changes | No |
| `build` | Build system or dependency changes | No |

### Correct examples

```bash
feat(iam): add UsersController with CRUD endpoints
feat(scraping): add OFAC SDN search endpoint
fix(auth): handle locked accounts in sign-in flow
refactor(shared): extract pagination logic to PageableExtensions
test(iam): add CreateRoleCommandHandler unit tests
docs(adr): add ADR-0007 Angular framework decision
chore(deps): upgrade MediatR to 12.3.0
```

### Incorrect examples

```bash
# BAD — does not follow the format
update stuff
fixed bug
WIP
added feature

# BAD — non-existent scope
feat(random): add thing
```

---

## Step-by-step workflow

### 1. Start a new User Story

```bash
# Make sure develop is up to date
git checkout develop
git pull origin develop

# Create branch from develop
git checkout -b feature/us-scr-001-ofac-search

# ... work on the feature ...
```

### 2. During development

```bash
# Atomic, descriptive commits
git add RiskScreening.API/Modules/Scraping/...
git commit -m "feat(scraping): add OFACHttpClient with XML feed parsing"

git add RiskScreening.API/Modules/Scraping/...
git commit -m "test(scraping): add SearchOfacQueryHandler unit tests"

# Update CHANGELOG.md in [Unreleased] before the PR
git add CHANGELOG.md
git commit -m "docs(changelog): add OFAC search endpoint entry"
```

### 3. Open a Pull Request

- **Base branch:** `develop`
- **Title:** `feat(scraping): US-SCR-001 — OFAC SDN search endpoint`
- **PR description must include:**
  - Which User Story it implements (`US-SCR-001`)
  - What technical changes it contains
  - How to test it (curl, Postman, test command)

### 4. Merge requirements

- [ ] `dotnet test` passes without errors
- [ ] No new build warnings
- [ ] `CHANGELOG.md` updated in `[Unreleased]`
- [ ] At least one reviewer approves the PR
- [ ] All PR comments resolved

### 5. Release

```bash
# When develop is ready for release
git checkout develop
git checkout -b release/1.0.0

# Bug fixes only in the release branch — NO new features
# Update CHANGELOG.md: move [Unreleased] to [1.0.0] with date
# Update version in .csproj

# Merge to main
git checkout main
git merge --no-ff release/1.0.0
git tag -a v1.0.0 -m "Release v1.0.0"

# Merge back to develop
git checkout develop
git merge --no-ff release/1.0.0

# Delete release branch
git branch -d release/1.0.0
```

---

## Module structure (Backend)

Each new module under `RiskScreening.API/Modules/` follows this structure:

```
Modules/{ModuleName}/
  Application/
    {SubFolder}/
      {Action}CommandHandler.cs
      {Action}CommandValidator.cs    (if applicable)
      {Action}QueryHandler.cs
    Ports/
      I{Name}Repository.cs
      I{Name}Service.cs
  Domain/
    Exceptions/
    Model/
      Aggregates/
      Commands/
      Queries/
      ValueObjects/
  Infrastructure/
    Persistence/
      {Name}Repository.cs
      Configurations/
        {Name}Configuration.cs
    Extensions/
      ServiceCollectionExtensions.cs
  Interfaces/
    REST/
      Controllers/
      Documentation/
      Mappers/
        Request/
        Response/
      Resources/
        Requests/
        Responses/
```

---

## SQL Migrations

1. Create `Migrations/Scripts/V00N__description_snake_case.sql` (next number in sequence)
2. Scripts should be idempotent where possible
3. Never modify an already-executed script — create a new one
4. The runner (`DatabaseMigrator`) executes them automatically at API startup

---

## File naming conventions

### Backend (.NET)

| Artifact | Convention | Example |
|----------|-----------|---------|
| Commands | `{Action}Command.cs` | `CreateSupplierCommand.cs` |
| Queries | `{Action}Query.cs` | `GetAllSuppliersQuery.cs` |
| Handlers | `{Action}CommandHandler.cs` | `CreateSupplierCommandHandler.cs` |
| Validators | `{Action}CommandValidator.cs` | `CreateSupplierCommandValidator.cs` |
| Controllers | `{Name}Controller.cs` | `SuppliersController.cs` |
| Repositories | `I{Name}Repository.cs` + `{Name}Repository.cs` | `ISupplierRepository.cs` |
| Migrations | `V{NNN}__{description}.sql` | `V005__create_suppliers_table.sql` |

---

## Tests

- Unit tests in `RiskScreening.UnitTests/`
- Use the **Object Mother / Builder** pattern for test data (`Mothers/`, `Builders/`)
- Test naming: `{Class}_{Method}_{Scenario}`
  - Example: `User_RecordFailedLogin_LocksAccountAt5thAttempt`
- One assertion per test (preferred)
- Run: `dotnet test`

---

## ADRs (Architecture Decision Records)

- Create an ADR **before implementing** when the decision affects structure, dependencies, NFRs, or interfaces
- Location: `docs/adr/NNNN-title-kebab-case.md`
- ADRs are **immutable** — if the decision changes, create a new ADR and mark the previous one as `Superseded by ADR-XXXX`
- Never delete an existing ADR

---

## Updating the CHANGELOG

- Update `CHANGELOG.md` in the `[Unreleased]` section with each merge to `develop`
- When creating a `release/X.Y.Z`, move the `[Unreleased]` content to the new version with its date
- Do not document every commit — document notable changes from the user's or API consumer's perspective
- Change types: `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, `Security`
