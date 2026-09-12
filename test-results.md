<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test- und Verifikationsergebnisse

## Issue #57: PolyForm Noncommercial License 1.0.0

Branch: `task/issue-57-08ef43ab80c44d8cabda37af9d9cb29a-polyform-noncommercial-license`

### Verifikation

| Lauf | Befehl | Ergebnis |
|------|--------|----------|
| Static Checks | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Security-Scan, Release-Build mit `TreatWarningsAsErrors` — 0 Warnungen, 0 Fehler) |
| Node-Tests | `npm test` | 36 bestanden, 0 fehlgeschlagen |
| .NET-Tests | `dotnet test Reporter.sln` | 239 bestanden, 0 fehlgeschlagen |
| Lizenzheader-Abdeckung | `node scripts/add-license-headers.mjs --check` | 0 Dateien ohne Lizenzheader |
| Lizenzheader-Staged-Modus | `node scripts/add-license-headers.mjs --staged` | neue Datei ohne Header → Exit 1; nach Fix → Exit 0 |

Automatisierung für neue Dateien: `pre-commit`-Hook (`--staged`), CI-Step „License header check" im `static checks`-Job (`pr-staging-ci.yml`, `staging-ci.yml`) und `Run-StaticChecks.ps1 -Check LicenseHeaders`.

### Konsistenzprüfung (gemäß Anforderung 10)

- [x] `LICENSE` im Root vorhanden, unveränderter Volltext der PolyForm Noncommercial License 1.0.0 (plus `Required Notice:`-Zeile und klar abgetrenntem Hinweis auf kommerzielle Lizenzanfragen — keine zusätzlichen Bedingungen)
- [x] README enthält Lizenzangabe, Erlaubnis für private/nicht-kommerzielle Nutzung, Verbot kommerzieller Nutzung, Kontaktadresse und Lizenz-FAQ (private vs. kommerzielle Nutzung, kommerzielle Lizenzierung, erlaubt/verboten)
- [x] Quellcode enthält Lizenzheader (alle kommentierfähigen Dateien: `.cs`, `.xaml`, `.csproj`, `.resx`, `.xml`, `.plist`, `.yml`, `.ps1`, `.py`, `.mjs`, `.md` u. a.; Binärdateien, `.sln` und JSON-Dateien ohne Kommentarsyntax ausgenommen — `package.json` trägt das `license`-Feld)
- [x] Keine widersprüchlichen Lizenzangaben: `package.json`/`package-lock.json` = `PolyForm-Noncommercial-1.0.0`, `Directory.Build.props` = `PackageLicenseExpression` `PolyForm-Noncommercial-1.0.0`
- [x] Contributions: `CONTRIBUTING.md` + `.github/pull_request_template.md` mit Bestätigungs-Checkbox
- [x] Kommerzielle Lizenzierung: `COMMERCIAL-LICENSE.md` + README-Hinweis; gilt nicht automatisch, individuell vereinbart
- [x] Repository-Metadaten: GitHub-Topics `polyform-noncommercial`, `noncommercial-license` gesetzt; Lizenz-Anzeige im About-Bereich ergibt sich automatisch aus der `LICENSE`-Datei
- [x] Abhängigkeiten: NuGet-/npm-Pakete permissiv lizenziert (MIT/Apache-2.0 o. ä.), keine restriktiven/inkompatiblen Lizenzen; Security-Scan ohne Befund

Keine UI-Änderung — Mobile-UI-Design-Review nicht erforderlich.

## Issue #28: Offline-Fähigkeit und Mehrsprachigkeit (EN/DE)

Branch: `task/issue-28-b7b5807abfea421fba84f7ded823c28c-offline-faehigkeit-und-mehrspr`

### Build und Tests

