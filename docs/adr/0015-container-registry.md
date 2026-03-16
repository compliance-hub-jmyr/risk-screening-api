# ADR-0015: Container Registry — Docker Hub

## Status
`Accepted`

## Date
2026-03-15

## Context

Container images need a registry for CI/CD pipelines and Azure Container Apps to pull from. Two options were evaluated:

| Option | Description | Pros | Cons |
|--------|-------------|------|------|
| **Azure Container Registry (ACR)** | Azure-native private registry | Integrated auth with Azure, private by default, geo-replication | Basic tier $5/mo, requires Azure service principal or managed identity for pulls |
| **Docker Hub** | Public container registry | Free for public repos, well-known, simple setup | Public images (acceptable for open-source/assessment), rate limits on free tier (100 pulls/6h) |

## Decision

Use **Docker Hub** (`jhosepmyr/*`) as the container registry.

### Rationale

1. **$0 cost** — Free tier is sufficient for the project's scale
2. **Simplicity** — No additional Azure resource to manage, no ACR login configuration
3. **CI/CD integration** — GitHub Actions has first-class `docker/login-action` support for Docker Hub
4. **Public images are acceptable** — This is a technical assessment project, not a proprietary product
5. **Rate limits are not a concern** — Low deployment frequency stays well within the 100 pulls/6h free limit

## Consequences

- Images are public — do not bake secrets, credentials, or proprietary code into images
- If the project becomes proprietary, migrate to ACR or Docker Hub paid plan
- Pull rate limits could be an issue if scaling to many replicas pulling simultaneously (unlikely for this project)
