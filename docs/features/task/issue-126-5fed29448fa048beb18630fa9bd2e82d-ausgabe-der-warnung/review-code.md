<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Keine Befunde

## Befunde aus Iteration 2 — Verifizierung der Behebung

- **Duplizierter Stale-Feed-Arrange-Block → `SeedStaleFeedThenResyncAsync` extrahiert** (FeedSyncServiceTests.cs Zeilen 131–143): Der Helper kapselt Seed + Stale-XML (`pubDate = DateTime.UtcNow.AddDays(-40)`, 40 Tage > 30-Tage-Schwelle, deterministisch) + zwei Syncs und gibt `(FeedId, StaleResult)` als Tupel zurück — exakt das Muster von `SeedThenShrinkFeedAsync`. Beide betroffenen Tests (`SyncFeedAsync_NoNewItemsForThirtyDays_SetsWarning` Zeile 349, `SyncFeedAsync_NoNewItemsForThirtyDays_PersistsWarningMessage` Zeile 380) nutzen ihn; der wörtliche Block existiert kein weiteres Mal. Behoben.

- **Fehlende Testabdeckung für den Per-Feed-Lock → neue Testklasse `FeedSyncServiceTests_Concurrency`**: `SyncFeedAsync_ConcurrentCallsOnSameFeed_AreSerialized` überlappt zwei `SyncFeedAsync`-Aufrufe desselben Feeds deterministisch über ein `TaskCompletionSource`-Gate im `FakeHttpMessageHandler` und deckt damit genau das Serialisierungsversprechen des `_feedSyncLocks`-Fixes ab. Behoben.

## Prüfung des neuen Concurrency-Tests (Korrektheit / Flakiness)

- **Deterministisch für die korrekte Implementierung:** `firstRequestStarted` (mit `WaitAsync(TimeSpan.FromSeconds(10))` statt barem `await` → aussagekräftiger `TimeoutException`-Fehler statt Hängen) garantiert, dass der erste Sync den HTTP-Request erreicht hat, bevor der zweite gestartet wird. Beide TCS verwenden `RunContinuationsAsynchronously` — keine Inline-Continuation im Handler-Thread.
- **`try/finally` um die Beobachtungs-Assertions** stellt sicher, dass `releaseFirstRequest` auch bei einem Assert-Fehler gesetzt wird — der blockierte erste Sync läuft dann zu Ende, der Test hängt nie in `Task.WhenAll`.
- **Korrekte Speichersynchronisation:** `Interlocked.Increment` für den Request-Zähler, `Volatile.Read` für die Assertions.
- **`Task.Delay(250)` ist kein Flaky-Risiko:** Die Assertions danach (`requestCount == 1`, `!secondSync.IsCompleted`) gelten für die gelockte Implementierung zu jedem Zeitpunkt — sie hängen nicht davon ab, dass der zweite Aufruf das Warten auf den Lock schon erreicht hat. Bei einer Regression (Lock entfernt) reichen 250 ms für den in-memory-SQLite-/Fake-HTTP-Pfad aus, um `requestCount == 2` auszulösen; selbst im Extremfall (überlastete CI) wäre das Ergebnis ein falsches Pass, kein Flaky-Fail — zusätzlich fangen die Endzustands-Assertions (`Assert.All(results, Ok)`, exakt 2 Items ohne Duplikate, kein persistierter Error) einen fehlenden Lock über die `DbUpdateException`-Fehlpersistenz ab.
- **Mehrschichtige Assertions:** Serialisierung am Lock (1 Request während Blockade, zweiter Sync unvollständig), tatsächliches Ausführen des zweiten Syncs (`requestCount == 2`), keine doppelten Items (`Assert.Single` je Guid), sauberer Endzustand (`HealthStatus = Ok`, `LastMessageKind`/`LastMessage = null`).
- **Konventionen:** Datei-/Klassenname `FeedSyncServiceTests_Concurrency` folgt dem bestehenden Muster `FeedSyncServiceTests_DebugLog` (Testklasse pro Thema, eigener Konstruktor mit Repository-Setup, eigene In-Memory-DB via `Guid`-Suffix im Connection String → keine Parallelisierungskonflikte zwischen Testklassen). Lizenzheader und XML-Dokumentation (CS1591 ist warnings-as-errors) vorhanden; keine ungenutzten Felder oder Variablen.
- **Empirisch verifiziert:** `dotnet build` der Testprojekte ohne Warnungen/Fehler; der neue Test bestand 4× in Folge, die gesamte `FeedSyncServiceTests*`-Suite (61 Tests inkl. `_DebugLog` und `_Concurrency`) bestand fehlerfrei.

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
- `src/Reporter.Tests/FeedSyncServiceTests_Concurrency.cs` (neu)
- `src/Reporter.Tests/ReporterDbContextTests_Persistence.cs`
- `src/Reporter.Tests/ReporterDbContextTests_Schema.cs`

Dokumentation (stichprobenartig auf Konsistenz geprüft):
- `docs/help/anwendung/feeddetailansicht.md`, `synchronisation.md`, `mobile-ui-design.md`, `datenmodell.md`
- `docs/features/task/issue-126-…/` Feature-Artefakte

## Verifizierte Prüfpunkte (ohne Befund)

- **Per-Feed-Lock (erneut verifiziert, unverändert seit Iteration 2):** `GetOrAdd` + `WaitAsync(cancellationToken)` vor dem `try` (kein Release auf nicht-erworbenem Semaphore), `Release()` im `finally`; strikte Lock-Hierarchie `_syncAllLock` → `feedLock` ohne zyklische Wartebeziehung; `FeedSyncService` ist Singleton, Lock-Map greift prozessweit.
- **Migration datenerhaltend:** ausschließlich `RenameColumn` in `Up`/`Down`; Designer-Datei und `ReporterDbContextModelSnapshot` konsistent (`last_message_kind` TEXT max. 50, `last_message` TEXT).
- **Vollständigkeit der Umbenennung:** Grep über `src/` findet keine verbliebenen `LastError*`/`last_error_*`/`GetFeedErrorMessage`/`ButtonShowErrorDetails`/`FeedErrorDetailsTitle`/`DetermineStatus`-Referenzen außerhalb historischer Migrationen (`AddFeedLastError` — korrekt unverändert) und `SetLastError`-P/Invoke-Attributen in `E2EProcessGuard.cs` (unrelated).
- **Testqualität der überarbeiteten Tests:** `SeedThenShrinkFeedAsync`/`SeedStaleFeedThenResyncAsync` tragen erklärende Kommentare, die das fachliche Szenario benennen; die aufrufenden Tests behalten je genau einen fachlichen Fall und klare Assert-Blöcke.
- **E2E-Warning-Test unverändert korrekt:** `stale-feed.xml` mit pubDates Jan 2024 → erster Sync `Ok`, Refresh `NoRecentItems`; Testnamen `FeedDetail_Message_ForErrorFeed`/`ForWarningFeed` beschreiben die Abdeckung korrekt.
- **Ressourcen EN/DE** schlüsselsymmetrisch; `AppResources.Designer.cs` synchron.
