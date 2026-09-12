<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Release Notes

## Important Notes Before Update

- Branch protection for `main` and `staging` must still be configured manually by a repository admin (required checks `static checks`, `build & test`, `verify-source`); the protection APIs return HTTP 403 for private repositories on the free plan — see `docs/help/release-management/installation.md`.
- Automatic backmerge PRs `main` → `staging` must be merged with "Create a merge commit" — squash or rebase would detach release tags from the `staging` history and break RC numbering.
- The iOS release asset `release-ios.ipa` is only built once the repository variable `IOS_SIGNING_ENABLED=true` is set together with the secrets `IOS_CODESIGN_KEY` and `IOS_PROVISIONING_PROFILE`.
- Old read articles are automatically deleted at app start once they exceed the retention period (default: 30 days) — only articles saved for later are permanently protected; unread articles are never deleted.
- Read, non-saved articles matching a configured keyword filter are also removed by this automatic cleanup once the retention period has expired (keyword list is empty by default).
- Automatic background feed refresh is enabled by default (every 30 minutes while the app is open) — can be changed or disabled in Settings.

## What's New

- Fully automated release pipeline (`staging` → `main`): every push to `staging` produces an RC pre-release `vX.Y.Z-rc.N` with `release-win-x64.zip` (Windows), `release-android.apk` (Android) and the update manifest `update.json`.
- After a successful pre-release run, a draft promotion PR `staging` → `main` is opened automatically (label `automated-promotion`).
- Push to `main` or tag `v*.*.*` creates the stable release `vX.Y.Z` via semantic-release; if a release already exists with incomplete assets, the missing files are repaired (`upload-existing`) instead of creating a new release.
- After every push to `main`, a backmerge PR `main` → `staging` is opened automatically when needed (label `automated-backmerge`).
- Optional Android target: `net10.0-android` can be enabled via the MSBuild switch `-p:IncludeAndroidTarget=true` (requires the Android workload); `IncludeIosTarget` remains enabled by default.
- New administrator documentation for release management under `docs/help/release-management/` (pipeline overview, installation & configuration, iOS signing, troubleshooting).
- Offline reading: already synced articles, feeds, categories and the "saved for later" list remain fully readable without an internet connection; all offline hints disappear automatically once connectivity returns.
- Offline indicators: the sync button is dimmed and a "No internet connection." hint appears on Unread, plus banners on the Feeds and Later pages and in the article detail view — pull-to-refresh and manual syncs are skipped while offline.
- Article reading offline: links inside article text are disabled (a localized hint dialog is shown on tap) and external images plus list thumbnails are hidden to avoid empty image frames.
- "Open in browser" shows a hint instead of failing while offline; automatic background refresh pauses without network and resumes with the next interval (no sync log entries while offline).
- Full localization: all UI texts — page titles, buttons, hints, placeholders and screen reader descriptions — appear in German or English following the device system language (English is the fallback); no manual language switch in the app, the sync log remains English.
- Local iOS notifications for new articles after every feed sync (manual or automatic background refresh) — shown with banner and sound, even while the app is open.
- Per-feed notifications switch on the Feeds page: individual feeds can be muted (enabled by default).
- Quiet hours respected: notifications raised during the configured quiet hours are discarded, not delivered later.
- Keyword-filtered articles do not trigger notifications (same keyword list as automatic cleanup).
- Optional summary mode in Settings: a single summary notification per feed and sync instead of one notification per article.
- Tapping a notification opens the article directly; a summary notification opens the "Unread" view.
- The iOS notification permission is requested when notifications are enabled in Settings; if denied, a hint with an "Open Settings" button is shown on the Settings page. If permission was never requested, a neutral "Allow notifications" row leads directly to the system dialog. On platforms without notifications (currently only iOS supports them), the toggles are disabled with a hint.
- Dismissing a notification (swipe away) no longer triggers navigation — only tapping opens the article or the "Unread" view.
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

