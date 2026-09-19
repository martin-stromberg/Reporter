<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Anforderung: Artikelbilder lokal speichern für Offline-Verfügbarkeit — Issue #111

## Fachliche Zusammenfassung

Der `FeedSyncService` lädt beim Synchronisieren eines Feeds zusätzlich zum `ContentHtml` das Artikelbild herunter — bestimmt aus dem dedizierten Bild-Feld des `SyndicationItem` bzw. dem ersten `<img src>` des Beitragsinhalts — und legt es als Binärdaten im Content-Store (`IItemContentStore`, `reporter-content.db`) ab. `ArticleCardView` (Listen-Thumbnail) und `ArticleDetailPage` (Artikeldetailansicht) zeigen das lokal gespeicherte Bild auch ohne Internetverbindung; fällt der Download aus, bleibt die bisherige Remote-URL als Fallback und der Feed-Abruf wird nicht blockiert. Speicherung, Backup-Ausschluss, Retention-Cleanup und Löschung folgen denselben Regeln wie die bereits im Content-Store liegenden Artikelinhalte.

## Betroffene Klassen und Komponenten

### Datenmodellklassen

- `Reporter.Data.Entities.ItemContent` (`src/Reporter.Data/Entities/ItemContent.cs`) — voraussichtlich Erweiterung um Bild-Spalten (`byte[]` BLOB plus MIME-Typ/`ContentType`, ggf. Origin-URL) **oder** neue Entity `ItemImage` im `ContentDbContext`. Beide Varianten bleiben in `reporter-content.db` und sind damit automatisch vom iCloud-Backup ausgeschlossen (`BackupExclusionPlan`).
- `Reporter.Data.ContentDbContext` (`src/Reporter.Data/ContentDbContext.cs`) — Mapping der neuen Spalten/Tabelle.
- Neue EF-Migration für `ContentDbContext` unter `src/Reporter.Data/Migrations/Content/`.
- `Reporter.Core.Models.ItemContentEntry` (`src/Reporter.Core/Models/ItemContentEntry.cs`) — Erweiterung um Bild-Felder oder neuer Transport-Record (z. B. `ItemImageEntry`) für Batch-Schreibvorgänge.
- `Reporter.Core.Models.ItemListItem` (`src/Reporter.Core/Models/ItemListItem.cs`) — neues Feld für das lokal gespeicherte Bild (z. B. `byte[]`/`ImageSource`); `ImageUrl` bleibt als Remote-Fallback erhalten; `CopyWith` führt das neue Feld mit.
- `Reporter.Core.Models.Item` — ggf. Erweiterung um die Bilddaten, damit `ArticleDetailViewModel` darüber ans lokale Bild kommt (Hydratisierung über `IItemRepository.GetByIdAsync`).

### Logikklassen / Services

- `FeedSyncService` (`src/Reporter.Core/Services/FeedSyncService.cs`) — in `RunSyncAsync`/`CollectNewItems` die Bild-URL je neuem `Item` ermitteln, Download über den injizierten `HttpClient` ausführen und Ergebnis in den Content-Store schreiben; strikt fehlerisoliert (Muster wie `TryFindFaviconUrlAsync`).
- Neuer Service für Bild-Download/Größenbegrenzung (Annahme: eigenständige, testbare Klasse analog `FeedIconService`, z. B. `ItemImageService` mit `IFeedIconService`-ähnlicher Fehlerisolation).
- `ItemRepository` (`src/Reporter.Data/Repositories/ItemRepository.cs`) — `ExtractImageUrl` bleibt Herkunft der Remote-Fallback-URL; `MapToListItem`/`MapToModel` hydratisieren zusätzlich das lokale Bild; die bestehenden Löschpfade `DeleteAsync`/`DeleteExpiredAsync`/`DeleteRangeAsync` müssen das Bild mit entfernen (automatisch abgedeckt, falls das Bild in der `item_contents`-Zeile liegt — bei separater Tabelle explizit erweitern).
- `ItemContentRepository` (`src/Reporter.Data/Repositories/ItemContentRepository.cs`) — Lese-/Schreib-/Löschpfade um Bilddaten erweitern.
- `FeedRepository.DeleteAsync` — Feed-Kaskade muss gespeicherte Bilder der Feed-Artikel mit löschen (analog zur bestehenden Content-Mitlöschung).
- `RetentionCleanupService` (`src/Reporter.Core/Services/RetentionCleanupService.cs`) — Waisen-Sweep `RemoveOrphanedContentAsync` muss verwaiste Bilder mit berücksichtigen (automatisch, wenn Bilddaten in `item_contents` liegen).
- `ArticleHtmlSanitizer` (`src/Reporter.Core/Services/ArticleHtmlSanitizer.cs`) — das Offline-Entfernen sämtlicher `<img>`-Tags ist zu überdenken: das lokal gespeicherte Artikelbild soll offline sichtbar bleiben (z. B. `src` des ersten `<img>` durch `data:`-URI ersetzen oder als separates Header-Bild einblenden; die WebView-CSP erlaubt bereits `img-src * data: blob:`).

