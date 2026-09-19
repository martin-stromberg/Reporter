<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ergebnisse

## Ergebnis

**Status:** Fehler vorhanden

Alle neun geplanten Pflicht-E2E-Szenarien existieren in `src/Reporter.E2ETests/FeedDetailTests.cs` und sind im zweiten vollständigen Suite-Lauf **vollständig grün** gelaufen (25/26). Der einzige Fehlschlag des letzten Suite-Laufs ist `ArticleImageTests.ArticleImage_StoredLocally_AndShownOnCardAndDetail` — die in `inventory/tests.md` (Baseline-Läufe 3/4) und `plan-check.md` („Bekannte E2E-Instabilität") dokumentierte, nicht feature-bedingte Flaky-Instabilität; im isolierten Wiederholungslauf bestanden beide `ArticleImageTests` (2/2). Im ersten Suite-Lauf beendete sich `Reporter.exe` mitten in `FeedDetail_Edit_PersistsChanges` (gleiche Instabilitäts-Signatur wie der dokumentierte 0xC000027B-WinUI-Absturz) — alle danach laufenden Tests scheiterten kaskadierend mit `The Reporter.exe process is not running anymore`; der zweite Lauf bewies alle `FeedDetailTests` grün. Unit-Tests und Node-Tests sind fehlerfrei.

Ausgeführte Läufe (alle selbst ausgeführt, interaktive Windows-Session):

- `dotnet build Reporter.sln` (`IncludeIosTarget=false`): **0 Fehler, 0 Warnungen** (Debug, gesamte Solution inkl. `Reporter` win-x64, `Reporter.Tests`, `Reporter.E2ETests`)
- `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage"`: **639/639** ([TRX](../../../../test-results/run-tests-unittests.trx))
- `powershell -File scripts/Run-E2ETests.ps1` (Suite-Lauf 1, Build + `dotnet test src/Reporter.E2ETests`): **17/26** — App-Prozess beendete sich während `FeedDetail_Edit_PersistsChanges`; 9 Kaskaden-Fehlschläge mit `The Reporter.exe process is not running anymore` (6× `FeedDetailTests`, `ArticleLinkTests`, 2× `ArticleImageTests`)
- `powershell -File scripts/Run-E2ETests.ps1` (Suite-Lauf 2, kompletter Lauf): **25/26** — einziger Fehler `ArticleImage_StoredLocally_AndShownOnCardAndDetail`; alle 9 `FeedDetailTests` bestanden ([Log](../../../../test-results/run-tests-e2e-rerun-console.log))
- `dotnet test src/Reporter.E2ETests --filter "FullyQualifiedName~ArticleImageTests"` (isolierter Wiederholungslauf): **2/2**
- `npm test` (`node --test "scripts/*.test.mjs"`): **36/36**

## Fehlgeschlagene Tests

### ArticleImageTests

- **ArticleImage_StoredLocally_AndShownOnCardAndDetail** — `Assert.NotNull() Failure: Value is null` (`ArticleImageTests.cs:85`, Karten-Thumbnail `ArticleCardThumbnail` im UIA-Baum nicht gefunden; Suite-Lauf 2 — dokumentierte Preexisting-Instabilität, in isoliertem Wiederholungslauf bestanden)

Kontext Suite-Lauf 1 (keine separaten Defekte, sondern Kaskade nach App-Prozess-Ende — `The Reporter.exe process is not running anymore`): `FeedDetail_Edit_PersistsChanges`, `FeedDetail_Search_FiltersItems`, `FeedDetail_ErrorDetails_OnlyForErrorFeed`, `FeedDetail_Rename_UpdatesTitle`, `FeedDetail_Delete_ReturnsToList`, `FeedDetail_InfiniteScroll_LoadsNextPage`, `ArticleLinkTests.ExternalLinkInArticle_OpensSystemBrowser`, `ArticleImage_StoredLocally_AndShownOnCardAndDetail`, `BrokenImage_DoesNotFailSync_StoresNoImage`. Alle neun Tests bestanden im Suite-Lauf 2 bzw. im Wiederholungslauf.

## E2E-Abdeckung

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Tap auf Feed-Karte → Detailansicht mit Beitragsliste | `FeedDetailTests.FeedDetail_Navigation_ShowsFeedItems` (Pflicht) | Bestanden (Suite-Lauf 2) |
| Suche filtert Beitragsliste (`SearchBar` → `CollectionView`) | `FeedDetailTests.FeedDetail_Search_FiltersItems` (Pflicht) | Bestanden (Suite-Lauf 2; in Lauf 1 Kaskaden-Fehlschlag nach App-Ende) |
| Infinite Scroll: Scrollen ans Listenende lädt nächste Seite nach | `FeedDetailTests.FeedDetail_InfiniteScroll_LoadsNextPage` (Pflicht) | Bestanden (Suite-Lauf 2; in Lauf 1 Kaskaden-Fehlschlag nach App-Ende) |
| Aktionsblatt „Aktualisieren" → Einzel-Feed-Sync | `FeedDetailTests.FeedDetail_RefreshAction_SyncsFeed` (Pflicht) | Bestanden (Suite-Lauf 2) |
| Aktionsblatt „Umbenennen" → Titel aktualisiert | `FeedDetailTests.FeedDetail_Rename_UpdatesTitle` (Pflicht) | Bestanden (Suite-Lauf 2; in Lauf 1 Kaskaden-Fehlschlag nach App-Ende) |
| Aktionsblatt „Kategorie ändern" inkl. „Keine" | `FeedDetailTests.FeedDetail_ChangeCategory_IncludingNone` (Pflicht) | Bestanden (Suite-Lauf 2) |
| Aktionsblatt „Bearbeiten" → Edit-Sheet, URL/Benachrichtigungen persistiert | `FeedDetailTests.FeedDetail_Edit_PersistsChanges` (Pflicht) | Bestanden (Suite-Lauf 2; in Lauf 1 Abbruch durch App-Prozess-Ende während des Tests) |
| Aktionsblatt „Fehlerdetails anzeigen" nur bei `HealthStatus == Error` | `FeedDetailTests.FeedDetail_ErrorDetails_OnlyForErrorFeed` (Pflicht) | Bestanden (Suite-Lauf 2; in Lauf 1 Kaskaden-Fehlschlag nach App-Ende) |
| Aktionsblatt „Löschen" → Bestätigung, Kaskade, Rücksprung | `FeedDetailTests.FeedDetail_Delete_ReturnsToList` (Pflicht) | Bestanden (Suite-Lauf 2; in Lauf 1 Kaskaden-Fehlschlag nach App-Ende) |
| Pull-to-Refresh löst Einzel-Sync aus | `FeedDetailTests.FeedDetail_PullToRefresh_SyncsFeed` (Soll) | Plangemäß durch dokumentierte manuelle Verifikation ersetzt (`docs/help/anwendung/mobile-ui-design.md`, Abschnitt „Detailansicht für Feeds (issue-115)"); der Sync-Nachweis selbst ist über `FeedDetail_RefreshAction_SyncsFeed` abgedeckt |

## Zusammenfassung

Letzter vollständiger Lauf je Suite (E2E = Suite-Lauf 2):

- Gesamt: 701 (639 `Reporter.Tests` + 26 `Reporter.E2ETests` + 36 Node-Tests)
- Bestanden: 700
- Fehlgeschlagen: 1
- Übersprungen: 0

Hinweise:

- Der einzige Fehlschlag des finalen Suite-Laufs (`ArticleImage_StoredLocally_AndShownOnCardAndDetail`) entspricht der in `inventory/tests.md` nachgewiesenen Preexisting-Flakiness der `ArticleImageTests` (Baseline: 2× instabil, im Wiederholungslauf grün) — diesmal als UIA-Timing auf das Karten-Thumbnail (`ArticleCardThumbnail`), ohne App-Absturz und ohne Kaskade. Isolierter Wiederholungslauf: 2/2 bestanden. Nicht feature-bedingt.
- Der App-Prozess-Abbruch in Suite-Lauf 1 (`FeedDetail_Edit_PersistsChanges` → Kaskade) hat die gleiche Signatur wie der in `test-results.1.md` dokumentierte 0xC000027B-Absturz in `Microsoft.UI.Xaml.dll` (WinUI-Stowed-Exception; identische Crash-Signatur im Windows-Eventlog, u. a. 19.09. 16:39 aus diesem Worktree) — bekannte Suite-Instabilität, die in einem vollständigen zweiten Lauf nicht reproduziert wurde. Es gibt keinen Beleg für einen deterministischen Feature-Defekt.
- Alle neun Pflicht-E2E-Szenarien der `FeedDetailTests` sind in einem einzigen vollständigen Suite-Lauf grün gelaufen — der primäre Funktionsnachweis für den Benutzerfluss ist erbracht.
- Vor den Läufen wurden keine laufenden `Reporter.exe`-Prozesse gefunden; nach den Läufen keine verbliebenen Prozesse.
- Statische Checks (`Run-StaticChecks.ps1`) wurden in diesem Schritt nicht ausgeführt (kein Code geändert; reiner Test-/Dokumentationsschritt).

## Testabdeckung

**Abdeckung:** 86,5 % Zeilenabdeckung (Cobertura `coverage.cobertura.xml` über `dotnet test src/Reporter.Tests --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage"`; erfasst werden `Reporter.Core` und `Reporter.Data`, `Reporter.Data.Migrations.*` ist laut Runsettings ausgeschlossen — die MAUI-App `src/Reporter` und das E2E-Projekt haben keine eigene Abdeckung)

| Datei | Abdeckung |
|-------|-----------|
| `Reporter.Core\Models\CategoryFilterItem.cs` | 22,2 % |
| `Reporter.Core\Resources\Strings\AppResources.Designer.cs` | 24,9 % (generiert) |
| `Reporter.Core\Services\FeedSearchUnavailableException.cs` | 33,3 % |

Die Feature-Dateien liegen oberhalb der 80-%-Marke: `FeedDetailViewModel.cs` 91,9 %, `ItemRepository.cs` 95,5 %, `FeedsViewModel.cs` 99,1 %, `FeedsViewModel.Search.cs` 95,9 %, `FeedUrlValidator.cs` 100 %, `BaseViewModel.cs` 87,5 %.

## Fehlende Tests

Quelle: `Coverage-Daten`

- Keine nicht-generierte Quelldatei mit 0 % Zeilenabdeckung gefunden (niedrigste nicht-generierte Datei: `CategoryFilterItem.cs` 22,2 %, `FeedSearchUnavailableException.cs` 33,3 % — beide nicht Feature-bezogen).
