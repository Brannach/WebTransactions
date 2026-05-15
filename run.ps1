# WebTransactions - Application Launcher
# Runs the pre-built self-contained executable if available.
# Falls back to dotnet run, installing .NET 9 SDK if necessary.

$requiredMajorVersion = 9
$executablePath = "$PSScriptRoot\WebTransactions.Api.exe"
$releaseUrl = "https://github.com/Brannach/WebTransactions/releases/latest"

function Test-DotnetVersion {
    try {
        [string]$output = dotnet --version 2>$null
        if ($output -match "^(\d+)\.") {
            return [int]$Matches[1]
        }
    } catch {
        return -1
    }
    return -1
}

function Install-DotnetWithWinget {
    Write-Host "Attempting to install .NET 9 SDK via winget..." -ForegroundColor Yellow
    winget install Microsoft.DotNet.SDK.9 --accept-source-agreements --accept-package-agreements
}

function Install-DotnetWithScript {
    Write-Host "Attempting to install .NET 9 SDK via Microsoft install script..." -ForegroundColor Yellow
    [string]$installScriptUrl = "https://dot.net/v1/dotnet-install.ps1"
    [string]$installScriptPath = "$env:TEMP\dotnet-install.ps1"

    Invoke-WebRequest -Uri $installScriptUrl -OutFile $installScriptPath -UseBasicParsing
    & $installScriptPath -Channel 9.0 -InstallDir "$env:LOCALAPPDATA\Microsoft\dotnet"

    [string]$dotnetPath = "$env:LOCALAPPDATA\Microsoft\dotnet"
    if (-not ($env:PATH -like "*$dotnetPath*")) {
        $env:PATH = "$dotnetPath;$env:PATH"
    }
}

# --- Main ---

Write-Host "WebTransactions - Starting up..." -ForegroundColor Cyan

# If the self-contained executable exists, run it directly — no .NET required
if (Test-Path $executablePath) {
    Write-Host "Pre-built executable found. Starting without .NET installation..." -ForegroundColor Green
    Write-Host ""
    Write-Host "Starting WebTransactions..." -ForegroundColor Cyan
    Write-Host "The application will be available at: http://localhost:5107" -ForegroundColor Green
    Write-Host "Press Ctrl+C to stop the application." -ForegroundColor Gray
    Write-Host ""
    & $executablePath
    exit
}

# No executable found — inform the user and fall back to dotnet run
Write-Host "Pre-built executable not found." -ForegroundColor Yellow
Write-Host "You can download it from: $releaseUrl" -ForegroundColor Yellow
Write-Host "Falling back to dotnet run..." -ForegroundColor Yellow
Write-Host ""

[int]$installedVersion = Test-DotnetVersion

if ($installedVersion -eq $requiredMajorVersion) {
    Write-Host ".NET $installedVersion SDK found." -ForegroundColor Green
} else {
    if ($installedVersion -gt 0) {
        Write-Host ".NET $installedVersion SDK found, but version 9 is required." -ForegroundColor Yellow
    } else {
        Write-Host ".NET SDK not found." -ForegroundColor Yellow
    }

    Write-Host "Installing .NET 9 SDK..." -ForegroundColor Yellow

    [bool]$wingetAvailable = $null -ne (Get-Command winget -ErrorAction SilentlyContinue)

    if ($wingetAvailable) {
        Install-DotnetWithWinget
        [bool]$installed = $LASTEXITCODE -eq 0
    } else {
        Install-DotnetWithScript
        [bool]$installed = $LASTEXITCODE -eq 0
    }

    if (-not $installed) {
        Write-Host "Automatic installation failed." -ForegroundColor Red
        Write-Host "Please install .NET 9 SDK manually from: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }

    [int]$installedVersion = Test-DotnetVersion
    if ($installedVersion -ne $requiredMajorVersion) {
        Write-Host "Installation completed but .NET 9 could not be verified. Please restart this script." -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }

    Write-Host ".NET 9 SDK installed successfully." -ForegroundColor Green
}

Write-Host ""
Write-Host "Starting WebTransactions..." -ForegroundColor Cyan
Write-Host "The application will be available at: http://localhost:5107" -ForegroundColor Green
Write-Host "Press Ctrl+C to stop the application." -ForegroundColor Gray
Write-Host ""

dotnet run --project src/WebTransactions.Api
