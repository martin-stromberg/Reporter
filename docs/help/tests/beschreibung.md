<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Tests — Beschreibung

## Zweck

Die End-to-End-Testinfrastruktur prüft die Reporter-App so, wie ein Anwender sie bedient: Die echte Windows-App wird gestartet, über die Benutzeroberfläche gesteuert und ihre sichtbaren Reaktionen sowie die gespeicherten Daten werden geprüft. Damit werden genau die Fehler sichtbar, die die bisherigen Unit-Tests nicht sehen — etwa falsch verdrahtete Oberflächenelemente, nicht ausgelöste Dialoge oder Tippfehler in Oberflächen-Verknüpfungen. Ergänzend prüft die Oberflächendefinition der App ihre Verknüpfungen bereits beim Übersetzen, sodass solche Tippfehler künftig gar nicht mehr erst unbemerkt zur Laufzeit scheitern können.

## Funktionsweise

- **Lokaler Prüflauf:** Ein Startskript (`scripts/Run-E2ETests.ps1`) übersetzt die App und führt die Smoke-Suite aus dem Projekt `src/Reporter.E2ETests` aus. Die Suite startet die App selbst, klickt sich durch die wichtigsten Abläufe und räumt danach alles wieder auf.
- **Echte App, kontrollierte Umgebung:** Die App läuft vollständig echt — echte Bedienung, echte lokale Datenspeicherung. Lediglich das Feed-Verzeichnis und die abgerufenen Feeds kommen von einem lokalen Test-Webserver mit festen, wiederholbaren Antworten; die Datendateien (Nutzerdaten- und Inhalts-Datenbank) liegen in einem temporären Ordner, damit die eigene Entwicklungsumgebung unverändert bleibt. Der Beispiel-Feed, den die App beim allerersten Start anlegt, wird für die Suite unterdrückt, damit die Umgebung hermetisch bleibt.
- **Neun geprüfte Kernabläufe:** Die App startet und zeigt die Feed-Liste; die Schaltfläche zum Hinzufügen öffnet das Eingabeformular mit Fokus im Adressfeld; ein direkt hinzugefügter Feed erscheint in der Liste und wird gespeichert; ein Feed lässt sich über sein Aktionsmenü umbenennen; die Kategorie eines Feeds lässt sich setzen und über „Keine Kategorie" wieder entfernen; die Suche findet einen bekannten Eintrag und der Abo-Dialog speichert den Feed; die Suche findet einen Feed auch dann, wenn die Website ihn nur selbst anbietet (automatische Erkennung); ein Link im geöffneten Artikel verlässt die App und öffnet den externen Browser, während die Detailansicht geöffnet bleibt — nachgewiesen über den Abruf-Zähler des Testservers. Ein weiterer Test belegt den umgekehrten Pfad: Auf einer frischen Datendatei und ohne Unterdrückung legt die App beim ersten Start die Kategorie „News" und den Beispiel-Feed „Apple Newsroom" selbst an — nachgewiesen über die gespeicherten Datensätze und die sichtbare Feed-Karte.
- **Übersetzungszeit-Prüfung der Oberflächen-Verknüpfungen:** Alle Ansichten der App geben ihrer Oberfläche jetzt den Typ der dahinterliegenden Daten bekannt. Dadurch werden Verknüpfungsfehler beim Übersetzen als Fehler gemeldet statt still zur Laufzeit zu scheitern.
- **Bewusste Abgrenzung:** Die Suite läuft nur unter Windows und benötigt eine sichtbare, interaktive Sitzung — sie ist als lokale, bei Bedarf gestartete Prüfung gedacht und läuft nicht in der automatischen Build-Pipeline. Die bisherigen Unit-Tests und die manuelle Sichtprüfung von Optik, Themes und Layout bleiben unverändert bestehen.

## Beispiele

- Ein Entwickler ändert das Umbenennen eines Feeds: `.\scripts\Run-E2ETests.ps1` aus dem Repository-Root fährt die App hoch, benennt einen Feed über dasselbe Menü um wie ein Anwender und meldet grün, wenn der neue Titel auf der Karte erscheint.
- Beim Übersetzen der App meldet der Build sofort einen Fehler, wenn eine Oberflächen-Verknüpfung auf ein nicht vorhandenes Datenfeld zeigt — früher wäre das erst beim Blick auf die betroffene Ansicht aufgefallen.

## Einschränkungen

- Die Suite prüft ausschließlich die Windows-App; über das Verhalten auf iOS, Android oder Mac sagt sie nichts aus.
- Sie deckt neun Kernabläufe ab — es gibt keinen Anspruch auf vollständige Oberflächenabdeckung. Optik, Themes und Layout werden weiterhin manuell per Screenshot geprüft.
- Ein Lauf benötigt eine interaktive Desktop-Sitzung und eine zuvor übersetzte App; auf Rechnern ohne sichtbare Oberfläche (etwa reine Build-Server) ist er nicht ausführbar.
- Da die Steuerung über die Oberfläche läuft, können einzelne Prüfungen bei laufenden Bedienhandlungen am selben Rechner gestört werden — das Skript sollte ohne parallele Eingaben laufen.
