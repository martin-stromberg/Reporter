<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedDetailViewModel.cs (FeedDetailViewModel)

- **Doppelter Code** — `SyncAsync` (Z. 571–609) ist nahezu identisch zu `FeedsViewModel.SyncAsync` (`FeedsViewModel.cs` Z. 227–267) — inklusive des mehrzeiligen Erklärkommentars, des `_isSyncInProgress`-Guards, des Offline-Early-Returns und der Fehlerbehandlung. Einzige Abweichung: das Nachladen danach (`LoadAsync` vs. `ReloadFeedAsync` + `RestartListAsync` im Aufrufer).

  Empfehlung: Den gemeinsamen Guard/Sync-Rumpf in eine wiederverwendbare Stelle verlagern (z. B. `protected`-Methode in `BaseViewModel` mit `Func<Task>`-Nachlade-Callback oder eigene Sync-Helferklasse), statt die Logik zu kopieren.

- **Doppelter Code** — `IsValidFeedUrl` (Z. 730–734) ist wörtlich identisch zu `FeedsViewModel.IsValidFeedUrl` (`FeedsViewModel.Search.cs` Z. 201–205).

  Empfehlung: In eine gemeinsame statische Hilfsmethode auslagern (z. B. neben `FeedTitleFallback` in `Reporter.Core.Services`) und in beiden ViewModels verwenden.

- **Doppelter Code** — Der Kategorie-Pseudo-Eintrag `new Category { Id = Guid.Empty, Name = AppResources.CategoryNone }` inklusive Listenaufbau (Z. 308–313) wiederholt `FeedsViewModel.LoadAsync` (`FeedsViewModel.cs` Z. 193–198) wörtlich.

  Empfehlung: In eine kleine gemeinsame Methode auslagern (z. B. `BuildCategoriesWithNoneAsync(ICategoryRepository)` an zentraler Stelle) oder in beiden VMs eine private Hilfsmethode nach einheitlichem Muster nutzen.

- **Fehlende Kapselung / Doppelter Code** — Der Block „Fehler zurücksetzen, `_currentPage = 0`, `HasMore = true`, `Items.Clear()`, `LoadPageCoreAsync()` unter `_loadLock` ist in `LoadAsync` (Z. 322–334) und `RestartListAsync` (Z. 489–501) identisch dupliziert.

  Empfehlung: In eine private Methode `ResetAndLoadFirstPageAsync()` auslagern und aus beiden Stellen aufrufen.

- **Fehlerbehandlung** — `LoadAsync` ruft `_categoryRepository.GetAllAsync()` (Z. 312) und `_feedRepository.GetAllWithDetailsAsync()` (Z. 315) ohne Fehlerbehandlung auf. Wirft das Repository, landet die Exception im `catch` des Page-Code-Behinds (`FeedDetailPage.xaml.cs` Z. 84–87), wird nur per `Debug.WriteLine` protokolliert und die Seite bleibt leer — ohne `ErrorMessage` für den Nutzer.

  Empfehlung: Die Header-Ladevorgänge in `LoadAsync` ebenfalls mit try/catch absichern und bei Fehler `ErrorMessage = AppResources.ErrorLoadFailed` setzen (analog zu `LoadPageCoreAsync`).

- **Fehlerbehandlung / Kopplung** — Der `SearchText`-Setter (Z. 243–247) startet `RestartListAsync` als Fire-and-Forget (`_ =`) bei jedem Tastenschlag — ohne Debounce und ohne Abbruch laufender Läufe. Mehrere schnelle Eingaben reihen serielle Komplett-Neuläden der Liste an den `_loadLock` und lösen je Tastenschlag eine Datenbankabfrage aus.

  Empfehlung: Debounce (z. B. `CancellationTokenSource`, die beim nächsten Tastenschlag abgebrochen wird, mit kurzer Verzögerung) oder zumindest einen „laufender Restart wird verworfen"-Mechanismus einbauen.

### FeedDetailPage.xaml (FeedDetailPage)

