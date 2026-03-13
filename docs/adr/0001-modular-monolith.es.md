# ADR-0001: Arquitectura Modular Monolith

## Estado
`Accepted`

## Fecha
2026-03-13

## Contexto

La plataforma requiere desarrollar dos modulos independientes:
1. Un API de scraping de listas de riesgo con autenticacion por API Key
2. Una SPA de gestion de proveedores con autenticacion JWT

Ambos deben convivir en el mismo backend .NET. Se evaluaron tres opciones:

| Opcion | Descripcion | Pros | Contras |
|--------|-------------|------|---------|
| **Monolito simple** | Todo en un proyecto sin separacion de modulos | Rapido de implementar | Dificil de escalar, alto acoplamiento |
| **Modular Monolith** | Un proceso, multiples modulos con fronteras claras | Balance entre simplicidad y organizacion | Requiere disciplina de separacion |
| **Microservicios** | Servicios independientes por modulo | Escalabilidad independiente | Overhead de infraestructura excesivo |

## Decision

Usar **Modular Monolith**: un unico proceso ASP.NET Core con modulos independientes, cada uno con su propia capa `Domain / Application / Infrastructure / Interfaces`.

La estructura de modulos es:
```
Modules/
  IAM/           -- Identity & Access Management
  Scraping/      -- Modulo Scraping: busqueda en listas de alto riesgo
  Suppliers/     -- Modulo Suppliers: gestion de proveedores y screening
Shared/          -- Kernel compartido (DbContext, pipelines, excepciones)
```

Cada modulo tiene fronteras estrictas:
- Los modulos no se referencian entre si directamente
- La comunicacion entre modulos se hace via eventos de dominio o a traves del `Shared` kernel
- Cada modulo registra sus propias dependencias mediante extension methods de `IServiceCollection`

## Consecuencias

**Positivo:**
- Separacion clara de responsabilidades — cada modulo es independiente
- Un solo despliegue — Docker Compose con un contenedor de API
- Facil de entender para revisores
- Los modulos son cohesivos — todos los artefactos de IAM estan en `Modules/IAM/`

**Negativo:**
- El DbContext compartido crea dependencia de infraestructura entre modulos
- Escalar un modulo individualmente requeriria extraerlo a microservicio

**Mitigacion:**
- Cada modulo tiene su propia clase de configuracion EF (`IEntityTypeConfiguration<T>`)
- El `AppDbContext` solo aplica configuraciones via `ApplyConfigurationsFromAssembly` — no conoce las entidades directamente

## Referencias
- [Modular Monolith: A Primer — Kamil Grzybek](https://www.kamilgrzybek.com/design/modular-monolith-primer/)
- [Clean Architecture — Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
