<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Automatisierung wiederkehrender Architektur-, Sicherheits- und Compliance-Reviews (GitHub Issue #165)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Workflows | `.github/workflows/review-architecture.yml` anlegen (Cron `0 6 1 * *` + `workflow_dispatch`, `issues: write`, Concurrency `review-architecture`, Label `scheduled-architecture-review`, Titel „Architektur- und Sicherheitsreview JJJJ-MM", Architektur-Body mit `@softwarewerkstatt`) | Offen | — |
| 2 | Workflows | `.github/workflows/review-nuget.yml` anlegen (Cron `0 6 * * 1` + `workflow_dispatch`, `issues: write`, Concurrency `review-nuget`, Label `scheduled-nuget-review`, Titel „NuGet-Paketprüfung KW JJJJ-Www", NuGet-Body mit `@softwarewerkstatt`) | Offen | — |
| 3 | Workflows | `.github/workflows/review-compliance.yml` anlegen (Cron `0 6 1 1 *` + `workflow_dispatch`, `issues: write`, Concurrency `review-compliance`, Label `scheduled-compliance-review`, Titel „Compliance- und Copyright-Review JJJJ", Compliance-Body mit `@softwarewerkstatt`) | Offen | — |
| 4 | Verifikation | YAML-Syntax der drei neuen Workflow-Dateien lokal prüfen (Python `yaml.safe_load` oder `yamllint`) sowie Cron-Ausdrücke und Body-Texte gegen die Anforderung sichtprüfen | Offen | — |
| 5 | Dokumentation | `docs/help/release-management/wiederkehrende-reviews.md` anlegen (Zeitpläne, Titel-/Label-Schema, Dedup-Mechanismus, manuelle Auslösung, Cold-Start-Hinweis, `@softwarewerkstatt`-Einschränkung) | Offen | — |
| 6 | Dokumentation | `docs/help/release-management/index.md`: Inhaltsliste um „Wiederkehrende Reviews" ergänzen | Offen | — |
| 7 | Dokumentation | `docs/help/release-management/architektur.md`: Komponententabelle um die drei `review-*.yml`-Workflows ergänzen | Offen | — |
| 8 | Dokumentation | `docs/help/release-management/installation.md`: Permissions-Tabelle um drei Zeilen (`issues: write`) und Installationsschritte um Label-/Erstlauf-Hinweis ergänzen | Offen | — |
| 9 | Dokumentation | `docs/help/index.md`: Release-Management-Eintrag in „Systemverwaltung" um die Review-Workflows ergänzen | Offen | — |
| 10 | Dokumentation | `README.md`: Release-Management-Bullet unter „Weitere Informationen → Betrieb und Entwicklung" um wiederkehrende Review-Issues ergänzen | Offen | — |
| 11 | Verifikation | `.\scripts\Run-StaticChecks.ps1` ausführen — Exit-Code 0 ohne Befund (v. a. `LicenseHeaders`) | Offen | — |
| 12 | Verifikation | Nach Merge/Promotion auf `main`: `Scheduled Architecture Review` einmal per `workflow_dispatch` starten (Label + Issue prüfen), Zweitlauf zur Dedup-Verifikation, Nacharbeit dokumentieren | Offen | — |
