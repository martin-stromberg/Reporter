<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Release Notes

## Important Notes Before Update

- License change: the project is now licensed under the PolyForm Noncommercial License 1.0.0 — free for noncommercial use only; commercial use requires a separate commercial license (see `LICENSE` and `COMMERCIAL-LICENSE.md`).
- GitHub release notes are no longer generated automatically from commits — `docs/RELEASE_NOTES.md` is used as the release body for stable releases and RC pre-releases and must be maintained before each release.

## What's New

- Fixed: feeds published in the legacy Atom 0.3 format (e.g. sportschau.de) now synchronize — previously the refresh failed with an "invalid feed format" error.
- Internal quality measure — no user-facing change: new end-to-end test suite for the Windows app (`src/Reporter.E2ETests`, invoked via `scripts/Run-E2ETests.ps1`) that launches the app against a local stub server and smoke-tests the main screens via FlaUI/UIA3 — enabled by compiled bindings on all views and the test-only environment variables `REPORTER_FEEDSEARCH_ENDPOINT` and `REPORTER_DB_PATH`.
- iOS: feeds are now also refreshed while the app is closed, via an OS-scheduled background refresh task — the schedule follows the "Automatic background refresh" switch and the "Fetch interval" setting, and the task is cancelled when automatic refresh is disabled.
- iOS: system notifications for new articles now only appear when the app is not in the foreground (the OS background refresh) — while the app is open (manual sync, refresh timer, refresh on start) no banner or sound is shown; Windows and Android are unchanged.
- HTTP feeds (`http://` URLs) now also sync on iOS and Mac Catalyst — cleartext connections are permitted via App Transport Security.
- Feeds page: for a feed with a sync error the action menu now offers "Show error details" — an alert names the cause (unencrypted connection blocked, HTTP error, network problem, invalid feed format or unknown) and shows the technical detail message; previously only "Error" was displayed.
- Settings: new "Diagnostics & support" section with an opt-in switch "Collect debug information" — the setting is persisted, the session log is cleared on every app start, and crash entries from the previous session are kept so they can still be sent after a restart.
- "Send debug report" in the same section opens a prefilled e-mail draft in the device's mail app containing app, device and OS information, the current settings, feed health, the sync log and the session debug log.
- Reading time is no longer shown for articles with an estimated reading time of one minute or less — in the article lists and the article detail view.
- Placeholder image cascade on article cards: article image → feed favicon → a circle with the feed's initial letter; the feeds page also shows each feed's favicon with the initial circle as fallback.
- Favicon discovery: a feed's website favicon is detected when the feed is added and is backfilled automatically for existing feeds during sync.
- Settings: new "Refresh on app start" switch (default: on) — feeds are loaded when the app starts while online.
- Settings: new "Sort order of unread articles" picker with "Newest first" (default) and "Oldest first".
- The restart hint below the language picker now only appears after the language has actually been changed and disappears again when the saved language is reselected.
- Redesigned app icon and splash screen: a rounded badge with RSS signal arcs and an amber accent dot; the splash screen additionally shows the "Reporter" wordmark.
- Fixed keyword filter: articles matching a filter keyword are now discarded while the feed is being retrieved and no longer appear in the article lists — the filtered count is noted in the internal sync log.
- Unified design across all pages: cards with subtle borders and consistent rounding, refined typography (Newsreader headlines, Inter UI text) and harmonized colors in light and dark mode.
- Unread page: the category filter is now a horizontally scrolling chip bar that shows each category's unread count — the previous funnel button and action sheet are replaced.
- Article view: the action bar is now a floating, pill-shaped control bar with a translucent background and shadow.
- Feeds page: feed health is shown as a compact colored status badge (status dot on a tinted pill) instead of plain colored text.
- Article cards now mark unread articles with a small dot, show the estimated reading time, and highlight saved articles with a gold bookmark.
- "Saved for later" loads articles in pages while scrolling (infinite scroll) with a loading indicator and an error message if loading fails.
- New icons for all five tabs in the bottom tab bar.
- Accessibility: screen reader descriptions for all icon-only buttons and cards (including selected state and bookmark set/remove), texts scale with the device's font size setting, improved color contrast.
- Faster feed sync: new articles are inserted in a single batch with in-memory duplicate detection instead of one database write per article.
- Settings: new "Language" section with a "Language" picker offering "System" (default, follows the device language), "German" and "English" — the selection is saved immediately and takes effect after restarting the app (a hint is shown below the picker).
- After the restart all visible texts — including number and date formats — appear in the selected language, independent of the device language.
- Feeds page now opens with just the feed list; "+ Add feed by URL" opens a bottom sheet with a URL field plus "Search" and "Add URL directly" buttons — direct add stays available offline (search is disabled with a hint).
- Feed context menu extended: "Rename" (prompt dialog) and "Change category" (action sheet including "No category"); "Edit" opens the same bottom sheet in edit mode with URL, notifications toggle and Save.
- Feeds subscribed without a title get a fallback title derived from the URL (file name such as `heise-atom.xml`, otherwise the host); the first sync replaces it with the real feed title.
- Feed search in the add sheet: entering a website address or feed URL and tapping "Search" queries the public feedsearch.dev directory plus client-side feed autodiscovery on the website itself (with "powered by feedsearch.dev" attribution).
- Search results appear as a card list (title, description, site name/address, feed URL); tapping a card subscribes the feed after a confirmation dialog with duplicate check — new feeds start without a category and with notifications enabled.
- Fallbacks retained: if a full URL finds no match or the search is unreachable, the app offers to add the URL directly.
- No free-text or keyword search — matches are only produced for website addresses or URLs.

