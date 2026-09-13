<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

## Umgesetzte Planelemente

### Neue Klassen / Modelle / Verträge

- [x] `FeedSearchMatchKind` (Enum, `src/Reporter.Core/Models/FeedSearchMatchKind.cs`) — angelegt mit `ExactUrl`, `Directory`, `Discovered` in Sortierreihenfolge
- [x] `FeedSearchResult` (Datenmodellklasse, `src/Reporter.Core/Models/FeedSearchResult.cs`) — angelegt mit `Title`, `Description`, `SiteName`, `SiteUrl`, `FeedUrl` (`required`), `Score`, `MatchKind`
- [x] `IFeedSearchService` (Interface, `src/Reporter.Core/Interfaces/IFeedSearchService.cs`) — angelegt mit `SearchAsync(string, CancellationToken)` → `Task<IReadOnlyList<FeedSearchResult>>`, dokumentiert `FeedSearchUnavailableException`
- [x] `FeedSearchService` (Klasse, `src/Reporter.Core/Services/FeedSearchService.cs`) — angelegt: Endpoint-Konstante `https://feedsearch.dev/api/v1/search`, gelinkte 2-s-`CancellationTokenSource` pro Aufruf, Verzeichnis-GET mit `info=true&favicon=false&opml=false&skip_crawl=true`, `bozo`-Filter, Mapping auf `FeedSearchResult`, `ExactUrl`-Ableitung, Autodiscovery (`<link rel="alternate">`-Regex-Parsing im `<head>`, relative `href`-Auflösung, `title`-Attribut), Standardpfade (`/feed`, `/rss`, `/rss.xml`, `/atom.xml`, `/feed.xml`, `/index.xml`), Feed-Content-Type-Erkennung (Query ist selbst Feed → `ExactUrl`-Treffer), Dedupe nach `FeedUrl` (OrdinalIgnoreCase), Sortierung `MatchKind`→`Score` desc→`FeedUrl` ordinal, `FeedSearchUnavailableException` nur bei Fehlschlag beider Quellen
- [x] `FeedSearchUnavailableException` (Exception, `src/Reporter.Core/Services/FeedSearchUnavailableException.cs`) — angelegt mit Standard-Konstruktoren
- [x] `FakeFeedSearchService` (Test-Double, `src/Reporter.Tests/FakeFeedSearchService.cs`) — angelegt mit `NextResults`, `NextException`, `LastQuery`, `CallCount`

### Änderungen an bestehenden Klassen

