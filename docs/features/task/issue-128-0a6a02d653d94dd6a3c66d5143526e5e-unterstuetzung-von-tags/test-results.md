<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ergebnisse

## Ergebnis

**Status:** Fehler vorhanden

Begründung: Alle 753 Unit-/Integrationstests (`src/Reporter.Tests`) sind bestanden, und alle drei als Pflicht geplanten Tag-E2E-Szenarien wurden ausgeführt und bestanden. Der Status ergibt sich aus einem einzelnen fehlgeschlagenen E2E-Test, der nicht zu diesem Feature gehört und nach allen verfügbaren Belegen vorbestehend auf `staging` ist (siehe „Fehlgeschlagene Tests").

## Fehlgeschlagene Tests

### Reporter.E2ETests

- **`FeedDetailTests.FeedDetail_MetaInfo_WrapsInsteadOfOverlappingActionsButton`** — `System.TimeoutException : Element not found within 00:00:20: name 'meta-wrap-target.xml'` (`FeedDetailTests.cs:401` via `OpenFeedDetail` → `WaitForFeedCard`).

  **Einordnung — vorbestehender Testfehler, feature-fremd:** Der Test stammt aus Issue #127 (Commit `146e2fd`) und ist auf `origin/staging` unverändert enthalten. Er übergibt den Pre-Sync-Kartentitel `meta-wrap-target.xml` (URL-abgeleitet) an `OpenFeedDetail`, obwohl die Karte nach `SyncAllViaUnreadTab()` den Kanaltitel `Stub Feed meta-wrap-target` trägt — dieselbe Namenskonvention, die alle Geschwistertests (`"Stub Feed nav-target"`, `"Stub Feed paged-feed"` u. a.) erfolgreich verwenden. Der Fehlschlag ist deterministisch reproduzierbar (isolation rerun, gleiches Bild). Der Branch ändert weder Feed-Benennung, Sync-Ablauf, Kartentitel noch Navigation. Formeller Vorbehalt: In der Bestandsaufnahme (`inventory/tests.md`) wurde kein E2E-Baseline-Lauf ausgeführt (Umgebungsannahme: keine interaktive Session) — der Fehler kann daher nicht als im Baseline-Lauf nachgewiesen gelten, sondern nur als durch Codeanalyse belegt.

## E2E-Abdeckung

Die E2E-Suite wurde am 2026-10-16 lokal via `scripts/Run-E2ETests.ps1` ausgeführt (Debug-Build `net10.0-windows10.0.19041.0/win-x64`, interaktive Session): 36 Tests, **35 bestanden, 1 fehlgeschlagen** (siehe oben), 0 übersprungen.

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Feed mit `<app:tags>`/`<category>` synchronisieren → Tag-Zeile am Ende des Artikeltexts im WebView sichtbar | `ArticleTagsTests.ArticleDetail_Tags_ShownBelowArticleText` (`src/Reporter.E2ETests/ArticleTagsTests.cs:71`, Fixture `src/Reporter.E2ETests/Fixtures/tag-feed.xml`) | Bestanden |
| Tag-Persistenz in `items.tags` der realen App-Datenbank nach Sync | `ArticleTagsTests.ArticleDetail_Tags_PersistedInDatabase` (`src/Reporter.E2ETests/ArticleTagsTests.cs:128`) | Bestanden |
| Suchfeld in der Feed-Detailansicht → Tag-Treffer filtert die Artikelliste | `FeedDetailTests.FeedDetail_Search_FiltersByTag` (`src/Reporter.E2ETests/FeedDetailTests.cs:290`) | Bestanden |

## Zusammenfassung

### Unit-/Integrationstests (`src/Reporter.Tests`)

- Gesamt: 753
- Bestanden: 753
- Fehlgeschlagen: 0
- Übersprungen: 0

Ausgeführt: `dotnet build Reporter.sln -c Release` (0 Fehler, 0 Warnungen), danach `dotnet test src/Reporter.Tests --no-build -c Release --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"` (Laufzeit: 5,4 s).

### E2E-Tests (`src/Reporter.E2ETests`)

- Gesamt: 36
- Bestanden: 35
- Fehlgeschlagen: 1 (vorbestehend, feature-fremd)
- Übersprungen: 0

Ausgeführt: `scripts/Run-E2ETests.ps1` (Debug-Build, FlaUI/UIA3, Laufzeit 10 m 35 s).

## Testabdeckung

**Abdeckung:** 40,6 % Zeilen (Cobertura-Gesamtwert über alle instrumentierten Dateien; 4.093/10.072 Zeilen, 87,8 % Zweige). Der Gesamtwert wird durch generierte EF-Core-Migrationsdateien verzerrt — **ohne generierte Dateien** (`*.Designer.cs`, `Migrations/`): **96,2 %** (3.869/4.023 Zeilen).

Die für dieses Feature neuen/geänderten Dateien sind nahezu vollständig abgedeckt: `ArticleHtmlBuilder.cs` 100 %, `ItemRepository.cs` 98,1 %, `FeedSyncService.cs` 97,5 %, `FeedDetailViewModel.cs` 94,6 %.

Dateien unter 80 % (generierte Dateien ausgenommen):

| Datei | Abdeckung |
|-------|-----------|
| `src/Reporter.Core/Models/CategoryFilterItem.cs` | 28,6 % |
| `src/Reporter.Core/Services/FeedSearchUnavailableException.cs` | 33,3 % |
| `src/Reporter.Data/Entities/Keyword.cs` | 75,0 % |
| `src/Reporter.Core/Services/Atom03NormalizingXmlReader.cs` | 78,4 % |

## Fehlende Tests

Quelle: `Coverage-Daten`

Keine nicht-generierte Quelldatei hat 0 % Zeilenabdeckung. Sämtliche Dateien mit 0 % Abdeckung sind generierte EF-Core-Migrationsdateien unter `src/Reporter.Data/Migrations/` (inkl. `ReporterDbContextModelSnapshot.cs`, auch die neue `20261017045445_AddItemTags*.cs`) sowie `AppResources.Designer.cs` — diese werden gemäß Testanleitung ignoriert. Coverage-Report: `src/Reporter.Tests/TestResults/5ba06bfa-eebf-4730-b85d-7d7b88d64016/coverage.cobertura.xml`.
