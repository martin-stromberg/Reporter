<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Website — Beschreibung

## Zweck

Die Projekt-Website macht die Reporter-App öffentlich sichtbar: Sie stellt die App vor, liefert Presse-Material und beantwortet die Fragen, die Interessierte, Tester und Journalisten vor einer Veröffentlichung im App Store haben — ohne dass dafür ein eigener Server oder ein Content-Management-System nötig wäre. Die Site wird über GitHub Pages betrieben und ist unter `https://martin-stromberg.github.io/Reporter/` erreichbar.

## Funktionsweise

Die Website ist vollständig zweisprachig: Deutsch ist die Standardsprache, über einen Sprachumschalter im Kopfbereich jeder Seite wechseln Besucher zur englischen Version (und zurück). Jede Seite trägt dieselbe Hauptnavigation (Start bzw. Home, Datenschutz/Privacy, Changelog, Presse/Press, GitHub) und einen gemeinsamen Fußbereich mit Kontakt- und Lizenzangaben.

Die Site umfasst pro Sprache vier Seiten:

- **Startseite:** Hero-Bereich mit App-Icon, App-Name und dem Slogan „Deine Feeds. Dein Gerät. Kein Tracking." (englisch: „Your feeds. Your device. No tracking."), eine Beschreibung der App samt Zielgruppe und Nutzen, die Feature-Liste, iOS-Screenshots und ein eingebettetes Demo-Video (GIF) der App. Ein Download-Bereich zeigt einen „Bald im App Store"-Hinweis und verlinkt auf die GitHub-Releases-Seite. Dazu kommen Support- und Kontaktangaben (E-Mail, GitHub Issues) sowie Informationen zur Entwicklung und Lizenz.
- **Datenschutz:** Die Datenschutzerklärung der App als HTML-Seite — deutsch als vollständiger Text, englisch als Zusammenfassung. Jede Seite verweist auf das maßgebliche Originaldokument im Repository.
- **Changelog:** Verweise auf die GitHub-Releases-Liste als primäre Quelle für Versionshinweise sowie auf das `changes.log` und die Release Notes im Repository.
- **Presse/Press Kit:** Download-Bereich für Logo- und App-Icon-Dateien (Vektor-Master als SVG plus PNG-Varianten in 1024-, 512- und 256-Pixel-Größe), alle iOS-Screenshots als Einzel-Downloads, ein kurzer und ein längerer Beschreibungstext zur freien Verwendung in Berichten sowie der Presse-Kontakt.

Die Seiten passen sich der Bildschirmgröße an (Smartphone wie Desktop) und respektieren den Dunkelmodus des Betriebssystems.

## Beispiele

- Eine Journalistin, die über lokale Feed-Reader berichten möchte, lädt über die Presse-Seite App-Icon und Screenshots herunter und kopiert den vorbereiteten Kurztext in ihren Artikel.
- Ein App-Store-Besucher prüft vor der Installation über die Datenschutz-Seite, dass Reporter ohne Konto, Backend, Tracking und Werbung auskommt.
- Ein Interessierter verfolgt über die Changelog-Seite den Versionsfortschritt und findet von dort zu den GitHub-Releases mit den Build-Artefakten.

## Einschränkungen

- Die App ist noch nicht im App Store veröffentlicht — der Download-Bereich zeigt daher einen „Bald im App Store"-Hinweis statt eines Store-Links; sobald die App veröffentlicht ist, wird der Hinweis durch den echten Link ersetzt.
- Es gibt keinen Android-Download und keine APK-Links — Reporter ist derzeit eine reine iOS-App (iPhone).
- Die Datenschutz- und Presse-Texte auf der Website sind Kopien der Repository-Originaldokumente; maßgeblich bleibt immer die Version im Repository (siehe [Business Rules](business-rules.md)).
- Es existiert kein vollständiges Impressum mit Anschrift; als Betreiber ist im Fußbereich Martin Stromberg mit Kontakt-E-Mail genannt.
- Screenshots und Demo-Material spiegeln den App-Stand zum Aufnahmezeitpunkt und können nach UI-Änderungen veralten.
