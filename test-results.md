<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test- und Verifikationsergebnisse

## Issue #81: Debuginformationen sammeln und per E-Mail versenden

Umfang: Opt-in-Schalter `settings.debug_collection_enabled`, session-scoped
Tabelle `debug_log_entries` (Reset bei jedem App-Start, Maximum 500 Einträge),
`DebugLogService`/`DebugReportService` in `Reporter.Core`, Gateways
`IEmailService`/`IDeviceInfoProvider` in `Reporter`, Instrumentierung in
`FeedSyncService`/`AutoRefreshService`/`App.xaml.cs` (Lifecycle, Sync-Fehler,
unbehandelte Exceptions), neuer Abschnitt „Diagnose & Support" auf der
Einstellungsseite mit Senden-Aktion über den System-Mail-Client.

### Build und Tests

| Lauf | Befehl | Ergebnis |
|------|--------|----------|
| Tests (Release) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release` | 454 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| Static Checks | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Lizenzheader, Security, Static-Analysis-Release-Build inkl. MAUI-App ohne Befund) |
| Migrationen | `dotnet ef migrations script` + `MigrateAsync` gegen Kopie der echten `reporter.db` | `debug_collection_enabled` (Default 0) und `debug_log_entries` + Index `IX_debug_log_entries_timestamp` angelegt |

Befund während der Verifikation: die generierte Migration `AddSettingsDebugCollection`
enthielt ein leeres `UpdateData` auf die Seed-Zeile `settings`, das SQLite mit
„near \"WHERE\": syntax error" ablehnte — `MigrateAsync` scheiterte dadurch beim
App-Start. Der leere `UpdateData`-Aufruf wurde entfernt (die `AddColumn`-Defaults
decken Bestandszeilen ab); anschließend lief die Migration gegen eine Kopie der
echten Datenbank fehlerfrei.

Neue Tests u. a.: `DebugLogRepositoryTests` (Reihenfolge, `DeleteAll`, `TrimToLatest`),
`DebugLogServiceTests` (Session-Reset, Enabled-State, Disabled-No-op, Schreiben,
Trim auf 500, Repository-Fehler werfen nicht, Übergangseintrag),
`DebugReportServiceTests` (Unsupported/Compose-false → `false`, Empfänger +
lokalisierter Betreff, App-/Device-/Netzwerk-/Settings-/Feed-Health-/Sync-Log-/
Session-Log-Inhalte, Limits 50/200, leere Logs),
`FeedSyncServiceTests_DebugLog` + `AutoRefreshServiceTests_DebugLog`
(Error-Level-Einträge bei Sync-Fehlern), `SettingsViewModelTests_Debug`
(Laden, Persistieren, `SetEnabled`-Aufruf, `DebugSendEnabled`-Matrix,
Senden über echten `DebugReportService` + `FakeEmailService`,
`DebugReportFailed`-Event), `DebugReportTests_E2E` (Persistenz-Roundtrip,
Report-Komposition über echte SQLite-Repositories, Session-Log Write/Reset),
`ReporterDbContextTests_Persistence`/`_Schema` (Roundtrips + Tabellen-Mapping),
`ServiceCollectionTests` (DI-Auflösung der neuen Services).
Neue Fakes: `FakeEmailService`, `FakeDeviceInfoProvider`, `FakeDebugLogService`;
`TestSettingsHelper.SaveAsync` um `debugCollectionEnabled` erweitert.

### Manuelle UI-Verifikation (durchgeführt)

Unpackaged `win-x64`-Debug-Build gestartet; Fenstergröße per `GetWindowRect`
verifiziert: **390 × 844 pt**. Interaktion über UI Automation
(`test-results/issue-59/uia.ps1`), deutsch lokalisierte UI. Screenshots unter
`test-results/issue-81/manual-*.png`.

- [x] **Migration beim App-Start:** `__EFMigrationsHistory` enthält nach dem
  Start `20260914060413_AddSettingsDebugCollection` +
  `20260914060416_AddDebugLogEntries`; `settings.debug_collection_enabled=0`,
  `debug_log_entries` leer (Sammlung aus → kein Start-Eintrag)
- [x] **Abschnitt „Diagnose & Support":** Section-Header, Switch-Zeile
  „Debuginformationen sammeln" + Hint, „Debugbericht senden" + Hint,
  Button **Senden** (286 × 44 pt) — `manual-02`
- [x] **Opt-in-Schalter:** Toggle per UIA → `settings.debug_collection_enabled=1`
  persistiert und `debug_log_entries` erhält sofort den Übergangseintrag
  `Info | Lifecycle | Debug collection enabled` — `manual-03`
- [x] **Senden:** Button **Senden** öffnet auf diesem Arbeitsplatz den
  Windows-Systemdialog „kein E-Mail-Programm zugeordnet" (`mailto:` ohne
  registrierten Client); danach ist im Session-Log `Info | Report | Debug
  report composed` sowie `Info | Lifecycle | App resumed` (Resume-Logging)
  sichtbar — `manual-04` (Systemdialog), `manual-05`
- [x] **Session-Reset:** App-Neustart → `debug_log_entries` enthält nur noch
  `Info | Lifecycle | Debug session started`; `debug_collection_enabled`
  bleibt `1`
- [x] **Dark Mode:** Abschnitt mit `AppThemeBinding`-Kartenfarben geprüft —
  `manual-06`
- [ ] **Mail-Entwurf mit echtem Client:** auf diesem Arbeitsplatz ist kein
  Mail-Client registriert; der vorbefüllte Entwurf (Empfänger-Platzhalter,
  lokalisierter Betreff, Plain-Text-Body mit allen sieben Sektionen) muss auf
  einem Gerät mit eingerichtetem Mail-Client bzw. iOS-Simulator nachgeholt
  werden. Body-Inhalt und Empfänger/Betreff sind durch
  `DebugReportServiceTests`/`DebugReportTests_E2E` abgedeckt.

### iOS-Verifikation

Steht aus — nur auf macOS möglich (`net10.0-ios`, `scripts/iOS-Deployment.ps1`).

## Issue #77: Verbesserungen der App (Lesezeit, Favicons, Start-Abruf, Sortierung, Sprach-Hinweis, Icon/Splash)

Branch: `task/issue-77-b1023d3d5f804e239e02f48af24b0ac3-verbesserungen-der-app`

Umfang: R1 Lesezeit-Text ab ≤ 1 Min ausblenden; R2 Feed-Favicons mit
Initial-Fallback; R3 optionaler Abruf beim App-Start (Voreinstellung: ein);
R4 konfigurierbare Sortierung der Ungelesen-Liste; R5 Neustart-Hinweis nur
nach Sprachänderung; R6 neue App-Icon- und Splash-Assets. R7/R8 wurden in die
folgenden eigenen Issues ausgelagert: #81 (Debug-Versand per E-Mail) und
#82 (Benachrichtigungen nur bei Hintergrundabruf).

### Build und Tests

| Lauf | Befehl | Ergebnis |
|------|--------|----------|
| Restore | `dotnet restore Reporter.sln -r win-x64 -p:IncludeIosTarget=false` | Erfolgreich (iOS-Target auf diesem Windows-Arbeitsplatz deaktiviert, da `Microsoft.NETCore.App.Runtime.Mono.win-x64` 10.0.12 nicht im Feed liegt) |
| Build (Release, Solution) | `dotnet build Reporter.sln -c Release --no-restore -p:IncludeIosTarget=false` | Erfolgreich, 0 Warnungen, 0 Fehler |
| Tests (Release) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release --no-build` | 400 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| Static Checks | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Lizenzheader, Security, Static-Analysis-Release-Build inkl. MAUI-App ohne Befund) |

