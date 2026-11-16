<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Supply-Chain-Härtung (Dependabot, SHA-Pinning, npm-Audit-Gate)

## Übersicht

Drei Lücken in der kontinuierlichen Dependency-/Supply-Chain-Überwachung der CI/CD-Pipeline werden geschlossen: (1) Neue `.github/dependabot.yml` für die Ökosysteme `nuget`, `npm` und `github-actions` mit wöchentlichem Intervall gegen `staging`, (2) Pinning aller 49 externen `uses:`-Referenzen (45 in 7 Workflows, 4 in Composite Actions) auf Commit-SHAs, (3) Erweiterung der Composite Action `.github/actions/security-scan` um ein npm-Audit-Gate gegen `package-lock.json` inkl. lokaler Parität in `scripts/Run-StaticChecks.ps1`. Kein Anwendungscode betroffen — der Eingriff liegt vollständig auf CI/CD-, Konfigurations- und Dokumentationsebene.

## Scope-Klärung

Verbindliche fachliche Abgrenzung vor der Umsetzung — übernommen bzw. konkretisiert aus dem Abschnitt `Fachlicher Scope` der `requirement.md`:

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Drei Einheiten: (a) wöchentlicher Dependabot-Update-Lauf je Ökosystem (`nuget`, `npm`, `github-actions`), der Update-PRs gegen `staging` erzeugt; (b) einzelne externe Action-Referenz `uses: <owner>/<repo>@<ref>` in Workflows und Composite Actions, die unveränderbar auf einen Commit-SHA gepinnt wird; (c) ein Lauf der Composite Action `.github/actions/security-scan`, der NuGet- und npm-Abhängigkeiten gemeinsam gate-prüft |
| Auslösende Ereignisse | Dependabot-Schedule (wöchentlich) pro Ökosystem; bestehende CI-Trigger `pull_request → staging` (`pr-staging-ci.yml`), `push → staging` (`staging-ci.yml`), Cron `0 6 * * 1` bzw. `workflow_dispatch` (`security-scan.yml`) — alle rufen `.github/actions/security-scan` auf und durchlaufen damit automatisch das erweiterte npm-Gate |
| Erwartete Wirkung | Dependabot öffnet wöchentlich Update-PRs gegen `staging`; alle 49 externen `uses:`-Referenzen zeigen auf Commit-SHAs mit `# vN`-Kommentar; der Security-Scan schlägt (`::error::` + `exit 1`) zusätzlich bei npm-Schwachstellen der Severity `high`/`critical` aus `package-lock.json` fehl; der Report (`vulnerable-packages.txt` + `npm-audit.txt`) landet im Scan-Artefakt |
| Umgang mit fehlenden Objekten | `.github/dependabot.yml` → **erzeugen** (`version: 2`, drei `updates`-Einträge, `schedule.interval: weekly`, `target-branch: "staging"`, PolyForm-`#`-Header). `package-lock.json` → **wiederverwenden** (Audit-Basis, kein `npm ci` im Gate). Node/npm auf `windows-latest`-Runnern → **wiederverwenden** (vorinstalliert, kein `setup-node` im Gate). Lizenzheader in neuer `.yml`-Datei → **erzeugen** (wird von `add-license-headers.mjs --check` in `static-checks` geprüft) |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine Auto-Merge-Automatisierung für Dependabot-PRs — PRs durchlaufen die bestehenden `PR CI for Staging`-Gates und werden manuell gemergt | Anforderung schließt explizit aus | bestätigt (2026-11-16, automatisierte SCM-Bearbeitung: Ausschluss folgt direkt aus der Anforderung) |
| 2 | Lokale Referenzen `uses: ./.github/actions/*` (12×) werden nicht gepinnt | Anforderung schließt aus; technisch nicht möglich (lokale Pfade ohne `@<ref>`) | bestätigt (2026-11-16, automatisierte SCM-Bearbeitung: technisch zwingend) |
| 3 | Keine Behebung der 35 moderaten npm-Audit-Befunde in dieser Aufgabe — das Gate wird auf `--audit-level=high` gesetzt und bleibt ab Tag 1 grün | Einziger verfügbarer Fix (`npm audit fix --force`) wäre ein Breaking-Downgrade auf `semantic-release@21.1.2`; Anforderung schließt Dependency-Versionsupdates aus | bestätigt (2026-11-16, automatisierte SCM-Bearbeitung: einzig vertretbare Option) |
| 4 | Keine Erweiterung der Überwachung auf weitere Ökosysteme (`pip`, `docker` u. a.) | Nicht Teil der Anforderung; `website/` enthält kein eigenes `package.json` | bestätigt (2026-11-16, automatisierte SCM-Bearbeitung: Ausschluss folgt direkt aus der Anforderung) |
| 5 | Keine Änderung an Permissions-, Runner- oder Trigger-Modellen der Workflows | Anforderung schließt aus | bestätigt (2026-11-16, automatisierte SCM-Bearbeitung: Ausschluss folgt direkt aus der Anforderung) |
| 6 | Die wöchentlichen Scheduled-Review-Issues (`review-nuget.yml` u. a.) bleiben unverändert bestehen — Dependabot ergänzt, ersetzt sie nicht | Anforderung schließt aus | bestätigt (2026-11-16, automatisierte SCM-Bearbeitung: Ausschluss folgt direkt aus der Anforderung) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| npm-Audit-Gate (Audit-Umfang) | Audit des vollständigen Lockfiles via `npm audit --package-lock-only` — **kein** `--omit=dev` | Sämtliche npm-Abhängigkeiten sind `devDependencies` (semantic-release-Toolchain); `--omit=dev` würde nichts prüfen und die vom Issue genannten Schwachstellen nicht finden. `--package-lock-only` auditiert `package-lock.json` ohne `node_modules`/`npm ci`. Die Anforderung erlaubt mit „bzw. Audit des Lockfiles" diese sinnvollere Variante |
| npm-Audit-Gate (Schwellenwert) | `--audit-level=high` — Gate schlägt nur bei `high`/`critical` fehl | Aktueller Befund: 35 moderate, 0 hohe/kritische Schwachstellen. Ein Gate ab `moderate` wäre ab Tag 1 rot, ohne dass die Befunde im Scope behebbar wären (Fix nur per Breaking-Downgrade). Die NuGet-Prüfung bleibt unverändert bei „jeder gemeldete Befund" |
| Pinning-Reichweite | Externe Referenzen **auch innerhalb** der Composite Actions pinnen (4 Referenzen in `security-scan`, `build-and-package`, `package-android`, `package-ios`) | Gleicher Supply-Chain-Vektor wie in Workflows; Dependabot (`github-actions`-Ökosystem) aktualisiert `action.yml`-Dateien unter `.github/actions/` ebenfalls |
| Pinning-Schreibweise | `uses: <owner>/<action>@<commit-sha> # vN` — bisheriger Tag bleibt als Kommentar erhalten | Erhält Lesbarkeit; Dependabot pflegt SHA und Kommentar bei Updates mit |
| Report-Dateien im Gate | Getrennte Datei `npm-audit.txt` neben `vulnerable-packages.txt`, beide über mehrzeiligen `path:` ins bestehende Upload-Artefakt | Getrennte Ursachen (NuGet vs. npm) bleiben unterscheidbar; kein neuer Upload-Step, kein neuer Input nötig |
| Node-Verfügbarkeit im Gate | Vorinstalliertes Node/npm auf `windows-latest` wiederverwenden — kein `setup-node`-Step im Gate | GitHub-gehostete Runner-Images enthalten Node.js LTS inkl. npm standardmäßig; verifiziertes Verhalten. Fallback, falls der CI-Lauf es widerlegt: `actions/setup-node` (gepinnt) als zusätzlichen Step in den Aufrufer-Jobs ergänzen |
| Lokale Parität | `Invoke-SecurityScan` in `scripts/Run-StaticChecks.ps1` um denselben npm-Audit (`--package-lock-only --audit-level=high`) erweitern | AGENTS.md verlangt, dass das Skript den CI-`static-checks`-Job spiegelt; ohne Erweiterung driftet der lokale Check vom erweiterten Gate ab |
| Dependabot-PR-Metadaten | Defaults verwenden (kein `open-pull-requests-limit`, keine Labels/Reviewer, kein `commit-message.prefix`) | Anforderung nicht spezifiziert; GitHub-Default (Limit 5 PRs je Ökosystem) ist angemessen |
| `ci-instructions.md` | Nachziehen: §6.2-Generierungsvorlage `security-scan` um npm-Step und SHA-Pinning aktualisieren, `dependabot.yml` in der Pipeline-Beschreibung ergänzen | Die Pipeline wurde nach dieser Vorlage erzeugt; ohne Aktualisierung driftet die Referenzdokumentation weiter von der Implementierung ab |

