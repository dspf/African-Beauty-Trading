Deploying African Beauty Trading (Core) to Railway

This document explains how to push the current project to GitHub and deploy the Core (Linux) app to Railway using the PostgreSQL setup included in this repository.

Summary
- The repo contains a .NET Core scaffold in `src/CoreApp` configured to use PostgreSQL via env var `CONNECTION_STRING`.
- Local testing: `docker-compose.yml` runs the Core app + Postgres.
- Production deployment: push to GitHub and deploy to Railway (use GHCR or let Railway build from repo).

1) Commit & push your changes to GitHub
Run from the repository root locally (PowerShell or Bash):

# review changes
git status

git add .
git commit -m "Port Core app to Postgres, add migrations helper and Railway deployment docs"
# replace 'master' with your main branch name if needed
git push origin master

If you do not have a remote repo yet:
- Create a new repository on GitHub and follow the instructions to add `origin` and push.

2) Generate and commit EF Core migrations (required)
Run locally (PowerShell):

# ensure dotnet-ef is installed and up to date
dotnet tool update --global dotnet-ef

# set your Railway Postgres connection string in environment (example uses YOUR_URI)
$env:CONNECTION_STRING = 'postgres://<user>:<pass>@<host>:<port>/<db>?sslmode=require'

# create migration files
tools\create-migrations.ps1 -MigrationName InitialPostgres

# commit migrations and push
git add src/CoreApp/Migrations
git commit -m "Add InitialPostgres EF Core migrations"
git push origin master

3) Prepare Railway environment
Option A — Deploy from Docker image (recommended for control)
- Build & push image to GitHub Container Registry (GHCR) via CI (existing workflow `.github/workflows/ci-ghcr-linux.yml`).
- In Railway create a new service and use the GHCR image tag `ghcr.io/<OWNER>/<REPO>:linux-latest`.

Option B — Deploy from GitHub repo (Railway builds the image)
- On Railway, create a new project ? Deploy from GitHub ? choose this repository and branch.

Railway environment variables (set in Railway project settings)
- CONNECTION_STRING = your Postgres URI (e.g. the Railway-provided Postgres add-on URI)
- ASPNETCORE_ENVIRONMENT = Production
- APPLY_MIGRATIONS = true  # optional — use with caution; better to run migrations from CI
- PAYFAST__MERCHANT_ID, PAYFAST__MERCHANT_KEY, PAYFAST__PASSPHRASE (set real payment secrets)
- SMTP__HOST, SMTP__PORT, SMTP__USER, SMTP__PASS (if sending email)

Note: never commit credentials into source. Use Railway secrets UI.

4) Migrations strategy in production
- Preferred: run a CI job that applies migrations to the production DB before deployment (safer).
- Alternative: set `APPLY_MIGRATIONS=true` in Railway to let the container call `db.Database.Migrate()` on startup. Use only if you accept the risk and have backups.

5) Files & permissions
- Ensure `wwwroot/uploads` exists and is writable by the container. The dockerfile maps `wwwroot` into the image; on Railway the container FS is ephemeral — consider using object storage for persistent uploads.

6) Test after deploy
- Check Railway logs for successful DB connection and (if enabled) migrations application.
- Test endpoints: `/`, `/Products`, `/Account/Register`, `/Cart/Checkout`.
- For payments, test PayFast sandbox and confirm IPN reaches `/Cart/PaymentNotify` (public URL required).

7) Rollback & backups
- Always snapshot or backup your DB before applying production migrations.
- Use CI to create migration backups or use database-level backup features of your DB provider.

Troubleshooting
- "Cannot connect to database" — verify `CONNECTION_STRING` and that Postgres allows connections from Railway; use SSL settings as required by your provider.
- Migration errors — inspect migration files and run locally first.
- 500 on startup — check Railway logs, enable verbose logging by setting `ASPNETCORE_ENVIRONMENT=Development` temporarily and review logs.

If you want, I can:
- Add a GitHub Actions job to run `dotnet ef database update` as part of CI (requires a DB connection and secrets). I can craft a safe job that runs only on a protected branch.
- Add instructions to migrate existing SQL Server data to Postgres.

