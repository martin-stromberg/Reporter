<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Inventar: TargetFrameworks

Alle 5 Projekte in `Reporter.sln` (Zeilen 8–16). Kein `Directory.Build.props`-TFM
(`Directory.Build.props` enthält nur `PackageLicenseExpression` + `DebugReportRecipient`).

## src/Reporter/Reporter.csproj (MAUI-App, `UseMaui`, `SingleProject`)

### Bedingte TFM-Listen (Zeilen 6–13)

Steuerung über `IncludeIosTarget` (Default `true`) und `IncludeAndroidTarget` (Default `false`):

| Zeile | Bedingung | TFMs (IST) |
|---|---|---|
| 8 | Windows && iOS && Android | `net10.0-windows10.0.19041.0;net10.0-ios;net10.0-android` |
| 9 | Windows && iOS && !Android | `net10.0-windows10.0.19041.0;net10.0-ios` |
| 10 | Windows && !iOS && Android | `net10.0-windows10.0.19041.0;net10.0-android` |
| 11 | Windows && !iOS && !Android | `net10.0-windows10.0.19041.0` |
| 12 | !Windows_NT && Android | `net10.0-android` |
| 13 | OSX && !Android | `net10.0-ios` |

→ Bei der Migration alle 6 Zeilen auf `net11.0-*` umstellen (incl. `net11.0-windows10.0.19041.0`).

### SupportedOSPlatformVersion / TargetPlatformMinVersion (Zeilen 45–49)

```xml
<SupportedOSPlatformVersion Condition="... 'ios'">15.0</SupportedOSPlatformVersion>
<SupportedOSPlatformVersion Condition="... 'maccatalyst'">15.0</SupportedOSPlatformVersion>
<SupportedOSPlatformVersion Condition="... 'android'">21.0</SupportedOSPlatformVersion>
<SupportedOSPlatformVersion Condition="... 'windows'">10.0.17763.0</SupportedOSPlatformVersion>
<TargetPlatformMinVersion Condition="... 'windows'">10.0.17763.0</TargetPlatformMinVersion>
```

Hinweise:
- Windows-TFM zielt auf `windows10.0.19041.0`, Min-Support aber `10.0.17763.0` – bewusste Konvention.
- Ob MAUI 11 die plattformseitigen Mindestversionen (iOS 15, Android API 21, Win 17763) anhebt,
  ist im Migrationsplan zu prüfen (MAUI 10 hatte z. B. iOS-Mindestversionen angehoben).
- `MacCatalyst`-Condition vorhanden, aber kein maccatalyst-TFM in den Listen.

### Weitere TFM-bedingte PropertyGroups

- Zeilen 52–56: `Condition="'$(TargetFramework)'=='net10.0-ios'` → `<UseInterpreter>true</UseInterpreter>`
  (EF-Core-/AOT-Begründung im Kommentar; Details in [efcore.md](efcore.md)).
- Zeile 30: `<MauiXamlInflator>SourceGen</MauiXamlInflator>` – die iOS-CI-Action überschreibt
  das mit `XamlC` wegen SourceGen-Bugs der Runner-MAUI-Version
  (`.github/actions/package-ios/action.yml:143-161`); bei MAUI 11 erneut verifizieren.
- Zeile 43: `<WindowsPackageType>None</WindowsPackageType>` (unpackaged).

## src/Reporter.Core/Reporter.Core.csproj

- Zeile 6: `<TargetFramework>net10.0</TargetFramework>` → `net11.0`.

## src/Reporter.Data/Reporter.Data.csproj

- Zeile 6: `<TargetFramework>net10.0</TargetFramework>` → `net11.0`.

## src/Reporter.Tests/Reporter.Tests.csproj

- Zeile 6: `<TargetFramework>net10.0</TargetFramework>` → `net11.0`.

## src/Reporter.E2ETests/Reporter.E2ETests.csproj

- Zeile 6: `<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>` → `net11.0-windows10.0.19041.0`.
- Zeile 24: `<FrameworkReference Include="Microsoft.AspNetCore.App" />` – folgt automatisch der
  TFM-Runtime (kein Version-Attribut; der Runner braucht .NET-11-Runtime + ASP.NET-Core-11-Shared-Framework).

## Harte TFM-Strings außerhalb der .csproj (Querverweis)

| Stelle | Wert |
|---|---|
| `scripts/Run-E2ETests.ps1:24` | `$tfm = "net10.0-windows10.0.19041.0"` (inkl. Doc-Kommentar Z. 13) |
| `scripts/iOS-Deployment.ps1:94` | `$framework = "net10.0-ios"` |
| `src/Reporter.E2ETests/ReporterAppFixture.cs:143` | Konventionspfad `bin/Debug/net10.0-windows10.0.19041.0/win-x64/Reporter.exe` |
| `.github/actions/build-and-package/action.yml:48` | `--framework net10.0-windows10.0.19041.0` |
| `.github/actions/package-android/action.yml:50` | `--framework net10.0-android` |
| `.github/actions/package-ios/action.yml:156` | `--framework net10.0-ios` |
| `.github/actions/package-ios/action.yml:87` | Pack-Glob `Microsoft.iOS.Sdk.net10.0_*` → `net11.0_*` |
