<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: CI-Lanes, Retry-/Statistik-Integration und Dokumentation

## Ausgangslage

- `pr-staging-ci.yml` und `staging-ci.yml` enthalten jeweils `detect-backmerge`, `static-checks` und `build-and-test`.
- `build-and-test` baut `Reporter.sln` für Windows und führt `dotnet test` auf `src/Reporter.Tests` mit Coverlet und TRX aus.
- `staging-ci.yml` hängt `version` an `static-checks` und `build-and-test`; ein zusätzlicher Best-Effort-Job darf dort nicht in `needs` aufgenommen werden.
- `Reporter.E2ETests` bleibt lokal/on-demand; die Issue-Anforderung bestätigt, dass Lane-Trennung auf `Reporter.Tests` beschränkt bleibt.

## Relevante Detaildokumente

- [CI-Ausgangslage](inventory/ci.md)
- [Skript- und Filterbefunde](inventory/scripts.md)

## Baseline

- `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj --no-build`: 805 bestanden, 0 fehlgeschlagen, 0 übersprungen.
- Nachweis: [baseline-reporter-tests.txt](inventory/test-results/baseline-reporter-tests.txt)
