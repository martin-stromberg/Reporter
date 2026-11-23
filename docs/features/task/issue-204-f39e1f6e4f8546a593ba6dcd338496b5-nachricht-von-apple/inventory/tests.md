<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Tests und Test-Ausgangszustand

Die Anforderung (Apple ITMS-90725, iOS-Build-Toolchain) berührt keine C#-Klassen;
fachlich betroffene Testklassen existieren nicht. Dieses Dokument hält den
projektweiten Test-Ausgangszustand vor der Umsetzung fest.

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 23.11.2026, ~10:00–11:30 Uhr (lokale Zeit, UTC+1, `date`: `Mon Nov 23 10:01:37 2026`)
- Branch und Commit-ID: `task/issue-204-f39e1f6e4f8546a593ba6dcd338496b5-nachricht-von-apple`, HEAD = `a412efa4d390df9f44fe6206a0e0299ed79c5a6e` (Stand = `staging` nach Merge der .NET-11-Migration)
- Uncommittete Änderungen im getesteten Stand: keine — einzig die neu angelegten, noch untracked Feature-Dokumente unter `docs/features/task/issue-204-…/` (requirement.md sowie die hier erzeugten Inventory-Artefakte); keine Änderungen an getrackten Dateien
- Testumgebung und Runtime-/SDK-Versionen:
  - Windows (lokale Dev-Maschine), Git-Bash/PowerShell
  - `dotnet --version` = `11.0.100` (entspricht `global.json`-Pin `11.0.100`/`latestPatch`)
  - `dotnet workload list`: `android 37.2.31`, `ios 27.2.11039`, `maccatalyst 27.2.11039`, `maui-windows 11.0.0` (jeweils Band `11.0.100`, Quelle VS 18.11) — der iOS-Workload-Pin `27.2.11039` ist lokal bereits installiert
  - Node.js `v24.15.0`, npm `11.10.0`
  - Test-Runner: Microsoft.Testing.Platform (`global.json` → `test.runner`)
