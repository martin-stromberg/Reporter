# Reporter

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![Pre-Release](https://img.shields.io/github/actions/workflow/status/martin-stromberg/Reporter/staging-ci.yml?branch=staging&label=Pre-Release)](https://github.com/martin-stromberg/Reporter/actions/workflows/staging-ci.yml)
[![Release-Workflow](https://img.shields.io/github/actions/workflow/status/martin-stromberg/Reporter/release.yml?label=Release-Workflow)](https://github.com/martin-stromberg/Reporter/actions/workflows/release.yml)
[![Release](https://img.shields.io/github/v/release/martin-stromberg/Reporter?include_prereleases)](https://github.com/martin-stromberg/Reporter/releases)

Lokaler RSS-/Feed-Reader als .NET MAUI-App für Windows und iOS.

## Features

- .NET MAUI-App mit Shell-Navigation
- Untere Navigationsleiste mit **Ungelesen** (Dashboard mit Kategoriefilter, Pull-to-Refresh und Infinity-Scroll), **Feeds**, **Später**, **Kategorien** und **Einstellungen**
- Light/Dark-Theme-Unterstützung über .NET MAUI `AppThemeBinding`
- Design-System mit den Schriftarten **Newsreader** und **Inter** sowie Farb- und Typografie-Tokens aus dem `design-draft`
- Dependency Injection mit `Microsoft.Extensions.DependencyInjection`
- ViewModel-Basen mit `CommunityToolkit.Mvvm`
- Vollständig lokalisierte UI über RESX-Dateien (`AppResources`, neutral = Englisch, `AppResources.de.resx` = Deutsch): Die Sprache folgt automatisch der Systemsprache (`CurrentUICulture`) und wirkt beim nächsten App-Start; Englisch ist der Fallback für alle anderen Sprachen. Ein manueller Sprachwechsel ist bewusst zurückgestellt (Folge-Issue #62), das Sync-Protokoll bleibt technisch/englisch — Details siehe [docs/help/anwendung/sprache.md](docs/help/anwendung/sprache.md) (Issue #28)
- RSS-/Atom-Feed-Abruf, Parsing und Speicherung neuer Artikel inklusive Feed-Health (`OK`/`Warning`/`Error`) und Sync-Log
- Artikeldetailansicht mit WebView-Volltextdarstellung, automatischem Gelesen-Markieren, `Für später bewahren`-Toggle, Teilen und Öffnen im Browser (Issue #24)
- `Für später bewahren`-Funktion: Bookmark-Toggle in Artikelliste und Detailansicht; der Tab **Später** zeigt bewahrte Artikel nach `PublishedAt` absteigend sortiert und erlaubt das Entfernen der Bewahrung (Issue #25)
- Ausgebaute **Einstellungen**-Seite (Issue #26) mit Sofort-Persistierung in fünf Sektionen: Aufbewahrungsdauer & Speicher, Keyword-Filter (Blacklist), Synchronisation & Lesefluss, Benachrichtigungen & Ruhezeiten sowie Erscheinungsbild — Details siehe [docs/help/einstellungen/](docs/help/einstellungen/index.md)
- Keyword-Blacklist: Schlagworte werden als Chips verwaltet und per `KeywordMatcher` case-insensitiv als Teilwort auf Titel und HTML-Inhalt gematcht; gelesene Artikel mit Treffer werden nach Ablauf der Aufbewahrungsfrist gelöscht
- Automatische Hintergrund-Aktualisierung: `AutoRefreshService` ruft `IFeedSyncService.SyncAllAsync` per `PeriodicTimer` auf (Intervalle 15/30/60/240 Minuten, solange die App geöffnet ist)
- Theme-Auswahl **System** / **Hell** / **Dunkel** über `AppThemeService` (`Application.UserAppTheme`) — wirkt sofort app-weit und wird beim App-Start angewendet
- Automatische Retention-Löschung beim App-Start: `RetentionCleanupService` entfernt gelesene Artikel, deren Stichtag (`ReadAt ?? PublishedAt`) älter als `Settings.RetentionDays` ist, sowie keyword-gefilterte Artikel auf Basis ihres Veröffentlichungsdatums — ungelesene und bewahrte Artikel (`IsSavedForLater`) sind davon ausgenommen
- Lokale iOS-Benachrichtigungen bei neuen Artikeln (Issue #27): `NotificationService` wertet nach jedem Feed-Sync aus — globaler Schalter, Pro-Feed-Schalter im Feed-Formular, Ruhezeit (wird verworfen, nicht nachgeholt) und Keyword-Filter; wählbar einzeln pro Artikel oder als Sammel-Benachrichtigung pro Feed; Antippen öffnet den Artikel bzw. den Tab **Ungelesen** — Details siehe [docs/help/benachrichtigungen/](docs/help/benachrichtigungen/index.md)
- Offline-Fähigkeit (Issue #28): Alle synchronisierten Artikel, Feeds und Kategorien bleiben ohne Netzwerk lesbar (lokale SQLite-Daten); `INetworkStatusService`/`NetworkStatusService` überwacht die Konnektivität über `Connectivity.Current`. Offline zeigen **Ungelesen** (gedimmter Sync-Button + Statuszeilen-Hinweis), **Feeds** und **Später** (Hinweis-Banner) sowie die Artikeldetailansicht den Zustand an; Refresh-Commands, `AutoRefreshService` und `FeedSyncService` brechen offline sauber ab (ohne `SyncLog`-Eintrag); `ArticleHtmlSanitizer` neutralisiert Links und entfernt `<img>`-Elemente im Artikel-HTML, `WebViewNavigationGuard` sperrt Rest-Navigation mit lokalisiertem Hinweis-Dialog, `ArticleCardView`-Thumbnails werden ausgeblendet und „Im Browser öffnen" zeigt einen Hinweis statt abzustürzen — Details siehe [docs/help/anwendung/offline.md](docs/help/anwendung/offline.md)

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

`Reporter` ist auf `net10.0-windows10.0.19041.0` und `net10.0-ios` ausgerichtet; `dotnet build Reporter.sln` baut beide Ziele. Die Target Frameworks lassen sich über die MSBuild-Schalter `IncludeIosTarget` (Default `true`) und `IncludeAndroidTarget` (Default `false`) steuern — mit `-p:IncludeAndroidTarget=true` kommt `net10.0-android` hinzu (erfordert den Android-Workload). Für iOS-Geräte-Deployment/Signing ist Xcode auf macOS erforderlich.

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
- `INetworkStatusService` / `NetworkStatusService` (`src/Reporter/Services/`) abstrahiert `Connectivity.Current` nach demselben Gateway-Muster und dispatcht `ConnectivityChanged` per `MainThread.BeginInvokeOnMainThread` auf den UI-Thread. `BaseViewModel` kapselt das `IsOnline`-Tracking für alle ViewModels (`InitConnectivity`/`TrackConnectivity`/`UntrackConnectivity`/`RefreshConnectivityStatus`); `ArticleHtmlSanitizer` und `WebViewNavigationGuard` in `Reporter.Core` halten die Offline-HTML-Aufbereitung und Link-Klassifizierung unit-testbar.
- `Reporter.Data` stellt die EF Core-Entitäten und Repository-Implementierungen bereit; Repositories verwenden `IDbContextFactory<ReporterDbContext>` für kurzlebige, thread-sichere DbContext-Instanzen.
- `Reporter` (MAUI-Projekt) enthält die Seiten (`FeedsPage` usw.) und das App-Shell-Setup.
- `Reporter.Core` enthält `BaseViewModel`, die ViewModels (`FeedsViewModel`, `CategoriesViewModel`, `UnreadViewModel`, `LaterViewModel`, `SettingsViewModel`) und `AppResources`.
- `FeedsViewModel` nutzt `IFeedSyncService` für manuelles Refresh einzelner oder aller Feeds.

## Tests

```bash
dotnet test Reporter.sln
npm test   # node:test-Suite für die Release-Skripte unter scripts/ (*.test.mjs)
```

- `Reporter.Tests` referenziert `Reporter.Core` und `Reporter.Data`, sodass ViewModels (z. B. `FeedsViewModel`) und Services direkt getestet werden können.
- `npm test` führt die `node:test`-Suite für die Release-Tooling-Skripte aus (`resolve-release-version.mjs`, `release-assets.mjs`, `create-update-manifest.mjs`) — u. a. Tag-Parsing, Release-Klassifizierung, Prerelease-Guard und `update.json`-Manifest-Erzeugung.
- xUnit mit EF Core SQLite (In-Memory) für Repository- und Integrationstests; `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`) für den Timer-basierten `AutoRefreshService`; handgeschriebene Fakes statt Mocking-Framework.
- `SettingsViewModelTests_*` decken den Einstellungs-Flow inkl. Keyword-Verwaltung und Sofort-Persistierung ab, `KeywordMatcherTests` das Teilwort-Matching.
- `FeedsViewModelTests` deckt die UI-nahen Refresh-Commands ab: Refresh für einen Feed, Refresh aller Feeds und Fehleranzeige.
- `NotificationServiceTests` decken die Benachrichtigungs-Entscheidungslogik ab (Ruhezeiten inkl. Mitternachts-Wrap-around via `FakeTimeProvider`, Keyword-Filter, Einzel-/Sammel-Modus, Dedup-Identifier); `FakeLocalNotificationService`/`FakeNotificationService` kapseln den nicht unit-testbaren Plattformdienst, und `FeedSyncServiceTests` prüft die Integration Ende-zu-Ende auf Service-Ebene.
- `FakeNetworkStatusService` simuliert Online-/Offline-Wechsel; `ArticleHtmlSanitizerTests` prüft Link-Neutralisierung und `<img>`-Entfernung, `WebViewNavigationGuardTests` die externe-URL-Klassifizierung und `BaseViewModelConnectivityTests` das `IsOnline`-Tracking; die ViewModel-Tests (`UnreadViewModelTests`, `FeedsViewModelTests`, `LaterViewModelTests`) decken die Offline-Frühabbrüche der Refresh-Commands ab.

## CI/CD

Das Repository verwendet GitHub Actions für eine vollautomatische Release-Pipeline nach dem Branch-Modell `staging` → `main` — Details siehe [docs/help/release-management/](docs/help/release-management/index.md):

- `.github/workflows/pr-staging-ci.yml` (`PR CI for Staging`) — führt bei PRs nach `staging` parallel `static checks` (Format, Security-Scan, statische Analyse) und `build & test` (inkl. Coverage-Threshold 70 %) aus; reine Backmerge-PRs werden über `detect-backmerge`/`back-merge-skip` erkannt und überspringen die Gates.
- `.github/workflows/staging-ci.yml` (`Pre-Release`) — nach den Gates ermittelt `semantic-release --dry-run` aus den Conventional-Commits-Botschaften die nächste Version und erzeugt ein RC-Pre-Release `vX.Y.Z-rc.N` mit `release-win-x64.zip` (Windows), `release-android.apk` (Android) und dem Update-Manifest `update.json`; `release-ios.ipa` kommt hinzu, sobald die Repository-Variable `IOS_SIGNING_ENABLED=true` gesetzt ist.
- `.github/workflows/staging-to-main-promotion.yml` — öffnet nach erfolgreichem Pre-Release-Lauf einen Draft-PR `staging` → `main` (Label `automated-promotion`).
- `.github/workflows/release.yml` (`Release`) — stabiles Release `vX.Y.Z` bei Push auf `main` bzw. manuellem Tag `v*.*.*` (semantic-release, `release.config.js` mit `branches: ["main"]`); existiert ein Release bereits ohne vollständige Assets, werden die fehlenden Dateien per Asset-Repair (`upload-existing`) nachgeladen statt ein neues Release anzulegen.
- `.github/workflows/sync-staging-with-main.yml` (`Backmerge Main to Staging`) — öffnet nach jedem Push auf `main` bei Bedarf einen PR `main` → `staging` (Label `automated-backmerge`, zwingend per „Create a merge commit" mergen).
- `.github/workflows/verify-pr-source.yml` — erlaubt PRs nach `main` nur aus `staging`.
- `.github/workflows/security-scan.yml` — wöchentlicher Sicherheits-Scan der Abhängigkeiten.
- Composite Actions unter `.github/actions/`: `build-and-package` (Windows-`win-x64`-ZIP), `package-android` (APK), `package-ios` (IPA auf `macos-latest`, über `vars.IOS_SIGNING_ENABLED` aktivierbar), `checkout-release-tag` (Tag-Checkout für den Asset-Repair-Pfad) und `security-scan`.
- Release-Tooling: `release.config.js` + `package.json` (gepinnte semantic-release-Toolchain, `npm run release`), Skripte unter `scripts/`: `resolve-release-version.mjs` (Versions-/Release-Auflösung inkl. Asset-Vollständigkeitsprüfung und Prerelease-Guard), `release-assets.mjs` (Asset-Liste inkl. iOS-Schalter), `create-update-manifest.mjs` (`update.json` mit `sha256`/`sizeBytes`/`assetUrl` pro Asset).

**Repository-Nacharbeiten:** Die Labels `automated-promotion`/`automated-backmerge` legen die Workflows bei Bedarf selbst an. Branch-Protection für `main`/`staging` (Required Checks `static checks`, `build & test`, `verify-source`) ist als Admin-Nacharbeit dokumentiert — die Protection-APIs antworten im privaten Repository auf dem Free-Plan mit HTTP 403 (siehe [Installation & Konfiguration](docs/help/release-management/installation.md)). iOS-Signierung wird ohne Codeänderung über die Secrets `IOS_CODESIGN_KEY`/`IOS_PROVISIONING_PROFILE` plus die Variable `IOS_SIGNING_ENABLED=true` aktiviert.

## Changelog

Siehe [changes.log](changes.log).

## Dokumentation

- [Hilfe / Anwenderdokumentation](docs/help/index.md)
- [Einstellungen](docs/help/einstellungen/index.md) — Aufbewahrungsdauer, Keyword-Filter, Hintergrund-Aktualisierung, Benachrichtigungen & Ruhezeiten, Erscheinungsbild
- [Benachrichtigungen](docs/help/benachrichtigungen/index.md) — Lokale iOS-Benachrichtigungen: Schalter, Ruhezeiten, Sammel-Modus, Berechtigung und Tap-Navigation
- [Offline lesen](docs/help/anwendung/offline.md) — Offline-Indikatoren, deaktivierte Links/Bilder, pausierter Hintergrund-Sync
- [Sprache (Deutsch / Englisch)](docs/help/anwendung/sprache.md) — UI-Sprache nach Systemsprache, Englisch als Fallback
- [Release-Management](docs/help/release-management/index.md) — Release-Pipeline: RC-Pre-Releases auf `staging`, Promotion nach `main`, stabile Releases mit Plattform-Artefakten, Backmerge
