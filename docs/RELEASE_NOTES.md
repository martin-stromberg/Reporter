<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Release Notes

## Important Notes Before Update

- License change: the project is now licensed under the PolyForm Noncommercial License 1.0.0 — free for noncommercial use only; commercial use requires a separate commercial license (see `LICENSE` and `COMMERCIAL-LICENSE.md`).
- GitHub release notes are no longer generated automatically from commits — `docs/RELEASE_NOTES.md` is used as the release body for stable releases and RC pre-releases and must be maintained before each release.

## What's New

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

- Die Feeds-Seite zeigt zunächst nur die Feed-Liste; „+ Feed per URL hinzufügen" öffnet ein Bottom-Sheet mit URL-Feld sowie den Schaltflächen „Suchen" und „URL direkt hinzufügen" — das direkte Hinzufügen bleibt offline verfügbar (die Suche ist mit Hinweis deaktiviert).
- Feed-Kontextmenü erweitert: „Umbenennen" (Dialog) und „Kategorie ändern" (ActionSheet inklusive „Keine Kategorie"); „Bearbeiten" öffnet dasselbe Bottom-Sheet im Bearbeitungsmodus mit URL, Benachrichtigungs-Schalter und Speichern.
- Feeds ohne Titel erhalten einen aus der URL abgeleiteten Ersatztitel (Dateiname wie `heise-atom.xml`, sonst der Host); beim ersten Abgleich wird er durch den echten Feed-Titel ersetzt.
- Feed-Suche im Hinzufügen-Sheet: eine Website-Adresse oder Feed-URL eingeben und „Suchen" tippen fragt das öffentliche Verzeichnis feedsearch.dev sowie clientseitige Feed-Autodiscovery auf der Website selbst ab (mit Hinweis „powered by feedsearch.dev").
- Suchtreffer erscheinen als Kartenliste (Titel, Beschreibung, Name/Adresse der Website, Feed-URL); ein Tipp auf die Karte abonniert den Feed nach Bestätigungsdialog mit Dublettenprüfung — neue Feeds starten ohne Kategorie und mit aktivierten Benachrichtigungen.
- Fallbacks bleiben erhalten: findet eine vollständige URL keinen Treffer oder ist die Suche nicht erreichbar, bietet die App das direkte Hinzufügen der URL an.
- Keine Freitext- oder Stichwortsuche — Treffer entstehen nur bei Website-Adressen oder URLs.
