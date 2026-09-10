# Umsetzungsplan

## Zusammenfassung

Anpassung der CI-Workflows aus `ci-instructions.md` an das Projekt `Reporter` als GitHub Actions-Grundgerüst.

## Geplante Schritte

1. `.github/actions/security-scan/action.yml` als Composite Action anlegen.
2. `.github/workflows/verify-pr-source.yml` anlegen.
3. `.github/workflows/pr-staging-ci.yml` anlegen mit parallelen `static-checks` und `build-and-test` auf `windows-latest`.
4. `.github/workflows/security-scan.yml` als wöchentlicher Scan anlegen.
5. Projektbezüge anpassen: `Reporter.sln`, `src/Reporter.Tests/Reporter.Tests.csproj`, `src/Reporter/Reporter.csproj`.
6. Lokale Verifikation: `dotnet build Reporter.sln` und `dotnet test src/Reporter.Tests/Reporter.Tests.csproj`.
7. Commit der Workflows und Lifecycle-Artefakte.

## Offene Punkte

- Keine.

## Nicht im Scope

- Pre-Release/Promotion/Release-Workflows.
- iOS-/Android-spezifische Runner.
- Branch-Protection und Labels (manuell im Repository einzurichten).
