# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### LocalNotificationService.cs (LocalNotificationService)

- **Fehlerquelle / falsche API-Verwendung** — `BuildUserInfo` (Zeilen 89–103): `new NSDictionary(keys.ToArray(), values.ToArray())` ruft einen nicht existierenden „Zwei-Arrays"-Konstruktor auf. In .NET für iOS gibt es keinen öffentlichen `NSDictionary(NSObject[], NSObject[])`-Konstruktor — der Binding-Konstruktor `Constructor(NSArray objects, NSArray keys)` ist `internal`, und die einzige öffentliche Array-API ist `NSDictionary.FromObjectsAndKeys(objects, keys)` mit **Values zuerst**. Der Aufruf bindet daher an `NSDictionary(object first, object second, params object[] args)` und erzeugt ein Dictionary mit **einem einzigen Eintrag**: Key = das gesamte Keys-Array, Value = das gesamte Values-Array. Folgen: Die Schlüssel `identifier`/`itemId`/`link`/`feedId` existieren nie, `NotificationDelegate.DidReceiveNotificationResponse` findet `link` nicht (Tap-Handling tot), und ein `userInfo` mit NSArray als Key ist kein gültiges Property-List-Dictionary — `AddNotificationRequestAsync` wirft beim Serialisieren, die Ausnahme wird vom `catch` in `FeedSyncService.RunSyncAsync` geschluckt, sodass auf iOS vermutlich **keine Benachrichtigung zugestellt wird**. Da der Code hinter `#if IOS` steht und das iOS-Target lokal nicht kompiliert wird, fällt der Fehler nicht im Build auf.

  Empfehlung: `return NSDictionary.FromObjectsAndKeys(values.ToArray(), keys.ToArray());` (Reihenfolge: Objects/Values zuerst, Keys zweitens) oder ein `NSMutableDictionary` füllen (`dict[new NSString(pair.Key)] = new NSString(pair.Value)` bzw. `dict[(NSString)pair.Key] = (NSString)pair.Value`).

- **Toter Code (gering, Restbefund aus Iteration 1)** — `BuildUserInfo` schreibt weiterhin den Schlüssel `"identifier"` in das `UserInfo`-Dictionary (Zeilen 91–92), doch `NotificationDelegate.DidReceiveNotificationResponse` liest ausschließlich `"link"` aus; `itemId`/`feedId` werden ebenfalls nirgends konsumiert. Nach der Behebung des obigen Konstruktionsfehlers bleiben nur tatsächlich gelesene Schlüssel sinnvoll.

  Empfehlung: Nur die vom Tap-Handler gelesenen Schlüssel (`link`) bzw. bewusst für spätere Navigation vorgesehene Schlüssel übertragen — oder `DidReceiveNotificationResponse` so erweitern, dass `itemId`/`feedId` für In-App-Navigation genutzt werden.

### FeedsViewModel.cs (FeedsViewModel)

- **Doppelter Code** — Der Formular-Reset-Block steht nahezu identisch zweimal: in `SaveAsync` (Zeilen 259–263: `NewUrl`/`NewTitle` leeren, `FeedNotificationsEnabled = true`, `SelectedFeed = null`, `SelectedCategory` zurücksetzen) und in `DeleteAsync` (Zeilen 292–298). Fünf Zuweisungen werden jetzt an zwei Stellen parallel gepflegt; das in Iteration 1 aufgetretene Problem (vergessenes Flag) entsteht bei jeder neuen Formular-Eigenschaft erneut.

  Empfehlung: Eine private Methode `ResetForm()` extrahieren und an beiden Stellen aufrufen.

### Testdateien (Testinfrastruktur)

- **Doppelter Code über Dateien hinweg** — `TestSettingsHelper.SaveAsync` wurde in dieser Iteration gezielt eingeführt, um die Feld-für-Feld-`Settings`-Kopien zu eliminieren, wird aber nicht durchgehend genutzt: `SettingsViewModelTests_Load.Load_PopulatesNotificationSummaryEnabled` und `SettingsRepositoryTests.SaveAsync_PersistsNotificationSummaryEnabled` (zweimal derselbe Copy-Block pro Speichervorgang) kopieren weiterhin alle `Settings`-Felder manuell — exakt das Muster, das der Helper ersetzt (`TestSettingsHelper.SaveAsync(_settingsRepository, notificationSummaryEnabled: true/false)` wäre äquivalent).

  Empfehlung: Die genannten Tests auf `TestSettingsHelper.SaveAsync` umstellen; bei Bedarf den Helper um weitere Override-Parameter erweitern.

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
- `src/Reporter/App.xaml.cs`
- `src/Reporter/MauiProgram.cs`
- `src/Reporter/Services/LocalNotificationService.cs` (neu)
- `src/Reporter/Platforms/iOS/AppDelegate.cs`
- `src/Reporter/Platforms/iOS/NotificationDelegate.cs` (neu)
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
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

## Anmerkung zur Iteration 1

Alle acht Befunde aus `review-code.1.md` wurden bearbeitet: starkes Delegate-Feld in `AppDelegate`, `EnsureAuthorizedAsync` + zentrale `RequestedAuthorizationOptions`, `CancellationToken`-Nutzung (`ThrowIfCancellationRequested`/`WaitAsync`), Entfernung des Berechtigungsblocks aus `App.OnStart`, `OperationCanceledException`-Filter an beiden Catch-Stellen in `FeedSyncService` (propagiert korrekt zu `SyncAllAsync`/`AutoRefreshService`), `FeedNotificationsEnabled`-Reset in `DeleteAsync`, entlasteter Hash-Test (`StartsWith` statt Hash-Replikation) und der gemeinsame `TestSettingsHelper`. Verbleibende Restpunkte sind oben als eigenständige Befunde geführt.
