Docker & GHCR publishing

Important notes
- This repository originally targets .NET Framework 4.8 (Windows-only). To deploy Linux containers (e.g., on Railway) you must migrate to .NET 6/7+ (or later) and use the provided .NET Core project and Linux Dockerfile.

Migration scaffold provided
- A minimal ASP.NET Core project is added: `AfricanBeautyTrading.Core.csproj`, `Program.cs`, and `appsettings.json`.
- This is a scaffold to help port controllers/views and configuration. You must migrate your existing controllers, models, views and third-party dependencies to .NET 6+.

Linux Dockerfile and CI
- `Dockerfile.linux` - multi-stage Dockerfile for .NET 7 (adjust base images if needed)
- `.github/workflows/ci-ghcr-linux.yml` - GitHub Actions workflow that builds and pushes a Linux image to GHCR as `ghcr.io/<OWNER>/<REPO>:linux-latest`.

Database (Postgres)
- The Core project is now configured to use PostgreSQL via the `CONNECTION_STRING` environment variable. For local testing a `docker-compose.yml` service runs a Postgres (TimescaleDB) instance.

Migrations (required step for production)
- Do NOT rely on `EnsureCreated()` in production. Generate EF Core migrations locally and commit them before deploying.

Local migration steps (run on your machine):
1. Install/verify dotnet-ef tool:
   docker run --rm -it -v $(pwd):/app -w /app mcr.microsoft.com/dotnet/sdk:7.0-alpine sh -c "dotnet tool update --global dotnet-ef && dotnet --info"
2. Set your Postgres connection string in the environment (use Railway URI or local one):
   PowerShell:
     $env:CONNECTION_STRING = 'postgres://...'
   Bash:
     export CONNECTION_STRING='postgres://...'
3. From repository root run:
   docker run --rm -it -v $(pwd):/app -w /app mcr.microsoft.com/dotnet/sdk:7.0-alpine sh -c "dotnet ef migrations add InitialPostgres -p src/CoreApp/AfricanBeautyTrading.Core.csproj -s src/CoreApp/AfricanBeautyTrading.Core.csproj -o src/CoreApp/Migrations"
4. Inspect and commit the migration files `src/CoreApp/Migrations/*`.
5. Apply migrations locally (optional):
   docker run --rm -it -v $(pwd):/app -w /app mcr.microsoft.com/dotnet/sdk:7.0-alpine sh -c "dotnet ef database update --project src/CoreApp/AfricanBeautyTrading.Core.csproj --startup-project src/CoreApp/AfricanBeautyTrading.Core.csproj --connection "$CONNECTION_STRING""

Automating migrations at startup (optional & risky)
- The Core app supports an `APPLY_MIGRATIONS=true` environment variable. If set, the application will call `db.Database.Migrate()` on startup and apply any pending migrations. This can be used in CI or controlled deployments but requires caution and backups for production.

Docker-compose for local testing
- A sample `docker-compose.yml` is provided to run the app together with Postgres locally.
- Start the stack:
  docker-compose up --build
- The compose file exposes the app at http://localhost:8080 and Postgres at port 5432. Update `CONNECTION_STRING` in the compose file to a secure value for your environment.

Railway deployment checklist
1. Generate and commit EF Core migrations.
2. Push the repository to GitHub and enable the GHCR workflow (or build/push image locally).
3. In Railway, create a new service and set the image reference to the built GHCR image or let Railway build from the repository.
4. Add environment variables in Railway: `CONNECTION_STRING`, `ASPNETCORE_ENVIRONMENT=Production`, payment and SMTP secrets, and optionally `APPLY_MIGRATIONS=true` if you want migrations applied at startup.
5. Monitor logs for database connectivity and migration application (if enabled).

Notes:
- Ensure Postgres is reachable from Railway environment and that credentials are secure.
- For production, prefer running migrations via CI/CD rather than automatic in-container migration.
- Keep backups of your database before applying schema migrations.

Next steps to complete migration
1. Port controllers and views from the old .NET Framework app into the new ASP.NET Core project.
2. Update authentication/identity to ASP.NET Core Identity if used.
3. Update any NuGet packages to .NET 6/7-compatible versions.
4. Test locally and run the CI workflow to build and publish the Linux image.

Production secrets guidance
- Never commit production secrets into the repo. Use Railway environment variables or GitHub Secrets.
- Recommended environment variables and secret names:
  - `CONNECTION_STRING` – SQL Server connection string.
  - `SMTP__HOST`, `SMTP__PORT`, `SMTP__USER`, `SMTP__PASS` – SMTP settings used by the app.
  - `STRIPE__APIKEY` or `PAYMENT__KEY` – payment provider secrets.
  - `APP__LOG_LEVEL` – override logging level (optional).
- For ASP.NET Core, hierarchical env var names map to configuration; use `__` to represent `:` in keys (e.g. `Logging__LogLevel__Default`).
