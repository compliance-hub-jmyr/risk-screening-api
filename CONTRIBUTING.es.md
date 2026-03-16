# Guia de Contribucion

## Repositorios

Este proyecto esta compuesto por dos repositorios independientes:

| Repositorio | Stack | Descripcion |
|-------------|-------|-------------|
| `risk-screening-api` | .NET 10 / ASP.NET Core | Backend REST API (este repositorio) |
| `risk-screening-app` | Angular 21 / TypeScript | Frontend SPA (repositorio separado) |

Cada repositorio tiene su propio `CHANGELOG.md`, `CONTRIBUTING.md` y ciclo de versionado. Esta guia cubre unicamente el repositorio del **backend API**.

---

## Configuración para Desarrollo Local

### Prerrequisitos

- .NET 10 SDK
- Docker Desktop (para SQL Server via Compose)
- Node.js 22+ (solo necesario para ejecutar el script `playwright.ps1`)

### 1. Iniciar la base de datos

```bash
docker compose up -d sqlserver
```

### 2. Configurar el entorno

```bash
cp .env.example .env
# Editar .env con los valores locales (JWT key, admin password, etc.)
```

### 3. Instalar browsers de Playwright

El módulo de scraping ICIJ usa **Microsoft Playwright** (Chromium headless). El binario del browser debe instalarse una vez al clonar el repositorio, y nuevamente cada vez que se actualice el paquete NuGet `Microsoft.Playwright`:

```bash
# Desde la raíz del repositorio (Git Bash)
powershell RiskScreening.API/bin/Debug/net10.0/playwright.ps1 install chromium
```

> **Cuándo volver a ejecutarlo:** Si la API lanza `PlaywrightException: Executable doesn't exist at ...chromium-XXXX\chrome-win64\chrome.exe` al iniciar, el paquete fue actualizado a una nueva revisión del browser. Ejecutar el comando anterior para descargarlo.
>
> **Producción:** No es necesario — el `Dockerfile` instala Chromium durante el build de la imagen.

### 4. Ejecutar la API

```bash
dotnet run --project RiskScreening.API
```

La API estará disponible en `http://localhost:5215`. Swagger UI en `http://localhost:5215/swagger`.

---

## Gitflow

Este proyecto sigue el modelo **Gitflow** estandar:

```
main          <- solo releases estables con tag (v1.0.0, v1.1.0)
  |
develop       <- integracion — todos los features se mergean aqui primero
  |
  |-- feature/us-iam-001-sign-in          <- una rama por User Story
  |-- feature/us-scr-001-ofac-search
  |-- feature/us-sup-001-create-supplier
  |-- bugfix/fix-jwt-expiry-claim
  |-- release/1.0.0                       <- rama de preparacion de release
  `-- hotfix/critical-auth-bypass         <- solo desde main, merge a main Y develop
```

### Tipos de ramas (Gitflow estandar)

| Tipo | Patron | Origen | Merge hacia | Cuando usarla |
|------|--------|--------|-------------|---------------|
| `feature/` | `feature/us-XXX-descripcion` | `develop` | `develop` | Nueva funcionalidad (User Story o Technical Story) |
| `bugfix/` | `bugfix/descripcion-corta` | `develop` | `develop` | Bug detectado en desarrollo |
| `release/` | `release/X.Y.Z` | `develop` | `main` + `develop` | Preparar un release — solo bug fixes, no features |
| `hotfix/` | `hotfix/descripcion-corta` | `main` | `main` + `develop` | Bug critico en produccion que no puede esperar |

> No uses `chore/` ni `test/` como nombre de rama — esos son tipos de **commit**, no de rama.
> Una rama de tests va como `feature/test-iam-coverage` o directamente en la feature que los genera.

---

## Conventional Commits

Todos los commits deben seguir el estandar [Conventional Commits v1.0.0](https://www.conventionalcommits.org/en/v1.0.0/):

```
<tipo>(<alcance>): <descripcion>

[cuerpo opcional]

