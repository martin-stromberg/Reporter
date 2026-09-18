<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# UI-Komponenten — Bestandsaufnahme

Bezug: Anforderung „Artikeldaten getrennt von Nutzerdaten speichern (iCloud-Backup)" (`requirement.md`). Die Anforderung verlangt keine neuen Oberflächen; folgende vorhandene Komponenten konsumieren `ContentHtml` bzw. daraus abgeleitete Felder.

## `ArticleDetailViewModel` / `ArticleDetailPage`

Dateien: `src/Reporter/ViewModels/ArticleDetailViewModel.cs`, `src/Reporter/Views/ArticleDetailPage.xaml`

- Lädt den Artikel über `IItemRepository.GetByIdAsync(itemId)` (`ArticleDetailViewModel.cs` Zeile 286); bei `null` wird `GoBackAsync` aufgerufen.
- `ReadingTime` wird aus `item.ContentHtml` via `ReadingTimeEstimator.EstimateText` befüllt (Zeile 299).
- `RebuildHtml()` (Zeile 353) rendert `Item?.ContentHtml` nach `ArticleHtmlSanitizer.Sanitize(..., forOffline: !IsOnline)` in ein lokales HTML-Dokument (`HtmlSource`, WebView). Bei leerem/weißem Content ist `HtmlSource = string.Empty` — ein defensiver Pfad für fehlenden Inhalt existiert bereits (Zeilen 356–360).
- `OnConnectivityChanged` (Zeile 347) baut das HTML bei Netzwechsel neu; Offline werden Links/Bilder entfernt (`forOffline`).
- Update-Pfade (`MarkRead`, `ToggleSaved`, FontSize etc.) erzeugen ein `Item` mit `ContentHtml = Item.ContentHtml` (Zeile 583) und schreiben über `IItemRepository.UpdateAsync` zurück — ein Schreibpfad, der den Content mittransportiert.
- Registrierung: `AddTransient<ArticleDetailViewModel>` / `AddTransient<ArticleDetailPage>` (`MauiProgram` Zeilen 109–110).

## `UnreadPage` / `LaterPage` über `ArticleCardView`

Dateien: `src/Reporter/Views/UnreadPage.xaml`, `src/Reporter/Views/LaterPage.xaml`, `src/Reporter/Views/ArticleCardView.xaml`, ViewModels `src/Reporter.Core/ViewModels/UnreadViewModel.cs`, `src/Reporter.Core/ViewModels/LaterViewModel.cs`

- `UnreadViewModel` lädt `ItemListItem`-Seiten über `IItemRepository.GetUnreadByDateAsync(page, pageSize, categoryId, ascending)`; `LaterViewModel` über `GetSavedForLaterAsync(page, pageSize)`.
- `ArticleCardView.xaml` bindet `ImageUrl` (Vorschaubild, offline ausgeblendet), `Summary` (Teaser) und `ReadingTimeText` — alle drei werden im `ItemRepository` aus `ContentHtml` abgeleitet (siehe `models.md` → `ItemListItem`, `logic.md` → `ItemRepository.MapToListItem`).
- Fehlt `ContentHtml` (z. B. nach Restore), ergeben `ExtractImageUrl`/`ExtractSummary`/`EstimateText` heute `null` — die Karten zeigen dann kein Bild/keinen Teaser/keine Lesezeit, bleiben aber bedienbar.

## Weitere indirekt betroffene Oberflächen

| Komponente | Datei | Bezug |
|------------|-------|-------|
| `FeedsPage` / `FeedsViewModel` | `src/Reporter/Views/FeedsPage.xaml`, `src/Reporter.Core/ViewModels/FeedsViewModel.cs` | Feed-Löschung triggert die `items`-Kaskade (Content würde mitgelöscht werden müssen). |
| `SettingsPage` / `SettingsViewModel` | `src/Reporter/Views/SettingsPage.xaml`, `src/Reporter.Core/ViewModels/SettingsViewModel.cs` | `RetentionDays` steuert `RetentionCleanupService` — der Cleanup müsste den Content-Speicher mitbereinigen. |
