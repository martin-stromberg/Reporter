<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Tests und Test-Ausgangszustand

Die Anforderung (Workload-/Toolchain-Update `Microsoft.Maui.Controls`
11.0.0 → 11.0.10) berührt keine C#-Klassen; fachlich zugeordnete Testklassen
existieren nicht. Dieses Dokument hält den projektweiten Test-Ausgangszustand
vor der Umsetzung fest.

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 24.11.2026, ~04:25–05:30 Uhr (Europe/Berlin, UTC+1;
  `Get-Date -o`: `2026-11-24T04:25:10+01:00`)
- Branch und Commit-ID:
  `task/issue-214-062ec75bd4364b43a40f5e52a0d77b61-maui-workload-update-microsoft`,
  HEAD = `73b1bcb3d5cef2d83a589d338840cbfe9e4098d9`
  (`chore(deps-dev): bump semantic-release from 25.0.9 to 26.0.1 (#209)`)
- Uncommittete Änderungen im getesteten Stand: keine getrackten Änderungen
  (`git status --porcelain` zeigt ausschließlich das untracked
  Feature-Verzeichnis `docs/features/task/issue-214-…/` mit requirement.md,
  todo.md und den hier erzeugten Inventory-Artefakten)
- Testumgebung und Runtime-/SDK-Versionen:
  - Windows `10.0.26300` (lokale Dev-Maschine), Git-Bash/PowerShell
  - `dotnet --version` = `11.0.100` (entspricht `global.json`-Pin
    `11.0.100`/`latestPatch`); installierte SDKs `10.0.404`, `11.0.100`
  - `dotnet workload list`: Workload-Version `11.0.100.1`; `maui-windows
    11.0.0/11.0.100`, `ios 27.2.11039/11.0.100`, `maccatalyst 27.2.11039`,
    `android 37.2.31` — Quelle `SDK 11.0.100, VS 18.11.12402.416`, Typ `Msi`,
    Update-Modus „workload sets"
  - Node.js `v24.15.0`, npm `11.10.0`
  - Test-Runner: Microsoft.Testing.Platform (`global.json` → `test.runner`)
  - Alle dotnet-Läufe mit `IncludeIosTarget=false`,
    `IncludeAndroidTarget=false` (projektübliche Konvention für
    Windows-Lokalläufe, vgl. `scripts/Run-StaticChecks.ps1:34`,
    `scripts/Run-E2ETests.ps1:22`)
  - Gesamte Umgebung: [test-results/environment.txt](test-results/environment.txt)