## Programmabläufe

### Dependabot-Update-Lauf (wöchentlich, je Ökosystem)

1. GitHub liest `.github/dependabot.yml` vom Default-Branch (`main`) — die Konfiguration wird daher erst nach der Promotion `staging → main` wirksam.
2. Zum wöchentlichen Schedule-Zeitpunkt (oder bei manuellem „Check for updates" unter *Insights → Dependency graph → Dependabot*) prüft Dependabot pro Ökosystem: `nuget` scannt `Reporter.sln` + `src/**/*.csproj` ab `/`, `npm` scannt `package.json`/`package-lock.json` ab `/`, `github-actions` scannt `.github/workflows/*.yml` und `.github/actions/*/action.yml`.
3. Für verfügbare Updates öffnet Dependabot PRs mit `target-branch: "staging"` (gilt für Version-Updates); GitHub-Actions-PRs heben dabei die gepinnten Commit-SHAs samt `# vN`-Kommentar an.
4. Jeder Dependabot-PR durchläuft `pr-staging-ci.yml` (inkl. des erweiterten Security-Gates) und wird manuell gemergt.

Beteiligte Komponenten: `.github/dependabot.yml`, `Reporter.sln`, `src/**/*.csproj`, `package.json`, `package-lock.json`, `.github/workflows/*.yml`, `.github/actions/*/action.yml`, `.github/workflows/pr-staging-ci.yml`

### Security-Scan-Lauf mit npm-Gate

1. Ein Aufrufer-Workflow (`pr-staging-ci.yml`, `staging-ci.yml` oder `security-scan.yml`) erreicht im Job `static-checks` bzw. `security-scan` den Step `uses: ./.github/actions/security-scan` (lokal, unverändert).
2. Step `Scan for vulnerable packages` (bestehend, unverändert): `dotnet list "${{ inputs.solution-path }}" package --vulnerable --include-transitive` → `vulnerable-packages.txt`; bei `grep`-Match `::error::` + `exit 1`.
3. Neuer Step `Audit npm toolchain` (`shell: bash`): `npm audit --package-lock-only --audit-level=high` im Repo-Root; Ausgabe via `tee` nach `npm-audit.txt`; bei Exit-Code ≠ 0 `::error::` + `exit 1`.
4. Step `Upload report` (`if: always()`): lädt `vulnerable-packages.txt` **und** `npm-audit.txt` (mehrzeiliger `path:`) als `${{ inputs.artifact-name }}` hoch — auch im Fehlerfall steht der Report zur Diagnose bereit.

Beteiligte Komponenten: `.github/actions/security-scan/action.yml`, `package.json`, `package-lock.json`, `.github/workflows/pr-staging-ci.yml`, `.github/workflows/staging-ci.yml`, `.github/workflows/security-scan.yml`

### Lokaler Static-Check-Lauf mit npm-Parität

1. `scripts/Run-StaticChecks.ps1 -Check Security` ruft `Invoke-SecurityScan`.
2. Bestehend: `dotnet list Reporter.sln package --vulnerable --include-transitive` → Fail bei Match.
3. Neu: anschließend `npm audit --package-lock-only --audit-level=high` im Repo-Root → Fail bei Exit-Code ≠ 0 (identische Schwelle wie das CI-Gate).
4. Gesamtlauf `Run-StaticChecks.ps1` (Format, Security, LicenseHeaders, Build) bleibt bei 0 hohen/kritischen npm-Befunden grün.

Beteiligte Komponenten: `scripts/Run-StaticChecks.ps1` (`Invoke-SecurityScan`), `package-lock.json`

### SHA-Pinning (einmaliger Migrationsablauf bei der Umsetzung)

1. Für jede der 8 distinkten externen Actions wird der Commit-SHA des referenzierten Tags ermittelt: `git ls-remote https://github.com/<owner>/<repo> refs/tags/<tag>` — bei annotierten Tags muss `refs/tags/<tag>^{}` (dereferenzierter Commit) verwendet werden.
2. Mapping-Tabelle Tag → SHA erstellen: `actions/checkout@v7`, `actions/setup-dotnet@v6`, `actions/setup-node@v7`, `actions/upload-artifact@v7`, `actions/download-artifact@v7`, `actions/configure-pages@v5`, `actions/upload-pages-artifact@v3`, `actions/deploy-pages@v4`.
3. In den 7 Workflow-Dateien (45 Referenzen) und 4 Composite Actions (4 Referenzen) jede Zeile `uses: <owner>/<action>@vN` durch `uses: <owner>/<action>@<sha>  # vN` ersetzen.
4. Lokale Referenzen `uses: ./.github/actions/...` bleiben unverändert. Workflows ohne externe Referenzen (`verify-pr-source.yml`, `review-architecture.yml`, `review-nuget.yml`, `review-compliance.yml`) bleiben unverändert.

Beteiligte Komponenten: `.github/workflows/staging-ci.yml` (17), `.github/workflows/release.yml` (13), `.github/workflows/pr-staging-ci.yml` (7), `.github/workflows/deploy-pages.yml` (4), `.github/workflows/security-scan.yml` (2), `.github/workflows/staging-to-main-promotion.yml` (1), `.github/workflows/sync-staging-with-main.yml` (1), `.github/actions/security-scan/action.yml` (1), `.github/actions/build-and-package/action.yml` (1), `.github/actions/package-android/action.yml` (1), `.github/actions/package-ios/action.yml` (1)

## Neue Klassen

Keine — der Eingriff liegt vollständig auf CI/CD-/Konfigurationsebene. Einzige neue Datei: `.github/dependabot.yml` (Konfiguration, keine Klasse).

## Änderungen an bestehenden Dateien und Komponenten

### `.github/dependabot.yml` (neu anzulegen)

- PolyForm-Lizenzheader als `#`-Kommentar in Zeile 1 (Pflicht für `.yml`, geprüft durch `add-license-headers.mjs --check`).
- `version: 2`
- Drei `updates`-Einträge, jeweils mit `schedule.interval: weekly` und `target-branch: "staging"`:
  - `package-ecosystem: "nuget"`, `directory: "/"` (erfasst `Reporter.sln` + `src/**/*.csproj`)
  - `package-ecosystem: "npm"`, `directory: "/"` (erfasst `package.json`/`package-lock.json`)
  - `package-ecosystem: "github-actions"`, `directory: "/"` (erfasst Workflows *und* `action.yml`-Dateien → hält gepinnte SHAs aktuell)
- Keine weiteren Optionen (`open-pull-requests-limit`, `labels`, `reviewers`, `commit-message.prefix`) — Defaults.

### `.github/actions/security-scan/action.yml` (Composite Action)

- **Neuer Step** `Audit npm toolchain` (`shell: bash`, nach dem NuGet-Step): `npm audit --package-lock-only --audit-level=high`, Ausgabe nach `npm-audit.txt` (`tee`), bei Exit-Code ≠ 0 `::error::` + `exit 1`. Muster analog zum bestehenden NuGet-Step (`set -o pipefail`).
- **Geänderter Step** `Upload report`: `path:` wird mehrzeilig um `npm-audit.txt` ergänzt.
- **Gepinnte Referenz:** `actions/upload-artifact@v7` → Commit-SHA + `# v7`-Kommentar.
- Keine neuen Inputs (`solution-path`, `artifact-name` bleiben); die drei Aufrufstellen benötigen keine Änderung.

### `.github/workflows/*` — SHA-Pinning (7 Dateien, 45 Referenzen)

| Datei | Referenzen | Betroffene Actions |
|-------|-----------|---------------------|
| `staging-ci.yml` | 17 | `checkout`, `setup-dotnet`, `setup-node`, `upload-artifact`, `download-artifact` |
| `release.yml` | 13 | `checkout`, `setup-dotnet`, `setup-node`, `upload-artifact`, `download-artifact` |
| `pr-staging-ci.yml` | 7 | `checkout`, `setup-dotnet`, `upload-artifact` |
| `deploy-pages.yml` | 4 | `checkout`, `configure-pages`, `upload-pages-artifact`, `deploy-pages` |
| `security-scan.yml` | 2 | `checkout`, `setup-dotnet` |
| `staging-to-main-promotion.yml` | 1 | `checkout` |
| `sync-staging-with-main.yml` | 1 | `checkout` |

Jede Referenz wird zu `uses: <action>@<sha>  # vN`. `with:`-Blöcke, Job-Struktur und lokale `uses:` bleiben unverändert.

### `.github/actions/build-and-package`, `package-android`, `package-ios` (je 1 Referenz)

- `actions/setup-dotnet@v6` → Commit-SHA + `# v6`-Kommentar (Zeilen 17 bzw. 57). Sonst keine Änderungen.

### `scripts/Run-StaticChecks.ps1`

- **Geänderte Funktion** `Invoke-SecurityScan` (Zeilen 61–75): nach der bestehenden NuGet-Prüfung `npm audit --package-lock-only --audit-level=high` ausführen; bei Exit-Code ≠ 0 als Check-Fehler werten (konsistent zum bestehenden Fehler-Reporting der Funktion).

### Dokumentation

- `docs/help/release-management/architektur.md` — Komponententabelle: `dependabot.yml` als neue Komponente ergänzen; Rolle von `security-scan` auf „NuGet + npm" erweitern; SHA-Pinning der externen Referenzen vermerken.
- `docs/help/release-management/ablauf-technisch.md` — Gate-Beschreibung (`static-checks`/`security-scan.yml`) um den npm-Audit-Schritt und den Schwellenwert `high` ergänzen.
- `docs/help/release-management/wiederkehrende-reviews.md` — Zusammenspiel Dependabot ↔ bestehende Scheduled-Review-Issues beschreiben; Hinweis auf Dependabot-Security-Updates gegen `main` (siehe Seiteneffekte).
- `ci-instructions.md` — §6.2-Generierungsvorlage `security-scan/action.yml` um npm-Step und SHA-Pinning aktualisieren; `dependabot.yml` und die Pinning-Konvention in der Pipeline-Beschreibung ergänzen (Drift vermeiden).

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Externe `uses:`-Referenz | Muss auf 40-stelligen Commit-SHA zeigen, Original-Tag als `# vN`-Kommentar; lokale `./.github/actions/*`-Referenzen ausgenommen | Versehentlich beweglicher Tag bleibt → Nachprüfung per `grep -nE "uses: [a-z0-9_-]+/[a-z0-9_-]+@v?[0-9]"` über `.github/**/*.yml` muss leer sein |
| `dependabot.yml` | `version: 2`; genau die drei Ökosysteme `nuget`, `npm`, `github-actions`; `target-branch: "staging"`; PolyForm-Header | `add-license-headers.mjs --check` schlägt fehl; YAML-Syntaxprüfung (`yaml.safe_load`) schlägt fehl |
| npm-Abhängigkeiten (`package-lock.json`) | `npm audit --package-lock-only --audit-level=high` Exit-Code 0 | Exit ≠ 0 → Gate-Fail (`::error::` + `exit 1`), Befund im Artefakt `npm-audit.txt` |
| NuGet-Abhängigkeiten (`Reporter.sln`) | Unverändert: `dotnet list package --vulnerable --include-transitive` ohne Befund | Exit/Fail wie bisher (`grep`-Match → `exit 1`) |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `.github/dependabot.yml` → `updates[].package-ecosystem` | Konfigurationsdatei | `nuget`, `npm`, `github-actions` | Zu überwachende Ökosysteme |
| `.github/dependabot.yml` → `updates[].directory` | Konfigurationsdatei | `/` | Scan-Wurzel (Solution, package.json, `.github/`) |
| `.github/dependabot.yml` → `updates[].schedule.interval` | Konfigurationsdatei | `weekly` | Update-Frequenz |
| `.github/dependabot.yml` → `updates[].target-branch` | Konfigurationsdatei | `staging` | Zielbranch der Version-Update-PRs (Staging-Workflow) |
| npm-Audit-Schwelle im Gate | Konvention in `action.yml` + `Run-StaticChecks.ps1` | `--audit-level=high` | Fail-Schwelle des npm-Gates (moderate Befunde werden gemeldet, blockieren nicht) |

## Seiteneffekte und Risiken

- **CI-Last:** Dependabot erzeugt bis zu 15 PRs pro Woche (Default-Limit 5 je Ökosystem), jeder durchläuft `pr-staging-ci.yml`. Akzeptabel, manuell steuerbar über `open-pull-requests-limit`, falls nötig.
- **Dependabot-Security-Update-PRs ignorieren `target-branch`:** Security-Updates (nicht Version-Updates) zielen plattformbedingt auf den Default-Branch `main` — ihre Branches (`dependabot/...`) würden an `verify-pr-source.yml` scheitern (`head_ref == 'staging'` erwartet). → siehe Offener Punkt 1.
- **Wirksamkeit erst nach Promotion:** `dependabot.yml` wird nur vom Default-Branch gelesen — Update-PRs beginnen erst, nachdem der Stand `main` erreicht hat. Kein Code-Risiko, aber relevant für die Abnahme (Akzeptanzszenario 1).
- **Moderate npm-Befunde bleiben sichtbar:** `npm audit` meldet weiterhin 35 moderate Befunde in `npm-audit.txt`/lokal — sie blockieren nicht, können aber bei manueller Lektüre als „neues Problem" missverstanden werden; Doku nennt den Stand und den Schwellenwert.
- **Node-Verfügbarkeit auf `windows-latest`:** Annahme (standardmäßig vorinstalliert). Falls ein CI-Lauf `npm: command not found` zeigt: `actions/setup-node` (gepinnt) in den drei Aufrufer-Jobs ergänzen — isolierte Nachbesserung ohne Designänderung.
- **Pinning-Erhaltung:** Ohne Dependabot würden gepinnte SHAs veralten — der `github-actions`-Eintrag in `dependabot.yml` ist damit fachlich notwendige Bedingung, nicht optional.
- **`Microsoft.Maui.Controls` (`$(MauiVersion)`):** SDK-gesteuert, von Dependabot nicht direkt anhebbar — NuGet-PRs betreffen die übrigen gepinnten Pakete; kein Fehler, aber Erwartungsmanagement in der Doku.

## Umsetzungsreihenfolge

1. **SHA-Mapping der 8 distinkten Action-Tags ermitteln**
   - Voraussetzungen: Netzwerkzugriff auf github.com; `git` lokal.
   - Beschreibung: `git ls-remote https://github.com/actions/<repo> refs/tags/<tag>` bzw. `refs/tags/<tag>^{}` für alle 8 Tags; Ergebnis als Mapping-Tabelle Tag → 40-stelliger Commit-SHA festhalten (Arbeitsnotiz, Basis für Schritt 3–4).

2. **`.github/dependabot.yml` anlegen**
   - Voraussetzungen: Keine (Datei existiert noch nicht; PolyForm-Header-Text aus bestehenden `.yml`-Dateien übernehmen).
   - Beschreibung: Datei mit Header, `version: 2` und den drei `updates`-Einträgen (siehe oben) erstellen.

3. **SHA-Pinning in den 7 Workflow-Dateien**
   - Voraussetzungen: Schritt 1 (Mapping).
   - Beschreibung: 45 Referenzen ersetzen (Verteilung siehe Tabelle oben); lokale `uses:` unverändert lassen; Nachprüfung per `grep`, dass kein `@vN` bei externen Referenzen verbleibt.

4. **SHA-Pinning in den 4 Composite Actions**
   - Voraussetzungen: Schritt 1 (Mapping).
   - Beschreibung: `setup-dotnet` in `build-and-package`, `package-android`, `package-ios` sowie `upload-artifact` in `security-scan` pinnen. Der `security-scan`-Pinning-Teil kann mit Schritt 5 zusammenfallen.

5. **npm-Audit-Gate in `security-scan/action.yml` ergänzen**
   - Voraussetzungen: `package-lock.json` im Repo-Root (vorhanden); Node/npm auf dem Runner (Vorinstallationsannahme, wird in Schritt 9 verifiziert).
   - Beschreibung: Neuen Bash-Step `Audit npm toolchain` einfügen (`npm audit --package-lock-only --audit-level=high` → `npm-audit.txt`, `::error::` + `exit 1` bei Befund); `path:` des Upload-Steps mehrzeilig um `npm-audit.txt` erweitern.

6. **`Invoke-SecurityScan` in `scripts/Run-StaticChecks.ps1` um npm-Audit erweitern**
   - Voraussetzungen: Node/npm lokal vorhanden (verifiziert: Node 24.15.0 / npm 11.10.0).
   - Beschreibung: Gleiche Befehlszeile und Schwelle wie Schritt 5; Fehlerbehandlung konsistent zum bestehenden Muster der Funktion.

7. **Dokumentation aktualisieren**
   - Voraussetzungen: Schritte 2–6 abgeschlossen (Doku beschreibt den Soll-Stand).
   - Beschreibung: `architektur.md` (Komponententabelle), `ablauf-technisch.md` (Gate-Beschreibung), `wiederkehrende-reviews.md` (Zusammenspiel + Security-Update-Hinweis), `ci-instructions.md` (§6.2-Vorlage, Dependabot-/Pinning-Beschreibung).

8. **Lokale statische Verifikation**
   - Voraussetzungen: Schritte 2–6; Python/PyYAML, Node, PowerShell lokal (vorhanden).
   - Beschreibung: `yaml.safe_load` über alle `.github/**/*.yml` (17 Dateien inkl. `dependabot.yml`); `node scripts/add-license-headers.mjs --check`; `.\scripts\Run-StaticChecks.ps1` muss mit Exit-Code 0 ohne Befunde durchlaufen (AGENTS.md-Pflicht); `grep`-Nachprüfung der Pinning-Vollständigkeit.

9. **Lokale Simulation des Gate-Schritts**
   - Voraussetzungen: Schritt 5 (Befehlszeile steht); `package-lock.json` vorhanden.
   - Beschreibung: Den exakten Gate-Befehl in Git Bash ausführen und Exit-Code + `npm-audit.txt`-Erzeugung prüfen; erwartet: Exit 0 (nur moderate Befunde). Negativprobe optional mit `--audit-level=moderate` → Exit 1 belegt die Gate-Logik.

10. **Verifikation nach Merge/Promotion (manuell, im CI)**
    - Voraussetzungen: Branch auf `staging` gemergt (Gate) bzw. nach `main` promoted (Dependabot).
    - Beschreibung: Nächster `staging-ci`-/PR-Lauf zeigt den npm-Step im `static-checks`-Job samt Artefakt mit beiden Reports; Dependabot-Tab nach `main`-Promotion auf „Last checked" bzw. manuellen Check triggern und PR-Erzeugung gegen `staging` verifizieren; in `test-results.md` dokumentieren.

## Tests

### Neue Tests

Keine Unit-/Integrationstests erforderlich — es wird kein Anwendungscode geändert. Verifikation erfolgt über die folgenden Prüfläufe:

| Prüflauf | Art | Was wird geprüft |
|----------|-----|-------------------|
| `yaml.safe_load` über `.github/**/*.yml` | Syntaxprüfung (etabliertes Muster aus Issue #165) | Alle 17 YAML-Dateien (11 Workflows + 5 `action.yml` + `dependabot.yml`) sind valide |
| `node scripts/add-license-headers.mjs --check` | Lizenzheader-Gate | `dependabot.yml` und alle geänderten `.yml` tragen den PolyForm-Header |
| `grep -nE "uses: ...@v?[0-9]+"` über `.github/` | Vollständigkeitsprüfung | Keine externe `uses:`-Referenz verbleibt auf beweglichem Tag |
| `.\scripts\Run-StaticChecks.ps1` | Lokale CI-Spiegelung | Exit-Code 0 inkl. neuem npm-Audit-Teil |
| Gate-Simulation in Git Bash | Verhaltensprüfung | Exakte Gate-Befehlszeile: Exit 0 bei aktuellem Bestand; Negativprobe `--audit-level=moderate` → Exit 1 |

### Betroffene bestehende Tests

Keine — `npm test` (36 Tests, Release-Skripte) und `dotnet test` (806 Tests) sind von CI/CD-Konfigurationsänderungen nicht betroffen. `scripts/Run-StaticChecks.ps1` hat keine eigenen Tests; seine Änderung wird durch den erfolgreichen Gesamtlauf nachgewiesen.

### E2E-Tests (primärer Funktionsnachweis)

Keine E2E-Tests erforderlich — begründet: Die Anforderung berührt keinen über UI oder Nutzeraktion erreichbaren Ablauf. Der „Akteur" aller drei Änderungen ist GitHub Actions/Dependabot, nicht ein Endanwender der MAUI-App; die FlaUI-basierte E2E-Suite (`src/Reporter.E2ETests`, `REPORTER_APP_PATH`-gesteuert) kann Pipeline-Verhalten grundsätzlich nicht abbilden. Der Funktionsnachweis erfolgt stattdessen über die CI selbst: ein realer `staging-ci`-/PR-Lauf mit dem neuen Gate-Step samt Report-Artefakt sowie die tatsächliche Erzeugung von Dependabot-PRs nach der Promotion (Akzeptanzszenarien 1–3, dokumentiert in `test-results.md`).

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path Dependabot (Version-Updates) | Merge des Feature-Branchs → `staging`, anschließend Promotion → `main`; nächster wöchentlicher Dependabot-Lauf oder manueller „Check for updates" | `dependabot.yml` liegt auf `main`; zu prüfende Ökosysteme mit überholbaren Paketen; noch keine Dependabot-PRs vorhanden. **Wartesituation:** Zwischen Merge und erstem Lauf vergehen bis zu 7 Tage — Zustand während des Wartens: Dependabot-Tab zeigt `dependabot.yml` als erkannt; auslösender Übergang: Schedule oder manueller Check im Dependabot-Tab; Wiederaufnahme: nicht nötig (plattformgetrieben), Beschleunigung über „Check for updates" möglich | GitHub liest `dependabot.yml` → Dependabot prüft `nuget`/`npm`/`github-actions` → öffnet Update-PRs mit Ziel `staging` → PRs durchlaufen `PR CI for Staging` | In der PR-Liste erscheinen `dependabot[bot]`-PRs gegen `staging`; bei Actions-PRs sind gepinnte SHAs + `# vN`-Kommentare angehoben | Reale Abnahme im Repo nach Promotion; dokumentiert in `test-results.md` |
| 2 | Happy Path Security-Gate (grün) | `pull_request → staging` oder `push → staging` oder wöchentlicher `security-scan`-Lauf | `package-lock.json` mit Bestand von 0 hohen/kritischen Befunden; Gate-Stand dieser Aufgabe auf dem Branch | Job `static-checks` → `./.github/actions/security-scan` → NuGet-Scan schreibt `vulnerable-packages.txt` → neuer Step schreibt `npm-audit.txt` → Upload-Step lädt beide Dateien | Job ist grün; Artefakt (`vulnerable-packages-pr|staging|weekly`) enthält beide Report-Dateien | CI-Lauf auf `staging` nach Merge; Artefakt-Download zeigt `npm-audit.txt` |
| 3 | Fehlerfall Security-Gate (rot bei high/critical) | Wie #2, jedoch liegt ein `high`/`critical`-Befund in `package-lock.json` vor (lokal simulierbar über gesenkten Schwellenwert `--audit-level=moderate` mit dem Ist-Bestand von 35 moderaten Befunden) | Wie #2, plus vorhandener Befund oberhalb der Schwelle | `npm audit --package-lock-only --audit-level=<Schwelle>` → Exit ≠ 0 → `::error::` + `exit 1` → Upload-Step läuft trotzdem (`if: always()`) | Job schlägt fehl mit `::error::Vulnerable npm dependencies detected…`; `npm-audit.txt` mit den Befunden liegt im Artefakt | Lokale Negativprobe (Exit 1 nachgewiesen); im CI identische Logik |
| 4 | Pinning-Vollständigkeit | Abschluss der Umsetzung (vor Commit) | 49 externe Referenzen auf beweglichen Tags (Ist-Stand) | Ersetzung aller externen Referenzen → `grep`-Nachprüfung → `yaml.safe_load` → `add-license-headers.mjs --check` | Kein `@vN` bei externen `uses:` mehr; alle YAML-Dateien valide; Lizenzheader-Check grün | Lokale Prüfläufe + `Run-StaticChecks.ps1` Exit 0 |

## Offene Punkte

Keine — der zuvor offene Punkt 1 wurde wie folgt entschieden:

| # | Ehemals offener Punkt | Entscheidung |
|---|------------------------|--------------|
| 1 | Dependabot-**Security**-Update-PRs ignorieren `target-branch` und zielen auf `main`; ihre `dependabot/*`-Branches scheitern an `verify-pr-source.yml` (`head_ref == 'staging'`). Es gibt keine reposeitige Dependabot-Einstellung, die Security-Updates auf `staging` umleitet | **Entschieden (2026-11-16, automatisierte SCM-Bearbeitung):** Akzeptanz als bekannte Plattform-Einschränkung gemäß Empfehlung — Security-PRs gegen `main` werden separat behandelt; Verhalten wird in `wiederkehrende-reviews.md` dokumentiert. Reguläre Mergebarkeit (`verify-pr-source.yml`-Ausnahme für `dependabot/*`) ist bewusst nicht Teil dieser Aufgabe |
