<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Artikeldaten getrennt von Nutzerdaten speichern (iCloud-Backup)

Analysiert wurde der Ist-Zustand der Datenspeicherung der Reporter-App bezogen auf die Anforderung aus `requirement.md` (Issue #105): Trennung von re-downloadbarem Artikel-Content (`Item.ContentHtml`) und Nutzerdaten, damit nur der Content-Speicher vom iCloud-Backup ausgeschlossen bleibt.

## Zusammenfassung

- **Ein einziger Speicher existiert heute:** Alle Entities (`Feed`, `Category`, `Item`, `Keyword`, `Settings`, `SyncLog`, `DebugLogEntry`) liegen in einer SQLite-Datei `reporter.db` (`FileSystem.AppDataDirectory`, Override `REPORTER_DB_PATH`) hinter einem `ReporterDbContext` + einer `IDbContextFactory`. Die Spalte `items.content_html` existiert seit `InitialCreate` (`20260909214617_InitialCreate.cs` Zeile 93, ModelSnapshot Zeile 154).
- **Backup-Ausschluss ist vorhanden, aber nur in eine Richtung:** `IBackupExclusionService`/`BackupExclusionService` setzt unter `#if IOS` `NSUrl.IsExcludedFromBackupKey` (= `NSNumber.FromBoolean(true)`); `App.ExcludeDatabaseFilesFromBackup` wendet es auf `reporter.db` + `-wal` + `-shm` an. Ein Umkehrpfad (Wieder-Einschließen) existiert nicht.
- **`ContentHtml` wird an mehreren Stellen gelesen/geschrieben:** `ItemRepository` (Insert/Update/Select inkl. Listenprojektion `SelectListItemRows`/`MapToListItem` → `ImageUrl`/`Summary`/`ReadingTimeText`), `FeedSyncService` (`GetContentHtml` → `AddRangeAsync`; `GuidOrHash`-`HashSet`-Deduplizierung überspringt bekannte Items komplett — kein Content-Backfill-Pfad vorhanden), `RetentionCleanupService` (`MatchesAny` auf `i.ContentHtml` der Kandidaten), `NotificationService` (In-Memory), `ArticleDetailViewModel` (WebView-Rendern; defensiver Leer-Pfad existiert).
- **Löschpfade:** `DeleteAsync`, `DeleteExpiredAsync`, `DeleteRangeAsync` und die DB-Kaskade `items.feed_id → OnDelete(Cascade)` entfernen ganze `items`-Zeilen; Content liegt in derselben Zeile — datenbankübergreifende Kaskaden sind nicht vorhanden.
- **Startsequenz ist fehlerisoliert erweiterbar:** `App.OnStart` orchestriert `MigrateDatabaseAsync` → `ExcludeDatabaseFilesFromBackup` → `SeedDemoContentAsync` → `CleanupRetainedDataAsync` u. a. über `RunStartupStep`/`RunStartupStepAsync`; `MauiProgram` registriert `DatabasePath`- und `FirstRunState`-Singletons (DI-Träger-Muster für einen Content-Pfad wäre analog nutzbar).
- **Nicht vorhanden (für die Anforderung fehlend):** Content-Entity (z. B. `ItemContent`), zweiter `DbContext`/`reporter-content.db`, Pfad-Träger (z. B. `ContentDatabasePath`), Content-Store-Interface (`IContentStore`/`IItemContentRepository`), Backup-Re-Include, Content-Migrations-/Backfill-Mechanismus, Content-Bereinigung im Retention-Cleanup.
- **Test-Ausgangszustand:** `dotnet test Reporter.sln --filter "Category!=E2E"` → 531/531 erfolgreich, 0 fehlgeschlagen (Exit 0); `npm test` → 36/36 erfolgreich (Exit 0). Keine nachgewiesenen Testfehler. Testlücken: 9 E2E-Tests (`Category=E2E`) nicht ausgeführt (per Vorgabe ausgenommen, benötigen interaktive Windows-Session); kein EF-Migrationspfad getestet (`EnsureCreated` statt `Migrate`); `BackupExclusionService` iOS-only/#if IOS; keine Assertions auf die ausgeschlossenen Pfade; `REPORTER_DB_PATH` nur über E2E abgedeckt. Details und Nachweise: [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md) — `Item` (Entity + Domänenmodell), `ItemListItem`, `ReporterDbContext`, `DatabasePath`, übrige Entities, `FirstRunState`.
- [Logik](inventory/logic.md) — `ItemRepository`, `FeedSyncService`, `RetentionCleanupService`, `NotificationService`, `BackupExclusionService`, `App`, `MauiProgram`, `FeedRepository` (Kaskade), Content-Konsumenten, `ArticleDetailViewModel`.
- [Enums](inventory/enums.md) — `FeedHealth`, `FeedSyncErrorKind`, `SettingsValues`, `DebugLogCategory`/`DebugLogLevel` (Konstanten-Typen; keine Speicher-Enums vorhanden).
- [Interfaces](inventory/interfaces.md) — `IItemRepository`, `IBackupExclusionService`, `IKeywordFilter`, `IKeywordMatcher`, `IFeedSyncService`, `IRetentionCleanupService`, `INotificationService`, `IFeedRepository`, `IDbContextFactory<ReporterDbContext>`.
- [UI-Komponenten](inventory/ui.md) — `ArticleDetailViewModel`/`ArticleDetailPage`, `UnreadPage`/`LaterPage`/`ArticleCardView`, indirekt betroffene Oberflächen.
- [Dokumentation](inventory/documentation.md) — Ist-Zustand der zu aktualisierenden Doku (`docs/app-store-review.md`, `docs/privacy-policy.md`, `docs/help/anwendung/*`, `docs/help/tests/*`).
- [Tests](inventory/tests.md) — Test-Ausgangszustand (Läufe, Exit-Codes, Zähler), nachgewiesene Fehler (keine), Testlücken, Testklassen und Hilfsmethoden, E2E-Infrastruktur. Testnachweise unter [inventory/test-results/](inventory/test-results/).
