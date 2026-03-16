# ADR-0018: Database Strategy — Azure SQL Database

## Status
`Accepted`

## Date
2026-03-15

## Context

The platform uses SQL Server 2022 for persistence. For Azure deployment, three options were evaluated:

| Option | Description | Pros | Cons |
|--------|-------------|------|------|
| **Azure SQL Database** | Fully managed PaaS database | Automated backups, patching, HA, no infrastructure management | Basic tier ~$5/month |
| **SQL Server on Azure VM** | IaaS with full control | Full SQL Server features, configurable | Requires VM management, $15+/month for VM, OS patching |
| **SQL Server container** | SQL Server 2022 Developer Edition as a container in Container Apps | $0 cost, same deployment model | Ephemeral data without persistent volume, requires containerapp extension |

## Decision

Use **Azure SQL Database** (PaaS) with Basic tier.

### Rationale

1. **Managed** — Automatic backups, security patching and high availability with no configuration
2. **Guaranteed persistence** — Data is not lost on restart, unlike a container without a persistent volume
3. **Simplicity** — One `az sql server create` + `az sql db create` command, no additional extensions needed
4. **Low cost** — Basic tier costs ~$5/month, covered by Azure $200 credit
5. **DbUp migrations** — The API runs migration scripts automatically on startup, tables are self-provisioning

### Connection String

```
Server=riskscreening-sqlserver.database.windows.net,1433;Database=RiskScreeningDb;User Id=sqladmin;Password=***;Encrypt=True;TrustServerCertificate=True
```

### Firewall

An `AllowAzureServices` firewall rule (`0.0.0.0 - 0.0.0.0`) is configured to allow connections from Azure Container Apps.

## Consequences

- ~$5/month cost (Basic tier) — acceptable with Azure credit
- Connection string differs between local (`Server=localhost,1433`) and production (`Server=*.database.windows.net,1433`)
- To scale up, upgrade from Basic to Standard S0 ($15/month) or higher