- Ermittelte Testsuiten und Quellen der Testbefehle:
  1. `src/Reporter.Tests` (xUnit v3, MTP) — CI-Befehl aus `staging-ci.yml`/
     `pr-staging-ci.yml`/`release.yml`:
     `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj -c Release`
     (regular: `-- --filter-not-trait "Category=OsInterface"`; OS-Lane:
     `-- --filter-trait "Category=OsInterface"`)
  2. `src/Reporter.E2ETests` (FlaUI/UIA3, xUnit v3) — Befehl:
     `scripts/Run-E2ETests.ps1` (Debug-Build `net11.0-windows10.0.19041.0`/
     win-x64, startet `Reporter.exe` mit Stub-Feed-Server; benötigt
     interaktive Desktop-Session)
  3. `scripts/*.test.mjs` (36 `node:test`-Tests der Release-Skripte) —
     Befehl: `npm test` (`node --test "scripts/*.test.mjs"`)
  4. `scripts/Run-StaticChecks.ps1` (Restore, Format, Lizenzheader, Security,
     Static-Analysis-Build) — lokales Pendant zum CI-`static-checks`-Gate

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 | `dotnet restore Reporter.sln -r win-x64` (`IncludeIosTarget=false`, `IncludeAndroidTarget=false`) | Repo-Root | 0 | — | — | — | [restore.log](test-results/restore.log) |
| 2 | `dotnet list Reporter.sln package --outdated` | Repo-Root | 0 | — | — | — | [list-package-outdated.log](test-results/list-package-outdated.log) — reproduziert den Befund `Microsoft.Maui.Controls 11.0.0 → 11.0.10` |
| 3 | `dotnet list Reporter.sln package --vulnerable --include-transitive` | Repo-Root | 0 | — | — | — | [list-package-vulnerable.log](test-results/list-package-vulnerable.log) — kein Befund |
| 4 | `dotnet build Reporter.sln -c Release --no-restore -p:TreatWarningsAsErrors=true` | Repo-Root | 0 (0 Warnungen, 0 Fehler) | — | — | — | [dotnet-build-release.log](test-results/dotnet-build-release.log) |
| 5 | `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj -c Release -- --filter-not-trait "Category=OsInterface" --report-trx --report-trx-filename test-results-baseline.trx` | Repo-Root | 0 | 801 | 0 | 0 | [dotnet-test-unit.log](test-results/dotnet-test-unit.log), [TRX](test-results/test-results-unit.trx) |
| 6 | `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj -c Release --no-build -- --filter-trait "Category=OsInterface" --report-trx --report-trx-filename test-results-os-interface-baseline.trx` | Repo-Root | 0 | 4 | 0 | 0 | [dotnet-test-osinterface.log](test-results/dotnet-test-osinterface.log), [TRX](test-results/test-results-osinterface.trx) |
| 7 | `npm test` (`node --test "scripts/*.test.mjs"`) | Repo-Root | 0 | 36 | 0 | 0 | [npm-test.log](test-results/npm-test.log) |
| 8 | `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Run-E2ETests.ps1` | Repo-Root | 2 (`dotnet test`-Exitcode, vom Skript weitergereicht, `Run-E2ETests.ps1:71`) | 21 | 18 | 0 | [e2e-tests.log](test-results/e2e-tests.log) |
| 9 | `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Run-StaticChecks.ps1` | Repo-Root | 1 | — | — | — | [static-checks.log](test-results/static-checks.log) — Format ✅, Lizenzheader ❌ (nur durch die hier neu angelegten, headerlosen Inventory-/Log-Dateien und `todo.md`), Security/Build nicht mehr erreicht (Skript bricht am ersten Fehlschlag ab) |

### Nachgewiesene bestehende Testfehler

Aus den Unit-/OsInterface-/npm-Läufen: **keine** (801 + 4 + 36 Tests
fehlerfrei; Release-Build der ganzen Solution inkl. MAUI-App unter Workload
`11.0.0` mit `TreatWarningsAsErrors` fehlerfrei).

E2E-Lauf (Lauf 8): `gesamt: 39, fehlgeschlagen: 18, erfolgreich: 21,
übersprungen: 0, Dauer: 24m 47s`, Exitcode `2`. Alle 18 Fehler teilen
dasselbe Fehlerbild — `System.TimeoutException: Element not found within
00:00:20: name 'Aktionen'` (FlaUI/UIA3, Warten auf den Aktions-Button der
Feed-Detailseite über `E2EPageHelpers.OpenFeedDetail`; die App startete und
das Hauptfenster wurde gefunden, das Zielelement aber nicht). Das ist
**exakt derselbe Satz von 18 Testfällen mit demselben Fehlerbild**, den die
Bestandsaufnahme zu Issue #204 am 23.11.2026 auf derselben Maschine
dokumentiert hat (`docs/features/task/issue-204-f39e1f6e4f8546a593ba6dcd338496b5-nachricht-von-apple/inventory/tests.md`
Z. 35–77, ebendort 18/39) — die Fehler gelten damit als **bereits
vorhandenes, umgebungsbedingtes Problem** dieser Agenten-Session (fehlende
vollwertige interaktive Desktop-Session), nicht als neuer Defekt.

Fehlgeschlagene Tests (vollständige Liste aus `e2e-tests.log`):

