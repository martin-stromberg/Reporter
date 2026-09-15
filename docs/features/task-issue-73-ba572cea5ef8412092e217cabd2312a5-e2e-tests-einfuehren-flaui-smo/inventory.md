<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: E2E-Tests einführen — FlaUI-Smoke-Suite + Compiled Bindings (Issue #73)

Analysiert wurden die XAML-Views der MAUI-App (`src/Reporter`), die Feed-Suche und
DI-Registrierung (`src/Reporter.Core`, `src/Reporter/MauiProgram.cs`), das
Unit-Testprojekt (`src/Reporter.Tests`) sowie die Issue-59-Vorarbeiten
(`test-results/issue-59/`), bezogen auf die Anforderung „Compiled Bindings
nachrüsten + konfigurierbarer Feed-Directory-Endpunkt + FlaUI-Smoke-Suite mit
in-process Test-Webserver".

## Zusammenfassung

- **`x:DataType` fehlt bestätigt** auf `FeedsPage.xaml`, `CategoriesPage.xaml` und
  `ArticleCardView.xaml` — und **zusätzlich auf Seitenebene** von
  `UnreadPage.xaml`, `LaterPage.xaml` und `SettingsPage.xaml` (dort sind nur die
  `DataTemplate`s typisiert; die Anforderung listet diese drei Seiten fälschlich
  als „bereits vorhanden" — tatsächlich ist nur `ArticleDetailPage.xaml` komplett
  auf Seitenebene typisiert). `xmlns:vm` existiert in `FeedsPage.xaml`, fehlt in
  `CategoriesPage.xaml`; `xmlns:models` fehlt in `FeedsPage.xaml` und
  `CategoriesPage.xaml`.
- **`FeedSearchService.DirectoryEndpoint` ist eine harte `private const`**
  (`FeedSearchService.cs` Zeile 17, Verwendung Zeile 112); der Konstruktor
  (`HttpClient`) hat keinen Endpoint-Parameter. `MauiProgram.cs` Zeile 56
  registriert `AddSingleton<IFeedSearchService, FeedSearchService>()`; Zeile 43
  fixiert den DB-Pfad auf `FileSystem.AppDataDirectory/reporter.db` — kein
  Override für E2E-Datenisolation vorhanden.
- **Alle für die Smoke-Tests benötigten Anker existieren**: `x:Name` (`Page`,
  `NewUrlEntry`, `CategoryNameEntry`, `ArticlesCollection`, `Card`,
  `ArticleWebView`, `PageRoot`) und lokalisierte `SemanticProperties.Description`
  auf Listenelementen, Dialog-Auslösern und Sheet-Dismiss-Fläche. Die
  Code-Behind-Dialogpfade (`OnFeedTapped` → `DisplayActionSheetAsync` →
  `RenameFeedAsync`/`ChangeCategoryAsync`/`ConfirmDeleteFeedAsync`,
  `OnSearchResultTapped` → `DisplayAlertAsync` → `SubscribeResultCommand`,
  `ConfirmDirectAddAsync`-Callback, `OnBackButtonPressed`,
  `OnViewModelPropertyChanged` → `NewUrlEntry.Focus()`) sind verdrahtet, aber
  ungetestet.
- **Keine E2E-Testinfrastruktur vorhanden**: kein UI-Testprojekt, kein
  FlaUI/WireMock-Paket, kein Webserver-Fixture. Vorarbeiten (`uia.ps1`,
  `stubserver.py`, `probe.fsx`) sind manuelle PoCs; `stubserver.py` deckt nur
  Autodiscovery/Well-Known-Pfade ab, **nicht** die feedsearch.dev-JSON-API;
  `probe.fsx` referenziert eine DLL aus einem anderen Arbeitsverzeichnis.
- **Windows-Build ist lokal möglich**: `maui-windows`-Workload installiert,
  Release-Build des Windows-TFM erzeugt die unverpackte `Reporter.exe`
  (`WindowsPackageType=None`) — Voraussetzung für das App-Prozess-Fixture.
- **Test-Ausgangszustand**: `dotnet test src/Reporter.Tests` (Release, mit
  Coverage) — **491 Tests, alle erfolgreich, 0 fehlgeschlagen, 0 übersprungen**,
  Exit-Code 0 (Nachweis: [tests.md](inventory/tests.md), Logs unter
  [inventory/test-results/](inventory/test-results/)). Die vom Issue genannten
  „316 Tests" sind überholt — aktuell 491 ausgeführte Testfälle. Keine bekannten
  Fehlschläge. Verbleibende Testlücke: die komplette UI-/E2E-Ebene ist ungetestet;
  die App ist nicht per `dotnet test` ausführbar (interaktive Desktop-Session nötig).

## Details

- [XAML-Views: Compiled-Bindings-Status, Code-Behind, Automation-Anker](inventory/xaml-views.md)
- [Logikklassen und Services](inventory/logic.md) — `FeedSearchService`, `MauiProgram`, `FeedsViewModel`, `CategoriesViewModel`, `FeedTitleFallback`, `FeedSyncService`/`FeedIconService`, `App`/`AppShell`
- [Datenmodelle](inventory/models.md) — `FeedListItem`, `FeedSearchResult`, `CategoryWithCount`, `ItemListItem`, `FeedHealth`
- [Interfaces und Enums](inventory/interfaces.md) — `IFeedSearchService`, `FeedSearchMatchKind`
- [Tests](inventory/tests.md) — Test-Ausgangszustand (491/491 grün), testrelevante Klassen, Fakes/Helpers, Issue-59-Vorarbeiten
