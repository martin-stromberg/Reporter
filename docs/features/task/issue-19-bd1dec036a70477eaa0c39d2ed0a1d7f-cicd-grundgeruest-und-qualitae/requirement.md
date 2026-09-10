# Anforderung

## Ziel

Automatisierte Qualitätsprüfungen für Pull Requests in `staging` einrichten, bevor weitere Features entwickelt werden. Die Workflows aus `ci-instructions.md` sollen konkret auf das Projekt `Reporter` angepasst werden.

## Scope

- `.github/workflows/verify-pr-source.yml` für PRs aus `staging` in `main`.
- `.github/workflows/pr-staging-ci.yml` mit parallelen Jobs `static-checks` und `build-and-test`.
- Wiederverwendbare Composite Action `.github/actions/security-scan/action.yml` plus `.github/workflows/security-scan.yml`.
- Mapping `MyApp.sln` -> `Reporter.sln`, `MyApp.Tests` -> `Reporter.Tests`; Web-Projekt entfällt, stattdessen MAUI-App `src/Reporter/Reporter.csproj`.
- Branch-Protection für `staging` mit required Status Checks `static-checks` und `build & test`.
- Labels `automated-promotion` und `automated-backmerge`.
- Coverage-Threshold 70 %.
- Workflows laufen auf `windows-latest`.

## Akzeptanzkriterien

- Jeder PR in `staging` führt Build, Test, Format-Check und Security-Scan aus.
- Branch-Protection auf `staging` verlangt beide Status Checks.
- Security Scan ist als Composite Action umgesetzt.
- Keine `MyApp.*`-Pfade mehr im Workflow; alle Verweise auf `Reporter.*` korrekt.
- Build und Tests laufen auf `windows-latest` erfolgreich durch.
