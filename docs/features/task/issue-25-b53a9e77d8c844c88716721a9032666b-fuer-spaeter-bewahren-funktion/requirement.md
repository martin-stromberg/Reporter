# Übersetzte Anforderung

## Titel
„Für später bewahren“-Funktion und separate Ansicht (Issue #25)

## Fachliche Zusammenfassung
Artikel (`Item`) erhalten einen nutzergesteuerten Bewahrungsstatus über das vorhandene Flag `IsSavedForLater` (Spalte `is_saved_for_later`). Der Status ist in der Artikelliste (`ArticleCardView` auf `UnreadPage` und `LaterPage`) sowie in der Detailansicht (`ArticleDetailPage`) umschaltbar; eine eigene Tab-Seite `LaterPage` („Später“) listet ausschließlich bewahrte Artikel absteigend sortiert und erlaubt das Entfernen der Bewahrung. Bewahrte Artikel sind von jeder automatischen Löschung (Retention-Cleanup auf Basis von `Settings.RetentionDays`) auszunehmen. Die technischen Grundlagen sind bereits weitgehend vorhanden (Repository-Filter `GetSavedForLaterAsync`, `ToggleSavedForLaterAsync`, `LaterViewModel`, `LaterPage`, Tab-Registrierung, Toggles in Liste und Detailansicht); das Arbeitspaket umfasst im Kern die Verifikation/Vervollständigung dieser Bausteine sowie die Absicherung der Lösch-Invariante.

## Betroffene Klassen und Komponenten

### Datenmodell
- `Reporter.Data.Entities.Item.IsSavedForLater` (vorhanden; `ReporterDbContext` mappt auf `is_saved_for_later`, Migration `InitialCreate`).
- `Reporter.Core.Models.Item.IsSavedForLater` und `Reporter.Core.Models.ItemListItem.IsSavedForLater` (vorhanden).
- Optional neu (nur falls Sortierung nach Speicherdatum gefordert wird): Eigenschaft `SavedAt` auf Entity und Domänenmodell inkl. EF-Core-Migration — Annahme, siehe Offene Fragen.

### Interfaces / Logikklassen
- `Reporter.Core.Interfaces.IItemRepository`: `GetSavedForLaterAsync()` (vorhanden, filtert `IsSavedForLater`, sortiert `PublishedAt` absteigend) und `ToggleSavedForLaterAsync(Guid)` (vorhanden) — deckt die im Issue genannte „Vorbereitung in der Repository-Schicht“ ab.
- `Reporter.Data.Repositories.ItemRepository`: Implementierungen beider Methoden vorhanden; `UpdateAsync` überträgt `IsSavedForLater` bereits.
- Löschlogik: Aktuell existiert **keine** automatische Artikellöschung im Code — `Settings.RetentionDays` wird nirgends ausgewertet, `FeedSyncService` löscht keine Items, `App.OnStart` führt nur `Database.MigrateAsync()` aus. Eine künftige Retention-Löschung (z. B. neuer Cleanup-Service oder neue `IItemRepository`-Methode wie `DeleteExpiredAsync(DateTime cutoff)`) muss `IsSavedForLater == false` als zwingende Löschbedingung enthalten.
- `IItemRepository.DeleteAsync(Guid)` löscht einen Artikel ohne `IsSavedForLater`-Prüfung; derzeit kein produktiver Aufrufer für Items.

### UI-Komponenten
- `Reporter.Views.LaterPage` + `Reporter.Core.ViewModels.LaterViewModel` (vorhanden): `SavedItems`, `LoadCommand` (aufgerufen in `OnAppearing`), `ToggleSavedCommand` (entfernt Bewahrung und lädt Liste neu), `MarkReadCommand`; `CollectionView` mit `EmptyView` (`AppResources.PlaceholderLater`).
- `Reporter.AppShell` (Code-Behind `AppShell.xaml.cs`): Tab „Später“ (`AppResources.TabLater`) bereits registriert.
- `Reporter.Views.ArticleCardView`: `ToggleSavedCommand`-BindableProperty und Bookmark-Icon (44 × 44 pt Touch-Target) mit `DataTrigger` auf `IsSavedForLater`; eingebunden in `UnreadPage.xaml` und `LaterPage.xaml`.
- `Reporter.Views.ArticleDetailPage` + `ArticleDetailViewModel`: `ToggleSavedForLaterCommand` und `BookmarkButtonLabel` („Lesezeichen setzen“/„entfernen“) in der Bottom-Action-Bar vorhanden.
- `AppResources` (`resx`/`de.resx`): `TabLater`, `PageTitleLater`, `PlaceholderLater`, `ButtonBookmark` vorhanden.

### Tests
- Vorhanden in `ItemRepositoryTests`: `GetSavedForLaterAsync_ReturnsOnlySaved`, `ToggleSavedForLaterAsync_TogglesState`.
- Neu zu erstellen: `LaterViewModelTests` (Laden ausschließlich bewahrter Artikel; Entfernen der Bewahrung über `ToggleSavedCommand` entfernt den Eintrag aus `SavedItems`).
- Neu zu erstellen (sobald Retention-Löschung existiert): Test, der sicherstellt, dass bewahrte Artikel trotz überschrittener Aufbewahrungsdauer nicht gelöscht werden.

## Implementierungsansatz
- Bestandsimplementierung gegen die Akzeptanzkriterien verifizieren und fehlende Teile ergänzen; keine parallelen Neumechanismen einführen, sondern `IItemRepository`, `ArticleCardView` und `LaterViewModel`/`LaterPage` wiederverwenden.
- Lösch-Invariante: Jede Stelle, die Artikel automatisiert entfernt (heute: keine; künftig: Retention-Cleanup), muss bewahrte Artikel ausschließen (Bedingung `!i.IsSavedForLater` im Lösch-Query). Falls der Cleanup Teil dieses Arbeitspakets ist: als Repository-Methode mit Filter `!IsSavedForLater && PublishedAt < cutoff` umsetzen und per `ISettingsRepository.GetAsync()`/`Settings.RetentionDays` steuern; Aufrufpunkt z. B. App-Start (`App.OnStart`) oder nach `FeedSyncService.SyncAllAsync`.
- Sortierung: `GetSavedForLaterAsync` sortiert bereits `OrderByDescending(i => i.PublishedAt)`. Die Anforderung „Speicherdatum **oder** Veröffentlichungsdatum“ wird damit über die Veröffentlichungsdatum-Variante erfüllt (Annahme; ein separates `SavedAt`-Feld existiert nicht und wäre nur bei abweichender Kundenentscheidung per Migration zu ergänzen).
- Entfernen aus der Später-Ansicht: `LaterViewModel.ToggleSavedAsync` ruft `ToggleSavedForLaterAsync` und lädt anschließend die Liste neu, wodurch nicht mehr bewahrte Artikel aus der Ansicht verschwinden — entspricht der geforderten Interaktion ohne separate Löschaktion.
- UI-Abgleich: `LaterPage` gegen `design-draft/stitch_local_rss_feed_reader/f_r_sp_ter_bewahren/screen.png` (bzw. `..._dark_mode`) prüfen; der Entwurf sieht pro Karte eine „Lesezeichen entfernen“-Aktion vor, die über das Bookmark-Icon/`ToggleSavedCommand` abgebildet wird. Mobile-UI-Regeln einhalten (44 × 44 pt Touch-Targets vorhanden, `AppThemeBinding`, `CollectionView` füllt `Grid`-Row `*`, keine verschachtelten ScrollViews).
- Hinweis zum Cascade-Verhalten: `Item -> Feed` ist mit `DeleteBehavior.Cascade` konfiguriert; das explizite Löschen eines Feeds entfernt auch bewahrte Artikel. Da dies eine bestätigte Nutzeraktion ist, wird sie nicht als „automatische Löschung“ gewertet (Annahme, siehe Offene Fragen).

## Konfiguration
Keine neue Konfiguration erforderlich. Relevant ist die bestehende anwendungsweite Einstellung `Settings.RetentionDays` (Singleton-Datensatz, Default 30 Tage) für die künftige automatische Löschung; die Ausnahme für `IsSavedForLater` gilt unabhängig vom konfigurierten Wert und ist nicht abschaltbar.

## Offene Fragen
1. Die Anforderung „bewahrte Artikel werden nie automatisch gelöscht“ setzt eine Löschlogik voraus, die im Code noch nicht existiert (`Settings.RetentionDays` wird nirgends ausgewertet). Soll der Retention-Cleanup in diesem Arbeitspaket neu eingeführt werden, oder genügt es, die Invariante in `IItemRepository`-Dokumentation und Tests festzuhalten, bis das Lösch-Arbeitspaket folgt?
2. Reicht die bestehende Sortierung nach `PublishedAt` absteigend aus (Interpretation des „oder“), oder soll ein eigenes Speicherdatum (`SavedAt`) per Migration ergänzt und bevorzugt sortiert werden?
3. Gilt die Schutz-Invariante nur für die automatische Retention-Löschung oder auch beim expliziten Löschen eines Feeds, dessen Artikel per `DeleteBehavior.Cascade` mitgelöscht werden?
4. Falls der Retention-Cleanup in diesem Arbeitspaket entsteht: Betrifft die Aufbewahrungsfrist nur gelesene Artikel (Design-Text „Gelesene Artikel aufbewahren“, d. h. Löschbedingung `IsRead && !IsSavedForLater`) oder alle Artikel?
