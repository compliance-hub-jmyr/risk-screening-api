# ADR-0007: Framework Frontend — Angular 21.2 + PrimeNG 21

## Estado
`Accepted`

## Fecha
2026-03-13

## Contexto

El modulo Suppliers requiere una SPA (Single Page Application) para que oficiales de compliance gestionen proveedores y ejecuten screenings contra listas de alto riesgo. Se necesita elegir un framework frontend que satisfaga los siguientes criterios:

- Productividad: estructura de proyecto clara y opinada, con routing, formularios reactivos y HTTP client incluidos
- Mantenibilidad: CLI, modularidad, separacion de responsabilidades
- UI components: biblioteca de componentes enterprise-grade lista para usar
- Compatibilidad con el equipo: familiaridad con TypeScript orientado a objetos, similar al stack .NET del backend
- Testabilidad: framework de testing integrado

Los candidatos evaluados fueron:

| Criterio | Angular 21.2.x | React 18 + Vite | Vue 3 |
|----------|----------------|-----------------|-------|
| Estructura opinada | Si (CLI, standalone components, routing) | No (necesita configuracion) | Parcial |
| Formularios reactivos | Reactive Forms integrado | Librerias externas (RHF, Zod) | Parcial |
| HTTP client | HttpClient integrado | Axios/Fetch (externo) | Axios/Fetch (externo) |
| UI enterprise | PrimeNG (tabla avanzada, dialog, toast, filtros) | MUI / Ant Design (terceros) | PrimeVue / Vuetify |
| TypeScript OOP | Nativo (decoradores, inyeccion de dependencias) | Opcional, sin DI nativo | Opcional |
| Similaridad con .NET | Alta (DI, servicios, interceptors ≈ middleware) | Baja | Media |
| SSR / Hydration | Angular SSR integrado | Next.js (separado) | Nuxt (separado) |
| Bundle size inicial | Mayor | Menor | Menor |

Para la capa de UI se evaluo **PrimeNG** frente a **Angular Material**:

| Criterio | PrimeNG | Angular Material |
|----------|---------|-----------------|
| Componentes de tabla | `p-table` con filtros, sort, pagination integrados | `mat-table` (paginacion y sort como modulos separados) |
| Dialog / Modal | `p-dialog` configurable sin CDK adicional | `MatDialog` via CDK |
| Toast / Notifications | `p-toast` + `MessageService` integrado | Solo `MatSnackBar` (limitado) |
| Theming enterprise | Temas preconstruidos + PrimeNG Theme Designer | Material Design (solo paletas) |
| Formulario de filtro | `p-columnFilter`, `p-dropdown`, `p-calendar` out-of-the-box | Requiere composicion manual |
| Comunidad y docs | Activa, ejemplos abundantes para casos de negocio | Activa, orientada a Material Design |

## Decision

Usar **Angular 21.2.x** con **PrimeNG 21.1.3** como framework y biblioteca de UI para el frontend del modulo Suppliers.

### Stack Angular confirmado

| Rol | Tecnologia |
|-----|-----------|
    | Framework | Angular 21.2.x (Standalone Components) |
    | UI Components | PrimeNG 21.1.3 |
| Temas | PrimeNG Aura / Lara theme |
| Formularios | Angular Reactive Forms |
| Routing | Angular Router |
| HTTP | Angular `HttpClient` con interceptors |
| Estado | Angular Signals + servicios con `BehaviorSubject` |
| Testing | Jest + Angular Testing Library |
| Language | TypeScript 5.x |

### Estructura del repositorio frontend

El frontend vive en un repositorio separado `risk-screening-app`, en la misma organizacion GitHub que `risk-screening-api`. Esto permite:
- Versionado independiente del frontend y el backend
- Pipelines CI/CD separados
- Despliegue independiente (SPA en CDN, API en App Service)

```
risk-screening-app/
|-- src/
|   |-- app/
|   |   |-- core/              # Guards, interceptors, auth service
|   |   |-- shared/            # Componentes y pipes compartidos
|   |   |-- features/
|   |   |   |-- auth/          # Login page
|   |   |   |-- suppliers/     # Listado, formulario, detalle
|   |   |   `-- screening/     # Modal de screening, historial
|   |   `-- app.config.ts      # Providers, routing (Standalone)
|   |-- environments/
|   `-- styles.scss
|-- CHANGELOG.md
|-- CONTRIBUTING.md
`-- README.md
```

### Comunicacion con el backend

- La SPA consume la misma API REST del backend via `HttpClient`
- Autenticacion: JWT Bearer (almacenado en `localStorage`, adjunto en `HttpInterceptor`)
- CORS: el backend permite el origen `http://localhost:4200` en desarrollo

## Consecuencias

**Positivo:**
- Estructura del proyecto predecible y estandarizada por el Angular CLI
- `PrimeNG` provee componentes enterprise de alta calidad (`p-table`, `p-dialog`, `p-toast`, `p-dropdown`) con menos configuracion que Angular Material para casos de negocio complejos
- `p-table` de PrimeNG incluye filtros por columna, ordenamiento, paginacion y seleccion de filas sin dependencias adicionales — ideal para la tabla de proveedores
- `Reactive Forms` + validadores sincrono/asincrono cubren los requisitos de validacion de `US-SUP-001`
- Separacion clara entre el frontend (repositorio `risk-screening-app`) y el backend (`risk-screening-api`)
- Alineacion con el patron de inyeccion de dependencias ya utilizado en el backend .NET

**Negativo:**
- Bundle inicial mas grande que React/Vue para una SPA pequeña
- PrimeNG requiere configuracion de `MessageService` en el provider raiz para el sistema de toast
- Curva de aprendizaje mayor que React para desarrolladores nuevos en Angular

**Mitigacion:**
- Para el alcance de este proyecto (dos modulos: suppliers + screening) el tamaño del bundle es irrelevante
- Angular Standalone Components (desde Angular 17+) reducen boilerplate respecto a NgModules
- La documentacion de PrimeNG es extensa con ejemplos listos para copiar

## Referencias
- [Angular Docs — Standalone Components](https://angular.dev/guide/components/importing)
- [PrimeNG](https://primeng.org/)
- [PrimeNG p-table](https://primeng.org/table)
- [PrimeNG p-dialog](https://primeng.org/dialog)
- [Angular Reactive Forms](https://angular.dev/guide/forms/reactive-forms)
- [Angular HttpClient](https://angular.dev/guide/http)
