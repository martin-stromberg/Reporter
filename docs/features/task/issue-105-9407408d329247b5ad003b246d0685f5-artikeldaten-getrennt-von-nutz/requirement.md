<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung — Issue #105: Artikeldaten getrennt von Nutzerdaten speichern (iCloud-Backup)

## Fachliche Zusammenfassung

Die gesamte SQLite-Datenbank `reporter.db` unter `FileSystem.AppDataDirectory` wird aktuell beim App-Start vollständig vom iCloud-Backup ausgeschlossen (`App.ExcludeDatabaseFilesFromBackup` → `IBackupExclusionService`/`BackupExclusionService` setzt `NSURL.IsExcludedFromBackupKey` auf `reporter.db`, `-wal`, `-shm`). Dadurch gehen bei einer Geräte-Wiederherstellung auch die nicht wiederherstellbaren Nutzerdaten verloren: Abonnements (`Feed`), `Category`, `Keyword`, `Settings` sowie Lesestatus und Merkliste der `Item`s (`IsRead`, `IsSavedForLater`, `ReadAt`).

Die Anforderung ist die Aufteilung in zwei Speicherbereiche: einen **Nutzerdaten-Speicher** (verbleibt im iCloud-Backup) und einen **Content-Speicher** für re-downloadbare Massendaten — mindestens `Item.ContentHtml` — der weiterhin vom Backup ausgeschlossen bleibt. Dazu gehören die Anpassung des Backup-Ausschlusses, die Migration bestehender Installationen beim Update, die Erweiterung des Retention-Cleanups auf den Content-Speicher sowie die Aktualisierung der Dokumentation.

## Betroffene Klassen und Komponenten

### Datenmodell

- `Reporter.Data.Entities.Item` (`src/Reporter.Data/Entities/Item.cs`) — die Spalte `content_html` wird aus der `items`-Tabelle per EF-Core-Migration entfernt und in den Content-Speicher verlagert.
- Neue Content-Entity (bei SQLite-Variante), z. B. `ItemContent` mit `ItemId` als Schlüssel und `ContentHtml`; die Zuordnung läuft über `Item.Id`, Matching-Kandidaten (`GuidOrHash`, `FeedId`, `Link`, `PublishedAt`) verbleiben in `reporter.db`.
- `Reporter.Core.Models.Item` (`src/Reporter.Core/Models/Item.cs`) — `ContentHtml` bleibt voraussichtlich im Domänenmodell, wird aber aus dem Content-Speicher befüllt.
- `ReporterDbContext` (`src/Reporter.Data/ReporterDbContext.cs`) — Modelländerung an `items`; bei der SQLite-Variante zusätzlich ein zweiter `DbContext` (z. B. `ContentDbContext`) für `reporter-content.db` samt eigener Migrationshistorie.
- `DatabasePath` (`src/Reporter.Core/Models/DatabasePath.cs`) — Pendant für den Content-Speicher, z. B. `ContentDatabasePath`/`ContentStorePath` nach demselben DI-Träger-Muster.
- `SyncLog` und `DebugLogEntry` — verbleiben nach aktueller Annahme in `reporter.db` (klein; `DebugLogEntry` ist sitzungsbezogen). Annahme, siehe Offene Fragen.

### Logikklassen / Services

- `ItemRepository` (`src/Reporter.Data/Repositories/ItemRepository.cs`) — alle `ContentHtml`-Pfade müssen auf die zweistufige Speicherung umgestellt werden:
  - Schreibpfade: `AddAsync`, `AddRangeAsync`, `UpdateAsync` schreiben Content in den neuen Speicher.
  - Lesepfade: `GetByIdAsync`, `GetAllAsync`, `GetByFeedAsync`, `GetByCategoryAsync`, `MapToModel`/`MapToEntity` befüllen `ContentHtml` aus dem Content-Speicher.
  - `SelectListItemRows`/`MapToListItem` leiten `ItemListItem.ImageUrl`, `Summary` und `ReadingTimeText` pro Listeneintrag aus `ContentHtml` ab — diese Listenprojektion braucht künftig Content-Zugriff oder eine Alternative.
  - `GetExpiredKeywordCandidatesAsync` liefert Kandidaten, deren `ContentHtml` der `RetentionCleanupService` gegen `IKeywordFilter.MatchesAny` prüft — Matching braucht Content aus dem neuen Speicher.
  - `DeleteAsync`, `DeleteExpiredAsync`, `DeleteRangeAsync` sowie die Feed-Kaskade (`OnDelete(DeleteBehavior.Cascade)` auf `items.feed_id`) müssen zugehörige Content-Einträge mitentfernen — datenbankübergreifendes Cascade ist nicht möglich.
