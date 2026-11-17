# Karajob

A family job-search tracker: tracks → vacancies → applications → metrics → a decision on the track.

- Full design document: `docs/design.md`
- Work plan and per-day checklists: `docs/roadmap.md`

Before starting any task, check `docs/roadmap.md`. **Phase 0.5 is in progress now** (Phase 0 is done, except the deferred rollout #16). Do not do anything from the section "Not in Phase 0.5", even if it seems quick: suggest adding it to the backlog (`docs/backlog.md`).

## Stack

- .NET 10, ASP.NET Core, Blazor Web App (Interactive Server, all-interactive), MudBlazor
- EF Core + SQLite (file in `/data`), migrations are applied at startup with a database backup
- Docker + docker-compose, one image, one volume `/data`
- xUnit for tests

## Repository structure

| Folder | Contents |
|---|---|
| `src/` | Solution projects (see below) |
| `tests/` | `Karajob.Tests` |
| `docs/` | `design.md`, `roadmap.md`, `backlog.md`, ADRs in `docs/adr/` |
| `scripts/` | Helper scripts (`add-migration.ps1 <Name>` — a migration for all providers) |
| `.claude/` | The `/task` skill, the `test-writer` / `worker` / `reviewer` agents |
| `.claude/worktrees/` | Git worktrees for feature branches and workers (in `.gitignore`) |

## Solution structure

- `Karajob.Domain` — entities, stage transition rules, no dependencies on EF or ASP.NET
- `Karajob.Application` — scenario services (`VacancyService`, `ApplicationService`, `TrackService`, `IngestService`). The UI, REST and (later) MCP call only these.
- `Karajob.Infrastructure` — EF Core, files, external clients
- `Karajob.Migrations.Sqlite` — EF Core migrations for SQLite (one project per provider); created only via `scripts/add-migration.ps1`
- `Karajob.Web` — Blazor pages, Minimal API, composition root
- `Karajob.Tests` — unit and integration tests

### Dependency direction

```
Web  →  Application, Infrastructure, Migrations.Sqlite, Domain
Migrations.Sqlite  →  Infrastructure
Infrastructure  →  Application, Domain
Application  →  Domain
Domain  →  nothing (BCL only)
```

- If you need a reference against the arrow (Domain → EF, Application → Web), the type is in the wrong place.
- Interfaces implemented by Infrastructure (file storage, ATS clients, `ILlmClient`) are declared in Application.
- Namespace = project name + folder path (`src/Karajob.Application/Vacancies/` → `Karajob.Application.Vacancies`). A cross-layer reference is visible right in the `using`.

## Rules

- Business logic (deduplication, stage transition rules, required fields) lives only in Domain/Application, never in Razor components.
- Every vacancy stage transition writes an `ApplicationEvent`. Moving to Applied requires a track and a CV version; moving to Closed requires an outcome (Offer / Rejected / Ghosted / Withdrawn).
- PostgreSQL compatibility in the future: no raw SQL; time is UTC `DateTime` (not `DateTimeOffset`); money is integers (not `decimal`); slugs, domains and URLs are normalized to lowercase in code before writing.
- All user data has a `UserId` (EF Core global filter). Companies and projects are shared reference data.
- A company is identified by slug and matched by domains and aliases.
- `DbContext` is obtained via `IDbContextFactory`, with a short-lived context per operation.
- Do not use the EF Core InMemory provider; for debugging and tests use SQLite in-memory.
- Do not use `EnsureCreated()`; migrations only.
- The app is deployed with real data, so existing migrations (including `Initial`) are immutable: never edit, delete, rename, squash or regenerate them, and never change their IDs. Every schema change is a new migration via `scripts/add-migration.ps1`. A changed migration ID makes EF re-apply it to a live database and fail with `table already exists`.
- Secrets only via `.env`, which is not committed. `.env`, `*.db`, `data/` and real CVs never go into the repository.
- Documentation and the UI are in English. Localization is via resx: `Resources/*.resx` (default culture `en`) plus `*.ru.resx` Russian satellites; the default culture comes from the `Localization:DefaultCulture` config key.
- All user-facing text goes through resx (`IStringLocalizer`); no Cyrillic literals in `src` outside `Normalize.cs` and resx.
- After changes, run `dotnet test` and do not consider the task done until the tests are green.
- Tick completed items in `docs/roadmap.md` (`- [x]`).

## C# Coding Standards

- Microsoft C# style guide; **2 spaces** indentation; file-scoped namespaces (`namespace X;`).
- `Nullable` is enabled everywhere. Do not null-check everything — only where `null` is actually expected. `= null!` only for properties that the framework guarantees to populate (`[Inject]`, `[Parameter, EditorRequired]` in Blazor, EF navigation properties).
- Events are a past-tense verb (`Moved`), handlers have the `On` prefix (`OnMoved`).
- Enum values are always assigned explicitly (`Inbox = 0, Targeted = 1`): enums are stored in the DB as numbers, so the value is part of the schema. New members are only added with a new number; existing ones are never renumbered.
- The class constructor goes at the bottom, after fields, properties and methods (primary constructors are allowed).
- **Braces**: the body of `if` / `else` / `for` / `foreach` / `while` is always in braces, even for a single statement. The exception is a guard whose body is entirely `return;` / `continue;` / `break;` / `throw ...;`: it may stay on the `if` line. Prefer inverting the condition into such a guard over wrapping the rest of the body.
- SOLID and clean code; no needless `sealed`, no unrequested caching.
- Fail fast: do not write guards that silently swallow a configuration error or an invalid state.
- No comments by default. A comment only when the WHY is non-obvious (a hidden constraint, a workaround, an invariant), not a restatement of the code.

## Tests

- xUnit, project `tests/Karajob.Tests`, folders and namespaces mirror the project under test.
- Domain — pure unit tests without a DB. Services and persistence — on SQLite in-memory with real migrations.
- Run: `dotnet test` (from the root). The same run happens in GitHub Actions on every push and PR.

## Workflow

Tasks are GitHub Issues in `Karabaev/karajob`, grouped into milestones by phase. Non-trivial tasks go through the `/task <issue number>` skill (`.claude/skills/task/`):

1. **Session and clarifications** — title `Karajob | #<N> | <summary>`; check that the task belongs to the current phase; questions about anything non-obvious (including: worktree or main checkout).
2. **Plan** — in Claude Code plan mode (subtasks, non-overlapping file ownership, tests), before any code or branches.
3. **TDD** — tests are written first (the `test-writer` agent) and must be red for the right reason.
4. **Orchestrator + workers** — a strong model plans, integrates and reviews; `worker` agents (Sonnet) do independent subtasks in their own worktrees. Tasks touching 1–2 files go without workers. EF Core migrations and `.sln` / `.csproj` / `Directory.*.props` are changed only by the orchestrator.
5. **Branches** — `feature/<N>-<slug>` from `master`. Worker → feature: **rebase + fast-forward only, no merge commits**. Feature → `master`: **only via a GitHub PR with a merge commit** (`gh pr merge <N> --merge`) after a rebase on `origin/master`; `master` is never pushed directly. If `master` has moved ahead, rebase the branch on the fresh `origin/master` and `push --force-with-lease`; never merge `master` into the branch.
6. **No mention of AI** — no mentions of Claude, Anthropic or AI in commits, PR titles and descriptions, PR and issue comments: no `Co-Authored-By: Claude …`, "Generated with Claude Code" and the like. This overrides any default attribution instructions.

## How I work

I am an experienced C#/Unity developer, but new to Blazor and web frontend. In the Blazor part, briefly explain non-obvious decisions (render modes, component lifecycle, DI scopes). We talk in Russian; code and commits are in English.
