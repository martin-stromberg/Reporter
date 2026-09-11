# Umsetzungsplan: „Für später bewahren“-Funktion und separate Ansicht (Issue #25)

## Übersicht

Der Großteil des Features existiert bereits: `IsSavedForLater` ist im Datenmodell, `IItemRepository.GetSavedForLaterAsync`/`ToggleSavedForLaterAsync` sind implementiert, `LaterPage` + `LaterViewModel` + Tab-Registrierung sowie die Toggles in `ArticleCardView` (Liste) und `ArticleDetailPage` (Detail) sind vorhanden. Das Arbeitspaket umfasst zwei echte Lücken: (1) die automatische Löschlogik auf Basis von `Settings.RetentionDays` fehlt vollständig — sie wird neu eingeführt und muss bewahrte Artikel zwingend ausnehmen (`!IsSavedForLater` als strukturelle Löschbedingung); (2) fehlende Tests, insbesondere `LaterViewModelTests` und der Nachweis der Lösch-Invariante. Ergänzend erfolgt die vorgeschriebene Mobile-UI-Design-Review der `LaterPage` gegen den Design-Draft.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Retention-Löschung: Ort der Löschabfrage | Neue Methode `DeleteExpiredAsync(DateTime cutoff)` auf `IItemRepository`, implementiert in `ItemRepository` per `ExecuteDeleteAsync` mit festem Filter `!IsSavedForLater` | Repository-Muster: Die Schutz-Invariante „bewahrte Artikel werden nie automatisch gelöscht" wird strukturell in der einzigen automatischen Löschmethode verankert — sie kann nicht versehentlich umgangen werden. `ExecuteDeleteAsync` entspricht dem vorhandenen `ExecuteUpdateAsync`-Muster in `MarkAllAsReadAsync`. |
| Retention-Löschung: Orchestrierung | Neuer Service `RetentionCleanupService` (Interface `IRetentionCleanupService`) in `Reporter.Core`, der `ISettingsRepository.GetAsync().RetentionDays` liest, den Stichtag berechnet und `DeleteExpiredAsync` aufruft | Service-Layer-Ansatz: Die Orchestrierung (Settings lesen → Stichtag → Löschen) bleibt in der testbaren `Reporter.Core`-Assembly; `App.OnStart` (nicht testbar, MAUI-Assembly) wird nur Aufrufpunkt. |
| Retention-Löschung: Aufrufpunkt | `App.OnStart` nach `Database.MigrateAsync()`, Service-Aufruf fehlerisoliert (Exception darf den App-Start nicht verhindern) | App-Start ist der einzige zuverlässige, nutzerunabhängige Auslösepunkt — Sync (`FeedSyncService.SyncAllAsync`) ist nutzerinitiiert und kann ausfallen. Bereits vorhandenes Muster: `OnStart` führt Startup-Arbeiten (`MigrateAsync`) in eigenem DI-Scope aus. |
| Löschbedingung (Zeitraum-Bezug) | Löschkandidaten: `IsRead && !IsSavedForLater && (ReadAt ?? PublishedAt) < cutoff` | Geklärte Entscheidung: Die Aufbewahrungsfrist gilt nur für gelesene Artikel; Stichtag ist `ReadAt ?? PublishedAt`. Passt zum Design-Draft (`einstellungen_filter`), der `RetentionDays` als „Gelesene Artikel aufbewahren" beschriftet; `ReadAt` entspricht dem Aufbewahrungs-Begriff (Frist ab Lektüre), `PublishedAt` ist der Fallback für Artikel ohne Lesezeitpunkt. |
| Sortierung der Später-Liste | Bestehende Sortierung `OrderByDescending(i => i.PublishedAt)` beibehalten, kein `SavedAt`-Feld | Geklärte Entscheidung: Die Anforderung verlangt „Speicherdatum **oder** Veröffentlichungsdatum" — die Oder-Variante ist über `PublishedAt` erfüllt; es wird kein `SavedAt`-Feld und keine Migration ergänzt. |
| Feed-Löschkaskade | `DeleteBehavior.Cascade` (`Item → Feed`) bleibt unverändert; bewahrte Artikel werden beim expliziten Feed-Löschen mitgelöscht | Geklärte Entscheidung: Das Feed-Löschen ist eine bestätigte Nutzeraktion, keine „automatische Löschung" im Sinne der Anforderung. Das Verhalten wird durch den Test `FeedRepositoryTests.DeleteAsync_CascadeDeletesSavedItems` dokumentiert. |

