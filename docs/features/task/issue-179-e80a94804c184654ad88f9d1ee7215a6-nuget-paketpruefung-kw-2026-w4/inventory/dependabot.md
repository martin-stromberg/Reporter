<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Dependabot, offene PRs und Review-Workflow

## `.github/dependabot.yml` (gesamte Datei, 22 Zeilen)

Drei Update-Ökosysteme, alle wöchentlich (`interval: "weekly"`), alle gegen `target-branch: "staging"`:

| Ökosystem | Verzeichnis | Intervall | Ziel-Branch |
|-----------|-------------|-----------|-------------|
| `nuget` | `/` | weekly | `staging` |
| `npm` | `/` | weekly | `staging` |
| `github-actions` | `/` | weekly | `staging` |

Keine weiteren Einstellungen (kein `open-pull-requests-limit`, keine `groups`, kein `ignore`, keine `reviewers`/`assignees`).

## Offene Pull Requests (Stand 2026-11-17)

`gh pr list --repo martin-stromberg/Reporter --author app/dependabot --state open` → **keine Einträge** (kein offener Dependabot-PR).

`gh pr list --repo martin-stromberg/Reporter --state open` → einziger offener PR:

| # | Titel | Head-Branch | Status |
|---|-------|-------------|--------|
| 184 | Automated promotion from staging to main | `staging` | DRAFT |

Faktische Konsequenz: Kein Abgleich-/Konfliktbedarf mit laufenden Dependabot-Updates zum Zeitpunkt der Bestandsaufnahme.

## `.github/workflows/review-nuget.yml` (42 Zeilen)

- **Name:** `Scheduled NuGet Review`; Trigger: `schedule` cron `0 6 * * 1` (montags 06:00 UTC) + `workflow_dispatch`.
- **Permissions:** nur `issues: write`; `concurrency.group: review-nuget`, `cancel-in-progress: false`.
- **Einziger Job** `create-review-issue` (ubuntu-latest, 10 min Timeout):
  1. `gh label create scheduled-nuget-review --force` (Label `#0075CA`, Beschreibung „Scheduled NuGet package review issue").
  2. `gh issue list --label scheduled-nuget-review --state open`; nur wenn **kein** offenes Issue mit dem Label existiert, wird eines erzeugt: Titel `NuGet-Paketprüfung KW %G-W%V`, Label `scheduled-nuget-review`.
- **Issue-Text (Auftrag an @softwarewerkstatt, wörtlich im Workflow):** Analysiere alle verwendeten NuGet-Pakete; prüfe verfügbare Updates, Security-Updates, bekannt gewordene Schwachstellen, veraltete/nicht mehr gepflegte Pakete, empfohlene Nachfolgepakete, mögliche Breaking Changes. „Erstelle konkrete Update-Empfehlungen inklusive Risikobewertung. Setze einfache Versionshochsetzungen (unbedenkliche Empfehlungen mit niedrigem bis mittlerem Risiko) direkt mit dem Review in einem Pull Request um. Lege für komplexe Befunde (risikoreiche Updates, Breaking Changes, größere Anpassungen) ein oder mehrere Folge-Issues an und verlinke sie im Kommentar dieses Issues. Sind keine einfachen Anpassungen notwendig, schließe dieses Issue nach dem Review, sodass nur die Folge-Issues offen bleiben."
- Das Issue dieser Aufgabe ist **#179 „NuGet-Paketprüfung KW 2026-W47"** (laut `issue.md`).

## Weitere relevante Beobachtungen

- Der Workflow erzeugt nur das Issue — er führt selbst **keine** Paketprüfung aus; die eigentliche Prüfung ist manuell/agentisch (diese Aufgabe).
- Staging-Workflow: PRs für NuGet-Updates gehen gegen `staging` (Dependabot-Ziel und CI-Setup in `pr-staging-ci.yml` legen das nahe); Promotion `staging` → `main` läuft über `staging-to-main-promotion.yml` (aktuell offener Draft-PR #184).
- CI-Checks auf PRs gegen `staging` (`pr-staging-ci.yml`): `detect-backmerge`, `static-checks` (Format, Lizenzheader, Security-Scan inkl. NuGet-Vulnerability + `npm audit --audit-level=high`, Release-Build mit `TreatWarningsAsErrors`), `build-and-test` (Restore `-r win-x64`, Release-Build, `dotnet test` nur für `Reporter.Tests` mit Coverlet-Coverage ≥ 70 % Zeilen). Beide Jobs laufen unter Windows mit `IncludeIosTarget=false`, `IncludeAndroidTarget=false` nach `dotnet workload restore Reporter.sln`.
