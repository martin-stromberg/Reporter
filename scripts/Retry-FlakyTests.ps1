# Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

#Requires -Version 7.0
<#
.SYNOPSIS
    Wertet das Primaer-TRX eines Testlaufs aus und wiederholt fehlgeschlagene, als
    "Flaky" markierte Tests genau einmal gefiltert.

.DESCRIPTION
    Reporter.Tests laeuft ueber die Microsoft Testing Platform (xUnit v3). Ablauf:
      1. Primaer-TRX parsen: Namen aller "Failed"-Ergebnisse einsammeln (Theory-
         Datenzeilen werden auf den Methoden-FQN normalisiert).
      2. Flaky-Menge des Projekts via "dotnet test --no-build --list-tests --
         --filter-trait Category=Flaky" ermitteln.
      3. Schnittmenge leer  -> "keine Flaky-Fehlschlaege", Exit 0 (kein Retry).
         Sonst genau ein Re-Run ueber die MTP-VSTest-Query
         --filter "(Category=Flaky)&(FullyQualifiedName~...)" (--filter und
         --filter-trait duerfen nicht in denselben -- -Argumenten kombiniert
         werden; Category=Flaky wird nur in Kombination mit Category=OsInterface
         vergeben und reicht daher als zusaetzliche Einschraenkung).
      4. Retry-TRX auswerten: recovered (fehlgeschlagen -> bestanden) vs. persistent.
         Persistente Fehlschlaege -> Exit 1 (ehrliches Signal; in der CI bleibt der
         Job durch continue-on-error trotzdem nicht-blockierend).

    Fehlendes oder unauswertbares Primaer-TRX gilt als "keine Flaky-Fehlschlaege"
    (Exit 0), damit ein als if: failure() laufender CI-Schritt bei Build-Fehlschlag
    oder fehlendem TRX nichts verschleiert.

.PARAMETER TestProject
    Pfad zum Testprojekt (.csproj), dessen Primaerlauf ausgewertet wird.

.PARAMETER PrimaryTrx
    Pfad zur TRX-Datei des Primaerlaufs.

.PARAMETER Configuration
    Build-Konfiguration (Standard: Release — entspricht der CI-Lane).

.PARAMETER RetryTrxFileName
    Dateiname des Retry-TRX im selben TestResults-Verzeichnis.
    Standard: <primaername>-flaky-retry.trx

.EXAMPLE
    ./scripts/Retry-FlakyTests.ps1 -TestProject src/Reporter.Tests/Reporter.Tests.csproj -PrimaryTrx TestResults/test-results-os-interface.trx -Configuration Release
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$TestProject,
    [Parameter(Mandatory = $true)]
    [string]$PrimaryTrx,
    [string]$Configuration = "Release",
    [string]$RetryTrxFileName = ""
)

$ErrorActionPreference = "Stop"
$inGitHubActions = $env:GITHUB_ACTIONS -eq "true"

. (Join-Path $PSScriptRoot "lib/Trx.ps1")
. (Join-Path $PSScriptRoot "lib/TestDiscovery.ps1")

function Write-Annotation {
    param([string]$Kind, [string]$Message)
    if ($inGitHubActions) { Write-Host "::$Kind`::$Message" }
    else { Write-Host "[$Kind] $Message" }
}

function ConvertTo-MethodName {
    # Theory-Datenzeilen ("Ns.Class.Method(arg: 1)") auf den Methoden-FQN
    # normalisieren: alles vor der ersten Argumentklammer "(" ist der Methodenname.
    # So bleiben auch generische/nested Typnamen ("Ns.GenericTests<T>.Method",
    # "Ns.GenericTests`1[T]", "Ns.Outer+Inner") komplett erhalten, die ein reiner
    # Identifier-Regex am "<"/"[" abschneiden wuerde — konsistent zur Normalisierung
    # in Get-FlakyTestNames (lib/TestDiscovery.ps1).
    param([string]$TestName)
    $parenIndex = $TestName.IndexOf('(')
    if ($parenIndex -gt 0) { return $TestName.Substring(0, $parenIndex).TrimEnd() }
    return $TestName
}

# 1. Primaer-TRX auswerten
$primaryResults = Get-TrxTestResults -TrxPath $PrimaryTrx
if ($null -eq $primaryResults) {
    Write-Host "Keine Flaky-Fehlschlaege: Primaer-TRX '$PrimaryTrx' fehlt oder ist unauswertbar."
    exit 0
}

$failedMethods = @($primaryResults.GetEnumerator() |
    Where-Object { $_.Value -eq "Failed" } |
    ForEach-Object { ConvertTo-MethodName $_.Key } |
    Sort-Object -Unique)
if ($failedMethods.Count -eq 0) {
    Write-Host "Keine Flaky-Fehlschlaege: Der Primaerlauf hat keine fehlgeschlagenen Tests."
    exit 0
}

# 2. Flaky-Menge des Projekts ermitteln (Helper aus lib/TestDiscovery.ps1)
$flakyMethods = Get-FlakyTestNames -ProjectPath $TestProject -Configuration $Configuration

$retryMethods = @($failedMethods | Where-Object { $flakyMethods.Contains($_) })
if ($retryMethods.Count -eq 0) {
    Write-Host "Keine Flaky-Fehlschlaege: $($failedMethods.Count) Fehlschlag/Fehlschlaege im Primaerlauf, davon keiner Flaky-markiert."
    exit 0
}

