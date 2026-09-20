<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Anwendung — Datenmodell

## Entitäten

### `Category`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `Guid` | Eindeutige Kennung der Kategorie. |
| `Name` | `string` | Name der Kategorie. |

### `Feed`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `Guid` | Eindeutige Kennung des Feeds. |
| `Url` | `string` | URL des Feeds. |
| `Title` | `string` | Titel des Feeds. |
| `CategoryId` | `Guid?` | Optionale Kategoriekennung. |
| `LastCheckedAt` | `DateTime?` | Zeitpunkt der letzten Abfrage. |
| `HealthStatus` | `string?` | Aktueller Gesundheitsstatus. |
| `HealthLastChange` | `DateTime?` | Zeitpunkt der letzten Statusänderung. |
| `NotificationsEnabled` | `bool` | Pro-Feed-Schalter für Benachrichtigungen (Standard `true`). |
| `FaviconUrl` | `string?` | URL des Favicons der Feed-Website (optional). |
| `LastErrorKind` | `string?` | Kategorie des letzten Sync-Fehlers (ein `FeedSyncErrorKind`-Wert); `null`, wenn der letzte Abruf erfolgreich war. |
| `LastErrorMessage` | `string?` | Technische Rohmeldung des letzten Sync-Fehlers; `null`, wenn der letzte Abruf erfolgreich war. |

### `Item`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `Guid` | Eindeutige Kennung des Artikels. |
| `FeedId` | `Guid` | Kennung des zugehörigen Feeds. |
| `Title` | `string` | Titel des Artikels. |
| `Link` | `string?` | Link zum Originalartikel. |
| `PublishedAt` | `DateTime?` | Veröffentlichungszeitpunkt. |
| `GuidOrHash` | `string` | Original-GUID oder Hash. |
| `IsRead` | `bool` | Gibt an, ob der Artikel gelesen wurde. |
| `IsSavedForLater` | `bool` | Gibt an, ob der Artikel für später bewahrt wurde. |
| `ReadAt` | `DateTime?` | Zeitpunkt, an dem der Artikel gelesen wurde. |
| `ContentHtml` | `string?` | HTML-Inhalt für den Offline-Lesemodus; liegt nicht in der `items`-Tabelle, sondern in `item_contents` der separaten Content-Datenbank `reporter-content.db` (siehe `ItemContent`). |
| `Image` | `ItemImage?` | Lokal gespeichertes Artikelbild für die Offline-Verfügbarkeit; liegt ebenfalls in `item_contents` der Content-Datenbank (Spalten `image_data`, `image_content_type`, `image_url`). |

### `ItemContent` (Content-Datenbank `reporter-content.db`)

Re-downloadbarer Artikelinhalt samt Artikelbild, getrennt von den Nutzerdaten in `reporter.db` gehalten, damit die Nutzerdatenbank ins iCloud-Backup eingeschlossen werden kann, während die Massendaten ausgeschlossen bleiben. Eigener `ContentDbContext` mit eigener Migrationshistorie; es gibt keinen datenbankübergreifenden Foreign Key — die Zuordnung läuft allein über `ItemId` = `items.id`. Eine Zeile kann Inhalt, Bild oder beides tragen; schreibende Upserts aktualisieren die Felder getrennt (ein mitgegebenes Bild schreibt die Bildspalten, ein fehlendes Bild lässt sie unverändert, leerer Inhalt ohne Bild entfernt den gespeicherten Inhalt).

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `ItemId` | `Guid` | Kennung des zugehörigen Artikels (`items.id` in `reporter.db`); Primärschlüssel (`item_id`). |
| `ContentHtml` | `string?` | HTML-Inhalt des Artikels (`content_html`). |
| `ImageData` | `byte[]?` | Binärdaten des lokal gespeicherten Artikelbilds (`image_data`). |
| `ImageContentType` | `string?` | MIME-Typ des Bilds (`image_content_type`, z. B. `image/png`). |
| `ImageUrl` | `string?` | Quell-URL des Bilds (`image_url`); bleibt als Remote-Fallback erhalten. |

### `ItemImage` (Wertobjekt)

