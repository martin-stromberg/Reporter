# Test- und Verifikationsergebnisse

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
