# Detail: Composite Actions unter `.github/actions/`

## `build-and-package/action.yml` — `name: Build and package`

Datei: `.github/actions/build-and-package/action.yml`

| Element | Zeilen | Ist-Zustand |
|---------|--------|-------------|
| Inputs | 3–9 | `release-version` (erforderlich, ohne führendes `v`, für `update.json` und `-p:Version`), `release-tag` (erforderlich, z. B. `v1.2.3` / `v1.2.3-rc.1`, für Download-URLs) |
| Setup .NET | 14–17 | `actions/setup-dotnet@v6`, `10.0.x` |
| MAUI-Workload | 19–23 | `dotnet workload restore Reporter.sln`, `shell: bash`, `IncludeIosTarget: false` |
| Restore | 25–29 | `dotnet restore Reporter.sln -r win-x64` |
| Publish (win-x64) | 31–38 | `dotnet publish src/Reporter/Reporter.csproj -p:Version=<release-version> -c Release -f net10.0-windows10.0.19041.0 -r win-x64 --self-contained true -p:PublishReadyToRun=false --no-restore -o artifacts/publish-win-x64` |
| ZIP | 40–43 | `shell: pwsh`, `Compress-Archive` → `release-win-x64.zip` (Vorlage nutzt `bash`/`zip`) |
| `update.json` | 45–68 | `shell: pwsh`; Manifest mit `version` (= `release-version`, also inkl. RC-Suffix bei Pre-Releases), `releaseNotes: "Reporter release <TAG>"`, `publishedAt` (UTC), `assets[]` mit **einem** Eintrag: `platform: "windows"`, `runtimeIdentifier: "win-x64"`, `assetName/assetUrl: release-win-x64.zip` (URL `https://github.com/<repo>/releases/download/<tag>/release-win-x64.zip`), `sha256`, `sizeBytes` |

**Wird aufgerufen von:** `staging-ci.yml` Job `prerelease` (mit `rc_version`/`rc_tag`) und `release.yml` Job `release` (mit `version`/`tag`).

**Abweichungen / fehlende Teile gegenüber der Vorlage (Abschnitt 6.1) und Anforderung:**

- Nur **eine** Zielplattform (`win-x64`). Die Anforderung nennt MAUI-Publish für `ios`, `android`, `win10-x64` „etc." — iOS- und Android-Publish-Schritte fehlen vollständig. Plattformübergreifende Builds sind in einer Composite Action strukturell begrenzt (läuft im Runner-Kontext des aufrufenden Jobs — hier `windows-latest`); ein iOS-Build erfordert `macos-latest` und damit einen eigenen Job bzw. eine Aufteilung.
- `update.json` enthält entsprechend nur das Windows-Asset.
- Kein `release-metadata.json`-Schritt — korrekt, da `msTools.Updater`/`IInstalledVersionProvider` im Repository nicht referenziert wird (Vorlage Abschnitt 7.2 verlangt das Manifest nur bei tatsächlichem Self-Updater).
- `IncludeIosTarget: false` ist in allen Build-Schritten gesetzt; ohne diesen Schalter würde `Reporter.csproj` auf Windows zusätzlich `net10.0-ios` anvisieren.

## `security-scan/action.yml` — `name: Security scan`

Datei: `.github/actions/security-scan/action.yml`

- Inputs: `solution-path`, `artifact-name` (beide erforderlich, Zeilen 4–7).
- Step „Scan for vulnerable packages" (Z. 12–20): `dotnet list <solution> package --vulnerable --include-transitive`, schlägt fehlt (`exit 1`), wenn die Ausgabe `has the following vulnerable packages` oder `Severity` enthält — erfüllt die Vorlagen-Anforderung, dass der Scan blockiert und nicht nur warnt.
- Step „Upload report" (Z. 22–28): lädt `vulnerable-packages.txt` als Artefakt (`if: always()`, 14 Tage Retention).
- Entspricht der Vorlage (Abschnitt 6.2) nahezu wörtlich. **Wird aufgerufen von:** `static-checks` in `staging-ci.yml` und `pr-staging-ci.yml` sowie dem Standalone-Workflow `security-scan.yml`. Kein Änderungsbedarf im Scope.
