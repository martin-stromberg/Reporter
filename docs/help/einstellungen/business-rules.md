← [Zurück zur Übersicht](index.md)

# Einstellungen — Business Rules

## Lösch-Invarianten (niemals ungelesen, niemals gemerkt)

**Beschreibung:** Die automatische Löschung darf weder ungelesene noch für später gemerkte Artikel entfernen. Diese Invarianten gelten für beide Löschregeln (allgemeine Frist und Keyword-Regel) und sind nicht abschaltbar.

**Bedingungen:**
- `IsRead == false` → Artikel bleibt erhalten.
- `IsSavedForLater == true` → Artikel bleibt erhalten.

**Umsetzung:** Beide Prädikate stecken in den Repository-Abfragen `ItemRepository.DeleteExpiredAsync` und `ItemRepository.GetExpiredKeywordCandidatesAsync` — sie können nicht umgangen werden.

## Unterschiedliche Fristbasis der beiden Löschregeln

**Beschreibung:** Beide Regeln teilen denselben `cutoff` (`UtcNow - RetentionDays`), nutzen aber unterschiedliche Zeitstempel:

| Regel | Bedingung | Fristbasis |
|-------|-----------|------------|
| Allgemeine Retention | `IsRead && !IsSavedForLater && (ReadAt ?? PublishedAt) < cutoff` | Lesezeitpunkt (`ReadAt`, Fallback `PublishedAt`) |
| Keyword-Regel | `IsRead && !IsSavedForLater && (PublishedAt ?? ReadAt) < cutoff` **und** Keyword-Match | Veröffentlichungsdatum (`PublishedAt`, Fallback `ReadAt`) |

**Begründung:** Nutzt die Keyword-Regel denselben Zeitstempel wie die allgemeine Regel, wäre sie eine leere Teilmenge davon. Die Filter-Semantik verlangt, dass unerwünschte Artikel nach Ablauf der Frist seit ihrer Veröffentlichung entfernt werden — unabhängig davon, wann sie zuletzt gelesen wurden.

**Umsetzung:** `RetentionCleanupService.CleanupAsync` (Orchestrierung), `ItemRepository.GetExpiredKeywordCandidatesAsync` (Kandidaten), `ItemRepository.DeleteRangeAsync` (Löschung per IDs).

## Keyword-Matching ist fest verdrahtet

**Beschreibung:** Die Match-Semantik ist Teilwort + case-insensitiv und **nicht** konfigurierbar. In der UI ist das als nicht-interaktives Badge „Immer aktiv" (`SettingsKeywordMatchStatus`) neben der Zeile „Teilwort & Case-Insensitive" dargestellt.

**Bedingungen:**
- Match-Felder: `Item.Title` **und** `Item.ContentHtml`; `Item.Link` wird bewusst nicht gematcht (URLs sind opak, Zufallstreffer-Gefahr).
- Vergleich: `string.Contains(keyword, StringComparison.OrdinalIgnoreCase)`; leere/Whitespace-Keywords werden übersprungen.
- Matching-Zeitpunkt: zur **Cleanup-Zeit** gegen die gespeicherten Artikelinhalte — es gibt kein persistiertes Filter-Flag am `Item`, sodass Keyword-Änderungen beim nächsten App-Start sofort wirken, ohne Artikel neu bewerten zu müssen.

**Umsetzung:** `KeywordMatcher.MatchesAny`

## RetentionDays-Schutzregel

**Beschreibung:** `RetentionDays <= 0` deaktiviert das gesamte Aufräumen (beide Regeln). Ohne diesen Abbruch läge der `cutoff` in der Zukunft und alle gelesenen Artikel würden gelöscht.

**Bedingungen:**
- UI-`Slider` begrenzt die Eingabe auf 1–365 (`MinRetentionDays`/`MaxRetentionDays` im `SettingsViewModel`); `PersistAsync` und `SaveRetention` clampen zusätzlich.
- DB-Default: 30 Tage.

**Umsetzung:** `RetentionCleanupService.CleanupAsync` (Frühabbruch), `SettingsViewModel` (Clamping).

## `AutoMarkReadMode`-String-Konvention

**Beschreibung:** Der globale Ein/Aus-Schalter „Automatisch als gelesen markieren" schreibt kein Boolean, sondern den Modus-String: `"on_open"` bei Ein, `"off"` bei Aus.

