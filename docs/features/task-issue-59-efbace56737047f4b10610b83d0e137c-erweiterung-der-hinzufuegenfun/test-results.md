<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

Ausgeführt: `dotnet build Reporter.sln` (Debug: 0 Fehler, 1 bekannte iOS-NULL-Zulässigkeitswarnung `CS8765` in `Platforms/iOS/AppDelegate.cs`), `dotnet build Reporter.sln -c Release` (0 Fehler — stellt die Release-Binaries für `--no-build` bereit und deckt den MAUI-Windows-Release-Build ab), `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release --no-build --logger "console;verbosity=normal" --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage"`, `npm test` (`node --test "scripts/*.test.mjs"`).

Dritte Ausführung dieser Runde (Re-Verifikation nach Iteration 3 der Codeänderungen: `TryPersistNewFeedAsync`/`FinishAddFlowAsync`/`HandleSearchFailure`-Helper, `IsEditMode`-Guard für `DirectAddCommand`, `ToFeed`-Helper, positionsbasierte ActionSheet-Auflösung, Meta-URL-Trigger, `MinimumHeightRequest`, Offline-Hinweis-Edit-Mode-Trigger). Ergebnis identisch zu `test-results.2.md`: alle automatisierten Tests grün (311 .NET statt 309 — zwei neue Tests `DirectAddCommand_WhenDuplicate_ClearsStaleSearchError` und `DirectAddCommand_InEditMode_DoesNotAddFeed`), Coverage nahezu unverändert (90,8 % statt 90,9 %).

## E2E-Abdeckung

Kein UI-Test-Framework im Projekt (MAUI-App nicht im Testprojekt) — Nachweisform laut Plan: dokumentierte manuelle UI-Verifikation auf Windows-Handy-Fenster 390 × 844 pt (UIA via `test-results/issue-59/uia.ps1`, Screenshots `test-results/issue-59/manual-2-*.png` / `manual-3-*.png` / `manual-4-*.png` / `manual-5-*.png`, dokumentiert in Root-`test-results.md` und `docs/help/anwendung/mobile-ui-design.md`) plus ViewModel-/Service-Tests. Manuelle Verifikation wurde durchgeführt; alle Pflicht-Szenarien sind mit Screenshots belegt.