## Programmabläufe

### Bewahrung umschalten (Liste, vorhanden — verifizieren)

1. Nutzer tippt auf das Bookmark-Icon (44 × 44 pt `Border` mit `TapGestureRecognizer`) einer `ArticleCardView` auf `UnreadPage` oder `LaterPage`.
2. Das von der Seite injizierte `ToggleSavedCommand` (`UnreadViewModel.ToggleSavedCommand` bzw. `LaterViewModel.ToggleSavedCommand`) wird mit dem `ItemListItem` ausgeführt.
3. `IItemRepository.ToggleSavedForLaterAsync(item.Id)` invertiert `IsSavedForLater` in der Datenbank.
4. `UnreadViewModel` ersetzt das Element in `Articles` in-place mit invertiertem Flag (Bookmark-Icon aktualisiert sich via `DataTrigger`); `LaterViewModel` lädt `SavedItems` neu, wodurch nicht mehr bewahrte Artikel aus der Ansicht verschwinden.

Beteiligte Klassen/Komponenten: `ArticleCardView`, `UnreadViewModel`, `LaterViewModel`, `IItemRepository`/`ItemRepository`

### Bewahrung umschalten (Detailansicht, vorhanden — verifizieren)

1. Nutzer tippt auf den Bookmark-`Border` in der Bottom-Action-Bar der `ArticleDetailPage`.
2. `ArticleDetailViewModel.ToggleSavedForLaterCommand` ruft `ToggleSavedForLaterAsync(Item.Id)` und ersetzt `Item` via `CreateItemCopy` mit invertiertem `IsSavedForLater`.
3. `BookmarkButtonLabel` und `DataTrigger` (Gold-Fill) aktualisieren sich über `PropertyChanged`.

Beteiligte Klassen/Komponenten: `ArticleDetailPage`, `ArticleDetailViewModel`, `IItemRepository`

### Später-Ansicht anzeigen (vorhanden — verifizieren)

1. Nutzer öffnet den Tab „Später" (`AppShell` → `LaterPage`).
2. `LaterPage.OnAppearing` führt `LaterViewModel.LoadCommand` aus.
3. `GetSavedForLaterAsync` liefert nur `IsSavedForLater == true` Artikel, `PublishedAt` absteigend, inkl. `FeedTitle`/`CategoryName`/`ImageUrl`/`Summary`.
4. `CollectionView` zeigt `ArticleCardView`-Karten; leere Liste → `EmptyView` (`AppResources.PlaceholderLater`).

Beteiligte Klassen/Komponenten: `AppShell`, `LaterPage`, `LaterViewModel`, `IItemRepository`

### Retention-Cleanup beim App-Start (neu)

1. `App.OnStart` führt `Database.MigrateAsync()` im DI-Scope aus (vorhanden).
2. Anschließend wird `IRetentionCleanupService` aus demselben Scope aufgelöst und `CleanupAsync` aufgerufen; der Aufruf ist fehlerisoliert (try/catch — ein Cleanup-Fehler darf den Start nicht blockieren).
3. `RetentionCleanupService.CleanupAsync` lädt die Singleton-Einstellungen via `ISettingsRepository.GetAsync()`.
4. Bei `RetentionDays <= 0` wird der Cleanup übersprungen (Validierungsregel, siehe unten).
5. Stichtag `cutoff = DateTime.UtcNow.AddDays(-settings.RetentionDays)` wird berechnet.
6. `IItemRepository.DeleteExpiredAsync(cutoff)` führt `ExecuteDeleteAsync` mit der Bedingung `IsRead && !IsSavedForLater && (ReadAt ?? PublishedAt) < cutoff` aus; Artikel ohne beide Zeitstempel bleiben erhalten (NULL-Vergleich trifft nicht). Rückgabewert: Anzahl gelöschter Artikel.
7. Bewahrte Artikel (`IsSavedForLater == true`) können per Konstruktion nicht gelöscht werden — die Bedingung ist Teil des Repository-Vertrags.

