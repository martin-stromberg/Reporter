<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Anforderung – Projekt .NET-11-Migration

Quelle: GitHub Issue #202 (`martin-stromberg/Reporter`), Kommentar-Auftrag
„@softwarewerkstatt Erledige diese Aufgabe". Folge-Issue zu #180; die dort
gefällte Entscheidung **geplante Migration auf .NET 11 (STS)** ist bindend.
Verbindlicher Detailplan: `docs/help/entwicklung/dotnet-11-migration.md`
(Gates G1–G8, Phasen 0–5, Risiken & Rollback).

## Ziel

Reporter (Lösung aus 5 Projekten, .NET MAUI App + Core/Data/Test/E2ETests)
vollständig von .NET 10 auf **.NET 11 / MAUI 11 / EF Core 11** migrieren —
inklusive Paketwelle, EF-Core-11-Verifikation, CI/CD-Umstellung und
Doku-Welle — so dass die Erfolgskriterien des Plans erfüllt sind und das
Ergebnis als PR gegen `staging` vorliegt.

## Fachliche Anforderungen (belegt aus Issue/Plan)

1. **Phase 0 (bereits erledigt, nur Dokupflege):** `maintenance/net10`
   existiert (v0.7.0); CI-Trigger-Erweiterung erst bei erstem Hotfix-Bedarf —
   keine Aktion jetzt.
2. **Phase 1 — SDK-/TFM-Flip:** alle TFMs `net10.0*` → `net11.0*` in allen
   `.csproj` (6 bedingte TFM-Listen in `Reporter.csproj`), `UseInterpreter`-
   Condition, harte `net10`-Strings in Scripts, `ReporterAppFixture.cs`,
   Composite-Actions und Pack-Cleanup-Glob; `global.json`-SDK-Pin-Entscheidung.
