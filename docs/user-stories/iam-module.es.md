# User Stories — Modulo IAM (Identity & Access Management)

> **Formato:** Titulo / Descripcion / Entregable / Dependencias / Criterios de Aceptacion (BDD Given/When/Then).
> **Tags de tareas:** `[BE-DOMAIN]` `[BE-APP]` `[BE-INFRA]` `[BE-INTERFACES]` `[BE-DB]` `[BE-TEST]` `[DOCS]`

---

## Historia Tecnica: Bootstrapping del Modulo IAM

---

### TS-IAM-000: Setup Inicial del Modulo IAM

**Titulo:** Scripts de base de datos, configuracion EF Core y seeder IAM

**Descripcion:**
Como desarrollador, necesito configurar la infraestructura fundamental del modulo IAM — tablas de base de datos, configuraciones de entidad EF Core, abstracciones del shared kernel y el seeder de arranque — para que todas las historias de usuario IAM tengan una base estable sobre la que construir.

**Entregable:**
Scripts de migracion SQL V001–V003, configuraciones EF Core para `User`, `Role` y `UserRole`, clases base del shared kernel y un `IamSeeder` que siembra los roles de sistema `ADMIN` y `ANALYST` mas un usuario administrador por defecto al arrancar la aplicacion.

**Dependencias:**
- Ninguna — esta es la tarea fundacional del modulo IAM.

**Prioridad:** Critica | **Estimacion:** 3 SP | **Estado:** Implementado (v0.2.0)

#### Tareas

- `[BE-DB]` Script `V001__create_roles_table.sql` — columnas: `id`, `name`, `description`, `is_system_role`, `created_at`
- `[BE-DB]` Script `V002__create_users_table.sql` — columnas: `id`, `email`, `username`, `password_hash`, `account_status`, `failed_login_attempts`, `created_at`, `last_login_at`
- `[BE-DB]` Script `V003__create_user_roles_table.sql` — tabla de union: `user_id`, `role_id`
- `[BE-INFRA]` EF Core `IEntityTypeConfiguration<User>`, `IEntityTypeConfiguration<Role>`, `IEntityTypeConfiguration<UserRole>`
- `[BE-INFRA]` `IamSeeder` — siembra el rol `ADMIN`, el rol `ANALYST` y un usuario administrador por defecto con password hasheado en `IHostedService.StartAsync`
- `[BE-APP]` Shared kernel: clases base `AggregateRoot`, `ValueObject`, `DomainException`; interfaz `IUnitOfWork`

#### Criterios de Aceptacion

- Given que la aplicacion arranca por primera vez contra una base de datos vacia
- When la secuencia de arranque se completa
- Then la tabla `roles` contiene exactamente dos roles de sistema: `ADMIN` y `ANALYST`, ambos con `is_system_role = true`
- And la tabla `users` contiene un usuario administrador por defecto con `account_status = ACTIVE`
- And arranques posteriores son idempotentes (el seeder no crea duplicados)

---

## Epica: Autenticacion

---

### US-IAM-001: Iniciar sesion con email y password

**Titulo:** Autenticacion de usuario con credenciales

**Descripcion:**
Como usuario de la plataforma (oficial de compliance o administrador), quiero iniciar sesion con mi email y password, para acceder a la plataforma con mi sesion personalizada y obtener un token JWT de acceso.

**Entregable:**
Endpoint `POST /api/authentication/sign-in` que valida credenciales, registra intentos fallidos, bloquea la cuenta tras 5 fallos consecutivos y retorna un JWT Bearer con duracion configurable.

**Dependencias:**
- `TS-IAM-000`: Setup inicial del modulo IAM (tablas, shared kernel, EF Core)

**Prioridad:** Alta | **Estimacion:** 3 SP | **Estado:** Implementado (v0.2.0)

#### Tareas

