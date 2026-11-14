# Deployment and recovery

Karajob runs as one Docker container on a home server. All state lives in one host folder mounted as `/data`. The app has no passwords, so it must stay inside the home network: from outside, reach it only through the router's WireGuard VPN and never expose the port publicly.

## 1. Prepare the server

1. Install Docker Engine with the compose plugin (`docker compose version` should work).
2. Give the server a fixed IP address: add a DHCP reservation for its MAC address on the router (preferred over a static IP set on the server itself, so the router stays the single source of addresses).
3. Clone the repository and create the config:

   ```bash
   git clone https://github.com/Karabaev/karajob.git /srv/karajob
   cd /srv/karajob
   cp .env.example .env              # adjust KARAJOB_PORT / KARAJOB_DATA_DIR if needed
   mkdir -p data && sudo chown 1654:1654 data   # the container runs as user app (UID 1654)
   ```

## 2. Start and check

```bash
docker compose up -d --build
docker compose logs -f karajob    # expect "Applying N migration(s)" on the first start, then "Now listening on"
```

Check from the phone:

1. Connect to the home WireGuard VPN (or the home Wi-Fi).
2. Open `http://<server-ip>:<KARAJOB_PORT>/`.
3. Add a vacancy with **+ Vacancy** (Quick Add) and make sure it shows up in Inbox.
4. Move it to the board, apply and close it once to check the whole flow on a small screen.

## 3. Update and roll back

```bash
cd /srv/karajob
git pull
docker tag karajob karajob:prev   # keep the running image for a rollback
docker compose up -d --build
```

Migrations run on start. Before applying any, the app copies the database to `data/backups/<timestamp>-before-<migration>.db`. If a migration fails, the container does not start and the log names that backup.

Rollback (no down migrations are supported):

```bash
docker compose down
cp data/backups/<timestamp>-before-<migration>.db data/karajob.db
rm -f data/karajob.db-journal data/karajob.db-wal data/karajob.db-shm
sudo chown 1654:1654 data/karajob.db
git checkout <previous commit>
docker tag karajob:prev karajob
docker compose up -d              # without --build: runs the previous image
```

## 4. What is in `data/`

| Path | Content | Back up off the server |
|---|---|---|
| `karajob.db` (+ `-journal` / `-wal` / `-shm`) | Live SQLite database | **No**: copying a live file can give a broken copy. Back up the daily snapshots instead. |
| `backups/daily/<yyyyMMdd-HHmmss>.db` | Daily consistent snapshot (SQLite backup API), the newest `Backup:KeepDaily` (default 14) are kept | Yes |
| `backups/<timestamp>-before-<migration>.db` | Copy taken before each migration | Yes |
| `files/` | Uploaded CV files | Yes |
| `keys/` | ASP.NET Data Protection keys (antiforgery, cookies) | Yes |

The daily snapshot is taken by the app itself: right after start if the newest snapshot is 24 hours old or older, then checked every hour. A snapshot is written to a temporary file first and renamed when complete, so a backup tool never sees a half-written one. Failures are logged at `Error` level and retried on the next hourly check.

## 5. Off-server copy with restic

Back up the whole data folder except the live database. Example for a repository on another device over SFTP:

```bash
export RESTIC_REPOSITORY=sftp:backup@nas.lan:/backups/karajob
export RESTIC_PASSWORD_FILE=/root/.karajob-restic-password

restic init    # once

restic backup /srv/karajob/data \
  --exclude '/srv/karajob/data/karajob.db' \
  --exclude '/srv/karajob/data/karajob.db-*'
restic forget --keep-daily 14 --keep-weekly 8 --keep-monthly 12 --prune
```

Run it daily from a root cron job or a systemd timer. Any time of day works: the snapshot folder only ever holds complete files. For example:

```cron
30 4 * * * root /usr/local/bin/karajob-restic.sh >> /var/log/karajob-restic.log 2>&1
```

Check it regularly: `restic snapshots` and, once in a while, `restic check`.

## 6. Restore

1. Stop the app: `docker compose down`.
2. If the server or its disk is lost, prepare a new server as in section 1 and restore the folder:

   ```bash
   restic restore latest --target / --include /srv/karajob/data
   ```

3. Put the chosen snapshot in place of the live database:

   ```bash
   cp data/backups/daily/<yyyyMMdd-HHmmss>.db data/karajob.db
   rm -f data/karajob.db-journal data/karajob.db-wal data/karajob.db-shm
   sudo chown -R 1654:1654 data
   ```

4. Start: `docker compose up -d --build`. If the snapshot is from an older schema, the pending migrations are applied on start, with a backup taken first.
5. Open the app and check that the latest vacancies and applications are there.

Try a restore into a scratch folder once after setting up restic, so the procedure is known to work before it is needed.
