<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Supply-Chain-Härtung (Dependabot, SHA-Pinning, npm-Audit-Gate)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | SHA-Pinning | SHA-Mapping für die 8 distinkten Action-Tags ermitteln (`git ls-remote`, annotierte Tags via `^{}` dereferenzieren) und als Mapping-Tabelle festhalten | Offen | — |
| 2 | Dependabot | `.github/dependabot.yml` mit PolyForm-Header, `version: 2` und drei `updates`-Einträgen (`nuget`, `npm`, `github-actions`; `weekly`; `target-branch: "staging"`) anlegen | Offen | — |
| 3 | SHA-Pinning | `staging-ci.yml`: 17 externe `uses:`-Referenzen auf Commit-SHAs pinnen (`# vN`-Kommentar) | Offen | — |
| 4 | SHA-Pinning | `release.yml`: 13 externe Referenzen pinnen | Offen | — |
| 5 | SHA-Pinning | `pr-staging-ci.yml`: 7 externe Referenzen pinnen | Offen | — |
| 6 | SHA-Pinning | `deploy-pages.yml`: 4 externe Referenzen pinnen (`checkout`, `configure-pages`, `upload-pages-artifact`, `deploy-pages`) | Offen | — |
| 7 | SHA-Pinning | `security-scan.yml`: 2 externe Referenzen pinnen (`checkout`, `setup-dotnet`) | Offen | — |
| 8 | SHA-Pinning | `staging-to-main-promotion.yml`: 1 Referenz pinnen (`checkout`) | Offen | — |
| 9 | SHA-Pinning | `sync-staging-with-main.yml`: 1 Referenz pinnen (`checkout`) | Offen | — |
| 10 | SHA-Pinning | `.github/actions/build-and-package/action.yml`: `actions/setup-dotnet@v6` pinnen | Offen | — |
| 11 | SHA-Pinning | `.github/actions/package-android/action.yml`: `actions/setup-dotnet@v6` pinnen | Offen | — |
| 12 | SHA-Pinning | `.github/actions/package-ios/action.yml`: `actions/setup-dotnet@v6` pinnen | Offen | — |
| 13 | SHA-Pinning | `.github/actions/security-scan/action.yml`: `actions/upload-artifact@v7` pinnen | Offen | — |
| 14 | npm-Audit-Gate | `security-scan/action.yml`: neuen Bash-Step `Audit npm toolchain` ergänzen (`npm audit --package-lock-only --audit-level=high` → `npm-audit.txt`, `::error::` + `exit 1` bei Exit ≠ 0) | Offen | — |
| 15 | npm-Audit-Gate | `security-scan/action.yml`: `path:` des Upload-Steps mehrzeilig um `npm-audit.txt` erweitern | Offen | — |
| 16 | Lokale Parität | `Invoke-SecurityScan` in `scripts/Run-StaticChecks.ps1` um `npm audit --package-lock-only --audit-level=high` erweitern (identische Schwelle wie CI-Gate) | Offen | — |
| 17 | Dokumentation | `docs/help/release-management/architektur.md`: `dependabot.yml` als Komponente ergänzen, `security-scan`-Rolle auf „NuGet + npm" erweitern, SHA-Pinning vermerken | Offen | — |
| 18 | Dokumentation | `docs/help/release-management/ablauf-technisch.md`: Gate-Beschreibung um npm-Audit-Step und Schwelle `high` ergänzen | Offen | — |
| 19 | Dokumentation | `docs/help/release-management/wiederkehrende-reviews.md`: Zusammenspiel Dependabot ↔ Scheduled-Review-Issues + Hinweis auf Security-Update-PRs gegen `main` ergänzen | Offen | — |
| 20 | Dokumentation | `ci-instructions.md`: §6.2-Generierungsvorlage `security-scan` aktualisieren (npm-Step, SHA-Pinning), Dependabot-/Pinning-Konvention in Pipeline-Beschreibung ergänzen | Offen | — |
| 21 | Verifikation | `yaml.safe_load` über alle 17 `.github/**/*.yml` (inkl. `dependabot.yml`) ausführen | Offen | — |
| 22 | Verifikation | `grep`-Nachprüfung: keine externe `uses:`-Referenz auf beweglichem `@vN`-Tag verblieben | Offen | — |
| 23 | Verifikation | `node scripts/add-license-headers.mjs --check` grün | Offen | — |
| 24 | Verifikation | `.\scripts\Run-StaticChecks.ps1` mit Exit-Code 0 ohne Befunde (inkl. neuem npm-Teil) | Offen | — |
| 25 | Verifikation | Lokale Gate-Simulation: exakte Befehlszeile in Git Bash → Exit 0 + `npm-audit.txt`; Negativprobe `--audit-level=moderate` → Exit 1 | Offen | — |
| 26 | Verifikation | Post-Merge-Verifikation: CI-Lauf zeigt npm-Step + Artefakt mit beiden Reports; Dependabot-PR-Erzeugung nach `main`-Promotion prüfen; Ergebnis in `test-results.md` dokumentieren | Offen | — |