| Test-ID | Suite / Dateipfad | Fehlerbild | Nachweis |
|---------|-------------------|------------|----------|
| `Reporter.E2ETests.SmokeTests.Search_SubscribesResult_PersistsFeed` | `src/Reporter.E2ETests/SmokeTests.cs` | `TimeoutException`: Element `Aktionen` nicht gefunden | [e2e-tests.log](test-results/e2e-tests.log) |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_ChangeCategory_IncludingNone` | `src/Reporter.E2ETests/FeedDetailTests.cs:362` | dto. | dto. |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Delete_ReturnsToList` | `src/Reporter.E2ETests/FeedDetailTests.cs` | dto. | dto. |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Edit_AddKeyword_PersistsAndShowsChip` | `src/Reporter.E2ETests/FeedDetailTests.cs` | dto. | dto. |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Edit_FeedKeywordValidation_ShowsErrors` | `src/Reporter.E2ETests/FeedDetailTests.cs` | dto. | dto. |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Edit_PersistsChanges` | `src/Reporter.E2ETests/FeedDetailTests.cs:469` | dto. | dto. |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Edit_RemoveKeyword_RemovesChipAndRow` | `src/Reporter.E2ETests/FeedDetailTests.cs:688` | dto. | dto. |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_FeedKeyword_FiltersOnlyOwnFeed` | `src/Reporter.E2ETests/FeedDetailTests.cs` | dto. | dto. |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_FeedKeyword_NotInGlobalSettings` | `src/Reporter.E2ETests/FeedDetailTests.cs` | dto. | dto. |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_InfiniteScroll_LoadsNextPage` | `src/Reporter.E2ETests/FeedDetailTests.cs` | dto. | dto. |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Message_ForErrorFeed` | `src/Reporter.E2ETests/FeedDetailTests.cs` | dto. | dto. |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Message_ForWarningFeed` | `src/Reporter.E2ETests/FeedDetailTests.cs` | dto. | dto. |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_MetaInfo_WrapsInsteadOfOverlappingActionsButton` | `src/Reporter.E2ETests/FeedDetailTests.cs` | dto. | dto. |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Navigation_ShowsFeedItems` | `src/Reporter.E2ETests/FeedDetailTests.cs` | dto. | dto. |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_RefreshAction_SyncsFeed` | `src/Reporter.E2ETests/FeedDetailTests.cs:497` | dto. | dto. |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Rename_UpdatesTitle` | `src/Reporter.E2ETests/FeedDetailTests.cs` | dto. | dto. |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Search_FiltersByTag` | `src/Reporter.E2ETests/FeedDetailTests.cs:297` | dto. | dto. |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Search_FiltersItems` | `src/Reporter.E2ETests/FeedDetailTests.cs` | dto. | dto. |

Gemeinsamer Stack (repräsentativ, aus `e2e-tests.log`):

```text
Xunit.MicrosoftTestingPlatform.XunitException: System.TimeoutException : Element not found within 00:00:20: name 'Aktionen'
   um Reporter.E2ETests.UiRetry.WaitForElement(...)                    UiRetry.cs:37
   um Reporter.E2ETests.UiRetry.WaitForElementByName(...)              UiRetry.cs:133
   um Reporter.E2ETests.E2EPageHelpers.OpenFeedDetail(String feedTitle) E2EPageHelpers.cs:293
   um <jeweilige Testmethode>                                          FeedDetailTests.cs / SmokeTests.cs
