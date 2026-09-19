<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Keine Befunde

## Befunde

Keine.

## Verifizierte Fixes aus `continue.md`

Alle drei Code-Befunde des dritten Reviews (jetzt `review-code.3.md`) wurden adressiert:

1. `FeedDetailViewModel.SaveEditAsync` (Z. 653–670) — `GetByUrlAsync`/`UpdateAsync` laufen jetzt in try/catch; bei Fehler `Debug.WriteLine` + `ErrorMessage = AppResources.ErrorActionFailed`, das Sheet bleibt offen (Parität zu `LoadAsync`/`LoadPageCoreAsync`). Der Duplikat-Validierungspfad ist innerhalb des try-Blocks unverändert. Regressionstest `SaveEditCommand_WhenRepositoryThrows_SetsErrorAndKeepsSheetOpen` war vor dem Fix rot (Exception entkam aus Z. 646) und ist jetzt grün.
2. `FeedDetailViewModel.ReloadFeedAsync` (Z. 596–609) — try/catch mit `ErrorMessage = AppResources.ErrorLoadFailed`. Die Methode gibt `bool` zurück; `RefreshAsync` (Z. 611–618) startet die Liste nur bei erfolgreichem Reload neu — sonst würde `ResetAndLoadFirstPageAsync` die gesetzte Fehlermeldung sofort wieder leeren. Die übrigen Aufrufer (`RenameFeedAsync`, `ChangeFeedCategoryAsync`, `SaveEditAsync`) ignorieren den Rückgabewert, die Meldung bleibt dort ebenfalls sichtbar. Regressionstest `RefreshCommand_WhenReloadThrows_SetsLocalizedErrorMessage` war vor dem Fix rot (Exception entkam aus Z. 598) und ist jetzt grün.
3. `FeedDbAssertions` — `MarkAllItemsReadAsync` (Z. 204–205) und `DeleteAllFeedsAsync` (Z. 217–223) delegieren auf die neue private Hilfsmethode `ExecuteWriteWithRetryAsync` (Z. 228–262); das ~25-zeilige Retry-Gerüst existiert nur noch einmal, der `CommandText` ist Parameter. Der FK-Kaskaden-Hinweis des Delete-Pfads steht als Kommentar in `DeleteAllFeedsAsync`.
4. `FakeFeedSyncService.SyncFeedResult` (Z. 53) — bereits auf `new(FeedHealth.Ok, 0)` vereinheitlicht, deckungsgleich mit `SyncAllResult` (Z. 18).

## Hinweise

- Neue Test-Infrastruktur folgt den Bestandsmustern: `DelegatingFeedRepository` (`src/Reporter.Tests/DelegatingFeedRepository.cs`) ist die exakte Parallele zu `DelegatingItemRepository`; die abgeleiteten Fakes `FailingReloadFeedRepository`/`FailingSaveFeedRepository` sind private Nested Classes in `FeedDetailViewModelTests` wie `FailingItemRepository`/`CountingItemRepository`.
- `catch (Exception)` in den neuen Blöcken entspricht dem im Branch etablierten Muster (`LoadAsync`, `LoadPageCoreAsync`) inkl. `Debug.WriteLine`-Kontext — kein stillschweigendes Schlucken.
- Kein toter Code: `ExecuteWriteWithRetryAsync` wird von beiden Cleanup-Helfern genutzt; `DelegatingFeedRepository` hat zwei konkrete Ableitungen im Einsatz.
- Kompilations-Check: `Reporter.Tests`, `Reporter.E2ETests` und die gesamte Solution im Release-Build (statische Analyse via `scripts/Run-StaticChecks.ps1`, Warnungen als Fehler) bauen fehlerfrei — 0 Warnungen, 0 Fehler. Format-, Lizenzheader- und Paket-Checks ebenfalls ohne Befund.
- Die E2E-Helfer `MarkAllItemsReadAsync`/`DeleteAllFeedsAsync` sind reine Refactorings mit unverändertem Laufzeitverhalten; ihre Ausführung bleibt der E2E-Suite vorbehalten (benötigt interaktive Windows-Session).

## Geprüfte Dateien

Liste aller in dieser Iteration geänderten und neuen Dateien:
- `src/Reporter.Core/ViewModels/FeedDetailViewModel.cs`
- `src/Reporter.E2ETests/FeedDbAssertions.cs`
- `src/Reporter.Tests/DelegatingFeedRepository.cs` (neu)
- `src/Reporter.Tests/FeedDetailViewModelTests.cs`
- `src/Reporter.Tests/FakeFeedSyncService.cs` (Verifikation des bereits umgesetzten Punkts)
