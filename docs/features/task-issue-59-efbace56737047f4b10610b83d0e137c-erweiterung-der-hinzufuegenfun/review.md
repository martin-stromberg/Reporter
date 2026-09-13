<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Neue Klassen / Modelle / Verträge

- [x] `FeedSearchMatchKind` (Enum, `src/Reporter.Core/Models/FeedSearchMatchKind.cs`) — angelegt mit `ExactUrl`, `Directory`, `Discovered` in Sortierreihenfolge
- [x] `FeedSearchResult` (Datenmodellklasse, `src/Reporter.Core/Models/FeedSearchResult.cs`) — angelegt mit `Title`, `Description`, `SiteName`, `SiteUrl`, `FeedUrl` (`required`), `Score`, `MatchKind`
- [x] `IFeedSearchService` (Interface, `src/Reporter.Core/Interfaces/IFeedSearchService.cs`) — angelegt mit `SearchAsync(string, CancellationToken)` → `Task<IReadOnlyList<FeedSearchResult>>`, dokumentiert `FeedSearchUnavailableException`
- [x] `FeedSearchService` (Klasse, `src/Reporter.Core/Services/FeedSearchService.cs`) — angelegt: Endpoint-Konstante `https://feedsearch.dev/api/v1/search` (Zeile 17), gelinkte 2-s-`CancellationTokenSource` pro Aufruf (Zeilen 18/64), Verzeichnis-GET mit `info=true&favicon=false&opml=false&skip_crawl=true` (Zeile 112), `bozo`-Filter (Zeilen 138–139), Mapping auf `FeedSearchResult`, `ExactUrl`-Ableitung (Zeilen 156–158, 180), Autodiscovery (`<link rel="alternate">`-Parsing, relative `href`-Auflösung, `title`-Attribut), Standardpfade `/feed`, `/rss`, `/rss.xml`, `/atom.xml`, `/feed.xml`, `/index.xml` (Zeilen 39–44), Feed-Content-Type-Erkennung (Zeilen 23–34), Dedupe nach `FeedUrl`, Sortierung `MatchKind`→`Score` desc→`FeedUrl` (Zeile 104), `FeedSearchUnavailableException` nur bei Fehlschlag beider Quellen
- [x] `FeedSearchUnavailableException` (Exception, `src/Reporter.Core/Services/FeedSearchUnavailableException.cs`) — angelegt mit Standard-Konstruktoren
- [x] `FakeFeedSearchService` (Test-Double, `src/Reporter.Tests/FakeFeedSearchService.cs`) — angelegt mit `NextResults`, `NextException`, `LastQuery`, `CallCount`

### Änderungen an bestehenden Klassen

- [x] `FeedsViewModel`-Konstruktor (`src/Reporter.Core/ViewModels/FeedsViewModel.cs:53`) — Pflichtparameter `IFeedSearchService feedSearchService` vor dem optionalen `localNotificationService`; Feld `_feedSearchService` (Zeile 21); `SearchCommand`/`SubscribeResultCommand`/`CloseSearchResultsCommand` im Konstruktor verdrahtet (Zeilen 69–71)
- [x] Eigenschaft `SearchResults` (`ObservableCollection<FeedSearchResult>`) — vorhanden
- [x] Eigenschaft `ShowSearchResults` (`bool`, Zeile 265) — vorhanden
- [x] Eigenschaft `IsSearching` (`bool`, Zeile 274) — triggert `SearchCommand.NotifyCanExecuteChanged()` (Zeile 281)
- [x] Eigenschaften `SearchErrorMessage`/`HasSearchError` (Zeile 289 ff.) — nach `SyncErrorMessage`-Muster
- [x] Eigenschaft `ConfirmDirectAddAsync` (`Func<string, Task<bool>>?`, Zeile 311) — vorhanden
- [x] `NewUrl`-Setter (Zeilen 130–131) — setzt `ShowSearchResults`/`SearchErrorMessage` zurück und leert `SearchResults`
- [x] `SearchCommand` (`AsyncRelayCommand`, Zeilen 69/107) — CanExecute `IsOnline && !IsSearching`
- [x] `SubscribeResultCommand` (`AsyncRelayCommand<FeedSearchResult?>`, Zeilen 70/112) — vorhanden
- [x] Methode `SearchAsync` (Zeile 508) — Klassifikation URL/Domain/Freitext (Zeile 575 ff.), leere Eingabe ohne Aufruf, defensiver Offline-Pfad mit `FeedSearchOfflineHint` (Zeile 518), Service-Aufruf (Zeile 537), `ConfirmDirectAddAsync` nur für gültige URLs (Zeilen 588–596), `FeedSearchUnavailableException` → `SearchErrorMessage` (differenziert `FeedSearchUnavailable` vs. `FeedSearchUnavailableRetry`, Zeilen 553–555), `IsSearching` in `finally` zurückgesetzt (Zeile 562)
- [x] Methode `SubscribeResultAsync` (Zeile 620) — Dublettenprüfung `GetByUrlAsync` → `ErrorFeedDuplicate`, `AddAsync` mit Titel-bzw.-`FeedUrl`-Platzhalter, `CategoryId` aus `SelectedCategory`, `FeedNotificationsEnabled`, `ShowSearchResults = false` (Zeile 650)
- [x] Methode `IsValidFeedUrl` (`private static`, Zeile 655) — aus `SaveAsync` extrahiert (`Uri.TryCreate` absolut, http/https); `SaveAsync` nutzt sie (Zeile 343)
- [x] Methode `TryNormalizeDomainUrl` (`private static`, Zeile 661) — kein Schema/Whitespace, Host mit Punkt → `https://…`
- [x] `OnConnectivityChanged` (Zeile 681) — `SearchErrorMessage` leeren + `SearchCommand.NotifyCanExecuteChanged()` (Zeilen 684–685)
- [x] `FeedSyncService.RunSyncAsync` (`src/Reporter.Core/Services/FeedSyncService.cs:186`) — Dokumenttitel ermittelt, Platzhalter-Erkennung (leer oder `== Url`, OrdinalIgnoreCase, Zeilen 187–189), `resolvedTitle` an `UpdateFeedHealthAsync` durchgereicht (Zeile 193)
- [x] `FeedSyncService.UpdateFeedHealthAsync` (Zeile 228) — optionaler Parameter `string? resolvedTitle = null`, `Title = resolvedTitle ?? feed.Title` (Zeile 240)
- [x] `MauiProgram` (`src/Reporter/MauiProgram.cs:53`) — `.AddSingleton<IFeedSearchService, FeedSearchService>()` direkt nach der `HttpClient`-Registrierung

