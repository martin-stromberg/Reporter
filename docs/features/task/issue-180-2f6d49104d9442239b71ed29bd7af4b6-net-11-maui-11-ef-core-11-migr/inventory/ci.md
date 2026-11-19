<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Inventar: CI/CD und Skripte

## Workflows mit .NET-Bezug

### `.github/workflows/staging-ci.yml` (Pre-Release, Push auf staging)

| Job | Runner | .NET-Stellen |
|---|---|---|
| `detect-backmerge` | ubuntu-latest | – |
| `static-checks` | windows-latest | Z. 68-70 `setup-dotnet` `dotnet-version: '10.0.x'`; Z. 73 `dotnet workload restore Reporter.sln`; Z. 76 `dotnet restore -r win-x64`; Z. 79 `dotnet format`; Z. 91 `dotnet build` |
| `build-and-test` | windows-latest | Z. 107-109 `10.0.x`; Z. 112 workload restore; Z. 115/118 restore+build; Z. 121-126 `dotnet test` (MTP: `-- --coverlet ... --report-trx`, Ausschluss `[*]Reporter.Data.Migrations.*`); Z. 129 `dotnet-reportgenerator-globaltool 5.5.11` |
| `version` | ubuntu-latest | nur Node/semantic-release |
| `package-windows` | windows-latest | Composite `./.github/actions/build-and-package` |
| `package-android` | windows-latest | Composite `./.github/actions/package-android` |
| `package-ios` | macos-latest | Composite `./.github/actions/package-ios` (nur bei `vars.IOS_SIGNING_ENABLED`) |
| `prerelease` | ubuntu-latest | – |

### `.github/workflows/pr-staging-ci.yml`

Spiegel von staging-ci (Jobs `static-checks`, `build-and-test`): `10.0.x` an Z. 71-73 und
Z. 110-112, gleiche Workload-/dotnet-Aufrufe.

### `.github/workflows/release.yml` (Push auf main / Tag)

- `release-gate` (windows-latest): Z. 73-75 `setup-dotnet` `10.0.x`, Z. 78 `dotnet test`.
- `package-windows` / `package-android` / `package-ios`: gleiche Composites wie staging-ci.
- `resolve`/`publish`: ubuntu + node/gh (kein .NET).

### `.github/workflows/security-scan.yml` (wöchentlich)

- Z. 24-26 `setup-dotnet` `10.0.x`, Z. 29 workload restore, Z. 32 `dotnet restore`,
  dann Composite `./.github/actions/security-scan` (`dotnet list package --vulnerable`).

### Workflows ohne .NET-Bezug

`deploy-pages.yml`, `sync-staging-with-main.yml`, `staging-to-main-promotion.yml`,
`verify-pr-source.yml`, `review-architecture.yml`, `review-compliance.yml`,
`review-nuget.yml` – keine Änderung nötig.

## Composite Actions

| Action | .NET-Stellen |
|---|---|
| `.github/actions/build-and-package/action.yml` | Z. 17-19 `10.0.x`; Z. 22 workload restore (`IncludeIosTarget=false`); Z. 28 `dotnet restore -r win-x64`; Z. 45-49 `dotnet publish ... --framework net10.0-windows10.0.19041.0 --runtime win-x64 --self-contained true -p:PublishReadyToRun=false` |
| `.github/actions/package-android/action.yml` | Z. 17-19 `10.0.x`; Z. 22 workload restore (`IncludeAndroidTarget=true`); Z. 29 restore; Z. 47-51 `dotnet publish --framework net10.0-android -p:AndroidPackageFormat=apk` |
| `.github/actions/package-ios/action.yml` | Z. 44-47 Input `ios-workload-version` Default `26.5.10318`; Z. 57-59 `dotnet-version: '10.0.1xx'`; Z. 68 temporäre global.json `10.0.100`; Z. 81-90 Rollback-Pin + `dotnet workload install maui-ios ios --skip-manifest-update` + Pack-Cleanup-Glob `Microsoft.iOS.Sdk.net10.0_*`; Z. 98 `dotnet restore -r ios-arm64`; Z. 153-162 `dotnet publish --framework net10.0-ios -p:MauiXamlInflator=XamlC` |
| `.github/actions/security-scan/action.yml` | Z. 18 `dotnet list package --vulnerable --include-transitive` (versionsneutral) |
| `.github/actions/checkout-release-tag` | kein .NET |

## Skripte

| Skript | .NET-Stellen |
|---|---|
| `scripts/Run-StaticChecks.ps1` | `dotnet restore -r win-x64` (Z. 43), `dotnet format` (Z. 53), `dotnet list package --vulnerable` (Z. 63), `dotnet build -p:TreatWarningsAsErrors=true` (Z. 105); kein harter TFM/Versionsstring – folgt dem installierten SDK |
| `scripts/Run-E2ETests.ps1` | Z. 24 `$tfm = "net10.0-windows10.0.19041.0"`; Z. 13 Kommentar-Pfad; Z. 36 `dotnet build -f $tfm`; Z. 53 `dotnet test -f $tfm` |
| `scripts/iOS-Deployment.ps1` | Z. 94 `$framework = "net10.0-ios"`; Remote-Pfade `.../Caches/maui/PairToMac/SDKs/dotnet/` (Z. 103, VS-2026-Cache) sind versionsneutral |
| `scripts/iOS-Deployment.md` | Doku: net10.0-ios-Pfade (Z. 89, 114, 127, 235, 241), `Microsoft.iOS.Sdk`-Pack-Hinweis Z. 137 |
| `scripts/*.mjs` (add-license-headers, create-update-manifest, release-assets, resolve-release-version) | kein .NET-Bezug |

## CI-/Runner-Implikationen für .NET 11

- Alle `dotnet-version: '10.0.x'` → `'11.0.x'`; `package-ios` `'10.0.1xx'` → passendes
  11er-SDK-Band samt Workload-Manifest-Verfügbarkeit (iOS 26.x/27.x vs. Release-Xcode).
- `actions/setup-dotnet@v6` (SHA-gepinnt) muss 11er-SDKs auflösen können – ggf. Action-Update.
- Neue x64-CPU-Mindestbasis (x86-64-v2): `windows-latest`-Runner und E2E-/Nutzergeräte prüfen.
- `Microsoft.Testing.Platform`-Runner (global.json) + `dotnet test -- ...`-Aufrufe unter
  .NET-11-SDK verifizieren.
- `ci-instructions.md` (Repo-Root) ist eine generische Anleitungsvorlage mit
  `dotnet-version: '10.0.x'`/`net10.0`-Beispielen (Z. 194, 228, 449, 458, 465) – bei
  Doku-Aktualisierung mitziehen.
