<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Projektplan – Migration auf .NET 11 / MAUI 11 / EF Core 11

## Übersicht

**Projekttitel:** Umsetzung .NET-11-Migration (Issue #202)
**Gesamtziel:** Reporter komplett auf .NET 11 / MAUI 11 / EF Core 11 heben —
TFMs, Pakete, EF-Verifikation, CI/CD, Dokumentation — gemäß dem verbindlichen
Migrationsplan `docs/help/entwicklung/dotnet-11-migration.md` (Phasen 1–5;
Phase 0 ist organisatorisch bereits erledigt).
**Betroffene Bereiche:** alle 5 Projekte in `src/`, `global.json`,
`scripts/`, `.github/`, `ci-instructions.md`, `README.md`, `docs/help/**`.

## Grobe Vorgehensentscheidungen

- **Phasen-Bindung statt freier Zerlegung:** Der Migrationsplan ist bereits
  abgenommen (Issue #180/#202) und enthält die verbindliche Task-Liste mit
  Datei:Zeile-Referenzen. Die Entwicklungsschritte bilden seine Phasen direkt
  ab — keine eigenständige Neuplanung des Migrationsweges.
- **Atomarer TFM-/Paket-Flip:** Phase 1+2 wird als ein Schritt/Commit-Satz
  umgesetzt, weil EF 11 nicht auf net10 läuft (Plankonform, Task „atomar").
- **Open-Point-Entscheidungen vorab (dokumentiert in `requirement.md`):**
  `global.json` Pin `11.0.100`+`latestPatch`; `dotnet-ef` via neuer
  `.config/dotnet-tools.json` auf 11.x gepinnt; `package-ios` auf GA-Pin
  `27.2.11039`/Band `11.0.1xx`/`global.json 11.0.100`; `MauiXamlInflator=XamlC`
  iOS-Override bleibt (konservativ, nicht lokal verifizierbar);
  `ci-instructions.md` wird mit gezogen.
- **Getrennte Prüfung EF 11:** Die EF-Core-11-Verifikation (Phase 3) ist ein
  eigener Schritt mit realem Bestands-DB-Upgrade-Test (v0.7.0-DBs aus
  `maintenance/net10`-Worktree erzeugen, unter .NET-11-Build migrieren).
- **CI-Umbau nach Code-Flip:** Workflows/Actions folgen in einem eigenen
  Schritt, damit Build-Fehler klar dem Code- oder dem CI-Schritt zugeordnet
  werden können. Der finale PR-Check verifiziert alle Pfade zusammen.
- **Doku-Welle als letzter Schritt:** inkl. Status-Hebung des
  Migrationsdokuments und Schluss-Grep (Plan 5.6/5.7/5.9).
- **Verifikationsgrenzen dokumentiert statt ignoriert:** iOS-Geräte-Test,
  TestFlight und CI-Gesamtlauf können lokal nicht ausgeführt werden —
  sie werden als explizite Restpunkte im PR/Report benannt (Plan 5.3/5.4);
  FlaUI-E2E wird lokal versucht.

## Entwicklungsschritte

### Schritt 1: SDK-/TFM-Flip + Paketwelle (Migrationsplan Phase 1+2)

Alle Ziel-Frameworks und Paketversionen der Lösung atomar auf .NET 11 heben:
alle 6 bedingten TFM-Listen in `src/Reporter/Reporter.csproj`
(`net10.0-*` → `net11.0-*`), `UseInterpreter`-Condition
`net10.0-ios` → `net11.0-ios`, TFMs in `Reporter.Core`, `Reporter.Data`,
`Reporter.Tests` (`net10.0` → `net11.0`), `Reporter.E2ETests`
(`net10.0-windows10.0.19041.0` → `net11.0-...`). Paketwelle:
`Microsoft.EntityFrameworkCore.Design`/`...Sqlite`/`Microsoft.Data.Sqlite`
10.0.12 → 11.0.0, `Microsoft.Extensions.Logging.Debug` 10.0.0 → 11.0.0,
`System.ServiceModel.Syndication` 10.0.11 → 11.0.0,
`Microsoft.Extensions.TimeProvider.Testing` 10.1.0 → 11.0.1;
`Microsoft.Maui.Controls` bleibt `$(MauiVersion)` (11er via Workload).
Harte TFM-Strings: `scripts/Run-E2ETests.ps1` (Kommentar Z. 13, `$tfm` Z. 24),
`scripts/iOS-Deployment.ps1:94`, `ReporterAppFixture.cs` Konventionspfad
(Z. ~143). `global.json` um `sdk.version 11.0.100` + `rollForward latestPatch`
ergänzen; `.config/dotnet-tools.json` mit `dotnet-ef` 11.0.0 neu anlegen;
`dotnet ef` auf 11.x aktualisieren. Gate G5 prüfen: falls MAUI 11 die
Plattform-Mindestversionen anhebt, `SupportedOSPlatformVersion`/
`TargetPlatformMinVersion` und `Package.appxmanifest` synchron ziehen,
sonst unverändert lassen. `dotnet workload restore Reporter.sln`
ausführen (Band 11.0.100), Restore+Build aller lokal baubaren TFMs
(Windows; Android, sofern Workload installiert) sowie `dotnet test`
der nicht-UI-Projekte grün. Test-Stack-Kompatibilität (Task 2.6:
`coverlet.MTP` 10.1.1, `Microsoft.NET.Test.Sdk` 18.12.0, `TrxReport`
2.5.3, `xunit.v3` 4.0.3, `xunit.runner.visualstudio` 4.0.1,
`FlaUI.UIA3` 5.0.0) unter .NET-11-SDK verifizieren — Anhebung nur bei
tatsächlicher Inkompatibilität. Dependabot-Status (Task 2.4): PRs
#196/#200 sind geschlossen und werden durch die eigene Anhebung
ersetzt — `dependabot.yml` bleibt unverändert (im Schritt dokumentiert,
kein Code-Eingriff).

**Akzeptanzkriterien:** `dotnet restore` + `dotnet build` für
`net11.0-windows10.0.19041.0` erfolgreich; `dotnet test` auf
Core/Data/Tests erfolgreich (MTP-Aufruf inkl. `--coverlet`/
`--report-trx`-Argumenten läuft unter .NET-11-SDK);
`scripts/Run-StaticChecks.ps1` läuft mit Exit 0 (Format, Security,
Static-Analysis-Build); kein `net10.0`-Verweis mehr in
`src/**/*.csproj`, `global.json`, genannten Scripts/Fixtures;
keine neue EF-Schema-Migration erzeugt.

### Schritt 2: EF-Core-11-Verifikation (Migrationsplan Phase 3)

Breaking-Changes-Checkliste gegen den aktuellen Repo-Stand erneut
durchsuchen (`OwnsOne`/`OwnsMany`, `AsSplitQuery`, `EFOptimizeContext`,
`Property()` auf primitive Collections, identische Owned-Instanz an
mehreren Navigations, `Migrate()` ohne Migrationen) und Ergebnis
dokumentieren. `MigrateAsync`-Pfad (`DatabaseMigrationService`,
`ItemContentMigrationService`, `PRAGMA table_info('items')` via
`GetDbConnection`/`DbCommand`) unter EF 11 durch Tests + manuellen Lauf
absichern; Reihenfolge-Invariante bleibt unverändert. Test-Factories
`EnsureCreated` vs. `Migrate` prüfen (kein Test fährt eine
`EnsureCreated`-DB mit `Migrate()` an). **Bestands-DB-Upgrade:** reale
`reporter.db` + `reporter-content.db` auf `maintenance/net10`-Stand
(v0.7.0, Git-Worktree) per `dotnet ef database update` (EF-10) erzeugen,
Kopien unter dem .NET-11-Build durch den echten Migrationspfad
(`MigrateAsync` + Legacy-Content-Kopie) migrieren, Erfolg + Daten-
Integrität dokumentieren. `UseInterpreter` inhaltlich reverifizieren
(Doku-/Code-Befund; Gerätetest = dokumentierter Restpunkt).
Coverage-Ausschluss `[*]Reporter.Data.Migrations.*` bleibt unverändert
(Task 3.6, Verifikation im Diff — keine Änderung nötig).
Keine neue EF-Schema-Migration anlegen (Migrations-Disziplin).

**Akzeptanzkriterien:** Checkliste re-verifiziert und dokumentiert;
alle Migrations-/DB-Tests grün; v0.7.0-DBs migrieren unter EF 11
fehlerfrei (Nachweis im Test-Artefakt); keine neue Schema-Migration
und keine Änderung am Coverage-Ausschluss im Diff.

### Schritt 3: CI/CD auf .NET 11 (Migrationsplan Phase 4)

`dotnet-version: '10.0.x'` → `'11.0.x'` in `staging-ci.yml` (inkl.
os-interface-tests-Job Z. ~188, vom Migrationsplan nicht enumeriert),
`pr-staging-ci.yml` (inkl. Z. ~191), `release.yml`, `security-scan.yml`,
`build-and-package/action.yml`, `package-android/action.yml` —
maßgeblich ist der dateiweite Grep, nicht die Zeilenliste des Plans;
`--framework`-Argumente und Kommentarpfade `net10.0-*` → `net11.0-*`.
`package-ios/action.yml`: `ios-workload-version` Default `26.5.10318` →
`27.2.11039`, SDK-Band `'10.0.1xx'` → `'11.0.1xx'`, temporäre `global.json`
`10.0.100` → `11.0.100`, Kommentarblock (Z. 52-68) auf die 11er-Feed-Logik
fortschreiben, Pack-Cleanup-Glob `Microsoft.iOS.Sdk.net10.0_*` →
`net11.0_*`, `--framework net10.0-ios` → `net11.0-ios`;
`MauiXamlInflator=XamlC` bleibt, Kommentar aktualisieren (SourceGen-Bug-
Workaround unter MAUI 11 nicht verifizierbar → konservativ beibehalten).
`setup-dotnet` SHA-Pin (v6) bleibt — Gate G3: löst `11.0.x`/`11.0.1xx`
zur Laufzeit über den Live-Index auf, Verifizierung durch den PR-Check.
`ci-instructions.md` mit ziehen (`10.0.x`/`net10.0`-Beispiele → 11er;
inkl. Z. ~1018, vom Migrationsplan nicht enumeriert).
Unverändert bleiben und im Schritt-Artefakt verifiziert werden:
`dotnet workload restore Reporter.sln`-Aufrufe in allen Workflows/
Actions (Task 4.3 — ziehen mit 11er-SDK die 11er-Workloads), das
ASP.NET-Core-Shared-Framework für E2E (Task 4.5 — kommt mit
`setup-dotnet '11.0.x'`), der Coverage-Ausschluss
`[*]Reporter.Data.Migrations.*` (Task 3.6). ReportGenerator 5.5.11-/
MTP-Aufrufe unverändert lassen, Verifikation über PR-Check
dokumentieren (Task 2.5/4.7).

**Akzeptanzkriterien:** kein `10.0.x`/`net10`-Verweis mehr in
`.github/`; alle geänderten YAML-Dateien syntaktisch valide;
`ci-instructions.md` 11er-konsistent; `workload restore`-Aufrufe und
Coverage-Ausschluss nachweislich unverändert; Gate-G3-/G4-Entscheidungen
dokumentiert.

### Schritt 4: Doku-Welle, Plan-Status und Release-Hinweise (Migrationsplan Phase 5.6/5.7/5.9)

Alle verbleibenden .NET-10-Referenzen bereinigen: `README.md`
(Badge `Z. 4`, SDK-Voraussetzung `Z. 47`, TFM-Beispiele),
`docs/help/release-management/{installation,ablauf-technisch,architektur,business-rules}.md`,
`docs/help/tests/{ablauf-technisch,api,architektur,installation,troubleshooting}.md`,
`docs/help/ios-deployment/*`, `docs/help/anwendung/mobile-ui-design.md`,
`docs/help/benachrichtigungen/installation.md`,
`docs/help/einstellungen/architektur.md`, `scripts/iOS-Deployment.md`.
Troubleshooting-Eintrag zur x86-64-v2-Mindestbasis mit CLR-Fehlertext
(G1/Task 5.9) und ggf. angehobenen Plattform-Mindestversionen (G5).
`docs/help/entwicklung/dotnet-11-migration.md` auf Status „durchgeführt"
heben (Datum, iOS-Pin 27.2.11039, verarbeitete Entscheidungen,
Restpunkte iOS-Gerät/TestFlight). `docs/RELEASE_NOTES.md` aktualisieren
(soweit Konvention vorhanden). Abschluss-Volltextsuche
`net10\.0\|10\.0\.x\|\.NET 10` über `docs/`, `README.md`,
`ci-instructions.md`, `scripts/`, `src/`, `.github/` — verbleibende
Treffer nur, wo bewusst historisch/Maintenance-Kontext korrekt ist
(z. B. `maintenance/net10`-Referenzen). Beim Status-Heben des
Migrationsdokuments die dortigen veralteten Zeilen-Enumerationen
gleich mit korrigieren (`staging-ci.yml:188`, `pr-staging-ci.yml:191`,
`ci-instructions.md:1018`, ReportGenerator-Zeilen). Abschließende
Gesamtverifikation des Gesamt-Diffs: `scripts/Run-StaticChecks.ps1`
Exit 0 sowie `dotnet test` aller Projekte (Task 5.1/5.2);
FlaUI-E2E-Lauf via `scripts/Run-E2ETests.ps1` versuchen — Ergebnis
(grün oder Umgebungsblocker mit Begründung) dokumentieren; der
CI-Gesamtlauf aller Plattformen (Task 5.3) erfolgt über den PR-Check
gegen `staging`.

**Akzeptanzkriterien:** Schluss-Grep ohne unbeabsichtigte Treffer;
Migrationsdokument trägt „durchgeführt" mit Pin-/Entscheidungsstand
und korrigierten Zeilen-Referenzen; Troubleshooting-/Release-Hinweise
zu x86-64-v2 vorhanden; `Run-StaticChecks.ps1` Exit 0 und
`dotnet test` aller Projekte grün auf dem Gesamtstand; E2E-Ergebnis
dokumentiert (grün oder begründeter Umgebungs-Restpunkt).

## Übersichtstabelle

| # | Titel | Abhängigkeiten | Betroffene Bereiche |
|---|-------|----------------|---------------------|
| 1 | SDK-/TFM-Flip + Paketwelle | Keine | `src/**`, `global.json`, `.config/`, `scripts/`, E2E-Fixture |
| 2 | EF-Core-11-Verifikation | 1 | `src/Reporter.Data`, `src/Reporter.Tests`, Verifikationsartefakte |
| 3 | CI/CD auf .NET 11 | 1 | `.github/workflows/*`, `.github/actions/*`, `ci-instructions.md` |
| 4 | Doku-Welle + Plan-Status | 1, 2, 3 | `README.md`, `docs/help/**`, `scripts/iOS-Deployment.md`, `docs/RELEASE_NOTES.md` |

## Offene Punkte

Keine ungeklärten fachlichen Punkte — die im Migrationsplan „zum
Migrationszeitpunkt zu klärenden" Punkte sind in `requirement.md`
(Abschnitt „Offene Punkte zum Migrationszeitpunkt") mit Befund und
getroffener Entscheidung dokumentiert. Verbleibende
Umgebungs-/organisatorische Restpunkte:

- **Release-Timing (Task 5.8):** Der Migrations-PR soll nicht direkt
  vor einem Release auf `main` gemergt werden — Merge-Zeitpunkt
  obliegt dem Anwender; der technische Teil (erste 11er-Auslieferung
  ohne neue EF-Schema-Migration) ist als Akzeptanzkriterium verankert.
- **iOS-Echtgerät/TestFlight (Task 5.4):** reale Geräte-Abnahme
  obliegt dem Anwender (Dokumentation im PR).
- **CI-Gesamtlauf (Task 5.3):** erfolgt über den PR-Check gegen
  `staging` (wird überwacht, Fehler werden bearbeitet).
- **FlaUI-E2E (Task 5.2):** lokaler Lauf via `scripts/Run-E2ETests.ps1`
  wird versucht und das Ergebnis dokumentiert.
