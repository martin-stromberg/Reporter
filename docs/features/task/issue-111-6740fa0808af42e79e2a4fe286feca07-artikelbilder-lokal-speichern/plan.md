<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Artikelbilder lokal speichern für Offline-Verfügbarkeit — Issue #111

## Übersicht

Der `FeedSyncService` lädt beim Synchronisieren zusätzlich zum `ContentHtml` das Artikelbild herunter (dediziertes Bild-Feld des `SyndicationItem` bzw. erstes `<img src>` des Inhalts) und speichert es als neue Spalten in der `item_contents`-Tabelle der `reporter-content.db`. `ArticleCardView` und `ArticleDetailPage` zeigen das lokale Bild auch offline; bei Download-Fehlschlag bleibt die Remote-`ImageUrl` als Fallback, ohne den Sync zu blockieren. Löschung, Backup-Ausschluss und Waisen-Sweep greifen durch die Ablage in `item_contents` ohne Zusatzlogik.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Ablageort der Bilddaten | Neue Spalten `image_data` (BLOB), `image_content_type`, `image_url` in der bestehenden `item_contents`-Tabelle (`ItemContent`-Entity) — keine separate `ItemImage`-Tabelle, keine Dateisystem-Ablage | Eine Zeile je Artikel fasst alle re-downloadbaren Nutzdaten zusammen: Upsert- (`SetRangeAsync`), Einzel-/Batch-Lösch-, Feed-Kaskaden- und Waisen-Sweep-Pfade sowie der iCloud-Backup-Ausschluss (`BackupExclusionPlan`) gelten ohne Zusatzlogik. Eine Schwester-Tabelle müsste alle fünf Pfade explizit erweitern; eine Dateiablage müsste Backup-Ausschluss und Aufräumen separat abbilden. |
| Store-Contract | `IItemContentStore` um Bild-Methoden erweitern — kein Schwester-Interface `IItemImageStore` | Bild und Content bilden fachlich einen Datensatz (Repository-Muster bleibt eine Zugriffskomponente auf `reporter-content.db`); ein zweiter Store auf dieselbe Tabelle würde die Upsert-/Lösch-Semantik duplizieren. |
| Bild-Transport zwischen Schichten | Neuer Record `ItemImage` (`Data` `byte[]`, `ContentType` `string?`, `Url` `string?`) als Value Object; `ItemContentEntry` erhält optionalen Parameter `Image` (Default `null`) | `ItemImage` kapselt die drei gespeicherten Werte als eine Einheit über Store, Repository und Modelle hinweg; der optionale Eintragsparameter hält bestehende `ItemContentEntry`-Aufrufstellen (`ItemContentMigrationService`, `contentBackfill`) kompilierfähig. |
| `Item.Image`-Mutabilität | `set`-Accessor — einzige nicht-`init`-Eigenschaft von `Item` | `Item` ist eine `class` ohne `with`-Kopie (`Item.cs:13-58`); `CollectNewItems` konstruiert die `Item`-Entities (`FeedSyncService.cs:284-295`) vor der Download-Schleife in `RunSyncAsync`, das Bild wird erst danach zugewiesen — mit `init` nicht kompilierbar. `set` ist die geringste Abweichung: Die Alternative, die Item-Konstruktion hinter den Download zu verlagern, würde `CollectNewItems`/`RunSyncAsync` umbauen, und ein Bildtransport nur über `ItemContentEntry` würde `Item.Image` auf den Lesepfad beschränken und den Schreibpfad neuer Items auf zwei Store-Aufrufe verteilen. |
| Upsert-/Lösch-Semantik des Stores | Feldweiser Upsert in `SetAsync`/`SetRangeAsync`: `Image != null` schreibt die Bild-Spalten, `Image == null` lässt gespeicherte Bilddaten unverändert; `ContentHtml` nicht leer schreibt `content_html`; `ContentHtml` `null`/leer lässt den gespeicherten Inhalt **unverändert, sobald der Eintrag ein Bild trägt** (`Image != null`), und entfernt ihn nur bei `Image == null`; die Zeile wird gelöscht, wenn sie nach der Operation weder Inhalt noch Bild enthielte | Bild-Backfill-Einträge `ItemContentEntry(id, null, image)` ergänzen das Bild, ohne gespeicherten `content_html` zu löschen — der Hauptfall des Backfills sind Bestandsitems mit Inhalt und ohne Bild; für alle bisherigen Aufrufe ohne `Image` bleibt „`null` entfernt den Eintrag" exakt wie heute (Bestandstests unverändert), und `UpdateAsync` (`SetAsync` ohne Bild-Parameter) kann ein gespeichertes Bild nie entfernen. |
| Download-Komponente | Neuer Service `ItemImageService` hinter `IItemImageService` — streng fehlerisoliertes `Try…`-Muster analog `FeedIconService`/`IFeedIconService`, nutzt den bestehenden `HttpClient`-Singleton (30 s) | Bewährtes Muster für isolierte Neben-Downloads im Sync; `TryDownloadImageAsync` schluckt ausschließlich Download-Fehler (HTTP-Fehlerstatus, Netzwerk-/Timeout-Fehler, ungültige Antworten) zu `null`, sodass ein Bild-Fehlschlag weder Artikel noch Sync gefährdet; `OperationCanceledException` propagiert dagegen — bewusste Abweichung vom Referenzmuster `FeedIconService.TryFindFaviconUrlAsync`, das OCE verschluckt (`FeedIconService.cs:95-99`), damit ein Sync-Abbruch nicht als Bild-Fehlschlag kaschiert wird; als eigene injizierbare Komponente isoliert testbar (inkl. `FakeItemImageService`). |
| Bild-URL-Auflösung | `ItemImageService.ResolveImageUrl(feedItem, contentHtml, itemLink, feedUrl)`: Priorität Enclosure/`link rel="enclosure"` mit `image/*`-MIME → MediaRSS-`ElementExtensions` (`media:thumbnail`/`media:content` mit `image/*`) → `itunes:image` → erstes `<img src>` des `ContentHtml`; relative URLs gegen Item-Link, dann Feed-URL auflösen | Vom Anwender bestätigte Priorität — deckt die in der Anforderung genannten Kandidatenquellen ab; die `<img>`-Extraktion teilt sich eine Hilfsmethode mit `ItemRepository.ExtractImageUrl` (keine doppelte Regex). |
| Größenbegrenzung | Interne Konstante `MaxImageBytes = 5 MB` in `ItemImageService`, geprüft über `Content-Length` vorab plus Streaming-Cap beim Lesen; Timeout = vorhandener 30-s-`HttpClient`; Überschreitung → `null` (Remote-Fallback) | Vom Anwender festgelegter Wert; begrenzt Speicher- und Downloadvolumen ohne `Settings`-Eintrag und deckt gängige Feed-Bilder ab. |
| Download-Parallelität | Sequentielle Bild-Downloads in `RunSyncAsync` (ein `await` je Kandidat in der Reihenfolge der Feed-Items) | Ein einzelner Sync ist ohnehin I/O-dominiert und laufzeitunkritisch; Sequenzialität vermeidet Bandbreiten-/Speicherspitzen und hält das Fehlerisolation-Muster des Favicon-Pfads. |
| Transport in die Liste | `ItemListItem.LocalImageData` (`byte[]?`) + berechnetes `HasLocalImage`; `Image.Source` bindet `byte[]` direkt über den eingebauten MAUI-`ImageSourceConverter` — kein neuer `IValueConverter` nötig | `byte[]`-Binding ist Bordmittel; `HasLocalImage` liefert ein boolesches Bindungsziel für die `MultiTrigger`-Bedingungen der Thumbnail-Kaskade. |
| Offline-Ausblendung des Thumbnails | `DataTrigger` auf dem Thumbnail-`Border` wird zum `MultiTrigger`: ausblenden nur bei `IsOnline == False` **und** `HasLocalImage == False`; Kaskade wird `LocalImageData` → `ImageUrl` (remote) → `FeedFaviconUrl` → `FeedInitial` | Lokal gespeichertes Bild bleibt offline sichtbar; remote Stufen werden bei vorhandenem lokalem Bild gar nicht erst sichtbar (kein Netzabruf offline). |
| Einbettung in der Detailansicht | `ArticleHtmlSanitizer.Sanitize` erhält optionalen Parameter `localImage` (`ItemImage?`): bei `forOffline` und vorhandenem `localImage` wird das **erste** `<img>`-Tag durch `<img src="data:{ContentType};base64,…">` ersetzt statt entfernt; enthält der Inhalt kein `<img>` (z. B. Enclosure-Bild) **oder ist der Inhalt leer/`null`** (bild-only-Artikel), wird ein Header-`<img>` vorangestellt; alle übrigen `<img>` werden weiterhin entfernt. Die `localImage`-Behandlung liegt **vor** der `IsNullOrWhiteSpace`-Frührückkehr (`ArticleHtmlSanitizer.cs:39-42`) | Hält die ursprüngliche Bildposition des Artikels bei und deckt Enclosure-Feeds ohne Inline-Bild sowie reine Enclosure-Artikel ohne `ContentHtml` ab; die bestehende WebView-CSP (`img-src * data: blob:`) erlaubt `data:`-URIs bereits. Online-Verhalten unverändert (Remote-`src`); übrige Inline-Bilder bleiben remote und werden offline wie bisher entfernt (vom Anwender bestätigt: genau ein Bild pro Artikel). |
| Hydratisierung des Bilds | Nur die Pfade, die das Bild anzeigen, laden die Bytes: `GetByIdAsync` → `Item.Image` (Detailansicht) und die paged `ItemListItem`-Projektionen (`GetUnreadByDateAsync(page,…)`, `GetSavedForLaterAsync`) → `LocalImageData`; übrige `MapToModel`-Pfade übergeben `null` | Vermeidet unnötige BLOB-Last bei jedem Sync (`GetByFeedAsync` liefert alle Bestandsitems) und bei zählenden Vergleichspfaden; pro Seite fallen nur ~20 Bilder an. |
| Bild-Backfill für Bestandsartikel | `RunSyncAsync` lädt einmal `GetImageIdsAsync` (Item-IDs mit gespeichertem Bild) und reicht die Menge in `CollectNewItems`: Bestandsitems ohne Bild, die der Feed erneut ausliefert, werden Bildkandidaten; Treffer werden pro `ItemId` in `contentBackfill` eingemergt — ein vorhandener Content-Backfill-Eintrag derselben `ItemId` wird zu `ItemContentEntry(id, content, image)` zusammengeführt, andernfalls wird `ItemContentEntry(id, null, image)` angehängt; pro `ItemId` steht höchstens ein Eintrag in der Liste | Spiegelt den bewährten `contentBackfill`-Mechanismus und deckt das Geräte-Wiederherstellungs-Szenario ab (Content-DB fehlt komplett): Die Zusammenführung verhindert, dass ein Item ohne Inhalt **und** ohne Bild zwei Einträge mit derselben `ItemId` erzeugt, von denen die „letzter gewinnt"-Deduplizierung in `SetRangeAsync` einen verwerfen würde. Vom Anwender bestätigt; zugleich der Retry-Mechanismus: fehlgeschlagene Downloads hinterlassen kein gespeichertes Bild und werden so beim nächsten Sync implizit erneut versucht — kein separater Retry-Mechanismus. |
| Fehlerprotokollierung | Fehlgeschlagene Bild-Downloads werden im `ItemImageService` per `Debug.WriteLine` notiert (wie `FeedIconService`); `FeedSyncService` schreibt zusätzlich eine aggregierte `IDebugLogService`-Warnung (Kategorie `DebugLogCategory.Sync`) mit der Anzahl fehlgeschlagener Downloads je Sync | Einzelnes Bild ist kein Fehler des Syncs; die Aggregatzeile vermeidet Log-Spam bei vielen Artikeln und folgt dem Notification-Pfad-Muster (optionaler `IDebugLogService`). |

