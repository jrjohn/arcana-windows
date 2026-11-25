# AGENTS.md — arcana-windows

Instructions for any AI coding agent (Claude Code, Codex, Cursor, Copilot, Gemini…) working
in this repository **or in any product created from it**. `CLAUDE.md`, `GEMINI.md` and
`.github/copilot-instructions.md` all point here; this file is the single source.

> **You MUST NOT report a task as done until `tools/arch-check.sh` exits 0 and the CI lanes
> (§5) are green.** Breaking the layering costs a red build: the pre-commit hook
> (`tools/githooks/pre-commit`) rejects new `.cs` files outside the layer directories and
> concrete `*Impl` references outside the DI composition roots, and Jenkins stage
> "Architecture Qube" fails every branch and PR on any critical violation or a score below 90.

## 0. First command in a fresh clone

Run `tools/install-hooks.sh` once. Git does not install hooks on clone; until you run it the
pre-commit check does not exist and only CI catches a flat layout — after the work is
written. `tools/new-app.sh` runs it for you. On Windows, run the `tools/*.sh` scripts from
Git Bash.

## 1. Starting something new

| Situation | Do this | Never do this |
|---|---|---|
| New desktop product | `tools/new-app.sh <ProductName> <dest-dir>` — copies the whole repo, renames the solution prefix / namespaces / assemblies, MSIX identity, app-data folder, Jenkins APP_NAME, image and Sonar names, resets the version to 0.1.0, installs the hook | `dotnet new winui` and copy a few folders into it |
| New business feature (entity + list page) | `tools/new-feature.sh <Name>` — one vertical slice: entity, validator, service interface + impl, repository interface + impl, DAO interface + impl, `AppDbContext` DbSet, DI registration, ViewModel (Input/Output/Effect) + page, NavGraph route, MainWindow page map, strings, unit tests | Query `AppDbContext` from a ViewModel or page, or add `src/Arcana.App/Foo.cs` |
| More screens for a feature | Add `Views/<X>DetailPage.xaml(.cs)` + `ViewModels/<Xs>/<X>DetailViewModel.cs` mirroring `OrderDetailPage` / `OrderDetailViewModel`; register the ViewModel in `App.xaml.cs`, the route in `Navigation/NavGraph.cs` | Put state or service calls in the page code-behind |
| Menu entries / commands | A built-in plugin in `src/Arcana.App/Plugins/` like `CustomerModulePlugin.cs`, registered in `App.xaml.cs` | Hard-code menu items in `MainWindow.xaml.cs` |

Copying parts instead of `new-app.sh` loses this file, the hook and the CI gate, so nothing
tells the next agent the rules exist.

## 2. Layers — where a file goes

Projects live under `src/` (prefix `Arcana`, or the product name after `new-app.sh`). Call
direction is one way: page → ViewModel → service interface → (service impl) → repository →
DAO → `AppDbContext` (EF Core + SQLite, local-first).