- `FeedSyncService` (`src/Reporter.Core/Services/FeedSyncService.cs`) — schreibt `ContentHtml` beim Sync in den Content-Speicher. Die heutige In-Memory-Deduplizierung über `GuidOrHash` (`HashSet` + `AddRangeAsync`) überspringt bereits bekannte Items komplett — für den Restore-Fall (Items vorhanden, Content fehlt) muss ein Nachladen fehlender Inhalte ergänzt werden.
- `RetentionCleanupService` (`src/Reporter.Core/Services/RetentionCleanupService.cs`) — `CleanupAsync` (aufgerufen aus `App.CleanupRetainedDataAsync`) muss den Content-Speicher mitbereinigen: keine verwaisten Inhalte gelöschter oder abgelaufener Items.
- `BackupExclusionService`/`IBackupExclusionService` (`src/Reporter/Services/BackupExclusionService.cs`, `src/Reporter.Core/Interfaces/IBackupExclusionService.cs`) — Mechanismus unverändert wiederverwendbar; Aufrufstelle `App.ExcludeDatabaseFilesFromBackup` (`src/Reporter/App.xaml.cs`) schließt nur noch die Content-Speicher-Dateien aus (bei SQLite-Variante inkl. `-wal`/`-shm`); `reporter.db` wird nicht mehr ausgeschlossen.
- `NotificationService` (`src/Reporter.Core/Services/NotificationService.cs`) — nutzt `IKeywordFilter.MatchesAny(title, contentHtml, …)` auf neuen Items; Content liegt hier noch im Arbeitsspeicher vor, prüfen, ob die Signatur unverändert bleiben kann.
- `MauiProgram.CreateMauiApp` (`src/Reporter/MauiProgram.cs`) — Registrierung des Content-Speicher-Pfads, ggf. zweite `IDbContextFactory`; Ableitung des Content-Pfads muss den `REPORTER_DB_PATH`-Override berücksichtigen (E2E-Hermetik); `isFirstRun`-Erkennung beachten.
- `App.OnStart` (`src/Reporter/App.xaml.cs`) — `ExcludeDatabaseFilesFromBackup` anpassen; neuer, fehlerisolierter Start-Schritt für die Bestandsmigration über die vorhandene `RunStartupStep`/`RunStartupStepAsync`-Absicherung; Content-DB-Initialisierung/Migration als eigener Schritt.

### Interfaces

- Neues Interface für den Content-Speicher (z. B. `IContentStore`/`IItemContentRepository` in `Reporter.Core.Interfaces`) mit Implementierung je nach gewählter Technologie (EF-basiert in `Reporter.Data` oder dateibasiert in `Reporter.Services`).
- `IItemRepository` — Signaturänderungen prüfen (Content-Zugriff transparent im Repository oder separates Interface).

### UI-Komponenten

Keine neuen Oberflächen erforderlich. Indirekt betroffen:

- `ArticleDetailViewModel`/`ArticleDetailPage` — lädt `ContentHtml` über `IItemRepository.GetByIdAsync`; fehlender Content muss defensiv behandelt werden (leerer `HtmlSource`-Pfad in `RebuildHtml` existiert bereits; ggf. konsistentes Offline-/Re-Fetch-Verhalten).
- `UnreadPage`/`LaterPage` über `ArticleCardView` — Karten zeigen `ImageUrl`/`Summary`/`ReadingTimeText`, die aus `ContentHtml` abgeleitet werden.

