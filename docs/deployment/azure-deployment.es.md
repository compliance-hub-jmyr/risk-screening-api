# Guia de Despliegue — Azure Container Apps

> Guia paso a paso para desplegar la plataforma Risk Screening en **Azure Container Apps** usando **Docker Hub** y **GitHub Actions** para CI/CD.
>
> Para diagramas de arquitectura, ver [Diagramas C4 — Despliegue](../architecture/c4-diagrams.es.md#diagrama-de-despliegue--azure-container-apps).

---

## Prerrequisitos

- Azure CLI (`az`) >= 2.80
- Cuenta Docker Hub (`jhosepmyr`)
- Repositorio GitHub con Actions habilitado
- Usar **Git Bash** (no PowerShell — la sintaxis es diferente)

---

## Paso 1 — Configuracion Inicial

### 1.1 Registrar Resource Providers

Solo la primera vez por suscripcion:

```bash
az provider register -n Microsoft.App --wait
az provider register -n Microsoft.OperationalInsights --wait
az provider register -n Microsoft.Sql --wait
```

### 1.2 Crear Resource Group

```bash
az group create --name rg-riskscreening-prod --location centralus
```

### 1.3 Crear Log Analytics Workspace

```bash
az monitor log-analytics workspace create --workspace-name riskscreening-logs --resource-group rg-riskscreening-prod --location centralus
```

### 1.4 Crear Container Apps Environment

```bash
az containerapp env create \
  --name riskscreening-env \
  --resource-group rg-riskscreening-prod \
  --location centralus \
  --logs-workspace-id $(az monitor log-analytics workspace show --workspace-name riskscreening-logs --resource-group rg-riskscreening-prod --query customerId -o tsv) \
  --logs-workspace-key $(az monitor log-analytics workspace get-shared-keys --workspace-name riskscreening-logs --resource-group rg-riskscreening-prod --query primarySharedKey -o tsv)
```

### Verificar Paso 1

```bash
az containerapp env show --name riskscreening-env --resource-group rg-riskscreening-prod --query "{name:name, state:properties.provisioningState}" -o table
```

> Debe mostrar `Succeeded`.

---

## Paso 2 — Desplegar Base de Datos

```bash
az sql server create --name riskscreening-sqlserver --resource-group rg-riskscreening-prod --location westus --admin-user sqladmin --admin-password 'TuPassword2026!'

az sql db create --name RiskScreeningDb --resource-group rg-riskscreening-prod --server riskscreening-sqlserver --service-objective Basic

az sql server firewall-rule create --name AllowAzureServices --resource-group rg-riskscreening-prod --server riskscreening-sqlserver --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
```

> Azure SQL Database PaaS
>
> Las tablas se crean solas. Cuando el API inicia, **DbUp** ejecuta los scripts de migracion automaticamente.
>
> Para decisiones de arquitectura, ver [ADR-0018](../adr/0018-database-strategy.es.md).

### Verificar Paso 2

```bash
az sql db show --name RiskScreeningDb --resource-group rg-riskscreening-prod --server riskscreening-sqlserver --query "{name:name, status:status}" -o table
```

---

## Paso 3 — Desplegar Backend

Instalar extension (solo la primera vez):

```bash
az extension add --name containerapp --upgrade
```

Construir y subir imagen:

```bash
docker build -f RiskScreening.API/Dockerfile -t jhosepmyr/riskscreening-api:1.0.0 .
docker push jhosepmyr/riskscreening-api:1.0.0
```

Crear Container App:

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
  --secrets "jwt-key=TU_JWT_KEY_MIN_64_CHARS" "admin-password=TU_ADMIN_PASSWORD" \
  --env-vars \
    "ASPNETCORE_ENVIRONMENT=Production" \
    "App__TimeZone=America/Lima" \
    "ConnectionStrings__DefaultConnection=Server=riskscreening-sqlserver.database.windows.net,1433;Database=RiskScreeningDb;User Id=sqladmin;Password=TuPassword2026!;Encrypt=True;TrustServerCertificate=True" \
    "Jwt__Key=secretref:jwt-key" \
    "Cors__AllowedOrigins__0=https://PENDIENTE" \
    "IamSeed__AdminEmail=admin@riskscreening.com" \
    "IamSeed__AdminPassword=secretref:admin-password" \
    "AllowedHosts=*"
```

> `Cors__AllowedOrigins__0` se actualiza en el Paso 5 despues de obtener la URL del frontend.

### Verificar Paso 3

```bash
az containerapp show --name riskscreening-api --resource-group rg-riskscreening-prod --query "{name:name, fqdn:properties.configuration.ingress.fqdn}" -o table
```

---

## Paso 4 — Desplegar Frontend

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

### Verificar Paso 4

```bash
az containerapp show --name riskscreening-web --resource-group rg-riskscreening-prod --query "{name:name, fqdn:properties.configuration.ingress.fqdn}" -o table
```

---

## Paso 5 — Actualizar CORS

```bash
az containerapp update \
  --name riskscreening-api \
  --resource-group rg-riskscreening-prod \
  --set-env-vars "Cors__AllowedOrigins__0=https://$(az containerapp show --name riskscreening-web --resource-group rg-riskscreening-prod --query 'properties.configuration.ingress.fqdn' -o tsv)"
```

---

## Paso 6 — Configurar CI/CD (GitHub Actions)

### 6.1 Crear Service Principal

```bash
# Obtener subscription ID
az account show --query id -o tsv

# Crear service principal (reemplazar <SUBSCRIPTION_ID> con el valor anterior)
MSYS_NO_PATHCONV=1 az ad sp create-for-rbac --name github-riskscreening --role contributor --scopes "/subscriptions/<SUBSCRIPTION_ID>/resourceGroups/rg-riskscreening-prod" --sdk-auth
```

> **Git Bash:** Es necesario `MSYS_NO_PATHCONV=1` para evitar que Git Bash convierta `/subscriptions/...` a rutas de Windows.
>
> **Guardar el JSON completo** — se usa como secret en GitHub. Solo se muestra una vez.

### 6.2 Secrets en GitHub

| Secret | Valor |
|--------|-------|
| `AZURE_CREDENTIALS` | JSON del service principal |
| `DOCKER_USERNAME` | `jhosepmyr` |
| `DOCKER_PASSWORD` | Access token de Docker Hub |

### 6.3 Variables en GitHub

| Variable | Valor |
|----------|-------|
| `AZURE_RESOURCE_GROUP` | `rg-riskscreening-prod` |
| `BACKEND_CONTAINER_APP_NAME` | `riskscreening-api` |
| `FRONTEND_CONTAINER_APP_NAME` | `riskscreening-web` |

### 6.4 Flujo del Pipeline

Ambos repos siguen el mismo patron: `push a main → CI (Build & Test) → CD (Docker Push + az containerapp update)`.

---

## Paso 7 — Verificar

```bash
# URLs
az containerapp show --name riskscreening-web --resource-group rg-riskscreening-prod --query "properties.configuration.ingress.fqdn" -o tsv
az containerapp show --name riskscreening-api --resource-group rg-riskscreening-prod --query "properties.configuration.ingress.fqdn" -o tsv

# Logs del backend
az containerapp logs show --name riskscreening-api --resource-group rg-riskscreening-prod --follow
```

Abrir la URL del frontend en el navegador. Iniciar sesion con `admin@riskscreening.com`.

---

## Limpieza (eliminar todo)

```bash
az group delete --name rg-riskscreening-prod --yes
```

> Un solo comando elimina todo: resource group, environment, base de datos, contenedores, logs.

---

## Variables de Entorno

| Variable | Descripcion | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Entorno | `Production` |
| `ASPNETCORE_URLS` | URL(s) de escucha | `http://+:8080` |
| `App__TimeZone` | Zona horaria IANA | `America/Lima` |
| `TZ` | Zona horaria del SO | `America/Lima` |
| `ConnectionStrings__DefaultConnection` | Conexion Azure SQL | — |
| `Jwt__Key` | Clave HMAC para JWT (min 64 chars) | — |
| `Cors__AllowedOrigins__0` | URL del frontend para CORS | — |
| `IamSeed__AdminEmail` | Email del admin inicial | — |
| `IamSeed__AdminPassword` | Password del admin inicial | — |
