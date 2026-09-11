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
- `SettingsPage` / `SettingsViewModel` — Ansicht und ViewModel für Einstellungen.
- `IFeedSyncService` / `FeedSyncService` — Service zum Abruf, Parsen und Speichern von Feed-Inhalten.
- `IRetentionCleanupService` / `RetentionCleanupService` — Service für das automatische Aufräumen gelesener Artikel nach `Settings.RetentionDays`; wird in `App.OnStart` aufgerufen, gemerkte Artikel bleiben erhalten. Details siehe [Aufbewahrung und automatisches Aufräumen](aufbewahrung.md).
- `Item` — Domänenmodell für einen Artikel (`Id`, `Title`, `IsRead`, `ContentHtml`).
- `IItemRepository` / `ItemRepository` — Schnittstelle und Implementierung für den Artikel-Zugriff.
- `ISyncLogRepository` / `SyncLogRepository` — Schnittstelle und Implementierung für Synchronisations-Logs.
