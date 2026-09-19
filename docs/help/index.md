<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Dokumentation

Übersicht über alle dokumentierten Funktionsbereiche.

## Anwendung

- [Anwendung](anwendung/index.md) — Übersicht, Bedienung, Feed-Suche und Hinzufügen-Flow inkl. Feed-Symbolen (Favicon/Initialen), Offline-Verhalten, Sprache (System/Deutsch/Englisch), Barrierefreiheit, Architektur und Datenmodell der Reporter-App.
- [Benachrichtigungen](benachrichtigungen/index.md) — Reporter informiert auf iOS-Geräten per lokaler Benachrichtigung über neue Artikel: Nach jedem Feed-Abgleich wird geprüft, ob neue Artikel eingetroffen sind, und — abhängig von Schaltern, Ruhezeit und Schlagwort-Filtern — eine Benachrichtigung erzeugt; sichtbar wird sie nur aus dem automatischen Hintergrund-Abgleich bei geschlossener App.

## Konfiguration

- [Einstellungen](einstellungen/index.md) — Die Einstellungen-Seite bündelt alle konfigurierbaren Optionen der Reporter-App: Aufbewahrungsdauer, Keyword-Filter, automatische Aktualisierung, Abruf beim Programmstart, Sortierung der Ungelesen-Liste und Lesefluss, Benachrichtigungen mit Ruhezeiten, das Erscheinungsbild, die Sprache sowie Diagnose & Support (Debug-Sammlung und Debugbericht per E-Mail).

## Systemverwaltung

- [iOS-Deployment](ios-deployment/index.md) — Der lokale iOS-Buildlauf bringt Reporter per Skript auf ein Gerät oder in den App Store/TestFlight: Einmal-Setup (Zertifikat, Profil, API-Key), Upload-Ablauf und Schritt-für-Schritt-Anleitung für TestFlight-Tester und öffentliche Freigabe.
- [Datenschutzerklärung](../privacy-policy.md) — Öffentlich referenzierbare Datenschutzerklärung (deutsch/englisch) der Reporter-App: lokale SQLite-Ablage, Netzwerkzugriffe auf anwenderbestimmte Feed-Server und `feedsearch.dev` sowie der freiwillige Debugbericht per E-Mail; in App Store Connect als Datenschutz-URL zu hinterlegen.
- [App-Store-Review](../app-store-review.md) — Arbeitsnotizen zur App-Store-Einreichung: Antworten zum App-Datenschutz-Fragebogen, ATS-Begründung für `NSAllowsArbitraryLoads`, Review-Hinweis „kein Login — Demo-Feed", Altersfreigabe-Empfehlung, iPad-Entscheidung (nur iPhone) und App-Icon-Verifikation.
- [Release-Management](release-management/index.md) — Die automatisierte Release-Pipeline erzeugt RC-Pre-Releases auf `staging`, Promotion-PRs nach `main`, stabile Releases mit Plattform-Artefakten und Backmerge-PRs zurück nach `staging`.
- [Website](website/index.md) — Die öffentliche Projekt-Website der Reporter-App ist eine zweisprachige (deutsch/englisch) statische Site unter `website/` (Landing-Page, Datenschutzerklärung, Changelog-Verweise, Press Kit); sie wird über den Workflow `deploy-pages.yml` auf GitHub Pages veröffentlicht.
- [Tests](tests/index.md) — Die automatisierte Testinfrastruktur umfasst neben den Unit-Tests eine End-to-End-Smoke-Suite, die die echte Windows-App gegen einen lokalen Test-Webserver fährt, sowie Compiled Bindings auf allen XAML-Views, die Binding-Fehler bereits zur Compile-Zeit sichtbar machen.

## Entwicklung

- [Entwicklung](entwicklung/index.md) — Lokale Qualitätssicherung für Beitragende: Git-Hooks aus `.githooks` (Lokalisierungs-, Lizenz-Header- und Abdeckungs-Checks, Schutz der Branches `main`/`staging`) und der lokale Lauf der CI-Static-Checks.
