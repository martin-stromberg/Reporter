<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ergebnisse

## Ergebnis

**Status:** Fehler vorhanden

Alle neun geplanten Pflicht-E2E-Szenarien existieren in `src/Reporter.E2ETests/FeedDetailTests.cs` und wurden ausgeführt — aber **keiner von drei vollständigen Suite-Läufen war vollständig grün**. In Lauf 1 schlugen zwei Pflicht-Szenarien (`FeedDetail_Edit_PersistsChanges`, `FeedDetail_InfiniteScroll_LoadsNextPage`), in Lauf 3 ein weiteres (`FeedDetail_Delete_ReturnsToList`) fehl — jedes Mal andere Tests, jeweils bestanden in den übrigen Läufen. Zusätzlich schlugen in den Läufen 2 und 3 beide `ArticleImageTests` fehl (in Lauf 2 stürzte `Reporter.exe` mitten im Test mit Code -1073741189/0xC000027B ab). Unit-Tests und Node-Tests sind fehlerfrei.

Ausgeführte Läufe (Verifikations-Iteration, alle selbst ausgeführt):

- `dotnet build Reporter.sln` (`IncludeIosTarget=false`, `IncludeAndroidTarget=false`): **0 Fehler, 0 Warnungen**
- `dotnet test src/Reporter.Tests --no-build --collect:"XPlat Code Coverage"`: **636/636**
- `dotnet test src/Reporter.E2ETests --no-build` (Suite-Lauf 1): **24/26** — Fehler: `FeedDetail_Edit_PersistsChanges`, `FeedDetail_InfiniteScroll_LoadsNextPage`
- `dotnet test src/Reporter.E2ETests --no-build` (Suite-Lauf 2): **24/26** — Fehler: `ArticleImage_StoredLocally_AndShownOnCardAndDetail` (App-Absturz während des Tests), `BrokenImage_DoesNotFailSync_StoresNoImage` (Kaskade)
- `dotnet test src/Reporter.E2ETests --no-build` (Suite-Lauf 3): **23/26** — Fehler: `FeedDetail_Delete_ReturnsToList`, `ArticleImage_StoredLocally_AndShownOnCardAndDetail`, `BrokenImage_DoesNotFailSync_StoresNoImage`
- `npm test -- --passWithNoTests` (`scripts/*.test.mjs`): **36/36**

Logs der drei E2E-Läufe: `test-results/e2e-run-20260919-230518.log`, `test-results/e2e-run-20260919-231241.log`, `test-results/e2e-run-20260919-231810.log`.

## Fehlgeschlagene Tests

### FeedDetailTests

- **FeedDetail_InfiniteScroll_LoadsNextPage** — `System.TimeoutException : Element not found within 00:00:20: name 'Stub Feed scroll-feed'` (`E2EPageHelpers.WaitForFeedCard` → `OpenFeedDetail`, Suite-Lauf 1; identische Signatur wie die vermeintlich behobenen Suite-Fehler aus den Läufen 1/2 der vorherigen Iteration)
- **FeedDetail_Edit_PersistsChanges** — `System.TimeoutException : Element not found within 00:00:20: action sheet entry 'Bearbeiten'` (`E2EPageHelpers.OpenFeedDetailActions`, Suite-Lauf 1)
- **FeedDetail_Delete_ReturnsToList** — `The deleted feed card 'delete-target.xml' is still rendered in the feed list.` (Suite-Lauf 3: Delete-Flow lief durch, die Karte blieb nach Rückkehr sichtbar)

### ArticleImageTests

- **ArticleImage_StoredLocally_AndShownOnCardAndDetail** — Suite-Lauf 2: `No feed row with URL '…/feeds/image-feed.xml' found in reporter.db — the direct add did not persist the feed.` (die App beendete sich während des Tests selbst mit Code -1073741189); Suite-Lauf 3: `No item row titled 'image-feed article' found — the stub feed was not synced.`
- **BrokenImage_DoesNotFailSync_StoresNoImage** — Suite-Lauf 2: `The Reporter.exe process is not running anymore.` (Kaskade nach dem Absturz); Suite-Lauf 3: `No item row titled 'broken-image-feed article' found — a rejected image download must not break the sync.`

