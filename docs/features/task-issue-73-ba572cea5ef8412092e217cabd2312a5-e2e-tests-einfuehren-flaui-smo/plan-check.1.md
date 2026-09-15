<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan lückenhaft

Der Plan deckt alle fachlichen Anforderungen ab und seine Behauptungen zum Ist-Stand wurden im Repo verifiziert (siehe Hinweise). Eine Lücke besteht bei der Testdaten-/Setup-Spezifikation der E2E-Smoke-Tests: Zwei Pflicht-Tests setzen einen vorhandenen Feed in der Liste voraus, ohne dass der Plan beschreibt, wie diese Vorbedingung hergestellt wird — und die Suite teilt sich einen einzigen App-Prozess, ohne dass Reihenfolge- oder Isolationsstrategie festgelegt ist.

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| `x:DataType` auf `FeedsPage.xaml` (Seitenebene + beide `DataTemplate`s) | Schritt 1 + Abschnitt „XAML-Views": `xmlns:models` neu, `x:DataType="vm:FeedsViewModel"` auf `ContentPage`, `models:FeedSearchResult` (Z. 70) / `models:FeedListItem` (Z. 145) in den Templates — verifiziert: `xmlns:vm` existiert (Z. 7), `x:Name="Page"` (Z. 9), kein `x:DataType` vorhanden | `dotnet build` des Windows-TFM in Schritt 1/9; Bindings werden zusätzlich durch die FlaUI-Tests (Liste rendert, Suche rendert Ergebnisse) mitbewiesen | Abgedeckt |
| `x:DataType` auf `CategoriesPage.xaml` (inkl. `DataTemplate`) | `xmlns:vm` + `xmlns:models` neu, `x:DataType="vm:CategoriesViewModel"` auf Page, `models:CategoryWithCount` im Template (Z. 40) — verifiziert: beide xmlns fehlen tatsächlich | `dotnet build` + `CategoryWithCount`-Bindung durch Kategorie-Anlage im E2E-Test indirekt sichtbar | Abgedeckt |
| „Prüfen, ob weitere Views ohne kompilierte Bindings sind" | Bestandsaufnahme-Erweiterung umgesetzt: `ArticleCardView.xaml` (`models:ItemListItem` auf `ContentView`) plus seitenebenes `x:DataType` auf `UnreadPage`, `LaterPage`, `SettingsPage` (`vm:…ViewModel`) — verifiziert: diese drei Seiten haben nur `DataTemplate`-`x:DataType` (UnreadPage Z. 115/196, LaterPage Z. 45, SettingsPage Z. 84), `ArticleDetailPage` ist die einzige typisierte Seite | `dotnet build` + App-Smoke in Schritt 1; Compile-Fehler-Bereinigung explizit eingeplant | Abgedeckt |
| `FeedSearchService.DirectoryEndpoint` konfigurierbar | Optionaler Ctor-Parameter `directoryEndpoint = null` mit Fallback auf die Konstante; `SearchDirectoryAsync` nutzt `_directoryEndpoint` — verifiziert: `private const` Z. 17, Verwendung Z. 112, Ctor Z. 56 ohne Parameter | Unit-Test `Ctor_CustomDirectoryEndpoint_RequestsOverrideHost` über `StubHttpMessageHandler.RequestedUrls` (privater Handler existiert, Z. 400/418) | Abgedeckt |
| Override beim App-Start (`REPORTER_FEEDSEARCH_ENDPOINT`) | `MauiProgram` liest Env-Var, validiert per `Uri.TryCreate` (http/https), Factory-Lambda-Registrierung an Z. 56 — verifiziert: Zeilenangaben korrekt, `WindowsPackageType=None` (Reporter.csproj Z. 43) | Indirekt durch `Search_SubscribesResult_PersistsFeed` (Suite funktioniert nur mit gesetztem Override gegen Stub) | Abgedeckt |
| In-process Test-Webserver (Port 0, Feeds + feedsearch-JSON + Discovery/404) | `StubFeedServer` auf Kestrel (`FrameworkReference Microsoft.AspNetCore.App`), Port `0`, Routen `/directory` (JSON dynamisch aus `BaseUrl`), `/feeds/*.xml`, `/site` (`<link rel="alternate">`), `/empty` + Well-Known-/Favicon-404 — fachliche Vorlage `stubserver.py` verifiziert (Zwei-Site-Muster, keine feedsearch-JSON-Abdeckung dort) | Fixture + `Search_SubscribesResult_PersistsFeed` + `Search_SiteUrl_DiscoversFeedViaLinkTag` | Abgedeckt |
| Datenisolation der E2E-App (`REPORTER_DB_PATH`) | Zweite Env-Var vor `Directory.CreateDirectory` (MauiProgram Z. 43–44 verifiziert); Temp-Dir im Fixture | `DirectAdd_FeedAppearsInListAndDatabase` / `Search_SubscribesResult_PersistsFeed` über `FeedDbAssertions` gegen `feeds`-Tabelle der Temp-DB (Tabellenname `feeds` in `ReporterDbContext` Z. 86 verifiziert) | Abgedeckt |
| Unit-Tests bleiben beim gemockten `HttpMessageHandler` | Keine Änderung an `FeedSearchServiceTests`-Instantiierung; optionaler Parameter bleibt kompatibel — verifiziert: `new FeedSearchService(new HttpClient(handler))` (Z. 17) und `ServiceCollectionTests` Z. 52 kompilieren unverändert | Baseline 491 Tests bleiben grün (Verifikation Schritt 9) | Abgedeckt |
| Smoke: App startet, Feed-Liste rendert | `AppStarts_FeedListRenders` | E2E-Test vorhanden (Hauptfenster + `CollectionView`/`EmptyView` `PlaceholderFeeds`, Z. 143) | Abgedeckt |
| Smoke: „+"-Button öffnet Sheet, Fokus im URL-Feld | `AddButton_OpensSheet_FocusesUrlEntry` | E2E-Test vorhanden; Verdrahtung `OnViewModelPropertyChanged` → `NewUrlEntry.Focus()` in `FeedsPage.xaml.cs` Z. 255–260 verifiziert | Abgedeckt |
| Smoke: Direkt-Add landet in Liste + SQLite | `DirectAdd_FeedAppearsInListAndDatabase` | E2E-Test vorhanden (UI-Assert + `FeedDbAssertions`) | Abgedeckt |
| Smoke: `FeedListItem`-Tap → ActionSheet → „Umbenennen" → Prompt → Titel geändert | `FeedActionSheet_Rename_UpdatesTitle` | Test vorhanden, aber Setup (Feed muss existieren) unspezifiziert | Lücke (Setup) |
| Smoke: „Kategorie ändern"-ActionSheet inkl. „Keine Kategorie" | `FeedActionSheet_ChangeCategory_IncludingNone`; Pseudo-Eintrag `CategoryNone`/`Guid.Empty` in `FeedsViewModel.LoadAsync` (Z. 277–282) verifiziert; Kategorie-Anlage per UI im Test enthalten | Test vorhanden, aber Setup (Feed muss existieren) unspezifiziert; Reihenfolge-/Isolationsfrage offen | Lücke (Setup) |
| Smoke: Suche gegen Test-Webserver, `FeedSearchResult`-Auswahl + Abo-Bestätigungsdialog | `Search_SubscribesResult_PersistsFeed` | E2E-Test vorhanden (Stub-Directory, `OnSearchResultTapped` → `DisplayAlertAsync` → `SubscribeResultCommand`, Z. 208–226 verifiziert) | Abgedeckt |
| Deterministische Antworten inkl. Autodiscovery-Pfad | `Search_SiteUrl_DiscoversFeedViaLinkTag` (`/site` mit `<link rel="alternate">`) | E2E-Test vorhanden | Abgedeckt |
| Testanzahl ~5–8, kein Vollabdeckungs-Anspruch | 7 Smoke-Tests geplant | — | Abgedeckt |
| Manuelle Screenshot-Verifikation bleibt | Übersicht + Schritt 9; AGENTS.md-konform | — | Abgedeckt |
| Retries/Timeouts gegen Flaky-Anfälligkeit | `UiRetry`-Helfer (WaitForElement-Polling, Invoke-/Value-/Dialog-Helfer), großzügige Timeouts im Fixture | Pflichtbestandteil in Risiken benannt | Abgedeckt |
| Nur Windows/UIA, interaktive Session → lokale/on-demand-Suite, kein CI-Job | Kein CI-Workflow; `scripts/Run-E2ETests.ps1` + CONTRIBUTING-Hinweis; `[Trait("Category","E2E")]` zur Trennung — verifiziert: CI testet nur `Reporter.Tests.csproj`, CONTRIBUTING Z. 22 nennt `dotnet test Reporter.sln` | — | Abgedeckt |
| Vorarbeiten (`uia.ps1`, `stubserver.py`, `probe.fsx`) | Bleiben als Referenz im Repo (begründete Entscheidung) — Dateien verifiziert vorhanden | — | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