Beteiligte Klassen/Komponenten: `App`, `IRetentionCleanupService`/`RetentionCleanupService`, `ISettingsRepository`/`SettingsRepository`, `IItemRepository`/`ItemRepository`, `Settings`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `IRetentionCleanupService` | Interface (`src/Reporter.Core/Interfaces/IRetentionCleanupService.cs`) | Contract `Task<int> CleanupAsync(CancellationToken cancellationToken = default)` — führt die Retention-Löschung aus, liefert Anzahl gelöschter Artikel |
| `RetentionCleanupService` | Klasse (`src/Reporter.Core/Services/RetentionCleanupService.cs`) | Implementiert `IRetentionCleanupService`; liest `ISettingsRepository`, berechnet Stichtag, ruft `IItemRepository.DeleteExpiredAsync` |
| `LaterViewModelTests` | Testklasse (`src/Reporter.Tests/LaterViewModelTests.cs`) | Tests für `LaterViewModel` nach Muster `UnreadViewModelTests` (echte Repositories auf `TestDbContextFactory`) |
| `RetentionCleanupServiceTests` | Testklasse (`src/Reporter.Tests/RetentionCleanupServiceTests.cs`) | Tests für `RetentionCleanupService` inkl. Lösch-Invariante für bewahrte Artikel |

## Änderungen an bestehenden Klassen

### `IItemRepository` (Interface)

- **Neue Methoden:** `DeleteExpiredAsync(DateTime cutoff)` — löscht alle abgelaufenen Artikel; Contract-Dokumentation legt fest: nur `IsRead == true`, niemals `IsSavedForLater == true`, Zeitvergleich auf `ReadAt ?? PublishedAt`; Rückgabewert `Task<int>` (Anzahl gelöschter Items). Die XML-Doku von `DeleteAsync` wird ergänzt: die Methode ist für explizite Einzellöschungen vorgesehen und unterliegt nicht der Bewahrungs-Invariante.

### `ItemRepository` (Klasse)

- **Neue Methoden:** `DeleteExpiredAsync(DateTime cutoff)` — kurzlebiger Context via `_factory.CreateDbContextAsync()`, `context.Items.Where(i => i.IsRead && !i.IsSavedForLater && (i.ReadAt ?? i.PublishedAt) < cutoff).ExecuteDeleteAsync()`; Rückgabe der gelöschten Zeilenzahl.

### `App` (Klasse, `App.xaml.cs`)

- **Geänderte Methoden:** `OnStart` — nach `MigrateAsync()` wird `IRetentionCleanupService` aus dem vorhandenen Scope aufgelöst und `CleanupAsync` fehlerisoliert aufgerufen (try/catch, keine UI-Rückmeldung).

### `MauiProgram` (Klasse)

- **Änderung:** DI-Registrierung `.AddSingleton<IRetentionCleanupService, RetentionCleanupService>()` in der bestehenden Service-Kette (Reihenfolge analog zu `IFeedSyncService`).

## Datenbankmigrationen

Keine. `is_saved_for_later` existiert seit `InitialCreate`; ein `SavedAt`-Feld wird nicht ergänzt (Sortierung über `PublishedAt`, siehe Designentscheidungen).

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `Settings.RetentionDays` | Wert `<= 0` → Cleanup wird übersprungen | Ohne Regel würde `cutoff >= UtcNow` sämtliche gelesenen Artikel löschen |
| `DeleteExpiredAsync`-Kandidaten | `ReadAt` **und** `PublishedAt` sind `null` → Artikel wird nicht gelöscht | NULL-Vergleich in SQL trifft nicht — automatisch abgedeckt, in Test verifizieren |

## Konfigurationsänderungen

Keine. Es wird die bestehende Einstellung `Settings.RetentionDays` (Singleton, Default 30) ausgewertet; die `IsSavedForLater`-Ausnahme ist nicht konfigurierbar.

## Seiteneffekte und Risiken