- [x] `FeedsViewModel`-Konstruktor (`src/Reporter.Core/ViewModels/FeedsViewModel.cs:49`) — Pflichtparameter `IFeedSearchService feedSearchService` vor dem optionalen `localNotificationService`; Feld `_feedSearchService`; `SearchCommand`/`SubscribeResultCommand` im Konstruktor verdrahtet
- [x] Eigenschaft `SearchResults` (`ObservableCollection<FeedSearchResult>`, `FeedsViewModel.cs:249`) — vorhanden
- [x] Eigenschaft `ShowSearchResults` (`bool`, `FeedsViewModel.cs:259`) — vorhanden
- [x] Eigenschaft `IsSearching` (`bool`, `FeedsViewModel.cs:268`) — vorhanden, triggert `SearchCommand.NotifyCanExecuteChanged()`
- [x] Eigenschaften `SearchErrorMessage`/`HasSearchError` (`FeedsViewModel.cs:283`/`:298`) — vorhanden nach `SyncErrorMessage`-Muster
- [x] Eigenschaft `ConfirmDirectAddAsync` (`Func<string, Task<bool>>?`, `FeedsViewModel.cs:305`) — vorhanden
- [x] `NewUrl`-Setter (`FeedsViewModel.cs:116`) — setzt `SearchResults`/`ShowSearchResults`/`SearchErrorMessage` zurück
- [x] `SearchCommand` (`AsyncRelayCommand`, `FeedsViewModel.cs:69`/`:106`) — CanExecute `IsOnline && !IsSearching`
- [x] `SubscribeResultCommand` (`AsyncRelayCommand<FeedSearchResult?>`, `FeedsViewModel.cs:70`/`:111`) — vorhanden
- [x] Methode `SearchAsync` (`FeedsViewModel.cs:502`) — Klassifikation URL/Domain/Freitext, leere Eingabe ohne Aufruf, defensiver Offline-Pfad mit `FeedSearchOfflineHint`, Service-Aufruf, `ConfirmDirectAddAsync` bei leerem Ergebnis/nicht erreichbarer Suche nur für gültige URLs, Titel-Vorbelegung aus Host, `FeedSearchUnavailableException` → `SearchErrorMessage`, sonstige Exceptions geloggt (`Debug.WriteLine`) und als `FeedSearchUnavailable` behandelt, `IsSearching` in `finally` zurückgesetzt
- [x] Methode `SubscribeResultAsync` (`FeedsViewModel.cs:600`) — Dublettenprüfung `GetByUrlAsync` → `ErrorFeedDuplicate` ohne `AddAsync`, `AddAsync` mit Titel-bzw.-`FeedUrl`-Platzhalter, `CategoryId` aus `SelectedCategory` (`Guid.Empty` → `null`), `FeedNotificationsEnabled`, `HealthStatus = FeedHealth.Ok`, `ResetForm` + `LoadAsync`
- [x] Methode `IsValidFeedUrl` (`private static`, `FeedsViewModel.cs:635`) — aus `SaveAsync` extrahiert (`Uri.TryCreate` absolut, http/https)
- [x] Methode `TryNormalizeDomainUrl` (`private static`, `FeedsViewModel.cs:641`) — kein Schema/Whitespace, Host mit Punkt → `https://…`
- [x] `SaveAsync` (`FeedsViewModel.cs:332`) — nutzt `IsValidFeedUrl`, Verhalten unverändert (Titel-Pflicht bleibt)
- [x] `OnConnectivityChanged` (`FeedsViewModel.cs:661`) — `SearchCommand.NotifyCanExecuteChanged()` + `SearchErrorMessage` leeren
- [x] `FeedSyncService.RunSyncAsync` (`src/Reporter.Core/Services/FeedSyncService.cs:186`) — Dokumenttitel ermittelt, Platzhalter-Erkennung (leer oder `== Url`, OrdinalIgnoreCase), `resolvedTitle` an `UpdateFeedHealthAsync` durchgereicht
- [x] `FeedSyncService.UpdateFeedHealthAsync` (`FeedSyncService.cs:228`) — optionaler Parameter `string? resolvedTitle = null`, `Title = resolvedTitle ?? feed.Title`
- [x] `MauiProgram` (`src/Reporter/MauiProgram.cs:53`) — `.AddSingleton<IFeedSearchService, FeedSearchService>()` direkt nach der `HttpClient`-Registrierung

### UI (`src/Reporter/Views/FeedsPage.xaml` / `.xaml.cs`)

- [x] `Entry NewUrl` — `PlaceholderFeedSearch`, `Keyboard="Url"`, `ReturnCommand` → `SearchCommand` (Zeile 21–24)
- [x] Suchen-`Button` (`ButtonSearch`, `SearchCommand`, vertikal gestapelt oberhalb Speichern, Zeile 74) und `ActivityIndicator` (`IsSearching`, Zeile 71) — vorhanden
- [x] `SearchErrorMessage`-`Label` mit `HasSearchError`-Sichtbarkeit und Error-`AppThemeBinding` (Zeile 104–107) — vorhanden
- [x] `FeedSearchOfflineHint`-`Label` mit `DataTrigger` `IsOnline == false` (Zeile 94–103) — vorhanden; bestehendes `OfflineHint`-Banner unverändert
- [x] Treffer-`CollectionView` in `Grid.Row="1"` (Zeile 114–179) — `ItemsSource` `SearchResults`, `EmptyView` `FeedSearchNoResults`, Sichtbarkeits-`DataTrigger` auf `ShowSearchResults`, inverser Trigger auf der `RefreshView`/Feed-Liste (Zeile 184–188), keine `ScrollView`-Verschachtelung
- [x] Karten-Template — `Border` + `RoundRectangle 12`, `AppThemeBinding`-Hintergrund wie Feed-Karten, `MinimumHeightRequest="44"`, `TapGestureRecognizer` → `OnSearchResultTapped` mit `CommandParameter="{Binding .}"`, Titel-Fallback auf `FeedUrl` (DataTrigger + `StringNotEmptyToBoolConverter`), `Description` (max. 3 Zeilen, ausgeblendet wenn leer), `SiteName`/`SiteUrl`-Meta-Zeile, `FeedUrl` sekundär
- [x] `FeedSearchAttribution`-`Label` (Zeile 174–178) — unterhalb der Trefferliste im selben Sichtbarkeits-Container
- [x] `OnSearchResultTapped` (`FeedsPage.xaml.cs:87`) — `DisplayAlertAsync` mit `ConfirmSubscribeFeedTitle`/`ConfirmSubscribeFeedMessage` + `ButtonYes`/`ButtonNo` → `SubscribeResultCommand.ExecuteAsync(result)`
- [x] `ConfirmDirectAddAsync`-Wiring (`FeedsPage.xaml.cs:23`, Methode `:113`) — `DisplayAlertAsync` mit `FeedSearchNoResultsTitle`/`FeedSearchNoResultsAddUrl`, liefert `Task<bool>`
- [x] `OnFeedTapped` unverändert