- Ermittelte Testsuiten und Quellen der Testbefehle:
  1. `src/Reporter.Tests` (xUnit v3, MTP; 104 Testdateien, ~71 Testklassen) — CI-Befehl aus `staging-ci.yml`/`release.yml`: `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj -c Release` (regular: `--filter-not-trait "Category=OsInterface"`; OS-Lane: `--filter-trait "Category=OsInterface"`)
  2. `src/Reporter.E2ETests` (FlaUI/UIA3, xUnit v3) — Befehl: `scripts/Run-E2ETests.ps1` (baut `net11.0-windows10.0.19041.0`/win-x64 Debug, startet `Reporter.exe` mit Stub-Feed-Server)
  3. `scripts/*.test.mjs` (node:test, 36 Tests für `resolve-release-version.mjs`, `release-assets.mjs`, `create-update-manifest.mjs`) — Befehl: `npm test` (`node --test "scripts/*.test.mjs"`); dokumentiert in `docs/help/release-management/installation.md` („36 `node:test`-Tests")
  4. Statische Checks `scripts/Run-StaticChecks.ps1` (Format, Lizenzheader, Security, Static-Analysis-Build) — CI-Gate-Pendant, kein Testlauf im engeren Sinn; nicht ausgeführt (kein Produktiv-/Testcode verändert; der Gate prüft C#-Code, der von dieser Anforderung unberührt bleibt)

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 | `npm test` (`node --test "scripts/*.test.mjs"`) | Repo-Root | 0 | 36 | 0 | 0 | [npm-test.log](test-results/npm-test.log) |
| 2 | `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj --configuration Release -- --filter-not-trait "Category=OsInterface" --report-trx --report-trx-filename test-results-inventory-regular.trx` | Repo-Root | 0 | 801 | 0 | 0 | [dotnet-test-regular.log](test-results/dotnet-test-regular.log), [TRX](test-results/test-results-inventory-regular.trx) |
| 3 | `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build -- --filter-trait "Category=OsInterface" --report-trx --report-trx-filename test-results-inventory-osinterface.trx` | Repo-Root | 0 | 4 | 0 | 0 | [dotnet-test-osinterface.log](test-results/dotnet-test-osinterface.log), [TRX](test-results/test-results-inventory-osinterface.trx) |
| 4 | `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Run-E2ETests.ps1` | Repo-Root | 2 (`dotnet test`-Exitcode, vom Skript weitergegeben, `Run-E2ETests.ps1` Zeile 71) | 21 | 18 | 0 | [e2e-tests.log](test-results/e2e-tests.log) |

### Nachgewiesene bestehende Testfehler

E2E-Lauf: `gesamt: 39, fehlgeschlagen: 18, erfolgreich: 21, übersprungen: 0,
Dauer: 25m 09s`. Alle 18 Fehler teilen dasselbe Fehlerbild —
`System.TimeoutException: Element not found within 00:00:20: name 'Aktionen'`
(FlaUI/UIA3, Warten auf den Aktions-Button der Feed-Detailseite; die App startete
und das Hauptfenster wurde gefunden, das Zielelement aber nicht).

Fehlgeschlagene Tests (vollständige Liste aus `e2e-tests.log`):

1. `Reporter.E2ETests.SmokeTests.Search_SubscribesResult_PersistsFeed`
2. `Reporter.E2ETests.FeedDetailTests.FeedDetail_Search_FiltersByTag`
3. `Reporter.E2ETests.FeedDetailTests.FeedDetail_MetaInfo_WrapsInsteadOfOverlappingActionsButton`
4. `Reporter.E2ETests.FeedDetailTests.FeedDetail_Navigation_ShowsFeedItems`
5. `Reporter.E2ETests.FeedDetailTests.FeedDetail_FeedKeyword_FiltersOnlyOwnFeed`
6. `Reporter.E2ETests.FeedDetailTests.FeedDetail_Delete_ReturnsToList`
7. `Reporter.E2ETests.FeedDetailTests.FeedDetail_Rename_UpdatesTitle`
8. `Reporter.E2ETests.FeedDetailTests.FeedDetail_Edit_FeedKeywordValidation_ShowsErrors`
9. `Reporter.E2ETests.FeedDetailTests.FeedDetail_Search_FiltersItems`
10. `Reporter.E2ETests.FeedDetailTests.FeedDetail_RefreshAction_SyncsFeed`
11. `Reporter.E2ETests.FeedDetailTests.FeedDetail_FeedKeyword_NotInGlobalSettings`
12. `Reporter.E2ETests.FeedDetailTests.FeedDetail_InfiniteScroll_LoadsNextPage`
13. `Reporter.E2ETests.FeedDetailTests.FeedDetail_Edit_AddKeyword_PersistsAndShowsChip`
14. `Reporter.E2ETests.FeedDetailTests.FeedDetail_Edit_RemoveKeyword_RemovesChipAndRow`
15. `Reporter.E2ETests.FeedDetailTests.FeedDetail_Message_ForWarningFeed`
16. `Reporter.E2ETests.FeedDetailTests.FeedDetail_Edit_PersistsChanges`
17. `Reporter.E2ETests.FeedDetailTests.FeedDetail_ChangeCategory_IncludingNone`
18. `Reporter.E2ETests.FeedDetailTests.FeedDetail_Message_ForErrorFeed`

Gemeinsamer Stack (repräsentativ, aus `e2e-tests.log`):

```text
Xunit.MicrosoftTestingPlatform.XunitException: System.TimeoutException : Element not found within 00:00:20: name 'Aktionen'
   um Reporter.E2ETests.UiRetry.WaitForElement(...)                    UiRetry.cs:37
   um Reporter.E2ETests.UiRetry.WaitForElementByName(...)              UiRetry.cs:133
   um Reporter.E2ETests.E2EPageHelpers.OpenFeedDetail(String feedTitle) E2EPageHelpers.cs:293
   um <jeweilige Testmethode>                                          FeedDetailTests.cs / SmokeTests.cs
```

Nach dem Testlauf beendete das Skript `Reporter.exe` (Exit `-1`,
`[E2E] Reporter.exe exited at 10:27:43`); Abschlussmeldung:
`Reporter.E2ETests.dll (net11.0|x64) fehlerhaft mit 18 Fehler(n) [+21/x18/?0]
(25m 08s 202ms)`, `Exitcode: 2`.

### Testlücken und Ausführungsprobleme

- **E2E-Suite (`src/Reporter.E2ETests`):** FlaUI/UIA3 setzt eine interaktive
  Desktop-Session mit sichtbarem Fenster voraus (`Run-E2ETests.ps1` Kommentar
  Zeilen 9–13; `ReporterAppFixture.cs` Zeile 216: „requires an interactive
  Windows desktop session"). In dieser Agenten-Session startete die App und
  das Hauptfenster wurde gefunden, einzelne UI-Elemente (z. B. der
  `Aktionen`-Button, Dialog-Button `Ja`) wurden jedoch nicht gefunden —
  die Fehlerbilder sind unten dokumentiert. Die Suite ist in CI nicht Teil
  der Gates und liefert für die reine Build-/CI-Anforderung ohnehin kein
  Signal.
- **Statische Checks:** `Run-StaticChecks.ps1` wurde nicht ausgeführt
  (keine Codeänderung für die Bestandsaufnahme; dokumentierte Entscheidung
  oben).
- **CI-Artefakt-Tests:** Für die eigentlich betroffenen Artefakte
  (`.github/actions/package-ios/action.yml`, Workflows, `iOS-Deployment.ps1`)
  existiert keine automatisierte Testsuite — Verifikation solcher Änderungen
  geschieht projektüblich ausschließlich über CI-Läufe (macOS-Job) bzw.
  manuelle Skriptläufe auf einem Mac. Insbesondere kann der iOS-Job wegen
  `vars.IOS_SIGNING_ENABLED`-Gate + `version.changed`-Gate nur auf
  `staging`/`main` ausgeübt werden — `pr-staging-ci.yml` hat keinen iOS-Job.

## Testklassen

Für die Anforderung (reine Build-/CI-Korrektur, kein App-Code) gibt es **keine
fachlich zugeordneten Testklassen**. Übergreifend relevante Suites:

### `Reporter.Tests` (Unit-Tests, 801 regular + 4 OsInterface)

- Prüfen `Reporter.Core`/`Reporter.Data` — keine Berührung mit der iOS-Build-Kette.
- Hilfsattribute: `OsInterfaceAttribute`/`FlakyAttribute`/`TestCategories`
  markieren OS-abhängige bzw. flaky Tests; CI splittet danach in zwei Lanes.
- `TestClassificationConventionTests` erzwingt die Trait-Konventionen.

### `Reporter.E2ETests` (FlaUI-Desktop-Suite)

- ~39 Tests in `SmokeTests` (5), `FeedDetailTests` (17), `UnreadPacketTests` (3),
  `ArticleImageTests` (2), `ArticleLinkTests` (1), `ArticleTagsTests` (2),
  `DemoSeedTests` (1), `E2EProcessGuardTests` (8).
- Hilfscode: `ReporterAppFixture` (App-Start mit Stub-Server +
  Temp-DB, UIA3-Attach), `StubFeedServer`, `UiRetry`, `E2EProcessGuard`,
  `E2EPageHelpers`, `FeedDbAssertions`, `FlakyAttribute`,
  `OsInterfaceAttribute`, `TestCategories`, `E2ETestCollection`.

### `scripts/*.test.mjs` (36 `node:test`-Tests der Release-Skripte)

- `resolve-release-version.test.mjs` — Ref-Klassifizierung, Repair-Logik
  (inkl. iOS-Asset-Erwartung via `IOS_SIGNING_ENABLED`-Auswertung der
  Aufrufer-Seite: `EXPECTED_ASSETS` enthält `release-ios.ipa` nur bei
  aktivem Signing-Schalter, Test „requires release-ios.ipa only when
  IOS_SIGNING_ENABLED is true" ✅).
- `release-assets.test.mjs` — Asset-Listen inkl. `release-ios.ipa`.
- `create-update-manifest.test.mjs` — `update.json`-Schema.
- **Berührungspunkt mit der Anforderung:** Änderungen am iOS-Artefaktfluss
  (Name/Pfad der `.ipa`) würden diese Tests betreffen; eine reine
  Xcode-/SDK-Korrektur ändert daran nichts.