### Tests

- `src/Reporter.Tests`: `ItemRepositoryTests`, `FeedSyncServiceTests`, Retention-Cleanup-Tests, `ServiceCollectionTests` (DI-Auflösung des neuen Speichers/Pfad-Trägers), `FakeBackupExclusionService`-gestützte Tests der neuen Ausschluss-Pfade, neue Migrationstests für den Update-Pfad (Bestands-`reporter.db` mit `content_html` → neue Aufteilung).
- `src/Reporter.E2ETests`: `FeedDbAssertions`/`StubFeedServer`/`ReporterAppFixture` — `REPORTER_DB_PATH` zeigt auf ein Temp-Verzeichnis; der Content-Pfad muss daraus ableitbar sein oder ein eigenes Override erhalten, damit die Suite hermetisch bleibt; ggf. Assertion für die Content-DB.

### Dokumentation

- `docs/app-store-review.md` — Backup-Verhalten neu beschreiben (nur Content ausgeschlossen, Nutzerdaten gesichert).
- `docs/privacy-policy.md` — Abschnitt „Lokale Datenspeicherung" beschreibt aktuell eine einzelne, vollständig ausgeschlossene `reporter.db` — anpassen.
- `docs/help/anwendung/offline.md`, `docs/help/anwendung/datenmodell.md`, `docs/help/anwendung/architektur.md`, `docs/help/anwendung/aufbewahrung.md` — neue Speicheraufteilung dokumentieren.
- `docs/help/tests/*` — falls `REPORTER_DB_PATH`-Verhalten oder `FeedDbAssertions` angepasst werden.

## Implementierungsansatz

- **Content-Speicher:** Eine zweite SQLite-Datei (z. B. `reporter-content.db`) im selben Verzeichnis wie `reporter.db` ist die naheliegende Variante — konsistent zum bestehenden EF-Core-Stack (`IDbContextFactory`, Migrationen) und zum `BackupExclusionService` (`IsExcludedFromBackupKey` auf Datei plus `-wal`/`-shm`). Alternativen laut Anforderung: dateibasiertes Verzeichnis unter ausgeschlossenem Pfad oder `NSCachesDirectory` (strengste Variante, System darf bei Speicherdruck löschen — Re-Fetch-Toleranz nötig). Entscheidung offen.
- **Aufteilung der `Item`-Daten:** `Title`, `Link`, `PublishedAt`, `GuidOrHash`, `FeedId` sowie `IsRead`, `IsSavedForLater`, `ReadAt` verbleiben in `reporter.db` (Empfehlung — klein, für Identität/Wiederzuordnung, Sortierung, Listen und Retention nötig); nur `ContentHtml` wandert in den Content-Speicher. Begründungspflichtige Designentscheidung laut Anforderung.
- **Backup-Ausschluss:** `ExcludeFromBackup` nur noch auf Content-Pfade anwenden; `reporter.db` (inkl. `-wal`/`-shm`) nicht mehr ausschließen. Bestandsgeräte: Das auf `reporter.db` gesetzte `IsExcludedFromBackupKey`-Flag muss beim ersten Start nach dem Update aktiv entfernt werden — der vorhandene Service kann nur ausschließen, nicht wieder einschließen (`NSNumber.FromBoolean(true)`); hier fehlt der Umkehrpfad und ist zu ergänzen.
- **Migration beim Update:** Neuer `RunStartupStepAsync`-Schritt beim Start: bestehende `content_html`-Werte in den neuen Speicher überführen oder bewusst verwerfen (Entscheidung dokumentieren); darf den App-Start nicht blockieren.
- **Re-Fetch nach Restore:** Inhalte fehlen nach Wiederherstellung; der nächste Sync muss sie zurückholen. Zu klären, wie — die `GuidOrHash`-Deduplizierung allein reicht nicht (siehe Offene Fragen).
- **Retention:** Item-Löschungen (Retention, Keyword-Regel, Feed-Löschung, manuelles `DeleteAsync`) müssen Content-Einträge explizit mitlöschen; ggf. zusätzlich Content-Waisen ohne Item-Zeile aufräumen.
- **DI/Startsequenz:** `DatabasePath` bleibt; neuer Pfad-Träger und ggf. zweite `IDbContextFactory` in `MauiProgram` registrieren; Content-Pfad aus dem `databasePath`-Verzeichnis ableiten, damit `REPORTER_DB_PATH`-Overrides automatisch greifen.
- **Abhängigkeiten:** `IKeywordFilter.MatchesAny` bleibt In-Memory, aber die Aufrufer (`FeedSyncService`, `NotificationService`, `RetentionCleanupService`) benötigen `ContentHtml` zur Laufzeit aus dem neuen Speicher; `ReadingTimeEstimator`, `ExtractImageUrl`/`ExtractSummary` und `ArticleHtmlSanitizer` bleiben unverändert, werden aber an anderer Stelle mit Content versorgt.