### Ressourcen

- [x] Alle 10 neuen Schlüssel (`PlaceholderFeedSearch`, `ButtonSearch`, `FeedSearchNoResults`, `FeedSearchNoResultsTitle`, `FeedSearchNoResultsAddUrl`, `FeedSearchUnavailable`, `FeedSearchOfflineHint`, `FeedSearchAttribution`, `ConfirmSubscribeFeedTitle`, `ConfirmSubscribeFeedMessage`) in `AppResources.resx` **und** `AppResources.de.resx`; `AppResources.Designer.cs` regeneriert

### Tests

- [x] `FeedSearchServiceTests` (`src/Reporter.Tests/FeedSearchServiceTests.cs`) — 12 Tests inkl. privatem `StubHttpMessageHandler` (Requestzähler/`RequestedUrls`, `Delay` für Timeout, `Fallback`-Status): Mapping+`bozo`, Sortierung, `ExactUrl`-Ranking, Link-Tag-Autodiscovery, Standardpfade, Feed-Dokument, Verzeichnis-Fehler+Site erreichbar, beide Quellen fehl → Exception, Timeout → Exception, kaputtes JSON → Autodiscovery, Dedupe, Fast-Path ohne Site-Request, leere Liste
- [x] `FeedSyncServiceTests` — `SyncFeedAsync_WhenTitleIsPlaceholder_UpdatesTitleFromFeedDocument`, `SyncFeedAsync_WhenTitleIsSet_DoesNotOverwriteTitle` (RssXml-Helfer um `channelTitle` erweitert)
- [x] `FeedsViewModelTests` — `CreateViewModel` an neue Signatur angepasst (Fake übergeben); 13 neue Tests: `SearchCommand_PopulatesSearchResults`, `_DomainInput_NormalizesToHttpsUrl`, `_FreeText_DoesNotCallService_ShowsEmptyResults`, `_WhenEmpty_DoesNotCallService`, `_WhenOffline_DoesNotCallService_AndSetsHint`, `_WhenServiceUnavailable_SetsSearchErrorMessage`, `_NoResultsAndValidUrl_InvokesConfirmDirectAdd`, `_NoResultsAndDomainInput_DoesNotInvokeConfirm`, `NewUrl_Changed_ClearsSearchState`, `SubscribeResultCommand_PersistsFeedFromResult`, `_WhenTitleEmpty_StoresFeedUrlAsPlaceholder`, `_WhenDuplicate_SetsErrorAndDoesNotAdd`, `ConnectivityChanged_UpdatesSearchCommandCanExecute`
- [x] `ServiceCollectionTests.AddReporterServices_ResolvesFeedSearchService` — `HttpClient` + `IFeedSearchService`→`FeedSearchService` registriert und aufgelöst
- [x] Testlauf verifiziert: `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release` → 268/268 grün (Baseline 239 + 29 neue)

### Qualität

- [x] `.\scripts\Run-StaticChecks.ps1` — beim Review erneut ausgeführt: Exit-Code 0 (Format, Lizenzheader, NuGet-Security, Static-Analysis-Release-Build inkl. MAUI-App, 0 Warnungen/0 Fehler)

## Offene Aufgaben

- [ ] `Manuelle UI-Verifikation (Tasks-Datei #26)` — teilweise umgesetzt: Statische XAML-Prüfung gegen AGENTS.md-Regeln und Design-Entwurf ist in `docs/help/anwendung/mobile-ui-design.md` und `test-results.md` dokumentiert, aber die geforderte **interaktive** Verifikation der Benutzerflüsse im 390 × 844-pt-Fenster (Light + Dark) inkl. Screenshots steht aus — in `test-results.md` explizit als „Manuelle UI-Verifikation (ausstehend)" mit Checkliste markiert (kein interaktiver App-Start in der Implementierungsumgebung möglich).

## Hinweise

- Die offene Aufgabe erfordert eine interaktive Sitzung (Windows-Handysize-Fenster 390 × 844 pt oder iOS-Simulator via `scripts/iOS-Deployment.ps1`); die nachzuholenden Szenarien sind als Checkliste in `test-results.md` (Abschnitt „Issue #59 → Manuelle UI-Verifikation (ausstehend)") hinterlegt.
- Alle fachlichen Planelemente sind im Code vollständig vorhanden; der automatisierte Nachweis (268 Tests grün, Static Checks Exit-Code 0) wurde im Review erneut verifiziert.
