<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenmodell — Bestandsaufnahme

Bezug: Anforderung „Artikeldaten getrennt von Nutzerdaten speichern (iCloud-Backup)" (`requirement.md`). Es existiert genau ein `DbContext` (`ReporterDbContext`) und genau eine SQLite-Datei `reporter.db`; ein Content-Speicher (z. B. `ItemContent`-Entity, `ContentDbContext`, `ContentDatabasePath`) existiert noch nicht.

## `Item` (Entity)

Datei: `src/Reporter.Data/Entities/Item.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel (`items.id`). |
| `FeedId` | `Guid` | Fremdschlüssel auf `feeds.id`; `OnDelete(DeleteBehavior.Cascade)` in `ReporterDbContext.ConfigureItem` (Zeile 119). |
| `Title` | `string` | Pflicht, `title`, max. 500. |
| `Link` | `string?` | `link`, max. 2048. |
| `PublishedAt` | `DateTime?` | `published_at`; Sortierkriterium der Listen. |
| `GuidOrHash` | `string` | Pflicht, `guid_or_hash`, max. 500; Identität für die Deduplizierung, Unique-Index zusammen mit `FeedId` (`items`-Konfiguration Zeile 120). |
| `IsRead` | `bool` | `is_read`; Nutzerdatum (Lesestatus). |
| `IsSavedForLater` | `bool` | `is_saved_for_later`; Nutzerdatum (Merkliste). |
| `ReadAt` | `DateTime?` | `read_at`; Nutzerdatum, Referenzzeitpunkt der Retention. |
| `ContentHtml` | `string?` | `content_html`; re-downloadbarer Artikelinhalt — die Spalte, die laut Anforderung in den Content-Speicher wandern soll. |
| `Feed` | `Feed` | Navigation zur `Feed`-Entity (`= null!`). |

Die Spalte `content_html` ist in der Migration `src/Reporter.Data/Migrations/20260909214617_InitialCreate.cs` (Zeile 93) und im `ReporterDbContextModelSnapshot` (Zeile 154) vorhanden — Bestandsinstallationen tragen sie in `reporter.db`.

## `Item` (Domänenmodell)

Datei: `src/Reporter.Core/Models/Item.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Entspricht `items.id`. |
| `FeedId` | `Guid` (required, init) | Feed-Zuordnung. |
| `Title` | `string` (required, init) | Titel. |
| `Link` | `string?` (init) | Artikel-Link. |
| `PublishedAt` | `DateTime?` (init) | Veröffentlichungszeitpunkt. |
| `GuidOrHash` | `string` (required, init) | Deduplizierungs-Schlüssel (auch vom `FeedSyncService` via `HashSet` genutzt). |
| `IsRead` | `bool` (required, init) | Lesestatus. |
| `IsSavedForLater` | `bool` (required, init) | Merkliste. |
| `ReadAt` | `DateTime?` (init) | Lesezeitpunkt. |
| `ContentHtml` | `string?` (init) | HTML-Inhalt; wird aktuell eins zu eins aus `items.content_html` gemappt (`ItemRepository.MapToModel`/`MapToEntity`). |

## `ItemListItem`

Datei: `src/Reporter.Core/Models/ItemListItem.cs`

Listen-Projektion für `UnreadPage`/`LaterPage` (`ArticleCardView`). Wird in `ItemRepository.SelectListItemRows`/`MapToListItem` befüllt.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Item-Id. |
| `FeedId` | `Guid` (required, init) | Feed-Id. |
| `Title` | `string` (required, init) | Titel. |
| `Link` | `string?` (init) | Link. |
| `PublishedAt` | `DateTime?` (init) | Sortierdatum. |
| `IsRead` | `bool` (required, init) | Lesestatus. |
| `IsSavedForLater` | `bool` (required, init) | Merkstatus. |
| `FeedTitle` | `string` (init) | Anzeigetitel des Feeds (Join über `items.feed_id`). |
| `CategoryId` | `Guid?` (init) | Kategorie des Feeds. |
| `CategoryName` | `string?` (init) | Kategoriename. |
| `ImageUrl` | `string?` (init) | Aus `ContentHtml` extrahiert (`ItemRepository.ExtractImageUrl`). |
| `FeedFaviconUrl` | `string?` (init) | Favicon des Feeds. |
| `FeedInitial` | `string` (get) | Erster Buchstabe von `FeedTitle` via `FeedAvatar.Initial`, Fallback `"?"`. |
| `Summary` | `string?` (init) | Aus `ContentHtml` extrahiert (`ItemRepository.ExtractSummary`, max. 120 Zeichen). |
| `ReadingTimeText` | `string?` (init) | Aus `ContentHtml` geschätzt (`ReadingTimeEstimator.EstimateText`). |
| `CopyWith(bool?, bool?)` | `ItemListItem` | Kopie mit geändertem Lese-/Merkstatus. |

Relevant: `ImageUrl`, `Summary` und `ReadingTimeText` werden pro Listeneintrag zur Laufzeit aus `ContentHtml` abgeleitet — die Listenseite liest die Spalte `content_html` bereits in der SQL-Projektion mit (`SelectListItemRows`, `ItemRepository.cs` Zeile 310).