- **Datenverlust durch Retention-Löschung (gewollt):** Ab Einführung werden gelesene Artikel älter als `RetentionDays` beim App-Start tatsächlich gelöscht. Das ist die geforderte Funktion, aber eine Verhaltensänderung — Nutzer verlieren alte gelesene Artikel ohne Rückfrage. Bewahrte und ungelesene Artikel bleiben erhalten (geklärte Auslegung: Frist gilt nur für gelesene Artikel).
- **`App.OnStart` ist `async void`:** Der Cleanup-Aufruf muss fehlerisoliert werden; eine unbehandelte Exception würde den App-Start gefährden. Außerdem: `OnStart` blockiert nicht — Cleanup läuft parallel zum ersten Render.
- **`IItemRepository.DeleteAsync` bleibt ungeschützt:** Die Einzellöschmethode prüft `IsSavedForLater` nicht (kein produktiver Aufrufer). Risiko bleibt dokumentiert; Contract-Klarstellung via XML-Doku.
- **Feed-Kaskade:** `FeedsViewModel.DeleteAsync` → `DeleteBehavior.Cascade` löscht weiterhin bewahrte Artikel des Feeds (geklärt: bestätigte Nutzeraktion, keine automatische Löschung; dokumentiert durch `FeedRepositoryTests.DeleteAsync_CascadeDeletesSavedItems`).
- **`FeedsViewModelTests`/`FeedSyncServiceTests` unberührt:** Sync fügt weiterhin nur hinzu; Cleanup ist entkoppelt vom Sync-Pfad.
- **`ArticleDetailViewModel`-Fallback:** Der dort hartkodierte `RetentionDays = 30`-Fallback (Zeile 241) bleibt funktional unverändert, hat keine Beziehung zur Löschlogik.

## Umsetzungsreihenfolge

1. **`IItemRepository.DeleteExpiredAsync` deklarieren und in `ItemRepository` implementieren**
   - Voraussetzungen: Keine — `ExecuteDeleteAsync` ist EF Core (bereits via `Microsoft.EntityFrameworkCore` verfügbar, `ExecuteUpdateAsync`-Muster in `MarkAllAsReadAsync` vorhanden).
   - Beschreibung: Interface-Methode mit Contract-Doku (Invariante `!IsSavedForLater`, `IsRead`-Bedingung, `ReadAt ?? PublishedAt`-Stichtag); Implementierung per `ExecuteDeleteAsync`; XML-Doku von `DeleteAsync` präzisieren.
2. **`IRetentionCleanupService` und `RetentionCleanupService` anlegen**
   - Voraussetzungen: Schritt 1; vorhandene Interfaces `ISettingsRepository` (`GetAsync`) und `IItemRepository` — beide im Repo.
   - Beschreibung: Interface in `src/Reporter.Core/Interfaces/`, Implementierung in `src/Reporter.Core/Services/`; liest Settings, prüft `RetentionDays > 0`, berechnet Stichtag, delegiert an `DeleteExpiredAsync`.
3. **DI-Registrierung und `App.OnStart`-Anbindung**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: `.AddSingleton<IRetentionCleanupService, RetentionCleanupService>()` in `MauiProgram`; in `App.OnStart` nach `MigrateAsync` Service auflösen und `CleanupAsync` fehlerisoliert aufrufen.
4. **Unit-Tests schreiben**
   - Voraussetzungen: Schritte 1–3; Testinfrastruktur (`TestDbContextFactory`, xUnit, In-Memory-SQLite) ist im Repo vorhanden.
   - Beschreibung: `LaterViewModelTests` und `RetentionCleanupServiceTests` neu anlegen; `ItemRepositoryTests` und `UnreadViewModelTests` um die in „Tests" gelisteten Methoden ergänzen.
