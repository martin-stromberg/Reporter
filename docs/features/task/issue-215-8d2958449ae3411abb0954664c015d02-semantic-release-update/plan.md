<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan — Issue #215 „Semantic Release Update"

Behebung des CI-Fehlers `[semantic-release]: node version >=26.13 is required. Found v24.x`
durch Anhebung der gepinnten Node-Version in den drei `actions/setup-node`-Schritten.
Zweite Planrevision: schließt die drei Lücken aus `plan-check.md`
(Plugin-Major-Kompatibilität, Output-Format-Risiko, fehlende lokale
Node-26-Vorabprüfung) durch einen verbindlichen lokalen Verifikationsschritt
unter Node 26 **vor** dem Commit.

## 1. Scope-Klärung

### Verarbeitungseinheit, Auslöser, Wirkung

- **Verarbeitungseinheit:** exakt die drei Stellen mit `actions/setup-node` +
  `node-version: '24'`, unter deren Laufzeit `semantic-release` ausgeführt wird
  (grep über das gesamte Repo verifiziert — keine weiteren Pins):
  | Datei | Zeilen | Job | semantic-release-Aufruf |
  |---|---|---|---|
  | `.github/workflows/release.yml` | 38–41 | `resolve` | `node scripts/resolve-release-version.mjs` (Z. 57), spawnt intern `npx semantic-release --dry-run` (Skript Z. 118–129) |
  | `.github/workflows/release.yml` | 202–205 | `publish` | `npm run release` (Z. 239) → Script `"release": "semantic-release"` (`package.json` Z. 6) |
  | `.github/workflows/staging-ci.yml` | 254–257 | `version` | `npx semantic-release --dry-run --no-ci --branches staging` (Z. 269, Schritt `id: semver`) |
- **Auslösende Ereignisse:** `push` auf `staging` (`staging-ci.yml`) sowie `push` auf
  `main` / Tag `v*.*.*` (`release.yml`). Bei Pull Requests läuft **keiner** der
  betroffenen Jobs — `pr-staging-ci.yml` enthält weder `setup-node` noch
  `semantic-release` (Inventory §5).
- **Erwartete Wirkung:** alle semantic-release-Aufrufe laufen unter Node `>=26.13`;
  die jeweiligen Schritte brechen nicht mehr mit `exit code 1` und der
  Engines-Meldung ab.
- **Umgang mit nicht vorhandenen Objekten:** Es gibt keine `.nvmrc`-,
  `.node-version`- oder `.tool-versions`-Datei und kein `engines`-Feld in
  `package.json` — nichts Zusätzliches zu synchronisieren. In `.github/actions/`
  existiert keine `setup-node`-Verwendung. Neue `setup-node`-Stellen sind derzeit
  nicht geplant.

### Entscheidung: Node-Version anheben (empfohlen, bestätigt)

- **Gewählt:** Pin auf Node 26 anheben — `semantic-release@26.0.1` bleibt installiert.
- **Verworfen:** Downgrade auf `semantic-release@25.x`.
  Begründung: Ein Downgrade dreht das Dependabot-Update `db72e70` zurück, wird vom
  wöchentlichen Dependabot-Lauf (`target-branch: staging`) sofort wieder
  vorgeschlagen und verschiebt die Anhebung nur. Node 26 ist die vom Toolchain-
  Maintainer geforderte Laufzeit; die unter Node 24 laufenden Skripte nutzen
  ausschließlich langjährig stabile `node:`-APIs (Inventory §4) — es spricht nichts
  gegen die Anhebung. Status: **bestätigt** (direktester Fix, kein Funktionsverlust).

### Pin-Syntax-Analyse → Empfehlung

`setup-node` akzeptiert Semver-Ausdrücke; bei Major-Pins/Ranges wird zunächst der
lokale Runner-Toolcache geprüft (ohne `check-latest` wird ein passender
Cache-Treffer verwendet, sonst wird die neueste passende Version geladen).

