<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik

## `SettingsViewModel`
Datei: `src/Reporter.Core/ViewModels/SettingsViewModel.cs` (`partial class : ObservableObject`)

Konstruktor-Abhängigkeiten: `ISettingsRepository`, `IKeywordRepository`, `IAutoRefreshService`, `IAppThemeService`, optional `TimeProvider?`, optional `ILocalNotificationService?`. **Kein `SendDebugReportCommand`, keine `DebugCollectionEnabled`-/`DebugEmailSupported`-Property vorhanden.**

Commands:

| Command | Typ | Aktion |
|---------|-----|--------|
| `LoadCommand` | `AsyncRelayCommand` | `LoadAsync` |
| `AddKeywordCommand` | `AsyncRelayCommand` | `AddKeywordAsync` |
| `RemoveKeywordCommand` | `AsyncRelayCommand<Keyword>` | `RemoveKeywordAsync` |
| `SaveRetentionCommand` | `RelayCommand` | `SaveRetention` |
| `RequestNotificationPermissionCommand` | `AsyncRelayCommand` | `RequestNotificationAuthorizationAsync` |

Bindbare Properties (Auswahl, für die Anforderung relevant): `Title`, `Settings`, `RetentionDays`/`RetentionDaysText`, `AutoRefreshEnabled`, `SelectedRefreshInterval`, `RefreshOnStartupEnabled`, `SelectedSortOrder`, `AutoMarkReadEnabled`, `SelectedAutoMarkReadDelay`, `NotificationsEnabled`, `NotificationsSupported` (get-only, `_localNotificationService?.IsSupported == true`), `NotificationControlsEnabled`, `NotificationPermissionDenied`, `NotificationPermissionNotDetermined`, `NotificationSummaryEnabled`, `QuietHoursEnabled`/`QuietHoursStart`/`QuietHoursEnd`, `SelectedTheme`, `SelectedLanguage`, `LanguageRestartHintVisible`, `HasError`, `ErrorMessage`, `Keywords`, Optionslisten (`RefreshIntervalOptions`, `AutoMarkReadDelayOptions`, `ThemeOptions`, `LanguageOptions`, `SortOrderOptions`).

Private Methoden:

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadAsync` | `private` | Lädt Settings + Keywords in die Properties; `_isLoading`-Guard unterdrückt Persist während des Ladens; ruft `RefreshNotificationPermissionAsync` |
| `PersistOnChange` | `private` | Fire-and-forget `_ = PersistAsync()`, übersprungen während `_isLoading` — wird von nahezu jedem Property-Setter aufgerufen |
| `PersistAsync` | `private` | Baut neuen `Settings`-Record aus den VM-Properties und ruft `_settingsRepository.SaveAsync`; serialisiert über `_persistLock` (`SemaphoreSlim`); triggert `ApplyTheme`/`ApplySettingsAsync` bei Änderung |
| `ScheduleRetentionPersist`/`PersistRetentionDebouncedAsync`/`CancelRetentionDebounce`/`SaveRetention` | `private` | Debounced-Persist (500 ms über `TimeProvider`) für den Retention-Slider |
| `RefreshNotificationPermissionAsync` | `private` | Fragt `GetAuthorizationStatusAsync` ab und mappt auf `NotificationPermissionDenied`/`NotDetermined` |
| `RequestNotificationAuthorizationAsync` | `private` | Fordert Plattform-Autorisierung an; löst bei `Denied` das Event `NotificationAuthorizationDenied` aus |
| `ApplyAuthorizationStatus` | `private` | Setzt die Permission-Flags |
| `AddKeywordAsync`/`RemoveKeywordAsync` | `private` | Keyword-CRUD mit Validierung (`ErrorKeyword*` aus `AppResources`) |
| `FormatRetentionDays` | `private static` | Lokalisierte Anzeige der Aufbewahrungsdauer |

Publizierte Events: `NotificationAuthorizationDenied` (`Func<Task>?` — abonniert von `SettingsPage.OnAppearing`, Handler `OnNotificationAuthorizationDenied`).
Fehlerbehandlung: alle async-Pfade fangen `Exception` → `Debug.WriteLine`.

## `NotificationService`
Datei: `src/Reporter.Core/Services/NotificationService.cs` (`: INotificationService`)

Referenz für das Orchestrierungsmuster „Regelwerk in Core, Plattformwirkung über Gateway".

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `NotifyNewItemsAsync` | `public` | Wertet Feed-/Global-Schalter, Ruhezeiten und Keyword-Filter aus; ruft `ILocalNotificationService.ShowAsync` (pro Item oder Summary via `AppResources.NotificationSummaryFormat`) |
| `IsQuietHoursActive` | `private` | Ruhezeiten-Auswertung über `TimeProvider` |
| `BuildSummaryIdentifier`/`BuildItemUserInfo`/`Truncate` | `private static` | Stabile Notification-Identifier (SHA-256), Payload, Kürzung |

Abhängigkeiten: `ISettingsRepository`, `IKeywordFilter`, `ILocalNotificationService`, `TimeProvider?`.

## `FeedSyncService`
Datei: `src/Reporter.Core/Services/FeedSyncService.cs` (`: IFeedSyncService`)

Schreibt die `sync_logs`-Einträge, die der Debug-Report auswertet.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SyncFeedAsync` | `public` | Offline → `SyncResult(FeedHealth.Error, …, OfflineHint)`; legt `SyncLog` mit `Status = FeedHealth.Ok` an (`AddAsync`), fängt Nicht-Cancel-Exceptions → `UpdateFeedHealthAsync` + `UpdateLogAsync` mit `FeedHealth.Error` |
| `SyncAllAsync` | `public` | Iteriert alle Feeds, aggregiert Status (`Error` > `Warning` > `Ok`) |
| `RunSyncAsync` | `private` | Eigentlicher Sync; ruft `INotificationService.NotifyNewItemsAsync` für neue Items |
| `UpdateLogAsync` | `private` | Persistiert `FinishedAt`, `Status`, `Message` via `ISyncLogRepository.UpdateAsync` |
| `UpdateFeedHealthAsync` | `private` | Setzt `Feed.HealthStatus`/`HealthLastChange` |

