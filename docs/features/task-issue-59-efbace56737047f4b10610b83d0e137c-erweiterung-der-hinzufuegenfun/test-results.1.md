<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

Ausgeführt: `dotnet build src/Reporter.Tests/Reporter.Tests.csproj -c Release` (0 Fehler, 0 Warnungen), `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release --no-build --logger "console;verbosity=normal" --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage"`, `npm test` (`node --test "scripts/*.test.mjs"`).

## E2E-Abdeckung

Kein UI-Test-Framework im Projekt (MAUI-App nicht im Testprojekt) — Nachweisform laut Plan: dokumentierte manuelle UI-Verifikation auf Windows-Handy-Fenster 390 × 844 pt (UIA via `test-results/issue-59/uia.ps1`, Screenshots `test-results/issue-59/manual-2-*.png` Dark / `manual-3-*.png` Light, dokumentiert in Root-`test-results.md` Z. 79–131 und `docs/help/anwendung/mobile-ui-design.md`) plus ViewModel-/Service-Tests. Manuelle Verifikation wurde bereits durchgeführt; alle Pflicht-Szenarien sind mit Screenshots belegt.

| Szenario | Test / Testklasse / Nachweis | Ergebnis |
|----------|------------------------------|----------|
| Standardansicht (nur Liste + „+"-Button), Sheet öffnet als Bottom-Sheet, Fokus im URL-Feld | Manuell: `manual-2-01`, `manual-2-02` (Dark), `manual-3-01`, `manual-3-02` (Light); `OpenAddFormCommand_ShowsForm`, `CloseAddFormCommand_ResetsFormAndHidesForm` | Bestanden (manuell + Tests) |
| URL eingeben → „Suchen" → Sheet schließt, Trefferliste ersetzt Feed-Liste → Treffer tippen → Bestätigung → Feed in Liste | Manuell: `manual-2-03`…`manual-2-06`; `SearchCommand_WhenResultsShown_ClosesAddForm`, `CloseSearchResultsCommand_ExitsResultsView`, `SubscribeResultCommand_PersistsFeedFromResult`, `SubscribeResultCommand_WhenDuplicate_SetsErrorAndDoesNotAdd` | Bestanden (manuell + Tests) |
| Treffer ohne Titel abonnieren → Dateiname als Titel; nach Sync echter Titel | `FeedTitleFallbackTests`, `SubscribeResultCommand_WhenTitleEmpty_StoresFileNameAsPlaceholder`, `SyncFeedAsync_WhenTitleIsFileNamePlaceholder_UpdatesTitleFromFeedDocument`, `SyncFeedAsync_WhenTitleIsSet_DoesNotOverwriteTitle`; manuell `manual-2-15` | Bestanden |
| Direkt-Hinzufügen: Suche ohne Treffer → Dialog → „Ja" → Feed sofort in Liste (`mein-feed.xml`, `category_id=NULL`, `notifications_enabled=1`); Ablehnung ohne Persistieren | Manuell: `manual-2-13`, `manual-2-14` (SQLite-geprüft); `SearchCommand_NoResultsAndValidUrl_*`, `SearchCommand_WhenUnavailableAndConfirmedDuplicate_KeepsSheetOpenAndSetsError` | Bestanden (manuell + Tests) |
| Kontextmenü → „Umbenennen" → Prompt mit Vorbelegung → neuer Titel in Liste; leerer Titel → Fehler, Titel unverändert | Manuell: `manual-2-07`, `manual-2-08`; `RenameFeedAsync_UpdatesTitle`, `RenameFeedAsync_EmptyTitle_SetsErrorAndKeepsTitle`, `RenameFeedAsync_NullFeed_DoesNothing` | Bestanden (manuell + Tests) |
| Kontextmenü → „Kategorie ändern" → Auswahl inkl. „—" (`CategoryNone`) → `CategoryName` ändert sich | Manuell: `manual-2-09` (`category_id` in SQLite geprüft); `ChangeFeedCategoryAsync_SetsCategoryId`, `ChangeFeedCategoryAsync_EmptyGuid_ClearsCategory`, `ChangeFeedCategoryAsync_NullArguments_DoNothing` | Bestanden (manuell + Tests) |
| Kontextmenü → „Bearbeiten" → Sheet im Edit-Modus (URL + Switch + Speichern) → URL ändern speichert | Manuell: `manual-2-10`; `EditAsync_OpensSheetInEditMode`, `SaveCommand_*`-Tests | Bestanden (manuell + Tests) |
| Fehlerfälle bei geöffnetem Sheet: `ErrorFeedUrlInvalid`/`ErrorFeedDuplicate` (Edit-Speichern), `FeedSearchUnavailableRetry` (Domain-Suche) — jeweils im Sheet sichtbar | Manuell: `manual-2-11`, `manual-2-12`, `manual-2-16`; `SaveCommand_EditMode_WhenInvalidUrl_KeepsSheetOpenAndSetsError`, `SearchCommand_WhenUnavailable_KeepsSheetOpenAndSetsSearchError` | Bestanden (manuell + Tests) |
| `FeedSearchOfflineHint` + deaktivierter „Suchen"-Button im Sheet | Nicht interaktiv verifizierbar (Offline-Schaltung ohne Adminrechte nicht möglich); abgedeckt durch `SearchCommand_WhenOffline_SkipsSearchWithoutError`, `ConnectivityChanged_UpdatesSearchCommandCanExecute` | Bestanden (Tests, manuell nicht möglich — dokumentiert) |
| System-Zurück bei offenem Sheet schließt Sheet; Backdrop-Tap schließt; Dark Mode, Design-Abgleich, 390 × 844 pt | Manuell: System-Zurück via XButton1 (`uia.ps1 -Action sysback`), Backdrop-Tap, Dark- und Light-Screenshots, `GetWindowRect`-verifiziert 390 × 844 pt | Bestanden (manuell) |
|| „URL direkt hinzufügen"-Button im Sheet (offline aktiv): URL ohne Suche/Dialog persistiert (Dateinamen-Titel, `category_id=NULL`, `notifications_enabled=1`); Domain-Normalisierung; ungültige URL/Dublette → Fehler im offenen Sheet | Manuell: `manual-4-02`, `manual-4-03` (Dark), `manual-4-06`, `manual-4-07` (Light), SQLite-geprüft; `DirectAddCommand_WhenOffline_PersistsFeedWithDefaults`, `DirectAddCommand_DomainInput_NormalizesToHttpsUrl`, `DirectAddCommand_WhenUrlInvalid_SetsErrorAndKeepsSheetOpen`, `DirectAddCommand_WhenDuplicate_SetsErrorAndKeepsSheetOpen` | Bestanden (manuell + Tests; Offline-Pfad per Test, Live-Offline nicht möglich) |
|| Kategorie-ActionSheet: Pseudo-Eintrag mit Klartext „Keine Kategorie" (statt „—"); Cancel-Kollision ausgeschlossen (Optionen ≠ `ButtonCancel`) | Manuell: `manual-4-04` (Dark); VM-Seite über `ChangeFeedCategoryAsync_*`-Tests | Bestanden (manuell + Tests) |
|| „Suchen"/„URL direkt hinzufügen" im Edit-Modus ausgeblendet (DataTrigger `IsEditMode`); `SearchCommand` zusätzlich per CanExecute/`SearchAsync`-Guard im Edit-Modus inaktiv (Enter-Taste verwirft Bearbeitung nicht mehr) | Manuell: `manual-4-05` (Dark, Sheet zeigt nur URL + Switch + „Speichern"); `SearchCommand_InEditMode_DoesNotDiscardEdit` | Bestanden (manuell + Test) |
|| Dubletten-Direkt-Add aus leerer Trefferansicht schließt Trefferansicht, Sheet öffnet über Feed-Liste | `SearchCommand_NoResultsAndConfirmedDuplicate_ClosesResultsView` (vor Fix rot) | Bestanden (Test) |
|| `OpenAddForm` nach verworfenem Edit startet sauber (kein `SelectedFeed`, leere Felder) | `OpenAddFormCommand_AfterEdit_ClearsStaleEditState` (vor Fix rot) | Bestanden (Test) |
|| `SaveAsync` ohne `SelectedFeed` persistiert nichts (Add-Zweig entfernt) | `SaveCommand_WithoutSelectedFeed_DoesNotAddFeed` | Bestanden (Test) |

## Zusammenfassung

- Gesamt: 345 (309 .NET + 36 Node.js)
- Bestanden: 345
- Fehlgeschlagen: 0
- Übersprungen: 0

## Testabdeckung

**Abdeckung:** 90,9 % Zeilenabdeckung gesamt (`coverage.cobertura.xml`, `line-rate` 0.9085; Reporter.Core + Reporter.Data, Migrations ausgeschlossen)

| Datei | Abdeckung |
|-------|-----------|
| `Reporter.Core\Resources\Strings\AppResources.Designer.cs` | 22,8 % (generierte Datei — nicht bewertet) |
| `Reporter.Core\Services\FeedSearchUnavailableException.cs` | 33,3 % |

Alle übrigen Quelldateien ≥ 80 % Zeilenabdeckung; keine Datei mit 0 % Abdeckung.
