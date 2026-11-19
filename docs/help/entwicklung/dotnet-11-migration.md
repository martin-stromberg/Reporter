<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# .NET-11-Migration — Entscheidung und Migrationsplan

**Status:** geplant — Umsetzung in einem Folge-PR.
**Entscheidung gefällt:** 19.11.2026 (GitHub Issue #180).
**Fallback-Linie:** Der Maintenance-Branch `maintenance/net10` existiert bereits
(Commit `5492d71`, Stand v0.7.0).

Dieses Dokument hält die Entscheidung für die Migration auf .NET 11 / .NET MAUI 11 /
EF Core 11 samt Begründung und geprüfter Alternative fest und beschreibt den
Migrationsplan (Gates, Phasen, Risiken und Rollback), der im Migrations-PR
abgearbeitet wird.

---

## Entscheidung

**Geplante Migration auf .NET 11 (STS), mit `maintenance/net10` als Fallback-Linie.**

### Begründung

1. **Gleiches Support-Ende.** .NET 11 STS und .NET 10 LTS enden beide am **14.11.2028**.
   Auf .NET 10 zu verbleiben kauft kein zusätzliches Support-Fenster, blockiert aber die
   gesamte 11er-Paketwelle — die offenen Dependabot-PRs
   (`System.ServiceModel.Syndication 10.0.11 → 11.0.0`, `Microsoft.Data.Sqlite 10.0.12 → 11.0.0`)
   lassen sich ohne die Runtime-Migration nicht mergen und stapeln sich.
2. **EF Core 11 erzwingt .NET 11.** `Microsoft.EntityFrameworkCore.*` und
   `Microsoft.Data.Sqlite` können nicht isoliert angehoben werden (Build- und
   Runtime-Anforderung). Wer EF-Sicherheits-/Feature-Updates will, muss die Runtime mitziehen.
3. **MAUI-Workload-Kopplung an Xcode/iOS-SDK.** App-Store-Einreichungen erzwingen jährlich
   neuere Xcode-Versionen; neue Xcode-Versionen erfordern neue iOS-Workload-Generationen.
   Der iOS-Build ist heute schon gepinnt (`Microsoft.iOS.Sdk` **26.5.10318** + SDK-Band
   `10.0.1xx` in `.github/actions/package-ios/action.yml:44-68`, weil die 26.x-Manifeste nur
   im 10.0.100-Feed liegen). Auf der 10er-Workload-Linie zu frieren riskiert, dass
   iOS-Builds/TestFlight-Submissions in ~1 Jahr nicht mehr möglich sind.
4. **Migrationssprung klein halten.** .NET 10 → 11 jetzt ist ein kontrollierter Ein-Schritt-
   Sprung; „10 → 12 auf einmal" akkumuliert zwei Major-Versionen Breaking Changes
   (Runtime, MAUI, EF) in einer einzigen riskanten Migration.
5. **.NET 12 LTS wird ohnehin evaluiert** (Release Nov 2027). Die 11er-Migration schließt
   eine spätere 12er-Evaluation nicht aus — im Gegenteil: Sie hält die Upgrade-Routine
   warm und reduziert den Sprung 11 → 12 auf eine übliche Jahresmigration.

### Geprüfte Alternative: auf .NET 10 LTS bleiben → direkt .NET 12 (Nov 2027)

| Kriterium | Bewertung |
|---|---|
| Aufwand | Nur eine Migration statt zwei — aber dafür ein Doppelsprung mit deutlich mehr gleichzeitigen Breaking Changes |
| Paket-Updates | ~12 Monate blockiert bzw. auf 10er-Patch-Ebene begrenzt; 11er-Dependabot-PRs müssten dauerhaft ignoriert/geschlossen werden |
| iOS/Xcode | Höchstes Risiko: Workload-Pin 26.5.10318 altert gegen den App-Store-Xcode-Zwang; ein „Reaktivieren" der iOS-Pipeline im Jahr 2027 trifft evtl. auf nicht mehr verfügbare 10er-Manifeste |
| Support | .NET 10 LTS läuft bis 14.11.2028 — kein Sicherheitsrisiko, aber auch kein Gewinn |
| EF Core | EF Core 12 erfordert dann .NET 12 — die gleiche untrennbare Kopplung, nur später und größer |

**Fazit:** Die Alternative löst keines der drei Treiber-Probleme (Paketwelle,
Workload-Alterung, EF-Kopplung) und vergrößert den späteren Sprung. Verworfen — mit
dokumentiertem Trigger: Sollte die 11er-Migration bis zum .NET-12-Release (Nov 2027) nicht
gestartet sein, wird die Entscheidung erneut evaluiert.

### Verifizierte externe Fakten (Basis der Planung)

- **CPU-Mindestbasis .NET 11:** x64-JIT/AOT-Basis steigt von x86-64-v1 auf
  **x86-64-v2** (SSE4.2 + POPCNT u. a.); Windows-Arm64 auf **armv8.0-a + LSE**;
  R2R-Ziel x86-64-v3 (Win/Linux). CPUs ohne v2 starten die App nicht mehr
  („The current CPU is missing one or more of the baseline instruction sets.").
  Letzte betroffene CPUs ~2013 (z. B. Intel Bonnell/Atom). **Es gibt kein
  Projekt-Flag zum Absenken der Basis.**
- **EF Core 11** erfordert .NET-11-SDK (Build) und -Runtime. Relevante Breaking Changes:
  `Migrate()`/`MigrateAsync()` wirft, wenn keine Migrationen in der Assembly existieren
  (Reporter: beide Contexts haben Migrationen — 15 + 2, siehe Phase 3);
  `EFOptimizeContext`-Property entfernt; `Property()` konfiguriert keine primitiven
  Collections mehr; Split-Queries werfen bei Concurrent Modifications; dieselbe
  Owned-Instanz an mehreren Navigations wirft; EF-Tools-Pakete referenzieren
  `*.Design` nicht mehr automatisch.
- Neu in EF Core 11: `dotnet ef database update --add` (Migration erzeugen + anwenden
  in einem Schritt).

---

## Erfolgskriterien der Migration

Checkliste für den Migrations-PR — die Migration gilt als erfolgreich, wenn:

- [ ] Build aller Plattformen (Windows unpackaged, Android-APK, iOS) grün in CI.
- [ ] Alle Unit-Tests + FlaUI-E2E-Tests grün; `Run-StaticChecks.ps1` Exit 0.
- [ ] Bestands-DB-Upgrade getestet: `reporter.db`/`reporter-content.db` einer
  v0.7.0-Installation migrieren unter dem .NET-11-Build fehlerfrei.
- [ ] iOS: App läuft auf echtem Gerät (kein `ExecutionEngineException`) —
  `UseInterpreter` reverifiziert.
- [ ] Keine neue EF-Schema-Migration im selben Release wie der Runtime-Wechsel
  (Rollback-Fähigkeit der Nutzer-DBs, siehe Risiken).

---

## Voraussetzungen / Gates vor dem Umstieg

Diese Punkte sind **Vorbedingungen** (Go/No-Go) und werden zu Beginn des Migrations-PRs
abgehakt — nicht erst bei der Verifikation.

| # | Gate | Prüfung | Bewertung vorab |
|---|---|---|---|
| G1 | **Zielgeräte-CPU (x86-64-v2)** | Windows `SupportedOSPlatformVersion`/`TargetPlatformMinVersion` = 10.0.17763 (Win 10 1809). Geräte ohne SSE4.2+POPCNT sind faktisch nur CPUs vor ~2013 (letzte: Intel Bonnell/Atom). Keine Telemetrie vorhanden → Annahme „vernachlässigbar" + Release-Note + Troubleshooting-Eintrag mit der CLR-Fehlermeldung. Da PublishReadyToRun=false (`build-and-package/action.yml:49`) gilt nur die JIT-Basis v2, nicht das v3-R2R-Ziel. Arm64-Windows: armv8.0-a+LSE — Reporter baut nicht für win-arm64, relevant nur falls künftig hinzugefügt. | Voraussichtlich unkritisch |
| G2 | **Build-Agent-CPU** | `windows-latest`-/`macos-latest`-Runner (Azure/GitHub-VMs) erfüllen x86-64-v2 — beim ersten CI-Lauf faktisch verifiziert. | Unkritisch |
| G3 | **setup-dotnet-Auflösung** | `actions/setup-dotnet@v6` (SHA-gepinnt) muss `11.0.x`/`11.0.1xx` auflösen können; ggf. Action-Pin heben. | Zu prüfen |
| G4 | **iOS-Workload/Xcode-Kopplung** | Zum Migrationszeitpunkt ermitteln: welche iOS-Workload-Pack-Version (11er-Generation) zur Xcode-Version des `macos-latest`-Images und zu App-Store-Anforderungen passt; in welchem SDK-Band-Feed das Manifest liegt (gleiche Logik wie der bestehende 26.5.10318/10.0.1xx-Pin). | Offen bis Migrationszeitpunkt |
| G5 | **Plattform-Mindestversionen MAUI 11** | Prüfen, ob MAUI 11 iOS 15 / Android API 21 / Win 10.0.17763 anhebt (`Reporter.csproj:45-49`); falls ja: `Info.plist`-Effekt, `Package.appxmanifest` (`TargetDeviceFamily` Min `10.0.17763.0`, Z. 22-23) synchron ziehen, Release-Note. | Zu prüfen |
| G6 | **EF-Breaking-Changes-Vorabgleich** | Repo-Stand bereits geprüft: keine `OwnsOne`/`OwnsMany`, keine `AsSplitQuery`, kein `EFOptimizeContext`, `Tags` ist `string?` (keine primitive Collection) → vier der fünf Breaking Changes treffen das Modell nicht. `Migrate()`-ohne-Migrationen: beide Contexts haben Migrationen. Restliche Verifikation = Phase 3. | Vorab unkritisch |
| G7 | **Tooling** | `dotnet-ef` ist nicht repo-gepinnt → 11er-Version sicherstellen/dokumentieren; `dotnet-reportgenerator-globaltool 5.5.11` (CI) und MTP/`coverlet.MTP`-Stack unter .NET-11-SDK verifizieren. | Zu prüfen |
| G8 | **Entwicklungsmaschinen** | .NET-11-SDK + MAUI-11-Workloads lokal installieren (`dotnet workload restore Reporter.sln` nach TFM-Flip); iOS-Entwicklung: Mac/Xcode-Version gegen G4. | Organisatorisch |

---

## Phasen

Die Phasen beschreiben den **Migrations-PR** (und dessen Umfeld). Reihenfolge ist
bindend; Phase 1+2 werden als ein atomarer Umstellungs-Commit empfohlen, weil TFM- und
Paket-Flip untrennbar sind (EF 11 läuft nicht auf net10).

### Phase 0 — Maintenance-Strategie (Vorbereitung, jederzeit)

| Task | Detail |
|---|---|
| 0.1 | `maintenance/net10` existiert (`5492d71`, v0.7.0) — Rolle: **nur kritische Fixes** (Security, Datenverlust, Absturz), die vor der .NET-11-Auslieferung noch an Nutzer müssen. Keine Features. |
| 0.2 | Release-Weg für Hotfixes festlegen: `staging-ci.yml`/`pr-staging-ci.yml` triggern nur auf `staging` (`staging-ci.yml:6-8`, `pr-staging-ci.yml:6-8`) — PRs gegen `maintenance/net10` laufen also **ohne CI-Gate**. Entscheidung: bei erstem Hotfix-Bedarf den `branches`-Trigger um `maintenance/net10` erweitern oder manuelle Verifikation dokumentieren (empfohlen: Trigger erweitern, sobald der erste Hotfix ansteht — nicht präventiv). |
| 0.3 | semantic-release läuft auf `staging`/`main` — Hotfix-Release von `maintenance/net10` erfolgt manuell (Tag + Build via vorhandener Package-Actions), kein automatisierter Promotions-Pfad. |
| 0.4 | Backport-Regel: Fixes für `maintenance/net10` werden (falls für 11er-Linie relevant) zusätzlich auf `staging`/`main` gecherry-pickt — nie umgekehrt automatisch. |

### Phase 1 — SDK-/TFM-Flip

| Task | Stellen (IST → SOLL) |
|---|---|
| 1.1 | `src/Reporter/Reporter.csproj:8-13` — alle **6 bedingten TFM-Listen**: `net10.0-*` → `net11.0-*` (inkl. `net11.0-windows10.0.19041.0`; `IncludeIosTarget`/`IncludeAndroidTarget`-Mechanik bleibt). |
| 1.2 | `src/Reporter/Reporter.csproj:52-56` — `UseInterpreter`-Condition `'net10.0-ios'` → `'net11.0-ios'` (Inhalt Phase 3.4). |
| 1.3 | `src/Reporter.Core/Reporter.Core.csproj:6`, `src/Reporter.Data/Reporter.Data.csproj:6`, `src/Reporter.Tests/Reporter.Tests.csproj:6` — `net10.0` → `net11.0`. |
| 1.4 | `src/Reporter.E2ETests/Reporter.E2ETests.csproj:6` — `net10.0-windows10.0.19041.0` → `net11.0-windows10.0.19041.0`; `FrameworkReference Microsoft.AspNetCore.App` (Z. 24) folgt automatisch, Runner braucht ASP.NET-Core-11-Shared-Framework (via setup-dotnet). |
| 1.5 | `SupportedOSPlatformVersion`/`TargetPlatformMinVersion` (`Reporter.csproj:45-49`) — gegen Gate G5 prüfen; bei Anhebung `Package.appxmanifest` (Z. 22-23) synchron. |
| 1.6 | Harte TFM-Strings: `scripts/Run-E2ETests.ps1:24` (+Doc-Kommentar Z. 13), `scripts/iOS-Deployment.ps1:94`, `src/Reporter.E2ETests/ReporterAppFixture.cs:143` (Konventionspfad `bin/Debug/net10.0-...`), Composite-Actions `--framework`-Argumente (`build-and-package/action.yml:48`, `package-android/action.yml:50`, `package-ios/action.yml:156`), Pack-Glob `Microsoft.iOS.Sdk.net10.0_*` (`package-ios/action.yml:87`) → `net11.0_*`. |
| 1.7 | `global.json` — Entscheidung SDK-Pin: heute nur `test.runner` (kein Pin). Empfehlung: `sdk.version: "11.0.100", rollForward: "latestPatch"` pinnen, damit lokale Builds und CI dasselbe Band verwenden (CI schreibt in `package-ios` ohnehin eine temporäre global.json). Alternative „kein Pin" ist zulässig, muss aber dokumentiert werden. |
| 1.8 | `MauiXamlInflator` = `SourceGen` (`Reporter.csproj:30`) vs. iOS-CI-Override `XamlC` (`package-ios/action.yml:143-161`, SourceGen-Bug-Workaround) — unter MAUI 11 neu bewerten; Workaround ggf. entfernen. |

### Phase 2 — Paketwelle (atomar mit Phase 1)

| Task | Pakete |
|---|---|
| 2.1 | **MAUI:** `Microsoft.Maui.Controls` `$(MauiVersion)` (`Reporter.csproj:76`) — kein Repo-Pin; die 11er-Version kommt über `dotnet workload restore`/`install` mit dem 11er-SDK. Dependabot kann diese Zeile nie anheben (dokumentiert in `docs/help/release-management/wiederkehrende-reviews.md:38`). |
| 2.2 | **EF Core/SQLite 10.0.12 → 11.x:** `Microsoft.EntityFrameworkCore.Design` + `...Sqlite` (`Reporter.Data.csproj:18-22`), `...Sqlite` (`Reporter.Tests.csproj:17`), `Microsoft.Data.Sqlite` (`Reporter.E2ETests.csproj:17`). |
| 2.3 | **Microsoft.\*-Runtime-Pakete** — nur konsistent mit der Runtime anheben: `Microsoft.Extensions.Logging.Debug` 10.0.0 → 11.x (`Reporter.csproj:77`), `System.ServiceModel.Syndication` 10.0.11 → 11.x (`Reporter.Core.csproj:15`), `Microsoft.Extensions.TimeProvider.Testing` 10.1.0 → 11.x (`Reporter.Tests.csproj:18`). |
| 2.4 | **Dependabot-Koordination:** offene 11er-PRs (`System.ServiceModel.Syndication`, `Microsoft.Data.Sqlite`) nach dem TFM-Flip entweder rebasen/mergen oder durch die eigene Anhebung ersetzen und schließen. Keine Ignore-Regeln nötig, wenn die Migration zeitnah erfolgt. `dependabot.yml` (`target-branch: staging`, 3 Ökosysteme) bleibt unverändert. |
| 2.5 | **Tools:** `dotnet-ef` 11.x verwenden (nicht gepinnt → Version im Migrations-Doku-Stand festhalten; EF-Tools-Pakete referenzieren `*.Design` nicht mehr automatisch — `Reporter.Data` hat die Design-Referenz bereits, Z. 18-21). `dotnet-reportgenerator-globaltool 5.5.11` (`staging-ci.yml:129`, `pr-staging-ci.yml:132`) unter .NET-11-SDK testen, ggf. heben. |
| 2.6 | **Test-Stack (Welle 4, unabhängig, aber mitverifizieren):** `coverlet.MTP` 10.1.1, `Microsoft.NET.Test.Sdk` 18.12.0, `Microsoft.Testing.Extensions.TrxReport` 2.5.3, `xunit.v3` 4.0.3, `xunit.runner.visualstudio` 4.0.1, `FlaUI.UIA3` 5.0.0 — auf 11er-kompatible Versionen prüfen, nicht zwingend anheben. |

### Phase 3 — EF Core 11 Verifikation

| Task | Detail |
|---|---|
| 3.1 | **Breaking-Changes-Checkliste gegen `Reporter.Data`:** (a) `Migrate()` wirft ohne Migrationen — beide Contexts haben Migrationen (15 + 2), unkritisch, aber Design-Time-Factories (`ReporterDbContextFactory`, `ContentDbContextFactory`) behalten; (b) `EFOptimizeContext` — nicht im Repo; (c) `Property()`/primitive Collections — `Tags` ist `string?`, keine primitive Collection; (d) Split-Query Concurrent Modifications — kein `AsSplitQuery` im Repo, bei Migration erneut greppen; (e) gleiche Owned-Instanz an mehreren Navigations — keine `OwnsOne`/`OwnsMany`. **Bei Migration finale Repo-Suche wiederholen** (Modell kann sich bis dahin ändern). |
| 3.2 | **MigrateAsync-Pfad:** `DatabaseMigrationService.cs:34-44` (Reihenfolge-Invariante: `ItemContentMigrationService.MigrateLegacyContentAsync` **vor** `context.Database.MigrateAsync()`, weil `items.content_html` kopiert sein muss, bevor die Schema-Migration die Spalte löscht) und `ItemContentMigrationService.cs:42-50` (Content-Schema zuerst, dann Legacy-Kopie) unverändert — aber unter EF 11 durch Tests + manuellen Lauf absichern. Raw-SQL `PRAGMA table_info('items')` via `GetDbConnection()`/`DbCommand` (`ItemContentMigrationService.cs:97`) unter EF 11 verifizieren (Command-Behandlung, Sync-I/O). |
| 3.3 | **Test-Factories `EnsureCreated` vs. `Migrate`:** Unit-Tests nutzen `EnsureCreatedAsync` (`ReporterDbContextTests_*`, `ContentDbContextTests`) — sicherstellen, dass kein Test eine `EnsureCreated`-DB anschließend mit `Migrate()` anfährt (inkompatible Pfade, fehlende `__EFMigrationsHistory`). Migrations-Tests, die `MigrateAsync` auf Test-DBs fahren, bleiben unter EF 11 grün. |
| 3.4 | **UseInterpreter (iOS):** Condition wird in 1.2 umgezogen; inhaltlich verifizieren, ob EF Core 11 weiter `Expression.Compile()`/Reflection.Emit im Ausführungspfad nutzt (AOT-only-Gerät → `ExecutionEngineException`). Verifikation auf **echtem iOS-Gerät** (Phase 5): App starten, Migration laufen lassen, Feed-Refresh + Item-Queries ausführen. Parallel prüfen, ob MAUI 11 das `UseInterpreter`-Handling geändert hat. |
| 3.5 | **Bestands-DB-Upgrade:** reale `reporter.db` + `reporter-content.db` aus einer v0.7.0-Installation unter dem .NET-11-Build starten → `MigrateAsync` muss fehlerfrei durchlaufen (EF-10-History → EF-11-Runtime), inkl. Legacy-Content-Kopie-Pfad. Snapshot-Format der 17 vorhandenen Migrationen wird nicht neu generiert (nur bei neuer Migration nötig). |
| 3.6 | **Coverage-Ausschluss** `[*]Reporter.Data.Migrations.*` (`staging-ci.yml:125`, `pr-staging-ci.yml:128`) bleibt; Namenskonvention bei künftig neu generierten Migrationen beachten. |

### Phase 4 — CI/CD

| Task | Stellen |
|---|---|
| 4.1 | `dotnet-version: '10.0.x'` → `'11.0.x'` in: `staging-ci.yml` (Z. 68-70, 107-109), `pr-staging-ci.yml` (Z. 71-73, 110-112), `release.yml` (Z. 73-75), `security-scan.yml` (Z. 24-26), `build-and-package/action.yml` (Z. 17-19), `package-android/action.yml` (Z. 17-19). |
| 4.2 | **`package-ios/action.yml` (kritischste Stelle):** Input `ios-workload-version` Default `26.5.10318` (Z. 44-47) → 11er-Pack-Version (zum Migrationszeitpunkt gegen Gate G4 ermitteln); SDK-Band `'10.0.1xx'` (Z. 57-59) → passendes 11er-Band inkl. Feed-Verfügbarkeit der Manifeste (Kommentar-Logik Z. 52-59 entsprechend fortschreiben); temporäre global.json `10.0.100` (Z. 68) → `11.0.100`; Pack-Cleanup-Glob `net10.0_*` → `net11.0_*` (Z. 87); `MauiXamlInflator`-Override (Z. 143-161) gegen Task 1.8. |
| 4.3 | `dotnet workload restore Reporter.sln`-Aufrufe (staging-ci Z. 73/112, pr-staging-ci Z. 76/115, security-scan Z. 29, build-and-package Z. 22, package-android Z. 22) laufen unverändert — ziehen mit dem 11er-SDK automatisch die 11er-Workloads. |
| 4.4 | `setup-dotnet@v6` (SHA-gepinnt) — Gate G3: 11er-Auflösung prüfen, Pin ggf. heben. |
| 4.5 | E2E-Laufzeitabhängigkeit: Runner brauchen ASP.NET-Core-11-Shared-Framework (`FrameworkReference` in `Reporter.E2ETests.csproj:24`; `ReporterAppFixture.cs:143` Konventionspfad angepasst in 1.6) — kommt mit setup-dotnet `11.0.x`. |
| 4.6 | `ci-instructions.md` (generische Vorlage, `10.0.x`/`net10.0`-Beispiele Z. 194, 228, 449, 458, 465): Entscheidung — mitziehen oder als bewusst generische Vorlage stehen lassen (im Migrations-PR festlegen). |
| 4.7 | MTP-Runner (`global.json` `test.runner`) + `dotnet test -- ...`-Aufrufe (`--coverlet`, `--report-trx`) unter .NET-11-SDK verifizieren. |

### Phase 5 — Verifikation & Rollout

| Task | Detail |
|---|---|
| 5.1 | Lokaler Build + `scripts/Run-StaticChecks.ps1` → Exit 0 (Restore, Format, Security, Static-Analysis-Build). |
| 5.2 | `dotnet test` aller Projekte; E2E-Lauf via `scripts/Run-E2ETests.ps1` in interaktiver Session (FlaUI braucht sichtbares Fenster). |
| 5.3 | CI-Gesamtlauf: Build + Package **Windows** (unpackaged, `WindowsPackageType=None`), **Android** (APK, `IncludeAndroidTarget=true`), **iOS** (`macos-latest`, nur bei `IOS_SIGNING_ENABLED`). |
| 5.4 | iOS-TestFlight + echtes Gerät: `UseInterpreter`-Verifikation (3.4), App-Start, Migration, Feed-Refresh. |
| 5.5 | Bestands-DB-Upgrade-Test (3.5) dokumentieren. |
| 5.6 | **Doku-Welle** (nicht vergessen — alle Stellen mit .NET-10-Bezug): `README.md` (Badge `.NET-10.0` Z. 4, SDK-Voraussetzung Z. 47, TFM-Beispiele Z. 57/63/66), `docs/help/release-management/{installation,ablauf-technisch,architektur,business-rules}.md`, `docs/help/tests/{ablauf-technisch,api,architektur,installation,troubleshooting}.md`, `docs/help/ios-deployment/*`, `docs/help/anwendung/mobile-ui-design.md`, `docs/help/benachrichtigungen/installation.md`, `docs/help/einstellungen/architektur.md`, `scripts/iOS-Deployment.md`. Volltextsuche vor Abschluss: `grep -rn "net10\.0\|10\.0\.x\|\.NET 10" docs/ README.md ci-instructions.md scripts/`. |
| 5.7 | Dieses Dokument auf „durchgeführt"-Status heben (Datum, verarbeitete Abweichungen, gefundene iOS-Workload-Version). |
| 5.8 | **Release-Timing:** Migration bewusst **nicht** direkt vor einem Release auf `main` mergen. Empfehlung: direkt nach einem Release auf `staging` bringen, damit der volle staging→main-Zyklus als Stabilisierungsphase dient; erste .NET-11-Auslieferung **ohne** neue EF-Schema-Migration (Rollback-Fähigkeit, s. Risiken). |
| 5.9 | Release-Notes: CPU-Mindestbasis (x86-64-v2) + CLR-Fehlermeldung in Troubleshooting-Doku; ggf. angehobene Plattform-Mindestversionen (G5). |

---

## Risiken & Rollback

| Risiko | Eintritt | Mitigation / Rollback |
|---|---|---|
| iOS-Workload-Generation passt nicht zum Runner-Xcode | Mittel | Pin-Mechanik (`ios-workload-version` + Rollback-File) bleibt erhalten; Notlauf: iOS-Package pausieren (`IOS_SIGNING_ENABLED`), Rest der Migration unabhängig mergebar. |
| Nutzergerät ohne x86-64-v2 | Sehr gering (CPUs vor ~2013) | Kein Flag zum Absenken vorhanden → harte Grenze. Release-Note + Troubleshooting-Eintrag mit CLR-Fehlertext; Betroffene bleiben auf v0.7.x (`maintenance/net10`-Stand). |
| EF-11-Verhaltensänderung bricht `MigrateAsync`/PRAGMA-Pfad | Gering | Phase-3-Verifikation vor Merge; Fallback: Migration-PR zurückstellen (Branch verwerfen), `main` bleibt .NET 10. |
| Nutzer-DB nach Downgrade nicht mehr nutzbar | Mittel (nur bei neuen Schema-Migrationen) | **EF-Migrationen sind forward-only.** Eine unter EF 11 neu erzeugte Schema-Migration, die auf Nutzer-DBs angewendet wurde, kann ein Downgrade auf v0.7.x funktional brechen (Schema-Rückbau fehlt). → **Migrations-Disziplin:** erste .NET-11-Auslieferung ohne neue Schema-Migration (reiner Runtime-Wechsel, identisches Schema → Downgrade bleibt DB-seitig möglich). Danach jede neue Migration als „Point of no Return" im Release dokumentieren. |
| Merge-Konflikte durch Dependabot/parallele Arbeiten | Mittel | Migration als ein atomarer PR, kurze Lebensdauer; offene 11er-Dependabot-PRs im gleichen PR erledigen/schließen (Task 2.4). |
| .NET 11 STS „wasted effort" (Support-Ende identisch zu .NET 10) | — | Bewusst akzeptiert: Der Wert liegt in der entsperrten Paket-/Workload-Linie und dem kleineren Sprung zu .NET 12; Re-Evaluation Nov 2027. |
| Rollback-Pfad nach Merge | — | `maintenance/net10` (v0.7.0) bleibt als Fallback-Linie stehen; Code-Rollback per Revert-PR des Migrations-Commits auf `staging`; vor Release jederzeit möglich, nach Release nur für Nutzer ohne neue Schema-Migration sauber (s. o.). |

---

## Offene Punkte

Die folgenden Punkte sind bewusst offen gelassen und **zum Migrationszeitpunkt zu
klären** (jeweils mit Verweis auf Gate/Task):

1. **iOS-Workload-Pack-Version (11er-Generation)** — erst zum Migrationszeitpunkt
   ermittelbar (abhängig vom dann aktuellen Release-Xcode, `macos-latest`-Image und
   SDK-Band-Feed). Gate G4 / Task 4.2.
2. **Plattform-Mindestversionen unter MAUI 11** — ob iOS 15 / Android 21 / Win 17763
   angehoben werden. Gate G5.
3. **`global.json`-SDK-Pin** — Empfehlung `11.0.100` + `latestPatch` (Task 1.7), finale
   Entscheidung im Migrations-PR.
4. **`dotnet-ef`-Versionierung** — `.config/dotnet-tools.json` einführen oder weiter
   ungepinnt lassen (Task 2.5).
5. **setup-dotnet@v6-Auflösung für 11.x** — Gate G3, ggf. SHA-Pin heben.
6. **ReportGenerator 5.5.11 / MTP-Stack unter .NET-11-SDK** — Task 2.5/2.6/4.7.
7. **`ci-instructions.md` mitziehen oder stehen lassen** — Task 4.6.
8. **CI-Gate für `maintenance/net10`** — Trigger-Erweiterung erst bei erstem
   Hotfix-Bedarf (Task 0.2) oder präventiv.
9. **Betroffene Zielgeräte ohne x86-64-v2** — keine Telemetrie; Annahme
   „vernachlässigbar", Kommunikation über Release-Notes (Task 5.9).
10. **Central Package Management** — nicht Scope dieses Plans; als separates
    Folgethema für künftige Paketwellen prüfenswert.

## Verwandte Bereiche

- [Release-Management](../release-management/index.md) — CI/CD-Pipeline, Release-Prozess und Dependabot-Verwaltung.
- [Tests](../tests/index.md) — Unit-/Integrationstests und E2E-Smoke-Suite, die im Migrations-PR verifiziert werden.