Neue/geänderte Tests u. a.: `ReadingTimeEstimatorTests`
(`EstimateText_OneMinuteContent_ReturnsEmpty`,
`EstimateText_TwoMinuteContent_ReturnsText`, angepasste Kurzinhalt-/HTML-Fälle),
`FeedIconServiceTests` (Link-Tags, relative URLs, `/favicon.ico`-Fallback,
Fehlerfälle, Kandidaten-Durchlauf), `FeedsViewModelTests`
(`DirectAddCommand_StoresFaviconUrl`, `_IconLookupFails_FeedStillAdded`,
`_Offline_SkipsIconLookup`, `SubscribeResultCommand_UsesSiteUrlForFaviconLookup`,
`RenameFeedAsync_PreservesFaviconUrl`), `FeedSyncServiceTests`
(`SyncFeedAsync_MissingFavicon_BackfillsFromFeedAuthority`,
`_ExistingFavicon_SkipsLookup`, `_FaviconLookupFails_SyncStillSucceeds`),
`ItemRepositoryTests` (`GetUnreadByDateAsync_ProjectsFeedFaviconUrl`,
`_Ascending_OrdersByPublishedAtAscending`,
`_Ascending_NullPublishedAt_SortsFirst`), `FeedRepositoryTests`
(`GetAllWithDetailsAsync_ProjectsFaviconUrl`, `UpdateAsync_PersistsFaviconUrl`),
`AutoRefreshServiceTests` (`StartAsync_StartupRefreshEnabled_SyncsImmediately`,
`_StartupRefreshDisabled_DoesNotSyncImmediately`, `_Offline_SkipsStartupSync`,
`_StartupSyncThrows_StartStillCompletes`), `SettingsViewModelTests`
(`LanguageChange_SetsRestartHint`, `LanguageReverted_ClearsRestartHint`,
`Load_ResetsRestartHint`, `Load_PopulatesRefreshOnStartup`,
`Load_PopulatesSelectedSortOrder`, `RefreshOnStartup_Change_Persists`,
`SelectedSortOrder_Change_Persists`), `SettingsRepositoryTests`
(`SaveAsync_PersistsStartupRefreshAndSortOrder`),
`SettingsViewModelTests_E2E` (`E2E_RefreshOnStartup_PersistRoundtrip`,
`E2E_SortOrder_PersistRoundtrip`), `UnreadViewModelTests`
(`LoadPage_PassesSortOrderFromSettings`, `LoadPage_InvalidSortOrder_UsesDescending`).
Neue Fakes: `FakeFeedIconService`.

### Manuelle UI-Verifikation (durchgeführt)

Unpackaged `win-x64`-Release-Build gestartet; Fenstergröße per `GetWindowRect`
verifiziert: **390 × 844 pt**. Interaktion über UI Automation
(`test-results/issue-59/uia.ps1`), deutsch lokalisierte UI. Screenshots unter
`test-results/issue-77/manual-*.png`.

- [x] **Start-Abruf (R3):** `settings.refresh_on_startup_enabled=1`
  (Voreinstellung). Beim App-Start lief die Synchronisation — der bestehende
  Feed `Golem.de` erhielt dabei live `favicon_url=https://www.golem.de/favicon.ico`
  per Nachhole-Pfad (`SyncFeedAsync_MissingFavicon`…) — `manual-01`
- [x] **Einstellungen Synchronisation & Lesefluss:** Schalter
  „Beim Programmstart abrufen" sichtbar und per UIA `TogglePattern` als **On**
  verifiziert; darunter der Picker „Sortierung der ungelesenen Artikel"
  (159 × 52 pt) — `manual-03`, `manual-04`
- [x] **Sortierung (R4):** Picker-Dropdown listet „Neueste zuerst" /
  „Älteste zuerst" (`manual-05`); Auswahl „Älteste zuerst" persistiert
  `unread_sort_order='asc'` und die Liste **Ungelesen** zeigt sofort
  aufsteigende Daten (11.09. 11:30 → 11:42 → 12:05) — `manual-06`.
  Anschließend auf „Neueste zuerst" zurückgestellt (`desc`)
- [x] **Sprach-Hinweis (R5):** Ohne Änderung ist der Neustart-Hinweis in der
  UIA-Baumstruktur nicht vorhanden (`manual-07`); nach Wechsel auf „Englisch"
  erscheint „Die neue Sprache wird nach einem Neustart der App wirksam."
  sichtbar unter dem Picker (`manual-08`, `manual-09`); Rückwahl auf „System"
  blendet den Hinweis wieder aus (`manual-10`, UIA ohne Treffer)
- [x] **Favicon auf Feed-Karte (R2):** Feed-Karte „Golem.de" zeigt links das
  gerenderte Favicon — `manual-11`
- [x] **Initial-Fallback (R2):** Per „URL direkt hinzufügen" angelegter Feed
  `https://beispiel.invalid/feed.xml` (Favicon-Ermittlung schlägt fehl,
  `favicon_url=NULL`, Feed wird trotzdem angelegt) zeigt den Kreis mit dem
  Anfangsbuchstaben „F" — `manual-14`; Testfeed danach über
  **Feed-Aktionen → Löschen → Ja** wieder entfernt (DB wieder nur `Golem.de`)
- [x] **Light Mode:** Einstellungen und Ungelesen-Liste im hellen Schema
  korrekt gerendert (`AppThemeBinding`) — `manual-15`, `manual-16`; danach
  Farbschema auf „System" zurückgestellt
- [x] **Icon/Splash (R6):** Generierte Resizetizer-Assets geprüft —
  `resizetizer/r/appicon*.png` zeigt das neue Badge (dunkler Hintergrund
  `#1e293b`, weißer Rahmen, Amber-Punkt `#f59e0b`); `resizetizer/sp/
  splashSplashScreen.scale-400.png` enthält die Wortmarke „Reporter" als
  gerenderten Pfad in Weiß (Füllung von `#1e293b` auf `#ffffff` angepasst,
  damit sie auf dem dunklen Splash-Hintergrund sichtbar ist)

