<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Skript- und Filterbefunde

## Vorhandene Skripte

- `scripts/Run-E2ETests.ps1` startet die lokale Desktop-Suite und räumt Prozesse im `finally` auf.
- Es gibt noch keine `scripts/lib`-Hilfen und keine Flake-/Retry-Auswertung.

## Verifizierte MTP-Syntax

- `dotnet test ... -- --filter-not-trait "Category=OsInterface"`: 801 Tests in `Reporter.Tests`.
- `dotnet test ... -- --filter-trait "Category=OsInterface"`: 4 Tests in `Reporter.Tests`.
- `--filter` und `--filter-trait` dürfen nicht kombiniert werden.
- Für den Flaky-Re-Run wird daher die VSTest-Query `-- --filter "(Category=Flaky)&(FullyQualifiedName~...)"` verwendet; lokal verifiziert mit `Category=OsInterface` plus zwei FQNs.
- `--report-trx --report-trx-filename <name>` schreibt nach `TestResults/<name>` relativ zum Arbeitsverzeichnis.

## Blaupause

Softwareschmiede PR #432 liefert:

- `scripts/lib/Trx.ps1` für TRX-Parsing
- `scripts/lib/TestDiscovery.ps1` für Flaky-Discovery
- `scripts/Retry-FlakyTests.ps1`
- `scripts/Analyze-FlakyTestResults.ps1`
- separate best-effort OS-CI-Jobs

Die Skripte werden auf Reporter-MTP-Syntax und auf `Reporter.Tests` angepasst.