### Interfaces

- `IItemContentStore` (`src/Reporter.Core/Interfaces/IItemContentStore.cs`) — Erweiterung um Bild-Zugriff (z. B. `GetImageAsync`/`SetImageAsync` bzw. Bild-Felder in `ItemContentEntry`) **oder** neues Schwester-Interface (z. B. `IItemImageStore`) auf derselben Content-Datenbank.
- Ggf. neues Interface für den Bild-Download-Service (Annahme).

### Enums

- Keine neuen Enums ableitbar.

### UI-Komponenten

- `ArticleCardView` (`src/Reporter/Views/ArticleCardView.xaml`) — Thumbnail-Kaskade `ImageUrl` → `FeedFaviconUrl` → `FeedInitial`-Kreis um das lokale Bild ergänzen (lokales Bild vor Remote-URL priorisieren); das `IsOnline`-`BindableProperty`/`DataTrigger` blendet den Thumbnail aktuell offline vollständig aus — mit lokalem Bild muss die Ausblendung nur noch greifen, wenn kein lokales Bild vorhanden ist.
- `ArticleDetailViewModel` (`src/Reporter/ViewModels/ArticleDetailViewModel.cs`, `RebuildHtml`) — lokales Bild in das WebView-HTML einbetten (siehe `ArticleHtmlSanitizer`-Punkt oben).
- `docs/help/anwendung/offline.md` — Offline-Verhalten der Bilder neu dokumentieren.

### Tests

- `FakeItemContentStore` (`src/Reporter.Tests/FakeItemContentStore.cs`) — um Bild-Zugriff erweitern.
- `FeedSyncServiceTests` — Bild-Download bei neuen Items, Fehlerisolation (Download-Fehlschlag → Sync bleibt erfolgreich, Remote-URL-Fallback), Größenbegrenzung.
- `ItemContentRepositoryTests` — Roundtrip/Überschreiben/Löschen der Bilddaten.
- `ItemRepositoryTests` — Hydratisierung des lokalen Bilds in `ItemListItem`, Mitlöschung der Bilddaten in allen Löschpfaden.
- `RetentionCleanupServiceTests` — Waisen-Sweep entfernt verwaiste Bilder.
- `ArticleHtmlSanitizerTests` bzw. ViewModel-Tests — Offline-Darstellung des lokalen Bilds.
- `ServiceCollectionTests` — neue Registrierungen auflösbar.
- `FakeHttpMessageHandler` — für Download-Tests wiederverwendbar (bereits in `FeedIconServiceTests` im Einsatz).

## Implementierungsansatz

