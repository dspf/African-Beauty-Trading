<#
.SYNOPSIS
Safely remove a stale Git index.lock if no git processes are running.

.PARAMETER RepoPath
Path to the repository root (defaults to current directory).

.PARAMETER Force
If present, removes the lock even if git processes are found (use with caution).

.EXAMPLE
.
    .\tools\clear-git-lock.ps1

.
    .\tools\clear-git-lock.ps1 -RepoPath "C:\repos\myrepo" 

.
    .\tools\clear-git-lock.ps1 -Force
#>
param(
    [string]$RepoPath = (Get-Location).Path,
    [switch]$Force
)

try {
    $repoFull = (Resolve-Path -Path $RepoPath).Path
} catch {
    Write-Error "Repository path not found: $RepoPath"
    exit 1
}

$lockPath = Join-Path $repoFull ".git\index.lock"

# Check for running git processes
$gitProcs = Get-Process -Name git -ErrorAction SilentlyContinue
if ($gitProcs) {
    Write-Host "Detected running git processes:" -ForegroundColor Yellow
    $gitProcs | ForEach-Object { Write-Host "  Id=$($_.Id) Name=$($_.ProcessName)" }
    if (-not $Force) {
        Write-Host "Aborting: stop git processes or re-run with -Force to remove the lock." -ForegroundColor Red
        exit 2
    }
    else {
        Write-Host "Force flag provided — proceeding to remove lock despite running git processes." -ForegroundColor Magenta
    }
}

if (Test-Path $lockPath) {
    try {
        Remove-Item -Path $lockPath -Force -ErrorAction Stop
        Write-Host "Removed stale lock: $lockPath" -ForegroundColor Green
        exit 0
    }
    catch {
        Write-Error "Failed to remove lock: $_"
        exit 3
    }
}
else {
    Write-Host "No lock file found at: $lockPath" -ForegroundColor Green
    exit 0
}
