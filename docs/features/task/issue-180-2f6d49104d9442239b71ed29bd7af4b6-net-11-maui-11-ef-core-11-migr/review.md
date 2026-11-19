<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Abgleich

| Planelement (Akzeptanzkriterium) | Umsetzung | Status |
|---|---|---|
| AK 1: `docs/help/entwicklung/dotnet-11-migration.md` enthält Entscheidung inkl. geprüfter Alternative | Z. 19–59 (Entscheidung + Alternativ-Tabelle + Fazit mit Re-Evaluations-Trigger) | OK |
| AK 1: Voraussetzungen/Gates | Gates G1–G8, Z. 96–110 | OK |
| AK 1: Phasen 0–5 mit konkreten Tasks | Z. 114–188 (Phase 0 Maintenance, 1 TFM-Flip, 2 Paketwelle, 3 EF-Verifikation, 4 CI/CD, 5 Verifikation/Rollout) | OK |
| AK 1: Risiken & Rollback | Z. 192–202 (7 Risiken inkl. Rollback-Pfad über `maintenance/net10`) | OK |
| AK 1: Offene Punkte | Z. 206–228 (10 Punkte mit Gate-/Task-Verweisen) | OK |
| AK 2: TFM-Umstellung inkl. bedingter TFM-Listen | Tasks 1.1–1.6; alle 6 bedingten TFM-Listen in `Reporter.csproj:8-13` verifiziert | OK |
| AK 2: Paketwelle | Phase 2 (Tasks 2.1–2.6: MAUI, EF/SQLite, Microsoft.*-Pakete, Dependabot, Tools, Test-Stack) | OK |
| AK 2: MAUI-Workloads 11er-Generation inkl. CI | Task 2.1 (`$(MauiVersion)` über Workload), Tasks 4.2/4.3 (iOS-Pin, `workload restore`) | OK |
| AK 2: CPU-Mindestbasis | Verifizierte Fakten (x86-64-v2/armv8.0-a+LSE), Gate G1, Risiko-Zeile, Task 5.9 (Release-Notes) | OK |
| AK 2: `UseInterpreter`-Reverifikation | Tasks 1.2 (Condition-Umzug), 3.4 (inhaltliche Verifikation), 5.4 (echtes Gerät) | OK |
| AK 2: CI-Matrizen/Release-Pipeline | Phase 4 inkl. `release.yml`, `security-scan.yml`, Composite Actions | OK |
| AK 2: EF-Core-11-Breaking-Changes/Migrations-Verhalten | Verifizierte Fakten + Gate G6 + Phase 3 (Tasks 3.1–3.6) | OK |
| AK 2: Rolle von `maintenance/net10` | Phase 0 (Tasks 0.1–0.4) + Risiken/Rollback; Remote-Branch verifiziert auf `5492d71` = v0.7.0 | OK |
| AK 3: Verlinkung in `docs/help/entwicklung/index.md` | Z. 10: direkter Link mit Kurzbeschreibung | OK |
| AK 3: Verlinkung in `docs/help/index.md` | Z. 27: Entwicklung-Eintrag nennt Entscheidung + Plan zur .NET-11-Migration („bzw." erfüllt — Bereich verlinkt und Inhalt benannt) | OK |

## Fachliche Qualität (Stichproben gegen Repo-Stand)

Stichprobenartig verifizierte Datei:Zeile-Referenzen — alle korrekt:

- `Reporter.csproj`: Z. 8–13 (6 TFM-Listen), Z. 30 (`MauiXamlInflator=SourceGen`), Z. 45–49 (Plattform-Mindestversionen), Z. 52–56 (`UseInterpreter` `'net10.0-ios'`), Z. 76–77 (Maui.Controls `$(MauiVersion)`, Logging.Debug 10.0.0)
- `Reporter.Core.csproj:6/15`, `Reporter.Data.csproj:6/18-22`, `Reporter.Tests.csproj:6/17/18`, `Reporter.E2ETests.csproj:6/17/24`
- Workflows: `staging-ci.yml` (Trigger Z. 6–8, dotnet-version Z. 70/109, workload restore Z. 73/112, Coverage-Ausschluss Z. 125, ReportGenerator Z. 129), `pr-staging-ci.yml` (Z. 6–8, 73/112, 76/115, 128, 132), `release.yml:75`, `security-scan.yml:26/29`
- Actions: `build-and-package` (Z. 19, 22, 48 `--framework`, 49 `PublishReadyToRun=false`), `package-android` (Z. 19, 22, 50), `package-ios` (Z. 44–47 `26.5.10318`, Z. 59 `10.0.1xx`, Z. 68 global.json `10.0.100`, Z. 87 Pack-Glob `net10.0_*`, Z. 143–161 XamlC-Override, Z. 156 `--framework`)
- Skripte/Code: `Run-E2ETests.ps1:13/24`, `iOS-Deployment.ps1:94`, `ReporterAppFixture.cs:143`, `Package.appxmanifest:22-23`, `DatabaseMigrationService.cs:34-44` (Reihenfolge-Invariante bestätigt: `MigrateLegacyContentAsync` Z. 40 vor `MigrateAsync` Z. 43), `ItemContentMigrationService.cs:42-50/97` (PRAGMA)
- Fakten: 15 + 2 = 17 Migrationen gezählt; `Tags` ist `string?` (`Item.cs:59`); keine `OwnsOne`/`OwnsMany`/`AsSplitQuery`/`EFOptimizeContext` im Repo; `global.json` enthält nur `test.runner`; `dependabot.yml` = 3 Ökosysteme mit `target-branch: staging`; `wiederkehrende-reviews.md:38` dokumentiert die `$(MauiVersion)`-Einschränkung; README/ci-instructions-Referenzen stimmen
- Keine Artefakt-Verweise auf `docs/features/` (requirement/inventory/plan) im Dauer-Dokument — sauber für den Lösch-Schritt
- Remote `maintenance/net10` = `5492d71` = v0.7.0 — wie dokumentiert

## Offene Aufgaben

Keine.

(Hinweis: Issue-Kommentar in #180, PR gegen `staging` mit `Closes #180` sowie Commit/Push der drei Dateien sind dem Planungs-PR-Ablauf zugeordnet und laufen außerhalb dieses Reviews — sie waren zum Prüfzeitpunkt bewusst noch nicht ausgeführt. Git-Status: `dotnet-11-migration.md` untracked, beide `index.md` modified.)