Hinweis zur Artikelkarten-Kaskade: Auf **Ungelesen** zeigen alle vorhandenen
Artikel ein `ImageUrl` (Golem-Tracking-Pixel `cpx.golem.de/cpx.php`), sodass
per Spezifikation das Artikelbild Vorrang vor Favicon und Initial hat —
der Favicon-/Initial-Zweig der Karte ist per XAML-`MultiTrigger` geprüft und
über die Feed-Karten (Favicon bzw. Initial) live verifiziert.

Bekannte Einschränkung: `favicon.ico`-Dateien ohne PNG-Link-Alternativen
(z. B. golem.de) werden gespeichert und auf der Feeds-Seite gerendert;
die Darstellung einzelner ICO-Varianten hängt vom Plattform-Decoder ab.

### iOS-Verifikation

Nicht möglich auf diesem Windows-Arbeitsplatz (`net10.0-ios` benötigt macOS).
Nachzuholen auf einem Mac: Splash- und Icon-Darstellung, Favicon-Anzeige,
neue Einstellungen bedienen.

## Issue #62: Manueller Sprachwechsel (EN/DE) in den Einstellungen

Branch: `task/issue-62-c777ec701888411a864e714ceaa8f8b3-manueller-sprachwechsel-ende-i`

### Build und Tests

| Lauf | Befehl | Ergebnis |
|------|--------|----------|
| Build (Debug, Solution) | `dotnet build Reporter.sln` | Erfolgreich, 1 Warnung (pre-existing CS8765 in `AppDelegate.FinishedLaunching`), 0 Fehler |
| Tests | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --no-build` | 331 bestanden, 0 fehlgeschlagen, 0 übersprungen (Baseline: 316) |
| Tests mit Coverage | `dotnet test --collect:"XPlat Code Coverage"` | 331 bestanden, 53,5 % Zeilenabdeckung |
| Static Checks | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Security, Static-Analysis-Release-Build ohne Befund) |

Neue Tests (15): `AppCultureTests` (3), `SettingsRepositoryTests.SaveAsync_PersistsLanguage`,
`SettingsViewModelTests_Load` (3: `LanguageOptions_ExposePersistedValues`,
`Load_InvalidLanguage_UsesSystemFallback`, `Load_PopulatesSelectedLanguage`),
`SettingsViewModelTests_Persist` (`SelectedLanguage_Change_Persists`,
`OtherChange_DoesNotLoseLanguage`), `SettingsViewModelTests_E2E`
(`E2E_ChangeLanguage_PersistRoundtrip` — VM → Repository → SQLite → Reload als
simulierter Neustart). `TestSettingsHelper.SaveAsync` um optionalen
`language`-Parameter erweitert.

### Mobile-UI-Design-Review „Sprache"-Sektion

Statische XAML-Prüfung von `SettingsPage.xaml` gegen
`design-draft/stitch_local_rss_feed_reader/einstellungen_filter/` (Light) und
`..._dark_mode/` sowie die AGENTS.md-Regeln:

- [x] Neue Sektions-Karte „Sprache" (`Border` + `RoundRectangle 12`,
  `AppThemeBinding` `SurfaceContainer`) im bestehenden `ScrollView` unter
  `Grid RowDefinitions="Auto,*"` — keine Verschachtelung von
  `ScrollView`/`CollectionView`, Sektion folgt exakt dem
  „Erscheinungsbild"-Kartenmuster
- [x] `Picker` (System/Deutsch/English) als Zeilen-Control wie der
  „Farbschema"-Picker; Klartext-Labels, keine internen Kennungen
- [x] Neustart-Hinweis als statischer Info-`Border` unterhalb der Zeile
  (`SettingsInfoBox`-Muster wie `SettingsRetentionInfo`)
- [x] Touch-Ziele ≥ 44 pt (Picker-Zeile 52 pt per UIA-Rect gemessen)
- [x] Dark Mode ausschließlich über `AppThemeBinding`; alle Texte aus
  `AppResources.*` (EN + DE), `SemanticProperties.Description` gesetzt

### Manuelle UI-Verifikation (durchgeführt)

Die App wurde auf dem Windows-Target im 390 × 844-pt-Fenster gestartet
(unpackaged `win-x64`-Debug-Build, Fenstergröße via `App.CreateWindow`, per
UIA-`BoundingRectangle` verifiziert: 390 × 844). Interaktion über UI Automation
+ `mouse_event`. Screenshots unter `test-results/issue-62/manual-*.png`:

- [x] `SettingsPage` Dark Mode (System-Theme dunkel): Sektionen und Karten
  unverändert, deutsche Texte (`manual-01`)
- [x] Karte „Sprache": Zeilenlabel „Sprache", Picker „System", Info-Text
  „Die neue Sprache wird nach einem Neustart der App wirksam." (`manual-02`);
  Picker-Dropdown listet „System / Deutsch / Englisch" (per
  `ExpandCollapsePattern` verifiziert)
- [x] Sprachwechsel „Englisch" → Auswahl sofort persistiert
  (`settings.language='en'`); App-Neustart → komplette UI englisch: Tabs
  „Unread/Feeds/Later", „228 unread articles", „Pull down to refresh",
  US-Datumsformat `9/12/2026 9:49 PM` (`CurrentCulture` mit umgeschaltet);
  `SettingsPage` zeigt „Language"-Karte mit Picker „English" und
  englischem Neustart-Hinweis (`manual-03`) — Nachweis, dass `AppCulture.Apply`
  in `MauiProgram` vor `CreateWindow` wirkt
- [x] Light Mode: „Color scheme" → „Light" → Sprach-Karte und Hinweis-Box
  korrekt hell gerendert (`manual-04`)
- [x] Rückweg: Sprache „System" → App-Neustart → UI wieder Deutsch
  (Systemsprache); Theme „System" wiederhergestellt

Hinweis: Der Windows-TabBar-Overflow-Button „Mehr" ist ein Plattform-String von
WinUI/MAUI und wechselt nicht mit der App-Sprache — bekanntes
Plattformverhalten, nicht Teil des App-Ressourcen-Umfangs.

### iOS-Simulator-Verifikation

Nicht möglich auf diesem Windows-Arbeitsplatz — der iOS-Build
(`net10.0-ios`) und `scripts/iOS-Deployment.ps1` benötigen macOS.
Nachzuholen auf einem Mac: Sprach-Picker in den Einstellungen bedienen,
App-Neustart, Tab-Titel und Einstellungstexte in der gewählten Sprache prüfen.

## Issue #59 (Iteration 5): Review-Nacharbeiten (Offline-Hint im Edit-Modus, Refactorings, ActionSheet-Disambiguierung)

Branch: `task-issue-59-efbace56737047f4b10610b83d0e137c-erweiterung-der-hinzufuegenfun`

Nacharbeiten aus `review-usability.md` (1 Befund) und `review-code.md`
(8 Befunde):

- Sheet-`FeedSearchOfflineHint` wird im Edit-Modus per zusätzlichem
  `DataTrigger` auf `IsEditMode` ausgeblendet — der Hinweis verspricht
  „Suchen"/„URL direkt hinzufügen", die es in diesem Modus nicht gibt.
- `FeedsViewModel.Search.cs`: gemeinsame Hilfsmethoden
  `TryPersistNewFeedAsync` (Dubletten-Prüfung + `Feed`-Initializer +
  Bereinigung beider Fehlerkanäle) und `FinishAddFlowAsync`
  (Trefferansicht schließen + `ResetForm` + `LoadAsync`) für
  `OfferDirectAddAsync`, `DirectAddAsync` und `SubscribeResultAsync`;
  `catch`-Block von `SearchAsync` in `HandleSearchFailure` ausgelagert;
  `DirectAddAsync` mit `IsEditMode`-Guard und
  `DirectAddCommand.CanExecute = !IsSearching && !IsEditMode`
  (`NotifyCanExecuteChanged` im `IsEditMode`-Setter) — symmetrisch zu
  `SearchCommand`.
- `DirectAddAsync` bereinigt jetzt `SearchErrorMessage` zentral im
  Persist-Schritt — Such- und Dubletten-Fehler stehen nicht mehr
  gleichzeitig im Sheet.
- `FeedsViewModel.cs`: `OpenAddForm` verwendet `ResetForm` statt des
  duplizierten Reset-Blocks; `FeedListItem`→`Feed`-Mapping über die
  gemeinsame Hilfsmethode `ToFeed` in `RenameFeedAsync`/
  `ChangeFeedCategoryAsync`; `SaveCommand`-XML-Doc auf „saves the feed
  currently being edited" korrigiert.
- `FeedsPage.xaml`: `MinimumHeightRequest="44"` an „Suchen", „URL direkt
  hinzufügen" und „Speichern" ergänzt; URL-Meta-Zeile der Trefferkarte per
  `DataTrigger` (`StringNotEmptyToBoolConverter` → `False`) ausgeblendet,
  wenn `Title` leer ist — die URL erscheint nicht mehr doppelt.
- `FeedsPage.xaml.cs`: `ChangeCategoryAsync` löst die ActionSheet-Auswahl
  positionsbasiert auf; gleichnamige Kategorien erhalten einen Zählsuffix
  („News", „News (2)"), damit jeder angezeigte Eintrag eindeutig einer
  Kategorie zugeordnet wird.

### Build und Tests

| Lauf | Befehl | Ergebnis |
|------|--------|----------|
| Tests | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release` | 311 bestanden, 0 fehlgeschlagen, 0 übersprungen (Vorher: 309) |
| Static Checks | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Lizenzheader, Security, Static-Analysis-Release-Build inkl. MAUI-App ohne Befund) |