- Branch-Protection für `main` und `staging` muss weiterhin manuell durch einen Repository-Admin eingerichtet werden (erforderliche Checks `static checks`, `build & test`, `verify-source`); die Protection-APIs antworten bei privaten Repositories im Free-Plan mit HTTP 403 — siehe `docs/help/release-management/installation.md`.
- Automatische Backmerge-PRs `main` → `staging` müssen per „Create a merge commit" gemergt werden — Squash oder Rebase würde die Release-Tags aus der `staging`-Historie lösen und die RC-Zählung zerstören.
- Das iOS-Release-Asset `release-ios.ipa` wird erst erzeugt, wenn die Repository-Variable `IOS_SIGNING_ENABLED=true` zusammen mit den Secrets `IOS_CODESIGN_KEY` und `IOS_PROVISIONING_PROFILE` gesetzt ist.
- Alte gelesene Artikel werden beim App-Start automatisch gelöscht, sobald sie die Aufbewahrungsdauer überschreiten (Standard: 30 Tage) — nur „für später bewahrte" Artikel sind dauerhaft geschützt; ungelesene Artikel werden nie gelöscht.
- Gelesene, nicht gemerkte Artikel, die einem konfigurierten Keyword-Filter entsprechen, werden ebenfalls nach Ablauf der Aufbewahrungsdauer automatisch gelöscht (Keyword-Liste ist standardmäßig leer).
- Die automatische Hintergrund-Aktualisierung der Feeds ist standardmäßig aktiviert (alle 30 Minuten bei geöffneter App) — kann in den Einstellungen geändert oder deaktiviert werden.

## Neuerungen

- Vollautomatische Release-Pipeline (`staging` → `main`): jeder Push auf `staging` erzeugt ein RC-Pre-Release `vX.Y.Z-rc.N` mit `release-win-x64.zip` (Windows), `release-android.apk` (Android) und dem Update-Manifest `update.json`.
- Nach erfolgreichem Pre-Release-Lauf wird automatisch ein Draft-Promotion-PR `staging` → `main` geöffnet (Label `automated-promotion`).
- Push auf `main` oder Tag `v*.*.*` erzeugt das stabile Release `vX.Y.Z` via semantic-release; existiert ein Release bereits mit unvollständigen Assets, werden die fehlenden Dateien nachgeladen (`upload-existing`), statt ein neues Release anzulegen.
- Nach jedem Push auf `main` wird bei Bedarf automatisch ein Backmerge-PR `main` → `staging` geöffnet (Label `automated-backmerge`).
- Optionales Android-Target: `net10.0-android` lässt sich über den MSBuild-Schalter `-p:IncludeAndroidTarget=true` aktivieren (erfordert den Android-Workload); `IncludeIosTarget` bleibt standardmäßig aktiviert.
- Neue Administratoren-Dokumentation zum Release-Management unter `docs/help/release-management/` (Pipeline-Übersicht, Installation & Konfiguration, iOS-Signierung, Troubleshooting).
- Offline lesen: bereits synchronisierte Artikel, Feeds, Kategorien und die „Später"-Liste bleiben ohne Internetverbindung vollständig lesbar; alle Offline-Hinweise verschwinden bei Netzrückkehr von selbst.
- Offline-Anzeigen: der Aktualisieren-Button wird abgedunkelt und der Hinweis „Keine Internetverbindung." erscheint unter Ungelesen, zusätzlich Banner auf den Seiten Feeds und Später sowie in der Artikeldetailansicht — Ziehen zum Aktualisieren und manuelle Abgleiche werden offline übersprungen.
- Artikel offline lesen: Links im Artikeltext sind deaktiviert (bei Antippen erscheint ein lokalisierter Hinweisdialog), externe Bilder und Listen-Thumbnails werden offline ausgeblendet, damit keine leeren Bildrahmen entstehen.
- „Im Browser öffnen" zeigt offline einen Hinweis statt zu scheitern; die automatische Hintergrund-Aktualisierung pausiert ohne Netzwerk und setzt mit dem nächsten Intervall fort (keine Sync-Protokoll-Einträge offline).
- Vollständige Lokalisierung: alle UI-Texte — Seitentitel, Schaltflächen, Hinweise, Platzhalter und Screenreader-Beschreibungen — erscheinen je nach Systemsprache des Geräts auf Deutsch oder Englisch (Englisch ist die Ersatzsprache); kein manueller Sprachwechsel in der App, das Sync-Protokoll bleibt englisch.
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
