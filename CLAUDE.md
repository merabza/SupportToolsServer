# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Workspace layout

This repo (`merabza/SupportToolsServer`) is one of several sibling git clones inside the workspace folder `D:\1WorkDotnet\SupportToolsServer\`, which is **not** itself a repo. Project references reach siblings by relative path (`..\..\SystemTools\...`), so these must be cloned next to this repo:

| Sibling | What SupportToolsServer uses from it |
|---|---|
| `SystemTools` | `Application.Abstractions` (CQRS `ICommand`/`ICommandHandler`/`IQueryHandler`, `AddApplication`), `SharedKernel` (`Result`), `Domain.Abstractions` (`IUnitOfWork`), `ApiKeysManagement` |
| `WebSystemTools` | Host plumbing: Serilog, Swagger, API-key auth, SignalR, exception handler, Windows service, static files |
| `SupportToolsServerCore` | `SupportToolsServerCore.Domain` (entities, `Primitives/Entity<TId>` + `ValueObject`, repository interfaces, `Sync/Syncroniser<T,TId>`) and `SupportToolsServerCore.Application.Abstractions` (`ISupportToolsServerDbContext`) |
| `SupportToolsServerDbPart` | `SupportToolsServerDbContext`, `IEntityTypeConfiguration<T>` classes, `SupportToolsServerUnitOfWork` and the database DI (`AddSupportToolsServerDatabase`) |
| `SupportToolsServerShared` | `SupportToolsServerApiContracts` — route constants (`SupportToolsServerApiRoutes`), request/response models and `SupportToolsServerApiClient`, shared with the SupportTools CLI |

`SupportToolsServerCore` and `SupportToolsServerDbPart` are also cloned into the database-tooling workspace `D:\1WorkDotnet\SupportToolsServerDbTools` (together with `SystemTools` and `SupportToolsServerDbTools`, which holds the EF Core migrations and the `dotnet ef` FakeHost). That workspace has no clone of this repo or of `SupportToolsServerShared`, so the two shared repos must never reference them: dependencies point only from this repo towards Core/DbPart. TravelGuide (`TravelGuideCore`/`TravelGuideDbPart`/`TravelGuideDbTools`) and GanmartebaGe use the same split. Each sibling is its own repo: commit changes in the repo where the file lives.

## Build / run

```bash
dotnet build SupportToolsServer.slnx
dotnet run --project SupportToolsServer/SupportToolsServer.csproj
```

- .NET 10, central package management (`Directory.Packages.props` — add versions there, not in csproj). Every sibling repo has its own `Directory.Packages.props`.
- `Directory.Build.props` sets `TreatWarningsAsErrors`, `AnalysisMode=All`, `EnforceCodeStyleInBuild` and SonarAnalyzer, so any analyzer/style warning breaks the build. `ImplicitUsings` is **disabled** — write explicit `using`s.
- Tests: `SupportToolsServer.Tests` (xUnit + Moq, `dotnet test SupportToolsServer.slnx`) covers Application handlers/validators with mocked repositories, the Infrastructure repositories over an in-memory SQLite database built from the real `SupportToolsServerDbContext` model (`Foreign Keys=True`), and the WebApi route mapping (a real `WebApplication` with `AddApplication`), endpoint results and `Debug.WriteLine` traces (asserted under `#if DEBUG`). Application exposes its internals to it via `InternalsVisibleTo`. The sibling repos have their own test projects: `SupportToolsServerCore.Tests`, `SupportToolsServerDbPart.Tests`, `SupportToolsServerApiContracts.Tests` (in `SupportToolsServerShared.slnx`) and `SystemTools.ApiContracts.Tests`.
- The host listens on `http://*:5033` and needs `Data:SupportToolsServerDatabase:ConnectionString` (User Secrets; `appsettings.json` only holds a placeholder). `AddSupportToolsServerDatabase` throws at startup if it is empty or not a valid SQL Server connection string.

## Architecture

Clean Architecture / CQRS, the same layout as `AppGanmartebaGe`. Request flow:

endpoint (`SupportToolsServer.WebApi/Endpoints/V1/*Endpoints.cs`) → command/query → `ICommandHandler`/`IQueryHandler` (`SupportToolsServer.Application/<Area>/<UseCase>/`) → repository interface (`SupportToolsServerCore.Domain`) → EF implementation (`SupportToolsServer.Infrastructure/Repositories`) over `ISupportToolsServerDbContext` → the handler commits with `IUnitOfWork.SaveChangesAsync` → `Result` → `TypedResults` / `CustomResults.Problem`.

