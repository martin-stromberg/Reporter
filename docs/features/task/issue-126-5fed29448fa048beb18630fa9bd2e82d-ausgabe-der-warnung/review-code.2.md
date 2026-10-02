<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedSyncServiceTests.cs (FeedSyncServiceTests)

- **Doppelter Code** — Der ~10-zeilige Arrange-Block für den Stale-Feed-Fall (`SeedFeedAsync` + RSS mit `pubDate = DateTime.UtcNow.AddDays(-40)` + zwei `SyncFeedAsync`-Aufrufe) steht wörtlich in `SyncFeedAsync_NoNewItemsForThirtyDays_SetsWarning` (Zeilen 332–340) und `SyncFeedAsync_NoNewItemsForThirtyDays_PersistsWarningMessage` (Zeilen 371–379). Dasselbe Duplikationsmuster wurde in Iteration 1 für den Shrink-Block beanstandet und dort mit `SeedThenShrinkFeedAsync` gelöst — für den Stale-Block wurde kein Pendant extrahiert.

  Empfehlung: Einen privaten Helper analog `SeedThenShrinkFeedAsync` extrahieren, z. B. `SeedStaleFeedThenResyncAsync()` (Feed anlegen, Stale-XML syncen, erneut syncen), und beide Tests darauf aufsetzen.

- **Testqualität / fehlende Testabdeckung** — Der in Iteration 2 neu eingeführte Per-Feed-Lock in `FeedSyncService.SyncFeedAsync` (`_feedSyncLocks`, Zeilen 82–131) implementiert die Serialisierungsgarantie, die den Race-Befund aus Iteration 1 behebt (paralleler manueller Refresh vs. `SyncAllAsync` auf demselben Feed → doppelte Item-Inserts/`DbUpdateException` → falsch persistierter Error-Zustand). Für dieses neue Nebenläufigkeitsverhalten existiert kein Test; ein Refactoring könnte den Lock unbemerkt entfernen oder schwächen.

  Empfehlung: Einen Test ergänzen, der zwei überlappende `SyncFeedAsync`-Aufrufe für denselben Feed deterministisch überlappt — z. B. `FakeHttpMessageHandler` mit `TaskCompletionSource`-Gate, das den ersten Request blockiert, bis der zweite Aufruf gestartet ist — und assertiert, dass beide Aufrufe fehlerfrei enden, keine `DbUpdateException` als Feed-`Error` persistiert wird und keine doppelten Items entstehen.

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
- `docs/features/task/issue-126-…/` (die `inventory/*.md`-Dateien nennen bewusst die alten `LastError*`/`DetermineStatus`-Namen — korrekt, da sie den Ist-Zustand vor der Umsetzung beschreiben)

## Befunde aus Iteration 1 — Verifizierung der Behebung

- **`DetermineStatus` → umbenannt in `DetermineHealth`** (FeedSyncService.cs Zeile 430): Name beschreibt jetzt das `HealthDecision`-Ergebnis; die `<summary>`-Verweise im Record stimmen. Behoben.
- **Race manueller Refresh vs. `SyncAllAsync` → Per-Feed-Lock** (`_feedSyncLocks`, `GetOrAdd` + `WaitAsync(cancellationToken)` vor `try`, `Release()` im `finally`): Fix fachlich korrekt — der zweite, serialisierte Lauf liest Feed- und Item-Snapshot frisch, wodurch sowohl das Last-Writer-Wins-Überschreiben der `LastMessage*`-/Health-Felder als auch die `DbUpdateException`-Fehlpersistenz entfallen. Nebenläufigkeitsprüfung des Fixes selbst: deadlocksicher (strikte Lock-Hierarchie `_syncAllLock` → `feedLock`; `SyncFeedAsync` erwirbt `_syncAllLock` nie, `feedLock` wird nie gehalten, während ein weiterer Lock angefordert wird); kein Release auf nicht-erworbenem Semaphore bei Cancel-While-Waiting (`WaitAsync` steht vor dem `try`); `SyncAllAsync` hält den Feed-Lock nur für die Dauer des jeweiligen Feeds; `FeedSyncService` ist als Singleton registriert (`MauiProgram.cs` Zeile 95), sodass die Lock-Map prozessweit greift — die in Tests verwendeten Einzelinstanzen sind davon nicht betroffen, da jede Test-Instanz ohnehin sequenziell fährt. Restrisiko, als akzeptiert bewertet: `_feedSyncLocks`-Einträge werden nie entfernt und die `SemaphoreSlim`-Instanzen nie disposed — das Dictionary wächst pro jemals synchronisierter Feed-ID (bounded durch die nutzerverwaltete, kleine Feed-Anzahl; `SemaphoreSlim` benötigt ohne `AvailableWaitHandle`-Zugriff kein Dispose). Ein entfernendes Cleanup wäre fehleranfälliger als der Nutzen.
- **Vierfach duplizierter Shrink-Arrange-Block → `SeedThenShrinkFeedAsync` extrahiert** (Zeile 106): alle vier betroffenen Tests nutzen den Helper. Behoben (Restduplikat beim Stale-Block siehe oben).
- **`GetFeedMessage_MapsWarningKindToLocalizedText` mit zwei Fällen → aufgeteilt**: jetzt separate Facts `GetFeedMessage_FewerItemsWarning_MapsToLocalizedText` und `GetFeedMessage_NoRecentItemsWarning_MapsToLocalizedText`, ergänzt um `GetFeedMessage_WarningFallsBackToUnknown`. Behoben.
- **E2E-Testnamen/-kommentare**: `FeedDetail_Message_ForErrorFeed` und `FeedDetail_Message_ForWarningFeed` korrigieren das „Only"-Missverständnis; beide Kommentare benennen die Abdeckung (Error- bzw. Warning-Pfad plus Healthy-Negativ). Behoben.

## Verifizierte Prüfpunkte (ohne Befund)

- **Migration datenerhaltend** (erneut verifiziert): ausschließlich `RenameColumn` in `Up`/`Down`; Snapshot und Designer konsistent (`last_message_kind` TEXT max. 50, `last_message` TEXT).
- **Vollständigkeit der Umbenennung:** Grep über `src/` findet keine verbliebenen `LastError*`/`last_error_*`/`GetFeedErrorMessage`/`ButtonShowErrorDetails`/`FeedErrorDetailsTitle`-/`DetermineStatus`-Referenzen außerhalb historischer Migrationen und des Feature-Inventars (beides korrekt unverändert).
- **E2E-Warning-Test fachlich korrekt:** `stale-feed.xml` trägt pubDates von Jan 2024, sodass der erste Sync (2 neue Items) `Ok` liefert und der Refresh (`newItems == 0`, `lastPublishedAt` > 30 Tage alt) `NoRecentItems` auslöst; die Fixture wird per `Fixtures\**\*`-Content-Eintrag kopiert und vom `StubFeedServer` vor dem generischen Template aufgelöst. `healthy-target`-Doppelanlage ist unproblematisch, weil `ResetUiState` alle Feeds zwischen den Tests löscht.
- **Ressourcen EN/DE:** beide `.resx` weiterhin schlüsselsymmetrisch (je 209 Einträge); `AppResources.Designer.cs` synchron regeneriert.
- **Lizenzheader** in allen neuen Dateien vorhanden.
