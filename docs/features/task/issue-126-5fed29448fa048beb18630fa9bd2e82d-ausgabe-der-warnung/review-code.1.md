<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedSyncService.cs (FeedSyncService)

- **Namenskonventionen und Einheitlichkeit** — `DetermineStatus` (Zeile 412) liefert seit diesem Branch keinen Status-String mehr, sondern ein `HealthDecision`-Record mit Status, Warnungs-Kind und technischer Meldung. Der Methodenname beschreibt nur noch einen Teil des Ergebnisses.

  Empfehlung: Methode in `DetermineHealth` oder `EvaluateHealthDecision` umbenennen (und den Verweis im `<summary>` von `HealthDecision` entsprechend anpassen).

- **Fehlerbehandlung / Nebenläufigkeit (Bestandsproblem, nicht durch diesen Branch eingeführt)** — `SyncFeedAsync` wird nicht von `_syncAllLock` abgedeckt (nur `SyncAllAsync` vs. `SyncAllAsync` ist serialisiert). Ein manueller Refresh (`FeedDetailViewModel.RefreshAsync` → `SyncFeedAsync`) kann daher parallel zu einem laufenden `SyncAllAsync` denselben Feed synchronisieren. Beide Läufe lesen denselben Feed-Snapshot; `UpdateFeedHealthAsync` (Zeile 434) schreibt die gesamte Entität per Last-Writer-Wins zurück — ein älterer Lauf kann dabei frisch geschriebene `LastMessageKind`/`LastMessage`/`HealthStatus`-/`HealthLastChange`-Werte wieder mit veralteten überschreiben. Zusätzlich kann der zweite Lauf identische neue Items einspielen wollen; der Unique-Index `items(feed_id, guid_or_hash)` löst dann eine `DbUpdateException` aus, die im `catch`-Zweig als `FeedSyncErrorKind.Unknown` mit `HealthStatus = Error` persistiert würde — und damit einen korrekten Ok-/Warning-Zustand samt Warnungsmeldung überschreibt.

  Empfehlung: Mindestens dokumentieren; besser `SyncFeedAsync` (oder den Feed-spezifischen Kern in `RunSyncAsync`) unter denselben Lock stellen bzw. einen per-Feed-Lock einführen.

### FeedSyncServiceTests.cs (FeedSyncServiceTests)

- **Doppelter Code** — Die drei neuen Tests `SyncFeedAsync_FewerItems_PersistsWarningMessage` (Zeile 342), `SyncFeedAsync_SuccessAfterWarning_ClearsLastMessage` (Zeile 403) und `SyncFeedAsync_ErrorAfterWarning_OverwritesLastMessage` (Zeile 445) wiederholen wörtlich denselben ~14-zeiligen Arrange-Block (`SeedFeedAsync` + `fourItems`-XML + `oneItem`-XML + zwei Syncs). Identisch steht er bereits im Bestandstest `SyncFeedAsync_FewerItems_SetsWarning` (Zeile ~285) — durch diesen Branch existiert der Block nun 4×.

  Empfehlung: Einen privaten Helper extrahieren, z. B. `SeedThenShrinkFeedAsync()` der den Feed anlegt, mit 4 Items synct und mit 1 Item re-synct; die Tests rufen ihn auf und prüfen nur ihre spezifischen Assertions.

### FeedDetailViewModelTests.cs (FeedDetailViewModelTests)

- **Testqualität** — `GetFeedMessage_MapsWarningKindToLocalizedText` (Zeile 697) prüft zwei fachlich getrennte Fälle in einer Testmethode (Mapping `FewerItems` und Mapping `NoRecentItems`); bei einem Fehlschlag ist nicht ohne Weiteres erkennbar, welches Mapping gebrochen ist.

  Empfehlung: In zwei `[Fact]`-Methoden aufteilen oder als `[Theory]` mit `[InlineData(FeedSyncWarningKind.FewerItems, …)]` modellieren.

### FeedDetailTests.cs (FeedDetailTests)

