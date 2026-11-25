<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests — Bestandsaufnahme (Issue #217)

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-11-25, 08:24–09:16 Uhr +01:00 (CET)
- Branch und Commit-ID: `task/issue-217-27d7d5322b5345098d166cc36bfb390f-pre-release-reparieren` @ `d682343408e1f4786defd54aa701713f94540fbe` („fix(ci): Node-Pins auf >=26.13 <27 anheben (semantic-release@26) (#216)")
- Uncommittete Änderungen im getesteten Stand: nur untracked Dateien unter `docs/features/task/issue-217-27d7d5322b5345098d166cc36bfb390f-pre-release-reparieren/` (`requirement.md`, diese Inventory-Nachweise) — keine Quellcode-Änderungen.
- Testumgebung und Runtime-/SDK-Versionen: Windows 11 (interaktive Konsolensession 1), .NET SDK `11.0.100` (via `global.json` gepinnt: `11.0.100`/`latestPatch`; zusätzlich `10.0.404` installiert), Test-Runner `Microsoft.Testing.Platform` (`global.json` → `test.runner`). Installierte Workloads: `android 37.2.31/11.0.100`, `ios 27.2.11039/11.0.100`, `maccatalyst 27.2.11039/11.0.100`, `maui-windows 11.0.0/11.0.100` (VS 18.11.12402.416). Node `v24.15.0`, npm `11.10.0`.
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `src/Reporter.Tests` (xunit.v3, `net11.0`) — CI-Befehle aus `.github/workflows/staging-ci.yml` Zeilen 121–127 (Haupt-Lane `--filter-not-trait "Category=OsInterface"`) und Zeilen 201–204 (OsInterface-Lane `--filter-trait "Category=OsInterface"`, `continue-on-error`).
  - Node-Skript-Tests `scripts/*.test.mjs` — `npm test` (`package.json` → `node --test "scripts/*.test.mjs"`); läuft **nicht** in CI.
  - `src/Reporter.E2ETests` (xunit.v3 + FlaUI.UIA3, `net11.0-windows10.0.19041.0`) — nur manuell via `scripts/Run-E2ETests.ps1` (baut die App Debug/win-x64 und startet sie unter UIA3); läuft in **keinem** CI-Workflow.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Restore (Vorbereitung) | `dotnet restore Reporter.sln -r win-x64` (env `IncludeIosTarget=false`, `IncludeAndroidTarget=false`) | Repo-Root | 0 | — | — | — | Konsolenausgabe (alle 5 Projekte wiederhergestellt) |
| Unit-Tests (Haupt-Lane) | `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj --configuration Release -- --filter-not-trait "Category=OsInterface" --report-trx --report-trx-filename test-results-inventory.trx` | Repo-Root | 0 | 801 | 0 | 0 | [TRX](test-results/test-results-inventory.trx), [Konsole](test-results/unit-run-console.txt) |
| Unit-Tests (OsInterface-Lane) | `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build -- --filter-trait "Category=OsInterface" --report-trx --report-trx-filename test-results-os-interface-inventory.trx` | Repo-Root | 0 | 4 | 0 | 0 | [TRX](test-results/test-results-os-interface-inventory.trx), [Konsole](test-results/os-interface-run-console.txt) |
| Node-Skript-Tests | `npm test` | Repo-Root | 0 | 36 | 0 | 0 | [Konsole](test-results/node-test-output.txt) |
| E2E (Lauf 1, via Skript) | `pwsh scripts/Run-E2ETests.ps1` (baut `Reporter` Debug/`net11.0-windows10.0.19041.0`/`win-x64`, dann `dotnet test src/Reporter.E2ETests` Debug) | Repo-Root | 2 | 21 | 18 | 0 | Konsolen-Tail (nur letzte 3 Fehler sichtbar; Gesamtsumme dokumentiert: 39 gesamt, 25m 05s) |
| E2E (Lauf 2, Wiederholung) | `dotnet test --project src/Reporter.E2ETests/Reporter.E2ETests.csproj -c Debug -f net11.0-windows10.0.19041.0 --no-build` (env `REPORTER_APP_PATH=src/Reporter/bin/Debug/net11.0-windows10.0.19041.0/win-x64/Reporter.exe`) | Repo-Root | 2 | 21 | 18 | 0 | [Konsole vollständig](test-results/e2e-run-console.txt) |

Hinweis zu den E2E-Läufen: Lauf 2 reproduziert Lauf 1 exakt (gleiche 18 Fehler, gleiche
Fehlerbilder) — die Fehlschläge sind **deterministisch**, nicht flaky. Ein TRX-Report ist
für `Reporter.E2ETests` nicht möglich: dem Projekt fehlt das Paket
`Microsoft.Testing.Extensions.TrxReport` (`--report-trx` → „Unbekannte Option", Exit 5).

### Nachgewiesene bestehende Testfehler

Alle 18 Fehlschläge stammen aus `src/Reporter.E2ETests` (manueller FlaUI-Suite, nicht in
CI). Fehlerbild bei 17 Tests identisch: `System.TimeoutException : Element not found
within 00:00:20: name 'Aktionen'` in `E2EPageHelpers.OpenFeedDetail`
(`src/Reporter.E2ETests/E2EPageHelpers.cs:293`, via `UiRetry.WaitForElementByName`).

| Test-ID | Suite / Dateipfad | Fehlerbild / Fehlermeldung | Lauf und Nachweis |
|---------|------------------|---------------------------|-------------------|
| `Reporter.E2ETests.SmokeTests.Search_SubscribesResult_PersistsFeed` | `src/Reporter.E2ETests/SmokeTests.cs:139` | `TimeoutException: Element not found within 00:00:20: name 'Ja'` (`WaitForElementInScopeByName`, Bestätigungsdialog) | Lauf 2, [Konsole](test-results/e2e-run-console.txt) |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_RefreshAction_SyncsFeed` | `src/Reporter.E2ETests/FeedDetailTests.cs` | `TimeoutException: name 'Aktionen'` in `OpenFeedDetail` | Lauf 1+2, Konsole |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_InfiniteScroll_LoadsNextPage` | `src/Reporter.E2ETests/FeedDetailTests.cs` | `TimeoutException: name 'Aktionen'` in `OpenFeedDetail` | Lauf 1+2, Konsole |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Edit_AddKeyword_PersistsAndShowsChip` | `src/Reporter.E2ETests/FeedDetailTests.cs:656` | `TimeoutException: name 'Aktionen'` in `OpenFeedDetail` | Lauf 1+2, Konsole |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_MetaInfo_WrapsInsteadOfOverlappingActionsButton` | `src/Reporter.E2ETests/FeedDetailTests.cs:406` | `TimeoutException: name 'Aktionen'` in `OpenFeedDetail` | Lauf 1+2, Konsole |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Message_ForWarningFeed` | `src/Reporter.E2ETests/FeedDetailTests.cs:610` | `TimeoutException: name 'Aktionen'` in `OpenFeedDetail` | Lauf 1+2, Konsole |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Message_ForErrorFeed` | `src/Reporter.E2ETests/FeedDetailTests.cs` | `TimeoutException: name 'Aktionen'` in `OpenFeedDetail` | Lauf 1+2, Konsole |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Delete_ReturnsToList` | `src/Reporter.E2ETests/FeedDetailTests.cs` | `TimeoutException: name 'Aktionen'` in `OpenFeedDetail` | Lauf 1+2, Konsole |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Edit_RemoveKeyword_RemovesChipAndRow` | `src/Reporter.E2ETests/FeedDetailTests.cs` | `TimeoutException: name 'Aktionen'` in `OpenFeedDetail` | Lauf 1+2, Konsole |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_FeedKeyword_NotInGlobalSettings` | `src/Reporter.E2ETests/FeedDetailTests.cs` | `TimeoutException: name 'Aktionen'` in `OpenFeedDetail` | Lauf 1+2, Konsole |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Search_FiltersByTag` | `src/Reporter.E2ETests/FeedDetailTests.cs` | `TimeoutException: name 'Aktionen'` in `OpenFeedDetail` | Lauf 1+2, Konsole |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Rename_UpdatesTitle` | `src/Reporter.E2ETests/FeedDetailTests.cs` | `TimeoutException: name 'Aktionen'` in `OpenFeedDetail` | Lauf 1+2, Konsole |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_FeedKeyword_FiltersOnlyOwnFeed` | `src/Reporter.E2ETests/FeedDetailTests.cs` | `TimeoutException: name 'Aktionen'` in `OpenFeedDetail` | Lauf 1+2, Konsole |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_ChangeCategory_IncludingNone` | `src/Reporter.E2ETests/FeedDetailTests.cs` | `TimeoutException: name 'Aktionen'` in `OpenFeedDetail` | Lauf 1+2, Konsole |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Search_FiltersItems` | `src/Reporter.E2ETests/FeedDetailTests.cs` | `TimeoutException: name 'Aktionen'` in `OpenFeedDetail` | Lauf 1+2, Konsole |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Navigation_ShowsFeedItems` | `src/Reporter.E2ETests/FeedDetailTests.cs:250` | `TimeoutException: name 'Aktionen'` in `OpenFeedDetail` | Lauf 1+2, Konsole |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Edit_PersistsChanges` | `src/Reporter.E2ETests/FeedDetailTests.cs:469` | `TimeoutException: name 'Aktionen'` in `OpenFeedDetail` | Lauf 1+2, Konsole |
| `Reporter.E2ETests.FeedDetailTests.FeedDetail_Edit_FeedKeywordValidation_ShowsErrors` | `src/Reporter.E2ETests/FeedDetailTests.cs:766` | `TimeoutException: name 'Aktionen'` in `OpenFeedDetail` | Lauf 1+2, Konsole |

Einordnung: `Reporter.E2ETests` läuft in keinem CI-Workflow — die Fehlschläge blockieren
weder `staging-ci` noch `release`. Sie stehen in keinem Bezug zum betroffenen Bereich
(iOS-Scene-Lifecycle / Android-Packaging); vermutlich ist das Windows-UI-Element
„Aktionen" im Feed-Detail umbenannt/verschoben worden, ohne die E2E-Selektoren
anzupassen. Als „preexisting" dokumentiert.

### Testlücken und Ausführungsprobleme

- **iOS-/Android-Packaging nicht lokal ausführbar:** Die fehlschlagenden Pfade
  (`package-ios` auf `xcode-27`/macOS mit Codesigning, `package-android` Publish) sind
  CI-seitig; der macOS-Pfad lässt sich unter Windows nicht reproduzieren (kein Xcode).
  Kein automatisierter Test deckt `Info.plist`-Inhalte, `SceneDelegate` oder die
  Action-YAMLs ab.
- **`staging-ci`/`release` selbst:** Workflow-Ergebnisse sind nur auf GitHub prüfbar;
  der letzte dokumentierte Lauf (Run `45486899366`, 2026-11-24) schlug in
  `package-ios` (MT7199) und `package-android` (NETSDK1094) fehl — nicht lokal
  reproduzierbar.
- Keine übersprungenen/abgebrochenen Unit-Tests; alle Suiten liefen vollständig.

## Testklassen

Für den betroffenen Bereich (Plattform-Lifecycle iOS/MacCatalyst, CI-Packaging)
existieren **keine** Testklassen. Die nächstliegenden Abdeckungen:

### `scripts/resolve-release-version.test.mjs` (Node `node:test`)
- `parseManualTag`, `classifyWorkflowRef`, `releaseHasExpectedAsset`, `semanticReleaseDryRunVersion`, `repairIncompleteRelease`, `incompleteReleases` — prüft u. a. die erwartete Asset-Liste inkl. iOS-Bedingung (`requires release-ios.ipa only when IOS_SIGNING_ENABLED is true`).

### `scripts/release-assets.test.mjs`
- `buildReleaseAssets`/`releaseAssetEnv` — Asset-Liste `release-win-x64.zip`, `release-android.apk`, optional `release-ios.ipa`.

### `scripts/create-update-manifest.test.mjs`
- `update.json`-Erzeugung für den `prerelease`-/`publish`-Job.

### `src/Reporter.Tests` (Auswahl, kein direkter Bezug)
- `TestClassificationConventionTests` — stellt die Trait-Konvention (`Category=OsInterface`) sicher; relevant, weil die CI-Lanes danach filtern.

## Hilfsmethoden

### `src/Reporter.E2ETests` (FlaUI-Hilfen)
- `UiRetry.WaitForElement`/`WaitForElementByName`/`WaitForElementInScope` — UIA-Polling mit Timeout (Ort aller E2E-Fehlschläge).
- `E2EPageHelpers.OpenFeedDetail` (`E2EPageHelpers.cs:293`) — navigiert in die Feed-Detailseite; wartet dort vergeblich auf Element `Aktionen`.
- `ReporterAppFixture`/`E2EProcessGuard`/`E2ETestCollection` — App-Start, Prozess-Guard, serielle Ausführung.
- `StubFeedServer` + `Fixtures/*.xml` — lokale HTTP-Feed-Stubs.
- `OsInterfaceAttribute`/`FlakyAttribute` — Trait-Markierung (OsInterface) bzw. Flaky-Kennzeichnung.

### `scripts/Retry-FlakyTests.ps1` / `Analyze-FlakyTestResults.ps1`
- CI-Helfer für die OsInterface-Lane (Retry + Sichtbarkeitsreport).