- `[BE-DOMAIN]` Agregado `User` con metodos `EnsureCanLogin()`, `RecordFailedLogin()`, `RecordSuccessfulLogin()`
- `[BE-DOMAIN]` Value objects: `Email`, `Username`, `Password` (hash + verify), `AccountStatus`
- `[BE-DOMAIN]` Excepciones de dominio: `InvalidCredentialsException`, `AccountLockedException`
- `[BE-APP]` `SignInCommand` + `SignInCommandHandler` (MediatR)
- `[BE-APP]` `SignInCommandValidator` (FluentValidation — formato email, password requerido)
- `[BE-INFRA]` `JwtTokenService.GenerateToken(User)` — claims: sub, email, name, roles
- `[BE-INFRA]` `BCryptPasswordHasher.Verify(plain, hash)`
- `[BE-INFRA]` `UserRepository.FindByEmailAsync(email)` con roles incluidos
- `[BE-INTERFACES]` `AuthenticationController.SignIn` — mapea `SignInRequest` → `SignInCommand`
- `[BE-TEST]` Unit test: `SignInCommandHandlerTests` — login exitoso, credenciales incorrectas, cuenta bloqueada, cuenta suspendida

#### Criterios de Aceptacion

**Escenario 1: Login exitoso**
- Given que soy un usuario registrado con cuenta activa
- And envio `POST /api/authentication/sign-in` con email y password correctos
- When la peticion se procesa
- Then recibo HTTP 200 con `{ token, email, username, roles }`
- And el token JWT es valido por 24 horas

**Escenario 2: Credenciales incorrectas**
- Given que envio email valido pero password incorrecto
- When la peticion se procesa
- Then recibo HTTP 401 Unauthorized
- And se incrementa el contador de intentos fallidos en 1

**Escenario 3: Cuenta bloqueada por intentos fallidos**
- Given que el usuario ha fallado 5 intentos consecutivos de login
- When intenta hacer login nuevamente
- Then recibo HTTP 401 con mensaje de cuenta bloqueada
- And la cuenta permanece en estado `LOCKED` hasta ser desbloqueada por un ADMIN

**Escenario 4: Cuenta suspendida**
- Given que el administrador ha suspendido la cuenta del usuario
- When el usuario intenta hacer login
- Then recibo HTTP 401 con mensaje de cuenta suspendida

**Escenario 5: Campos vacios o invalidos**
- Given que envio el request sin el campo `email` o con formato de email invalido
- When la peticion llega al pipeline de validacion
- Then recibo HTTP 400 Bad Request con listado de errores de validacion

**Escenario 6: Usuario no encontrado**
- Given que envio un email que no existe en la base de datos
- When la peticion se procesa
- Then recibo HTTP 401 Unauthorized (sin revelar si el email existe o no, por seguridad)

---

### US-IAM-002: Obtener perfil del usuario autenticado

**Titulo:** Consulta del perfil del usuario en sesion

**Descripcion:**
Como usuario autenticado, quiero consultar mi informacion de perfil actual, para que la SPA pueda mostrar mi nombre, roles y datos de sesion en la cabecera de navegacion.

**Entregable:**
Endpoint `GET /api/authentication/me` protegido por JWT Bearer que retorna el perfil del usuario extraido del token.

**Dependencias:**
- US-IAM-001 (sign-in implementado y JWT valido)

**Prioridad:** Alta | **Estimacion:** 1 SP | **Estado:** Implementado (v0.2.0)

#### Tareas

- `[BE-APP]` `GetCurrentUserQuery` + `GetCurrentUserQueryHandler` — extrae userId del claim `sub`
- `[BE-INFRA]` `UserRepository.FindByIdAsync(userId)` con roles
- `[BE-INTERFACES]` `AuthenticationController.Me` — `[Authorize]`, extrae claim y delega al handler
- `[BE-TEST]` Unit test: token valido retorna perfil, token ausente retorna 401

#### Criterios de Aceptacion

**Escenario 1: Token valido**
- Given que tengo un JWT Bearer token activo en el header `Authorization`
- When llamo `GET /api/authentication/me`
- Then recibo HTTP 200 con mi perfil `{ email, username, roles, token }`

**Escenario 2: Token ausente o expirado**
- Given que no incluyo el header `Authorization` o el token esta expirado
- When hago la peticion
- Then recibo HTTP 401 Unauthorized

---

## Epica: Gestion de Usuarios (solo ADMIN)

---

### US-IAM-003: Listar todos los usuarios

**Titulo:** Listado paginado de usuarios del sistema

**Descripcion:**
Como administrador, quiero ver el listado paginado de todos los usuarios registrados, para poder gestionar sus cuentas y estados desde el panel de administracion.

**Entregable:**
Endpoint `GET /api/users` paginado, con soporte de ordenamiento por campo y direccion, restringido al rol `ADMIN`.

