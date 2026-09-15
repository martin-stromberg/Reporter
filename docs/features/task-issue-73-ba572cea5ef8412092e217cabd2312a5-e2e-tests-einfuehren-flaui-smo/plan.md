<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: E2E-Tests einführen — FlaUI-Smoke-Suite + Compiled Bindings (Issue #73)

## Übersicht

Drei gestufte Maßnahmen: (1) Compiled Bindings via `x:DataType` auf allen XAML-Views nachrüsten, die noch keinen typisierten Seitenkontext haben — laut Bestandsaufnahme sind das `FeedsPage.xaml`, `CategoriesPage.xaml`, `ArticleCardView.xaml` **und zusätzlich auf Seitenebene** `UnreadPage.xaml`, `LaterPage.xaml`, `SettingsPage.xaml`; (2) `FeedSearchService.DirectoryEndpoint` konfigurierbar machen und in `MauiProgram` per Umgebungsvariable übersteuerbar, plus `REPORTER_DB_PATH` für die Datenisolation der E2E-App; (3) neues Testprojekt `src/Reporter.E2ETests` mit in-process Kestrel-Test-Webserver, App-Prozess-Fixture und einer FlaUI-Smoke-Suite (7 Tests) für die kritischen UI-Pfade (Add-Sheet, Direkt-Add, Umbenennen, Kategorie ändern, Suche/Abo). Manuelle Screenshot-Verifikation bleibt bestehen.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Compiled-Bindings-Umfang | Alle Views ohne Seiten-`x:DataType` typisieren: `FeedsPage`, `CategoriesPage`, `UnreadPage`, `LaterPage`, `SettingsPage` (Seitenebene) sowie `ArticleCardView` (ContentView-Ebene) | Bestandsaufnahme hat verifiziert, dass die drei Seiten nur typisierte `DataTemplate`s, aber unkompilierte Seitenbindungen haben; das Issue verlangt explizit „prüfen, ob weitere Views ohne kompilierte Bindings sind" |
| Test-Webserver | Eigener in-process Server auf **Kestrel**-Basis via `<FrameworkReference Include="Microsoft.AspNetCore.App" />` im Testprojekt, Port `0` | Kein neues NuGet-Paket nötig (Framework-Reference statt `WireMock.Net`); `HttpListener` scheidet aus, weil er unter Windows HTTP.sys-URLACL-Reservierungen braucht, die ohne Admin-Rechte scheitern — Kestrel umgeht das. Antwortfläche ist klein und statisch (fachliche Vorlage `stubserver.py` plus feedsearch-JSON-Endpunkt) |
| `DirectoryEndpoint`-Override | Optionaler Konstruktorparameter `FeedSearchService(HttpClient, string? directoryEndpoint = null)`; `MauiProgram` liest ausschließlich die Umgebungsvariable `REPORTER_FEEDSEARCH_ENDPOINT` (kein Kommandozeilenparameter, keine Beschränkung auf Debug-Builds) und registriert via Factory-Lambda | Rückwärtskompatibel: `FeedSearchServiceTests`, `ServiceCollectionTests` und `FakeFeedSearchService` bleiben unverändert kompilierfähig; `IFeedSearchService` unverändert. Umgebungsvariable statt Kommandozeilenparsing, weil die unverpackte Exe (`WindowsPackageType=None`) Env-Vars direkt erbt und das Fixture sie beim Prozessstart setzt; die URI-Validierung federt Fehlkonfigurationen ab, sodass eine Build-Beschränkung entbehrlich ist |
| E2E-Datenisolation | Zweite Umgebungsvariable `REPORTER_DB_PATH` (vollständiger Dateipfad), gelesen in `MauiProgram` vor `Directory.CreateDirectory` | Der Test „Feed landet in SQLite" braucht eine bekannte, isolierte DB; ohne Override würde die Suite die produktive Entwickler-Datenbank unter `FileSystem.AppDataDirectory` lesen und verändern. Kein Benutzer-Setting — nur Prozess-Start-Ebene |
| Testprojekt-Anbindung | `src/Reporter.E2ETests/` (`net10.0-windows10.0.19041.0`, xunit analog `Reporter.Tests.csproj`), Aufnahme in `Reporter.sln`, alle Tests mit `[Trait("Category", "E2E")]`, Ausführung über neues `scripts/Run-E2ETests.ps1` | sln-Aufnahme sorgt dafür, dass `Run-StaticChecks.ps1` (Restore, Format, Lizenzheader, Security, TreatWarningsAsErrors-Build) das Projekt mit abdeckt. Der CI-Befehl `dotnet test src/Reporter.Tests/Reporter.Tests.csproj` läuft die Suite nicht mit; für lokale `dotnet test Reporter.sln`-Läufe wird `--filter "Category!=E2E"` dokumentiert |
| App-Prozess-Fixture | Fixture startet eine **vorher gebaute** `Reporter.exe` (Konvention: `bin/Debug/net10.0-windows10.0.19041.0/win-x64/Reporter.exe`, übersteuerbar via `REPORTER_APP_PATH`); Build erfolgt im Skript, nicht im Fixture. FlaUI **UIA3** | Trennung Build/Test hält Testläufe schnell und Fehler zuordenbar; Debug-Konvention entspricht lokaler Entwicklung. UIA3 ist die von Microsoft für WinUI 3 empfohlene UIA-Version (UIA2 deckt WinUI-3-Controls nicht vollständig ab) |
| Lokalisierte UI-Texte in Tests | `Reporter.E2ETests` referenziert `Reporter.Core` und löst erwartete Beschriftungen über `AppResources.*` auf | App-Default `language="system"` übernimmt die OS-Kultur; der Testprozess läuft auf derselben Maschine mit derselben Kultur → deterministische Namen ohne String-Duplikation und ohne DB-Seeding der Sprache |
| CI-Einbindung | Kein CI-Workflow — nur dokumentierter lokaler/on-demand-Aufruf über `scripts/Run-E2ETests.ps1` plus Hinweis in `CONTRIBUTING.md` | Das Issue ordnet die Suite selbst als „lokale/on-demand" ein; die erforderliche interaktive Desktop-Session ist auf CI-Runnern nicht gegeben |
| Issue-59-Vorarbeiten | `test-results/issue-59/uia.ps1`, `stubserver.py` und `probe.fsx` bleiben unverändert im Repo | Historische Verifikationsartefakte ohne laufende Kosten; `probe.fsx` wird ohnehin nicht ausgeführt |
| Feed-URLs im Test | `http://127.0.0.1:{port}/...` | `FeedTitleFallback` funktioniert mit IP-/Port-URLs; `*.localhost` nur als optionale Verschönerung, `.test`/`.local` ausdrücklich nicht (Issue-Vorgabe) |
| E2E-Testdaten (Setup der Aktions-Tests) | **UI-Seeding pro Test:** Jeder Test, der einen vorhandenen Feed voraussetzt (`FeedActionSheet_Rename_UpdatesTitle`, `FeedActionSheet_ChangeCategory_IncludingNone`), legt ihn zu Testbeginn selbst über den Direkt-Add-UI-Flow mit einer eigenen Stub-URL (`/feeds/{eindeutiger-name}.xml`) an; Kategorien werden über den Kategorien-Tab per UI angelegt. Kein DB-Seeding, keine Testreihenfolge-Abhängigkeit | xunit garantiert keine Methodenreihenfolge — UI-Seeding macht die Tests zustandsunabhängig und in beliebiger Reihenfolge lauffähig. DB-Seeding scheidet aus: Die Temp-DB existiert vor dem App-Start (`Migrate()`/`MigrateAsync`) noch nicht, und ein eigenes Schema-Setup über `Microsoft.Data.Sqlite` koppelt die Suite an EF-Migrationsdetails. Der Direkt-Add-Flow ist selbst Testgegenstand und damit ein gesicherter Setup-Baustein; ein UI-Add-Vorschalt-Test deckt zudem die Karte-in-Liste-Vorbedingung mit ab |
| E2E-Testisolation | Serielle Ausführung über die eine `E2ETestCollection` (xunit parallelisiert Tests innerhalb einer Collection nicht), **ohne** `ITestCaseOrderer`; stattdessen: eindeutige Testdaten je Test (eigene Feed-URL → eigener Feed-Titel via Stub-`<title>`-Substitution), zustandsagnostische Asserts und UI-Reset am Testende (Add-Sheet/Dialog schließen, Feeds-Tab aktiv) | Eine erzwungene Reihenfolge koppelt Tests implizit (läuft `DirectAdd_…` vor `AppStarts_…`, bricht der `EmptyView`-Assert) und ist bei Suite-Erweiterung fehleranfällig. `AppStarts_FeedListRenders` prüft daher zustandsagnostisch: Feed-Liste gerendert, nachgewiesen über Kindelemente — `EmptyView`-Platzhalter (`PlaceholderFeeds`) **oder** mindestens eine Feed-Karte (die `CollectionView` selbst trägt keinen `x:Name`/Automation-Anker) — beides sind zulässige gerenderte Zustände |
| `BindingContext.*`-`x:Reference`-Bindings | **Alle** `BindingContext.*`-`x:Reference`-Hops bleiben unverändert Runtime-Bindings — das gilt auch für `FeedsPage` Z. 176/191: Beide liegen **innerhalb** des `Feeds`-`DataTemplate` (Z. 145–298), also im Item-Kontext `models:FeedListItem`, nicht im Seitenscope. Ebenso `UnreadPage` Z. 123/197–199, `LaterPage` Z. 46–48, `SettingsPage` Z. 97 | `BindingContext` ist als `object` deklariert — Compiled Bindings können die Member dahinter nicht auflösen und fallen mit XamlC-Warnung auf Runtime-Binding zurück. Für `FeedsPage` Z. 176/191 ist eine Ersetzung durch `{Binding IsOnline}` zusätzlich falsch: Sie würde nach `x:DataType="models:FeedListItem"` gegen den Item-Typ kompilieren, der kein `IsOnline`-Member besitzt (XamlC-Fehler bzw. stiller Bruch der Favicon-/Initial-Trigger). Ohne Umstrukturierung (Member aufs Item-Modell verlagern) gibt es in allen diesen Fällen keine typierte Variante — Funktionalität bleibt per Runtime-Binding korrekt. Da diese Hops bereits heute innerhalb typisierter `DataTemplate`s existieren, ohne den Build zu brechen, wird erwartet, dass die Warnung unter `TreatWarningsAsErrors` nicht als Fehler durchschlägt — der Build-Schritt verifiziert das explizit (Fallback: `WarningsNotAsErrors` auf den konkreten XC-Code) |
| Stub-Feed-Dokumente | `GET /feeds/{name}.xml` liefert das RSS-Template `stub-feed.xml` mit `{name}`-Substitution im Channel-`<title>` (und `<link>`); `site-feed.xml` bleibt statisch | Jeder Test kann eine eigene Feed-URL mit eigenem Titel anlegen → eindeutige Karten-Identifikation über `SemanticProperties.Description` (= Titel) ohne Kollisionen zwischen den Tests und ohne Rückgriff auf Reihenfolge |
| Stub-`GET /directory`-Antwortverhalten | **Query-abhängig:** Die Route wertet den `url`-Query-Parameter aus. Nur für bekannte Werte aus einer kleinen Directory-Tabelle (Eintrag: Stub-Origin `{BaseUrl}` → Feed-URL `{BaseUrl}/feeds/search-hit.xml`) liefert sie ein JSON-Array mit einem Treffer im feedsearch-Format; für alle anderen `url`-Werte (insb. `{BaseUrl}/site` und `{BaseUrl}/empty`) antwortet sie HTTP 200 mit **leerem JSON-Array `[]`** — nicht 404 | `FeedSearchService.SearchAsync` ruft `SearchDirectoryAsync` immer zuerst auf (`FeedSearchService.cs` Z. 74) und erreicht `DiscoverFeedsAsync` nur bei `results.Count == 0` (Z. 84–88) — eine statisch nicht-leere `/directory`-Antwort würde den Discovery-Pfad unerreichbar machen und `Search_SiteUrl_DiscoversFeedViaLinkTag` könnte nie ein `Discovered`-Ergebnis sehen. Das leere Array wird von `SearchDirectoryAsync` als „keine Treffer" geparst (Z. 124–162) ohne `directoryFailed` zu setzen; ein 404 löste den Fallback zwar über `EnsureSuccessStatusCode` (Z. 114) ebenfalls aus, markierte das Directory aber als fehlgeschlagen — bei gleichzeitigem Discovery-Fehler würde `SearchAsync` `FeedSearchUnavailableException` werfen (Z. 96–99). `[]` entspricht der realen feedsearch-Semantik und ist die robustere Variante |
| E2E-Tab-Navigation (Arrange) | Eigener Helfer `SelectTab(localizedTabTitle)` in `SmokeTests`: wählt einen Shell-`TabBar`-Eintrag per UIA-Name (lokalisierte `AppResources.Tab*`-Titel) aus und wartet auf das Rendern der Zielseite. **Jeder** Test, der Feeds-Seiten-Elemente erwartet, ruft `SelectTab(AppResources.TabFeeds)` als Arrange-Schritt zu Testbeginn auf; `AddFeedViaUiAsync` und `ResetUiState` nutzen denselben Helfer | Die App startet auf dem **Unread-Tab** (`AppShell.xaml.cs` fügt `unreadTab` zuerst zum `TabBar` hinzu, Z. 24/40 — kein `GoToAsync`/Start-Navigation). Die Feed-`CollectionView` ist im Startzustand nicht sichtbar; ohne explizite Navigation würden alle Feeds-Tests an ihrer Vorbedingung scheitern. Tab-Aktivierung pro Test (statt einmalig im Fixture) hält die Tests zustandsunabhängig — auch nach einem abgebrochenen Vorgänger — und nutzt denselben Benutzerfluss wie ein Anwender |

## Programmabläufe

### App-Start mit E2E-Overrides (`MauiProgram.CreateMauiApp`)

1. `Environment.GetEnvironmentVariable("REPORTER_DB_PATH")` lesen; wenn nicht leer, als `databasePath` verwenden, sonst `Path.Combine(FileSystem.AppDataDirectory, "reporter.db")` wie bisher; `Directory.CreateDirectory` unverändert.
2. `Environment.GetEnvironmentVariable("REPORTER_FEEDSEARCH_ENDPOINT")` lesen; mit `Uri.TryCreate` auf absolute `http`/`https`-URI validieren; ungültig oder leer → `null` (Default greift).
3. `AddDbContextFactory<ReporterDbContext>` mit dem ermittelten `databasePath` registrieren (unverändert).
4. `IFeedSearchService` per Factory registrieren: `HttpClient` aus dem Container holen, `FeedSearchService` mit Endpoint-Override (oder `null`) erzeugen.
5. Restliche Registrierungen, `ApplyPersistedLanguage` und `App.OnStart` (Migration, Theme, Connectivity, AutoRefresh) laufen unverändert — `ApplyPersistedLanguage`/`Migrate` arbeiten damit automatisch gegen die isolierte Test-DB.

Beteiligte Klassen/Komponenten: `MauiProgram`, `FeedSearchService`, `ReporterDbContext`.

### E2E-Fixture-Lebenszyklus (xunit Collection-Fixture)

1. `StubFeedServer`: Kestrel-`WebApplication` auf `http://127.0.0.1:0` starten; nach `StartAsync` den vergebenen Port auslesen; `BaseUrl`/`DirectoryUrl` bereitstellen. Routen:
   - `GET /directory?url={query}&…` → **query-abhängig:** Der `url`-Parameter wird ausgewertet. Für bekannte Werte aus der Stub-Directory-Tabelle (Eintrag: Stub-Origin `{BaseUrl}` → Feed-URL `{BaseUrl}/feeds/search-hit.xml`) liefert die Route ein JSON-Array mit einem Treffer im feedsearch-Format (`url`, `title`, `description`, `site_name`, `site_url`, `score`, `bozo: 0`; URLs dynamisch aus `BaseUrl` erzeugt). Für alle anderen `url`-Werte (insb. `{BaseUrl}/site` und `{BaseUrl}/empty`) → HTTP 200 mit leerem JSON-Array `[]`. Hintergrund: `SearchAsync` ruft `SearchDirectoryAsync` zuerst auf und fällt nur bei `results.Count == 0` auf `DiscoverFeedsAsync` zurück (`FeedSearchService.cs` Z. 74, 84–88) — das leere Array signalisiert „keine Treffer" ohne `directoryFailed`-Flag, sodass die `/site`-Query tatsächlich im Discovery-Zweig landet
   - `GET /feeds/{name}.xml` → RSS-Fixture-Dokument (`stub-feed.xml` als Template, `{name}` im Channel-`<title>` substituiert; `Content-Type: application/rss+xml`) — eindeutiger Feed-Titel je Stub-URL
   - `GET /site` → `site.html` mit `Content-Type: text/html` (`DiscoverFeedsAsync` wertet den Medientyp aus, `FeedSearchService.cs` Z. 189) und `<link rel="alternate" type="application/rss+xml" href="/feeds/site-feed.xml" title="…">` **im `<head>`** (Autodiscovery-Pfad; `ExtractFeedLinks` durchsucht bei vorhandenem `<head>` nur diesen Bereich, Z. 250–255; das `title`-Attribut liefert einen stabilen `DisplayTitle` für die Ergebniskarte)
   - `GET /empty` (+ alle Well-Known-Pfade `/feed`, `/rss`, …) → leere HTML-Seite bzw. 404 (Direct-Add-Fallback; `ProbeStandardPathsAsync`-Sonden laufen ins Leere)
   - übrige Pfade (Favicon-Sonden von `FeedIconService`) → 404
2. `ReporterAppFixture`: Temp-Verzeichnis anlegen; `Process.Start` der `Reporter.exe` mit Umgebungsvariablen `REPORTER_FEEDSEARCH_ENDPOINT={DirectoryUrl}` und `REPORTER_DB_PATH={tempDir}\reporter.db`; per FlaUI (`UIA3Automation`, `Application.Attach`) auf das Hauptfenster warten (Retry, großzügiger Timeout — erster Start inkl. Migration); `MainWindow` und `DatabasePath` an Tests bereitstellen.
3. Tests finden Elemente über Name (`SemanticProperties.Description`, lokalisierte `AppResources`-Texte) bzw. `AutomationId` (`x:Name` wird von MAUI als AutomationId propagiert); Dialoge (`DisplayActionSheetAsync`, `DisplayPromptAsync`, `DisplayAlertAsync`) erscheinen unter WinUI 3 als modale Elemente im UIA-Baum des Fensters/Desktop und werden primär per UIA-Name/ControlType lokalisiert — zeigt sich ein Dialog darüber nicht erreichbar, wird er als Fallback über Fenster-/Popup-Roots gesucht.
4. SQLite-Verifikation: `FeedDbAssertions` öffnet die Temp-`reporter.db` per `Microsoft.Data.Sqlite` und prüft `feeds`-Zeilen — **rein lesend**, kein Seeding (EF Core hält keine Dauerverbindung → lesende Zugriffe sind unproblematisch).
5. Teardown: App-Prozess killen, `UIA3Automation`/`Application` disposen, `StubFeedServer` stoppen, Temp-Verzeichnis löschen.

Beteiligte Klassen/Komponenten: `StubFeedServer`, `ReporterAppFixture`, `E2ETestCollection`, `UiRetry`, `FeedDbAssertions`, `SmokeTests`.

### Testzustand und Isolation (E2E)

1. Alle sieben Tests liegen in `SmokeTests` in der einen `E2ETestCollection` → serielle Ausführung, ein App-Prozess, eine Temp-DB, ein `StubFeedServer`.
2. **Kein `ITestCaseOrderer`** — Tests sind zustandsunabhängig: Jeder Test, der einen vorhandenen Feed braucht, stellt ihn selbst per UI-Direkt-Add über `AddFeedViaUiAsync("{eindeutiger-name}")` her; `FeedActionSheet_ChangeCategory_IncludingNone` legt zusätzlich eine Kategorie über den Kategorien-Tab per UI an.
3. Eindeutigkeit der Testdaten: Jeder Test verwendet eine eigene Stub-URL (`/feeds/direct-add.xml`, `/feeds/rename-target.xml`, `/feeds/category-target.xml`, …); der Stub substituiert `{name}` in den Feed-`<title>` → die eigene Karte ist per `SemanticProperties.Description` eindeutig auffindbar, unabhängig davon, welche Feeds frühere Tests angelegt haben.
4. **Start-Tab-Vorbedingung:** Die App startet auf dem **Unread-Tab** (`AppShell.xaml.cs` Z. 24/40 — `unreadTab` wird zuerst zum `TabBar` hinzugefügt); die Feed-`CollectionView` ist im Startzustand nicht sichtbar. Jeder Test, der Feeds-Seiten-Elemente erwartet (`AppStarts_FeedListRenders`, `AddButton_OpensSheet_FocusesUrlEntry`, `DirectAdd_…`, beide `FeedActionSheet_…`-Tests, beide `Search_…`-Tests), aktiviert den Feeds-Tab zu Testbeginn per `SelectTab(AppResources.TabFeeds)` — der Helfer wählt den `TabBar`-Eintrag per UIA-Name aus und wartet auf das Rendern der Zielseite. `AddFeedViaUiAsync` beginnt ebenfalls mit diesem Schritt; `FeedActionSheet_ChangeCategory_IncludingNone` nutzt `SelectTab(AppResources.TabCategories)` für die Kategorie-Anlage und `SelectTab(AppResources.TabFeeds)` für die Rückkehr.
5. `AppStarts_FeedListRenders` ist zustandsagnostisch: Arrange = `SelectTab(AppResources.TabFeeds)`; Assert = Feed-Liste gerendert — die Feeds-`CollectionView` trägt weder `x:Name` noch `SemanticProperties.Description` (`FeedsPage.xaml` Z. 142), der Nachweis läuft daher über deren Kindelemente: `EmptyView`-Platzhalter (`PlaceholderFeeds`) ODER mindestens eine Feed-Karte per `SemanticProperties.Description` (= Titel) — lauffähig in beliebiger Reihenfolge.
6. UI-Reset am Testende: Geöffnete Add-Sheets werden geschlossen (`AccessibilityDismissSheet`-Fläche bzw. `CloseAddFormCommand`-Button), Dialoge bestätigt/abgebrochen, der Feeds-Tab wird per `SelectTab(AppResources.TabFeeds)` aktiviert — der nächste Test startet aus einem definierten UI-Zustand. Persistente Daten (angelegte Feeds/Kategorien) bleiben bewusst bestehen; sie stören dank eindeutiger Testdaten nicht.

### Suche + Abonnieren gegen den Stub (Smoke-Test)

1. Feed-Liste: „+"-Button (`AppResources.ActionAddFeed`) per InvokePattern → `ShowAddForm = true` → `OnViewModelPropertyChanged` setzt Fokus auf `NewUrlEntry`.
2. **Explizite URL-Eingabe:** `NewUrlEntry` = `{BaseUrl}` (vollständige `http://`-Stub-URL). Freitext ist keine zulässige Eingabe: `TryResolveSearchUrl` lehnt Nicht-URLs ab (`ShowSearchResults` mit leerem Ergebnis, kein Service-Call; `FeedsViewModel.Search.cs` Z. 125–131, 204–215), und nackte `127.0.0.1:{port}`-Eingaben würden via `TryNormalizeDomainUrl` zu `https://…` normalisiert — nur `http://`-URLs erreichen `SearchAsync` unverändert.
3. `ButtonSearch` → `SearchCommand` → `FeedSearchService.SearchAsync` → `SearchDirectoryAsync` fragt `GET {DirectoryUrl}?url={BaseUrl}&…` am Stub → die Stub-Directory-Tabelle enthält den Origin → Treffer-Eintrag mit `url = {BaseUrl}/feeds/search-hit.xml` (`MatchKind.Directory`, da `feedUrl != query`) → Ergebniskarten erscheinen (`ShowSearchResults`, `SemanticProperties.Description` = `DisplayTitle`).
4. Tap auf Ergebniskarte → `OnSearchResultTapped` → `DisplayAlertAsync` (`ConfirmSubscribeFeedTitle`) → `ButtonYes` → `SubscribeResultCommand` → `TryPersistNewFeedAsync` → Feed in `CollectionView` und in der Test-DB nachweisbar; Favicon-/Sync-Abrufe laufen gegen denselben Stub (`FeedSyncService`/`FeedIconService` teilen den DI-`HttpClient`).

### Autodiscovery-Fallback gegen den Stub (Smoke-Test)

1. Add-Sheet öffnen, `NewUrlEntry` = `{BaseUrl}/site` (vollständige `http://`-URL, dieselbe Eingaberegel wie oben).
2. `ButtonSearch` → `SearchCommand` → `SearchAsync` → `SearchDirectoryAsync` fragt `GET {DirectoryUrl}?url={BaseUrl}/site&…` → der Stub kennt die Query nicht → leeres Array `[]` → `results.Count == 0` → `DiscoverFeedsAsync` lädt `GET {BaseUrl}/site` → `site.html` liefert das `<link rel="alternate">` im `<head>` → Ergebnis mit `MatchKind.Discovered` (`FeedSearchService.cs` Z. 84–88, 189–205).
3. Assert: Ergebniskarte sichtbar (`SemanticProperties.Description` = `DisplayTitle` = `title`-Attribut des Link-Tags bzw. die aufgelöste Feed-URL `{BaseUrl}/feeds/site-feed.xml`); die `feeds/site-feed.xml`-URL führt beim anschließenden Abo auf ein reales Stub-Feed-Dokument.

### Direkt-Add + Persistenz (Smoke-Test)

1. Add-Sheet öffnen, `NewUrlEntry` = `http://127.0.0.1:{port}/feeds/direct-add.xml` (eigene Stub-URL des Tests), `ButtonDirectAdd` → `DirectAddCommand` → `TryPersistNewFeedAsync` persistiert sofort.
2. Assert: Feed-Karte in der `CollectionView` sichtbar; `feeds`-Tabelle der Temp-DB enthält die URL; der Stub liefert beim Sync das RSS-Dokument → Titel-Auflösung (`FeedTitleFallback` bzw. Feed-Titel).

### Feed-Aktionen über ActionSheet/Prompt (Smoke-Tests)

**Umbenennen (`FeedActionSheet_Rename_UpdatesTitle`):**

1. Setup im Test: `AddFeedViaUiAsync("rename-target")` — Add-Sheet öffnen, `NewUrlEntry` = `{BaseUrl}/feeds/rename-target.xml`, `ButtonDirectAdd` → eigene Feed-Karte (Titel aus Stub-`<title>`) in der `CollectionView` sichtbar.
2. Tap auf die eigene Feed-Karte (`SemanticProperties.Description` = Titel) → `OnFeedTapped` → `DisplayActionSheetAsync` mit `ButtonRefresh`/`ButtonRename`/`ButtonChangeCategory`/`ButtonEdit`/`ButtonDelete`.
3. „Umbenennen" (`ButtonRename`) → `DisplayPromptAsync` → neuen Titel eintippen → `ButtonOk` → `RenameFeedAsync` → Karte zeigt neuen Titel.

**Kategorie ändern (`FeedActionSheet_ChangeCategory_IncludingNone`):**

1. Setup im Test: Kategorie über den Kategorien-Tab per UI anlegen (`CategoryNameEntry` + `SaveCommand`), zurück auf den Feeds-Tab; `AddFeedViaUiAsync("category-target")` — eigene Feed-Karte in der Liste.
2. Tap auf die eigene Feed-Karte → `OnFeedTapped` → „Kategorie ändern" → `ChangeCategoryAsync` listet `FeedsViewModel.Categories` inkl. Pseudo-Eintrag `CategoryNone` (`Id == Guid.Empty`) → Auswahl der angelegten Kategorie → `ChangeFeedCategoryAsync` → Kategorie-Name auf der Karte.
3. Erneut „Kategorie ändern" → Auswahl `CategoryNone` → Zuordnung entfernt (Kategorie-Name verschwindet von der Karte).

## Neue Klassen

Alle neuen Klassen liegen im Projekt `src/Reporter.E2ETests` (Namespace `Reporter.E2ETests`).

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `StubFeedServer` | Klasse (`IAsyncLifetime`/`IDisposable`) | In-process Kestrel-Server auf Port `0`; Routen `/directory` (**query-abhängig:** Treffer-Tabelle für bekannte `?url=`-Werte — Stub-Origin → `feeds/search-hit.xml` —, sonst leeres `[]`), `/feeds/{name}.xml` (Template mit `{name}`-Substitution im `<title>`), `/site` (`<link rel="alternate">` im `<head>`), `/empty`, Well-Known-/Favicon-Fallbacks; exponiert `BaseUrl`, `DirectoryUrl` |
| `ReporterAppFixture` | Klasse (`IAsyncLifetime`/`IDisposable`) | Startet `Reporter.exe` mit `REPORTER_FEEDSEARCH_ENDPOINT`/`REPORTER_DB_PATH`; FlaUI-`Application`/`UIA3Automation`/`MainWindow`; killt Prozess und räumt Temp-DB auf |
| `E2ETestCollection` | xunit `CollectionDefinition` | Teilt `StubFeedServer` + `ReporterAppFixture` über alle Smoke-Tests (serielle Ausführung — ein App-Prozess; kein `ITestCaseOrderer`, Tests sind zustandsunabhängig) |
| `UiRetry` | statische Helferklasse | `WaitForElement`-Polling mit Timeout/Retry (Name, AutomationId, ControlType), Invoke-/Value-Pattern-Helfer, ActionSheet-/Dialog-Helfer — Kapselung der FlaUI-Flaky-Anfälligkeit |
| `FeedDbAssertions` | statische Helferklasse | **Rein lesende** SQLite-Abfragen gegen die Temp-`reporter.db` (`feeds`-Tabelle: Feed mit URL/Titel vorhanden) via `Microsoft.Data.Sqlite` — kein DB-Seeding |
| `SmokeTests` | Testklasse | Die 7 FlaUI-Smoke-Tests (siehe Tests-Abschnitt) plus private Helfer `SelectTab(localizedTabTitle)` (Shell-`TabBar`-Eintrag per UIA-Name auswählen und auf das Rendern der Zielseite warten — Arrange-Schritt, da die App auf dem Unread-Tab startet), `AddFeedViaUiAsync(stubName)` (UI-Direkt-Add als Test-Setup inkl. Assert auf die neue Karte) und `ResetUiState` (Add-Sheet/Dialog schließen, Feeds-Tab aktivieren) |
| `scripts/Run-E2ETests.ps1` | PowerShell-Skript | Baut `Reporter.csproj` (Debug, Windows-TFM), setzt `REPORTER_APP_PATH`, führt `dotnet test src/Reporter.E2ETests` aus; dokumentiert die interaktive-Session-Voraussetzung |

Fixture-Content (als `Content`-Items mit `CopyToOutputDirectory`): `Fixtures/stub-feed.xml` (RSS-2.0-Template mit `{name}`-Platzhalter im Channel-`<title>`), `Fixtures/site-feed.xml` (für `/site`-Discovery, statisch), `Fixtures/site.html` (mit `<link rel="alternate" type="application/rss+xml" href="/feeds/site-feed.xml" title="…">` **im `<head>`** — `ExtractFeedLinks` durchsucht bei vorhandenem `<head>` nur diesen Bereich), `Fixtures/empty.html`. Die Directory-Antwort wird dynamisch aus `BaseUrl` und dem `url`-Query-Parameter erzeugt (Treffer-Tabelle: Stub-Origin → `feeds/search-hit.xml`; unbekannte Queries → leeres `[]`; URLs müssen auf den Laufzeit-Port zeigen) — keine statische JSON-Datei.

## Änderungen an bestehenden Klassen

### `FeedSearchService` (Klasse, `src/Reporter.Core/Services/FeedSearchService.cs`)

- **Neue Felder:** `_directoryEndpoint` (`string`) — effektiver Directory-Endpunkt.
- **Geänderte Methoden:** Konstruktor `FeedSearchService(HttpClient)` → `FeedSearchService(HttpClient httpClient, string? directoryEndpoint = null)`; weist `directoryEndpoint` zu, Fallback auf die bisherige Konstante `DirectoryEndpoint` (bleibt als Default erhalten).
- **Geänderte Methoden:** `SearchDirectoryAsync` (Zeile 110–112) — baut `requestUri` aus `_directoryEndpoint` statt der Konstanten.

### `MauiProgram` (statische Klasse, `src/Reporter/MauiProgram.cs`)

- **Geänderte Methoden:** `CreateMauiApp` —
  - Zeile 43: `databasePath` zuvor aus `REPORTER_DB_PATH` lesen (`string.IsNullOrWhiteSpace`-Check, sonst Default).
  - Zeile 56: Registrierung `AddSingleton<IFeedSearchService, FeedSearchService>()` → Factory-Lambda, das `HttpClient` aus dem Container auflöst und den validierten `REPORTER_FEEDSEARCH_ENDPOINT`-Wert (oder `null`) an den Konstruktor übergibt.
- **Neue Hilfslogik:** Endpoint-Validierung (`Uri.TryCreate` + Scheme `http`/`https`) als `private static`-Methode.

### XAML-Views (Compiled Bindings)

- **`FeedsPage.xaml`:** `xmlns:models="clr-namespace:Reporter.Core.Models;assembly=Reporter.Core"` neu; `x:DataType="vm:FeedsViewModel"` auf `ContentPage`; `x:DataType="models:FeedSearchResult"` im `SearchResults`-`DataTemplate` (Zeile 70), `x:DataType="models:FeedListItem"` im `Feeds`-`DataTemplate` (Zeile 145). Die `x:Reference Page`-Hops `BindingContext.IsOnline` (Zeilen 176/191) liegen **innerhalb** des `Feeds`-`DataTemplate` im Item-Kontext und bleiben **unverändert Runtime-Bindings** — `FeedListItem` besitzt kein `IsOnline`-Member, eine Ersetzung durch `{Binding IsOnline}` würde gegen den Item-Typ kompilieren und fehlschlagen bzw. die Favicon-/Initial-Visibility-Trigger (Zeilen 167–201) still brechen; `CommandParameter="{Binding .}"` bleibt unverändert.
- **`CategoriesPage.xaml`:** `xmlns:vm` und `xmlns:models` neu deklarieren; `x:DataType="vm:CategoriesViewModel"` auf `ContentPage`; `x:DataType="models:CategoryWithCount"` im `DataTemplate` (Zeile 40).
- **`ArticleCardView.xaml`:** `xmlns:models` neu; `x:DataType="models:ItemListItem"` auf `ContentView`. Die `x:Reference Card`-Bindings (Zeilen 85, 183, 212, 238) binden direkte `BindableProperty`s des Views (`IsOnline`, `ToggleSavedCommand`, `MarkReadCommand`, `OpenArticleCommand`) und kompilieren gegen den `ArticleCardView`-Typ — bleiben unverändert.
- **`UnreadPage.xaml`:** `xmlns:vm="clr-namespace:Reporter.Core.ViewModels;assembly=Reporter.Core"` neu; `x:DataType="vm:UnreadViewModel"` auf `ContentPage` — damit kompilieren die direkten Seitenbindungen (`MarkAllReadCommand`, `IsSyncing`, `UnreadCountText` u. a.). Die `x:Reference PageRoot`-Hops innerhalb der `DataTemplate`s (`BindingContext.SelectCategoryCommand` Zeile 123, `BindingContext.ToggleSavedCommand`/`MarkReadCommand`/`OpenArticleCommand` Zeilen 197–199) bleiben bewusst Runtime-Bindings: `BindingContext` ist als `object` deklariert, die Member dahinter sind für Compiled Bindings nicht auflösbar (Fallback mit XamlC-Warnung — funktional korrekt).
- **`LaterPage.xaml`:** `xmlns:vm` neu; `x:DataType="vm:LaterViewModel"` auf `ContentPage`; `x:Reference PageRoot`-Hops in Zeilen 46–48 bleiben Runtime-Bindings (siehe `UnreadPage`).
- **`SettingsPage.xaml`:** `xmlns:vm` neu; `x:DataType="vm:SettingsViewModel"` auf `ContentPage`; `BindingContext.RemoveKeywordCommand, Source={x:Reference PageRoot}` (Zeile 97, im `DataTemplate`) bleibt Runtime-Binding.
- **Compile-Fehler und XamlC-Warnungen bereinigen/prüfen:** Beim Setzen der `x:DataType`s können latente Binding-Fehler sichtbar werden (Tippfehler, falsche Typen, fehlende Member) — diese werden im selben Schritt korrigiert; `TargetNullValue`/`FallbackValue`/`StringFormat`-Angaben auf Nullable-Membern (`SiteName`, `CategoryName`, `LastCheckedAt`, `PublishedAt`) sind unverändert zulässig. Für die verbleibenden `BindingContext.*`-Runtime-Bindings wird im Windows-TFM-Build geprüft, ob die XamlC-Warnungen unter `TreatWarningsAsErrors` (`Run-StaticChecks.ps1`) als Fehler durchschlagen — da diese Hops bereits heute innerhalb typisierter `DataTemplate`s existieren, ohne den Build zu brechen, wird eine Warnung ohne Fehlerwirkung erwartet; schlagen sie doch durch, werden sie über `WarningsNotAsErrors` auf den konkreten XC-Code gezielt entschärft.

### `FeedSearchServiceTests` (`src/Reporter.Tests/`)

- **Neue Methoden:** `Ctor_CustomDirectoryEndpoint_RequestsOverrideHost` — `new FeedSearchService(new HttpClient(handler), "http://localhost:9/dir")`, `SearchAsync` aufrufen, über `StubHttpMessageHandler.RequestedUrls` prüfen, dass der Directory-Request gegen den Override-Host ging.

### `Reporter.sln`

- `Reporter.E2ETests`-Projekt in die `src`-Solution-Folder aufnehmen.

### `CONTRIBUTING.md`

- Hinweis ergänzen: E2E-Suite läuft über `scripts/Run-E2ETests.ps1`; den bestehenden `dotnet test Reporter.sln`-Befehl (Zeile 22) konkret um den Filter ergänzen → `dotnet test Reporter.sln --filter "Category!=E2E"` (damit die dokumentierte Anleitung nach sln-Aufnahme des E2E-Projekts nicht in eine nicht lauffähige Suite führt).

## Datenbankmigrationen

Keine. `REPORTER_DB_PATH` ändert nur den Dateipfad; das Schema wird wie bisher per `Migrate`/`MigrateAsync` angelegt.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `REPORTER_FEEDSEARCH_ENDPOINT` (Env) | Muss absolute URI mit Scheme `http` oder `https` sein (`Uri.TryCreate`, `UriKind.Absolute`) | Ungültig/leer → Wert ignorieren, Default-Endpunkt verwenden (kein App-Fehler) |
| `REPORTER_DB_PATH` (Env) | Nur `string.IsNullOrWhiteSpace`-Prüfung; `Directory.CreateDirectory` legt das Verzeichnis an | Ungültiger Pfad → Ausnahme beim App-Start ist bewusst sichtbar (nur Test-/Dev-Override) |
| `REPORTER_APP_PATH` (Env des Test-Fixtures) | Datei muss existieren, sonst Konventionspfad `bin/Debug/.../win-x64/Reporter.exe` | Beide Pfade fehlen → Fixture bricht mit klarer Fehlermeldung ab („App zuerst bauen, siehe Run-E2ETests.ps1") |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `REPORTER_FEEDSEARCH_ENDPOINT` | Umgebungsvariable (App-Prozess) | leer → `https://feedsearch.dev/api/v1/search` | E2E-App auf den Stub-Endpunkt zeigen; kein `settings`-Datensatz (kein Anwender-Feature) |
| `REPORTER_DB_PATH` | Umgebungsvariable (App-Prozess) | leer → `FileSystem.AppDataDirectory/reporter.db` | Isolierte SQLite-DB pro E2E-Lauf |
| `REPORTER_APP_PATH` | Umgebungsvariable (Test-Fixture) | `src/Reporter/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Reporter.exe` | Pfad zur zu testenden Exe |
| `[Trait("Category", "E2E")]` | xunit-Attribut auf allen E2E-Tests | — | Trennung vom regulären `dotnet test`-Lauf |
| `Reporter.E2ETests.csproj` | neues Testprojekt | `net10.0-windows10.0.19041.0`, `IsPackable=false`, `GenerateDocumentationFile` + `WarningsAsErrors CS1591` (Konvention aus `Reporter.Tests.csproj`) | Pakete: `FlaUI.UIA3`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `Microsoft.Data.Sqlite`; `FrameworkReference Microsoft.AspNetCore.App`; `ProjectReference Reporter.Core` |

## Seiteneffekte und Risiken

- **Compiled Bindings:** Das Setzen von `x:DataType` kann latente Binding-Fehler zu Compile-Fehlern machen (beabsichtigt) — und in seltenen Fällen zuvor still fehlschlagende Laufzeit-Bindings ändern. Nach dem XAML-Schritt ist ein Windows-Build plus kurzer App-Smoke nötig; `MauiXamlInflator=SourceGen` ist bereits aktiv.
- **`FeedSearchService`-Konstruktor:** Optionaler Parameter → alle Aufrufstellen (`MauiProgram`, `FeedSearchServiceTests` Zeile 17, `ServiceCollectionTests` Zeilen 51–52, `probe.fsx`) kompilieren unverändert; `FakeFeedSearchService` unberührt.
- **`MauiProgram`-Registrierung:** Wechsel auf Factory-Lambda — `ServiceCollectionTests.AddReporterServices_ResolvesFeedSearchService` spiegelt weiterhin `AddSingleton<IFeedSearchService, FeedSearchService>()` und bleibt gültig (optionaler Parameter löst auf).
- **Env-Overrides in Release:** Ein versehentlich gesetzter `REPORTER_FEEDSEARCH_ENDPOINT`/`REPORTER_DB_PATH` wirkt auch im Release-Build. Akzeptiertes Risiko (nur dokumentierte Dev-/Test-Variablen); URI-Validierung verhindert wirksam falsche Werte beim Endpoint.
- **`dotnet test Reporter.sln`:** läuft künftig auch das E2E-Projekt — ohne gebaute Exe/interaktive Session schlagen diese Tests fehl. Durch `Category=E2E`-Trait + dokumentierten Filter + `Run-E2ETests.ps1` abgefedert; CI ist unbetroffen (testet nur `Reporter.Tests.csproj`).
- **Static Checks:** Neues Projekt wird von Restore/Format/Lizenzheader/Security/TreatWarningsAsErrors mitgeprüft → neue Dateien brauchen Lizenzheader und XML-Doc-Kommentare (CS1591 als Fehler); `FlaUI.UIA3`-Paketversion muss den Vulnerability-Scan bestehen.
- **Flaky-Anfälligkeit:** UIA-Timing/Fokus → `UiRetry`-Polling und großzügige Timeouts sind Pflichtbestandteil, keine Nachbesserung.
- **Geteilter Suite-Zustand:** Alle E2E-Tests teilen App-Prozess und Temp-DB — abgefedert über eindeutige Testdaten je Test (eigene Stub-URL/Titel), UI-Reset am Testende, zustandsagnostische Asserts und den `SelectTab`-Arrange-Schritt zu Testbeginn (die App startet auf dem Unread-Tab; ein abgebrochener Vorgänger kann den Tab beliebig hinterlassen); `AppStarts_FeedListRenders` deckt den „leere Liste"-Fall (`EmptyView`) nur ab, wenn es vor den mutierenden Tests läuft — akzeptiert, da „Liste rendert" das Kriterium ist und beide Zustände ihn belegen.
- **Nur Windows/UIA:** Keine Aussage über iOS/Android; interaktive Desktop-Session erforderlich (lokale/on-demand-Suite, kein CI-Job).

## Umsetzungsreihenfolge

1. **Compiled Bindings in allen sechs Views setzen**
   - Voraussetzungen: Keine (`MauiXamlInflator=SourceGen` aktiv; `xmlns:vm` in `FeedsPage` vorhanden).
   - Beschreibung: `x:DataType`/fehlende `xmlns:vm`/`xmlns:models` in `FeedsPage.xaml`, `CategoriesPage.xaml`, `ArticleCardView.xaml`, `UnreadPage.xaml`, `LaterPage.xaml`, `SettingsPage.xaml` ergänzen; sämtliche `BindingContext.*`-`x:Reference`-Hops innerhalb der `DataTemplate`s (`FeedsPage` Z. 176/191 — liegen im Item-Kontext `FeedListItem`, nicht im Seitenscope —, `UnreadPage`, `LaterPage`, `SettingsPage`) bewusst unverändert als Runtime-Bindings belassen; auftretende Compile-Fehler beheben; prüfen, ob die XamlC-Warnungen der Runtime-Fallbacks unter `TreatWarningsAsErrors` (`Run-StaticChecks.ps1`) als Fehler durchschlagen — ggf. `WarningsNotAsErrors` auf den konkreten XC-Code; Verifikation `dotnet build src/Reporter/Reporter.csproj -f net10.0-windows10.0.19041.0` + kurzer manueller Smoke (App startet, Listen rendern, Favicon-/Initial-Anzeige auf den Feed-Karten unverändert).
2. **`FeedSearchService` um Endpoint-Parameter erweitern**
   - Voraussetzungen: Keine.
   - Beschreibung: `_directoryEndpoint`-Feld + optionaler Ctor-Parameter; `SearchDirectoryAsync` nutzt das Feld; neuen Unit-Test `Ctor_CustomDirectoryEndpoint_RequestsOverrideHost` in `FeedSearchServiceTests` ergänzen.
3. **`MauiProgram` Env-Overrides (`REPORTER_DB_PATH`, `REPORTER_FEEDSEARCH_ENDPOINT`)**
   - Voraussetzungen: Schritt 2 (Konstruktorparameter muss existieren).
   - Beschreibung: DB-Pfad-Override vor `Directory.CreateDirectory`; Endpoint lesen + validieren; `IFeedSearchService`-Factory-Registrierung.
4. **Testprojekt `src/Reporter.E2ETests` anlegen**
   - Voraussetzungen: Keine (NuGet-Pakete `FlaUI.UIA3`, `Microsoft.Data.Sqlite`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` werden mit dem Projekt eingeführt; `FrameworkReference Microsoft.AspNetCore.App` ist Teil des installierten SDKs).
   - Beschreibung: csproj nach `Reporter.Tests.csproj`-Konvention, sln-Eintrag, `Fixtures/`–Content-Dateien (`stub-feed.xml`, `site-feed.xml`, `site.html`, `empty.html`).
5. **`StubFeedServer` implementieren**
   - Voraussetzungen: Schritt 4.
   - Beschreibung: Kestrel-Server auf Port `0` mit den Routen aus dem Programmablauf; `BaseUrl`/`DirectoryUrl`; **`/directory` wertet den `url`-Query-Parameter aus** — Treffer-Tabelle für bekannte Werte (Stub-Origin `{BaseUrl}` → `{BaseUrl}/feeds/search-hit.xml`, dynamisch aus `BaseUrl`), für unbekannte Queries HTTP 200 mit leerem `[]` (damit `SearchAsync` bei 0 Directory-Treffern in `DiscoverFeedsAsync` fällt, `FeedSearchService.cs` Z. 74, 84–88); `stub-feed.xml` als Template mit `{name}`-Substitution im Channel-`<title>` (eindeutige Feed-Titel je Stub-URL für die Testisolation); `site.html` mit `<link rel="alternate">` im `<head>`.
6. **`ReporterAppFixture`, `E2ETestCollection`, `UiRetry`, `FeedDbAssertions` implementieren**
   - Voraussetzungen: Schritte 3, 4, 5 (Env-Overrides müssen in der App wirken).
   - Beschreibung: Prozess-Start mit Env, FlaUI-Attach, Fenster-Wait, SQLite-Assertions, Retry-Helfer, Collection-Definition mit `Trait("Category","E2E")`-Konvention.
7. **Smoke-Tests implementieren (7 Tests in `SmokeTests`)**
   - Voraussetzungen: Schritte 1, 5, 6 (stabilisierte Bindings, Server, Fixture).
   - Beschreibung: Zuerst die privaten Helfer `SelectTab(localizedTabTitle)`, `AddFeedViaUiAsync(stubName)` und `ResetUiState` implementieren (Navigations-/Setup-/Reset-Bausteine; `SelectTab` wählt den Shell-`TabBar`-Eintrag per UIA-Name und wartet auf das Rendern der Zielseite). Dann die Tests gemäß Abschnitt „Testzustand und Isolation": App-Start → `SelectTab(AppResources.TabFeeds)` → Feed-Liste (zustandsagnostisch, Assert über Kindelemente — `EmptyView`-Text oder Feed-Karte, da die `CollectionView` keinen `x:Name` trägt); „+"-Sheet + Fokus (Arrange: Feeds-Tab aktivieren; Sheet am Ende schließen); Direkt-Add → Liste + SQLite; Umbenennen-Prompt (Setup per `AddFeedViaUiAsync("rename-target")`); Kategorie-ActionSheet inkl. `CategoryNone` (Kategorie per `SelectTab(AppResources.TabCategories)` anlegen, zurück per `SelectTab(AppResources.TabFeeds)`, Setup per `AddFeedViaUiAsync("category-target")`); Suche mit expliziter `http://`-Stub-URL `{BaseUrl}` (kein Freitext — `TryResolveSearchUrl`) → Directory-Treffer `feeds/search-hit.xml` → Ergebnis-Tap → Abo-Alert → Persistenz; Autodiscovery mit `{BaseUrl}/site` (Directory liefert `[]` → `DiscoverFeedsAsync`-Fallback → `Discovered`-Ergebnis). Die App startet auf dem Unread-Tab — jeder Test, der Feeds-Elemente erwartet, beginnt mit `SelectTab(AppResources.TabFeeds)` als Arrange; `AddFeedViaUiAsync` enthält diesen Schritt. Jeder Test verwendet eine eigene Stub-URL — kein `ITestCaseOrderer`.
8. **`scripts/Run-E2ETests.ps1` + `CONTRIBUTING.md`-Hinweis**
   - Voraussetzungen: Schritte 4–7.
   - Beschreibung: Skript baut Debug-Windows-TFM, setzt `REPORTER_APP_PATH`, ruft `dotnet test src/Reporter.E2ETests`; CONTRIBUTING dokumentiert den Skript-Aufruf und ergänzt den `dotnet test Reporter.sln`-Befehl konkret zu `dotnet test Reporter.sln --filter "Category!=E2E"`.
9. **Gesamtverifikation**
   - Voraussetzungen: Schritte 1–8.
   - Beschreibung: `dotnet test src/Reporter.Tests` (unverändert grün, 491+1 Tests), E2E-Lauf via Skript (interaktive Session), `.\scripts\Run-StaticChecks.ps1` mit Exit-Code 0 (inkl. Lizenzheader aller neuen Dateien), manuelle UI-Verifikation nach AGENTS.md bleibt dokumentiert.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Ctor_CustomDirectoryEndpoint_RequestsOverrideHost` | `FeedSearchServiceTests` (`Reporter.Tests`) | Directory-Request geht an den Override-Endpunkt (`RequestedUrls` des `StubHttpMessageHandler`) |
| `UiRetry.WaitForElement` / Invoke-/Value-Helfer | `UiRetry` (`Reporter.E2ETests`) | Element-Suche mit Polling/Timeout gegen Flaky-Anfälligkeit |
| `FeedDbAssertions.FeedExistsAsync` | `FeedDbAssertions` (`Reporter.E2ETests`) | `feeds`-Zeile mit URL/Titel in der Temp-`reporter.db` (rein lesend) |
| `StubFeedServer` | Fixture | `BaseUrl`/`DirectoryUrl`, Feed-/Site-/Directory-/Empty-Routen, `{name}`-Titelsubstitution, query-abhängige `/directory`-Route (Treffer-Tabelle vs. leeres `[]`) |
| `ReporterAppFixture` | Fixture | App-Prozess mit Env-Overrides, FlaUI-`MainWindow`, `DatabasePath`, Teardown |
| `SelectTab` / `AddFeedViaUiAsync` / `ResetUiState` | `SmokeTests` (private Helfer) | `SelectTab(localizedTabTitle)`: Shell-`TabBar`-Eintrag per UIA-Name (`AppResources.Tab*`) auswählen und auf das Rendern der Zielseite warten — Arrange-Baustein aller Feeds-Tests (App startet auf Unread-Tab) sowie der Kategorie-Anlage; `AddFeedViaUiAsync`: UI-Direkt-Add als Test-Setup (beginnt mit `SelectTab(TabFeeds)`, eigene Stub-URL → eigene Feed-Karte); `ResetUiState`: UI-Reset am Testende (Sheet/Dialog schließen, Feeds-Tab per `SelectTab` aktivieren) |
| `AppStarts_FeedListRenders` | `SmokeTests` | Arrange: `SelectTab(AppResources.TabFeeds)` (App startet auf Unread-Tab); Assert: Hauptfenster + Feed-Liste gerendert — die `CollectionView` hat keinen `x:Name`/Automation-Anker (`FeedsPage.xaml` Z. 142), Nachweis über Kindelemente, zustandsagnostisch: `EmptyView`-Platzhalter (`PlaceholderFeeds`) ODER Feed-Karte per `SemanticProperties.Description` |
| `AddButton_OpensSheet_FocusesUrlEntry` | `SmokeTests` | Arrange: `SelectTab(AppResources.TabFeeds)`; `OpenAddFormCommand`-Verdrahtung + `NewUrlEntry.Focus()` via `OnViewModelPropertyChanged`; Sheet am Testende schließen |
| `DirectAdd_FeedAppearsInListAndDatabase` | `SmokeTests` | Arrange: `SelectTab(AppResources.TabFeeds)`; `DirectAddCommand` mit eigener Stub-URL → Karte in `CollectionView` + `feeds`-Zeile in SQLite |
| `FeedActionSheet_Rename_UpdatesTitle` | `SmokeTests` | Setup per `AddFeedViaUiAsync("rename-target")` (inkl. `SelectTab(TabFeeds)`); dann `OnFeedTapped` → ActionSheet → `DisplayPromptAsync` → `RenameFeedAsync` → neuer Titel sichtbar |
| `FeedActionSheet_ChangeCategory_IncludingNone` | `SmokeTests` | Setup: Kategorie per `SelectTab(AppResources.TabCategories)` anlegen, zurück per `SelectTab(AppResources.TabFeeds)` + `AddFeedViaUiAsync("category-target")`; dann `ChangeCategoryAsync` mit echter Kategorie und `CategoryNone` |
| `Search_SubscribesResult_PersistsFeed` | `SmokeTests` | Arrange: `SelectTab(AppResources.TabFeeds)`; Eingabe `{BaseUrl}` (explizite `http://`-Stub-URL — `TryResolveSearchUrl` lehnt Freitext ab) → `SearchCommand` → Stub-Directory-Treffer `feeds/search-hit.xml` (`MatchKind.Directory`) → `OnSearchResultTapped` → `DisplayAlertAsync` → `SubscribeResultCommand` → Liste + DB |
| `Search_SiteUrl_DiscoversFeedViaLinkTag` | `SmokeTests` | Arrange: `SelectTab(AppResources.TabFeeds)`; Eingabe `{BaseUrl}/site` → Stub-Directory liefert `[]` für die unbekannte Query → `DiscoverFeedsAsync`-Fallback → `<link rel="alternate">` im `<head>` von `site.html` liefert `Discovered`-Ergebnis |

### Betroffene bestehende Tests

Keine. Der `FeedSearchService`-Konstruktor bleibt rückwärtskompatibel (optionaler Parameter); `ServiceCollectionTests`, `FeedsViewModelTests` und alle übrigen der 491 Tests kompilieren und laufen unverändert.

### E2E-Tests (primärer Funktionsnachweis)

Die Suite ist selbst das Feature: Sie weist die bisher ungetestete Ebene (XAML-Bindings, Code-Behind-Verdrahtung, `DataTrigger`, Dialoge) nach. Alle sieben Szenarien sind Pflicht, da sie die vom Issue genannten kritischen Pfade abdecken und nur über die echte UI erreichbar sind.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|---------------------|
| Pflicht | App startet (Unread-Tab aktiv), Navigation zum Feeds-Tab, Feed-Liste rendert (zustandsagnostisch: `EmptyView` oder Karten) | `SmokeTests.AppStarts_FeedListRenders` | Issue: „App startet, Feed-Liste rendert" | Startpfad (Migration, Shell-Tab-Aufbau, `LoadCommand`) ist nur prozessübergreifend sichtbar; die Feed-Liste existiert erst nach Tab-Wechsel im UI-Baum |
| Pflicht | „+"-Button öffnet Add-Sheet, Fokus im URL-Feld | `SmokeTests.AddButton_OpensSheet_FocusesUrlEntry` | Issue: „„+"-Button öffnet das Bottom-Sheet, Fokus im URL-Feld" | `OnViewModelPropertyChanged` → `Dispatcher.Dispatch(NewUrlEntry.Focus)` ist Code-Behind — ViewModel-Tests sehen ihn nicht |
| Pflicht | Direkt-Add landet in Liste + SQLite | `SmokeTests.DirectAdd_FeedAppearsInListAndDatabase` | Issue: „Direkt-Add: Feed landet in Liste + SQLite" | Binding der `CollectionView` auf `Feeds` + reale Persistenz gegen Stub-Feed |
| Pflicht | UI-Direkt-Add (Setup) → Feed-Tap → ActionSheet → „Umbenennen" → Prompt → neuer Titel | `SmokeTests.FeedActionSheet_Rename_UpdatesTitle` | Issue: „Umbenennen-Dialog ändert Titel"; Anforderung: Auswahl eines `FeedListItem` per Tap | `OnFeedTapped`/`DisplayActionSheetAsync`/`DisplayPromptAsync` sind reine UI-Verdrahtung |
| Pflicht | Kategorie per UI anlegen + UI-Direkt-Add (Setup) → „Kategorie ändern"-ActionSheet inkl. „Keine Kategorie" | `SmokeTests.FeedActionSheet_ChangeCategory_IncludingNone` | Issue + Anforderung: Eintrag aus bestehender Kategorienmenge inkl. `CategoryNone` wählen | ActionSheet-Befüllung aus `FeedsViewModel.Categories` und `MakeUniqueOptionLabels` im Code-Behind |
| Pflicht | Suche mit expliziter Stub-URL `{BaseUrl}` → Directory-Treffer `feeds/search-hit.xml`, Ergebnis-Tap, Abo-Bestätigung | `SmokeTests.Search_SubscribesResult_PersistsFeed` | Issue: „Suche gegen den Test-Webserver"; Anforderung: `FeedSearchResult`-Auswahl + Abo-Dialog | Zusammenspiel `DirectoryEndpoint`-Override, `SearchResults`-Binding, `OnSearchResultTapped`, `DisplayAlertAsync`, `SubscribeResultCommand`; deckt den Directory-Zweig von `SearchAsync` ab |
| Pflicht | Suche mit `{BaseUrl}/site` → Directory liefert `[]` → Autodiscovery via `<link rel="alternate">` im `<head>` | `SmokeTests.Search_SiteUrl_DiscoversFeedViaLinkTag` | Anforderung: deterministische Stub-Antworten inkl. Discovery-Pfad (Vorlage `stubserver.py` Site A) | `DiscoverFeedsAsync`/`ProbeStandardPathsAsync` laufen nur gegen echtes HTTP; der Fallback wird nur erreicht, weil die Stub-`/directory`-Route query-abhängig `[]` liefert (`FeedSearchService.cs` Z. 84–88) |

Bestehende E2E-Tests, die angepasst werden müssen: Keine — es existiert bisher keine E2E-Infrastruktur; die PoC-Skripte unter `test-results/issue-59/` bleiben als Referenz erhalten.

## Offene Punkte

Keine.
