<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Feed-Suche über feedsearch.dev und clientseitige Autodiscovery statt reiner URL-Eingabe

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `FeedSearchMatchKind`-Enum anlegen (`src/Reporter.Core/Models/FeedSearchMatchKind.cs`: `ExactUrl`, `Directory`, `Discovered`) | Offen | — |
| 2 | Datenmodell | `FeedSearchResult`-Datenmodellklasse anlegen (`src/Reporter.Core/Models/FeedSearchResult.cs`: `Title`, `Description`, `SiteName`, `SiteUrl`, `FeedUrl` (required), `Score`, `MatchKind`) | Offen | — |
| 3 | Interfaces | `IFeedSearchService`-Interface anlegen (`src/Reporter.Core/Interfaces/IFeedSearchService.cs`: `SearchAsync(string, CancellationToken)` → `Task<IReadOnlyList<FeedSearchResult>>`, wirft `FeedSearchUnavailableException`) | Offen | — |
| 4 | Logik | `FeedSearchUnavailableException` anlegen (`src/Reporter.Core/Services/FeedSearchUnavailableException.cs`) | Offen | — |
| 5 | Logik | `FeedSearchService` implementieren (`src/Reporter.Core/Services/FeedSearchService.cs`: feedsearch.dev-GET `?url=…&info=true&favicon=false&opml=false&skip_crawl=true`, gelinkte 2-s-`CancellationTokenSource` pro Aufruf, Autodiscovery via `<link rel="alternate">`-Parsing + Standardpfade + Feed-Content-Type-Erkennung, `bozo`-Filter, `MatchKind`-Ableitung, Dedupe nach `FeedUrl`, Sortierung `MatchKind`→`Score`→`FeedUrl`, `FeedSearchUnavailableException` nur bei Fehlschlag beider Quellen) | Offen | — |
| 6 | Logik | `FeedSyncService`: `RunSyncAsync`/`UpdateFeedHealthAsync` erweitern — Platzhalter-Titel (leer oder `== Url`) beim ersten Sync durch `SyndicationFeed.Title` ersetzen (optionaler Parameter `resolvedTitle`) | Offen | — |
| 7 | Logik | `FeedsViewModel`: Konstruktor um Pflichtparameter `IFeedSearchService` erweitern und neue Commands verdrahten | Offen | — |
| 8 | Logik | `FeedsViewModel`: `IsValidFeedUrl` aus `SaveAsync` extrahieren, `TryNormalizeDomainUrl` hinzufügen und `SaveAsync` auf `IsValidFeedUrl` umbauen | Offen | — |
| 9 | Logik | `FeedsViewModel`: Eigenschaften `SearchResults`, `ShowSearchResults`, `IsSearching`, `SearchErrorMessage`/`HasSearchError`, `ConfirmDirectAddAsync` hinzufügen; `NewUrl`-Setter um Suchzustand-Reset erweitern | Offen | — |
| 10 | Logik | `FeedsViewModel`: `SearchCommand` + `SearchAsync` implementieren (CanExecute `IsOnline && !IsSearching`, Klassifikation URL/Domain/Freitext, Service-Aufruf, leere Treffer + gültige URL → `ConfirmDirectAddAsync`, `FeedSearchUnavailableException` → `SearchErrorMessage`) | Offen | — |
| 11 | Logik | `FeedsViewModel`: `SubscribeResultCommand` + `SubscribeResultAsync` implementieren (Dublettenprüfung `GetByUrlAsync`, `AddAsync` mit `FeedUrl`/Titel-bzw.-Platzhalter/`SelectedCategory`/`FeedNotificationsEnabled`, `ResetForm`, `LoadAsync`) | Offen | — |
| 12 | Logik | `FeedsViewModel`: `OnConnectivityChanged` erweitern (`SearchCommand.NotifyCanExecuteChanged`, `SearchErrorMessage` leeren) | Offen | — |
| 13 | Konfiguration | `MauiProgram`: `.AddSingleton<IFeedSearchService, FeedSearchService>()` nach der `HttpClient`-Registrierung einfügen | Offen | — |
| 14 | Ressourcen | Neue `AppResources`-Schlüssel in `AppResources.resx` **und** `AppResources.de.resx` eintragen (`PlaceholderFeedSearch`, `ButtonSearch`, `FeedSearchNoResults`, `FeedSearchNoResultsTitle`, `FeedSearchNoResultsAddUrl`, `FeedSearchUnavailable`, `FeedSearchOfflineHint`, `FeedSearchAttribution`, `ConfirmSubscribeFeedTitle`, `ConfirmSubscribeFeedMessage`); Designer regenerieren | Offen | — |
| 15 | UI | `FeedsPage.xaml`: `Entry NewUrl` auf Suche umstellen (`PlaceholderFeedSearch`, `ReturnCommand` → `SearchCommand`), Suchen-`Button` (`ButtonSearch`, vertikal gestapelt) und `ActivityIndicator` (`IsSearching`) im Formular ergänzen | Offen | — |
| 16 | UI | `FeedsPage.xaml`: Treffer-`CollectionView` in Grid-Row 1 mit Karten-Template (`TapGestureRecognizer` → `OnSearchResultTapped`, `EmptyView` = `FeedSearchNoResults`, ≥44-pt-Touch-Targets, `AppThemeBinding`, Titel-Fallback auf `FeedUrl`), Sichtbarkeits-`DataTrigger` auf `ShowSearchResults` (Feed-Liste invers) und `FeedSearchAttribution`-`Label` einbauen | Offen | — |
| 17 | UI | `FeedsPage.xaml`: `SearchErrorMessage`-`Label` und `FeedSearchOfflineHint`-Hinweis (DataTrigger `IsOnline == false`) ergänzen | Offen | — |
| 18 | UI | `FeedsPage.xaml.cs`: `OnSearchResultTapped` hinzufügen (`DisplayAlertAsync`-Bestätigung → `SubscribeResultCommand`) | Offen | — |
| 19 | UI | `FeedsPage.xaml.cs`: `ConfirmDirectAddAsync`-Callback im Konstruktor verdrahten (`DisplayAlertAsync` „URL direkt hinzufügen?") | Offen | — |
| 20 | Tests | `FakeFeedSearchService` Test-Double anlegen (`src/Reporter.Tests/FakeFeedSearchService.cs`: `NextResults`, `NextException`, `LastQuery`, `CallCount`) | Offen | — |
| 21 | Tests | `FeedSearchServiceTests` anlegen mit privatem `StubHttpMessageHandler` (Mapping, Sortierung, `ExactUrl`-Erkennung, Autodiscovery-Link-Tags/Standardpfade/Feed-Dokument, Fast-Path ohne Site-Request, Dedupe, Fehler beider Quellen/Timeout/Parsefehler → `FeedSearchUnavailableException`, leere Liste) | Offen | — |
| 22 | Tests | `FeedSyncServiceTests` erweitern (`SyncFeedAsync_WhenTitleIsPlaceholder_UpdatesTitleFromFeedDocument`, `SyncFeedAsync_WhenTitleIsSet_DoesNotOverwriteTitle`) | Offen | — |
| 23 | Tests | `FeedsViewModelTests`: `CreateViewModel` an neue Konstruktorsignatur anpassen (Fake übergeben) | Offen | — |
| 24 | Tests | `FeedsViewModelTests`: neue Tests für Suche (inkl. Domain-Normalisierung, Freitext ohne Service-Aufruf), `NewUrl`-Reset, Treffer-Abonnieren (inkl. Titel-Platzhalter), Dublette, Fallback-Confirm, Offline-Verhalten und Connectivity-CanExecute (siehe Plan-Tabelle „Neue Tests") | Offen | — |
| 25 | Tests | `ServiceCollectionTests`: `HttpClient` + `IFeedSearchService`-Registrierung ergänzen und Auflösung prüfen | Offen | — |
| 26 | E2E / Manuelle Verifikation | Benutzerflüsse lt. E2E-Tabelle manuell auf 390 × 844 pt verifizieren (Light + Dark), Vergleich mit `design-draft/stitch_local_rss_feed_reader/feeds_health_status/screen.png`, Screenshots + getestete Größen in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md` dokumentieren | Offen | — |
| 27 | Qualität | `.\scripts\Run-StaticChecks.ps1` ausführen — Format, Security und Static Analysis müssen ohne Befunde durchlaufen (Exit-Code 0) | Offen | — |