Domänenmodell des Artikelbilds; wird beim Lesen aus den drei `image_*`-Spalten der `item_contents`-Zeile hydratisiert.

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Data` | `byte[]` | Bilddaten. |
| `ContentType` | `string?` | MIME-Typ des Bilds. |
| `Url` | `string?` | Quell-URL des Bilds (Remote-Fallback). |

### `Keyword`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `Guid` | Eindeutige Kennung des Keywords. |
| `KeywordText` | `string` | Der Keyword-Text. |
| `FeedId` | `Guid?` | Optionale Feed-Zuordnung (Spalte `feed_id`, FK auf `feeds.id`); `null` kennzeichnet ein globales Schlagwort. |

### `Settings`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `Guid` | Eindeutige Kennung (Singleton). |
| `RetentionDays` | `int` | Aufbewahrungsdauer in Tagen (Standard `30`, zulässig 1–365). |
| `AutoMarkReadMode` | `string?` | Modus für automatisches Als-gelesen-markieren (`"off"` deaktiviert die Markierung, alle anderen Werte — Standard `"on_scroll"` — aktivieren sie). |
| `AutoMarkReadDelaySeconds` | `int` | Verzögerung in Sekunden (Standard `5`; UI-Auswahl 0/1/3/5). |
| `NotificationsEnabled` | `bool` | Gibt an, ob Benachrichtigungen aktiv sind (Standard `true`; nur unter iOS wirksam). |
| `QuietHoursStart` | `TimeSpan?` | Beginn der Ruhezeit (Standard `null` = keine Ruhezeit). |
| `QuietHoursEnd` | `TimeSpan?` | Ende der Ruhezeit (Standard `null`). |
| `AutoRefreshEnabled` | `bool` | Gibt an, ob die automatische Hintergrund-Aktualisierung aktiv ist (Standard `true`). |
| `RefreshIntervalMinutes` | `int` | Abruf-Intervall in Minuten (Standard `30`; UI-Auswahl 15/30/60/240). |
| `Theme` | `string?` | Erscheinungsbild (`"system"`/`"light"`/`"dark"`, Standard `"system"`). |
| `Language` | `string?` | Sprachauswahl (`"system"`/`"de"`/`"en"`, Standard `"system"`). |
| `NotificationSummaryEnabled` | `bool` | Benachrichtigungsmodus: `false` = eine Benachrichtigung pro Artikel (Standard), `true` = Sammel-Benachrichtigung pro Feed. |
| `RefreshOnStartupEnabled` | `bool` | Gibt an, ob die Feeds beim Start der App einmalig abgerufen werden (Standard `true`). |
| `UnreadSortOrder` | `string?` | Sortierrichtung der Ungelesen-Liste (`"desc"` = neueste zuerst, Standard; `"asc"` = älteste zuerst). |
| `DebugCollectionEnabled` | `bool` | Opt-in-Schalter für die Sammlung von Debuginformationen / das Session-Debug-Log (Standard `false`). |

### `SyncLog`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `Guid` | Eindeutige Kennung des Sync-Logs. |
| `FeedId` | `Guid?` | Optionale Feed-Kennung. |
| `StartedAt` | `DateTime?` | Startzeitpunkt der Synchronisation. |
| `FinishedAt` | `DateTime?` | Endzeitpunkt der Synchronisation. |
| `Status` | `string?` | Status der Synchronisation. |
| `Message` | `string?` | Nachricht oder Fehlerdetails. |

### `DebugLogEntry`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `Guid` | Eindeutige Kennung des Eintrags. |
| `Timestamp` | `DateTime` | UTC-Zeitpunkt des Eintrags (indiziert). |
| `Level` | `string?` | Schweregrad (`DebugLogLevel`: `Info`/`Warning`/`Error`). |
| `Category` | `string?` | Kategorie (`DebugLogCategory`: `Lifecycle`/`Sync`/`Exception`/`Settings`/`Report`). |
| `Message` | `string?` | Logmeldung. |
| `Details` | `string?` | Optionale Details, z. B. `exception.ToString()`. |

## Beziehungen

- Ein `Feed` gehört optional zu einer `Category` (`CategoryId`).
- Ein `Item` gehört immer zu einem `Feed` (`FeedId`).
- Ein `SyncLog` gehört optional zu einem `Feed` (`FeedId`).
- Ein `Keyword` gehört optional zu einem `Feed` (`FeedId`, `null` = globales Schlagwort); beim Löschen eines Feeds werden seine feed-spezifischen Schlagworte per `ON DELETE CASCADE` mitentfernt — globale Schlagworte sind nicht betroffen.
- `DebugLogEntry` hat keine Beziehungen — die Einträge sind sitzungsbezogen und werden beim App-Start bis auf `Error`-Einträge zurückgesetzt.

## Datenzugriff

Die App verwendet eine saubere Schichtung:

- `Reporter.Core.Models` enthält die Domänenmodelle.
- `Reporter.Core.Interfaces` definiert Repository-Schnittstellen für alle Entitäten.
- `Reporter.Data.Repositories` implementiert die Schnittstellen mit Entity Framework Core und SQLite.
- Die Ablage ist auf zwei SQLite-Dateien aufgeteilt: `reporter.db` (Nutzerdaten: `feeds`, `categories`, `keywords`, `settings`, `sync_logs`, `debug_log_entries` sowie `items` ohne `content_html`) und `reporter-content.db` (re-downloadbare Artikelinhalte und Artikelbilder, Tabelle `item_contents` mit den Spalten `content_html`, `image_data`, `image_content_type`, `image_url` — per Migration `AddItemContentImageColumns` ergänzt). `ItemRepository`/`FeedRepository` arbeiten zweistufig auf beiden Speichern: Artikelinhalt und Bild laufen über `IItemContentStore`/`ItemContentRepository` (`IDbContextFactory<ContentDbContext>`) — Lesepfade hydratisieren `ContentHtml` und `Image` per Batch-Lookup, Schreib-/Löschpfade (inkl. der Feed-Kaskade, die die `items`-Zeilen entfernt) spiegeln die Änderungen in den Content-Speicher; verwaiste `item_contents`-Zeilen räumt der Retention-Cleanup beim Start weg (siehe [Aufbewahrung](aufbewahrung.md)). Die Migration `DropItemContentHtml` hat die Legacy-Spalte `items.content_html` entfernt; ihre Werte wurden vorab per `IContentMigrationService` in den Content-Speicher kopiert. Fehlende Inhalte und Bilder (z. B. nach einem Restore ohne Content-Datei oder nach einem fehlgeschlagenen Bild-Download) lädt der Sync beim nächsten Abruf nach — der `FeedSyncService` löst die Bild-URL über `IItemImageService` (Enclosure → MediaRSS → iTunes → erstes `<img src>`) auf, lädt das Bild mit 5-MB-Limit herunter und backfillt fehlende Inhalte/Bilder pro Item in einem gemeinsamen Content-Eintrag.
- `Reporter.Data.Repositories` injiziert `IDbContextFactory<ReporterDbContext>` (bzw. `IDbContextFactory<ContentDbContext>` im `ItemContentRepository`), um pro Operation einen neuen `DbContext` zu erzeugen.
- `Settings` wird als Singleton verwaltet; es existiert immer genau ein Datensatz.
- `IItemRepository` bietet zusätzliche Queries für ungelesene Artikel, Artikel pro Feed/Kategorie und gespeicherte Artikel (paged); die Dublettenerkennung pro Feed läuft im Sync über ein In-Memory-`HashSet` auf `GuidOrHash` mit Batch-Insert via `AddRangeAsync`.
- `IItemRepository.DeleteExpiredAsync` entfernt abgelaufene Artikel für die automatische Aufbewahrungsfrist (`IsRead && !IsSavedForLater && (ReadAt ?? PublishedAt) < cutoff`); `GetExpiredKeywordCandidatesAsync` liefert die Kandidaten der Keyword-Löschregel (`IsRead && !IsSavedForLater && (PublishedAt ?? ReadAt) < cutoff`), `DeleteRangeAsync` löscht Treffer per IDs; Details siehe [Aufbewahrung und automatisches Aufräumen](aufbewahrung.md).
- Die `settings`-Spalten `auto_refresh_enabled`, `refresh_interval_minutes` und `theme` wurden per Migration `AddSettingsAutoRefreshAndTheme` ergänzt; die Spalte `language` (Standard `"system"`, inkl. `UpdateData` des Singletons) per Migration `AddSettingsLanguage`.
- Für die lokalen Benachrichtigungen wurden per Migration `AddFeedNotificationsEnabled` die Spalte `feeds.notifications_enabled` (Default `true`) und per `AddSettingsNotificationSummary` die Spalte `settings.notification_summary_enabled` (Default `false`, inkl. `UpdateData` des Singletons) ergänzt — Details siehe [Benachrichtigungen](../benachrichtigungen/index.md).
- Für die Feed-Symbole wurde per Migration `AddFeedFaviconUrl` die Spalte `feeds.favicon_url` ergänzt; für Start-Abruf und Ungelesen-Sortierung per `AddSettingsStartupRefreshAndSortOrder` die Spalten `settings.refresh_on_startup_enabled` (Default `true`, inkl. `UpdateData` des Singletons) und `settings.unread_sort_order` (Default `"desc"`, inkl. `UpdateData` des Singletons).
- Für die Diagnose-Funktion wurden per Migration `AddSettingsDebugCollection` die Spalte `settings.debug_collection_enabled` (Default `false`) und per `AddDebugLogEntries` die Tabelle `debug_log_entries` (`id`, `timestamp` mit Index `IX_debug_log_entries_timestamp`, `level` max. 20, `category` max. 50, `message`, `details`) ergänzt — Details zum Session-Log siehe [Einstellungen](../einstellungen/index.md).
- Für die feed-spezifischen Schlagworte hat die Migration `AddKeywordFeedId` die Spalte `keywords.feed_id` (nullable, FK `feeds.id`, `ON DELETE CASCADE`) ergänzt und die Eindeutigkeit neu gebaut: Der bisherige Unique-Index auf `keyword_text` wurde durch einen Composite-Unique-Index `(feed_id, keyword_text)` plus einem gefilterten Unique-Index `keyword_text WHERE feed_id IS NULL` ersetzt — SQLite wertet `NULL`-FK-Werte im Composite-Index als verschieden, der Partial Index erhält daher die globale Eindeutigkeit. Dasselbe Schlagwort darf damit global und in mehreren Feeds gleichzeitig existieren; bestehende Zeilen bleiben global (`feed_id = NULL`, kein Backfill nötig). Gepflegt werden globale Schlagworte in den Einstellungen und Feed-Schlagworte im Formular **Feed bearbeiten** der Feeddetailansicht (Details siehe [Einstellungen](../einstellungen/index.md) und [Feeddetailansicht](feeddetailansicht.md)).
- Für die Fehlerdetails-Anzeige hat die Migration `AddFeedLastError` die Spalten `feeds.last_error_kind` (max. 50 Zeichen, nullable — ein `FeedSyncErrorKind`-Wert wie `InsecureHttpBlocked`, `HttpStatus`, `Network`, `Parse` oder `Unknown`) und `feeds.last_error_message` (nullable — die technische Rohmeldung, identisch zum `SyncLog.Message`-Text des Fehlschlags) ergänzt. Beide werden bei jedem fehlgeschlagenen Abruf gesetzt und beim nächsten erfolgreichen Abruf auf `null` zurückgesetzt; `SyncLog` bleibt der Verlauf, die Feed-Spalten den aktuellen Stand — Details siehe [Feeds synchronisieren](synchronisation.md).
- Beim allerersten Start — erkannt daran, dass die Datenbankdatei vor der Migration noch nicht existiert — legt `DemoContentService` einmalig die Kategorie „News" und den Feed `https://www.apple.com/newsroom/rss-feed.rss` (Titel „Apple Newsroom", `NotificationsEnabled = false`) über die regulären Repositories an. Das Seeden entfällt bei Bestandsdatenbanken, bei bereits vorhandenem Demo-Feed (`feeds.url`-Abgleich) sowie bei gesetzter Umgebungsvariable `REPORTER_DISABLE_DEMO_SEED` (E2E-/CI-Läufe); eine vorhandene Kategorie „News" wird wiederverwendet statt dupliziert.
