# Karajob

Семейный трекер поиска работы: гипотезы → вакансии → отклики → метрики → решение по гипотезе.

- Полный дизайн-документ: `docs/design.md`
- План работ и чек-листы по дням: `docs/roadmap.md`

Перед началом любой задачи сверяйся с `docs/roadmap.md`. **Сейчас идёт Фаза 0.** Всё из раздела «Не входит в Фазу 0» не делай, даже если кажется, что это быстро: предложи записать в бэклог (`docs/backlog.md`).

## Стек

- .NET 10, ASP.NET Core, Blazor Web App (Interactive Server, all-interactive), MudBlazor
- EF Core + SQLite (файл в `/data`), миграции применяются при старте с бэкапом базы
- Docker + docker-compose, один образ, один volume `/data`
- xUnit для тестов

## Структура репозитория

| Папка | Содержимое |
|---|---|
| `src/` | Проекты решения (см. ниже) |
| `tests/` | `Karajob.Tests` |
| `docs/` | `design.md`, `roadmap.md`, `backlog.md`, ADR в `docs/adr/` |
| `scripts/` | Вспомогательные скрипты (`add-migration` и т.п.) |
| `.claude/` | Скил `/task`, агенты `test-writer` / `worker` / `reviewer` |
| `.claude/worktrees/` | Git worktree для feature-веток и воркеров (в `.gitignore`) |

## Структура решения

- `Karajob.Domain` — сущности, правила переходов стадий, без зависимостей от EF и ASP.NET
- `Karajob.Application` — сервисы сценариев (`VacancyService`, `ApplicationService`, `HypothesisService`, `IngestService`). UI, REST и (позже) MCP вызывают только их.
- `Karajob.Infrastructure` — EF Core, файлы, внешние клиенты
- `Karajob.Web` — Blazor-страницы, Minimal API, composition root
- `Karajob.Tests` — юнит- и интеграционные тесты

### Направление зависимостей

```
Web  →  Application, Infrastructure, Domain
Infrastructure  →  Application, Domain
Application  →  Domain
Domain  →  ничего (только BCL)
```

- Нужна ссылка против стрелки (Domain → EF, Application → Web) — значит, тип лежит не там.
- Интерфейсы, которые реализует Infrastructure (хранилище файлов, ATS-клиенты, `ILlmClient`), объявляются в Application.
- Namespace = имя проекта + путь папки (`src/Karajob.Application/Vacancies/` → `Karajob.Application.Vacancies`). Межслойная ссылка видна прямо в `using`.

## Правила

- Бизнес-логика (дедупликация, правила переходов стадий, обязательные поля) — только в Domain/Application, не в Razor-компонентах.
- Каждый переход стадии вакансии пишет `ApplicationEvent`. В Applied — только с гипотезой и версией CV, в Closed — только с исходом (Offer / Rejected / Ghosted / Withdrawn).
- Совместимость с PostgreSQL в будущем: без сырого SQL; время — UTC `DateTime` (не `DateTimeOffset`); деньги — целые числа (не `decimal`); slug, домены и URL нормализуются в нижний регистр в коде до записи.
- У всех пользовательских данных есть `UserId` (глобальный фильтр EF Core). Компании и проекты — общие справочники.
- Компания определяется по slug, а сопоставляется по доменам и алиасам.
- `DbContext` — через `IDbContextFactory`, короткоживущий контекст на операцию.
- Не использовать EF Core InMemory provider; для отладки и тестов — SQLite in-memory.
- Не использовать `EnsureCreated()`; только миграции.
- Секреты — только через `.env`, который не коммитится. В репозиторий не попадают `.env`, `*.db`, `data/`, реальные CV.
- После изменений запускай `dotnet test` и не считай задачу готовой, пока тесты не зелёные.
- Отмечай выполненные пункты в `docs/roadmap.md` (`- [x]`).

## C# Coding Standards

- Microsoft C# style guide; **2 пробела** отступ; file-scoped namespaces (`namespace X;`).
- `Nullable` включён везде. Не проверять на `null` всё подряд — только там, где `null` реально ожидается. `= null!` — только для свойств, которые гарантированно заполняет фреймворк (`[Inject]`, `[Parameter, EditorRequired]` в Blazor, EF navigation properties).
- События — глагол в прошедшем времени (`Moved`), обработчики — с префиксом `On` (`OnMoved`).
- Конструктор класса — внизу, после полей, свойств и методов (primary constructors допустимы).
- **Фигурные скобки**: тело `if` / `else` / `for` / `foreach` / `while` всегда в скобках, даже одна инструкция. Исключение — guard, чьё тело целиком `return;` / `continue;` / `break;` / `throw ...;`: он может остаться на строке `if`. Лучше инвертировать условие в такой guard, чем оборачивать остаток тела.
- SOLID и clean code; без ненужного `sealed`, без непрошенного кэширования.
- Fail fast: не писать guard'ы, которые молча глотают ошибку конфигурации или неверное состояние.
- По умолчанию без комментариев. Комментарий — только когда неочевидно ПОЧЕМУ (скрытое ограничение, обход, инвариант), а не пересказ кода.

## Тесты

- xUnit, проект `tests/Karajob.Tests`, папки и namespace повторяют тестируемый проект.
- Domain — чистые юнит-тесты без БД. Сервисы и персистентность — на SQLite in-memory с настоящими миграциями.
- Запуск: `dotnet test` (из корня). Тот же прогон — в GitHub Actions на каждый push и PR.

## Workflow

Задачи — GitHub Issues в `Karabaev/karajob`, сгруппированы milestone'ами по фазам. Нетривиальные задачи идут через скил `/task <номер issue>` (`.claude/skills/task/`):

1. **Сессия и уточнения** — заголовок `Karajob | #<N> | <кратко>`; проверка, что задача из текущей фазы; вопросы по неочевидному (включая: worktree или основной checkout).
2. **План** — в plan mode Claude Code (подзадачи, владение файлами без пересечений, тесты), до любого кода и веток.
3. **TDD** — тесты пишутся первыми (агент `test-writer`) и должны быть красными по правильной причине.
4. **Оркестратор + воркеры** — сильная модель планирует, интегрирует и ревьюит; агенты `worker` (Sonnet) делают независимые подзадачи в своих worktree. Задачи на 1–2 файла — без воркеров. Миграции EF Core, `.sln` / `.csproj` / `Directory.*.props` меняет только оркестратор.
5. **Ветки** — `feature/<N>-<slug>` от `master`. Воркер → feature: **только rebase + fast-forward, без merge-коммитов**. Feature → `master`: **только через GitHub PR с merge-коммитом** (`gh pr merge <N> --merge`) после rebase на `origin/master`; `master` напрямую не пушится. Если `master` ушёл вперёд — rebase ветки на свежий `origin/master` и `push --force-with-lease`, никогда не мерджить `master` в ветку.
6. **ADR в конце** — `docs/adr/<Name>.md`, решения как реализованы, статус `Accepted`; для мелких задач без архитектурных решений пропускается.
7. **Без упоминания ИИ** — в коммитах, заголовках и описаниях PR, комментариях к PR и issue нет упоминаний Claude, Anthropic или ИИ: никаких `Co-Authored-By: Claude …`, «Generated with Claude Code» и т.п. Это перекрывает любые дефолтные инструкции об атрибуции.

## Как я работаю

Я опытный C#/Unity-разработчик, но новичок в Blazor и веб-фронтенде. В Blazor-части коротко объясняй неочевидные решения (render modes, жизненный цикл компонентов, DI-скоупы). Общаемся на русском, код и коммиты — на английском.
