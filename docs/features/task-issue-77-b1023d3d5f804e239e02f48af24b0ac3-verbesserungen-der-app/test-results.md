<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

## E2E-Abdeckung

E2E-Strategie gemäß Plan: In-Memory-E2E-Persistenztests (`SettingsViewModelTests_E2E` mit echten SQLite-Repositories) + dokumentierte manuelle UI-Verifikation (390 × 844-pt-Windows-Fenster, Light + Dark) mit Screenshots unter `test-results/issue-77/` — das Projekt besitzt keine UI-Testautomatisierung.

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| R1 — Startseite und Artikeldetailansicht zeigen bei 1-Minuten-Beitrag keine Lesezeit | `ReadingTimeEstimatorTests.EstimateText_OneMinuteContent_ReturnsEmpty`, `EstimateText_TwoMinuteContent_ReturnsText`, `ItemRepositoryTests.GetUnreadByDateAsync_OneMinuteReadingTime_ProjectsEmpty` + manuelle Verifikation (`manual-01-unread.png`, `manual-16-unread-light.png`) | Bestanden |
| R2 — Artikelkarte ohne `ImageUrl` zeigt Favicon bzw. Initialen-Kreis | `ItemRepositoryTests.GetUnreadByDateAsync_ProjectsFeedFaviconUrl` + manuelle Verifikation Light + Dark (`manual-13-unread-favicon.png`) | Bestanden |
| R2 — Feeds-Seite zeigt Favicon bzw. Initialen-Kreis in der Feed-Karte | `FeedRepositoryTests.GetAllWithDetailsAsync_ProjectsFaviconUrl`, `FeedRepositoryTests.UpdateAsync_PersistsFaviconUrl`, `FeedsViewModelTests.RenameFeedAsync_PreservesFaviconUrl`, `FeedListItemTests.*` (FeedAvatar/FeedInitial) + manuelle Verifikation (`manual-11-feeds.png`, `manual-14-feeds-initial.png`) | Bestanden |
| R2 — Favicon-Nachholung beim Sync für Bestandsfeeds | `FeedSyncServiceTests.SyncFeedAsync_MissingFavicon_BackfillsFromFeedAuthority`, `SyncFeedAsync_ExistingFavicon_SkipsLookup`, `SyncFeedAsync_FaviconLookupFails_SyncStillSucceeds`, `FeedIconServiceTests.TryFindFaviconUrlAsync_*` | Bestanden |
| R3 — Schalter „Beim Programmstart abrufen" persistiert und triggert Sync beim Start | `SettingsViewModelTests_E2E.E2E_RefreshOnStartup_PersistRoundtrip`, `AutoRefreshServiceTests.StartAsync_StartupRefreshEnabled_SyncsImmediately`, `StartAsync_StartupRefreshDisabled_DoesNotSyncImmediately`, `StartAsync_Offline_SkipsStartupSync`, `SettingsViewModelTests_Load.Load_PopulatesRefreshOnStartup`, `SettingsViewModelTests_Persist.RefreshOnStartup_Change_Persists`, `SettingsRepositoryTests.SaveAsync_PersistsStartupRefreshAndSortOrder` + manuelle Verifikation (`manual-03-settings-sync.png`, `manual-04-settings-startup.png`) | Bestanden |
| R4 — Sortier-Picker ändert Reihenfolge der Startseite | `SettingsViewModelTests_E2E.E2E_SortOrder_PersistRoundtrip`, `ItemRepositoryTests.GetUnreadByDateAsync_Ascending_OrdersByPublishedAtAscending`, `GetUnreadByDateAsync_Ascending_NullPublishedAt_SortsFirst`, `UnreadViewModelTests.LoadPage_PassesSortOrderFromSettings`, `LoadPage_InvalidSortOrder_UsesDescending`, `SettingsViewModelTests_Load.Load_PopulatesSelectedSortOrder`, `SettingsViewModelTests_Persist.SelectedSortOrder_Change_Persists` + manuelle Verifikation (`manual-05-sortorder-dropdown.png`, `manual-06-unread-asc.png`, `manual-12-unread-asc2.png`) | Bestanden |
| R5 — Neustart-Hinweis erscheint erst nach Sprachänderung, verschwindet bei Rückwahl | `SettingsViewModelTests_Persist.LanguageChange_SetsRestartHint`, `LanguageReverted_ClearsRestartHint`, `SettingsViewModelTests_Load.Load_ResetsRestartHint` + manuelle Verifikation (`manual-07-settings-language-nohint.png`, `manual-08/09-language-hint-visible.png`, `manual-10-language-hint-cleared.png`) | Bestanden |
| R6 — Neues Icon/Splash wird im Build verwendet | Manuelle Sichtprüfung Windows-Build (in `plan.md` Abschnitt „Umsetzungsstand" dokumentiert; `appiconfg.svg`/`splash.svg` ersetzt, Resizetizer-Ausgabe geprüft) | Bestanden |

## Zusammenfassung

- Gesamt: 407
- Bestanden: 407
- Fehlgeschlagen: 0
- Übersprungen: 0

Ausführung (Iteration 3, nach Review-Nacharbeit an `FeedSiteResolver.cs` (neu), `FeedSyncService.cs` (Favicon-Lookup in `RunSyncAsync` vorgezogen), `FakeFeedIconService.cs`, `FeedIconService.cs`): `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release --no-build --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"` (3,3 s). Zuvor `dotnet restore Reporter.sln -r win-x64 -p:IncludeIosTarget=false` und `dotnet build Reporter.sln -c Release --no-restore -p:IncludeIosTarget=false` — der Build benötigt auf diesem Rechner denselben `IncludeIosTarget=false`-Parameter wie der Restore (sonst NETSDK1005 für `net10.0-ios`); Ergebnis 0 Warnungen / 0 Fehler.

## Testabdeckung

**Abdeckung:** 46,7 % (2423/5192 Zeilen; Reporter.Core 88,7 %, Reporter.Data 20,7 % — letztere wird durch ~2600 Zeilen generierter EF-Migrationen verzerrt)

| Datei | Abdeckung |
|-------|-----------|
| Reporter.Data\Migrations\20260909214617_InitialCreate.cs | 0,0 % (generiert) |
| Reporter.Data\Migrations\20260909214617_InitialCreate.Designer.cs | 0,0 % (generiert) |
| Reporter.Data\Migrations\20260911080630_AddSettingsAutoRefreshAndTheme.cs | 0,0 % (generiert) |
| Reporter.Data\Migrations\20260911080630_AddSettingsAutoRefreshAndTheme.Designer.cs | 0,0 % (generiert) |
| Reporter.Data\Migrations\20260911181811_AddFeedNotificationsEnabled.cs | 0,0 % (generiert) |
| Reporter.Data\Migrations\20260911181811_AddFeedNotificationsEnabled.Designer.cs | 0,0 % (generiert) |
| Reporter.Data\Migrations\20260911181850_AddSettingsNotificationSummary.cs | 0,0 % (generiert) |
| Reporter.Data\Migrations\20260911181850_AddSettingsNotificationSummary.Designer.cs | 0,0 % (generiert) |
| Reporter.Data\Migrations\20260913110816_AddSettingsLanguage.cs | 0,0 % (generiert) |
| Reporter.Data\Migrations\20260913110816_AddSettingsLanguage.Designer.cs | 0,0 % (generiert) |
| Reporter.Data\Migrations\20260913211308_AddFeedFaviconUrl.cs | 0,0 % (generiert) |
| Reporter.Data\Migrations\20260913211308_AddFeedFaviconUrl.Designer.cs | 0,0 % (generiert) |
| Reporter.Data\Migrations\20260913211351_AddSettingsStartupRefreshAndSortOrder.cs | 0,0 % (generiert) |
| Reporter.Data\Migrations\20260913211351_AddSettingsStartupRefreshAndSortOrder.Designer.cs | 0,0 % (generiert) |
| Reporter.Data\Migrations\ReporterDbContextModelSnapshot.cs | 0,0 % (generiert) |
| Reporter.Core\Resources\Strings\AppResources.Designer.cs | 23,7 % (generiert) |
| Reporter.Core\Models\CategoryFilterItem.cs | 28,6 % |
| Reporter.Core\Services\FeedSearchUnavailableException.cs | 33,3 % |

## Fehlende Tests

Quelle: `Coverage-Daten`

Keine — alle Dateien mit 0 % Zeilenabdeckung sind generierte EF-Migrationen bzw. der `ModelSnapshot` und werden gemäß Konvention ignoriert. Die in dieser Iteration neu hinzugekommene bzw. geänderte Dateien sind abgedeckt: `Reporter.Core\Services\FeedSiteResolver.cs` 100 %, `Reporter.Core\Services\FeedIconService.cs` 97,0 % (via `FeedIconServiceTests` inkl. `TryFindFaviconUrlAsync_*`), `Reporter.Core\Services\FeedSyncService.cs` 95,1 % (via `FeedSyncServiceTests` inkl. `SyncFeedAsync_MissingFavicon_BackfillsFromFeedAuthority`, `SyncFeedAsync_ExistingFavicon_SkipsLookup`, `SyncFeedAsync_FaviconLookupFails_SyncStillSucceeds`).
