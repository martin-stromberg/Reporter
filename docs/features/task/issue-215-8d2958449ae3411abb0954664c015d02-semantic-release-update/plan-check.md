<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Prüfung (Runde 2) — Issue #215 „Semantic Release Update"

Kritische Prüfung der aktualisierten `plan.md` (zweite Revision) gegen
`requirement.md`, `inventory.md` und die drei Lücken aus `plan-check.1.md`.
Alle Zeilen-/Metadaten-Behauptungen wurden am Repo-Stand verifiziert.

## 1. Schließung der drei Lücken aus plan-check.1.md

### Lücke 1: Plugin-Major-Kompatibilität — geschlossen (belastbar)

Die peer/engines-Behauptungen des Plans sind in `package-lock.json` verifiziert:

| Plugin | Lock-Zeilen | `peerDependencies.semantic-release` | `engines.node` | Plan-Angabe stimmt? |
|---|---|---|---|---|
| `@semantic-release/commit-analyzer@13.0.1` | Z. 372–393 | `>=20.1.0` (Z. 391–393) → durch 26.0.1 erfüllt | `>=20.8.1` (Z. 388–390) | ✓ |
| `@semantic-release/github@12.0.9` | Z. 632–662 | `>=24.1.0` (Z. 660–662) → erfüllt | `^22.14.0 \|\| >= 24.10.0` (Z. 657–659) → Node 26 ok | ✓ |
| `@semantic-release/release-notes-generator@14.1.1` | Z. 825–846 | `>=20.1.0` (Z. 844–846) → erfüllt | `>=20.8.1` (Z. 841–843) | ✓ |

Zusatzbehauptung verifiziert: `semantic-release@26.0.1` deklariert
`@semantic-release/github: ^12.0.0` (Lock Z. 5005) — das Top-Level-Pin `12.0.9`
ist exakt die von v26 erwartete Generation. Die gebündelten Beta-Generationen
liegen unter `node_modules/semantic-release/node_modules/…` (ab Z. 5024);
`release.config.js` lädt die Plugins per Namen → Auflösung aus dem
Projekt-Top-Level, d. h. die alten Majors werden tatsächlich geladen — die vom
Plan beschriebene Konstellation ist real.

Die verbleibende Unsicherheit (nicht deklarierbarer Plugin-API-Bruch) wird durch
den eingeplanten Laufzeit-Smoke-Test (Schritt 5.3: dynamische Imports aller
drei Plugins unter Node 26) vor dem Commit abgedeckt — mit expliziter
Folgeaktion (Plugin-Pins im selben PR mitanheben) bei Befund. Belastbar.

### Lücke 2: Output-Format-Risiko der Parser — geschlossen (belastbar)

- Beide Parser-Stellen verifiziert: `staging-ci.yml` Z. 273
  (Bash-Regex `[Tt]he\ next\ release\ version\ is …`) und
  `scripts/resolve-release-version.mjs` Z. 114
  (`/the next release version is (…)/i`).
- Schritt 5.5 prüft das reale Dry-Run-Output unter Node 26 gegen beide
  Regex-Varianten **vor** dem Commit; der „kein Release"-Fall ist mit dem
  Fallback (temporärer `git commit --allow-empty -m "fix: …"`, danach
  `git reset HEAD~1`, nichts pushen) sauber aufgelöst. Die Anweisung, bei
  Format-Abweichung beide Parser **und** `resolve-release-version.test.mjs`
  im selben PR anzupassen, verhindert einen inkonsistenten Teil-Fix.
- A4 wurde verschärft: `changed=false` gilt nur noch mit expliziter
  No-Release-Meldung im Log als Erfolg; fehlt jede Aussage, gilt es als
  Format-Bruch. Das stille-Fehlverhalten-Szenario aus plan-check.1 §4 ist
  damit sowohl lokal (A2b) als auch remote (A4) abgedeckt.

### Lücke 3: Lokale Node-26-Vorabprüfung — geschlossen (belastbar)

Schritt 5 beschreibt eine technisch korrekte Vorabprüfung:

- **`npx -p node@26 node …`:** legitime Technik — das npm-Paket `node`
  liefert die offiziellen Plattform-Binaries ohne Admin-Installation.
  Verifiziert: `npm view node@26` listet bis `26.13.1` → `npx -p node@26`
  löst auf ein `26.x ≥ 26.13` auf und erfüllt damit die Engines-Forderung.
  Der npx-Bootstrap läuft selbst unter Node 24 — korrekt als unkritisch
  eingeordnet (nur das Binary wird gestartet). Netzwerk-Risiko und
  Fallback (portabler Download) sind benannt.
- **Binary-Pfad:** Lock Z. 5017–5019 (`"bin": {"semantic-release":
  "bin/semantic-release.js"}`) → `node_modules/semantic-release/bin/
  semantic-release.js` stimmt. `node_modules` ist aktuell nicht
  installiert — Schritt 5.1 (`npm ci`) steht korrekt vor Schritt 5.2–5.4.
- **`RESOLVE_DRY_RUN`-Pfad:** `release.config.js` Z. 21/26 verifiziert —
  unter `RESOLVE_DRY_RUN === "true"` wird nur `commit-analyzer` geladen;
  `staging-ci.yml` Z. 266 setzt `'true'` — die lokale Nachbildung ist
  identisch.
- **`--branches`-Verhalten:** `--branches` ist in v26 weiterhin gültige
  CLI-Option (Docs), die Config-Branches übersteuert. Der Plan-Hinweis ist
  korrekt und wichtig: Ohne den ausgecheckten Task-Branch in `--branches`
  meldet semantic-release nur „not configured to run on this branch" und
  erzeugt keine Versionszeile → falsch-negatives Format-Signal. Verhindert.
- **Reihenfolge:** `npm ci` unter Node 24 (EBADENGINE-Warnung, kein
  `engine-strict` — unverändert korrekt) → Pfad-Check → Smoke-Imports →
  Dry-Run → Regex-Verifikation → Doku in `test-results.md`. Konsistent.

## 2. Verifikation der Umsetzungsschritte / Zeilenangaben

| Plan-Angabe | Repo-Stand | OK? |
|---|---|---|
| `staging-ci.yml` Pin Z. 254–257, `node-version` Z. 257 | verifiziert | ✓ |
| `staging-ci.yml` Dry-Run-Aufruf Z. 269, Regex Z. 273 | verifiziert | ✓ |
| `staging-ci.yml` `RESOLVE_DRY_RUN: 'true'` Z. 266 | verifiziert | ✓ |
| `release.yml` `resolve`-Pin Z. 38–41, `node-version` Z. 41 | verifiziert | ✓ |
| `release.yml` `publish`-Pin Z. 202–205, `node-version` Z. 205 | verifiziert | ✓ |
| `release.yml` `npm run release` Z. 239 | verifiziert | ✓ |
| `resolve-release-version.mjs` spawnSync Z. 118–129, Regex Z. 114 | verifiziert | ✓ |
| `package.json` `"release": "semantic-release"` Z. 6 | verifiziert | ✓ |
| `release.config.js` dryRunPlugins Z. 21, Ternary Z. 26 | verifiziert | ✓ |
| `ci-instructions.md` Snippets Z. 344 und Z. 725 (`node-version: '24'`) | verifiziert | ✓ |

Repo-weiter grep bestätigt weiterhin exakt fünf `node-version`-Vorkommen
(drei Workflows, zwei Doku-Snippets) — Schritte 1–4 decken alle ab;
Vollständigkeit der Pin-Änderung gegeben.

## 3. Akzeptanzszenarien / Verifikationsplan — vollständig und widerspruchsfrei

- A1 (Syntax/Grep), A2 (EBADENGINE-Plausibilität), A2b (neue lokale
  Node-26-Vorabprüfung), A3 (PR-Checks), A4 (verschärfter Merge-Nachweis),
  A5 (Folge-Verifikation `release.yml`): decken die Requirement-Szenarien
  vollständig ab; die dreistufige Einordnung (lokal vor Commit / PR /
  staging-Push) ist realistisch, weil der fehlerhafte Schritt nicht in den
  PR-Checks läuft (`pr-staging-ci.yml` hat keinen semver-Job — verifiziert).