5. **Mobile-UI-Design-Review und manuelle Verifikation der `LaterPage`**
   - Voraussetzungen: Schritte 1–3 (damit der produktive Stand getestet wird); Design-Drafts unter `design-draft/stitch_local_rss_feed_reader/f_r_sp_ter_bewahren/` und `..._dark_mode/` sind im Repo vorhanden.
   - Beschreibung: `LaterPage` (und Bookmark-Toggles auf `UnreadPage`/`ArticleDetailPage`) im Windows-Fenster 390 × 844 pt gegen `screen.png` vergleichen; AGENTS.md-Regeln prüfen (keine horizontalen Tabellen, Touch-Targets ≥ 44 × 44 pt, `AppThemeBinding`, `CollectionView` füllt `Grid`-Row `*`, kein verschachteltes Scrollen); Screenshot + geprüfte Fenstergrößen in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md` dokumentieren. Festgestellte Abweichungen werden als eigene Fix-Commits umgesetzt.
6. **Statische Prüfungen und Gesamttestlauf**
   - Voraussetzungen: Schritte 1–5.
   - Beschreibung: `.\scripts\Run-StaticChecks.ps1` (muss mit Exit-Code 0 ohne Befunde durchlaufen) sowie `dotnet test` wie in CI (`coverlet.runsettings`, TRX-Logger) ausführen; Ergebnis in `test-results.md` festhalten.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `LaterViewModelTests()` + `SeedFeedAsync()` | `LaterViewModelTests` (neu) | Fixture nach `UnreadViewModelTests`-Muster: `TestDbContextFactory`, echtes `ItemRepository`, ViewModel direkt instanziert; Seeding eines Feeds über den Context |
| `LoadCommand_PopulatesOnlySavedItems` | `LaterViewModelTests` | `LoadCommand` lädt ausschließlich `IsSavedForLater == true` Artikel in `SavedItems` |
| `LoadCommand_OrdersByPublishedAtDescending` | `LaterViewModelTests` | Reihenfolge der `SavedItems` entspricht `PublishedAt` absteigend |
| `ToggleSavedCommand_RemovesItemFromSavedItems` | `LaterViewModelTests` | `ToggleSavedCommand` auf einem bewahrten `ItemListItem` entfernt es nach Reload aus `SavedItems` |
| `ToggleSavedCommand_NullItem_DoesNothing` | `LaterViewModelTests` | Null-Parameter führt zu keinem Fehler und keiner Änderung |
| `MarkReadCommand_SetsReadAndKeepsItemInList` | `LaterViewModelTests` | `MarkReadCommand` markiert gelesen (`IsRead` persistiert); der Artikel bleibt in `SavedItems` (Liste filtert nur `IsSavedForLater`) |
| `DeleteExpiredAsync_RemovesExpiredReadItems` | `ItemRepositoryTests` | Gelesene, alte (`ReadAt`/`PublishedAt` < cutoff) und nicht bewahrte Items werden gelöscht; Rückgabewert = Anzahl |
| `DeleteExpiredAsync_KeepsSavedForLaterItems` | `ItemRepositoryTests` | **Kerninvariante:** bewahrter, gelesener, abgelaufener Artikel wird nicht gelöscht |
| `DeleteExpiredAsync_KeepsUnreadItems` | `ItemRepositoryTests` | Ungelesene, abgelaufene Artikel bleiben erhalten (geklärte Auslegung: Frist gilt nur für gelesene Artikel) |
| `DeleteExpiredAsync_KeepsNonExpiredItems` | `ItemRepositoryTests` | Gelesene, nicht bewahrte Artikel innerhalb der Frist bleiben erhalten |
| `DeleteExpiredAsync_UsesReadAtOverPublishedAt` | `ItemRepositoryTests` | Artikel mit altem `PublishedAt`, aber frischem `ReadAt` bleibt erhalten (Stichtag `ReadAt ?? PublishedAt`) |
| `GetSavedForLaterAsync_OrdersByPublishedAtDescending` | `ItemRepositoryTests` | Sortierungs-Assert für die Später-Liste (bisher ungetestet) |
| `ToggleSavedForLaterAsync_TogglesBackToFalse` | `ItemRepositoryTests` | Rück-Toggle `true → false` (bisher nur `false → true` getestet) |
| `CleanupAsync_DeletesExpiredButKeepsSaved` | `RetentionCleanupServiceTests` (neu) | End-to-End über Service: Settings mit `RetentionDays`, Seeds, `CleanupAsync` → gespeicherte Items bleiben, abgelaufene gelesene werden entfernt, Rückgabewert korrekt |
| `CleanupAsync_ZeroOrNegativeRetentionDays_Skips` | `RetentionCleanupServiceTests` | `RetentionDays <= 0` → kein Löschaufruf, Rückgabe 0 (Validierungsregel) |
| `CleanupAsync_RespectsConfiguredRetentionDays` | `RetentionCleanupServiceTests` | Der konfigurierte Wert (nicht der Default 30) bestimmt den Stichtag |
| `ToggleSavedCommand_TogglesFlagInPlace` | `UnreadViewModelTests` | `ToggleSavedCommand` invertiert `IsSavedForLater` auf dem Listenelement ohne Reload (Lücke lt. Inventory) |
| `DeleteAsync_CascadeDeletesSavedItems` | `FeedRepositoryTests` | Dokumentiert das geklärte Cascade-Verhalten: `FeedRepository.DeleteAsync` entfernt auch bewahrte Items des Feeds (bestätigte Nutzeraktion) |

### Betroffene bestehende Tests

Keine. Alle Erweiterungen sind additiv (neue Interface-Methode, neuer Service, neuer Aufruf in `OnStart`); keine bestehende Signatur oder Verhaltensweise ändert sich.

### E2E-Tests (primärer Funktionsnachweis)

**Es existiert keine E2E-/UI-Testinfrastruktur im Repo.** Einzige Testsuite ist `src/Reporter.Tests` (xUnit, `net10.0`), die nur `Reporter.Core` und `Reporter.Data` referenziert — die MAUI-Assembly `Reporter` (Pages, `App`, `AppShell`) kann im Testprozess nicht instanziiert werden; es gibt kein Appium-/UITest-Projekt, keine `AutomationId`s und kein Device-Test-Setup. Den Aufbau einer solchen Infrastruktur würde den Rahmen dieses Arbeitspakets sprengen.

Ersatz gemäß `AGENTS.md` (Mobile UI Design Review): **dokumentierte manuelle Verifikation** der Benutzerflüsse auf dem Windows-Handysize-Fenster 390 × 844 pt (bzw. iOS-Simulator via `scripts/iOS-Deployment.ps1`), mit Screenshot und Notiz der getesteten Größen in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md`. Die fachliche Lösch-Invariante ist nicht über die UI auslösbar und wird durch die Repository-/Service-Tests (siehe oben) nachgewiesen.

