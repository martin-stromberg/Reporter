<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Inventar: SDK- und Workload-Pins

## global.json

`global.json` (komplette Datei, 5 Zeilen):

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

- **Kein `sdk`-Eintrag** – lokal darf jedes installierte SDK laufen; Entscheidung für den
  Migrationsplan: SDK-Version pinnen oder nicht (CI setzt Versionen explizit).
- `test.runner: Microsoft.Testing.Platform` bleibt relevant; `dotnet test` mit `--`-Args
  hängt daran.

## MauiVersion

- `src/Reporter/Reporter.csproj:76`: `Microsoft.Maui.Controls` mit `Version="$(MauiVersion)"`.
- `MauiVersion` ist **nirgends im Repo definiert** (Repo-weite Suche: nur diese Verwendung
  + Doku-Erwähnung in `docs/help/release-management/wiederkehrende-reviews.md:38`).
  Der Wert kommt aus dem installierten MAUI-Workload-Manifest → die Paketversion folgt
  automatisch dem Workload-Stand des jeweiligen SDK-Bands.
- Konsequenz: Die MAUI-11-Anhebung erfolgt primär über `dotnet workload install/restore`
  mit einem 11er-SDK, nicht über eine Versionsnummer im Repo.

## Workload-Installation (Übersicht der Aufrufe)

| Stelle | Befehl | Kontext |
|---|---|---|
| `.github/workflows/staging-ci.yml:73,112` | `dotnet workload restore Reporter.sln` | static-checks + build-and-test (windows-latest, `IncludeIosTarget=false`, `IncludeAndroidTarget=false`) |
| `.github/workflows/pr-staging-ci.yml:76,115` | `dotnet workload restore Reporter.sln` | gleiche Jobs für PRs |
| `.github/workflows/security-scan.yml:29` | `dotnet workload restore Reporter.sln` | Weekly-Scan (`IncludeIosTarget=false`) |
| `.github/actions/build-and-package/action.yml:22` | `dotnet workload restore Reporter.sln` | Windows-Package (`IncludeIosTarget=false`) |
| `.github/actions/package-android/action.yml:22` | `dotnet workload restore Reporter.sln` | Android-Package (`IncludeAndroidTarget=true`) |
| `.github/actions/package-ios/action.yml:78-91` | Rollback-Pin + `dotnet workload install maui-ios ios --skip-manifest-update` | iOS-Package (macos-latest) |
| `scripts/Run-StaticChecks.ps1` (Kommentar Z. 10) | kein Befehl; Voraussetzung: lokales MAUI-Workload | lokale Checks |

## iOS-Workload-Pin (kritischste Stelle)

`.github/actions/package-ios/action.yml`:

- Input `ios-workload-version`, Default **`26.5.10318`** (Zeilen 44–47) – Microsoft.iOS.Sdk-
  Pack-Version, die mit einem Release-Xcode kompatibel sein muss.
- Zeilen 52–59: SDK-Band-Pin `dotnet-version: '10.0.1xx'` mit Kommentar: 26.x-Manifeste
  existieren nur im 10.0.100-Feed; der 10.0.400-Feed kennt nur 27.x mit Beta-Xcode.
- Zeilen 61–70: schreibt eine temporäre (nicht committed) `global.json` mit
  `{"sdk":{"version":"10.0.100","rollForward":"latestPatch"}}`.
- Zeilen 78–91: Rollback-File `{"microsoft.net.sdk.ios": "<version>"}` +
  `dotnet workload update --from-rollback-file` +
  `dotnet workload install maui-ios ios --skip-manifest-update`;
  danach werden fremde Packs per Glob `Microsoft.iOS.Sdk.net10.0_*/*` gelöscht (Zeile 87).

→ Bei .NET 11: SDK-Band (11.0.xxx), iOS-Pack-Version (11er-Generation, z. B. 27.x/28.x),
Xcode-Kompatibilität des macos-Runners und der Pack-Glob `net10.0_*` → `net11.0_*` müssen
zusammenhängend neu festgelegt werden.

## Runner-Voraussetzungen / CPU-Basis

- `.NET 11 hebt die x64-Mindest-CPU auf x86-64-v2 (SSE4.2 + POPCNT); Arm64-Windows: LSE`
  (Issue-Text). Zu prüfen: `windows-latest`-Runner und Zielgeräte der Nutzer.
  `publish` läuft mit `--self-contained true -p:PublishReadyToRun=false`
  (`build-and-package/action.yml:48-49`) – ReadyToRun ist aus, trotzdem gilt die
  Runtime-Mindestbasis für die ausgelieferte App.