## Programmabläufe

### Feed-Sync: Bild-Download für neue Artikel

1. `SyncFeedAsync` → `RunSyncAsync` lädt Bestandsitems (`GetByFeedAsync`) und einmal `GetImageIdsAsync` des `IItemContentStore`.
2. `CollectNewItems` iteriert die `SyndicationItem`s wie bisher (Duplikat-`GuidOrHash`-Erkennung, Keyword-Filter, `GetContentHtml`) und ruft je neuem Item zusätzlich `IItemImageService.ResolveImageUrl(feedItem, contentHtml, itemLink, feed.Url)` auf; das Ergebnis geht als Bildkandidat `(ItemId, ImageUrl)` in die Rückgabe mit. Bestandsitems ohne gespeichertes Bild (ID nicht in der `GetImageIdsAsync`-Menge), die der Feed erneut liefert, werden ebenfalls Bildkandidaten (Backfill).
3. `RunSyncAsync` iteriert die Bildkandidaten sequentiell: `TryDownloadImageAsync(url, cancellationToken)` liefert `ItemImage` oder `null` — Download-Fehler werden zu `null` geschluckt (Muster `TryFindFaviconUrlAsync`), `OperationCanceledException` propagiert dagegen.
4. Neue Items erhalten das `ItemImage` per `set`-Accessor als `Item.Image` zugewiesen (die Entities wurden bereits in `CollectNewItems` konstruiert); Bild-Backfill-Treffer für Bestandsitems werden in `contentBackfill` eingemergt: Enthält die Liste bereits einen Content-Backfill-Eintrag mit derselben `ItemId` (Restore-Szenario: Item ohne Inhalt **und** ohne Bild), wird dieser zu `ItemContentEntry(existingId, contentHtml, image)` zusammengeführt; andernfalls wird `ItemContentEntry(existingId, null, image)` angehängt — pro `ItemId` genau ein Eintrag.
5. `_itemRepository.AddRangeAsync(newItemEntities)` persistiert die `items`-Zeilen und schreibt `ItemContentEntry(ItemId, ContentHtml, Image)` via `SetRangeAsync` — die `item_contents`-Zeile enthält damit `content_html` und ggf. `image_data`/`image_content_type`/`image_url`.
6. `contentBackfill` wird wie bisher via `_contentStore.SetRangeAsync` geschrieben; die feldweise Upsert-Regel stellt sicher, dass Einträge mit `ContentHtml == null` und `Image != null` ausschließlich die Bild-Spalten schreiben und gespeicherte `content_html`-Werte unberührt lassen.
7. Fehlgeschlagene Downloads (`null`-Ergebnis) verändern weder `SyncResult` noch `FeedHealth`; am Ende wird bei Bedarf eine aggregierte Warnung über `_debugLogService` (`DebugLogCategory.Sync`, `DebugLogLevel.Warning`) geschrieben.
8. Status, `SyncLog`, Favicon-Backfill und Notification laufen unverändert weiter (`DetermineStatus`, `UpdateFeedHealthAsync`, `UpdateLogAsync`, `NotifyNewItemsAsync`).

