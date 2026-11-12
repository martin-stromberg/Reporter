<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Aufzählungswerte — Bestandsaufnahme Lesepakete

Es gibt **keine Enum-Typen** im betroffenen Bereich. Die für die Anforderung relevanten diskreten Werte sind String-Konstanten in `SettingsValues` sowie numerische Konstanten im ViewModel/Page.

## `SettingsValues` (Konstanten, Ausschnitt)

Datei: `src/Reporter.Core/Models/SettingsValues.cs`

| Wert | Bedeutung |
|------|-----------|
| `SortOrderDescending = "desc"` (Zeile 68) | Ungelesen-Liste neueste zuerst; Default von `Settings.UnreadSortOrder`. |
| `SortOrderAscending = "asc"` (Zeile 73) | Ungelesen-Liste älteste zuerst — die von der Anforderung vorausgesetzte Reihenfolge; Vergleichswert in `UnreadViewModel.LoadPageAsync` (`settings.UnreadSortOrder == SettingsValues.SortOrderAscending`). |
| `UnreadLayoutList = "list"` / `UnreadLayoutCards = "cards"` (Zeilen 78, 83) | Layout der Ungelesen-Seite; für Lesepakete nur indirekt relevant (beide Layouts lesen aus derselben `Articles`-Liste). |
| `NormalizeUnreadLayout(string?)` | Normalisiert persistierte Layoutwerte auf `"list"`/`"cards"`. |

## Numerische Konstanten

| Konstante | Ort | Wert | Bedeutung |
|-----------|-----|------|-----------|
| `PageSize` | `UnreadViewModel` (Zeile 20) | `20` | Seitengröße des Ungelesen-Pagings; `HasMore = items.Count == PageSize`. Interagiert mit der neuen <5-Schwelle (kein Konflikt: 5 < 20). |

## `FeedHealth` (Enum, nur Randbezug)

Datei: `src/Reporter.Core/Services/FeedHealth.cs` (Zeile 8) — wird in `RefreshAsync` nur für die Fehlererkennung des Sync-Ergebnisses ausgewertet (`result.Status == FeedHealth.Error`); für die Lesepaket-Logik nicht relevant.