- `SupportToolsServer.Application` — one folder per use case (`GitRepos/UpdateGitRepo/`, `GitIgnoreFileTypes/SyncUp/`, …) holding the command/query, its handler and an optional `AbstractValidator`. `AddApplication(debugLogger, typeof(SupportToolsServer.Application.AssemblyReference))` scans this assembly and decorates the handlers with validation + logging, and `AddFluentValidation` (WebSystemTools.ValidationTools) registers the validators, so a new handler or validator needs no DI code. Only **command** handlers get the validation decorator. Validators use the error codes of `SupportToolsServerApiClientErrors` (Shared repo); shared rules live in `Validation/` (`RequiredWithMaxLength`, `UniqueValues`) and the lengths come from the entity constants (`GitRepo.NameMaxLength`, …) that the EF configurations also use.
- `SupportToolsServer.Infrastructure` — EF repositories; a new repository needs an `AddScoped` in `DependencyInjection/SupportToolsServerRepositoriesDependencyInjection.cs` (`AddSupportToolsServerRepositories`).
- `SupportToolsServer.WebApi` — minimal-API endpoint groups exposing `Use...Endpoints` extension methods, aggregated in `UseSupportToolsServerApi`. Endpoints inject `ICommandHandler<TCommand>` directly. Routes come from `SupportToolsServerApiRoutes` in the Shared repo.
- `SupportToolsServer` (host) — `Program.cs` wires `AddSupportToolsServerDatabase` (DbPart: context, `ISupportToolsServerDbContext`, `IUnitOfWork`), `AddApplication`, `AddFluentValidation` and `AddSupportToolsServerRepositories`.

Repositories read with `AsNoTracking` and stage changes with explicit `Add`/`Update`/`Delete` (a new instance with the stored `Id` is passed to `Update`); the handler commits once. Adding a table: entity (+ repository interface) in `SupportToolsServerCore.Domain`, a `DbSet` on both `ISupportToolsServerDbContext` and `SupportToolsServerDbContext`, a configuration in `SupportToolsServerDbPart.Db/Configurations`, then a migration in `SupportToolsServerDbTools` (`AddGitRepos` is the latest after `Initial`; generate it where the Core and DbPart clones are current).

### Endpoints (`api/v1/git`, all anonymous — the groups' `RequireAuthorization()` is commented out)

Consumer: `SupportToolsServerApiClient` of the SupportTools CLI (`LibSupportToolsServerWork/Cruders/GitStsCruder.cs`, `GitIgnoreFileTypesStsCruder.cs`, `LibGitWork/ToolActions/UploadGitProjectsToSupportToolsServerToolAction.cs`, `SyncUpGitignoreFilesCliMenuCommand.cs`). A route with a key has a `…Prefix` constant in `SupportToolsServerApiRoutes`; the client appends `/{Uri.EscapeDataString(key)}`, the server maps the full `…/{key}` template. Keys and names match case-insensitively, like the unique indexes.

- Git repos (`GitReposEndpoints`): `GET gitrepos` (ordered by name), `GET gitrepo/{key}` (404 `GitWithKeyNotFound`), `POST updategitrepo/{key}` (add or update; the route key wins over the body's `GitProjectName`), `DELETE deletegitrepo/{key}`, `POST uploadgitrepos` (`SyncGitRequest`: upserts gitignore types and repos by name in one transaction, never deletes).
- GitIgnore file types (`GitIgnoreFileTypesEndpoints`): `GET gitignorefiletypeslist`, `POST updategitignorefiletype/{key}` (the CLI sends only the name: adds the type with empty content when missing, leaves an existing one unchanged), `DELETE deletegitignorefiletype/{key}`, `POST syncupgitignorefiletypes/{merge?}` (`merge=false` also deletes the server rows that are missing from the upload).

`GitRepo` references its pattern by `GitIgnoreFileTypeId` (FK, `Restrict`), while the contract carries `GitIgnorePatternName`. The CLI's sync-up sends a new `Guid` for every row, so `SyncUp` and `UploadGitRepos` match gitignore rows **by name** and keep the server `Id`; the client's `Id` is ignored. Deleting a type that a repo uses — by route or by `merge=false` — is refused with 409 `GitIgnoreFileTypeIsInUse`, an unknown pattern name with `GitIgnoreFileTypeWithNameNotFound`, and an address that another repo has with 409 `GitAddressIsInUse` (addresses are unique because the CLI's `GitCruder` looks a remote repo up by address). Errors are ProblemDetails from `CustomResults.Problem`; `SystemTools.ApiContracts.ApiClient` reads a code-shaped `title` (no spaces) as the error code, `detail` as the description, the status as the type and the `errors` extension as the list, so the CLI can compare codes such as `GitWithKeyNotFound`.

## Conventions

- DI extension methods take `ILogger? debugLogger` and log `"{MethodName} Started"` / `"{MethodName} Finished"`. Follow this pattern for new registration and `Use...` methods.
- Many code comments are in Georgian. Keep the language of surrounding comments.
- New projects/repos must also be registered in `D:\1WorkSecurity\SupportTools\SupportTools.json`, which lives outside every repo: a `Gits` entry and the `GitProjectNames` of every project record that clones the repo (`SupportToolsServer`, `SupportToolsServerDbTools`).