- **Namenskonventionen / Konsistenz** — Der `EditUrlEntry` (Z. 265–268) verwendet `PlaceholderFeedSearch` („Feed URL or website address…") sowohl als `Placeholder` als auch als `SemanticProperties.Description`. Dadurch tragen das Add-Sheet der `FeedsPage` und das Edit-Sheet denselben Barrierefreiheitsnamen — der E2E-Helper `WaitForVisibleEditEntry` (`FeedDetailTests.cs` Z. 200–228) musste diese Mehrdeutigkeit mit einem Workaround auflösen.

  Empfehlung: Eigene Ressource für das Edit-Feld anlegen (z. B. `PlaceholderFeedEditUrl` / „Feed-URL") und als Placeholder + Semantic Description verwenden.

### FeedDetailPage.xaml.cs (FeedDetailPage)

- **Fehlerbehandlung** — `OnFeedActionsClicked` (Z. 96–149) sowie `RenameFeedAsync`, `ChangeCategoryAsync` und `ConfirmDeleteFeedAsync` awaiten ViewModel-Methoden mit Repository-Zugriffen (`_feedRepository.UpdateAsync`, `DeleteAsync` u. a.) ohne try/catch. Da der Handler `async void` ist, führt eine Repository-Exception zu einer unbehandelten Exception im UI-Kontext.

  Empfehlung: Die Aufrufe im Action-Sheet-Handler (oder in den VM-Methoden selbst) mit try/catch absichern und Fehler über `ErrorMessage` bzw. ein `DisplayAlertAsync` melden — analog zum Muster in `OnFeedTapped`/`LoadAsync` mit zumindest `Debug.WriteLine` + sichtbarer Fehlermeldung.

- **Fehlende Validierung** — `ApplyQueryAttributes` (Z. 34–48) kehrt bei fehlendem oder nicht parse-barem `feedId` still zurück: Der Nutzer sieht eine leere Detailseite ohne jede Fehlermeldung.

  Empfehlung: Bei fehlendem/ungültigem Parameter `ErrorMessage` (z. B. `ErrorLoadFailed`) setzen oder zurücknavigieren, damit die Seite nicht stumm leer bleibt.

### FeedsPage.xaml (FeedsPage)

- **Namenskonventionen / Konsistenz** — Die Feed-Karte trägt weiterhin `SemanticProperties.Hint="{x:Static strings:AppResources.AccessibilityTapForActions}"` (Z. 153, „Tippen für Aktionen" / „Double-tap for actions"), obwohl ein Tap jetzt zur Detailseite navigiert und kein Action Sheet mehr öffnet. Screenreader-Nutzer bekommen eine falsche Erwartung kommuniziert.

  Empfehlung: Hint-Text an das neue Verhalten anpassen (z. B. neue Ressource „Feed-Details öffnen") oder die bestehende Ressource sinngemäß umbenennen/umtexten.

### FeedDetailTests.cs (FeedDetailTests)

- **Doppelter Code** — Mehrere private Helfer sind nahezu identisch zu vorhandenen Helfern kopiert: `WaitForElementByName`/`WaitForElementInScopeByName`/`TryFindElementInScopeByName` (Z. 58–80, identisch zu `SmokeTests` Z. 39–61), `AddFeedViaUi` (Z. 90–101, nahezu identisch zu `SmokeTests` Z. 72–84), `ScrollDetailListDown` (Z. 134–145, gleiche Scroll-Pattern/Maus-Fallback-Logik wie `E2EPageHelpers.ScrollListDownOnce` Z. 147–170) und `ResetUiState` (Z. 150–193, Kopie von `SmokeTests.ResetUiState` Z. 102–146 plus Zusatzschritte).

  Empfehlung: Die gemeinsamen UIA-Wrapper und Feed-Anlege-/Cleanup-Helfer in `E2EPageHelpers` bzw. `UiRetry` konsolidieren (z. B. `TryFindElementInScopeByName`, `AddFeedViaUi`, `DismissPopups` dorthin verlagern) und in beiden Testklassen nutzen.

### FeedDbAssertions.cs (FeedDbAssertions)

- **Fehlerbehandlung** — `MarkAllItemsReadAsync` (Z. 204–231) schluckt nach fünf fehlgeschlagenen `SqliteException`-Versuchen den Fehler vollständig: kein Log, kein Wurf, kein Rückgabewert — ein dauerhaft gesperrtes DB-File bleibt unsichtbar und äußert sich nur indirekt über spätere Testfehler.

  Empfehlung: Die letzte Exception nach den Retries weiterwerfen (der Cleanup-Aufrufer kann sie tolerieren) oder zumindest per `Debug.WriteLine`/Test-Output protokollieren.

## Hinweise

- Der Lifecycle-Hinweis zu `RaiseUiActionRequested`-Handlern in Blazor-Komponenten ist hier nicht anwendbar (.NET MAUI-Projekt, keine Blazor-Seiten).
- Alle geänderten Projekte wurden testweise gebaut (`Reporter`, `Reporter.Tests`, `Reporter.E2ETests`) — jeweils 0 Fehler, 0 Warnungen.
- Basisbranch: `origin/staging` (per Reflog ermittelt: „Created from origin/staging"); die Implementierung liegt uncommitted im Arbeitsstand vor, daher wurden zusätzlich zu den committed geänderten Dateien auch alle uncommitted geänderten und neuen Quelldateien geprüft.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `src/Reporter.Core/Interfaces/IItemRepository.cs`
- `src/Reporter.Core/ViewModels/FeedDetailViewModel.cs` (neu)
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs`
- `src/Reporter.Data/Repositories/ItemRepository.cs`
- `src/Reporter/Views/FeedDetailPage.xaml` (neu)
- `src/Reporter/Views/FeedDetailPage.xaml.cs` (neu)
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml.cs`
- `src/Reporter/AppShell.xaml.cs`
- `src/Reporter/MauiProgram.cs`
- `src/Reporter.Tests/FeedDetailViewModelTests.cs` (neu)
- `src/Reporter.Tests/FeedsViewModelTests.cs`
- `src/Reporter.Tests/ItemRepositoryTests.cs`
- `src/Reporter.Tests/DelegatingItemRepository.cs`
- `src/Reporter.Tests/FakeFeedSyncService.cs`
- `src/Reporter.E2ETests/FeedDetailTests.cs` (neu)
- `src/Reporter.E2ETests/E2EPageHelpers.cs`
- `src/Reporter.E2ETests/FeedDbAssertions.cs`
- `src/Reporter.E2ETests/SmokeTests.cs`
- `src/Reporter.E2ETests/UiRetry.cs`
- `src/Reporter.E2ETests/Fixtures/paged-feed.xml` (neu, Fixture)
- `src/Reporter.E2ETests/Fixtures/scroll-feed.xml` (neu, Fixture)