## Konfiguration

Kein neuer benutzerseitiger Konfigurationsbedarf — das Verhalten ist fest vorgegeben (kein Schalter). Technische Konfiguration:

- Pfad des Content-Speichers über DI-Träger analog `DatabasePath`.
- Für E2E-Isolation: Ableitung aus dem `REPORTER_DB_PATH`-Verzeichnis oder neues Env-Override (z. B. `REPORTER_CONTENT_DB_PATH`) — Entscheidung im Umsetzungsplan.
- `Settings` benötigt voraussichtlich kein neues Feld; ein etwaiger Migrations-Zustand („Content-Migration gelaufen") kann über das Vorhandensein des Content-Speichers oder eine interne Markierung abgeleitet werden (Annahme).

## Offene Fragen

1. **Content-Speicher-Technologie:** Zweite SQLite-Datei (`reporter-content.db` + `IsExcludedFromBackupKey`) vs. dateibasiertes Verzeichnis vs. `NSCachesDirectory`? Die Anforderung lässt die Wahl frei; die SQLite-Variante ist konsistent zum `BackupExclusionService`.
2. **Migration des Altbestands:** Bestehende `content_html`-Werte in den neuen Speicher kopieren (Offline-Verfügbarkeit bleibt, kostet Startzeit/Speicher) oder verwerfen (einfacher, Inhalte kommen beim nächsten Sync wieder)? Die Anforderung verlangt eine dokumentierte Entscheidung.
3. **Content-Backfill nach Restore:** Wie wird `ContentHtml` für Items nachgeladen, die in `reporter.db` weiterhin existieren und daher von der `GuidOrHash`-Deduplizierung im `FeedSyncService` übersprungen werden — Backfill fehlender Inhalte beim Sync oder Lazy-Load beim Öffnen der Detailansicht?
4. **Backup-Flag zurücksetzen:** Wie wird das bereits gesetzte `IsExcludedFromBackupKey` auf `reporter.db` bei Bestandsinstallationen wieder entfernt (`NSNumber.FromBoolean(false)`-Pfad im `BackupExclusionService` oder Datei-Neuanlage)? Ohne Umkehrung bliebe die Nutzer-DB weiterhin ausgeschlossen.
5. **Verbleib von `SyncLog`/`DebugLogEntry`:** In `reporter.db` belassen (Annahme: ja — klein, teils sitzungsbezogen) oder in den Content-Speicher verschieben?
6. **Content-Zugriff für Listen und Matching:** `ItemListItem`-Ableitungen (`Summary`, `ImageUrl`, `ReadingTimeText`) und das Keyword-Matching in `RetentionCleanupService`/`NotificationService` benötigen `ContentHtml` — Cross-Store-Zugriff je Aufruf oder Vorhalten abgeleiteter/redundanter Felder im Nutzerdaten-Speicher?
7. **E2E-Hermetik:** Reicht die Ableitung des Content-Pfads aus dem `REPORTER_DB_PATH`-Verzeichnis, oder ist ein separates Override (`REPORTER_CONTENT_DB_PATH`) nötig?
