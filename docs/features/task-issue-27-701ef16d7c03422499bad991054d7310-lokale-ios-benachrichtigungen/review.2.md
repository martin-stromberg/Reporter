# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Neue Klassen und Interfaces

- [x] `INotificationService` (Interface) — angelegt in `src/Reporter.Core/Interfaces/INotificationService.cs`, Signatur `NotifyNewItemsAsync(Feed, IReadOnlyList<Item>, CancellationToken)` plangemäß
- [x] `ILocalNotificationService` (Interface) — angelegt in `src/Reporter.Core/Interfaces/ILocalNotificationService.cs`; `RequestAuthorizationAsync` → `Task<bool>` und `ShowAsync` plangemäß; **Iteration 2:** um `IsSupported` (`ILocalNotificationService.cs:14`) und den optionalen `userInfo`-Parameter (`IReadOnlyDictionary<string, string>?`, `:37`) erweitert — schließt die Iteration-1-Lücke der `UserInfo`-Belegung
- [x] `NotificationService` (Klasse) — angelegt in `src/Reporter.Core/Services/NotificationService.cs`; vollständige Entscheidungskette: Pro-Feed-Flag → globaler Schalter → Ruhezeit (Wrap-around `Start > End`, leeres Intervall `Start == End`, einseitig `null`, via `TimeProvider`) → Keyword-Filter (`IKeywordMatcher.MatchesAny` auf `Title`/`ContentHtml`) → Modus-Verzweigung; Einzelmodus mit `Item.Id` als Identifier und `userInfo` = `itemId` + `link` (`:88,92-101`), Sammelmodus mit `AppResources.NotificationSummaryFormat`-Body, Identifier `{feedId}-{sha256(sortierte ItemIds)}` (`:116-121`) und `userInfo` = `feedId` (`:81`)
- [x] `LocalNotificationService` (Klasse) — angelegt in `src/Reporter/Services/LocalNotificationService.cs`; `EnsureAuthorizedAsync` (`:72-87`) prüft `GetNotificationSettingsAsync`, fordert bei `NotDetermined` `RequestAuthorizationAsync(Alert | Badge | Sound)` an und bricht bei Verweigerung ab (lazy Fallback, plangemäß); `BuildUserInfo` (`:89-103`) überträgt `identifier` plus alle `userInfo`-Paare in das `UserInfo`-`NSDictionary`; Nicht-iOS: No-Op, `IsSupported` = `false`
- [x] `NotificationDelegate` (Klasse) — angelegt in `src/Reporter/Platforms/iOS/NotificationDelegate.cs`; `WillPresentNotification` → `Banner | List | Sound`; **Iteration 2:** `DidReceiveNotificationResponse` (`:23-44`) liest `link` aus `UserInfo` und öffnet ihn via `Launcher.Default.OpenAsync` (Deep-Link beim Antippen, in `try/catch/finally` mit garantiertem `completionHandler()`)
- [x] `FakeLocalNotificationService` (Test-Hilfsklasse) — `src/Reporter.Tests/FakeLocalNotificationService.cs`; zeichnet `ShowAsync`-Aufrufe inkl. `UserInfo` auf, `AuthorizationResult`/`IsSupported` konfigurierbar, `RequestAuthorizationCallCount`-Zähler
- [x] `FakeNotificationService` (Test-Hilfsklasse) — `src/Reporter.Tests/FakeNotificationService.cs`; zeichnet Aufrufe auf, optional `Exception`

### Felder in bestehenden Objekten

- [x] `Feed.NotificationsEnabled` (`bool`, `required init`) — `src/Reporter.Core/Models/Feed.cs`
- [x] `Entities.Feed.NotificationsEnabled` (`bool`, Default `true`) — `src/Reporter.Data/Entities/Feed.cs`
- [x] `FeedListItem.NotificationsEnabled` (`bool`, `required init`) — `src/Reporter.Core/Models/FeedListItem.cs`
- [x] `Settings.NotificationSummaryEnabled` (`bool`, `required init`) — `src/Reporter.Core/Models/Settings.cs`
- [x] `Entities.Settings.NotificationSummaryEnabled` (`bool`, Default `false`) — `src/Reporter.Data/Entities/Settings.cs`
- [x] `FeedsViewModel.FeedNotificationsEnabled` (`bool`, Startwert `true`) — `src/Reporter.Core/ViewModels/FeedsViewModel.cs:99`
- [x] `SettingsViewModel.NotificationSummaryEnabled` (`bool`, Startwert `false`, Setter → `PersistOnChange`) — `src/Reporter.Core/ViewModels/SettingsViewModel.cs:301-311`
- [x] `SettingsViewModel.NotificationAuthorizationDenied` (Event `Func<Task>?`, **Iteration 2 neu**) — `SettingsViewModel.cs:274`