## Wichtige Hinweise vor dem Update

- Lizenzwechsel: Das Projekt steht jetzt unter der PolyForm Noncommercial License 1.0.0 — kostenlos nur für nicht-kommerzielle Nutzung; für kommerzielle Nutzung ist eine separate kommerzielle Lizenz erforderlich (siehe `LICENSE` und `COMMERCIAL-LICENSE.md`).
- GitHub-Release-Notes werden nicht mehr automatisch aus Commits generiert — als Release-Text für stabile Releases und RC-Pre-Releases dient `docs/RELEASE_NOTES.md`, die vor jedem Release gepflegt werden muss.

## Neuerungen

- Korrigiert: Feeds im veralteten Atom-0.3-Format (z. B. sportschau.de) werden jetzt synchronisiert — zuvor schlug der Abruf mit dem Fehler „ungültiges Feed-Format" fehl.
- Interne Qualitätsmaßnahme — keine anwendersichtbare Änderung: neue End-to-End-Testsuite für die Windows-App (`src/Reporter.E2ETests`, Aufruf via `scripts/Run-E2ETests.ps1`), die die App gegen einen lokalen Stub-Server startet und die Hauptansichten per FlaUI/UIA3-Smoke-Tests prüft — ermöglicht durch Compiled Bindings auf allen Views und die nur für Testzwecke gedachten Umgebungsvariablen `REPORTER_FEEDSEARCH_ENDPOINT` und `REPORTER_DB_PATH`.
- iOS: Feeds werden jetzt auch bei geschlossener App aktualisiert — über einen vom System eingeplanten Hintergrundabruf, der den Einstellungen „Automatische Hintergrund-Aktualisierung" und „Abruf-Intervall" folgt und bei deaktiviertem automatischem Abruf abgemeldet wird.
- iOS: Systembenachrichtigungen über neue Artikel erscheinen nur noch, wenn die App nicht im Vordergrund läuft (OS-Hintergrundabruf) — bei geöffneter App (manueller Abgleich, Abruf-Timer, Abruf beim Start) werden Banner und Ton unterdrückt; Windows und Android bleiben unverändert.
- HTTP-Feeds (`http://`-URLs) werden jetzt auch unter iOS und Mac Catalyst abgeglichen — unverschlüsselte Verbindungen sind per App Transport Security freigegeben.
- Feeds-Seite: Bei einem Feed mit Sync-Fehler bietet das Aktionsmenü jetzt „Fehlerdetails anzeigen" — ein Dialog nennt die Ursache (unverschlüsselte Verbindung blockiert, HTTP-Fehler, Netzwerkproblem, ungültiges Feed-Format oder unbekannt) und zeigt die technische Detailmeldung; bisher stand dort nur „Fehler".
- Einstellungen: neue Sektion „Diagnose & Support" mit Opt-in-Schalter „Debuginformationen sammeln" — die Einstellung wird gespeichert, das Sitzungsprotokoll bei jedem App-Start zurückgesetzt; Absturz-Einträge der vorherigen Sitzung bleiben erhalten, damit sie nach einem Neustart noch versendet werden können.
- „Debugbericht senden" in derselben Sektion öffnet einen vorbefüllten E-Mail-Entwurf in der Mail-App des Geräts — mit App-, Geräte- und OS-Informationen, den aktuellen Einstellungen, dem Feed-Status, dem Sync-Protokoll und dem Sitzungsprotokoll.
- Die Lesezeit wird bei Artikeln mit einer geschätzten Lesezeit von einer Minute oder weniger nicht mehr angezeigt — in den Artikellisten und in der Artikeldetailansicht.
- Platzhalterbild-Kaskade auf Artikelkarten: Artikelbild → Feed-Favicon → Kreis mit dem Anfangsbuchstaben des Feeds; die Feeds-Seite zeigt ebenfalls das Favicon jedes Feeds mit dem Initialen-Kreis als Ersatz.
- Favicon-Ermittlung: Das Website-Favicon eines Feeds wird beim Hinzufügen erkannt und bei bestehenden Feeds beim Abgleich automatisch nachgerüstet.
- Einstellungen: neuer Schalter „Beim Programmstart abrufen" (Voreinstellung: an) — bei bestehender Verbindung werden die Feeds beim Start der App geladen.
- Einstellungen: neues Auswahlfeld „Sortierung der ungelesenen Artikel" mit „Neueste zuerst" (Voreinstellung) und „Älteste zuerst".
- Der Neustart-Hinweis unter der Sprachauswahl erscheint jetzt erst nach tatsächlicher Änderung der Sprache und verschwindet wieder, sobald die gespeicherte Sprache erneut gewählt wird.
- Neu gestaltetes App-Symbol und Splash-Screen: ein abgerundetes Badge mit RSS-Signalbögen und bernsteinfarbenem Akzentpunkt; der Splash-Screen zeigt zusätzlich den Schriftzug „Reporter".
- Schlagwortfilter korrigiert: Artikel, die auf ein Filter-Schlagwort passen, werden jetzt bereits beim Abruf des Feeds verworfen und erscheinen nicht mehr in den Artikellisten — die Anzahl der verworfenen Artikel wird im internen Sync-Protokoll vermerkt.
- Einheitliches Design auf allen Seiten: Karten mit feinen Rahmen und konsistenten Rundungen, verfeinerte Typografie (Newsreader für Überschriften, Inter für UI-Texte) und abgestimmte Farben im hellen und dunklen Modus.
- Ungelesen-Seite: Der Kategoriefilter ist jetzt eine horizontal scrollbare Chip-Leiste, die die Ungelesen-Anzahl je Kategorie anzeigt — der bisherige Trichter-Button mit Aktionsmenü entfällt.
- Artikelansicht: Die Aktionsleiste ist jetzt eine schwebende, pillenförmige Steuerleiste mit transluzentem Hintergrund und Schatten.
- Feeds-Seite: Der Feed-Status wird als kompaktes farbiges Status-Badge (Statuspunkt auf getönter Pille) statt als einfacher farbiger Text angezeigt.
- Artikelkarten markieren ungelesene Artikel mit einem kleinen Punkt, zeigen die geschätzte Lesezeit und heben gespeicherte Artikel mit einem goldenen Lesezeichen hervor.
- „Später lesen" lädt Artikel seitenweise beim Scrollen nach (Infinite Scroll) — mit Ladeanzeige und Fehlermeldung, falls das Laden scheitert.
- Neue Symbole für alle fünf Tabs in der unteren Tab-Leiste.
- Barrierefreiheit: Screenreader-Beschreibungen für alle Schaltflächen und Karten ohne sichtbaren Text (inkl. Auswahlzustand und Lesezeichen setzen/entfernen), Texte skalieren mit der Schriftgröße des Geräts, verbesserte Farbkontraste.
- Schnellerer Abgleich: Neue Artikel werden gesammelt in einem Schreibvorgang gespeichert — mit Duplikaterkennung im Speicher statt eines Datenbankzugriffs pro Artikel.
- Einstellungen: neue Sektion „Sprache" mit Auswahlfeld „Sprache" — Optionen „System" (Voreinstellung, folgt der Gerätesprache), „Deutsch" und „Englisch"; die Auswahl wird sofort gespeichert und wirkt nach einem Neustart der App (Hinweis unter dem Auswahlfeld).
- Nach dem Neustart erscheinen alle sichtbaren Texte — inklusive Zahlen- und Datumsformaten — in der gewählten Sprache, unabhängig von der Gerätesprache.
- Die Feeds-Seite zeigt zunächst nur die Feed-Liste; „+ Feed per URL hinzufügen" öffnet ein Bottom-Sheet mit URL-Feld sowie den Schaltflächen „Suchen" und „URL direkt hinzufügen" — das direkte Hinzufügen bleibt offline verfügbar (die Suche ist mit Hinweis deaktiviert).
- Feed-Kontextmenü erweitert: „Umbenennen" (Dialog) und „Kategorie ändern" (ActionSheet inklusive „Keine Kategorie"); „Bearbeiten" öffnet dasselbe Bottom-Sheet im Bearbeitungsmodus mit URL, Benachrichtigungs-Schalter und Speichern.
- Feeds ohne Titel erhalten einen aus der URL abgeleiteten Ersatztitel (Dateiname wie `heise-atom.xml`, sonst der Host); beim ersten Abgleich wird er durch den echten Feed-Titel ersetzt.
- Feed-Suche im Hinzufügen-Sheet: eine Website-Adresse oder Feed-URL eingeben und „Suchen" tippen fragt das öffentliche Verzeichnis feedsearch.dev sowie clientseitige Feed-Autodiscovery auf der Website selbst ab (mit Hinweis „powered by feedsearch.dev").
- Suchtreffer erscheinen als Kartenliste (Titel, Beschreibung, Name/Adresse der Website, Feed-URL); ein Tipp auf die Karte abonniert den Feed nach Bestätigungsdialog mit Dublettenprüfung — neue Feeds starten ohne Kategorie und mit aktivierten Benachrichtigungen.
- Fallbacks bleiben erhalten: findet eine vollständige URL keinen Treffer oder ist die Suche nicht erreichbar, bietet die App das direkte Hinzufügen der URL an.
- Keine Freitext- oder Stichwortsuche — Treffer entstehen nur bei Website-Adressen oder URLs.
