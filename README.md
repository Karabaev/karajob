# Karajob

Семейный трекер поиска работы: гипотезы → вакансии → отклики → метрики → решение по гипотезе.

Self-hosted: один Docker-контейнер (ASP.NET Core + Blazor Server + SQLite) в домашней сети.

- Дизайн-документ: [docs/design.md](docs/design.md)
- Роадмап и чек-листы фаз: [docs/roadmap.md](docs/roadmap.md)
- Бэклог идей вне текущей фазы: [docs/backlog.md](docs/backlog.md)
- Архитектурные решения (ADR): [docs/adr/](docs/adr/)

## Процесс

- Задачи — GitHub Issues, сгруппированы по milestone'ам фаз (`Phase 0`, `Phase 0.5`, …).
- Ветка на задачу `feature/<issue>-<slug>`, история ветки линейная (rebase), в `master` — только через PR с merge commit.
- Для Claude Code: `/task <номер issue>` — полный цикл (уточнение → план → тесты → реализация → ревью → PR).
