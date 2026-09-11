# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

## Umgesetzte Planelemente

### Neue Klassen und Interfaces

- [x] `INotificationService` (Interface) — angelegt in `src/Reporter.Core/Interfaces/INotificationService.cs`, Signatur `NotifyNewItemsAsync(Feed, IReadOnlyList<Item>, CancellationToken)` plangemäß
- [x] `ILocalNotificationService` (Interface) — angelegt in `src/Reporter.Core/Interfaces/ILocalNotificationService.cs`, `RequestAuthorizationAsync` → `Task<bool>` und `ShowAsync(title, body, identifier, ct)` → `Task` plangemäß
- [x] `NotificationService` (Klasse) — angelegt in `src/Reporter.Core/Services/NotificationService.cs`; vollständige Entscheidungskette: Pro-Feed-Flag → globaler Schalter → Ruhezeit (Wrap-around `Start > End`, leeres Intervall `Start == End`, einseitig `null`, via `TimeProvider`) → Keyword-Filter (`IKeywordMatcher.MatchesAny` auf `Title`/`ContentHtml`) → Modus-Verzweigung; Einzelmodus mit `Item.Id` als Identifier, Sammelmodus mit `AppResources.NotificationSummaryFormat`-Body und Identifier `{feedId}-{sha256(sortierte ItemIds)}`; gefilterte Items fließen weder in Body noch in den Hash ein
- [x] `NotificationDelegate` (Klasse) — angelegt in `src/Reporter/Platforms/iOS/NotificationDelegate.cs`, `WillPresentNotification` → `Banner | List | Sound`
- [x] `FakeLocalNotificationService` (Test-Hilfsklasse) — angelegt in `src/Reporter.Tests/FakeLocalNotificationService.cs`, zeichnet `ShowAsync`-Aufrufe auf, `AuthorizationResult` konfigurierbar
- [x] `FakeNotificationService` (Test-Hilfsklasse) — angelegt in `src/Reporter.Tests/FakeNotificationService.cs`, zeichnet Aufrufe auf, optional `Exception`

### Felder in bestehenden Objekten

- [x] `Feed.NotificationsEnabled` (`bool`, `required init`) — `src/Reporter.Core/Models/Feed.cs:46`
- [x] `Entities.Feed.NotificationsEnabled` (`bool`, Default `true`) — `src/Reporter.Data/Entities/Feed.cs:46`
- [x] `FeedListItem.NotificationsEnabled` (`bool`, `required init`) — `src/Reporter.Core/Models/FeedListItem.cs:56`
- [x] `Settings.NotificationSummaryEnabled` (`bool`, `required init`) — `src/Reporter.Core/Models/Settings.cs:68`
- [x] `Entities.Settings.NotificationSummaryEnabled` (`bool`, Default `false`) — `src/Reporter.Data/Entities/Settings.cs:70`
- [x] `FeedsViewModel.FeedNotificationsEnabled` (`bool`, Startwert `true`) — `src/Reporter.Core/ViewModels/FeedsViewModel.cs:99`
- [x] `SettingsViewModel.NotificationSummaryEnabled` (`bool`, Startwert `false`, Setter → `PersistOnChange`) — `src/Reporter.Core/ViewModels/SettingsViewModel.cs:284`

### Geänderte Methoden

