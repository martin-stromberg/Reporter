# Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

#Requires -Version 7.0
<#
.SYNOPSIS
    Gemeinsame Test-Discovery-Helfer fuer die Test-Skripte (per Dot-Sourcing eingebunden).

.DESCRIPTION
    Kapselt die Auswertung von "dotnet test --list-tests"-Ausgaben, damit Aufruf und
    Zeilen-Parsing nicht in jedem Skript gepflegt werden. Reporter.Tests laeuft ueber
    die Microsoft Testing Platform (xUnit v3): Trait-Filter werden hinter "--" als
    --filter-trait uebergeben (--filter und --filter-trait duerfen nicht kombiniert
    werden). Verwendung im aufrufenden Skript:

        . (Join-Path $PSScriptRoot "lib/TestDiscovery.ps1")
#>

function Get-FlakyTestNames {
    # Liefert die Methoden-FQNs aller Tests mit Category=Flaky des Projekts als
    # HashSet[string] (leere Menge bei leerem/fehlgeschlagenem List-Lauf — MTP meldet
    # dann zusaetzlich einen Exit-Code != 0, das Parsing bleibt trotzdem korrekt).
    # Der Aufruf nutzt --no-build und setzt daher einen zuvor erfolgten Build voraus.
    # Theory-Datenzeilen ("Ns.Class.Method(arg: 1)") werden auf den Methoden-FQN
    # reduziert, indem jede Zeile an der ersten Argumentklammer "(" abgeschnitten
    # wird; generische/nested Typnamen ("Ns.GenericTests<T>.Method",
    # "Ns.GenericTests`1[T]", "Ns.Outer+Inner") gehoeren zum FQN und bleiben
    # erhalten. Anschliessend werden nur Zeilen uebernommen, die komplett wie ein
    # Methoden-FQN aussehen (mind. ein Punkt), damit Ueberschriften und
    # Build-Ausgaben des List-Laufs nicht in die Menge gelangen.
    # Rueckgabe per ","-Operator: HashSet implementiert IEnumerable und wuerde sonst
    # beim Funktions-Output in die Einzelnamen unrollt werden.
    param([string]$ProjectPath, [string]$Configuration = "Release")
    $listOutput = & dotnet test --project $ProjectPath -c $Configuration --no-build --list-tests -- --filter-trait "Category=Flaky" 2>&1
    $names = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($line in $listOutput) {
        $trimmed = "$line".Trim()
        $parenIndex = $trimmed.IndexOf('(')
        if ($parenIndex -ge 0) { $trimmed = $trimmed.Substring(0, $parenIndex).TrimEnd() }
        if ($trimmed -match '^[A-Za-z0-9_`+<>\[\], ]+(\.[A-Za-z0-9_`+<>\[\], ]+)+$') {
            [void]$names.Add($matches[0])
        }
    }
    return ,$names
}
