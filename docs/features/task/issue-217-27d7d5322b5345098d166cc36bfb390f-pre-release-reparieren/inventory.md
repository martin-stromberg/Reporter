<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Issue #217 „Pre-Release reparieren"

Analysiert wurden der `Pre-Release`-Workflow (`staging-ci.yml`) mit seinen
Composite Actions sowie die iOS-/MacCatalyst-Plattformdateien, bezogen auf die
beiden dokumentierten CI-Fehler `error MT7199` (iOS) und `error NETSDK1094`
(Android) — siehe `requirement.md`.

## Zusammenfassung

- **iOS MT7199 bestätigt:** `src/Reporter/Platforms/iOS/Info.plist` enthält kein
  `UIApplicationSceneManifest`, und es existiert kein `SceneDelegate.cs`
  (repo-weit kein Treffer für `SceneDelegate`/`MauiUISceneDelegate`). Der
  `AppDelegate` (`MauiUIApplicationDelegate`) kapselt drei verankerte
  Initialisierungen in `FinishedLaunching`: `UNUserNotificationCenter`-Delegate,
  verzögerte `BGTaskScheduler`-Registrierung und `CreateMauiApp`. Kein
  `ConfigureLifecycleEvents` in `MauiProgram`.
- **MacCatalyst ebenfalls ohne Scene-Manifest** (`Platforms/MacCatalyst/Info.plist`,
  kein `SceneDelegate.cs`); `net11.0-maccatalyst` ist nicht in `TargetFrameworks`
  — kein Job baut die Plattform.
- **Android NETSDK1094 — Ursache verifiziert (offene Frage 1 beantwortet):**
  `PublishReadyToRun` wird **nicht** im Repo gesetzt. Das installierte
  Android-Workload-Pack `Microsoft.Android.Sdk.Windows` 37.2.31 (.NET-11-Generation)
  defaultet `_AndroidRuntime` auf **`CoreCLR`**
  (`targets/Microsoft.Android.Sdk.DefaultProperties.targets` Z. 33–35); dessen
  `Microsoft.Android.Sdk.CoreCLR.targets` Z. 19 setzt `PublishReadyToRun=true`
  für `Configuration=Release`/Publish. `dotnet restore Reporter.sln` läuft RID-los,
  `dotnet publish` mit `--no-restore` fordert dann das R2R-Runtime-Pack →
  NETSDK1094. Unter .NET 10 war der MonoVM-Default ohne R2R aktiv — die
  Migration `9a636b0` (TFM `net10.0-android`→`net11.0-android`) hat das
  Fehlerbild erzeugt. Präzedenzfall im Repo: Commit `ee41281` löste denselben
  Fehler im Windows-Pfad bereits mit `-p:PublishReadyToRun=false`.
- **Workflow-Struktur:** `staging-ci.yml` braucht selbst voraussichtlich keine
  Änderung; `package-ios` läuft nachweislich (MT7199-Fehlschlag ⇒
  `vars.IOS_SIGNING_ENABLED == 'true'`, Runner `xcode-27`, Xcode-27.2-Pin,
  Workload-Pin `ios-workload-version=27.2.11039`). `release.yml` nutzt
  identische Actions → implizit mitbetroffen.
- **Keine Datenmodell-, Enum- oder Interface-Änderungen** betroffen (entsprechende
  Detaildateien entfallen).

**Test-Ausgangszustand:** Unit-Tests 801/801 grün (Haupt-Lane), OsInterface 4/4
grün, Node-Skript-Tests 36/36 grün. E2E-Suite (manuell, nicht in CI): 21/39
grün, **18 deterministische Fehlschläge** (17× `TimeoutException name
'Aktionen'` in `OpenFeedDetail`, 1× `name 'Ja'` im Smoke-Test) — als
preexisting dokumentiert, Details und Nachweise in
[inventory/tests.md](inventory/tests.md).

## Details

- [Plattform-Dateien (Info.plist, csproj-Properties)](inventory/platform.md)
- [Logik (AppDelegate, CI-Actions, PublishReadyToRun-Herkunft, Release-Skripte)](inventory/logic.md)
- [CI/Workflows (Job-Graph, Gating, Test-Lanes)](inventory/ci.md)
- [Tests (Ausgangszustand, Läufe, Fehlschläge, Nachweise)](inventory/tests.md)
