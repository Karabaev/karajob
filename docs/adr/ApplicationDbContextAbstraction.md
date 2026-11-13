# ApplicationDbContextAbstraction

## Status
Accepted. Implemented for issue #9.

## Context
Сервисы сценариев (`CompanyService`, дальше `VacancyService`, `HypothesisService` …) живут в `Karajob.Application` и должны читать и писать данные. `KarajobDbContext` лежит в `Karajob.Infrastructure`, а ссылка Application → Infrastructure идёт против направления зависимостей. design.md отказывается от своего слоя репозиториев: EF Core уже абстракция над СУБД, репозитории добавили бы только код.

## Decisions

### 1. Интерфейс контекста и фабрики в Application
**Decision.** `Karajob.Application.Persistence.IKarajobDbContext` объявляет `DbSet<T>` всех сущностей и `SaveChangesAsync`; `IKarajobDbContextFactory.CreateDbContextAsync` создаёт короткоживущий контекст на операцию. `KarajobDbContext` реализует интерфейс, `KarajobDbContextFactory` оборачивает `IDbContextFactory<KarajobDbContext>` и регистрируется singleton'ом в `AddKarajobPersistence`. Сервисы пишут LINQ к `DbSet` напрямую.

**Alternatives.**
- Репозитории (`ICompanyRepository`) — по методу на каждый запрос или протекающий `IQueryable`; переносимости не добавляют, моки не нужны (сервисы тестируются на SQLite in-memory с настоящими миграциями).
- Реализация сервисов в Infrastructure, в Application только интерфейсы — бизнес-правила (валидация, уникальность) уехали бы из Application.

### 2. Application зависит от пакета `Microsoft.EntityFrameworkCore`, но не от провайдера
**Decision.** `Karajob.Application.csproj` ссылается только на `Microsoft.EntityFrameworkCore` (`DbSet`, `IgnoreQueryFilters`, async-LINQ). Провайдеры (`Sqlite`, позже `Npgsql`) — только в Infrastructure и проектах миграций. Закреплено тестом `ProjectReferenceTests`.

**Alternatives.**
- Application без EF — потребовало бы репозиториев (см. выше).
