# Test-Ergebnisse

Geprüfter Stand: Branch `task/issue-28-b7b5807abfea421fba84f7ded823c28c-offline-faehigkeit-und-mehrspr`, Working Tree mit den uncommitteten Feature-Änderungen (Issue #28: Offline-Fähigkeit + Mehrsprachigkeit EN/DE).
Testumgebung: Windows, .NET SDK 10.0.401, Umgebungsvariable `IncludeIosTarget=false` (iOS-Target auf Windows deaktiviert, wie in CI und Baseline).

## Ergebnis

**Status:** Fehler vorhanden

Hinweis zum Status: Alle 208 automatisierten Tests sind bestanden (0 fehlgeschlagen). Der Status resultiert ausschließlich daraus, dass die als **Pflicht** geplanten E2E-Szenarien nicht ausgeführt wurden — es existiert keine automatisierte UI-Test-Infrastruktur; der Plan sieht dokumentierte manuelle Verifikation vor (plan.md, Abschnitt „E2E-Tests"). Diese manuellen Verifikationsnachweise sind **ausstehend** (kein Gerät-/Emulator-Zugriff in dieser Umgebung) und in der Tabelle „E2E-Abdeckung" vollständig aufgeführt — nicht als bestanden, nicht als fehlgeschlagene Tests.

## Testläufe

| Lauf | Befehl | Arbeitsverzeichnis | Exit-Code | Ergebnis | Nachweis |
|------|--------|--------------------|-----------|----------|----------|
| Restore | `dotnet restore Reporter.sln -r win-x64` (Env: `IncludeIosTarget=false`) | Repo-Root | 0 | Alle 4 Projekte wiederhergestellt | Konsolenausgabe |
| Build | `dotnet build Reporter.sln --configuration Release --no-restore` (Env: `IncludeIosTarget=false`) | Repo-Root | 0 | 0 Warnungen, 0 Fehler | Konsolenausgabe |
| Tests + Coverage | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 | 208 bestanden, 0 fehlgeschlagen, 0 übersprungen (Baseline: 189) | [03-dotnet-test.log](test-results/03-dotnet-test.log), [test-results.trx](test-results/test-results.trx), [coverage.cobertura.xml](test-results/coverage.cobertura.xml) |
| Static Checks | `.\scripts\Run-StaticChecks.ps1` | Repo-Root | 0 | Format-Check bestanden; keine verwundbaren NuGet-Pakete; statische Analyse (Release-Build mit Warnungen als Fehler, inkl. MAUI-App) ohne Befund — „Alle gewaehlten Static-Checks wurden erfolgreich ausgefuehrt." | Konsolenausgabe |

## Fehlgeschlagene Tests

Keine — alle 208 automatisierten Tests bestanden. Der Status „Fehler vorhanden" begründet sich allein aus den nicht ausgeführten Pflicht-E2E-Szenarien (siehe unten), nicht aus fehlgeschlagenen Tests.

## E2E-Abdeckung

Es existiert keine automatisierte UI-Test-Infrastruktur (`Reporter.Tests` referenziert nur `Reporter.Core`/`Reporter.Data`; kein Appium/UITest-Projekt). Die geplanten E2E-Szenarien sind daher **manuelle Verifikationsnachweise** (plan.md Z. 355–370). In dieser Umgebung nicht ausführbar (kein interaktiver App-Start, keine Netzwerk-Umschaltung, kein Gerät/Emulator) — alle Szenarien bleiben **offen** und sind nachzuholen (Windows-Target im 390 × 844-pt-Fenster; iOS-Simulator nur auf macOS-Host via `scripts/iOS-Deployment.ps1`).

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| App komplett offline (Flugmodus) starten → Ungelesen/Später/Kategorien/Feeds zeigen synchronisierte Inhalte; `ArticleCardView`-Thumbnails offline ausgeblendet; Artikeldetail lesbar | Manuelle Verifikation (Pflicht) | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Sync-Button auf Ungelesen gedimmt + `OfflineHint`; Tap und Pull-to-Refresh → lokalisierter Hinweis, kein Absturz, kein `SyncLog`-Fehler-Rauschen | Manuelle Verifikation (Pflicht); Logik-Teil abgedeckt durch `UnreadViewModelTests.RefreshCommand_WhenOffline_SetsOfflineHintAndSkipsSync` u. a. | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Links im `ArticleWebView` nicht klickbar/neutralisiert; Tap auf Rest-Links → lokalisierter Alert (`OfflineHint`/`ArticleOfflineLinksDisabled`) | Manuelle Verifikation (Pflicht); Sanitizer-Teil abgedeckt durch `ArticleHtmlSanitizerTests` | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Artikeldetail rendert ohne externe `<img>`-Bilder (Elemente entfernt, keine leeren Platzhalter); Text vollständig lesbar | Manuelle Verifikation (Pflicht); Sanitizer-Teil abgedeckt durch `ArticleHtmlSanitizerTests.Sanitize_Offline_RemovesImages` | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Online→Offline→Online-Wechsel zur Laufzeit ohne App-Neustart → Statusanzeige und Link-Verhalten folgen | Manuelle Verifikation (Pflicht); Logik-Teil abgedeckt durch `ConnectivityChanged_UpdatesIsOnline`-Tests | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Systemsprache DE → deutsche Texte; EN → englische Texte; Drittsprache (z. B. FR) → englischer Fallback (neutrales `AppResources.resx`) | Manuelle Verifikation mit Screenshots (Pflicht) | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Feeds-Seite zeigt ohne Nutzeraktion den `OfflineHint`-Banner oberhalb der Liste; Banner verschwindet nach Netzrückkehr | Manuelle Verifikation (Pflicht); Logik-Teil abgedeckt durch `FeedsViewModelTests.ConnectivityChanged_UpdatesIsOnline` | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Pull-to-Refresh und Einzel-Feed-Aktualisierung auf Feeds → lokalisierter Hinweis, kein Absturz | Manuelle Verifikation (Pflicht); Logik-Teil abgedeckt durch `FeedsViewModelTests.RefreshAllCommand_WhenOffline_SetsOfflineHintAndSkipsSync` / `RefreshCommand_WhenOffline_...` | Nicht ausgeführt — manuelle Verifikation ausstehend |
| Offline: Artikeldetail → „Im Browser öffnen" (Footer + Aktionsleisten-Icon) → lokalisierter Hinweis im `ErrorMessage`-Label statt Browser-Öffnung, kein Absturz; Offline-Label sichtbar | Manuelle Verifikation (Pflicht; `ArticleDetailViewModel` liegt im MAUI-Projekt, nicht unit-testbar) | Nicht ausgeführt — manuelle Verifikation ausstehend |
| iOS-Simulator-Lauf mit denselben Kern-Szenarien (Plattform-Abdeckung `Connectivity`/WebView) | Manuelle Verifikation (Soll) via `scripts/iOS-Deployment.ps1` | Nicht ausgeführt — kein macOS-Host verfügbar; dokumentierte Einschränkung |

## Zusammenfassung

- Gesamt: 208
- Bestanden: 208
- Fehlgeschlagen: 0
- Übersprungen: 0

Neue Tests gegenüber Baseline (189): `UnreadViewModelTests` (3), `FeedsViewModelTests` (3), `LaterViewModelTests` (1), `AutoRefreshServiceTests` (2: Offline-Tick ohne Sync, Wiederaufnahme nach Online), `FeedSyncServiceTests` (2: `SyncAllAsync`/`SyncFeedAsync` offline ohne SyncLog-/Health-/HTTP-Zugriff), `ArticleHtmlSanitizerTests` (8 Fälle). Neue Test-Hilfsklasse: `FakeNetworkStatusService`.

## Testabdeckung

**Abdeckung:** 89,08 % Zeilenabdeckung (Cobertura `line-rate` 0,8908; gemessen werden nur die von `Reporter.Tests` referenzierten Assemblies `Reporter.Core` und `Reporter.Data`; `Reporter.Data.Migrations.*` per `coverlet.runsettings` ausgeschlossen).

| Datei | Abdeckung |
|-------|-----------|
| `Reporter.Core\Resources\Strings\AppResources.Designer.cs` | 21,0 % (generierte ResX-Designer-Datei) |

## Fehlende Tests

Quelle: `Coverage-Daten`

- Keine Quelldatei mit 0 % Zeilenabdeckung in den gemessenen Assemblies (`Reporter.Core`, `Reporter.Data`) gefunden. `AppResources.Designer.cs` (21 %) ist generierter Code und wird entsprechend den Kommando-Regeln nicht als Testlücke gewertet.
- Einschränkung der Messung: Das MAUI-Projekt `src/Reporter/**` (u. a. `Views`, `ViewModels/ArticleDetailViewModel`, `Services/NetworkStatusService`, `MauiProgram`, `App`) ist von `Reporter.Tests` nicht referenziert und daher vollständig ohne Coverage-Messung — diese Bereiche sind ausschließlich über die ausstehende manuelle Verifikation (siehe E2E-Abdeckung) nachzuweisen. Bestehende, bereits im Test-Inventory dokumentierte Einschränkung, keine neue Lücke.

## Static Checks

`.\scripts\Run-StaticChecks.ps1` — **Exit-Code 0**, alle Prüfungen ohne Befund:

- Format-Check (`dotnet format`): bestanden
- Security (`dotnet list package --vulnerable`): keine verwundbaren Pakete in allen 4 Projekten
- Statische Analyse (Release-Build `TreatWarningsAsErrors`, inkl. MAUI-App mit XAML-SourceGen): 0 Warnungen, 0 Fehler
