# ADR-0007: Frontend Framework — Angular 21.2 + PrimeNG 21

## Status
`Accepted`

## Date
2026-03-13

## Context

The Suppliers module requires a SPA (Single Page Application) for compliance officers to manage suppliers and run screenings against high-risk lists. A frontend framework is needed that satisfies the following criteria:

- Productivity: opinionated project structure with routing, reactive forms, and HTTP client included
- Maintainability: CLI, modularity, separation of concerns
- UI components: enterprise-grade component library ready to use
- Team compatibility: familiarity with object-oriented TypeScript, similar to the .NET backend stack
- Testability: integrated testing framework

The evaluated candidates were:

| Criterion | Angular 21.2.x | React 18 + Vite | Vue 3 |
|-----------|----------------|-----------------|-------|
| Opinionated structure | Yes (CLI, standalone components, routing) | No (requires configuration) | Partial |
| Reactive forms | Built-in Reactive Forms | External libraries (RHF, Zod) | Partial |
| HTTP client | Built-in HttpClient | Axios/Fetch (external) | Axios/Fetch (external) |
| Enterprise UI | PrimeNG (advanced table, dialog, toast, filters) | MUI / Ant Design (third-party) | PrimeVue / Vuetify |
| TypeScript OOP | Native (decorators, dependency injection) | Optional, no native DI | Optional |
| Similarity to .NET | High (DI, services, interceptors ≈ middleware) | Low | Medium |
| SSR / Hydration | Built-in Angular SSR | Next.js (separate) | Nuxt (separate) |
| Initial bundle size | Larger | Smaller | Smaller |

For the UI layer, **PrimeNG** was evaluated against **Angular Material**:

| Criterion | PrimeNG | Angular Material |
|-----------|---------|-----------------|
| Table components | `p-table` with built-in filters, sort, pagination | `mat-table` (pagination and sort as separate modules) |
| Dialog / Modal | Configurable `p-dialog` without extra CDK | `MatDialog` via CDK |
| Toast / Notifications | `p-toast` + built-in `MessageService` | Only `MatSnackBar` (limited) |
| Enterprise theming | Pre-built themes + PrimeNG Theme Designer | Material Design (palette only) |
| Filter form | `p-columnFilter`, `p-dropdown`, `p-calendar` out-of-the-box | Requires manual composition |
| Community and docs | Active, abundant examples for business use cases | Active, oriented to Material Design |

## Decision

Use **Angular 21.2.x** with **PrimeNG 21.1.3** as the framework and UI library for the Suppliers module frontend.

### Confirmed Angular stack

| Role | Technology |
|------|-----------|
| Framework | Angular 21.2.x (Standalone Components) |
| UI Components | PrimeNG 21.1.3 |
| Themes | PrimeNG Aura / Lara theme |
| Forms | Angular Reactive Forms |
| Routing | Angular Router |
| HTTP | Angular `HttpClient` with interceptors |
| State | Angular Signals + services with `BehaviorSubject` |
| Testing | Jest + Angular Testing Library |
| Language | TypeScript 5.x |

### Frontend repository structure

The frontend lives in a separate `risk-screening-app` repository, in the same GitHub organization as `risk-screening-api`. This allows:
- Independent versioning of frontend and backend
- Separate CI/CD pipelines
- Independent deployment (SPA on CDN, API on App Service)

```
risk-screening-app/
|-- src/
|   |-- app/
|   |   |-- core/              # Guards, interceptors, auth service
|   |   |-- shared/            # Shared components and pipes
|   |   |-- features/
|   |   |   |-- auth/          # Login page
|   |   |   |-- suppliers/     # List, form, detail
|   |   |   `-- screening/     # Screening modal, history
|   |   `-- app.config.ts      # Providers, routing (Standalone)
|   |-- environments/
|   `-- styles.scss
|-- CHANGELOG.md
|-- CONTRIBUTING.md
`-- README.md
```

### Communication with the backend

- The SPA consumes the same backend REST API via `HttpClient`
- Authentication: JWT Bearer (stored in `localStorage`, attached via `HttpInterceptor`)
- CORS: the backend allows origin `http://localhost:4200` in development

## Consequences

**Positive:**
- Predictable and standardized project structure enforced by the Angular CLI
- `PrimeNG` provides high-quality enterprise components (`p-table`, `p-dialog`, `p-toast`, `p-dropdown`) with less configuration than Angular Material for complex business cases
- PrimeNG's `p-table` includes column filters, sorting, pagination, and row selection without additional dependencies — ideal for the supplier table
- `Reactive Forms` + synchronous/asynchronous validators cover the validation requirements
- Clear separation between the frontend (`risk-screening-app` repository) and the backend (`risk-screening-api`)
- Alignment with the dependency injection pattern already used in the .NET backend

**Negative:**
- Larger initial bundle than React/Vue for a small SPA
- PrimeNG requires `MessageService` configuration at the root provider for the toast system
- Steeper learning curve than React for developers new to Angular

**Mitigation:**
- For the scope of this project (two modules: suppliers + screening) bundle size is irrelevant
- Angular Standalone Components (since Angular 17+) reduce boilerplate compared to NgModules
- PrimeNG documentation is extensive with copy-ready examples

## References
- [Angular Docs — Standalone Components](https://angular.dev/guide/components/importing)
- [PrimeNG](https://primeng.org/)
- [PrimeNG p-table](https://primeng.org/table)
- [PrimeNG p-dialog](https://primeng.org/dialog)
- [Angular Reactive Forms](https://angular.dev/guide/forms/reactive-forms)
- [Angular HttpClient](https://angular.dev/guide/http)
