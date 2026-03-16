# ADR-0017: CI/CD Pipeline — GitHub Actions with workflow_run Pattern

## Status
`Accepted`

## Date
2026-03-15

## Context

The project needs automated CI/CD for both backend (.NET) and frontend (Angular) repositories. Key requirements:

- Build and test on every push/PR
- Automated deployment to Azure Container Apps on merge to `main`
- Manual approval gate before production deployment
- Docker image versioning

| Option | Description | Pros | Cons |
|--------|-------------|------|------|
| **Single workflow** | One YAML file with build, test, push, deploy stages | Simple, one file | Long-running, no separation of concerns, can't rerun deploy without rebuilding |
| **Separate CI + CD with `workflow_run`** | CI runs on push/PR; CD triggers only when CI completes on `main` | Clear separation, CD only runs after tests pass, can rerun deploy independently | Two files to maintain, slight complexity in `workflow_run` configuration |
| **Azure DevOps Pipelines** | Azure-native CI/CD | Deep Azure integration | Separate tool from GitHub, less convenient for GitHub-hosted repos |

## Decision

Use **GitHub Actions with two workflows** connected via `workflow_run`:

1. **`ci.yml`** — Triggered on `push` to `main` and `pull_request` to `main`/`develop`. Builds and runs tests.
2. **`cd.yml`** — Triggered by `workflow_run` when CI completes successfully on `main`. Builds Docker image, pushes to Docker Hub, deploys to Azure Container Apps.

### Key Design Choices

- **Version extraction** — Backend reads `<Version>` from `.csproj`; frontend reads `version` from `package.json`
- **Docker image tags** — Every push tags with both version number and `latest`
- **GitHub environment `production`** — CD deploy job requires approval via GitHub environment protection rules
- **Docker layer caching** — Uses GitHub Actions cache (`cache-from: type=gha`) to speed up builds

## Consequences

- Both repos must maintain two workflow files (`ci.yml` + `cd.yml`)
- `workflow_run` only triggers on the default branch events — cannot be used for staging branches without additional configuration
- Docker Hub credentials and Azure service principal must be stored as GitHub secrets
