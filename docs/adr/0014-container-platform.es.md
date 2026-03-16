# ADR-0014: Plataforma de Contenedores — Azure Container Apps

## Estado
`Aceptado`

## Fecha
2026-03-15

## Contexto

La plataforma necesita una solucion de hosting de contenedores en Azure. Se evaluaron tres opciones:

| Opcion | Descripcion | Pros | Contras |
|--------|-------------|------|---------|
| **Azure Kubernetes Service (AKS)** | Cluster Kubernetes administrado | Control total, ecosistema maduro | Complejo de administrar, requiere ops de cluster, minimo ~$70/mes por nodos |
| **Azure App Service (Containers)** | PaaS con soporte de contenedores | Simple, familiar | Sin free tier para contenedores, $13+/mes por instancia, escalado limitado |
| **Azure Container Apps** | Plataforma serverless de contenedores sobre Kubernetes | Free tier (180K vCPU-sec/mes), auto-scaling, HTTPS con dominio gratis, cero ops | Menos control que AKS, servicio mas nuevo |

El proyecto es un assessment tecnico — el costo debe ser cercano a cero y la carga operativa minima.

## Decision

Usar **Azure Container Apps** como plataforma de hosting de contenedores.

### Justificacion

1. **Free tier** — 180,000 vCPU-sec + 360,000 GiB-sec/mes cubre workloads de bajo trafico a $0
2. **Cero ops** — Sin gestion de cluster, parcheo de nodos ni configuracion de ingress controller
3. **HTTPS incluido** — Cada contenedor obtiene un dominio gratuito `*.azurecontainerapps.io` con certificado TLS auto-provisionado
4. **Auto-scaling** — Escala a cero o hasta N replicas basado en trafico HTTP o reglas custom
5. **Container Apps Environment** — Red virtual compartida para todos los contenedores en el mismo environment

## Consecuencias

- Limitado a workloads basados en HTTP (sin ingress TCP/UDP raw sin workarounds)
- Menos flexibilidad que AKS para networking avanzado o controllers custom
- Vendor lock-in al API de Azure Container Apps (mitigado por imagenes Docker estandar)
