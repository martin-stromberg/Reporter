<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Feed-Suche über feedsearch.dev und clientseitige Autodiscovery statt reiner URL-Eingabe

## Übersicht

Der Hinzufügen-Dialog auf der `FeedsPage` wird um eine Feed-Suche erweitert: Das bisherige URL-Eingabefeld akzeptiert Domains und vollständige URLs; die Suche fragt zuerst die öffentliche API **feedsearch.dev** (`GET /api/v1/search?url=…&info=true&favicon=false&opml=false&skip_crawl=true`) ab und fällt bei leerem/fehlschlagendem Ergebnis auf **clientseitige Autodiscovery** zurück (`<link rel="alternate">`-Tags im HTML der Website, ggf. Standardpfade). Treffer erscheinen als kartenbasierte `CollectionView` und können per Tap nach Bestätigung direkt abonniert werden; der Feed-Titel wird dabei aus dem Treffer bzw. beim ersten Sync aus dem Feed-Dokument befüllt. Die bisherige direkte URL-Hinzufügung (`SaveCommand`/`SaveAsync` in `FeedsViewModel`) bleibt als Fallback für leere Trefferlisten, ungültige URLs und nicht erreichbare Suche unverändert erhalten. Betroffen sind `Reporter.Core` (neues Such-Interface, Service, Modelle, ViewModel-Erweiterung, kleine `FeedSyncService`-Erweiterung, Ressourcen), `src/Reporter` (DI-Registrierung, `FeedsPage`-XAML/Code-Behind) und `src/Reporter.Tests`.

