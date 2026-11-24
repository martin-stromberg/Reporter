<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Toolchain und Workload-Stand

Ist-Zustand der für die Anforderung relevanten Toolchain-Artefakte
(`global.json`, `Reporter.csproj`, installierte Workloads, Dev-Umgebung).

## `global.json`

Datei: `global.json` (Repo-Root)

| Schlüssel | Wert | Befund |
|-----------|------|--------|
| `sdk.version` | `11.0.100` | SDK-Pin aufs Band 11.0.1xx |
| `sdk.rollForward` | `latestPatch` | höhere Patch-Stände des Bands erlaubt |
| `sdk.workloadVersion` | — | **nicht vorhanden** — es gibt kein repo-seitiges Workload-Set-Pinning |
| `test.runner` | `Microsoft.Testing.Platform` | MTP-Runner für `dotnet test` |

Es existiert **kein** `workloadVersion`-Eintrag und kein sonstiger Mechanismus,
der den `maui-windows`-Workload-Stand repo-weit festlegt. Die aufgelöste
`Microsoft.Maui.Controls`-Version folgt damit allein dem jeweils installierten
Workload der Umgebung (Floating).

## `src/Reporter/Reporter.csproj`

Datei: `src/Reporter/Reporter.csproj`

| Stelle | Inhalt | Befund |
|--------|--------|--------|
| Zeile 76 | `<PackageReference Include="Microsoft.Maui.Controls" Version="$(MauiVersion)" />` | Version kommt aus dem installierten Workload, nicht aus dem Repo |
| Zeile 77 | `<PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="11.0.0" />` | fest gepinntes separates Paket (eigener Befund, kein Workload) |
| Zeile 78 | `<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />` | reguläres Paket |
| Zeile 6 | `IncludeIosTarget` Default `true` | lokal standardmäßig iOS-TFM aktiv; Skripte setzen `false` |
| Zeile 7 | `IncludeAndroidTarget` Default `false` | Android lokal/CI-Windows deaktiviert |
| Zeilen 8–13 | bedingte `TargetFrameworks` | `net11.0-windows10.0.19041.0`, `net11.0-ios`, `net11.0-android` je nach Flags/OS |
| Zeile 30 | `<MauiXamlInflator>SourceGen</MauiXamlInflator>` | wird in `package-ios` mit `XamlC` überschrieben (SourceGen-Bug-Workaround) |

Verifiziert via `obj/project.assets.json` nach Restore: aufgelöst ist
`Microsoft.Maui.Controls/11.0.0` (samt `Microsoft.Maui.Controls.Core`,
`.Xaml`, `.Build.Tasks`, `Microsoft.Maui.Core`, `Microsoft.Maui.Essentials`,
`Microsoft.Maui.Graphics`, `Microsoft.Maui.Resizetizer` — alle `11.0.0`).

## Installierte Workloads (diese Dev-Maschine)

Nachweis: [test-results/environment.txt](test-results/environment.txt)
(`dotnet workload list`, 24.11.2026 ~04:25 UTC+1)

| Workload-ID | Manifestversion | Installationsquelle | Bezug zur Anforderung |
|-------------|-----------------|---------------------|------------------------|
| `maui-windows` | `11.0.0/11.0.100` | SDK 11.0.100, VS 18.11.12402.416 | **Befund** — `11.0.0` erzeugt die outdated-Meldung `→ 11.0.10` |
| `ios` | `27.2.11039/11.0.100` | SDK 11.0.100, VS 18.11.12402.416 | entspricht bereits dem CI-Pin aus `package-ios/action.yml` (Default `ios-workload-version`) |
| `maccatalyst` | `27.2.11039/11.0.100` | SDK 11.0.100, VS 18.11.12402.416 | gleicher Stand wie `ios` |
| `android` | `37.2.31/11.0.100` | SDK 11.0.100, VS 18.11.12402.416 | außerhalb des Scope (`IncludeAndroidTarget` standardmäßig `false`) |

Zusätzliche Umgebungsfakten:

- Workload-Gesamtversion: `11.0.100.1`; Installationstyp aller Workloads: `Msi`.
- `dotnet --info` meldet: „Für die Verwendung von **workload sets** bei der
  Installation neuer Manifeste konfiguriert." — der Update-Modus der Maschine
  steht auf Workload-Sets (relevant für die offene Frage
  `sdk.workloadVersion`-Pinning).
- Installationsquelle enthält `VS 18.11.12402.416` — Workloads sind
  VS-verwaltet (offene Frage 3 der Anforderung ist hier mit „ja" beantwortet:
  ein `dotnet workload update` kann sich mit der VS-Verwaltung überschneiden).
- Installierte SDKs: `10.0.404`, `11.0.100` (`C:\Program Files\dotnet\sdk`);
  `dotnet --version` im Repo = `11.0.100` (greift `global.json`).
- OS: Windows `10.0.26300`, RID `win-x64`; Git-Bash/PowerShell als Shells.

## Befund-Reproduktion

Nachweis: [test-results/list-package-outdated.log](test-results/list-package-outdated.log)

`dotnet list Reporter.sln package --outdated` (mit `IncludeIosTarget=false`,
`IncludeAndroidTarget=false`) meldet exakt den Issue-Befund:

```text
[net11.0-windows10.0.19041.0]:
   Paket oberster Ebene           Angefordert   Aufgelöst   Neueste
   > Microsoft.Maui.Controls      11.0.0        11.0.0      11.0.10
```

Alle anderen Projekte (`Reporter.Core`, `Reporter.Data`, `Reporter.Tests`,
`Reporter.E2ETests`) ohne Update-Befund. `dotnet list package --vulnerable
--include-transitive` ohne Befund
([list-package-vulnerable.log](test-results/list-package-vulnerable.log)) —
bestätigt „kein Security-Fix".

## Bestehender Pin-/Abgleich-Mechanismus

- **Repo-seitig für `maui-windows`:** nichts. Keine `workloadVersion` in
  `global.json`, kein `--version`-Argument, kein Rollback-File in den
  Windows-/Android-CI-Pfaden — `dotnet workload restore Reporter.sln` zieht
  überall das jeweils neueste Manifest des SDK-Bands 11.0.1xx.
- **Repo-seitig für `ios` (nur CI-Package-Job):** vollständiger Pin über
  Rollback-File in `.github/actions/package-ios/action.yml` (Input
  `ios-workload-version`, Default `27.2.11039`, gekoppelt an `xcode-path`
  `/Applications/Xcode_27.2.app` und Runner `xcode-27`) — Vorarbeit aus
  Issue #204. Details in [ci.md](ci.md).
- **Dokumentiert, dass Dependabot die `$(MauiVersion)`-Zeile nie anheben
  kann:** `docs/help/release-management/wiederkehrende-reviews.md` (Abschnitt
  „Dependabot", um Z. 38) — die wöchentliche Issue-Review
  (`review-nuget.yml`, cron `0 6 * * 1`) ist der einzige Mechanismus, der den
  Befund aufdeckt; er wiederholt sich wöchentlich bis zur Umsetzung.
- **Dev-Doku zum Workload-Abgleich:** `docs/help/entwicklung/` enthält nur
  `dotnet-11-migration.md` (Migrations-Plan aus Issue #202) und
  `git-hooks.md` — ein allgemeiner Hinweis „wie werden Workload-Stände
  abgeglichen/gepinnt" existiert nicht.
