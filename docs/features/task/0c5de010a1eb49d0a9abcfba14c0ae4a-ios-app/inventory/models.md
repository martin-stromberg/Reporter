<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme — Datenmodell und Datenartefakte

Die Anforderung verlangt keine Schema-Änderung und keine neue EF-Core-Migration. Betroffen ist einzig das Laufzeit-Attribut der vorhandenen SQLite-Datenbankdatei (iCloud-Backup-Ausschluss, Punkt 8) sowie fachlich die Modelle, die für die Datenschutz-Dokumentation (Datenflüsse, Debugbericht) und die Review-Notizen (Demo-Seed) relevant sind.

## Datenbankdatei `reporter.db`

Kein Modell im Code, aber der zentrale betroffene Datenartefakt:

- Pfad wird in `MauiProgram.CreateMauiApp` (`src/Reporter/MauiProgram.cs`, Zeilen 44–54) bestimmt: `Path.Combine(FileSystem.AppDataDirectory, "reporter.db")`, Override über die Umgebungsvariable `REPORTER_DB_PATH`; das Zielverzeichnis wird vorab per `Directory.CreateDirectory` angelegt.
- `isFirstRun = !File.Exists(databasePath)` (Zeile 61) wird vor `builder.Build()` ausgewertet, weil `ApplyPersistedLanguage` die Datei per `context.Database.Migrate()` erzeugt.
- Erzeugt/migriert wird die Datei zweifach: synchron in `MauiProgram.ApplyPersistedLanguage` (Zeile 169, mit `try/catch`) und async in `App.OnStart` (`src/Reporter/App.xaml.cs`, Zeile 42, derzeit **ohne** `try/catch`).
- SQLite-Öffnung über `options.UseSqlite($"Data Source={databasePath}")` (Zeile 73); damit relevante Sidecar-Dateien `reporter.db-wal`/`reporter.db-shm` (WAL-Modus-abhängig) — offene Frage der Anforderung.
- Aktuell wird **kein** `NSUrl.IsExcludedFromBackupKey`-Attribut gesetzt — ein Durchgriff auf `Foundation.NSUrl` existiert im iOS-Code bisher nur in `Platforms/iOS/` (`NotificationDelegate`, `AppDelegate`).

## `ReporterDbContext`
Datei: `src/Reporter.Data/ReporterDbContext.cs`

| DbSet / Tabelle | Inhalt | Relevanz für die Anforderung |
|-----------------|--------|------------------------------|
| `Feeds` (`feeds`) | Abonnierte Feed-URLs, Titel, Kategorie, Health-Status, `FaviconUrl`, Fehlerfelder | Inhalte liegen ausschließlich lokal vor (Datenschutz-Doku, Punkt 1/2) |
| `Categories` (`categories`) | Feed-Kategorien | lokal |
| `Items` (`items`) | Artikel inkl. `ContentHtml`, `Link`, `IsRead`, `IsSavedForLater` | lokal |
| `Keywords` (`keywords`) | Keyword-Blacklist | lokal |
| `Settings` (`settings`) | Singleton-Einstellungssatz (`HasData(new Settings())`) | lokal; `DebugCollectionEnabled` steuert die Debug-Log-Sammlung |
| `SyncLogs` (`sync_logs`) | Sync-Protokoll inkl. Status/Meldung | geht in den Debugbericht ein (freiwilliger E-Mail-Versand) |
| `DebugLogEntries` (`debug_log_entries`) | Session-Debuglog (`Level`, `Category`, `Message`, `Details`, Index auf `Timestamp`) | geht in den Debugbericht ein |

## `Settings`
Datei: `src/Reporter.Core/Models/Settings.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Singleton-Id, `DefaultId = a1f5c6d2-…` |
| `RetentionDays` | `int` (required) | Aufbewahrungsdauer gelesener Artikel |
| `AutoMarkReadMode` | `string?` | Auto-Gelesen-Modus |
| `AutoMarkReadDelaySeconds` | `int` (required) | Verzögerung der Auto-Markierung |
| `NotificationsEnabled` | `bool` (required) | Globaler Benachrichtigungs-Schalter |
| `QuietHoursStart` / `QuietHoursEnd` | `TimeSpan?` | Ruhezeiten |
| `AutoRefreshEnabled` | `bool` (required) | Hintergrund-Aktualisierung (steuert auch den iOS-`BGAppRefreshTask`) |
| `RefreshIntervalMinutes` | `int` (required) | Abrufintervall |
| `RefreshOnStartupEnabled` | `bool` (required) | Abruf beim Start |
| `UnreadSortOrder` | `string?` | Sortierung der Ungelesen-Liste |
| `Theme` | `string?` | `system`/`light`/`dark` |
| `NotificationSummaryEnabled` | `bool` (required) | Sammel- vs. Einzel-Benachrichtigung |
| `Language` | `string?` | `system`/`de`/`en` — deckt sich mit den für `CFBundleLocalizations` (Punkt 9) vorgesehenen Sprachen |
| `DebugCollectionEnabled` | `bool` (required) | Schalter für die Session-Debug-Log-Sammlung (Default `false` per DB-Default) — relevant für die App-Privacy-Angaben |

## `FirstRunState`
Datei: `src/Reporter.Core/Models/FirstRunState.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `IsFirstRun` | `bool` (init) | `true`, wenn die DB-Datei vor diesem Start nicht existierte (ausgewertet in `MauiProgram`) |
| `DemoSeedSuppressed` | `bool` (init) | `REPORTER_DISABLE_DEMO_SEED`-Override (E2E/CI) |
| `ShouldSeedDemoContent` | `bool` (berechnet) | `IsFirstRun && !DemoSeedSuppressed`; steuert `DemoContentService.EnsureSeededAsync` — Grundlage des Review-Hinweises „kein Login erforderlich, Demo-Feed wird angelegt" |

## `AppDeviceInfo`
Datei: `src/Reporter.Core/Models/AppDeviceInfo.cs`

Snapshot, der im Debugbericht per E-Mail versendet wird (Quelle für die Datenschutz-Doku):

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `AppName` | `string?` | App-Name (`AppInfo`) |
| `AppVersion` | `string?` | Versionsstring |
| `AppBuild` | `string?` | Buildstring |
| `DeviceModel` | `string?` | Gerätemodell (`DeviceInfo`) |
| `DeviceManufacturer` | `string?` | Hersteller |
| `Platform` | `string?` | Plattform |
| `OsVersion` | `string?` | OS-Version |

Weitere im Debugbericht enthaltene Modell-Daten (ohne eigene Strukturänderung): `Feed` (`Title`, `Url`, `HealthStatus`, `LastCheckedAt`, `HealthLastChange`), `SyncLog` (`StartedAt`, `Status`, `Message`), `DebugLogEntry` (`Timestamp`, `Level`, `Category`, `Message`, `Details`) — siehe `DebugReportService.BuildBody` in [logic.md](logic.md).