| Option | Auflösung | `>=26.13` zuverlässig? | Bewertung |
|---|---|---|---|
| `'26'` / `'26.x'` (äquivalent) | neueste gecachte/verfügbare 26.x | i. d. R. ja (aktuellstes 26.x ist ≥26.13); **Restrisiko:** veralteter Toolcache mit `26.x < 26.13` würde stillschweigend akzeptiert → exakt der zu behebende Fehler käme zurück | einfach, konsistent mit Bestandsstil — aber Restrisiko nicht konstruktiv ausgeschlossen |
| `'>=26.13'` (unbeschränkt) | höchste verfügbare Version insgesamt | ja, aber **Major-Drift:** sobald Node 27/28 erscheint, wird die genommen → unkontrollierte Versionswechsel | verworfen |
| `'26.13'` (exakt) | exakt 26.13 | ja, deterministisch | verworfen: friert Patchstand ein, Node-Security-Fixes laufen nicht mehr automatisch mit (Dependabot pflegt `node-version` nicht) |
| **`'>=26.13 <27'`** | höchste 26.x ≥ 26.13 | **ja, konstruktiv:** ein gecachtes `26.x < 26.13` erfüllt die Range nicht → es wird die neueste passende 26.x geladen; bleibt innerhalb Major 26, Patch-Updates laufen mit | **Empfehlung** |

**Empfehlung (Status: bestätigt, im Review änderbar):** `node-version: '>=26.13 <27'`
an allen drei Stellen, ergänzt um einen kurzen Kommentar, der die `26.13`-Untergrenze
auf die `semantic-release`-Engines-Anforderung zurückführt (künftige
Dependabot-Major-Bumps erfordern dieselbe Pin-Nachführung).

- Alternative, falls Einfachheit/Stil priorisiert wird: `node-version: '26'`.
  Dann wäre `check-latest: true` im `with:`-Block die wirksame Absicherung gegen
  den veralteten Toolcache (erzwingt Auflösung auf neuestes 26.x). Wird nur
  empfohlen, falls die Range-Syntax im Review abgelehnt wird.

### Plugin-Kompatibilität — deklarativ geprüft, Laufzeit-Verifikation eingeplant

`release.config.js` lädt die Plugins per Namen aus dem Projekt-Top-Level — nicht
die von `semantic-release@26.0.1` gebündelten Beta-Generationen. Der
`package-lock.json`-Befund zur deklarierten Kompatibilität:

| Plugin (Top-Level, geladen) | `peerDependencies.semantic-release` | `engines.node` | Bewertung |
|---|---|---|---|
| `@semantic-release/commit-analyzer@13.0.1` (Lock Z. 372–393) | `>=20.1.0` → **durch 26.0.1 erfüllt** | `>=20.8.1` → Node 26 ok | deklarativ kompatibel |
| `@semantic-release/release-notes-generator@14.1.1` (Lock Z. 825–846) | `>=20.1.0` → **erfüllt** | `>=20.8.1` → Node 26 ok | deklarativ kompatibel |
| `@semantic-release/github@12.0.9` (Lock Z. 632–662) | `>=24.1.0` → **erfüllt** | `^22.14.0 \|\| >= 24.10.0` → Node 26 ok | deklarativ kompatibel |

Zusätzlich belastbar: `semantic-release@26.0.1` selbst deklariert
`@semantic-release/github: ^12.0.0` als direkte Abhängigkeit (Lock Z. 5005) —
das Top-Level-Pin `12.0.9` entspricht also exakt der von v26 erwarteten
Plugin-Generation. Restrisiko ist nur ein nicht deklarierter Plugin-API-Bruch;
dieser wird durch den lokalen Node-26-Dry-Run (Schritt 5) geprüft, bevor er im
Merge-Lauf sichtbar würde.

### Bewusste Scope-Ausschlüsse (Status)

- **Keine Änderung an `pr-staging-ci.yml` / `verify-pr-source.yml`:** enthalten kein
  `setup-node`; `node scripts/add-license-headers.mjs --check` läuft unter
  Runner-Default-Node und ist versionsneutral. — bestätigt.
- **Keine Änderung an `staging-ci.yml` Job `prerelease` (Z. 379ff.) und
  `static-checks` (Z. 54ff.):** nutzen Runner-Default-Node für
  `release-assets.mjs`/`create-update-manifest.mjs`/`add-license-headers.mjs` —
  dort läuft kein `semantic-release`, die Skripte sind versionsneutral. — bestätigt.
