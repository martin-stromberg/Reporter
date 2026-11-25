<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Release-Management — Fehlerbehebung

## Kein Pre-Release trotz Push auf `staging`

**Symptom:** Der Workflow `Pre-Release` läuft grün durch, aber es entsteht kein GitHub-Pre-Release und kein Promotion-PR.

**Ursache:** `semantic-release --dry-run` hat keine releasefähige Änderung gefunden (`changed=false`) — die Commits folgen nicht dem Conventional-Commits-Format (`fix:`/`feat:`/`BREAKING CHANGE` usw.), oder der Push war ein reiner Backmerge (`is_backmerge=true`).

**Lösung:**
1. Im Job `version` den Step `Determine next version` prüfen — dort steht die Dry-Run-Ausgabe.
2. Commit-Botschaften auf `staging` gegen das Preset `conventionalcommits` prüfen.
3. Bei Backmerge: gewolltes Verhalten — kein Eingriff nötig.

## `semantic-release` bricht ab: „node version >=… is required"

**Symptom:** Der Schritt `Determine next version` im Job `version` (`staging-ci.yml`) oder ein `semantic-release`-Aufruf in `release.yml` (`resolve`/`publish`) endet mit `exit code 1` und einer Meldung wie `[semantic-release]: node version >=26.13 is required. Found v24.x`.

**Ursache:** Ein `semantic-release`-Major-Update (typischerweise ein wöchentlicher Dependabot-PR gegen `staging`) hat die `engines.node`-Untergrenze des Pakets angehoben — `semantic-release@26` verlangt z. B. Node `>=26.13`. Der `node-version`-Pin in den `actions/setup-node`-Schritten wurde nicht mitgeführt, sodass die Jobs unter einer zu alten Node-Version laufen.

**Lösung:**
1. Die geforderte Mindestversion aus der Fehlermeldung bzw. aus `package-lock.json` (`semantic-release` → `engines.node`) entnehmen.
2. Den `node-version`-Pin an allen drei Stellen nachführen — `.github/workflows/staging-ci.yml` (Job `version`) und `.github/workflows/release.yml` (Jobs `resolve` und `publish`). Aktueller Pin: `'>=26.13 <27'` — die Range schließt ein veraltetes `26.x < 26.13` aus dem Runner-Toolcache konstruktiv aus und bleibt innerhalb der Major-Version.
3. Kein Downgrade von `semantic-release` als Alternative: Dependabot würde den Major-Bump beim nächsten Lauf erneut vorschlagen.

> **Hinweis:** Bei jedem `semantic-release`-Major-Update durch Dependabot prüfen, ob die neue `engines.node`-Forderung der aktuelle Pin noch erfüllt — sonst tritt der Fehler im `staging-ci`-Push-Lauf direkt nach dem Merge auf.

## Der erste Promotion-PR wird nicht geöffnet

**Symptom:** Nach dem ersten erfolgreichen `Pre-Release`-Lauf erscheint kein Draft-PR `staging` → `main`.

**Ursache:** `workflow_run`-Trigger wirken nur für Workflow-Dateien, die auf dem **Standardbranch** (`main`) liegen. Beim allerersten Rollout existiert `staging-to-main-promotion.yml` dort noch nicht.

**Lösung:**
1. Den ersten Promotion-PR einmalig manuell anlegen: `gh pr create --base main --head staging --draft --label automated-promotion`.
2. Danach feuert die Promotion automatisch bei jedem erfolgreichen `Pre-Release`-Abschluss.

> **Hinweis:** Gleiches gilt für `sync-staging-with-main.yml` (`push → main`): erst wirksam, sobald die Datei auf `main` liegt.

## `package-ios` wird übersprungen / `release-ios.ipa` fehlt

**Symptom:** Der iOS-Job erscheint als `skipped`, Releases enthalten kein `.ipa`.

**Ursache:** Die Repository-Variable `IOS_SIGNING_ENABLED` ist nicht auf `true` gesetzt — gewolltes Verhalten, solange die Signierungs-Secrets fehlen.

