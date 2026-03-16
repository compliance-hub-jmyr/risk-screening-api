# ADR-0017: Pipeline CI/CD — GitHub Actions con Patron workflow_run

## Estado
`Aceptado`

## Fecha
2026-03-15

## Contexto

El proyecto necesita CI/CD automatizado para ambos repositorios: backend (.NET) y frontend (Angular). Requisitos clave:

- Build y test en cada push/PR
- Deploy automatizado a Azure Container Apps al hacer merge a `main`
- Gate de aprobacion manual antes del deploy a produccion
- Versionado de imagenes Docker

| Opcion | Descripcion | Pros | Contras |
|--------|-------------|------|---------|
| **Workflow unico** | Un solo archivo YAML con etapas de build, test, push, deploy | Simple, un archivo | Ejecucion larga, sin separacion de concerns, no se puede re-ejecutar deploy sin rebuild |
| **CI + CD separados con `workflow_run`** | CI se ejecuta en push/PR; CD se activa solo cuando CI completa en `main` | Separacion clara, CD solo corre despues de que tests pasan, se puede re-ejecutar deploy independiente | Dos archivos que mantener, leve complejidad en configuracion de `workflow_run` |
| **Azure DevOps Pipelines** | CI/CD nativo de Azure | Integracion profunda con Azure | Herramienta separada de GitHub, menos conveniente para repos en GitHub |

## Decision

Usar **GitHub Actions con dos workflows** conectados via `workflow_run`:

1. **`ci.yml`** — Se activa en `push` a `main` y `pull_request` a `main`/`develop`. Compila y ejecuta tests.
2. **`cd.yml`** — Se activa por `workflow_run` cuando CI completa exitosamente en `main`. Construye imagen Docker, sube a Docker Hub, despliega en Azure Container Apps.

### Decisiones Clave de Diseno

- **Extraccion de version** — Backend lee `<Version>` del `.csproj`; frontend lee `version` del `package.json`
- **Tags de imagen Docker** — Cada push tagea con numero de version y `latest`
- **GitHub environment `production`** — El job de deploy en CD requiere aprobacion via reglas de proteccion de environment
- **Cache de capas Docker** — Usa cache de GitHub Actions (`cache-from: type=gha`) para acelerar builds

## Consecuencias

- Ambos repos deben mantener dos archivos de workflow (`ci.yml` + `cd.yml`)
- `workflow_run` solo se activa en eventos de la rama default — no se puede usar para ramas de staging sin configuracion adicional
- Credenciales de Docker Hub y service principal de Azure deben almacenarse como GitHub secrets
