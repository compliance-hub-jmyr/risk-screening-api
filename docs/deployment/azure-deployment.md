# Deployment Guide — Azure Container Apps

> Step-by-step guide to deploy the Risk Screening platform on **Azure Container Apps** using **Docker Hub** and **GitHub Actions** for CI/CD.
>
> For architecture diagrams, see [C4 Diagrams — Deployment](../architecture/c4-diagrams.md#deployment-diagram--azure-container-apps).

---

## Prerequisites

- Azure CLI (`az`) >= 2.80
- Docker Hub account (`jhosepmyr`)
- GitHub repository with Actions enabled
- Use **Git Bash** (not PowerShell — variable syntax differs)

---

## Step 1 — Initial Setup

### 1.1 Register Resource Providers

Only needed once per subscription:

```bash
az provider register -n Microsoft.App --wait
az provider register -n Microsoft.OperationalInsights --wait
az provider register -n Microsoft.Sql --wait
```

### 1.2 Create Resource Group

```bash
az group create --name rg-riskscreening-prod --location centralus
```

### 1.3 Create Log Analytics Workspace

```bash
az monitor log-analytics workspace create --workspace-name riskscreening-logs --resource-group rg-riskscreening-prod --location centralus
```

### 1.4 Create Container Apps Environment

```bash
az containerapp env create \
  --name riskscreening-env \
  --resource-group rg-riskscreening-prod \
  --location centralus \
  --logs-workspace-id $(az monitor log-analytics workspace show --workspace-name riskscreening-logs --resource-group rg-riskscreening-prod --query customerId -o tsv) \
  --logs-workspace-key $(az monitor log-analytics workspace get-shared-keys --workspace-name riskscreening-logs --resource-group rg-riskscreening-prod --query primarySharedKey -o tsv)
```

### Verify Step 1

```bash
az containerapp env show --name riskscreening-env --resource-group rg-riskscreening-prod --query "{name:name, state:properties.provisioningState}" -o table
```

> Should show `Succeeded`.

---

## Step 2 — Deploy Database

```bash
az sql server create --name riskscreening-sqlserver --resource-group rg-riskscreening-prod --location westus --admin-user sqladmin --admin-password 'YourPassword2026!'

az sql db create --name RiskScreeningDb --resource-group rg-riskscreening-prod --server riskscreening-sqlserver --service-objective Basic

az sql server firewall-rule create --name AllowAzureServices --resource-group rg-riskscreening-prod --server riskscreening-sqlserver --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
```

> Azure SQL Database is a managed PaaS service.
>
> No need to create tables manually. When the API starts, **DbUp** runs migration scripts and creates all tables automatically.
>
> For architecture decisions, see [ADR-0018](../adr/0018-database-strategy.md).

### Verify Step 2

```bash
az sql db show --name RiskScreeningDb --resource-group rg-riskscreening-prod --server riskscreening-sqlserver --query "{name:name, status:status}" -o table
```

---

## Step 3 — Deploy Backend

Install extension (only once):

```bash
az extension add --name containerapp --upgrade
```

Build and push the image:

```bash
docker build -f RiskScreening.API/Dockerfile -t jhosepmyr/riskscreening-api:1.0.0 .
docker push jhosepmyr/riskscreening-api:1.0.0
```

Create the Container App:

```bash
az containerapp create \
  --name riskscreening-api \
  --resource-group rg-riskscreening-prod \
  --environment riskscreening-env \
  --image jhosepmyr/riskscreening-api:1.0.0 \
  --target-port 8080 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 3 \
  --cpu 1.0 \
  --memory 2.0Gi \
  --secrets "jwt-key=YOUR_JWT_KEY_MIN_64_CHARS" "admin-password=YOUR_ADMIN_PASSWORD" \
  --env-vars \
    "ASPNETCORE_ENVIRONMENT=Production" \
    "App__TimeZone=America/Lima" \
    "ConnectionStrings__DefaultConnection=Server=riskscreening-sqlserver.database.windows.net,1433;Database=RiskScreeningDb;User Id=sqladmin;Password=YourPassword2026!;Encrypt=True;TrustServerCertificate=True" \
    "Jwt__Key=secretref:jwt-key" \
    "Cors__AllowedOrigins__0=https://PENDING" \
    "IamSeed__AdminEmail=admin@riskscreening.com" \
    "IamSeed__AdminPassword=secretref:admin-password" \
    "AllowedHosts=*"
```

> **Note:** `Cors__AllowedOrigins__0` is updated in Step 5 after getting the frontend URL.

### Verify Step 3

```bash
az containerapp show --name riskscreening-api --resource-group rg-riskscreening-prod --query "{name:name, fqdn:properties.configuration.ingress.fqdn}" -o table
```

---

## Step 4 — Deploy Frontend

```bash
cd risk-screening-app
docker build -t jhosepmyr/riskscreening-web:1.0.0 .
docker push jhosepmyr/riskscreening-web:1.0.0

az containerapp create \
  --name riskscreening-web \
  --resource-group rg-riskscreening-prod \
  --environment riskscreening-env \
  --image jhosepmyr/riskscreening-web:1.0.0 \
  --target-port 8080 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 2 \
  --cpu 0.5 \
  --memory 1.0Gi
```

### Verify Step 4

```bash
az containerapp show --name riskscreening-web --resource-group rg-riskscreening-prod --query "{name:name, fqdn:properties.configuration.ingress.fqdn}" -o table
```

---

## Step 5 — Update CORS

```bash
az containerapp update \
  --name riskscreening-api \
  --resource-group rg-riskscreening-prod \
  --set-env-vars "Cors__AllowedOrigins__0=https://$(az containerapp show --name riskscreening-web --resource-group rg-riskscreening-prod --query 'properties.configuration.ingress.fqdn' -o tsv)"
```

---

## Step 6 — Configure CI/CD (GitHub Actions)

### 6.1 Create Azure Service Principal

```bash
# Get subscription ID
az account show --query id -o tsv

# Create service principal (replace <SUBSCRIPTION_ID> with the value above)
MSYS_NO_PATHCONV=1 az ad sp create-for-rbac --name github-riskscreening --role contributor --scopes "/subscriptions/<SUBSCRIPTION_ID>/resourceGroups/rg-riskscreening-prod" --sdk-auth
```

> **Git Bash:** `MSYS_NO_PATHCONV=1` is required to prevent Git Bash from converting `/subscriptions/...` to Windows paths.
>
> **Save the full JSON output** — it is used as a GitHub secret. It is only shown once.

### 6.2 GitHub Secrets

| Secret | Value |
|--------|-------|
| `AZURE_CREDENTIALS` | JSON from service principal (previous step) |
| `DOCKER_USERNAME` | `jhosepmyr` |
| `DOCKER_PASSWORD` | Docker Hub access token |

### 6.3 GitHub Variables

| Variable | Value |
|----------|-------|
| `AZURE_RESOURCE_GROUP` | `rg-riskscreening-prod` |
| `BACKEND_CONTAINER_APP_NAME` | `riskscreening-api` |
| `FRONTEND_CONTAINER_APP_NAME` | `riskscreening-web` |

### 6.4 Pipeline Flow

Both repos follow the same pattern: `push to main → CI (Build & Test) → CD (Docker Push + az containerapp update)`.

---

## Step 7 — Verify

```bash
# URLs
az containerapp show --name riskscreening-web --resource-group rg-riskscreening-prod --query "properties.configuration.ingress.fqdn" -o tsv
az containerapp show --name riskscreening-api --resource-group rg-riskscreening-prod --query "properties.configuration.ingress.fqdn" -o tsv

# Backend logs
az containerapp logs show --name riskscreening-api --resource-group rg-riskscreening-prod --follow
```

Open the frontend URL in a browser. Sign in with `admin@riskscreening.com`.

---

## Cleanup (delete everything)

```bash
az group delete --name rg-riskscreening-prod --yes
```

> One command deletes everything: resource group, environment, database, containers, logs.

---

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` |
| `ASPNETCORE_URLS` | Listen URL(s) | `http://+:8080` |
| `App__TimeZone` | IANA timezone for audit timestamps | `America/Lima` |
| `TZ` | OS-level timezone | `America/Lima` |
| `ConnectionStrings__DefaultConnection` | Azure SQL connection string | — |
| `Jwt__Key` | HMAC key for JWT (min 64 chars) | — |
| `Cors__AllowedOrigins__0` | Frontend URL for CORS | — |
| `IamSeed__AdminEmail` | Initial admin email | — |
| `IamSeed__AdminPassword` | Initial admin password | — |
