# Logik — Bestandsaufnahme

## `RetentionCleanupService`

Datei: `src/Reporter.Core/Services/RetentionCleanupService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `RetentionCleanupService(ISettingsRepository, IItemRepository)` | `public` ctor | Injiziert die beiden Repositories (Zeilen 18–22). Kein `IKeywordRepository`-Zugriff. |
| `CleanupAsync(CancellationToken)` | `public` | Lädt `Settings` via `GetAsync`; bei `RetentionDays <= 0` Rückgabe `0`; sonst `cutoff = UtcNow - RetentionDays` und Aufruf von `IItemRepository.DeleteExpiredAsync(cutoff, ct)` (Zeilen 25–35). |

Abonnierte Events: keine. Publizierte Events: keine.

Wird aufgerufen von `App.OnStart` (`src/Reporter/App.xaml.cs` Zeilen 42–43), Fehler isoliert via `try/catch` + `Debug.WriteLine` (Zeilen 45–49). Registriert als Singleton in `MauiProgram.CreateMauiApp` (Zeile 51). Implementiert `IRetentionCleanupService`. Eine Keyword-Auswertung existiert nicht; die Invarianten liegen vollständig in `ItemRepository.DeleteExpiredAsync`.

## `ItemRepository` (relevante Teile)

Datei: `src/Reporter.Data/Repositories/ItemRepository.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `DeleteExpiredAsync(DateTime cutoff, CancellationToken)` | `public` | `ExecuteDeleteAsync` über `Items` mit Bedingung `i.IsRead && !i.IsSavedForLater && (i.ReadAt ?? i.PublishedAt) < cutoff` (Zeilen 305–311). Artikel ohne beide Zeitstempel bleiben erhalten (NULL-Vergleich). |
| `MarkAsReadAsync(Guid)` | `public` | Setzt `IsRead = true`, `ReadAt = UtcNow` (Zeilen 212–224). |
| `ToggleSavedForLaterAsync(Guid)` | `public` | Invertiert `IsSavedForLater` (Zeilen 198–209). |
| `DeleteAsync(Guid)` | `public` | Expliziter Einzel-Delete **ohne** Invarianten; laut Interface-Kommentar nicht für automatisches Cleanup gedacht (Zeilen 78–89). |
| `MapToModel` / `MapToEntity` | `private static` | Mapping Entity ↔ Core-Modell (Zeilen 344–377). |

