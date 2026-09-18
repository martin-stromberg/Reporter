<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

Finaler Nachweis (Iteration 3, 18.09.2026 ~11:0x MESZ): `dotnet build Reporter.sln` (`IncludeIosTarget=false IncludeAndroidTarget=false`) 0 Fehler/0 Warnungen; `dotnet test Reporter.sln --filter "Category!=E2E"` **574/574** (566 `Reporter.Tests` + 8 `E2EProcessGuardTests`); `npm test` **36/36**; voller `scripts/Run-E2ETests.ps1`-Suite-Lauf **17/17 bestanden** (alle 9 E2E-Tests inkl. des zuvor geflaketen `SmokeTests.FeedActionSheet_ChangeCategory_IncludingNone` + 8 `E2EProcessGuardTests`, Dauer 56 s, Exit-Code 0) — die `OpenFeedActions`-Retry-Härtung aus Iteration 3 ist verifiziert. Danach `Get-Process Reporter` **leer**, `%TEMP%/reporter-e2e-*`-Zählung **44 → 44** (kein neuer Leichnam; neuester Ordner 10:19:55 aus dem Iteration-2-Abbruchlauf, dieser Lauf ~11:0x). `Run-StaticChecks.ps1` Exit 0 (Format, Lizenzheader, Security, Static-Analysis-Build ohne Befund). Baseline vor allen Läufen: 0 `Reporter`-Prozesse, 44 historische Temp-Ordner.

## E2E-Abdeckung

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Pflicht: Erfolgreicher `Run-E2ETests.ps1`-Lauf → `Get-Process Reporter` leer, keine neuen Temp-Leichname | Manueller Verifikationslauf | Bestanden — Suite-Lauf am 18.09.2026 ~11:0x: Exit-Code 0, danach 0 `Reporter`-Prozesse und **+0 neue** `%TEMP%/reporter-e2e-*`-Verzeichnisse (44→44); Fixture- und Demo-Temp-Dirs wurden im Teardown gelöscht. |
| Pflicht: Fehlschlag-Lauf (mind. 1 scheiternder Test) → `Get-Process Reporter` leer | Manueller Verifikationslauf | Bestanden — Nachweis aus Iteration 2 (Suite-Lauf mit Flake-Fehlschlag: `DisposeAsync` lief, danach 0 `Reporter`-Prozesse, 0 Temp-Leichname); dieser Lauf war vollständig grün, Fehlschlag-Pfad daher nicht erneut ausgelöst. |
| Pflicht: Abbruch-Lauf (harter `testhost.exe`-Kill) → `Get-Process Reporter` leer | Manueller Verifikationslauf | Bestanden — Nachweis aus Iteration 2: direkter `dotnet test` ohne Skript-`finally`, `testhost.exe` (PID 24988) per `Stop-Process -Force` gekillt, während `Reporter.exe` (PID 33468) lief; danach kein `Reporter`-Prozess — allein das Job Object `KILL_ON_JOB_CLOSE` ließ die App mitsterben. Erwartbarer Temp-Leichnam `reporter-e2e-184d3767…` (10:19:55) wie dokumentiert. |
| Neun bestehende E2E-Tests unverändert und grün | `Run-E2ETests.ps1` (`SmokeTests` 7, `ArticleLinkTests` 1, `DemoSeedTests` 1) | Bestanden — 9/9 E2E-Tests grün; `SmokeTests.FeedActionSheet_ChangeCategory_IncludingNone` (zuvor 3/3 Suite-Läufe geflakt) bestanden dank `OpenFeedActions`-Retry (max. 3 Versuche); `DemoSeedTests.FirstStart_SeedsNewsCategoryAndDemoFeed` bestanden. |
| Regression: `dotnet test Reporter.sln --filter "Category!=E2E"` (`IncludeIosTarget=false IncludeAndroidTarget=false`) | `Reporter.Tests` + `E2EProcessGuardTests` | Bestanden — 574/574 (566 `Reporter.Tests` + 8 `E2EProcessGuardTests` im E2E-Projekt ohne `Category=E2E`-Trait) |
| Regression: `npm test` | `scripts/*.test.mjs` | Bestanden — 36/36 |
| Regression: `.\scripts\Run-StaticChecks.ps1` | Format, Lizenzheader, Security, Static-Analysis-Build | Bestanden — Exit 0, alle Checks ohne Befund |

