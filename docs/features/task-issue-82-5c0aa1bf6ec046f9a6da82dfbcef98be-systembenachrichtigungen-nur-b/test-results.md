<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ergebnisse

## Ergebnis

**Status:** Fehler vorhanden

Alle 491 automatisierten Tests sind im Release-Modus bestanden (0 fehlgeschlagen, 0 übersprungen), darunter die für dieses Feature neu hinzugekommenen Tests `ScheduledSyncRunnerTests` (4 Tests: `RunAsync_SyncSucceeds_ReturnsTrueAndReschedules`, `RunAsync_SyncFails_ReturnsFalseAndStillReschedules`, `RunAsync_SyncCancelled_ReturnsFalseAndStillReschedules`, `RunAsync_ReschedulingFails_ReturnsTrue`), die `IBackgroundRefreshService`-Weiterleitungs-Tests in `AutoRefreshServiceTests` (`ApplySettings_ForwardsToBackgroundRefresh`, `ApplySettings_WhenAutoRefreshDisabled_StillForwardsToBackgroundRefresh`, `ApplySettings_BackgroundRefreshThrows_TimerStillConfigured`, `ApplySettings_BackgroundRefreshUnsupported_DoesNotForward`) und `ServiceCollectionTests.AddReporterServices_ResolvesScheduledSyncRunner`. Der Solution-Build lief mit 0 Warnungen und 0 Fehlern durch; `.\scripts\Run-StaticChecks.ps1` (Format, Lizenzheader, Security-Scan, statische Analyse mit `TreatWarningsAsErrors`) ist vollständig grün. Der Status ist dennoch „Fehler vorhanden", weil alle sieben im Plan als Pflicht markierten E2E-Szenarien nicht ausgeführt werden konnten: Es handelt sich um manuelle iOS-Verifikationen via `scripts/iOS-Deployment.ps1` bzw. lldb-Task-Simulation, die ein macOS-System mit iOS-Gerät/Simulator voraussetzen und in dieser Windows-Umgebung nicht ausführbar sind. Es liegt kein Produktfehler vor — die fehlenden Nachweise sind eine Umgebungslimitation und müssen auf macOS/iOS nachgeholt werden.

## Fehlgeschlagene Tests

### Automatisierte Tests

Keine — 491/491 bestanden.

### Manuelle iOS-E2E-Szenarien (Pflicht laut Plan, nicht ausgeführt)

- **Manueller Sync (Pull-to-Refresh) bei Vordergrund-App erzeugt kein Banner/keinen Sound/keinen Mitteilungszentrum-Eintrag** — Nicht ausgeführt: macOS/iOS-Gerät erforderlich (`scripts/iOS-Deployment.ps1` unter Windows nicht ausführbar)
- **Timer-Abruf (`AutoRefreshService`) bei laufender App erzeugt keine Systemmitteilung** — Nicht ausgeführt: macOS/iOS-Gerät erforderlich
- **Start-Abruf bei aktiviertem `RefreshOnStartupEnabled` (Kaltstart in den Vordergrund) erzeugt keine Systemmitteilung** — Nicht ausgeführt: macOS/iOS-Gerät erforderlich
- **Simulierter `BGAppRefreshTask` (`lldb` `_simulateLaunchForTaskWithIdentifier:@"de.martinstromberg.reporter.feedrefresh"`) führt Sync aus und erzeugt sichtbare Mitteilung** — Nicht ausgeführt: macOS/iOS-Gerät erforderlich
- **Task-Expiration (`_simulateExpirationForTaskWithIdentifier`) kancelliert den Sync, `SetTaskCompleted` läuft, App bleibt stabil** — Nicht ausgeführt: macOS/iOS-Gerät erforderlich
- **`AutoRefreshEnabled` aus → kein `BGAppRefreshTask` eingeplant; Neuplanung nach Wiederaktivierung** — Nicht ausgeführt: macOS/iOS-Gerät erforderlich
- **Antippen einer Hintergrund-Mitteilung navigiert wie bisher (`articledetail`/`//unread`, Link-Fallback)** — Nicht ausgeführt: macOS/iOS-Gerät erforderlich

