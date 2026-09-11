# Release Notes

## Important Notes Before Update

- Old read articles are automatically deleted at app start once they exceed the retention period (default: 30 days) — only articles saved for later are permanently protected; unread articles are never deleted.
- Read, non-saved articles matching a configured keyword filter are also removed by this automatic cleanup once the retention period has expired (keyword list is empty by default).
- Automatic background feed refresh is enabled by default (every 30 minutes while the app is open) — can be changed or disabled in Settings.

## What's New

- Local iOS notifications for new articles after every feed sync (manual or automatic background refresh) — shown with banner and sound, even while the app is open.
- Per-feed notifications switch on the Feeds page: individual feeds can be muted (enabled by default).
- Quiet hours respected: notifications raised during the configured quiet hours are discarded, not delivered later.
- Keyword-filtered articles do not trigger notifications (same keyword list as automatic cleanup).
- Optional summary mode in Settings: a single summary notification per feed and sync instead of one notification per article.
- Tapping a notification opens the article directly; a summary notification opens the "Unread" view.
- The iOS notification permission is requested when notifications are enabled in Settings; if denied, a hint with an "Open Settings" button is shown on the Settings page.
- Notifications appear at most once per article (deduplication via stable identifiers).
- Retention slider: value changes (e.g. via keyboard) are now saved after a short delay (~0.5 s), not only when the slider is released.
- Localization fixes: the auto-read label in the article detail now follows the UI language (previously hardcoded German); clearer info texts for retention and keyword matching on the Settings page.
- Quiet hours can now be toggled on/off on the Settings page; when off, the from/to time pickers are disabled and no quiet hours are stored (defaults 10:00 PM–7:00 AM if never configured).
- The local "auto mark as read" switch in the article detail view is disabled and dimmed when the global option is turned off.
- Settings page polish: the fixed keyword-matching mode is shown as a non-interactive "Always active" badge; screen reader descriptions added for keyword chips and toggles.
- New full-featured Settings page: retention period, keyword filter, synchronization & reading flow, notifications & quiet hours, and appearance — every change is saved instantly.
- Keyword filter (blacklist): keywords managed as chips on the Settings page; matching is case-insensitive substring matching on article title and content.
- Automatic background refresh: periodically syncs all feeds while the app is open, with selectable intervals of 15/30/60/240 minutes.
- Appearance setting: light, dark, or system theme applied at app start and immediately on change.
- Global "auto mark as read" option honored by the article detail view; configurable delay including "immediately" (0 s).
- "Save for later" feature: articles can be bookmarked via the bookmark icon in the article list and in the article detail view.
- New "Later" tab listing all saved articles, sorted by publication date.
- Automatic retention cleanup at app start removes expired read articles (see important notes above).

## Wichtige Hinweise vor dem Update

- Alte gelesene Artikel werden beim App-Start automatisch gelöscht, sobald sie die Aufbewahrungsdauer überschreiten (Standard: 30 Tage) — nur „für später bewahrte" Artikel sind dauerhaft geschützt; ungelesene Artikel werden nie gelöscht.
- Gelesene, nicht gemerkte Artikel, die einem konfigurierten Keyword-Filter entsprechen, werden ebenfalls nach Ablauf der Aufbewahrungsdauer automatisch gelöscht (Keyword-Liste ist standardmäßig leer).
- Die automatische Hintergrund-Aktualisierung der Feeds ist standardmäßig aktiviert (alle 30 Minuten bei geöffneter App) — kann in den Einstellungen geändert oder deaktiviert werden.

## Neuerungen