**Dependencias:**
- US-IAM-001 (auth implementada)
- Scripts SQL V001–V003

**Prioridad:** Media | **Estimacion:** 2 SP | **Estado:** Implementado (v0.3.0)

#### Tareas

- `[BE-APP]` `GetUsersQuery` (page, size, sortBy, sortDirection) + `GetUsersQueryHandler`
- `[BE-INFRA]` `UserRepository.FindAllAsync(query)` con paginacion EF Core
- `[BE-INTERFACES]` `UsersController.GetAll` — `[Authorize(Roles = "ADMIN")]`
- `[BE-TEST]` Unit test: listado paginado, ordenamiento, acceso sin ADMIN retorna 403

#### Criterios de Aceptacion

**Escenario 1: Listado exitoso con paginacion**
- Given que soy ADMIN autenticado
- When llamo `GET /api/users?page=1&size=10`
- Then recibo HTTP 200 con `{ content: [...], page: { number, size, totalElements, totalPages } }`
- And cada usuario incluye `{ id, email, username, status, roles, createdAt, lastLoginAt }`

**Escenario 2: Filtrado y ordenamiento**
- Given que llamo `GET /api/users?sortBy=email&sortDirection=asc`
- When la peticion se procesa
- Then recibo los usuarios ordenados por email ascendente

**Escenario 3: Sin permisos de ADMIN**
- Given que soy un usuario con rol `ANALYST` autenticado
- When llamo `GET /api/users`
- Then recibo HTTP 403 Forbidden

**Escenario 4: Sin autenticacion**
- Given que no incluyo token JWT
- When llamo `GET /api/users`
- Then recibo HTTP 401 Unauthorized

---

### US-IAM-004: Obtener usuario por ID

**Titulo:** Consulta del perfil completo de un usuario por ID

**Descripcion:**
Como administrador, quiero ver el perfil completo de un usuario especifico, para revisar su estado de cuenta, roles asignados e historial de accesos.

**Entregable:**
Endpoint `GET /api/users/{id}` que retorna el perfil completo del usuario.

**Dependencias:**
- US-IAM-003

**Prioridad:** Media | **Estimacion:** 1 SP | **Estado:** Implementado (v0.3.0)

#### Tareas

- `[BE-APP]` `GetUserByIdQuery` + `GetUserByIdQueryHandler` — lanza `UserNotFoundException` si no existe
- `[BE-INTERFACES]` `UsersController.GetById` — mapea 404 via `GlobalExceptionHandler`
- `[BE-TEST]` Unit test: usuario encontrado, usuario no encontrado retorna 404

#### Criterios de Aceptacion

**Escenario 1: Usuario encontrado**
- Given que soy ADMIN y el userId existe
- When llamo `GET /api/users/{userId}`
- Then recibo HTTP 200 con el perfil completo del usuario

**Escenario 2: Usuario no encontrado**
- Given que el userId no existe en la base de datos
- When llamo `GET /api/users/{userId}`
- Then recibo HTTP 404 Not Found con mensaje descriptivo

---

### US-IAM-005: Eliminar usuario (soft delete)

**Titulo:** Eliminacion logica de usuario

**Descripcion:**
Como administrador, quiero eliminar logicamente la cuenta de un usuario, para que exempleados o usuarios no autorizados no puedan seguir accediendo a la plataforma sin perder el registro de auditoria.

**Entregable:**
Endpoint `DELETE /api/users/{id}` que cambia el `AccountStatus` del usuario a `DELETED` (soft delete).

**Dependencias:**
- US-IAM-003

**Prioridad:** Alta | **Estimacion:** 2 SP | **Estado:** Implementado (v0.3.0)

#### Tareas

- `[BE-DOMAIN]` Metodo `User.Delete()` — cambia status a `DELETED`, valida que no este ya eliminado
- `[BE-APP]` `DeleteUserCommand` + `DeleteUserCommandHandler`
- `[BE-INTERFACES]` `UsersController.Delete` — retorna 204
- `[BE-TEST]` Unit test: eliminacion exitosa, usuario ya eliminado retorna 404

#### Criterios de Aceptacion

**Escenario 1: Eliminacion exitosa**
- Given que soy ADMIN y el usuario con ese userId existe y no esta eliminado
- When llamo `DELETE /api/users/{userId}`
- Then recibo HTTP 204 No Content
- And el campo `account_status` del usuario se cambia a `DELETED` (soft delete)
- And el usuario no puede hacer login