**Lösung:**
1. Secrets `IOS_CODESIGN_KEY` und `IOS_PROVISIONING_PROFILE` hinterlegen.
2. Variable `IOS_SIGNING_ENABLED=true` setzen — danach läuft `package-ios` automatisch mit und das Asset erscheint in `update.json`.

> **Hinweis:** Niemals `IOS_SIGNING_ENABLED` setzen, ohne die Secrets zu hinterlegen — der Publish würde zwar laufen, liefert aber kein verteilbares signiertes `.ipa` bzw. schlägt im Collect-Step fehl.

## `package-ios` schlägt fehl: „requires Xcode 27.2" / SDK-Gate

**Symptom:** Der `package-ios`-Job bricht mit `::error::Xcode not found at '/Applications/Xcode_27.2.app'…` oder `::error::… is not a usable Xcode bundle ('Contents/Developer' is missing)…` im `Select Xcode`-Schritt, mit `error : This version of .NET for iOS (…) requires Xcode 27.2` im `Publish (ios)`-Schritt oder mit `::error::Built with SDK '…' - Apple requires iOS SDK >= 27` im `Verify built SDK version`-Schritt ab.

**Ursache:** Das gepinnte `Microsoft.iOS.Sdk`-Pack (`ios-workload-version`, `27.2.11039`) verlangt exakt Xcode 27.2 — drei Kopplungen müssen zusammenpassen: Runner-Image (`xcode-27`), `xcode-path` (`/Applications/Xcode_27.2.app`, wird per `DEVELOPER_DIR` aktiviert) und Workload-Pin. Der `Select Xcode`-Fehler bedeutet, das Runner-Image enthält die erwartete Xcode-App nicht mehr (Image-Update) oder der Pfad zeigt auf ein Verzeichnis ohne `Contents/Developer` (kein Xcode-Bundle); der Pack-Fehler im Publish, dass ein anderes als das per `xcode-path` gewählte Xcode wirksam war; der Gate-Fehler, dass die erzeugte `.ipa` gegen ein SDK < `iphoneos27` gebaut wurde (z. B. veralteter Workload-Pin).

