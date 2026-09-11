# Tasks: Lokale iOS-Benachrichtigungen mit Ruhezeiten (Issue #27)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `Feed.NotificationsEnabled` (`bool`, `required init`) in `src/Reporter.Core/Models/Feed.cs` hinzufügen | Offen | — |
| 2 | Datenmodell | `Entities.Feed.NotificationsEnabled` (`bool`, Default `true`) in `src/Reporter.Data/Entities/Feed.cs` hinzufügen | Offen | — |
| 3 | Datenmodell | `FeedListItem.NotificationsEnabled` (`bool`, `required init`) in `src/Reporter.Core/Models/FeedListItem.cs` hinzufügen | Offen | — |
| 4 | Datenmodell | `Settings.NotificationSummaryEnabled` (`bool`, `required init`) in `src/Reporter.Core/Models/Settings.cs` hinzufügen | Offen | — |
| 5 | Datenmodell | `Entities.Settings.NotificationSummaryEnabled` (`bool`, Default `false`) in `src/Reporter.Data/Entities/Settings.cs` hinzufügen | Offen | — |
| 6 | Datenmodell | `ReporterDbContext.ConfigureFeed`: Spalten-Mapping `notifications_enabled` (`IsRequired`, `HasDefaultValue(true)`) | Offen | — |
| 7 | Datenmodell | `ReporterDbContext.ConfigureSettings`: Spalten-Mapping `notification_summary_enabled` (`IsRequired`, `HasDefaultValue(false)`) | Offen | — |
| 8 | Datenmodell | EF-Migration `AddFeedNotificationsEnabled` erzeugen (`AddColumn<bool>` auf `feeds`, `defaultValue: true`) | Offen | — |
| 9 | Datenmodell | EF-Migration `AddSettingsNotificationSummary` erzeugen (`AddColumn<bool>` auf `settings`, `defaultValue: false`, inkl. `UpdateData` des Singletons) | Offen | — |
| 10 | Datenmodell | `new Settings`-Fallback in `ArticleDetailViewModel.LoadAsync` (`src/Reporter/ViewModels/ArticleDetailViewModel.cs`) um `NotificationSummaryEnabled` ergänzen (`required`) | Offen | — |
| 11 | Logik | `FeedRepository`: `MapToModel`, `MapToEntity`, `UpdateAsync` und `GetAllWithDetailsAsync`-Projektion um `NotificationsEnabled` erweitern | Offen | — |
| 12 | Logik | `SettingsRepository`: `MapToModel` und `SaveAsync` um `NotificationSummaryEnabled` erweitern | Offen | — |
| 13 | Logik | Interface `ILocalNotificationService` in `src/Reporter.Core/Interfaces/` anlegen (`RequestAuthorizationAsync`, `ShowAsync`) | Offen | — |
| 14 | Logik | Interface `INotificationService` in `src/Reporter.Core/Interfaces/` anlegen (`NotifyNewItemsAsync`) | Offen | — |
| 15 | Logik | `NotificationService` in `src/Reporter.Core/Services/` implementieren (globaler Schalter, Pro-Feed-Flag, Ruhezeit inkl. Wrap-around via `TimeProvider`, Keyword-Filter via `IKeywordMatcher`, Modus-Verzweigung Einzel-/Sammel-Versand anhand `Settings.NotificationSummaryEnabled`, Versand via `ILocalNotificationService`) | Offen | — |
| 16 | Plattform | `LocalNotificationService` in `src/Reporter/Services/` implementieren (`#if IOS`: `UNUserNotificationCenter`-Berechtigung + `UNNotificationRequest` mit modusabhängigem Identifier, `UserInfo` für späteres Deep-Linking; sonst No-Op) | Offen | — |
| 17 | Plattform | `NotificationDelegate` unter `src/Reporter/Platforms/iOS/` anlegen (`WillPresentNotification` → `Banner \| List \| Sound`) | Offen | — |
| 18 | Plattform | `AppDelegate.FinishedLaunching` überschreiben und `UNUserNotificationCenter.Current.Delegate` setzen | Offen | — |
| 19 | Logik | `FeedSyncService`: `INotificationService`-Konstruktorabhängigkeit, `newItemEntities`-Liste in `RunSyncAsync`, Aufruf nach `UpdateLogAsync` in `try/catch` | Offen | — |
| 20 | Logik | `FeedSyncService.UpdateFeedHealthAsync`: `NotificationsEnabled = feed.NotificationsEnabled` am rekonstruierten `Feed` durchreichen | Offen | — |
| 21 | UI | `FeedsViewModel`: Property `FeedNotificationsEnabled` (Startwert `true`), Belegung in `SaveAsync` (Add/Update), Vorbefüllung in `EditAsync`, Reset nach Save | Offen | — |
| 22 | UI | `FeedsPage.xaml`: `Switch`-Zeile in der Bearbeitungskarte nach `SettingsPage.xaml`-Muster (≥ 44 pt Touch-Target, `SemanticProperties.Description`) | Offen | — |
| 23 | UI | `SettingsViewModel`: Property `NotificationSummaryEnabled` (Startwert `false`, Setter → `PersistOnChange`), Vorbefüllung in `LoadAsync`, Bindung in `PersistAsync` | Offen | — |
| 24 | UI | `SettingsPage.xaml`: `Switch`-Zeile für den Benachrichtigungsmodus innerhalb des `NotificationsEnabled`-gesteuerten `Border` in der Karte „Benachrichtigungen & Ruhezeiten" (Muster Ruhezeiten-Zeile, ≥ 44 pt, `SemanticProperties.Description`) | Offen | — |
| 25 | UI | `AppResources.resx`/`.de.resx`: Schlüssel `FeedNotificationsLabel`, `FeedNotificationsHint`, `SettingsNotificationSummaryLabel`, `SettingsNotificationSummaryHint`, `NotificationSummaryFormat` ergänzen, `AppResources.Designer.cs` regenerieren | Offen | — |
| 26 | Konfiguration | `MauiProgram.CreateMauiApp`: `AddSingleton<INotificationService, NotificationService>()` und `AddSingleton<ILocalNotificationService, LocalNotificationService>()` | Offen | — |
| 27 | Konfiguration | `App.OnStart`: `try/catch`-Block für `ILocalNotificationService.RequestAuthorizationAsync` bei aktivem `Settings.NotificationsEnabled` | Offen | — |
| 28 | Tests | `FakeLocalNotificationService` in `src/Reporter.Tests/` anlegen (zeichnet `ShowAsync`-Aufrufe auf) | Offen | — |
| 29 | Tests | `FakeNotificationService` in `src/Reporter.Tests/` anlegen (zeichnet `NotifyNewItemsAsync` auf, optional Exception) | Offen | — |
| 30 | Tests | `NotificationServiceTests` anlegen: Feed-/Global-Schalter, Ruhezeit (Wrap-around, `Start == End`, einseitig `null`), Keyword-Filter, Einzelmodus mit `Item.Id`-Identifier, Sammelmodus (ein `ShowAsync`, stabiler Identifier aus `Feed.Id` + Item-Hash, gefilterte Items ausgeschlossen) | Offen | — |
| 31 | Tests | `FeedSyncServiceTests` erweitern: `SyncFeedAsync_NewItems_NotifiesWithFeedAndItems`, `SyncFeedAsync_SummaryMode_SendsSingleSummaryNotification`, `SyncFeedAsync_NoNewItems_DoesNotNotify`, `SyncFeedAsync_NotificationThrows_SyncStillSucceeds` | Offen | — |
| 32 | Tests | `FeedRepositoryTests` erweitern: `NotificationsEnabled`-Persistenz (Add/Update) und `GetAllWithDetailsAsync`-Projektion | Offen | — |
| 33 | Tests | `FeedsViewModelTests` erweitern: `EditCommand`-Vorbefüllung, `SaveCommand`-Persistenz (Add/Update), Formular-Reset | Offen | — |
| 34 | Tests | `SettingsRepositoryTests` erweitern: `SaveAsync_PersistsNotificationSummaryEnabled` (Roundtrip beider Werte) | Offen | — |
| 35 | Tests | `SettingsViewModelTests_Persist`/`_Load` erweitern: `NotificationSummaryEnabled_Change_Persists`, `Load_PopulatesNotificationSummaryEnabled` | Offen | — |
| 36 | Tests | `SettingsViewModelTests_E2E` erweitern: `E2E_NotificationSummary_PersistRoundtrip` | Offen | — |
| 37 | Tests | Betroffene Initializer anpassen: `new Feed { … }` um `NotificationsEnabled` (`FeedRepositoryTests`, `FeedSyncServiceTests.SeedFeedAsync`, `FeedsViewModelTests`); `new Settings { … }` um `NotificationSummaryEnabled` (`SettingsViewModelTests_Persist`, `_Load`, `SettingsRepositoryTests`, `RetentionCleanupServiceTests`, `AutoRefreshServiceTests`); `CreateService`/`CreateFailingService` um `FakeNotificationService` erweitern | Offen | — |
| 38 | Tests | `dotnet test src/Reporter.Tests` grün (147 bestehende + neue Tests; die zwei bekannten flaky `SettingsViewModelTests_Persist`-Fälle beachten) | Offen | — |
| 39 | Verifikation | Manuelle UI-Verifikation `FeedsPage` und `SettingsPage` im 390 × 844 Handysize-Fenster (beide Switches bedienbar, ≥ 44 pt, Modus-Schalter bei globalem Schalter aus abgedunkelt, `AppThemeBinding`/Dark Mode, Vergleich mit `design-draft/.../feeds_health_status/screen.png`), Edit→Toggle→Save→Reload- und Modus-Persist-Fluss; Doku in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md` | Offen | — |
| 40 | Verifikation | iOS-Verifikation via `scripts/iOS-Deployment.ps1` (Aktion `simulator`, nur macOS): Berechtigungsdialog, Vordergrund-Banner in beiden Modi, Ruhezeit-Unterdrückung — ggf. als Folgeschritt dokumentieren | Offen | — |
| 41 | Verifikation | `scripts/Run-StaticChecks.ps1` ausführen — Exit-Code 0 ohne Befunde | Offen | — |
