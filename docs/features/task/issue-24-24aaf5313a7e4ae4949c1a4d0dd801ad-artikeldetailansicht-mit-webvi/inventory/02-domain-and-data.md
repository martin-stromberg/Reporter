# Detaildokument: Domain-Modell und Datenzugriff

## Domain-Modell
- `src/Reporter.Core/Models/Item.cs`
  - Enthält bereits: `Id`, `FeedId`, `Title`, `Link`, `PublishedAt`, `GuidOrHash`, `IsRead`, `IsSavedForLater`, `ReadAt`, `ContentHtml`.
  - `ContentHtml` ist der gespeicherte Volltext für das WebView.

- `src/Reporter.Core/Models/ItemListItem.cs`
  - Listendarstellung ohne `ContentHtml`, aber mit `FeedTitle`, `CategoryId`, `CategoryName`, `ImageUrl`, `Summary`.

## Repository
- `src/Reporter.Core/Interfaces/IItemRepository.cs`
  - `GetByIdAsync(Guid id)` für Detailansicht vorhanden.
  - `MarkAsReadAsync(Guid id)` und `ToggleSavedForLaterAsync(Guid id)` bereits definiert.

- `src/Reporter.Data/Repositories/ItemRepository.cs`
  - Implementiert alle Operationen mit EF Core + SQLite.
  - `MapToModel` / `MapToEntity` kopieren `ContentHtml` korrekt.

## Feststellungen
- Datenbankschema und Mapping für Gelesen-/Gespeichert-Status sind bereits implementiert.
- `Item.ContentHtml` existiert und kann direkt im WebView geladen werden.
- `IItemRepository.MarkAsReadAsync` aktualisiert `IsRead` und `ReadAt`; `ToggleSavedForLaterAsync` kehrt `IsSavedForLater` um.

## Offene Punkte
- Keine Verzögerungslogik für automatisches Markieren als gelesen vorhanden (kommt aus Einstellungen-Arbeitspaket).
- Keine "Lesezeit"-Eigenschaft; muss ggf. berechnet oder ergänzt werden.
