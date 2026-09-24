# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Workspace layout

This repo (`merabza/SupportToolsServer`) is one of several sibling git clones inside the workspace folder `D:\1WorkDotnet\SupportToolsServer\`, which is **not** itself a repo. Project references reach siblings by relative path (`..\..\SystemTools\...`), so these must be cloned next to this repo:

| Sibling | What SupportToolsServer uses from it |
|---|---|
| `SystemTools` | `Application.Abstractions` (CQRS `ICommand`/`ICommandHandler`/`IQueryHandler`, `AddApplication`), `SharedKernel` (`Result`), `RepositoriesShared` (`UnitOfWork`), `ApiKeysManagement` |
| `WebSystemTools` | Host plumbing: Serilog, Swagger, API-key auth, SignalR, exception handler, Windows service, static files |
| `SupportToolsServerDbPart` | `SupportToolsServerDbContext` + `IEntityTypeConfiguration<T>` classes |
| `SupportToolsServerShared` | `SupportToolsServerApiContracts` — route constants (`SupportToolsServerApiRoutes`) and request/response models shared with the client app |
| `DatabaseTools` | SQL Server helpers used by the Dapper command repositories |

EF Core migrations are **not** in this repo or in DbPart — they live in the separate `D:\1WorkDotnet\SupportToolsServerDbTools` workspace. A change to an entity or its configuration needs a migration added there. Each sibling is its own repo: commit changes in the repo where the file lives.

## Build / run

```bash
dotnet build SupportToolsServer.slnx
dotnet run --project SupportToolsServer/SupportToolsServer.csproj
```

- .NET 10, central package management (`Directory.Packages.props` — add versions there, not in csproj).
- `Directory.Build.props` sets `TreatWarningsAsErrors`, `AnalysisMode=All`, `EnforceCodeStyleInBuild` and SonarAnalyzer, so any analyzer/style warning breaks the build. `ImplicitUsings` is **disabled** — write explicit `using`s.
- No test projects exist in this solution.
- The host listens on `http://*:5033` and needs `Data:SupportToolsServerDatabase:ConnectionString` (User Secrets; `appsettings.json` only holds a placeholder). Startup throws if it is empty.

## Architecture

The code is mid-migration from an older layered design to a Clean Architecture / CQRS design. Both are wired up in `SupportToolsServer/Program.cs`.

**New design (target for new work)** — `SupportToolsServer.<Layer>` projects:
- `SupportToolsServer.Domain` — entities derived from `Primitives/Entity<TId>` with strongly-typed ids (`GitIgnoreFileTypeId`), repository interfaces, and the generic `Sync/Syncroniser<T,TId>` which reconciles an uploaded list against the DB (add/update, and delete-missing unless `merge`).
- `SupportToolsServer.Application` — one folder per feature/use-case (`GitIgnoreFileTypes/SyncUp/`) holding an `ICommand` and its `ICommandHandler`; `Data/ISupportToolsServerDbContext` exposes the `DbSet`s. Handlers are auto-registered by `AddApplication(debugLogger, typeof(ISupportToolsServerDbContext))`, which scans that assembly and decorates handlers with validation + logging.
- `SupportToolsServer.Persistence` — registers `SupportToolsServerDbContext` (from DbPart) and maps `ISupportToolsServerDbContext` to it.
- `SupportToolsServer.Repositories` — EF implementations of domain repository interfaces plus `SupportToolsServerUnitOfWork`.
- `SupportToolsServer.Api` — minimal-API endpoint groups (`Endpoints/V1/*Endpoints.cs`) exposing `Use...Endpoints` extension methods, aggregated in `UseSupportToolsServerApi`. Endpoints inject `ICommandHandler<TCommand>` directly and map `Result` to `TypedResults` / `CustomResults.Problem`. Routes come from `SupportToolsServerApiRoutes` in the Shared repo.

**Old design (still referenced by the host)** — `SupportToolsServerApplication` (services discovered via the `IScopedServiceSupportToolsServerApplication` marker interface), `SupportToolsServerQueryRepositories` (EF), `SupportToolsServerCommandRepositories` (Dapper via `IDbConnectionFactory`), `SupportToolsServerApiKeyIdentity`.

**Tracked but outside the solution (dead code, not built):** `SupportToolsServerApi`, `SupportToolsServerMappers`, `LibSupportToolsServerRepositories`. Don't extend these. Ask before deleting them.

`SupportToolsServerDbContext` is registered twice (`AddSupportToolsServerPersistence` and `AddSupportToolsServerDb`). This is a leftover of the migration.

## Conventions

- DI extension methods take `ILogger? debugLogger` and log `"{MethodName} Started"` / `"{MethodName} Finished"`. Follow this pattern for new registration and `Use...` methods.
- Many code comments are in Georgian. Keep the language of surrounding comments.
- New projects/repos must also be registered in `D:\1WorkSecurity\SupportTools\SupportTools.json`, which lives outside every repo.