- **Kein `engines`-Feld in `package.json`:** würde `npm ci`/`npm install` unter
  älterem Node nur warnen und Entwickler:innen mit Node <26.13 betreffen, obwohl
  die Skript-Tests (`npm test`) kein Node 26 brauchen. Nutzen gering, Rauschen
  hoch. — bestätigt.
- **Kein Downgrade von `semantic-release`:** siehe Entscheidung oben. — bestätigt.
- **Keine Anhebung der Plugin-devDependencies** auf die Beta-Generationen:
  deklarierte peer/engines-Ranges sind erfüllt, die Beta-Pins wären ein fachlich
  separater, riskanterer Wechsel. Bedingte Aktion nur bei Befund in Schritt 5.
- **Keine neue CI-Absicherung Pin-gegen-Engines:** ein automatisierter Check, der
  `node-version`-Pins gegen `package-lock.json`-Engines prüft, existiert nicht und
  ist nicht gefordert. — offen als mögliche spätere Härtung, nicht Teil dieses Fixes.
- **`docs/help/release-management/troubleshooting.md`:** Ergänzung des
  Node-Versions-Fehlerbilds optional, nicht erforderlich für die Behebung. —
  offen/optional.

## 2. Konkrete Umsetzungsschritte

### Schritt 1 — `.github/workflows/staging-ci.yml` (Job `version`, Z. 254–257)

`node-version: '24'` (Z. 257) auf `node-version: '>=26.13 <27'` anheben und die
Begründung als Kommentar ergänzen:

```yaml
      - name: Setup Node.js
        uses: actions/setup-node@949feb2413d6458794dcd2491c4babbbce0c15c1  # v7
        with:
          # semantic-release@26 verlangt node >=26.13; Range bleibt auf Major 26.
          node-version: '>=26.13 <27'
```

### Schritt 2 — `.github/workflows/release.yml` (Job `resolve`, Z. 38–41)

Analog: `node-version: '24'` (Z. 41) → `node-version: '>=26.13 <27'`, gleicher
Kommentar (gilt für `resolve-release-version.mjs` → interner `npx`-Aufruf).

### Schritt 3 — `.github/workflows/release.yml` (Job `publish`, Z. 202–205)

Analog: `node-version: '24'` (Z. 205) → `node-version: '>=26.13 <27'`, gleicher
Kommentar (gilt für `npm run release` = `semantic-release` sowie die
Node-Hilfsskripte in demselben Job).

### Schritt 4 — `ci-instructions.md` Doku-Snippets synchronisieren (Z. 344 und Z. 725)

Die beiden wiedergegebenen Workflow-Ausschnitte zeigen ebenfalls
`node-version: '24'`; auf `'>=26.13 <27'` angleichen, damit die Doku die
tatsächlichen Workflows abbildet (Snippet bei Z. 341–344 = `version`-Job,
Snippet bei Z. 722–725 = Release-Job).

### Schritt 5 — Lokale Node-26-Vorabprüfung (verbindlich, vor dem Commit)

Ziel: Plugin-Kompatibilität (Lücke 1) und Dry-Run-Output-Format (Lücke 2) unter
Node 26 verifizieren, **bevor** der Fix committet/gemergt wird. Lokal steht nur
Node `v24.15.0` ohne Versionsmanager zur Verfügung; Node 26 wird ad-hoc über das
npm-Paket `node` bereitgestellt (`npx -p node@26 node …` liefert die offiziellen
Binaries, keine Installation/Adminrechte nötig).

1. **Dependencies installieren:** `npm ci` (unter Node 24 — läuft trotz
   EBADENGINE-Warnung durch, kein `engine-strict`).
2. **Binary-Pfad verifizieren:** Lock-Eintrag `bin/semantic-release.js`
   (`package-lock.json` Z. 5017–5019) → Ziel
   `node_modules/semantic-release/bin/semantic-release.js`
   (Alternativpfad `node_modules/.bin/semantic-release*` nur falls abweichend).
