param(
    [Parameter(Position = 0)]
    [ValidateSet('up', 'down')]
    [string]$action,

    [Parameter(Position = 1)]
    [ValidateSet('all', 'infra', 'app')]
    [string]$scope = 'all',

    [switch]$noBuild
)

# Requires PowerShell 7+
$ErrorActionPreference = 'Stop'

function Write-Usage {
    Write-Host "Usage: ./manage.ps1 <up|down> [all|infra|app] [-noBuild]" -ForegroundColor Yellow
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  ./manage.ps1 up all" 
    Write-Host "  ./manage.ps1 up infra" 
    Write-Host "  ./manage.ps1 up app" 
    Write-Host "  ./manage.ps1 down app" 
    Write-Host "  ./manage.ps1 down all" 
    Write-Host "Options:" -ForegroundColor Yellow
    Write-Host "  -noBuild           Skip building images (useful when pulling from GHCR)"
    Write-Host "Env:" -ForegroundColor Yellow
    Write-Host "  Set DOCKER_REGISTRY=ghcr.io/<owner>/ in .env to pull images from GHCR"
}

if (-not $action) { Write-Usage; exit 1 }

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $scriptDir '..')
Set-Location $root

$composeInfra = "infra/docker-compose.yml"
$composeServices = "services/docker-compose.yml"
$composeServicesNetworkOverlay = "services/networks.overlay.yml"
$composeWatchtower = "services/docker-compose.watchtower.yml"
$envFile = ".env"
$networkName = "certval-net"

function Compose {
    param([string[]]$files, [string[]]$composeArgs)
    $fileArgs = @()
    foreach ($f in $files) { $fileArgs += @('-f', $f) }
    $cmdArgs = @()
    if (Test-Path $envFile) { $cmdArgs += @('--env-file', $envFile) }
    $cmdArgs += $fileArgs + $composeArgs
    Write-Host "Running: docker compose $($cmdArgs -join ' ')" -ForegroundColor Cyan
    & docker compose @cmdArgs
}

function EnsureNetwork {
    param([string]$name)
    try {
        docker network inspect $name | Out-Null
    }
    catch {
        Write-Host "Creating network '$name'" -ForegroundColor Yellow
        docker network create $name | Out-Null
    }
}

switch ($action) {
    'up' {
        switch ($scope) {
            'infra' { Compose @($composeInfra) @('up', '-d') }
            'app' { 
                EnsureNetwork -name $networkName
                $buildArgs = @()
                if (-not $noBuild) { $buildArgs = @('--build') }
                $files = @($composeServicesNetworkOverlay, $composeServices)
                if (Test-Path $composeWatchtower) { $files += $composeWatchtower }
                Compose $files (@('up', '-d') + $buildArgs)
            }
            'all' {
                Compose @($composeInfra) @('up', '-d')
                $buildArgs = @()
                if (-not $noBuild) { $buildArgs = @('--build') }
                $files = @($composeServicesNetworkOverlay, $composeServices)
                if (Test-Path $composeWatchtower) { $files += $composeWatchtower }
                Compose $files (@('up', '-d') + $buildArgs)
            }
        }
    }
    'down' {
        switch ($scope) {
            'infra' { Compose @($composeInfra) @('down') }
            'app' { Compose @($composeServicesNetworkOverlay, $composeServices) @('down') }
            'all' {
                Compose @($composeServicesNetworkOverlay, $composeServices) @('down')
                Compose @($composeInfra) @('down')
            }
        }
    }
}
