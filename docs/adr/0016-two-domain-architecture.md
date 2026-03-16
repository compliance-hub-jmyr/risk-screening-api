# ADR-0016: Two-Domain Architecture — Separate Public URLs for Frontend and Backend

## Status
`Accepted`

## Date
2026-03-15

## Context

With frontend and backend deployed as separate containers on Azure Container Apps, three routing strategies were evaluated:

| Option | Description | Pros | Cons |
|--------|-------------|------|------|
| **Nginx reverse proxy** | Frontend container proxies `/api/*` to backend (internal ingress) | Single domain, `window.location.origin/api` works | Couples frontend to backend networking, adds proxy latency, nginx config complexity |
| **Azure Application Gateway** | Azure L7 load balancer routes `/api/*` to backend, `/*` to frontend | Single domain, managed service | $18+/month minimum, complex configuration, overkill for a tech assessment |
| **Two public domains** | Both containers get `--ingress external` with their own `*.azurecontainerapps.io` URL | Zero cost, zero complexity, independent scaling and deployment | CORS required, frontend must know backend URL at build time |

## Decision

Use **two separate public domains** — each container gets its own Azure-provided HTTPS URL.

### Rationale

1. **$0 extra cost** — No Application Gateway, no custom domain fees
2. **Simplicity** — No nginx proxy configuration, no internal networking to debug
3. **Independent deployment** — Frontend and backend can be deployed, scaled, and updated independently
4. **CORS is trivial** — One `Cors__AllowedOrigins__0` env var on the backend
5. **Standard pattern** — SPA + API on different origins is the most common production topology

### API URL Configuration

The Angular app uses `environment.prod.ts` to set the backend URL at build time. When the backend Azure domain changes (rare — only on initial creation), rebuild and redeploy the frontend image.

## Consequences

- CORS must be configured on the backend (`Cors__AllowedOrigins__0` env var)
- The frontend must be rebuilt if the backend URL changes (mitigated by stable Azure domains)
- Browser makes cross-origin requests — credentials and headers must be explicitly allowed in CORS policy
