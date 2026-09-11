<#
    Run-StaticChecks.ps1

    Fuehrt die Pruefungen des CI-Workflows "static-checks" lokal aus,
    damit vor einem Push geprueft werden kann, ob Korrekturen noetig sind.

    Voraussetzung: Das .NET MAUI Workload muss lokal bereits installiert sein
    (z. B. via Visual Studio Installer oder dotnet workload install).

    Beispiele:
        .\scripts\Run-StaticChecks.ps1
        .\scripts\Run-StaticChecks.ps1 -Check Format
        .\scripts\Run-StaticChecks.ps1 -Check Security
        .\scripts\Run-StaticChecks.ps1 -Check Build
        .\scripts\Run-StaticChecks.ps1 -Check Restore
#>

param(
    [ValidateSet("All", "Format", "Security", "Build", "Restore")]
    [string]$Check = "All",

    [switch]$SkipRestore
)

$ErrorActionPreference = "Stop"

$solution = "Reporter.sln"
$configuration = "Release"
$runtime = "win-x64"

$env:IncludeIosTarget = 'false'

if (-not (Test-Path $solution)) {
    Write-Host "Fehler: $solution wurde nicht im aktuellen Verzeichnis gefunden. Bitte das Skript aus dem Repository-Root ausfuehren." -ForegroundColor Red
    exit 1
}

function Invoke-PackageRestore {
    Write-Host "Stelle Pakete fuer $runtime wieder her..." -ForegroundColor Cyan
    & dotnet restore $solution -r $runtime
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Fehler: Paket-Restore fehlgeschlagen." -ForegroundColor Red
        exit 1
    }
    Write-Host "Paket-Restore abgeschlossen." -ForegroundColor Green
}

function Invoke-FormatCheck {
    Write-Host "Pruefe Code-Formatierung..." -ForegroundColor Cyan
    & dotnet format $solution --verify-no-changes --no-restore --severity error
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Fehler: Format-Check fehlgeschlagen. Fuehre 'dotnet format $solution' aus, um Probleme zu beheben." -ForegroundColor Red
        exit 1
    }
    Write-Host "Format-Check bestanden." -ForegroundColor Green
}

function Invoke-SecurityScan {
    Write-Host "Pruefe auf verwundbare NuGet-Pakete..." -ForegroundColor Cyan
    & dotnet list $solution package --vulnerable --include-transitive 2>&1 | Tee-Object -Variable output
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Fehler: Sicherheits-Scan konnte nicht ausgefuehrt werden (Exit-Code: $LASTEXITCODE)." -ForegroundColor Red
        exit 1
    }

    $hasVulnerabilities = $output | Where-Object { "$_" -match "has the following vulnerable packages|Severity" }
    if ($hasVulnerabilities) {
        Write-Host "Fehler: Verwundbare Pakete gefunden." -ForegroundColor Red
        exit 1
    }
    Write-Host "Keine verwundbaren Pakete gefunden." -ForegroundColor Green
}

function Invoke-StaticAnalysisBuild {
    Write-Host "Fuehre statische Analyse (Release-Build mit Warnungen als Fehler) durch..." -ForegroundColor Cyan
    & dotnet build $solution --configuration $configuration --no-restore -p:TreatWarningsAsErrors=true
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Fehler: Statische Analyse/Build fehlgeschlagen." -ForegroundColor Red
        exit 1
    }
    Write-Host "Statische Analyse bestanden." -ForegroundColor Green
}

if (-not $SkipRestore) {
    Invoke-PackageRestore
}

switch ($Check) {
    "All" {
        Invoke-FormatCheck
        Invoke-SecurityScan
        Invoke-StaticAnalysisBuild
    }
    "Format" {
        Invoke-FormatCheck
    }
    "Security" {
        Invoke-SecurityScan
    }
    "Build" {
        Invoke-StaticAnalysisBuild
    }
    "Restore" {
        Write-Host "Paket-Restore abgeschlossen." -ForegroundColor Green
    }
}

Write-Host "" -ForegroundColor Cyan
Write-Host "Alle gewaehlten Static-Checks wurden erfolgreich ausgefuehrt." -ForegroundColor Green
exit 0
