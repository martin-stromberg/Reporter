# Bestandsaufnahme – Artikeldetailansicht mit WebView und Lesestatus

## Aufgabe
- Issue #24: Artikeldetailansicht mit WebView und Lesestatus
- Branch: `task/issue-24-24aaf5313a7e4ae4949c1a4d0dd801ad-artikeldetailansicht-mit-webvi`

## Übersicht
Die Artikeldetailansicht existiert noch nicht. Domain-Modell (`Item`) und Datenzugriff (`IItemRepository` / `ItemRepository`) sind bereits vorbereitet und enthalten alle benötigten Felder (`IsRead`, `IsSavedForLater`, `ContentHtml`, `Link`, `PublishedAt`, `Title`).

Für die UI-Implementierung steht ein ausführlicher Design-Draft bereit (`design-draft/stitch_local_rss_feed_reader/artikel_lesemodus/`), der Light- und Dark-Mode-Varianten sowie die gewünschte Floating Reader Control Bar beschreibt.

## Relevante Detaildokumente
- [01-design-draft.md](inventory/01-design-draft.md) – Analyse des Design-Drafts für die Artikeldetailansicht
- [02-domain-and-data.md](inventory/02-domain-and-data.md) – Domain-Modell und Datenzugriff
- [03-existing-ui.md](inventory/03-existing-ui.md) – Bestehende UI-Elemente und Navigation
- [04-mobile-ui-review.md](inventory/04-mobile-ui-review.md) – Mobile UI Design Review-Checkliste
- [05-open-questions.md](inventory/05-open-questions.md) – Offene Punkte und Annahmen

## Wichtige Dateien im Projekt
- `src/Reporter.Core/Models/Item.cs`
- `src/Reporter.Core/Models/ItemListItem.cs`
- `src/Reporter.Core/Interfaces/IItemRepository.cs`
- `src/Reporter.Data/Repositories/ItemRepository.cs`
- `src/Reporter/Views/UnreadPage.xaml`
- `src/Reporter/Views/ArticleCardView.xaml`
- `src/Reporter.Core/ViewModels/UnreadViewModel.cs`
- `src/Reporter/MauiProgram.cs`
- `src/Reporter/AppShell.xaml`
- `src/Reporter/Resources/Styles/Colors.xaml`
- `src/Reporter/Resources/Styles/Styles.xaml`
- `design-draft/stitch_local_rss_feed_reader/artikel_lesemodus/screen.png`
- `design-draft/stitch_local_rss_feed_reader/artikel_lesemodus_dark_mode/screen.png`

## Feststellungen
- `Item.ContentHtml` ist bereits im Domain-Modell und Repository vorhanden und kann im `WebView` angezeigt werden.
- `IItemRepository.MarkAsReadAsync` und `ToggleSavedForLaterAsync` sind implementiert und können für Lesestatus und Lesezeichen verwendet werden.
- `ArticleCardView` und `UnreadPage` zeigen, dass Card-Layouts, Touch-Targets mit 44 × 44 pt und `AppThemeBinding` im Projekt bereits etabliert sind.
- Eine `ArticleDetailPage` und ein `ArticleDetailViewModel` fehlen noch.
- Die Navigation von Karte zu Detailseite muss eingerichtet werden (`AppShell` ist leer).
- Die Mobile UI Design Review muss bei der Implementierung beachtet werden (Design-Draft-Vergleich, 390 × 844 pt, Touch-Targets, Dark Mode, UI-Verifikation).

## Nächster Schritt
- Umsetzungsplanung (`plan.md`) erstellen, basierend auf dieser Bestandsaufnahme.
