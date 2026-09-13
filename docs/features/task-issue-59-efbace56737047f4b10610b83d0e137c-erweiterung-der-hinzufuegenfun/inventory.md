<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Unaufdringliches Hinzufügen-Formular, Dateiname als Titel-Fallback, Kontextmenü-Aktionen „Umbenennen" und „Kategorie ändern"

Analysiert wurde der Feeds-Bereich der MAUI-App (`FeedsPage`, `FeedsViewModel`, `FeedSyncService`, Repositories, Ressourcen, Tests) bezogen auf die Anforderung, das dauerhaft sichtbare Hinzufügen-Formular hinter einem „+"-Button zu verbergen, titellose Feeds per Dateinamen der Feed-URL zu benennen und das Feed-Kontextmenü um „Umbenennen" und „Kategorie ändern" zu erweitern.

## Zusammenfassung

- **`FeedsPage`** ist aktuell eine kombinierte Formular-plus-Liste-Seite: Die `Border`-Formularkarte (XAML Zeilen 17–79) mit `NewUrl`-Entry, `NewTitle`-Entry, `Picker` `Categories`/`SelectedCategory`, `Switch` `FeedNotificationsEnabled` (inkl. `NotificationsSupported`-/iOS-Hinweis) und den Buttons „Suchen" (`SearchCommand`) + „Speichern" (`SaveCommand`) ist permanent sichtbar. Es existiert **kein** „+"-Button, keine Formular-Sichtbarkeitssteuerung und keine Bottom-Sheet-/Modal-Struktur.
- **Kontextmenü** `OnFeedTapped` (`DisplayActionSheetAsync`) kennt nur `ButtonRefresh`, `ButtonEdit`, `ButtonDelete` — „Umbenennen" und „Kategorie ändern" fehlen; `DisplayPromptAsync` wird im gesamten Codebestand nicht verwendet (neues Pattern für den Umbenennen-Dialog).
- **`FeedsViewModel`** hat alle Formular-Zustände (`NewUrl`, `NewTitle`, `SelectedCategory`, `FeedNotificationsEnabled`, `SelectedFeed`) und Commands (`SaveCommand`, `EditCommand`, `DeleteCommand`, `RefreshCommand`, `RefreshAllCommand`, `SearchCommand`, `SubscribeResultCommand`, `CloseSearchResultsCommand`). Es fehlen: Sichtbarkeitsflag für das Formular (`ShowAddForm`-ähnlich), `OpenAddFormCommand`/`CloseAddFormCommand`, `RenameFeedCommand`, `ChangeFeedCategoryCommand`.
- **Titel-Fallbacks heute:** `SubscribeResultAsync` speichert bei leerem Treffer-Titel die **volle `FeedUrl`** als `Title` (Search.cs Zeile 270); `OfferDirectAddAsync` belegt `NewTitle` mit dem **Host** vor (Zeilen 233–237). Eine Dateinamen-Ableitung (letztes Pfadsegment) existiert nirgends.
- **`FeedSyncService`** löst beim ersten Sync Platzhalter-Titel auf: leer, `Title == Url` oder `Title == Host` (`IsHostPlaceholderTitle`, Zeilen 186–219). Ein **Dateiname** (`heise-atom.xml`) wird aktuell **nicht** als Platzhalter erkannt und würde dauerhaft stehen bleiben.
- **Datenmodell** erfordert keine Änderung: `Feed.Title`/`Feed.CategoryId`, `FeedListItem` und `IFeedRepository.UpdateAsync` decken beide neuen Aktionen ab; `Categories` enthält bereits den `CategoryNone`-Pseudo-Eintrag (`Guid.Empty` → `null`).
- **Ressourcen:** Alle heute verwendeten Schlüssel existieren in EN/DE. Fehlen u. a. `ButtonRename`, `ButtonChangeCategory`, Beschriftung für den „+"-Button und Dialogtitel/-texte; `AppResources.Designer.cs` muss regeneriert werden.
- **Design-Entwurf** `design-draft/stitch_local_rss_feed_reader/feeds_health_status/` (`screen.png` + `code.html`) zeigt einen primären Button „+ Feed per URL hinzufügen" (`btn-open-add-feed`) und ein Bottom-Sheet `modal-add-feed` mit URL-Feld, optionaler Kategorie-`select` und „Feed abonnieren"; zusätzlich `modal-edit-feed` mit Titel/URL. Als UI-Referenz in [ui.md](inventory/ui.md) dokumentiert.
- **Test-Ausgangszustand:** Alle Suiten grün — `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release`: **280/280** bestanden (0 Fehler, 0 übersprungen, Exit 0); `npm test` (`node --test "scripts/*.test.mjs"`): **36/36** bestanden (Exit 0). Nachweise in [tests.md](inventory/tests.md) und [test-results/](inventory/test-results/). Testlücke: Die MAUI-UI (`FeedsPage.xaml(.cs)`) wird vom Testprojekt nicht kompiliert und ist ungetestet.

## Details

- [Datenmodell](inventory/models.md) — `Feed`, `FeedListItem`, `Category`, `FeedSearchResult`, `SyncResult`, `CategoryWithCount`
- [Logik](inventory/logic.md) — `FeedsViewModel` (+ Search-Partial), `FeedsPage` Code-Behind, `FeedSyncService`, `FeedSearchService`, `FeedRepository`, `CategoryRepository`, `BaseViewModel`
- [Enums](inventory/enums.md) — `FeedSearchMatchKind`, `FeedHealth`-Konstanten, `NotificationAuthorizationStatus`
- [Interfaces](inventory/interfaces.md) — `IFeedRepository`, `ICategoryRepository`, `IFeedSyncService`, `IFeedSearchService`, `INetworkStatusService`, `ILocalNotificationService`
- [UI](inventory/ui.md) — `FeedsPage.xaml`-Struktur und Design-Entwurf `feeds_health_status` (inkl. `modal-add-feed`)
- [Ressourcen](inventory/resources.md) — vorhandene `AppResources`-Schlüssel (EN/DE) und fehlende Schlüssel
- [Tests](inventory/tests.md) — Test-Ausgangszustand, Nachweise, betroffene Testklassen und Hilfsmethoden
