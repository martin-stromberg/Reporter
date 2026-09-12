# Übersetzte Anforderungsbeschreibung: Release-Management, Pre-Releases und Staging-Promotion

**Quelle:** Issue #29, Vorlage `ci-instructions.md` (Abschnitte 5, 6.1, 8, 9 und 11)
**Hinweis zum Projektkontext:** `docs/features.md` existiert nicht im Repository; der Projektkontext wurde daher direkt aus dem Bestand (`.github/`, `src/`, `scripts/`, `release.config.js`, `package.json`) erhoben.

## Fachliche Zusammenfassung

Es ist eine vollständige, automatisierte Release-Pipeline nach dem Branch-Modell `staging` → `main` für die .NET-MAUI-App `Reporter` einzurichten. Jeder Push auf `staging` soll — nach erfolgreichen Quality Gates — über `semantic-release` (Dry-Run) die nächste Version ermitteln und ein RC-Pre-Release (`vX.Y.Z-rc.N`) als GitHub-Pre-Release erzeugen; ein erfolgreicher `staging`-Lauf öffnet automatisch einen Promotion-PR nach `main`. Jeder Push auf `main` erzeugt ein stabiles Release (`vX.Y.Z`) inkl. versionierter MAUI-Artefakte und `update.json`; anschließend wird ein Backmerge-PR `main` → `staging` geöffnet, damit `staging` nicht driftet. Die Workflow-Dateien existieren weitgehend bereits, sind aber teilweise deaktiviert (`on:`-Blöcke auskommentiert) bzw. noch auf die `MyApp.*`-Vorlage bzw. nur auf Windows-Artefakte ausgelegt und müssen reaktiviert, vervollständigt und auf `Reporter` adaptiert werden.

## Betroffene Klassen und Komponenten

Es handelt sich um ein reines CI-/Infrastruktur-Feature; betroffen sind keine C#-Klassen, sondern folgende Artefakte:

### Workflow-Dateien unter `.github/workflows/` (alle bereits vorhanden)

- `staging-ci.yml` (`name: Pre-Release`) — **Trigger `on: push → staging` ist auskommentiert** und zu reaktivieren. Jobs `static-checks`, `build-and-test`, `version` und `prerelease` sind bereits angelegt; zu prüfen/ergänzen: `detect-backmerge`-Job gemäß Vorlage, korrekte `needs:`-Kette (`version` benötigt `[detect-backmerge, static-checks, build-and-test]`), und dass `rc_version` (nicht `version`) in `build-and-package` verdrahtet ist (Vorlage Abschnitt 11.2).
- `staging-to-main-promotion.yml` (`name: Staging to Main Promotion`) — vorhanden und aktiv (`workflow_run` auf `"Pre-Release"`, `branches: [staging]`); Konsistenz zum Display-Namen von `staging-ci.yml` prüfen (Vorlage Abschnitt 11.5).
- `sync-staging-with-main.yml` (`name: Backmerge Main to Staging`) — vorhanden und aktiv (`push → main`); Backmerge-PR-Erzeugung mit Label `automated-backmerge`.
- `release.yml` (`name: Release`) — **Trigger `on: push → main` + `tags: ['v*.*.*']` ist auskommentiert** und zu reaktivieren; Jobs/Steps für `resolve-release-version.mjs`, Asset-Repair (`upload-existing`), Release-Gate-Tests, `build-and-package` sowie Release-Erzeugung (automatisch via `semantic-release`, manuell via `gh release create`) sind bereits angelegt und auf Vollständigkeit gegen Abschnitt 8 zu prüfen.
- `verify-pr-source.yml` — vorhanden und aktiv; lehnt PRs nach `main` ab, die nicht aus `staging` stammen.
- `pr-staging-ci.yml` (`name: PR CI for Staging`) — **Trigger `on: pull_request → staging` ist auskommentiert.** Nicht explizit im Scope der Anforderung, aber faktisch Voraussetzung für das Branch-Protection-Modell (Status Checks `static checks` / `build & test` auf `staging`-PRs); Aktivierung ist mitzuplanen.
- `security-scan.yml` — vorhanden und aktiv (`schedule` + `workflow_dispatch`); kein Änderungsbedarf im Scope.

### Composite Actions unter `.github/actions/` (vorhanden)

- `build-and-package/action.yml` — vorhanden und bereits auf `Reporter.sln` / `src/Reporter/Reporter.csproj` adaptiert (Windows-`publish` für `net10.0-windows10.0.19041.0`/`win-x64`, `release-win-x64.zip`, `update.json` via PowerShell). Zu erweitern um MAUI-Publish für die geforderten Plattformen (`ios` auf `macos-latest`, ggf. `android`, `win10-x64`) und entsprechende Release-Artefakte (`.ipa`, `.apk`, `.msix` bzw. ZIP) sowie Erweiterung des `update.json`-Manifests um die zusätzlichen Assets.
- `security-scan/action.yml` — vorhanden; kein Änderungsbedarf im Scope.