[pie opcional: BREAKING CHANGE, Closes #issue]
```

### Tipos de commit permitidos

| Tipo | Cuando usarlo | Aparece en CHANGELOG |
|------|--------------|----------------------|
| `feat` | Nueva funcionalidad | Si — bajo `Added` |
| `fix` | Correccion de bug | Si — bajo `Fixed` |
| `refactor` | Cambio de codigo sin nueva funcionalidad ni fix | No |
| `test` | Agregar o corregir tests | No |
| `docs` | Solo documentacion (README, ADRs, user stories) | No |
| `chore` | Mantenimiento, config, dependencias | No |
| `perf` | Mejora de performance | Si — bajo `Changed` |
| `ci` | Cambios en pipelines CI/CD | No |
| `build` | Cambios en sistema de build, dependencias | No |

### Ejemplos correctos

```bash
feat(iam): add UsersController with CRUD endpoints
feat(scraping): add OFAC SDN search endpoint
fix(auth): handle locked accounts in sign-in flow
refactor(shared): extract pagination logic to PageableExtensions
test(iam): add CreateRoleCommandHandler unit tests
docs(adr): add ADR-0007 Angular framework decision
chore(deps): upgrade MediatR to 12.3.0
```

### Ejemplos incorrectos

```bash
# MAL — no sigue el formato
update stuff
fixed bug
WIP
added feature

# MAL — scope inexistente
feat(random): add thing
```

---

## Flujo de trabajo paso a paso

### 1. Iniciar una nueva User Story

```bash
# Asegurarte de tener develop actualizado
git checkout develop
git pull origin develop

# Crear rama desde develop
git checkout -b feature/us-scr-001-ofac-search

# ... trabajar en la feature ...
```

### 2. Durante el desarrollo

```bash
# Commits atomicos y descriptivos
git add RiskScreening.API/Modules/Scraping/...
git commit -m "feat(scraping): add OFACHttpClient with XML feed parsing"

git add RiskScreening.API/Modules/Scraping/...
git commit -m "test(scraping): add SearchOfacQueryHandler unit tests"

# Actualizar CHANGELOG.md en [Unreleased] antes del PR
git add CHANGELOG.md
git commit -m "docs(changelog): add OFAC search endpoint entry"
```

### 3. Abrir Pull Request

- **Base branch:** `develop`
- **Titulo:** `feat(scraping): US-SCR-001 — OFAC SDN search endpoint`
- **Descripcion del PR debe incluir:**
  - Que User Story implementa (`US-SCR-001`)
  - Que cambios tecnicos contiene
  - Como probar (curl, Postman, test command)

### 4. Requisitos para merge

- [ ] `dotnet test` pasa sin errores
- [ ] No hay warnings de build nuevos
- [ ] `CHANGELOG.md` actualizado en `[Unreleased]`
- [ ] Al menos un reviewer aprueba el PR
- [ ] Todos los comentarios del PR resueltos

### 5. Release

```bash
# Cuando develop esta listo para release
git checkout develop
git checkout -b release/1.0.0

# Solo bug fixes en la rama release — NO nuevas features
# Actualizar CHANGELOG.md: mover [Unreleased] a [1.0.0] con fecha
# Actualizar version en .csproj

# Merge a main
git checkout main
git merge --no-ff release/1.0.0
git tag -a v1.0.0 -m "Release v1.0.0"

# Merge back a develop
git checkout develop
git merge --no-ff release/1.0.0

# Eliminar rama release
git branch -d release/1.0.0
```

---

## Estructura de modulos (Backend)

Cada nuevo modulo en `RiskScreening.API/Modules/` sigue esta estructura:

```
Modules/{NombreModulo}/
  Application/
    {SubCarpeta}/
      {Accion}CommandHandler.cs
      {Accion}CommandValidator.cs    (si aplica)
      {Accion}QueryHandler.cs
    Ports/
      I{Nombre}Repository.cs
      I{Nombre}Service.cs
  Domain/
    Exceptions/
    Model/
      Aggregates/
      Commands/
      Queries/
      ValueObjects/
  Infrastructure/
    Persistence/
      {Nombre}Repository.cs
      Configurations/
        {Nombre}Configuration.cs
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

## Migraciones SQL

1. Crear `Migrations/Scripts/V00N__descripcion_snake_case.sql` (siguiente numero en secuencia)
2. El script debe ser idempotente cuando sea posible
3. Nunca modificar un script ya ejecutado — crear uno nuevo
4. El runner (`DatabaseMigrator`) los ejecuta automaticamente al iniciar la API

---

## Nomenclatura de archivos

### Backend (.NET)

| Artefacto | Convencion | Ejemplo |
|-----------|-----------|---------|
| Commands | `{Accion}Command.cs` | `CreateSupplierCommand.cs` |
| Queries | `{Accion}Query.cs` | `GetAllSuppliersQuery.cs` |
| Handlers | `{Accion}CommandHandler.cs` | `CreateSupplierCommandHandler.cs` |
| Validators | `{Accion}CommandValidator.cs` | `CreateSupplierCommandValidator.cs` |
| Controllers | `{Nombre}Controller.cs` | `SuppliersController.cs` |
| Repositories | `I{Nombre}Repository.cs` + `{Nombre}Repository.cs` | `ISupplierRepository.cs` |
| Migrations | `V{NNN}__{descripcion}.sql` | `V005__create_suppliers_table.sql` |

---

## Tests

- Tests unitarios en `RiskScreening.UnitTests/`
- Usar patron **Object Mother / Builder** para datos de prueba (`Mothers/`, `Builders/`)
- Nombrar tests: `{Clase}_{Metodo}_{Escenario}`
  - Ejemplo: `User_RecordFailedLogin_LocksAccountAt5thAttempt`
- Un assert por test (preferencia)
- Ejecutar: `dotnet test`

---

## ADRs (Architecture Decision Records)

- Crear un ADR **antes de implementar** cuando la decision afecte estructura, dependencias, NFRs o interfaces
- Ubicacion: `docs/adr/NNNN-titulo-kebab-case.md`
- Los ADRs son **inmutables** — si la decision cambia, crea un nuevo ADR y marca el anterior como `Superseded by ADR-XXXX`
- Nunca eliminar un ADR existente

---

## Actualizacion del CHANGELOG

- Actualizar `CHANGELOG.md` en la seccion `[Unreleased]` al hacer cada merge a `develop`
- Al crear un `release/X.Y.Z`, mover el contenido de `[Unreleased]` a la nueva version con fecha
- No documentar cada commit — documentar cambios notables desde la perspectiva del usuario o del API consumer
- Tipos de cambio: `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, `Security`