## `ReporterDbContext`

Datei: `src/Reporter.Data/ReporterDbContext.cs`

| Member | Art | Beschreibung / Zweck |
|--------|-----|----------------------|
| `Feeds` | `DbSet<Feed>` | Tabelle `feeds`. |
| `Categories` | `DbSet<Category>` | Tabelle `categories`, Unique-Index `name`. |
| `Items` | `DbSet<Item>` | Tabelle `items` (siehe `ConfigureItem`, Zeilen 104–121). |
| `Keywords` | `DbSet<Keyword>` | Tabelle `keywords`, Unique-Index `keyword_text`. |
| `Settings` | `DbSet<Settings>` | Tabelle `settings`; `HasData(new Settings())` seedet eine Standardzeile. |
| `SyncLogs` | `DbSet<SyncLog>` | Tabelle `sync_logs`; `FeedId` mit `OnDelete(DeleteBehavior.SetNull)`. |
| `DebugLogEntries` | `DbSet<DebugLogEntry>` | Tabelle `debug_log_entries`, Index auf `timestamp`. |
| `OnModelCreating` | protected override | Ruft die `Configure*`-Methoden je Entity auf. |

Ein einziger Kontext verwaltet alle Tabellen in `reporter.db`. Laufzeitverbindung: `IDbContextFactory<ReporterDbContext>` mit `UseSqlite("Data Source={databasePath}")` in `MauiProgram` (Zeile 73). Design-Zeit: `ReporterDbContextFactory` (`src/Reporter.Data/ReporterDbContextFactory.cs`, `IDesignTimeDbContextFactory<ReporterDbContext>`, `Data Source=reporter.db`).

## `DatabasePath`

Datei: `src/Reporter.Core/Models/DatabasePath.cs`

| Member | Typ | Beschreibung / Zweck |
|--------|-----|----------------------|
| `DatabasePath(string filePath)` | Konstruktor | Träger-Objekt. |
| `FilePath` | `string` (get) | Effektiver DB-Dateipfad inkl. `REPORTER_DB_PATH`-Override; als Singleton in `MauiProgram` (Zeile 88) registriert; wird in `App.ExcludeDatabaseFilesFromBackup` und in den `ServiceCollectionTests` verwendet. |

Es existiert kein Pendant für einen Content-Speicher.

## Weitere Entities in `reporter.db`

Alle liegen in `src/Reporter.Data/Entities/` und bleiben laut Anforderung im Nutzerdaten-Speicher (Annahme für `SyncLog`/`DebugLogEntry`, siehe Offene Fragen der Anforderung):

| Entity | Datei | Eigenschaften |
|--------|-------|---------------|
| `Feed` | `src/Reporter.Data/Entities/Feed.cs` | `Id`, `Url` (unique), `Title`, `CategoryId` (FK → `categories`, `SetNull`), `LastCheckedAt`, `HealthStatus`, `HealthLastChange`, `NotificationsEnabled`, `FaviconUrl`, `LastErrorKind`, `LastErrorMessage`, `Category` (Navigation) |
| `Category` | `src/Reporter.Data/Entities/Category.cs` | `Id`, `Name` (unique) |
| `Keyword` | `src/Reporter.Data/Entities/Keyword.cs` | `Id`, `KeywordText` (unique) |
| `Settings` | `src/Reporter.Data/Entities/Settings.cs` | `Id` (Default `DefaultId`), `RetentionDays`, `AutoMarkReadMode`, `AutoMarkReadDelaySeconds`, `NotificationsEnabled`, `QuietHoursStart`, `QuietHoursEnd`, `AutoRefreshEnabled`, `RefreshIntervalMinutes`, `RefreshOnStartupEnabled`, `UnreadSortOrder`, `Theme`, `NotificationSummaryEnabled`, `Language`, `DebugCollectionEnabled` |
| `SyncLog` | `src/Reporter.Data/Entities/SyncLog.cs` | `Id`, `FeedId` (FK → `feeds`, `SetNull`), `StartedAt`, `FinishedAt`, `Status`, `Message`, `Feed` (Navigation) |
| `DebugLogEntry` | `src/Reporter.Data/Entities/DebugLogEntry.cs` | `Id`, `Timestamp` (Index), `Level`, `Category`, `Message`, `Details` |

## `FirstRunState`

Datei: `src/Reporter.Core/Models/FirstRunState.cs`

| Member | Typ | Beschreibung / Zweck |
|--------|-----|----------------------|
| `IsFirstRun` | `bool` (init) | `true`, wenn `reporter.db` vor `builder.Build()` noch nicht existierte (`MauiProgram` Zeile 61). |
| `DemoSeedSuppressed` | `bool` (init) | Aus `REPORTER_DISABLE_DEMO_SEED` ausgewertet. |
| `ShouldSeedDemoContent` | `bool` (get) | `IsFirstRun && !DemoSeedSuppressed`; steuert `DemoContentService.EnsureSeededAsync`. |

Relevant für die Startsequenz: Die Erststart-Erkennung hängt am Vorhandensein der einen DB-Datei; ein zweiter Speicher muss diese Logik berücksichtigen.
