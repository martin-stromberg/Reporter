<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung: Supply-Chain-Härtung (GitHub Issue #182)

Folge-Issue aus dem Architektur- und Sicherheitsreview 2026-11 (#178). Drei Lücken in der kontinuierlichen Dependency-/Supply-Chain-Überwachung der CI/CD-Pipeline des Repositories werden geschlossen: (1) Dependabot-Konfiguration für die Ökosysteme `nuget`, `npm` und `github-actions`, (2) Pinning aller extern referenzierten GitHub Actions auf Commit-SHAs statt beweglicher Tags, (3) Erweiterung des Security-Scan-Gates um eine Prüfung der npm-Toolchain (`package-lock.json`, semantic-release).

## Fachliche Zusammenfassung

Die Dependency-Überwachung des Repositories wird von einer rein wöchentlichen, issue-basierten Sichtprüfung (`review-nuget.yml`) auf einen automatisierten, PR-basierten Update-Kreislauf per Dependabot umgestellt. Gleichzeitig wird die Ausführungsintegrität der Pipeline gehärtet: Externe GitHub Actions werden nicht mehr über bewegliche Versions-Tags (z. B. `actions/checkout@v7`), sondern über immutable Commit-SHAs referenziert. Schließlich wird das bestehende Security-Gate `.github/actions/security-scan` — bisher ausschließlich `dotnet list package --vulnerable` für NuGet — um einen Audit der npm-Toolchain ergänzt, da Schwachstellen in der `package-lock.json` (zeitweise 1 kritische + 2 hohe) unbemerkt mitliefen.

## Fachlicher Scope

- **Verarbeitungseinheit:** Der Dependency-/Supply-Chain-Überwachungszyklus des Repositories — konkret drei Einheiten: (a) der wöchentliche Dependabot-Update-Lauf je Ökosystem (`nuget`, `npm`, `github-actions`), der Update-PRs erzeugt; (b) eine einzelne externe Action-Referenz (`uses: <owner>/<repo>@<ref>`) in Workflows und Composite Actions, die unveränderbar zu pinnen ist; (c) ein Security-Scan-Lauf der Composite Action `.github/actions/security-scan`, der künftig NuGet- und npm-Abhängigkeiten gemeinsam gate-prüft.
- **Auslösende Ereignisse:**
  - Dependabot-Schedule (wöchentlich) pro Ökosystem → erzeugt Update-PRs gegen `staging`.
  - Bestehende CI-Trigger: `pull_request → staging` (`pr-staging-ci.yml`), `push → staging` (`staging-ci.yml`) sowie der wöchentliche Cron `0 6 * * 1` bzw. `workflow_dispatch` (`security-scan.yml`) — alle rufen `.github/actions/security-scan` auf und durchlaufen damit automatisch das erweiterte npm-Gate.
- **Erwartete Wirkung:**
  - Dependabot öffnet wöchentlich Update-PRs gegen `staging` (NuGet-Pakete, npm-Toolchain, GitHub-Actions-Versionen) — Updates werden zeitnah und pro PR sichtbar statt nur über die wöchentlichen Scheduled-Review-Issues.
  - Jede externe Action-Referenz in `.github/workflows/*.yml` zeigt auf einen Commit-SHA; eine Tag-Übernahme durch Dritte kann die Pipeline nicht mehr nachträglich verändern. Dependabot aktualisiert die gepinnten SHAs über den `github-actions`-Ökosystem-Eintrag weiter.
  - Der Security-Scan schlägt (Gate, `exit 1`) nicht nur bei verwundbaren NuGet-Paketen fehl, sondern auch bei verwundbaren npm-Abhängigkeiten aus `package-lock.json`; das Ergebnis landet im Scan-Report-Artefakt.
- **Umgang mit fehlenden Objekten:**
  - `.github/dependabot.yml` existiert nicht → **neu erzeugen** (`version: 2`, drei `updates`-Einträge, `schedule.interval: weekly`, `target-branch: staging`).
  - `package-lock.json` existiert bereits im Repository-Root → **wiederverwenden** (Audit-Basis; kein `npm ci` im Gate erforderlich, wenn der Audit direkt auf dem Lockfile arbeitet).
  - Lizenzheader: Neue/geänderte `.yml`-Dateien müssen den PolyForm-Header (`# Licensed under the PolyForm Noncommercial License 1.0.0 - ...`) tragen — `static-checks` führt `node scripts/add-license-headers.mjs --check` aus, der `.yml`-Dateien prüft → Header **erzeugen**/beibehalten.
  - Für den npm-Audit-Schritt wird Node.js/`npm` benötigt; auf den verwendeten Runnern (`windows-latest`) ist Node vorinstalliert → **wiederverwenden**, kein zusätzliches Setup nötig (Annahme, im Plan zu verifizieren).
- **Scope-Ausschlüsse:**
  - Keine Auto-Merge-Automatisierung für Dependabot-PRs — die PRs durchlaufen die bestehenden `PR CI for Staging`-Gates und werden manuell gemergt.
  - Kein Pinning der lokalen Referenzen `uses: ./.github/actions/*` — vom Issue explizit ausgenommen und technisch nicht möglich (lokale Pfade ohne `@<ref>`).
  - Keine Aktualisierung konkreter Dependency-Versionen in dieser Aufgabe — weder NuGet- noch npm-Pakete noch Action-Versionen werden fachlich angehoben; das SHA-Pinning fixiert die *aktuelle* Version (der zum Tag gehörende Commit).
  - Keine Erweiterung der Überwachung auf weitere Ökosysteme (z. B. `pip`, `docker`) — nicht Teil der Anforderung; die `website/` enthält kein eigenes `package.json`.
  - Keine Änderung an Permissions-, Runner- oder Trigger-Modellen der Workflows.
  - Die wöchentlichen Scheduled-Review-Issues (`review-nuget.yml` u. a.) bleiben unverändert bestehen — Dependabot ergänzt, ersetzt sie nicht.

## Betroffene Klassen und Komponenten

Keine Anwendungsklassen betroffen — der Eingriff liegt vollständig auf CI/CD- und Konfigurationsebene.

- **Neu anzulegen:**
  - `.github/dependabot.yml` — `version: 2`; `updates` für `nuget` (directory `/`, erfasst `Reporter.sln` + `src/**/*.csproj`), `npm` (directory `/`, `package.json`/`package-lock.json`), `github-actions` (directory `/`); jeweils `schedule.interval: weekly`, `target-branch: "staging"`; PolyForm-Lizenzheader als `#`-Kommentar.
- **Zu ändern (SHA-Pinning, 49 externe `uses:`-Referenzen):**
  - `.github/workflows/staging-ci.yml` — 17 Referenzen: `actions/checkout@v7`, `actions/setup-dotnet@v6`, `actions/setup-node@v7`, `actions/upload-artifact@v7`, `actions/download-artifact@v7`.
  - `.github/workflows/release.yml` — 13 Referenzen: `actions/checkout@v7`, `actions/setup-dotnet@v6`, `actions/setup-node@v7`, `actions/upload-artifact@v7`, `actions/download-artifact@v7`.
  - `.github/workflows/pr-staging-ci.yml` — 7 Referenzen: `actions/checkout@v7`, `actions/setup-dotnet@v6`, `actions/upload-artifact@v7`.
  - `.github/workflows/deploy-pages.yml` — 4 Referenzen: `actions/checkout@v7`, `actions/configure-pages@v5`, `actions/upload-pages-artifact@v3`, `actions/deploy-pages@v4`.
  - `.github/workflows/security-scan.yml` — 2 Referenzen: `actions/checkout@v7`, `actions/setup-dotnet@v6`.
  - `.github/workflows/staging-to-main-promotion.yml`, `.github/workflows/sync-staging-with-main.yml` — je 1 Referenz `actions/checkout@v7`.
  - `.github/workflows/verify-pr-source.yml`, `review-architecture.yml`, `review-nuget.yml`, `review-compliance.yml` — keine externen `uses:`-Referenzen (nur `run:`/`gh`), unverändert.
  - Bedingt (siehe Offene Fragen): externe Referenzen *innerhalb* der Composite Actions — `.github/actions/security-scan/action.yml` (`actions/upload-artifact@v7`), `.github/actions/build-and-package/action.yml`, `.github/actions/package-android/action.yml`, `.github/actions/package-ios/action.yml` (je `actions/setup-dotnet@v6`). `.github/actions/checkout-release-tag/action.yml` enthält keine externen Referenzen.
- **Zu ändern (npm-Audit-Gate):**
  - `.github/actions/security-scan/action.yml` — zusätzlicher Composite-Step, der `package-lock.json` auditiert und bei Befunden fehlschlägt; Aufnahme des Audit-Outputs in das Report-Artefakt (`artifact-name`-Input, bisher `vulnerable-packages.txt`).
- **Bedingt zu ändern (Parität/Doku):**
  - `scripts/Run-StaticChecks.ps1` — lokaler `Invoke-SecurityScan` spiegelt bisher nur die NuGet-Prüfung; Parität mit dem erweiterten CI-Gate prüfen (siehe Offene Fragen).
  - Dokumentation: `docs/help/release-management/architektur.md` (Komponententabelle: `dependabot.yml` neu, Rolle von `security-scan` erweitern), `docs/help/release-management/ablauf-technisch.md` (Gate-Beschreibung `static-checks`/`security-scan.yml`), ggf. `docs/help/release-management/wiederkehrende-reviews.md` (Zusammenspiel Dependabot ↔ Review-Issues), ggf. `ci-instructions.md` (Generierungsvorlage — Drift vermeiden), ggf. `README.md`/`docs/help/index.md`.
- **Tests:** Kein Unit-Test-Bedarf (kein Anwendungscode). Verifikation über YAML-Syntaxprüfung (`yaml.safe_load`, etabliertes Muster aus Issue #165), `node scripts/add-license-headers.mjs --check`, `scripts/Run-StaticChecks.ps1` (Exit-Code 0) sowie manuelle Verifikation des Dependabot-Verhaltens und des npm-Gates nach Merge.

## Implementierungsansatz

- **Dependabot:** Neue Datei `.github/dependabot.yml` mit drei `updates`-Blöcken. NuGet-Ökosystem scannt ab Repository-Root die Solution/Projektdateien; npm scannt `package.json`/`package-lock.json`; `github-actions` scannt Workflows *und* `action.yml`-Dateien unter `.github/actions/` und hält damit auch gepinnte SHAs aktuell. `target-branch: "staging"` richtet die Update-PRs gegen den Integrationsbranch (passend zum Staging-Workflow: Feature → `staging` → `main`).
- **SHA-Pinning:** Für jede externe Referenz `uses: <owner>/<action>@vN` wird der Commit-SHA des zugehörigen Tags ermittelt (z. B. `git ls-remote https://github.com/<owner>/<action> refs/tags/vN`) und die Referenz als `uses: <owner>/<action>@<sha>  # vN` ersetzt — der Kommentar erhält die Lesbarkeit und wird von Dependabot bei Updates mitgepflegt. Alle 49 Referenzen in den Workflow-Dateien sowie — vorbehaltlich der Klärung — die 4 externen Referenzen in den Composite Actions. Lokale `uses: ./.github/actions/...` bleiben unverändert.
- **npm-Audit-Gate:** In `.github/actions/security-scan/action.yml` wird analog zum bestehenden NuGet-Step ein zweiter Bash-Step ergänzt, der den Audit gegen `package-lock.json` ausführt, die Ausgabe in eine Report-Datei schreibt (z. B. `npm-audit.txt`, mit in den `path:` des Upload-Steps aufnehmen) und bei Befunden über `::error::` + `exit 1` das Gate auslöst. Das Gate greift automatisch an allen drei Aufrufstellen (`pr-staging-ci.yml`, `staging-ci.yml`, `security-scan.yml`), da die Logik genau einmal in der Composite Action liegt (Konvention laut `ci-instructions.md` §6.2).
- **Abhängigkeiten:** Keine neuen Tools — `npm` ist auf `windows-latest`/`ubuntu-latest` vorinstalliert; der Audit benötigt kein `npm ci`, wenn er direkt auf dem Lockfile arbeitet. Keine Abhängigkeit zu Anwendungsklassen.

## Konfiguration

- `dependabot.yml` ist selbst die Konfiguration (Intervalle, Zielbranch, Ökosysteme). Weitere Optionen (`open-pull-requests-limit`, `labels`, `commit-message.prefix`, `reviewers`) sind in der Anforderung nicht spezifiziert — Defaults verwenden, sofern nicht anders entschieden (siehe Offene Fragen).
- Audit-Schwellenwert des npm-Gates (welche Severities fehlschlagen) — Vorschlag: analog zur NuGet-Prüfung bei jedem gemeldeten Befund fehlschlagen bzw. `--audit-level=high`, im Plan festzulegen.

## Offene Fragen

1. **`npm audit --omit=dev` wäre hier wirkungslos:** Sämtliche npm-Abhängigkeiten des Repositories sind `devDependencies` (semantic-release-Toolchain in `package.json`). `--omit=dev` auditiert nur Produktiv-Abhängigkeiten — das Gate wäre leer und hätte die im Issue genannten Schwachstellen (1 kritisch, 2 hoch) nicht gefunden. **Annahme:** Stattdessen Audit des gesamten Lockfiles (`npm audit` ohne `--omit=dev` bzw. `npm audit --package-lock-only`). Die Anforderung selbst bietet mit „bzw. Audit des Lockfiles" die Alternative; als zu klärender Punkt markiert.
2. **Reichweite der Composite-Action-Ausnahme:** Gilt „projekteigene Composite Actions unter `.github/actions/` sind ausgenommen" nur für die Aufrufe `uses: ./.github/actions/...` (nicht pinnbar), oder auch für die *innerhalb* dieser Dateien referenzierten externen Actions (`actions/upload-artifact@v7` in `security-scan/action.yml`, `actions/setup-dotnet@v6` in den drei Packaging-Actions)? **Annahme/Empfehlung:** Externe Referenzen auch dort pinnen — gleicher Supply-Chain-Vektor, Dependabot aktualisiert `action.yml`-Dateien ebenfalls.
3. **`target-branch` wirkt nur auf Version-Updates:** Dependabot-Security-Update-PRs ignorieren `target-branch` und zielen auf den Default-Branch (`main`) — sie umgehen damit den `staging`-Pfad und würden an `verify-pr-source.yml` scheitern bzw. die Gate-Logik unterlaufen. Klären: Akzeptanz des Verhaltens (Security-PRs separat behandeln) oder reposeitige Einstellung.
4. **Dependabot-PR-Metadaten:** `open-pull-requests-limit`, Labels, Reviewer/Zuweisung für Dependabot-PRs — nicht spezifiziert; Defaults verwenden?
5. **Lokale Parität:** Soll `scripts/Run-StaticChecks.ps1` um einen npm-Audit-Check erweitert werden, damit der lokale Lauf das erweiterte CI-Gate weiterhin vollständig spiegelt (AGENTS.md verlangt, dass das Skript den `static-checks`-Job abbildet)?
6. **`ci-instructions.md` nachziehen?** Die Pipeline wurde nach dieser Vorlage erzeugt; ohne Aktualisierung driftet die Referenzdokumentation (§6.2 `security-scan/action.yml`, Abschnitt „seven workflow files") von der Implementierung ab.
