<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: CI-Lauf-Nachweise (Ursache ITMS-90725 und aktueller Zustand)

Nachweise via `gh run view`/`gh run view --log` (GitHub-CLI, Repo
`martin-stromberg/Reporter`). Vollständige Job-Logs liegen als
[ci-run-44460132570-package-ios.log](ci-run-44460132570-package-ios.log)
und [ci-run-45117817038-package-ios.log](ci-run-45117817038-package-ios.log)
vor (Secrets sind von GitHub als `***` maskiert).

## Run 44460132570 — `staging-ci` auf `46e47b2` (Tag `v0.8.0-rc.1`) — Erfolg, produzierte Build 331

| Feld | Wert |
|------|------|
| headSha | `46e47b23241fd0ee011a8776ffc1873c785a6dc2` („Doku: .NET-11-Migrationsplan und Entscheidung (Closes #180) (#201)") |
| createdAt | 2026-11-19T04:42:16Z |
| Runner-Image | `macos-26-arm64`, Image-Version `20261112.0504.1`, macOS 26.7.2 (Job-Log Zeilen 17–19) |
| Job `package-ios` | conclusion `success`, inkl. TestFlight-Upload und Artefakt-Upload |
| Effektiver Workload-Pin | `ios-workload-version: 26.5.10318` (Job-Log Zeile ~227) |
| SDK-Band | `10.0.1xx` (`global.json`-Pin `10.0.100` im Log, Zeile ~256) — Stand **vor** der .NET-11-Migration |
| Buildnummer | `BUILD_NUMBER = git rev-list --count HEAD + 1 - 1` = **331** (zusätzlich verifiziert: `git rev-list --count v0.8.0-rc.1` = 331) |
| Upload | `iTMSTransporter -m upload` → „Upload succeeded via transport: HTTP", „1 package was uploaded successfully" (Job-Log Zeilen 784–799); App-Store-Connect buildUpload `9cae7428-ac7d-49a0-9d22-c0db0aa7400e` |

→ Dieser Lauf lieferte das von Apple mit ITMS-90725 beanstandete Binary
(„Version 0.8.0, Build 331, built with the iOS 26.5 SDK"): Pack
`Microsoft.iOS.Sdk 26.5.10318` auf einem `macos-26`-Image, dessen Xcode
`iphoneos26.5` liefert. `ApplicationDisplayVersion` wurde per
`${DISPLAY_VERSION%%-*}` auf `0.8.0` gekürzt — ein RC-Tag erscheint in
App Store Connect als Version `0.8.0`.

## Run 45117817038 — `staging-ci` auf `a412efa` (Merge .NET-11-Migration, aktueller `staging`-Stand) — `package-ios` fehlgeschlagen

| Feld | Wert |
|------|------|
| headSha | `a412efa4d390df9f44fe6206a0e0299ed79c5a6e` („feat: Migration auf .NET 11 / MAUI 11 / EF Core 11 (Closes #202) (#208)") |
| createdAt | 2026-11-23T05:26:47Z |
| Runner-Image | `macos-26-arm64`, Image-Version `20261117.0516.1` (Job-Log Zeilen 17–18) |
| Jobs | `detect-backmerge`, `static checks`, `build & test`, `os-interface tests`, `version`, `package-windows`: success · **`package-ios`: failure** · **`package-android`: failure** · `prerelease`: skipped |
| Effektiver Workload-Pin | `ios-workload-version: 27.2.11039` (wird korrekt installiert: `Installing pack Microsoft.iOS.Sdk.net11.0_27.2 version 27.2.11039`) |
| Fehlerstelle | `Publish (ios)`: `dotnet publish … --framework net11.0-ios` bricht nach ~8 s ab |

Exakte Fehlermeldung (Job-Log Zeile 433):

```text
/Users/runner/.dotnet/packs/Microsoft.iOS.Sdk.net11.0_27.2/27.2.11039/targets/Xamarin.Shared.Sdk.targets(3190,3):
error : This version of .NET for iOS (27.2.11039) requires Xcode 27.2.
The current version of Xcode is 26.6. Either install Xcode 27.2, or use a
different version of .NET for iOS. See https://aka.ms/xcode-requirement
for more information.
```

→ Der Pack-Pin `27.2.11039` ist installierbar und korrekt aufgelöst, aber das
Pack erzwingt **exakt Xcode 27.2** — auf `macos-latest` (= `macos-26-arm64`,
Default-Xcode 26.6, kein Xcode 27 installiert) scheitert der Build. Es wird
**keine** `.ipa` erzeugt; eine erneute ITMS-90725-Warnung kann so zwar nicht
entstehen, aber auch kein auslieferbares Binary.

## Nebenbefund: `package-android` im selben Lauf

`package-android` schlug ebenfalls (unabhängige Ursache, außerhalb dieser
Anforderung):

```text
error NETSDK1094: Unable to optimize assemblies for performance: a valid
runtime package was not found. Either set the PublishReadyToRun property
to false, or use a supported runtime identifier when publishing.
[src/Reporter/Reporter.csproj::TargetFramework=net11.0-android]
```

Betrifft den Android-Publish-Pfad — für diese Anforderung nur als
Pipeline-Nebenbefund relevant.

## Vorherige Läufe (Kontext)

`gh run list --workflow staging-ci.yml`: Läufe vor der Migration (u. a.
`53bd434`/`v0.8.0-rc.3`, `82c9bbd`/`v0.8.0-rc.2`, `46e47b2`/`v0.8.0-rc.1`)
alle `success` — der iOS-Fehlschlag trat erstmals mit dem Migrations-Stand
auf, weil erst dieser den Pack-Pin `27.2.11039` enthält.

`gh run list --workflow release.yml`: letzter `main`-Release-Lauf vom
17.11.2026 (`5492d71`, success) — ein `main`-Release aus dem .NET-11-Stand
steht noch aus; der dortige `package-ios` läuft über dieselbe Action und
dasselbe `macos-latest`-Label und würde denselben Xcode-Fehler erzeugen.