## Zusammenfassung

Automatisierte Tests (letzter vollständiger Lauf je Suite):

- Gesamt: 619 (574 dotnet ohne E2E + 36 Node + 9 E2E)
- Bestanden: 619
- Fehlgeschlagen: 0
- Übersprungen: 0

Hinweise:

- `Run-E2ETests.ps1` führte das gesamte `Reporter.E2ETests`-Projekt aus (17 Tests: 9 E2E + 8 `E2EProcessGuardTests`, die bewusst kein E2E-Trait tragen): 17 bestanden, 0 fehlgeschlagen, Exit-Code 0, Dauer ~56 s.
- Prozess-Freiheit wurde nach dem Suite-Lauf per `Get-Process -Name Reporter` verifiziert (leer); Temp-Baseline vor allen Läufen 44 Ordner, Endstand 44 Ordner — der einzige Leichnam aus diesem Feature-Zyklus bleibt `reporter-e2e-184d3767…` aus dem dokumentierten Iteration-2-Abbruchlauf (bei Test-Host-Tod ist kein Teardown möglich, das Job Object verhindert nur das Prozess-Leck).
- `dotnet test` auf die Solution lief nach vorausgegangenem sauberem `dotnet build Reporter.sln` (0 Warnungen, 0 Fehler).

## Testabdeckung

**Abdeckung:** 46,9 % Zeilenabdeckung (Cobertura über `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --collect:"XPlat Code Coverage"`, nur `Reporter.Tests`-Ausführung; `src/Reporter` (MAUI-App) ohne eigene Testprojekt-Abdeckung)

| Datei | Abdeckung |
|-------|-----------|
| `Reporter.Core\ViewModels\UnreadViewModel.cs` | 0 % |
| `Reporter.Core\Models\CategoryFilterItem.cs` | 22,2 % |
| `Reporter.Core\Resources\Strings\AppResources.Designer.cs` | 25,9 % (generiert) |
| `Reporter.Core\Services\FeedSearchUnavailableException.cs` | 33,3 % |
| `Reporter.Data\Repositories\ItemRepository.cs` | 45,5 % / 77,8 % (zwei Klassen) |
| `Reporter.Core\ViewModels\CategoriesViewModel.cs` | 53,8 % |
| `Reporter.Core\ViewModels\SettingsViewModel.cs` | 53,8–75,0 % (partiell) |
| `Reporter.Core\Services\FeedIconService.cs` | 62,5 % |
| `Reporter.Core\ViewModels\LaterViewModel.cs` | 63,6 % |
| `Reporter.Core\ViewModels\FeedsViewModel.cs` | 66,7 % |
| `Reporter.Data\Migrations\Content\20260918022828_InitialContentCreate.cs` | 76,5 % (generiert) |
| `Reporter.Data\Repositories\CategoryRepository.cs`, `ItemContentRepository.cs`, `KeywordRepository.cs`, `SyncLogRepository.cs` | je 77,8 % |
| `Reporter.Data\Migrations\*` (23 Dateien, generiert, inkl. `ReporterDbContextModelSnapshot.cs`) | 0 % |

## Fehlende Tests

Quelle: `Coverage-Daten` + `Dateinamen-Konvention`

- `src/Reporter.Core/ViewModels/UnreadViewModel.cs` — 0 % Abdeckung.
- `src/Reporter.Data/Migrations/*` — 0 % Abdeckung (generierte EF-Core-Migrationen, üblicherweise ungetestet).
- `src/Reporter.E2ETests/E2EProcessGuard.cs` ist durch `E2EProcessGuardTests.cs` (8 Tests, alle bestanden) abgedeckt; der Funktionsnachweis des Kill-on-Close-Pfads erfolgt zusätzlich über den manuellen Abbruch-Verifikationslauf (siehe E2E-Abdeckung).
