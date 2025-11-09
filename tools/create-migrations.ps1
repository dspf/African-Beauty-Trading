# PowerShell helper to create EF Core migrations for Core project
param(
    [string]$MigrationName = "InitialPostgres"
)

# Ensure dotnet-ef is present
Write-Host "Updating dotnet-ef tool..."
dotnet tool update --global dotnet-ef

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to update dotnet-ef. Ensure dotnet SDK is installed and try again."; exit 1
}

# Ensure connection string is set
if (-not $env:CONNECTION_STRING) {
    Write-Host "Please set CONNECTION_STRING environment variable first. Example:"
    Write-Host "    $env:CONNECTION_STRING = 'postgres://user:pass@host:port/db?sslmode=require'"
    exit 1
}

Write-Host "Creating migration '$MigrationName'..."
cd ..\
# Run the migrations command
dotnet ef migrations add $MigrationName -p src/CoreApp/AfricanBeautyTrading.Core.csproj -s src/CoreApp/AfricanBeautyTrading.Core.csproj -o src/CoreApp/Migrations

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet ef failed. Check errors above."; exit 1
}

Write-Host "Migration created. Inspect files under src/CoreApp/Migrations and commit them."