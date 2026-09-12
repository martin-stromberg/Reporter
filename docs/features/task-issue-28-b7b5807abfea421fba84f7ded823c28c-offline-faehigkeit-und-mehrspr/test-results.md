# Test-Ergebnisse

Geprüfter Stand: Branch `task/issue-28-b7b5807abfea421fba84f7ded823c28c-offline-faehigkeit-und-mehrspr`, Working Tree mit den uncommitteten Feature-Änderungen (Issue #28: Offline-Fähigkeit + Mehrsprachigkeit EN/DE) — **3. Iteration**, inkl. der Fixes aus den Review-Befunden (Runde 2).
Testumgebung: Windows, .NET SDK 10.0.401, Umgebungsvariable `IncludeIosTarget=false` (iOS-Target auf Windows deaktiviert, wie in CI und Baseline).
Vorherige Läufe: `test-results.1.md` (Iteration 1, 208 Tests), `test-results.2.md` (Iteration 2, 227 Tests) — Logs dieser Iteration unter `test-results/run3/`.

## Ergebnis

**Status:** Fehler vorhanden

Hinweis zum Status: Alle 231 automatisierten Tests sind bestanden (0 fehlgeschlagen). Der Status resultiert ausschließlich daraus, dass die als **Pflicht** geplanten E2E-Szenarien nicht ausgeführt wurden — es existiert keine automatisierte UI-Test-Infrastruktur; der Plan sieht dokumentierte manuelle Verifikation vor (plan.md, Abschnitt „E2E-Tests", Z. 355–370). Diese manuellen Verifikationsnachweise sind **ausstehend** (kein Gerät-/Emulator-Zugriff, keine Netzwerk-Umschaltung und kein interaktiver App-Start in dieser Umgebung) und in der Tabelle „E2E-Abdeckung" vollständig aufgeführt — nicht als bestanden, nicht als fehlgeschlagene Tests.

## Testläufe

| Lauf | Befehl | Arbeitsverzeichnis | Exit-Code | Ergebnis | Nachweis |
|------|--------|--------------------|-----------|----------|----------|
| Restore | `dotnet restore Reporter.sln -r win-x64` (Env: `IncludeIosTarget=false`) | Repo-Root | 0 | Alle 4 Projekte wiederhergestellt | [01-dotnet-restore.log](test-results/run3/01-dotnet-restore.log) |
| Build | `dotnet build Reporter.sln --configuration Release --no-restore` (Env: `IncludeIosTarget=false`) | Repo-Root | 0 | 0 Warnungen, 0 Fehler (ganze Solution inkl. MAUI-App) | [02-dotnet-build.log](test-results/run3/02-dotnet-build.log) |
| Tests + Coverage | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 | 231 bestanden, 0 fehlgeschlagen, 0 übersprungen (Baseline: 189; Iteration 1: 208; Iteration 2: 227) | [03-dotnet-test.log](test-results/run3/03-dotnet-test.log), [test-results.trx](test-results/run3/test-results.trx), [coverage.cobertura.xml](test-results/run3/coverage.cobertura.xml) |
| Static Checks | `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Run-StaticChecks.ps1` | Repo-Root | 0 | Format-Check bestanden; keine verwundbaren NuGet-Pakete; statische Analyse (Release-Build mit Warnungen als Fehler, inkl. MAUI-App) ohne Befund — „Alle gewaehlten Static-Checks wurden erfolgreich ausgefuehrt." | [04-static-checks.log](test-results/run3/04-static-checks.log) |

## Fehlgeschlagene Tests

Keine — alle 231 automatisierten Tests bestanden. Der Status „Fehler vorhanden" begründet sich allein aus den nicht ausgeführten Pflicht-E2E-Szenarien (siehe unten), nicht aus fehlgeschlagene Tests.

## E2E-Abdeckung

Es existiert keine automatisierte UI-Test-Infrastruktur (`Reporter.Tests` referenziert nur `Reporter.Core`/`Reporter.Data`; kein Appium/UITest-Projekt). Die geplanten E2E-Szenarien sind daher **manuelle Verifikationsnachweise** (plan.md Z. 355–370, plan-check.md Abschnitt „E2E-Abdeckung": alle als „Abgedeckt" geplant). In dieser Umgebung nicht ausführbar — alle Szenarien bleiben **offen** und sind nachzuholen (Windows-Target im 390 × 844-pt-Fenster; iOS-Simulator nur auf macOS-Host via `scripts/iOS-Deployment.ps1`).

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| App komplett offline (Flugmodus) starten → Ungelesen/Später/Kategorien/Feeds zeigen synchronisierte Inhalte; `ArticleCardView`-Thumbnails offline ausgeblendet; Artikeldetail lesbar | Manuelle Verifikation (Pflicht) | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Sync-Button auf Ungelesen gedimmt + `OfflineHint`; Tap und Pull-to-Refresh → lokalisierter Hinweis, kein Absturz, kein `SyncLog`-Fehler-Rauschen | Manuelle Verifikation (Pflicht); Logik-Teil abgedeckt durch `UnreadViewModelTests.RefreshCommand_WhenOffline_SkipsSyncWithoutError` u. a. | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Links im `ArticleWebView` nicht klickbar/neutralisiert; Tap auf Rest-Links → lokalisierter Alert (`OfflineHint`/`ArticleOfflineLinksDisabled`) | Manuelle Verifikation (Pflicht); Logik-Teil abgedeckt durch `ArticleHtmlSanitizerTests` + `WebViewNavigationGuardTests` (10 Fälle) | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Artikeldetail rendert ohne externe `<img>`-Bilder (Elemente entfernt, keine leeren Platzhalter); Text vollständig lesbar | Manuelle Verifikation (Pflicht); Sanitizer-Teil abgedeckt durch `ArticleHtmlSanitizerTests` (12 Fälle) | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Online→Offline→Online-Wechsel zur Laufzeit ohne App-Neustart → Statusanzeige und Link-Verhalten folgen | Manuelle Verifikation (Pflicht); Logik-Teil abgedeckt durch `BaseViewModelConnectivityTests` (5 Fälle) + `ConnectivityChanged_ClearsErrorMessage`/`ConnectivityChanged_ClearsSyncErrorMessage` | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Systemsprache DE → deutsche Texte; EN → englische Texte; Drittsprache (z. B. FR) → englischer Fallback (neutrales `AppResources.resx`) | Manuelle Verifikation mit Screenshots (Pflicht) | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Feeds-Seite zeigt ohne Nutzeraktion den `OfflineHint`-Banner oberhalb der Liste; Banner verschwindet nach Netzrückkehr | Manuelle Verifikation (Pflicht); Logik-Teil abgedeckt durch `FeedsViewModelTests`-Connectivity-Fälle | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Pull-to-Refresh und Einzel-Feed-Aktualisierung auf Feeds → lokalisierter Hinweis, kein Absturz | Manuelle Verifikation (Pflicht); Logik-Teil abgedeckt durch `FeedsViewModelTests.RefreshAllCommand_WhenOffline_SkipsSyncWithoutError` / `RefreshCommand_WhenOffline_SkipsSyncWithoutError` | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Artikeldetail → „Im Browser öffnen" (Footer + Aktionsleisten-Icon) → lokalisierter Hinweis im `ErrorMessage`-Label statt Browser-Öffnung, kein Absturz; `ArticleOfflineLinksDisabled`-Offline-Label sichtbar | Manuelle Verifikation (Pflicht; `ArticleDetailViewModel` liegt im MAUI-Projekt, nicht unit-testbar) | Nicht ausgeführt — manuelle Verifikation ausstehend |
| iOS-Simulator-Lauf mit denselben Kern-Szenarien (Plattform-Abdeckung `Connectivity`/WebView) | Manuelle Verifikation (Soll) via `scripts/iOS-Deployment.ps1` | Nicht ausgeführt — kein macOS-Host verfügbar; dokumentierte Einschränkung |

## Zusammenfassung

- Gesamt: 231
- Bestanden: 231
- Fehlgeschlagen: 0
- Übersprungen: 0

Änderungen gegenüber Iteration 2 (227 → 231, +4) — Folge der Review-Befund-Fixes:

- **Neu (+4):** `UnreadViewModelTests.ConnectivityChanged_ClearsErrorMessage`, `UnreadViewModelTests.RefreshCommand_WhenSyncThrows_SetsLocalizedSyncError`, `FeedsViewModelTests.ConnectivityChanged_ClearsSyncErrorMessage`, `FeedsViewModelTests.RefreshAllCommand_WhenSyncThrows_SetsLocalizedSyncError`.
- **Umbenannt/angepasst:** `RefreshCommand_WhenOffline_SetsOfflineHintAndSkipsSync` → `RefreshCommand_WhenOffline_SkipsSyncWithoutError` (`UnreadViewModelTests`); `RefreshAllCommand_WhenOffline_SetsOfflineHintAndSkipsSync` → `RefreshAllCommand_WhenOffline_SkipsSyncWithoutError` und `RefreshCommand_WhenOffline_SetsOfflineHintAndSkipsSync` → `RefreshCommand_WhenOffline_SkipsSyncWithoutError` (`FeedsViewModelTests`) — der Offline-Frühabbruch setzt kein `ErrorMessage` mehr (sichtbarer Offline-Status läuft über `IsOnline`-`DataTrigger`/`OfflineHint`-Banner). `BaseViewModelTests_Connectivity` → `BaseViewModelConnectivityTests` (5 Fälle unverändert).

## Testabdeckung

**Abdeckung:** 89,44 % Zeilenabdeckung (Cobertura `line-rate` 0,8944; `Reporter.Core` 85,23 %, `Reporter.Data` 97,73 %; gemessen werden nur die von `Reporter.Tests` referenzierten Assemblies `Reporter.Core` und `Reporter.Data`; `Reporter.Data.Migrations.*` per `coverlet.runsettings` ausgeschlossen).

| Datei | Abdeckung |
|-------|-----------|
| `Reporter.Core\Resources\Strings\AppResources.Designer.cs` | 21,4 % (generierte ResX-Designer-Datei) |

## Fehlende Tests

Quelle: `Coverage-Daten`

- Keine Quelldatei mit 0 % Zeilenabdeckung auf Dateiebene in den gemessenen Assemblies (`Reporter.Core`, `Reporter.Data`) gefunden. `AppResources.Designer.cs` (21,4 %) ist generierter Code und wird entsprechend den Kommando-Regeln nicht als Testlücke gewertet.
- Bekannter, nicht neuer Befund: Die Methode `UnreadViewModel.LoadMoreAsync` (src/Reporter.Core/ViewModels/UnreadViewModel.cs Z. 270) liegt auf Methodenebene bei 0 % Zeilenabdeckung — identisch zu Iterationen 1 und 2, keine Regression durch die Review-Fixes. Die Datei selbst ist insgesamt abgedeckt.
- Einschränkung der Messung: Das MAUI-Projekt `src/Reporter/**` (u. a. `Views`, `ViewModels/ArticleDetailViewModel`, `Services/NetworkStatusService`, `MauiProgram`, `App`) ist von `Reporter.Tests` nicht referenziert und daher vollständig ohne Coverage-Messung — diese Bereiche sind ausschließlich über die ausstehende manuelle Verifikation (siehe E2E-Abdeckung) nachzuweisen. Bestehende, bereits im Test-Inventory dokumentierte Einschränkung, keine neue Lücke.

## Static Checks

`./scripts/Run-StaticChecks.ps1` — **Exit-Code 0**, alle Prüfungen ohne Befund:

- Format-Check (`dotnet format`): bestanden
- Security (`dotnet list package --vulnerable`): keine verwundbaren Pakete in allen 4 Projekten
- Statische Analyse (Release-Build `TreatWarningsAsErrors`, inkl. MAUI-App mit XAML-SourceGen): 0 Warnungen, 0 Fehler
