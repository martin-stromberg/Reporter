# Test- und Verifikationsergebnisse

## Issue #26: Einstellungen, Aufbewahrungsdauer, Keyword-Filter und Löschlogik

Branch: `task/issue-26-a32dbfb7fca140d88d166629e421fbe8-einstellungen-aufbewahrungsdau`

### Build und Tests

| Lauf | Befehl | Ergebnis |
|------|--------|----------|
| Build (Debug, MAUI-App Windows) | `IncludeIosTarget=false dotnet build src/Reporter/Reporter.csproj -f net10.0-windows10.0.19041.0` | Erfolgreich, 0 Warnungen, 0 Fehler |
| Tests (wie CI: coverlet.runsettings + TRX) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx" --logger "console;verbosity=normal"` | 128 bestanden, 0 fehlgeschlagen, 0 übersprungen (Baseline: 90) |
| Static Checks | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Security, Static-Analysis-Release-Build inkl. MAUI-App ohne Befund) |
| Build (Iteration 2, nach Review-Fixes) | `dotnet build Reporter.sln` | Erfolgreich, 0 Warnungen, 0 Fehler |
| Tests (Iteration 2, nach Review-Fixes) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release` | 132 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| Static Checks (Iteration 2, nach Review-Fixes) | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Security, Static Analysis ohne Befund) |

Neue Tests (38): `SettingsViewModelTests_Load` (2), `SettingsViewModelTests_Persist` (5 Fälle inkl. Theory),
`SettingsViewModelTests_Keywords` (5), `SettingsViewModelTests_E2E` (3), `KeywordMatcherTests` (7),
`AutoRefreshServiceTests` (7 Fälle inkl. Theory, `FakeTimeProvider`), `RetentionCleanupServiceTests` (3 neue),
`ItemRepositoryTests` (5 neue), `SettingsRepositoryTests.SaveAsync_PersistsNewFields`.
Neue Hilfsklassen: `FakeFeedSyncService`, `FakeAutoRefreshService`, `FakeAppThemeService`, `TestWaitHelper`;
NuGet `Microsoft.Extensions.TimeProvider.Testing` im Testprojekt ergänzt.

### Mobile-UI-Design-Review „Einstellungen"

Statische XAML-Prüfung von `SettingsPage.xaml` gegen
`design-draft/stitch_local_rss_feed_reader/einstellungen_filter/` (Light) und
`..._dark_mode/` sowie die AGENTS.md-Regeln:

- [x] Fünf Sektions-Karten (`Border` + `RoundRectangle 12`, `AppThemeBinding` `SurfaceContainer`)
  im `ScrollView` unter `Grid RowDefinitions="Auto,*"` — Formularseite, kein `CollectionView`,
  keine Verschachtelung von `ScrollView`/`CollectionView`
- [x] Slider 1–365 mit `DragCompletedCommand` (Persistierung erst nach Drag), Skalen-Label und
  Invarianten-Hinweis gemäß Entwurf
- [x] Keyword-Eingabe (`Entry` + `ReturnCommand` + „+ Hinzufügen"-`Button`), Chips via
  `FlexLayout Wrap="Wrap"` + `BindableLayout` mit ×-`Button` (44 × 44 pt)
- [x] Fest aktivierter, deaktivierter `Switch` für „Teilwort & Case-Insensitive" (feste Semantik)
- [x] Optionszeilen (Abruf-Intervall, Verzögerung, Ruhezeiten) werden per `IsEnabled`-Binding +
  `DataTrigger` (`Opacity` 0,4) ausgegraut, wenn der zugehörige Toggle aus ist
- [x] `TimePicker` ×2 (VON/BIS) für Ruhezeiten; Theme-`Picker` (System/Hell/Dunkel)
- [x] Touch-Ziele ≥ 44 pt (Slider `MinimumHeightRequest`, Chips, Buttons via globalem Style)
- [x] Dark Mode ausschließlich über `AppThemeBinding`; alle Texte aus `AppResources.*`
- [x] `Shell.NavBarIsVisible="False"`, Header-`Label` mit `HeadlineStyle`

Bewusste Scope-Abweichungen zum Design-Entwurf (laut Plan nicht umgesetzt):
„Lokalen Cache leeren", Sektion „Datenbank & Datensicherung" (OPML-Export, JSON-Backup,
DB-Größenanzeige), Ruhezeiten-Status-Badge „Aktiv" und der „Änderungen gespeichert"-Toast.

### Manuelle UI-Verifikation (Iteration 2, durchgeführt)

Die App wurde auf dem Windows-Target im 390 × 844-pt-Fenster gestartet (unpackaged
`win-x64`-Build, Fenstergröße via `App.CreateWindow` vorgegeben und per
`GetWindowRect` verifiziert). Interaktion über UI Automation + `mouse_event`;
Zustandsnachweise zusätzlich in der SQLite-Datenbank geprüft. Screenshots unter
`test-results/issue-26-manual-*.png`:

- [x] App-Start: `UnreadPage` rendert mit TabBar (`manual-01`); bei 390 pt Breite
  wandern „Kategorien"/„Einstellungen" korrekt ins „Mehr"-Overflow-Menü (`manual-02`)
- [x] `SettingsPage` Dark Mode: alle fünf Sektions-Karten sichtbar, Chips der
  bestehenden Keywords, deaktivierter Match-`Switch`, deutsche Texte
  (`manual-03` oben, `manual-04` unten — Windows-System ist dunkel, Theme „System")
- [x] Theme-Umschaltung per Picker „Farbschema": „Dunkel" → `settings.theme='dark'`
  in der DB; „Hell" → `settings.theme='light'` und sichtbar helles Rendering
  (`manual-05`/`manual-06` unten/oben, `manual-07` Artikeldetail)
- [x] Verzögerung-„Sofort"-Fall: Picker „Verzögerung bis Markierung" auf „Sofort" →
  `auto_mark_read_delay_seconds=0` persistiert; Öffnen eines Artikels markierte ihn
  sofort als gelesen (`is_read` 109 → 108, `read_at` gesetzt)
- [x] Touch-Ziele und Karten-Layout im 390 × 844-Fenster ohne horizontale Tabellen;
  Sektionen via `ScrollView` scrollbar

Nach der Verifikation wurden die Test-Änderungen zurückgesetzt
(`theme='system'`, `auto_mark_read_delay_seconds=5`, Artikel wieder ungelesen).

## Issue #25: „Für später bewahren"-Funktion und separate Ansicht

Branch: `task/issue-25-b53a9e77d8c844c88716721a9032666b-fuer-spaeter-bewahren-funktion`

### Build und Tests

| Lauf | Befehl | Ergebnis |
|------|--------|----------|
| Build (Release, Solution via Static Checks) | `dotnet build Reporter.sln --configuration Release -p:TreatWarningsAsErrors=true` | Erfolgreich, 0 Warnungen, 0 Fehler |
| Tests | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx"` | 88 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| Tests (Iteration 2, nach Review-Fixes, wie CI: coverlet.runsettings + TRX) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx" --logger "console;verbosity=normal"` | 90 bestanden, 0 fehlgeschlagen, 0 übersprungen; Abdeckung 84,17 % Zeilen (≥ 70 %) |
| Static Checks | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Security, Static Analysis ohne Befund) |
| Static Checks (Iteration 2, nach Review-Fixes) | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Security, Static Analysis ohne Befund) |