3. **Plugin-Load-Smoke-Test** unter Node 26 (deckt alle drei geladenen
   Plugin-Majors ab — `RESOLVE_DRY_RUN` lädt sonst nur commit-analyzer,
   `release.config.js` Z. 21/26):

   ```bash
   npx -p node@26 node --input-type=module -e "for (const p of ['@semantic-release/commit-analyzer','@semantic-release/release-notes-generator','@semantic-release/github']) { await import(p); console.log('ok', p); }"
   ```

   Erwartung: drei `ok`-Zeilen, kein Import-/Syntaxfehler. Ein API-/Ladefehler
   wäre der Beleg für einen Plugin-Bruch → dann Plugin-Pins im selben Fix
   mitbetrachten.
4. **Dry-Run unter Node 26 nach CI-Vorbild** (identisch zu `staging-ci.yml`
   Z. 269, `RESOLVE_DRY_RUN=true` → nur commit-analyzer, **keine** GitHub-Calls
   nötig):

   ```bash
   RESOLVE_DRY_RUN=true npx -p node@26 node node_modules/semantic-release/bin/semantic-release.js \
     --dry-run --no-ci --branches "$(git branch --show-current)" 2>&1 | tee test-results-sr-dryrun.log
   ```

   Hinweis: `--branches` muss den **ausgecheckten** Branch enthalten (CI: `staging`,
   lokal: Task-Branch), sonst meldet semantic-release nur „not configured to run
   on this branch" und gibt keine Versionszeile aus — das wäre ein
   falsch-negatives Format-Signal.