### Skripte und Konfiguration im Repo-Root

- `scripts/resolve-release-version.mjs` — vorhanden; `EXPECTED_ASSETS = ["release-win-x64.zip", "update.json"]` muss an die finale Asset-Liste (zusätzliche Plattform-Artefakte) angepasst werden; Prerelease-Guard aus Abschnitt 11.1 sicherstellen.
- `release.config.js` — vorhanden, weicht aber von der Vorlage ab: `branches` enthält aktuell `"main"` **und** `{ name: "staging", prerelease: "rc" }`, während die Vorlage `branches: ["main"]` verlangt (RC-Suffix wird in `staging-ci.yml` manuell angehängt; Dry-Run nutzt `--branches staging` als Override). Zu vereinheitlichen.
- `package.json` — vorhanden mit den benötigten `devDependencies` (`semantic-release` 25.x, `@semantic-release/commit-analyzer`, `@semantic-release/release-notes-generator`, `@semantic-release/github`, `conventional-changelog-conventionalcommits`) und Script `"release": "semantic-release"`; Versionen/Assets ggf. an Vorlage angleichen.
- `package-lock.json` — vorhanden; für `npm ci` in den Workflows zwingend konsistent zu halten.

### Projektdateien (bedingt betroffen)

- `src/Reporter/Reporter.csproj` — `TargetFrameworks` enthält aktuell nur `net10.0-windows10.0.19041.0` (Windows) und `net10.0-ios` (bedingt über `IncludeIosTarget`); `net10.0-android` ist **nicht** konfiguriert, obwohl die Anforderung Android-Publish nennt. `WindowsPackageType` ist `None` (unpackaged) — ein `.msix`-Artefakt wäre damit nicht ohne Weiteres erzeugbar.
- `Reporter.sln`, `src/Reporter.Tests/Reporter.Tests.csproj` — werden von den Gates/Release-Gate referenziert (`dotnet test`); keine inhaltliche Änderung erwartet.

### GitHub-Repository-Einstellungen (außerhalb des Codes)

- Branch-Protection für `main`: nur PRs aus `staging` erlaubt (ergänzend zu `verify-pr-source.yml`), PR-Pflicht, ggf. Einschränkung der Push-Berechtigten.
- Branch-Protection für `staging`: PR-Pflicht, required Status Checks (`static checks`, `build & test`), „up to date"-Anforderung (Vorlage Abschnitt 1).
- Labels `automated-promotion` (Farbe `0E8A16`) und `automated-backmerge` (Farbe `1D76DB`) — werden von den Workflows per `gh label create --force` beim ersten Lauf angelegt bzw. einmalig vorab zu erstellen.
- Secrets für iOS-Code-Signing (Zertifikat, Provisioning Profile) — falls signierte `.ipa`-Builds gefordert sind.

### Tests

- Unit-Tests für `scripts/resolve-release-version.mjs` sind laut Vorlage (Abschnitt 8.2) zu schreiben/vorhanden zu halten — insbesondere ein Regressionstest für den Prerelease-Guard (Abschnitt 11.1).
- Keine Änderungen an `Reporter.Tests` selbst erforderlich; der Release-Gate-Step in `release.yml` muss auf `Reporter.Tests` zeigen (kein `MyApp.Tests`).

## Implementierungsansatz