### Geänderte Methoden

- [x] `ReporterDbContext.ConfigureFeed` / `ConfigureSettings` — Spalten-Mappings `notifications_enabled` (`HasDefaultValue(true)`) bzw. `notification_summary_enabled` (`HasDefaultValue(false)`)
- [x] `FeedRepository.MapToModel` / `MapToEntity` / `UpdateAsync` / `GetAllWithDetailsAsync` — `NotificationsEnabled` überall übertragen bzw. projiziert
- [x] `SettingsRepository.MapToModel` / `SaveAsync` — `NotificationSummaryEnabled` übertragen
- [x] `FeedSyncService` — Konstruktor-Abhängigkeit `INotificationService` (`FeedSyncService.cs:30-42`); `RunSyncAsync` sammelt `newItemEntities` (`:128,161`) und ruft `NotifyNewItemsAsync` nach `UpdateLogAsync` in eigenem `try/catch` mit `when (ex is not OperationCanceledException)` auf (`:172-183`); `UpdateFeedHealthAsync` reicht `NotificationsEnabled` durch (`:221`)
- [x] `FeedsViewModel.SaveAsync` — `NotificationsEnabled = FeedNotificationsEnabled` in Add- und Update-Pfad (`:241,255`), Reset auf `true` (`:261`); `EditAsync` — Vorbefüllung (`:274`); `DeleteAsync` setzt den Schalter ebenfalls zurück (`:296`)
- [x] `SettingsViewModel.LoadAsync` — Vorbefüllung aus `settings.NotificationSummaryEnabled` (`:425`); `PersistAsync` — Bindung ans Property (`:563`); **Iteration 2:** Setter `NotificationsEnabled` löst beim Einschalten (außerhalb `_isLoading`) `RequestNotificationAuthorizationAsync` aus (`:289-292,461-480`) — `IsSupported`-Check → `RequestAuthorizationAsync` → bei Verweigerung `NotificationAuthorizationDenied`-Event; neuer optionaler Konstruktorparameter `ILocalNotificationService?` (`:63-69`, per DI injiziert)
- [x] `ArticleDetailViewModel.LoadAsync` — `new Settings`-Fallback um `NotificationSummaryEnabled = false` ergänzt
- [x] `AppDelegate.FinishedLaunching` — überschrieben, setzt `UNUserNotificationCenter.Current.Delegate = _notificationDelegate`; Instanz in `private readonly`-Feld gehalten (`AppDelegate.cs:15,24-28`) — behebt den weak-Delegate/GC-Befund aus `review-code.1.md`
- [x] `MauiProgram.CreateMauiApp` — `AddSingleton<INotificationService, NotificationService>()` und `AddSingleton<ILocalNotificationService, LocalNotificationService>()` vor den ViewModels (`MauiProgram.cs:56-57`)
- [x] `SettingsPage.xaml.cs` (**Iteration 2**) — abonniert `NotificationAuthorizationDenied` in `OnAppearing`/deabonniert in `OnDisappearing` und zeigt `DisplayAlertAsync` „Einstellungen öffnen" → `AppInfo.Current.ShowSettingsUI()` (`SettingsPage.xaml.cs:26-55`)

### UI und Ressourcen

- [x] `FeedsPage.xaml` — Switch-Zeile in der Bearbeitungskarte (`IsToggled="{Binding FeedNotificationsEnabled}"`, `FeedsPage.xaml:39`)
- [x] `SettingsPage.xaml` — Switch-Zeile „Sammel-Benachrichtigung" innerhalb des `IsEnabled="{Binding NotificationsEnabled}"`-`Border` (`SettingsPage.xaml:275`)
- [x] `AppResources.resx` / `.de.resx` / `Designer.cs` — alle fünf Plan-Schlüssel vorhanden; **Iteration 2** zusätzlich `NotificationDeniedTitle`, `NotificationDeniedMessage`, `NotificationDeniedOpenSettings` (en + de, Designer regeneriert)

### Datenbankmigrationen

- [x] `AddFeedNotificationsEnabled` — `src/Reporter.Data/Migrations/20260911181811_AddFeedNotificationsEnabled.cs`
- [x] `AddSettingsNotificationSummary` — `src/Reporter.Data/Migrations/20260911181850_AddSettingsNotificationSummary.cs` inkl. `UpdateData` des Singletons; ModelSnapshot aktualisiert

### Tests und Verifikation

