# Deploying ARISe

Deployment guide for hosting ARISe on a Linux VM, following the conventions in the
Sparrow [web-server setup guide](https://github.com/Sparrow-RMS/docs/blob/main/setup/web-server/README.md)
and the *Hosting the API* section of the
[backend development guide](https://github.com/Sparrow-RMS/docs/blob/main/backend/README.md).

ARISe is a **.NET 10 API + a static React bundle**, so nginx serves the frontend from disk
and reverse-proxies `/api/` to the backend on loopback. There is no external database —
the API stores everything in **SQLite**, which changes a few things versus the standard
SQL Server services (see [Deviations](#deviations-from-the-standard-guide)).

---

## Topology

| Piece | Location on the VM |
|---|---|
| Backend (published FDD) | `/var/www/projects/arise/backend/` |
| Frontend (Vite `dist/`) | `/var/www/projects/arise/frontend/` |
| SQLite database | `/var/sparrow/files/arise/planreview.db` |
| Service environment file | `/etc/sparrow/sites/arise.env` |
| systemd unit | `/etc/systemd/system/arise-api.service` |
| nginx site | `/etc/nginx/sites-available/arise.conf` |
| API listen address | `http://127.0.0.1:5099` (loopback only) |
| Logs | `journalctl -t arise-api` |

```
                  ┌──────────────────────── VM ────────────────────────┐
   client ──443──▶│ nginx ──/──────▶ /var/www/projects/arise/frontend  │
                  │       └─/api/──▶ 127.0.0.1:5099 (arise-api.service)│
                  │                        └──▶ /var/sparrow/files/arise/planreview.db
                  └────────────────────────────────────────────────────┘
```

## Prerequisites

- Ubuntu 22.04 VM prepared per the web-server guide: UFW enabled, timezone set,
  nginx installed and hardened, `/etc/nginx/snippets/{global,frontend}.conf` in place,
  `/etc/systemd/system/backend.slice` created, and `/var/www/projects` owned by `ubuntu`.
- The VM must be **arm64** (e.g. AWS Graviton, Ampere, Azure Dpsv5) — the backend is
  published for `linux-arm64` only (step 4), so its native libraries will not load on
  an x64 host.
- **.NET 10 ASP.NET Core runtime** at `/opt/dotnet` (see step 1 — the guide's snippet
  installs 9.0; ARISe needs 10.0).
- A DNS record pointing at the load balancer / VM, and a TLS certificate. This guide
  assumes TLS terminates *above* nginx, matching the shared `nginx.conf`
  (`proxy_set_header X-Forwarded-Proto https`).
- Node 22 and the .NET 10 SDK on the **build machine** (not needed on the VM).
- `sqlite3` on the VM for consistent backups (`sudo apt install sqlite3`). The API bundles
  its own native SQLite, so this is only for the backup and inspection commands below.

---

## 1. Install the .NET 10 runtime (one-time, on the VM)

The web-server guide installs channel 9.0. ARISe targets `net10.0`, so install the
10.0 ASP.NET Core runtime alongside it:

```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x ./dotnet-install.sh
sudo ./dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir /opt/dotnet
```

Verify:

```bash
/opt/dotnet/dotnet --list-runtimes | grep 'Microsoft.AspNetCore.App 10'
```

`/opt/dotnet` stays owned by root; the service only ever reads from it.

## 2. Create the deployment directories (one-time)

```bash
sudo install -d -m 775 -o ubuntu -g ubuntu /var/www/projects/arise/backend
sudo install -d -m 775 -o ubuntu -g ubuntu /var/www/projects/arise/frontend
sudo install -d -m 755 /etc/sparrow/sites
sudo install -d -m 0750 -o www-data -g www-data /var/sparrow/files/arise
```

The database directory **must** exist before the service first starts. The unit
sandboxes itself with `ReadWritePaths=/var/sparrow/files/arise/`, and systemd assembles
that mount namespace *before* running any `ExecStartPre` — if the path is missing, the
service fails with `Failed to set up mount namespacing: /var/sparrow/files/arise: No
such file or directory`. The unit's `ExecStartPre` only re-asserts owner and mode on an
existing directory; it cannot create a missing one.

## 3. Prepare release configuration

Per step 1 of the guide's hosting section, replace the parameters in the config files
*before* publishing. For ARISe the one that matters is the CORS origin list in
`backend/PlanReview.Api/appsettings.json`:

```jsonc
"Cors": {
  "AllowedOrigins": [ "https://arise.sparrowios.com" ]
}
```

> **Why this must be edited rather than overridden.** .NET merges configuration arrays
> **by index**. `appsettings.json` ships two dev origins, so setting only
> `Cors__AllowedOrigins__0` in the env file would override index 0 and silently leave
> `http://localhost:3000` at index 1 still allowed in production. Trim the list in the
> file, or override *every* index.

## 4. Build and publish (on the build machine)

```bash
# Backend — framework-dependent deployment, linux-arm64 only
dotnet publish backend/PlanReview.Api/PlanReview.Api.csproj -c Release \
  -r linux-arm64 --self-contained false -o ./publish

# Frontend — static bundle
cd frontend && npm ci && npm run build && cd ..
```

This produces `./publish/PlanReview.Api.dll` (the assembly the unit launches) and
`frontend/dist/`.

`-r linux-arm64` makes this a RID-specific publish: instead of shipping
`runtimes/<rid>/` folders for every platform, the output contains only the linux-arm64
build of `libe_sqlite3.so` (the native SQLite library), placed at the publish root.
`--self-contained false` must accompany `-r` — specifying a runtime alone flips the
publish to self-contained, bundling an entire .NET runtime the VM doesn't need and
bypassing the one installed in step 1. Cross-publishing from macOS or Windows is still
fine; the flags fix the target platform, not the build machine.

> `publish/appsettings.json` still contains the **development JWT key**. It is overridden
> at runtime by the env file, but treat the published bundle as non-public regardless,
> and rotate that key (step 6) since it is already in git history.

## 5. Ship the release to the VM

```bash
scp -i <path-to-private-key> -r ./publish/* ubuntu@<host>:/var/www/projects/arise/backend/
scp -i <path-to-private-key> -r ./frontend/dist/* ubuntu@<host>:/var/www/projects/arise/frontend/
```

## 6. Create the service environment file

```bash
sudo nano /etc/sparrow/sites/arise.env
```

```env
ASPNETCORE_URLS=http://127.0.0.1:5099

# SQLite lives outside the app directory, which the unit mounts read-only.
ConnectionStrings__Default=Data Source=/var/sparrow/files/arise/planreview.db

# Generate with: openssl rand -base64 48
Jwt__Key=<paste-a-fresh-random-key>
Jwt__Issuer=ARISe
Jwt__Audience=ARISeClient
Jwt__ExpiryHours=12

# Optional — leave Enabled=false to record notifications in-app only.
Email__Enabled=true
Email__From=arise@sparrowrms.in
Email__FromName=ARISe
Email__SmtpHost=<smtp-host>
Email__SmtpPort=587
Email__SmtpUser=<smtp-user>
Email__SmtpPassword=<smtp-password>
Email__UseSsl=true
```

Lock it down — it holds the signing key and SMTP credentials:

```bash
sudo chown root:www-data /etc/sparrow/sites/arise.env
sudo chmod 640 /etc/sparrow/sites/arise.env
```

> Config keys map to env vars by replacing `:` with `__`. See
> [Naming of environment variables](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/#naming-of-environment-variables).

## 7. Install and start the service

```bash
sudo cp deploy/arise-api.service /etc/systemd/system/arise-api.service
sudo systemctl daemon-reload
sudo systemctl enable arise-api.service
sudo systemctl start arise-api.service
sudo systemctl status arise-api.service
```

On first start the API applies its EF Core migrations, creates
`/var/sparrow/files/arise/planreview.db` and seeds the master data (functions, roles,
skills, company traits) plus a default administrator.

Check it came up:

```bash
journalctl -t arise-api -n 50 --no-pager
curl -s -o /dev/null -w '%{http_code}\n' http://127.0.0.1:5099/api/functions   # expect 200
```

## 8. Secure the default administrator — before exposing the site

> [!IMPORTANT]
> The seeder creates `admin@company.com` with the password `Admin@123`, hardcoded in
> `DbSeeder.cs` and published in the project README. Do this **now**, while the API is
> still bound to loopback and no nginx site is enabled — that is what keeps the exposure
> window closed. Do not enable the site in step 9 until this returns success.

```bash
# On the VM.
TOKEN=$(curl -s -X POST http://127.0.0.1:5099/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@company.com","password":"Admin@123"}' \
  | python3 -c 'import json,sys; print(json.load(sys.stdin)["token"])')

ADMIN_ID=$(curl -s http://127.0.0.1:5099/api/auth/me \
  -H "Authorization: Bearer $TOKEN" \
  | python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])')

curl -s -X POST "http://127.0.0.1:5099/api/users/$ADMIN_ID/reset-password" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"newPassword":"<a-strong-password>"}'
```

Confirm the old password no longer works (expect `401`):

```bash
curl -s -o /dev/null -w '%{http_code}\n' -X POST http://127.0.0.1:5099/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@company.com","password":"Admin@123"}'
```

Then sign in and change the admin's email to a real address from the Users page.

## 9. Create the nginx site

```bash
sudo nano /etc/nginx/sites-available/arise.conf
```

```nginx
server {

    server_name arise.sparrowios.com;

    # API — the trailing slash is deliberately omitted, see the note below.
    location /api/ {
        proxy_pass http://127.0.0.1:5099;
    }

    # Frontend SPA
    location / {
        root /var/www/projects/arise/frontend;
        include /etc/nginx/snippets/frontend.conf;
    }

    listen 80;
    listen [::]:80;
    include /etc/nginx/snippets/global.conf;
}
```

> [!WARNING]
> **Two adaptations of the standard site config, both required for ARISe:**
>
> 1. **No trailing slash on `proxy_pass`.** The guide's example uses
>    `proxy_pass http://localhost:<port>/;`, which rewrites `/api/auth/login` to
>    `/auth/login`. ARISe's controllers are routed at `[Route("api/...")]`, so the prefix
>    must be preserved — every request would 404 with the trailing slash.
> 2. **`style-src` in `frontend.conf` must be relaxed.** The shared snippet sets
>    `Content-Security-Policy: ... style-src 'none'`, which blocks ARISe's stylesheet and
>    renders the app unusable. Use the permissive alternative already commented out in
>    that file, or scope an override into this site's `location /`:
>    ```nginx
>    add_header Content-Security-Policy "default-src 'self'; frame-ancestors 'none'; style-src 'self'; img-src 'self' data:; form-action 'self'" always;
>    ```
>    (The snippet's `image-src` directive is a typo — the real directive is `img-src`, so
>    that line is ignored by browsers and images are unaffected either way.)

Enable, test and reload:

```bash
sudo ln -s /etc/nginx/sites-available/arise.conf /etc/nginx/sites-enabled/
sudo nginx -t
sudo service nginx reload
```

> Always use absolute paths for both ends of the symlink, per the guide's note about
> symlink loops.

## 10. Verify

```bash
curl -s -o /dev/null -w 'frontend %{http_code}\n' https://arise.sparrowios.com/
curl -s -o /dev/null -w 'api      %{http_code}\n' https://arise.sparrowios.com/api/functions
curl -s -o /dev/null -w 'authz    %{http_code}\n' https://arise.sparrowios.com/api/cycles   # expect 401
```

Then in a browser: sign in as the admin, confirm the dashboard renders and the sidebar
navigates. A blank page with console CSP errors means the `style-src` fix in step 9 was
not applied.

Note that **Swagger is not served in production** — `Program.cs` maps it only when the
environment is Development.

---

## Redeploying a new version

```bash
# On the build machine
dotnet publish backend/PlanReview.Api/PlanReview.Api.csproj -c Release \
  -r linux-arm64 --self-contained false -o ./publish
cd frontend && npm ci && npm run build && cd ..

# On the VM
sudo systemctl stop arise-api.service
```

```bash
# From the build machine
scp -i <key> -r ./publish/* ubuntu@<host>:/var/www/projects/arise/backend/
scp -i <key> -r ./frontend/dist/* ubuntu@<host>:/var/www/projects/arise/frontend/
```

```bash
# On the VM
sudo systemctl start arise-api.service
journalctl -t arise-api -n 50 --no-pager
```

Stop the service before copying: the app directory is mounted read-only for the running
process, and EF Core migrations run on start, so the new binaries must be fully in place
first. **Take a database backup before any deploy that carries a migration** — EF Core
migrations are not automatically reversible.

## Backup and restore

The whole application state is one SQLite file. Do **not** copy `planreview.db` while the
service is running — WAL mode means a plain copy can be torn. Take a consistent snapshot:

```bash
sudo -u www-data /usr/bin/sqlite3 /var/sparrow/files/arise/planreview.db \
  ".backup '/var/sparrow/files/arise/backup-$(date +%F).db'"
```

Point `restic`/`resticprofile` (already configured per the web-server guide) at the
snapshot rather than the live file.

To restore:

```bash
sudo systemctl stop arise-api.service
sudo -u www-data cp /path/to/backup.db /var/sparrow/files/arise/planreview.db
sudo systemctl start arise-api.service
```

## Operations cheat sheet

```bash
sudo systemctl status arise-api.service        # state
sudo systemctl restart arise-api.service       # restart
journalctl -t arise-api -f                     # follow logs
journalctl -t arise-api -p err -n 100          # errors only
systemd-analyze security arise-api.service     # hardening report
sudo systemctl show arise-api.service -p MemoryCurrent   # memory in use
```

---

## Deviations from the standard guide

Recorded here so the next person does not "fix" them back:

| # | Standard | ARISe | Why |
|---|---|---|---|
| 1 | Shared `backend@.service` template unit | Dedicated `arise-api.service` | The template's `ExecStartPre` builds a Python venv and installs `requirements.txt`; ARISe is pure .NET. |
| 2 | App directory read-only, data in SQL Server | One `ReadWritePaths` entry at `/var/sparrow/files/arise/` | SQLite must create the `.db` plus `-wal`/`-shm` sidecars. Keeping them outside `/var/www` preserves the read-only app directory. |
| 3 | `proxy_pass http://localhost:<port>/;` | No trailing slash | ARISe's routes already include the `/api` prefix. |
| 4 | `location / { return 501; }` | Serves the SPA from disk | ARISe ships its own frontend on the same origin. |
| 5 | `MemoryMax=2048M` | `MemoryMax=512M` | Small single-tenant app; raise if the cohort grows. |
| 6 | Secrets in `/etc/sparrow/shared/common.env` | Own `arise.env`; shared file optional (`-` prefix) | ARISe's config keys are unprefixed and share nothing with the `IosApi_` services. |

## Known gaps to close

Carried over from the code review — none blocks a deploy, but they shape how you run it:

- **The seeded admin password is hardcoded**, not configurable. Step 8 closes the window
  by sequencing, but making the seed credentials read from configuration (as
  `SeedingSettings:RootUserCredentials` does elsewhere) would remove the sharp edge.
- **No rate limiting on `/api/auth/login`.** BCrypt verification is deliberately
  expensive, so this is a resource-exhaustion vector as much as a brute-force one. The
  shared `nginx.conf` already defines a `ratelimit` zone — applying
  `limit_req zone=ratelimit burst=20 nodelay;` inside `location /api/` is the cheap fix.
- **No `UseHttpsRedirection`/HSTS in the app.** Harmless here because TLS terminates
  above nginx and the shared config already sends HSTS, but it means the API must never
  be exposed directly.
- **SQLite is single-writer.** Fine for one team; if ARISe grows to hundreds of concurrent
  developers, move to SQL Server — only the `UseSqlite(...)` call in `Program.cs` and the
  connection string change, as the EF model is provider-agnostic.