- [x] `ReporterDbContext.ConfigureFeed` — Mapping `notifications_enabled` mit `IsRequired().HasDefaultValue(true)` (`src/Reporter.Data/ReporterDbContext.cs:87`)
- [x] `ReporterDbContext.ConfigureSettings` — Mapping `notification_summary_enabled` mit `IsRequired().HasDefaultValue(false)` (`ReporterDbContext.cs:135`); `HasData`-Seed übernimmt Entity-Default
- [x] `FeedRepository.MapToModel` / `MapToEntity` / `UpdateAsync` / `GetAllWithDetailsAsync` — `NotificationsEnabled` überall übertragen bzw. projiziert (`src/Reporter.Data/Repositories/FeedRepository.cs:69,106,135,150`)
- [x] `SettingsRepository.MapToModel` / `SaveAsync` — `NotificationSummaryEnabled` übertragen (`src/Reporter.Data/Repositories/SettingsRepository.cs:62,81`)
- [x] `FeedSyncService` — neue Konstruktor-Abhängigkeit `INotificationService` (5. Parameter, `FeedSyncService.cs:30-42`); `RunSyncAsync` sammelt `newItemEntities` und ruft `NotifyNewItemsAsync` nach `UpdateLogAsync` in eigenem `try/catch` (`:128,161,172-183`); `UpdateFeedHealthAsync` reicht `NotificationsEnabled` durch (`:221`)
- [x] `FeedsViewModel.SaveAsync` — `NotificationsEnabled = FeedNotificationsEnabled` in Add- und Update-Pfad, Reset auf `true` (`FeedsViewModel.cs:241,255,261`); `EditAsync` — Vorbefüllung (`:274`)
- [x] `SettingsViewModel.LoadAsync` — Vorbefüllung aus `settings.NotificationSummaryEnabled` (`SettingsViewModel.cs:408`); `PersistAsync` — Bindung ans Property (`:525`)
- [x] `ArticleDetailViewModel.LoadAsync` — `new Settings`-Fallback um `NotificationSummaryEnabled = false` ergänzt (`src/Reporter/ViewModels/ArticleDetailViewModel.cs:257`)
- [x] `App.OnStart` — eigener `try/catch`-Block: bei `settings.NotificationsEnabled` Aufruf von `ILocalNotificationService.RequestAuthorizationAsync` (`src/Reporter/App.xaml.cs:64-78`)
- [x] `AppDelegate.FinishedLaunching` — überschrieben, setzt `UNUserNotificationCenter.Current.Delegate = new NotificationDelegate()` (`src/Reporter/Platforms/iOS/AppDelegate.cs:20-24`)
- [x] `MauiProgram.CreateMauiApp` — `AddSingleton<INotificationService, NotificationService>()` und `AddSingleton<ILocalNotificationService, LocalNotificationService>()` vor den ViewModels (`src/Reporter/MauiProgram.cs:56-57`)

### UI und Ressourcen

- [x] `FeedsPage.xaml` — Switch-Zeile (`*,Auto`-Grid, Label + `MetaStyle`-Hint, `MinimumWidth/HeightRequest="44"`, `SemanticProperties.Description`) unterhalb des `Picker` in der Bearbeitungskarte (`src/Reporter/Views/FeedsPage.xaml:30-44`)
- [x] `SettingsPage.xaml` — Switch-Zeile „Sammel-Benachrichtigung" innerhalb des `IsEnabled="{Binding NotificationsEnabled}"`-`Border`, vor dem Ruhezeiten-`Grid` (`src/Reporter/Views/SettingsPage.xaml:266-280`)
- [x] `AppResources.resx` / `AppResources.de.resx` — alle fünf Schlüssel (`FeedNotificationsLabel`, `FeedNotificationsHint`, `SettingsNotificationSummaryLabel`, `SettingsNotificationSummaryHint`, `NotificationSummaryFormat` mit Platzhaltern `{0}`/`{1}`) vorhanden; `AppResources.Designer.cs` regeneriert (Properties ab Zeile 975)

### Datenbankmigrationen

- [x] `AddFeedNotificationsEnabled` — `src/Reporter.Data/Migrations/20260911181811_AddFeedNotificationsEnabled.cs`, `AddColumn<bool>` `INTEGER NOT NULL DEFAULT true` auf `feeds`
- [x] `AddSettingsNotificationSummary` — `src/Reporter.Data/Migrations/20260911181850_AddSettingsNotificationSummary.cs`, `AddColumn<bool>` `DEFAULT false` plus `UpdateData` des Settings-Singletons; ModelSnapshot aktualisiert

### Tests und Verifikation

