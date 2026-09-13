<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

Hinweis: Kein automatisierter Test ist fehlgeschlagen (274/274 .NET, 36/36 Node bestanden). Die manuelle UI-Verifikation wurde in Iteration 2 im Windows-Handy-Fenster 390 × 844 pt (per `GetWindowRect` verifiziert) in Light- und Dark-Mode durchgeführt; 12 Screenshots unter `test-results/issue-59/manual-*.png`, Details im Repo-Root `test-results.md` und in `docs/help/anwendung/mobile-ui-design.md`. Zwei E2E-Szenarien sind in dieser Umgebung nicht interaktiv ausführbar (Umgebungs-Ausschlüsse mit Begründung siehe E2E-Abdeckung) und durch bestehende automatisierte Tests abgedeckt.

## Fehlgeschlagene Tests

Keine — alle automatisierten Tests bestanden; die manuell verifizierbaren E2E-Szenarien wurden bestanden (siehe E2E-Abdeckung).

## E2E-Abdeckung

Das Projekt besitzt keine UI-/E2E-Testinfrastruktur (Plan-Begründung: kein UI-Runner, kein Emulator-Lauf; AGENTS.md lässt dokumentierte manuelle Verifikation ausdrücklich zu). Der Plan definiert als Funktionsnachweis je Szenario: manuelle UI-Verifikation (390 × 844 pt, Light + Dark) + benannte ViewModel-/Service-Tests. In Iteration 2 wurde der unpackaged `win-x64`-Release-Build gestartet und über UI Automation (`test-results/issue-59/uia.ps1`) interaktiv verifiziert; Screenshots unter `test-results/issue-59/manual-*.png`.

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Domain-/URL-Suche → Trefferliste → Tap → Confirm → Feed abonniert und in Liste sichtbar | `FeedsViewModelTests.SearchCommand_PopulatesSearchResults`, `FeedsViewModelTests.SubscribeResultCommand_PersistsFeedFromResult` + manuelle UI-Verifikation | Bestanden (manuell verifiziert: `tagesschau.de`/`heise.de` → Trefferkarten → „Feed abonnieren?"-Dialog → „Ja" → Feed in Liste + SQLite-persistiert; `manual-04`/`manual-09` Dark, `manual-11` Light, `manual-05`, `manual-06`) |
| Treffer ohne Titel abonnieren → Titel nach erstem Sync aus Feed-Dokument | `FeedsViewModelTests.SubscribeResultCommand_WhenTitleEmpty_StoresFeedUrlAsPlaceholder`, `FeedSyncServiceTests.SyncFeedAsync_WhenTitleIsPlaceholder_UpdatesTitleFromFeedDocument` | Nicht ausführbar (manuell): localhost-Stub-Server vom App-Prozess aus nicht erreichbar (Anfragen kommen nicht am Stub an); fachlicher Kern durch beide Tests bestanden — dokumentiert im Repo-Root `test-results.md` |
| URL-Eingabe ohne Treffer → Confirm-Dialog → „Ja" → Formular vorbefüllt → `SaveCommand` speichert | `FeedsViewModelTests.SearchCommand_NoResultsAndValidUrl_Confirmed_PrefillsTitleFromHost`, `SearchCommand_NoResultsAndValidUrl_Declined_KeepsFormState` + manuelle UI-Verifikation | Bestanden (manuell verifiziert: `https://example.com/`/`https://iana.org/` → Dialog nennt die Adresse → „Ja" → `NewTitle`=`example.com` per UIA ausgelesen → „Speichern" persistiert; `manual-12` Light, `manual-03` Dark) |
| Suche nicht erreichbar (Timeout/HTTP-Fehler beider Quellen) → `FeedSearchUnavailable`-Hinweis + Fallback nutzbar | `FeedsViewModelTests.SearchCommand_WhenServiceUnavailable_SetsSearchErrorMessage`, `SearchCommand_WhenServiceUnavailableAndDomainInput_SetsRetryHint`, `FeedSearchServiceTests.SearchAsync_Timeout_ThrowsFeedSearchUnavailableException`, `SearchAsync_WhenCallerCancels_ThrowsOperationCanceledException` + manuelle UI-Verifikation | Bestanden (manuell verifiziert via `127.0.0.1:8099`-Query: beide Quellen fehlgeschlagen → Hinweis „Die Feed-Suche ist nicht erreichbar. …" + Direkt-Hinzufügen-Dialog nutzbar; `manual-03`) |
| Offline: Suchen-Button deaktiviert + `FeedSearchOfflineHint` sichtbar; direkte URL-Hinzufügung weiter möglich | `FeedsViewModelTests.SearchCommand_WhenOffline_DoesNotCallService_AndSetsHint`, `FeedsViewModelTests.ConnectivityChanged_UpdatesSearchCommandCanExecute` | Nicht ausführbar (manuell): Netzwerkadapter-Deaktivierung ohne Adminrechte nicht möglich („Zugriff verweigert"); fachlicher Kern durch beide Tests bestanden — dokumentiert im Repo-Root `test-results.md` |
| Dubletten-Treffer abonnieren → `ErrorFeedDuplicate`, kein zweiter Feed | `FeedsViewModelTests.SubscribeResultCommand_WhenDuplicate_SetsErrorAndDoesNotAdd` + manuelle Sichtprüfung | Bestanden (manuell verifiziert: Fehlertext „Ein Feed mit dieser URL existiert bereits.", keine Dublette in SQLite — 1 Zeile; `manual-07`) |
| Trefferliste-Layout: Karten, ≥ 44-pt-Targets, Dark Mode, keine ScrollView-Verschachtelung, EmptyView, Attribution (Optional, rein visuell) | Manuelle UI-Verifikation (Screenshots Light + Dark, 390 × 844 pt) | Bestanden (Karten ≥ 44 pt, `AppThemeBinding` per Pixelprobe hell 236,238,240 / dunkel 28,32,40, Attribution + „Zurück zu meinen Feeds"-Button sichtbar und funktional, EmptyView „Keine Feeds gefunden."; `manual-02`/`manual-08` Dark, `manual-10`/`manual-11` Light) |

## Zusammenfassung

Ausgeführte Läufe:

- `dotnet build src/Reporter.Tests/Reporter.Tests.csproj -c Release` → 0 Warnungen, 0 Fehler
- `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release --no-build --logger "console;verbosity=normal" --collect:"XPlat Code Coverage" --settings src/Reporter.Tests/coverlet.runsettings` → erfolgreich
- `npm test` (`node --test "scripts/*.test.mjs"`) → erfolgreich

- Gesamt: 310 (274 .NET + 36 Node)
- Bestanden: 310
- Fehlgeschlagen: 0
- Übersprungen: 0

Geplante neue Tests des Features — alle vorhanden und bestanden (Iteration 2:
sechs Review-Regressionstests hinzugefügt/aufgeteilt):

- `FeedSearchServiceTests` (15): `SearchAsync_MapsDirectoryEntries_ToFeedSearchResults`, `SearchAsync_SortsByMatchKindThenScoreThenFeedUrl`, `SearchAsync_WhenResultFeedUrlEqualsQuery_RanksExactUrlFirst`, `SearchAsync_DirectoryEmpty_FallsBackToAutodiscovery_LinkTags`, `SearchAsync_DirectoryEmpty_ProbesStandardPaths`, `SearchAsync_WhenQueryIsFeedDocument_ReturnsExactUrlResult`, `SearchAsync_DirectoryError_ButSiteReachable_ReturnsDiscoveredResults`, `SearchAsync_BothSourcesFail_ThrowsFeedSearchUnavailableException`, `SearchAsync_Timeout_ThrowsFeedSearchUnavailableException`, `SearchAsync_WhenCallerCancels_ThrowsOperationCanceledException`, `SearchAsync_MalformedDirectoryJson_FallsBackToAutodiscovery`, `SearchAsync_DirectoryDuplicates_DedupesByFeedUrl` + `SearchAsync_DiscoveryDuplicates_DedupesByFeedUrl` (aufgeteilt), `SearchAsync_DirectoryResultsPresent_SkipsAutodiscovery`, `SearchAsync_EverythingEmpty_ReturnsEmptyList`
- `FeedsViewModelTests` (17): `SearchCommand_PopulatesSearchResults`, `SearchCommand_DomainInput_NormalizesToHttpsUrl`, `SearchCommand_FreeText_DoesNotCallService_ShowsEmptyResults`, `SearchCommand_WhenEmpty_DoesNotCallService`, `SearchCommand_WhenOffline_DoesNotCallService_AndSetsHint`, `SearchCommand_WhenServiceUnavailable_SetsSearchErrorMessage`, `SearchCommand_WhenServiceUnavailableAndDomainInput_SetsRetryHint`, `SearchCommand_WhenConfirmDirectAddThrows_InvokesDialogOnlyOnce`, `SearchCommand_NoResultsAndValidUrl_Confirmed_PrefillsTitleFromHost` + `SearchCommand_NoResultsAndValidUrl_Declined_KeepsFormState` (aufgeteilt), `SearchCommand_NoResultsAndDomainInput_DoesNotInvokeConfirm`, `NewUrl_Changed_ClearsSearchState`, `CloseSearchResultsCommand_ExitsResultsView`, `SubscribeResultCommand_PersistsFeedFromResult`, `SubscribeResultCommand_WhenTitleEmpty_StoresFeedUrlAsPlaceholder`, `SubscribeResultCommand_WhenDuplicate_SetsErrorAndDoesNotAdd`, `ConnectivityChanged_UpdatesSearchCommandCanExecute`
- `FeedSyncServiceTests` (2): `SyncFeedAsync_WhenTitleIsPlaceholder_UpdatesTitleFromFeedDocument`, `SyncFeedAsync_WhenTitleIsSet_DoesNotOverwriteTitle`
- `ServiceCollectionTests` (1): `AddReporterServices_ResolvesFeedSearchService`

## Testabdeckung

**Abdeckung:** 90.5 % Zeilenabdeckung, 84.5 % Branch-Abdeckung (coverlet, XPlat Code Coverage über `src/Reporter.Tests/coverlet.runsettings`; `Reporter.Data.Migrations.*` per Runsettings ausgeschlossen)

| Datei | Abdeckung |
|-------|-----------|
| `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs` | 23.2 % (generierte Designer-Datei) |
| `src/Reporter.Core/Services/FeedSearchUnavailableException.cs` | 33.3 % (2/6 Zeilen — ungenutzte Standard-Konstruktoren der Exception) |

Alle übrigen Quelldateien liegen ≥ 80 %; die übrigen neuen Feature-Dateien (`FeedSearchService.cs`, `FeedSearchResult.cs`, `FeedSearchMatchKind.cs`, `IFeedSearchService.cs`) sind durch `FeedSearchServiceTests` abgedeckt.

## Fehlende Tests

Quelle: `Coverage-Daten`

Keine Quelldatei mit 0 % Abdeckung gefunden. `AppResources.Designer.cs` (23.2 %) ist generierter Code und wird per Skill-Konvention ignoriert; `FeedSearchUnavailableException.cs` (33.3 %) betrifft ausschließlich ungenutzte Exception-Standardkonstruktoren — kein fachlicher Codepfad ohne Test.