Weitere Methoden (`GetAllAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `GetUnreadByDateAsync` ×2, `GetUnreadCountAsync`, `MarkAllAsReadAsync`, `GetByFeedAsync`, `GetByCategoryAsync`, `GetSavedForLaterAsync`, `GetByGuidOrHashAsync`) sind für die Anforderung nicht direkt relevant. Alle Methoden nutzen `IDbContextFactory<ReporterDbContext>` mit `await using`-Kontexten.

## `SettingsRepository`

Datei: `src/Reporter.Data/Repositories/SettingsRepository.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAsync(CancellationToken)` | `public` | Liest Singleton per `DefaultId`; legt Default-Entity an, falls keiner existiert (Zeilen 25–40). |
| `SaveAsync(Settings)` | `public` | Schreibt immer auf den Singleton-Datensatz (`FindAsync(DefaultId)`, ignoriert `settings.Id`); kopiert alle sechs Fachfelder (Zeilen 43–61). |
| `MapToModel(SettingsEntity)` | `private static` | Mapping in das immutable Core-Modell (Zeilen 63–75). |

## `KeywordRepository`

Datei: `src/Reporter.Data/Repositories/KeywordRepository.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAllAsync()` | `public` | Alle Keywords, sortiert nach `KeywordText` (Zeilen 25–33). |
| `GetByIdAsync(Guid)` | `public` | Einzelnes Keyword oder `null` (Zeilen 36–43). |
| `AddAsync(Keyword)` | `public` | Direkter Insert; Dubletten würden am Unique-Index auf `keyword_text` scheitern (Zeilen 46–51). |
| `UpdateAsync(Keyword)` | `public` | Aktualisiert `KeywordText`; No-Op bei fehlender ID (Zeilen 54–65). |
| `DeleteAsync(Guid)` | `public` | Löscht per ID; No-Op bei fehlender ID (Zeilen 68–79). |
| `MapToModel` / `MapToEntity` | `private static` | Mapping (Zeilen 81–97). |

## `FeedSyncService`

Datei: `src/Reporter.Core/Services/FeedSyncService.cs`

`SyncAllAsync(CancellationToken)` (ab Zeile 73) synchronisiert alle Feeds und liefert ein `SyncResult` (`record` mit `Status`, `NewItems`, `Message`, `src/Reporter.Core/Services/SyncResult.cs`). `SyncFeedAsync(Guid, CancellationToken)` ab Zeile 40. Aktuell nur aus ViewModels (`RefreshCommand`, `RefreshAllCommand`) aufgerufen — kein zeitgesteuerter Aufruf vorhanden.

## `SettingsViewModel`

Datei: `src/Reporter.Core\ViewModels\SettingsViewModel.cs`

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `SettingsViewModel(ISettingsRepository)` | `public` ctor | Injiziert Repository, erzeugt `LoadCommand` (Zeilen 21–25). Kein `IKeywordRepository`. |
| `LoadCommand` | `public AsyncRelayCommand` | Lädt Settings via `GetAsync` (Zeilen 30, 50–53). |
| `Title` | `public string` | Seitentitel, initial `AppResources.PageTitleSettings` (Zeilen 35–39). |
| `Settings` | `public Settings?` | Der geladene Datensatz (Zeilen 44–48). |
| `LoadAsync()` | `private` | `Settings = await _settingsRepository.GetAsync()` (Zeilen 50–53). |

Basisklasse `BaseViewModel : ObservableObject` (`src/Reporter.Core/ViewModels/BaseViewModel.cs`, CommunityToolkit.Mvvm). Keine Add/Remove-Keyword-Commands, keine Sofort-Persistierung, keine Validierung.

## `ArticleDetailViewModel` (relevante Teile)

Datei: `src/Reporter/ViewModels/ArticleDetailViewModel.cs` (Namespace `Reporter.Core.ViewModels`, als `AddTransient` registriert)

- `LoadAsync(Guid itemId)` (Zeilen 226–278): lädt `Settings` via `ISettingsRepository.GetAsync`; bei Fehler Fallback-Objekt mit `AutoMarkReadMode = "on_open"` (Zeilen 238–246). `AutoMarkReadDelaySeconds` wird gelesen (Standard-Fallback `DefaultAutoMarkDelaySeconds = 5`, Zeile 19); **`AutoMarkReadMode` wird nicht ausgewertet**.
- `IsAutoMarkRead` (Zeilen 143–153): lokaler Toggle, Standard `true`; bei `true` und ungelesenem Artikel startet `MarkReadDelayedAsync` (Zeilen 269–272, 399–441) einen `Task.Delay`-Timer mit `CancellationTokenSource` (`_autoMarkCts`, `_autoMarkLock`).
- `CancelAutoMarkRead()` (Zeilen 283–290), `ToggleAutoMarkRead`/`ToggleAutoMarkReadCommand` (Zeilen 204, 394–397).
- Commands: `GoBackCommand`, `ToggleSavedForLaterCommand`, `ToggleMarkReadCommand`, `ToggleAutoMarkReadCommand`, `OpenInBrowserCommand`, `ShareCommand`, `ToggleFontSizeCommand`.

## `App` (Start-Logik)

Datei: `src/Reporter/App.xaml.cs`

- `OnStart` (Zeilen 32–50): `Database.MigrateAsync()`, dann `IRetentionCleanupService.CleanupAsync()` mit Fehlerisolierung. Kein Theme-Setup (`UserAppTheme` wird nirgends gesetzt), kein Auto-Refresh-Timer.
- `CreateWindow` (Zeilen 57–70): Windows-Fenster fix 390 × 844 pt (Handysize-Vorgabe).
- Konstruktor merged `Colors` und `Styles` ResourceDictionaries (Zeilen 25–26).

## DI-Registrierung (`MauiProgram.CreateMauiApp`)

Datei: `src/Reporter/MauiProgram.cs` Zeilen 41–64

- `AddDbContextFactory<ReporterDbContext>` mit SQLite `reporter.db` in `FileSystem.AppDataDirectory`.
- Singletons: alle Repositories (`IFeedRepository`, `ICategoryRepository`, `IItemRepository`, `IKeywordRepository`, `ISettingsRepository`, `ISyncLogRepository`), `HttpClient` (30 s Timeout), `IFeedSyncService`, `IRetentionCleanupService`, ViewModels `UnreadViewModel`, `FeedsViewModel`, `LaterViewModel`, `CategoriesViewModel`, `SettingsViewModel`.
- Transients: `ArticleDetailViewModel`, alle Pages, `AppShell`.
- DB-Pfad-Erzeugung in Zeilen 38–39.

## String-Konstanten statt Enums

`FeedHealth` (`src/Reporter.Core/Services/FeedHealth.cs`): `Ok`/`Warning`/`Error` als `const string`, dazu `Changed(current, next)`. `AutoMarkReadMode` nutzt denselben Ansatz (`"on_scroll"`; `ArticleDetailViewModel`-Fallback verwendet `"on_open"`).