- [x] `NotificationServiceTests` — angelegt mit 11 Fällen (Feed-/Global-Schalter, Ruhezeit-Wrap-around als Theory 23:00/06:00 vs. 12:00/21:00, `Start == End`, einseitig `null`, Keyword auf Titel/`ContentHtml`, Einzel-Identifier, Sammelmodus, stabile Summary-ID, gefilterte Items in Summary)
- [x] `FeedSyncServiceTests` — `SyncFeedAsync_NewItems_NotifiesWithFeedAndItems`, `SyncFeedAsync_SummaryMode_SendsSingleSummaryNotification`, `SyncFeedAsync_NoNewItems_DoesNotNotify`, `SyncFeedAsync_FeedDisabled_NoNotifications` (E2E-Pflichtszenario), `SyncFeedAsync_NotificationThrows_SyncStillSucceeds`; `CreateService`/`CreateFailingService`/`SeedFeedAsync` angepasst
- [x] `FeedRepositoryTests` — `UpdateAsync_PersistsChanges` assertiert `false`-Roundtrip, `GetAllWithDetailsAsync_ProjectsNotificationsEnabled` neu; alle Initializer ergänzt
- [x] `FeedsViewModelTests` — Edit-Vorbefüllung, Persistenz Add/Update, Formular-Reset (siehe Hinweise zu abweichenden Testnamen)
- [x] `SettingsRepositoryTests.SaveAsync_PersistsNotificationSummaryEnabled`, `SettingsViewModelTests_Persist.NotificationSummaryEnabled_Change_Persists`, `SettingsViewModelTests_Load.Load_PopulatesNotificationSummaryEnabled`, `SettingsViewModelTests_E2E.E2E_NotificationSummary_PersistRoundtrip` — alle vorhanden
- [x] Betroffene `new Settings`-Initializer in `SettingsViewModelTests_Persist` (4×), `_Load` (2×), `SettingsRepositoryTests` (3×), `RetentionCleanupServiceTests` (1×), `AutoRefreshServiceTests` (2×) ergänzt
- [x] Suite-Lauf dokumentiert: 176 Tests grün (`test-results.md`); `Run-StaticChecks.ps1` Exit-Code 0; manuelle UI-Verifikation 390 × 844 pt dokumentiert in `test-results.md` + `docs/help/anwendung/mobile-ui-design.md` inkl. Screenshots

## Offene Aufgaben

- [ ] `LocalNotificationService.ShowAsync` — teilweise umgesetzt: `UserInfo` enthält nur den Schlüssel `identifier` (`src/Reporter/Services/LocalNotificationService.cs:57`). Der Plan verlangt `Item.Id`/`Link` (Einzelmodus) bzw. `Feed.Id` (Sammelmodus) in `UserInfo` für späteres Deep-Linking. `Item.Id` ist im Einzelmodus nur indirekt enthalten (identisch mit dem Identifier), `Link` fehlt vollständig (die `ILocalNotificationService.ShowAsync`-Signatur transportiert keinen Link), und `Feed.Id` ist im Sammelmodus nur als Substring des Identifiers enthalten, nicht als eigener Schlüssel.
- [ ] `NotificationServiceTests.NotifyNewItemsAsync_SummaryEnabled_SameItems_SameIdentifier` — teilweise umgesetzt: Der Test (`src/Reporter.Tests/NotificationServiceTests.cs:221`) prüft nur „gleicher Artikelbestand → gleicher Identifier". Der im Plan geforderte zweite Aspekt „veränderter Bestand → anderer Identifier" ist nicht assertiert.

## Hinweise

- **iOS-Verifikation ausstehend (plangekonformer Folgeschritt):** Der Plan sieht die iOS-Simulator-Verifikation via `scripts/iOS-Deployment.ps1` nur auf macOS vor; sie ist in `test-results.md` als Folgeaufgabe dokumentiert (Tasks-Datei #40). Task #16 sollte idealerweise vor dieser Verifikation geschlossen werden, da die `UserInfo`-Belegung den späteren Deep-Linking-Pfad betrifft.
- **Testnamen-Abweichungen (Abdeckung äquivalent, keine Lücke):** `EditCommand_PrefillsFeedNotificationsEnabled` → `EditCommand_PopulatesFeedNotificationsEnabled`; `SaveCommand_PersistsNotificationsEnabled_AddAndUpdate` → aufgeteilt in `SaveCommand_NewFeed_PersistsNotificationsEnabledFalse` + `SaveCommand_ExistingFeed_PersistsNotificationsEnabled`; `SaveCommand_ResetsToggleToTrue` → `SaveCommand_ResetsFeedNotificationsEnabled`; `AddAsync_PersistsNotificationsEnabled`/`UpdateAsync_PersistsNotificationsEnabled` → als Asserts in `UpdateAsync_PersistsChanges` bzw. `GetAllWithDetailsAsync_ProjectsNotificationsEnabled` integriert.
- **Keine direkten Tests (plangemäß):** EF-Migrationen (Suite nutzt `EnsureCreated`), DI-Registrierung in `MauiProgram`, `App.OnStart`-Berechtigungsblock und iOS-Plattformcode (`NotificationDelegate`, `AppDelegate`, `LocalNotificationService`-`#if IOS`-Zweig) sind nicht unit-testbar — im Plan so einkalkuliert.
- **`App.OnStart` lädt die Settings zweimal** (`App.xaml.cs:54` und `:67`) — funktional plangemäß (eigener `try/catch`), nur als Beobachtung.
