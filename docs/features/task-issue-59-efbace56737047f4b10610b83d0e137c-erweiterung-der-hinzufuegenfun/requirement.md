<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung — Feed-Suche über feedsearch.dev und clientseitige Autodiscovery statt reiner URL-Eingabe

> **Hinweis zur Issue-Formulierung:** Das zugrunde liegende Issue #59 beschreibt die Suche ursprünglich als Verzeichnissuche gegen „RSS Atlas" (rssatlas.com) mit Freitext-/Stichwortsuche. Der Anwender hat sich in der Klärungsrunde **bewusst dagegen** entschieden (siehe Abschnitt „Geklärte Punkte / Entscheidungen"): Als Suchdienst dient **feedsearch.dev** (`GET https://feedsearch.dev/api/v1/search?url=…`), ergänzt um **clientseitige Feed-Autodiscovery** (HTML-Link-Tags und gängige Standardpfade der eingegebenen Website). Es gibt **keine Freitext-/Stichwortsuche** — Treffer entstehen nur bei Domain- oder URL-Eingabe.

## Fachliche Zusammenfassung

Der bisherige Hinzufügen-Dialog auf der `FeedsPage` (`FeedsViewModel.NewUrl`/`NewTitle` + `SaveCommand`) wird um eine Feed-Suche erweitert. Das Eingabefeld akzeptiert Domains und vollständige URLs; die Eingabe wird gegen die öffentliche API **feedsearch.dev** und — falls dort nichts gefunden wird — per clientseitiger Autodiscovery gegen die Website selbst geprüft. Die Treffer werden als scrollbare, kartenbasierte Liste angezeigt, aus der der Nutzer einen Feed direkt auswählen und abonnieren kann. Die bisherige direkte URL-Hinzufügung bleibt als Fallback erhalten — bei leerem Trefferergebnis, ungültiger URL oder nicht erreichbarer Suche.

## Betroffene Klassen und Komponenten

### Datenmodellklassen (`Reporter.Core/Models`)

- **Neu:** Modellklasse für einen Suchtreffer, `FeedSearchResult` — mit den von feedsearch.dev gelieferten bzw. in der UI benötigten Feldern `Title`, `Description`, `SiteName`, `SiteUrl`, `FeedUrl` (API-Feld `url`), `Score` sowie einer Trefferart-Information für die Sortierung. Reines In-Memory-Modell.
- **Neu:** Enum `FeedSearchMatchKind` für die Trefferart (exakter URL-Treffer, Verzeichnis-Treffer, Autodiscovery-Treffer), damit eine stabile, deterministische Relevanzsortierung abbildbar und testbar ist.
- `Feed` bleibt unverändert; beim Abonnieren eines Treffers wird wie bisher ein `Feed`-Datensatz über `IFeedRepository.AddAsync` angelegt. `Feed.Title` wird beim ersten Sync aus dem Feed-Dokument befüllt (siehe Entscheidung 2).

### Logikklassen / Services (`Reporter.Core/Services`)

- **Neu:** Such-Service `FeedSearchService` — kapselt den HTTP-Zugriff auf feedsearch.dev (Parameter `info=true&favicon=false&opml=false&skip_crawl=true`), die clientseitige Autodiscovery (`<link rel="alternate">`-Auswertung, Standardpfade), die Abbildung auf `FeedSearchResult` und die Relevanzsortierung. Nutzt den per DI registrierten `HttpClient` (`MauiProgram`, 30 s Timeout); für das 2-Sekunden-Ziel ist ein gelinktes `CancellationTokenSource` pro Suchaufruf vorgesehen.
- `FeedSyncService`: **kleine Erweiterung** — beim ersten Sync wird `Feed.Title` aus `SyndicationFeed.Title` befüllt, sofern der gespeicherte Titel ein Platzhalter ist (siehe Entscheidung 2; heute schreibt `UpdateFeedHealthAsync` den Titel unverändert zurück).

### Interfaces (`Reporter.Core/Interfaces`)

- **Neu:** `IFeedSearchService` mit `SearchAsync(string query, CancellationToken)`, das eine geordnete `IReadOnlyList<FeedSearchResult>` liefert und bei Nichterreichbarkeit `FeedSearchUnavailableException` wirft.
- `IFeedRepository` unverändert (`GetByUrlAsync` für die Dublettenprüfung, `AddAsync` zum Speichern).

### Enums

- `FeedSearchMatchKind` (neu, siehe Datenmodellklassen).

### UI-Komponenten / ViewModels

