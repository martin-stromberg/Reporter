<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Tests — API

## Übersicht

Die E2E-Infrastruktur exponiert zwei Schnittstellen: den HTTP-Antwortbereich des in-process `StubFeedServer` (die „API", gegen die die getestete App läuft) sowie den erweiterten `FeedSearchService`-Konstruktor und die vier Umgebungsvariablen als Prozess-Schnittstelle.

## Authentifizierung

Keine — der Stubserver bindet ausschließlich an `http://127.0.0.1:{port}` (Loopback, Port `0` = vom OS vergebener freier Port) und lebt nur für die Dauer des Testlaufs.

## Rate Limiting

Keines. `FeedSearchService.SearchAsync` arbeitet mit einem eigenen Zeitbudget (`SearchTimeout` = 2 s pro Quelle) — für den lokalen Stub ohne praktische Relevanz.

## Endpunkte des StubFeedServer

Alle Routen werden in `StubFeedServer.InitializeAsync` per `MapGet`/`MapFallback` registriert; Fixture-Dateien liegen unter `Fixtures/` und werden als `Content` in das Test-Output-Verzeichnis kopiert.

### `GET /directory`

**Beschreibung:** Imitiert die feedsearch.dev-Verzeichnis-API, auf die `FeedSearchService.SearchDirectoryAsync` zugreift. Die Antwort ist **query-abhängig**: Nur wenn `url` dem Stub-Origin (`BaseUrl`) entspricht, wird ein Treffer geliefert; alle anderen Werte erhalten ein leeres Array `[]` — damit fällt `SearchAsync` in den Autodiscovery-Zweig (`DiscoverFeedsAsync`), ohne das Directory als fehlgeschlagen zu markieren.

**Parameter:**

| Name | Typ | Pflicht | Beschreibung |
|------|-----|---------|--------------|
| `url` | Query-String | Nein | Gesuchte Website-URL; Treffer nur bei Gleichheit mit `BaseUrl` (OrdinalIgnoreCase) |

**Rückgabe:**

| Typ | Beschreibung |
|-----|--------------|
| `application/json` | Treffer: JSON-Array mit einem Objekt (`url`, `title` = „Stub Search Hit", `description`, `site_name`, `site_url`, `score` = 0.9, `bozo` = 0; URLs dynamisch aus `BaseUrl`). Sonst: `[]` |

**Beispiel:**
```
GET http://127.0.0.1:{port}/directory?url=http%3A%2F%2F127.0.0.1%3A{port}&info=true
→ 200 [{"url":"http://127.0.0.1:{port}/feeds/search-hit.xml","title":"Stub Search Hit",...}]

GET http://127.0.0.1:{port}/directory?url=http%3A%2F%2F127.0.0.1%3A{port}%2Fsite
→ 200 []
```

### `GET /feeds/{name}.xml`

**Beschreibung:** Liefert einen RSS-2.0-Feed mit `application/rss+xml`. Existiert eine dedizierte Fixture-Datei `Fixtures/{name}.xml` (z. B. `link-feed.xml`, `image-feed.xml`, `broken-image-feed.xml`, `stale-feed.xml`), wird sie ausgeliefert — so können Tests den Feed-Inhalt formen (der `link-feed`-Artikel enthält z. B. einen externen `<a href="{baseUrl}/external-link">`-Link; der `image-feed`-Artikel trägt ein `<enclosure type="image/png" url="{baseUrl}/images/article.png">` plus Inline-`<img>`, `broken-image-feed` zeigt sein Enclosure auf `/not-an-image`, `stale-feed.xml` trägt fixe `pubDate`-Werte über 30 Tage zurück für den Warnungs-Test); sonst greift das generische Template `Fixtures/stub-feed.xml`. In beiden Fällen werden die Platzhalter `{name}` (Channel-`<title>` „Stub Feed {name}") und `{baseUrl}` (`BaseUrl` des Stubs) substituiert → eindeutiger Feed-Titel je Stub-URL für die Testisolation. Die Route `/feeds/site-feed.xml` ist vor dem Template registriert und liefert das statische `Fixtures/site-feed.xml` („Stub Site Feed").

**Parameter:**

| Name | Typ | Pflicht | Beschreibung |
|------|-----|---------|--------------|
| `name` | Route-String | Ja | Feed-Name ohne `.xml`; landet im Channel-Titel |

### `GET /site`

**Beschreibung:** Liefert `Fixtures/site.html` als `text/html` — eine HTML-Seite mit `<link rel="alternate" type="application/rss+xml" href="/feeds/site-feed.xml" title="Stub Site Feed">` **im `<head>`** (die Autodiscovery von `FeedSearchService` durchsucht bei vorhandenem `<head>` nur diesen Bereich). Testobjekt für den Discovery-Fallback.

### `GET /empty`

**Beschreibung:** Liefert `Fixtures/empty.html` (`text/html`) — eine Seite ohne Feed-Verweise; dient als negativer Discovery-Pfad.

### `GET /external-link`

**Beschreibung:** Liefert eine kleine statische HTML-Seite (`text/html`) und inkrementiert dabei `StubFeedServer.ExternalLinkHitCount` (`Interlocked`-gezählt). Die Route ist das Ziel des externen Links im `link-feed`-Artikel: Da der System-Browser die URL nach `Browser.OpenAsync` abruft, belegt der Zähler im Test `ArticleLinkTests.ExternalLinkInArticle_OpensSystemBrowser`, dass die Navigation die App verlassen hat statt im WebView zu laufen.

### `GET /images/{name}.png`

**Beschreibung:** Liefert unabhängig vom Namen ein festes 64×48-PNG (`image/png`) aus der eingebetteten Base64-Konstante `StubFeedServer.StubImageBase64` — groß genug, damit die UIA-Prüfung des gerenderten Bildes (≥ 40 px) in `ArticleImageTests.ArticleImage_StoredLocally_AndShownOnCardAndDetail` stabil ist. Die Route ist das Enclosure-/Inline-Bildziel des `image-feed`-Stubs.

**Parameter:**

| Name | Typ | Pflicht | Beschreibung |
|------|-----|---------|--------------|
| `name` | Route-String | Ja | Beliebiger Bildname; dient nur der eindeutigen URL, die Antwort ist immer dasselbe PNG |

### `GET /not-an-image`

**Beschreibung:** Liefert `text/plain` („this is not an image") — bewusst kein Bild. Die Route ist das Enclosure-Ziel des `broken-image-feed`-Stubs und treibt damit den MIME-Reject-Pfad des Bild-Downloads in `ArticleImageTests.BrokenImage_DoesNotFailSync_StoresNoImage`: Der Abruf läuft erfolgreich, es wird kein Bild gespeichert und die Karte bleibt auf der Ersatz-Kaskade.

### Übrige Pfade

`MapFallback` antwortet mit **404** — Well-Known-Feed-Sonden (`/feed`, `/rss`, `/rss.xml`, `/atom.xml`, `/feed.xml`, `/index.xml`) und Favicon-Lookups des `FeedIconService` laufen damit in ein definiertes, schnelles Leeres.

## Programmierschnittstellen

### `FeedSearchService(HttpClient httpClient, string? directoryEndpoint = null)`

**Beschreibung:** Optionaler Endpoint-Override für die Feed-Verzeichnis-Abfrage. `null`/leer → die interne Konstante `DirectoryEndpoint` (`https://feedsearch.dev/api/v1/search`). `SearchDirectoryAsync` baut die Request-URI `{_directoryEndpoint}?url={query}&info=true&favicon=false&opml=false&skip_crawl=true`. `IFeedSearchService` ist unverändert; bestehende Aufrufstellen kompilieren ohne Änderung.

**Fehler:**

| Code / Exception | Ursache |
|------------------|---------|
| `InvalidDataException` | Directory-Antwort ist kein JSON-Array (`SearchDirectoryAsync`) |
| `FeedSearchUnavailableException` | Directory **und** Autodiscovery fehlgeschlagen (`SearchAsync`) |

### `E2EProcessGuard` (intern, `src/Reporter.E2ETests/E2EProcessGuard.cs`)

**Beschreibung:** Statische Hilfsklasse für den Lebenszyklus der von der Suite gestarteten `Reporter.exe`-Instanzen. Alle Methoden sind bewusst nie-werfend ausgelegt — der Guard ist die letzte Verteidigungslinie im Teardown und darf das eigentliche Testergebnis nicht maskieren; Probleme werden als `[E2E]`-Meldungen auf die Konsole geschrieben. Gedeckt durch `E2EProcessGuardTests` im selben Projekt (bewusst ohne `Category=E2E`-Trait → laufen im regulären `dotnet test` mit).

|| Methode | Rückgabe | Verhalten |
||---------|----------|-----------|
|| `TrackProcess(Process)` | `void` | Weist den Prozess einem prozessweiten Windows-Job-Objekt mit `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` zu (lazy erzeugt via kernel32-P/Invoke `CreateJobObject`/`SetInformationJobObject`/`AssignProcessToJobObject`). Best-effort: Scheitert die Zuweisung (z. B. Test-Host läuft selbst in einem restriktiven Job), wird eine `[E2E]`-Warnung geschrieben — Teardown und Skript-`finally` bleiben zweite Linie. Das Job-Handle bleibt absichtlich offen: Es schließt erst beim Test-Host-Exit — genau dann wirkt `KILL_ON_JOB_CLOSE`. |
|| `KillAndWaitAsync(Process, TimeSpan? = null)` | `Task<bool>` | `Kill(entireProcessTree: true)` (nur wenn `!HasExited`), dann `WaitForExitAsync` begrenzt auf `timeout` (Default 30 s). `true` = Exit bestätigt; Timeout/Fehler → `[E2E]`-Warnung + `false`. |
|| `KillAndWaitAsync(int processId, TimeSpan? = null)` | `Task<bool>` | Wie oben, löst die PID vorher per `Process.GetProcessById` auf — nicht (mehr) existierende PIDs gelten als bestätigter Exit. Wird verwendet, wenn nur die PID zuverlässig ist (FlaUI kann das `Process`-Objekt beim Attach ersetzen). |
|| `TryDeleteDirectoryAsync(string)` | `Task` | Rekursives `Directory.Delete` mit bis zu fünf Versuchen à 200 ms — eine frisch gekillte App kann Dateien (z. B. die SQLite-DB) kurz nach dem bestätigten Exit noch sperren. Wirft nie; ein verbleibender Leichnam ist toleriert. |

## Umgebungsvariablen (Prozess-Schnittstelle)

| Variable | Gelesen von | Validierung | Wirkung |
|----------|-------------|-------------|---------|
| `REPORTER_FEEDSEARCH_ENDPOINT` | `MauiProgram.CreateMauiApp` → `ResolveFeedSearchEndpoint` | Nur absolute `http`/`https`-URIs (`Uri.TryCreate`, `UriKind.Absolute`); ungültig/leer → `null` → Default-Endpunkt | Feed-Verzeichnis-Anfragen gehen an den Override statt `feedsearch.dev` |
| `REPORTER_DB_PATH` | `MauiProgram.CreateMauiApp` | Nur `IsNullOrWhiteSpace`; `Directory.CreateDirectory` legt das Verzeichnis an | Vollständiger Dateipfad der SQLite-Nutzerdaten-DB statt `FileSystem.AppDataDirectory/reporter.db`; die Content-DB `reporter-content.db` wird aus dem absoluten Verzeichnis dieses Pfads abgeleitet und liegt damit daneben |
| `REPORTER_DISABLE_DEMO_SEED` | `MauiProgram.CreateMauiApp` → `ResolveDemoSeedSuppressed` | Gesetzt und weder `"0"` noch `"false"` (`OrdinalIgnoreCase`) → unterdrückt; leer/`0`/`false` → Seed aktiv | Macht `IDemoContentService.EnsureSeededAsync` zum No-op — die App legt auf der frischen DB keine Demo-Kategorie/keinen Demo-Feed an |
| `REPORTER_APP_PATH` | `ReporterAppFixture.ResolveAppPath` | Muss auf eine existierende Datei zeigen, sonst Konventionspfad `src/Reporter/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Reporter.exe` (Vorfahren-Suche ab `AppContext.BaseDirectory`) | Pfad zur zu testenden `Reporter.exe` |