- **Namenskonventionen und Einheitlichkeit / Testdokumentation** — Der XML-Kommentar von `FeedDetail_Message_OnlyForErrorFeed` (Zeile ~402) behauptet weiterhin, „Show message" erscheine *nur* bei Feeds mit Error-Status; seit diesem Branch erscheint der Eintrag auch bei Warning. Auch der Testname `OnlyForErrorFeed` suggeriert eine Exklusivität, die nicht mehr gilt (das Healthy-Feed-Negativ am Ende des Tests bleibt korrekt).

  Empfehlung: Kommentar korrigieren (z. B. „…for feeds with error or warning health; this test covers the error path") und den Test ggf. in `FeedDetail_Message_ForErrorFeed` umbenennen; analog bei `FeedDetail_Message_OnlyForWarningFeed` das „Only" im Namen prüfen.

## Geprüfte Dateien

Liste aller geprüften Dateien:

Quellcode:
- `src/Reporter.Core/Models/Feed.cs`
- `src/Reporter.Core/Models/FeedListItem.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Services/DemoContentService.cs`
- `src/Reporter.Core/Services/FeedHealthUpdate.cs`
- `src/Reporter.Core/Services/FeedSyncService.cs`
- `src/Reporter.Core/Services/FeedSyncWarningKind.cs` (neu)
- `src/Reporter.Core/ViewModels/FeedDetailViewModel.cs`
- `src/Reporter.Data/Entities/Feed.cs`
- `src/Reporter.Data/Migrations/20261002192257_RenameFeedLastErrorToLastMessage.cs` (neu)
- `src/Reporter.Data/Migrations/20261002192257_RenameFeedLastErrorToLastMessage.Designer.cs` (neu)
- `src/Reporter.Data/Migrations/ReporterDbContextModelSnapshot.cs`
- `src/Reporter.Data/ReporterDbContext.cs`
- `src/Reporter.Data/Repositories/FeedRepository.cs`
- `src/Reporter/Views/FeedDetailPage.xaml.cs`

Tests:
- `src/Reporter.E2ETests/FeedDetailTests.cs`
- `src/Reporter.E2ETests/Fixtures/stale-feed.xml` (neu)
- `src/Reporter.Tests/DemoContentServiceTests.cs`
- `src/Reporter.Tests/FeedDetailViewModelTests.cs`
- `src/Reporter.Tests/FeedRepositoryTests.cs`
- `src/Reporter.Tests/FeedSyncServiceTests.cs`
- `src/Reporter.Tests/ReporterDbContextTests_Persistence.cs`
- `src/Reporter.Tests/ReporterDbContextTests_Schema.cs`

Dokumentation (stichprobenartig auf Konsistenz geprüft):
- `docs/help/anwendung/feeddetailansicht.md`, `synchronisation.md`, `mobile-ui-design.md`, `datenmodell.md`
- `docs/features/task/issue-126-…/plan.md`, `plan-check.md`, `requirement.md`, `…-tasks.md`, `review.md`

## Verifizierte Prüfpunkte (ohne Befund)

- **Migration datenerhaltend:** `20261002192257_RenameFeedLastErrorToLastMessage.cs` enthält ausschließlich `RenameColumn` in `Up`/`Down` — kein `DropColumn`/`AddColumn`, keine Datenverluste. Snapshot und Designer-Datei sind konsistent (`last_message_kind` TEXT max. 50, `last_message` TEXT).
- **Vollständigkeit der Umbenennung:** Grep über `src/` zeigt keine verbliebenen Referenzen auf `LastError*`/`last_error_*`/`GetFeedErrorMessage`/`ButtonShowErrorDetails`/`FeedErrorDetailsTitle`/`ShowFeedErrorDetails` außer in den historischen Migrationen `AddFeedLastError`/`DropItemContentHtml`/`AddKeywordFeedId` (korrekt unverändert) und `SetLastError`-P/Invoke-Attributen (unrelated). Alle Feed-Kopierstellen (`FeedRepository.UpdateAsync`/`MapToModel`/`MapToEntity`/`GetAllWithDetailsAsync`, `FeedDetailViewModel.ToFeed`, `FeedSyncService.UpdateFeedHealthAsync`, `DemoContentService`) sind umgestellt; `FeedsViewModel.Search.cs` setzt die Felder nicht explizit — korrekt, da `null`-Default für neue Feeds.
- **Ressourcen EN/DE:** Beide `.resx`-Dateien haben identische 209 Schlüssel (keine Asymmetrie); die drei neuen `FeedWarningKind*`-Schlüssel und die Umbenennungen `ButtonShowMessage`/`FeedMessageDetailsTitle` sind in beiden Sprachen vorhanden und konsistent übersetzt; `AppResources.Designer.cs` ist synchron regeneriert.
- **Lizenzheader:** Alle neuen Dateien (`FeedSyncWarningKind.cs`, beide Migrationsdateien, `stale-feed.xml`) tragen den PolyForm-Header.
- **AGENTS.md / Mobile UI:** Kein neuer Screen; Aktionsblatt-Eintrag + `DisplayAlertAsync` entsprechen dem Mobile-Muster; Verifikation in `docs/help/anwendung/mobile-ui-design.md` (Abschnitt „Sync-Meldung bei Fehler und Warnung (issue-126)") dokumentiert.
- **Semantik der Meldungsfelder:** Error-Pfad schreibt `FeedSyncErrorKind` + Techniktext, Warning-Pfad `FeedSyncWarningKind` + Kennzahl, Ok-Pfad `null`/`null` (Clearing per Tests `SyncFeedAsync_SuccessAfterWarning_ClearsLastMessage` und `SyncFeedAsync_Success_ClearsLastMessage` abgedeckt); `GetFeedMessage`-Fallback wählt severity-abhängig `FeedWarningKindUnknown`/`FeedErrorKindUnknown` — korrekt auch für migrierte Warning-Feeds ohne gespeicherten Kind-Wert.