**Verhalten:**
- `AutoMarkReadMode != "off"` → automatische Markierung aktiv (`SettingsValues.IsAutoMarkReadEnabled`). Damit bleiben der Seed-Default `"on_scroll"` und bestehende Fallbacks rückwärtskompatibel aktiviert.
- `AutoMarkReadMode == "off"` → `ArticleDetailViewModel` startet den `MarkReadDelayedAsync`-Timer nicht und setzt `IsAutoMarkReadAvailable = false`: Der lokale Schalter in der Detailansicht ist dann deaktiviert und abgedunkelt (`Opacity` 0,4), das Label lautet `ArticleAutoMarkReadDisabled` („Auto-Gelesen (in den Einstellungen deaktiviert)").
- Verzögerung `AutoMarkReadDelaySeconds >= 0` ist zulässig — die Option „Sofort" (0 s) markiert ohne spürbare Wartezeit.
- Der lokale `IsAutoMarkRead`-Toggle in der Detailansicht bleibt eine sitzungsbezogene Abwahl; beide Bedingungen müssen erfüllt sein.

**Umsetzung:** `SettingsValues` (Konstanten `AutoMarkReadOnOpen`/`AutoMarkReadOnScroll`/`AutoMarkReadOff`, Prüfmethode `IsAutoMarkReadEnabled`), `SettingsViewModel.AutoMarkReadEnabled`, `ArticleDetailViewModel.LoadAsync`/`OnAutoMarkReadChanged`/`IsAutoMarkReadAvailable`.

## Refresh-Intervall-Wertebereich

**Beschreibung:** Die UI bietet nur die vier benannten Intervalle 15/30/60/240 Minuten an (`RefreshIntervalOption`); persistiert wird der `int`-Minutenwert (`RefreshIntervalMinutes`, Default 30).

**Verhalten:**
- `AutoRefreshService.ApplySettingsAsync` clampet den gelesenen Wert defensiv auf `[1, 1440]` Minuten (`MinRefreshIntervalMinutes`/`MaxRefreshIntervalMinutes`), falls die DB einen anderen Wert enthält.
- `AutoRefreshEnabled == false` → kein Timer-Loop; `ApplySettingsAsync` stoppt einen laufenden Loop.
- Änderungen am Toggle oder Intervall starten den Timer sofort neu (Aufruf aus `SettingsViewModel.PersistAsync`).

**Umsetzung:** `AutoRefreshService`, `SettingsViewModel.RefreshIntervalOptions`.

## Theme-String und Fallback

**Beschreibung:** `Settings.Theme` ist ein `string?` mit den Werten `"system"`/`"light"`/`"dark"` (Default `"system"`).

**Verhalten:**
- `"light"` → `AppTheme.Light`, `"dark"` → `AppTheme.Dark`, alles andere (`"system"`, `null`, unbekannt) → `AppTheme.Unspecified` (folgt dem Betriebssystem).
- `SettingsViewModel.LoadAsync` fällt bei unbekanntem gespeicherten Wert auf die `ThemeOption` `"system"` zurück.

**Umsetzung:** `AppThemeService.ApplyTheme`, `SettingsViewModel`.

## Ruhezeiten ohne Start-vor-Ende-Validierung

**Beschreibung:** `QuietHoursStart`/`QuietHoursEnd` werden unvalidiert gespeichert; Bereiche über Mitternacht (z. B. 22:00–07:00) sind zulässig. Die Auswertung (Wrap-around, Unterdrückung von Benachrichtigungen, Keyword-Einfluss) ist **nicht** Teil dieses Features und folgt mit dem Benachrichtigungs-Arbeitspaket.

**Verhalten:**
- `QuietHoursEnabled` existiert nur im `SettingsViewModel` — die Aktivierung ergibt sich beim Laden aus `QuietHoursStart is not null || QuietHoursEnd is not null`.
- Ausschalten persistiert `null` für beide Felder; die zuletzt gewählten Werte bleiben in den ViewModel-Feldern erhalten und werden beim Wiedereinschalten derselben Sitzung restauriert.
- Einschalten ohne gesetzte Werte befüllt die Defaults `DefaultQuietHoursStart` (22:00) / `DefaultQuietHoursEnd` (07:00).
- Die `TimePicker` sind per `IsEnabled`-Binding an den Schalter gekoppelt und bei ausgeschalteter Ruhezeit auf `Opacity` 0,4 abgedunkelt.

**Umsetzung:** `SettingsViewModel.QuietHoursEnabled`/`QuietHoursStart`/`QuietHoursEnd` (`TimePicker`-Bindung, `null`-Mapping in `PersistAsync`), `SettingsRepository.SaveAsync`.