| Lauf | Befehl | Ergebnis |
|------|--------|----------|
| Tests | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release` | 208 bestanden, 0 fehlgeschlagen, 0 übersprungen (Baseline: 189) |
| Static Checks | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Security, Static-Analysis-Release-Build inkl. MAUI-App ohne Befund) |
| Lokalisierungs-Check | `python .githooks/translation-check.py --all` | OK: 0 fehlende Schlüssel, 2 ResX-Pakete konsistent |

Neue Tests (19): `UnreadViewModelTests` (3: Offline-Hint + Sync-Skip,
`ConnectivityChanged` → `IsOnline`, Sync läuft wieder nach Online),
`FeedsViewModelTests` (3: RefreshAll/Refresh offline, `ConnectivityChanged`),
`LaterViewModelTests` (1: `ConnectivityChanged`), `AutoRefreshServiceTests` (2:
Offline-Tick ohne Sync, Wiederaufnahme nach Online), `FeedSyncServiceTests` (2:
`SyncAllAsync`/`SyncFeedAsync` offline ohne SyncLog/Health-/HTTP-Zugriff),
`ArticleHtmlSanitizerTests` (8 Fälle: Script/Event-Handler-Entfernung online +
offline, Online erhält `<a>`/`<img>`, Offline neutralisiert `<a>` und entfernt
`<img>`, null/whitespace-Randfälle).
Neue Hilfsklasse: `FakeNetworkStatusService`.

### Mobile-UI-Design-Review „Offline-Indikatoren"

Statische XAML-Prüfung der Änderungen an `UnreadPage.xaml`, `FeedsPage.xaml`,
`ArticleDetailPage.xaml`, `ArticleCardView.xaml` und `LaterPage.xaml` gegen die
AGENTS.md-Regeln:

- [x] Offline-Banner (`Border` + `RoundRectangle 8`, `AppThemeBinding`
  `SurfaceSubtle`/`TextSecondary`) auf `FeedsPage` und im Artikeldetail werden per
  `DataTrigger` (`IsOnline == false`) ein-/ausgeblendet — kein horizontales Layout,
  keine neuen Text-Button-Reihen
- [x] `UnreadPage`: Sync-Button wird offline per `DataTrigger` auf `Opacity` 0,4
  gedimmt; `OfflineHint`-Label und `ErrorMessage`-Label ergänzt; `CollectionView`
  unverändert in `Grid`-Row `*`, keine `ScrollView`/`CollectionView`-Verschachtelung
- [x] `ArticleCardView`: neues `IsOnline`-`BindableProperty` (Default `true`),
  Thumbnail-`Border` wird offline per `DataTrigger` ausgeblendet; die restliche
  Kartenstruktur bleibt unverändert
- [x] `ArticleDetailPage`: neue Grid-Row für Offline-/Fehlerhinweis oberhalb des
  `WebView`; `Navigating`-Handler bricht Navigation offline ab; alle neuen Texte
  aus `AppResources.*` (EN + DE); `SemanticProperties.Description` für die
  Bottom-Bar-Aktionen lokalisiert
- [x] Dark Mode ausschließlich über `AppThemeBinding`; Touch-Ziele unverändert
  ≥ 44 × 44 pt

### Manuelle UI-Verifikation (ausstehend — nicht in dieser Umgebung durchführbar)

Eine interaktive Verifikation im 390 × 844-pt-Fenster war in dieser Sitzung nicht
möglich (kein interaktiver App-Start/keine Netzwerk-Umschaltung). Die folgenden
Szenarien aus `plan.md` sind manuell nachzuholen — insbesondere das Umschalten
Online → Offline → Online zur Laufzeit (z. B. WLAN/Flugmodus bzw. Windows-
Netzwerkadapter deaktivieren):

- [ ] Offline-Start: App zeigt lokal gespeicherte Artikel, Feeds, Kategorien und
  Metadaten ohne Fehler
- [ ] `UnreadPage`: Sync-Button sichtbar gedimmt; Refresh-Button und
  Pull-to-Refresh zeigen den lokalisierten `OfflineHint`, kein SyncLog-Rauschen
- [ ] `FeedsPage`: persistentes Offline-Banner unterhalb der Eingabe-Karte;
  „Alle aktualisieren" und Einzel-Refresh zeigen `OfflineHint`
- [ ] `ArticleDetailPage` offline: Links im WebView nicht klickbar/neutralisiert
  bzw. lokalisierter Alert (`OfflineHint` + `ArticleOfflineLinksDisabled` +
  `ButtonOk`); externe `<img>`-Bilder entfernt, Text bleibt lesbar
- [ ] Laufzeit-Wechsel Online → Offline → Online: `IsOnline`-Triggers aktualisieren
  Banner, Thumbnails und Artikel-HTML ohne Neustart
- [ ] „Im Browser öffnen" offline: lokalisierter Hinweis statt Browser-Start
- [ ] `LaterPage`/`UnreadPage`: Artikel-Thumbnails offline ausgeblendet
- [ ] Sprachen: System auf Deutsch → deutsche Texte; System auf Englisch →
  englische Texte; Drittsprache (z. B. Französisch) → englischer Fallback
  (neutrales `AppResources.resx`)

Die ViewModel-/Service-Logik hinter allen Szenarien ist durch die neuen
Unit-Tests abgedeckt; die XAML-Trigger wurden statisch geprüft und der
Release-Build (XAML-SourceGen, Warnungen als Fehler) ist ohne Befund.

## Issue #27: Lokale iOS-Benachrichtigungen

Branch: `task/issue-27-701ef16d7c03422499bad991054d7310-lokale-ios-benachrichtigungen`

### Build und Tests

| Lauf | Befehl | Ergebnis |
|------|--------|----------|
| Build (Release, Solution) | `dotnet build Reporter.sln -c Release -p:IncludeIosTarget=false` | Erfolgreich, 0 Warnungen, 0 Fehler |
| Tests | `dotnet test src/Reporter.Tests -c Release --no-build` | 176 bestanden, 0 fehlgeschlagen, 0 übersprungen (Baseline: 147) |
| Static Checks | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Security, Static-Analysis-Release-Build ohne Befund) |

Neue Tests (29): `NotificationServiceTests` (11 Fälle: globaler/Feed-Schalter, Ruhezeiten inkl.
Mitternachts-Wraparound, `Start == End`, einseitig `null`, Keyword-Filter auf Titel und
`ContentHtml`, Einzel-Identifier, Sammel-Modus, stabile Summary-ID, gefilterte Items in der
Summary), `FeedSyncServiceTests` (5 neue: Notify bei neuen Items über die echte
Entscheidungskette, Sammel-Modus, keine neuen Items, Feed deaktiviert, Notify-Ausnahme
verändert Sync-Ergebnis nicht), `FeedsViewModelTests` (4 neue: Edit lädt Flag, neuer Feed
persistiert `false`, Update persistiert Flag, Reset nach Speichern),
`FeedRepositoryTests` (`UpdateAsync` assertiert `NotificationsEnabled`,
`GetAllWithDetailsAsync_ProjectsNotificationsEnabled`), `SettingsRepositoryTests`
(`SaveAsync_PersistsNotificationSummaryEnabled` Roundtrip beider Werte),
`SettingsViewModelTests_Load` (`Load_PopulatesNotificationSummaryEnabled`),
`SettingsViewModelTests_Persist` (`NotificationSummaryEnabled_Change_Persists`),
`SettingsViewModelTests_E2E` (`E2E_NotificationSummary_PersistRoundtrip` und Roundtrip-Erweiterung).
Neue Hilfsklassen: `FakeLocalNotificationService`, `FakeNotificationService`.

### Mobile-UI-Design-Review „Feeds" und „Einstellungen"

Statische XAML-Prüfung der Änderungen an `FeedsPage.xaml` (Feed-Formular) und
`SettingsPage.xaml` (Karte „Benachrichtigungen & Ruhezeiten") gegen die AGENTS.md-Regeln:

- [x] Feed-Formular: `Grid ColumnDefinitions="*,Auto"` mit Label + Hinweis links und
  `Switch` rechts (`MinimumWidthRequest/MinimumHeightRequest="44"`,
  `SemanticProperties.Description` für Accessibility)
- [x] Einstellungen: „Sammel-Benachrichtigung" liegt in der bestehenden Optionsgruppe und
  wird mit den übrigen Zeilen per `IsEnabled="{Binding NotificationsEnabled}"` +
  `DataTrigger` (`Opacity` 0,4) ausgegraut, wenn Push aus ist
- [x] Keine horizontalen Tabellen, keine mehreren Text-Buttons in einer Zeile;
  Seitenstruktur unverändert (`Grid Auto,*`, Formular-`ScrollView`, `CollectionView`
  ohne Verschachtelung)
- [x] Dark Mode ausschließlich über `AppThemeBinding`; alle neuen Texte aus
  `AppResources.*` (EN + DE)

### Manuelle UI-Verifikation (durchgeführt)

Die App wurde auf dem Windows-Target im 390 × 844-pt-Fenster gestartet (unpackaged
`win-x64`-Release-Build, Fenstergröße via `App.CreateWindow`, per `GetWindowRect`
verifiziert: 390 × 844). Interaktion über UI Automation + `mouse_event`;
Schalter-Zustände per `TogglePattern` gelesen. Screenshots unter
`test-results/issue-27/manual-*.png`:

- [x] `FeedsPage`: Feed-Formular zeigt Switch „Benachrichtigungen" mit Hinweistext
  „Bei neuen Artikeln dieses Feeds benachrichtigen" oberhalb von „Speichern"
  (`manual-02`); Tab-Leiste „Ungelesen/Feeds/Später" + „Mehr" unverändert (`manual-01`)
- [x] `FeedsPage` Edit-Flow: Feed antippen → ActionSheet „Feed-Aktionen" → „Bearbeiten"
  lädt den Flag in den Switch (Heise: On = Migrations-Default `true`)
- [x] Per-Feed-Persistenz: Switch Off → „Speichern" → erneutes „Bearbeiten" zeigt Off;
  danach wieder On gespeichert (Ausgangszustand wiederhergestellt)
- [x] `SettingsPage`: Karte „Benachrichtigungen & Ruhezeiten" zeigt neue Zeile
  „Sammel-Benachrichtigung" mit Hinweis „Eine Benachrichtigung pro Feed statt pro Artikel"
  zwischen „Push-Benachrichtigungen" und „Ruhezeit (Nicht stören)"
  (`manual-04`, `manual-07`)
- [x] Globaler Summary-Schalter: Toggle On → App-Neustart → Zustand bleibt On
  (Persistenz über `SettingsRepository` → SQLite → Reload, `manual-05`); danach wieder
  auf Off zurückgesetzt

### iOS-Simulator-Verifikation

Nicht möglich auf diesem Windows-Arbeitsplatz — der iOS-Build (`net10.0-ios`) und
`scripts/iOS-Deployment.ps1` benötigen macOS. Nachzuholen auf einem Mac mit iOS-Simulator:
App starten, Benachrichtigungs-Berechtigung bestätigen, Sync auslösen und Banner/
Mitteilungszentrale sowie Ruhezeiten-Verhalten prüfen.

### Iteration 3 (Review-Nachbearbeitung)

| Lauf | Befehl | Ergebnis |
|------|--------|----------|
| Build (Debug, Solution inkl. `net10.0-ios`) | `dotnet build Reporter.sln` | Erfolgreich, 1 Warnung (pre-existing CS8765 in `AppDelegate.FinishedLaunching`), 0 Fehler |
| Tests | `dotnet test src/Reporter.Tests --no-build` | 185 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| Static Checks | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Security, Static-Analysis-Release-Build ohne Befund) |

Behobene Review-Befunde (Code):

- `LocalNotificationService.BuildUserInfo`: `NSDictionary` wird jetzt korrekt über
  `NSDictionary.FromObjectsAndKeys(values, keys)` befüllt (bisher falscher
  `NSDictionary(object, object, params object[])`-Konstruktor → NSArray als Key,
  `UserInfo`-Verlust/Exception beim Serialisieren). Der `identifier`-Schlüssel wurde
  entfernt — der Identifier ist bereits `UNNotificationRequest`-Identifier.
- `FeedsViewModel`: doppelter Formular-Reset in `SaveAsync`/`DeleteAsync` in
  `ResetForm()` extrahiert.
- `SettingsViewModelTests_Load.Load_PopulatesNotificationSummaryEnabled` und
  `SettingsRepositoryTests.SaveAsync_PersistsNotificationSummaryEnabled` nutzen jetzt
  `TestSettingsHelper.SaveAsync`.

Behobene Review-Befunde (Usability):

- `NotificationDelegate.DidReceiveNotificationResponse`: Antippen navigiert jetzt
  in-app — Einzel-Benachrichtigung → `articledetail?itemId=…` (Route wie
  `ArticleCardView`), Sammel-Benachrichtigung (`feedId`) → `//unread`
  (`ShellContent.Route = "unread"` in `AppShell` ergänzt). Navigation via
  `MainThread.InvokeOnMainThreadAsync` mit `Shell.Current`-Null-Check; falls die Shell
  beim Kaltstart noch nicht bereit ist, Fallback auf `Launcher` mit `link`.