Write-Host "==> $($retryMethods.Count) Flaky-Fehlschlag/Fehlschlaege werden genau einmal wiederholt:" -ForegroundColor Cyan
$retryMethods | ForEach-Object { Write-Host "    $_" }

# 3. Genau ein gefilterter Re-Run (MTP: --report-trx statt --logger trx)
$resultsDirectory = Split-Path -Parent $PrimaryTrx
if ([string]::IsNullOrEmpty($resultsDirectory)) {
    # Reiner Dateiname ohne Verzeichnisanteil (lokaler Aufruf aus dem TRX-Verzeichnis):
    # "" wuerde --results-directory "" und ein unbrauchbares Join-Path erzeugen.
    $resultsDirectory = "."
}
if ([string]::IsNullOrWhiteSpace($RetryTrxFileName)) {
    $RetryTrxFileName = [IO.Path]::GetFileNameWithoutExtension($PrimaryTrx) + "-flaky-retry.trx"
}
# Eine etwaig vorhandene Ziel-TRX stammt aus einer frueheren Ausfuehrung und
# muss vor dem Re-Run entfernt werden: Bleibt sie liegen und der Re-Run
# schreibt kein neues TRX (Crash vor Ergebnis-Ausgabe, Discovery-Ausfall),
# wuerden die veralteten Outcomes dem aktuellen Lauf zugeschrieben. Kann sie
# nicht entfernt werden, bricht das Skript ehrlich ab statt auf dem alten
# Stand weiterzulaufen — nur eine in diesem Aufruf neu erzeugte Datei darf
# ausgewertet werden.
$retryTrxPath = Join-Path $resultsDirectory $RetryTrxFileName
if (Test-Path -LiteralPath $retryTrxPath) {
    try {
        Remove-Item -LiteralPath $retryTrxPath -Force -ErrorAction Stop
        Write-Host "Bestehendes Retry-TRX '$retryTrxPath' aus frueherem Lauf entfernt."
    }
    catch {
        Write-Annotation -Kind "error" -Message "Flaky-Retry abgebrochen: Vorhandenes Retry-TRX '$retryTrxPath' konnte nicht entfernt werden ($($_.Exception.Message)); Ergebnis waere nicht eindeutig dem aktuellen Re-Run zuordenbar."
        exit 1
    }
}
$nameFilter = ($retryMethods | ForEach-Object { "FullyQualifiedName~$_" }) -join "|"
$retryOutput = & dotnet test --project $TestProject -c $Configuration --no-build `
    -- --filter "(Category=Flaky)&($nameFilter)" `
    --report-trx --report-trx-filename $RetryTrxFileName `
    --results-directory $resultsDirectory 2>&1
$retryOutput | ForEach-Object { Write-Host $_ }

# 4. Retry-TRX auswerten: Nur "Passed" gilt als recovered. Ein echtes "Failed"
# im Retry ist persistent (bestaetigter Doppelfehlschlag). Alles andere ist
# ehrlich "nicht recovered": andere Outcomes (z. B. NotExecuted durch einen
# Skip-Mechanismus) ebenso wie ein fehlendes Ergebnis (Retry-TRX fehlt/
# unauswertbar, gefilterter Re-Run ohne Ergebnis, Discovery-Ausfall) — ein
# nicht nachweisbar gelaufener Retry ist kein bestaetigter Doppelfehlschlag.
$retryResults = Get-TrxTestResults -TrxPath $retryTrxPath
$persistent = [System.Collections.Generic.List[string]]::new()
$indeterminate = [System.Collections.Generic.List[string]]::new()
foreach ($method in $retryMethods) {
    $retryOutcomes = @()
    if ($null -ne $retryResults) {
        $retryOutcomes = @($retryResults.GetEnumerator() |
            Where-Object { (ConvertTo-MethodName $_.Key) -eq $method } |
            ForEach-Object { $_.Value })
    }
    if ($retryOutcomes.Count -gt 0 -and @($retryOutcomes | Where-Object { $_ -ne "Passed" }).Count -eq 0) {
        Write-Annotation -Kind "notice" -Message "Flaky-Retry recovered: $method (fehlgeschlagen im Primaerlauf, bestanden im Retry)"
    }
    elseif (@($retryOutcomes | Where-Object { $_ -eq "Failed" }).Count -gt 0) {
        $persistent.Add($method)
    }
    elseif ($retryOutcomes.Count -eq 0) {
        $indeterminate.Add("$method (kein bestimmbares Retry-Ergebnis)")
    }
    else {
        $indeterminate.Add("$method (Retry-Outcome: $(($retryOutcomes | Sort-Object -Unique) -join ', '))")
    }
}

foreach ($method in $persistent) {
    Write-Annotation -Kind "warning" -Message "Flaky-Retry persistent: $method (fehlgeschlagen im Primaerlauf UND im Retry)"
}
foreach ($method in $indeterminate) {
    Write-Annotation -Kind "warning" -Message "Flaky-Retry nicht recovered: $method"
}

if ($persistent.Count -gt 0 -or $indeterminate.Count -gt 0) {
    Write-Host "==> $($persistent.Count) Flaky-Test(s) auch im Retry fehlgeschlagen (persistent), $($indeterminate.Count) ohne Passed-Ergebnis (nicht recovered)." -ForegroundColor Red
    exit 1
}
Write-Host "==> Alle Flaky-Fehlschlaege im Retry bestanden (recovered)." -ForegroundColor Green
exit 0
