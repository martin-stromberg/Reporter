<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Prüfung — Issue #215 „Semantic Release Update"

Kritische Prüfung des Umsetzungsplans (`plan.md`) gegen die Anforderung
(`requirement.md`), die Bestandsaufnahme (`inventory.md`) und den verifizierten
Ist-Stand des Repos (Branch `task/issue-215-8d2958449ae3411abb0954664c015d02-semantic-release-update`).

## 1. Vollständigkeit der node-version-Pins — bestätigt

Repo-weiter grep (`node-version|setup-node`) bestätigt exakt die drei
geplanten Stellen, alle mit identischem Action-Pin
(`actions/setup-node@949feb2413d6458794dcd2491c4babbbce0c15c1  # v7`):

- `.github/workflows/release.yml` Z. 41 (Job `resolve`) — verifiziert Z. 38–44
- `.github/workflows/release.yml` Z. 205 (Job `publish`) — verifiziert Z. 202–208
- `.github/workflows/staging-ci.yml` Z. 257 (Job `version`) — verifiziert Z. 254–260

Übersehene Stellen gibt es **nicht**:

- `.github/actions/` (fünf Composite Actions: `build-and-package`,
  `checkout-release-tag`, `package-android`, `package-ios`, `security-scan`)
  enthält keine `setup-node`-Verwendung — verifiziert per grep.
- Keine Dockerfiles, kein devcontainer, keine `.nvmrc`-, `.node-version`-
  oder `.tool-versions`-Datei (verifiziert per `find` über das Repo-Root).
- Nur eine `package.json` (Repo-Root), kein `engines`-Feld.
- Übrige Workflows (`deploy-pages.yml`, `pr-staging-ci.yml`,
  `review-architecture.yml`, `review-compliance.yml`, `review-nuget.yml`,
  `security-scan.yml`, `staging-to-main-promotion.yml`,
  `sync-staging-with-main.yml`, `verify-pr-source.yml`) nutzen kein
  `node`/`npm`/`npx` außer `node scripts/add-license-headers.mjs --check`
  in `staging-ci.yml` Z. 82 und `pr-staging-ci.yml` Z. 85 (Runner-Default-Node,
  versionsneutral — korrekt vom Plan ausgeschlossen).
- `staging-ci.yml` Job `prerelease` (Z. 398, 404) und
  `.github/actions/security-scan` (`npm audit`) laufen ohne eigenes
  setup-node und führen kein `semantic-release` aus — Ausschluss korrekt
  begründet.

Kleinigkeit: Die Anforderung nennt den Job „semver"; tatsächlich heißt der Job
`version` (`staging-ci.yml` Z. 236) mit Schritt `id: semver` (Z. 263). Plan und
Inventory mappen das korrekt.

## 2. Pin-Syntax `>=26.13 <27` — korrekt, Begründung stichhaltig

- `actions/setup-node` wertet `node-version` als Semver-Range aus: zuerst
  `semver.satisfies` gegen den Runner-Toolcache, bei fehlendem Treffer Download
  der höchsten passenden Version (`maxSatisfying`) aus der Node-Distribution.
  `'>=26.13 <27'` ist eine gültige Range und erzwingt konstruktiv `>=26.13`:
  ein veralteter Toolcache-Eintrag `26.x < 26.13` erfüllt die Range nicht und
  wird ignoriert (statt stillschweigend verwendet zu werden, wie bei `'26'`).
- YAML-Quoting `'>=26.13 <27'` ist erforderlich und korrekt (führendes `>`
  wäre ungequotet ein ungültiger/folded-scalar-Kontext).
- Der Plan trifft die richtigen Nebenpunkte: ohne `check-latest` darf ein
  gecachtes `26.x ≥ 26.13` verwendet werden (erfüllt die Anforderung immer);
  ein Runner ohne Node 26 im Toolcache ist unkritisch, da `setup-node`
  fehlende Versionen nachlädt. Alternative `'26'` + `check-latest: true` ist
  korrekt als Fallback dokumentiert.
- Die gepinnte `setup-node`-Version unterstützt Range-Syntax seit jeher —
  der SHA-Pin (`# v7`) ist für die Range-Auflösung unerheblich.

## 3. Verifikationsplan — realistisch, aber mit Lücke (s. §7)

- Korrekt verifiziert: `staging-ci.yml` triggert ausschließlich
  `push` auf `staging` (Z. 5–9); `pr-staging-ci.yml` (Z. 5–12) enthält keinen
  `version`-/semver-Job, kein `setup-node`, kein `semantic-release`. Der
  fehlerhafte Schritt läuft **nicht** in den PR-Checks — die zweistufige
  Verifikation (PR-Checks grün + staging-Push-Lauf nach Merge) ist die einzig
  mögliche remote Route und korrekt beschrieben.
- Korrekt: `release.yml` triggert nur `push` auf `main`/Tags `v*.*.*`
  (Z. 5–8), kein `workflow_dispatch` → kein provozierbarer Testlauf; A5 als
  Folge-Verifikation ist ehrlich eingeordnet.
- Lokale Node-Version stimmt: `node --version` = `v24.15.0` (Plan-Angabe A2
  korrekt); `npm ci` reproduziert lokal die EBADENGINE-Situation.