Abhängigkeiten: u. a. `IFeedRepository`, `IItemRepository`, `ISyncLogRepository`, `INetworkStatusService`, `INotificationService`, `IKeywordFilter`, `IFeedIconService`.

## `RetentionCleanupService`
Datei: `src/Reporter.Core/Services/RetentionCleanupService.cs` (`: IRetentionCleanupService`)

`CleanupAsync` löscht ausschließlich abgelaufene `items` (`IItemRepository.DeleteExpiredAsync`/`GetExpiredKeywordCandidatesAsync`/`DeleteRangeAsync`). **`sync_logs` werden nie bereinigt — das Protokoll wächst unbegrenzt.**

## `SettingsRepository`
Datei: `src/Reporter.Data/Repositories/SettingsRepository.cs` (`: ISettingsRepository`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAsync` | `public` | Liest Singleton-`Settings` (`AsNoTracking`); legt Default-Entity an, falls keiner existiert |
| `SaveAsync` | `public` | Feld-für-Feld-Update der Entity (13 Felder); `DebugCollectionEnabled` müsste hier ergänzt werden |
| `MapToModel` | `private static` | Entity → Core-Modell (13 Felder) |

## `SyncLogRepository`
Datei: `src/Reporter.Data/Repositories/SyncLogRepository.cs` (`: ISyncLogRepository`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAllAsync` | `public` | Alle Einträge `OrderByDescending(s => s.StartedAt)` — unbegrenzt |
| `GetByIdAsync` | `public` | Einzeleintrag oder `null` |
| `AddAsync`/`UpdateAsync`/`DeleteAsync` | `public` | CRUD; `UpdateAsync` setzt FeedId/Zeiten/Status/Message |
| `MapToModel`/`MapToEntity` | `private static` | Mapping ohne Navigation |

## `LocalNotificationService` (MAUI-Implementierung)
Datei: `src/Reporter/Services/LocalNotificationService.cs` (`: ILocalNotificationService`)

Gateway-Referenzimplementierung: `IsSupported` ist per `#if IOS` `true`, sonst `false`; alle Methoden sind auf Nicht-iOS No-ops (`Task.FromResult(false)`/`Unsupported`/`CompletedTask`). iOS-Pfad nutzt `UserNotifications` (`UNUserNotificationCenter`, `EnsureAuthorizedAsync`, `MapStatus`, `BuildUserInfo`, `IsAuthorized`).

## `NetworkStatusService` (MAUI-Implementierung)
Datei: `src/Reporter/Services/NetworkStatusService.cs` (`: INetworkStatusService`)

Kapselt `Microsoft.Maui.Networking.Connectivity`: `IsOnline` = `NetworkAccess.Internet`; `ConnectivityChanged` wird per `MainThread.BeginInvokeOnMainThread` auf den UI-Thread gemarshallt.

## `AppThemeService` (MAUI-Implementierung)
Datei: `src/Reporter/Services/AppThemeService.cs` (`: IAppThemeService`)

Setzt `Application.Current.UserAppTheme` aus `SettingsValues.Theme*`; Null-sicher (`Application.Current is null` → Return).

## `MauiProgram`
Datei: `src/Reporter/MauiProgram.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CreateMauiApp` | `public static` | Baut `MauiApp`; `builder.Services`-Block (Zeilen 46–78) registriert alle Repositories, Services, Gateways, ViewModels und Pages als `AddSingleton`/`AddTransient`; neue Gateways (`IEmailService` o. ä.) wären hier zu ergänzen |
| `ApplyPersistedLanguage` | `private static` | Führt `context.Database.Migrate()` synchron beim Start aus und wendet `AppCulture.Apply(settings.Language)` an — Migrationen werden also automatisch angewendet |

## `SettingsPage` (Code-Behind)
Datei: `src/Reporter/Views/SettingsPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnAppearing` | `protected override` | Abonniert `NotificationAuthorizationDenied`, führt `LoadCommand` aus |
| `OnDisappearing` | `protected override` | Deabonniert das Event |
| `OnNotificationAuthorizationDenied` | `private` | `DisplayAlertAsync` mit `NotificationDenied*`-Texten; „Einstellungen öffnen" → `AppInfo.Current.ShowSettingsUI()` — Muster für einen Fehlerpfad beim E-Mail-Versand |
| `OnOpenNotificationSettingsClicked` | `private` | `Clicked`-Handler → `AppInfo.Current.ShowSettingsUI()` |

`AppInfo.Current` ist die einzige MAUI-Essentials-Nutzung im bestehenden Code (`Email`/`DeviceInfo` werden nirgends verwendet).
