# Übersetzte Anforderung

## Ziel
Aufbau einer persistenten lokalen Speicherung für Feeds, Kategorien, Artikel, Keywords, Einstellungen und Sync-Log gemäß Anforderungskatalog.

## Scope
- Hinzufügen von `Microsoft.EntityFrameworkCore.Sqlite` in `Reporter.Data`.
- Erstellen der Entitäten und eines `ReporterDbContext` für:
  - `Feed` (id, url, title, category_id, last_checked_at, health_status, health_last_change)
  - `Category` (id, name)
  - `Item` (id, feed_id, title, link, published_at, guid_or_hash, is_read, is_saved_for_later, read_at, content_html)
  - `Keyword` (id, keyword_text)
  - `Settings` (Singleton: retention_days, auto_mark_read_mode, auto_mark_read_delay_seconds, notifications_enabled, quiet_hours_start, quiet_hours_end)
  - `SyncLog` (id, feed_id, started_at, finished_at, status, message)
- Erstellen der Initial-Migration und Datenbank-Initialisierung beim App-Start.
- Konfiguration der Beziehungen:
  - `Feed -> Category` optional
  - `Item -> Feed` (erforderlich)
  - `SyncLog -> Feed` optional
- Unterstützung für In-Memory-Tests mit SQLite.

## Akzeptanzkriterien
1. `dotnet ef migrations add InitialCreate` und `dotnet ef database update` (bzw. App-Initialisierung) erstellen alle Tabellen korrekt.
2. Tabellen und Spalten entsprechen exakt dem Anforderungskatalog.
3. `ReporterDbContext` lässt sich in Unit-Tests mit einer Test-SQLite-Datenbank instanziieren.
4. Migrations sind versionskontrollfähig.

## Lieferzustand
Datenbankschema ist erstellt und initialisierbar, noch ohne UI.
