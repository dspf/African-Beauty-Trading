CoreApp migration project

This folder contains a standalone ASP.NET Core 7 project that links source files (models, views) from the existing .NET Framework project.

Build
- From repository root run: `dotnet restore src/CoreApp` then `dotnet build src/CoreApp`

Run
- From repository root run: `dotnet run --project src/CoreApp`

EF Core migrations
- To create an initial migration (after confirming models):
  - `dotnet ef migrations add InitialCreate --project src/CoreApp --startup-project src/CoreApp`
- To apply migrations:
  - `dotnet ef database update --project src/CoreApp --startup-project src/CoreApp`

Railway instructions
- Use the Linux Docker image published to GHCR (`ghcr.io/<OWNER>/<REPO>:linux-latest`) and configure Railway to pull that image.
- Ensure your app binds to port 80 or uses the `PORT` env var provided by Railway. In ASP.NET Core, set `ASPNETCORE_URLS` or configure Kestrel to listen on the environment port.

SignalR
- The Core project registers a SignalR hub at `/chathub`. Update client scripts in views to call `/chathub` instead of the older `/signalr` endpoints.