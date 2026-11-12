<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests — Bestandsaufnahme Lesepakete

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-11-12, Lauf gestartet ca. 06:00:17 +01:00 (CET), Testdauer 7,0 s (inkl. Build deutlich länger)
- Branch und Commit-ID: `task/issue-164-6be1afef20db4ef4bb6330ebefbb4fdb-lesepakete` @ `d40f7b3854c9c3cb34edc779f9c0aaa097a8acbb` (2026-10-17 10:20:19 +0200)
- Uncommittete Änderungen im getesteten Stand: nur untracked Dateien unter `docs/features/task/issue-164-6be1afef20db4ef4bb6330ebefbb4fdb-lesepakete/` (requirement.md, diese Bestandsaufnahme); kein Produktiv- oder Testcode verändert
- Testumgebung und Runtime-/SDK-Versionen: Windows (lokale Entwicklermaschine), .NET SDK 10.0.403 (einzige installierte 10.x-Version laut `dotnet --list-sdks`), `Reporter.Tests` auf `net10.0`, xunit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `src/Reporter.Tests/Reporter.Tests.csproj` — Unit-/Integrationstests (SQLite-In-Memory via `TestDbContextFactory`); CI-Befehl aus `.github/workflows/pr-staging-ci.yml` bzw. `staging-ci.yml` (Zeile ~125): `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build` mit Coverlet-Settings
  - `src/Reporter.E2ETests/Reporter.E2ETests.csproj` — FlaUI-UIA3-Tests (`net10.0-windows10.0.19041.0`); werden **nicht** in der CI ausgeführt; lokale Ausführung über `scripts/Run-E2ETests.ps1` (erfordert interaktive Desktop-Session und App-Build). Nicht ausgeführt, siehe unten.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline Unit-Tests | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --logger "trx;LogFileName=baseline-inventory.trx" --logger "console;verbosity=normal"` | Repo-Root `D:\Repositories\softwareschmiede\6be1afef-20db-4ef4-bb63-30ebefbb4fdb` | 0 | 753 | 0 | 0 | [TRX](test-results/unittests-baseline.trx), [Konsole](test-results/unittests-baseline-console.log) |

Hinweis: Der Lauf baute die Test-Assembly und deren Abhängigkeiten (`Reporter.Core`, `Reporter.Data`) zuvor im selben Befehl (kein `--no-build`); die MAUI-App `Reporter` wird vom Testprojekt nicht referenziert und war für den Lauf nicht erforderlich.

### Nachgewiesene bestehende Testfehler

Es wurden **keine** Testfehler nachgewiesen — alle 753 Tests der Unit-Test-Suite bestanden im Ausgangslauf (TRX: `total="753" executed="753" passed="753" failed="0"`, keine `Failed`- oder `NotExecuted`-Einträge).

### Testlücken und Ausführungsprobleme

- `Reporter.E2ETests` **nicht ausgeführt**: FlaUI-Tests benötigen eine interaktive Windows-Desktop-Session und einen vollständigen MAUI-App-Build (Windows-TFM, win-x64). Die CI führt diese Suite ebenfalls nicht aus. Für die Anforderung ist dort laut Anforderungsübersetzung ein neuer Akzeptanztest vorgesehen; ein Ausgangszustand („grün") ist nicht nachgewiesen — für die neuen E2E-Tests zählt der Nachweis ab ihrer Implementierung.
- Bestehende fachliche Testlücken (direkt aus dem Code bestätigt):
  - `ItemRepository.MarkAllAsReadAsync` hat **keinen dedizierten Repository-Test**; abgedeckt nur indirekt über `UnreadViewModelTests.MarkAllReadCommand_ClearsArticles`.
  - Der `categoryId`-Parameter von `GetUnreadByDateAsync(int, int, Guid?, bool)` hat **keinen dedizierten Repository-Test**; Kategoriefilterung nur über den ViewModel-Test `SelectCategoryCommand_FiltersArticles` gegen das echte Repository abgedeckt.
  - Keine Tests für Tagesgrenzen, `<5`-Schwelle oder paket-begrenztes Markieren — Funktionalität existiert noch nicht.
- Build-, Setup- oder Infrastrukturfehler: keine aufgetreten.

## Testklassen

### `UnreadViewModelTests` (`src/Reporter.Tests/UnreadViewModelTests.cs`)

Testet gegen das **echte** `ItemRepository` auf In-Memory-SQLite (`TestDbContextFactory`) — keine Mocks für den Repository-Pfad.

- `LoadCommand_PopulatesArticlesAndCategories` — Laden befüllt `Articles` und `Categories`.
- `SelectCategoryCommand_FiltersArticles` — Kategoriefilter reduziert die Liste.
- `SelectCategoryCommand_UpdatesChipSelectionState` — `IsSelected`-Zustand der Chips.
- `MarkReadCommand_RemovesArticle` — einzelnes Als-gelesen-Markieren entfernt den Eintrag.
- `MarkReadCommand_DecrementsUnreadCount` — Zähler- und Chip-Aktualisierung.
- `MarkAllReadCommand_ClearsArticles` — **aktuelles Verhalten**: markiert alle ungelesenen Artikel (wird zur erwarteten Änderung — nur das Tagespaket).
- `RefreshCommand_InvokesSyncService`, `RefreshCommand_WhenOffline_SkipsSyncWithoutError`, `RefreshCommand_WhenSyncThrows_SetsLocalizedSyncError`, `RefreshCommand_WhenOffline_ResetsPresetIsSyncing`, `RefreshCommand_WhenIsSyncingPresetByBinding_StillSyncs`, `RefreshCommand_WhenLoadFailsAfterSync_KeepsLoadErrorMessage`, `RefreshCommand_WhenBackOnline_InvokesSyncService` — Refresh-/Sync-Verhalten.
- `ConnectivityChanged_ClearsSyncErrorMessage`, `ConnectivityChanged_KeepsLoadErrorMessage`, `ConnectivityChanged_UpdatesIsOnline` — Konnektivität.
- `ToggleSavedCommand_TogglesFlagInPlace` — Gespeichert-Toggle.
- `LoadPage_PassesSortOrderFromSettings` — `UnreadSortOrder` „asc"/„desc" wird als `ascending`-Flag ans Repository durchgereicht (via `RecordingItemRepository`).
- `LoadPage_InvalidSortOrder_UsesDescending` — unbekannter Wert → absteigend.

### `UnreadViewModelTests_CardStack` (`src/Reporter.Tests/UnreadViewModelTests_CardStack.cs`)

- `LoadCommand_ShowsFirstArticleOnTop`, `NextCardCommand_AdvancesStack`, `NextCardCommand_OnLastCard_CannotExecute`, `PreviousCardCommand_MovesBack`, `CardPosition_OutOfRange_IsClamped`, `MarkReadCommand_OnTopCard_PromotesNextArticle`, `LoadCommand_Reload_KeepsTopArticle` — Kartenstapel-Verhalten.

### `UnreadViewModelTests_ChipCounts` (`src/Reporter.Tests/UnreadViewModelTests_ChipCounts.cs`)

- `MarkReadCommand_UnderAllFilter_DecrementsCategoryChip`, `MarkReadCommand_UnderCategoryFilter_DecrementsAllChip` — Chip-Zähler-Mitführung beim Als-gelesen-Markieren.

### `UnreadViewModelTests_Layout` (`src/Reporter.Tests/UnreadViewModelTests_Layout.cs`)

- `LoadCommand_WithDefaultSettings_UsesListLayout`, `LoadCommand_WithPersistedCardLayout_UsesCardLayout`, `LoadCommand_WithUnknownLayout_FallsBackToList`, `ToggleLayoutCommand_FromList_PersistsCardLayout`, `ToggleLayoutCommand_KeepsOtherSettings`, `LayoutToggleDescription_InCardLayout_NamesListLayout` — Layout-Persistenz.

### `UnreadViewModelTests_ListState` (`src/Reporter.Tests/UnreadViewModelTests_ListState.cs`)

- `LoadCommand_WithoutArticles_ShowsEmptyState`, `LoadCommand_WithArticles_HidesEmptyState`, `CardPositionText_ReflectsPositionAndTotal`, `ToggleSavedCommand_OnCurrentCard_UpdatesCurrentCard`, `MarkReadCommand_WithStaleCopy_RemovesListedArticle`, `SyncStatusText_WhenOffline_ReportsOffline` — Listen-/Leerzustände.

### `ItemRepositoryTests` (`src/Reporter.Tests/ItemRepositoryTests.cs`, 2271 Zeilen)

Relevante Tests für die Ungelesen-Abfrage:

- `GetUnreadByDateAsync_ReturnsUnreadSortedByPublishedAtDescending` (Zeile 168) — unpagierte Variante.
- `GetUnreadByDateAsync_Paged_ReturnsPage` (Zeile 518) — Paging (`page=0`, `pageSize=2`).
- `GetUnreadByDateAsync_Paged_ProjectsReadingTimeText` (Zeile 394), `GetUnreadByDateAsync_OneMinuteReadingTime_ProjectsEmpty` (Zeile 1043), `GetUnreadByDateAsync_ProjectsFeedFaviconUrl` (Zeile 1068), `GetUnreadByDateAsync_Paged_ProjectsLocalImageLoader` (Zeile 1734) — Projektionsdetails der Listenelemente.
- `GetUnreadByDateAsync_Ascending_OrdersByPublishedAtAscending` (Zeile 1103) — aufsteigende Sortierung inkl. `Id`-Tie-Breaker (bei gleichem `PublishedAt` absteigende `Id`-Reihenfolge).
- `GetUnreadByDateAsync_Ascending_NullPublishedAt_SortsFirst` (Zeile 1166) — `PublishedAt == null` sortiert in SQLite bei `OrderBy` zuerst (für die Tag-Zuordnung der Lesepakete relevant!).
- `GetUnreadByDateAsync_AllRead_ReturnsEmpty` (Zeile 495) — leere Liste.
- `GetUnreadCountAsync_ReturnsCorrectCount` (Zeile 546) — Zähler.
- `MarkAsReadAsync_SetsIsRead` (Zeile 578) — Einzelmarkierung.
- Kein dedizierter Test für `MarkAllAsReadAsync` (siehe Testlücken).
- Keine Tests für kategoriefilterte Ungelesen-Abfrage auf Repository-Ebene (siehe Testlücken).

### `Reporter.E2ETests` (nicht ausgeführt)

- `SmokeTests.cs`, `ArticleLinkTests.cs`, `ArticleTagsTests.cs`, `FeedDetailTests.cs`, `DemoSeedTests.cs` — FlaUI-basierte App-Tests; enthalten bisher keinen Akzeptanztest für „Alles gelesen"/Lesepakete.
- `FeedDbAssertions.cs` `MarkAllItemsReadAsync` (Zeile 298) markiert alle Artikel per direktem SQL (`UPDATE items SET is_read = 1`) zum Aufräumen zwischen Tests — unabhängig vom App-Markierverhalten, daher von der Lesepaket-Änderung unberührt.
- `StubFeedServer.cs`, `E2EPageHelpers.cs`, `ReporterAppFixture.cs`, `E2ETestCollection.cs` — Infrastruktur (App-Start über `REPORTER_APP_PATH`, HTTP-Stub-Server mit Feed-Fixtures unter `Fixtures/`).

## Hilfsmethoden und Test-Doubles

### `TestDbContextFactory` / `TestDbContextFactoryBase` (`src/Reporter.Tests/`)

- Stellt In-Memory-SQLite-`ReporterDbContext`-Instanzen bereit (inkl. Content-DB-Fabrik `TestContentDbContextFactory`); Basis fast aller Repository-/ViewModel-Tests.

### `TestDataSeeder` (`src/Reporter.Tests/TestDataSeeder.cs`)

- `SeedFeedAsync(TestDbContextFactory, string url)` — legt einen Feed direkt in der DB an, liefert `feedId`.
- `SeedFeedAsync(IFeedRepository, string url, bool notificationsEnabled)` — Variante über das Repository.

### `TestSettingsHelper` (`src/Reporter.Tests/TestSettingsHelper.cs`)

- `SaveAsync(SettingsRepository, ...)` — speichert `Settings` mit überschreibbaren Feldern (u. a. `unreadSortOrder`); genutzt in den SortOrder-Tests.

### `DelegatingItemRepository` (`src/Reporter.Tests/DelegatingItemRepository.cs`)

- Dekorator über `IItemRepository`, alle 21 Member `virtual` und delegierend — Basisklasse für `RecordingItemRepository`, `FailingItemRepository` und weitere Doubles (in `FeedDetailViewModelTests`, `LaterViewModelTests` je eigene private Ableitungen). **Neue Interface-Member müssen hier ergänzt werden**, sonst bricht die Kompilierung.

### Test-Doubles in `UnreadViewModelTests`

- `RecordingItemRepository : DelegatingItemRepository` (Zeilen 530–551) — zeichnet das `ascending`-Flag der letzten paginierten Ungelesen-Abfrage auf (`LastAscending`); Muster für Tests neuer Repository-Parameter.
- `FailingItemRepository : DelegatingItemRepository` (Zeilen 589–605) — wirft in `GetUnreadByDateAsync(page, pageSize, ...)` eine Exception; simuliert Ladefehler.
- `FakeFeedSyncService` (Zeilen 553–579, private in der Testklasse) — `SyncAllCalled`-Flag, `NextResult`/`NextException`. Hinweis: es existiert zusätzlich eine gemeinsame `FakeFeedSyncService`-Datei im Testprojekt.

### Weitere Fakes (`src/Reporter.Tests/`)

- `FakeItemContentStore` — In-Memory-Implementierung von `IItemContentStore` für `ItemRepository`-Konstruktion.
- `FakeNetworkStatusService` — steuerbares `IsOnline` + `RaiseConnectivityChanged()`.
- `ThrowingFeedRepository`, `ThrowingCategoryRepository` — Fehlerfälle für Repository-Aufrufe.