**Escenario 2: Usuario ya eliminado**
- Given que el usuario ya tiene status `DELETED`
- When llamo `DELETE /api/users/{userId}`
- Then recibo HTTP 404 Not Found

**Escenario 3: Usuario no encontrado**
- Given que el userId no existe
- When llamo `DELETE /api/users/{userId}`
- Then recibo HTTP 404 Not Found

---

### US-IAM-006: Activar usuario suspendido o bloqueado

**Titulo:** Reactivacion de cuenta de usuario

**Descripcion:**
Como administrador, quiero reactivar la cuenta de un usuario suspendido o bloqueado, para que usuarios legitimos puedan recuperar acceso a la plataforma.

**Entregable:**
Endpoint `PATCH /api/users/{id}/activate` que cambia el `AccountStatus` a `ACTIVE` y resetea el contador de intentos fallidos.

**Dependencias:**
- US-IAM-005

**Prioridad:** Alta | **Estimacion:** 1 SP | **Estado:** Implementado (v0.3.0)

#### Tareas

- `[BE-DOMAIN]` Metodo `User.Activate()` — cambia status a `ACTIVE`, resetea `FailedLoginAttempts`
- `[BE-APP]` `ActivateUserCommand` + `ActivateUserCommandHandler`
- `[BE-INTERFACES]` `UsersController.Activate`
- `[BE-TEST]` Unit test: activacion exitosa, usuario ya activo es idempotente (retorna 204 sin error)

#### Criterios de Aceptacion

**Escenario 1: Activacion exitosa**
- Given que soy ADMIN y el usuario tiene status `SUSPENDED` o `LOCKED`
- When llamo `PATCH /api/users/{userId}/activate`
- Then recibo HTTP 204 No Content
- And el usuario puede volver a hacer login

**Escenario 2: Usuario ya activo (idempotente)**
- Given que el usuario ya tiene status `ACTIVE`
- When llamo `PATCH /api/users/{userId}/activate`
- Then recibo HTTP 204 No Content

---

### US-IAM-007: Suspender usuario activo

**Titulo:** Suspension temporal de cuenta de usuario

**Descripcion:**
Como administrador, quiero suspender temporalmente la cuenta de un usuario activo, para restringir su acceso sin eliminar permanentemente la cuenta.

**Entregable:**
Endpoint `PATCH /api/users/{id}/suspend` que cambia el `AccountStatus` a `SUSPENDED`.

**Dependencias:**
- US-IAM-006

**Prioridad:** Alta | **Estimacion:** 1 SP | **Estado:** Implementado (v0.3.0)

#### Tareas

- `[BE-DOMAIN]` Metodo `User.Suspend()` — cambia status a `SUSPENDED`
- `[BE-APP]` `SuspendUserCommand` + `SuspendUserCommandHandler`
- `[BE-INTERFACES]` `UsersController.Suspend`
- `[BE-TEST]` Unit test: suspension exitosa, usuario no encontrado retorna 404

#### Criterios de Aceptacion

**Escenario 1: Suspension exitosa**
- Given que soy ADMIN y el usuario tiene status `ACTIVE`
- When llamo `PATCH /api/users/{userId}/suspend`
- Then recibo HTTP 204 No Content
- And el usuario no puede hacer login (recibe 401)

**Escenario 2: Usuario no encontrado**
- Given que el userId no existe
- When llamo `PATCH /api/users/{userId}/suspend`
- Then recibo HTTP 404 Not Found

---

### US-IAM-008: Asignar rol a usuario

**Titulo:** Asignacion de rol a cuenta de usuario

**Descripcion:**
Como administrador, quiero asignar un rol a un usuario, para otorgarle los permisos asociados a ese rol dentro de la plataforma.

**Entregable:**
Endpoint `POST /api/users/{id}/roles` que crea la relacion `user_roles` entre el usuario y el rol indicado.

**Dependencias:**
- US-IAM-003
- Los roles se siembran al arrancar via `TS-IAM-000` — no se requiere creacion dinamica de roles.

**Prioridad:** Alta | **Estimacion:** 2 SP | **Estado:** Implementado (v0.3.0)

#### Tareas

