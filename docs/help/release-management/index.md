# Release-Management

Die automatisierte Release-Pipeline des Repositories baut aus jedem geprüften `staging`-Stand ein RC-Pre-Release, öffnet Promotion-PRs nach `main`, erzeugt auf `main` stabile Releases mit allen Plattform-Artefakten und hält `staging` per Backmerge-PR synchron. Zielgruppe dieser Dokumentation sind Maintainer und Betreiber des Repositories.

## Inhalt

- [Beschreibung](beschreibung.md)
- [Technischer Ablauf](ablauf-technisch.md)
- [Architektur](architektur.md)
- [Business Rules](business-rules.md)
- [Installation & Konfiguration](installation.md)
- [Fehlerbehebung](troubleshooting.md)