5. **Output-Format gegen beide Parser verifizieren** (stiller `changed=false`-
   Durchlauf muss ausgeschlossen werden):
   - Dasselbe Bash-Regex wie `staging-ci.yml` Z. 273 auf die Ausgabe anwenden;
   - dieselbe JS-Regex wie `resolve-release-version.mjs` Z. 114:

     ```bash
     node -e "const s=require('fs').readFileSync('test-results-sr-dryrun.log','utf8'); const m=s.match(/the next release version is ([0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?)/i); console.log(m ? 'MATCH: '+m[1] : 'NO MATCH'); process.exit(m?0:1)"
     ```

   - **Fall „kein Release erkannt":** enthält die Ausgabe stattdessen die
     No-Release-Meldung (z. B. „no relevant changes"), ist das Formatrisiko
     unentschieden → mit einem temporären lokalen Commit
     `git commit --allow-empty -m "fix: verify semantic-release output"` ein
     releasefähiges Commit erzeugen, Dry-Run erneut ausführen (`The next release
     version is …` muss erscheinen), danach `git reset HEAD~1` — nichts pushen.
   - **Bei Format-Abweichung:** die Regex in `staging-ci.yml` Z. 273 und in
     `resolve-release-version.mjs` Z. 114 im selben PR an das beobachtete
     Format anpassen (Tests `resolve-release-version.test.mjs` mitziehen).
6. **Vollen Plugin-Pfad einordnen:** `RESOLVE_DRY_RUN` begrenzt bewusst auf
   commit-analyzer; das `github`-Plugin (`verify`/`publish`, Token/API-Calls) ist
   offline nicht vollständig prüfbar — abgedeckt durch (a) Smoke-Import,
   (b) peer-Range `>=24.1.0` ✓, (c) v26 deklariert selbst `github ^12.0.0`.
   Restnachweis in A5. Optional bei vorhandenem Token: Dry-Run ohne
   `RESOLVE_DRY_RUN` (github-`verifyConditions` würde API-Calls ausführen).
7. **Ergebnis dokumentieren** in `test-results.md` (Repo-Root, neu anlegen):
   genutzte npx-Node-Version (`npx -p node@26 node --version`), Plugin-Load-
   Ausgabe, Dry-Run-Log-Ausschnitt mit der Versions-/No-Release-Zeile und die
   beiden Regex-Ergebnisse.

### Schritt 6 — PR und Merge (Prozessvorgaben)

- PR gegen `staging`; Commit-Message mit Closing-Keyword **`Closes #215`**
  (Issue schließt erst bei Promotion nach `main` — gewünscht laut Staging-Workflow).
- PR-Checks mit `gh pr checks <nr> --watch --fail-fast` beobachten; bei `fail`
  sofort in die Diagnose (`gh run view --job <id> --log-failed`).
- Nach grünen Checks: Admin-Merge (Benutzerkontextwechsel) gemäß `issue.md`.
- Nach dem Merge: Verifikation am `staging-ci`-Push-Lauf (siehe Abschnitt 3/5).

## 3. Akzeptanzszenarien

> Wichtig: Der fehlerhafte Schritt läuft **nicht** in den PR-Checks
> (`pr-staging-ci.yml` hat keinen semver-Job). Verifikation ist dreistufig:
> (a) lokale Node-26-Vorabprüfung vor dem Commit (A2b), (b) PR-Checks grün,
> (c) `staging-ci`-Push-Lauf nach dem Merge.

- **A1 (lokal, Syntax):** `release.yml` und `staging-ci.yml` sind nach der Änderung
  YAML-parsebar; `grep -n "node-version" .github/workflows/*.yml` zeigt an genau
  den drei Stellen `'>=26.13 <27'` und nirgends mehr `'24'`.
- **A2 (lokal, Plausibilität):** `npm ci` unter dem lokalen Node `v24.15.0`
  installiert weiterhin durch (kein `engine-strict`) und meldet
  `npm warn EBADENGINE` für `semantic-release@26.0.1` bzw.
  `@semantic-release/core` — Beleg, dass die `>=26.13`-Forderung real greift und
  der Fix im Runner-Node liegen muss, nicht in npm. Optional:
  `npx semantic-release --version` reproduziert lokal unter Node 24 die
  Fehlermeldung `node version >=26.13 is required. Found v24.15.0.` und belegt so
  den Fehlerpfad.
- **A2b (lokal, Node-26-Vorabprüfung — neu, schließt Lücken 1–3):** Der
  Verifikationslauf aus Schritt 5 zeigt: (a) alle drei Top-Level-Plugins laden
  fehlerfrei unter Node 26; (b) `semantic-release@26` läuft im Dry-Run ohne
  Engines- oder Plugin-Fehler; (c) die Dry-Run-Ausgabe enthält bei
  releasefähigen Commits die Zeile `The next release version is X.Y.Z` und
  beide Produktiv-Parser (Bash-Regex `staging-ci.yml` Z. 273, JS-Regex
  `resolve-release-version.mjs` Z. 114) treffen darauf; bei nicht
  releasefähigen Commits erscheint eine explizite No-Release-Meldung.
  Ergebnis liegt dokumentiert in `test-results.md` vor.
- **A3 (PR-Ebene):** Alle Checks des PR gegen `staging` sind grün
  (`gh pr checks <nr>`). Erwartung: kein `version`-/semver-Job darunter —
  dokumentiert, dass die PR-Checks den Fix nicht verifizieren können.
- **A4 (Merge-Ebene, primärer Nachweis):** Der durch den Merge-Push ausgelöste
  `staging-ci`-Lauf erreicht den Job `version` (Voraussetzung: kein Backmerge,
  `static-checks` und `build-and-test` grün). Im Log des Schritts
  „Setup Node.js" steht `Now using node v26.x` mit `x ≥ 13`; der Schritt
  „Determine next version" (`npx semantic-release --dry-run`) enthält **keine**
  Zeile `node version >=26.13 is required` und endet erfolgreich.
  **Zusätzlich (schließt Lücke 2 remote):** Das Schritt-Log muss eine der
  beiden expliziten Aussagen enthalten — entweder `The next release version is
  X.Y.Z` (→ `changed=true`, `version` gesetzt, konsistent zum Commit-Inhalt)
  oder eine erkennbare No-Release-Meldung (→ `changed=false`, nur zulässig
  wenn der Merge-/Push-Stand keinen `fix:`/`feat:`-Commit enthält). Ein
  `changed=false` **ohne** solche Log-Aussage gilt nicht als Nachweis und
  ist als Format-Bruch zu diagnostizieren.
- **A5 (Folge-Verifikation `release.yml`):** Beim nächsten Push auf `main`
  (bzw. Tag-Push) laufen die Jobs `resolve`/`publish` ebenfalls unter Node
  ≥26.13; `npm run release` scheitert nicht an der Node-Version — inklusive
  des `github`-Plugin-Pfads (`verify`/`publish`/`success`), der lokal nicht
  vollständig prüfbar ist. Bis dahin gilt A4 als stellvertretender Nachweis —
  beide Workflows nutzen dieselbe `setup-node`-Action (`@949feb24… # v7`) mit
  demselben Pin. `release.yml` hat keinen `workflow_dispatch`-Trigger; ein
  provozierter Testlauf ist ohne Push nicht möglich.

## 4. Risiken

- **Plugin-Major-Kompatibilität (neu adressiert):** Top-Level-Pins
  (`commit-analyzer@13.0.1`, `release-notes-generator@14.1.1`,
  `github@12.0.9`) liegen einen Major unter den von v26 gebündelten
  Beta-Generationen. Deklarativ ist alles abgedeckt (peer-Ranges `>=20.1.0`/
  `>=24.1.0` erfüllt; engines Node-26-tauglich; v26 verlangt selbst
  `github ^12.0.0`). Nicht deklarierbares Restrisiko (Plugin-API-Bruch) wird
  durch Schritt 5 vor dem Commit geprüft; bei Befund werden die Plugin-Pins im
  selben Fix mitangehoben.
- **Output-Format-Risiko (neu adressiert):** Beide Parser
  (`staging-ci.yml` Z. 273, `resolve-release-version.mjs` Z. 114) hängen am
  wörtlichen Log-String `the next release version is`. Ein Major-Bump könnte den
  Wortlaut ändern → stiller `changed=false`-Erfolg trotz releasefähiger Commits.
  Abgedeckt durch den lokalen Node-26-Dry-Run mit Regex-Abgleich (Schritt 5.5)
  **vor** dem Commit und durch die verschärfte A4-Log-Prüfung nach dem Merge.
  Bei Format-Abweichung gehört die Parser-Anpassung in denselben PR.
- **Node-26-Kompatibilität der Hilfsskripte** (`resolve-release-version.mjs`,
  `release-assets.mjs`, `create-update-manifest.mjs`): nutzen ausschließlich
  stabile `node:`-Stdlib-APIs (`child_process`, `fs`, `crypto`, `path`, `url`)
  — keine ersichtliche Inkompatibilität. `npm test` deckt die Skriptlogik ab,
  läuft lokal unter Node 24; der Node-26-Lauf in Schritt 5 prüft zusätzlich
  die Laufzeitumgebung des eigentlichen semantic-release-Aufrufs.
- **npx-Node-26-Vorabprüfung:** `npx -p node@26 node` nutzt das npm-Paket
  `node` (Binaries). Risiken: erster Download benötigt Netzwerk;
  `npx`-Bootstrap läuft selbst unter Node 24 (unkritisch — es wird nur das
  Node-26-Binary gestartet). Fallback: portabler Node-26-Download.
- **npm-Version unter Node 26:** Node 26 liefert npm ≥12.x; `semantic-release`
  bündelt zudem `npm@12.2.3` (`engines: ^22.22.2 || ^24.15.0 || >=26.0.0`) —
  konsistent. `npm ci` mit `lockfileVersion: 3` unproblematisch.
- **Vergessene Pins:** grep über das gesamte Repo bestätigt exakt die drei
  Workflow-Stellen plus zwei Doku-Snippets — Vollständigkeit gegeben.
  `.github/actions/security-scan` ruft nur `npm audit` ohne eigenes setup-node
  auf — unberührt.
- **Floating-Pin-Fluktuation:** `'>=26.13 <27'` löst jeweils auf die neueste
  26.x auf → neue Patchstände können Verhalten ändern. Identisches Risiko
  bestand bereits beim `'24'`-Pin und ist bewusst gewollt (Security-Patches).
- **Auflösereihenfolge beim Range-Pin:** Falls ein veraltetes `26.x < 26.13`
  im Toolcache läge, wird es durch die Range korrekt ignoriert und die neueste
  passende Version geladen — genau das gewünschte Verhalten; kein Risiko.
- **Verifikationslatenz:** Job `version` hängt an
  `needs: [detect-backmerge, static-checks, build-and-test]` und wird bei
  erkanntem Backmerge übersprungen — der Nachweislauf kann also den vollen
  Build durchlaufen bzw. im Backmerge-Fall ausfallen (dann ist A4 beim
  nächsten regulären Push nachzuholen).
- **Dependabot-Langlebigkeit:** Der nächste `semantic-release`-Major
  (z. B. 27 mit `node >=27.x`) erfordert erneut eine Pin-Anhebung; der
  geplante Kommentar an den Pins macht den Zusammenhang sichtbar.

## 5. Test-/Verifikationsplan

**Lokal vor dem Commit:**

- YAML-Parse der geänderten Workflows, z. B.
  `python -c "import yaml; yaml.safe_load(open('.github/workflows/staging-ci.yml', encoding='utf-8'))"`
  (analog `release.yml`); alternativ `npx js-yaml <datei>`.
- `grep -n "node-version" .github/workflows/*.yml ci-instructions.md` → alle
  Fundstellen zeigen `'>=26.13 <27'`, kein `'24'` mehr.
- `npm ci` + EBADENGINE-Beobachtung (A2); `npm test`
  (`node --test "scripts/*.test.mjs"`) muss grün bleiben — Skriptverhalten
  unverändert (bzw. bei Regex-Anpassung: Testfälle für das neue Format).
- **Node-26-Vorabprüfung (A2b, verbindlich):** komplette Schrittfolge aus
  Schritt 5 — Plugin-Load-Smoke-Test, Dry-Run unter `npx -p node@26`,
  Regex-Verifikation der Ausgabe in beiden Parser-Syntaxen. Ergebnis
  (inkl. Log-Ausschnitt und Node-Version) in `test-results.md` festhalten.
- `.\scripts\Run-StaticChecks.ps1` → Exit-Code 0 (Format, Security, Static
  Analysis). Erwartbar unberührt: keine C#-Änderung; `npm audit` nutzt das
  unveränderte `package-lock.json`.

**PR-Ebene (remote):**

- `gh pr checks <nr> --watch --fail-fast`; bei `fail` sofort
  `gh run view --job <id> --log-failed` (kein stilles Warten).

**Merge-Ebene (remote, primärer Nachweis):**

- `gh run list --workflow "Pre-Release" --branch staging --limit 1` → neuen
  Lauf identifizieren; `gh run view --job <version-job-id> --log` → Schritt
  „Setup Node.js" zeigt `v26.x` (x ≥ 13), Schritt „Determine next version"
  ohne Engines-Fehler und erfolgreich **plus** explizite Log-Aussage
  (Versionszeile oder No-Release-Meldung, siehe A4).
- Erst remote verifizierbar: tatsächlich installierte Node-Version auf dem
  Runner, `github`-Plugin-Publish-Pfad, Erfolg von `release.yml` beim
  nächsten `main`-Push (A5). Lokal bereits vorab verifiziert:
  Plugin-Ladefähigkeit, Dry-Run-Laufzeitverhalten und Output-Format
  unter Node 26 (A2b).

## 6. Offene Punkte

Keine. Die im Requirement genannten Unklarheiten sind im Plan entschieden
(Pin-Syntax: `'>=26.13 <27'`; Fehlerbehebung via Node-Anhebung statt Downgrade)
bzw. durch die Bestandsaufnahme verifiziert (Log-Herkunft: `staging-ci.yml`,
Job `version`, Schritt „Determine next version"). Die drei Prüflücken aus
`plan-check.md` sind durch Schritt 5 / A2b / erweitertes A4 geschlossen;
Plugin-Metadaten sind in `package-lock.json` verifiziert. Bedingte Folgeaktion
(kein offener Punkt): Nur falls der lokale Node-26-Check einen Plugin- oder
Format-Bruch zeigt, kommen Plugin-Pin-Anhebung bzw. Parser-Anpassung in
denselben PR. Admin-Merge und Nach-Verifikation sind Prozessvorgaben.
