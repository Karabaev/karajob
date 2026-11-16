# ApplicationDbContextAbstraction

## Status
Accepted. Implemented for issue #9.

## Context
The scenario services (`CompanyService`, later `VacancyService`, `HypothesisService` …) live in `Karajob.Application` and must read and write data. `KarajobDbContext` lives in `Karajob.Infrastructure`, and an Application → Infrastructure reference goes against the dependency direction. design.md rejects a repository layer of its own: EF Core is already an abstraction over the DBMS, and repositories would only add code.

## Decisions

### 1. The context and factory interfaces live in Application
**Decision.** `Karajob.Application.Persistence.IKarajobDbContext` declares the `DbSet<T>` of all entities and `SaveChangesAsync`; `IKarajobDbContextFactory.CreateDbContextAsync` creates a short-lived context per operation. `KarajobDbContext` implements the interface, and `KarajobDbContextFactory` wraps `IDbContextFactory<KarajobDbContext>` and is registered as a singleton in `AddKarajobPersistence`. Services write LINQ against the `DbSet`s directly.

**Alternatives.**
- Repositories (`ICompanyRepository`) — a method per query or a leaking `IQueryable`; they add no portability, and mocks are not needed (services are tested on SQLite in-memory with real migrations).
- Implementing the services in Infrastructure with only interfaces in Application — the business rules (validation, uniqueness) would move out of Application.

### 2. Application depends on the `Microsoft.EntityFrameworkCore` package, but not on a provider
**Decision.** `Karajob.Application.csproj` references only `Microsoft.EntityFrameworkCore` (`DbSet`, `IgnoreQueryFilters`, async LINQ). Providers (`Sqlite`, later `Npgsql`) live only in Infrastructure and the migration projects. Enforced by the `ProjectReferenceTests` test.

**Alternatives.**
- Application without EF — would require repositories (see above).