1. **Bestandsaufnahme der deaktivierten Teile:** In `staging-ci.yml`, `release.yml` und `pr-staging-ci.yml` sind die `on:`-Trigger auskommentiert. Diese sind gemäß Vorlage zu reaktivieren (`staging-ci.yml`: `push → staging`; `release.yml`: `push → main` + `tags: ['v*.*.*']`; `pr-staging-ci.yml`: `pull_request → staging`).
2. **`staging-ci.yml` vervollständigen:** `detect-backmerge`-Job gemäß Vorlage (Abschnitte 4/5) ergänzen, falls nicht vorhanden; `version`-Job mit `semantic-release --dry-run --no-ci --branches staging`, RC-Nummer aus `git tag --list "v<version>-rc.*"`, Outputs `changed`, `version`, `rc_tag`, `rc_version`; `prerelease`-Job ruft `./.github/actions/build-and-package` mit `release-version: rc_version` (nicht `version` — Vorlage 11.2) und erzeugt das GitHub-Pre-Release via `gh release create --prerelease --generate-notes`.
3. **`build-and-package`-Action erweitern:** MAUI-Publish pro Zielplattform. Windows (`net10.0-windows10.0.19041.0`, `win-x64`) existiert bereits. iOS (`net10.0-ios`) erfordert `macos-latest`-Runner und Code-Signing-Secrets; Android (`net10.0-android`) erfordert das TFM im `.csproj`. Je nach Zielplattform entstehen `.ipa`, `.apk`, `.msix` oder ZIP-Artefakte; das `update.json`-Manifest ist um diese Assets (Plattform, `runtimeIdentifier`, `assetName`, `assetUrl`, `sha256`, `sizeBytes`) zu erweitern. Achtung: Die Action läuft als Composite Action im Kontext des aufrufenden Jobs — plattformübergreifende Builds erfordern entweder mehrere Jobs oder eine Aufteilung der Action (Annahme; Details in der Planung zu klären).
4. **`release.yml` reaktivieren und prüfen:** Ablauf gemäß Abschnitt 8: `resolve-release-version.mjs` klassifiziert den Ref (manueller Tag-Push vs. automatischer `main`-Push), prüft Release-Existenz und Asset-Vollständigkeit (`create` / `upload-existing` / kein Release), inkl. Fallback-Scan auf unvollständige Releases mit Prerelease-Guard. Anschließend Release-Gate-Tests (`Reporter.Tests`), `build-and-package`, dann Release-Erzeugung: automatisch via `npm run release` (semantic-release erstellt Tag + GitHub-Release + Asset-Upload) oder manuell via `gh release create`; Repair-Pfad via `gh release upload --clobber`.
5. **Semantic-Release-Konfiguration vereinheitlichen:** `release.config.js` auf `branches: ["main"]`, `tagFormat: "v${version}"`, `preset: "conventionalcommits"` bei `commit-analyzer` und `release-notes-generator`, `successComment`/`failComment: false` bei `@semantic-release/github` sowie `RESOLVE_DRY_RUN`-Plugin-Umschaltung gemäß Vorlage; `@semantic-release/github`-Assetliste an die tatsächlichen Asset-Namen anpassen.
6. **Promotion und Backmerge:** `staging-to-main-promotion.yml` feuert per `workflow_run` auf den Abschluss von `Pre-Release` (Display-Name, nicht Dateiname — Vorlage 11.5), prüft `git diff origin/main..HEAD` und öffnet bei Änderungen einen Draft-PR `staging` → `main` mit Label `automated-promotion` (nur wenn noch keiner offen ist). `sync-staging-with-main.yml` feuert auf jeden Push nach `main` und öffnet einen PR `main` → `staging` mit Label `automated-backmerge`; dieser PR ist zwingend per „Create a merge commit" zu mergen (Rebase/Squash würde den Release-Tag aus der `staging`-Historie lösen und die RC-Zählung brechen — Vorlage Abschnitt 9).
7. **Branch-Protection einrichten:** `main` so konfigurieren, dass nur PRs aus `staging` gemergt werden können (kombiniert mit `verify-pr-source.yml` als CI-Seitensicherung); `staging` mit Required Status Checks. Hinweis aus Vorlage 11.7: Bypass-Berechtigungen für Admins beachten — Schutz via `gh api .../branches/main/protection` verifizieren, nicht durch Test-Push.
8. **Cold-Start beachten (Vorlage 11.5):** Da `workflow_run`- und `push → main`-Trigger nur auf dem Default-Branch wirken, feuern Promotion/Backmerge erst, nachdem die Workflow-Dateien einmalig auf `main` gelandet sind; der allererste Promotion-PR ist manuell zu erstellen.
9. **Namenskonventionen:** Alle Verweise auf `Reporter.sln` / `src/Reporter/Reporter.csproj` / `Reporter.Tests`; keine `MyApp.*`-Pfade (Akzeptanzkriterium). Versionierung `vX.Y.Z` (stabil) und `vX.Y.Z-rc.N` (RC, `N` beginnt bei 1 pro Version).

Abhängigkeiten: `security-scan`-Action und `security-scan.yml` existieren bereits und bleiben unverändert; `resolve-release-version.mjs`, `release.config.js`, `package.json`/`package-lock.json` existieren bereits und sind anzupassen statt neu zu erstellen. Der Hinweis „falls CI-Grundgerüst noch nicht steht" entfällt weitgehend — das Grundgerüst ist vorhanden, jedoch teilweise deaktiviert.

## Konfiguration

