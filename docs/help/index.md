<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Dokumentation

Übersicht über alle dokumentierten Funktionsbereiche.

## Anwendung

- [Anwendung](anwendung/index.md) — Übersicht, Bedienung, Feed-Suche und Hinzufügen-Flow inkl. Feed-Symbolen (Favicon/Initialen), Offline-Verhalten, Sprache (System/Deutsch/Englisch), Barrierefreiheit, Architektur und Datenmodell der Reporter-App.
- [Benachrichtigungen](benachrichtigungen/index.md) — Reporter informiert auf iOS-Geräten per lokaler Benachrichtigung über neue Artikel: Nach jedem Feed-Abgleich wird geprüft, ob neue Artikel eingetroffen sind, und — abhängig von Schaltern, Ruhezeit und Schlagwort-Filtern — eine Benachrichtigung angezeigt.

## Konfiguration

- [Einstellungen](einstellungen/index.md) — Die Einstellungen-Seite bündelt alle konfigurierbaren Optionen der Reporter-App: Aufbewahrungsdauer, Keyword-Filter, automatische Aktualisierung, Abruf beim Programmstart, Sortierung der Ungelesen-Liste und Lesefluss, Benachrichtigungen mit Ruhezeiten, das Erscheinungsbild, die Sprache sowie Diagnose & Support (Debug-Sammlung und Debugbericht per E-Mail).

## Systemverwaltung

- [Release-Management](release-management/index.md) — Die automatisierte Release-Pipeline erzeugt RC-Pre-Releases auf `staging`, Promotion-PRs nach `main`, stabile Releases mit Plattform-Artefakten und Backmerge-PRs zurück nach `staging`.
