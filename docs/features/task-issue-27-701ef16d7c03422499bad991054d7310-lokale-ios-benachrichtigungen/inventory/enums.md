# Enums und Konstanten

Im Projekt existieren keine echten `enum`-Typen für den betroffenen Bereich; Status- und Einstellungswerte werden über `static class`-Konstanten (`string`) abgebildet.

## `FeedHealth`
Datei: `src/Reporter.Core/Services/FeedHealth.cs`

Statische Klasse mit den Sync-/Feed-Health-Statuswerten — relevant für die Frage, bei welchem Sync-Status benachrichtigt wird.

| Wert | Bedeutung |
|------|-----------|
| `Ok` (`"OK"`, Zeile 11) | Feed synchronisiert ohne Probleme |
| `Warning` (`"Warning"`, Zeile 16) | Warnung (deutlich weniger Feed-Einträge oder >30 Tage keine neuen Artikel) |
| `Error` (`"Error"`, Zeile 21) | Synchronisation fehlgeschlagen |

Hilfsmethode: `Changed(string? current, string? next)` (Zeile 29).

## `SettingsValues`
Datei: `src/Reporter.Core/Models/SettingsValues.cs`

Persistierte Einstellungswerte (nicht benachrichtigungsbezogen, aber Teil des `Settings`-Modells).

| Wert | Bedeutung |
|------|-----------|
| `AutoMarkReadOnOpen` (`"on_open"`, Zeile 11) | Als gelesen markieren beim Öffnen |
| `AutoMarkReadOnScroll` (`"on_scroll"`, Zeile 16) | Als gelesen markieren beim Scrollen |
| `AutoMarkReadOff` (`"off"`, Zeile 21) | Automatisches Markieren deaktiviert |
| `ThemeSystem` / `ThemeLight` / `ThemeDark` (Zeilen 26-36) | Farbschema-Werte |

Hilfsmethode: `IsAutoMarkReadEnabled(string?)` (Zeilen 43-46).
