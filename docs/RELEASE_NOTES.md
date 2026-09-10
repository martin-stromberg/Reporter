# Release Notes

## 0.0.3

### RSS/Atom-Synchronisation, Artikelabruf und Feed-Health

- `IFeedSyncService` / `FeedSyncService` implementiert Abruf, Parsing und Speicherung neuer Artikel aus RSS-/Atom-Feeds.
- Artikel werden anhand ihrer GUID oder eines SHA256-Hashs (Titel + Link + `PublishedAt`) pro Feed dedupliziert.
- Feed-Health wird bei jedem Abruf aktualisiert: `OK`, `Warning` oder `Error`.
- Sync-Log-Eintrag wird für jeden Abruf mit Startzeit, Endzeit, Status und Nachricht geschrieben.
- Fehler (nicht erreichbar, ungültiges XML) löschen bestehende Artikel nicht.
- `IItemRepository.GetByGuidOrHashAsync` ermöglicht effiziente Dublettenprüfung.
- `FeedsViewModel`/`FeedsPage` bieten Buttons für "Aktualisieren" und "Alle aktualisieren".
- Lokalisierungen für Refresh-Texte in `AppResources.resx` / `AppResources.de.resx` hinzugefügt.
- Integrationstests in `Reporter.Tests/FeedSyncServiceTests.cs` für Happy Path, Duplikate, Fehlerfälle, Warnungen und Sync-All hinzugefügt.

## 0.0.2

### Kategorieverwaltung

- Neuer Tab **Kategorien** mit MAUI-Seite und ViewModel.
- Kategorien können erstellt, umbenannt und gelöscht werden.
- Validierung: leerer Name und Duplikate werden abgelehnt.
- `ICategoryRepository.GetAllWithFeedCountAsync()` liefert Kategorie plus Anzahl zugeordneter Feeds.
- Beim Löschen einer Kategorie setzt EF Core `DeleteBehavior.SetNull` die `category_id` zugeordneter Feeds auf `NULL`.
- Unit- und Integrationstests für Repository und ViewModel in `Reporter.Tests` hinzugefügt.

## 0.0.1

Erste Release-Version mit vollständigem CI/CD-Grundgerüst.

### CI/CD-Grundgerüst

- GitHub Actions-Workflows für PR-Qualitätsgates nach `staging` eingerichtet.
- `verify-pr-source.yml` erlaubt PRs nach `main` nur aus `staging`.
- `pr-staging-ci.yml` führt `static checks` (Format, Security-Scan, statische Analyse) und `build & test` (Coverage ≥ 70 %) parallel aus.
- `security-scan.yml` führt einen wöchentlichen Vulnerability-Scan aus.
- Wiederverwendbare Composite Action `.github/actions/security-scan` für den Sicherheits-Scan.

### Repository-Schicht und Domänenmodelle

- Domänenmodelle `Feed`, `Category`, `Item`, `Keyword`, `Settings` und `SyncLog` in `Reporter.Core.Models` hinzugefügt.
- Repository-Schnittstellen `IFeedRepository`, `ICategoryRepository`, `IItemRepository`, `IKeywordRepository`, `ISettingsRepository` und `ISyncLogRepository` in `Reporter.Core.Interfaces` definiert.
- Repository-Implementierungen in `Reporter.Data.Repositories` mit `async`/`await` und Entity Framework Core umgesetzt.
- Repositories verwenden `IDbContextFactory<ReporterDbContext>` für kurzlebige, thread-sichere DbContext-Instanzen.
- `IItemRepository` bietet Filter-/Sortier-Queries für ungelesene Artikel, Artikel pro Feed/Kategorie und gespeicherte Artikel.
- `ISettingsRepository` stellt immer genau einen Datensatz bereit (Singleton-Muster).
- Alle Repositories sind in `MauiProgram` per Dependency Injection registriert und in den ViewModels verfügbar.
- Unit-Tests für alle Repositories in `Reporter.Tests` hinzugefügt.
