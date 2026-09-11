# Tasks: „Für später bewahren“-Funktion und separate Ansicht (Issue #25)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `IItemRepository.DeleteExpiredAsync(DateTime cutoff)` deklarieren (Contract: nur `IsRead`, nie `IsSavedForLater`, Stichtag `ReadAt ?? PublishedAt`; XML-Doku von `DeleteAsync` präzisieren) | Offen | — |
| 2 | Logik | `ItemRepository.DeleteExpiredAsync` per `ExecuteDeleteAsync` implementieren (Bedingung `IsRead && !IsSavedForLater && (ReadAt ?? PublishedAt) < cutoff`, Rückgabe = Anzahl gelöschter Items) | Offen | — |
| 3 | Logik | Interface `IRetentionCleanupService` in `src/Reporter.Core/Interfaces/` anlegen (`CleanupAsync(CancellationToken)` → `Task<int>`) | Offen | — |
| 4 | Logik | `RetentionCleanupService` in `src/Reporter.Core/Services/` implementieren (`ISettingsRepository.GetAsync` → `RetentionDays > 0` prüfen → `cutoff = UtcNow.AddDays(-RetentionDays)` → `DeleteExpiredAsync`) | Offen | — |
| 5 | Konfiguration | `MauiProgram`: `.AddSingleton<IRetentionCleanupService, RetentionCleanupService>()` registrieren | Offen | — |
| 6 | Logik | `App.OnStart`: `IRetentionCleanupService` im vorhandenen Scope auflösen und `CleanupAsync` fehlerisoliert (try/catch) nach `MigrateAsync` aufrufen | Offen | — |
| 7 | UI | Design-Draft-Vergleich `LaterPage` vs. `design-draft/stitch_local_rss_feed_reader/f_r_sp_ter_bewahren/screen.png` und `..._dark_mode/screen.png`; AGENTS.md-Regeln prüfen (keine horizontalen Tabellen, Touch-Targets ≥ 44 × 44 pt, `AppThemeBinding`, `CollectionView` füllt `Grid`-Row `*`, kein verschachteltes Scrollen); Abweichungen beheben | Offen | — |
| 8 | UI | Manuelle UI-Verifikation 390 × 844 pt (Bewahren auf `UnreadPage`, Entfernen auf `LaterPage` inkl. `EmptyView`, Bookmark-Toggle auf `ArticleDetailPage`, Light + Dark) mit Screenshot dokumentieren in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md` | Offen | — |
| 9 | Tests | `LaterViewModelTests` anlegen: Fixture nach `UnreadViewModelTests`-Muster (`TestDbContextFactory`, echtes `ItemRepository`) + `SeedFeedAsync`-Hilfsmethode | Offen | — |
| 10 | Tests | `LaterViewModelTests.LoadCommand_PopulatesOnlySavedItems` schreiben | Offen | — |
| 11 | Tests | `LaterViewModelTests.LoadCommand_OrdersByPublishedAtDescending` schreiben | Offen | — |
| 12 | Tests | `LaterViewModelTests.ToggleSavedCommand_RemovesItemFromSavedItems` schreiben | Offen | — |
| 13 | Tests | `LaterViewModelTests.ToggleSavedCommand_NullItem_DoesNothing` schreiben | Offen | — |
| 14 | Tests | `LaterViewModelTests.MarkReadCommand_SetsReadAndKeepsItemInList` schreiben | Offen | — |
| 15 | Tests | `ItemRepositoryTests.DeleteExpiredAsync_RemovesExpiredReadItems` schreiben | Offen | — |
| 16 | Tests | `ItemRepositoryTests.DeleteExpiredAsync_KeepsSavedForLaterItems` schreiben (Kerninvariante) | Offen | — |
| 17 | Tests | `ItemRepositoryTests.DeleteExpiredAsync_KeepsUnreadItems` schreiben | Offen | — |
| 18 | Tests | `ItemRepositoryTests.DeleteExpiredAsync_KeepsNonExpiredItems` schreiben | Offen | — |
| 19 | Tests | `ItemRepositoryTests.DeleteExpiredAsync_UsesReadAtOverPublishedAt` schreiben | Offen | — |
| 20 | Tests | `ItemRepositoryTests.GetSavedForLaterAsync_OrdersByPublishedAtDescending` schreiben (Sortierungs-Assert) | Offen | — |
| 21 | Tests | `ItemRepositoryTests.ToggleSavedForLaterAsync_TogglesBackToFalse` schreiben | Offen | — |
| 22 | Tests | `RetentionCleanupServiceTests` anlegen: `CleanupAsync_DeletesExpiredButKeepsSaved`, `CleanupAsync_ZeroOrNegativeRetentionDays_Skips`, `CleanupAsync_RespectsConfiguredRetentionDays` | Offen | — |
| 23 | Tests | `UnreadViewModelTests.ToggleSavedCommand_TogglesFlagInPlace` ergänzen | Offen | — |
| 24 | Tests | `FeedRepositoryTests.DeleteAsync_CascadeDeletesSavedItems` ergänzen (dokumentiert Cascade-Verhalten) | Offen | — |
| 25 | Abschluss | `dotnet test` (CI-Befehl mit `coverlet.runsettings` + TRX-Logger) und `.\scripts\Run-StaticChecks.ps1` ausführen; Ergebnis in `test-results.md` festhalten | Offen | — |