## E2E-Abdeckung

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Tap auf Feed-Karte → Detailansicht mit Beitragsliste | `FeedDetailTests.FeedDetail_Navigation_ShowsFeedItems` (Pflicht) | Bestanden (3/3 Suite-Läufe) |
| Suche filtert Beitragsliste (`SearchBar` → `CollectionView`) | `FeedDetailTests.FeedDetail_Search_FiltersItems` (Pflicht) | Bestanden (3/3) |
| Infinite Scroll: Scrollen ans Listenende lädt nächste Seite nach | `FeedDetailTests.FeedDetail_InfiniteScroll_LoadsNextPage` (Pflicht) | Fehlgeschlagen (Suite-Lauf 1, Setup-Timeout beim Feed-Karten-Suchlauf; in den Läufen 2 und 3 bestanden — instabil im Suite-Kontext) |
| Aktionsblatt „Aktualisieren" → Einzel-Feed-Sync | `FeedDetailTests.FeedDetail_RefreshAction_SyncsFeed` (Pflicht) | Bestanden (3/3) |
| Aktionsblatt „Umbenennen" → Titel aktualisiert | `FeedDetailTests.FeedDetail_Rename_UpdatesTitle` (Pflicht) | Bestanden (3/3) |
| Aktionsblatt „Kategorie ändern" inkl. „Keine" | `FeedDetailTests.FeedDetail_ChangeCategory_IncludingNone` (Pflicht) | Bestanden (3/3) |
| Aktionsblatt „Bearbeiten" → Edit-Sheet, URL persistiert | `FeedDetailTests.FeedDetail_Edit_PersistsChanges` (Pflicht) | Fehlgeschlagen (Suite-Lauf 1, Aktionsblatt-Eintrag „Bearbeiten" nicht gefunden; in den Läufen 2 und 3 bestanden — instabil im Suite-Kontext) |
| Aktionsblatt „Fehlerdetails anzeigen" nur bei `HealthStatus == Error` | `FeedDetailTests.FeedDetail_ErrorDetails_OnlyForErrorFeed` (Pflicht) | Bestanden (3/3) |
| Aktionsblatt „Löschen" → Bestätigung, Kaskade, Rücksprung | `FeedDetailTests.FeedDetail_Delete_ReturnsToList` (Pflicht) | Fehlgeschlagen (Suite-Lauf 3, gelöschte Karte blieb gerendert; in den Läufen 1 und 2 bestanden — instabil im Suite-Kontext) |
| Pull-to-Refresh löst Einzel-Sync aus | `FeedDetailTests.FeedDetail_PullToRefresh_SyncsFeed` (Soll) | Plangemäß durch dokumentierte manuelle Verifikation ersetzt (`docs/help/anwendung/mobile-ui-design.md`, Abschnitt „Detailansicht für Feeds (issue-115)"); der Sync-Nachweis selbst ist über `FeedDetail_RefreshAction_SyncsFeed` abgedeckt |

## Zusammenfassung

Letzter vollständiger Lauf je Suite (E2E = Suite-Lauf 3):

- Gesamt: 698 (636 `Reporter.Tests` + 26 `Reporter.E2ETests` + 36 Node-Tests)
- Bestanden: 695
- Fehlgeschlagen: 3
- Übersprungen: 0

Über alle drei E2E-Suite-Läufe summiert: 5 verschiedene Tests schlugen mindestens einmal fehl (3 davon Pflicht-E2E-Szenarien der `FeedDetailTests`), kein Suite-Lauf erreichte 26/26. Die Fehlschläge sind nicht deterministisch — jeder Lauf scheiterte an anderen Stellen, und jeder `FeedDetailTests`-Fehlschlag war in mindestens einem anderen Suite-Lauf grün.

Hinweise:

- Die Instabilität der FlaUI-Suite besteht fort: Die in `E2EPageHelpers.cs` nachgearbeiteten Helfer (`WaitForFeedCard`-`ControlType.Group`-Präferenz, verifiziertes Scrollen, 60-s-Scan) haben `FeedDetail_InfiniteScroll_LoadsNextPage` in Lauf 1 nicht gerettet — gleiche Timeout-Signatur wie vor dem Fix. In Lauf 3 traf es stattdessen `FeedDetail_Delete_ReturnsToList` an einer anderen Stelle (Assertion, kein Timeout).
- `Reporter.exe` beendete sich in Suite-Lauf 2 mitten im Test `ArticleImage_StoredLocally_AndShownOnCardAndDetail` selbst mit Code -1073741189 (0xC000027B, WinUI-Stowed-Exception) — ein App-Absturz, kein reines FlaUI-Timing-Problem. Die `ArticleImageTests`-Fehlschläge sind laut `plan-check.md` als bekannte, nicht feature-bedingte Instabilität eingeordnet; der Absturz ist dennoch erwähnenswert.
- Die E2E-Tests liefen gegen den frisch gebauten Debug-`win-x64`-`Reporter.exe` in einer interaktiven Windows-Session; die geteilte App-Instanz (`E2ETestCollection`) macht die Tests seriell und zustandsabhängig.
- Statische Checks (`Run-StaticChecks.ps1`) wurden in dieser Iteration nicht erneut ausgeführt (kein Code geändert).

## Testabdeckung

**Abdeckung:** 49,6 % Zeilenabdeckung (Cobertura `coverage.cobertura.xml` über `dotnet test src/Reporter.Tests --collect:"XPlat Code Coverage"`; nur `Reporter.Tests` erfasst `Reporter.Core`/`Reporter.Data` — die MAUI-App `src/Reporter` und das E2E-Projekt haben keine eigene Abdeckung)

| Datei | Abdeckung |
|-------|-----------|
| `Reporter.Core\Models\CategoryFilterItem.cs` | 22,2 % |
| `Reporter.Core\Resources\Strings\AppResources.Designer.cs` | 25,0 % (generiert) |
| `Reporter.Core\Services\FeedSearchUnavailableException.cs` | 33,3 % |
| `Reporter.Data\Migrations\Content\20260919084558_AddItemContentImageColumns.cs` | 60,7 % (generiert) |
| `Reporter.Data\Migrations\Content\20260918022828_InitialContentCreate.cs` | 76,5 % (generiert) |
| `Reporter.Data\Migrations\*` (23 Dateien, inkl. `ReporterDbContextModelSnapshot.cs`) | 0 % (generiert) |

Die Feature-Dateien liegen oberhalb der 80-%-Marke: `FeedDetailViewModel.cs` 91,8 %, `FeedUrlValidator.cs` 100 %, `ItemRepository.cs` 95,5 %, `FeedsViewModel.cs` 99,1 %, `FeedsViewModel.Search.cs` 95,9 %.

## Fehlende Tests

Quelle: `Coverage-Daten`

- `src/Reporter.Data/Migrations/*` — 0 % Abdeckung (generierte EF-Core-Migrationen inkl. `*Designer.cs` und `ReporterDbContextModelSnapshot.cs`, üblicherweise ungetestet).
- `src/Reporter.Data/Migrations/Content/*` — teilweise 0 % Abdeckung (generierte EF-Core-Migrationen).
- Keine nicht-generierte Quelldatei mit 0 % Abdeckung gefunden.