Beteiligte Klassen/Komponenten: `FeedSyncService`, `IItemImageService`/`ItemImageService`, `IItemRepository`/`ItemRepository`, `IItemContentStore`, `ItemContentEntry`, `ItemImage`, `HttpClient`, `IDebugLogService`

### Artikelliste: Thumbnail mit lokalem Bild

1. `UnreadViewModel`/`LaterViewModel` laden `ItemListItem`s über `ItemRepository.GetUnreadByDateAsync(page,…)` bzw. `GetSavedForLaterAsync`.
2. `ItemRepository` projiziert `ItemListRow`s (`SelectListItemRows`) und lädt parallel zum Content-Lookup (`GetContentsAsync` → `GetRangeAsync`) den Bild-Lookup `GetImagesAsync(rowIds)`.
3. `MapToListItem(row, contentHtml, image)` setzt `LocalImageData = image?.Data` und weiterhin `ImageUrl = ExtractImageUrl(contentHtml)` als Remote-Fallback.
4. `ArticleCardView` rendert die Kaskade: sichtbares `Image` für `LocalImageData` (höchste Priorität), darunter `ImageUrl`, `FeedFaviconUrl`, `FeedInitial`-Kreis — die hinteren Stufen erhalten zusätzliche `HasLocalImage == False`-Bedingungen.
5. Der `MultiTrigger` am Thumbnail-`Border` blendet den Rahmen bei `IsOnline == False` nur noch aus, wenn `HasLocalImage == False`; mit lokalem Bild bleibt der Thumbnail offline sichtbar.

Beteiligte Klassen/Komponenten: `ItemRepository`, `IItemContentStore`, `ItemListItem`, `ItemImage`, `ArticleCardView`, `UnreadViewModel`, `LaterViewModel`

### Artikeldetailansicht: lokales Bild offline einbetten

1. `ArticleDetailViewModel.LoadAsync` lädt das `Item` via `IItemRepository.GetByIdAsync` — `ItemRepository` hydratisiert dabei zusätzlich `GetImageAsync` nach `Item.Image`.
2. `RebuildHtml` ruft `ArticleHtmlSanitizer.Sanitize(Item?.ContentHtml, forOffline: !IsOnline, localImage: Item?.Image)` auf.
3. Offline mit `localImage`: das erste `<img>` des Inhalts erhält `src="data:{ContentType};base64,{Data}"` (alle weiteren `<img>` werden wie bisher entfernt); ohne `<img>` im Inhalt wird ein Header-`<img>`-Element mit derselben `data:`-URI vorangestellt. Die `localImage`-Behandlung liegt vor der `IsNullOrWhiteSpace`-Frührückkehr in `Sanitize`: Bei leerem/`null`-`ContentHtml` (bild-only-Artikel, z. B. reiner Enclosure-Artikel) liefert `Sanitize` ein Fragment, das nur den Header-`<img>` enthält — `RebuildHtml` fällt damit nicht auf `HtmlSource = string.Empty` zurück, sondern rendert ein Bild-only-Dokument.
4. Online oder ohne `localImage` bleibt das bisherige Verhalten erhalten (online: Remote-`img` unverändert; offline ohne Bild: alle `<img>` entfernt).
5. `OnConnectivityChanged` → `RebuildHtml` baut das HTML bei Netzwechsel neu — beim Wechsel auf offline erscheint das lokale Bild ohne erneutes Laden.
6. `CreateItemCopy` führt `Image` mit, damit `ToggleSavedForLaterAsync`/`ToggleMarkReadAsync`/`MarkReadDelayedAsync` das Bild nicht verlieren.

Beteiligte Klassen/Komponenten: `ArticleDetailViewModel`, `ArticleHtmlSanitizer`, `ItemRepository`, `IItemContentStore`, `Item`, `ItemImage`

### Löschung, Retention und Waisen-Sweep

1. `ItemRepository.DeleteAsync`/`DeleteExpiredAsync`/`DeleteRangeAsync` rufen unverändert `_contentStore.DeleteAsync`/`DeleteRangeAsync` — die `item_contents`-Zeile geht samt Bild-Spalten verloren.
2. `FeedRepository.DeleteAsync` löscht die `item_contents`-Zeilen aller Feed-Items per `DeleteRangeAsync(itemIds)` — unverändert, deckt Bilder mit ab.
3. `RetentionCleanupService.RemoveOrphanedContentAsync` vergleicht `GetItemIdsAsync` (alle `item_contents`-Zeilen, jetzt inkl. bild-only-Zeilen) mit `GetAllIdsAsync` und löscht Waisen per `DeleteRangeAsync` — unverändert.
4. `BackupExclusionPlan`/`App.OnStart`: `reporter-content.db` samt Sidecars bleibt vom iCloud-Backup ausgeschlossen — Bilddaten automatisch abgedeckt, keine Änderung.

Beteiligte Klassen/Komponenten: `ItemRepository`, `FeedRepository`, `RetentionCleanupService`, `IItemContentStore`, `ItemContentRepository`, `BackupExclusionPlan`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `ItemImage` (`src/Reporter.Core/Models/ItemImage.cs`) | Datenmodellklasse (Record, Value Object) | Transportiert Bildnutzdaten über Store/Repository/Modelle: `Data` (`byte[]`), `ContentType` (`string?`), `Url` (`string?`, Origin-URL des Downloads) |
| `IItemImageService` (`src/Reporter.Core/Interfaces/IItemImageService.cs`) | Interface | Contract für Bild-URL-Auflösung (`ResolveImageUrl`) und fehlerisolierten Download (`TryDownloadImageAsync`) |
| `ItemImageService` (`src/Reporter.Core/Services/ItemImageService.cs`) | Klasse (Service, Singleton) | Implementierung: Kandidaten-Priorität (Enclosure → MediaRSS → `itunes:image` → erstes `<img src>`), relative-URL-Auflösung, `image/*`-MIME-Prüfung, `Content-Length`-Vorabprüfung + Streaming-Cap (`MaxImageBytes = 5 MB`), `Try…`-Fehlerisolation; teilt die `<img>`-Extraktion mit `ItemRepository.ExtractImageUrl` |
| `FakeItemImageService` (`src/Reporter.Tests/FakeItemImageService.cs`) | Test-Fake | Konfigurierbares `IItemImageService`-Double mit `NextResult`/Aufrufprotokoll (Muster `FakeFeedIconService`) |

