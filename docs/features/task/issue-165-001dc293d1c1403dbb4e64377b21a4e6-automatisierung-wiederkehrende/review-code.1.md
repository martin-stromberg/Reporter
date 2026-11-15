<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### .github/workflows/review-architecture.yml, review-nuget.yml, review-compliance.yml (Job `create-review-issue` — identischer Befund in allen drei Dateien)

- **Fehlende Vorbedingung / funktionaler Fehler** — Die `gh`-CLI-Aufrufe (`gh label create` in Schritt „Ensure … label exists", `gh issue list`/`gh issue create` in Schritt „Create review issue if none open") laufen ohne `actions/checkout` und ohne gesetzte `GH_REPO`-Umgebungsvariable bzw. `--repo`-Flag. Die `gh`-CLI löst das Ziel-Repository ausschließlich über Git-Remotes des Arbeitsverzeichnisses oder die Env-Variable `GH_REPO` auf — `GITHUB_REPOSITORY` wird **nicht** ausgewertet (offenes Enhancement cli/cli#3556). Ohne Checkout existiert kein Git-Repository auf dem Runner, daher schlagen alle drei `gh`-Aufrufe fehl (`failed to run git: fatal: not a git repository` / „no git remotes found"). Die Workflows würden damit beim ersten Lauf — inklusive des vom Auftraggeber geforderten manuellen `workflow_dispatch`-Verifikationslaufs — komplett fehlschlagen. Die Planbegründung „reine `gh`-CLI-Operationen benötigen keinen Repository-Inhalt" (plan.md, Zeile 15) verwechselt Repository-*Inhalt* mit Repository-*Kontext*.

  Empfehlung: Auf Job-Ebene `env: GH_REPO: ${{ github.repository }}` ergänzen (minimalinvasiv, kein Checkout nötig), alternativ jedem `gh`-Aufruf `--repo ${{ github.repository }}` mitgeben oder — analog zu `staging-to-main-promotion.yml` und `sync-staging-with-main.yml` — einen `actions/checkout`-Schritt voranstellen. Beispiel:

  ```yaml
  jobs:
    create-review-issue:
      runs-on: ubuntu-latest
      timeout-minutes: 10
      env:
        GH_REPO: ${{ github.repository }}
  ```

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `.github/workflows/review-architecture.yml`
- `.github/workflows/review-nuget.yml`
- `.github/workflows/review-compliance.yml`

Geprüft, aber ohne Befund geblieben (zur Dokumentation):

- **Issue-Body-Texte:** Alle drei `--body`-Strings sind zeichengenau identisch mit den Vorgaben in `requirement.md` (programmatischer Abgleich), inklusive führender `@softwarewerkstatt`-Erwähnung.
- **Dedup-Logik:** `gh issue list --label <typ-label> --state open --json number` + `jq length` verhindert ein neues Issue bei offenem Vorgänger korrekt; das Muster entspricht exakt `staging-to-main-promotion.yml`/`sync-staging-with-main.yml`. Das Default-Limit 30 von `gh issue list` ist unkritisch, da nur `length > 0` geprüft wird.
- **Race-Bedingung list→create:** Durch `concurrency`-Gruppe pro Workflow mit `cancel-in-progress: false` serienreif abgesichert.
- **Berechtigungen:** `issues: write` reicht für `gh label create` (Label-API ist Teil der Issues-API), `gh issue list` und `gh issue create` aus; `contents: read` ist ohne Checkout nicht erforderlich.
- **Shell-Robustheit:** `set -euo pipefail` im mehrzeiligen `run`-Block vorhanden; alle Variablen expansionsfähig gequotet (`"$existing"`); `${{ }}`-Ausdrücke nur in `env:`-Blöcken, keine Template-Injection im `run`-Skript; `date -u`-Formate (`%Y-%m`, `%G-W%V` für ISO-Kalenderwoche, `%Y`) korrekt und locale-unabhängig.
- **YAML:** `python -c "yaml.safe_load"` erfolgreich für alle drei Dateien; `on:`/`schedule`/`workflow_dispatch` korrekt strukturiert; Cron-Ausdrücke entsprechen der Anforderung (`0 6 1 * *` monatlich, `0 6 * * 1` wöchentlich, `0 6 1 1 *` jährlich; UTC).
- **Lizenzheader:** Korrekte `#`-Kommentar-Kopfzeile in Zeile 1 aller drei Dateien (License-Header-Check).
- **Namenskonventionen:** Dateinamen und Label-Strings (`scheduled-architecture-review`, `scheduled-nuget-review`, `scheduled-compliance-review`) konsistent in Kleinbuchstaben mit Bindestrichen, Farbcodes und Beschreibungen je Label gesetzt; `gh label create --force` idempotent.
