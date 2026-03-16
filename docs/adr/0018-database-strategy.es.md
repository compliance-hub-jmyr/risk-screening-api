# ADR-0018: Estrategia de Base de Datos — Azure SQL Database

## Estado
`Aceptado`

## Fecha
2026-03-15

## Contexto

La plataforma usa SQL Server 2022 para persistencia. Para el despliegue en Azure, se evaluaron tres opciones:

| Opcion | Descripcion | Pros | Contras |
|--------|-------------|------|---------|
| **Azure SQL Database** | Base de datos PaaS completamente administrada | Backups automaticos, parcheo, HA, sin gestion de infraestructura | Tier Basic ~$5/mes |
| **SQL Server en Azure VM** | IaaS con control total | Todas las features de SQL Server, configurable | Requiere gestion de VM, $15+/mes por VM, parcheo de SO |
| **Contenedor SQL Server** | SQL Server 2022 Developer Edition como contenedor en Container Apps | $0 de costo, mismo modelo de deploy | Datos efimeros sin volumen persistente, requiere extension containerapp |

## Decision

Usar **Azure SQL Database** (PaaS) con tier Basic.

### Justificacion

1. **Administrado** — Backups automaticos, parcheo de seguridad y alta disponibilidad sin configuracion
2. **Persistencia garantizada** — Los datos no se pierden al reiniciar, a diferencia de un contenedor sin volumen
3. **Simplicidad** — Un comando `az sql server create` + `az sql db create`, sin extensiones adicionales
4. **Bajo costo** — Tier Basic cuesta ~$5/mes, cubierto por los $200 de credito Azure
5. **Migraciones DbUp** — La API ejecuta scripts de migracion automaticamente al iniciar, las tablas se crean solas

### Connection String

```
Server=riskscreening-sqlserver.database.windows.net,1433;Database=RiskScreeningDb;User Id=sqladmin;Password=***;Encrypt=True;TrustServerCertificate=True
```

### Firewall

Se configura una regla `AllowAzureServices` (`0.0.0.0 - 0.0.0.0`) para permitir conexiones desde Azure Container Apps.

## Consecuencias

- Costo de ~$5/mes (tier Basic) — aceptable con credito Azure
- El connection string cambia entre local (`Server=localhost,1433`) y produccion (`Server=*.database.windows.net,1433`)
- Para escalar, subir de tier Basic a Standard S0 ($15/mes) o superior
