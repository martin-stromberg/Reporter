<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Supply-Chain-Härtung (Dependabot, SHA-Pinning, npm-Audit-Gate)

Analysiert wurde die CI/CD- und Konfigurationsebene des Repositories (`.github/workflows/`, `.github/actions/`, npm-Toolchain, `scripts/Run-StaticChecks.ps1`, Lizenzheader-Mechanik) bezogen auf die Anforderung aus `requirement.md` (GitHub Issue #182). Es gibt keine betroffenen Anwendungsklassen — der Eingriff liegt vollständig außerhalb von `src/`.

## Zusammenfassung

- **`.github/dependabot.yml` existiert nicht.** `.github/` enthält nur `actions/` (5 Composite Actions), `workflows/` (11 Dateien) und `pull_request_template.md`. Keine Dependabot-Konfiguration, keine Dependabot-Referenzen in Doku oder Workflows.
- **49 externe `uses:`-Referenzen, alle per beweglichem Major-Tag (`@vN`) referenziert — keine einzige SHA-Pinning.** Verteilung: 45 in 7 Workflow-Dateien, 4 innerhalb von Composite Actions (`security-scan`, `build-and-package`, `package-android`, `package-ios`). Distinkte Actions: `actions/checkout@v7` (21×), `actions/upload-artifact@v7` (11×), `actions/setup-dotnet@v6` (9×), `actions/setup-node@v7` (3×), `actions/download-artifact@v7` (2×), `actions/configure-pages@v5`, `actions/upload-pages-artifact@v3`, `actions/deploy-pages@v4` (je 1×). Ausschließlich `actions/*` (GitHub-eigen), keine Drittanbieter. 12 lokale Referenzen `uses: ./.github/actions/...` sind nicht pinnbar (Scope-Ausschluss).
- **Security-Scan-Gate prüft nur NuGet.** `.github/actions/security-scan/action.yml` führt `dotnet list package --vulnerable --include-transitive` aus, lädt `vulnerable-packages.txt` hoch und schlägt per `grep`-Match + `exit 1` fehl. Aufgerufen an 3 Stellen (`pr-staging-ci.yml`, `staging-ci.yml`, `security-scan.yml`) — eine Erweiterung um npm greift automatisch überall. Kein npm-Schritt, kein `setup-node` im Gate vorhanden.
- **npm-Toolchain:** `package.json` hat **ausschließlich `devDependencies`** (5 Pakete, semantic-release 25.0.9) — `npm audit --omit=dev` wäre wirkungslos. `package-lock.json` (`lockfileVersion: 3`) ist die Audit-Basis; `npm audit` funktioniert ohne `npm ci`. **Audit-Bestand heute: 35 moderate Schwachstellen, 0 hohe/kritische** (transitiv via `node_modules/npm` der semantic-release-Kette). Ein Gate, das ab `moderate` fehlschlägt, wäre ab Tag 1 rot; `--audit-level=high` wäre aktuell grün. Fix nur per `npm audit fix --force` mit Downgrade auf `semantic-release@21.1.2` (Breaking Change).
- **NuGet-Bestand:** 13 distinkte `PackageReference` über `Reporter.sln` + 5 `*.csproj` unter `src/`; keine verwundbaren Pakete. `Microsoft.Maui.Controls` nutzt `$(MauiVersion)` (SDK-gesteuert, von Dependabot nicht direkt anhebbar).
- **`Run-StaticChecks.ps1`** spiegelt den `static-checks`-Job (Format, Lizenzheader, NuGet-Security-Scan via `Invoke-SecurityScan`, Static Analysis) — kein npm-Audit; bei Erweiterung des CI-Gates droht lokale Drift (Offene Frage 5).
- **Lizenzheader-Pflicht:** `scripts/add-license-headers.mjs --check` prüft `.yml` (Hash-Stil) repo-weit und läuft in CI (`static-checks`) sowie im Pre-Commit-Hook (`--staged`). Neue `.github/dependabot.yml` braucht den PolyForm-`#`-Header.
- **Doku-Drift-Punkte:** `ci-instructions.md` §6.2 enthält die alte `security-scan`-Vorlage (nur NuGet), Abschnitt 2 spricht von „seven workflow files" (Ist: 11 Workflows, 5 Composite Actions). `docs/help/release-management/architektur.md` (Komponententabelle: `security-scan` „nur NuGet", kein Dependabot-Eintrag), `ablauf-technisch.md` und `wiederkehrende-reviews.md` beschreiben den Ist-Zustand ohne Dependabot.
- **Runner:** `windows-latest`, `ubuntu-latest`, `macos-latest`; `security-scan` läuft auf `windows-latest` — Node/npm-Vorinstallation dort ist eine Annahme (in `staging-ci.yml`/`release.yml` wird Node dagegen explizit per `setup-node` installiert).

**Test-Ausgangszustand:** Alle ausführbaren Tests grün — `npm test` 36/36, `dotnet test Reporter.sln --filter "Category!=E2E"` 806/806 (798 `Reporter.Tests` + 8 nicht-E2E `Reporter.E2ETests`), YAML-Syntaxprüfung 16/16 Dateien, NuGet-Security-Scan und Static-Analysis-Build sauber. Einzige Befunde: Lizenzheader-Check schlägt ausschließlich auf den untracked Lifecycle-Artefakten in `docs/features/task/issue-182-…/` fehl (`todo.md`; die eigenen Protokolle wurden mit Headern versehen), und `npm audit` meldet 35 moderate Befunde. E2E-Tests (FlaUI) nicht ausgeführt — benötigen interaktive Desktop-Session. Nachweis: [inventory/tests.md](inventory/tests.md).

## Details

- [Workflows und externe Action-Referenzen (Pinning-Bestand)](inventory/workflows.md)
- [Security-Scan-Gate, Aufrufstellen, `Run-StaticChecks.ps1`, Lizenzheader-Mechanik](inventory/security-gate.md)
- [npm-Toolchain, `package-lock.json`-Audit-Bestand und NuGet-Abhängigkeiten](inventory/npm-toolchain.md)
- [Tests und Test-Ausgangszustand](inventory/tests.md)
