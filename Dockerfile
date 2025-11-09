# Use Windows-based images because project targets .NET Framework 4.8
# This Dockerfile expects the repository root to contain the solution and project files.
# NOTE: Building Windows container images requires a Windows Docker host. GitHub Actions "windows-latest" runner may build this image.
# Railway may not support Windows container images. See README.md for details.

# Stage 1 - build using the .NET Framework SDK image
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2019 AS build
SHELL ["cmd", "/S", "/C"]
WORKDIR C:\src

# Copy repository files
COPY . .

# Restore NuGet packages
RUN nuget restore "African Beauty Trading.sln"

# Build solution in Release configuration
RUN msbuild "African Beauty Trading.sln" /p:Configuration=Release /p:Platform="Any CPU"

# Publish web project output to a folder
# This uses OutDir to place compiled output under C:\publish\
RUN msbuild "African Beauty Trading.csproj" /p:Configuration=Release /p:OutDir=C:\publish\

# Stage 2 - runtime image using ASP.NET 4.8 image
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2019 AS final
WORKDIR C:\inetpub\wwwroot

# Copy published site from build stage
COPY --from=build C:\publish\ .

# Expose default HTTP port
EXPOSE 80

# IIS runs by default in the base image; no CMD required
