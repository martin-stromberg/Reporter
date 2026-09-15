# Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

<#
    Run-E2ETests.ps1

    Baut die Reporter-App (Debug, Windows-TFM, win-x64) und fuehrt anschliessend
    die FlaUI-Smoke-Tests in src/Reporter.E2ETests aus.

    Voraussetzungen:
      - Windows mit installiertem .NET MAUI Workload.
      - Eine interaktive Desktop-Session (UIA3 benoetigt ein sichtbares Fenster).
      - Reporter.exe wird vom Testprojekt ueber REPORTER_APP_PATH gefunden bzw.
        ansonsten unter src/Reporter/bin/Debug/net10.0-windows10.0.19041.0/win-x64
        gesucht.

    Beispiel:
        .\scripts\Run-E2ETests.ps1
#>

$ErrorActionPreference = "Stop"

$env:IncludeIosTarget = 'false'

$tfm = "net10.0-windows10.0.19041.0"
$runtime = "win-x64"
$configuration = "Debug"
$repoRoot = Split-Path $PSScriptRoot -Parent
$appOutput = Join-Path $repoRoot "src\Reporter\bin\$configuration\$tfm\$runtime\Reporter.exe"

if (-not (Test-Path "Reporter.sln")) {
    Write-Host "Fehler: Reporter.sln nicht gefunden. Bitte aus dem Repository-Root ausfuehren." -ForegroundColor Red
    exit 1
}

Write-Host "==> Build Reporter ($configuration | $tfm | $runtime)" -ForegroundColor Cyan
dotnet build "src/Reporter/Reporter.csproj" -c $configuration -f $tfm -r $runtime
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build fehlgeschlagen." -ForegroundColor Red
    exit $LASTEXITCODE
}

if (-not (Test-Path $appOutput)) {
    Write-Host "Fehler: $appOutput wurde nicht erzeugt." -ForegroundColor Red
    exit 1
}

$env:REPORTER_APP_PATH = $appOutput
Write-Host "==> REPORTER_APP_PATH = $env:REPORTER_APP_PATH" -ForegroundColor Cyan

Write-Host "==> dotnet test src/Reporter.E2ETests" -ForegroundColor Cyan
dotnet test "src/Reporter.E2ETests/Reporter.E2ETests.csproj" -c $configuration -f $tfm
exit $LASTEXITCODE
