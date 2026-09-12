<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenmodell

Betroffene Datenmodellklassen in `Reporter.Core/Models`. Ein Suchtreffer-Modell (in der Anforderung als `FeedSearchResult` bezeichnet) existiert nicht.

## `Feed`
Datei: `src/Reporter.Core/Models/Feed.cs`

Domänenmodell eines RSS/Atom-Feeds. Beim Speichern wird ein `Feed` über `IFeedRepository.AddAsync`/`UpdateAsync` angelegt (Aufrufer: `FeedsViewModel.SaveAsync`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Eindeutige Feed-ID |
| `Url` | `string` (required, init) | Feed-URL; wird in `FeedsViewModel.SaveAsync` auf absolute http(s)-URL geprüft, Dublettenprüfung via `GetByUrlAsync` |
| `Title` | `string` (required, init) | Feed-Titel; Pflicht beim Speichern (`ErrorFeedTitleEmpty`) |
| `CategoryId` | `Guid?` (init) | Optionale Kategorie-Zuordnung (`null` = keine) |
| `LastCheckedAt` | `DateTime?` (init) | Zeitpunkt des letzten Abrufs |
| `HealthStatus` | `string?` (init) | Health-Status (`FeedHealth.Ok`/`Warning`/`Error` — String-Konstanten, kein Enum) |
| `HealthLastChange` | `DateTime?` (init) | Zeitpunkt der letzten Statusänderung |
| `NotificationsEnabled` | `bool` (required, init) | Feed-spezifischer Benachrichtigungs-Schalter |

Hinweis: kein `Description`-Feld — die Anforderung sieht eine Beschreibung nur in den Suchtreffern vor (Offene Frage 4 in `requirement.md`).

## `FeedListItem`
Datei: `src/Reporter.Core/Models/FeedListItem.cs`

Anzeigemodell für die Feed-Liste (`FeedsViewModel.Feeds`, geladen via `IFeedRepository.GetAllWithDetailsAsync`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Feed-ID |
| `Title` | `string` (required, init) | Anzeigetitel |
| `Url` | `string` (required, init) | Feed-URL |
| `CategoryId` | `Guid?` (init) | Zugeordnete Kategorie |
| `CategoryName` | `string?` (init) | Anzeigename der Kategorie (`null` bei keiner) |
| `LastCheckedAt` | `DateTime?` (init) | Letzter Abruf |
| `HealthStatus` | `string?` (init) | Health-Status-String (steuert Status-Punkt und -Label in `FeedsPage.xaml` über DataTriggers) |
| `HealthLastChange` | `DateTime?` (init) | Letzte Statusänderung |
| `UnreadCount` | `int` (required, init) | Anzahl ungelesener Einträge |
| `NotificationsEnabled` | `bool` (required, init) | Notification-Flag (befüllt `FeedNotificationsEnabled` beim Editieren) |

## `Category`
Datei: `src/Reporter.Core/Models/Category.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Kategorie-ID; `Guid.Empty` wird in `FeedsViewModel.LoadAsync` als „Keine Kategorie“-Eintrag (`AppResources.CategoryNone`) verwendet |
| `Name` | `string` (required, init) | Anzeigename (Picker-`ItemDisplayBinding` in `FeedsPage.xaml`) |

## Nicht vorhanden

- `FeedSearchResult` (Suchtreffer-Modell mit `Title`, `Description`, `Category`, `FeedUrl`, `SiteUrl`, Relevanz/Trefferart) — in `requirement.md` als neu gefordert.
- Trefferart-/Relevanz-Typ (Enum o. ä.) — siehe [enums.md](enums.md).
