<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Referenzimplementierung: Softwareschmiede PR #432

Relevante Muster aus der Blaupause:

- `OsInterfaceAttribute` liefert xUnit-v3-Trait `Category=OsInterface` über `ITraitAttribute`.
- `OsInterfaceFactAttribute` / `OsInterfaceTheoryAttribute` koppeln Fakt/Theory direkt an die Kategorie.
- `Flaky` wird als eigener Trait verwendet; die Konvention verlangt `Flaky` nur zusammen mit `OsInterface`.
- `TempDirectoryCleanup` löscht nach Prozess-Kill mit Retry/Backoff und protokolliert Restfehler, ohne sie über den eigentlichen Assert zu werfen.
- `Retry-FlakyTests.ps1` ermittelt fehlgeschlagene `Flaky`-Tests aus TRX und startet einen einzelnen, gefilterten Re-Run.
- `Analyze-FlakyTestResults.ps1` wertet TRX-Dateien aus und berichtet Flake-Ereignisse.
- CI trennt blockierende Suite und best-effort-OS-Lane mit eigenen Artefakten.

Anpassung an Reporter:

- MTP-Filteroptionen müssen nach `--` übergeben werden (`--filter-trait`, `--filter-not-trait`).
- `Reporter.Tests` besitzt keine bestehenden ConPTY-/Prozess-Tests; eine 1:1-Übernahme der Referenztestliste ist nicht sinnvoll.
- `Reporter.E2ETests` bleibt lokal/on-demand und erhält keine reguläre CI-Lane, wird aber als OS-sensitiv klassifiziert.