- [ ] Setup/Testdaten für `SmokeTests.FeedActionSheet_Rename_UpdatesTitle` und `SmokeTests.FeedActionSheet_ChangeCategory_IncludingNone` (Akzeptanzkriterien „Umbenennen-Dialog ändert Titel" bzw. „Kategorie ändern"-ActionSheet): Beide Tests setzen einen vorhandenen Feed-Eintrag in der `CollectionView` voraus, der Plan beschreibt aber nicht, wie dieser hergestellt wird — weder per UI-Flow im Test (z. B. Direkt-Add als Teil des Tests), noch per DB-Seeding in die Temp-`reporter.db` vor dem App-Start, noch über eine definierte Testreihenfolge.
- [ ] Isolations-/Reihenfolgestrategie für die `E2ETestCollection`: Alle sieben Tests teilen sich einen App-Prozess und eine SQLite-DB; xunit garantiert keine Methodenreihenfolge. `DirectAdd_FeedAppearsInListAndDatabase` und `Search_SubscribesResult_PersistsFeed` verändern den persistenten Zustand, den die Aktions-Smoke-Tests implizit benötigen; umgekehrt ist `AppStarts_FeedListRenders` mit dem `EmptyView`-Assert zustandsabhängig (Platzhalter ist nur bei leerer Liste sichtbar). Der Plan legt weder fest, ob Tests zustandsunabhängig sein müssen, noch wie der Ausgangszustand je Test hergestellt wird.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| App-Start, Feed-Liste rendert | `AppStarts_FeedListRenders` | Abgedeckt (Reihenfolgeabhängigkeit des EmptyView-Asserts beachten) |
| „+" öffnet Add-Sheet, Fokus in `NewUrlEntry` | `AddButton_OpensSheet_FocusesUrlEntry` | Abgedeckt |
| Direkt-Add → Liste + SQLite | `DirectAdd_FeedAppearsInListAndDatabase` | Abgedeckt |
| Feed-Tap → ActionSheet → Umbenennen-Prompt → neuer Titel | `FeedActionSheet_Rename_UpdatesTitle` | Lücke: Setup (Feed vorhanden) nicht spezifiziert |
| Feed-Tap → „Kategorie ändern" → echte Kategorie + `CategoryNone` | `FeedActionSheet_ChangeCategory_IncludingNone` | Lücke: Setup (Feed vorhanden) nicht spezifiziert; Kategorie-Anlage per UI ist beschrieben |
| Suche gegen Stub → Ergebnis-Tap → Abo-Alert → Persistenz | `Search_SubscribesResult_PersistsFeed` | Abgedeckt |
| Autodiscovery via `<link rel="alternate">` | `Search_SiteUrl_DiscoversFeedViaLinkTag` | Abgedeckt |
| Compiled-Bindings-Kriterium | — | Nicht erforderlich mit Begründung: Compile-Zeit-Kriterium; Nachweis über `dotnet build` des Windows-TFMs plus indirekte Laufzeitabdeckung durch die Suite |

