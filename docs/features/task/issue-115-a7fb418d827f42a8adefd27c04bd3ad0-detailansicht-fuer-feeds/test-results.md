<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ergebnisse

## Ergebnis

**Status:** Fehler vorhanden

Diese Iteration (Nacharbeiten aus `continue.md`) hat Unit-Tests, Node-Tests und die statischen Checks vollständig grün durchlaufen lassen. Die E2E-Suite wurde in dieser Iteration **nicht erneut ausgeführt** — sie benötigt eine interaktive Windows-Session (UIA3/FlaUI), die in dieser Laufumgebung nicht zur Verfügung steht. Die E2E-Abdeckungstabelle übernimmt daher die Ergebnisse des letzten vollständigen Suite-Laufs (Suite-Lauf 2 aus `test-results.2.md`: 25/26, alle neun `FeedDetailTests` grün). Einziges offenes Ergebnis bleibt der dokumentierte, nicht feature-bedingte `ArticleImageTests`-Flaky-Fehlschlag.

Ausgeführte Läufe (diese Iteration):

- `dotnet build src/Reporter.Tests/Reporter.Tests.csproj` + `dotnet build src/Reporter.E2ETests`: **0 Fehler, 0 Warnungen**
- `dotnet test src/Reporter.Tests/Reporter.Tests.csproj`: **641/641** — inkl. der beiden neuen Regressionstests `RefreshCommand_WhenReloadThrows_SetsLocalizedErrorMessage` und `SaveEditCommand_WhenRepositoryThrows_SetsErrorAndKeepsSheetOpen` (beide vor dem Fix rot verifiziert, danach grün)
- `npm test` (`node --test "scripts/*.test.mjs"`): **36/36**
- `powershell -File scripts/Run-StaticChecks.ps1`: **alle Checks bestanden** (Format, Lizenzheader, verwundbare Pakete, statische Analyse als Release-Build der gesamten Solution mit Warnungen als Fehler — 0 Warnungen, 0 Fehler)

Nicht ausgeführt (diese Iteration):

- `powershell -File scripts/Run-E2ETests.ps1` — erfordert interaktive Windows-Session; letztes Ergebnis siehe `test-results.2.md` (Suite-Lauf 2: 25/26, einziger Fehlschlag `ArticleImage_StoredLocally_AndShownOnCardAndDetail` — preexisting-flaky, isolierter Wiederholungslauf 2/2 grün)

## Fehlgeschlagene Tests

### ArticleImageTests

- **ArticleImage_StoredLocally_AndShownOnCardAndDetail** — `Assert.NotNull() Failure: Value is null` (`ArticleImageTests.cs:85`, Karten-Thumbnail `ArticleCardThumbnail` im UIA-Baum nicht gefunden; Stand: letzter vollständiger Suite-Lauf aus `test-results.2.md` — dokumentierte Preexisting-Instabilität laut `inventory/tests.md`, in isoliertem Wiederholungslauf 2/2 bestanden, nicht feature-bedingt; in dieser Iteration nicht erneut ausgeführt)

## E2E-Abdeckung

Übernommen vom letzten vollständigen Suite-Lauf (Suite-Lauf 2, `test-results.2.md`) — in dieser Iteration nicht erneut ausgeführt:

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Tap auf Feed-Karte → Detailansicht mit Beitragsliste | `FeedDetailTests.FeedDetail_Navigation_ShowsFeedItems` (Pflicht) | Bestanden (Suite-Lauf 2) |
| Suche filtert Beitragsliste (`SearchBar` → `CollectionView`) | `FeedDetailTests.FeedDetail_Search_FiltersItems` (Pflicht) | Bestanden (Suite-Lauf 2) |
| Infinite Scroll: Scrollen ans Listenende lädt nächste Seite nach | `FeedDetailTests.FeedDetail_InfiniteScroll_LoadsNextPage` (Pflicht) | Bestanden (Suite-Lauf 2) |
| Aktionsblatt „Aktualisieren" → Einzel-Feed-Sync | `FeedDetailTests.FeedDetail_RefreshAction_SyncsFeed` (Pflicht) | Bestanden (Suite-Lauf 2) |
| Aktionsblatt „Umbenennen" → Titel aktualisiert | `FeedDetailTests.FeedDetail_Rename_UpdatesTitle` (Pflicht) | Bestanden (Suite-Lauf 2) |
| Aktionsblatt „Kategorie ändern" inkl. „Keine" | `FeedDetailTests.FeedDetail_ChangeCategory_IncludingNone` (Pflicht) | Bestanden (Suite-Lauf 2) |
| Aktionsblatt „Bearbeiten" → Edit-Sheet, URL/Benachrichtigungen persistiert | `FeedDetailTests.FeedDetail_Edit_PersistsChanges` (Pflicht) | Bestanden (Suite-Lauf 2) |
| Aktionsblatt „Fehlerdetails anzeigen" nur bei `HealthStatus == Error` | `FeedDetailTests.FeedDetail_ErrorDetails_OnlyForErrorFeed` (Pflicht) | Bestanden (Suite-Lauf 2) |
| Aktionsblatt „Löschen" → Bestätigung, Kaskade, Rücksprung | `FeedDetailTests.FeedDetail_Delete_ReturnsToList` (Pflicht) | Bestanden (Suite-Lauf 2) |
| Pull-to-Refresh löst Einzel-Sync aus | `FeedDetailTests.FeedDetail_PullToRefresh_SyncsFeed` (Soll) | Plangemäß durch dokumentierte manuelle Verifikation ersetzt (`docs/help/anwendung/mobile-ui-design.md`, Abschnitt „Detailansicht für Feeds (issue-115)") |

## Zusammenfassung

Letzter vollständiger Lauf je Suite (E2E = Suite-Lauf 2 aus `test-results.2.md`):

- Gesamt: 703 (641 `Reporter.Tests` + 26 `Reporter.E2ETests` + 36 Node-Tests)
- Bestanden: 702
- Fehlgeschlagen: 1
- Übersprungen: 0

Hinweise:

- Die in dieser Iteration geänderten Pfade sind durch Unit-Tests abgesichert: `SaveEditAsync`-/`ReloadFeedAsync`-Fehlerbehandlung durch zwei neue Regressionstests; das `FeedDbAssertions`-Refactoring ist verhaltensgleich und bleibt der E2E-Suite zur Ausführung vorbehalten.
- Der einzige verbleibende Fehlschlag (`ArticleImage_StoredLocally_AndShownOnCardAndDetail`) ist die in `inventory/tests.md` nachgewiesene Preexisting-Flakiness der `ArticleImageTests` — nicht feature-bedingt; Suite-Stabilität separat zu verbessern.
- Vor den Läufen wurden keine laufenden `Reporter.exe`-Prozesse gefunden; nach den Läufen keine verbliebenen Prozesse.

## Testabdeckung

**Abdeckung:** In dieser Iteration nicht erneut gemessen (letzter Wert 86,5 % Zeilenabdeckung über `dotnet test src/Reporter.Tests --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage"`; `FeedDetailViewModel.cs` 91,9 % — die neuen Catch-Pfade sind durch die beiden Regressionstests abgedeckt).

## Fehlende Tests

- Keine neuen Abdeckungslücken identifiziert — beide neuen Fehlerpfade (`SaveEditAsync`-Repository-Fehler, `ReloadFeedAsync`-Repository-Fehler) haben dedizierte Regressionstests.