- **Repository-Ebene (GitHub Settings / `gh api`):** Branch-Protection für `main` (nur PRs aus `staging`, PR-Pflicht, Push-Restriktion) und `staging` (PR-Pflicht, Required Checks `static checks` + `build & test`, „up to date"); Labels `automated-promotion` (`0E8A16`) und `automated-backmerge` (`1D76DB`).
- **Secrets:** `GITHUB_TOKEN` reicht für Releases/PRs (mit `contents: write`, `pull-requests: write`, `issues: write` in den Workflow-Permissions). Für signierte iOS-Builds zusätzliche Secrets erforderlich (Signing-Zertifikat, Provisioning Profile, ggf. Apple-ID/Team-ID) — Umfang abhängig von der geklärten iOS-Anforderung.
- **Versions-/Release-Konfiguration im Code:** `release.config.js` (Branches, Tag-Format, Plugins, Asset-Pfade via `RELEASE_ASSET_PATHS`/`RELEASE_MANIFEST_PATH`/`RELEASE_VERSION`), `package.json` (pinned devDependencies + `release`-Script), `scripts/resolve-release-version.mjs` (`EXPECTED_ASSETS`, `AUTOMATIC_RELEASE_BRANCHES`).
- **Projekt-Konfiguration:** `Reporter.csproj` (`TargetFrameworks`, `IncludeIosTarget`-Schalter, `WindowsPackageType`, `ApplicationDisplayVersion`/`ApplicationVersion` vs. `-p:Version=`-Override beim Publish).

## Offene Fragen

1. **Zielformen der MAUI-Artefakte:** Die Anforderung nennt `ios`, `android`, `win10-x64` „etc." — welche Plattformen sind verbindlich? `net10.0-android` ist in `Reporter.csproj` aktuell nicht als Target Framework enthalten; soll es ergänzt werden? Soll `maccatalyst` ebenfalls gebaut werden?
2. **iOS-Code-Signing:** Sind die benötigten Secrets/Zertifikate/Provisioning Profiles vorhanden bzw. vom Kunden bereitzustellen, oder genügt ein unsignierter bzw. ad-hoc-signierter Build? Ohne Secrets ist kein verteilbares `.ipa` erzeugbar.
3. **Windows-Artefakt-Form:** `WindowsPackageType` ist `None` (unpackaged); die vorhandene Action liefert `release-win-x64.zip`. Ist das ZIP weiterhin das gewünschte Windows-Artefakt, oder soll auf `.msix` (paketiert) umgestellt werden (erfordert `WindowsPackageType`-Änderung und Signierung)?
4. **`update.json` / Self-Updater:** `msTools.Updater` ist im Repository nicht referenziert (kein Treffer in `src/`). Soll `update.json` dennoch als Release-Asset erzeugt werden (Anforderung: „falls Self-Updater gewünscht"), und ist `release-metadata.json` aus Vorlage Abschnitt 7.2 zu erzeugen — oder entfällt beides?
5. **RC-Strategie in `release.config.js`:** Die bestehende Datei konfiguriert `staging` als semantic-release-Prerelease-Branch (`prerelease: "rc"`); die Vorlage verlangt `branches: ["main"]` mit manuell angehängtem RC-Suffix. Soll auf die Vorlagen-Variante umgestellt werden (empfohlen, da `staging-ci.yml` den RC-Suffix bereits selbst berechnet), oder ist die aktuelle Variante beabsichtigt?
6. **Reaktivierung von `pr-staging-ci.yml`:** Der PR-Workflow ist ebenfalls auskommentiert, aber nicht explizit im Scope genannt. Da Branch-Protection auf `staging` die Status Checks `static checks`/`build & test` voraussetzt und die Promotion sonst ungeprüfte Stände hochziehen könnte: Ist die Reaktivierung Teil dieses Issues?
7. **Branch-Protection-Umsetzung:** Wer richtet die Protection-Regeln ein — manuell durch den Repo-Admin, oder soll ein dokumentiertes `gh api`-/Skript-Vorgehen Teil des Deliverables sein? Sind Bypass-Ausnahmen für Admins gewünscht (Vorlage 11.7)?
8. **Mehrplattform-Build-Topologie:** Sollen die Plattform-Artefakte in einem einzigen `prerelease`/`release`-Job (Matrix über Runner) oder in getrennten Jobs pro Plattform erzeugt und im Release aggregiert werden? Das beeinflusst die Struktur der Composite Action (Aufteilung in Build- vs. Package-Teile).
9. **Release-Gate bei `Reporter.Tests`:** Der Vorlagen-Step `dotnet test MyApp.Tests` ist auf `src/Reporter.Tests/Reporter.Tests.csproj` zu mappen; gelten die bekannten Stolpersteine (Release-Konfiguration in Test-Helpern, Vorlage 11.6) für `Reporter.Tests` — gibt es Helper mit hartcodiertem `bin/Debug`?