- Lokale iOS-Benachrichtigungen bei neuen Artikeln nach jedem Feed-Abgleich (manuell oder automatische Hintergrund-Aktualisierung) — mit Banner und Ton, auch bei geöffneter App.
- Pro-Feed-Schalter für Benachrichtigungen auf der Feeds-Seite: einzelne Feeds lassen sich stummschalten (standardmäßig aktiviert).
- Ruhezeiten werden berücksichtigt: Benachrichtigungen während der konfigurierten Ruhezeit werden verworfen, nicht nachgeholt.
- Keyword-gefilterte Artikel lösen keine Benachrichtigung aus (gleiche Schlagwort-Liste wie beim automatischen Löschen).
- Optionaler Sammel-Modus in den Einstellungen: eine Sammel-Benachrichtigung pro Feed und Abgleich statt einer Benachrichtigung pro Artikel.
- Antippen einer Benachrichtigung öffnet direkt den Artikel; bei einer Sammel-Benachrichtigung öffnet sich die Ansicht „Ungelesen".
- Die iOS-Benachrichtigungs-Berechtigung wird beim Aktivieren in den Einstellungen angefragt; bei Verweigerung zeigt die Einstellungsseite einen Hinweis mit Schaltfläche „Einstellungen öffnen". Wurde die Berechtigung noch nie angefragt, bietet eine neutrale Zeile „Benachrichtigungen erlauben" den direkten Weg zum System-Dialog. Auf Plattformen ohne Benachrichtigungen (derzeit nur iOS) sind die Schalter deaktiviert und mit einem Hinweis versehen.
- Das Wegwischen einer Benachrichtigung (Dismiss) löst keine Navigation mehr aus — nur das Antippen öffnet den Artikel bzw. die Ansicht „Ungelesen".
- Benachrichtigungen erscheinen höchstens einmal pro Artikel (Deduplizierung über stabile Kennungen).
- Aufbewahrungsdauer-Regler: Wertänderungen (z. B. per Tastatur) werden jetzt nach kurzer Verzögerung (~0,5 s) gespeichert, nicht nur beim Loslassen des Reglers.
- Lokalisierungs-Fixes: das „Auto-Gelesen“-Label in der Artikeldetailansicht folgt jetzt der UI-Sprache (vorher fest auf Deutsch); klarere Hinweistexte für Aufbewahrungsdauer und Keyword-Abgleich auf der Einstellungen-Seite.
- Ruhezeiten lassen sich auf der Einstellungen-Seite jetzt ein- und ausschalten; bei ausgeschalteter Ruhezeit sind die VON/BIS-Felder deaktiviert und es werden keine Ruhezeiten gespeichert (Vorgabe 22:00–07:00, falls nie konfiguriert).
- Der lokale „Auto-Gelesen"-Schalter in der Artikeldetailansicht ist deaktiviert und abgedunkelt, wenn die globale Option ausgeschaltet ist.
- Feinschliff der Einstellungen-Seite: der feste Keyword-Matching-Modus wird als nicht-interaktives Badge „Immer aktiv" angezeigt; Screenreader-Beschreibungen für Keyword-Chips und Schalter ergänzt.
- Neue vollwertige Einstellungen-Seite: Aufbewahrungsdauer, Keyword-Filter, Synchronisation & Lesefluss, Benachrichtigungen & Ruhezeiten sowie Erscheinungsbild — jede Änderung wird sofort gespeichert.
- Schlagwort-Filter: Schlagworte werden als Chips auf der Einstellungen-Seite verwaltet; der Abgleich erfolgt case-insensitiv als Teilwort auf Artikeltitel und -inhalt.
- Automatische Hintergrund-Aktualisierung: synchronisiert periodisch alle Feeds bei geöffneter App, wählbare Intervalle 15/30/60/240 Minuten.
- Erscheinungsbild-Einstellung: helles, dunkles oder System-Theme, angewendet beim App-Start und sofort bei Änderung.
- Globale Option „Automatisch als gelesen markieren" wird von der Artikeldetailansicht berücksichtigt; Verzögerung konfigurierbar inklusive „Sofort" (0 s).
- „Für später bewahren"-Funktion: Artikel können per Lesezeichen-Symbol in der Artikelliste und in der Artikeldetailansicht gemerkt werden.
- Neuer Tab „Später" listet alle gemerkten Artikel, sortiert nach Veröffentlichungsdatum.
- Automatische Aufbewahrungs-Löschung beim App-Start entfernt abgelaufene gelesene Artikel (siehe wichtige Hinweise oben).