- Lücke: Eine lokale Vorabprüfung unter Node ≥26.13
  (`npx semantic-release --dry-run --no-ci --branches staging` nach
  `npm ci`) wäre technisch möglich (nvm/volta/direkter Download) und würde
  Laufzeit- und Formatprobleme **vor** dem Merge sichtbar machen. Der Plan
  verzichtet darauf („echte Prüfung erst im Workflow-Lauf") — damit würde ein
  eventueller Folgefehler erst auf dem gemergten `staging`-Stand sichtbar.

## 4. Akzeptanzszenarien — weitgehend vollständig, eine Prüflücke

A1–A5 decken den Kernfehler vollständig ab (Syntax, Plausibilität, PR-Ebene,
Merge-Nachweis, Folge-Verifikation). **Lücke in A4:** Der Schritt
„Determine next version" parst die dry-run-Ausgabe per Bash-Regex
(`staging-ci.yml` Z. 273: `The next release version is …`); dasselbe Format
parst `resolve-release-version.mjs` Z. 114
(`/the next release version is (…)/i`). A4 akzeptiert `changed=false` als
„zulässiges, erfolgreiches Ergebnis". Ändert `semantic-release@26` den
Log-Wortlaut, endet der Schritt erfolgreich mit `changed=false` — ein
**stilles Fehlverhalten** (keine Versionsermittlung trotz releasefähiger
Commits), das A4 fälschlich als Erfolg wertet. A4 müsste ergänzend verlangen,
die `next release version`-Zeile im Log zu prüfen, sofern der Merge-Commit
einen Release erwarten lässt (`fix:`/`feat:`-Prefix), bzw. die Regex-
Kompatibilität explizit zu verifizieren.

## 5. Anforderungsteile — alle abgedeckt

- PR gegen `staging`: Schritt 5 des Plans. ✓
- Admin-Merge (Benutzerkontextwechsel): Schritt 5, referenziert `issue.md`. ✓
- „Sicherstellen, dass der Fehler behoben ist": A4/A5 + §5 (Merge-Ebene). ✓
- Commit-Message mit Closing-Keyword `Closes #215` — Issue-Nummer aus
  `issue.md` verifiziert; Schließung erst bei Promotion nach `main`
  entspricht dem Staging-Workflow. ✓
- `.\scripts\Run-StaticChecks.ps1` im lokalen Verifikationsplan enthalten
  (AGENTS-Vorgabe). ✓
- Reine CI-Korrektur, kein App-Code — Scope eingehalten. ✓

## 6. ci-instructions.md-Snippets — korrekt identifiziert

- Z. 341–344: `Setup Node.js` / `actions/setup-node@v7` / `node-version: '24'`
  im wiedergegebenen `version`-Job-Ausschnitt aus `staging-ci.yml` —
  verifiziert (Z. 335–344).
- Z. 722–725: `Setup Node.js` / `actions/setup-node@v7` / `node-version: '24'`
  im wiedergegebenen Release-Job-Ausschnitt — verifiziert (Z. 717–725).
- Beide Snippets sind die einzigen `node-version`-Vorkommen in der Datei
  (grep: Z. 344 und Z. 725). Zeilenangaben im Plan stimmen.

## 7. Risiken — zwei vom Plan übersehene Punkte

a) **Plugin-Major-Kompatibilität (nicht im Plan):** `package-lock.json`
   bündelt unter `semantic-release@26.0.1` Plugin-Versionen der neuen
   Generation (`@semantic-release/commit-analyzer@14.0.0-beta.4`,
   `release-notes-generator@15.0.0-beta.3`, `@semantic-release/npm@14.0.0-beta.2`,
   `@semantic-release/core@1.0.0-beta.13`, alle ab Z. 5024). `release.config.js`
   lädt die Plugins aber aus dem Projekt-Top-Level — also die **alten
   Majors** `commit-analyzer@13.0.1`, `release-notes-generator@14.1.1`,
   `@semantic-release/github@12.0.9` (`package.json` Z. 10–12). Ob diese
   unter semantic-release 26 lauffähig sind, ist ungeprüft; das Inventory
   listet die Fakten, der Plan benennt kein Risiko und keine Prüfung. Ein
   Plugin-API-Bruch würde erst im Merge-Lauf (A4) sichtbar — besser vorab
   lokal unter Node 26 verifizieren oder die Plugin-Pins im selben Fix
   mitbetrachten.

b) **Dry-run-Output-Format (nicht im Plan):** siehe §4 — beide Parser hängen
   am exakten Wortlaut der semantic-release-Ausgabe; ein Major-Bump kann die
   Log-Zeile ändern. Risiko stilles `changed=false`; der Plan sieht keine
   Verifikation des Output-Formats vor.

Korrekt beurteilt im Plan dagegen: gepinnte `setup-node`-Version (irrelevant
für Range-Support), Cache-/Toolcache-Verhalten, Runner ohne vorinstalliertes
Node 26 (Nachladen durch setup-node), Floating-Pin-Fluktuation,
Verifikationslatenz durch `needs:`-Kette und Backmerge-Skip
(`staging-ci.yml` Z. 238–239, verifiziert), Dependabot-Langlebigkeit.

## Lückenliste

1. **Plugin-Kompatibilität ungeprüft:** semantic-release@26 bündelt neue
   Plugin-Generationen; die Top-Level-Pins (analyzer 13, rng 14, github 12)
   sind einen Major-Stand dahinter und werden von `release.config.js` geladen.
   Plan benötigt entweder eine lokale Verifikation unter Node 26 oder eine
   explizite Risikobewertung/ggf. Mit-Anhebung der Plugin-devDependencies.
2. **Output-Format-Risiko fehlt:** Die Parser (`staging-ci.yml` Z. 273,
   `resolve-release-version.mjs` Z. 114) hängen am Log-Wortlaut; A4 wertet
   `changed=false` als Erfolg und kann einen Format-Bruch nicht erkennen.
   A4 um eine explizite Prüfung der `next release version`-Zeile ergänzen.
3. **Keine lokale Node-26-Vorabprüfung:** `npx semantic-release --dry-run`
   unter Node ≥26.13 ist lokal machbar und würde Lücken 1+2 **vor** dem
   Merge aufdecken; der Verifikationsplan sieht das nicht vor.

Status: Plan lückenhaft
