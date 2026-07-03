<!-- docs: CLAUDE.md v1.0.0 — 2026-07-03 — bootstrap inicial de project-docs -->

# QuickTix

Sistema de venta de entradas y abonos para piscinas municipales. Nacido como TFG de DAM,
en evolución a producto comercial para el Ayuntamiento de Nalda (La Rioja). Tres clientes
contra una API central: escritorio para taquilla/administración, móvil para managers y abonados.

**Stack**: .NET 8 — ASP.NET Core Web API + EF Core/SQL Server | WPF (WPF-UI) | .NET MAUI (Android) | xUnit

## Reference Docs

- Estado actual y kanban: @docs/PROGRESS.md
- Arquitectura técnica: @docs/ARCHITECTURE.md
- ADRs y decisiones históricas: `docs/ARCHITECTURE_ADRS.md`
- Visión del producto: `docs/PROJECT.md`
- Historial de sesiones: `docs/PROGRESS_HISTORY.md`

## Commands

```bash
# Build & run (desde la raíz de la solución)
dotnet build QuickTix.sln
dotnet run --project QuickTix.API           # API en http://0.0.0.0:5137 y https://0.0.0.0:7137
docker compose up -d                        # SQL Server 2022 en contenedor

# Testing
dotnet test QuickTix.Tests/QuickTix.Tests.csproj

# EF Core (el startup project es la API)
dotnet ef migrations add <Nombre> --project QuickTix.DAL --startup-project QuickTix.API
dotnet ef database update --project QuickTix.DAL --startup-project QuickTix.API
```

Gotcha: la API aplica migraciones y seed automáticamente al arrancar (`AppDbSeeder.MigrateAsync`).
No hace falta `database update` manual para desarrollo normal.

## Established Patterns

- **CRUD de API**: heredar de `BaseController<TEntity, TDto, TCreateDto>` (`QuickTix.API/Controllers/BaseController.cs`). Solo salirse del patrón para agregados o endpoints de solo lectura (ver `PricingController` como ejemplo documentado).
- **Respuestas**: todo endpoint devuelve `ApiResponse<T>` (`QuickTix.Contracts/Common/ApiResponse.cs`). Nunca devolver payloads desnudos.
- **Acceso a datos**: repositorio por entidad implementando `IRepository<TEntity>` (`QuickTix.Core/Interfaces/`). Cada repo gestiona su propio `SaveAsync` — ver Critical Rules antes de tocar esto.
- **Rutas de cliente**: constantes desde `QuickTix.Contracts/Routes/ApiRoutes.cs` — nunca strings de ruta inline en los clientes.
- **Clientes HTTP**: ambos front usan su `HttpJsonClient` (`Services/HttpsJsonClient.cs` en Desktop y Mobile) + `TokenStore`. No crear HttpClients ad-hoc.
- **MVVM**: CommunityToolkit.Mvvm en ambos clientes; pantallas CRUD de Desktop heredan de `ViewModels/Base/BaseCrudViewModel.cs`.
- **Tests de integración**: SQLite in-memory (nunca el provider InMemory de EF — no soporta transacciones). Referencia: `QuickTix.Tests/Sales/SellTicketsBatchTests.cs`.

## Language Conventions

- Código, identificadores, tests, enums, logs internos: **inglés**.
- Comentarios (el porqué), textos de UI, mensajes de error de cara al usuario: **español**.
- Commits: conventional commits, type/scope en inglés.

## Critical Rules

### Transacciones y datos
- NEVER refactorizar los `SaveAsync` de los repositorios hacia Unit of Work sin revisar la decisión aparcada (ver Pending Decisions en PROGRESS.md) — la frontera de transacción se decidirá junto con la capa de servicios.
- ALWAYS ejecutar `dotnet test` antes de mergear cambios que toquen `QuickTix.DAL/Repositories/Sale*` o el flujo de ventas.
- NEVER usar el provider EF InMemory en tests que involucren transacciones.

### Seguridad
- NEVER añadir nuevos secretos a `appsettings.json` ni a `docker-compose.yml` (los existentes son deuda conocida, bloqueante antes de piloto — ver PROGRESS.md).
- NEVER repetir en respuestas o logs datos personales reales (NIF, teléfonos) — el producto manejará datos de vecinos, incluidos menores.

### Repo
- NEVER commitear `bin/`, `obj/`, `.vs/` ni `_site/` (hubo artefactos trackeados; ya limpiado).
- ALWAYS crear rama desde `main` actualizado; las ramas `learning/*` son de sesiones de aprendizaje y se mergean en verde el mismo día.

## Modo de trabajo (flujo dual)

Este proyecto se trabaja en dos corrientes que convergen en `main`:
- **Sprints autónomos** (demo/features): feature branches cortas, merge rápido.
- **Sesiones de aprendizaje** (estilo tutor): ramas `learning/*` desde main fresco, cortas.
Los tests de `QuickTix.Tests` son la red de seguridad compartida entre ambas corrientes.

## GitHub

Proyecto usa GitHub (repo `SantiPMendoza/QuickTix`).
Antes de cualquier acción de GitHub (issues, PRs, board), leer el skill `sdd-github`.