- **Erweiterungspunkt Sync:** `FeedSyncService.RunSyncAsync` bzw. `CollectNewItems` ist der zentrale Hook — dort entsteht je `SyndicationItem` bereits `ContentHtml` (`GetContentHtml`) und die `contentBackfill`-Liste. Die Bild-URL wird je neuem Item aus dem dedizierten Bild-Feld des Feed-Eintrags (Annahme: Enclosure/`SyndicationLink` mit `RelationshipType "enclosure"` und `image/*`-MIME-Typ bzw. MediaRSS-`ElementExtensions`) oder aus dem ersten `<img src>` des Contents ermittelt; relative URLs sind gegen die Item- bzw. Feed-Basis-URL aufzulösen. Der Download erfolgt über den bereits injizierten `HttpClient`, mit Größenbegrenzung (Content-Length-Prüfung und/oder Streaming-Cap) und pro Bild isoliertem `try/catch`, damit ein Fehlschlag weder den einzelnen Artikel noch den Gesamt-Sync gefährdet — Remote-URL bleibt Fallback.
- **Speicherung:** Die Bilddaten gehören laut Anforderung in die Content-Datenbank — naheliegend als neue Spalten der `item_contents`-Tabelle (`ItemContent`) oder als eigene Tabelle im `ContentDbContext`. Liegen sie in `item_contents`, greifen Upsert-Semantik (`SetRangeAsync`), Löschpfade und der Waisen-Sweep ohne Zusatzlogik; der Backup-Ausschluss (`BackupExclusionPlan`, `reporter-content.db` nicht im iCloud-Backup) gilt dann automatisch. Eine Dateisystem-Ablage wäre die Alternative, müsste aber das Backup-Ausschluss- und Aufräumkonzept separat abbilden.
- **Löschung/Retention:** „Mit dem Löschen eines Artikels wird auch das Bild gelöscht" — abgedeckt über die bestehenden Spiegelpfade `ItemRepository.DeleteAsync`/`DeleteExpiredAsync`/`DeleteRangeAsync`, `FeedRepository.DeleteAsync` (Kaskade) und `RetentionCleanupService.RemoveOrphanedContentAsync`; bei separater Tabelle/Dateiablage sind alle Pfade explizit zu erweitern.
- **Anzeige:** `ItemRepository.MapToListItem` liefert künftig lokales Bild + Remote-`ImageUrl`; `ArticleCardView` priorisiert das lokale Bild und bleibt damit offline sichtbar. In der Detailansicht bindet `ArticleDetailViewModel.RebuildHtml` das lokale Bild offline ein — aktuell entfernt `ArticleHtmlSanitizer.Sanitize(…, forOffline: true)` alle `<img>`-Tags.
- **Backfill:** Für bestehende Items ohne gespeicherten Inhalt existiert der `contentBackfill`-Mechanismus; ob fehlende Bilder bei Bestandsartikeln analog nachgeladen werden, ist zu klären (siehe Offene Fragen).
- **Abhängigkeiten:** `HttpClient` (bereits im `FeedSyncService`), `IItemContentStore`, `IItemRepository`, `INetworkStatusService` (Download nur online nötig — Sync läuft ohnehin nur online), `IDebugLogService` (optionale Protokollierung von Bild-Download-Fehlern, Muster wie beim Notification-Pfad).

## Konfiguration

Aus der Anforderung ist kein benutzerseitiger Konfigurationsbedarf ableitbar. Die Download-Größenbegrenzung wird als sinnvoll bezeichnet — naheliegend als interne Konstante (konkreter Wert offen, siehe Offene Fragen). Annahme: kein neuer Eintrag in `Settings`.

## Offene Fragen

1. **Welche dedizierten Bild-Felder zählen?** RSS-`<enclosure>` mit `image/*`-Typ, Atom-`link rel="enclosure"`, MediaRSS (`media:thumbnail`/`media:content`), iTunes-`<itunes:image>` — oder nur `<enclosure>` + erstes `<img src>`? Annahme bis zur Klärung: Enclosure mit Bild-MIME-Typ hat Vorrang, sonst erstes `<img src>` des `ContentHtml`.
2. **Konkrete Größenbegrenzung:** Welcher Maximalwert (z. B. 2/5/10 MB) und welches Timeout gelten für den Bild-Download? Verhalten bei Überschreitung: Download abbrechen und Remote-URL-Fallback nutzen?
3. **Umfang der Offline-Bilder:** Die Anforderung spricht von „dem Artikelbild" (erstes `<img>` bzw. dediziertes Feld). Sollen wirklich nur dieses eine Bild lokal gespeichert werden — andere Inline-Bilder im `ContentHtml` bleiben dann remote und werden offline weiterhin entfernt? Annahme: ja, nur ein Bild pro Artikel.
4. **Backfill für Bestandsartikel:** Sollen bereits gespeicherte Artikel (inkl. der `contentBackfill`-Kandidaten nach Geräte-Wiederherstellung) ihr Bild beim nächsten Sync nachgeladen bekommen, oder gilt das Feature nur für neu eintreffende Artikel?
5. **Einbettung in der Detailansicht:** Soll das lokale Bild den `src` des ersten `<img>` im bestehenden `ContentHtml` ersetzen (z. B. `data:`-URI) oder als separates Header-Bild oberhalb des Inhalts erscheinen? Interne Design-Frage, beeinflusst `ArticleDetailViewModel`/`ArticleHtmlSanitizer`.
6. **Abgelaufene Remote-URLs:** Wenn der Download fehlschlägt und die Remote-URL als Fallback dient — soll ein erneuter Download-Versuch beim nächsten Sync erfolgen, oder bleibt es beim einmaligen Versuch?