- `FeedsViewModel` (`Reporter.Core/ViewModels`): Erweiterung um Such-Eigenschaften und -Befehle — `SearchResults` (`ObservableCollection<FeedSearchResult>`), `ShowSearchResults`, `IsSearching`, `SearchErrorMessage`/`HasSearchError`, `SearchCommand` sowie `SubscribeResultCommand`. Der bestehende `SaveCommand`/`SaveAsync`-Pfad bleibt für die direkte URL-Hinzufügung und das Bearbeiten unverändert (inkl. Titel-Pflicht `ErrorFeedTitleEmpty`).
- `FeedsPage.xaml` (`src/Reporter/Views`): Das bisherige URL-Eingabefeld wird zum Such-/URL-Eingabefeld erweitert; darunter eine scrollbare `CollectionView` mit kartenbasierten Treffer-Einträgen (Titel, Beschreibung, Site-Name/Host, Feed-URL) mit `TapGestureRecognizer` zum Abonnieren — entsprechend den Mobile-UI-Regeln in `AGENTS.md` (keine horizontalen Tabellen, Touch-Targets ≥ 44 pt, `AppThemeBinding`). Zusätzlich eine „powered by feedsearch.dev"-Attribution im Trefferbereich (Nutzungsbedingung der API).
- `FeedsPage.xaml.cs`: Behandlung der Treffer-Auswahl sowie der Confirm-Dialoge für die Fallback-Fälle (Muster: `DisplayActionSheetAsync`/`DisplayAlertAsync` wie in `OnFeedTapped`).
- `AppResources.resx` / `AppResources.de.resx` (`Reporter.Core/Resources/Strings`): neue lokalisierte Texte — Such-Placeholder, „Kein Feed gefunden. Möchtest du die eingegebene URL direkt hinzufügen?", Hinweis bei nicht erreichbarer Suche, Offline-Hinweis für deaktivierte Suche, Abonnieren-Bestätigung, Attribution.

### Tests (`src/Reporter.Tests`)

- **Neu:** `FeedSearchServiceTests` — Mapping, Autodiscovery-Pfade, Sortierung, Fehlerfälle, Timeout — mit `HttpClient` auf gemocktem `HttpMessageHandler`.
- **Neu:** `FakeFeedSearchService` als Test-Double nach dem Muster der vorhandenen `Fake*`-Klassen.
- **Erweiterung:** `FeedsViewModelTests` um Suche, Trefferauswahl, Dublettenprüfung und Offline-Verhalten; `FeedSyncServiceTests` um die Titel-Befüllung beim ersten Sync.
- `ServiceCollectionTests`: Registrierung des neuen Services prüfen.

## Implementierungsansatz

