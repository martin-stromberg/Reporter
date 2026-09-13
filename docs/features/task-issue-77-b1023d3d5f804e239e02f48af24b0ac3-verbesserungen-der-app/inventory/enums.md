<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Enums und Konstanten — Bestandsaufnahme (Issue #77)

## `FeedSearchMatchKind` (Enum, R2-Kontext)
Datei: `src\Reporter.Core\Models\FeedSearchMatchKind.cs`

| Wert | Bedeutung |
|------|-----------|
| `ExactUrl` | Eingegebene URL ist selbst ein Feed-Dokument bzw. exakter Treffer |
| `Directory` | Treffer aus dem feedsearch.dev-Verzeichnis |
| `Discovered` | Per Link-Tags/Standardpfaden auf der Website gefunden |

Reihenfolge definiert die Ergebnis-Sortierung in `FeedSearchService.SearchAsync`.

## `NotificationAuthorizationStatus` (Enum, R8-Kontext)
Datei: `src\Reporter.Core\Models\NotificationAuthorizationStatus.cs`

| Wert | Bedeutung |
|------|-----------|
| `Unsupported` | Plattform ohne lokale Benachrichtigungen (Windows, Android, MacCatalyst) |
| `NotDetermined` | Berechtigung noch nicht angefragt |
| `Denied` | Vom Nutzer in den Systemeinstellungen verweigert |
| `Authorized` | Erteilt (inkl. Provisional/Ephemeral) |

## `FeedHealth` (Konstanten-Klasse, R8-/R7-Kontext)
Datei: `src\Reporter.Core\Services\FeedHealth.cs`

| Wert | Bedeutung |
|------|-----------|
| `Ok` (`"OK"`) | Feed fehlerfrei |
| `Warning` (`"Warning"`) | Warnung (z. B. deutlich weniger Items, >30 Tage ohne neue Items) |
| `Error` (`"Error"`) | Abruf fehlgeschlagen |

Hilfsmethode: `Changed(string? current, string? next)`. Wird u. a. in `FeedSyncService` und `FeedsViewModel.TryPersistNewFeedAsync` verwendet.

## `SettingsValues` (Konstanten-Klasse, R4)
Datei: `src\Reporter.Core\Models\SettingsValues.cs`

| Wert | Bedeutung |
|------|-----------|
| `AutoMarkReadOnOpen` (`"on_open"`) | Auto-Markieren beim Öffnen |
| `AutoMarkReadOnScroll` (`"on_scroll"`) | Auto-Markieren beim Scrollen (Seed-Default) |
| `AutoMarkReadOff` (`"off"`) | Auto-Markieren aus |
| `ThemeSystem`/`ThemeLight`/`ThemeDark` (`"system"`/`"light"`/`"dark"`) | Erscheinungsbild |
| `LanguageSystem`/`LanguageGerman`/`LanguageEnglish` (`"system"`/`"de"`/`"en"`) | Sprache |

Hilfsmethode: `IsAutoMarkReadEnabled(string?)`. **Fehlt für R4:** Konstanten für die Sortierrichtung (z. B. `SortOrderDescending`/`SortOrderAscending`).