- [x] `NotificationServiceTests` — 12 Fälle; die Iteration-1-Lücke ist geschlossen: `NotifyNewItemsAsync_SummaryEnabled_SameItems_SameIdentifier` (`NotificationServiceTests.cs:243-262`) assertiert jetzt beide Plan-Aspekte (gleicher Bestand → gleicher Identifier, veränderter Bestand → `Assert.NotEqual`); neu: `NotifyNewItemsAsync_SendsItemLink_InUserInfo` (`:202-213`) prüft die `link`-/`itemId`-Übergabe
- [x] `FeedSyncServiceTests` — alle vier Plan-Tests plus `SyncFeedAsync_FeedDisabled_NoNotifications` (E2E-Pflichtszenario)
- [x] `SettingsViewModelTests_Persist` — **Iteration 2 neu:** `NotificationsEnabled_TurnedOn_RequestsAuthorization`, `NotificationsEnabled_TurnedOn_Denied_RaisesNotificationAuthorizationDenied`, `NotificationsEnabled_TurnedOn_UnsupportedPlatform_SkipsRequest` (`SettingsViewModelTests_Persist.cs:183,202,227`) — sichern den verlagerten Berechtigungsanfrage-Pfad ab
- [x] `FeedRepositoryTests`, `FeedsViewModelTests`, `SettingsRepositoryTests`, `SettingsViewModelTests_Load`/`_Persist`/`_E2E` — alle Plan-Tests vorhanden; `TestSettingsHelper` als gemeinsame Hilfsmethode extrahiert (behebt den Duplikat-Befund aus `review-code.1.md`)
- [x] Suite-Lauf im Rahmen dieses Reviews wiederholt: **181 bestanden, 0 fehlgeschlagen** (`dotnet test src/Reporter.Tests -c Release`)

## Offene Aufgaben

Keine.

## Hinweise

- **Bewusste Plan-Abweichung „Berechtigungsanfrage" (Beurteilung: fachlich vereinbar, Plan gilt als erfüllt):** Der Plan sah den `try/catch`-Block in `App.OnStart` vor („Durch Anwender bestätigt: App-Start statt erstem Versand"). In Iteration 2 wurde dieser Block entfernt (`App.xaml.cs` enthält keinen Benachrichtigungs-Code mehr); die Anfrage erfolgt stattdessen beim Aktivieren des globalen Schalters in `SettingsViewModel.NotificationsEnabled` → `RequestNotificationAuthorizationAsync` (`SettingsViewModel.cs:461-480`), mit `IsSupported`-Guard, Verweigerungs-Event und „Einstellungen öffnen"-Alert in `SettingsPage.xaml.cs`. Die lazy Anfrage in `LocalNotificationService.ShowAsync` (`EnsureAuthorizedAsync` bei `NotDetermined`) bleibt als Fallback. Fachliche Einordnung: Die Anforderung (`requirement.md`, Offene Frage #2) nennt „beim App-Start, beim erstmaligen Aktivieren des globalen Schalters in den Einstellungen oder erst vor dem ersten tatsächlichen Versand" ausdrücklich als gleichwertige Optionen — die Implementierung deckt Option 2 vollständig und Option 3 als Fallback ab. Die Abdeckung ist gegenüber der Plan-Variante sogar robuster: Bei bereits aktivem `NotificationsEnabled` (Seed-Default `true`), ohne dass der Schalter je betätigt wurde, greift der lazy Fallback beim ersten Versand; bei verweigerter Berechtigung erhält der Nutzer jetzt einen direkten Weg in die Systemeinstellungen statt eines stillen Fehlschlags. Es bleibt **kein Punkt offen**; empfohlen wird lediglich, die Formulierung in `plan.md` (Abschnitt „Berechtigungsanfrage beim App-Start" sowie `App`-Änderung und Umsetzungsschritt 9) beim nächsten Dokumentationslauf an die implementierte Variante anzugleichen.
- **iOS-Verifikation weiterhin ausstehend (plangekonformer Folgeschritt):** `scripts/iOS-Deployment.ps1` (Aktion `simulator`) erfordert macOS; in `test-results.md` dokumentiert (Tasks-Datei #40). Der in Iteration 2 ergänzte Tap-Pfad (`DidReceiveNotificationResponse` → `link` öffnen) sollte dort mitverifiziert werden.
- **Weitere Iteration-2-Änderungen außerhalb des Planumfangs (nur zur Kenntnis, keine Lücken):** `ILocalNotificationService.IsSupported`, `userInfo`-Parameter, `NotificationDenied*`-Ressourcen und die drei neuen `SettingsViewModelTests_Persist`-Fälle gehen über den ursprünglichen Plan hinaus und schließen die in `review.1.md` dokumentierten Lücken sowie `review-code.1.md`-Befunde.
- **Keine direkten Tests (plangemäß):** EF-Migrationen (Suite nutzt `EnsureCreated`), DI-Registrierung und iOS-Plattformcode sind nicht unit-testbar — im Plan so einkalkuliert.