**Bewusste Abweichung vom Issue-Wortlaut (Anwender-Entscheidung):** Suchdienst ist feedsearch.dev + Autodiscovery, nicht „RSS Atlas"; es gibt keine Freitext-/Stichwortsuche — Treffer entstehen nur bei Domain- oder URL-Eingabe.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Suchdienst-Anbindung (`FeedSearchService`) | `GET https://feedsearch.dev/api/v1/search?url={q}&info=true&favicon=false&opml=false&skip_crawl=true` (JSON-Array) + clientseitige Autodiscovery in **einem** Service hinter `IFeedSearchService` (Gateway-Muster) | Anwender-Entscheidung: bewusste Abweichung von „RSS Atlas" (dessen einziger App-tauglicher Endpunkt ein limitierter MCP-Demo-Endpunkt wäre). feedsearch.dev ist eine schlichte JSON-API ohne Auth; ein Service kapselt beide Quellen — ViewModel und Tests sehen nur den Vertrag. |
| `skip_crawl`-Trade-off | `skip_crawl=true` im Verzeichnisaufruf; unbekannte Domains deckt die eigene Autodiscovery ab | feedsearch.dev crawlt unbekannte Domains live — das kann > 2 s dauern und würde mit dem 2-s-Budget systematisch in Timeouts laufen. Mit `skip_crawl=true` antwortet das Verzeichnis aus seinem Cache schnell; die Autodiscovery ist faktisch der „eigene Crawl" im selben Zeitbudget. Konsequenz: Sites ohne `<link>`-Tags/Standardpfade, die das Verzeichnis nicht kennt, liefern keine Treffer → „Kein Feed gefunden"-Fallback. |
| Eingabe-Klassifikation | Drei Fälle im `FeedsViewModel`: (a) gültige absolute http(s)-URL (`IsValidFeedUrl`) → direkt an den Service; (b) Domain-artige Eingabe (kein Schema, host-förmig mit Punkt, kein Whitespace) → normalisiert zu `https://{eingabe}` → Service; (c) Freitext/alles andere → **kein** Service-Aufruf, sofort leere Trefferliste mit EmptyView | Anwender-Entscheidung: keine Freitext-Suche. Der „URL direkt hinzufügen"-Dialog wird nur bei Fall (a) angeboten — für Freitext/Domains würde `SaveAsync` ohnehin mit `ErrorFeedUrlInvalid` ablehnen. |
| Such-Timeout | Eine gelinkte `CancellationTokenSource` mit 2 s **pro `SearchAsync`-Aufruf**, die Verzeichnis- und Autodiscovery-Requests gemeinsam teilt | Anwender-Entscheidung („pro Suchaufruf"); begrenzt die Gesamtlatenz des Suchvorgangs. Der globale 30-s-`HttpClient` für `FeedSyncService` bleibt unverändert. Abbruch → `FeedSearchUnavailableException` → Hinweis + URL-Fallback. |
| Fehlerkontrakt des Such-Service | `FeedSearchUnavailableException` wird nur geworfen, wenn **beide** Quellen fehlschlagen (Verzeichnis-Fehler **und** Autodiscovery-Fehler/Timeout). Leere Trefferliste ist kein Fehler. | Trennt sauber „nicht erreichbar" (Hinweis `FeedSearchUnavailable` + URL-Fallback) von „kein Feed gefunden" (EmptyView + URL-Fallback). Ein Verzeichnis-Ausfall allein führt nicht zum Fehler, solange die Website selbst antwortet. |
| Trefferart / Sortierung | Clientseitig abgeleiteter Enum `FeedSearchMatchKind` (`ExactUrl`, `Directory`, `Discovered`); Sortierung `MatchKind` aufsteigend, dann `Score` absteigend, dann `FeedUrl` ordinal (stabile Reihenfolge) | feedsearch.dev liefert `score`, aber keine Trefferart; Autodiscovery-Treffer haben keinen Score. `ExactUrl` = Treffer, dessen `FeedUrl` der normalisierten Eingabe entspricht (bzw. die Eingabe selbst ein Feed-Dokument ist); `Directory` = feedsearch.dev-Treffer; `Discovered` = Autodiscovery-Treffer. Echter `enum` statt String-Konstanten (Muster: `NotificationAuthorizationStatus`). |
| Eingabefeld | `NewUrl`-`Entry` bleibt das einzige Eingabefeld (Such-/URL-Eingabe); kein separates `SearchQuery`-Property | Anforderung sieht genau dieses Feld als kombinierte Eingabe vor; ein zweites gebundenes Property würde `SaveAsync` und `EditAsync` (die `NewUrl` befüllen) unnötig verkomplizieren. `SearchCommand` liest `NewUrl`. |
| Platzierung/Sichtbarkeit der Trefferliste | Zweite `CollectionView` in `Grid`-Row `*` (Row 1), Sichtbarkeit per `DataTrigger` auf `ShowSearchResults` gegen die Feed-Liste getauscht | `ShowSearchResults` (nicht `HasSearchResults`) steuert die Sichtbarkeit, damit die `EmptyView` („Kein Feed gefunden") auch bei leerem Ergebnis sichtbar wird. Row 1 füllt den Restbildschirm (AGENTS-Regel); DataTrigger folgt dem bestehenden XAML-Stil; keine `ScrollView`-Verschachtelung. Rücksetzen bei `NewUrl`-Änderung und nach Abonnieren/Speichern. |
| Abonnieren-Interaktion | `TapGestureRecognizer` auf Trefferkarte → `OnSearchResultTapped` im Code-Behind → `DisplayAlertAsync`-Bestätigung → `SubscribeResultCommand` | Folgt dem bestehenden `OnFeedTapped`-Muster: Dialoge leben im Code-Behind, weil `Reporter.Core` plattformneutral (`net10.0`) ist und im Testprojekt ohne MAUI läuft. Explizite Nutzer-Auswahl statt Auto-Übernahme (Anforderung). |
| Fallback-Bestätigung („kein Treffer, URL direkt hinzufügen?") | VM-Callback `ConfirmDirectAddAsync` (`Func<string, Task<bool>>?`), im `FeedsPage`-Konstruktor auf `DisplayAlertAsync` verdrahtet | Hält den Dialog im Code-Behind und das ViewModel ohne MAUI-Abhängigkeit testbar (Fake-Callback im Test). |
| Fehleranzeige der Suche | Eigener Kanal `SearchErrorMessage`/`HasSearchError` im `FeedsViewModel` | Folgt dem bestehenden `SyncErrorMessage`-Muster; der Formular-Fehlerkanal (`ErrorMessage`) bleibt für `SaveAsync`-Validierung reserviert. |
| Titel beim Abonnieren | `Feed.Title` = `FeedSearchResult.Title`, falls nicht leer; sonst `FeedUrl` als **Platzhalter**. `FeedSyncService` ersetzt beim ersten Sync einen Platzhalter-Titel (leer oder `== feed.Url`) durch `SyndicationFeed.Title` | Anwender-Entscheidung: kein manueller Titel nötig. Bestandsaufnahme: `FeedSyncService.UpdateFeedHealthAsync` schreibt den Titel heute **unverändert** zurück — eine kleine Erweiterung ist nötig. Platzhalter-Erkennung über `Title == Url` statt neuem Feld → keine Migration, `Feed` bleibt unverändert. Der unveränderte `SaveAsync`-Pfad behält seine Titel-Pflicht (`ErrorFeedTitleEmpty`). |
| Kategorie beim Abonnieren | `CategoryId` aus dem Formular-`SelectedCategory` (`Guid.Empty` → `null`); kein Auto-Mapping/-Anlegen | Anwender-Entscheidung. feedsearch.dev liefert ohnehin keine Kategorien — die Trefferkarte zeigt stattdessen `SiteName`/`SiteUrl` als Meta-Zeile. |
| Beschreibung | `FeedSearchResult.Description` nur in der Trefferkarte anzeigen, nicht persistieren | Anwender-Entscheidung; `Feed` hat kein Beschreibungsfeld → keine neue Spalte/Migration für einen reinen Suchkontext-Wert. |
| Attribution | „powered by feedsearch.dev"-Hinweis-`Label` unterhalb der Trefferliste | Nutzungsbedingung von feedsearch.dev: sichtbare Attribution auf der Such-/Ergebnisseite erforderlich. |

## Programmabläufe

### Feed-Suche (Happy Path)

1. Nutzer gibt eine Domain oder vollständige URL in das `NewUrl`-`Entry` ein und löst `SearchCommand` aus (Return-Taste via `ReturnCommand` oder „Suchen"-`Button`).
2. `SearchAsync` im `FeedsViewModel`: Eingabe trimmen; leer → Abbruch ohne Fehler. `SearchCommand.CanExecute` (`IsOnline && !IsSearching`) verhindert den Aufruf primär bereits; defensiv im Handler nochmals `IsOnline` prüfen → sonst `SearchErrorMessage = AppResources.FeedSearchOfflineHint` + Abbruch.
3. Klassifikation: `IsValidFeedUrl(input)` → `searchUrl = input`. Sonst Domain-artig (kein Schema, host-förmig mit Punkt, kein Whitespace) → `searchUrl = "https://" + input`. Sonst (Freitext) → `SearchResults` leeren, `ShowSearchResults = true` (EmptyView „Kein Feed gefunden"), **kein** Service-Aufruf, kein Direkt-Hinzufügen-Dialog → Ende.
4. `IsSearching = true`, `SearchErrorMessage` leeren, `SearchResults` leeren, `ShowSearchResults = false`.
5. `_feedSearchService.SearchAsync(searchUrl, cancellationToken)` aufrufen → geordnete `IReadOnlyList<FeedSearchResult>`.
6. `SearchResults` befüllen, `ShowSearchResults = true` → UI blendet Treffer-`CollectionView` ein und die Feed-Liste aus (bei leerem Ergebnis EmptyView „Kein Feed gefunden" + ggf. Fallback-Dialog, siehe „Fallback: keine Treffer").
7. `finally`: `IsSearching = false`.

Beteiligte Klassen/Komponenten: `FeedsViewModel`, `IFeedSearchService`, `FeedSearchService`, `FeedSearchResult`, `FeedSearchMatchKind`, `FeedsPage.xaml`.

### Such-Service: Verzeichnisabfrage + Autodiscovery (intern)

1. `FeedSearchService.SearchAsync(query, ct)`: gelinkte `CancellationTokenSource(2 s)` erzeugen (verknüpft mit dem Aufruf-Token — gemeinsames Budget für alle folgenden Requests).
2. **Verzeichnis:** `GET https://feedsearch.dev/api/v1/search?url={Uri.EscapeDataString(query)}&info=true&favicon=false&opml=false&skip_crawl=true`.
   - Erfolg: JSON-Array parsen; Einträge ohne `url` oder mit `bozo == 1` verwerfen; Mapping `title` → `Title`, `url` → `FeedUrl`, `site_url`/`site_name` → `SiteUrl`/`SiteName`, `description` → `Description`, `score` → `Score`; `MatchKind = Directory` (bzw. `ExactUrl`, wenn `FeedUrl` der Query entspricht).
   - HTTP-Fehlerstatus, `OperationCanceledException` oder Parsefehler → Verzeichnis gilt als fehlgeschlagen, mit Autodiscovery fortfahren.
3. **Autodiscovery** (nur wenn das Verzeichnis leer oder fehlgeschlagen war):
   a. `GET {query}` mit `Accept: text/html` und demselben Token.
   b. Content-Type ist Feed-typisch (`application/rss+xml`, `application/atom+xml`, `application/feed+json`, `application/xml`, `text/xml`) → die eingegebene URL ist selbst ein Feed → ein `ExactUrl`-Treffer `{ FeedUrl = query }` (Titel leer → Platzhalter-Mechanismus greift beim Abonnieren); Ende.
   c. Content-Type HTML → `<link>`-Tags im `<head>` per leichtgewichtigem Regex-Parsing auswerten (`rel="alternate"` + `type` ∈ RSS/Atom/JSON-Feed-Medientypen); relative `href`s per `new Uri(baseUri, href)` auflösen; `title`-Attribut → `Title`; Ergebnisse als `MatchKind = Discovered`.
   d. Keine Link-Tags gefunden und Restbudget vorhanden → Standardpfade relativ zum Origin probieren (`/feed`, `/rss`, `/rss.xml`, `/atom.xml`, `/feed.xml`, `/index.xml`): `GET` mit Feed-Content-Type-Prüfung; Treffer als `Discovered`.
   e. Fehler (HTTP/Timeout/Parse) → Autodiscovery gilt als fehlgeschlagen.
4. **Ergebnisbildung:** Verzeichnis-Treffer und Discovered-Treffer mergen, Dedupe nach `FeedUrl` (OrdinalIgnoreCase; Verzeichnis-Treffer hat Vorrang). Sortierung: `MatchKind` (`ExactUrl` → `Directory` → `Discovered`), dann `Score` absteigend, dann `FeedUrl` ordinal.
5. **Fehlerfall:** Verzeichnis fehlgeschlagen **und** Autodiscovery fehlgeschlagen → `FeedSearchUnavailableException`. Mindestens eine Quelle geantwortet → gesammelte (ggf. leere) Liste zurückgeben.

Beteiligte Klassen/Komponenten: `FeedSearchService`, `FeedSearchResult`, `FeedSearchMatchKind`, `FeedSearchUnavailableException`, `HttpClient`, `System.Text.Json`.

### Treffer auswählen und abonnieren

1. Tap auf eine Trefferkarte → `OnSearchResultTapped` im `FeedsPage`-Code-Behind (`TapGestureRecognizer` mit `CommandParameter="{Binding .}"`).
2. `DisplayAlertAsync` (`ConfirmSubscribeFeedTitle`, `ConfirmSubscribeFeedMessage` mit Treffer-Titel bzw. `FeedUrl`, `ButtonYes`/`ButtonNo`).
3. Bei Bestätigung: `SubscribeResultCommand.ExecuteAsync(result)` → `SubscribeResultAsync` im `FeedsViewModel`.
4. Dublettenprüfung: `_feedRepository.GetByUrlAsync(result.FeedUrl)` — Treffer → `ErrorMessage = AppResources.ErrorFeedDuplicate`, `ShowSearchResults` bleibt, Abbruch (kein Anlegen).
5. `_feedRepository.AddAsync(new Feed { Url = result.FeedUrl, Title = !string.IsNullOrWhiteSpace(result.Title) ? result.Title.Trim() : result.FeedUrl, CategoryId = SelectedCategory?.Id == Guid.Empty ? null : SelectedCategory?.Id, HealthStatus = FeedHealth.Ok, NotificationsEnabled = FeedNotificationsEnabled, … })`.
6. `SearchResults` leeren, `ShowSearchResults = false`, `ResetForm()`, `LoadAsync` → Feed-Liste wieder sichtbar mit neuem Eintrag. Der erste Abruf läuft unverändert über `FeedSyncService.SyncFeedAsync` (manuelles Refresh/`AutoRefreshService`); dabei wird der Platzhalter-Titel durch den echten Feed-Titel ersetzt (siehe nächster Ablauf).

Beteiligte Klassen/Komponenten: `FeedsPage` (Code-Behind), `FeedsViewModel`, `IFeedRepository`, `Feed`, `FeedSearchResult`.

### Titel-Befüllung beim ersten Sync (`FeedSyncService`)

1. `RunSyncAsync` lädt wie bisher `SyndicationFeed.Load(reader)`.
2. Neu: `var resolvedTitle = syndicationFeed.Title?.Text` ermitteln.
3. `UpdateFeedHealthAsync` erhält einen optionalen Parameter `resolvedTitle`; dort `Title = resolvedTitle ?? feed.Title`.
4. Aufruf in `RunSyncAsync`: `resolvedTitle` nur übergeben, wenn der gespeicherte Titel ein Platzhalter ist — `string.IsNullOrWhiteSpace(feed.Title)` **oder** `string.Equals(feed.Title, feed.Url, OrdinalIgnoreCase)` — **und** `syndicationFeed.Title?.Text` nicht leer ist. Andernfalls `null` → Titel bleibt unverändert (nutzer-/trefferseitig gesetzte Titel werden nie überschrieben).

Beteiligte Klassen/Komponenten: `FeedSyncService` (`RunSyncAsync`, `UpdateFeedHealthAsync`), `SyndicationFeed`, `IFeedRepository.UpdateAsync`.

### Fallback: keine Treffer bei URL-Eingabe

1. `SearchAsync` erhält eine leere Liste → `ShowSearchResults = true`, `CollectionView` zeigt `EmptyView` mit `FeedSearchNoResults`.
2. War die Eingabe eine gültige absolute http(s)-URL (`IsValidFeedUrl`): VM ruft `ConfirmDirectAddAsync?.Invoke(url)` auf.
3. `FeedsPage` zeigt `DisplayAlertAsync` „Kein Feed gefunden. Möchtest du die eingegebene URL direkt hinzufügen?" (`FeedSearchNoResultsTitle`, `FeedSearchNoResultsAddUrl`).
4. Bestätigt: `NewUrl` bleibt gesetzt; ist `NewTitle` leer, wird es mit dem URL-Host vorbelegt (Komfort, überschreibbar); `SearchResults` leeren + `ShowSearchResults = false` → Nutzer prüft Titel/Kategorie und speichert über den unveränderten `SaveCommand`/`SaveAsync` inkl. aller Validierungen (`ErrorFeedUrlInvalid`, `ErrorFeedTitleEmpty`, `ErrorFeedDuplicate`).
5. Abgelehnt: `SearchResults` leeren + `ShowSearchResults = false`.
6. War die Eingabe Domain-artig oder Freitext ohne Treffer: kein Dialog — die leere Trefferliste mit `EmptyView`-Text bleibt stehen (Domain ist keine direkt speicherbare Feed-URL; Freitext ebenso wenig).

Beteiligte Klassen/Komponenten: `FeedsViewModel`, `FeedsPage` (Code-Behind), `IFeedRepository`.

### Fallback: Suche nicht erreichbar

1. `FeedSearchService` wirft `FeedSearchUnavailableException`, wenn Verzeichnis **und** Autodiscovery fehlschlagen (HTTP-Fehlerstatus, 2-s-Timeout, Parse-/Netzwerkfehler).
2. `SearchAsync` im VM fängt `FeedSearchUnavailableException` → `SearchErrorMessage = AppResources.FeedSearchUnavailable`, `SearchResults` leer, `ShowSearchResults = false` (Feed-Liste bleibt sichtbar).
3. War die Eingabe eine gültige URL, wird zusätzlich derselbe `ConfirmDirectAddAsync`-Dialog wie im „keine Treffer"-Fall angeboten (direkte URL-Hinzufügung bleibt als Fallback erhalten).
4. Unerwartete andere Exceptions werden analog `SyncAsync` geloggt (`Debug.WriteLine`) und als `FeedSearchUnavailable` behandelt.

Beteiligte Klassen/Komponenten: `FeedSearchService`, `FeedSearchUnavailableException`, `FeedsViewModel`, `FeedsPage`.

### Offline-Verhalten

1. `SearchCommand.CanExecute` = `IsOnline && !IsSearching`; `OnConnectivityChanged(bool)` im `FeedsViewModel` ruft `SearchCommand.NotifyCanExecuteChanged()` und leert `SearchErrorMessage`.
2. In `FeedsPage.xaml` wird der Suchen-`Button` offline deaktiviert (CanExecute) und ein Hinweis-`Label` (`FeedSearchOfflineHint`) per `DataTrigger` auf `IsOnline == false` eingeblendet; das vorhandene `OfflineHint`-Banner bleibt unverändert.
3. Die direkte URL-Eingabe/-Hinzufügung bleibt offline über `SaveCommand` möglich — unverändertes Verhalten, kein Suchaufruf nötig.

Beteiligte Klassen/Komponenten: `FeedsViewModel`, `BaseViewModel` (`IsOnline`/`OnConnectivityChanged`), `INetworkStatusService`, `FeedsPage.xaml`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `FeedSearchResult` (`src/Reporter.Core/Models/FeedSearchResult.cs`) | Datenmodellklasse | Ein Suchtreffer: `Title` (`string?`), `Description` (`string?`), `SiteName` (`string?`), `SiteUrl` (`string?`), `FeedUrl` (`string`, required), `Score` (`double`), `MatchKind` (`FeedSearchMatchKind`). Nur In-Memory, keine Tabelle. |
| `FeedSearchMatchKind` (`src/Reporter.Core/Models/FeedSearchMatchKind.cs`) | Enum | Trefferart in Sortierreihenfolge: `ExactUrl`, `Directory`, `Discovered`. |
| `IFeedSearchService` (`src/Reporter.Core/Interfaces/IFeedSearchService.cs`) | Interface | `Task<IReadOnlyList<FeedSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)` — geordnete Trefferliste; wirft `FeedSearchUnavailableException`, wenn beide Quellen unerreichbar sind. |
| `FeedSearchService` (`src/Reporter.Core/Services/FeedSearchService.cs`) | Klasse | Implementiert `IFeedSearchService`. Endpoint-Konstante `https://feedsearch.dev/api/v1/search`, Timeout-Konstante 2 s (gelinkte `CancellationTokenSource` pro Aufruf), Verzeichnisabfrage (`info=true&favicon=false&opml=false&skip_crawl=true`), Autodiscovery (`<link rel="alternate">`-Regex-Parsing, Standardpfade, Feed-Content-Type-Erkennung), Mapping auf `FeedSearchResult`, `MatchKind`-Ableitung, Dedupe nach `FeedUrl`, Sortierung. Konstruktor: `HttpClient` (DI). |
| `FeedSearchUnavailableException` (`src/Reporter.Core/Services/FeedSearchUnavailableException.cs`) | Exception-Klasse | Einheitlicher Fehler des Such-Service: beide Quellen (Verzeichnis + Autodiscovery) fehlgeschlagen. |
| `FakeFeedSearchService` (`src/Reporter.Tests/FakeFeedSearchService.cs`) | Test-Double | `IFeedSearchService`-Fake nach dem Muster der vorhandenen `Fake*`-Klassen: `NextResults`, `NextException`, `LastQuery`, `CallCount`. |

## Änderungen an bestehenden Klassen

### `FeedsViewModel` (ViewModel, `src/Reporter.Core/ViewModels/FeedsViewModel.cs`)

- **Geänderter Konstruktor:** neuer Pflichtparameter `IFeedSearchService feedSearchService` (vor dem optionalen `localNotificationService`-Parameter einordnen — Achtung: optionaler Parameter muss letzter bleiben); Befüllen eines privaten Felds `_feedSearchService`; Verdrahtung der neuen Commands im Konstruktor.
- **Neue Eigenschaften:**
  - `SearchResults` (`ObservableCollection<FeedSearchResult>`) — Trefferliste für die neue `CollectionView`.
  - `ShowSearchResults` (`bool`) — steuert die Sichtbarkeit der Treffer-`CollectionView` (auch bei leerem Ergebnis `true`, damit die `EmptyView` greift); wird bei `NewUrl`-Änderung, nach Abonnieren und im Fehlerfall zurückgesetzt.
  - `IsSearching` (`bool`) — Busy-Indikator; triggert `SearchCommand.NotifyCanExecuteChanged()` im Setter.
  - `SearchErrorMessage` (`string`) / `HasSearchError` (`bool`, get) — eigener Fehlerkanal nach dem `SyncErrorMessage`-Muster.
  - `ConfirmDirectAddAsync` (`Func<string, Task<bool>>?`, get/set) — Callback für den „URL direkt hinzufügen?"-Dialog; wird vom `FeedsPage`-Code-Behind gesetzt.
- **Geänderte Eigenschaft `NewUrl`:** im Setter nach `SetProperty` den Suchzustand zurücksetzen (`SearchResults` leeren, `ShowSearchResults = false`, `SearchErrorMessage` leeren) — veränderte Eingabe macht angezeigte Treffer obsolet. `ResetForm`/`EditAsync` profitieren davon automatisch.
- **Neue Commands:** `SearchCommand` (`AsyncRelayCommand`, CanExecute `IsOnline && !IsSearching`) → `SearchAsync`; `SubscribeResultCommand` (`AsyncRelayCommand<FeedSearchResult?>`) → `SubscribeResultAsync`.
- **Neue Methoden:**
  - `SearchAsync` — Ablauf siehe „Feed-Suche"/„Fallback"-Abschnitte (Klassifikation via `IsValidFeedUrl`/`TryNormalizeDomainUrl`, Service-Aufruf, `ConfirmDirectAddAsync`, Fehlerkanal).
  - `SubscribeResultAsync(FeedSearchResult?)` — Dublettenprüfung + `AddAsync` mit Titel-Platzhalter-Logik + `ResetForm` + `LoadAsync` (siehe „Treffer auswählen und abonnieren").
  - `IsValidFeedUrl(string)` — `private static`, aus `SaveAsync` extrahierte URL-Prüfung (`Uri.TryCreate` absolut, Scheme http/https).
  - `TryNormalizeDomainUrl(string, out string)` — `private static`, Domain-artige Eingaben (`kein Schema, kein Whitespace, Host mit Punkt`) → `https://…`; liefert `false` bei Freitext.
- **Geänderte Methoden:**
  - `SaveAsync` — ersetzt die inline-URL-Prüfung durch `IsValidFeedUrl(url)`; Verhalten unverändert (Titel-Pflicht bleibt).
  - `OnConnectivityChanged(bool)` — zusätzlich `SearchCommand.NotifyCanExecuteChanged()` und `SearchErrorMessage` leeren.
- **Neue Events:** keine (`ConfirmDirectAddAsync` ist bewusst ein Callback-Property, kein Event — das VM muss die boolsche Antwort abwarten können).

### `FeedSyncService` (Service, `src/Reporter.Core/Services/FeedSyncService.cs`)

- **Geänderte Methode `RunSyncAsync`:** nach `SyndicationFeed.Load` den Dokumenttitel (`syndicationFeed.Title?.Text`) ermitteln und an `UpdateFeedHealthAsync` durchreichen — nur wenn der gespeicherte Titel ein Platzhalter ist (`IsNullOrWhiteSpace` oder `== feed.Url`, OrdinalIgnoreCase) und der Dokumenttitel nicht leer ist.
- **Geänderte Methode `UpdateFeedHealthAsync`:** neuer optionaler Parameter `string? resolvedTitle = null`; setzt `Title = resolvedTitle ?? feed.Title` im `UpdateAsync`-Feed.
- Keine sonstigen Änderungen — Offline-Check, Health-Logik, Item-Anlage, Notification-Pfad bleiben unverändert.

### `MauiProgram` (`src/Reporter/MauiProgram.cs`)

- **Neue Registrierung:** `.AddSingleton<IFeedSearchService, FeedSearchService>()` direkt nach der `HttpClient`-Registrierung (Zeile 52) — der Service konsumiert denselben `HttpClient`-Singleton wie `FeedSyncService`; das 2-s-Zeitbudget wird im Service über `CancellationTokenSource` umgesetzt, das globale 30-s-Timeout bleibt unverändert.

### `FeedsPage.xaml` (`src/Reporter/Views/FeedsPage.xaml`)

- `Entry` `NewUrl`: `Placeholder` → neuer Schlüssel `PlaceholderFeedSearch` („Feed-URL oder Website-Adresse…"), `Keyboard="Url"` bleibt, `ReturnCommand` → `SearchCommand`.
- Neuer `Button` „Suchen" (`ButtonSearch`, `Command="{Binding SearchCommand}"`) im Formular — vertikal gestapelt im vorhandenen `VerticalStackLayout` (keine zwei Text-Buttons in einer Zeile, AGENTS-Regel) oberhalb des Speichern-`Button`s; der Speichern-`Button` bleibt für die direkte URL-Hinzufügung.
- `ActivityIndicator` (`IsVisible`/`IsRunning` = `IsSearching`) im Formular.
- `Label` `SearchErrorMessage`/`HasSearchError` unter dem Formular (Error-Farben via `AppThemeBinding`, analog `SyncErrorMessage`-`Label`).
- `Label` `FeedSearchOfflineHint` mit `DataTrigger` auf `IsOnline == false` (analog vorhandenem Offline-`Border`).
- Neue `CollectionView` in `Grid.Row="1"` (gleiche Row wie `RefreshView`, Sichtbarkeit gegeneinander getauscht): `ItemsSource="{Binding SearchResults}"`, `IsVisible` per `DataTrigger` auf `ShowSearchResults` (`True` → sichtbar; die `RefreshView`/Feed-Liste bekommt den inversen Trigger `ShowSearchResults == True` → `IsVisible = False`, Default `True`), `EmptyView` = `FeedSearchNoResults`. ItemTemplate: kartenbasierter `Border` (`RoundRectangle 12`, `AppThemeBinding`-Hintergrund wie Feed-Karten) mit `TapGestureRecognizer` → `OnSearchResultTapped`, `CommandParameter="{Binding .}"`; Inhalt: `Title` mit Fallback-Anzeige (`FeedUrl` wenn leer — `TargetNullValue`/`FallbackValue` am Binding), `Description` (max. 2–3 Zeilen), `SiteName`-/`SiteUrl`-Host als Meta-Zeile, `FeedUrl` (Body-Small, sekundäre Farbe). Mindesthöhe der Karte/Touch-Target ≥ 44 pt (`MinimumHeightRequest="44"`).
- Attribution: `Label` `FeedSearchAttribution` („Suche powered by feedsearch.dev") unterhalb der Treffer-`CollectionView` (Footer des Treffer-Bereichs), sichtbar wenn `ShowSearchResults` — Nutzungsbedingung der API.
- Keine Verschachtelung der neuen `CollectionView` in eine `ScrollView` (Seite bleibt `Grid RowDefinitions="Auto,*"`).
- Vergleichsreferenz Design-Draft: `design-draft/stitch_local_rss_feed_reader/feeds_health_status/screen.png` (Feeds-Flow) — Karten-Look der Trefferliste an die Feed-Karten angleichen.

### `FeedsPage.xaml.cs` (`src/Reporter/Views/FeedsPage.xaml.cs`)

- **Neue Methode `OnSearchResultTapped`:** Tap auf Trefferkarte → `DisplayAlertAsync`-Bestätigung (`ConfirmSubscribeFeedTitle`/`ConfirmSubscribeFeedMessage` + `ButtonYes`/`ButtonNo`) → `SubscribeResultCommand.ExecuteAsync(result)`.
- **Ctor-Erweiterung:** `viewModel.ConfirmDirectAddAsync = …`-Methode zuweisen, die `DisplayAlertAsync` (`FeedSearchNoResultsTitle`, `FeedSearchNoResultsAddUrl`, `ButtonYes`/`ButtonNo`) aufruft und das boolsche Ergebnis zurückgibt.
- `OnFeedTapped` bleibt unverändert.

### `AppResources.resx` / `AppResources.de.resx` (`src/Reporter.Core/Resources/Strings`)

Neue Schlüssel (beide Dateien paritätisch pflegen, `AppResources.Designer.cs` regenerieren lassen):

| Schlüssel | Inhalt (de / en) |
|-----------|-------------------|
| `PlaceholderFeedSearch` | „Feed-URL oder Website-Adresse…" / „Feed URL or website address…" |
| `ButtonSearch` | „Suchen" / „Search" |
| `FeedSearchNoResults` | „Keine Feeds gefunden." / „No feeds found." (EmptyView) |
| `FeedSearchNoResultsTitle` | Titel des Fallback-Dialogs, z. B. „Kein Feed gefunden" / „No feed found" |
| `FeedSearchNoResultsAddUrl` | „Kein Feed gefunden. Möchtest du die eingegebene URL direkt hinzufügen?" / EN-Pendant |
| `FeedSearchUnavailable` | „Die Feed-Suche ist nicht erreichbar. Du kannst die URL direkt hinzufügen." / EN-Pendant |
| `FeedSearchOfflineHint` | „Feed-Suche offline nicht verfügbar — URL direkt hinzufügen bleibt möglich." / EN-Pendant |
| `FeedSearchAttribution` | „Suche powered by feedsearch.dev" / „Search powered by feedsearch.dev" |
| `ConfirmSubscribeFeedTitle` | „Feed abonnieren?" / „Subscribe to feed?" |
| `ConfirmSubscribeFeedMessage` | Platzhalterfähig, z. B. „„{0}" abonnieren?" / „Subscribe to „{0}"?" |

`ErrorFeedUrlInvalid`, `ErrorFeedTitleEmpty`, `ErrorFeedDuplicate`, `ButtonYes`/`ButtonNo`, `OfflineHint` werden wiederverwendet.

## Datenbankmigrationen

Keine. `FeedSearchResult` ist ein reines In-Memory-Modell; `Feed`/`Category` bleiben unverändert; die Treffer-Beschreibung wird nicht persistiert; der Titel-Platzhalter nutzt das vorhandene `Title`-Feld (kein neues Flag).

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Such-Eingabe (`NewUrl`) | Leer/Whitespace → kein Service-Aufruf | Abbruch ohne Fehlermeldung |
| Such-Eingabe, Freitext | `TryNormalizeDomainUrl` liefert `false` | Kein Service-Aufruf; leere Trefferliste mit `EmptyView`-Text „Kein Feed gefunden"; kein Direkt-Hinzufügen-Dialog |
| Such-Eingabe, offline | `IsOnline == false` | `SearchErrorMessage = FeedSearchOfflineHint`, kein Aufruf (primär via CanExecute verhindert) |
| Klassifikation „URL" | `IsValidFeedUrl` (extrahiert aus `SaveAsync`: `Uri.TryCreate` absolut, Scheme http/https) | Steuert, ob bei leerem Ergebnis/nicht erreichbarer Suche der `ConfirmDirectAddAsync`-Dialog angeboten wird |
| Direkte Hinzufügung (`SaveAsync`) | Unverändert: `ErrorFeedUrlInvalid`, `ErrorFeedTitleEmpty`, `ErrorFeedDuplicate` | Unverändert — wirkt auch im Fallback-Pfad |
| Abonnieren (`SubscribeResultAsync`) | `GetByUrlAsync(result.FeedUrl)` Dublettenprüfung; `Title` = Treffer-Titel oder `FeedUrl`-Platzhalter | Dublette → `ErrorMessage = ErrorFeedDuplicate`, kein `AddAsync` |

## Konfigurationsänderungen

Keine. Der feedsearch.dev-Endpoint, die Query-Parameter und das Such-Timeout (2 s) werden als `private const`/statische Konstanten in `FeedSearchService` gehalten (Vorgabe: keine Nutzer-Konfiguration, keine `Settings`-Tabelle).

## Seiteneffekte und Risiken

- **`FeedsViewModel`-Konstruktor:** neuer Pflichtparameter — alle Aufrufer müssen angepasst werden: `MauiProgram` (DI löst auf) und `FeedsViewModelTests.CreateViewModel`. Compiler zwingt zur Anpassung; kein stiller Bruch.
- **`FeedSyncService`-Titel-Überschreibung:** Feeds, deren gespeicherter `Title` leer oder gleich `Url` ist, erhalten beim nächsten Sync den Dokumenttitel. Edge-Case: Ein Nutzer vergibt absichtlich die URL als Titel (z. B. per Edit) → wird beim nächsten Sync ersetzt. Risiko gering und fachlich vertretbar (URL als Titel ist ohnehin kein sinnvoller Anzeigename); keine Migration nötig.
- **`SaveAsync`-Refactoring:** Extraktion von `IsValidFeedUrl` darf das Verhalten nicht ändern — identische Prüfbedingung beibehalten.
- **Geteiltes 2-s-Budget:** Autodiscovery kann bei langsamen Sites unvollständig abbrechen → `FeedSearchUnavailableException` bzw. Teil-Ergebnis; bewusster Trade-off (Anwender-Entscheidung Timeout).
- **Autodiscovery-Blindheit:** Sites ohne `<link rel="alternate">`-Tags und ohne Standardpfade, die feedsearch.dev nicht kennt, liefern keine Treffer → dokumentierte Limitation; der URL-Fallback fängt den Fall ab.
- **`skip_crawl=true`:** feedsearch.dev lernt neue Domains durch diesen Client nicht kennen (kein Crawl-Auftrag); vertretbar, da die Autodiscovery denselben Zweck erfüllt.
- **`NewUrl`-Setter-Seiteneffekt:** Suchzustand wird bei jeder Eingabeänderung zurückgesetzt — `EditAsync` (befüllt `NewUrl`) leert damit ebenfalls eine ggf. sichtbare Trefferliste; gewollt und konsistent.
- **Sichtbarkeits-Toggle in Grid-Row 1:** Während Treffer angezeigt werden (`ShowSearchResults`), ist `RefreshView`/Feed-Liste nicht sichtbar — Pull-to-Refresh und `EmptyView` der Feed-Liste sind in diesem Zustand nicht erreichbar; nach Abonnieren/Leeren kehrt die Ansicht zurück.
- **Attribution-Pflicht:** feedsearch.dev verlangt sichtbare „powered by feedsearch.dev"-Attribution — im XAML verankert, darf nicht entfernt werden, solange der Dienst genutzt wird.
- **`AppResources.Designer.cs`:** generierte Datei — nach resx-Änderung muss sie neu erzeugt werden (Build/IDE), sonst schlagen die `x:Static`-Referenzen fehl.
- **Ressourcen-Parität:** `AppResources.resx` und `AppResources.de.resx` haben je 131 Schlüssel — neue Schlüssel müssen in beiden Dateien eingefügt werden.
- **Keine bestehenden Unit-Tests brechen fachlich**, lediglich `CreateViewModel` kompiliert ohne Signatur-Anpassung nicht (siehe „Betroffene bestehende Tests").

## Umsetzungsreihenfolge

1. **`FeedSearchMatchKind`-Enum anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: `src/Reporter.Core/Models/FeedSearchMatchKind.cs` mit `ExactUrl`, `Directory`, `Discovered` in Sortierreihenfolge; XML-Doc-Kommentare (CS1591 ist Fehler).

2. **`FeedSearchResult`-Datenmodellklasse anlegen**
   - Voraussetzungen: `FeedSearchMatchKind` (Schritt 1).
   - Beschreibung: `src/Reporter.Core/Models/FeedSearchResult.cs` mit `Title`, `Description`, `SiteName`, `SiteUrl`, `FeedUrl`, `Score`, `MatchKind` (`required`/`init`-Muster wie `Feed`; `FeedUrl` required).

3. **`FeedSearchUnavailableException` anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: `src/Reporter.Core/Services/FeedSearchUnavailableException.cs`, `Exception`-Ableitung mit Standard-Konstruktoren.

4. **`IFeedSearchService`-Interface anlegen**
   - Voraussetzungen: `FeedSearchResult` (Schritt 2), `FeedSearchUnavailableException` (Schritt 3 — dokumentiert im Vertrag).
   - Beschreibung: `src/Reporter.Core/Interfaces/IFeedSearchService.cs` mit `SearchAsync(string query, CancellationToken cancellationToken = default)` → `Task<IReadOnlyList<FeedSearchResult>>`.

5. **`FeedSearchService` implementieren**
   - Voraussetzungen: `IFeedSearchService` (Schritt 4); `HttpClient` per DI bereits vorhanden (`MauiProgram` Zeile 52); `System.Text.Json` im BCL von `net10.0` — kein NuGet nötig; kein HTML-Parser-Paket (Regex-basiertes `<link>`-Parsing).
   - Beschreibung: `src/Reporter.Core/Services/FeedSearchService.cs` — Endpoint-/Timeout-Konstanten, gelinkte 2-s-`CancellationTokenSource` pro Aufruf, Verzeichnis-GET (`info=true&favicon=false&opml=false&skip_crawl=true`), Autodiscovery (HTML-Link-Tags, Standardpfade, Feed-Content-Type-Erkennung), Mapping auf `FeedSearchResult`, `MatchKind`-Ableitung, Dedupe nach `FeedUrl`, Sortierung `MatchKind`→`Score`→`FeedUrl`; Fehlersemantik: `FeedSearchUnavailableException` nur wenn beide Quellen scheitern.

6. **DI-Registrierung in `MauiProgram`**
   - Voraussetzungen: `FeedSearchService` (Schritt 5).
   - Beschreibung: `.AddSingleton<IFeedSearchService, FeedSearchService>()` nach der `HttpClient`-Registrierung einfügen.

7. **`FeedSyncService`-Titel-Befüllung**
   - Voraussetzungen: Keine (unabhängiger Schritt).
   - Beschreibung: `RunSyncAsync`/`UpdateFeedHealthAsync` um die Platzhalter-Titel-Ersetzung aus `SyndicationFeed.Title` erweitern (siehe „Änderungen an bestehenden Klassen").

8. **Neue `AppResources`-Schlüssel pflegen**
   - Voraussetzungen: Keine.
   - Beschreibung: Schlüssel lt. Tabelle oben in `AppResources.resx` **und** `AppResources.de.resx` eintragen; Designer-Datei regenerieren (Build löst `PublicResXFileCodeGenerator` aus).

9. **`FeedsViewModel` erweitern**
   - Voraussetzungen: `IFeedSearchService` (Schritt 4), `FeedSearchResult`/`FeedSearchMatchKind` (Schritte 1–2), `FeedSearchUnavailableException` (Schritt 3), neue Ressourcen (Schritt 8).
   - Beschreibung: Konstruktor-Parameter, Eigenschaften (`SearchResults`, `ShowSearchResults`, `IsSearching`, `SearchErrorMessage`/`HasSearchError`, `ConfirmDirectAddAsync`, `NewUrl`-Setter-Reset), Commands (`SearchCommand`, `SubscribeResultCommand`), Methoden (`SearchAsync`, `SubscribeResultAsync`, `IsValidFeedUrl`-Extraktion, `TryNormalizeDomainUrl`), `SaveAsync`-Umbau, `OnConnectivityChanged`-Erweiterung — siehe Abschnitt „Änderungen an bestehenden Klassen".

10. **`FeedsPage.xaml` umbauen**
    - Voraussetzungen: `FeedsViewModel`-Erweiterung (Schritt 9), neue Ressourcen (Schritt 8); Vergleich mit `design-draft/stitch_local_rss_feed_reader/feeds_health_status/screen.png`.
    - Beschreibung: Such-Placeholder/`ReturnCommand`/Suchen-`Button`, `ActivityIndicator`, `SearchErrorMessage`-`Label`, `FeedSearchOfflineHint`, Treffer-`CollectionView` in Row 1 mit Sichtbarkeits-`DataTrigger`n auf `ShowSearchResults`, Karten-Template + `TapGestureRecognizer`, `FeedSearchAttribution`-`Label` — siehe Abschnitt oben.

11. **`FeedsPage.xaml.cs` erweitern**
    - Voraussetzungen: `FeedsViewModel`-Erweiterung (Schritt 9), `FeedsPage.xaml` (Schritt 10), neue Ressourcen (Schritt 8).
    - Beschreibung: `OnSearchResultTapped` (Confirm → `SubscribeResultCommand`), `ConfirmDirectAddAsync`-Wiring im Konstruktor.

12. **`FakeFeedSearchService` anlegen**
    - Voraussetzungen: `IFeedSearchService` (Schritt 4).
    - Beschreibung: `src/Reporter.Tests/FakeFeedSearchService.cs` mit `NextResults`, `NextException`, `LastQuery`, `CallCount`.

13. **`FeedSearchServiceTests` schreiben**
    - Voraussetzungen: `FeedSearchService` (Schritt 5); neuer `StubHttpMessageHandler`-Helfer (in der Testdatei) — `HttpClient` lässt sich mit injiziertem `HttpMessageHandler` ohne Netzwerk testen, kein Mock-Framework im Projekt vorhanden/nötig.
    - Beschreibung: siehe Tabelle „Neue Tests".

14. **`FeedSyncServiceTests` erweitern**
    - Voraussetzungen: `FeedSyncService`-Änderung (Schritt 7).
    - Beschreibung: Titel-Platzhalter-Ersetzung und Nicht-Überschreibung bestehender Titel testen (siehe Tabelle „Neue Tests").

15. **`FeedsViewModelTests` anpassen und erweitern**
    - Voraussetzungen: `FeedsViewModel`-Erweiterung (Schritt 9), `FakeFeedSearchService` (Schritt 12).
    - Beschreibung: `CreateViewModel` um das Fake erweitern; neue Tests lt. Tabelle; bestehende Tests bleiben inhaltlich unverändert.

16. **`ServiceCollectionTests` erweitern**
    - Voraussetzungen: `FeedSearchService` (Schritt 5), DI-Registrierung (Schritt 6 — der Test spiegelt `MauiProgram` ohne MAUI).
    - Beschreibung: `HttpClient` und `IFeedSearchService`→`FeedSearchService` in der Test-`ServiceCollection` registrieren und per `GetRequiredService` auflösen.

17. **Manuelle UI-Verifikation + Dokumentation**
    - Voraussetzungen: Schritte 10–11 (UI fertig); Windows-handysize-Fenster 390 × 844 pt oder iOS-Simulator via `scripts/iOS-Deployment.ps1`.
    - Beschreibung: Benutzerflüsse lt. E2E-Tabelle durchspielen, Screenshots (Light + Dark), Design-Draft-Vergleich, Touch-Targets/`AppThemeBinding`/Listenlayout prüfen; Ergebnis in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md` dokumentieren (AGENTS.md-Vorgabe).

18. **Statische Checks**
    - Voraussetzungen: Alle vorherigen Schritte.
    - Beschreibung: `.\scripts\Run-StaticChecks.ps1` — Format, Security und Static-Analysis-Build müssen ohne Befunde durchlaufen (Exit-Code 0), bevor das Feature als fertig gilt.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `StubHttpMessageHandler` (privat, in der Testdatei) | `FeedSearchServiceTests` | Liefert pro Request-URL vorgegebene Antworten (feedsearch.dev-JSON, HTML mit/ohne `<link>`-Tags, Feed-XML) oder HTTP-Fehlercodes; optional `Delay` zum Auslösen des 2-s-Timeouts; zählt Requests/URLs (Verzeichnis- vs. Site- vs. Standardpfad-Aufrufe unterscheidbar). |
| `SearchAsync_MapsDirectoryEntries_ToFeedSearchResults` | `FeedSearchServiceTests` | feedsearch.dev-JSON (`title`/`url`/`site_url`/`site_name`/`description`/`score`) wird vollständig auf `FeedSearchResult` (`FeedUrl` ← `url`) mit `MatchKind.Directory` gemappt; `bozo == 1`-Einträge werden verworfen. |
| `SearchAsync_SortsByMatchKindThenScoreThenFeedUrl` | `FeedSearchServiceTests` | Gemischte Treffer (ExactUrl/Directory/Discovered) werden in Enum-Reihenfolge, innerhalb `Directory` nach `Score` absteigend, stabiler Tiebreak `FeedUrl` sortiert. |
| `SearchAsync_WhenResultFeedUrlEqualsQuery_RanksExactUrlFirst` | `FeedSearchServiceTests` | Treffer, dessen `url` der Query entspricht, erhält `MatchKind.ExactUrl` und steht vorn. |
| `SearchAsync_DirectoryEmpty_FallsBackToAutodiscovery_LinkTags` | `FeedSearchServiceTests` | Verzeichnis liefert `[]` → Site-HTML mit `<link rel="alternate" type="application/rss+xml" href="/feed" title="…">` → `Discovered`-Treffer mit aufgelöster absoluter `FeedUrl` und `Title` aus dem `title`-Attribut. |
| `SearchAsync_DirectoryEmpty_ProbesStandardPaths` | `FeedSearchServiceTests` | HTML ohne Link-Tags → Standardpfade werden probiert; `/feed` mit Feed-Content-Type → `Discovered`-Treffer. |
| `SearchAsync_WhenQueryIsFeedDocument_ReturnsExactUrlResult` | `FeedSearchServiceTests` | Verzeichnis leer; `GET {query}` liefert Feed-Content-Type (z. B. `application/rss+xml`) → ein `ExactUrl`-Treffer mit `FeedUrl == query`. |
| `SearchAsync_DirectoryError_ButSiteReachable_ReturnsDiscoveredResults` | `FeedSearchServiceTests` | Verzeichnis-HTTP-Fehler → Autodiscovery läuft trotzdem; Treffer werden geliefert, **keine** Exception. |
| `SearchAsync_BothSourcesFail_ThrowsFeedSearchUnavailableException` | `FeedSearchServiceTests` | Verzeichnis-Fehler + Site-Abruf-Fehler → `FeedSearchUnavailableException`. |
| `SearchAsync_Timeout_ThrowsFeedSearchUnavailableException` | `FeedSearchServiceTests` | Stub verzögert > 2 s → `OperationCanceledException` wird in `FeedSearchUnavailableException` gewrappt (kein 30-s-Warten; Budget gilt aufrufübergreifend). |
| `SearchAsync_MalformedDirectoryJson_FallsBackToAutodiscovery` | `FeedSearchServiceTests` | Kaputte Verzeichnis-JSON → wie Verzeichnis-Fehler behandelt, Autodiscovery greift. |
| `SearchAsync_DirectoryAndDiscoveryOverlap_DedupesByFeedUrl` | `FeedSearchServiceTests` | Gleiche `FeedUrl` aus beiden Quellen → ein Eintrag; Verzeichnis-Treffer (`Directory`) hat Vorrang. |
| `SearchAsync_DirectoryResultsPresent_SkipsAutodiscovery` | `FeedSearchServiceTests` | Verzeichnis liefert Treffer → kein Site-Request (Stub-Requestzähler) — Fast-Path. |
| `SearchAsync_EverythingEmpty_ReturnsEmptyList` | `FeedSearchServiceTests` | `[]` vom Verzeichnis + HTML ohne Feeds → leere Liste, keine Exception. |
| `FakeFeedSearchService` | `src/Reporter.Tests/FakeFeedSearchService.cs` | Test-Double mit `NextResults`, `NextException`, `LastQuery`, `CallCount`. |
| `SearchCommand_PopulatesSearchResults` | `FeedsViewModelTests` | Happy Path: `SearchResults` gefüllt, `ShowSearchResults` true, Service mit getrimmtem/normalisiertem Query aufgerufen (`LastQuery`). |
| `SearchCommand_DomainInput_NormalizesToHttpsUrl` | `FeedsViewModelTests` | Eingabe `example.com` → `LastQuery == "https://example.com"`. |
| `SearchCommand_FreeText_DoesNotCallService_ShowsEmptyResults` | `FeedsViewModelTests` | Freitext → `CallCount == 0`, `ShowSearchResults == true` (EmptyView-Pfad), kein `ConfirmDirectAddAsync`. |
| `SearchCommand_WhenEmpty_DoesNotCallService` | `FeedsViewModelTests` | Leere Eingabe → `CallCount == 0`, kein Zustandswechsel. |
| `SearchCommand_WhenOffline_DoesNotCallService_AndSetsHint` | `FeedsViewModelTests` | `FakeNetworkStatusService.IsOnline = false` → `CanExecute` false bzw. defensiver Pfad setzt `SearchErrorMessage = FeedSearchOfflineHint`. |
| `SearchCommand_WhenServiceUnavailable_SetsSearchErrorMessage` | `FeedsViewModelTests` | `NextException = FeedSearchUnavailableException` → `SearchErrorMessage = FeedSearchUnavailable`, `ShowSearchResults == false`, Formular-`ErrorMessage` bleibt leer. |
| `SearchCommand_NoResultsAndValidUrl_InvokesConfirmDirectAdd` | `FeedsViewModelTests` | Leere Ergebnisliste + gültige URL → Fake-`ConfirmDirectAddAsync` wird mit der URL aufgerufen; bei `true` bleibt `NewUrl` gesetzt (Titel-Vorbelegung aus Host, falls leer); bei `false` keine Formularbefüllung. |
| `SearchCommand_NoResultsAndDomainInput_DoesNotInvokeConfirm` | `FeedsViewModelTests` | Domain ohne Treffer → Callback nicht aufgerufen (Domain ist keine speicherbare Feed-URL). |
| `NewUrl_Changed_ClearsSearchState` | `FeedsViewModelTests` | Nach erfolgreicher Suche → `NewUrl` ändern → `SearchResults` leer, `ShowSearchResults == false`. |
| `SubscribeResultCommand_PersistsFeedFromResult` | `FeedsViewModelTests` | `AddAsync` mit `Url`/`Title` aus dem Treffer, `CategoryId` aus `SelectedCategory`, `NotificationsEnabled` aus Formular; `SearchResults` geleert, `ShowSearchResults == false`, Liste neu geladen. |
| `SubscribeResultCommand_WhenTitleEmpty_StoresFeedUrlAsPlaceholder` | `FeedsViewModelTests` | Treffer ohne `Title` → persistierter `Feed.Title == result.FeedUrl` (Platzhalter für den Sync-Mechanismus). |
| `SubscribeResultCommand_WhenDuplicate_SetsErrorAndDoesNotAdd` | `FeedsViewModelTests` | Bereits geseedeter Feed mit gleicher `FeedUrl` → `ErrorMessage = ErrorFeedDuplicate`, kein zweites `AddAsync`. |
| `ConnectivityChanged_UpdatesSearchCommandCanExecute` | `FeedsViewModelTests` | `RaiseConnectivityChanged` offline → `SearchCommand.CanExecute` false; online → true; `SearchErrorMessage` wird geleert. |
| `SyncFeedAsync_WhenTitleIsPlaceholder_UpdatesTitleFromFeedDocument` | `FeedSyncServiceTests` | Feed mit `Title == Url` geseedet, Stub-Feed-Dokument mit `SyndicationFeed.Title` → nach Sync ist `Feed.Title` der Dokumenttitel. |
| `SyncFeedAsync_WhenTitleIsSet_DoesNotOverwriteTitle` | `FeedSyncServiceTests` | Feed mit echtem Titel → `Feed.Title` bleibt nach Sync unverändert, auch wenn das Dokument einen anderen Titel trägt. |
| `AddReporterServices_ResolvesFeedSearchService` (oder Erweiterung des bestehenden Tests) | `ServiceCollectionTests` | `IFeedSearchService` (mit registriertem `HttpClient`) lässt sich auflösen — Spiegel der `MauiProgram`-Registrierung. |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `FeedsViewModelTests.CreateViewModel` | Neuer Pflicht-Konstruktorparameter `IFeedSearchService` — Übergabe des `FakeFeedSearchService`. |
| `ServiceCollectionTests.AddReporterRepositories_ResolvesAllRepositories` | Spiegelt `MauiProgram` — `HttpClient`- und `IFeedSearchService`-Registrierung ergänzen (alternativ separater neuer Test, siehe oben). |

Die übrigen bestehenden Tests (239) sind fachlich nicht betroffen.

### E2E-Tests (primärer Funktionsnachweis)

**Begründung der Nachweisform:** Das Projekt besitzt **keine UI-/E2E-Testinfrastruktur** (Bestandsaufnahme `inventory/tests.md`: keine UI-Testsuite, `SettingsViewModelTests_E2E` ist trotz Namens ein reiner xUnit-Integrationstest ohne UI-Runner; kein Emulator-/Desktop-Testlauf vorgesehen). Das Aufsetzen eines UI-Test-Frameworks (Appium o. ä.) ist nicht Teil dieser Anforderung. Als nächstbeste Nachweisform wird daher geplant: (a) **ViewModel-/Integrationstests** gegen SQLite-In-Memory decken den fachlichen Kern aller Flüsse ab (siehe Tabelle oben), und (b) **dokumentierte manuelle UI-Verifikation** gemäß AGENTS.md (390 × 844 pt, Screenshots Light/Dark, Vergleich mit `design-draft/stitch_local_rss_feed_reader/feeds_health_status/screen.png`, Dokumentation in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md`).

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Domain-/URL-Suche → Trefferliste → Tap → Confirm → Feed abonniert und in Liste sichtbar | Manuelle UI-Verifikation (Doku in `test-results.md`/`mobile-ui-design.md`) + `SearchCommand_PopulatesSearchResults` + `SubscribeResultCommand_PersistsFeedFromResult` | Nutzer kann einen gefundenen Feed direkt abonnieren | Kompletter Benutzerfluss über UI-Interaktion; VM-Tests prüfen Logik, manuelle Verifikation das tatsächliche Rendering/Tapping |
| Pflicht | Treffer ohne Titel abonnieren → Titel nach erstem Sync aus Feed-Dokument | Manuelle UI-Verifikation + `SubscribeResultCommand_WhenTitleEmpty_StoresFeedUrlAsPlaceholder` + `SyncFeedAsync_WhenTitleIsPlaceholder_UpdatesTitleFromFeedDocument` | Kein manueller Titel nötig | Zusammenspiel Persistenz ↔ erster Sync nur im Gesamtfluss sichtbar |
| Pflicht | URL-Eingabe ohne Treffer → Confirm-Dialog → „Ja" → Formular vorbefüllt → `SaveCommand` speichert | Manuelle UI-Verifikation + `SearchCommand_NoResultsAndValidUrl_InvokesConfirmDirectAdd` | Fallback auf direkte URL-Hinzufügung funktioniert | Dialog-Flow (`DisplayAlertAsync`) ist nur auf dem Gerät verifizierbar |
| Pflicht | Suche nicht erreichbar (Timeout/HTTP-Fehler beider Quellen) → Hinweis `FeedSearchUnavailable` + Fallback bleibt nutzbar | Manuelle UI-Verifikation + `SearchCommand_WhenServiceUnavailable_SetsSearchErrorMessage` + `SearchAsync_Timeout_ThrowsFeedSearchUnavailableException` | Timeout/Fehler brechen den Flow nicht | Zeitverhalten (2-s-Abbruch) und Fehlerhinweis nur real prüfbar |
| Pflicht | Offline: Suchen-`Button` deaktiviert + `FeedSearchOfflineHint` sichtbar; direkte URL-Hinzufügung weiter möglich | Manuelle UI-Verifikation + `SearchCommand_WhenOffline_DoesNotCallService_AndSetsHint` + `ConnectivityChanged_UpdatesSearchCommandCanExecute` | Offline-Verhalten der Suche | Sichtbarkeit/CanExecute nur auf UI-Ebene endgültig prüfbar |
| Pflicht | Dubletten-Treffer abonnieren → `ErrorFeedDuplicate`, kein zweiter Feed | `SubscribeResultCommand_WhenDuplicate_SetsErrorAndDoesNotAdd` (+ manuelle Sichtprüfung) | Dublettenprüfung greift auch im Such-Pfad | Anwendersichtbarer Fehlerfall |
| Optional | Trefferliste-Layout: Karten, ≥44-pt-Targets, Dark Mode, keine ScrollView-Verschachtelung, EmptyView bei „keine Treffer", Attribution sichtbar | Manuelle UI-Verifikation (Screenshots Light + Dark, 390 × 844) | Mobile-UI-Regeln + Attribution-Pflicht eingehalten | Rein visuell/layout-bezogen — nicht unit-testbar |

Bestehende E2E-Tests, die angepasst werden müssten: **Keine** (keine E2E-Suite vorhanden).

## Offene Punkte

Keine.
