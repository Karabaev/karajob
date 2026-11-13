#!/usr/bin/env bash
# Builds the image, starts the container on a throwaway data folder, rebuilds the image from scratch,
# recreates the container and checks that the database and Data Protection keys survived.
# Usage (from anywhere): bash scripts/verify-docker-persistence.sh
set -euo pipefail

cd "$(dirname "$0")/.."

if [[ ! -f .env ]]; then
  echo "FAIL: .env not found. Run: cp .env.example .env" >&2
  exit 1
fi

# Git Bash on Windows would otherwise rewrite /data-like arguments into Windows paths.
export MSYS_NO_PATHCONV=1

data_dir="$(mktemp -d)"
if command -v cygpath > /dev/null; then
  data_dir="$(cygpath -m "$data_dir")"
fi
# The container runs as the non-root app user (UID 1654).
chmod 777 "$data_dir"

export KARAJOB_DATA_DIR="$data_dir"
export KARAJOB_PORT="${VERIFY_PORT:-18080}"
compose=(docker compose -p karajob-verify)
url="http://localhost:$KARAJOB_PORT/"

cleanup() {
  "${compose[@]}" down --remove-orphans > /dev/null 2>&1 || true
  # Files are owned by the container user, so remove them from inside a container.
  docker run --rm --user root --entrypoint rm -v "$data_dir:/d" karajob -rf /d/keys /d/backups /d/karajob.db \
    /d/karajob.db-shm /d/karajob.db-wal > /dev/null 2>&1 || true
  rm -rf "$data_dir" || true
}
trap cleanup EXIT

fail() {
  echo "FAIL: $1" >&2
  "${compose[@]}" logs --no-color >&2 || true
  exit 1
}

wait_for_http() {
  for _ in $(seq 1 60); do
    if [[ "$(curl -s -o /dev/null -w '%{http_code}' "$url" || true)" == "200" ]]; then
      return 0
    fi
    sleep 1
  done
  fail "$url did not return 200 within 60 s"
}

expect_log() {
  "${compose[@]}" logs --no-color karajob | grep -Eq "$1" || fail "log line matching '$1' not found"
}

key_files() {
  ls "$data_dir/keys" 2> /dev/null | sort
}

echo "== First start on empty data folder $data_dir"
"${compose[@]}" up -d --build
wait_for_http
[[ -f "$data_dir/karajob.db" ]] || fail "karajob.db was not created in the data folder"
expect_log "Applying [0-9]+ migration"
keys_before="$(key_files)"
[[ -n "$keys_before" ]] || fail "no Data Protection keys were written to /data/keys"

echo "== Rebuild image without cache and recreate container"
"${compose[@]}" down
"${compose[@]}" build --no-cache
"${compose[@]}" up -d --force-recreate
wait_for_http
expect_log "Database is up to date"
[[ "$(key_files)" == "$keys_before" ]] || fail "Data Protection keys changed after recreation"

echo "OK: database and keys survived image rebuild and container recreation"
