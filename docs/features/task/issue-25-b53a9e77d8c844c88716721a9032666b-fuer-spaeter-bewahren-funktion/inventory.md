# Bestandsaufnahme: „Für später bewahren“-Funktion und separate Ansicht (Issue #25)

Analysiert wurde der Bestand des RSS-Readers „Reporter“ (.NET MAUI, Projekte `Reporter`, `Reporter.Core`, `Reporter.Data`, `Reporter.Tests`) bezogen auf die Anforderung in [`requirement.md`](requirement.md): nutzergesteuerter Bewahrungsstatus `IsSavedForLater`, eigene „Später“-Ansicht und Schutz bewahrter Artikel vor automatischer Löschung.

## Zusammenfassung

- **Datenmodell vollständig vorhanden:** `IsSavedForLater` existiert auf `Reporter.Data.Entities.Item` (Spalte `is_saved_for_later`, `ReporterDbContext.ConfigureItem`), `Reporter.Core.Models.Item` und `Reporter.Core.Models.ItemListItem`. Ein separates Speicherdatum (`SavedAt`) existiert nicht.
- **Repository-Schicht vorhanden:** `IItemRepository.GetSavedForLaterAsync()` (Filter `IsSavedForLater`, Sortierung `PublishedAt` absteigend) und `ToggleSavedForLaterAsync(Guid)` sind deklariert und in `ItemRepository` implementiert; `UpdateAsync` überträgt `IsSavedForLater`.
- **UI vorhanden:** `LaterPage` + `LaterViewModel` (Liste `SavedItems`, `LoadCommand` in `OnAppearing`, `ToggleSavedCommand`, `MarkReadCommand`, `EmptyView`), Tab „Später“ in `AppShell.xaml.cs`, `ToggleSavedCommand` auf `ArticleCardView` (in `UnreadPage` und `LaterPage` eingebunden, 44 × 44 pt Bookmark-Icon mit `DataTrigger`), `ToggleSavedForLaterCommand` + `BookmarkButtonLabel` in `ArticleDetailViewModel`/`ArticleDetailPage`, Ressourcen `TabLater`/`PageTitleLater`/`PlaceholderLater`/`ButtonBookmark` in beiden RESX-Dateien.
- **Kritische Lücke — Löschlogik existiert nicht:** Es gibt im Produktivcode **keinerlei** automatische Artikellöschung. `Settings.RetentionDays` wird nirgends ausgewertet (nur persistiert und als Fallback in `ArticleDetailViewModel` hardcodiert), `FeedSyncService` löscht keine Items, `App.OnStart` führt nur `Database.MigrateAsync()` aus, `MauiProgram` registriert keinen Hintergrunddienst. `IItemRepository.DeleteAsync(Guid)` prüft `IsSavedForLater` nicht und hat keinen produktiven Aufrufer (nur Tests). Einzige produktive Lösch-Kaskade: `FeedsViewModel.DeleteAsync` → `IFeedRepository.DeleteAsync` löscht einen Feed; `Item → Feed` ist mit `DeleteBehavior.Cascade` konfiguriert und entfernt dabei auch bewahrte Artikel (bestätigte Nutzeraktion, siehe Offene Frage 3 in `requirement.md`).
- **Sortierung:** `GetSavedForLaterAsync` sortiert `OrderByDescending(i => i.PublishedAt)` — erfüllt die „Speicherdatum **oder** Veröffentlichungsdatum“-Variante über das Veröffentlichungsdatum.
- **Testlücken:** `LaterViewModelTests` existiert nicht; `UnreadViewModelTests` enthält keinen Test für `ToggleSavedCommand`; `ArticleDetailViewModel` liegt in der MAUI-Assembly `Reporter` und ist vom Testprojekt (referenziert nur `Reporter.Core` + `Reporter.Data`) nicht testbar. Ein Retention-Cleanup-Test ist nicht möglich, da die Funktion nicht existiert.

**Test-Ausgangszustand:** `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build` (mit Coverlet-Settings und TRX-Logger wie in CI): **70 erfolgreich, 0 fehlgeschlagen, 0 übersprungen** (Exit-Code 0). Vorangehender Solution-Build Release erfolgreich (0 Warnungen, 0 Fehler). Zeilenabdeckung laut Cobertura-Report: 80,39 %. Details und Nachweise: [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