## Fehlende oder unvollständige Planbestandteile

- [ ] Festlegung der Testdaten-/Zustandsstrategie des E2E-Fixtures: Der Plan definiert Fixture-Dateien und Hilfsklassen (`FeedDbAssertions` nur lesend), aber keine Methode, mit der Vorbedingungen in die isolierte `reporter.db` gelangen (z. B. schreibendes Seeding via `Microsoft.Data.Sqlite`/EF Core vor `Process.Start` der App) oder wie Tests ihre Eingangsdaten selbst per UI erzeugen. Betrifft die Setup-Lücke oben; eine Entscheidung (Seeding vs. UI-Aufbau vs. geordnete Tests) fehlt.

## Hinweise

- **Plan-Behauptungen im Repo verifiziert:** `x:DataType`-Status aller Views korrekt; `FeedSearchService` Z. 17/56/112 und `MauiProgram` Z. 43–44/56 korrekt; `ServiceCollectionTests` spiegelt die DI-Registrierung (Z. 51–52); `StubHttpMessageHandler.RequestedUrls` existiert (FeedSearchServiceTests Z. 400/418); `feeds`-Tabellenname korrekt (`ReporterDbContext` Z. 86); `WindowsPackageType=None`, `MauiXamlInflator=SourceGen`, `GenerateDocumentationFile`+`WarningsAsErrors CS1591`-Konvention in `Reporter.Tests.csproj` korrekt; `stubserver.py` bestätigt (kein feedsearch-JSON); alle ViewModels liegen im Namespace `Reporter.Core.ViewModels` (`FeedsViewModel`/`CategoriesViewModel`/`UnreadViewModel`/`LaterViewModel`/`SettingsViewModel` in Assembly `Reporter.Core`, `ArticleDetailViewModel` in `Reporter`); Tab-Titel in `AppShell.xaml.cs` lokalisiert (UIA-erreichbar); `Run-StaticChecks.ps1` führt Restore/Format/Lizenzheader/Security/TreatWarningsAsErrors-Build gegen `Reporter.sln` aus — die sln-Aufnahme des neuen Projekts wird damit korrekt mitgeprüft.
- **`x:Reference`-Bindings auf `BindingContext.*`:** Der Plan sagt, `BindingContext.IsOnline, Source={x:Reference Page}` (FeedsPage Z. 176/191) und `BindingContext.SelectCategoryCommand/ToggleSavedCommand/…` (UnreadPage Z. 123/197–199, LaterPage Z. 46–48, SettingsPage Z. 97) „lösen sich typkorrekt auf". Tatsächlich ist `BindingContext` als `object` deklariert — eine kompilierte Bindung kann die Member dahinter nicht auflösen und fällt auf Runtime-Binding zurück (typischerweise mit XamlC-Warnung). Funktional bleibt das korrekt; der Schritt „Compile-Fehler bereinigen" deckt etwaige Warnungen ab. Bei `TreatWarningsAsErrors=true` im Static-Checks-Build sollte geprüft werden, ob XamlC-Warnungen als Fehler durchschlagen — ggf. Bindings auf `x:Reference`-typisierte Pfade oder explizite Runtime-Bindings umstellen.
- **Negativfall der Endpoint-Validierung:** Die Regel „ungültiger `REPORTER_FEEDSEARCH_ENDPOINT` wird ignoriert" hat keinen Testnachweis. `MauiProgram` ist aus `Reporter.Tests` nicht erreichbar (MAUI-App-Projekt); falls Abdeckung gewünscht ist, die Validierung in eine testbare Hilfsmethode (z. B. in `Reporter.Core`) extrahieren. Nicht als Lücke gewertet, da die Anforderung dafür keinen Test verlangt.
- **CONTRIBUTING.md:** Zeile 22 dokumentiert `dotnet test Reporter.sln` — der geplante Hinweis sollte den Befehl dort konkret um `--filter "Category!=E2E"` ergänzen, damit die dokumentierte Anleitung nach sln-Aufnahme des E2E-Projekts nicht in eine nicht lauffähige Suite führt.