- **Eingabe-Klassifikation:** Im ViewModel wird die Eingabe klassifiziert — gültige absolute http(s)-URL (`Uri.TryCreate`, gleiche Prüfung wie heute in `SaveAsync`), Domain-artige Eingabe (wird zu `https://…` normalisiert) oder Freitext. Nur URLs und Domains gehen an den Such-Service; Freitext führt direkt zum „Kein Feed gefunden"-Fallback (bewusste Scope-Entscheidung, siehe Entscheidung 1).
- **Suche:** `SearchCommand` ruft `IFeedSearchService.SearchAsync` auf. Der Service fragt zuerst feedsearch.dev mit `skip_crawl=true` ab (nur gespeicherte Feeds → schnell); liefert das Verzeichnis keine Treffer oder ist es nicht erreichbar, folgt die clientseitige Autodiscovery: HTML der eingegebenen Website abrufen, `<link rel="alternate" type="application/rss+xml|application/atom+xml|application/feed+json">` aus dem `<head>` parsen, ggf. gängige Standardpfade (`/feed`, `/rss`, `/atom.xml`, `/feed.xml`, `/index.xml`) probieren. Beide Quellen teilen ein 2-s-Zeitbudget pro Suchaufruf. Sortierung deterministisch nach `MatchKind`, dann `Score`.
- **Selektion als eigener Fachpunkt:** Der Nutzer wählt einen Treffer explizit aus (Tap auf die Trefferkarte) und abonniert diesen nach Bestätigungsdialog direkt — es wird nicht die erste gefundene URL automatisch übernommen. Beim Abonnieren wird `Feed.Url` aus `FeedSearchResult.FeedUrl` gesetzt; `Feed.Title` wird aus dem Treffer übernommen oder — falls leer — als Platzhalter (`FeedUrl`) gespeichert und beim ersten Sync aus dem Feed-Dokument ersetzt (Entscheidung 2). `CategoryId` kommt aus dem Formular-`SelectedCategory` (Default „Keine Kategorie" → `null`). Die Dublettenprüfung über `IFeedRepository.GetByUrlAsync` gilt weiterhin.
- **URL-Fallback:** Findet die Suche bei URL-Eingabe keinen Treffer, wird die eingegebene URL weiterhin akzeptiert und der bisherige Anlege-Dialog (Titel, Kategorie, Benachrichtigungen) mit vorbefüllter URL angeboten. Findet die Suche mehrere Feeds derselben Domain, werden alle in der Trefferliste angezeigt.
- **Fehlerfälle:** Leere Trefferliste + gültige URL → Confirm-Dialog „Kein Feed gefunden. Möchtest du die eingegebene URL direkt hinzufügen?"; ungültige URL/Freitext → keine Direkt-Hinzufügung über den Dialog (das Formular validiert weiter mit `ErrorFeedUrlInvalid`); nicht erreichbare Suche (Timeout/HTTP-Fehler beider Quellen) → Hinweis + Fallback auf direkte URL-Hinzufügung.
- **Offline-Verhalten:** Über `BaseViewModel.IsOnline`/`INetworkStatusService` wird die Suche offline deaktiviert (`SearchCommand.CanExecute`); die direkte URL-Eingabe/-Hinzufügung bleibt möglich. `OnConnectivityChanged` wird entsprechend erweitert.
- **DI-Registrierung:** `IFeedSearchService` als Singleton in `MauiProgram.CreateMauiApp()` nach dem Muster der bestehenden Services.

## Konfiguration

- Die Anforderung verlangt keine Nutzer-Konfiguration. Basis-URL von feedsearch.dev und das 2-s-Timeout werden als Konstanten im Service gehalten, nicht in `Settings`.

## Geklärte Punkte / Entscheidungen

Die folgenden Punkte wurden in der Klärungsrunde vom Anwender entschieden und gelten als verbindlich:

1. **Suchdienst:** **Nicht** RSS Atlas, sondern **feedsearch.dev** (`GET https://feedsearch.dev/api/v1/search?url=…&info=true&favicon=false&opml=false`) plus **clientseitige Feed-Autodiscovery** (HTML der eingegebenen Website abrufen, `<link rel="alternate" type="application/rss+xml|application/atom+xml|application/feed+json">` aus dem `<head>` parsen, ggf. gängige Standardpfade probieren). **Bewusste Scope-Entscheidung:** Es gibt keine Freitext-/Stichwortsuche — Treffer entstehen nur bei Domain- oder URL-Eingabe; Freitext-Eingaben landen im „Kein Feed gefunden"-Fallback. Dies ist eine bewusste Abweichung vom Issue-Wortlaut („RSS Atlas", Freitext).
2. **Feed-Titel:** Beim Abonnieren eines Suchtreffers wird der Titel beim ersten Sync aus dem Feed-Dokument gelesen — der Nutzer muss keinen Titel manuell eingeben. Da `Feed.Title` Pflichtfeld ist und `FeedSyncService` den Titel heute nicht schreibt, wird beim Abonnieren ein Platzhalter (`FeedUrl`) gespeichert und `FeedSyncService` ersetzt Platzhalter-Titel beim ersten Sync durch `SyndicationFeed.Title`. Der unveränderte `SaveAsync`-Pfad (direkte URL-Hinzufügung) verlangt weiterhin `NewTitle` (`ErrorFeedTitleEmpty`).
3. **Kategorie:** Kein Auto-Mapping und kein automatisches Anlegen lokaler Kategorien — `CategoryId` kommt aus dem Formular-`SelectedCategory` (Default „Keine Kategorie" → `null`). Verzeichnis-Metadaten (feedsearch.dev liefert ohnehin keine Kategorien, dafür `site_name`) werden nur in der Trefferkarte angezeigt.
4. **Beschreibung:** Nur in der Trefferkarte anzeigen, nicht persistieren — kein `Feed.Description`, keine Migration.
5. **Timeout:** 2-s-`CancellationTokenSource` pro Suchaufruf; Abbruch → Hinweis „nicht erreichbar" + URL-Fallback. Da feedsearch.dev unbekannte Domains live crawlt (kann > 2 s dauern), wird `skip_crawl=true` genutzt — unbekannte Domains deckt die eigene Autodiscovery ab.
6. **Offline:** `SearchCommand` ist offline deaktiviert (bestehendes `IsOnline`-CanExecute-Muster); der bisherige `SaveCommand`-Pfad für direkte URL-Hinzufügung bleibt unverändert.
7. **Optionale Erweiterungen** (Autovervollständigung, Kategorie-Vorschläge, Such-Historie): Nicht Teil dieses Issues — Folgeaufgaben.