| Szenario | Test / Testklasse / Nachweis | Ergebnis |
|----------|------------------------------|----------|
| Standardansicht (nur Liste + „+"-Button), Sheet öffnet als Bottom-Sheet, Fokus im URL-Feld | Manuell: `manual-2-01`, `manual-2-02` (Dark), `manual-3-01`, `manual-3-02` (Light), `manual-4-01`, `manual-5-01` (Dark); `OpenAddFormCommand_ShowsForm`, `CloseAddFormCommand_ResetsFormAndHidesForm`, `OpenAddFormCommand_AfterEdit_ClearsStaleEditState` | Bestanden (manuell + Tests) |
| URL eingeben → „Suchen" → Sheet schließt, Trefferliste ersetzt Feed-Liste → Treffer tippen → Bestätigung → Feed in Liste | Manuell: `manual-2-03`…`manual-2-06`; `SearchCommand_WhenResultsShown_ClosesAddForm`, `CloseSearchResultsCommand_ExitsResultsView`, `SubscribeResultCommand_PersistsFeedFromResult`, `SubscribeResultCommand_WhenDuplicate_SetsErrorAndDoesNotAdd` | Bestanden (manuell + Tests) |
| Treffer ohne Titel abonnieren → Dateiname als Titel; nach Sync echter Titel | `FeedTitleFallbackTests`, `SubscribeResultCommand_WhenTitleEmpty_StoresFileNameAsPlaceholder`, `SyncFeedAsync_WhenTitleIsFileNamePlaceholder_UpdatesTitleFromFeedDocument`, `SyncFeedAsync_WhenTitleIsSet_DoesNotOverwriteTitle`; manuell `manual-2-15`, `manual-5-06` (Meta-URL-Zeile bei leerem Titel ausgeblendet) | Bestanden |
| Direkt-Hinzufügen: Suche ohne Treffer → Dialog → „Ja" → Feed sofort in Liste (`mein-feed.xml`, `category_id=NULL`, `notifications_enabled=1`); Ablehnung ohne Persistieren | Manuell: `manual-2-13`, `manual-2-14` (SQLite-geprüft); `SearchCommand_NoResultsAndValidUrl_*`, `SearchCommand_WhenUnavailableAndConfirmedDuplicate_KeepsSheetOpenAndSetsError` | Bestanden (manuell + Tests) |
| Kontextmenü → „Umbenennen" → Prompt mit Vorbelegung → neuer Titel in Liste; leerer Titel → Fehler, Titel unverändert | Manuell: `manual-2-07`, `manual-2-08`; `RenameFeedAsync_UpdatesTitle`, `RenameFeedAsync_EmptyTitle_SetsErrorAndKeepsTitle`, `RenameFeedAsync_NullFeed_DoesNothing` | Bestanden (manuell + Tests) |
| Kontextmenü → „Kategorie ändern" → Auswahl inkl. „Keine Kategorie" (`CategoryNone`) → `CategoryName` ändert sich; Namens-Dubletten positionsbasiert aufgelöst („News" / „News (2)") | Manuell: `manual-2-09` (`category_id` in SQLite geprüft), `manual-4-04` (Klartext statt „—", Cancel-Kollisions-Filter), `manual-5-04`, `manual-5-05` (Dubletten-Disambiguierung, SQLite-geprüft); `ChangeFeedCategoryAsync_SetsCategoryId`, `ChangeFeedCategoryAsync_EmptyGuid_ClearsCategory`, `ChangeFeedCategoryAsync_NullArguments_DoNothing` | Bestanden (manuell + Tests) |
| Kontextmenü → „Bearbeiten" → Sheet im Edit-Modus (URL + Switch + Speichern) → URL ändern speichert | Manuell: `manual-2-10`, `manual-4-05`, `manual-5-07` (kein „Suchen"/„URL direkt hinzufügen"/Offline-Hinweis im Edit-Modus); `EditAsync_OpensSheetInEditMode`, `SaveCommand_*`-Tests, `SaveCommand_WithoutSelectedFeed_DoesNotAddFeed`, `SearchCommand_InEditMode_DoesNotDiscardEdit`, `DirectAddCommand_InEditMode_DoesNotAddFeed` | Bestanden (manuell + Tests) |
| Fehlerfälle bei geöffnetem Sheet: `ErrorFeedUrlInvalid`/`ErrorFeedDuplicate` (Edit-Speichern), `FeedSearchUnavailableRetry` (Domain-Suche) — jeweils im Sheet sichtbar | Manuell: `manual-2-11`, `manual-2-12`, `manual-2-16`; `SaveCommand_EditMode_WhenInvalidUrl_KeepsSheetOpenAndSetsError`, `SearchCommand_WhenUnavailable_KeepsSheetOpenAndSetsSearchError`, `DirectAddCommand_WhenDuplicate_ClearsStaleSearchError` | Bestanden (manuell + Tests) |
| „URL direkt hinzufügen"-Button im Sheet (offline aktiv): URL ohne Suche/Dialog persistiert (Dateinamen-Titel, `category_id=NULL`, `notifications_enabled=1`); ungültige URL/Dublette → Fehler im offenen Sheet | Manuell: `manual-4-02`, `manual-4-03` (Dark), `manual-4-06`, `manual-4-07` (Light), SQLite-geprüft, `manual-5-02` (44-pt-Touch-Ziele); `DirectAddCommand_WhenOffline_PersistsFeedWithDefaults`, `DirectAddCommand_DomainInput_NormalizesToHttpsUrl`, `DirectAddCommand_WhenUrlInvalid_SetsErrorAndKeepsSheetOpen`, `DirectAddCommand_WhenDuplicate_SetsErrorAndKeepsSheetOpen` | Bestanden (manuell + Tests; Offline-Pfad per Test) |
| `FeedSearchOfflineHint` + deaktivierter „Suchen"-Button im Sheet; Hinweis im Edit-Modus ausgeblendet | Nicht interaktiv verifizierbar (Offline-Schaltung ohne Adminrechte nicht möglich); abgedeckt durch `SearchCommand_WhenOffline_SkipsSearchWithoutError`, `ConnectivityChanged_UpdatesSearchCommandCanExecute`, `DirectAddCommand_WhenOffline_PersistsFeedWithDefaults`; Edit-Modus-`DataTrigger` deckungsgleich mit dem live verifizierten Trigger der Buttons (`manual-5-07`) | Bestanden (Tests + XAML-Trigger-Nachweis, manuell dokumentiert) |
| System-Zurück bei offenem Sheet schließt Sheet; Backdrop-Tap schließt; Dark Mode, Design-Abgleich, 390 × 844 pt | Manuell: System-Zurück via XButton1 (`uia.ps1 -Action sysback`), Backdrop-Tap, Dark- und Light-Screenshots, `GetWindowRect`-verifiziert 390 × 844 pt | Bestanden (manuell) |

## Zusammenfassung

- Gesamt: 347 (311 .NET + 36 Node.js)
- Bestanden: 347
- Fehlgeschlagen: 0
- Übersprungen: 0

## Testabdeckung

**Abdeckung:** 90,8 % Zeilenabdeckung gesamt (`coverage.cobertura.xml`, `line-rate` 0.9083, `branch-rate` 0.8575; Reporter.Core + Reporter.Data, Migrations ausgeschlossen)

| Datei | Abdeckung |
|-------|-----------|
| `Reporter.Core\Resources\Strings\AppResources.Designer.cs` | 22,8 % (generierte Datei — nicht bewertet) |
| `Reporter.Core\Services\FeedSearchUnavailableException.cs` | 33,3 % |

Alle übrigen Quelldateien ≥ 80 % Zeilenabdeckung; keine Datei mit 0 % Abdeckung.