**Lösung:**
1. `Select Xcode`-Abbruch: Image-Inhalt des `xcode-27`-Runners prüfen (Job-Log `xcodebuild -version`/`xcodebuild -showsdks` bzw. [runner-images](https://github.com/actions/runner-images)-README); `xcode-path` an die vorhandene Release-Xcode-App anpassen bzw. `ios-workload-version` auf das dazu passende Pack anheben — beide Inputs sind bewusst gekoppelt.
2. Pack-Fehler „requires Xcode 27.2": `DEVELOPER_DIR`-Auswahl im `Select Xcode`-Schritt des Job-Logs prüfen; Workload-Pin und `xcode-path` müssen versionsgleich bleiben (Pack 27.2.x ↔ Xcode 27.2).
3. Gate-Fehlschlag in `Verify built SDK version`: absichtlicher Schutz — die `.ipa` würde sonst Apples ITMS-90725-Warnung auslösen. Workload-Pin/Xcode korrigieren und neu laufen lassen; die geloggten `DTSDKName`/`DTPlatformVersion`/`DTXcode`-Werte zeigen die tatsächliche Toolchain.

## `package-ios` schlägt fehl: `error MT7199` (Scene-Manifest)

**Symptom:** Der `Publish (ios)`-Schritt bricht mit `error MT7199` ab (fehlendes `UIApplicationSceneManifest` in der `Info.plist`) — aufgetreten im ersten `Pre-Release`-Lauf nach der .NET-11-/Xcode-27-Migration.

**Ursache:** Builds gegen das iOS-27-SDK verlangen den UIKit-Scene-Lifecycle. Ohne `UIApplicationSceneManifest` lehnt der Build-Check ab — und eine App ohne wirksames Scene-Manifest würde unter iOS 27 zudem beim Start abstürzen.

**Lösung:**
1. Bereits behoben: `Platforms/iOS/Info.plist` deklariert `UIApplicationSceneManifest` mit der MAUI-Standardkonfiguration `__MAUI_DEFAULT_SCENE_CONFIGURATION__`, `Platforms/iOS/SceneDelegate.cs` (`[Register("SceneDelegate")]`, `MauiUISceneDelegate`) liefert den Delegate; dieselbe Kombination liegt präventiv unter `Platforms/MacCatalyst`.
2. Tritt der Fehler erneut auf: `UISceneDelegateClassName` im Manifest muss exakt dem `[Register("SceneDelegate")]`-Namen entsprechen — ein Mismatch führt nicht zum Build-Fehler, sondern zum Startabsturz der App.
3. `ValidateSceneManifest=false` ist **keine** Lösung — es deaktiviert nur den Build-Check, der Startabsturz unter iOS 27 bliebe bestehen.

## iOS-Repair alter Release-Stände schlägt fehl

**Symptom:** Ein `release.yml`-Lauf mit `release_action == 'upload-existing'` auf einen Release-Tag vor der .NET-11-Migration bricht im `package-ios`-Job ab (`Select Xcode` findet kein passendes Xcode bzw. das Pack verlangt eine andere Xcode-Version).

**Ursache:** Der Repair-Pfad checkt den getaggten Stand aus (`checkout-release-tag`) und baut ihn mit **dessen** Workload-Pin — Stände < 0.8.x pinnen `Microsoft.iOS.Sdk 26.x`, das Xcode 26 verlangt. Auf dem `xcode-27`-Image ist kein Xcode 26 installiert — eine Reparatur alter iOS-Binaries ist dort nicht mehr möglich.

**Lösung:** Bewusste Einschränkung, kein Fehler: Die betroffenen alten iOS-Binaries liegen bereits ausgeliefert in App Store Connect und müssen nicht erneut gebaut werden. Reparaturen sind nur für neuere Stände (Pack-Pin ≥ 27.x) vorgesehen; Windows-/Android-Repair alter Stände ist davon nicht betroffen.

## Branch-Protection-API antwortet mit HTTP 403

**Symptom:** `gh api …/branches/<branch>/protection` oder `…/rulesets` liefern `403` mit dem Hinweis auf GitHub Pro / public Repository.

**Ursache:** Branch-Protection ist für **private** Repositories auf dem Free-Plan nicht verfügbar — ein Feature-Gate des GitHub-Plans, kein Pipeline-Fehler.

**Lösung:**
1. Repository public schalten oder Plan auf Pro/Team hochstufen.
2. Danach die dokumentierten `PUT`-Aufrufe aus der [Installationsanleitung](installation.md) ausführen und per `GET` verifizieren.
3. Übergangslösung: `verify-pr-source.yml` (Job `verify-source`) bleibt die aktive Absicherung für PRs nach `main`.

## Backmerge-PR wurde per Squash/Rebase gemergt

**Symptom:** Nach dem Backmerge stimmen RC-Nummern nicht mehr bzw. `semantic-release` findet den letzten Release-Tag auf `staging` nicht.

**Ursache:** Squash oder Rebase des PRs `main` → `staging` löst den Merge-Commit auf, der den Release-Tag `vX.Y.Z` mit der `staging`-Historie verbindet — die Tag-Erreichbarkeit geht verloren.

**Lösung:**
1. Backmerge-PRs immer per **„Create a merge commit"** mergen (Hinweis steht im PR-Body).
2. Bereits falsch gemergt: den Tag auf `staging` erreichbar machen, indem der getaggte Commit per Merge-Commit (`git merge --no-ff vX.Y.Z` bzw. `main`) nachgezogen wird.

## Release existiert, aber Assets fehlen oder sind unvollständig

**Symptom:** Ein `main`-Lauf meldet `release_action: upload-existing` und lädt Assets erneut hoch — oder ein manuell angelegtes Release wird als unvollständig repariert.

**Ursache:** `resolve-release-version.mjs` prüft jedes Release gegen `EXPECTED_ASSETS` (vollständig = jedes Asset `state: "uploaded"`, `size > 0`). Abgebrochene Uploads oder fehlende `update.json` lösen den Repair-Pfad aus; existiert kein neuer Release-Stand, wird automatisch das **älteste** unvollständige Nicht-Prerelease repariert.

**Lösung:**
1. Repair ist gewollt — der `publish`-Job lädt fehlende Assets per `gh release upload --clobber` nach; die `package-*`-Jobs bauen aus dem getaggten Stand (`checkout-release-tag`).
2. Bei wiederholten Reparaturen: Prüfen, ob ein Asset im Packaging scheitert (Job-Logs `package-windows`/`package-android`/`package-ios`) oder `IOS_SIGNING_ENABLED` zwischen Läufen geändert wurde — die erwartete Asset-Liste ändert sich mit der Variable.

## `create-update-manifest.mjs` bricht ab

**Symptom:** Der Aggregations-Job schlägt fehl mit `Expected release asset '…' is missing` oder `RELEASE_ASSETS is not set or empty`.

**Ursache:** Ein erwartetes Paket wurde nicht als Artefakt hochgeladen (Packaging-Job fehlgeschlagen oder Artefaktname weicht ab), oder `release-assets.mjs` lief nicht vor dem Manifest-Step.

**Lösung:**
1. Im fehlgeschlagenen Job prüfen, ob alle `package-*`-Artefakte (`release-win-x64`, `release-android`, ggf. `release-ios`) im Download-Step landen (`pattern: release-*`).
2. Reihenfolge prüfen: `release-assets.mjs` muss vor `create-update-manifest.mjs` laufen, damit `RELEASE_ASSETS` im Environment steht.
3. Bei geändertem `IOS_SIGNING_ENABLED`: Artefaktliste und `EXPECTED_ASSETS` müssen denselben Schalterstand verwenden.

## `package-android` schlägt fehl: `error NETSDK1094`

**Symptom:** Der `Publish (android)`-Schritt bricht mit `error NETSDK1094` ab — `PublishReadyToRun` verlangt ein `Microsoft.NETCore.App.Runtime.*`-Pack, das der Restore nicht aufgelöst hat.

**Ursache:** Seit .NET 11 defaultet der CoreCLR-Android-Runtime `PublishReadyToRun` bei `Release`-Builds implizit auf `true` (`Microsoft.Android.Sdk.CoreCLR.targets`). Der Restore in der Action läuft RID-los (`dotnet restore Reporter.sln`), sodass kein R2R-Runtime-Pack für den Ziel-RID in den Assets steht.

**Lösung:**
1. Bereits behoben: der Publish-Schritt der `package-android`-Action übergibt `-p:PublishReadyToRun=false` — gleiches Muster wie der Windows-Pfad in `build-and-package`; ReadyToRun bringt der `.apk` keinen dokumentierten Nutzen.
2. Alternative beim Umbau der Action: Restore gezielt um RID und `-p:PublishReadyToRun=true` erweitern, damit das Runtime-Pack aufgelöst wird — höherer Pflegeaufwand, daher bewusst nicht gewählt.

## Android-Job liefert nur ein unsigniertes APK

**Symptom:** Warnung `No signed *-Signed.apk found; falling back to unsigned APK …` im `package-android`-Job; das Release enthält ein unsigniertes `release-android.apk`.

**Ursache:** `dotnet publish` hat kein signiertes `*-Signed.apk` erzeugt (kein Signing-Keystore konfiguriert); die Action fällt auf das unsignierte APK zurück.

**Lösung:**
1. Für verteilbare APKs einen Keystore per MSBuild-Properties/Secrets einbinden (z. B. `AndroidSigningKeyStore` etc.) — aktuell bewusst nicht konfiguriert.
2. Der Fallback verhindert ein leeres Release; für Produktivverteilung sollte die Signierung ergänzt werden.

## Coverage-Gate schlägt fehl

**Symptom:** `build & test` bricht mit `Line coverage: X% (threshold: 70%)` ab.

**Ursache:** Die Zeilenabdeckung aus `coverage.cobertura.xml` liegt unter 70 %, oder die Summary konnte nicht gelesen werden (`Could not determine line coverage`).

**Lösung:**
1. Artefakt `coverage-report-staging`/`coverage-report-pr` herunterladen und die ungedeckten Bereiche prüfen.
2. Tests ergänzen; die Schwelle steht in `COVERAGE_THRESHOLD` im Workflow.

## `verify-pr-source` lehnt einen PR nach `main` ab

**Symptom:** Check `verify-source` rot mit `PRs into main are only allowed from staging (got '<branch>')`.

**Ursache:** Gewollt — nach `main` darf nur der Promotion-PR aus `staging` gemergt werden. Besonderheit: Dependabot-**Security**-Update-PRs zielen plattformbedingt immer auf `main` (sie ignorieren `target-branch`) und scheitern mit ihren `dependabot/*`-Branches an diesem Check — bekannte Plattform-Einschränkung, siehe [Wiederkehrende Reviews](wiederkehrende-reviews.md).

**Lösung:** Änderungen zuerst nach `staging` bringen; der reguläre Weg ist Feature-Branch → `staging` → Promotion-PR → `main`. Dependabot-Security-Updates separat behandeln (z. B. Änderung manuell über `staging` einbringen).

## `static checks` schlägt im npm-Audit fehl

**Symptom:** Der Job `static checks` (oder `Security Scan`) bricht mit `::error::Vulnerable npm dependencies detected - see the uploaded report.` ab.

**Ursache:** `npm audit --package-lock-only --audit-level=high` hat in `package-lock.json` mindestens eine Schwachstelle der Severity `high` oder `critical` gefunden — meist in der `semantic-release`-Toolchain (alle npm-Abhängigkeiten sind `devDependencies`, werden aber trotzdem geprüft).

**Lösung:**
1. Das Scan-Artefakt (`vulnerable-packages-pr`/`vulnerable-packages-staging`/`vulnerable-packages-weekly`) herunterladen — die Datei `npm-audit.txt` enthält die betroffenen Pakete und Advisory-Links.
2. Lokal denselben Befund reproduzieren: `npm audit --package-lock-only --audit-level=high` im Repository-Root.
3. Behebung durch Update der Toolchain in `package.json` + `package-lock.json` (bzw. über den von Dependabot geöffneten Update-PR), nicht durch Absenken der Schwelle. Moderate Befunde im Report sind erwartbar — sie blockieren das Gate nicht.

> **Hinweis:** Fehlt Node/npm lokal, bricht `scripts\Run-StaticChecks.ps1` den Security-Teil mit `npm wurde nicht gefunden` ab — Node.js installieren, die Runner-Images bringen es bereits mit.

## Dependabot erzeugt keine Update-PRs

**Symptom:** Trotz vorhandener `.github/dependabot.yml` erscheinen keine `dependabot[bot]`-PRs.

**Ursache:** GitHub liest die Konfiguration ausschließlich vom Standardbranch `main` — solange die Datei nur auf `staging` oder einem Feature-Branch liegt, läuft nichts (gleiche Cold-Start-Regel wie bei den `schedule`-Workflows). Zusätzlich müssen Dependency-Graph und Dependabot-Updates in den Repository-Einstellungen aktiviert sein.

**Lösung:**
1. Promotion `staging` → `main` abwarten bzw. anstoßen.
2. Unter *Settings → Code security and analysis* prüfen, ob Dependency-Graph und Dependabot-Updates aktiviert sind.
3. Manuellen Lauf anstoßen: *Insights → Dependency graph → Dependabot → Check for updates*.