### UI (`src/Reporter/Views/FeedsPage.xaml` / `.xaml.cs`)

- [x] `Entry NewUrl` — `PlaceholderFeedSearch`, `ReturnCommand` → `SearchCommand` (Zeilen 22–24)
- [x] Suchen-`Button` (`ButtonSearch`, `SearchCommand`, vertikal gestapelt, Zeilen 74–75) und `ActivityIndicator` (`IsSearching`, Zeilen 71–72) — vorhanden
- [x] `SearchErrorMessage`-`Label` mit `HasSearchError`-Sichtbarkeit und Error-`AppThemeBinding` (Zeilen 104–106) — vorhanden
- [x] `FeedSearchOfflineHint`-`Label` mit `DataTrigger` `IsOnline == false` (Zeilen 94–97) — vorhanden
- [x] Treffer-`CollectionView` in `Grid.Row="1"` — `ItemsSource` `SearchResults`, `EmptyView` `FeedSearchNoResults` (Zeile 123), Sichtbarkeits-`DataTrigger` auf `ShowSearchResults` (Zeile 118), inverser Trigger auf der `RefreshView`/Feed-Liste (Zeile 190), keine `ScrollView`-Verschachtelung
- [x] Karten-Template — `Border` + `RoundRectangle`, `AppThemeBinding`-Hintergrund, `MinimumHeightRequest="44"` (Zeile 128), `TapGestureRecognizer` → `OnSearchResultTapped` mit `CommandParameter="{Binding .}"` (Zeile 132)
- [x] `FeedSearchAttribution`-`Label` (Zeile 175) — unterhalb der Trefferliste im selben Sichtbarkeits-Container
- [x] `OnSearchResultTapped` (`FeedsPage.xaml.cs:87`) — `DisplayAlertAsync` mit `ConfirmSubscribeFeedTitle`/`ConfirmSubscribeFeedMessage` + `ButtonYes`/`ButtonNo` → `SubscribeResultCommand.ExecuteAsync(result)` (Zeilen 95–103)
- [x] `ConfirmDirectAddAsync`-Wiring (`FeedsPage.xaml.cs:23`, Methode `:113`) — `DisplayAlertAsync` mit `FeedSearchNoResultsTitle`/`FeedSearchNoResultsAddUrl`, liefert `Task<bool>`
- [x] `OnFeedTapped` unverändert (Zeile 203 im XAML)

### Ressourcen

- [x] Alle 10 geplanten Schlüssel (`PlaceholderFeedSearch`, `ButtonSearch`, `FeedSearchNoResults`, `FeedSearchNoResultsTitle`, `FeedSearchNoResultsAddUrl`, `FeedSearchUnavailable`, `FeedSearchOfflineHint`, `FeedSearchAttribution`, `ConfirmSubscribeFeedTitle`, `ConfirmSubscribeFeedMessage`) in `AppResources.resx` **und** `AppResources.de.resx` (je 1 Vorkommen, verifiziert); `AppResources.Designer.cs` regeneriert (Properties vorhanden)

### Tests

