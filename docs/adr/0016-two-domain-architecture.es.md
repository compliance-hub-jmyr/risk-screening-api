# ADR-0016: Arquitectura de Dos Dominios — URLs Publicas Separadas para Frontend y Backend

## Estado
`Aceptado`

## Fecha
2026-03-15

## Contexto

Con frontend y backend desplegados como contenedores separados en Azure Container Apps, se evaluaron tres estrategias de ruteo:

| Opcion | Descripcion | Pros | Contras |
|--------|-------------|------|---------|
| **Nginx reverse proxy** | El contenedor frontend hace proxy de `/api/*` al backend (ingress interno) | Un solo dominio, `window.location.origin/api` funciona | Acopla frontend con networking del backend, agrega latencia de proxy, complejidad de config nginx |
| **Azure Application Gateway** | Balanceador L7 de Azure rutea `/api/*` al backend, `/*` al frontend | Un solo dominio, servicio administrado | $18+/mes minimo, configuracion compleja, overkill para un assessment tecnico |
| **Dos dominios publicos** | Ambos contenedores con `--ingress external` y su propia URL `*.azurecontainerapps.io` | Cero costo, cero complejidad, escalado y deploy independiente | CORS requerido, frontend debe conocer URL del backend en build time |

## Decision

Usar **dos dominios publicos separados** — cada contenedor obtiene su propia URL HTTPS provista por Azure.

### Justificacion

1. **$0 de costo extra** — Sin Application Gateway, sin cargos por dominio custom
2. **Simplicidad** — Sin configuracion de proxy nginx, sin networking interno que debuggear
3. **Deploy independiente** — Frontend y backend pueden desplegarse, escalar y actualizarse independientemente
4. **CORS es trivial** — Una variable de entorno `Cors__AllowedOrigins__0` en el backend
5. **Patron estandar** — SPA + API en origenes distintos es la topologia de produccion mas comun

### Configuracion de URL del API

La app Angular usa `environment.prod.ts` para setear la URL del backend en build time. Si el dominio Azure del backend cambia (raro — solo en creacion inicial), reconstruir y redesplegar la imagen del frontend.

## Consecuencias

- CORS debe configurarse en el backend (variable de entorno `Cors__AllowedOrigins__0`)
- El frontend debe reconstruirse si cambia la URL del backend (mitigado por dominios Azure estables)
- El navegador hace requests cross-origin — credenciales y headers deben permitirse explicitamente en la politica CORS
