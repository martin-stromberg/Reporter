← [Zurück zur Übersicht](index.md)

# Anwendung — Architektur

## Beteiligte Komponenten

| Komponente | Projekt | Rolle |
|------------|---------|-------|
| `Reporter` | `src/Reporter` | .NET MAUI-App mit UI und Navigation |
| `Reporter.Core` | `src/Reporter.Core` | Domänenmodelle, Schnittstellen, ViewModels, mehrsprachige RESX-Ressourcen und Anwendungs-Services |
| `Reporter.Data` | `src/Reporter.Data` | Datenbankzugriff und Repositories |
| `Reporter.Tests` | `src/Reporter.Tests` | Unit- und Integrationstests |

## Abhängigkeiten

- `Reporter` referenziert `Reporter.Core` und `Reporter.Data`.
- `Reporter.Data` referenziert `Reporter.Core`.
- `Reporter.Tests` referenziert `Reporter.Core` und `Reporter.Data`.
- `Microsoft.Extensions.DependencyInjection` wird für alle Services und ViewModels verwendet.
- `CommunityToolkit.Mvvm` bildet die Basis für die ViewModels.

## Datenfluss

```mermaid
graph TD
    A[UI / View] --> B[ViewModel]
    B --> C[IFeedSyncService]
    C --> D[HttpClient]
    C --> E[IFeedRepository]
    C --> F[IItemRepository]
    C --> G[ISyncLogRepository]
    E --> H[FeedRepository]
    F --> I[ItemRepository]
    G --> J[SyncLogRepository]
```

## Wichtige Klassen

- `MauiProgram.CreateMauiApp()` — Konfiguriert DI, Fonts und MAUI.
- `Colors.xaml` / `Styles.xaml` — Enthalten das Design-System (Farb- und Typografie-Tokens, Light/Dark-Styles). Beide haben ein `x:Class`-Code-Behind und werden in `App.xaml.cs` der `MergedDictionaries` hinzugefügt.
- Alle `.csproj` erzwingen XML-Dokumentation (`GenerateDocumentationFile` + `CS1591` als Fehler).
- `AppShell` — Definiert die Shell-Navigation mit den Tabs **Ungelesen**, **Feeds**, **Später**, **Kategorien** und **Einstellungen**.
- `Newsreader` (Editorial-Headlines) und `Inter` (UI-Texte) — Eingebundene Schriftarten.
- `AppResources` — Typisierter Zugriff auf RESX-Lokalisierung (EN/DE).
- `BaseViewModel` — Basisklasse für alle ViewModels, erbt von `ObservableObject`.
- `UnreadPage` / `UnreadViewModel` — Ansicht und ViewModel für ungelesene Artikel.
- `FeedsPage` / `FeedsViewModel` — Ansicht und ViewModel für Feeds (inkl. Refresh-Buttons).
- `LaterPage` / `LaterViewModel` — Ansicht und ViewModel für später gemerkte Artikel.
- `IFeedSyncService` / `FeedSyncService` — Service zum Abruf, Parsen und Speichern von Feed-Inhalten; ruft nach jedem Sync mit neuen Artikeln fehlerisoliert `INotificationService.NotifyNewItemsAsync` auf.
- `INotificationService` / `NotificationService` (`Reporter.Core`) — Entscheidungslogik für lokale Benachrichtigungen (Feed-/globaler Schalter, Ruhezeit via `TimeProvider`, Keyword-Filter, Einzel- vs. Sammel-Modus); Details siehe [Benachrichtigungen](../benachrichtigungen/index.md).
- `ILocalNotificationService` / `LocalNotificationService` (`src/Reporter/Services/`) — Plattformabstraktion für lokale Benachrichtigungen; iOS-Ausprägung über `UserNotifications` (`#if IOS`), No-Op auf anderen Targets. Tap-Handling und Vordergrund-Darstellung über `NotificationDelegate`/`AppDelegate` unter `Platforms/iOS`.
- `IRetentionCleanupService` / `RetentionCleanupService` — Service für das automatische Aufräumen gelesener Artikel nach `Settings.RetentionDays` inkl. Keyword-Löschregel; wird in `App.OnStart` aufgerufen, ungelesene und gemerkte Artikel bleiben erhalten. Details siehe [Aufbewahrung und automatisches Aufräumen](aufbewahrung.md).
- `IKeywordMatcher` / `KeywordMatcher` — Zentrales Keyword-Matching (`OrdinalIgnoreCase`-Teilwort auf Titel und HTML-Inhalt) für den Cleanup und später das Benachrichtigungs-Paket.
- `IAutoRefreshService` / `AutoRefreshService` — `PeriodicTimer`-basierter Hintergrund-Sync (`IFeedSyncService.SyncAllAsync`) mit `TimeProvider` und Overlap-Guard; startet in `App.OnStart`, wird bei Einstellungsänderungen neu konfiguriert.
- `IAppThemeService` / `AppThemeService` (`src/Reporter/Services/`) — Setzt `Application.UserAppTheme` anhand `Settings.Theme`; Interface in `Reporter.Core`, Implementierung im MAUI-Projekt, da Core keine MAUI-Referenz hat.
- `SettingsPage` / `SettingsViewModel` — Ausgebaute Einstellungsseite mit Sofort-Persistierung und Keyword-Verwaltung; Details siehe [Einstellungen](../einstellungen/index.md).
- `Item` — Domänenmodell für einen Artikel (`Id`, `Title`, `IsRead`, `ContentHtml`).
- `IItemRepository` / `ItemRepository` — Schnittstelle und Implementierung für den Artikel-Zugriff.
- `ISyncLogRepository` / `SyncLogRepository` — Schnittstelle und Implementierung für Synchronisations-Logs.