Neue Tests (20): `LaterViewModelTests` (5), `RetentionCleanupServiceTests` (5 Fälle inkl. Theory),
`ItemRepositoryTests` (8 neue Methoden), `UnreadViewModelTests.ToggleSavedCommand_TogglesFlagInPlace`,
`FeedRepositoryTests.DeleteAsync_CascadeDeletesSavedItems`.

Iteration 2 (Review-Nachbearbeitung): `App.OnStart` protokolliert Cleanup-Fehler jetzt per
`Debug.WriteLine`; das `CancellationToken` von `CleanupAsync` wird durchgereicht
(`ISettingsRepository.GetAsync` und `IItemRepository.DeleteExpiredAsync` um
`CancellationToken`-Parameter erweitert); die dreifach duplizierte `SeedFeedAsync`-Hilfsmethode
wurde als `TestDataSeeder.SeedFeedAsync(TestDbContextFactory)` extrahiert. Zwei neue
Regressionstests (`CleanupAsync_CancelledToken_ThrowsOperationCanceled`,
`DeleteExpiredAsync_CancelledToken_ThrowsOperationCanceled`).

### Mobile-UI-Design-Review „Später"-Ansicht

Statische XAML-Prüfung von `LaterPage.xaml`, `ArticleCardView.xaml` und `ArticleDetailPage.xaml`
gegen `design-draft/stitch_local_rss_feed_reader/f_r_sp_ter_bewahren/` (Light) und
`..._dark_mode/` sowie die AGENTS.md-Regeln:

- [x] Karten-`CollectionView`, keine horizontalen Tabellen; Aktionen über `TapGestureRecognizer`
- [x] `CollectionView` füllt `Grid`-Row `*` (`RowDefinitions="Auto,*"`); kein verschachteltes Scrollen
- [x] Touch-Ziele ≥ 44 × 44 pt (Bookmark- und Gelesen-`Border` in `ArticleCardView`,
  Bottom-Bar-`Border` in `ArticleDetailPage`)
- [x] Dark Mode über `AppThemeBinding` (Karten, Icons, Gold-Fill `LightBookmarkGold`/`DarkBookmarkGold`)
- [x] Inhaltlicher Header (`HeadlineStyle`), `Shell.NavBarIsVisible="False"`, `EmptyView` = `PlaceholderLater`
- [x] „Lesezeichen entfernen"-Aktion des Design-Entwurfs ist über das Bookmark-Icon
  (`ToggleSavedCommand`) abgebildet; `DataTrigger` füllt das Icon bei `IsSavedForLater`

Bekannte Abweichungen zum Design-Entwurf (nicht Teil des Plans, zur Entscheidung offen):
Info-Banner „Dauerhafte Archivierung", Suchfeld, Kategorie-Chips und die Karten-Aktionspills
„Ungelesen"/„Teilen" sind im Entwurf enthalten, aber in der implementierten `LaterPage` nicht
vorhanden.

### Manuelle UI-Verifikation (Iteration 2, durchgeführt)

Die App wurde auf dem Windows-Target im 390 × 844-pt-Fenster gestartet und die vier
Pflicht-Szenarien interaktiv durchgeführt (Screenshots unter
`docs/help/anwendung/screenshots/issue-25/manual-*.png`):

- [x] Bewahren auf `UnreadPage`: Tap auf das Bookmark-Icon füllt es gold; der Artikel
  erscheint im Tab „Später" (`manual-02`, `manual-03`)
- [x] Entfernen auf `LaterPage`: Bookmark-Tap entfernt den Artikel; bei leerer Liste
  erscheint der `EmptyView`-Platzhalter (`manual-04`, `manual-05`)
- [x] Bookmark-Toggle auf `ArticleDetailPage`: Bottom-Bar-Icon wechselt zwischen
  Outline und Gold-Fill, DB-Flag toggelt entsprechend (`manual-06`, `manual-07`)
- [x] `LaterPage` in Light und Dark im 390 × 844-pt-Fenster verifiziert
  (`manual-03` Dark, `manual-08` Light)

Zustandsnachweise wurden zusätzlich direkt in der SQLite-Datenbank geprüft; die für die
Verifikation geänderten Flags wurden anschließend zurückgesetzt.