- [x] `FeedSearchServiceTests` (`src/Reporter.Tests/FeedSearchServiceTests.cs`) — 15 Tests inkl. privatem `StubHttpMessageHandler`: Mapping+`bozo`, Sortierung, `ExactUrl`-Ranking, Link-Tag-Autodiscovery, Standardpfade, Feed-Dokument, Verzeichnis-Fehler+Site erreichbar, beide Quellen fehl → Exception, Timeout → Exception, Aufrufer-Abbruch → `OperationCanceledException`, kaputtes JSON → Autodiscovery, Dedupe (Directory + Discovery getrennt), Fast-Path ohne Site-Request, leere Liste
- [x] `FeedSyncServiceTests` — `SyncFeedAsync_WhenTitleIsPlaceholder_UpdatesTitleFromFeedDocument` (Zeile 495), `SyncFeedAsync_WhenTitleIsSet_DoesNotOverwriteTitle` (Zeile 525)
- [x] `FeedsViewModelTests` — `CreateViewModel` an neue Signatur angepasst; 17 neue Tests: `SearchCommand_PopulatesSearchResults`, `_DomainInput_NormalizesToHttpsUrl`, `_FreeText_DoesNotCallService_ShowsEmptyResults`, `_WhenEmpty_DoesNotCallService`, `_WhenOffline_DoesNotCallService_AndSetsHint`, `_WhenServiceUnavailable_SetsSearchErrorMessage`, `_WhenServiceUnavailableAndDomainInput_SetsRetryHint`, `_WhenConfirmDirectAddThrows_InvokesDialogOnlyOnce`, `_NoResultsAndValidUrl_Confirmed_PrefillsTitleFromHost`, `_NoResultsAndValidUrl_Declined_KeepsFormState`, `_NoResultsAndDomainInput_DoesNotInvokeConfirm`, `NewUrl_Changed_ClearsSearchState`, `CloseSearchResultsCommand_ExitsResultsView`, `SubscribeResultCommand_PersistsFeedFromResult`, `_WhenTitleEmpty_StoresFeedUrlAsPlaceholder`, `_WhenDuplicate_SetsErrorAndDoesNotAdd`, `ConnectivityChanged_UpdatesSearchCommandCanExecute`
- [x] `ServiceCollectionTests.AddReporterServices_ResolvesFeedSearchService` (Zeile 53) — `HttpClient` + `IFeedSearchService`→`FeedSearchService` registriert und aufgelöst
- [x] Testlauf verifiziert: `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release` → 274/274 grün (Baseline 239 + 35 neue), 0 fehlgeschlagen

### Manuelle UI-Verifikation (Tasks-Datei #26 — in Iteration 2 nachgeholt)

- [x] Unpackaged `win-x64`-Release-Build im Windows-Handy-Fenster **390 × 844 pt** (`GetWindowRect`-verifiziert) gestartet; Interaktion via UI Automation (`test-results/issue-59/uia.ps1`); 12 Screenshots Light + Dark unter `test-results/issue-59/manual-*.png`; dokumentiert in `test-results.md` (Root, Abschnitt „Issue #59 → Manuelle UI-Verifikation (durchgeführt, Iteration 2)") und `docs/help/anwendung/mobile-ui-design.md` (Abschnitt „Feed-Suche und Hinzufügen-Flow (issue-59)")
- [x] Verifizierte Flüsse: Domain-Suche → Trefferkarten + Attribution (`manual-04`/`09`/`11`); Treffer-Tap → Confirm → Abonnieren + SQLite-Persistenz (`manual-05`/`06`); URL ohne Treffer → Direkt-Hinzufügen-Dialog → Titel-Vorbelegung → Speichern (`manual-12`, `manual-03`); Suche nicht erreichbar → Hinweis + Fallback-Dialog (`manual-03`); Dublette → `ErrorFeedDuplicate` (`manual-07`); „Zurück zu meinen Feeds"-Button; Layout/Karten ≥ 44 pt/`AppThemeBinding` per Pixelprobe; Design-Draft-Vergleich (`design-draft/stitch_local_rss_feed_reader/feeds_health_status/screen.png`) dokumentiert

### Qualität

- [x] `.\scripts\Run-StaticChecks.ps1` — Exit-Code 0 (Format, Lizenzheader, NuGet-Security, Static-Analysis-Release-Build inkl. MAUI-App, 0 Warnungen/0 Fehler); dokumentiert in `test-results.md`

## Hinweise

- Iteration 2 hat über den Plan hinaus Usability-Review-Befunde umgesetzt (dokumentiert in `review-usability.1.md`): `CloseSearchResultsCommand` + „Zurück zu meinen Feeds"-Button (`FeedsPage.xaml:180–181`), differenzierter Fehlerhinweis `FeedSearchUnavailableRetry` für Domain-Eingaben (`FeedsViewModel.cs:555`), `{0}`-Platzhalter in `FeedSearchNoResultsAddUrl`, sowie sechs zusätzliche Regressionstests — alle Abweichungen sind Erweiterungen, keine Planlücken.
- Zwei E2E-Szenarien waren in der Verifikationsumgebung nicht interaktiv prüfbar und sind dokumentiert durch VM-/Service-Tests abgedeckt (in `test-results.md` Root begründet): Offline-Umschaltung (keine Adminrechte für `Disable-NetAdapter`) und Titel-Befüllung nach erstem Sync (lokaler Stub-Server aus dem App-Prozess nicht erreichbar). Das ist eine dokumentierte Umgebungslimitation, keine Implementierungslücke.