Teständerungen (netto +2): neu `DirectAddCommand_WhenDuplicate_ClearsStaleSearchError`
(vor dem Fix rot: `HasSearchError` blieb `true`) und
`DirectAddCommand_InEditMode_DoesNotAddFeed` (vor dem Fix rot: `CanExecute`
war `true` und der Direkt-Add legte während eines laufenden Edits einen
neuen Feed an).

### Manuelle UI-Verifikation (durchgeführt, Iteration 5)

Unpackaged `win-x64`-Release-Build, Fenstergröße per `GetWindowRect`
verifiziert: **390 × 844 pt**. Interaktion über UI Automation
(`test-results/issue-59/uia.ps1`), Texteingabe über `ValuePattern.SetValue`;
deutsch lokalisierte UI, Dark Mode. Screenshots unter
`test-results/issue-59/manual-5-*.png`.

- [x] Add-Sheet: „Suchen" und „URL direkt hinzufügen" je 308 × 44 pt —
  `manual-5-02`
- [x] Kategorie-ActionSheet mit DB-seitig geseedeten Namens-Dubletten
  (`IX_categories_name` temporär entfernt, anschließend vollständig
  wiederhergestellt): Optionen „News" und „News (2)" getrennt wählbar;
  Tippen auf „News (2)" weist die **zweite** News-Kategorie zu (SQLite:
  `category_id` des Feeds = Duplikat-GUID `AAAAAAAA-…`, bestehende
  „News"-Zuordnung eines anderen Feeds unverändert) — `manual-5-04`
  (Sheet), `manual-5-05` (Karte zeigt „News")
- [x] Trefferkarte mit leerem Titel (`https://github.com/dotnet/maui/commits.atom`
  → `ExactUrl`-Discovery-Ergebnis ohne `Title`): Feed-URL erscheint nur
  einmal als Headline, die Meta-URL-Zeile ist ausgeblendet (UIA: genau
  ein `github.com/…`-Text-Element) — `manual-5-06`
- [x] Edit-Sheet: nur URL-Feld, Benachrichtigungs-Switch, iOS-Hinweis und
  „Speichern" (308 × 44 pt); kein „Suchen", kein „URL direkt hinzufügen",
  kein Offline-Hinweis — `manual-5-07`
- [x] Listenansicht unverändert — `manual-5-01`

Nicht interaktiv verifizierbar in dieser Umgebung:

- Offline-Hinweis im Edit-Modus (`Disable-NetAdapter` ohne Adminrechte
  nicht möglich): der neue `DataTrigger` auf `IsEditMode` ist deckungsgleich
  mit dem live verifizierten Trigger der „Suchen"-/„URL direkt
  hinzufügen"-Buttons (in `manual-5-07` ausgeblendet) und per XAML-Review
  geprüft; im Edit-Modus ist der Hinweis auch online nicht sichtbar.

## Issue #59 (Iteration 4): Review-Nacharbeiten (Offline-Direkt-Add, Sheet-UX, Lebenszyklus)

Branch: `task-issue-59-efbace56737047f4b10610b83d0e137c-erweiterung-der-hinzufuegenfun`

Nacharbeiten aus `review-usability.md` (3 Befunde) und `review-code.md`
(8 Befunde):

- Hinzufügen-Sheet hat jetzt einen zweiten, **offline aktiven** Button
  „URL direkt hinzufügen" (`ButtonDirectAdd` → `DirectAddCommand`,
  `CanExecute = !IsSearching`): persistiert die eingegebene URL ohne Suche
  direkt mit `FeedTitleFallback`-Titel, `CategoryId = null`,
  `NotificationsEnabled = true`; Domain-Eingaben werden wie bei der Suche zu
  `https://…` normalisiert; Dubletten/ungültige URLs melden den Fehler im
  geöffneten Sheet. Damit ist der `FeedSearchOfflineHint` („…bleibt möglich")
  wieder wahr.
- „Suchen"- und „URL direkt hinzufügen"-Buttons werden im Edit-Modus per
  `DataTrigger` auf `IsEditMode` ausgeblendet — eine begonnene Bearbeitung
  kann nicht mehr lautlos durch „Suchen" verworfen werden; `SearchCommand`
  ist zusätzlich per `CanExecute`/`SearchAsync`-Guard im Edit-Modus
  deaktiviert (deckt auch `Entry.ReturnCommand`/Enter-Taste ab).
- Kategorie-ActionSheet: Pseudo-Eintrag `CategoryNone` jetzt Klartext
  „Keine Kategorie"/„No category" (statt „—"); Kategorien, die exakt wie der
  Abbrechen-Button heißen, werden aus den Optionen gefiltert, damit Abbruch
  und Auswahl unterscheidbar bleiben (gleiches Muster in `UnreadPage`
  bewusst unverändert gelassen).
- `FeedsPage.xaml.cs`: `PropertyChanged`-Lambda durch benannten Handler
  `OnViewModelPropertyChanged` ersetzt — an `OnAppearing` abonniert, in
  `OnDisappearing` abgemeldet (Singleton-VM × Transient-Page); `OnFeedTapped`
  in `RenameFeedAsync`/`ChangeCategoryAsync`/`ConfirmDeleteFeedAsync`
  aufgeteilt.
- `FeedsViewModel.Search.cs`: Dubletten-Pfad des Direkt-Hinzufügens schließt
  jetzt die Trefferansicht (`ShowSearchResults = false`), das Sheet öffnet
  über der Feed-Liste; Persist-Logik in `TryAddFeedDirectlyAsync`
  zusammengeführt (durch `OfferDirectAddAsync` und `DirectAddAsync` geteilt).
- `FeedsViewModel.cs`: `OpenAddForm` setzt `SelectedFeed`/`NewUrl`/`NewTitle`/
  `FeedNotificationsEnabled`/`SelectedCategory` zurück (Invariante „Add-Modus
  ⇒ kein `SelectedFeed`"); toter Add-Zweig aus `SaveAsync` entfernt —
  Hinzufügen läuft ausschließlich über Suche/Abonnieren/Direkt-Add, der
  Speichern-Button existiert nur im Edit-Modus (Entscheidung: bereinigt statt
  umwidmen, weil `DirectAddAsync` ohne `NewTitle`/`SelectedCategory` mit
  `FeedTitleFallback` persistiert und den Pfad nicht benötigt).
- `FeedSyncService.cs`: veralteter Kommentar zur Host-Vorbelegung korrigiert;
  toter Schlüssel `PlaceholderFeedTitle` aus beiden resx + Designer entfernt;
  Doku-Wert `RoundRectangle 20,20,0,0` → `12,12,0,0` korrigiert.

### Build und Tests

| Lauf | Befehl | Ergebnis |
|------|--------|----------|
| Tests | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release` | 309 bestanden, 0 fehlgeschlagen, 0 übersprungen (Vorher: 303) |
| MAUI-App-Build (Windows) | `dotnet build src/Reporter/Reporter.csproj -f net10.0-windows10.0.19041.0 -c Release` | Erfolgreich, 0 Warnungen, 0 Fehler (XAML-SourceGen) |
| Static Checks | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Lizenzheader, Security, Static-Analysis-Release-Build inkl. MAUI-App ohne Befund) |

Teständerungen (netto +6): neu `DirectAddCommand_WhenOffline_PersistsFeedWithDefaults`,
`DirectAddCommand_DomainInput_NormalizesToHttpsUrl`,
`DirectAddCommand_WhenUrlInvalid_SetsErrorAndKeepsSheetOpen`,
`DirectAddCommand_WhenDuplicate_SetsErrorAndKeepsSheetOpen`,
`SaveCommand_WithoutSelectedFeed_DoesNotAddFeed`,
`SearchCommand_InEditMode_DoesNotDiscardEdit` sowie die Regressionstests
`SearchCommand_NoResultsAndConfirmedDuplicate_ClosesResultsView` (vor Fix rot:
`ShowSearchResults` blieb `true`) und `OpenAddFormCommand_AfterEdit_ClearsStaleEditState`
(vor Fix rot: `SelectedFeed`/`NewUrl` veraltet); entfernt
`SaveCommand_NewFeed_PersistsNotificationsEnabledFalse` und
`SaveCommand_NewFeed_PersistsHealthStatusOk` (toter UI-Pfad, Kontrakt jetzt
über `DirectAddCommand`-Tests abgedeckt); `SaveCommand_ResetsFeedNotificationsEnabled`
läuft jetzt über den Edit-Pfad.

### Manuelle UI-Verifikation (durchgeführt, Iteration 4)

Unpackaged `win-x64`-Release-Build gestartet; Fenstergröße per
`GetWindowRect` verifiziert: **390 × 844 pt**. Interaktion über UI Automation
+ `mouse_event` (`test-results/issue-59/uia.ps1`), Texteingabe über
`ValuePattern.SetValue`. Theme-Umschaltung über `settings.theme` in der
SQLite-DB mit App-Neustart. Screenshots unter
`test-results/issue-59/manual-4-*.png`; deutsch lokalisierte UI.

Verifizierte Szenarien (Live-Lauf):

- [x] Add-Sheet zeigt neben „Suchen" den neuen Button „URL direkt hinzufügen"
  (beide 44 pt, vertikal gestapelt) — `manual-4-02` (Dark), `manual-4-06`
  (Light)
- [x] „URL direkt hinzufügen" mit `https://example.com/direkt-add.xml` →
  Sheet schließt ohne Dialog, Feed `direkt-add.xml` (Dateinamen-Fallback) in
  Liste und SQLite (`category_id=NULL`, `notifications_enabled=1`) —
  `manual-4-03` (Dark)
- [x] Dublette über Direkt-Add → „Ein Feed mit dieser URL existiert bereits."
  im geöffneten Sheet, kein zweiter Feed (SQLite: 1 Zeile) — `manual-4-07`
  (Light)
- [x] Kategorie-ActionSheet zeigt Klartext „Keine Kategorie" statt „—" —
  `manual-4-04` (Dark)
- [x] Edit-Modus („Feed bearbeiten") zeigt nur noch URL-Feld,
  Benachrichtigungs-Switch, iOS-Hinweis und „Speichern" — kein „Suchen",
  kein „URL direkt hinzufügen" — `manual-4-05` (Dark)
- [x] Listenansicht unverändert — `manual-4-01` (Dark)

Nicht interaktiv verifizierbar in dieser Umgebung (durch Tests abgedeckt):

- Offline-Schaltung (`Disable-NetAdapter` ohne Adminrechte nicht möglich):
  der Direkt-Add-Button ist online wie offline sichtbar und funktional;
  `DirectAddCommand_WhenOffline_PersistsFeedWithDefaults` belegt, dass der
  Pfad bei `IsOnline == false` persistiert, während `SearchCommand` deaktiviert
  bleibt (`CanExecute = false`).

## Issue #59 (Iteration 3): Listenansicht, Bottom-Sheet, Umbenennen/Kategorie

Branch: `task-issue-59-efbace56737047f4b10610b83d0e137c-erweiterung-der-hinzufuegenfun`

Die `FeedsPage` wurde von einer kombinierten Formular-plus-Liste-Seite zu einer
reinen Listenansicht umgebaut: Ein primärer „+“-Button öffnet das
Hinzufügen-Formular als Bottom-Sheet-Overlay; Feeds ohne Titel erhalten den
Dateinamen der Feed-URL als Fallback; das Feed-Kontextmenü bietet neu
„Umbenennen" und „Kategorie ändern".

### Build und Tests

| Lauf | Befehl | Ergebnis |
|------|--------|----------|
| Tests | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release` | 303 bestanden, 0 fehlgeschlagen, 0 übersprungen (Baseline der Iteration: 274) |
| MAUI-App-Build (Windows) | `dotnet build src/Reporter/Reporter.csproj -f net10.0-windows10.0.19041.0 -c Release` | Erfolgreich, 0 Warnungen, 0 Fehler (XAML-SourceGen) |
| Static Checks | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Lizenzheader, Security, Static-Analysis-Release-Build inkl. MAUI-App ohne Befund) |

Neue Tests (29): `FeedTitleFallbackTests` (8 Fälle: letztes Pfadsegment,
URL-Dekodierung, Host-Fallback, unparsebare URL → Original-String, Trailing
Slash, Placeholder-Match/-Mismatch, Case-insensitiv), `FeedsViewModelTests`
(ca. 20 neue/angepasste: `OpenAddFormCommand`/`CloseAddFormCommand`,
`EditAsync` öffnet Sheet im Edit-Modus, `RenameFeedAsync` Erfolg/leer/null,
`ChangeFeedCategoryAsync` Setzen/`Guid.Empty`→null/null-Argumente,
Suche schließt Sheet bei Treffern, Sheet bleibt bei
`FeedSearchUnavailableException` und Save-Validierungsfehlern offen,
Direkt-Hinzufügen bestätigt → sofort persistiert mit Dateinamen-Titel +
`CategoryId = null` + `NotificationsEnabled = true`, Dublette →
`ErrorFeedDuplicate` im offenen Sheet, Abbruch bewahrt Formularzustand,
Treffer-Abonnieren nutzt `FeedTitleFallback` bei leerem Titel),
`FeedSyncServiceTests` (1 neu: Dateinamen-Platzhalter-Titel wird beim Sync
durch den Dokumenttitel ersetzt; explizite Titel bleiben unverändert).

Zusätzlich: `scripts/add-license-headers.mjs` überspringt jetzt `.vs` und
`TestResults` (git-ignorierte, maschinenlokale VS-/Test-Artefakte — analog zu
`bin`/`obj`), weil VS die Datei `.vs\...\testlog.manifest` im laufenden Betrieb
sperrt und der Check sonst falsch-negativ anschlägt.

### Mobile-UI-Design-Review „Feeds" (Listenansicht + Bottom-Sheet)

Statische XAML-Prüfung der Änderungen an `FeedsPage.xaml` gegen die
AGENTS.md-Regeln und den Design-Entwurf:

- [x] Seite ist list-first: permanente Formularkarte entfernt, Row 0 enthält
  nur den vollflächigen Primär-Button „+ Feed per URL hinzufügen"
  (`ActionAddFeed`, `MinimumHeightRequest="44"`), die Feed-Liste liegt in
  `Grid`-Row `*`
- [x] Bottom-Sheet als `Grid`-Overlay über beide Rows: halbtransparenter
  Backdrop (`AppThemeBinding`) mit `TapGestureRecognizer` →
  `CloseAddFormCommand`, bottom-alignierter `Border` mit oberen Rundungen
  (`RoundRectangle 12,12,0,0`), Sichtbarkeit per `DataTrigger` auf
  `ShowAddForm`
- [x] Sheet-Titel lokalisiert je nach Modus (`FeedAddSheetTitle` /
  `FeedEditSheetTitle` per `DataTrigger` auf `IsEditMode`), Schließen-Button
  ≥ 44 pt
- [x] Sheet enthält `ErrorMessage` + `SearchErrorMessage`, Offline-Hint,
  `NewUrl`-Entry (`ReturnCommand` = `SearchCommand`), `ActivityIndicator`,
  Suchen-Button; Edit-Modus zusätzlich Benachrichtigungs-Switch + iOS-Hinweis
  + Speichern-Button — alles vertikal gestapelt, keine Button-Reihen
- [x] Seiten-Fehler (`ErrorMessage`) liegt außerhalb des Sheets und bleibt
  sichtbar, wenn das Sheet schließt
- [x] Keine `ScrollView`/`CollectionView`-Verschachtelung; Trefferliste,
  Attribution, RefreshView und Offline-Banner unverändert
- [x] Dark Mode ausschließlich über `AppThemeBinding`; alle neuen Texte aus
  `AppResources.*` (EN + DE, 7 neue Schlüssel: `ActionAddFeed`,
  `FeedAddSheetTitle`, `FeedEditSheetTitle`, `ButtonRename`,
  `ButtonChangeCategory`, `PromptRenameFeedTitle`, `PromptRenameFeedMessage`)
- [x] `FeedsPage.xaml.cs`: `PropertyChanged` → `NewUrlEntry.Focus()` via
  `Dispatcher.Dispatch`; Kontextmenü erweitert um „Umbenennen"
  (`DisplayPromptAsync` mit Titel-Vorbelegung → `RenameFeedAsync`) und
  „Kategorie ändern" (`DisplayActionSheetAsync` mit `CategoryNone` +
  Kategorien → `ChangeFeedCategoryAsync`); `OnBackButtonPressed` schließt
  offenes Sheet

### Manuelle UI-Verifikation (durchgeführt, Iteration 3)

Unpackaged `win-x64`-Release-Build gestartet; Fenstergröße per
`GetWindowRect` verifiziert: **390 × 844 pt**. Interaktion über UI Automation
+ `mouse_event` (`test-results/issue-59/uia.ps1`, um `sysback` für
XButton1/System-Zurück erweitert), Texteingabe über `ValuePattern.SetValue`.
Theme-Umschaltung über `settings.theme` in der SQLite-DB mit App-Neustart.
Screenshots unter `test-results/issue-59/manual-2-*.png` (Dark) und
`manual-3-*.png` (Light); deutsch lokalisierte UI.

Verifizierte Szenarien (Live-Lauf):

- [x] Listenansicht: Feed-Liste + Primär-Button „+ Feed per URL hinzufügen",
  kein permanentes Formular — `manual-2-01` (Dark), `manual-3-01` (Light)
- [x] Sheet öffnet sich als Bottom-Overlay, URL-Feld erhält Fokus —
  `manual-2-02` (Dark), `manual-3-02` (Light)
- [x] Schließen-Button und Backdrop-Tap schließen das Sheet; System-Zurück
  (XButton1 via `uia.ps1 -Action sysback`) schließt das Sheet, App bleibt auf
  der Feeds-Seite
- [x] Suche → Trefferliste ersetzt die Liste → Treffer-Tap →
  „Feed abonnieren?"-Dialog → „Ja" → Feed in Liste, SQLite-persistiert —
  `manual-2-03` … `manual-2-06` (Dark)
- [x] Kontextmenü: „Aktualisieren", „Umbenennen", „Kategorie ändern",
  „Bearbeiten", „Löschen" — `manual-2-07` (Dark)
- [x] Umbenennen: Prompt mit vorbefülltem Titel → persistiert + Liste zeigt
  neuen Titel (`Heise umbenannt`) — `manual-2-08` (Dark)
- [x] Kategorie ändern: ActionSheet mit „—" (`CategoryNone`), „News",
  „Sport", „Unterhaltung" → Auswahl persistiert (`category_id` in SQLite
  geprüft) — `manual-2-09` (Dark)
- [x] Bearbeiten: Sheet im Edit-Modus (Titel „Feed bearbeiten", URL + Switch
  „Benachrichtigungen" + iOS-Hinweis + Speichern) — `manual-2-10` (Dark)
- [x] Ungültige URL im Edit-Modus → `ErrorMessage` im geöffneten Sheet —
  `manual-2-11` (Dark); doppelte URL → `ErrorFeedDuplicate` im Sheet —
  `manual-2-12` (Dark)
- [x] Direkt-Hinzufügen: `https://example.com/mein-feed.xml` → „Kein Feed
  gefunden"-Dialog → „Ja" → Sheet schließt, Liste zeigt `mein-feed.xml`
  (Dateinamen-Fallback), SQLite: `title='mein-feed.xml'`,
  `category_id=NULL`, `notifications_enabled=1` — `manual-2-13`,
  `manual-2-14` (Dark)
- [x] Sync-Platzhalter-Auflösung: per SQL eingefügter Feed mit Titel
  `heise-Rubrik-IT-atom.xml` → Kontextmenü „Aktualisieren" → Titel wird zu
  „heise online IT" — `manual-2-15` (Dark)
- [x] Suche nicht erreichbar (`nonexistent.invalid`) →
  `FeedSearchUnavailableRetry` im geöffneten Sheet, Suchen-Button bleibt —
  `manual-2-16` (Dark)

Nicht interaktiv verifizierbar in dieser Umgebung (durch Tests abgedeckt):

- Offline-Hint im Sheet (`FeedSearchOfflineHint` + deaktivierter
  Suchen-Button): `Disable-NetAdapter` ohne Adminrechte nicht möglich.
  Abgedeckt durch `SearchCommand_WhenOffline_SkipsSearchWithoutError` und
  `ConnectivityChanged_UpdatesSearchCommandCanExecute`.
- iOS-Simulator: nicht möglich auf Windows (siehe `scripts/iOS-Deployment.ps1`).

## Issue #59: Feed-Suche über feedsearch.dev und clientseitige Autodiscovery

Branch: `task/issue-59-efbace56737047f4b10610b83d0e137c-erweiterung-der-hinzufuegenfun`

### Build und Tests

| Lauf | Befehl | Ergebnis |
|------|--------|----------|
| Tests | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release` | 274 bestanden, 0 fehlgeschlagen, 0 übersprungen (Baseline: 239) |
| MAUI-App-Build (Windows) | `dotnet build src/Reporter/Reporter.csproj -f net10.0-windows10.0.19041.0 -c Release` | Erfolgreich, 0 Warnungen, 0 Fehler (XAML-SourceGen) |
| Static Checks | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Lizenzheader, Security, Static-Analysis-Release-Build inkl. MAUI-App ohne Befund) |

Neue Tests (35): `FeedSearchServiceTests` (15 Fälle: Directory-Mapping inkl.
`bozo`-Filter, Sortierung `MatchKind`→`Score`→`FeedUrl`, `ExactUrl`-Erkennung,
Autodiscovery via Link-Tags/Standardpfade/Feed-Dokument, Fast-Path ohne
Site-Request, Dedupe nach `FeedUrl` getrennt für Directory und Discovery,
Fehler beider Quellen/Timeout/Parsefehler → `FeedSearchUnavailableException`,
Aufrufer-Abbruch → `OperationCanceledException`, leere Liste),
`FeedSyncServiceTests` (2: Platzhalter-Titel `Title == Url` wird durch
`SyndicationFeed.Title` ersetzt, gesetzter Titel bleibt unverändert),
`FeedsViewModelTests` (17: Suche befüllt Trefferliste, Domain-Normalisierung
`https://…`, Freitext ohne Service-Aufruf, leere Eingabe, Offline-Hint,
`FeedSearchUnavailable`-Fehlerkanal, Retry-Hinweis bei Domain-Eingabe,
`ConfirmDirectAddAsync`-Fallback bestätigt/abgelehnt (getrennte Tests),
Dialog-Fehler löst keinen Doppel-Dialog aus, Domain ohne Treffer ohne Dialog,
`NewUrl`-Reset, `CloseSearchResultsCommand`, Abonnieren inkl.
Kategorie/Notifications/Platzhalter-Titel, Dublette, Connectivity-CanExecute),
`ServiceCollectionTests` (`AddReporterServices_ResolvesFeedSearchService`).
Neue Hilfsklassen: `FakeFeedSearchService`, `StubHttpMessageHandler` (privat in
`FeedSearchServiceTests`).

### Mobile-UI-Design-Review „Feeds"

Statische XAML-Prüfung der Änderungen an `FeedsPage.xaml` gegen die
AGENTS.md-Regeln und den Design-Entwurf
`design-draft/stitch_local_rss_feed_reader/feeds_health_status/screen.png`:

- [x] Kein horizontales Datentabellen-Layout; Trefferliste als kartenbasierte
  `CollectionView` (`Border` + `RoundRectangle 12`, `AppThemeBinding`
  `SurfaceContainer`) mit `TapGestureRecognizer` → `OnSearchResultTapped` +
  `DisplayAlertAsync`-Bestätigung
- [x] Keine mehreren Text-Buttons in einer Zeile — „Suchen" und „Speichern"
  vertikal gestapelt im Formular
- [x] Treffer-`CollectionView` in `Grid`-Row `*` (Row 1, Wrapper `*,Auto` mit
  Attribution-Footer), keine `ScrollView`-/`CollectionView`-Verschachtelung;
  Sichtbarkeit per `DataTrigger` auf `ShowSearchResults`, Feed-Liste invers
- [x] Touch-Ziele ≥ 44 pt: Trefferkarte `MinimumHeightRequest="44"`, Buttons über
  globalen Style (`MinimumHeightRequest="44"`)
- [x] Dark Mode ausschließlich über `AppThemeBinding`; alle neuen Texte aus
  `AppResources.*` (EN + DE, 12 neue Schlüssel inkl. `FeedSearchUnavailableRetry`
  und `ButtonCloseSearchResults` aus Iteration 2; `PlaceholderFeedUrl`
  entfernt, Designer-Properties ergänzt)
- [x] `EmptyView` „Keine Feeds gefunden." bei leerem Ergebnis;
  `FeedSearchAttribution` („powered by feedsearch.dev") sichtbar unter der
  Trefferliste (Nutzungsbedingung der API)
- [x] Offline: Suchen-Button per `SearchCommand.CanExecute` deaktiviert,
  `FeedSearchOfflineHint`-Label per `DataTrigger` (`IsOnline == false`)
- [x] Sichtbarer Rückweg aus der Trefferansicht: Button „Zurück zu meinen
  Feeds" (`CloseSearchResultsCommand`, `MinimumHeightRequest="44"`) unterhalb
  der Attribution (Iteration 2, Usability-Befund)
- [x] Fehlerhinweis differenziert: `FeedSearchUnavailable` (Direkt-Hinzufügen-
  Angebot) nur bei gültiger URL, sonst `FeedSearchUnavailableRetry`
  (Iteration 2, Usability-Befund)

### Manuelle UI-Verifikation (durchgeführt, Iteration 2)

Unpackaged `win-x64`-Release-Build gestartet; Fenstergröße per
`GetWindowRect` verifiziert: **390 × 844 pt** (Größe wird in
`App.xaml.cs` `CreateWindow` gesetzt). Interaktion über UI Automation +
`mouse_event` (`test-results/issue-59/uia.ps1`), Texteingabe über
`ValuePattern.SetValue`. Theme-Umschaltung über `settings.theme` in der
SQLite-Datenbank (`system`/`dark`/`light`) mit App-Neustart; OS-Default ist
Dark. Screenshots unter `test-results/issue-59/manual-*.png`.

Verifizierte Szenarien (Live-Lauf, deutsch lokalisierte UI):

- [x] Domain-Suche `tagesschau.de`/`heise.de` → Trefferliste als Karten
  (Titel, Site-Name + Site-URL, Feed-URL) mit Attribution „Suche powered by
  feedsearch.dev" und Button „Zurück zu meinen Feeds" (44 pt) —
  `manual-04`/`manual-09` (Dark), `manual-11` (Light)
- [x] Treffer-Tap → Bestätigungsdialog „Feed abonnieren?" mit Titel in der
  Nachricht → „Ja" → Feed erscheint in der Feed-Liste; Persistenz per SQLite-
  Abfrage verifiziert (`Kommentare zu: Der Tötungsfall in Offenburg`,
  Health `OK`) — `manual-05`, `manual-06` (Dark)
- [x] URL ohne Treffer (`https://example.com/`, `https://iana.org/`) →
  Direkt-Hinzufügen-Dialog nennt die eingegebene Adresse (`{0}`-Platzhalter)
  → „Ja" → `NewTitle` mit Host vorbefüllt (`example.com`, per UIA
  `ValuePattern` ausgelesen) → „Speichern" persistiert den Feed —
  `manual-12` (Light); derselbe Dialog im Dark-Lauf `manual-03`
- [x] Suche nicht erreichbar: beide Quellen schlugen fehl (feedsearch.dev
  lehnt localhost-Anfragen mit 400 ab; Discovery gegen `127.0.0.1:8099`
  schlug im App-Prozess fehl) → Hinweis „Die Feed-Suche ist nicht
  erreichbar. Du kannst die URL direkt hinzufügen." + Direkt-Hinzufügen-
  Dialog bleibt nutzbar — `manual-03` (Dark)
- [x] Dubletten-Treffer abonnieren → „Ein Feed mit dieser URL existiert
  bereits.", kein zweiter Feed (SQLite-Abfrage: 1 Zeile) — `manual-07` (Dark)
- [x] „Zurück zu meinen Feeds" verlässt die Trefferansicht und zeigt die
  Feed-Liste wieder (per UIA-Elementliste verifiziert)
- [x] Layout: Karten ≥ 44 pt, `AppThemeBinding` Light/Dark (Hintergrund
  hell 236,238,240 / dunkel 28,32,40 per Pixelprobe), keine
  Scroll-Verschachtelung, `EmptyView`, Attribution sichtbar;
  `FeedsPage`-Formular `manual-02`/`manual-08` (Dark), `manual-10` (Light)

Nicht interaktiv verifizierbar in dieser Umgebung (durch Tests abgedeckt):

- Offline-Szenario (Suchen-Button deaktiviert + `FeedSearchOfflineHint`):
  `Disable-NetAdapter` scheitert ohne Adminrechte („Zugriff verweigert").
  Abgedeckt durch `SearchCommand_WhenOffline_DoesNotCallService_AndSetsHint`
  und `ConnectivityChanged_UpdatesSearchCommandCanExecute`.
- Treffer ohne Titel → Titel-Befüllung nach erstem Sync: lokaler
  Stub-Feed-Server (`test-results/issue-59/stubserver.py`) ist vom
  App-Prozess aus nicht erreichbar (Anfragen kommen nicht am Stub an,
  während `dotnet fsi` denselben `FeedSearchService` erfolgreich in 135 ms
  mit 1 `Discovered`-Treffer beantwortet — siehe `probe.fsx`). Abgedeckt
  durch `SubscribeResultCommand_WhenTitleEmpty_StoresFeedUrlAsPlaceholder`
  und `SyncFeedAsync_WhenTitleIsPlaceholder_UpdatesTitleFromFeedDocument`.
- iOS-Simulator: nicht möglich auf Windows (siehe `scripts/iOS-Deployment.ps1`).

Hinweis zu den Screenshots: `manual-01` bis `manual-09` wurden bei
OS-Dark-Mode aufgenommen (`theme=system`/`dark`), `manual-10` bis
`manual-12` bei `theme=light`.

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
