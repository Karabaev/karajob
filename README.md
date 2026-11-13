# Karajob

Семейный трекер поиска работы: гипотезы → вакансии → отклики → метрики → решение по гипотезе.

Self-hosted: один Docker-контейнер (ASP.NET Core + Blazor Server + SQLite) в домашней сети.

- Дизайн-документ: [docs/design.md](docs/design.md)
- Роадмап и чек-листы фаз: [docs/roadmap.md](docs/roadmap.md)
- Бэклог идей вне текущей фазы: [docs/backlog.md](docs/backlog.md)
- Архитектурные решения (ADR): [docs/adr/](docs/adr/)

## Запуск в Docker

Один образ `karajob`, одна папка данных на хосте, смонтированная как `/data` (SQLite, бэкапы перед миграциями, ключи Data Protection).

```bash
cp .env.example .env
mkdir -p data && sudo chown 1654:1654 data   # только Linux: контейнер работает от пользователя app (UID 1654)
docker compose up -d --build
```

Приложение — на `http://<хост>:8080` (порт и папка данных — в `.env`). Обновление: `git pull && docker compose up -d --build`; миграции применятся при старте, перед ними база копируется в `data/backups/`. Бэкап — копия папки `data/`.

Проверка, что пересборка образа и пересоздание контейнера не теряют данные (её же гоняет CI):

```bash
bash scripts/verify-docker-persistence.sh
```

## Процесс

- Задачи — GitHub Issues, сгруппированы по milestone'ам фаз (`Phase 0`, `Phase 0.5`, …).
- Ветка на задачу `feature/<issue>-<slug>`, история ветки линейная (rebase), в `master` — только через PR с merge commit.
- Для Claude Code: `/task <номер issue>` — полный цикл (уточнение → план → тесты → реализация → ревью → PR).
