<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Lesepakete (GitHub Issue #164)

Bestandsaufnahme des Lade- und Markierpfads der Startseite **Ungelesen** (`UnreadViewModel`, `ItemRepository`, `UnreadPage`) bezogen auf die Anforderung, ungelesene Artikel als Lesepaket pro Kalendertag zu laden und „Alles gelesen" auf das geladene Tagespaket zu begrenzen.

## Zusammenfassung

- **Lade-Pfad vorhanden, aber tageslos:** `UnreadViewModel.LoadPageAsync` ruft `IItemRepository.GetUnreadByDateAsync(page, PageSize=20, categoryId, ascending)` auf — Filter nur `!IsRead` + optionaler Kategorie über `Feed.CategoryId`, Sortierung nach `PublishedAt` mit `Id`-Tie-Breaker. Es gibt keinen Datums-/Tag-Parameter und keine Abfrage des ältesten ungelesenen Datums.
- **„Alles gelesen" ist global:** `MarkAllReadAsync` → `ItemRepository.MarkAllAsReadAsync(categoryId)` markiert per `ExecuteUpdate` **alle** ungelesenen Artikel (optional kategoriefiltert). Kein tag-begrenztes Pendant vorhanden — weder im Interface noch in der Implementierung noch im Test-Dekorator `DelegatingItemRepository` (21 Member, alle `virtual`, müssen bei Interface-Erweiterung mitgepflegt werden).
- **`PublishedAt` ist `DateTime?` in UTC:** `FeedSyncService` speichert `PublishDate.UtcDateTime`; `null` bei fehlendem Feed-Datum. In SQLite sortiert `NULL` bei aufsteigendem `OrderBy` zuerst (per Test dokumentiert) — direkt relevant für die offene Frage, welchem Tag undatierte Artikel zugeordnet werden.
- **Frische-Lade-Pfade sauber trennbar:** `LoadAsync` (OnAppearing/Refresh/MarkAllRead/leere Liste nach `MarkReadAsync`) und `SelectCategoryAsync` setzen `_currentPage = 0` und laden Seite 0 neu; `LoadMoreAsync` appended nur. Damit existiert bereits eine klare Stelle, an der ein Paket-Kontext bestimmt werden kann. Einzelnes Als-gelesen-Markieren (`MarkReadAsync`) lädt nur bei `Articles.Count == 0` neu — das geforderte Verhalten „keine <5-Neubewertung beim Entfernen" ist strukturell schon gegeben.
- **Sortierreihenfolge kommt aus den Settings:** `UnreadSortOrder` (`"asc"`/`"desc"`, Default `"desc"`) wird bei jedem `LoadPageAsync` gelesen; die Anforderung beschreibt nur den aufsteigenden Fall, der absteigende ist offen (siehe requirement.md, Offene Fragen 1).
- **UI-Berührungspunkte minimal:** Doppelhaken-Button in `UnreadPage.xaml` (Tap → `OnMarkAllReadTapped` → `DisplayActionSheetAsync` mit `UnreadBadgeText` = **Gesamtzahl** ungelesener Artikel als Dialogtitel). `UnreadBadgeText`/`UnreadCount` basieren auf `GetUnreadCountAsync` über alle Tage — für den neuen Paket-Umfang ggf. anzupassen. Keine Tages-Header im Layout vorhanden (Scope-Ausschluss passt zum Bestand).
- **Zähler-Konsistenz zu beachten:** `UnreadCount`, Chip-`Count`s und `CardPositionText` spiegeln alle die Gesamt-Ungelesenzahl wider und werden in `LoadPageAsync`, `MarkReadAsync` und `MarkAllReadAsync`→`LoadAsync` synchron gehalten.

Test-Ausgangszustand: `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release` — **753/753 Tests bestanden, 0 Fehlschläge, 0 übersprungen** (Nachweis: [tests.md](inventory/tests.md), TRX unter `inventory/test-results/`). `Reporter.E2ETests` (FlaUI) nicht ausgeführt — erfordert interaktive Windows-Session, läuft auch in der CI nicht; für den geplanten neuen Akzeptanztest existiert kein Grün-Nachweis. Bekannte Testlücken: kein dedizierter Repository-Test für `MarkAllAsReadAsync` und keiner für den `categoryId`-Parameter der paginierten Ungelesen-Abfrage (beide nur indirekt über ViewModel-Tests abgedeckt).

## Details

- [Datenmodell](inventory/models.md) — `Item` (Core + Entity), `ItemListItem`, `ItemListRow`, `Feed.CategoryId`, `CategoryFilterItem`, `Settings.UnreadSortOrder`
- [Logik](inventory/logic.md) — `UnreadViewModel` (Lade-/Markier-Pipeline), `ItemRepository`, `UnreadPage` Code-Behind, `FeedSyncService` (Herkunft von `PublishedAt`)
- [Aufzählungswerte](inventory/enums.md) — `SettingsValues`-Konstanten (`"asc"`/`"desc"`), `PageSize = 20`; keine Enum-Typen betroffen
- [Interfaces](inventory/interfaces.md) — `IItemRepository` (relevante Member, keine tag-begrenzten), `ISettingsRepository`, `ICategoryRepository`, `IFeedSyncService`, `INetworkStatusService`
- [Tests](inventory/tests.md) — Test-Ausgangszustand (753 grün), betroffene Testklassen, Test-Doubles und Hilfsmethoden
