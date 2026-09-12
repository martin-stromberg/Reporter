# Reporter

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![Release](https://img.shields.io/github/v/release/martin-stromberg/Reporter?include_prereleases)](https://github.com/martin-stromberg/Reporter/releases)

Lokaler RSS-/Feed-Reader als .NET MAUI-App für Windows und iOS.

## Features

- .NET MAUI-App mit Shell-Navigation
- Untere Navigationsleiste mit **Ungelesen** (Dashboard mit Kategoriefilter, Pull-to-Refresh und Infinity-Scroll), **Feeds**, **Später**, **Kategorien** und **Einstellungen**
- Light/Dark-Theme-Unterstützung über .NET MAUI `AppThemeBinding`
- Design-System mit den Schriftarten **Newsreader** und **Inter** sowie Farb- und Typografie-Tokens aus dem `design-draft`
- Dependency Injection mit `Microsoft.Extensions.DependencyInjection`
- ViewModel-Basen mit `CommunityToolkit.Mvvm`
- Mehrsprachigkeits-Rüstung über RESX-Dateien (Deutsch/Englisch), Tab-Titel und Platzhaltertexte sind bereits an `AppResources` gebunden
- RSS-/Atom-Feed-Abruf, Parsing und Speicherung neuer Artikel inklusive Feed-Health (`OK`/`Warning`/`Error`) und Sync-Log
- Artikeldetailansicht mit WebView-Volltextdarstellung, automatischem Gelesen-Markieren, `Für später bewahren`-Toggle, Teilen und Öffnen im Browser (Issue #24)
- `Für später bewahren`-Funktion: Bookmark-Toggle in Artikelliste und Detailansicht; der Tab **Später** zeigt bewahrte Artikel nach `PublishedAt` absteigend sortiert und erlaubt das Entfernen der Bewahrung (Issue #25)
- Ausgebaute **Einstellungen**-Seite (Issue #26) mit Sofort-Persistierung in fünf Sektionen: Aufbewahrungsdauer & Speicher, Keyword-Filter (Blacklist), Synchronisation & Lesefluss, Benachrichtigungen & Ruhezeiten sowie Erscheinungsbild — Details siehe [docs/help/einstellungen/](docs/help/einstellungen/index.md)
- Keyword-Blacklist: Schlagworte werden als Chips verwaltet und per `KeywordMatcher` case-insensitiv als Teilwort auf Titel und HTML-Inhalt gematcht; gelesene Artikel mit Treffer werden nach Ablauf der Aufbewahrungsfrist gelöscht
- Automatische Hintergrund-Aktualisierung: `AutoRefreshService` ruft `IFeedSyncService.SyncAllAsync` per `PeriodicTimer` auf (Intervalle 15/30/60/240 Minuten, solange die App geöffnet ist)
- Theme-Auswahl **System** / **Hell** / **Dunkel** über `AppThemeService` (`Application.UserAppTheme`) — wirkt sofort app-weit und wird beim App-Start angewendet
- Automatische Retention-Löschung beim App-Start: `RetentionCleanupService` entfernt gelesene Artikel, deren Stichtag (`ReadAt ?? PublishedAt`) älter als `Settings.RetentionDays` ist, sowie keyword-gefilterte Artikel auf Basis ihres Veröffentlichungsdatums — ungelesene und bewahrte Artikel (`IsSavedForLater`) sind davon ausgenommen
- Lokale iOS-Benachrichtigungen bei neuen Artikeln (Issue #27): `NotificationService` wertet nach jedem Feed-Sync aus — globaler Schalter, Pro-Feed-Schalter im Feed-Formular, Ruhezeit (wird verworfen, nicht nachgeholt) und Keyword-Filter; wählbar einzeln pro Artikel oder als Sammel-Benachrichtigung pro Feed; Antippen öffnet den Artikel bzw. den Tab **Ungelesen** — Details siehe [docs/help/benachrichtigungen/](docs/help/benachrichtigungen/index.md)

## Projektstruktur

| Projekt | Verantwortlichkeit |
| --- | --- |
| `Reporter` | .NET MAUI-App, UI, Navigation |
| `Reporter.Core` | Domänenmodelle, Schnittstellen, ViewModels, mehrsprachige RESX-Ressourcen und Anwendungs-Services |
| `Reporter.Data` | Datenbankzugriff und Repositories |
| `Reporter.Tests` | Unit- und Integrationstests |

## Voraussetzungen

- .NET 10 SDK
- Windows: Windows 10 Build 19041 oder höher
- iOS/macOS: Xcode (auf macOS)

## Installation / Setup

```bash
dotnet build Reporter.sln
```

`Reporter` ist auf `net10.0-windows10.0.19041.0` und `net10.0-ios` ausgerichtet; `dotnet build Reporter.sln` baut beide Ziele. Für iOS-Geräte-Deployment/Signing ist Xcode auf macOS erforderlich.

## Git Hooks

Die Repository enthält Git-Hooks im Ordner `.githooks` (aus dem [Pattern-Collection](https://github.com/martin-stromberg/Pattern-Collection/tree/main/Git-Hooks)-Repo).

Aktivieren:

```bash
git config --local core.hooksPath .githooks
```

Oder unter Windows `install-hooks.cmd` / unter macOS/Linux `install-hooks.sh` ausführen.

Die Hooks prüfen unter anderem:
- Konsistenz der RESX-Lokalisierung (`translation-check.py`)
- Platzhalter-Implementierungen (`no-notimplemented-check.py`)
- Razor-Lokalisierung und -Verwendung (`razor-l10n-check.py`, `razor-usage-check.py`)
- Enum-Testabdeckung (`enum-coverage-check.py`)
- XML-Dokumentation in `.cs`/`.csproj` (`csproj-xmldoc-check.py`)

Hinweis: `pre-push` blockiert direkte Pushes auf `main` und `staging`.

Die Projekte sind bereits so konfiguriert, dass `GenerateDocumentationFile` aktiviert und `CS1591` als Fehler behandelt wird. Neue öffentliche APIs müssen also mit XML-Dokumentation (`<summary>`, `<param>`, `<returns>`) versehen werden.

## Starten

Auf Windows:

```bash
dotnet run --project src/Reporter/Reporter.csproj -f net10.0-windows10.0.19041.0
```

Auf macOS/iOS:

```bash
dotnet build src/Reporter/Reporter.csproj -f net10.0-ios
dotnet run --project src/Reporter/Reporter.csproj -f net10.0-ios
```

Fuer gezieltes Build/Deployment (IPA, Simulator, physisches Geraet) steht `scripts/iOS-Deployment.ps1` bereit:

**Wichtig:** `simulator` und `device` setzen voraus, dass das Skript direkt auf einem Mac ausgefuehrt wird.
Microsoft unterstuetzt `dotnet build -t:Run` fuer iOS/tvOS aktuell **nicht von Windows aus**.
Auf Windows koennen mit `build` die iOS-Kompilate (bzw. mit Codesigning `.ipa`) erzeugt werden;
das Deployment muss dann ueber Visual Studio oder manuell auf dem Mac erfolgen.

Auf dem Mac startet `simulator` die App im iOS-Simulator, wartet kurz und speichert
einen Screenshot unter `src/Reporter/bin/<config>/net10.0-ios/<rid>/`.

```powershell
# Menue starten
.\scripts\iOS-Deployment.ps1

# Beispiele
.\scripts\iOS-Deployment.ps1 -Action build -CodesignKey "Apple Distribution: ..." -CodesignProvision "ReporterProfile" -ServerAddress 192.168.1.10 -ServerUser me
.\scripts\iOS-Deployment.ps1 -Action simulator -Device "E25BBE37-69BA-4720-B6FD-D54C97791E79"
.\scripts\iOS-Deployment.ps1 -Action device -Device "DEINE-UDID" -CodesignKey "..." -CodesignProvision "..."
```

## Konfiguration

Alle nutzerkonfigurierbaren Optionen liegen im Singleton-`Settings`-Datensatz (Tabelle `settings`) und werden über die **Einstellungen**-Seite sofort persistiert:

| Einstellung | Feld | Standard | Beschreibung |
|-------------|------|----------|--------------|
| Aufbewahrungsdauer | `RetentionDays` | `30` | Tage, die gelesene Artikel aufbewahrt werden (1–365) |
| Hintergrund-Aktualisierung | `AutoRefreshEnabled` | `true` | Periodischer Feed-Sync Ein/Aus |
| Abruf-Intervall | `RefreshIntervalMinutes` | `30` | Minuten zwischen den Syncs (15/30/60/240) |
| Auto-Gelesen beim Öffnen | `AutoMarkReadMode` | `"on_scroll"` | `"off"` deaktiviert die automatische Gelesen-Markierung |
| Markierungs-Verzögerung | `AutoMarkReadDelaySeconds` | `5` | Sekunden bis zur Gelesen-Markierung (0/1/3/5) |
| Benachrichtigungen | `NotificationsEnabled` | `true` | Globaler Schalter für lokale Benachrichtigungen (nur iOS wirksam); beim Aktivieren wird die System-Berechtigung angefragt |
| Sammel-Benachrichtigung | `NotificationSummaryEnabled` | `false` | `true` = eine Benachrichtigung pro Feed und Sync statt je eine pro Artikel |
| Ruhezeit VON/BIS | `QuietHoursStart` / `QuietHoursEnd` | `null` | Nicht-stören-Zeitraum, auch über Mitternacht |
| Erscheinungsbild | `Theme` | `"system"` | `"system"` / `"light"` / `"dark"` |

Der Ein/Aus-Schalter für die Ruhezeit ist reiner Ansichts-Zustand (`QuietHoursEnabled`, keine eigene Spalte): Ausgeschaltet werden `QuietHoursStart`/`QuietHoursEnd` als `null` persistiert, die zuletzt gewählten Zeiten bleiben für die Sitzung erhalten.

Zusätzlich besitzt jeder Feed einen eigenen **Benachrichtigungen**-Schalter (`Feed.NotificationsEnabled`, Standard `true`) im Bearbeitungsformular auf der **Feeds**-Seite — damit lassen sich einzelne Feeds stummschalten.

Keyword-Filter liegen als eigene Datensätze in der Tabelle `keywords`. Details siehe [docs/help/einstellungen/](docs/help/einstellungen/index.md).

## Architektur

- `MauiProgram.CreateMauiApp()` konfiguriert DI, Fonts und MAUI.
- `AppShell` definiert die Tabs **Ungelesen**, **Feeds**, **Später**, **Kategorien** und **Einstellungen**.
- `Colors.xaml` und `Styles.xaml` implementieren das Design-System (Light/Dark, Newsreader/Inter, Farbtokens).
- `Reporter.Core` enthält die Domänenmodelle (`Feed`, `Category`, `Item`, `Keyword`, `Settings`, `SyncLog`), Repository-Schnittstellen (`IFeedRepository`, `ICategoryRepository`, `IItemRepository`, `IKeywordRepository`, `ISettingsRepository`, `ISyncLogRepository`) und die Anwendungs-Services `IFeedSyncService` / `FeedSyncService`, `IRetentionCleanupService` / `RetentionCleanupService` (Retention-Löschung gelesener Artikel beim App-Start inkl. Keyword-Regel, ausgenommen ungelesene und `IsSavedForLater`-Artikel), `IKeywordMatcher` / `KeywordMatcher` (`OrdinalIgnoreCase`-Teilwort-Matching) sowie `IAutoRefreshService` / `AutoRefreshService` (periodischer Hintergrund-Sync per `PeriodicTimer` über `TimeProvider`) und `INotificationService` / `NotificationService` (Benachrichtigungs-Entscheidung nach jedem Sync: Pro-Feed-/globaler Schalter, Ruhezeit via `TimeProvider`, Keyword-Filter, Einzel- vs. Sammel-Modus mit stabilen Dedup-Identifiern).
- `IAppThemeService` / `AppThemeService` (`src/Reporter/Services/`) setzt `Application.UserAppTheme` anhand `Settings.Theme` — das Interface liegt in `Reporter.Core`, die Implementierung im MAUI-Projekt, da Core keine MAUI-Referenz hat.
- `ILocalNotificationService` / `LocalNotificationService` (`src/Reporter/Services/`) kapselt das iOS-`UserNotifications`-Framework (`UNUserNotificationCenter`, `#if IOS`, No-Op auf anderen Plattformen) nach demselben Gateway-Muster. `NotificationDelegate` unter `Platforms/iOS/` zeigt Banner auch im Vordergrund und navigiert beim Antippen in-app zur Artikeldetailansicht (`articledetail?itemId=…`) bzw. bei Sammel-Benachrichtigungen zum Tab **Ungelesen** (mit Browser-Fallback beim Kaltstart).
- `Reporter.Data` stellt die EF Core-Entitäten und Repository-Implementierungen bereit; Repositories verwenden `IDbContextFactory<ReporterDbContext>` für kurzlebige, thread-sichere DbContext-Instanzen.
- `Reporter` (MAUI-Projekt) enthält die Seiten (`FeedsPage` usw.) und das App-Shell-Setup.
- `Reporter.Core` enthält `BaseViewModel`, die ViewModels (`FeedsViewModel`, `CategoriesViewModel`, `UnreadViewModel`, `LaterViewModel`, `SettingsViewModel`) und `AppResources`.
- `FeedsViewModel` nutzt `IFeedSyncService` für manuelles Refresh einzelner oder aller Feeds.

## Tests

```bash
dotnet test Reporter.sln
```

- `Reporter.Tests` referenziert `Reporter.Core` und `Reporter.Data`, sodass ViewModels (z. B. `FeedsViewModel`) und Services direkt getestet werden können.
- xUnit mit EF Core SQLite (In-Memory) für Repository- und Integrationstests; `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`) für den Timer-basierten `AutoRefreshService`; handgeschriebene Fakes statt Mocking-Framework.
- `SettingsViewModelTests_*` decken den Einstellungs-Flow inkl. Keyword-Verwaltung und Sofort-Persistierung ab, `KeywordMatcherTests` das Teilwort-Matching.
- `FeedsViewModelTests` deckt die UI-nahen Refresh-Commands ab: Refresh für einen Feed, Refresh aller Feeds und Fehleranzeige.
- `NotificationServiceTests` decken die Benachrichtigungs-Entscheidungslogik ab (Ruhezeiten inkl. Mitternachts-Wrap-around via `FakeTimeProvider`, Keyword-Filter, Einzel-/Sammel-Modus, Dedup-Identifier); `FakeLocalNotificationService`/`FakeNotificationService` kapseln den nicht unit-testbaren Plattformdienst, und `FeedSyncServiceTests` prüft die Integration Ende-zu-Ende auf Service-Ebene.

## CI/CD

Das Repository verwendet GitHub Actions für Qualitätsgates:

- `.github/workflows/pr-staging-ci.yml` — führt bei PRs nach `staging` parallel `static checks` (Format, Security-Scan, statische Analyse) und `build & test` (inkl. Coverage-Threshold 70 %) aus.
- `.github/workflows/staging-ci.yml` (`Pre-Release`) — Build-/Test-Pipeline auf `staging`.
- `.github/workflows/staging-to-main-promotion.yml` — automatisierte Promotion von `staging` nach `main` (Label `automated-promotion`).
- `.github/workflows/sync-staging-with-main.yml` — Backmerge-PRs von `main` nach `staging` (Label `automated-backmerge`).
- `.github/workflows/release.yml` — Semantic-Release auf `main` bzw. Tags `v*.*.*` (Konfiguration in `release.config.js`).
- `.github/workflows/verify-pr-source.yml` — erlaubt PRs nach `main` nur aus `staging`.
- `.github/workflows/security-scan.yml` — wöchentlicher Sicherheits-Scan der Abhängigkeiten.
- `.github/actions/security-scan/action.yml` — wiederverwendbare Composite Action für den Vulnerability-Scan.

**Manuelle Schritte nach dem Merge in `staging`:**
- Branch-Protection für `staging` aktivieren und die Status Checks `static checks` und `build & test` als erforderlich markieren.
- Labels `automated-promotion` (`0E8A16`) und `automated-backmerge` (`1D76DB`) anlegen.

## Changelog

Siehe [changes.log](changes.log).

## Dokumentation

- [Hilfe / Anwenderdokumentation](docs/help/index.md)
- [Einstellungen](docs/help/einstellungen/index.md) — Aufbewahrungsdauer, Keyword-Filter, Hintergrund-Aktualisierung, Benachrichtigungen & Ruhezeiten, Erscheinungsbild
- [Benachrichtigungen](docs/help/benachrichtigungen/index.md) — Lokale iOS-Benachrichtigungen: Schalter, Ruhezeiten, Sammel-Modus, Berechtigung und Tap-Navigation
