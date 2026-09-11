# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### AppDelegate.cs (AppDelegate)

- **Fehlerquelle / Objektlebensdauer** — Zeile 22: `UNUserNotificationCenter.Current.Delegate = new NotificationDelegate();` weist eine neu erzeugte Delegate-Instanz zu, ohne eine starke Referenz darauf zu halten. Die native `delegate`-Property von `UNUserNotificationCenter` ist auf iOS als `weak` deklariert; ohne verwaltete Referenz kann die Instanz vom GC freigegeben werden, woraufhin `WillPresentNotification` nicht mehr aufgerufen wird und Vordergrund-Benachrichtigungen still verschwinden.

  Empfehlung: Die Instanz in einem `private readonly NotificationDelegate`-Feld (oder `static`) der `AppDelegate`-Klasse halten und das Feld zuweisen.

### LocalNotificationService.cs (LocalNotificationService)

- **Doppelter Code** — Der Block „`GetNotificationSettingsAsync` → bei `NotDetermined` `RequestAuthorizationAsync(Alert | Badge | Sound)` aufrufen → sonst `IsAuthorized` prüfen" steht nahezu identisch in `RequestAuthorizationAsync` (Zeilen 19–27) und `ShowAsync` (Zeilen 37–50); auch die Options-Kombination `Alert | Badge | Sound` ist zweimal hartkodiert.

  Empfehlung: Eine private Methode `EnsureAuthorizedAsync()` (Rückgabe `Task<bool>`) extrahieren, die beide Aufrufstellen nutzen; die `UNAuthorizationOptions` als `private const`/statisches Feld zentral definieren.

- **Toter Code (ungenutzte Parameter)** — `cancellationToken` wird in `RequestAuthorizationAsync` (Zeile 16) und `ShowAsync` (Zeile 34) deklariert, aber in keinem der beiden Plattformzweige verwendet.

  Empfehlung: Das Token zumindest per `cancellationToken.ThrowIfCancellationRequested()` zu Beginn beachten oder an die awaiteten Aufrufe weiterreichen (z. B. `…Async().WaitAsync(cancellationToken)`).

- **Speculative Generality (gering)** — In `ShowAsync` (Zeile 57) wird `identifier` zusätzlich in `UserInfo` abgelegt, obwohl kein Code (`DidReceiveNotificationResponse` o. ä.) diesen Wert je ausliest; die Ersetzungssemantik des Identifiers ist bereits über `UNNotificationRequest.FromIdentifier` gegeben.

  Empfehlung: `UserInfo` entfernen oder ein Tap-Handling ergänzen, das den Wert tatsächlich nutzt.

### App.xaml.cs (App)

- **Doppelter Code** — In `OnStart` werden in zwei aufeinanderfolgenden `try`-Blöcken (Zeilen 51–62 und 64–78) identisch `ISettingsRepository` aufgelöst und `GetAsync()` aufgerufen — einmal für das Theme, einmal für die Benachrichtigungs-Berechtigung.

  Empfehlung: Die beiden Blöcke zu einem `try` zusammenführen und die geladene `settings`-Instanz für beide Zwecke verwenden.

### FeedSyncService.cs (FeedSyncService)

- **Fehlerbehandlung** — Der `catch (Exception)`-Block um `NotifyNewItemsAsync` (Zeilen 174–182) schluckt auch eine `OperationCanceledException`. Wird das Token während der Benachrichtigung abgebrochen, meldet `RunSyncAsync` dennoch `FeedHealth.Ok` statt den Abbruch zu propagieren; `SyncAllAsync` bricht erst beim nächsten Feed ab.

  Empfehlung: Den Handler auf `catch (Exception ex) when (ex is not OperationCanceledException)` einschränken oder vor dem catch `cancellationToken.ThrowIfCancellationRequested()` ergänzen.

### FeedsViewModel.cs (FeedsViewModel)

- **Inkonsistente Zurücksetzung des Formulars** — `DeleteAsync` (Zeilen 291–297) setzt beim Löschen des gerade bearbeiteten Feeds `NewUrl`, `NewTitle` und `SelectedCategory` zurück, nicht aber das neue `FeedNotificationsEnabled`. In `SaveAsync` (Zeile 261) wird dagegen `FeedNotificationsEnabled = true` zurückgesetzt. Nach dem Löschen behält der Schalter den veralteten Wert des gelöschten Feeds.

  Empfehlung: In `DeleteAsync` ebenfalls `FeedNotificationsEnabled = true;` ergänzen (bzw. das Zurücksetzen der Formularfelder in eine gemeinsame Methode auslagern, da der Block jetzt an zwei Stellen gepflegt wird).

### NotificationServiceTests.cs (NotificationServiceTests)

- **Testqualität (Implementierungsdetail)** — `NotifyNewItemsAsync_SummaryEnabled_KeywordFiltered_ExcludedFromSummary` (Zeilen 242–261) bildet den privaten Hash-Algorithmus `BuildSummaryIdentifier` (SHA-256 über die sortierten Item-IDs) im Test nach und assertiert den exakten Identifier. Damit prüft der Test die interne Implementierung statt des fachlichen Verhaltens („stabiler Identifier pro Item-Menge"), das bereits durch `NotifyNewItemsAsync_SummaryEnabled_SameItems_SameIdentifier` abgedeckt ist; bei jeder Änderung des Hash-Verfahrens muss der Test synchron angepasst werden.

  Empfehlung: Auf die Identifier-Prefix-Prüfung (`StartsWith($"{feed.Id}-")`) und die inhaltliche Prüfung des Bodies reduzieren oder den Vergleich des exakten Hashes streichen.

### Reporter.Tests (Testinfrastruktur)

- **Doppelter Code über Dateien hinweg** — Die neue Hilfsmethode `SaveSettingsAsync` in `NotificationServiceTests` (Zeilen 300–321) ist nahezu identisch mit der in diesem Branch neu hinzugekommenen `SaveSettingsAsync` in `FeedSyncServiceTests` (Zeilen ~181–198): Beide kopieren alle `Settings`-Felder Feld für Feld. Weitere Testklassen (`SettingsRepositoryTests`, `SettingsViewModelTests_*`) enthalten dasselbe Feld-für-Feld-Kopiermuster.

  Empfehlung: Eine gemeinsame Test-Hilfsmethode (z. B. `TestSettingsHelper.SaveAsync(repository, …)` oder einen `With(…)`-Copy-Helper) anlegen und in den Testklassen wiederverwenden.

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
- `src/Reporter.Tests/NotificationServiceTests.cs` (neu)
- `src/Reporter.Tests/FakeNotificationService.cs` (neu)
- `src/Reporter.Tests/FakeLocalNotificationService.cs` (neu)
- `src/Reporter.Tests/FeedSyncServiceTests.cs`
- `src/Reporter.Tests/FeedsViewModelTests.cs`
- `src/Reporter.Tests/FeedRepositoryTests.cs`
- `src/Reporter.Tests/SettingsRepositoryTests.cs`
- `src/Reporter.Tests/SettingsViewModelTests_E2E.cs`
- `src/Reporter.Tests/SettingsViewModelTests_Load.cs`
- `src/Reporter.Tests/SettingsViewModelTests_Persist.cs`
- `src/Reporter.Tests/AutoRefreshServiceTests.cs`
- `src/Reporter.Tests/RetentionCleanupServiceTests.cs`
