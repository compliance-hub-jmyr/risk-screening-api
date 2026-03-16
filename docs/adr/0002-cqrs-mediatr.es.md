# ADR-0002: CQRS con MediatR y Pipeline Behaviors

## Estado
`Accepted`

## Fecha
2026-03-13

## Contexto

La capa Application necesita un mecanismo para separar las operaciones de lectura (Queries) de las de escritura (Commands), aplicar comportamientos transversales (logging, validacion) de forma consistente, y mantener los handlers desacoplados de los controllers.

Se evaluaron estas opciones:

| Opcion | Descripcion |
|--------|-------------|
| **MediatR + CQRS** | Cada use case es un Command o Query, un Handler, con Pipeline Behaviors |
| **Application Services directos** | Servicios con metodos, inyectados directamente en controllers |
| **Vertical Slice Architecture** | Cada feature es un archivo self-contained |

## Decision

Usar **CQRS** con **MediatR 12** como mediador. El pipeline de behaviors se ejecuta en este orden:

```
Request
  |
  v
LoggingBehavior         -- Loggea inicio/fin de cada Command/Query
  |
  v
ValidationBehavior      -- Ejecuta todos los IValidator<TRequest> via FluentValidation
  |
  v
Handler                 -- Logica de negocio del use case
  |
  v
Response
```

Nomenclatura de artefactos:
- Escritura: `{Accion}Command` + `{Accion}CommandHandler` + `{Accion}CommandValidator`
- Lectura: `{Accion}Query` + `{Accion}QueryHandler`
- Ubicacion: `Modules/{Modulo}/Application/{SubCarpeta}/`

Ejemplo del modulo IAM:
```
Application/
  Authentication/
    SignInCommandHandler.cs
    SignInCommandValidator.cs
    GetCurrentUserQueryHandler.cs
  UserManagement/
    ActivateUserCommandHandler.cs
    DeleteUserCommandHandler.cs
    GetAllUsersQueryHandler.cs
  RoleManagement/
    CreateRoleCommandHandler.cs
    CreateRoleCommandValidator.cs
    GetAllRolesQueryHandler.cs
```

## Consecuencias

**Positivo:**
- Controllers delgados — solo deserializan requests y mapean responses
- Validacion consistente: FluentValidation corre automaticamente para todo Command con validator
- Logging automatico de todos los use cases sin codigo repetitivo
- Handlers son faciles de testear en aislamiento (solo dependen de sus ports/repos)
- Trazabilidad clara: un Command = un use case = un Handler

**Negativo:**
- Indirection adicional (Request -> Mediator -> Handler) puede dificultar debugging
- Mas archivos por feature comparado con Application Services simples
- FluentValidation en pipeline lanza `ValidationException` que debe ser mapeada a HTTP 400

**Mitigacion:**
- `GlobalExceptionHandler` mapea `ValidationException` -> 400, `DomainExceptions` -> 409/404/401
- MediatR es standard de la industria .NET — los revisores estaran familiarizados con el patron

## Referencias
- [MediatR — Jimmy Bogard](https://github.com/jbogard/MediatR)
- [CQRS Pattern — Martin Fowler](https://martinfowler.com/bliki/CQRS.html)
- [FluentValidation — MediatR Integration](https://docs.fluentvalidation.net/en/latest/aspnet.html)
