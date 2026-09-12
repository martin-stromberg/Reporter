# Detail: Projekt- und Repository-Konfiguration

## `src/Reporter/Reporter.csproj` (MAUI-App)

| Element | Zeilen | Ist-Zustand |
|---------|--------|-------------|
| `IncludeIosTarget` | 4 | MSBuild-Schalter, Default `true` — steuert, ob auf Windows zusätzlich `net10.0-ios` gebaut wird; alle CI-Workflows und `Run-StaticChecks.ps1` setzen ihn explizit auf `false` |
| `TargetFrameworks` | 5–7 | Windows + `IncludeIosTarget=true` → `net10.0-windows10.0.19041.0;net10.0-ios`; Windows ohne → nur `net10.0-windows10.0.19041.0`; macOS → `net10.0-ios`. **`net10.0-android` und `net10.0-maccatalyst` sind nicht konfiguriert** |
| `SupportedOSPlatformVersion` | 39–43 | Einträge für `ios` (15.0), `maccatalyst` (15.0) und `android` (21.0) vorhanden, obwohl Android/MacCatalyst keine TFMs sind |
| `WindowsPackageType` | 37 | `None` (unpackaged) — ein `.msix`-Artefakt ist damit ohne Konfigurationsänderung nicht erzeugbar; `Platforms/Windows/Package.appxmanifest` existiert trotzdem |
| Versionen | 33–34 | `ApplicationDisplayVersion` `1.0`, `ApplicationVersion` `1`; der Publish-Step in `build-and-package` überschreibt die Version per `-p:Version=<release-version>` |
| Weiteres | 16–24, 30 | `OutputType Exe`, `UseMaui`, `SingleProject`, `ApplicationId com.companyname.reporter`, `MauiXamlInflator SourceGen`, `WarningsAsErrors CS1591` |
| Pakete/Referenzen | 64–73 | `Microsoft.Maui.Controls` (`$(MauiVersion)`), `Microsoft.Extensions.Logging.Debug 10.0.0`, `CommunityToolkit.Mvvm 8.4.0`; Projektreferenzen auf `Reporter.Core` und `Reporter.Data` |

**Kontext zu Plattformen:** Unter `src/Reporter/Platforms/` existieren native Plattformordner für `Android` (`MainActivity.cs`, `MainApplication.cs`, `AndroidManifest.xml`, `colors.xml`), `iOS` (`AppDelegate.cs`, `NotificationDelegate.cs`, `Info.plist`, `PrivacyInfo.xcprivacy`), `MacCatalyst` (`AppDelegate.cs`, `Entitlements.plist`, `Info.plist`, `Program.cs`) und `Windows` — die Projektstruktur ist für Android/MacCatalyst vorbereitet, die TFMs fehlen jedoch im `csproj`.

## `Reporter.sln`

Enthält exakt vier Projekte (Zeilen 8–15): `Reporter` (`src/Reporter/Reporter.csproj`, MAUI-App), `Reporter.Core` (`src/Reporter.Core/`, `net10.0`), `Reporter.Data` (`src/Reporter.Data/`, `net10.0`, EF Core Sqlite 10.0.12 + Design), `Reporter.Tests` (`src/Reporter.Tests/`, `net10.0`, xUnit). Alle Projekte sind in den Konfigurationen `Debug`/`Release` × `Any CPU`/`x64`/`x86` gemappt.

## `src/Reporter.Core/Reporter.Core.csproj` und `src/Reporter.Data/Reporter.Data.csproj`

Beide `net10.0`, keine MAUI-Abhängigkeit — `Reporter.Tests` kann daher ohne MAUI-Workload gebaut und getestet werden (relevant für das Release-Gate in `release.yml`, das keinen Workload installiert).

## Repository- und GitHub-Zustand (außerhalb des Codes, soweit lokal prüfbar)

- **Remote:** `origin` = `https://github.com/martin-stromberg/Reporter`; `HEAD` → `main`.
- **Branches:** `main` und `staging` existieren lokal und remote; der Task-Branch `task/issue-29-...` entspricht exakt `origin/staging` (kein Diff). `staging` liegt inhaltlich vor `main` (Feature-Stände von Issue #26/#27 u. a.).
- **Tags:** 10 lokal sichtbare Tags: `v0.0.1` (stabil), `v0.0.2-rc.1`, `v0.1.0-rc.1`…`v0.1.0-rc.7`, `v1.0.0-rc.1` — RC-Namenskonvention `vX.Y.Z-rc.N` wurde bereits verwendet; RC-Nummerierung pro Version beginnend bei 1.
- **Workflow-Stand auf `main`:** Alle `.github/workflows/*` existieren auf `origin/main` mit **aktiven** `on:`-Triggern; das Auskommentieren ist ein reiner `staging`-Stand. `workflow_run`- und `push → main`-Trigger sind damit auf dem Default-Branch bereits scharf (Cold-Start aus Vorlagen-Abschnitt 11.5 teilweise überstanden); ihre Dateiversionen entsprechen aber dem aktuellen Stand (ohne `detect-backmerge` usw.).
- **Branch-Protection / Labels:** nicht aus dem Repository-Inhalt ableitbar — erfordert `gh api repos/.../branches/<branch>/protection` bzw. `gh label list`. Die Workflows legen `automated-promotion` (`0E8A16`) und `automated-backmerge` (`1D76DB`) per `gh label create --force` selbst an (Lazy-Init).
- **Weitere Dateien:** kein `.editorconfig`, kein `global.json`, kein `Directory.Build.props`, kein `NuGet.config` im Repo-Root; `dotnet format` läuft mit SDK-Defaults.
