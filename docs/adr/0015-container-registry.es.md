# ADR-0015: Registro de Contenedores — Docker Hub

## Estado
`Aceptado`

## Fecha
2026-03-15

## Contexto

Las imagenes de contenedores necesitan un registro para que los pipelines CI/CD y Azure Container Apps puedan hacer pull. Se evaluaron dos opciones:

| Opcion | Descripcion | Pros | Contras |
|--------|-------------|------|---------|
| **Azure Container Registry (ACR)** | Registro privado nativo de Azure | Auth integrada con Azure, privado por defecto, geo-replicacion | Tier basico $5/mes, requiere service principal o managed identity para pulls |
| **Docker Hub** | Registro publico de contenedores | Gratis para repos publicos, bien conocido, setup simple | Imagenes publicas (aceptable para open-source/assessment), rate limits en free tier (100 pulls/6h) |

## Decision

Usar **Docker Hub** (`jhosepmyr/*`) como registro de contenedores.

### Justificacion

1. **$0 de costo** — El free tier es suficiente para la escala del proyecto
2. **Simplicidad** — Sin recurso Azure adicional que administrar, sin configuracion de login ACR
3. **Integracion CI/CD** — GitHub Actions tiene soporte first-class con `docker/login-action` para Docker Hub
4. **Imagenes publicas son aceptables** — Es un proyecto de assessment tecnico, no un producto propietario
5. **Rate limits no son problema** — La baja frecuencia de deploy se mantiene dentro del limite de 100 pulls/6h

## Consecuencias

- Las imagenes son publicas — no incluir secretos, credenciales o codigo propietario en las imagenes
- Si el proyecto se vuelve propietario, migrar a ACR o plan de pago de Docker Hub
- Los rate limits podrian ser problema si se escala a muchas replicas haciendo pull simultaneo (improbable para este proyecto)
