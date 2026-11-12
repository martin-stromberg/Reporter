<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Lesepakete (GitHub Issue #164)

## Übersicht

Die Startseite **Ungelesen** lädt ungelesene Artikel künftig als **Lesepaket pro Kalendertag**: Beim frischen Laden bestimmt der erste Artikel der aktiven Sortierreihenfolge den Anker-Tag — bei aufsteigender Sortierung (`oldest first`) der älteste belegte Tag, bei absteigender (`newest first`) der neueste belegte Tag (i. d. R. der aktuelle Tag). Nur dessen Artikel werden angezeigt; bei weniger als 5 Artikeln zusätzlich die des nächsten belegten Tags in Sortierrichtung. Der Doppelhaken-Button (**Alles gelesen**) markiert den vollständigen Paketbereich in der Datenbank — auch Paket-Artikel, die noch nicht in die Liste geladen wurden. Umgesetzt wird das in der Repository-Schicht (`IItemRepository`/`ItemRepository`) mit einem neuen Paket-Parameterobjekt, im `UnreadViewModel` (Paket-Kontext beim frischen Laden, paket-begrenztes Markieren) und minimal in `UnreadPage.xaml.cs` (paket-bezogener Bestätigungstext).

## Scope-Klärung

Verbindliche fachliche Abgrenzung vor der Umsetzung — übernommen bzw. konkretisiert aus `requirement.md`, Abschnitt `Fachlicher Scope`, inkl. der Anwenderentscheidungen vom 2026-11-12:

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Das **Lesepaket**: alle ungelesenen `Item` (`IsRead == false`) des Anker-Kalendertags (Ortsdatum aus `PublishedAt`, lokale Gerätezeit), eingeschränkt auf `SelectedCategory.CategoryId`. Der **Anker-Tag** ist der Kalendertag des ersten Listenelements in der **aktiven** Sortierreihenfolge — die Paket-Logik ist sortierneutral/symmetrisch: `asc` → ältester belegter Tag, `desc` → neuester belegter Tag (meist der aktuelle Tag, falls belegt). Enthält der Anker-Tag weniger als 5 Artikel, erweitert sich das Paket um alle Artikel des **Folgetags** — des nächsten in Sortierrichtung gelegenen Kalendertags **mit** ungelesenen Artikeln. |
| Auslösende Ereignisse | Frisches Laden = `LoadPageAsync` mit `append == false`, ausgelöst durch: `OnAppearing`/`LoadCommand` → `LoadAsync`; `RefreshCommand`/`RefreshAsync` → `LoadAsync`; `SelectCategoryCommand`/`SelectCategoryAsync`; `MarkAllReadCommand` → `LoadAsync`; `MarkReadAsync` bei `Articles.Count == 0` → `LoadAsync` (**Anforderung**: Verschwindet der letzte sichtbare Artikel durch Markieren-als-gelesen, erfolgt automatisch ein frisches Neuladen mit neuer Paket-Entscheidung). **Nur** dort wird die Paket-Ermittlung (Anker + <5-Erweiterung) durchgeführt. `LoadMoreAsync` (`append == true`) und das einzelne Entfernen gelesener Artikel lösen keine neue Paket-Ermittlung aus. |
| Erwartete Wirkung | Die Liste zeigt nach frischem Laden nur Artikel des Lesepakets. Der Doppelhaken markiert den **vollständigen theoretisch sichtbaren Paketbereich in der Datenbank** — unabhängig davon, wie viele Paket-Artikel tatsächlich in die UI-Liste geladen wurden (ein Paket mit >`PageSize` Artikeln wird auch dann komplett markiert, wenn der Nutzer nicht bis zum Ende scrollte). Danach lädt `LoadAsync` automatisch das nächste Paket. Das Entfernen einzelner Artikel lässt den Paket-Umfang unverändert (kein Nachrücken des Folgetags); entfällt dadurch der letzte sichtbare Artikel, lädt die Liste frisch neu (neue Paket-Entscheidung erlaubt). |
| Umgang mit fehlenden Objekten | `PublishedAt == null` (Feed lieferte kein Datum): Zuordnung zu einem **Pseudo-Tag** — in aufsteigender Sortierung bildet er das **erste** Paket, in absteigender das **letzte** (spiegelt das dokumentierte SQLite-`NULL`-Sortierverhalten; Anwenderentscheidung 2026-11-12). Feeds, Kategorien, Settings existieren bereits — kein weiteres fachliches Objekt muss erzeugt oder aktiviert werden. Bei keinem ungelesenen Artikel existiert kein Paket (`null`); die Liste zeigt den bestehenden Leerzustand. |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine sichtbaren Tagesgruppen-Header oder Abschnittstrenner in der Liste — die Tagesbegrenzung ist ein reines Lade-/Markierverhalten. | Ausdrücklich aus der Anforderungsübersetzung; das Anforderungsbild verlangt keine Gruppierung. | bestätigt (2026-11-12) |
| 2 | Kein automatisches „Überspringen" älterer Tage — es wird immer das Paket des Anker-Tags gezeigt (aufsteigend: ältester Tag; absteigend: neuester Tag). | Anforderung beschreibt ausschließlich dieses Verhalten. | bestätigt (2026-11-12) |
| 3 | `LoadMore`/`RemainingItemsThreshold` lädt am Paketende **keinen** weiteren Tag per Scrollen nach; die Liste endet am Paketende. **Davon ausgenommen ist ausschließlich** das automatische frische Neuladen, wenn durch Markieren-als-gelesen der letzte sichtbare Artikel entfällt — dieses Verhalten ist ausdrücklich Anforderung (siehe Scope-Klärung → Auslösende Ereignisse). | Die Nachlade-Entscheidung darf laut Anforderung nur beim frischen Neuladen getroffen werden; ein Endlos-Scrollen über Tage hinweg würde die Paket-Idee unterlaufen. | bestätigt (2026-11-12) |
| 4 | Keine Änderung am Karten-Layout, an den Filter-Chips, an der Pull-to-Refresh-Geste und am `LaterPage`-/`FeedDetailPage`-Verhalten. | Ausdrücklich aus der Anforderungsübersetzung. | bestätigt (2026-11-12) |
| 5 | Keine neue Benutzereinstellung für die tageweise Anzeige oder die 5er-Schwelle — Festwert `5` als Konstante analog `PageSize`. | Anforderung legt den Wert fest; kein Konfigurationsbedarf genannt. | bestätigt (2026-11-12) |
| 6 | `UnreadBadgeText`, `UnreadCount` und die Chip-`Count`s zeigen weiterhin die **Gesamt**-Ungelesenzahl (über alle Tage) — kein Paket-Zähler in Badge/Chips. | Die Zähler dokumentieren die Gesamtmenge; nur der Bestätigungsdialog des Doppelhakens wird paket-bezogen. Ein Paket-Zähler im Badge würde Doppel-Information erzeugen. | bestätigt (2026-11-12) |

**Präzisierung zu Ausschluss 6 (ohne Änderung des bestätigten Ausschlusses):** Ausschluss 6 gilt ausschließlich für `UnreadBadgeText`, `UnreadCount` und die Chip-`Count`s. `CardPositionText` (Positionsanzeige des Kartenlayouts) ist davon nicht erfasst — dort wird der **Paket-Nenner** festgelegt (siehe Designentscheidung „Karten-Positionsanzeige"), weil sein Nenner die erreichbare Stapelgröße beschreibt und der Kartenstapel am Paketende endet.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Paket-Ermittlung (Anker-Tag + <5-Erweiterung) | Im `ItemRepository` als `ResolveUnreadPacketAsync` statt Orchestrierung mehrerer Abfragen im ViewModel | Die Tagesgrenzen und die Erweiterungsentscheidung sind Datenbank-Filterlogik; eine einzige Repository-Methode hält Paging, Zähler und Markierung konsistent (gleiches Prädikat) und ist auf DB-Ebene testbar (`ItemRepositoryTests`). Das „nur beim frischen Laden"-Kriterium der Anforderung wird durch den **Aufrufzeitpunkt** im ViewModel erzwungen, nicht durch den Ort der Regel. |
| Sortierneutralität | `ResolveUnreadPacketAsync` erhält `ascending` und wendet die Paket-Logik symmetrisch an: Anker = Tag des ersten Listenelements in aktiver Sortierrichtung, Folgetag = nächster belegter Tag **in Sortierrichtung** | Anwenderentscheidung 2026-11-12: Die Paketbildung orientiert sich an der Sortierrichtung; `asc` → erstes Paket = ältester Tag, `desc` → erstes Paket = neuester Tag (i. d. R. heute). Eine Begrenzung auf `asc` würde das Feature für die Standardeinstellung `"desc"` still deaktivieren. |
| Paket-Beschreibung | Neuer Parameterobjekt-Record `UnreadPacket` (`DateTime? FromUtc`, `DateTime? ToUtc`, `bool IncludeUndated`, `IReadOnlyList<DateOnly?> Days`) — Specification-Ansatz | Ein halboffener UTC-Bereich `[FromUtc, ToUtc)` deckt Anker-Tag **und** Folgetag ab, auch wenn diese nicht kalender-benachbart sind (dazwischenliegende Tage haben definitionsgemäß keine ungelesenen Artikel — Folgetag = nächster **belegter** Tag). `IncludeUndated` bildet den Pseudo-Tag undatierter Artikel ab. `Days` hält die konkreten lokalen Kalendertage des Pakets (Element 0 = Anker-Tag, Element 1 = Folgetag bei <5-Erweiterung; `null`-Eintrag = Pseudo-Tag undatierter Artikel) — nötig, weil das Dialog-Label die tatsächlich belegten Tage nennen soll und sich diese bei übersprungenen Leer-Tagen nicht aus dem zusammenhängenden UTC-Bereich ableiten lassen. Ein Record statt Einzelparameter hält Interface, Implementierung und `DelegatingItemRepository` schlank. |
| Bestätigungsdialog | Neue ViewModel-Eigenschaft `MarkAllReadConfirmationText` (Paket-Anzahl + Tageslabel) als `DisplayActionSheetAsync`-Titel statt `UnreadBadgeText` | Anwenderentscheidung 2026-11-12 (Dialog zeigt Paket-Umfang). Der Dialog zeigt heute die Gesamtzahl („42 ungelesen"), markiert künftig aber nur das Paket — ein irreführender Titel ist kein zulässiger Planansatz. |
| Dialog-/Karten-Zähler Konsistenz | `_packetUnreadCount` (`int`) als mitgeführter ViewModel-Zähler: beim frischen Laden via `GetUnreadPacketCountAsync` gesetzt (0, wenn `_currentPacket == null`), in `MarkReadAsync` pro entferntem Artikel um 1 dekrementiert (`Math.Max(0, …)` — analog `UnreadCount`). `MarkAllReadConfirmationText` ist ein **Getter** (Muster `UnreadBadgeText`), der `_packetUnreadCount` + `_packetDayLabel` über `MarkAllReadPacketTitleFormat` formatiert; `MarkReadAsync` benachrichtigt `MarkAllReadConfirmationText` und `CardPositionText` | Alle in der paket-begrenzten Liste sichtbaren Artikel gehören definitionsgemäß zum Paket — ein einzelnes Als-gelesen-Markieren verringert daher immer die verbleibende Paket-Anzahl. Die Zähler-Mitführung entspricht exakt dem bestehenden Pflegemuster für `UnreadCount` und Chip-`Count`s in `MarkReadAsync`, braucht keinen DB-Roundtrip beim Dialog-Öffnen und keinen neuen Async-Hook in `OnMarkAllReadTapped`. Alternative „Neuberechnung via `GetUnreadPacketCountAsync` beim Tap" verworfen: DB-Zugriff auf die UI-Geste, zusätzliche Latenz vor dem ActionSheet und ein weiterer öffentlicher VM-Einstiegspunkt nur für diese Anzeige. |
| Karten-Positionsanzeige | `CardPositionText`-Nenner: `Math.Max(_packetUnreadCount, _articles.Count)` statt `Math.Max(UnreadCount, _articles.Count)` — Nenner = **Gesamt-Paket-Anzahl**, konsistent zum Bestätigungsdialog | Der Nenner des „x von y"-Indikators beschreibt die erreichbare Stapelgröße. Der Kartenstapel endet am Paketende (Scope-Ausschluss 3) — mit Gesamt-Ungelesenzahl entstünde „3 von 42" bei nur 7 existierenden Karten, eine Position 42 wäre nie erreichbar. `_packetUnreadCount` ist ohnehin für den Dialog geführt und sinkt synchron mit jedem einzeln gelesenen Artikel. Bei `_currentPacket == null` ist `HasArticles == false` und der Getter liefert ohnehin `string.Empty`. |
| Paging-Abfrage | Erweiterung der bestehenden `GetUnreadByDateAsync(int, int, Guid?, bool)` um optionalen Parameter `UnreadPacket? packet = null` | Es existiert genau eine paged Ungelesen-Abfrage mit einer Projektions-Pipeline (`SelectListItemRows`/`MapToListItem`); ein optionaler Paket-Filter vermeidet eine parallele Methode mit duplizierter Projektion. `packet == null` = unbegrenzt (bisheriges Verhalten bleibt für andere Aufrufer erhalten). |
| Massenmarkierung | Neue Methode `MarkUnreadPacketAsReadAsync(UnreadPacket, Guid?)` neben unverändertem `MarkAllAsReadAsync`; markiert per `ExecuteUpdate` den **gesamten Paketbereich in der DB**, unabhängig vom Ladezustand der UI-Liste | Anwenderentscheidung 2026-11-12: „Alles gelesen" wirkt auf das gesamte **theoretisch sichtbare** Paket — auch auf Paket-Artikel, die wegen `PageSize`-Paging noch nicht geladen wurden. Daher darf die Markierung nicht aus `Articles` abgeleitet werden, sondern nutzt dasselbe DB-Prädikat wie die Leseabfrage. `MarkAllAsReadAsync` bleibt bestehen (Interface-Vertrag, `DelegatingItemRepository`, indirekte Testnutzung). |
| Tagesgrenze | Ortsdatum (lokale Gerätezeit): `SpecifyKind(PublishedAt, Utc).ToLocalTime().Date`; SQL-Filter in UTC-Grenzen `[lokaler Tagesbeginn, nächster lokaler Tagesbeginn)` | Anwenderentscheidung 2026-11-12 (Kalendertag = lokale Gerätezeit). `PublishedAt` wird ausschließlich als `PublishDate.UtcDateTime` geschrieben; aus SQLite zurückgelesene Werte sind `Kind.Unspecified` und müssen daher vor der Ortszeit-Umrechnung als UTC interpretiert werden (`DateTime.SpecifyKind`), sonst verschieben sich Tagesgrenzen um die UTC-Abweichung. |
| 5er-Schwelle | `public const int MinPacketSize = 5` auf `ItemRepository` | Fachlicher Festwert analog `PageSize`; `public`, damit Tests den Schwellwert referenzieren statt ihn zu duplizieren. |
| Auto-Reload beim Wegfall des letzten Artikels | Bestehendes Verhalten `MarkReadAsync` → `LoadAsync` bei `Articles.Count == 0` bleibt und gilt als Anforderung: frisches Neuladen, neue Paket-Entscheidung erlaubt | Anwenderentscheidung 2026-11-12: Verschwindet der einzige/letzte sichtbare Artikel durch Markieren-als-gelesen, soll automatisch das nächste Paket geladen bzw. aktualisiert werden. Kein Mechanismus-Neubau nötig — der bestehende Reload-Pfad zählt als frischer Ladevorgang. |

## Programmabläufe

### Frisches Laden der Ungelesen-Liste (Paket-Ermittlung, sortierneutral)

Auslöser: `OnAppearing` → `LoadCommand`, `RefreshAsync`, `SelectCategoryAsync`, `MarkAllReadAsync`, `MarkReadAsync` bei leerer Liste — alle enden in `LoadPageAsync(page: 0, append: false)`.

1. `LoadPageAsync` liest `ISettingsRepository.GetAsync()` und ermittelt `ascending` aus `Settings.UnreadSortOrder` (unverändert).
2. Neu: Bei `append == false` ruft das ViewModel `_itemRepository.ResolveUnreadPacketAsync(categoryId, ascending)` auf und speichert das Ergebnis in `_currentPacket` (`null` = keine ungelesenen Artikel).
3. `ResolveUnreadPacketAsync` im `ItemRepository`:
   a. Basisabfrage `Items.Where(i => !i.IsRead)` + optionaler `categoryId`-Filter über `feedIds`-Subquery (bestehendes Muster).
   b. Bei leerer Basisabfrage → `null` zurückgeben.
   c. Anker = `PublishedAt` des ersten Elements in der **aktiven** Sortierreihenfolge (aufsteigend `OrderBy(PublishedAt).ThenByDescending(Id)`, absteigend `OrderByDescending(PublishedAt).ThenBy(Id)` — dieselbe Ordnung wie die Listenabfrage). Aufsteigend ist das der älteste, absteigend der neueste belegte Tag.
   d. Anker-Tag bestimmen: `PublishedAt == null` → Pseudo-Tag (`IncludeUndated = true`, keine Bereichsgrenzen; asc → erstes Paket, desc → letztes Paket); sonst Ortsdatum (lokale Gerätezeit) → UTC-Grenzen `[fromUtc, toUtc)`.
   e. Anzahl der ungelesenen Artikel **im Anker-Tag** zählen (gleiches Prädikat). `< MinPacketSize` → Folgetag suchen: nächster **belegter** Tag in Sortierrichtung (aufsteigend: `PublishedAt >= toUtc`, undatierter Anker: `PublishedAt != null`; absteigend: `PublishedAt < fromUtc`, ohne Treffer zusätzlich `PublishedAt == null` prüfen → `IncludeUndated`). Gefundener Folgetag erweitert die Bereichsgrenze auf sein Tagesende (aufsteigend) bzw. seinen Tagesbeginn (absteigend); kein Folgetag → Paket bleibt der Anker-Tag.
   f. `Days` befüllen: Element 0 = Anker-Tag als lokales `DateOnly` (`null` bei undatiertem Anker), Element 1 = Folgetag (`null` bei undatiertem Folgetag), nur bei erfolgter Erweiterung. Mögliche Kombinationen: `[d]` (ein Tag), `[d₁, d₂]` (Anker + Folgetag, ggf. nicht kalender-benachbart), `[null]` (nur undatierte), `[null, d]` (undatierter Anker asc + datierter Folgetag), `[d, null]` (datierter Anker desc + undatierter Folgetag).
4. `LoadPageAsync` ruft `GetUnreadByDateAsync(page, PageSize, categoryId, ascending, _currentPacket)` auf. `_currentPacket == null` bedeutet Repository-seitig „unbegrenzt" (kein Paket-Filter) — praktisch liefert die Abfrage eine leere Seite, weil `_currentPacket == null` nur bei keinerlei ungelesenen Artikeln auftritt.
5. `Articles` ersetzen (frisch) bzw. anhängen (paging), `HasMore = items.Count == PageSize`, Chip-Count/`UnreadCount` aktualisieren (unverändert).
6. Neu (nur bei `append == false`): Paket-Anzeigestate setzen — `_packetUnreadCount = _currentPacket is null ? 0 : await GetUnreadPacketCountAsync(_currentPacket, categoryId)` (**Gesamt-Paket-Anzahl** inkl. nicht geladener Paket-Artikel) und `_packetDayLabel` aus `_currentPacket.Days` komponieren (`string.Empty`, wenn `_currentPacket == null`): `string.Join(" + ", days.Select(d => d?.ToString("d", CurrentCulture) ?? AppResources.UnreadNoDateLabel))`. Beispiele (Kurzdatumformat `d` der aktuellen Kultur, hier de-DE): `[12.11.]` → „12.11.2025"; `[12.11., 14.11.]` → „12.11.2025 + 14.11.2025"; `[null]` → „ohne Datum"; `[null, 15.11.]` → „ohne Datum + 15.11.2025"; `[15.11., null]` → „15.11.2025 + ohne Datum". Danach `OnPropertyChanged` für `MarkAllReadConfirmationText` und `CardPositionText` auslösen.

Beteiligte Klassen/Komponenten: `UnreadViewModel`, `IItemRepository`, `ItemRepository`, `UnreadPacket`, `ISettingsRepository`, `ICategoryRepository`.

### Nachladen innerhalb des Pakets (`LoadMoreAsync`)

1. `LoadMoreAsync` (Guard `IsLoading || !HasMore`) erhöht `_currentPage` und ruft `LoadPageAsync(page, append: true)` — **unverändert**.
2. Weil die Abfrage paket-begrenzt ist, endet das Paging am Paketende: Eine Seite mit `< PageSize` Treffern setzt `HasMore = false`; der Folgetag wird **nicht** per Scrollen nachgeladen (Scope-Ausschluss 3).

Beteiligte Klassen/Komponenten: `UnreadViewModel`, `IItemRepository`.

### Alles gelesen auf Paket-Umfang (`MarkAllReadCommand`)

1. Nutzer tippt den Doppelhaken → `UnreadPage.OnMarkAllReadTapped` (Guard `HasArticles`) zeigt `DisplayActionSheetAsync` mit Titel `MarkAllReadConfirmationText` (statt bisher `UnreadBadgeText`), Abbruch-Button `ButtonCancel`, Bestätigung `ButtonMarkAllRead`.
2. Bei Bestätigung → `MarkAllReadCommand` → `MarkAllReadAsync`: bei `_currentPacket != null` Aufruf von `MarkUnreadPacketAsReadAsync(_currentPacket, SelectedCategory?.CategoryId)` — `ExecuteUpdate` setzt `IsRead`/`ReadAt` für **alle** Artikel des Paketbereichs in der Datenbank (Bereich + `IncludeUndated` + Kategoriefilter), einschließlich Paket-Artikel, die noch nicht in `Articles` geladen waren (Anwenderentscheidung 2026-11-12). Bei `_currentPacket == null` wird die Markierung **übersprungen** und direkt `LoadAsync` ausgeführt (defensiver Pfad — über die UI wegen des `HasArticles`-Guards in `OnMarkAllReadTapped` praktisch nicht erreichbar, da `_currentPacket == null` ⇔ keine ungelesenen Artikel ⇔ leere Liste).
3. `LoadAsync` lädt frisch → `ResolveUnreadPacketAsync` findet den nächsten Anker-Tag in Sortierrichtung → Liste zeigt das Folge-Paket.
4. Abbruch im ActionSheet: keine Markierung, kein Reload (bestehendes Verhalten).

Beteiligte Klassen/Komponenten: `UnreadPage`, `UnreadViewModel`, `IItemRepository`, `ItemRepository`, `UnreadPacket`.

### Einzelnes Als-gelesen-Markieren (`MarkReadCommand`)

1. `MarkReadAsync` markiert per `MarkAsReadAsync`, entfernt den Eintrag aus `Articles`, aktualisiert Zähler — **ohne Paket-Neubewertung**: `_currentPacket` wird nicht angefasst, kein Nachrücken des Folgetags.
2. Neu: Zusätzlich `_packetUnreadCount = Math.Max(0, _packetUnreadCount - 1)` — jeder aus der paket-begrenzten Liste entfernte Artikel gehört definitionsgemäß zum Paket, die verbleibende Paket-Anzahl sinkt also synchron mit (gleiches Pflegemuster wie `UnreadCount`). Danach `OnPropertyChanged` für `MarkAllReadConfirmationText` und `CardPositionText`, damit Dialog-Titel und Karten-Nenner nicht veralten (Floor `0` deckt den defensiven Fall `_currentPacket == null` ab, `_packetUnreadCount` ist dann ohnehin `0`).
3. Bei `Articles.Count == 0` — d. h. der letzte sichtbare Artikel ist durch Markieren-als-gelesen entfallen — → `LoadAsync` → **frisches Neuladen mit neuer Paket-Entscheidung** (bestehender Pfad, ausdrücklich Anforderung laut Anwenderentscheidung 2026-11-12: automatisches Laden des nächsten Pakets bzw. Aktualisierung); `_packetUnreadCount`/`_packetDayLabel` werden dabei neu gesetzt.

Beteiligte Klassen/Komponenten: `UnreadViewModel`, `IItemRepository`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `UnreadPacket` (`src/Reporter.Core/Models/UnreadPacket.cs`) | `record` (Datenmodell) | Parameterobjekt eines Lesepakets: `FromUtc`/`ToUtc` (halboffener UTC-Bereich, `null` = unbegrenzt auf dieser Seite) + `IncludeUndated` (Pseudo-Tag für `PublishedAt == null`) + `Days` (`IReadOnlyList<DateOnly?>`, konkrete lokale Kalendertage — Element 0 = Anker-Tag, Element 1 = Folgetag bei Erweiterung; `null`-Eintrag = undatierter Pseudo-Tag; Grundlage des Dialog-Tageslabels). Filtersemantik: `(i.PublishedAt >= FromUtc && i.PublishedAt < ToUtc) \|\| (IncludeUndated && i.PublishedAt == null)`; beide Grenzen `null` + `IncludeUndated` → nur undatierte Artikel. |
| `UnreadPacketTests` (`src/Reporter.E2ETests/UnreadPacketTests.cs`) | FlaUI-Testklasse | E2E-Abdeckung des Lesepaket-Flusses (siehe E2E-Tests). |
| `day-packet-feed.xml` (`src/Reporter.E2ETests/Fixtures/day-packet-feed.xml`) | RSS-Fixture | Stub-Feed mit kontrollierten `pubDate`-Werten über 3 Kalendertage (z. B. 6 + 3 + 3 Artikel), damit Paketbildung und Paket-Wechsel E2E prüfbar sind. `pubDate`-Werte liegen jeweils zur **Orts-Mitte** der beabsichtigten lokalen Kalendertage (z. B. 12:00 UTC bzw. mit Puffer zu beiden Tagesgrenzen), damit die Tag-Zuordnung auf der Ausführungsmaschine unabhängig von deren Zeitzone eindeutig ist — keine Zeiten nahe Mitternacht. |

## Änderungen an bestehenden Klassen

### `IItemRepository` (Interface, `src/Reporter.Core/Interfaces/IItemRepository.cs`)

- **Neue Methoden:**
  - `ResolveUnreadPacketAsync(Guid? categoryId = null, bool ascending = false)` → `Task<UnreadPacket?>` — ermittelt Anker-Tag (sortierneutral) und <5-Erweiterung; `null` bei keinen ungelesenen Artikeln.
  - `GetUnreadPacketCountAsync(UnreadPacket packet, Guid? categoryId = null)` → `Task<int>` — Anzahl ungelesener Artikel im **gesamten** Paket (für den Bestätigungsdialog).
  - `MarkUnreadPacketAsReadAsync(UnreadPacket packet, Guid? categoryId = null)` → `Task` — Massenmarkierung des gesamten Paketbereichs in der DB.
- **Geänderte Methoden:** `GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId = null, bool ascending = false)` erhält den optionalen Parameter `UnreadPacket? packet = null`.

### `ItemRepository` (Klasse, `src/Reporter.Data/Repositories/ItemRepository.cs`)

- **Neue Konstanten:** `public const int MinPacketSize = 5` — fachliche Schwelle der Paket-Erweiterung.
- **Neue Methoden:** Implementierung der drei neuen Interface-Member (s. o.); `ResolveUnreadPacketAsync` füllt dabei auch `UnreadPacket.Days` (Anker-Tag + ggf. Folgetag als lokale `DateOnly`, `null`-Eintrag für den undatierten Pseudo-Tag).
- **Geänderte Methoden:** `GetUnreadByDateAsync(...)` — zusätzliches Paket-Prädikat in der `Where`-Kette vor Sortierung/Paging; Rest der Pipeline unverändert.
- **Neue private Hilfsmethoden (konzeptionell):** Paket-Prädikat als `Expression<Func<Item, bool>>` (geteilt zwischen Lese-, Zähl- und Markierpfad — garantiert, dass „Alles gelesen" denselben Bereich erfasst, den das Paket theoretisch sichtbar macht), Tagesgrenzen-Berechnung (`SpecifyKind(Utc).ToLocalTime().Date` → UTC-Grenzen), sortierneutrale Folgetag-Suche für `ResolveUnreadPacketAsync`.

### `UnreadViewModel` (Klasse, `src/Reporter.Core/ViewModels/UnreadViewModel.cs`)

- **Neue Felder/Eigenschaften:**
  - `_currentPacket` (`UnreadPacket?`) — Paket-Kontext des aktuell angezeigten Listeninhalts.
  - `_packetUnreadCount` (`int`, privat) — Gesamt-Paket-Anzahl; beim frischen Laden aus `GetUnreadPacketCountAsync`, in `MarkReadAsync` dekrementiert (Muster `UnreadCount`).
  - `_packetDayLabel` (`string`, privat) — Tageslabel aus `_currentPacket.Days` komponiert (siehe Programmablauf Schritt 6).
  - `MarkAllReadConfirmationText` (`string`, **Getter** analog `UnreadBadgeText`) — Titel des Bestätigungsdialogs; formatiert `_packetUnreadCount` + `_packetDayLabel` über `AppResources.MarkAllReadPacketTitleFormat`.
- **Geänderte Methoden/Getter:**
  - `LoadPageAsync` — bei `append == false` Paket-Ermittlung (`ResolveUnreadPacketAsync`) sowie `_packetUnreadCount`/`_packetDayLabel` setzen und `MarkAllReadConfirmationText`/`CardPositionText` benachrichtigen; übergibt `_currentPacket` an die Abfrage.
  - `MarkReadAsync` — zusätzlich `_packetUnreadCount` dekrementieren (`Math.Max(0, …)`) und `MarkAllReadConfirmationText`/`CardPositionText` benachrichtigen; `_currentPacket` selbst bleibt unangetastet.
  - `MarkAllReadAsync` — statt `MarkAllAsReadAsync` Aufruf von `MarkUnreadPacketAsReadAsync(_currentPacket, SelectedCategory?.CategoryId)`; bei `_currentPacket == null` Markierung überspringen und nur `LoadAsync` ausführen.
  - `CardPositionText` (Getter) — Nenner `Math.Max(_packetUnreadCount, _articles.Count)` statt `Math.Max(UnreadCount, _articles.Count)` (Paket-Gesamtzahl, s. Designentscheidung „Karten-Positionsanzeige").
- **Unverändert:** `LoadMoreAsync` (endet am Paketende), `OnMarkAllReadTapped`-Aufrufseite (liest `MarkAllReadConfirmationText` synchron — kein neuer Async-Hook nötig), `RefreshAsync`, `SelectCategoryAsync` (beide lösen über `LoadPageAsync(0, append: false)` automatisch eine neue Paket-Ermittlung aus).

### `UnreadPage` (Code-Behind, `src/Reporter/Views/UnreadPage.xaml.cs`)

- **Geänderte Methoden:** `OnMarkAllReadTapped` — `DisplayActionSheetAsync`-Titel `MarkAllReadConfirmationText` statt `UnreadBadgeText`. Kein XAML-Eingriff nötig (Dialog-Titel kommt aus dem ViewModel).

### `AppResources.resx` / `AppResources.de.resx` / `AppResources.Designer.cs` (`src/Reporter.Core/Resources/Strings/`)

- **Neue Einträge:** `MarkAllReadPacketTitleFormat` („{0} ungelesene Artikel vom {1}" / „{0} unread articles from {1}") und `UnreadNoDateLabel` („ohne Datum" / „no date") — letzteres als Tageslabel bei `IncludeUndated`-Paketen.
- `AppResources.Designer.cs` ist `<auto-generated>` und eingecheckt: nach den resx-Änderungen neu generieren (ResXFileCodeGenerator) bzw. die beiden Eigenschaften im gleichen generierten Stil ergänzen.

### `DelegatingItemRepository` (Test-Dekorator, `src/Reporter.Tests/DelegatingItemRepository.cs`)

- Neue Member ergänzen (`virtual`, delegierend) und die geänderte Signatur von `GetUnreadByDateAsync` übernehmen — sonst bricht die Kompilierung.

### Test-Doubles in `UnreadViewModelTests` (`src/Reporter.Tests/UnreadViewModelTests.cs`)

- `RecordingItemRepository` — Signatur-Anpassung + neues Aufzeichnungsfeld für das übergebene `packet`.
- `FailingItemRepository` — Signatur-Anpassung.

### `FeedDbAssertions` (E2E-Hilfsklasse, `src/Reporter.E2ETests/FeedDbAssertions.cs`)

- **Neue Hilfsmethoden:**
  - `ItemHasReadFlagAsync(databasePath, title, isRead)` — pollt `items.is_read` für einen Titel (Nachweis, dass nur Paket-Artikel gelesen wurden).
  - `SetUnreadSortOrderAsync(databasePath, value)` — schreibt `settings.unread_sort_order` (Spalte existiert, `HasMaxLength(50)`); macht den E2E-Test unabhängig von der Default-Sortierung `"desc"` und ermöglicht die Belegung **beider** Sortierrichtungen. Nutzt den bestehenden `ExecuteWriteWithRetryAsync`-Pfad.

## Datenbankmigrationen

Keine. Es wird weder ein neues Setting noch eine neue Spalte/Tabelle benötigt; `items.published_at` und `items.is_read` existieren bereits.

## Validierungsregeln

Keine. Es entstehen keine neuen Benutzereingaben; der Paket-Filter ist rein lesend/markierend auf vorhandenen Daten.

## Konfigurationsänderungen

Keine. Die 5er-Schwelle ist eine Konstante (`ItemRepository.MinPacketSize`); `UnreadSortOrder` bleibt unverändert, wird pro Ladevorgang gelesen und steuert die Paket-Richtung (Anwenderentscheidung 2026-11-12: Einstellung bleibt erhalten).

## Seiteneffekte und Risiken

- **Interface-Signaturänderung `GetUnreadByDateAsync`:** Alle `override`-Dekorateure (`DelegatingItemRepository`, `RecordingItemRepository`, `FailingItemRepository` sowie private Ableitungen in `FeedDetailViewModelTests`/`LaterViewModelTests`, sofern sie diesen Member überschreiben) müssen die neue Signatur übernehmen — sonst Compile-Fehler. Aufrufer (`KeywordFilterTests_E2E`, `FeedSyncServiceTests_TitleNormalization`, `UnreadViewModel`) bleiben quellkompatibel (optionaler Parameter).
- **Bestehender Test `MarkReadCommand_DecrementsUnreadCount`** seedet Artikel auf zwei verschiedenen Tagen und erwartet beide in `Articles`: Unter der bestätigten absteigenden Semantik (Anker = neuester Tag, 1 Artikel < 5 → Erweiterung auf den Folgetag) bleibt der Test grün; bei abweichendem Seed-Verhalten ist er anzupassen (Watch-Item, keine Umbauplanung nötig).
- **Kartenstapel-Navigation:** `GoToNextCard` triggert `LoadMoreCommand` ab Position `Count - 3` — am Paketende endet das Nachladen; die Kartenansicht endet dort ebenso wie die Liste (gewollt, Scope-Ausschluss 3).
- **Badge-/Chip-Zähler vs. Listeninhalt:** `UnreadBadgeText` und Chips zeigen weiterhin die Gesamtzahl — bewusste Inkonsistenz (Badge „42 ungelesen", Liste zeigt 7 Paket-Artikel), über den Dialog-Text abgefedert (bestätigt 2026-11-12). `CardPositionText` zeigt dagegen bewusst die **Paket**-Gesamtzahl („3 von 7"), weil der Kartenstapel am Paketende endet — bei Paketen >`PageSize` ist der Nenner zunächst größer als die geladene Kartenanzahl (analog zum Dialog, der ebenfalls die Gesamt-Paket-Anzahl nennt).
- **Mitgeführter `_packetUnreadCount`:** Der Zähler wird in `MarkReadAsync` dekrementiert, ohne die DB erneut zu befragen — konsistent, solange nur Artikel aus der paket-begrenzten `Articles`-Liste markiert werden (einziger UI-Pfad: `MarkReadCommand` über Swipe/Karten-Dismiss). Alle Pfade, die den Zähler zurücksetzen müssen (frisches Laden, Kategoriewechsel, Auto-Reload bei leerer Liste, „Alles gelesen"), enden in `LoadPageAsync(append: false)` und setzen ihn neu — abgesichert durch `MarkReadCommand_DecrementsPacketConfirmationText` und `CardPositionText_UsesPacketTotalAsDenominator`.
- **Zeitzonen-/Mitternachtsgrenze:** Ortszeit-Tagesgrenzen können UTC-Bereiche über Mitternacht hinaus verschieben; Tests müssen `PublishedAt` mit explizitem `DateTimeKind.Utc`/`DateTimeOffset.UtcDateTime` seeden oder die Erwartung über dieselbe `ToLocalTime`-Konvention bilden — `Kind.Unspecified`-Seeds werden sonst doppelt interpretiert.
- **Paket-Markierung bei teilweise geladener Liste:** Da `MarkUnreadPacketAsReadAsync` auf dem DB-Prädikat arbeitet, markiert sie auch Paket-Artikel jenseits der geladenen Seiten — gewollt (Anwenderentscheidung 2026-11-12), muss aber im Dialog-Text (`GetUnreadPacketCountAsync` = Gesamt-Paket-Anzahl) konsistent kommuniziert werden, sonst wundert sich der Nutzer über mehr markierte Artikel als sichtbar.
- **E2E-Suite teilt App-Instanz und DB:** Neue E2E-Tests müssen hinter sich aufräumen (`DeleteAllFeedsAsync`/`MarkAllItemsReadAsync`), damit andere Tests (z. B. `DemoSeedTests`, Feed-Detail-Paging) keine Fremdartikel in der Ungelesen-Liste vorfinden.

## Umsetzungsreihenfolge

1. **`UnreadPacket`-Record + `IItemRepository`-Erweiterung + Stubs**
   - Voraussetzungen: Keine.
   - Beschreibung: `UnreadPacket` anlegen; Interface-Member hinzufügen/Signatur ändern; `ItemRepository` mit kompilierenden Stubs versehen (`ResolveUnreadPacketAsync` → `null`/Minimalverhalten, Paket-Filter ignoriert, `MarkUnreadPacketAsReadAsync` → `Task.CompletedTask`, `GetUnreadPacketCountAsync` → `0`); `DelegatingItemRepository` und die überschreibenden Test-Doubles auf die neue Signatur bringen. Ergebnis: Solution kompiliert, Suite bleibt grün.
2. **Rote Repository-Tests schreiben**
   - Voraussetzungen: Schritt 1 (kompilierende Schnittstellen).
   - Beschreibung: Neue Tests in `ItemRepositoryTests` (siehe Tests) — sie schlagen gegen die Stubs fehl und belegen Tag-Grenzen, <5-Erweiterung, Pseudo-Tag, Kategoriefilter, beide Sortierrichtungen und paket-begrenztes Markieren (inkl. komplettem Paketbereich >`PageSize`).
3. **`ItemRepository` implementieren**
   - Voraussetzungen: Schritt 2 (rote Tests als Spezifikation).
   - Beschreibung: `ResolveUnreadPacketAsync` (sortierneutral), Paket-Prädikat in `GetUnreadByDateAsync`, `MarkUnreadPacketAsReadAsync`, `GetUnreadPacketCountAsync`, `MinPacketSize`, private Helfer für Tagesgrenzen/Folgetag. Ziel: neue Tests grün, Bestandstests unverändert grün.
4. **Test-Doubles erweitern + rote ViewModel-Tests schreiben**
   - Voraussetzungen: Schritt 3 (funktionierendes Repository).
   - Beschreibung: `RecordingItemRepository` um `packet`-Aufzeichnung erweitern; neue `UnreadViewModelTests` (Paket-Bildung, <5 nur bei frischem Laden, Paket-Markierung, Auto-Reload bei leerer Liste, `MarkAllReadConfirmationText`) — rot gegen das alte ViewModel-Verhalten.
5. **`UnreadViewModel` implementieren**
   - Voraussetzungen: Schritt 4.
   - Beschreibung: `_currentPacket`, `_packetUnreadCount`/`_packetDayLabel`, `MarkAllReadConfirmationText`-Getter, Paket-Auflösung im `append == false`-Pfad von `LoadPageAsync`, paket-begrenztes `MarkAllReadAsync` (Null-Guard → nur `LoadAsync`), `_packetUnreadCount`-Dekrement in `MarkReadAsync`, `CardPositionText`-Nenner auf Paket-Basis. Ziel: ViewModel-Tests grün.
6. **UI-Anbindung + Ressourcen**
   - Voraussetzungen: Schritt 5.
   - Beschreibung: `OnMarkAllReadTapped` auf `MarkAllReadConfirmationText` umstellen; resx-Einträge (neutral + `de`) ergänzen; `AppResources.Designer.cs` neu generieren.
7. **E2E-Test aufsetzen**
   - Voraussetzungen: Schritte 5–6; lauffähige FlaUI-Infrastruktur (`ReporterAppFixture`, `E2EPageHelpers`, `StubFeedServer` — im Repo vorhanden); gebaute `Reporter.exe` (Debug, `net10.0-windows10.0.19041.0`, win-x64) via `scripts/Run-E2ETests.ps1`.
   - Beschreibung: `day-packet-feed.xml`-Fixture, `FeedDbAssertions`-Helper (`ItemHasReadFlagAsync`, `SetUnreadSortOrderAsync`), `UnreadPacketTests` implementieren.
8. **Gesamt-Verifikation**
   - Voraussetzungen: Schritte 1–7.
   - Beschreibung: `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release` vollständig grün; `.\scripts\Run-StaticChecks.ps1` mit Exit-Code 0; `.\scripts\Run-E2ETests.ps1` in interaktiver Desktop-Session — ist die E2E-Ausführung im Laufzeitkontext nicht möglich, an den Anwender eskalieren (manuelle Abnahme `Bestanden (manuell)` oder dokumentierter `Verzicht`), nicht stillschweigend überspringen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `ResolveUnreadPacketAsync_ReturnsNull_WhenNoUnread` | `ItemRepositoryTests` | `null` bei leerer Ungelesen-Menge (und bei ausschließlich gelesenen Artikeln). |
| `ResolveUnreadPacketAsync_AnchorDayOnly_WhenDayHasEnough` | `ItemRepositoryTests` | Anker-Tag mit ≥5 Artikeln → Paket = genau dieser Tag (Grenzen = Orts-Tagesanfang/-ende), Folgetag nicht enthalten. Rot vor Implementierung. |
| `ResolveUnreadPacketAsync_ExtendsToNextDay_WhenBelowMin` | `ItemRepositoryTests` | Anker-Tag mit 4 Artikeln + Folgetag → Bereichsgrenze = Ende (asc) bzw. Beginn (desc) des Folgetags; Schwelle exakt: 5 → keine Erweiterung, 4 → Erweiterung (Randwerte). `Days` enthält bei Erweiterung beide Kalendertage (Anker + Folgetag), sonst nur den Anker-Tag. |
| `ResolveUnreadPacketAsync_NextDaySkipsEmptyCalendarDays` | `ItemRepositoryTests` | Anker <5, nächster Kalendertag leer, übernächster belegt → Paket reicht bis zum nächsten **belegten** Tag (bestätigt 2026-11-12). |
| `ResolveUnreadPacketAsync_UndatedAnchor_FormsPseudoDay` | `ItemRepositoryTests` | Undatierte + datierte Artikel (aufsteigend): `IncludeUndated == true` (Pseudo-Tag = erstes Paket), Bereich deckt den Folgetag nur bei <5 undatierten Artikeln ab. |
| `ResolveUnreadPacketAsync_UndatedPseudoDay_InDescending_IsLast` | `ItemRepositoryTests` | Absteigend: undatierte Artikel bilden das **letzte** Paket — sie kommen nur zum Zug, wenn alle datierten Pakete abgearbeitet sind (bzw. als Folgetag eines datierten <5-Ankers mit `IncludeUndated`). |
| `ResolveUnreadPacketAsync_RespectsCategoryFilter` | `ItemRepositoryTests` | Anker-Ermittlung und Zählung nur innerhalb der gewählten Kategorie (schließt zugleich die dokumentierte Testlücke „kein dedizierter Kategoriefilter-Test"). |
| `ResolveUnreadPacketAsync_Descending_AnchorsOnNewestDay` | `ItemRepositoryTests` | Absteigende Sortierung: Anker = neuester belegter Tag (i. d. R. heutiger Tag), Folgetag = nächster älterer belegter Tag in Sortierrichtung (bestätigt 2026-11-12). |
| `ResolveUnreadPacketAsync_Ascending_AnchorsOnOldestDay` | `ItemRepositoryTests` | Aufsteigende Sortierung: Anker = ältester belegter Tag (Gegenstück zum desc-Fall; belegt die Symmetrie beider Richtungen). |
| `GetUnreadByDateAsync_WithPacket_ReturnsOnlyPacketItems` | `ItemRepositoryTests` | Tagesgrenze exakt: Artikel um 23:59 des Anker-Tags enthalten, 00:00 des Folgetags nur bei Erweiterung; Paging innerhalb des Pakets funktioniert. |
| `GetUnreadByDateAsync_WithoutPacket_Unchanged` | `ItemRepositoryTests` | `packet: null` = bisheriges unbegrenztes Verhalten (Regressionsschutz). |
| `MarkUnreadPacketAsReadAsync_MarksOnlyPacketItems` | `ItemRepositoryTests` | Nur Paket-Artikel werden `IsRead = true`/`ReadAt` gesetzt; Artikel anderer Tage bleiben ungelesen (schließt zugleich die dokumentierte Testlücke „kein dedizierter Massenmarkier-Test"). |
| `MarkUnreadPacketAsReadAsync_MarksEntirePacket_BeyondLoadedPage` | `ItemRepositoryTests` | Paket-Tag mit >`PageSize` Artikeln: `MarkUnreadPacketAsReadAsync` markiert **alle** Paket-Artikel, nicht nur eine Seite — Negativfall: Markierung nur der geladenen Listenelemente ist falsch (Anwenderentscheidung 2026-11-12). |
| `MarkUnreadPacketAsReadAsync_RespectsCategoryAndUndated` | `ItemRepositoryTests` | Kategoriefilter und `IncludeUndated`-Prädikat beim Markieren. |
| `GetUnreadPacketCountAsync_ReturnsPacketCount` | `ItemRepositoryTests` | Zählt den **gesamten** Paket-Umfang inkl. Folgetag-Erweiterung (nicht die geladenen Seiten). |
| `LoadCommand_LoadsOnlyAnchorDayArticles` | `UnreadViewModelTests` | Zwei Tage à ≥5 Artikel → Liste enthält nur Anker-Tag-Artikel. |
| `LoadCommand_ExtendsPacket_WhenAnchorBelowMin` | `UnreadViewModelTests` | Tag1: 3 + Tag2: 4 → Liste zeigt 7 Artikel beider Tage. |
| `MarkReadCommand_Removal_DoesNotExtendPacket` | `UnreadViewModelTests` | Tag1: 6, Tag2: 4 → nach 2× `MarkReadCommand` bleiben 4 Artikel, **kein** Tag2-Artikel rückt nach (Negative-Test zum Kernkriterium „Entscheidung nur beim frischen Laden"). |
| `MarkReadCommand_EmptyingList_LoadsNextPacket` | `UnreadViewModelTests` | Letzter sichtbarer Artikel wird als gelesen markiert → `LoadAsync` lädt frisch → nächstes Paket wird angezeigt, neue Paket-Entscheidung erlaubt (Anforderung laut Anwenderentscheidung 2026-11-12). |
| `MarkAllReadCommand_MarksOnlyCurrentPacket` | `UnreadViewModelTests` | Tag1 ≥5 + Tag2 → nach `MarkAllReadCommand`: Tag1-Artikel in der DB gelesen, Tag2-Artikel ungelesen und nun in `Articles` (ersetzt `MarkAllReadCommand_ClearsArticles` als Kern-Nachweis). |
| `MarkAllReadCommand_WithExtendedPacket_MarksWholePacket` | `UnreadViewModelTests` | Erweitertes Paket (3+4) → alle 7 markiert (bestätigt 2026-11-12: gesamtes theoretisch sichtbares Paket). |
| `SelectCategoryCommand_ResolvesNewPacket` | `UnreadViewModelTests` | Kategoriewechsel ermittelt das Paket neu (Anker innerhalb der Kategorie). |
| `LoadMoreCommand_PagesWithinPacket_StopsAtEnd` | `UnreadViewModelTests` | Anker-Tag mit >`PageSize` Artikeln → `LoadMoreCommand` liefert Rest des Tages; danach `HasMore == false`, Folgetag kommt nicht. Randfall mitprüfen: Endet das Paket exakt auf einer vollen Seite (`items.Count == PageSize`), bleibt `HasMore` zunächst `true` und erst ein weiteres `LoadMore` (leere Seite) setzt `false` — bestehendes Verhalten, im Test entweder als zweite Seed-Variante belegen oder zumindest dokumentieren. |
| `MarkAllReadConfirmationText_ReflectsPacketScope` | `UnreadViewModelTests` | Dialog-Text enthält die Gesamt-Paket-Anzahl (inkl. nicht geladener Paket-Artikel) und das Tageslabel (bestätigt 2026-11-12). |
| `MarkReadCommand_DecrementsPacketConfirmationText` | `UnreadViewModelTests` | Paket mit 7 Artikeln → 2× `MarkReadCommand` → `MarkAllReadConfirmationText` enthält „5" statt „7", Tageslabel unverändert; zusätzlich PropertyChanged-Benachrichtigung prüfbar. Belegt die Konsistenz des Dialog-Texts nach einzelnem Als-gelesen-Markieren (kein veralteter/stale Wert). |
| `CardPositionText_UsesPacketTotalAsDenominator` | `UnreadViewModelTests` | 42 ungelesene Artikel über mehrere Tage, Paket = 7 → `CardPositionText` zeigt „… von 7" (Paket-Gesamtzahl), nicht „… von 42"; nach `MarkReadCommand` → „… von 6" (synchroner Dekrement). Negative-Test: Gesamt-Ungelesenzahl als Nenner ist falsch. |
| `MarkAllReadConfirmationText_ComposesDayLabel` | `UnreadViewModelTests` | Label-Komposition aus `UnreadPacket.Days`: erweitertes Paket (3+4 über zwei Tage) → Text enthält beide Kurzdaten mit „ + "; rein undatiertes Paket → `UnreadNoDateLabel`; undatierter Anker + datierter Folgetag (asc) → „ohne Datum + <Datum>". |
| `LastPacket`-Aufzeichnung | `RecordingItemRepository` (Double) | Zeichnet das zuletzt übergebene `UnreadPacket` auf — Basis für VM-Assertions. |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `DelegatingItemRepository`, `RecordingItemRepository`, `FailingItemRepository` (sowie private Ableitungen in `FeedDetailViewModelTests`/`LaterViewModelTests`, falls sie `GetUnreadByDateAsync` überschreiben) | Geänderte Interface-Signatur → `override`-Signaturen anpassen. |
| `LoadPage_PassesSortOrderFromSettings`, `LoadPage_InvalidSortOrder_UsesDescending` (`UnreadViewModelTests`) | Funktion bleibt gleich; durch Signaturänderung mitziehen und ggf. `LastPacket` mitprüfen (sortierneutrale Paket-Ermittlung respektiert die Einstellung). |
| `MarkReadCommand_DecrementsUnreadCount` (`UnreadViewModelTests`) | Seedet zwei Tage à 1 Artikel — unter der bestätigten desc-Semantik bleibt er grün; ggf. Seed anpassen (gleicher Tag), falls Erwartungen brechen. |
| `MarkAllReadCommand_ClearsArticles` (`UnreadViewModelTests`) | Bleibt gültig (ein Tag ⇒ Paket = dieser Tag), wird aber durch `MarkAllReadCommand_MarksOnlyCurrentPacket` als eigentlicher Paket-Nachweis ergänzt — keine Anpassung, nur Beobachtung. |
| `CardPositionText_ReflectsPositionAndTotal` (`UnreadViewModelTests_ListState`) | Nenner wird Paket-basiert: Numerisch bleibt die Erwartung gleich, solange die 3 Seed-Artikel am selben Tag liegen und das Test-Double `GetUnreadPacketCountAsync` mit `3` beantwortet; das Double muss den neuen Member liefern können (z. B. `packet`-gefilterte Zählung oder Stub-Rückgabe). Bei Seeds über mehrere Tage Erwartung auf den Paket-Nenner umstellen. |

### E2E-Tests (primärer Funktionsnachweis)

Infrastruktur: FlaUI/UIA3 (`src/Reporter.E2ETests`, `ReporterAppFixture` mit isolierter `REPORTER_DB_PATH`-SQLite-DB, `StubFeedServer` für RSS-Fixtures, `E2EPageHelpers` für Tabs/Karten/ActionSheets, `FeedDbAssertions` für DB-Nachweise). Die Suite läuft **nicht** in der CI und benötigt eine interaktive Windows-Desktop-Session — Ausführung über `scripts/Run-E2ETests.ps1`.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Happy Path (absteigend, Default): `day-packet-feed` (6+3+3 über 3 Tage, neuester Tag belegt) via UI hinzufügen → Unread-Tab zeigt nur Artikel des neuesten Tags → Doppelhaken tippen → ActionSheet mit Paket-Umfang bestätigen → neuester Tag in DB gelesen, ältere Tage ungelesen → Liste lädt das nächste Paket | `src/Reporter.E2ETests/UnreadPacketTests.cs` | Tag-begrenzte Anzeige + paket-begrenztes „Alles gelesen" inkl. automatischem Folgepaket in der Default-Sortierung | Einziger Test, der den kompletten Benutzerfluss vom UI-Tap bis zur sichtbaren Listenänderung und DB-Wirkung nachweist; VM-Tests decken die UI-Interaktion (Tap, ActionSheet) nicht ab. |
| Pflicht | Aufsteigende Sortierung: `SetUnreadSortOrderAsync("asc")` → gleicher Feed → Unread-Tab zeigt das **älteste** Tagespaket (Anker = erster Listeneintrag in asc) → Doppelhaken → nur dieses Paket gelesen | `src/Reporter.E2ETests/UnreadPacketTests.cs` | Sortierneutralität der Paket-Logik (Anwenderentscheidung 2026-11-12): asc → ältester Tag zuerst | Der Richtungswechsel ist ein über die Settings erreichbarer Benutzerfluss, dessen sichtbare Wirkung (welcher Tag zuerst erscheint) nur End-to-End über UI + DB-Setup belegt werden kann. |
| Soll | Kein Folgetag-Nachrücken beim einzelnen Als-gelesen-Markieren: `paged-feed` (1 Artikel/Tag; Paket = Tag1+Tag2) → einen Artikel per Swipe als gelesen markieren → verbleibender Tag2-Artikel sichtbar, Tag3-Artikel **nicht** in der Liste | `src/Reporter.E2ETests/UnreadPacketTests.cs` | „<5-Entscheidung nur beim frischen Laden" | Der Effekt (kein Nachrücken) ist nur über den echten UI-Entfernungspfad (`SwipeView` → `MarkReadCommand`) belegbar; VM-Test ergänzt, ersetzt ihn nicht. Fallback: `SwipeView`-Gesten sind per FlaUI-Maus-Drag möglich, aber potenziell instabil — ist der Test nicht zuverlässig automatisierbar, reale Abnahme dokumentieren (`Bestanden (manuell)`/`Verzichtet`), nicht still weglassen. |

Welche bestehenden E2E-Tests müssen angepasst werden?

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| Keine | Die neuen Artikel entstehen in der isolierten Suite-DB nur innerhalb der neuen Tests und werden per `DeleteAllFeedsAsync`/`MarkAllItemsReadAsync` aufgeräumt; bestehende Tests seeden/asserten ihre eigenen Feeds. |

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path (aufsteigend): Paket anzeigen und als gelesen bestätigen | Nutzer öffnet die App nach mehrtägiger Pause (`OnAppearing` → `LoadCommand`) | 8 ungelesene Artikel: 5 an Tag 1 (ältester Tag), 3 an Tag 2; `UnreadSortOrder = "asc"`; keine Paket-Objekte vorab vorhanden (werden beim Laden ermittelt) | Unread-Tab öffnet → `ResolveUnreadPacketAsync` → Liste zeigt genau die 5 Artikel des Tag-1-Pakets → Doppelhaken → ActionSheet mit Paket-Umfang → `ButtonMarkAllRead` → `MarkUnreadPacketAsReadAsync` → `LoadAsync` | Liste zeigt nur Tag-1-Artikel; nach Bestätigung sind diese gelesen und die Liste zeigt automatisch das Tag-2-Paket | E2E `UnreadPacketTests` (asc) + VM-/Repo-Tests; reale Abnahme durch App-Lauf |
| 2 | Happy Path (absteigend, Default): neuester Tag zuerst | `OnAppearing` → `LoadCommand` | Ungelesene Artikel über 3 Tage, inkl. heutigem/neuestem Tag; `UnreadSortOrder = "desc"` | Laden → Anker = neuester belegter Tag → Liste zeigt nur dessen Paket | Erste Liste zeigt i. d. R. die Artikel des aktuellen Tages; ältere Tage erscheinen erst als Folgepakete | E2E `UnreadPacketTests` (desc Happy Path) + `ResolveUnreadPacketAsync_Descending_AnchorsOnNewestDay` |
| 3 | <5-Erweiterung beim frischen Laden | Pull-to-Refresh (`RefreshCommand`) oder `OnAppearing` | Tag 1: 3 ungelesene Artikel, Tag 2 (nächster belegter Tag in Sortierrichtung): 4 | Frisches Laden → Anker = Tag 1, 3 < `MinPacketSize` → Erweiterung auf Tag 2 | Liste zeigt alle 7 Artikel | `LoadCommand_ExtendsPacket_WhenAnchorBelowMin`, `ResolveUnreadPacketAsync_ExtendsToNextDay_WhenBelowMin` |
| 4 | Kein Nachrücken beim einzelnen Entfernen | Nutzer markiert Artikel einzeln als gelesen (`MarkReadCommand`, Swipe) | Geladenes Paket: Tag 1 mit 6 Artikeln; Tag 2 mit 4 | 2 Artikel per Swipe als gelesen markieren → danach Doppelhaken tippen | Liste schrumpft auf 4 Artikel; **kein** Tag-2-Artikel erscheint; der ActionSheet-Titel zeigt „4 …" statt der ursprünglichen „6 …" (kein veralteter Paket-Zähler) | `MarkReadCommand_Removal_DoesNotExtendPacket`, `MarkReadCommand_DecrementsPacketConfirmationText` (Unit), E2E „Soll" |
| 5 | Auto-Reload beim Wegfall des letzten sichtbaren Artikels | Nutzer markiert den letzten verbleibenden Artikel der Liste als gelesen | Paket auf einen einzigen sichtbaren ungelesenen Artikel geschrumpft; weitere ungelesene Artikel (Folgetage) existieren | Artikel per Swipe als gelesen markieren → `Articles.Count == 0` → `LoadAsync` → `ResolveUnreadPacketAsync` | Liste lädt automatisch frisch und zeigt das nächste Paket (neue Paket-Entscheidung erlaubt) — kein manueller Refresh nötig | `MarkReadCommand_EmptyingList_LoadsNextPacket`; ggf. E2E-Swipe-Pfad (Fallback: dokumentierte manuelle Abnahme) |
| 6 | Paketende beim Nachladen | `LoadMoreCommand` am Listenende / Kartenstapel-Schwelle | Anker-Tag mit 25 Artikeln (> `PageSize`), Folgetag belegt | Scrollen → `LoadMoreAsync` liefert Rest des Tages | Liste endet nach 25 Artikeln (`HasMore = false`); Folgetag erscheint erst nach frischem Laden/„Alles gelesen" | `LoadMoreCommand_PagesWithinPacket_StopsAtEnd` |
| 7 | „Alles gelesen" auf teilweise geladenes Paket | Doppelhaken, obwohl das Paket mehr Artikel enthält als geladen | Anker-Tag mit 25 ungelesenen Artikeln; Nutzer hat nur die erste Seite (20) geladen/gesehen | Doppelhaken → ActionSheet zeigt „25 ungelesene Artikel vom <Tag>" → bestätigen | **Alle 25** Paket-Artikel sind in der DB gelesen — nicht nur die 20 geladenen; Liste lädt das Folgepaket | `MarkUnreadPacketAsReadAsync_MarksEntirePacket_BeyondLoadedPage` + `MarkAllReadConfirmationText_ReflectsPacketScope`; reale Abnahme |
| 8 | Leere Liste nach „Alles gelesen" | `MarkAllReadCommand` auf dem letzten vorhandenen Paket | Nur ein Tag mit ungelesenen Artikeln | Bestätigen → markieren → `LoadAsync` → `ResolveUnreadPacketAsync` → `null` | Liste zeigt den bestehenden Leerzustand (`ShowEmptyState`), kein Fehler | `MarkAllReadCommand_ClearsArticles` + Repo-Test `ResolveUnreadPacketAsync_ReturnsNull_WhenNoUnread` |
| 9 | Undatierte Artikel (Pseudo-Tag) | Frisches Laden bei `PublishedAt == null`-Artikeln | 2 undatierte + 6 datierte Artikel (aufsteigend) | Laden → Anker = Pseudo-Tag (2 < 5) → Erweiterung auf ältesten datierten Tag | Liste zeigt die 2 undatierten + die 6 Artikel des ersten datierten Tages; absteigend wäre der Pseudo-Tag das letzte Paket | `ResolveUnreadPacketAsync_UndatedAnchor_FormsPseudoDay`, `ResolveUnreadPacketAsync_UndatedPseudoDay_InDescending_IsLast` |
| 10 | Kategoriefilter | Nutzer tippt einen Kategorie-Chip (`SelectCategoryCommand`) | Ungelesene Artikel in zwei Kategorien auf verschiedenen Tagen | Chip wählen → `SelectCategoryAsync` → `LoadPageAsync(0)` → neue Paket-Ermittlung innerhalb der Kategorie | Liste zeigt nur das Paket der gewählten Kategorie; Chip-Count bleibt Gesamtzahl der Kategorie | `SelectCategoryCommand_ResolvesNewPacket`, `ResolveUnreadPacketAsync_RespectsCategoryFilter` |
| 11 | Abbruch der Bestätigung | Doppelhaken → ActionSheet → `ButtonCancel` | Beliebiges geladenes Paket | Cancel wählen | Keine Markierung, keine Listenänderung | Bestehendes Verhalten; implizit im E2E-Flow abgesichert (`DismissPopups`-Pfad) |

## Offene Punkte

Keine. Alle ursprünglichen offenen Punkte wurden durch den Anwender entschieden (Issue-Kommentar, 2026-11-12) und sind in diesem Plan eingearbeitet: sortierneutrale Paket-Logik (Antwort 1), Auto-Reload beim Wegfall des letzten Artikels (Antwort 2), Pseudo-Tag für `PublishedAt == null` (Antwort 3), Markierung des gesamten theoretisch sichtbaren Pakets (Antwort 4), lokale Gerätezeit für Kalendertage (Antwort 5), Paket-Umfang im Bestätigungsdialog (Antwort 6), Folgetag = nächster belegter Tag (Antwort 7).
