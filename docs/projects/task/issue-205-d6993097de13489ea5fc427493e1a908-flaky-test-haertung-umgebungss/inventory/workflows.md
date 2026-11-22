<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# CI-Workflows und Gate-Struktur

## `staging-ci.yml`

- Trigger: `push` auf `staging`.
- `detect-backmerge` steuert alle Folgejobs.
- `static-checks` und `build-and-test` laufen parallel auf `windows-latest`.
- `build-and-test` baut die Solution in Release und führt `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build -- --coverlet ... --report-trx` aus.
- `version` hängt an `[detect-backmerge, static-checks, build-and-test]` und ist die Release-Gate-Kette für Packaging/Prerelease.
- Artefakte: `coverage-report-staging`, `test-results-staging`.

## `pr-staging-ci.yml`

- Trigger: Pull Request gegen `staging`.
- `static-checks` und `build-and-test` laufen parallel auf `windows-latest`.
- `build-and-test` ist identisch zur Staging-Variante, Artefakte heißen `coverage-report-pr`, `test-results-pr`.
- Es gibt keinen zentralen Aggregatjob; die blockierenden Jobs wirken direkt als PR-Checks.

## Lane-Trennung

- Der reguläre `build-and-test`-Step muss `--filter-not-trait "Category=OsInterface"` verwenden.
- Ein separater `os-interface-tests`-Job darf nur `--filter-trait "Category=OsInterface"` ausführen, `continue-on-error: true` erhalten und nicht in `needs` der Gate-/Version-Jobs aufgenommen werden.
- TRX-/Report-Artefakte brauchen eigene Namen, damit sie nicht mit der regulären Lane kollidieren.
