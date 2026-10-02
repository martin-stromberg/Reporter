<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ergebnisse

## Ergebnis

**Status:** Fehler vorhanden

Ein nicht zum Feature gehörender E2E-Test ist im Suitenlauf fehlgeschlagen (`FeedDetail_Edit_FeedKeywordValidation_ShowsErrors`, Stichwort-Validierung im Edit-Sheet — ein Codepfad, den dieser Change nicht berührt). Der Test ist instabil: Er schlug diesmal zweimal in Folge fehl (Suitenlauf und erste isolierte Wiederholung, jeweils an `FeedDetailTests.cs:635`), bestand dann in der zweiten isolierten Wiederholung — im vorherigen Lauf (Iteration 2) war er an einer anderen Stelle (`:632`) fehlgeschlagen und im Einzellauf sofort grün. Alle geplanten E2E-Szenarien des Features haben bestanden.

## Fehlgeschlagene Tests

### FeedDetailTests (`src/Reporter.E2ETests`, FlaUI/UIA3)

- **FeedDetail_Edit_FeedKeywordValidation_ShowsErrors** — `System.TimeoutException : Element not found within 00:00:20: name 'Dieses Schlagwort existiert bereits.'` (`FeedDetailTests.cs:635`, Warten auf das Duplikat-Fehlerlabel `AppResources.ErrorKeywordDuplicate` nach erneutem Hinzufügen von `e2e-dup`). Flaky: Fehlschlag im Suitenlauf und in der ersten isolierten Wiederholung (`--filter 'FullyQualifiedName~FeedDetail_Edit_FeedKeywordValidation_ShowsErrors'`, identische Reporter.exe, beide `:635`), bestanden in der zweiten isolierten Wiederholung (22 s). In Iteration 2 schlug derselbe Test einmalig an `:632` fehl (Warten auf den Stichwort-Chip) und bestand im Einzellauf — die wechselnden Fehlerstellen innerhalb desselben Tests sprechen für UIA-Timing-Flakiness, nicht für einen Produktfehler. Der betroffene Fluss (Edit-Sheet, Stichwort-Entry/-Button/-Chips) wurde durch diesen Change nicht verändert (`FeedDetailPage.xaml` unverändert; `FeedDetailViewModel`-Diff nur `GetFeedMessage`/`ToFeed`-Umbenennung; das Duplikat-Verhalten ist zusätzlich durch den bestandenen Unit-Test `AddFeedKeyword_Duplicate_ShowsError` abgesichert).

## E2E-Abdeckung

Ausgeführt via `scripts/Run-E2ETests.ps1` (interaktive Desktop-Session, FlaUI/UIA3): 32 Tests, 31 bestanden, 1 fehlgeschlagen (siehe oben), 0 übersprungen, Dauer 7 m 52 s. Beide Pflicht-Szenarien sind in der Suite ausgeführt worden und haben bestanden.

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Warnungsgrund aus der UI einsehbar: `stale-feed` anlegen → 1. Sync (`Ok`) → Detailseite → Aktualisieren → 2. Sync (`Warning`) → Badge `HealthStatusWarningLabel` → Eintrag „Meldung anzeigen" (`ButtonShowMessage`) → Alert mit `FeedWarningKindNoRecentItems` + technischem Absatz → Negativnachweis gesunder Feed ohne Eintrag | `FeedDetailTests.FeedDetail_Message_ForWarningFeed` (`src/Reporter.E2ETests/FeedDetailTests.cs:464`, Fixture `src/Reporter.E2ETests/Fixtures/stale-feed.xml`) | Bestanden |
| Fehlerfall weiterhin über den gemeinsamen „Meldung"-Eintrag erreichbar: 404-Feed → Sync schlägt fehl → `SyncStatusError` → Eintrag `ButtonShowMessage` → Alert mit `FeedErrorKindHttpStatus` → Negativnachweis gesunder Feed ohne Eintrag | `FeedDetailTests.FeedDetail_Message_ForErrorFeed` (`src/Reporter.E2ETests/FeedDetailTests.cs:410`) | Bestanden |
| Sichtbarkeitsregel `Error or Warning` / Absenz bei `Ok` | Negativnachweis in beiden Pflicht-Tests enthalten | Bestanden |
| `FewerItems`-Variante über UI (im Plan als Optional markiert) | — nicht implementiert; Kind über `FeedSyncServiceTests.SyncFeedAsync_FewerItems_PersistsWarningMessage` und `FeedDetailViewModelTests.GetFeedMessage_FewerItemsWarning_MapsToLocalizedText` abgesichert | Nicht ausgeführt (optional laut Plan, nicht erforderlich) |

## Zusammenfassung

| Testprojekt | Gesamt | Bestanden | Fehlgeschlagen | Übersprungen |
|-------------|--------|-----------|----------------|--------------|
| `Reporter.Tests` (Unit/Integration, `dotnet test --no-build --collect:"XPlat Code Coverage"` nach erfolgreichem `dotnet build Reporter.sln`) | 677 | 677 | 0 | 0 |
| `Reporter.E2ETests` (FlaUI/UIA3, `scripts/Run-E2ETests.ps1`) | 32 | 31 | 1 | 0 |
| **Gesamt** | **709** | **708** | **1** | **0** |

Neu in diesem Lauf: `FeedSyncServiceTests_Concurrency.SyncFeedAsync_ConcurrentCallsOnSameFeed_AreSerialized` (379 ms) — bestanden, womit die Unit-Suite von 676 auf 677 Tests gewachsen ist.

Build-Voraussetzung: `dotnet build Reporter.sln` (Debug, `IncludeIosTarget=false`, `IncludeAndroidTarget=false`) — 0 Fehler, 0 Warnungen. Das E2E-Skript hat die App anschließend erneut gebaut (0 Fehler, 0 Warnungen).

## Testabdeckung

**Abdeckung:** 86,7 % Zeilenabdeckung gesamt über die instrumentierten Assemblys `Reporter.Core` (83,7 %) und `Reporter.Data` (96,6 % — Migrations-Klassen analog `coverlet.runsettings` `[*]Reporter.Data.Migrations.*` aus der Auswertung ausgenommen). Die MAUI-App `Reporter` (Windows-TFM) ist in der Unit-Test-Coverage nicht instrumentiert; UI-Flüsse werden über die E2E-Suite abgedeckt.

Quelle: `src/Reporter.Tests/TestResults/b0296d35-f238-4360-8658-2a387b6cd441/coverage.cobertura.xml` (XPlat Code Coverage), dateiweise aggregiert aus den Cobertura-Klassen-Einträgen.

| Datei | Abdeckung |
|-------|-----------|
| `Reporter.Core\Models\CategoryFilterItem.cs` | 22 % |
| `Reporter.Core\Resources\Strings\AppResources.Designer.cs` | 26 % (generierte Datei) |
| `Reporter.Core\Services\FeedSearchUnavailableException.cs` | 33 % |
| `Reporter.Data\Entities\Keyword.cs` | 75 % |

## Fehlende Tests

Quelle: `Coverage-Daten`

- Keine Quelldatei mit 0 % Zeilenabdeckung gefunden.
- `src/Reporter.Core/Services/FeedSyncWarningKind.cs` — neue Konstantenklasse ohne ausführbare Zeilen (nicht in der Coverage-Datei); hat bewusst keine eigene Testklasse (Muster `FeedSyncErrorKind`, dessen einzige Logik `Classify` kein Pendant hier hat) — die Werte sind indirekt über `FeedSyncServiceTests`/`FeedDetailViewModelTests` und den E2E-Test `FeedDetail_Message_ForWarningFeed` abgesichert.