| Priorität | Szenario (manuelle Verifikation) | Nachweis | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|--------------------------------|----------|-------------------------------|---------------------|
| Pflicht | Artikel auf `UnreadPage` per Bookmark-Icon bewahren → Icon färbt sich; Artikel erscheint im Tab „Später" | Screenshot + Notiz in `test-results.md`/`mobile-ui-design.md` | „Artikel können als Später markiert werden", „Später-Ansicht zeigt nur bewahrte Artikel" | Benutzerfluss über zwei Seiten/Tab — nur durch Ausführung der App belegbar |
| Pflicht | Auf `LaterPage` Bookmark-Icon tippen → Artikel verschwindet aus der Liste; `EmptyView` erscheint bei leerer Liste | Screenshot + Notiz | „Entfernen der Bewahrung aus der Später-Ansicht" | Benutzerfluss, Listenaktualisierung |
| Pflicht | In `ArticleDetailPage` Bookmark-Aktion in der Bottom-Bar toggeln → Label/Icon wechseln („Lesezeichen setzen"/„entfernen") | Screenshot + Notiz | „Aktionen sind in Liste und Detailansicht verfügbar" | Benutzerfluss Detailansicht |
| Pflicht | Design-Draft-Vergleich `LaterPage` vs. `f_r_sp_ter_bewahren/screen.png` + `..._dark_mode/screen.png` (Light + Dark, 390 × 844 pt) | Screenshots beider Themes + Notiz | Design-Konformität, Dark Mode via `AppThemeBinding` | AGENTS.md-Pflicht bei UI-Features |
| Optional | Retention-Verhalten: mit kleinem `RetentionDays`-Wert (DB-Eingriff oder Test-Seed) App neu starten → gelesene alte Artikel weg, bewahrte bleiben | Notiz in `test-results.md` | „Bewahrte Artikel bleiben von automatischer Löschung ausgenommen" | Vollständig durch automatisierte Tests abgedeckt; manuelle Stichprobe optional |

Bestehende E2E-Tests müssen nicht angepasst werden — es existieren keine.

## Offene Punkte

Keine.