| Layer | Project / folders | Contains | May depend on (csproj references) |
|---|---|---|---|
| Presentation | `Arcana.App/Views/`, `ViewModels/` (`ViewModels/<Feature>/`), `Navigation/`, `Plugins/` (+ `Plugins/<Module>/locales`), `Controls/`, `Converters/`, `FloatingWindows/`, `Services/` (UI-only: theme, settings, documents) | WinUI 3 pages (XAML + thin code-behind), ViewModels extending `ReactiveViewModelBase` with `In` / `Out` / `Fx`, `NavGraph` routes, built-in module plugins | Core, Domain (entities + service **interfaces**), Plugins.Contracts; Infrastructure only from `App.xaml.cs` |
| App composition root | `Arcana.App/App.xaml.cs` | `AddArcanaServices(...)`, ViewModel / UI-service registration, built-in plugin registration, seed data | everything |
| Domain | `Arcana.Domain/Entities/` (+ `Entities/Identity/`), `Services/`, `Validation/` | entities (`BaseEntity`: audit, soft delete, sync), service **interfaces** (`CustomerService`), FluentValidation validators | Core |
| Infrastructure | `Arcana.Infrastructure/Services/impl/`, `Security/`, `Settings/`, `Platform/`, `Localization/`, `DependencyInjection/` | service implementations (`CustomerServiceImpl`), auth / tokens / password hashing, localization, network monitor; `DependencyInjection/ServiceCollectionExtensions.cs` is the DI composition root | Core, Domain, Data, Sync, Plugins, Plugins.Contracts |
| Data | `Arcana.Data/Repository/` + `Repository/impl/`, `Dao/` + `Dao/impl/`, `Local/` | repository interfaces + `RepositoryImpl<T>` / `XRepositoryImpl`, DAO interfaces + `XDaoImpl` (the only place with LINQ-to-EF queries), `AppDbContext` | Core, Domain |
| Core | `Arcana.Core/Common/`, `Extensions/`, `Security/` | `Result<T>`, `AppError`, `PagedResult` / `PageRequest`, generic `Repository<T>` / `UnitOfWork` interfaces, extensions | nothing |
| Sync | `Arcana.Sync/` (+ `Crdt/`) | offline sync service, CRDT registers / vector clocks / conflict resolution | Core, Data |
| Plugin runtime | `Arcana.Plugins/Core/`, `Data/`, `Services/` | plugin loading, manifests, menu / view / command registries, message bus | Plugins.Contracts |
| Plugin SDK | `Arcana.Plugins.Contracts/` (+ `Manifest/`, `Mvvm/`, `Validation/`) | plugin interfaces, `LocalizationService`, `ReactiveViewModelBase`, `EffectSubject` | nothing |
| External plugins | `plugins/<Module>/` (`Views/`, `ViewModels/`, `Models/`, `Services/`, `Navigation/`, `Controls/`, `Tests/`, `TestHost/`, `<Module>Plugin.cs`) | separately built plugin assemblies (FlowChart is the reference) | Plugins.Contracts |
| Tests | `tests/Arcana.<Project>.Tests/<Folder>/` | xUnit + FluentAssertions + Moq, EF Core InMemory | the project under test |

There is no use-case layer: business rules live in the service impls. Do not describe or
create layers this table lacks. `Arcana.App` is WinUI 3 (`net10.0-windows`): it builds only on
Windows; every other project targets `net10.0` and builds anywhere.

## 3. Rules

| # | Rule | Enforced by |
|---|---|---|
| R1 | Every service, repository and DAO is an interface plus an `XServiceImpl` / `XRepositoryImpl` / `XDaoImpl` class in the `impl/` folder next to the interface's layer (`Services/impl` lives in Infrastructure). Interfaces carry no `I` prefix (`CustomerService`). | arch-qube "Interface/Impl Colocation", "Implementation Naming Convention" |
| R2 | Only the composition roots — `src/*.Infrastructure/DependencyInjection/*.cs` and `src/*.App/App.xaml.cs` — name a concrete `*Impl` type. Everything else injects the interface. Tests may construct impls. | arch-qube "Only DI Container Imports Impl"; pre-commit hook |
| R3 | Pages never resolve or call a domain service, repository, DAO or `AppDbContext`; they talk to their ViewModel only (`ViewModel.In.*`, bind `ViewModel.Out.*`, subscribe `ViewModel.Fx.*`). | arch-qube "View Cannot Call Service", "Unidirectional Data Flow" |
| R4 | Project references go downward only, as in the §2 table. A lower layer never references a higher one (Domain never references Data or App). | arch-qube "Layer Direction Enforcement"; the csproj graph (a back-reference is a build cycle) |
| R5 | A ViewModel exposes exactly `In` (actions), `Out` (read-only state) and `Fx` (`EffectSubject` one-time events), extending `ReactiveViewModelBase`. Page code-behind only wires (≤ 20 lines per handler). | arch-qube "No Business Logic in Boundary"; "MVVM I/O/E Structure" is AI-only (NOT EVALUATED with `--no-ai`) — code review |
| R6 | Navigation goes through `NavGraph` (`Routes.*` constants + `To<X>()` methods), not page-name strings in ViewModels. | arch-qube "Type-Safe NavGraph" |
| R7 | No new `.cs` file outside the layer folders of §2 (no new files at a project root). | pre-commit hook |
| R8 | Every new service impl, repository and validator gets tests mirroring `CustomerServiceTests` (Moq), `OrderRepositoryTests` (EF InMemory) and `CustomerValidatorTests`. | arch-qube "Test Coverage Threshold"; Jenkins "Unit Tests" + SonarQube quality gate |

