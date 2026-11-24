<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Anforderung — Issue #215 „Semantic Release Update"

Im Release-/Versionsautomatisierungs-Workflow der CI-Pipeline schlägt `semantic-release` fehl, weil die installierte Node.js-Version die von `semantic-release@26.0.1` geforderte Mindestversion nicht erfüllt. Der Fehler ist zu beheben, sodass die automatische Versionsermittlung und die Release-Ausführung wieder fehlerfrei durchlaufen. Der Fix ist als Pull Request gegen `staging` einzureichen, nach erfolgreichen Checks per Admin-Merge (Benutzerkontextwechsel) zu übernehmen und anschließend die Behebung des Fehlers sicherzustellen.

## Ausgangslage / Fehlerbild

Der Fehler-Log aus der GitHub-Action:

```
Run set -euo pipefail
[semantic-release]: node version >=26.13 is required. Found v24.22.3.

See https://semantic-release.org/support/node-version/ for more details and solutions.
Error: Process completed with exit code 1.
```

Verifizierte Ursache:

- Ein Dependabot-Update (Commit `db72e70`) hat `semantic-release` von `25.0.9` auf `26.0.1` angehoben (`package-lock.json`). `semantic-release@26.0.1` verlangt `engines.node >=26.13`.
- In den Workflows ist Node.js dagegen auf Major-Version `24` gepinnt — an drei Stellen:
  - `.github/workflows/release.yml`, Job `resolve`, Zeile ~41 (`node-version: '24'`; Aufruf über `scripts/resolve-release-version.mjs`)
  - `.github/workflows/release.yml`, Job `publish`, Zeile ~205 (`node-version: '24'`; Aufruf über `npm run release`)
  - `.github/workflows/staging-ci.yml`, Job `semver`, Zeile ~257 (`node-version: '24'`; Schritt „Determine next version", Aufruf `npx semantic-release --dry-run`)
- Die geloggte Meldung stammt mutmaßlich aus `staging-ci.yml` (Schritt „Determine next version"); `release.yml` ist mit denselben Pins identisch betroffen.

## Geforderte Wirkung / Akzeptanzszenarien

- In allen Workflow-Schritten, die `semantic-release` ausführen (`staging-ci.yml` Job `semver`; `release.yml` Jobs `resolve` und `publish`), ist eine Node.js-Version aktiv, die `>=26.13` erfüllt.
- Der Schritt „Determine next version" in `staging-ci.yml` (`npx semantic-release --dry-run`) läuft ohne Versionsfehler durch; der Workflow bricht nicht mehr mit `exit code 1` und der oben genannten Meldung ab.
- Die Jobs `resolve` und `publish` in `release.yml` können `semantic-release` ebenfalls ohne Versionsfehler ausführen — d. h. der Fix deckt alle betroffenen Stellen ab, nicht nur die im Log sichtbare.
- Nach dem Merge: Die Checks auf `staging` laufen erfolgreich; insbesondere schlägt kein `semantic-release`-Aufruf mehr wegen der Node-Version fehl. Die Behebung ist im Workflow-Lauf verifiziert.

## Rahmenbedingungen / Prozessvorgaben

- Pull Request gegen `staging` (nicht `main`).
- Nach erfolgreichen PR-Checks: Admin-Merge durchführen (Benutzerkontextwechsel).
- Nach dem Merge sicherstellen, dass der Fehler tatsächlich behoben ist (Verifikation anhand der Workflow-Läufe).
- Es handelt sich um eine reine Build-/CI-Korrektur; keine Änderung am App-Code gefordert.

## Unklarheiten

- **Zielversion von Node.js:** Das Issue nennt keine konkrete Zielversion. `semantic-release@26.0.1` verlangt `>=26.13`; ob die Pins auf `'26'` (jeweils aktuelles 26.x, erfüllt `>=26.13` ab 26.13) oder auf eine exakte Version (z. B. `26.13`) angehoben werden sollen, ist offen. Zu beachten: `node-version: '26'` würde auf Runnern mit älterem `actions/setup-node`-Cache auch ein `26.x < 26.13` liefern können — die gewählte Pinning-Strategie muss die `>=26.13`-Bedingung zuverlässig erfüllen.
- **Verträglichkeit der weiteren Node-Nutzung:** An den drei Stellen laufen ggf. weitere Node-basierte Schritte (z. B. `npm ci`, `npm run release`); ob Node 26 für diese unproblematisch ist bzw. ob eine Downgrade-Option auf `semantic-release < 26` als Alternative in Betracht kommt, legt das Issue nicht fest (Forderung ist nur „Fehler beheben").
- **Lokalisiertheit des Logs:** Der exakte Workflow/Run, aus dem der gepostete Log stammt, ist nicht benannt; verifiziert ist nur, dass beide Workflows denselben Fehler produzieren würden.
