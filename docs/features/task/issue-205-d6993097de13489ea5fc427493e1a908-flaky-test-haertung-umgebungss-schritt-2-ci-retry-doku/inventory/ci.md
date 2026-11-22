<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# CI-Ausgangslage

## `pr-staging-ci.yml`

- `detect-backmerge` auf Ubuntu.
- `static-checks` auf Windows, blockierend.
- `build-and-test` auf Windows, blockierend:
  - Restore, Build
  - `dotnet test` mit Coverlet und `test-results.trx`
  - Coverage-Threshold 70 %
  - Artefakte `coverage-report-pr` und `test-results-pr`

## `staging-ci.yml`

- Gleiche Gate-Jobs wie `pr-staging-ci.yml`.
- `version` benötigt `detect-backmerge`, `static-checks`, `build-and-test`.
- Paket-/Release-Jobs hängen an `version`; die neue OS-Lane darf dort nicht einfließen.

## Zu schützende Semantik

- `build-and-test` bleibt die einzige testbezogene Gate-Lane.
- `os-interface tests` darf sichtbar scheitern, ohne PR oder Staging-Pipeline zu blockieren (`continue-on-error`).
- Artefakte und Jobnamen müssen eindeutig sein: reguläre TRX vs. `*-os-interface*.trx`.