3. **Phase 2 — Paketwelle (atomar mit Phase 1):**
   `Microsoft.EntityFrameworkCore.{Design,Sqlite}` 10.0.12 → 11.x,
   `Microsoft.Data.Sqlite` 10.0.12 → 11.x, `Microsoft.Extensions.Logging.Debug`
   10.0.0 → 11.x, `System.ServiceModel.Syndication` 10.0.11 → 11.x,
   `Microsoft.Extensions.TimeProvider.Testing` 10.1.0 → 11.x,
   `Microsoft.Maui.Controls` via 11er-Workload (`$(MauiVersion)` unverändert);
   Dependabot-11er-PRs sind bereits geschlossen (PRs #196, #200 CLOSED).
4. **Phase 3 — EF-Core-11-Verifikation:** Breaking-Changes-Checkliste gegen
   aktuelles Modell, `MigrateAsync`-Pfad inkl. Reihenfolge-Invariante
   (`ItemContentMigrationService` vor `context.Database.MigrateAsync()`) und
   `PRAGMA table_info('items')`-Raw-SQL, Test-Factories
   (`EnsureCreated` vs. `Migrate`), `UseInterpreter`-Reverifikation,
   Bestands-DB-Upgrade aus v0.7.0.
5. **Phase 4 — CI/CD:** `dotnet-version '10.0.x'` → `'11.0.x'` in allen
   Workflows/Actions; `package-ios`-Pin-Trio (Workload-Version, SDK-Band,
   temporäre `global.json`), Pack-Cleanup-Glob `net10.0_*` → `net11.0_*`;
   setup-dotnet-Auflösung (G3); `MauiXamlInflator`-Bewertung (Task 1.8);
   `ci-instructions.md`-Entscheidung (Task 4.6).
6. **Phase 5 — Verifikation & Rollout:** lokale Builds + Tests +
   `Run-StaticChecks.ps1` Exit 0; Doku-Welle mit Volltextsuche
   `net10\.0\|10\.0\.x\|\.NET 10`; Plan-Dokument auf „durchgeführt" heben;
   Release-Notes/Troubleshooting zu x86-64-v2-Mindestbasis; Release-Timing
   beachten (kein Merge direkt vor Release, erste 11er-Auslieferung **ohne**
   neue EF-Schema-Migration).

## Betroffene Bereiche

- `src/**/*.csproj`, `global.json`, ggf. `.config/dotnet-tools.json`
- `scripts/Run-E2ETests.ps1`, `scripts/iOS-Deployment.ps1`,
  `src/Reporter.E2ETests/ReporterAppFixture.cs`
- `.github/workflows/*`, `.github/actions/{build-and-package,package-android,package-ios}/`
- `ci-instructions.md`, `README.md`, `docs/help/**`,
  `docs/help/entwicklung/dotnet-11-migration.md` (Status), ggf. `docs/RELEASE_NOTES.md`

## Akzeptanzkriterien (aus Plan „Erfolgskriterien")

- [ ] Build aller Plattformen (Windows unpackaged, Android-APK, iOS) grün in CI.
- [ ] Alle Unit-Tests + FlaUI-E2E grün; `Run-StaticChecks.ps1` Exit 0.
- [ ] Bestands-DB-Upgrade (`reporter.db`/`reporter-content.db` aus v0.7.0)
      migriert fehlerfrei unter .NET-11-Build.
- [ ] iOS: App läuft auf echtem Gerät ohne `ExecutionEngineException`
      (`UseInterpreter` reverifiziert) — nur CI-/geräteseitig prüfbar,
      dokumentierte Übergabe an den Anwender.
- [ ] Keine neue EF-Schema-Migration im selben Release wie der Runtime-Wechsel.

## Explizite Nicht-Ziele

- Kein Central Package Management (Plan „Offene Punkte" Nr. 10 — Folgethema).
- Keine neue EF-Schema-Migration.
- Keine präventive CI-Trigger-Erweiterung für `maintenance/net10`.
- Kein Release / kein Merge nach `main` (PR-Ziel ist `staging`).

## Offene Punkte zum Migrationszeitpunkt (mit Vorabbefund)

| # | Punkt | Vorabbefund |
|---|-------|-------------|
| G4 | iOS-Workload-Pack-Version 11er | `Microsoft.NET.Sdk.iOS.Manifest-11.0.100` = **27.2.11039** (einzige GA-Version im 11.0.100-Band; `net11.0_26.5` nur Previews). → Pin `27.2.11039`, Band `11.0.1xx` |
| G5 | Plattform-Mindestversionen MAUI 11 | Zum Flip per Build prüfen; bei Anhebung `Package.appxmanifest`/`Info.plist` synchron ziehen |
| G3 | setup-dotnet@v6 löst `11.0.x` auf | setup-dotnet löst zur Laufzeit über den Live-Release-Index auf; SHA-Pin (v6) bleibt; verifiziert sich im PR-Check |
| – | `global.json`-SDK-Pin | Empfehlung des Plans umsetzen: `11.0.100` + `latestPatch` |
| – | `dotnet-ef`-Versionierung | `.config/dotnet-tools.json` mit `dotnet-ef` 11.x einführen (reproduzierbar, dokumentiert die Anforderung) |
| – | `MauiXamlInflator`-Override iOS-CI | Konservativ beibehalten (`XamlC`), iOS-SourceGen-Korrektheit nicht lokal verifizierbar; Kommentar fortschreiben |
| – | `ci-instructions.md` | Mit ziehen (enthält konkrete 10er-Beispiele; Doku-Grep muss sauber sein) |
| – | Real-E2E (FlaUI/iOS-Gerät) | Umgebungsabhängig: FlaUI braucht sichtbares Fenster, iOS-Gerät nicht verfügbar → Verlauf dokumentieren, ggf. Anwender-Entscheidung |

## Abweichung Skill-Ausführung

Codex-/Claude-Unteragenten stehen in dieser Umgebung nicht zur Verfügung;
alle delegationspflichtigen Arbeitsaufträge (Übersetzung, Bestandsaufnahme,
Planung, Implementierung, Reviews, Tests, Dokumentation, Abnahme) werden
vom Hauptagenten selbst ausgeführt. Für die geforderten unabhängigen
Prüfungen (Planprüfung, Schrittabnahmen) werden schreibgeschützte
Explore-Subagenten als frische, unvoreingenommene Prüfer eingesetzt.
