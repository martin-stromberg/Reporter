<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Release Notes

## Important Notes Before Update

- License change: the project is now licensed under the PolyForm Noncommercial License 1.0.0 — free for noncommercial use only; commercial use requires a separate commercial license (see `LICENSE` and `COMMERCIAL-LICENSE.md`).
- GitHub release notes are no longer generated automatically from commits — `docs/RELEASE_NOTES.md` is used as the release body for stable releases and RC pre-releases and must be maintained before each release.

## What's New

- Feed search on the Feeds page: a combined search/URL field accepts a website address or feed URL — matches come from the public feedsearch.dev directory plus client-side feed autodiscovery on the website itself (with "powered by feedsearch.dev" attribution).
- Search results appear as a card list (title, description, site name/address, feed URL); tapping a card subscribes the feed after a confirmation dialog — duplicate check, selected category and the notifications toggle apply.
- No manual title needed for feeds subscribed via search: a missing title is automatically replaced by the real feed title on the first sync.
- Fallbacks retained: if a full URL finds no match or the search is unreachable, the app offers to add the URL directly; while offline the search is disabled with a hint and direct URL entry stays available.
- No free-text or keyword search — matches are only produced for website addresses or URLs.

## Wichtige Hinweise vor dem Update

- Lizenzwechsel: Das Projekt steht jetzt unter der PolyForm Noncommercial License 1.0.0 — kostenlos nur für nicht-kommerzielle Nutzung; für kommerzielle Nutzung ist eine separate kommerzielle Lizenz erforderlich (siehe `LICENSE` und `COMMERCIAL-LICENSE.md`).
- GitHub-Release-Notes werden nicht mehr automatisch aus Commits generiert — als Release-Text für stabile Releases und RC-Pre-Releases dient `docs/RELEASE_NOTES.md`, die vor jedem Release gepflegt werden muss.

## Neuerungen

- Feed-Suche auf der Feeds-Seite: ein kombiniertes Such-/URL-Feld akzeptiert eine Website-Adresse oder Feed-URL — Treffer kommen aus dem öffentlichen Verzeichnis feedsearch.dev sowie aus clientseitiger Feed-Autodiscovery auf der Website selbst (mit Hinweis „powered by feedsearch.dev").
- Suchtreffer erscheinen als Kartenliste (Titel, Beschreibung, Name/Adresse der Website, Feed-URL); ein Tipp auf die Karte abonniert den Feed nach Bestätigungsdialog — Dublettenprüfung, gewählte Kategorie und der Benachrichtigungs-Schalter gelten dabei.
- Kein manueller Titel nötig bei über die Suche abonnierten Feeds: ein fehlender Titel wird beim ersten Abgleich automatisch durch den echten Feed-Titel ersetzt.
- Fallbacks bleiben erhalten: findet eine vollständige URL keinen Treffer oder ist die Suche nicht erreichbar, bietet die App das direkte Hinzufügen der URL an; offline ist die Suche mit Hinweis deaktiviert, die direkte URL-Eingabe bleibt möglich.
- Keine Freitext- oder Stichwortsuche — Treffer entstehen nur bei Website-Adressen oder URLs.