## Änderungen an bestehenden Klassen

### `ItemContent` (Entity, `src/Reporter.Data/Entities/ItemContent.cs`)

- **Neue Eigenschaften:** `ImageData` (`byte[]?`) — Bild-Binärdaten; `ImageContentType` (`string?`) — MIME-Typ für `data:`-URI/Anzeige; `ImageUrl` (`string?`) — Origin-URL des Downloads (Nachvollziehbarkeit, Matching zum `<img src>`)

### `ContentDbContext` (`src/Reporter.Data/ContentDbContext.cs`)

- **Geänderte Methoden:** `ConfigureItemContent` — zusätzliche Spalten-Mappings `image_data`, `image_content_type`, `image_url` (alle nullable)

### `ItemContentEntry` (Record, `src/Reporter.Core/Models/ItemContentEntry.cs`)

- **Neue Eigenschaften:** dritter Positionsparameter `Image` (`ItemImage?`, Default `null`) — optionale Bilddaten je Batch-Eintrag
- **Geänderte Dokumentation:** Der XML-Kommentar des `ContentHtml`-Parameters („`null` to remove a stored entry", `ItemContentEntry.cs:10`) wird an die neue feldweise Upsert-Semantik angepasst — `null` entfernt den Eintrag nur noch bei `Image == null`; bei `Image != null` bleibt gespeicherter Inhalt unberührt.

### `Item` (Domain-Modell, `src/Reporter.Core/Models/Item.cs`)

- **Neue Eigenschaften:** `Image` (`ItemImage?`, `set`) — lokal gespeichertes Bild; wird in `GetByIdAsync` (Detailpfad) hydratisiert und im Sync nach der Download-Schleife zugewiesen. `set` statt `init` ist eine bewusste, dokumentierte Ausnahme von den `init`-Accessoren des Modells — siehe Designentscheidung „`Item.Image`-Mutabilität".

### `ItemListItem` (`src/Reporter.Core/Models/ItemListItem.cs`)

- **Neue Eigenschaften:** `LocalImageData` (`byte[]?`, `init`) — lokale Bilddaten für den Thumbnail; `HasLocalImage` (`bool`, berechnet: `LocalImageData is { Length: > 0 }`) — Bindungsziel für die Kaskaden-`MultiTrigger`
- **Geänderte Methoden:** `CopyWith` — führt `LocalImageData` mit

### `IItemContentStore` (Interface, `src/Reporter.Core/Interfaces/IItemContentStore.cs`)

- **Neue Methoden:** `GetImageAsync(Guid itemId, CancellationToken)` → `Task<ItemImage?>` — Bild eines Items lesen; `GetImagesAsync(IReadOnlyList<Guid> itemIds, CancellationToken)` → `Task<IReadOnlyDictionary<Guid, ItemImage>>` — Bild-Lookup für Listen-Projektionen; `GetImageIdsAsync(CancellationToken)` → `Task<IReadOnlyList<Guid>>` — Item-IDs mit gespeichertem Bild (Bild-Backfill-Kandidaten)
- **Geänderte Methoden:** `SetAsync` erhält optionalen Parameter `ItemImage? image = null` (vor `cancellationToken` eingefügt — kein Aufrufer übergibt das Token positional); `SetRangeAsync` verarbeitet `ItemContentEntry.Image`
- **Geänderte Semantik (Dokumentation im Contract):** Feldweiser Upsert — `Image != null` schreibt die Bild-Spalten, `Image == null` lässt gespeicherte Bilddaten unverändert; `ContentHtml` `null`/leer lässt den gespeicherten Inhalt bei `Image != null` unberührt und entfernt ihn bei `Image == null`; die Zeile wird nur entfernt, wenn sie nach der Operation weder Inhalt noch Bild enthielte

### `ItemContentRepository` (`src/Reporter.Data/Repositories/ItemContentRepository.cs`)

- **Neue Methoden:** `GetImageAsync`, `GetImagesAsync`, `GetImageIdsAsync` — Projektion der neuen Spalten
- **Geänderte Methoden:** `SetAsync`/`SetRangeAsync` — umgesetzte feldweise Upsert-Regel (Bild-Spalten bei `Image != null` schreiben, bei `Image == null` unberührt lassen; `content_html` bei `Image != null` und leerem `ContentHtml` unberührt lassen, sonst schreiben bzw. leeren; Zeile nur entfernen, wenn nach der Operation weder Inhalt noch Bild vorläge). Die „letzter gewinnt"-Deduplizierung pro `ItemId` bleibt als Sicherheitsnetz bestehen — der Sync erzeugt pro `ItemId` ohnehin nur einen zusammengeführten Eintrag.

### `ItemRepository` (`src/Reporter.Data/Repositories/ItemRepository.cs`)

- **Geänderte Methoden:**
  - `GetByIdAsync` — zusätzlich `GetImageAsync`; `MapToModel` mit Bild
  - `MapToModel` — dritter Parameter `ItemImage?` setzt `Item.Image`
  - `AddAsync` — die bestehende Guard `if (!string.IsNullOrEmpty(item.ContentHtml))` (`ItemRepository.cs:67`) wird symmetrisch zu `AddRangeAsync` um `|| item.Image is not null` erweitert; im Guard-Block dann `SetAsync(item.Id, item.ContentHtml, item.Image)` — sonst ginge ein bild-only-`Item` (`ContentHtml == null`, `Image != null`) still verloren, ohne `SetAsync` zu erreichen
  - `AddRangeAsync` — Filter wird `!string.IsNullOrEmpty(i.ContentHtml) || i.Image is not null`; Einträge als `ItemContentEntry(i.Id, i.ContentHtml, i.Image)`
  - `GetUnreadByDateAsync(page,…)`/`GetSavedForLaterAsync` — zusätzlicher `GetImagesAsync`-Lookup; `MapToListItem` mit Bild-Parameter
  - `MapToListItem` — dritter Parameter `ItemImage?` setzt `LocalImageData`
  - `ExtractImageUrl` — delegiert an die gemeinsame `<img>`-Extraktion des `ItemImageService` (keine doppelte Regex); bleibt Herkunft der Remote-Fallback-`ImageUrl`
- **Unverändert:** `UpdateAsync` (`SetAsync` ohne Bild-Parameter: `Image == null` lässt das gespeicherte Bild unberührt, und ein `null`-Content entfernt höchstens den Inhalt — nie eine bild-haltende Zeile) und alle Löschpfade (zeilenbasiert, decken Bilder automatisch ab)

### `FeedSyncService` (`src/Reporter.Core/Services/FeedSyncService.cs`)

- **Geänderte Konstruktor-Abhängigkeiten:** zusätzlich `IItemImageService` (vor `IItemContentStore` einsortiert bzw. nach `IFeedIconService`)
- **Geänderte Methoden:**
  - `RunSyncAsync` — lädt `GetImageIdsAsync`, übergibt die Menge an `CollectNewItems`, führt nach `CollectNewItems` die sequentielle Download-Schleife aus, setzt `Item.Image` auf neuen Entities, mergt Bild-Backfill-Treffer pro `ItemId` in `contentBackfill` (vorhandener Content-Backfill-Eintrag → `ItemContentEntry(id, content, image)`, sonst `ItemContentEntry(id, null, image)`), schreibt bei Bedarf die aggregierte `IDebugLogService`-Warnung
  - `CollectNewItems` — Rückgabe um Bildkandidaten-Liste `(ItemId, ImageUrl)` erweitert; ruft `IItemImageService.ResolveImageUrl` je neuem Item und je passendem Backfill-Bestandsitem (Item ohne gespeichertes Bild) auf
- **Neue Methoden:** private Hilfsmethode zum Download/Verteilen der Bildkandidaten (Fehlerisolation erfolgt im Service selbst)

### `ArticleHtmlSanitizer` (`src/Reporter.Core/Services/ArticleHtmlSanitizer.cs`)

- **Geänderte Methoden:** `Sanitize(string? html, bool forOffline, ItemImage? localImage = null)` — bei `forOffline` und `localImage != null` wird das erste `<img>`-Tag durch ein `<img>` mit `data:`-URI ersetzt; ohne `<img>` im Inhalt wird ein Header-`<img>` vorangestellt; übrige `<img>`-Tags werden weiterhin entfernt (bestehende `ImageTagRegex`-Logik plus Ersetzungs-/Voranstell-Regel). Die `localImage`-Behandlung liegt **vor** der `IsNullOrWhiteSpace`-Frührückkehr (`ArticleHtmlSanitizer.cs:39-42`): bei `forOffline`, `localImage != null` und leerem/`null`-`html` liefert `Sanitize` ein Fragment mit nur dem Header-`<img>` (bild-only-Artikel); ohne `localImage` bleibt die Frührückkehr unverändert.

### `ArticleDetailViewModel` (`src/Reporter/ViewModels/ArticleDetailViewModel.cs`)

- **Geänderte Methoden:** `RebuildHtml` — übergibt `Item?.Image` als `localImage` an `Sanitize`; kein eigener Sonderfall für leeren Inhalt nötig, da `Sanitize` bei `localImage` auch für leeren/`null`-Content ein nicht-leeres Header-Fragment liefert (die bestehende Whitespace-Frührückkehr greift dann nicht); `CreateItemCopy` — führt `Image` mit

### `ArticleCardView` (XAML, `src/Reporter/Views/ArticleCardView.xaml`)

- **Geänderte Thumbnail-Kaskade:** neue oberste Stufe `Image Source="{Binding LocalImageData}" IsVisible="{Binding HasLocalImage}"`; die Stufen `ImageUrl`, `FeedFaviconUrl` und `FeedInitial`-Kreis erhalten zusätzliche `BindingCondition` `HasLocalImage == False`
- **Geänderter Trigger:** der `DataTrigger` `IsOnline == False` am Thumbnail-`Border` wird zum `MultiTrigger` (`IsOnline == False` **und** `HasLocalImage == False` → `IsVisible = False`)

### `MauiProgram` (`src/Reporter/MauiProgram.cs`)

- **Geänderte DI-Registrierungen:** `.AddSingleton<IItemImageService, ItemImageService>()` (neben `IFeedIconService`/`FeedIconService`)

### `FakeItemContentStore` (`src/Reporter.Tests/FakeItemContentStore.cs`)

- **Neue Member:** Implementierung von `GetImageAsync`/`GetImagesAsync`/`GetImageIdsAsync` und des erweiterten `SetAsync`/`SetRangeAsync` mit identischer Upsert-Semantik wie `ItemContentRepository`

### `FeedRepository` / `RetentionCleanupService` / `ItemContentMigrationService` / `BackupExclusionPlan`

- Keine Änderungen — Lösch-, Sweep- und Backup-Pfade wirken zeilenbasiert und decken die Bild-Spalten automatisch ab; `ItemContentMigrationService` erzeugt `ItemContentEntry`s weiterhin ohne `Image`-Parameter.

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `AddItemContentImageColumns` | `item_contents`: `image_data` (BLOB, nullable), `image_content_type` (TEXT, nullable), `image_url` (TEXT, nullable) | Scaffold via `dotnet ef migrations add AddItemContentImageColumns --project src/Reporter.Data --context ContentDbContext --output-dir Migrations/Content` (Design-Time-Factory `ContentDbContextFactory` vorhanden; `dotnet-ef` 10.0.10 verfügbar); `ContentDbContextModelSnapshot` wird aktualisiert; `ItemContentMigrationService.MigrateLegacyContentAsync` wendet die Migration beim Start via `MigrateAsync` an |

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Bild-Kandidaten-URL | Nur absolute `http`/`https`-URIs; relative URLs werden gegen den Item-Link, dann die Feed-URL aufgelöst | Ungültig/nicht auflösbar → Kandidat verworfen, kein Download |
| Download-Antwort | `Content-Type` muss `image/*` sein | Anderer/fehlender MIME-Typ → `null` (Remote-Fallback) |
| Download-Größe | `Content-Length` > `MaxImageBytes` oder Stream übersteigt das Cap | Download abgebrochen → `null` (Remote-Fallback) |
| Download-Robustheit | HTTP-Fehlerstatus, Netzwerkfehler, Timeout (30 s des `HttpClient`) oder beliebige Exception außer `OperationCanceledException` | `TryDownloadImageAsync` → `null`; Sync und Artikel unberührt |
| Cancellation | `CancellationToken` wird in Auflösung und Download beachtet | Abbruch propagiert `OperationCanceledException` (kein Verschlucken) |

## Konfigurationsänderungen

Keine. `MaxImageBytes` ist eine interne Konstante in `ItemImageService`; es gibt keinen neuen `Settings`-Eintrag und keine `appsettings`-Änderung.

## Seiteneffekte und Risiken

- **`IItemContentStore`-Erweiterung:** Beide Implementierungen (`ItemContentRepository`, `FakeItemContentStore`) und die Contract-Doku (inkl. `ItemContentEntry`-XML-Kommentar) müssen synchron geändert werden; die feldweise Upsert-Regel („`ContentHtml` `null`/leer = unberührt bei `Image != null`") ist eine bewusste Semantik-Präzisierung — für Aufrufe ohne `Image` bleibt das bisherige Verhalten exakt erhalten, und `UpdateAsync` (`SetAsync` ohne Bild-Parameter) kann ein gespeichertes Bild nie entfernen.
- **`Item.Image`-Mutabilität:** `Image` ist die einzige `set`-Eigenschaft des ansonsten `init`-only-Modells — Abweichung ist in der Designentscheidung begründet; Kopierpfade (`ArticleDetailViewModel.CreateItemCopy`) müssen das Feld mitführen.
- **Doppelter Backfill (Restore-Szenario):** Ein Bestandsitem ohne Inhalt **und** ohne Bild würde ohne Zusammenführung zwei `ItemContentEntry`s mit derselben `ItemId` erzeugen; die „letzter gewinnt"-Deduplizierung in `SetRangeAsync` würde einen verwerfen. Der Sync mergt beide zu `ItemContentEntry(id, content, image)`, bevor `SetRangeAsync` aufgerufen wird.
- **`FeedSyncService`-Konstruktor:** Neue Abhängigkeit `IItemImageService` betrifft `MauiProgram`, `ServiceCollectionTests.AddReporterServices_ResolvesFeedSyncService` und alle direkten `new FeedSyncService(...)`-Aufrufe (`FeedSyncServiceTests` ×2, `FeedSyncServiceTests_DebugLog` ×2, `KeywordFilterTests_E2E` ×1).
- **`Item`-/`ItemListItem`-Kopien:** `ArticleDetailViewModel.CreateItemCopy` und `ItemListItem.CopyWith` müssen die neuen Felder mitführen — sonst geht das Bild beim Gelesen-/Gemerkt-Umschalten für die laufende Ansicht verloren.
- **Bild-only-`item_contents`-Zeilen:** `GetRangeAsync` (Content-Lookup) liefert für solche Zeilen kein `ContentHtml` — `MapToModel`/`MapToListItem` tolerieren `null`-Content bereits; `ExtractImageUrl` liefert dann `null` (Remote-Fallback entfällt bei Enclosure-Bildern ohne `<img>` — bewusste Konsequenz).
- **Sync-Laufzeit:** Pro neuem Artikel kommt ein HTTP-Download hinzu; sequentielle Downloads bei vielen neuen Items verlängern den Sync. Mit `MaxImageBytes`-Cap und 30-s-Timeout pro Bild ist das Risiko begrenzt; Fehlschläge kosten nur Fallback-Qualität.
- **DB-Wachstum:** `reporter-content.db` wächst durch BLOBs; Retention-/Löschpfade und Waisen-Sweep begrenzen die Gesamtmenge auf gespeicherte Artikel.
- **`ArticleHtmlSanitizer`-Offline-Verhalten:** Mit `localImage` bleibt gezielt ein `<img>` erhalten — Abweichung vom bisherigen „alle Bilder offline entfernt", in `offline.md` zu dokumentieren.
- **E2E-Hermetik:** `FeedDbAssertions` liest bisher nur `reporter.db`; Bildnachweise benötigen einen lesenden Zugriff auf die danebenliegende `reporter-content.db` (Pfad-Ableitung wie in `MauiProgram`).

## Umsetzungsreihenfolge

1. **`ItemImage`-Record anlegen** (`src/Reporter.Core/Models/ItemImage.cs`)
   - Voraussetzungen: Keine.
   - Beschreibung: Value Object `ItemImage` (`Data`, `ContentType`, `Url`) für den Transport der Bilddaten.

2. **`ItemContent`-Entity und `ContentDbContext`-Mapping erweitern; EF-Migration scaffolden**
   - Voraussetzungen: `Microsoft.EntityFrameworkCore.Design`-Paket und `ContentDbContextFactory` (beide vorhanden); `dotnet-ef`-CLI (vorhanden, 10.0.10).
   - Beschreibung: `ImageData`/`ImageContentType`/`ImageUrl` auf der Entity, Spalten-Mapping in `ConfigureItemContent`, `dotnet ef migrations add AddItemContentImageColumns --project src/Reporter.Data --context ContentDbContext --output-dir Migrations/Content`.

3. **`ItemContentEntry` erweitern; `IItemContentStore` erweitern; `ItemContentRepository` implementieren**
   - Voraussetzungen: Schritte 1 und 2.
   - Beschreibung: `ItemContentEntry` um optionalen `Image`-Parameter; Interface um `GetImageAsync`/`GetImagesAsync`/`GetImageIdsAsync` und den `SetAsync`-Bild-Parameter; Repository implementiert Bild-Upsert-Regel und Lese-Methoden.

4. **`FakeItemContentStore` erweitern**
   - Voraussetzungen: Schritt 3 (Interface-Änderung erzwingt Kompilierung des Fakes).
   - Beschreibung: In-Memory-Bild-Speicher mit identischer Upsert-Semantik.

5. **`Item` und `ItemListItem` erweitern**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `Item.Image` (`set`-Accessor, Ausnahme von den `init`-Accessoren); `ItemListItem.LocalImageData` + `HasLocalImage`; `CopyWith` führt `LocalImageData`.

6. **`IItemImageService` und `ItemImageService` anlegen**
   - Voraussetzungen: Schritt 1 (`ItemImage` als Rückgabetyp); `HttpClient`-Registrierung (vorhanden, `MauiProgram` Z. 90).
   - Beschreibung: `ResolveImageUrl` (Kandidaten-Priorität + relative URLs, gemeinsame `<img>`-Extraktion) und `TryDownloadImageAsync` (MIME-/Größen-Prüfung, `MaxImageBytes`, Download-Fehler → `null`, `OperationCanceledException` propagiert).

7. **`ItemRepository` anpassen**
   - Voraussetzungen: Schritte 3 und 5; für die gemeinsame `<img>`-Extraktion Schritt 6.
   - Beschreibung: `GetByIdAsync` + `MapToModel` mit Bild; `AddAsync` (Guard um `|| item.Image is not null` erweitern) und `AddRangeAsync` (Filter inkl. bild-only-Einträge) schreiben Bild; paged Listenpfade + `GetImagesAsync`; `MapToListItem` setzt `LocalImageData`; `ExtractImageUrl` delegiert.

8. **`FeedSyncService` erweitern und `IItemImageService` in `MauiProgram` registrieren**
   - Voraussetzungen: Schritte 5, 6 und 7.
   - Beschreibung: Konstruktor-Parameter, `GetImageIdsAsync`-Aufruf, `CollectNewItems` liefert Bildkandidaten, sequentielle Download-Schleife, `Item.Image`-Zuweisung per `set`-Accessor, Zusammenführung von Bild- und Content-Backfill zu je einem `ItemContentEntry` pro `ItemId` in `contentBackfill`, aggregierte `IDebugLogService`-Warnung; `.AddSingleton<IItemImageService, ItemImageService>()`.

9. **`ArticleHtmlSanitizer` und `ArticleDetailViewModel` anpassen**
   - Voraussetzungen: Schritte 1, 5 und 7 (`Item.Image` hydratisiert).
   - Beschreibung: `Sanitize`-Signatur um `localImage` (Behandlung vor der Whitespace-Frührückkehr — bild-only-Artikel erhalten ein Header-Fragment); `RebuildHtml` übergibt `Item?.Image`; `CreateItemCopy` führt `Image`.

10. **`ArticleCardView` anpassen**
    - Voraussetzungen: Schritte 5 und 7 (`LocalImageData`/`HasLocalImage` projiziert).
    - Beschreibung: Neue oberste Kaskaden-Stufe `LocalImageData`; `HasLocalImage == False`-Bedingungen auf allen Folgestufen; `IsOnline`-`DataTrigger` wird `MultiTrigger` mit `HasLocalImage == False`.

11. **Betroffene Test-Aufrufstellen und DI-Tests anpassen**
    - Voraussetzungen: Schritte 4, 6 und 8.
    - Beschreibung: `new FeedSyncService(...)`-Aufrufe um `FakeItemImageService` ergänzen; `ServiceCollectionTests` registriert/löst `IItemImageService` auf.

12. **Neue Unit-/Integrationstests schreiben**
    - Voraussetzungen: Schritte 1–10 (Implementierung liegt vor); `TestFeedXml`-Erweiterung für Enclosure-Fixtures (Teil dieses Schritts); `FakeHttpMessageHandler` (vorhanden).
    - Beschreibung: `ItemImageServiceTests`, Erweiterungen in `ItemContentRepositoryTests`, `ItemRepositoryTests`, `FeedSyncServiceTests`, `RetentionCleanupServiceTests`, `FeedRepositoryTests`, `ArticleHtmlSanitizerTests`, `ContentDbContextTests`.

13. **E2E-Infrastruktur und E2E-Test**
    - Voraussetzungen: Schritte 8 und 10; `StubFeedServer`, `ReporterAppFixture`, `E2ETestCollection` (vorhanden).
    - Beschreibung: `StubFeedServer` um `/images/{name}.png`-Endpunkt, Fixture-Feed `image-feed.xml` mit `<enclosure>` bzw. `<img>`, Content-DB-Assertion-Hilfsmethode, neuer E2E-Test für den Bild-Happy-Path.

14. **Dokumentation, manuelle UI-Verifikation und statische Checks**
    - Voraussetzungen: Schritte 1–13.
    - Beschreibung: `docs/help/anwendung/offline.md` (Listenbilder + Detailansicht), `docs/help/anwendung/datenmodell.md` (neue `item_contents`-Spalten); mobile UI-Verifikation der `ArticleCardView`-Änderung (390 × 844 pt) mit Screenshot-Notiz gemäß `AGENTS.md`; `.\scripts\Run-StaticChecks.ps1` ohne Befund.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `ResolveImageUrl_EnclosureImageType_Wins` | `ItemImageServiceTests` | `link rel="enclosure"` mit `image/*`-MIME hat Vorrang vor `<img src>` |
| `ResolveImageUrl_MediaRss_Used` / `..._ItunesImage_Used` | `ItemImageServiceTests` | `media:thumbnail`/`media:content` bzw. `itunes:image` als Kandidaten |
| `ResolveImageUrl_FirstImgSrc_Fallback` / `..._NoCandidate_ReturnsNull` | `ItemImageServiceTests` | Erstes `<img src>` als letzte Stufe; `null` ohne Kandidat |
| `ResolveImageUrl_RelativeUrl_ResolvedAgainstItemLink` | `ItemImageServiceTests` | Relative `<img>`-/Enclosure-Pfade gegen Item-Link (Feed-URL als Fallback) |
| `TryDownloadImageAsync_ValidImage_ReturnsItemImage` | `ItemImageServiceTests` | Erfolgreicher Download liefert `Data`/`ContentType`/`Url` (via `FakeHttpMessageHandler`) |
| `TryDownloadImageAsync_NonImageContentType_ReturnsNull` | `ItemImageServiceTests` | Nicht-`image/*`-Antworten werden verworfen |
| `TryDownloadImageAsync_OversizedContentLength_ReturnsNull` / `..._OversizedStream_ReturnsNull` | `ItemImageServiceTests` | `Content-Length`-Vorabprüfung und Streaming-Cap greifen |
| `TryDownloadImageAsync_HttpError_ReturnsNull` / `..._InvalidUrl_ReturnsNull` | `ItemImageServiceTests` | Fehlerisolation analog `FeedIconServiceTests` |
| `SetAsync_WithImage_PersistsImage` / `GetImageAsync_UnknownItem_ReturnsNull` / `GetImagesAsync_ReturnsOnlyStoredImages` / `GetImageIdsAsync_ReturnsImageIds` | `ItemContentRepositoryTests` | Bild-Roundtrip und Batch-Lesen auf `TestContentDbContextFactory` |
| `SetAsync_NullContentWithImage_KeepsRow` / `SetRangeAsync_ImageOnlyEntry_UpsertsImageColumns` / `SetRangeAsync_ContentOnlyEntry_KeepsImage` | `ItemContentRepositoryTests` | Bild-Upsert-Regel: bild-only-Einträge, Content-Backfill lässt Bilder unberührt |
| `SetAsync_NullContentWithImage_KeepsStoredContent` / `SetRangeAsync_ImageOnlyEntry_KeepsStoredContent` / `SetAsync_NullContent_KeepsStoredImage` | `ItemContentRepositoryTests` | Semantik-Nachweis des Bild-Backfills: `ItemContentEntry(id, null, image)` löscht gespeicherten `content_html` nicht; `SetAsync(id, null)` entfernt den Inhalt, erhält aber ein gespeichertes Bild (Zeile bleibt bild-only bestehen) |
| `DeleteAsync_RemovesImage` / `DeleteRangeAsync_RemovesImage` | `ItemContentRepositoryTests` | Zeilenlöschung entfernt Bilddaten |
| `GetByIdAsync_HydratesImage` | `ItemRepositoryTests` | `Item.Image` wird aus dem Store hydratisiert |
| `GetUnreadByDateAsync_Paged_ProjectsLocalImageData` / `GetSavedForLaterAsync_ProjectsLocalImageData` | `ItemRepositoryTests` | `LocalImageData`-Projektion der Listenpfade |
| `AddAsync_WithImage_StoresImage` / `AddAsync_ImageOnlyItem_StoresEntry` / `AddRangeAsync_ImageOnlyItem_StoresEntry` / `UpdateAsync_KeepsStoredImage` | `ItemRepositoryTests` | Schreibpfade inkl. bild-only-Items — `AddAsync_ImageOnlyItem_StoresEntry` (Item ohne `ContentHtml`, mit `Image`) belegt die erweiterte `AddAsync`-Guard; `UpdateAsync` lässt Bild unberührt |
| `DeleteAsync_RemovesStoredImage` / `DeleteExpiredAsync_RemovesStoredImage` / `DeleteRangeAsync_RemovesStoredImage` | `ItemRepositoryTests` | Mitlöschung der Bilddaten in allen Löschpfaden |
| `SyncFeedAsync_EnclosureImage_StoresImageLocally` | `FeedSyncServiceTests` | Enclosure-Bild wird heruntergeladen und in `item_contents` gespeichert |
| `SyncFeedAsync_FirstImgSrc_StoresImageLocally` / `SyncFeedAsync_RelativeImgSrc_DownloadsResolved` | `FeedSyncServiceTests` | `<img src>`-Fallback und relative-URL-Auflösung im Sync |
| `SyncFeedAsync_ImageDownloadFails_SyncStillSucceeds` | `FeedSyncServiceTests` | Download-Fehlschlag → `FeedHealth.Ok`, Artikel gespeichert, kein Bild (Remote-Fallback) |
| `SyncFeedAsync_OversizedImage_StoresNoImage` | `FeedSyncServiceTests` | Größenlimit greift im Sync — integrativ über `FakeHttpMessageHandler` (URL-abhängige Antworten: Feed-XML + Oversize-Bild) mit **echter** `ItemImageService`-Implementierung; `FakeItemImageService` würde das Limit nicht ausüben, da es in der echten Implementierung lebt |
| `SyncFeedAsync_ExistingItemsWithoutImage_BackfillsImage` / `SyncFeedAsync_ExistingItemWithoutContentAndImage_BackfillsBoth` / `SyncFeedAsync_ExistingImage_NotRedownloaded` | `FeedSyncServiceTests` | Bild-Backfill für Bestandsitems; Item ohne Inhalt **und** ohne Bild (Restore) erhält Inhalt und Bild in einem Sync über den zusammengeführten `ItemContentEntry`; kein erneuter Download bei vorhandenem Bild |
| `CleanupAsync_RemovesOrphanedImages` | `RetentionCleanupServiceTests` | Waisen-Sweep entfernt bild-only-`item_contents`-Zeilen |
| `DeleteAsync_RemovesItemImages` | `FeedRepositoryTests` | Feed-Kaskade entfernt Bilddaten der Feed-Artikel |
| `Sanitize_Offline_WithLocalImage_ReplacesFirstImgSrcWithDataUri` / `Sanitize_Offline_LocalImageWithoutImgTag_PrependsHeaderImage` / `Sanitize_Offline_EmptyContentWithLocalImage_ReturnsHeaderImage` / `Sanitize_Offline_WithLocalImage_RemovesOtherImages` / `Sanitize_Online_WithLocalImage_KeepsRemoteSrc` | `ArticleHtmlSanitizerTests` | Offline-Einbettung der `data:`-URI, Header-Fallback (auch bei leerem/`null`-Content — bild-only-Artikel), restliche `<img>`-Entfernung, Online-Unverändertheit |
| `CopyWith_PreservesLocalImageData` | `ItemListItemTests` (neue Klasse, Muster `FeedListItemTests`) | `LocalImageData`/`HasLocalImage` bleiben bei `CopyWith`-Kopien (Gelesen-/Gemerkt-Umschalten in `UnreadViewModel`/`LaterViewModel`) erhalten |
| `ItemContent_PersistImageRoundtrip` | `ContentDbContextTests` | Neue Spalten persistieren via `TestContentDbContextFactory` (echte Migrationen) |
| `AddReporterServices_ResolvesItemImageService` (oder Erweiterung von `..._ResolvesFeedSyncService`) | `ServiceCollectionTests` | `IItemImageService`/`ItemImageService` auflösbar; `FeedSyncService` mit neuer Abhängigkeit auflösbar |
| `FakeItemImageService` | Hilfsklasse | Konfigurierbares `IItemImageService`-Double (`NextResult`, `NextException`, `RequestedUrls`) |
| `TestFeedXml`-Erweiterung | Hilfsmethode | RSS-/Atom-Fixtures mit `<enclosure>`/`link rel="enclosure"` bzw. `<img>` im Content (neue Überladung, bestehende Aufrufe unverändert) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `FeedSyncServiceTests` (`CreateService`, `CreateFailingService`), `FeedSyncServiceTests_DebugLog` (2 Aufrufe), `KeywordFilterTests_E2E` (1 Aufruf) | Neuer `FeedSyncService`-Konstruktorparameter `IItemImageService` → `FakeItemImageService` übergeben |
| `ServiceCollectionTests.AddReporterServices_ResolvesFeedSyncService` | `IItemImageService` muss registriert werden, damit `IFeedSyncService` auflösbar bleibt |
| `FakeItemContentStore` | Neue Interface-Member von `IItemContentStore` zu implementieren |
| `ArticleHtmlSanitizerTests.Sanitize_Offline_RemovesImages` | Erweiterte `Sanitize`-Signatur; Verhalten für `localImage: null` bleibt gültig — Test ggf. präzisieren (ohne `localImage` werden weiterhin alle `<img>` entfernt) |
| `ItemContentRepositoryTests.SetRangeAsync_DuplicateItemId*` / `SetAsync_*` | Bestehende Verhaltensannahmen bleiben für content-only-Einträge gültig; Aufrufe kompilieren über den optionalen `Image`-Parameter weiter — bei der Semantik-Präzisierung auf Konsistenz prüfen |
| `ContentDbContextTests.ItemContents_MappedToExpectedTable` | Spaltenerwartung der Tabelle `item_contents` zwingend um `image_data`/`image_content_type`/`image_url` ergänzen — der Test prüft die Spalten einzeln (`ContentDbContextTests.cs:34-35`) |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Stub-Feed mit `<enclosure>`-Bild per UI hinzufügen, auf **Ungelesen** aktualisieren: Das Artikelbild wird heruntergeladen, in `reporter-content.db` gespeichert und auf der Artikelkarte als Thumbnail angezeigt | Neue Klasse `ArticleImageTests` (`src/Reporter.E2ETests/`); `StubFeedServer` um `/images/{name}.png`-Endpunkt + Fixture `image-feed.xml`; neue Content-DB-Assertion `ItemImageExistsAsync` (liest `reporter-content.db` neben `REPORTER_DB_PATH`, Muster `FeedDbAssertions`) | „`FeedSyncService` lädt das Artikelbild herunter und legt es im Content-Store ab; `ArticleCardView` zeigt es" | Happy Path des Benutzerflusses vom Feed-Abruf bis zur sichtbaren Anzeige — belegt, dass Download, Persistenz, Hydratisierung und UI-Kaskade im Zusammenspiel funktionieren; reine Unit-Tests erreichen weder die echte App-Integration noch die `Image`-Bindung |
| Empfohlen | Artikel mit lokalem Bild öffnen: In der `ArticleDetailPage` ist ein Bild-Element sichtbar (WebView-UIA-Baum, `ControlType.Image`/`data:`-Quelle) | `ArticleImageTests` | „`ArticleDetailPage` zeigt das lokal gespeicherte Bild" | Nachweis der `data:`-URI-Einbettung im realen WebView-Rendering; der Sanitizer-Pfad wird sonst nur isoliert ohne WebView getestet |
| Optional | Fehlerisolierung: Feed-Item mit ungültiger Bild-URL (404 am Stub) → Sync läuft erfolgreich, Artikel erscheint ohne Thumbnail (Favicon/Initial-Fallback) | `ArticleImageTests` | „Fällt der Download aus, bleibt die Remote-URL als Fallback und der Abruf wird nicht blockiert" | Anwendersichtbarer Fehlerfall über den realen Sync-Pfad |

**Begründung für fehlende E2E-Abdeckung des Offline-Bilds:** Der eigentliche Offline-Anzeigefall (Thumbnail/Detailbild ohne Netz) ist über FlaUI nicht auslösbar: `NetworkStatusService` liest `Connectivity.Current.NetworkAccess` des echten Systems, und die E2E-Umgebung bietet keinen Netzwerk-Toggle. Die Offline-Darstellung wird daher durch `ArticleHtmlSanitizerTests`/`ItemRepositoryTests` (Hydratisierung ohne Netz-Abhängigkeit) und die im `AGENTS.md` geforderte manuelle UI-Verifikation abgedeckt — nicht durch vorhandene Unit-Tests „ersetzt", sondern weil der Auslöser (Verbindungsverlust) außerhalb der E2E-Steuerbarkeit liegt.

Welche bestehenden E2E-Tests müssen angepasst werden?

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| Keine. | — |

## Offene Punkte

Keine.