## 4. Architecture check (arch-qube)

arch-qube (https://github.com/jrjohn/arcana-arch-qube) scans the source for layer-direction,
DI, MVVM and naming violations and scores it; usage guide:
https://github.com/jrjohn/arcana-skills/tree/main/arch-qube-skill. The version is pinned in
`tools/arch-qube.version`, read by both `tools/arch-check.sh` and the Jenkinsfiles.

`tools/arch-qube.version` is bumped automatically by Renovate when arch-qube releases; do not edit the version anywhere else.

- Run: `tools/arch-check.sh`. It uses `ghcr.io/jrjohn/arch-qube:<version>` (public,
  multi-arch), then `arcana.boo/arcana/arch-qube:<version>`, with the same
  `--framework windows`, `--threshold 90` and exclusions (`bin/`, `obj/`, test results) as
  Jenkins stage "Architecture Qube". Exit 0 = pass, 1 = fail, 2 = could not run (no Docker /
  neither image pullable) — 2 is **not** a pass. There is no `:latest` fallback on purpose.
- Without Docker: on a tree without `bin/` and `obj/` folders, run
  `pipx run --spec "git+https://github.com/jrjohn/arcana-arch-qube.git@v$(cat tools/arch-qube.version)" arch-qube scan . --framework windows --no-ai --ci --format json,markdown -o arch-qube-reports --threshold 90`.
- Report: `arch-qube-reports/arch-qube.md` (summary) and `arch-qube.json` (per rule:
  `files_checked`, violations). PASS needs the score ≥ threshold **and** zero violations in
  every critical rule — a 99.5 can still FAIL. Read it rule by rule:
  - a rule under "Not evaluated" (here: MVVM I/O/E Structure, MVVM I/O/E Models Binding,
    4-Layer Progressive Cache, Offline-First, Defense-in-Depth — AI-only with `--no-ai`;
    Module Boundary — not applicable) is **not** a pass for code you touched; review it by
    hand against §3;
  - a rule with `files_checked: 0` checked nothing;
  - an unlayered project scores 0 on "Layer Direction Enforcement".
- `--ci` prints nothing on failure. Rerun without `--ci` (or open `arch-qube.md`) to see why.
- False positive: open an issue on arcana-arch-qube with the report attached. Never lower
  `--threshold`, add exclusions, or move code to dodge a rule.

## 5. Before you say "done"

Run these, in this order, and paste the last line of each into your summary:

```bash
tools/arch-check.sh                                                     # must print "arch-check: PASS"
docker compose -f docker-compose.test.yml run --rm --build test         # Jenkinsfile.dotnet "Unit Tests" (Linux, all net10.0 projects); must exit 0
```

On Windows, also run what `Jenkinsfile.windows` runs on the `windows` agent (the only lane
that compiles the WinUI 3 app):

```bat
dotnet restore Arcana.Windows.sln
dotnet build src\Arcana.App\Arcana.App.csproj -c Release -p:Platform=x64
dotnet test -c Release -p:Platform=x64 --settings tests.runsettings
```

There is no lint stage; the compiler (nullable enabled) is the static check. CI also runs the
SonarQube quality gate, which you cannot run locally. On macOS / Linux you cannot compile
`Arcana.App`: say so in your summary — do not claim the WinUI build passed.

## 6. Reference

- Optional detail: `arcana-windows-developer-skill`
  (https://github.com/jrjohn/arcana-skills/tree/main/arcana-windows-developer-skill).
  This file is the source of truth: if the skill and this file disagree, this file wins.
