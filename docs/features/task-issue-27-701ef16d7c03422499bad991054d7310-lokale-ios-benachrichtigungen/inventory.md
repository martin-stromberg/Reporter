# Bestandsaufnahme: Lokale iOS-Benachrichtigungen mit Ruhezeiten (Issue #27)

Analysiert wurde der bestehende Projektcode (Reporter: .NET MAUI-App mit `Reporter.Core`, `Reporter.Data` und `Reporter.Tests`) bezogen auf die Anforderung „Lokale iOS-Benachrichtigungen mit Ruhezeiten" (`requirement.md`).

## Zusammenfassung

- **Globaler Benachrichtigungs-Schalter und Ruhezeiten sind bereits vollständig vorhanden:** `Settings.NotificationsEnabled`, `Settings.QuietHoursStart` und `Settings.QuietHoursEnd` existieren im Core-Modell, in der Daten-Entität (Spalten `notifications_enabled`, `quiet_hours_start`, `quiet_hours_end`), im `SettingsRepository`-Mapping, im `SettingsViewModel` und in der `SettingsPage`-Karte „Benachrichtigungen & Ruhezeiten". Laut `docs/help/einstellungen/business-rules.md` (Abschnitt „Ruhezeiten ohne Start-vor-Ende-Validierung") werden Ruhezeiten unvalidiert gespeichert; die Auswertung ist explizit diesem Arbeitspaket vorbehalten.
- **Pro-Feed-Schalter fehlt vollständig:** Weder `Reporter.Core.Models.Feed` noch `Reporter.Data.Entities.Feed` noch `Reporter.Core.Models.FeedListItem` besitzen eine `NotificationsEnabled`-Eigenschaft; `FeedRepository`-Mapping (`MapToModel`/`MapToEntity`/`UpdateAsync`/`GetAllWithDetailsAsync`), `ReporterDbContext.ConfigureFeed` und `FeedsViewModel`/`FeedsPage.xaml` kennen das Feld nicht.
- **Kein Benachrichtigungs-Service vorhanden:** In `src/Reporter.Core/Interfaces` existiert weder `INotificationService` noch `ILocalNotificationService`; unter `src/Reporter/Services/` existiert nur `AppThemeService` als Muster für einen Plattformdienst. Der iOS-`AppDelegate` ist minimal, `Info.plist` enthält keine Benachrichtigungs-Schlüssel.
- **Erweiterungspunkt im Sync vorhanden:** `FeedSyncService.RunSyncAsync` zählt neu gespeicherte `Item`s nur als `newItems`-Zähler; `SyncResult` trägt nur `Status`, `NewItems`, `Message`. Alle Sync-Pfade laufen über `SyncFeedAsync` (manuell via `FeedsViewModel`, periodisch via `AutoRefreshService.RunLoopAsync` → `SyncAllAsync`).
- **Wiederverwendbare Bausteine vorhanden:** `ISettingsRepository.GetAsync`, `IKeywordRepository.GetAllAsync`, `IKeywordMatcher.MatchesAny` (Teilwort, `OrdinalIgnoreCase`, auf `Title`/`ContentHtml`; bereits vom `RetentionCleanupService` genutzt), `IItemRepository.GetByGuidOrHashAsync` (Dublettenerkennung über Unique-Index `(feed_id, guid_or_hash)`), `TimeProvider`-Injection (Muster in `AutoRefreshService`/`SettingsViewModel`, in Tests via `FakeTimeProvider`).
- **Strings:** `AppResources.resx` und `AppResources.de.resx` enthalten bereits die Schlüssel für den globalen Benachrichtigungs- und Ruhezeiten-Bereich; kein Schlüssel für ein Pro-Feed-Benachrichtigungs-Label.

**Test-Ausgangszustand:** Die einzige Testsuite (`src/Reporter.Tests`, xUnit, 147 Tests) wurde zweimal ausgeführt. Lauf 1: 145 bestanden, **2 fehlgeschlagen** (`SettingsViewModelTests_Persist.RetentionDays_ChangeWithoutDragCompleted_PersistsAfterDebounce` und `Persist_QueuedBehindRunningSave_AppliesThemeOnce`, beide mit `SqliteException : SQLite Error 5: 'unable to delete/modify user-function due to active statements'` — Infrastruktur-/Timing-Problem der geteilten In-Memory-SQLite-Verbindung). Lauf 2 (Wiederholung, `--no-build`): alle 147 Tests bestanden. Die beiden Fehlschläge sind damit als vorab nachgewiesene, nicht-deterministische Testfehler dokumentiert. Nachweis siehe [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md) — `Feed` (Core/Entity), `FeedListItem`, `Settings` (Core/Entity), `Item`, `Keyword`, `SyncResult`
- [Logik](inventory/logic.md) — `FeedSyncService`, `AutoRefreshService`, `KeywordMatcher`, `RetentionCleanupService`, `FeedsViewModel`, `SettingsViewModel`, `App.OnStart`, `AppThemeService`, iOS-`AppDelegate`, `MauiProgram`
- [Enums und Konstanten](inventory/enums.md) — `FeedHealth`, `SettingsValues`
- [Interfaces](inventory/interfaces.md) — `IFeedSyncService`, `IFeedRepository`, `IItemRepository`, `ISettingsRepository`, `IKeywordRepository`, `IKeywordMatcher`, `IAutoRefreshService`, `IAppThemeService`
- [Tests](inventory/tests.md) — Test-Ausgangszustand, Testklassen und Hilfsmethoden
