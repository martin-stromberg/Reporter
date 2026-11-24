<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| `dotnet workload list` zeigt `maui-windows ≥ 11.0.10` lokal; `dotnet list package --outdated` meldet `Microsoft.Maui.Controls` nicht mehr | Schritt 1 (`sdk.workloadVersion`-Pin in `global.json`) + Schritt 2 (lokaler Workload-Abgleich via `dotnet workload update`, VS-Installer-Fallback); Akzeptanzszenario 2 | Schritt 5: `dotnet workload list`, `dotnet list package --outdated` mit `IncludeIosTarget=false`/`IncludeAndroidTarget=false`; Szenario-2-Nachweisspalte | Abgedeckt |
| Aufgelöste `Microsoft.Maui.Controls`-Version ≥ 11.0.10 im Build-Log lokal und in der CI nachweisbar | `sdk.workloadVersion` wirkt an allen neun `dotnet workload restore`-Stellen (verifiziert: `pr-staging-ci.yml` Z. 76/115/194, `staging-ci.yml` Z. 73/112/191, `security-scan.yml` Z. 29, `build-and-package/action.yml` Z. 22, `package-android/action.yml` Z. 22); neuer Schritt `Log installed workloads` (`dotnet --version` + `dotnet workload list`) an jeder Stelle | CI-Job-Logs aller neun Restore-Stellen (Schritt 3); Validierungsregel „Aufgelöste Version ≥ 11.0.10" | Abgedeckt |
| Pre-Release-Pipeline funktioniert unverändert: `static checks`, `build & test` (PR-CI), `staging-ci`-Gates, `package-windows`, `package-android`, ggf. `package-ios`, `prerelease` erzeugt GitHub-Pre-Release | Schritt 6 (`fix:`-Commit → `version.changed == 'true'`; verifiziert gegen `staging-ci.yml`: `version` Z. 236–289, `package-*` mit `if: needs.version.outputs.changed == 'true'`, `prerelease` Z. 379–416) + Schritt 7 (Post-Merge-Verifikation); Akzeptanzszenario 1 = vollständiger Happy Path vom Merge bis zum sichtbaren Pre-Release | Echter `staging-ci`-Lauf nach Merge mit fail-fast-Überwachung (`gh run watch`/`gh pr checks --fail-fast`, `gh run view --job <id> --log-failed`), `gh release list` | Abgedeckt |
| iOS-CI-Pin `27.2.11039` (`ios-workload-version`, `xcode-path` `/Applications/Xcode_27.2.app`, Runner `xcode-27`) bleibt unverändert wirksam | `.github/actions/package-ios/action.yml` explizit unverändert (Scope-Ausschluss 3, bestätigt). Verifiziert gegen `action.yml` Z. 96–100: die Action überschreibt `global.json` im CI-Workspace **ohne** `workloadVersion` → der Repo-Pin ist dort wirkungslos; eigener Rollback-Pin (Z. 108–123) und `DTSDKName`-Gate (Z. 230–256) bleiben wirksam | `package-ios`-Lauf auf `staging` (Gate `IOS_SIGNING_ENABLED` benannt, Szenario 1 + Schritt 7) | Abgedeckt |
| Repo-weiter Workload-Pin erzeugen (Anforderung: „erzeugen") | Schritt 1: `sdk.workloadVersion` (Workload-Set des Bands 11.0.1xx) in `global.json`; konkrete Set-Version bei Umsetzung zu ermitteln; Designentscheidung begründet Wahl gegenüber Rollback-File/`--version`-Alternativen | Lokaler Verifikations-Check in Schritt 1 (`dotnet workload restore Reporter.sln` installiert gepinnten Stand) + Szenario 4 (Fehlerfall ungültiger Pin) | Abgedeckt |
| CI-Diagnoseschritt `dotnet workload list` erzeugen | Schritt 3: identischer Schritt `Log installed workloads` an allen neun Stellen, `shell: bash` in Composite Actions | Schritt selbst ist der dauerhafte Nachweis im Job-Log; YAML-Syntax lokale Prüfung benannt | Abgedeckt |
| Lokaler `ios`-Stand `27.2.11039` nach Update prüfen und ggf. wiederherstellen | Schritt 2 + Akzeptanzszenario 3: Drift-Prüfung, Wiederherstellung per Rollback-File (`{"microsoft.net.sdk.ios": "27.2.11039"}` + `--from-rollback-file` + `--skip-manifest-update`) analog `package-ios`-Muster, alternativ begründete Dokumentation der Abweichung | `dotnet workload list`-Output (Szenario-3-Nachweis); Validierungsregel „Lokaler ios-Stand" | Abgedeckt |
| Entwickler-Dokumentation zum Workload-Abgleich erzeugen | Schritt 4: `docs/help/entwicklung/maui-workload-updates.md` neu (Pin-Mechanismus, Bump-Verfahren, VS-Verwaltungs-Fallback, iOS-Pin-Interaktion), Eintrag in `index.md`, Querverweis in `wiederkehrende-reviews.md` | Lizenzheader-Pflicht benannt; `Run-StaticChecks.ps1`-Lauf in Schritt 5 | Abgedeckt |
| Kein App-Code-/PackageReference-Bump; kein SDK-Bandwechsel; kein iOS-/Android-Pin-Eingriff; `Logging.Debug` separat; andere Dev-Maschinen nicht durch PR | Scope-Ausschlüsse 1–6, alle mit Begründung und Status `bestätigt (2026-11-23)` | — | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Pre-Release-Kette `staging-ci` (fix:-Commit → version.changed → package-* → prerelease) | Echter Workflow-Lauf nach Merge mit fail-fast-Überwachung und Run-/Release-Nachweis (Schritt 7, Szenario 1) | Abgedeckt — der „Benutzerfluss" ist die CI-Pipeline selbst; Funktionsnachweis nur über echte Läufe möglich |
| App-UI-Flüsse | — | Nicht erforderlich mit Begründung: keine Page/Control/Flow-Änderung; FlaUI-Suite liegt in keinem CI-Gate und liefert für Toolchain-Änderung kein Signal (Plan `Tests`-Abschnitt, S. E2E-Begründung; bestätigt durch Bestandsaufnahme tests.md) |
| Lokaler Workload-Abgleich auf Dev-Maschine | Kein automatisierbarer E2E-Test möglich (Umgebungszustand, VS-Verwaltung); geplant: `dotnet workload list`- und `list package --outdated`-Nachweis (Szenario 2/3) | Nicht erforderlich mit Begründung — Umgebungszustand ist kein App-Fluss; Nachweiswege konkret benannt |

## Hinweise

Optionale Hinweise zur Nachplanung (keine Lücken):

- **`test-results.md`:** Die Anforderung nennt `test-results.md` als Ort für den Verifikationsnachweis; die Datei existiert nicht im Repo-Root und wird im Plan nicht erwähnt. Der Plan dokumentiert den Nachweis stattdessen im PR (Schritt 5) und als Run-URL/Pre-Release-Tag (Schritt 7) — deckt die Absicht ab; bei Nachplanung ggf. explizit festlegen, wo der Nachweis final landet.
- **Ermittlung der konkreten `sdk.workloadVersion`:** Schritt 1 lässt die Methode offen. Konkreter Weg: Workload-Set-Versionen des Bands sind auf nuget.org unter `Microsoft.NET.Workloads.11.0.100` gelistet; `dotnet workload update`/`install --version <set>` oder `dotnet workload history` liefern den Kandidatenstand, `dotnet workload list` danach den `maui-windows`-Manifeststand (muss `≥ 11.0.10` zeigen).
- **`sdk.workloadVersion`-Mechanismus verifiziert:** Dokumentierter Standard seit .NET SDK 8.0.400 — ein `workloadVersion`-Eintrag in `global.json` versetzt `install`/`update`/`restore` im Repo-Kontext in den Workload-Set-Modus, auch ohne `workload config`-Aufruf. Gültig für 11.0.1xx (lokale Maschine läuft bereits im Set-Modus). Kein Plan-Risiko.
- **Bandkopplung beachten:** Der gepinnte Workload-Set ist an das Band 11.0.1xx gebunden. Löst `setup-dotnet '11.0.x'` künftig ein höheres Feature-Band auf (und `sdk.version 11.0.100`/`latestPatch` kann nicht roll-forwarden bzw. der Set passt nicht zum Band), schlägt der Restore sichtbar fehl — gewollt fail-fast, aber als künftiger Wartungspunkt in der Doku-Seite erwähnenswert.
- **`release.yml` nicht explizit benannt:** Die Anforderung führt `release.yml` als bei der nächsten Promotion betroffen. Der Plan ändert nur die Composite Actions — Pin und Diagnoseschritt greifen dort automatisch mit. Zur Klarheit kann die implizite Mitwirkung in `release.yml` in der Doku bzw. PR-Beschreibung benannt werden.
- **Rekurrente iOS-Drift durch den Set-Pin:** Nach dem Repo-Pin zieht jeder lokale `dotnet workload update`/`restore` im Repo-Kontext (bei `IncludeIosTarget`-Default `true`) `ios`/`maccatalyst` auf die Set-Version — d. h. die Abweichung vom CI-Pin `27.2.11039` ist künftig systematisch, nicht nur einmalig. Der Plan deckt das über Szenario 3 + Doku-Baustein „iOS-Pin-Interaktion" ab; die Doku-Seite sollte den wiederkehrenden Charakter explizit machen.
