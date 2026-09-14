<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Release-Management — Fehlerbehebung

## Kein Pre-Release trotz Push auf `staging`

**Symptom:** Der Workflow `Pre-Release` läuft grün durch, aber es entsteht kein GitHub-Pre-Release und kein Promotion-PR.

**Ursache:** `semantic-release --dry-run` hat keine releasefähige Änderung gefunden (`changed=false`) — die Commits folgen nicht dem Conventional-Commits-Format (`fix:`/`feat:`/`BREAKING CHANGE` usw.), oder der Push war ein reiner Backmerge (`is_backmerge=true`).

**Lösung:**
1. Im Job `version` den Step `Determine next version` prüfen — dort steht die Dry-Run-Ausgabe.
2. Commit-Botschaften auf `staging` gegen das Preset `conventionalcommits` prüfen.
3. Bei Backmerge: gewolltes Verhalten — kein Eingriff nötig.

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

**Ursache:** Gewollt — nach `main` darf nur der Promotion-PR aus `staging` gemergt werden.

**Lösung:** Änderungen zuerst nach `staging` bringen; der reguläre Weg ist Feature-Branch → `staging` → Promotion-PR → `main`.
