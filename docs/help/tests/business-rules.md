<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Tests — Business Rules

## Query-abhängige Directory-Antwort des Stubs

**Beschreibung:** Der `StubFeedServer` darf seine `/directory`-Route nicht statisch beantworten, weil `FeedSearchService.SearchAsync` die Verzeichnis-Suche immer zuerst aufruft und `DiscoverFeedsAsync` nur bei `results.Count == 0` erreicht — eine statisch nicht-leere Antwort würde den Discovery-Pfad unerreichbar machen.

**Bedingungen:**
- `url`-Query-Parameter == `BaseUrl` des Stubs (OrdinalIgnoreCase)
- alle anderen `url`-Werte (insb. `{BaseUrl}/site`, `{BaseUrl}/empty`)

**Verhalten:**
- Bei bekanntem `url`: JSON-Array mit einem Treffer (`/feeds/search-hit.xml`, Titel „Stub Search Hit", URLs dynamisch aus `BaseUrl`).
- Sonst: HTTP 200 mit leerem `[]` — **nicht** 404: Das leere Array wird als „keine Treffer" geparst, ohne `directoryFailed` zu setzen; ein Fehlerstatus markierte das Directory als fehlgeschlagen und könnte bei gleichzeitigem Discovery-Fehler `FeedSearchUnavailableException` auslösen.

**Umsetzung:** `StubFeedServer.InitializeAsync` — `MapGet("/directory")` mit Query-Auswertung (`src/Reporter.E2ETests/StubFeedServer.cs`).

## Testisolation ohne Reihenfolge-Abhängigkeit

**Beschreibung:** Alle E2E-Tests teilen einen App-Prozess und eine Datenbank. xunit garantiert keine Methodenreihenfolge — die Tests müssen daher in beliebiger Reihenfolge korrekt sein.

**Bedingungen:**
- Alle Tests liegen in der einen `E2ETestCollection` → serielle Ausführung, kein `ITestCaseOrderer`.
- Jeder Test, der einen vorhandenen Feed oder eine Kategorie braucht, erzeugt ihn selbst per UI-Seeding (`AddFeedViaUi`, `AddCategoryViaUi`) — kein DB-Seeding.
- Jeder Test nutzt eine eigene Stub-URL (`/feeds/{eindeutiger-name}.xml`); die `{name}`-Substitution im Channel-`<title>` erzeugt einen eindeutigen Karten-Titel.

**Verhalten:**
- Asserts sind zustandsagnostisch (z. B. akzeptiert `AppStarts_FeedListRenders` Karten **oder** den `EmptyView`-Platzhalter).
- `ResetUiState` schließt Sheets/Dialoge und aktiviert den Feeds-Tab; persistierte Testdaten bleiben bewusst bestehen und stören dank eindeutiger Titel nicht.
- Jeder Feeds-Test beginnt mit `SelectTab(AppResources.TabFeeds)`, weil die App auf dem **Unread-Tab** startet (Tab-Aktivierung statt angenommener Vorbedingung).

**Umsetzung:** `SmokeTests` + `E2ETestCollection` (`src/Reporter.E2ETests/`).

## Env-Override-Validierung in der App

**Beschreibung:** Die Test-Overrides sind bewusst schmal gehalten — sie existieren nur auf Prozess-Start-Ebene und sind kein persistiertes Benutzer-Setting.

**Bedingungen:**
- `REPORTER_FEEDSEARCH_ENDPOINT`: nicht leer **und** `Uri.TryCreate(…, UriKind.Absolute)` mit Scheme `http` oder `https`.
- `REPORTER_DB_PATH`: lediglich `!string.IsNullOrWhiteSpace`.
- `REPORTER_DISABLE_DEMO_SEED`: gesetzt (nicht leer) **und** weder `"0"` noch `"false"` (`OrdinalIgnoreCase`).

**Verhalten:**
- Gültiger Endpoint → `IFeedSearchService`-Factory übergibt ihn an `FeedSearchService(HttpClient, string?)`.
- Ungültiger/leerer Endpoint → `null` → Konstruktor-Fallback auf `https://feedsearch.dev/api/v1/search` (kein Fehler, keine Meldung).
- Ungültiger DB-Pfad → sichtbare Ausnahme beim App-Start (bewusst, da reiner Test-Override).
- Unterdrückungs-Flag gesetzt → `FirstRunState.DemoSeedSuppressed = true` → `IDemoContentService.EnsureSeededAsync` ist ein No-op; `0`/`false`/nicht gesetzt lassen den Seed unverändert aktiv.

**Umsetzung:** `MauiProgram.CreateMauiApp` / `ResolveFeedSearchEndpoint` / `ResolveDemoSeedSuppressed` (`src/Reporter/MauiProgram.cs`), `FeedSearchService`-Konstruktor (`src/Reporter.Core/Services/FeedSearchService.cs`), `DemoContentService` (`src/Reporter.Core/Services/DemoContentService.cs`).

## Demo-Seed-Unterdrückung statt impliziter Kopplung

**Beschreibung:** Jeder E2E-Lauf startet die App mit einer frischen Temp-Datenbank — ohne Gegenmaßnahme würde der First-Run-Demo-Seed (Kategorie „News", Feed „Apple Newsroom") in jedem Lauf greifen: eine zusätzliche Feed-Karte und Kategorie in den Smoke-Tests sowie ein echter externer Abruf von apple.com durch den Start-Sync.

**Bedingungen:**
- `ReporterAppFixture.InitializeAsync` setzt `REPORTER_DISABLE_DEMO_SEED=1` auf dem App-Prozess.
- `DemoSeedTests` braucht den Seed dagegen aktiv — die Unterdrückung darf nicht an das Vorhandensein von `REPORTER_DB_PATH` gekoppelt sein (dieses wird auch für manuelle Starts mit frischer DB genutzt, wo der Seed greifen soll).

**Verhalten:**
- Die Fixture-Instanz läuft hermetisch: kein Demo-Content, kein externer Netzverkehr zum Demo-Feed.
- `DemoSeedTests` startet eine eigene `Reporter.exe`-Instanz, entfernt `REPORTER_DISABLE_DEMO_SEED` explizit aus deren Environment (ein global gesetzter Wert würde die Testprämisse „kein Suppression-Flag" sonst verletzen) und weist den Seed über DB-Assertions und die sichtbare Karte nach — unabhängig vom Sync-Erfolg gegen apple.com.

**Umsetzung:** `ReporterAppFixture.InitializeAsync` und `DemoSeedTests.FirstStart_SeedsNewsCategoryAndDemoFeed` (`src/Reporter.E2ETests/`).

## Compiled Bindings: typisierte Kontexte und Runtime-Fallbacks

**Beschreibung:** Alle Views tragen `x:DataType` — auf Seitenebene das jeweilige ViewModel, in `DataTemplate`s der Elementtyp — damit Binding-Fehler zur Compile-Zeit fehlschlagen. Eine Klasse von Bindings bleibt dabei bewusst **untypisiert**.

**Bedingungen:**
- Direkte Seitenbindungen und Template-Itembindungen kompilieren gegen `x:DataType`.
- `x:Reference`-Hops der Form `BindingContext.*` (z. B. `BindingContext.IsOnline`, `BindingContext.ToggleSavedCommand` über `Source={x:Reference …}`) bleiben Runtime-Bindings.

**Verhalten:**
- `BindingContext` ist als `object` deklariert → Compiled Bindings können die Member dahinter nicht auflösen und fallen mit XamlC-Warnung auf das Runtime-Binding zurück — funktional unverändert korrekt.
- Diese Hops **nicht** in direkte Bindings umschreiben: Innerhalb eines `DataTemplate`s würde `{Binding IsOnline}` gegen den Item-Typ kompilieren (z. B. `FeedListItem` besitzt kein `IsOnline`) und fehlschlagen bzw. die Favicon-/Initial-Trigger still brechen.
- `x:Reference`-Bindings auf **direkte** `BindableProperty`s (z. B. `x:Reference Card` auf `IsOnline`/`ToggleSavedCommand` in `ArticleCardView`) kompilieren gegen den View-Typ und bleiben unverändert.

**Umsetzung:** `x:DataType` in `FeedsPage.xaml` (`vm:FeedsViewModel` + `models:FeedSearchResult`/`models:FeedListItem`), `CategoriesPage.xaml` (`vm:CategoriesViewModel` + `models:CategoryWithCount`), `UnreadPage.xaml` (`vm:UnreadViewModel` + `models:CategoryFilterItem`/`models:ItemListItem`), `LaterPage.xaml` (`vm:LaterViewModel` + `models:ItemListItem`), `SettingsPage.xaml` (`vm:SettingsViewModel` + `models:Keyword`), `ArticleCardView.xaml` (`models:ItemListItem`); `ArticleDetailPage.xaml` hatte den typisierten Kontext bereits.
