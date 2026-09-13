<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Release Notes

## Important Notes Before Update

- License change: the project is now licensed under the PolyForm Noncommercial License 1.0.0 — free for noncommercial use only; commercial use requires a separate commercial license (see `LICENSE` and `COMMERCIAL-LICENSE.md`).
- GitHub release notes are no longer generated automatically from commits — `docs/RELEASE_NOTES.md` is used as the release body for stable releases and RC pre-releases and must be maintained before each release.

## What's New

- Unified design across all pages: cards with subtle borders and consistent rounding, refined typography (Newsreader headlines, Inter UI text) and harmonized colors in light and dark mode.
- Unread page: the category filter is now a horizontally scrolling chip bar that shows each category's unread count — the previous funnel button and action sheet are replaced.
- Article view: the action bar is now a floating, pill-shaped control bar with a translucent background and shadow.
- Feeds page: feed health is shown as a compact colored status badge (status dot on a tinted pill) instead of plain colored text.
- Article cards now mark unread articles with a small dot, show the estimated reading time, and highlight saved articles with a gold bookmark.
- "Saved for later" loads articles in pages while scrolling (infinite scroll) with a loading indicator and an error message if loading fails.
- New icons for all five tabs in the bottom tab bar.
- New app icon and splash screen in the app's slate-blue design color, replacing the default .NET icon.
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

- Einheitliches Design auf allen Seiten: Karten mit feinen Rahmen und konsistenten Rundungen, verfeinerte Typografie (Newsreader für Überschriften, Inter für UI-Texte) und abgestimmte Farben im hellen und dunklen Modus.
- Ungelesen-Seite: Der Kategoriefilter ist jetzt eine horizontal scrollbare Chip-Leiste, die die Ungelesen-Anzahl je Kategorie anzeigt — der bisherige Trichter-Button mit Aktionsmenü entfällt.
- Artikelansicht: Die Aktionsleiste ist jetzt eine schwebende, pillenförmige Steuerleiste mit transluzentem Hintergrund und Schatten.
- Feeds-Seite: Der Feed-Status wird als kompaktes farbiges Status-Badge (Statuspunkt auf getönter Pille) statt als einfacher farbiger Text angezeigt.
- Artikelkarten markieren ungelesene Artikel mit einem kleinen Punkt, zeigen die geschätzte Lesezeit und heben gespeicherte Artikel mit einem goldenen Lesezeichen hervor.
- „Später lesen" lädt Artikel seitenweise beim Scrollen nach (Infinite Scroll) — mit Ladeanzeige und Fehlermeldung, falls das Laden scheitert.
- Neue Symbole für alle fünf Tabs in der unteren Tab-Leiste.
- Neues App-Symbol und Splash-Screen in der schieferblauen Designfarbe der App statt des Standard-.NET-Symbols.
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