## E2E-Abdeckung

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Manueller Sync (Pull-to-Refresh) bei laufender Vordergrund-App mit neuen Artikeln erzeugt kein Banner/keinen Sound/keinen Mitteilungszentrum-Eintrag | Manuell via `scripts/iOS-Deployment.ps1` | Nicht ausgeführt (in dieser Umgebung nicht ausführbar — macOS/iOS-Gerät erforderlich) |
| Timer-Abruf (`AutoRefreshService`) bei laufender App erzeugt keine Systemmitteilung | Manuell via `scripts/iOS-Deployment.ps1` | Nicht ausgeführt (in dieser Umgebung nicht ausführbar — macOS/iOS-Gerät erforderlich) |
| Start-Abruf bei aktiviertem `RefreshOnStartupEnabled`: Kaltstart in den Vordergrund mit neuen Artikeln → `RunStartupSyncAsync` läuft, keine Systemmitteilung | Manuell via `scripts/iOS-Deployment.ps1` | Nicht ausgeführt (in dieser Umgebung nicht ausführbar — macOS/iOS-Gerät erforderlich) |
| Simulierter `BGAppRefreshTask` (`lldb` `_simulateLaunchForTaskWithIdentifier`) führt Sync aus und erzeugt bei neuen Artikeln eine sichtbare Mitteilung | Manuell via `scripts/iOS-Deployment.ps1` + lldb | Nicht ausgeführt (in dieser Umgebung nicht ausführbar — macOS/iOS-Gerät erforderlich) |
| Task-Expiration (`_simulateExpirationForTaskWithIdentifier`) → Sync kancelliert, `SetTaskCompleted` läuft, App bleibt stabil | Manuell via `scripts/iOS-Deployment.ps1` + lldb | Nicht ausgeführt (in dieser Umgebung nicht ausführbar — macOS/iOS-Gerät erforderlich) |
| `AutoRefreshEnabled` aus → kein `BGAppRefreshTask` eingeplant; Neuplanung nach Wiederaktivierung | Manuell via `scripts/iOS-Deployment.ps1` | Nicht ausgeführt (in dieser Umgebung nicht ausführbar — macOS/iOS-Gerät erforderlich) |
| Antippen einer Hintergrund-Mitteilung navigiert wie bisher (`articledetail`/`//unread`, Link-Fallback) | Manuell via `scripts/iOS-Deployment.ps1` | Nicht ausgeführt (in dieser Umgebung nicht ausführbar — macOS/iOS-Gerät erforderlich) |

## Zusammenfassung

- Gesamt: 491
- Bestanden: 491
- Fehlgeschlagen: 0
- Übersprungen: 0
- Nicht ausgeführte manuelle E2E-Pflichtszenarien: 7 (Umgebungslimitation, siehe oben)

Ausgeführte Befehle (identisch zum CI-Job `build-and-test`, Env `IncludeIosTarget=false`, `IncludeAndroidTarget=false`):

1. `dotnet build Reporter.sln --configuration Release` → 0 Warnungen, 0 Fehler
2. `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"` → 491/491 bestanden in 3,5 s; Coverage: `src/Reporter.Tests/TestResults/32d23a01-3ad6-43c5-be2f-dd14331c3283/coverage.cobertura.xml`
3. `.\scripts\Run-StaticChecks.ps1` → alle Checks bestanden (Format, Lizenzheader, Security-Scan ohne Befunde, statische Analyse mit 0 Warnungen/0 Fehlern), Exit-Code 0

Hinweis: Der iOS-Plattformcode (`AppDelegate`, `NotificationDelegate`, `#if IOS`-Blöcke in `BackgroundRefreshService`) wird auf Windows nicht kompiliert; Compile- und Laufzeit-Verifikation dieses Codes stehen mit dem macOS-Build aus.

## Testabdeckung

**Abdeckung:** 91,9 % (Zeilen, gesamt laut `coverage.cobertura.xml`; Reporter.Core 89,5 %, Reporter.Data 99,0 %; Branch-Abdeckung 87,5 %; EF-Core-Migrationsdateien über `coverlet.runsettings` ausgeschlossen)

| Datei | Abdeckung |
|-------|-----------|
| `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs` | 26,2 % (generierte Datei) |
| `src/Reporter.Core/Models/CategoryFilterItem.cs` | 28,6 % |
| `src/Reporter.Core/Services/FeedSearchUnavailableException.cs` | 33,3 % |

Alle weiteren Quelldateien liegen bei mindestens 80 % Zeilenabdeckung (nach Aggregation partialer Klassen/Statemachines pro Datei). `AutoRefreshService.cs` einschließlich der `IBackgroundRefreshService`-Weiterleitung (93 %) und `ScheduledSyncRunner.cs` (100 %) sind durch die neuen Tests abgedeckt. `BackgroundRefreshService.cs` liegt im MAUI-Projekt (`src/Reporter/`), das von `Reporter.Tests` nicht referenziert wird, und ist `#if IOS`-dominiert — nicht coverage-messbar (plattformbedingt, siehe Plan).

## Fehlende Tests

Quelle: `Coverage-Daten`

Keine Quelldatei mit 0 % Zeilenabdeckung gefunden. Die generierte Datei `AppResources.Designer.cs` (26,2 %) wird gemäß Konvention nicht als fehlende Testabdeckung gewertet; `CategoryFilterItem.cs` (28,6 %) und `FeedSearchUnavailableException.cs` (33,3 %) sind teilweise abgedeckt.

Die tatsächliche Nachweislücke betrifft ausschließlich die sieben manuellen iOS-E2E-Pflichtszenarien (Abschnitt „E2E-Abdeckung"): Sie erfordern ein macOS-System mit iOS-Gerät/Simulator und `scripts/iOS-Deployment.ps1` sowie lldb (`_simulateLaunchForTaskWithIdentifier`, `_simulateExpirationForTaskWithIdentifier`) und sind unter Windows nicht ausführbar. Diese Nachweise sind auf macOS/iOS nachzuholen, bevor die Anforderung als vollständig verifiziert gilt.
