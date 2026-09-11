# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### NotificationDelegate.cs (NotificationDelegate)

- **Fehlerquelle / fehlende Aktionsprüfung** — `DidReceiveNotificationResponse` (Zeilen 24–68) prüft `response.ActionIdentifier` nicht. iOS ruft die Methode auch für `UNNotificationDismissActionIdentifier` auf — wenn die Benutzerin eine zugestellte Benachrichtigung wegwischt („Entfernen"), löst der Handler dennoch die Navigation aus (`articledetail?itemId=…` bzw. `//unread`) und öffnet im Kaltstart-Fallback sogar den externen Browser. Ein Dismiss ist das explizite Gegenteil von „Inhalt öffnen".

  Empfehlung: Zu Beginn `if (!response.IsDefaultAction) { completionHandler(); return; }` ergänzen (die .NET-iOS-Bindings stellen `IsDefaultAction`/`IsDismissAction`/`IsCustomAction` als Convenience-Properties auf `UNNotificationResponse` bereit). Damit reagieren auch zukünftige Custom-Actions nicht versehentlich mit der Standard-Navigation.

- **Thread-Sicherheit / Inkonsistenz (gering)** — Der Launcher-Fallback `await Launcher.Default.OpenAsync(link)` (Zeile 58) läuft auf dem Thread, auf dem iOS den Delegate-Callback zustellt; nur die Shell-Navigation wird via `MainThread.InvokeOnMainThreadAsync` gemarshal­ed (Zeilen 43–52). Die Thread-Zusage für `UNUserNotificationCenterDelegate`-Callbacks ist nicht dokumentiert; `Launcher`/`UIApplication.OpenUrl` gehört auf den Main-Thread. Im Fehlerfall wird die Ausnahme vom `catch (Exception)` still verschluckt — der Kaltstart-Fallback wäre dann wirkungslos.

  Empfehlung: Den `Launcher.OpenAsync`-Aufruf ebenfalls über `MainThread.InvokeOnMainThreadAsync` ausführen (analog zur Shell-Navigation).

### %TEMP%foundation.cs (Repository-Root)

- **Toter Code / versehentliches Artefakt** — Im Repo-Root liegt die untracked Datei `%TEMP%foundation.cs` (23.452 Zeilen, eine Kopie der xamarin-macios-Binding-Definition, offenbar bei der `NSDictionary`-Verifikation durch nicht expandiertes `%TEMP%` entstanden). Die Datei gehört nicht zum Feature und würde bei `git add -A` mit-committed.

  Empfehlung: Datei löschen.

## Verifizierte Iteration-3-Punkte (keine Befunde)

- `BuildUserInfo` (`LocalNotificationService.cs:103-121`): `NSDictionary.FromObjectsAndKeys(values.ToArray(), keys.ToArray())` ist korrekt — die öffentliche API bildet `dictionaryWithObjects:forKeys:` ab, Objects = Values zuerst (anhand der Binding-Definition `FromObjectsAndKeysInternal(NSArray objects, NSArray keys)` verifiziert).
- `IsAuthorizedAsync` (`LocalNotificationService.cs:40-51`): sauber, `IsAuthorized` deckt `Authorized`/`Provisional`/`Ephemeral` ab; Nicht-iOS-Zweig ist No-Op.
- `//unread`-Route: korrekt. Gegen MAUI-`ShellUriHandler.SearchPath`/`RouteRequestBuilder` verifiziert — bei `//unread` wird das einzelne Segment gegen jeden Knoten der Shell-Hierarchie geprüft; implizit generierte Routen (`IMPL_…`) von `TabBar`/`Tab` sind transparent, `ShellContent Route = "unread"` (`AppShell.xaml.cs:23`) matcht eindeutig und absolut. `articledetail?itemId=…` ist relativ (Push) und deckt sich mit der bestehenden Route (`ArticleCardView.xaml.cs:86`, `ArticleDetailPage.xaml.cs:28-39` parst `itemId` als `Guid` — Guid-Strings enthalten keine Query-Sonderzeichen).
- `MainThread.InvokeOnMainThreadAsync(Func<Task<bool>>)` (NotificationDelegate.cs:43): korrektes Overload, `bool`-Rückgabe wird richtig awaited; `completionHandler` läuft garantiert im `finally`.
- `SettingsViewModel`: `NotificationPermissionDenied`/`RefreshNotificationPermissionAsync`/`NotificationAuthorizationDenied`-Event korrekt verdrahtet; `IsSupported`-Guard verhindert Falschmeldung auf Nicht-iOS; `_isLoading` unterdrückt Autorisierungsanfrage beim Laden; Ausschalten setzt `NotificationPermissionDenied` zurück (Zeilen 287-301, 480-519).
- `SettingsPage`: MultiTrigger (`NotificationsEnabled` ∧ `NotificationPermissionDenied`) + `AppInfo.Current.ShowSettingsUI()` korrekt; Event-Abonnement in `OnAppearing`/`OnDisappearing` symmetrisch.
- resx: `NotificationDeniedTitle`/`NotificationDeniedMessage`/`NotificationDeniedOpenSettings` sowie alle weiteren neuen Schlüssel sind EN/DE/Designer konsistent.
- `FeedsViewModel.ResetForm()` (Zeilen 295-302): Iteration-2-Befund behoben, Semantik inkl. `SelectedCategory`-Rücksetzung an beiden Aufrufstellen identisch.
- `TestSettingsHelper` wird in allen in Iteration 2 beanstandeten Tests genutzt (`SettingsViewModelTests_Load`, `SettingsRepositoryTests`, `NotificationServiceTests`, `FeedSyncServiceTests`); neue Permission-Tests (`SettingsViewModelTests_Load`/`_Persist`) sind sauber AAA-strukturiert.
- `FeedSyncService`: `catch … when (ex is not OperationCanceledException)` an beiden Stellen korrekt; `NotificationsEnabled` wird in `UpdateFeedHealthAsync` mitgeschrieben (Zeile 221).
- `AppDelegate`: Delegate-Instanz als starkes Feld gehalten (Zeile 15) — Iteration-1-Befund behoben.
- DI-Registrierung `INotificationService`/`ILocalNotificationService` in `MauiProgram.cs:56-57` vollständig.

## Geprüfte Dateien

- `src/Reporter.Core/Interfaces/INotificationService.cs` (neu)
- `src/Reporter.Core/Interfaces/ILocalNotificationService.cs` (neu)
- `src/Reporter.Core/Services/NotificationService.cs` (neu)
- `src/Reporter.Core/Services/FeedSyncService.cs`
- `src/Reporter.Core/Models/Feed.cs`
- `src/Reporter.Core/Models/FeedListItem.cs`
- `src/Reporter.Core/Models/Settings.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs`
- `src/Reporter.Data/Entities/Feed.cs`
- `src/Reporter.Data/Entities/Settings.cs`
- `src/Reporter.Data/ReporterDbContext.cs`
- `src/Reporter.Data/Repositories/FeedRepository.cs`
- `src/Reporter.Data/Repositories/SettingsRepository.cs`
- `src/Reporter.Data/Migrations/20260911181811_AddFeedNotificationsEnabled.cs` + `.Designer.cs` (neu)
- `src/Reporter.Data/Migrations/20260911181850_AddSettingsNotificationSummary.cs` + `.Designer.cs` (neu)
- `src/Reporter.Data/Migrations/ReporterDbContextModelSnapshot.cs`
- `src/Reporter/MauiProgram.cs`
- `src/Reporter/AppShell.xaml.cs`
- `src/Reporter/Services/LocalNotificationService.cs` (neu)
- `src/Reporter/Platforms/iOS/AppDelegate.cs`
- `src/Reporter/Platforms/iOS/NotificationDelegate.cs` (neu)
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter/Views/ArticleDetailPage.xaml.cs` (Navigationsziel-Verifikation)
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/Views/SettingsPage.xaml.cs`
- `src/Reporter.Tests/NotificationServiceTests.cs` (neu)
- `src/Reporter.Tests/FakeNotificationService.cs` (neu)
- `src/Reporter.Tests/FakeLocalNotificationService.cs` (neu)
- `src/Reporter.Tests/TestSettingsHelper.cs` (neu)
- `src/Reporter.Tests/TestDbContextFactory.cs`
- `src/Reporter.Tests/FeedSyncServiceTests.cs`
- `src/Reporter.Tests/FeedsViewModelTests.cs`
- `src/Reporter.Tests/FeedRepositoryTests.cs`
- `src/Reporter.Tests/SettingsRepositoryTests.cs`
- `src/Reporter.Tests/SettingsViewModelTests_E2E.cs`
- `src/Reporter.Tests/SettingsViewModelTests_Load.cs`
- `src/Reporter.Tests/SettingsViewModelTests_Persist.cs`
- `src/Reporter.Tests/AutoRefreshServiceTests.cs`
- `src/Reporter.Tests/RetentionCleanupServiceTests.cs`
