# Test-Ergebnisse

Geprüfter Stand: Branch `task/issue-28-b7b5807abfea421fba84f7ded823c28c-offline-faehigkeit-und-mehrspr`, Working Tree mit den uncommitteten Feature-Änderungen (Issue #28: Offline-Fähigkeit + Mehrsprachigkeit EN/DE) — **2. Iteration**, inkl. der Fixes aus dem Code-/Usability-Review.
Testumgebung: Windows, .NET SDK, Umgebungsvariable `IncludeIosTarget=false` (iOS-Target auf Windows deaktiviert, wie in CI und Baseline).
Vorheriger Lauf: `test-results.1.md` (Iteration 1, 208 Tests) — Logs dieser Iteration unter `test-results/run2/`.

## Ergebnis

**Status:** Fehler vorhanden

Hinweis zum Status: Alle 227 automatisierten Tests sind bestanden (0 fehlgeschlagen). Der Status resultiert ausschließlich daraus, dass die als **Pflicht** geplanten E2E-Szenarien nicht ausgeführt wurden — es existiert keine automatisierte UI-Test-Infrastruktur; der Plan sieht dokumentierte manuelle Verifikation vor (plan.md, Abschnitt „E2E-Tests", Z. 355–370). Diese manuellen Verifikationsnachweise sind **ausstehend** (kein Gerät-/Emulator-Zugriff, keine Netzwerk-Umschaltung und kein interaktiver App-Start in dieser Umgebung) und in der Tabelle „E2E-Abdeckung" vollständig aufgeführt — nicht als bestanden, nicht als fehlgeschlagene Tests.

## Testläufe

| Lauf | Befehl | Arbeitsverzeichnis | Exit-Code | Ergebnis | Nachweis |
|------|--------|--------------------|-----------|----------|----------|
| Restore | `dotnet restore Reporter.sln -r win-x64` (Env: `IncludeIosTarget=false`) | Repo-Root | 0 | Alle 4 Projekte wiederhergestellt | [01-dotnet-restore.log](test-results/run2/01-dotnet-restore.log) |
| Build | `dotnet build Reporter.sln --configuration Release --no-restore` (Env: `IncludeIosTarget=false`) | Repo-Root | 0 | 0 Warnungen, 0 Fehler | [02-dotnet-build.log](test-results/run2/02-dotnet-build.log) |
| Tests + Coverage | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 | 227 bestanden, 0 fehlgeschlagen, 0 übersprungen (Baseline: 189; Iteration 1: 208) | [03-dotnet-test.log](test-results/run2/03-dotnet-test.log), [test-results.trx](test-results/run2/test-results.trx), [coverage.cobertura.xml](test-results/run2/coverage.cobertura.xml) |
| Static Checks | `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Run-StaticChecks.ps1` | Repo-Root | 0 | Format-Check bestanden; keine verwundbaren NuGet-Pakete; statische Analyse (Release-Build mit Warnungen als Fehler, inkl. MAUI-App) ohne Befund — „Alle gewaehlten Static-Checks wurden erfolgreich ausgefuehrt." | [04-static-checks.log](test-results/run2/04-static-checks.log) |

## Fehlgeschlagene Tests

Keine — alle 227 automatisierten Tests bestanden. Der Status „Fehler vorhanden" begründet sich allein aus den nicht ausgeführten Pflicht-E2E-Szenarien (siehe unten), nicht aus fehlgeschlagenen Tests.

## E2E-Abdeckung

Es existiert keine automatisierte UI-Test-Infrastruktur (`Reporter.Tests` referenziert nur `Reporter.Core`/`Reporter.Data`; kein Appium/UITest-Projekt). Die geplanten E2E-Szenarien sind daher **manuelle Verifikationsnachweise** (plan.md Z. 355–370, plan-check.md Abschnitt „E2E-Abdeckung": alle als „Abgedeckt" geplant). In dieser Umgebung nicht ausführbar — alle Szenarien bleiben **offen** und sind nachzuholen (Windows-Target im 390 × 844-pt-Fenster; iOS-Simulator nur auf macOS-Host via `scripts/iOS-Deployment.ps1`).

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| App komplett offline (Flugmodus) starten → Ungelesen/Später/Kategorien/Feeds zeigen synchronisierte Inhalte; `ArticleCardView`-Thumbnails offline ausgeblendet; Artikeldetail lesbar | Manuelle Verifikation (Pflicht) | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Sync-Button auf Ungelesen gedimmt + `OfflineHint`; Tap und Pull-to-Refresh → lokalisierter Hinweis, kein Absturz, kein `SyncLog`-Fehler-Rauschen | Manuelle Verifikation (Pflicht); Logik-Teil abgedeckt durch `UnreadViewModelTests.RefreshCommand_WhenOffline_SetsOfflineHintAndSkipsSync` u. a. | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Links im `ArticleWebView` nicht klickbar/neutralisiert; Tap auf Rest-Links → lokalisierter Alert (`OfflineHint`/`ArticleOfflineLinksDisabled`) | Manuelle Verifikation (Pflicht); Logik-Teil abgedeckt durch `ArticleHtmlSanitizerTests` + `WebViewNavigationGuardTests` (neu, 10 Fälle) | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Artikeldetail rendert ohne externe `<img>`-Bilder (Elemente entfernt, keine leeren Platzhalter); Text vollständig lesbar | Manuelle Verifikation (Pflicht); Sanitizer-Teil abgedeckt durch `ArticleHtmlSanitizerTests.Sanitize_Offline_RemovesImages` u. a. | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Online→Offline→Online-Wechsel zur Laufzeit ohne App-Neustart → Statusanzeige und Link-Verhalten folgen | Manuelle Verifikation (Pflicht); Logik-Teil abgedeckt durch `BaseViewModelTests_Connectivity` (neu, 5 Fälle: `ConnectivityChanged_UpdatesIsOnline` u. a.) | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Systemsprache DE → deutsche Texte; EN → englische Texte; Drittsprache (z. B. FR) → englischer Fallback (neutrales `AppResources.resx`) | Manuelle Verifikation mit Screenshots (Pflicht) | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Feeds-Seite zeigt ohne Nutzeraktion den `OfflineHint`-Banner oberhalb der Liste; Banner verschwindet nach Netzrückkehr | Manuelle Verifikation (Pflicht); Logik-Teil abgedeckt durch `FeedsViewModelTests.ConnectivityChanged_UpdatesIsOnline` | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Pull-to-Refresh und Einzel-Feed-Aktualisierung auf Feeds → lokalisierter Hinweis, kein Absturz | Manuelle Verifikation (Pflicht); Logik-Teil abgedeckt durch `FeedsViewModelTests.RefreshAllCommand_WhenOffline_SetsOfflineHintAndSkipsSync` / `RefreshCommand_WhenOffline_...` | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Artikeldetail → „Im Browser öffnen" (Footer + Aktionsleisten-Icon) → lokalisierter Hinweis im `ErrorMessage`-Label statt Browser-Öffnung, kein Absturz; `ArticleOfflineLinksDisabled`-Offline-Label sichtbar | Manuelle Verifikation (Pflicht; `ArticleDetailViewModel` liegt im MAUI-Projekt, nicht unit-testbar) | Nicht ausgeführt — manuelle Verifikation ausstehend |
| iOS-Simulator-Lauf mit denselben Kern-Szenarien (Plattform-Abdeckung `Connectivity`/WebView) | Manuelle Verifikation (Soll) via `scripts/iOS-Deployment.ps1` | Nicht ausgeführt — kein macOS-Host verfügbar; dokumentierte Einschränkung |

## Zusammenfassung

- Gesamt: 227
- Bestanden: 227
- Fehlgeschlagen: 0
- Übersprungen: 0

Neue Tests gegenüber Iteration 1 (208 → 227, +19): `WebViewNavigationGuardTests` (neu, 10 Fälle — Offline-Link-Neutralisierung/Navigations-Guard), `BaseViewModelTests_Connectivity` (neu, 5 Fälle — `TrackConnectivity`/`ConnectivityChanged`-Propagation), `ArticleHtmlSanitizerTests` (8 → 12, +4 Fälle). Gegenüber Baseline (189 → 227, +38): zusätzlich `UnreadViewModelTests`, `FeedsViewModelTests`, `LaterViewModelTests`, `AutoRefreshServiceTests` (Offline-Tick ohne Sync, Wiederaufnahme nach Online), `FeedSyncServiceTests` (`SyncAllAsync`/`SyncFeedAsync` offline ohne SyncLog-/Health-/HTTP-Zugriff). Test-Hilfsklasse: `FakeNetworkStatusService`.

## Testabdeckung

**Abdeckung:** 89,01 % Zeilenabdeckung (Cobertura `line-rate` 0,8901; gemessen werden nur die von `Reporter.Tests` referenzierten Assemblies `Reporter.Core` und `Reporter.Data`; `Reporter.Data.Migrations.*` per `coverlet.runsettings` ausgeschlossen).

| Datei | Abdeckung |
|-------|-----------|
| `Reporter.Core\Resources\Strings\AppResources.Designer.cs` | 20,9 % (generierte ResX-Designer-Datei) |

## Fehlende Tests

Quelle: `Coverage-Daten`

- Keine Quelldatei mit 0 % Zeilenabdeckung in den gemessenen Assemblies (`Reporter.Core`, `Reporter.Data`) gefunden. `AppResources.Designer.cs` (20,9 %) ist generierter Code und wird entsprechend den Kommando-Regeln nicht als Testlücke gewertet.
- Bekannter, nicht neuer Befund: Die Methode `UnreadViewModel.LoadMoreAsync` (src/Reporter.Core/ViewModels/UnreadViewModel.cs Z. 269) liegt auf Methodenebene bei 0 % Zeilenabdeckung — identisch zu Iteration 1, keine Regression durch die Review-Fixes. Die Datei selbst ist insgesamt abgedeckt.
- Einschränkung der Messung: Das MAUI-Projekt `src/Reporter/**` (u. a. `Views`, `ViewModels/ArticleDetailViewModel`, `Services/NetworkStatusService`, `MauiProgram`, `App`) ist von `Reporter.Tests` nicht referenziert und daher vollständig ohne Coverage-Messung — diese Bereiche sind ausschließlich über die ausstehende manuelle Verifikation (siehe E2E-Abdeckung) nachzuweisen. Bestehende, bereits im Test-Inventory dokumentierte Einschränkung, keine neue Lücke.

## Static Checks

`.\scripts\Run-StaticChecks.ps1` — **Exit-Code 0**, alle Prüfungen ohne Befund:

- Format-Check (`dotnet format`): bestanden
- Security (`dotnet list package --vulnerable`): keine verwundbaren Pakete in allen 4 Projekten
- Statische Analyse (Release-Build `TreatWarningsAsErrors`, inkl. MAUI-App mit XAML-SourceGen): 0 Warnungen, 0 Fehler