- §5 Verifikationsplan (YAML-Parse, grep, `npm ci`/`npm test`, Node-26-Lauf,
  `Run-StaticChecks.ps1`, `gh pr checks --watch --fail-fast`, `gh run view
  --log`) ist konsistent mit den Schritten und den AGENTS-/Prozessvorgaben.

## 4. Neue Risiken / Kleinigkeiten der aktualisierten Planung

- **Präzisierung (nicht blockierend) — Git-Auth-Check im Dry-Run:**
  `semantic-release` v26 führt `verifyAuth` (`git push --dry-run` gegen
  `origin`, mit `isBranchUpToDate`-Fetch) auch im `--dry-run` aus — vor
  `verifyConditions`, also unabhängig vom `RESOLVE_DRY_RUN`-Plugin-Subset
  (verifiziert am v26-Quellstand). Die Plan-Aussage „keine GitHub-Calls
  nötig" (Schritt 5.4) ist daher ungenau: Der lokale Dry-Run braucht
  Netzwerk und push-fähige Git-Credentials. Auf diesem Rechner dürfte der
  Git-Credential-Helper den Push-Check stillschweigend bestehen; sonst
  schlägt der Lauf mit `EGITNOPERMISSION` fehl → `GITHUB_TOKEN`/`GH_TOKEN`
  setzen (das CI-Äquivalent setzt `GITHUB_TOKEN` in `staging-ci.yml`
  Z. 265). Empfehlung: Ein-Satz-Hinweis in Schritt 5 ergänzen — der
  Fehler wäre selbsterklärend, kostet ohne Hinweis aber ggf. eine
  Diagnose-Iteration.
- **Kosmetik — Dateireferenz:** `plan.md` (Z. 7, 368) nennt `plan-check.md`
  als Quelle der drei Lücken; die erste Prüfrunde liegt als
  `plan-check.1.md` vor. Sobald diese zweite Prüfung als `plan-check.md`
  existiert, wird der Verweis mehrdeutig (zeigt dann auf Runde 2).
  Empfehlung: Verweis auf `plan-check.1.md` präzisieren.
- **`test-results.md`:** Ob die Datei committed oder nur Arbeitsartefakt
  bleibt, ist offen — trivial, bei Commit bitte License-Header nicht
  vergessen (Header-Check läuft in `static-checks`).
- Keine weiteren neuen Risiken gefunden; die Risiken Sektion 4 des Plans
  (Floating-Pin, Verifikationslatenz durch `needs:`-Kette und
  Backmerge-Skip Z. 238–239, Dependabot-Langlebigkeit) sind korrekt
  beurteilt.

## 5. Anforderungsabdeckung

- PR gegen `staging` (nicht `main`): Schritt 6 ✓
- Commit-Message mit Closing-Keyword `Closes #215`: ✓ (Issue-Kennung aus
  `issue.md` Z. 9 verifiziert; Schließung erst bei Promotion nach `main`
  entspricht dem Staging-Workflow)
- Admin-Merge (Benutzerkontextwechsel) nach grünen Checks: Schritt 6 ✓
- Nachweis der Behebung: A4/A5 + §5 Merge-Ebene, jetzt zusätzlich A2b lokal
  vor dem Commit ✓
- Reine Build-/CI-Korrektur, kein App-Code: eingehalten ✓
- `Run-StaticChecks.ps1` im Verifikationsplan (AGENTS-Vorgabe): ✓

## Ergebnis

Alle drei Lücken aus `plan-check.1.md` sind belastbar geschlossen — die
peer/engines-Behauptungen sind in `package-lock.json` verifiziert, der
lokale Node-26-Verifikationspfad ist technisch korrekt (inkl.
`npx -p node@26`, Binary-Pfad, `RESOLVE_DRY_RUN`, `--branches`-Semantik),
und A4 erkennt jetzt einen Format-Bruch. Verbleibend sind zwei nicht
blockierende Präzisierungen (verifyAuth-/Token-Hinweis in Schritt 5;
Dateireferenz `plan-check.1.md`) — optional, empfohlen.

Status: Plan vollständig