- Verweigerte Systemberechtigung wird beim Laden der Einstellungen erneut geprüft:
  `ILocalNotificationService.IsAuthorizedAsync` (neu) →
  `SettingsViewModel.NotificationPermissionDenied` → Hinweiszeile mit
  „Einstellungen öffnen"-Button in der Karte „Benachrichtigungen & Ruhezeiten"
  (MultiTrigger `NotificationsEnabled && NotificationPermissionDenied`, Texte aus
  `AppResources` `NotificationDeniedMessage`/`NotificationDeniedOpenSettings`, 44-pt-
  Touch-Target, `AppThemeBinding`).

Neue Tests (4): `SettingsViewModelTests_Load` (`Load_PermissionDenied_SetsNotificationPermissionDenied`,
`Load_PermissionGranted_ClearsNotificationPermissionDenied`,
`Load_NotificationsDisabled_HidesNotificationPermissionDenied`),
`SettingsViewModelTests_Persist` (`NotificationsEnabled_TurnedOff_ClearsNotificationPermissionDenied`).
`FakeLocalNotificationService` um `IsAuthorizedAsync`/`IsAuthorizedResult` erweitert.

Hinweis UI-Verifikation: Die neue Hinweiszeile ist unter Windows nicht sichtbar
auslösbar (`ILocalNotificationService.IsSupported == false` → `NotificationPermissionDenied`
bleibt `false`); die ViewModel-Logik ist durch die neuen Tests abgedeckt, das XAML
statisch gegen die AGENTS.md-Regeln geprüft (Touch-Target, `AppThemeBinding`,
resx-Texte). Die Sichtbarkeit der Zeile sowie die In-App-Navigation aus
Benachrichtigungen sind auf macOS/iOS-Simulator nachzuverifizieren.

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
