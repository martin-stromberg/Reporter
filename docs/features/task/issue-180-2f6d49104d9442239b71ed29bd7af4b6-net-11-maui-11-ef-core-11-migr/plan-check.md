<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

Geprüft: `requirement.md`, `inventory.md` + `inventory/{tfms,packages,ci,efcore,workloads,misc}.md`
gegen `plan.md`. Stichproben-Verifikation der zitierten Datei:Zeile-Referenzen und
Versionsnummern gegen den Repo-Stand (siehe Hinweise).

## Ergebnis

**Status:** Plan vollständig

Die Anforderung ist eine Planungs-/Entscheidungsaufgabe. `plan.md` liefert die geforderte
begründete Entscheidung (Migration auf .NET 11 STS, `maintenance/net10` als Fallback),
eine bewertete Alternative mit dokumentiertem Re-Evaluations-Trigger und einen
Migrationsplan (Gates G1–G8, Phasen 0–5, Risiken & Rollback, Offene Punkte), der alle
Pflichtpunkte aus `requirement.md` abdeckt. Die Akzeptanzkriterien für den Planungs-PR
sind konkret und prüfbar formuliert.

## Abgleich Akzeptanzkriterien

Pflichtpunkte aus `requirement.md` (Z. 26–43) ↔ Umsetzung in `plan.md`:

| Kriterium (requirement.md) | Umsetzung im Plan | Status |
|---|---|---|
| TFM-Umstellung aller Projekte inkl. plattformbedingter TFM-Listen in `Reporter.csproj` | Phase 1, Tasks 1.1–1.6: alle 6 bedingten Listen (`Reporter.csproj:8-13`), `UseInterpreter`-Condition (1.2), Core/Data/Tests/E2ETests-TFMs (1.3/1.4), Mindestversionen (1.5), harte TFM-Strings in Skripten/Actions/Fixture inkl. Pack-Glob `net10.0_*` (1.6), global.json-Entscheidung (1.7), `MauiXamlInflator` (1.8) | Abgedeckt |
| Paketwelle (MAUI, EF Core/Sqlite, Microsoft.Extensions.*) | Phase 2, Tasks 2.1–2.6 mit IST-Versionen und Datei:Zeile: `$(MauiVersion)` workload-gesteuert (2.1), EF Core/Sqlite 10.0.12 → 11.x (2.2), Logging.Debug/Syndication/TimeProvider.Testing (2.3), Dependabot-Koordination (2.4), Tools `dotnet-ef`/ReportGenerator (2.5), Test-Stack Welle 4 (2.6) | Abgedeckt |
| MAUI-Workloads 11er-Generation inkl. CI | Task 4.3 (`workload restore` zieht 11er-Workloads mit dem 11er-SDK), Task 4.2 + Gate G4 (iOS-Pack-Version/SDK-Band/Manifest-Feed zum Migrationszeitpunkt), Gate G8 (lokale Maschinen) | Abgedeckt |
| CPU-Mindestbasis (x86-64-v2 / SSE4.2+POPCNT; Arm64-Win: LSE), Build-Agents + Zielgeräte | Verifizierte externe Fakten (plan.md:59–64), Gate G1 (Zielgeräte, inkl. R2R-/JIT-Unterscheidung und win-arm64-Einordnung), Gate G2 (Build-Agents), Risiko-Tabelle + Task 5.9 (Release-Notes/Troubleshooting) | Abgedeckt |
| `UseInterpreter`-Hinweis für EF Core auf iOS erneut verifizieren | Task 1.2 (Condition-Flip), Task 3.4 (inhaltliche Reverifikation auf echtem Gerät), Task 5.4 (TestFlight/Gerät) | Abgedeckt |
| CI-Matrizen und Release-Pipeline (setup-dotnet, Workload-Installation) | Phase 4: Task 4.1 alle `10.0.x`-Stellen inkl. `release.yml` + `security-scan.yml`, 4.2 `package-ios` komplett, 4.3 Workload-Restore-Aufrufe, 4.4 setup-dotnet-Auflösung (G3), 4.5 ASP.NET-Core-11-Shared-Framework für E2E, 4.6 `ci-instructions.md`, 4.7 MTP-Stack | Abgedeckt |
| EF Core 11 Breaking Changes / Migrations-Verhalten für Bestandsinstallationen | Fakten-Block (plan.md:65–73) + Phase 3: 3.1 Checkliste aller 5 Breaking Changes mit Repo-Vorabgleich, 3.2 `MigrateAsync`-Reihenfolge-Invariante + PRAGMA-Pfad, 3.3 `EnsureCreated`- vs. `Migrate`-Test-Pfade, 3.5 Bestands-DB-Upgrade v0.7.0, Risiko „Migrations-Disziplin/forward-only" | Abgedeckt |
| Rolle von `maintenance/net10` während der Umstellung | Phase 0: Rolle (nur kritische Fixes, 0.1), CI-Gate-Lücke für Hotfixes + Trigger-Entscheidung (0.2), manueller Release-Weg (0.3), Backport-Regel (0.4); Scope-Tabelle „bereits erledigt"; Rollback-Zeile in Risiken | Abgedeckt |
| Entscheidung als Kommentar im Issue #180 | Scope „In diesem Issue enthalten" + Akzeptanzkriterium 4 | Abgedeckt |
| Migrationsplan als Dauer-Dokument in `docs/help/entwicklung/` | Scope + Akzeptanzkriterien 1–3 (Inhalt, Verlinkung `docs/help/index.md`/`docs/help/entwicklung/index.md` — beide existieren) | Abgedeckt |
| Abschluss als PR gegen `staging` mit Closing-Tag auf #180 | Scope + Akzeptanzkriterium 6 (`Closes #180`) | Abgedeckt |
| Scope-Ausschluss: keine Code-/TFM-Änderung in diesem PR | „Art der Aufgabe" (plan.md:8–11) + Scope-Ausschluss-Tabelle (Durchführung = Folge-Issue) | Abgedeckt |

