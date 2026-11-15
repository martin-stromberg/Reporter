<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Release-Management — Wiederkehrende Reviews

## Zweck

Drei eigenständige GitHub-Actions-Workflows erzeugen zeitgesteuert GitHub-Issues, die wiederkehrende Qualitäts- und Pflegeaufgaben des Repositories anstoßen: ein monatliches Architektur- und Sicherheitsreview, eine wöchentliche NuGet-Paketprüfung und ein jährliches Compliance- und Copyright-Review. Jedes Issue beginnt mit einer `@softwarewerkstatt`-Erwähnung im Body, über die eine externe Automatisierung die Analyse aufnimmt. Eine Duplikatprüfung verhindert, dass ein neuer Zyklus ein weiteres Issue anlegt, solange das vorherige noch offen ist.

## Zeitpläne und Issues

Alle Cron-Angaben laufen in UTC; jeder Workflow lässt sich zusätzlich jederzeit manuell über `workflow_dispatch` starten (z. B. *Actions → Workflow auswählen → Run workflow*).

| Workflow-Datei | Workflow-Name | Zeitplan (UTC) | Issue-Titel (Muster) | Label |
|----------------|---------------|----------------|----------------------|-------|
| `.github/workflows/review-architecture.yml` | `Scheduled Architecture Review` | Monatlich am 1., 06:00 (`0 6 1 * *`) | `Architektur- und Sicherheitsreview <JJJJ-MM>` | `scheduled-architecture-review` |
| `.github/workflows/review-nuget.yml` | `Scheduled NuGet Review` | Wöchentlich montags, 06:00 (`0 6 * * 1`) | `NuGet-Paketprüfung KW <JJJJ-Www>` | `scheduled-nuget-review` |
| `.github/workflows/review-compliance.yml` | `Scheduled Compliance Review` | Jährlich am 1. Januar, 06:00 (`0 6 1 1 *`) | `Compliance- und Copyright-Review <JJJJ>` | `scheduled-compliance-review` |

Die Datumsangaben im Titel werden zur Laufzeit per `date -u` ermittelt und sind damit locale-unabhängig und sortierbar. Die Issue-Bodies enthalten die festen Prüfaufträge an `@softwarewerkstatt` (u. a. Sicherheitsrisiken und CVEs, NuGet-Update-Empfehlungen mit Risikobewertung, Copyright-/Impressums-/Lizenzprüfung).

## Duplikatvermeidung

Alle drei Workflows folgen demselben Muster, das den bestehenden PR-Automatisierungen (`staging-to-main-promotion.yml`, `sync-staging-with-main.yml`) entspricht:

1. `gh label create <label> --force` stellt das typspezifische Label idempotent sicher — beim ersten Lauf wird es angelegt, spätere Läufe aktualisieren es fehlerfrei.
2. `gh issue list --label <label> --state open` listet offene Issues dieses Typs; die Auswertung erfolgt über `jq`.
3. Nur bei leerer Liste wird per `gh issue create` ein neues Issue mit Titel, Body und Label angelegt; existiert bereits ein offenes Issue, endet der Lauf mit einer Log-Meldung ohne weitere Aktion.

Die Duplikatprüfung gilt auch bei manueller Auslösung — ein neuer Zyklus wird bewusst dadurch gestartet, dass das vorherige Review-Issue geschlossen wird. Eine `concurrency`-Gruppe je Workflow (`cancel-in-progress: false`) verhindert parallele Läufe und damit Doppelerzeugung.

## Bereitstellung und Einschränkungen

- **Cron wirkt erst ab `main`:** `schedule`-Trigger laufen ausschließlich auf dem Standardbranch. Bis die Workflow-Dateien über den Promotions-Mechanismus (`staging` → `main`) auf `main` angekommen sind, passiert nichts Zeitgesteuertes — lediglich `workflow_dispatch` steht danach zur Verfügung. Nach dem ersten Merge auf `main` wird der Architektur-Workflow einmalig manuell gestartet, damit Label und erstes Issue sofort entstehen statt erst zum nächsten Monatsersten.
- **Externe Weiterverarbeitung:** Die `@softwarewerkstatt`-Erwähnung ist reiner Issue-Text; ob und wie eine Automatisierung darauf reagiert, liegt außerhalb dieses Repositories.
- **Permissions:** Die Workflows benötigen nur `issues: write` (Label- und Issue-Verwaltung über `GH_TOKEN: ${{ github.token }}`); ein Checkout des Repositories ist nicht erforderlich.