- `[BE-APP]` `AssignRoleCommand` + `AssignRoleCommandHandler` — valida que el rol exista, es idempotente
- `[BE-APP]` `AssignRoleCommandValidator` (FluentValidation — roleName requerido, no vacio)
- `[BE-INFRA]` `RoleRepository.FindByNameAsync(name)`
- `[BE-INTERFACES]` `UsersController.AssignRole`
- `[BE-TEST]` Unit test: asignacion exitosa, rol ya asignado es idempotente, rol no existe retorna 404

#### Criterios de Aceptacion

**Escenario 1: Asignacion exitosa**
- Given que soy ADMIN y envio `POST /api/users/{userId}/roles` con `{ "roleName": "ANALYST" }`
- When la peticion se procesa
- Then recibo HTTP 204 No Content
- And el usuario tiene el rol `ANALYST` en su perfil

**Escenario 2: Rol ya asignado (idempotente)**
- Given que el usuario ya tiene el rol `ANALYST`
- When intento asignarlo nuevamente
- Then recibo HTTP 204 No Content (sin error)

**Escenario 3: Rol no existe**
- Given que envio un roleName que no existe en el sistema
- When la peticion se procesa
- Then recibo HTTP 404 Not Found con mensaje "Role not found"

**Escenario 4: Validacion de campo**
- Given que envio el body sin el campo `roleName` o con valor vacio
- When la peticion llega al pipeline de validacion
- Then recibo HTTP 400 Bad Request

---

### US-IAM-009: Revocar rol de usuario

**Titulo:** Revocacion de rol de cuenta de usuario

**Descripcion:**
Como administrador, quiero revocar un rol de un usuario, para restringir sus permisos cuando cambien sus responsabilidades.

**Entregable:**
Endpoint `DELETE /api/users/{id}/roles/{roleName}` que elimina la relacion `user_roles` entre el usuario y el rol.

**Dependencias:**
- US-IAM-008

**Prioridad:** Alta | **Estimacion:** 1 SP | **Estado:** Implementado (v0.3.0)

#### Tareas

- `[BE-APP]` `RevokeRoleCommand` + `RevokeRoleCommandHandler`
- `[BE-APP]` `RevokeRoleCommandValidator` (FluentValidation — roleName requerido)
- `[BE-INTERFACES]` `UsersController.RevokeRole`
- `[BE-TEST]` Unit test: revocacion exitosa, usuario no encontrado retorna 404

#### Criterios de Aceptacion

**Escenario 1: Revocacion exitosa**
- Given que soy ADMIN y el usuario tiene el rol `ANALYST`
- When llamo `DELETE /api/users/{userId}/roles/ANALYST`
- Then recibo HTTP 204 No Content
- And el usuario ya no tiene el rol `ANALYST`

**Escenario 2: Usuario no encontrado**
- Given que el userId no existe
- When llamo `DELETE /api/users/{userId}/roles/ANALYST`
- Then recibo HTTP 404 Not Found

---

## Fuera de Alcance — v1.0

Las siguientes historias **no estan en alcance para v1.0**. Los roles son roles de sistema fijos (`ADMIN`, `ANALYST`) sembrados al arrancar. La gestion dinamica de roles se difiere a un hito futuro.

### US-IAM-010: Listar todos los roles *(diferido)*

Endpoint `GET /api/roles` que retorna el catalogo de roles. No requerido en v1.0 ya que los roles son sembrados y conocidos en tiempo de diseno.

### US-IAM-011: Crear rol personalizado *(diferido)*

Endpoint `POST /api/roles` para crear roles personalizados con nombres arbitrarios. No requerido en v1.0; solo se soportan `ADMIN` y `ANALYST`.

---

## Notas de Implementacion

| Aspecto | Implementacion |
|---------|---------------|
| Bloqueo de cuenta | Automatico al 5to intento fallido de login (`User.MaxFailedLoginAttempts = 5`) |
| Soft delete | Cambia `AccountStatus` a `DELETED`, nunca elimina el registro fisico |
| Roles del sistema | `ADMIN` y `ANALYST` son `IsSystemRole = true`, sembrados al startup via `TS-IAM-000` |
| Idempotencia | `AssignRole` y `Activate` son idempotentes — llamar multiples veces no causa error |
| Normalizacion | Nombres de roles se normalizan a UPPERCASE en el dominio |
| Estado de implementacion | Todas las US-IAM implementadas en v0.2.0 (auth) y v0.3.0 (users/roles) |
