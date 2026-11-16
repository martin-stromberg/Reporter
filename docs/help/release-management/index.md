<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Release-Management

Die automatisierte Release-Pipeline des Repositories baut aus jedem geprüften `staging`-Stand ein RC-Pre-Release, öffnet Promotion-PRs nach `main`, erzeugt auf `main` stabile Releases mit allen Plattform-Artefakten und hält `staging` per Backmerge-PR synchron. Ergänzend erzeugen drei zeitgesteuerte Workflows wiederkehrende Review-Issues (Architektur/Sicherheit, NuGet, Compliance) mit Duplikatvermeidung über Labels, und Dependabot öffnet wöchentlich Update-PRs für NuGet-, npm- und GitHub-Actions-Abhängigkeiten — sämtliche externen Actions sind dabei auf Commit-SHAs gepinnt, und das Security-Gate prüft NuGet- wie npm-Abhängigkeiten auf verwundbare Pakete. Zielgruppe dieser Dokumentation sind Maintainer und Betreiber des Repositories.

## Inhalt

- [Beschreibung](beschreibung.md)
- [Technischer Ablauf](ablauf-technisch.md)
- [Architektur](architektur.md)
- [Business Rules](business-rules.md)
- [Installation & Konfiguration](installation.md)
- [Wiederkehrende Reviews](wiederkehrende-reviews.md)
- [Fehlerbehebung](troubleshooting.md)
