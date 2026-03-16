# ADR-0014: Container Platform — Azure Container Apps

## Status
`Accepted`

## Date
2026-03-15

## Context

The platform needs a container hosting solution on Azure. Three options were evaluated:

| Option | Description | Pros | Cons |
|--------|-------------|------|------|
| **Azure Kubernetes Service (AKS)** | Managed Kubernetes cluster | Full control, ecosystem maturity | Complex to manage, requires cluster ops, minimum ~$70/mo for nodes |
| **Azure App Service (Containers)** | PaaS with container support | Simple, familiar | No free tier for containers, $13+/mo per instance, limited scaling |
| **Azure Container Apps** | Serverless container platform built on Kubernetes | Free tier (180K vCPU-sec/mo), auto-scaling, built-in HTTPS with free domain, zero ops | Less control than AKS, newer service |

The project is a technical assessment — cost must be near zero and operational overhead minimal.

## Decision

Use **Azure Container Apps** as the container hosting platform.

### Rationale

1. **Free tier** — 180,000 vCPU-sec + 360,000 GiB-sec/month covers low-traffic workloads at $0
2. **Zero ops** — No cluster management, node patching, or ingress controller configuration
3. **Built-in HTTPS** — Each container gets a free `*.azurecontainerapps.io` domain with auto-provisioned TLS certificate
4. **Auto-scaling** — Scale to zero or up to N replicas based on HTTP traffic or custom rules
5. **Container Apps Environment** — Shared virtual network for all containers in the same environment

## Consequences

- Limited to HTTP-based workloads (no raw TCP/UDP ingress without workarounds)
- Less flexibility than AKS for advanced networking or custom controllers
- Vendor lock-in to Azure Container Apps API (mitigated by standard Docker images)