```

Nach dem Testlauf beendete das Skript `Reporter.exe` (Exit `-1`,
`[E2E] Reporter.exe exited at 04:51:27`); Abschlussmeldung:
`Reporter.E2ETests.dll (net11.0|x64) fehlerhaft mit 18 Fehler(n)
[+21/x18/?0] (24m 46s)`, `Exitcode: 2`.

### Testlücken und Ausführungsprobleme

- **E2E-Suite (`src/Reporter.E2ETests`):** FlaUI/UIA3 setzt eine interaktive
  Desktop-Session mit sichtbarem Fenster voraus (`Run-E2ETests.ps1`
  Kopfkommentar Z. 9–13). In dieser Agenten-Session startet die App, das
  Hauptfenster wird gefunden, UI-Zielelemente (u. a. der `Aktionen`-Button)
  werden jedoch nicht gefunden → `TimeoutException` nach jeweils ~20 s
  Polling plus Retries (Lauf 8 oben: 18/39 Fehlschläge, deckungsgleich mit
  der Issue-#204-Baseline). Die Suite ist in keinem CI-Gate enthalten und
  liefert für eine reine Toolchain-/CI-Anforderung ohnehin kein Signal.
- **`Run-StaticChecks.ps1` (Lauf 9):** schlägt am Lizenzheader-Check fehl —
  ausschließlich wegen der hier neu erzeugten Nachweis-Dateien
  (`*.log`/`*.txt` unter `inventory/test-results/`) und des headerlosen
  `todo.md`; der Check verlangt Lizenzheader auch in `.log`/`.txt`
  (`scripts/add-license-headers.mjs` Z. 34). Kein Fehler im
  Produktiv-/Testbestand. Format-Check bestanden; Security-Scan und
  Static-Analysis-Build erreicht das Skript nach dem Abbruch nicht — diese
  beiden Checks sind durch Lauf 3 (Security, kein Befund) und Lauf 4
  (Release-Build mit `TreatWarningsAsErrors`, fehlerfrei) separat belegt.
- **CI-Artefakt-Tests:** Für die eigentlich betroffenen Artefakte
  (Workflows, Composite Actions, Workload-Installation) existiert keine
  automatisierte Testsuite — Verifikation solcher Änderungen geschieht
  projektüblich über echte CI-Läufe. Der `package-ios`-Job kann wegen
  `version.changed`- + `IOS_SIGNING_ENABLED`-Gates nur auf `staging`/`main`
  ausgeübt werden (`pr-staging-ci.yml` hat keinen iOS-Job).
- **iOS-TFM-Build lokal nicht ausgeführt:** Lokalläufe folgen der
  Projektkonvention `IncludeIosTarget=false` (iOS-Build auf Windows ohne
  Mac-Pairing nicht sinnvoll; `net11.0-ios` wird nur auf dem macOS-Runner
  gebaut). Der iOS-Workload-Stand `27.2.11039` ist lokal installiert, wird
  aber durch die Windows-Verifikation nicht ausgeübt.

## Testklassen

Für die Anforderung (reine Toolchain-/CI-Maßnahme, kein App-Code) gibt es
**keine fachlich zugeordneten Testklassen**. Übergreifend relevante Suites:

### `Reporter.Tests` (Unit-Tests, 801 regular + 4 OsInterface)

- Prüfen `Reporter.Core`/`Reporter.Data` — keine Berührung mit der
  MAUI-Workload-Kette; laufen ohne MAUI-Workload (`net11.0`-TFM).
- Hilfsattribute: `OsInterfaceAttribute`/`FlakyAttribute`/`TestCategories`
  markieren OS-abhängige bzw. flaky Tests; CI splittet in zwei Lanes
  (`--filter-trait`/`--filter-not-trait "Category=OsInterface"`).
- `TestClassificationConventionTests` erzwingt die Trait-Konventionen.

### `Reporter.E2ETests` (FlaUI-Desktop-Suite, ~39 Tests)

- `SmokeTests` (5), `FeedDetailTests` (17), `UnreadPacketTests` (3),
  `ArticleImageTests` (2), `ArticleLinkTests` (1), `ArticleTagsTests` (2),
  `DemoSeedTests` (1), `E2EProcessGuardTests` (8).
- Hilfscode: `ReporterAppFixture` (App-Start mit Stub-Server + Temp-DB,
  UIA3-Attach), `StubFeedServer`, `UiRetry`, `E2EProcessGuard`,
  `E2EPageHelpers`, `FeedDbAssertions`, `FlakyAttribute`,
  `OsInterfaceAttribute`, `TestCategories`, `E2ETestCollection`.
- Enthält keinen MAUI-Versions-Assertion; indirektes Signal nur über
  „App baut und startet unter dem installierten Workload".

### `scripts/*.test.mjs` (36 `node:test`-Tests)

- `resolve-release-version.test.mjs`, `release-assets.test.mjs`,
  `create-update-manifest.test.mjs` — Release-Logik; berührungslos zum
  Workload-Update, relevant nur für den `prerelease`-Job als Ganzes.
