# Karajob

A family job-search tracker: tracks → vacancies → applications → metrics → a decision on the track.

Self-hosted: a single Docker container (ASP.NET Core + Blazor Server + SQLite) on a home network.

- Design document: [docs/design.md](docs/design.md)
- Roadmap and phase checklists: [docs/roadmap.md](docs/roadmap.md)
- Backlog of ideas outside the current phase: [docs/backlog.md](docs/backlog.md)
- Architecture decisions (ADR): [docs/adr/](docs/adr/)
- Rollout and recovery: [docs/deploy.md](docs/deploy.md)

## Running in Docker

One `karajob` image, one data folder on the host mounted as `/data` (SQLite, backups before migrations, Data Protection keys).

Locally, build from source with the root `docker-compose.yml`:

```bash
cp .env.example .env
mkdir -p data && sudo chown 1654:1654 data   # Linux only: the container runs as the app user (UID 1654)
docker compose up -d --build
```

The app is at `http://<host>:8080` (port and data folder are set in `.env`). Local update: `git pull && docker compose up -d --build`; migrations are applied at startup, and the database is copied to `data/backups/` before them. Once a day the app puts a database snapshot into `data/backups/daily/`.

Nothing is built on the server: on every push to `master`, CI publishes the `ghcr.io/karabaev/karajob` image with the tags `sha-<commit>` and `latest`, and the server runs an explicit tag via [docs/deploy/compose.yaml](docs/deploy/compose.yaml). Rollout, update and rollback by tag, off-server backup (restic), recovery: [docs/deploy.md](docs/deploy.md).

Check that rebuilding the image and recreating the container do not lose data (CI runs the same check):

```bash
bash scripts/verify-docker-persistence.sh
```

## Process

- Tasks are GitHub Issues, grouped by phase milestones (`Phase 0`, `Phase 0.5`, …).
- One branch per task, `feature/<issue>-<slug>`, linear branch history (rebase), into `master` only via a PR with a merge commit.
- For Claude Code: `/task <issue number>` — the full cycle (clarification → plan → tests → implementation → review → PR).