## Gefundene Lücken

Keine.

## Hinweise

Nicht blockierend; können beim Aufsetzen des Dauer-Dokuments oder im Migrations-PR
berücksichtigt werden:

1. **Startup-/Retry-Einstiegspunkt der DB-Migration:** `plan.md` Task 3.2 benennt
   `DatabaseMigrationService`/`ItemContentMigrationService`, nicht aber die Aufrufer
   (`MauiProgram.MigrateDatabases` synchron in `CreateMauiApp`, `App.xaml.cs:43,108–117`
   `MigrateDatabasesAsync` inkl. `OnStart`-Retry — inventory/efcore.md:20–23). Im
   Migrations-PR den idempotenten Wiederholpfad mitverifizieren.
2. **`maccatalyst` aus der requirement-Workload-Aufzählung** wird im Plan nicht
   namentlich behandelt — faktisch korrekt, da kein maccatalyst-TFM aktiv ist
   (inventory/tfms.md:37). Ein Halbsatz im Dauer-Dokument würde das explizit festhalten.
3. **Stichproben-Verifikation bestanden:** Alle geprüften Datei:Zeile-Referenzen und
   Versionsnummern stimmen mit dem Repo-Stand überein — u. a. `Reporter.csproj:8-13/30/45-56/76-77`,
   alle vier weiteren .csproj (`:6`, Paketzeilen), `staging-ci.yml:6-8/68-70/73/107-112/125/129`,
   `pr-staging-ci.yml:6-8/71-76/110-115/128/132`, `release.yml:73-78`,
   `security-scan.yml:24-29`, alle drei Composite Actions inkl. `package-ios`
   (26.5.10318 an Z.47, `10.0.1xx` Z.59, global.json-Pin Z.68, Pack-Glob Z.87,
   `net10.0-ios` Z.156), `Run-E2ETests.ps1:13/24`, `iOS-Deployment.ps1:94`,
   `ReporterAppFixture.cs:143`, `Package.appxmanifest:22-23`, global.json ohne SDK-Pin,
   `dependabot.yml` (3 Ökosysteme, `target-branch: staging`), 15+2=17 Migrationen,
   `origin/maintenance/net10` = `5492d71` (= `origin/main`), `README.md:4/47/57/63/66`.
   Repo-weite Suche über `.github/` bestätigt: alle `10.0.x`/`10.0.1xx`/`net10.0`-Fundstellen
   sind in den Plan-Tasks 4.1/4.2/1.6 erfasst — keine unerfasste Stelle.
4. **Vorabgleich der EF-Breaking-Changes verifiziert:** kein `OwnsOne`/`OwnsMany`/
   `AsSplitQuery`/`EFOptimizeContext` im Repo; EF-Entity `Item.Tags` ist `string?`
   (`src/Reporter.Data/Entities/Item.cs:59` — das Core-Modell nutzt zwar
   `IReadOnlyList<string>`, wird aber nicht von EF gemappt); `TestContentDbContextFactory`
   nutzt `Migrate()` (grün unter EF 11, da Content-Migrationen existieren) —
   G6/3.1/3.3-Aussagen des Plans zutreffend.
5. `plan.md` Z.202 zitiert die `MauiXamlInflator`-Stelle als `action.yml:143-161`
   (wie inventory/tfms.md); ci.md nennt Z.153–162 für den Publish-Block. Beide Bereiche
   stimmen, kein Widerspruch — nur uneinheitliche Zeilenangabe.
