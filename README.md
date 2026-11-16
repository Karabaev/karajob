# Karajob

Семейный трекер поиска работы: гипотезы → вакансии → отклики → метрики → решение по гипотезе.

Self-hosted: один Docker-контейнер (ASP.NET Core + Blazor Server + SQLite) в домашней сети.

- Дизайн-документ: [docs/design.md](docs/design.md)
- Роадмап и чек-листы фаз: [docs/roadmap.md](docs/roadmap.md)
- Бэклог идей вне текущей фазы: [docs/backlog.md](docs/backlog.md)
- Архитектурные решения (ADR): [docs/adr/](docs/adr/)
- Выкат и восстановление: [docs/deploy.md](docs/deploy.md)

## Запуск в Docker

Один образ `karajob`, одна папка данных на хосте, смонтированная как `/data` (SQLite, бэкапы перед миграциями, ключи Data Protection).

Локально — сборка из исходников корневым `docker-compose.yml`:

```bash
cp .env.example .env
mkdir -p data && sudo chown 1654:1654 data   # только Linux: контейнер работает от пользователя app (UID 1654)
docker compose up -d --build
```

Приложение — на `http://<хост>:8080` (порт и папка данных — в `.env`). Локальное обновление: `git pull && docker compose up -d --build`; миграции применятся при старте, перед ними база копируется в `data/backups/`. Раз в сутки приложение кладёт снимок базы в `data/backups/daily/`.

На сервере ничего не собирается: CI на каждый push в `master` публикует образ `ghcr.io/karabaev/karajob` с тегами `sha-<коммит>` и `latest`, а сервер запускает явный тег через [docs/deploy/compose.yaml](docs/deploy/compose.yaml). Выкат, обновление и откат по тегу, бэкап вне сервера (restic), восстановление: [docs/deploy.md](docs/deploy.md).

Проверка, что пересборка образа и пересоздание контейнера не теряют данные (её же гоняет CI):

```bash
bash scripts/verify-docker-persistence.sh
```

## Процесс

- Задачи — GitHub Issues, сгруппированы по milestone'ам фаз (`Phase 0`, `Phase 0.5`, …).
- Ветка на задачу `feature/<issue>-<slug>`, история ветки линейная (rebase), в `master` — только через PR с merge commit.
- Для Claude Code: `/task <номер issue>` — полный цикл (уточнение → план → тесты → реализация → ревью → PR).
