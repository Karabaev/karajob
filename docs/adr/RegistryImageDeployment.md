# RegistryImageDeployment

## Status
Accepted. Implemented for issue #66.

## Context
The rollout to the home server was a `git clone` + `docker compose up -d --build` right on the server: the sources sit on the server, the image is built there, and a rollback is a `git checkout` of an old commit plus a manual `karajob:prev` tag. The other services on this server are installed from ready-made images, and the same model was wanted: only `compose.yaml`, `.env` and `data/` on the server.

## Decisions

### 1. The server runs a ready image from GitHub Container Registry
**Decision.** CI builds the image and publishes it to `ghcr.io/karabaev/karajob`. The server has [docs/deploy/compose.yaml](../deploy/compose.yaml) without `build:` and a `.env` based on [docs/deploy/.env.example](../deploy/.env.example). The root `docker-compose.yml` with `build: .` stays for local development and `scripts/verify-docker-persistence.sh`. The package is public, so the server needs no login; for a private package, a one-time `docker login ghcr.io` with a PAT scoped to `read:packages` only.

**Alternatives.**
- Building on the server — the server needs the sources and git, the .NET SDK build runs on weak hardware and costs time and disk, and there are no versioned images for rollback: the previous image has to be saved by hand under a tag like `karajob:prev`.
- `docker save | docker load` — the image is moved by hand as a file: there is no history of versions and tags in one place, a rollback means hunting for an old archive, and the archives must be copied to the server by hand or with a custom script.

### 2. Publish only from `master` and only what has been verified
**Decision.** The `publish` job in `.github/workflows/ci.yml` runs only on `push` to `master` and has `needs: [build-and-test, docker]`: only a commit that passed the tests and the `/data` persistence check is published. The image to publish is rebuilt in this job: it comes from the same commit but is not byte-for-byte the one the `docker` job verified. The `packages: write` permission is granted only to this job, the push goes through `GITHUB_TOKEN`, and there are no separate secrets. The image name and the OCI labels (including `org.opencontainers.image.source`, which links the package to the repository) come from `docker/metadata-action`: it also lowercases the owner name `Karabaev`, which the registry requires. The platform is `linux/amd64` only (the server is x86_64), and the layer cache is `type=gha`.

The shared `concurrency` with `cancel-in-progress` stays: if two pushes arrive in `master` quickly, the first one's publish is cancelled and its commit gets no image. This is acceptable — the latest one is what gets rolled out to the server.

**Alternatives.**
- A separate publishing workflow — duplicates the build and tests or needs `workflow_run`, which is harder to read.
- Publishing from feature branches — unverified images land in the registry, and they are easy to roll out by mistake.

### 3. An explicit `sha-` tag on the server, rollback is the previous tag
**Decision.** The image gets the tags `sha-<full commit sha>` and `latest`. The tag is mandatory in the server compose (`${KARAJOB_TAG:?...}`), and there is no `latest` default: the version on the server is always explicit and visible in `.env`. Updating means changing `KARAJOB_TAG` and running `docker compose pull && docker compose up -d`; rolling back means restoring the previous tag. There are no down migrations, so if the new image applied a migration, a rollback also requires restoring the database from `data/backups/<timestamp>-before-<migration>.db`, otherwise the old image fails on the newer schema.

**Alternatives.**
- `latest` on the server — `docker compose pull` silently changes the version, and the previous version is recorded nowhere, so there is nothing to roll back to.
- Semver tags from git tags — releases are not marked yet, and `sha-` links an image to a commit unambiguously.
