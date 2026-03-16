# ADR-0002: CQRS with MediatR and Pipeline Behaviors

## Status
`Accepted`

## Date
2026-03-13

## Context

The Application layer needs a mechanism to separate read operations (Queries) from write operations (Commands), apply cross-cutting behaviors (logging, validation) consistently, and keep handlers decoupled from controllers.

The following options were evaluated:

| Option | Description |
|--------|-------------|
| **MediatR + CQRS** | Each use case is a Command or Query, a Handler, with Pipeline Behaviors |
| **Direct Application Services** | Services with methods, injected directly into controllers |
| **Vertical Slice Architecture** | Each feature is a self-contained file |

## Decision

Use **CQRS** with **MediatR 12** as the mediator. The behavior pipeline executes in this order:

```
Request
  |
  v
LoggingBehavior         -- Logs start/end of each Command/Query
  |
  v
ValidationBehavior      -- Runs all IValidator<TRequest> via FluentValidation
  |
  v
Handler                 -- Business logic for the use case
  |
  v
Response
```

Artifact naming:
- Write: `{Action}Command` + `{Action}CommandHandler` + `{Action}CommandValidator`
- Read: `{Action}Query` + `{Action}QueryHandler`
- Location: `Modules/{Module}/Application/{SubFolder}/`

Example from the IAM module:
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

## Consequences

**Positive:**
- Thin controllers — they only deserialize requests and map responses
- Consistent validation: FluentValidation runs automatically for every Command that has a validator
- Automatic logging of all use cases without repetitive code
- Handlers are easy to test in isolation (they only depend on their ports/repos)
- Clear traceability: one Command = one use case = one Handler

**Negative:**
- Additional indirection (Request -> Mediator -> Handler) can complicate debugging
- More files per feature compared to simple Application Services
- FluentValidation in the pipeline throws `ValidationException` which must be mapped to HTTP 400

**Mitigation:**
- `GlobalExceptionHandler` maps `ValidationException` -> 400, `DomainExceptions` -> 409/404/401
- MediatR is the industry standard in .NET — reviewers will be familiar with the pattern

## References
- [MediatR — Jimmy Bogard](https://github.com/jbogard/MediatR)
- [CQRS Pattern — Martin Fowler](https://martinfowler.com/bliki/CQRS.html)
- [FluentValidation — MediatR Integration](https://docs.fluentvalidation.net/en/latest/aspnet.html)
