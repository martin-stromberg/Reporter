<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Debuginformationen sammeln und per E-Mail versenden (Issue #81)

## Übersicht

Die `SettingsPage` erhält einen neuen Abschnitt „Diagnose & Support" mit einem `Switch` für `Settings.DebugCollectionEnabled` und einer Versand-Aktion, die einen Debug-Report als vorbefüllte E-Mail über `Email.ComposeAsync` an den System-Mail-Client übergibt. Der Schalter aktiviert zusätzlich eine neue, persistierte **Session-Debug-Log-Infrastruktur**: Eine Tabelle `debug_log_entries` protokolliert während der Laufzeit unbehandelte Exceptions, Sync-/Feed-Fehler und wichtige Lifecycle-Ereignisse; sie wird bei jedem App-Start zurückgesetzt — mit Ausnahme der `Error`-Einträge (z. B. Absturzberichte) der Vor-Session, die übernommen werden, damit ein Absturz nach Neustart meldbar bleibt — und nur beschrieben, solange die Sammlung eingeschaltet ist. Betroffen sind `Reporter.Core` (Gateway-Interfaces `IEmailService`/`IDeviceInfoProvider`/`IDebugReportService`/`IDebugLogService`/`IDebugLogRepository`, Modelle `AppDeviceInfo`/`DebugLogEntry`, Konstanten `DebugLogLevel`/`DebugLogCategory`, Services `DebugReportService`/`DebugLogService`, `Settings`-Modell, `SettingsViewModel`, Instrumentierung in `FeedSyncService`/`AutoRefreshService`, resx-Schlüssel), `Reporter.Data` (Entities, `ReporterDbContext`, zwei Migrationen, `SettingsRepository`, `DebugLogRepository`), die MAUI-App `Reporter` (`EmailService`, `DeviceInfoProvider`, `MauiProgram`-DI, `App.xaml.cs`-Session-/Exception-Anbindung, `SettingsPage`) sowie die Testsuite `Reporter.Tests`.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Persistenz des Session-Debug-Logs | Neue SQLite-Tabelle `debug_log_entries` mit `DebugLogEntry`-Entity/-Modell und `IDebugLogRepository`/`DebugLogRepository` — **keine** Datei in `FileSystem.AppDataDirectory`. | Die App persistiert sämtliche Daten über EF Core + Repository-Muster; `sync_logs` ist der direkte Präzedenzfall einer Log-Tabelle. Die DB-Variante ist über `TestDbContextFactory`/In-Memory-SQLite ohne Zusatzinfrastruktur testbar, der Report liest sie wie `SyncLog` über ein Repository, und der Session-Reset ist ein atomares `DeleteAllExceptErrorsAsync`. Eine Datei bräuchte einen neuen Plattform-Pfad-Gateway (`FileSystem.AppDataDirectory` ist MAUI-only, `Reporter.Core` targetet `net10.0`), File-Locking/Rotation und läge außerhalb aller Projekt-Konventionen. Nachteil (Einträge vor der Migration nicht schreibbar) ist irrelevant: `Migrate()` läuft in `ApplyPersistedLanguage`/`App.OnStart` vor `BeginSessionAsync`. |
| Laufzeit-Schalter der Protokollierung | `DebugLogService` hält den Aktivierungsstand als `_enabled`-Flag im Speicher: gesetzt in `BeginSessionAsync` aus `Settings.DebugCollectionEnabled`, zur Laufzeit über `SetEnabled(bool)` aus dem `SettingsViewModel`-Toggle. `LogAsync` ist bei `false` ein sofortiger No-op. | Kein `ISettingsRepository.GetAsync` pro Log-Ereignis (DB-Zugriff pro Eintrag entfällt); der Toggle wirkt ohne App-Neustart mitten in der Session; das Flag ist trivial fakebar. |
| Session-Reset | `IDebugLogService.BeginSessionAsync` in `App.OnStart` direkt nach `context.Database.MigrateAsync()`: `DeleteAllExceptErrorsAsync` löscht die Vor-Session bis auf `Error`-Einträge, dann wird `_enabled` geladen und bei aktivierter Sammlung ein `Lifecycle`-Eintrag „Session started" geschrieben. | `App.OnStart` ist der erste async-Startpunkt mit garantiert migriertem Schema (die Migration läuft sogar schon früher synchron in `ApplyPersistedLanguage`); `async void OnStart` erlaubt `await` ohne Blocking. Der Reset erfolgt auch bei ausgeschalteter Sammlung — die Tabelle enthält damit immer genau eine Session plus übernommene Absturz-/Fehlerberichte. **Anwender-Entscheidung (Iteration 3, ersetzt den ursprünglichen pauschalen `DeleteAllAsync`-Reset):** Einträge mit `DebugLogLevel.Error` (u. a. `UnhandledException`/`UnobservedTaskException` aus `DebugLogCategory.Exception` und Sync-Fehler) überleben den Reset, damit ein Absturzbericht der Vor-Session nach Neustart noch versendbar ist; `Info`/`Warning`-Einträge werden weiterhin gelöscht. |
| Einbindung bestehender Fehlerpfade | Gezielte `IDebugLogService.LogAsync`-Aufrufe an den diagnostisch relevanten `Debug.WriteLine`-Catch-Stellen (`FeedSyncService`, `AutoRefreshService`, `SettingsViewModel`-Versandpfad, `App.OnStart`-Catches) sowie neue globale Hooks (`AppDomain.CurrentDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`, `App.OnSleep`/`OnResume`) — **kein** generischer `TraceListener`-Bridge und keine Retrofit aller 29 `Debug.WriteLine`-Stellen. | `Debug.WriteLine` ist `[Conditional("DEBUG")]` und im Release-Build (einziger ausgelieferter Build) komplett entfernt — ein Listener würde genau dann nichts liefern, wenn der Anwender den Report braucht. Artikeldetail-/Such-Fehlerpfade (`ArticleDetailViewModel`, `FeedsViewModel.Search` u. a.) bleiben bei `Debug.WriteLine`: geringer Diagnosewert gegenüber dem Änderungsumfang. Optionale Ctor-Parameter (`IDebugLogService? = null`) halten alle bestehenden Test-Konstruktoraufrufe kompilierbar. |
| Fehlverhalten des Log-Dienstes | `DebugLogService.LogAsync`/`BeginSessionAsync` fangen eigene Fehler intern ab (`Debug.WriteLine`) und werfen nie. | Der Logger sitzt in Fehlerpfaden (u. a. `UnhandledException`-Handler); er darf selbst keine Ausnahmequelle sein. |
| Begrenzung des Session-Logs | Speicher: `DebugLogService` ruft nach jedem `AddAsync` `TrimToLatestAsync(MaxStoredEntries = 500)` auf. Report: `DebugReportService` gibt die jüngsten `MaxDebugLogEntries = 200` Einträge aus. | Eine App-Session erzeugt normalerweise zweistellige Einträge; die Obergrenzen decken Fehlerschleifen ab, ohne paged Repository-Zugriff zu benötigen. |
| Gateway-Zuschnitt | Zwei getrennte Gateway-Interfaces `IEmailService` und `IDeviceInfoProvider` (statt eines kombinierten `IDebugReportGateway`). | Beide Gateways haben je genau eine Plattformverantwortung (Mail-Compose vs. App-/Geräteinfo) — das entspricht dem etablierten Muster (`IAppThemeService`, `INetworkStatusService`, `ILocalNotificationService` sind jeweils fokussierte Ein-Zweck-Gateways) und erlaubt getrennte Fakes in Tests. |
| Geräteinfo über die Core-Grenze | Neues Core-Datenmodell `AppDeviceInfo` als Snapshot (Value Object), das `IDeviceInfoProvider.GetSnapshot()` liefert. | `AppInfo`/`DeviceInfo` sind MAUI-Typen und können `Reporter.Core` (net10.0, ohne MAUI-Referenz) nicht erreichen; ein schlichter Daten-Container entkoppelt Report-Formatierung von der Plattform und ist im Fake trivial setzbar. |
| Fehlerprotokoll des Mail-Gateways | `IEmailService.ComposeAsync` liefert `Task<bool>` (`true` = Compose-Fenster geöffnet, `false` = nicht unterstützt/fehlgeschlagen); `IsSupported`-Property als Vorab-Prüfung für die UI. | `IEmail.IsComposeSupported` existiert in `Microsoft.Maui.ApplicationModel.Communication` — `IsSupported` delegiert an `Email.Default.IsComposeSupported`. `Email.ComposeAsync` kann dennoch `FeatureNotSupportedException` o. ä. werfen (z. B. kein eingerichteter Mail-Account); solche Plattformfehler werden im Gateway abgefangen und auf `false` gekapselt — so bleibt der Core-Vertrag ausnahmefrei und testbar. Muster: `ILocalNotificationService.IsSupported`. |
| Orchestrierung | Neues `IDebugReportService`-Interface + `DebugReportService` in `Reporter.Core/Services/`; der Service sammelt alle Datenquellen (inkl. `IDebugLogRepository`), formatiert den Plain-Text-Body und ruft `IEmailService.ComposeAsync`. | Exakt das Orchestrierungsmuster von `NotificationService`: Regelwerk in Core, Plattformwirkung über injizierte Gateways — vollständig mit In-Memory-SQLite und Fakes testbar. |
| ViewModel-Anbindung | `SettingsViewModel` erhält `IDebugReportService?` und `IDebugLogService?` als optionale Konstruktor-Parameter (letzte Parameter, `= null`). | Selbes Muster wie `ILocalNotificationService?` — die bestehenden Test-Konstruktoraufrufe in `SettingsViewModelTests_*` kompilieren unverändert weiter; bei `null` meldet `DebugEmailSupported` `false`. |
| Fehlerpfad „kein Mail-Client" | Neues Event `DebugReportFailed` (`Func<Task>?`) auf dem ViewModel → `SettingsPage` zeigt `DisplayAlertAsync`; zusätzlich permanenter Hinweis-`Border` im Abschnitt, solange `DebugEmailSupported == false`. | Exakt das bestehende `NotificationAuthorizationDenied`-Muster (Event → `DisplayAlertAsync` im Code-Behind) bzw. der `NotificationsIosOnlyHint`-`Border` (DataTrigger auf `*Supported == False`). |
| Report-Umfang/-Format | Plain-Text-`Body` mit Abschnitten: App-Info, Gerät/OS, Online-Status, Settings-Snapshot, Feed-Health (Titel, URL, `HealthStatus`, `LastCheckedAt`, `HealthLastChange`), die jüngsten 50 `SyncLog`-Einträge, die jüngsten 200 `DebugLogEntry`-Einträge der laufenden Session plus übernommener `Error`-Einträge der Vor-Session. Keine `EmailAttachment`, keine Artikeldaten (`Item.Title`, `ContentHtml`). | Vom Anwender bestätigter Umfang; die Begrenzungen halten den Body handhabbar, ohne neue Anhang-/Datei-Infrastruktur (`FileSystem.CacheDirectory`) einzuführen. Transparenz ergibt sich aus dem sichtbaren Mail-Entwurf — der Anwender prüft und sendet selbst. |
| Empfängeradresse | Feste Konstante `DebugReportRecipient` im `DebugReportService` (Platzhalter, vom Maintainer zu ersetzen). | Vom Anwender so entschieden; nicht benutzerkonfigurierbar. |
| Senden-Aktivierung | Die Senden-Aktion ist nur aktiv, wenn `DebugCollectionEnabled == true` **und** `DebugEmailSupported == true` (neue get-only-Property `DebugSendEnabled`; Deaktivierung per `IsEnabled`-Binding + `Opacity = 0.4`-`DataTrigger`). Der eigentliche Versand-Guard sitzt zusätzlich in `SendDebugReportAsync` selbst: frühe Rückkehr bei `_debugReportService is null` **oder** `!DebugCollectionEnabled` — bewusst **nicht** per `CanExecute` des Commands, da `IAsyncRelayCommand.ExecuteAsync` `CanExecute` nicht auswertet (Tests und programmatische Aufrufe würden den Guard sonst umgehen). Der `IsSupported`-Fall wird bewusst nicht weggeguardet: Bei eingeschalteter Sammlung, aber fehlendem Mail-Client läuft `SendReportAsync` → `false` → `DebugReportFailed`-Alert — das ist der gewünschte sichtbare Fehlerpfad. | Vom Anwender bestätigt; entspricht dem Deaktivierungs-Muster der bestehenden Sektionen (`NotificationsSupported`/`NotificationControlsEnabled`) und der Opt-in-Interpretation des Schalters. |

## Programmabläufe

### Session-Debug-Log: Start, Reset und Aktivierung

1. `MauiProgram.CreateMauiApp` → `ApplyPersistedLanguage` → `context.Database.Migrate()` stellt sicher, dass `debug_log_entries` (und `settings.debug_collection_enabled`) existieren.
2. `App.OnStart` → `context.Database.MigrateAsync()` → anschließend `IDebugLogService` aus dem DI-Scope auflösen → `await BeginSessionAsync()`.
3. `DebugLogService.BeginSessionAsync`: `IDebugLogRepository.DeleteAllExceptErrorsAsync()` löscht alle Einträge der Vor-Session außer `Error`-Einträgen (übernommene Absturz-/Fehlerberichte bleiben versendbar) → `ISettingsRepository.GetAsync()` → `_enabled = settings.DebugCollectionEnabled` → bei `true` ein `LogAsync(DebugLogCategory.Lifecycle, "Debug session started", level: Info)`-Eintrag. Eigene Fehler werden abgefangen (Logging darf den App-Start nie verhindern).
4. Läuft der Schalter weiter auf `false`, bleibt die Tabelle leer; jeder `LogAsync`-Aufruf ist ein No-op.

Beteiligte Klassen/Komponenten: `App`, `IDebugLogService`/`DebugLogService`, `IDebugLogRepository`/`DebugLogRepository`, `ISettingsRepository`, `ReporterDbContext`, `MauiProgram`.

### Session-Debug-Log: Ereignis schreiben

1. Aufrufer (Service, ViewModel oder `App`) → `IDebugLogService.LogAsync(category, message, details, level)`.
2. `IsEnabled == false` → Rückkehr ohne Schreibzugriff.
3. Sonst: `DebugLogEntry` mit `Id = Guid.NewGuid()`, `Timestamp = _timeProvider.GetUtcNow().UtcDateTime`, `Level`, `Category`, `Message`, `Details` (z. B. `exception.ToString()`) → `IDebugLogRepository.AddAsync` → `IDebugLogRepository.TrimToLatestAsync(MaxStoredEntries = 500)` begrenzt das Tabellenwachstum.
4. Eigene Exceptions (DB nicht bereit, Fehler beim Schreiben) → `Debug.WriteLine`, niemals weiterwerfen.

Beteiligte Klassen/Komponenten: `IDebugLogService`/`DebugLogService`, `IDebugLogRepository`/`DebugLogRepository`, `DebugLogEntry`, `DebugLogLevel`, `DebugLogCategory`, `TimeProvider`.

### Unbehandelte Exceptions und Lifecycle-Ereignisse

1. `App.OnStart` abonniert nach `BeginSessionAsync` `AppDomain.CurrentDomain.UnhandledException` und `TaskScheduler.UnobservedTaskException`; die Handler rufen fire-and-forget `_ = debugLogService.LogAsync(DebugLogCategory.Exception, "...", ex.ToString(), DebugLogLevel.Error)`.
2. `App.OnSleep`/`OnResume` (neue Overrides) schreiben `Lifecycle`-Einträge („App suspended" / „App resumed").
3. Die bestehenden Catch-Blöcke in `App.OnStart` (Retention-Cleanup, Theme, Netzwerk-Status, Auto-Refresh) rufen zusätzlich zu `Debug.WriteLine` `_ = debugLogService.LogAsync(DebugLogCategory.Lifecycle, "...", ex.ToString(), DebugLogLevel.Error)`.

Beteiligte Klassen/Komponenten: `App`, `IDebugLogService`.

### Sync-/Feed-Fehler in das Session-Log

1. `FeedSyncService.SyncFeedAsync` Catch-Pfad (nach `UpdateFeedHealthAsync`/`UpdateLogAsync` mit `FeedHealth.Error`): `_ = _debugLogService?.LogAsync(DebugLogCategory.Sync, $"Synchronization failed for feed '{feed.Title}'", ex.ToString(), DebugLogLevel.Error)`; der Notification-Catch (bisher `Debug.WriteLine("FeedSyncService notification failed")`) loggt mit `Warning`.
2. `AutoRefreshService` Catches (`RunStartupSyncAsync`, `RunLoopAsync`): `_ = _debugLogService?.LogAsync(DebugLogCategory.Sync, "Auto refresh sync failed", ex.ToString(), DebugLogLevel.Error)`.
3. Fehler bleiben zusätzlich in `sync_logs` sichtbar (dort Feed-Health-Kontext); das Session-Log ergänzt den Exception-Detail-Text und nicht-feedbezogene Fehler.

Beteiligte Klassen/Komponenten: `FeedSyncService`, `AutoRefreshService`, `IDebugLogService`.

### Schalter „Debuginformationen sammeln" (Persistenz-Toggle + Laufzeit-Schalter)

1. `SettingsPage.OnAppearing` → `LoadCommand` → `SettingsViewModel.LoadAsync`: lädt `Settings` via `ISettingsRepository.GetAsync` und setzt `DebugCollectionEnabled = settings.DebugCollectionEnabled` (unter `_isLoading`-Guard — kein Persist beim Laden); der Setter ruft `_debugLogService?.SetEnabled(value)` — gegenüber `BeginSessionAsync` ein No-op bei unverändertem Wert.
2. Anwender schaltet den `Switch` → Setter `DebugCollectionEnabled` → `SetProperty` → `OnPropertyChanged(nameof(DebugSendEnabled))` → `_debugLogService?.SetEnabled(value)` (aktiviert/stoppt die Protokollierung sofort; bei der Aktivierung schreibt der Service einen `Lifecycle`-Übergangseintrag) → `PersistOnChange()` → `PersistAsync` baut den `Settings`-Record inkl. `DebugCollectionEnabled` neu → `ISettingsRepository.SaveAsync` schreibt die Entity (Spalte `debug_collection_enabled`), serialisiert über `_persistLock`.
3. Beim nächsten App-Start liest `BeginSessionAsync` den persistierten Wert und setzt die Protokollierung fort bzw. lässt sie aus.

Beteiligte Klassen/Komponenten: `SettingsPage`, `SettingsViewModel`, `Settings` (Core-Modell + Entity), `ISettingsRepository`/`SettingsRepository`, `IDebugLogService`, `ReporterDbContext`.

### Debugbericht versenden

1. Anwender tippt die Senden-Aktion (nur aktiv bei `DebugSendEnabled == true`) → `SendDebugReportCommand` → `SettingsViewModel.SendDebugReportAsync`.
2. `SendDebugReportAsync`-Guard: Bei `_debugReportService is null` oder `!DebugCollectionEnabled` sofortige Rückkehr — kein Versand, kein Event (ausgeschaltete Sammlung = Feature inaktiv). Erst danach ruft die Methode `IDebugReportService.SendReportAsync(CancellationToken)`.
3. `DebugReportService.SendReportAsync` prüft `IEmailService.IsSupported`; bei `false` → Rückgabe `false` ohne Compose-Aufruf.
4. Der Service sammelt: `IDeviceInfoProvider.GetSnapshot()` (App-/Geräte-/OS-Angaben), `INetworkStatusService.IsOnline`, `ISettingsRepository.GetAsync()` (Settings-Snapshot), `IFeedRepository.GetAllAsync()` (`HealthStatus`/`LastCheckedAt`/`HealthLastChange` je Feed), `ISyncLogRepository.GetLatestAsync(50)` (absteigend nach `StartedAt`, bereits in der Abfrage begrenzt) und `IDebugLogRepository.GetLatestAsync(MaxDebugLogEntries = 200)` (absteigend nach `Timestamp` — das Session-Log).
5. Der Service formatiert den lokalisierten Betreff (`AppResources.DebugReportEmailSubject`) und den Plain-Text-Body mit lokalisierten Abschnitts-Headern für alle sieben Sektionen (`AppResources.DebugReportSection*`, siehe resx-Liste) und ruft `IEmailService.ComposeAsync(DebugReportRecipient, subject, body, ct)`. Anschließend schreibt er einen `Report`-Eintrag ins Session-Log (optionaler `IDebugLogService?`-Parameter): `Info` bei Erfolg („Debug report composed"), `Error` bei `ComposeAsync == false` sowie bei `IsSupported == false` (vorzeitige Rückkehr). Auf `false` abgebildet werden nur `IsSupported == false` und `ComposeAsync == false`; unerwartete Exceptions (Repository-Zugriff, `ComposeAsync`) werden nicht geschluckt, sondern an den Aufrufer weitergereicht — Schritt 7 behandelt sie.
6. `EmailService.ComposeAsync` (MAUI) baut `EmailMessage { To = { to }, Subject = subject, Body = body, BodyFormat = EmailBodyFormat.PlainText }` und ruft `Email.Default.ComposeAsync`; der System-Mail-Client öffnet den vorbefüllten Entwurf — der Anwender prüft und sendet selbst. `FeatureNotSupportedException`/andere Plattformfehler werden abgefangen → `false`.
7. Rückgabe `true` → Vorgang endet (Mail-Client ist offen). Rückgabe `false` → `SettingsViewModel` löst `DebugReportFailed` aus → `SettingsPage.OnDebugReportFailed` zeigt `DisplayAlertAsync` mit `DebugReportFailedTitle`/`DebugReportFailedMessage`/`ButtonOk`. Unerwartete Exceptions in `SendDebugReportAsync` → `Debug.WriteLine` + `_debugLogService?.LogAsync(…, Error)` + `DebugReportFailed` (konsistent zur bisherigen Fehlerbehandlung).

Beteiligte Klassen/Komponenten: `SettingsPage`, `SettingsViewModel`, `IDebugReportService`/`DebugReportService`, `IEmailService`/`EmailService`, `IDeviceInfoProvider`/`DeviceInfoProvider`, `INetworkStatusService`, `ISettingsRepository`, `IFeedRepository`, `ISyncLogRepository`, `IDebugLogRepository`, `IDebugLogService`, `AppResources`.

### Fehlerhinweis ohne Mail-Unterstützung (permanent)

1. `SettingsViewModel.DebugEmailSupported` = `_debugReportService?.IsSupported == true` (get-only, wird beim Erzeugen/Laden ausgewertet — analog `NotificationsSupported`).
2. `SettingsPage`-XAML: Hinweis-`Border` (`SurfaceSubtle`, `RoundRectangle 8`) mit `DataTrigger` `DebugEmailSupported == False` → `IsVisible = True`, Text `SettingsDebugEmailUnsupportedHint`; gleichzeitig deaktiviert `DebugSendEnabled == false` die Senden-Aktion (Opacity 0.4).

Beteiligte Klassen/Komponenten: `SettingsViewModel`, `SettingsPage.xaml`, `AppResources`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `IEmailService` | Interface (`src/Reporter.Core/Interfaces/IEmailService.cs`) | Gateway zum System-Mail-Client: `bool IsSupported { get; }`, `Task<bool> ComposeAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)` — Rückgabe `false` bei nicht unterstütztem/fehlgeschlagenem Versand |
| `IDeviceInfoProvider` | Interface (`src/Reporter.Core/Interfaces/IDeviceInfoProvider.cs`) | Gateway für App-/Geräte-/OS-Angaben: `AppDeviceInfo GetSnapshot()` |
| `IDebugLogRepository` | Interface (`src/Reporter.Core/Interfaces/IDebugLogRepository.cs`) | Datenzugriff auf das Session-Log: `Task<IReadOnlyList<DebugLogEntry>> GetAllAsync()` (absteigend nach `Timestamp`), `Task<IReadOnlyList<DebugLogEntry>> GetLatestAsync(int maxEntries)` (begrenzte Abfrage für den Report), `Task AddAsync(DebugLogEntry entry)`, `Task DeleteAllExceptErrorsAsync()` (Session-Reset unter Erhalt der `Error`-Einträge), `Task TrimToLatestAsync(int maxEntries)` (Wachstumsbremse, nur wirksam oberhalb des Limits) — Muster `ISyncLogRepository`, ohne `GetById`/`Update`/`Delete(id)` (nicht benötigt) |
| `IDebugLogService` | Interface (`src/Reporter.Core/Interfaces/IDebugLogService.cs`) | Schreibseite des Session-Logs: `bool IsEnabled { get; }`, `Task BeginSessionAsync(CancellationToken = default)` (Reset unter Erhalt der `Error`-Einträge + Schalter laden + Start-Eintrag), `void SetEnabled(bool enabled)` (Laufzeit-Umschaltung inkl. Übergangseintrag), `Task LogAsync(string category, string message, string? details = null, string level = DebugLogLevel.Info, CancellationToken cancellationToken = default)` — nie werfend |
| `IDebugReportService` | Interface (`src/Reporter.Core/Interfaces/IDebugReportService.cs`) | Orchestrierungs-Vertrag: `bool IsSupported { get; }` (delegiert an `IEmailService.IsSupported`), `Task<bool> SendReportAsync(CancellationToken cancellationToken = default)` |
| `AppDeviceInfo` | Datenmodellklasse (`src/Reporter.Core/Models/AppDeviceInfo.cs`) | Snapshot-Value-Object: `AppName`, `AppVersion`, `AppBuild`, `DeviceModel`, `DeviceManufacturer`, `Platform`, `OsVersion` (je `string`, init-only) |
| `DebugLogEntry` | Datenmodellklasse (`src/Reporter.Core/Models/DebugLogEntry.cs`) | Session-Log-Eintrag: `Id` (`required Guid`), `Timestamp` (`required DateTime`, UTC), `Level` (`string?`), `Category` (`string?`), `Message` (`string?`), `Details` (`string?`, z. B. `exception.ToString()`) |
| `DebugLogLevel` | Konstantenklasse (`src/Reporter.Core/Services/DebugLogLevel.cs`) | `Info`/`Warning`/`Error` — Muster `FeedHealth` |
| `DebugLogCategory` | Konstantenklasse (`src/Reporter.Core/Services/DebugLogCategory.cs`) | `Lifecycle`, `Sync`, `Exception`, `Settings`, `Report` — Muster `FeedHealth` |
| `DebugLogEntry` | Entity (`src/Reporter.Data/Entities/DebugLogEntry.cs`) | Persistenz-Entity mit denselben Feldern (settable), Tabelle `debug_log_entries` |
| `DebugLogRepository` | Klasse (`src/Reporter.Data/Repositories/DebugLogRepository.cs`) | `IDebugLogRepository`-Implementierung über `IDbContextFactory<ReporterDbContext>` — Muster `SyncLogRepository` (`AsNoTracking`, `OrderByDescending(Timestamp)`, `MapToModel`/`MapToEntity`); `TrimToLatestAsync` via `ExecuteDeleteAsync` auf alle Einträge außer den jüngsten `maxEntries` |
| `DebugLogService` | Klasse (`src/Reporter.Core/Services/DebugLogService.cs`) | `IDebugLogService`-Implementierung: `_enabled`-Flag, `BeginSessionAsync` (Reset via `DeleteAllExceptErrorsAsync` — `Error`-Einträge der Vor-Session bleiben erhalten + `Settings.DebugCollectionEnabled` laden + Start-Eintrag), `SetEnabled` (Übergangseintrag bei Aktivierung — da `void`-Signatur: fire-and-forget `_ = LogAsync(…)`, Fehlerpfad wie `LogAsync` niemals werfend), `LogAsync` (No-op wenn deaktiviert oder abgebrochen; `AddAsync` + `TrimToLatestAsync(MaxStoredEntries = 500)` — der Trim ist im Repository per Count-Prüfung ein No-op unterhalb des Limits; fängt eigene Fehler auf `Debug.WriteLine`); Abhängigkeiten `IDebugLogRepository`, `ISettingsRepository`, optional `TimeProvider?` |
| `EmailService` | Klasse (`src/Reporter/Services/EmailService.cs`) | `IEmailService`-Implementierung über `Microsoft.Maui.ApplicationModel.Communication.Email` (`Email.Default.ComposeAsync`/`EmailMessage`, `BodyFormat.PlainText`); `IsSupported` delegiert an `Email.Default.IsComposeSupported`; kapselt `FeatureNotSupportedException` u. ä. (z. B. fehlender Mail-Account) auf `false` |
| `DeviceInfoProvider` | Klasse (`src/Reporter/Services/DeviceInfoProvider.cs`) | `IDeviceInfoProvider`-Implementierung: liest `AppInfo.Current` (`Name`, `VersionString`, `BuildString`) und `DeviceInfo.Current` (`Model`, `Manufacturer`, `Platform`, `VersionString`) in `AppDeviceInfo` |
| `DebugReportService` | Klasse (`src/Reporter.Core/Services/DebugReportService.cs`) | Sammelt Report-Daten aus `ISettingsRepository`, `ISyncLogRepository` (`GetLatestAsync`), `IFeedRepository`, `IDebugLogRepository` (`GetLatestAsync`), `IDeviceInfoProvider`, `INetworkStatusService`; formatiert Betreff/Body mit sieben lokalisierten Abschnitts-Headern (`AppResources.DebugReportSection*`); `SendReportAsync` bildet nur `IsSupported == false`/`ComposeAsync == false` auf `false` ab — unerwartete Exceptions propagieren zum Aufrufer (`SettingsViewModel`-Fehlerpfad); Konstanten `DebugReportRecipient` (Platzhalter-Empfängeradresse), `MaxSyncLogEntries = 50`, `MaxDebugLogEntries = 200`; optionaler `TimeProvider?` für den Report-Zeitstempel und optionaler `IDebugLogService?` für `Report`-Logeinträge (Erfolg `Info`, Fehlschlag `Error` — jeweils nach dem Compose-Ergebnis) |

## Änderungen an bestehenden Klassen

### `Settings` (Core-Datenmodell, `src/Reporter.Core/Models/Settings.cs`)

- **Neue Eigenschaften:** `DebugCollectionEnabled` (`required bool`, init) — Opt-in-Schalter der Debug-Sammlung/Session-Protokollierung, Default über Entity/DB `false`.

### `ISyncLogRepository` (`src/Reporter.Core/Interfaces/ISyncLogRepository.cs`) und `SyncLogRepository` (`src/Reporter.Data/Repositories/SyncLogRepository.cs`)

- **Neue Methode:** `Task<IReadOnlyList<SyncLog>> GetLatestAsync(int maxEntries)` — absteigend nach `StartedAt`, `Take(maxEntries)` bereits in der Datenbankabfrage (wird vom `DebugReportService` genutzt; `GetAllAsync` bleibt für die übrigen Aufrufer bestehen).

### `Settings` (Entity, `src/Reporter.Data/Entities/Settings.cs`)

- **Neue Eigenschaften:** `DebugCollectionEnabled` (`bool`, C#-Default `false` — Muster `NotificationSummaryEnabled`).

### `ReporterDbContext` (`src/Reporter.Data/ReporterDbContext.cs`)

- **Neue Eigenschaften:** `DbSet<DebugLogEntry> DebugLogEntries`.
- **Geänderte Methoden:** `OnModelCreating` — Aufruf von `ConfigureDebugLogEntry` ergänzen; `ConfigureSettings` — neues Mapping `entity.Property(e => e.DebugCollectionEnabled).HasColumnName("debug_collection_enabled").IsRequired().HasDefaultValue(false)`; `entity.HasData(new Settings())` erbt den C#-Default `false`.
- **Neue Methoden:** `ConfigureDebugLogEntry` (private static) — Tabelle `debug_log_entries`, Spalten `id` (PK), `timestamp` (`IsRequired`), `level` (`HasMaxLength(20)`), `category` (`HasMaxLength(50)`), `message`, `details`; Index auf `timestamp` für die absteigende Sortierung (Muster `ConfigureSyncLog`, ohne FK).

### `SettingsRepository` (`src/Reporter.Data/Repositories/SettingsRepository.cs`)

- **Geänderte Methoden:** `SaveAsync` — `entity.DebugCollectionEnabled = settings.DebugCollectionEnabled` ergänzen; `MapToModel` — `DebugCollectionEnabled = entity.DebugCollectionEnabled` im Initializer ergänzen.

### `SettingsViewModel` (`src/Reporter.Core/ViewModels/SettingsViewModel.cs`)

- **Neue Abhängigkeiten:** optionale Konstruktor-Parameter `IDebugReportService? debugReportService = null` und `IDebugLogService? debugLogService = null` (nach `localNotificationService`), Felder `_debugReportService`/`_debugLogService`.
- **Neue Eigenschaften:** `DebugCollectionEnabled` (`bool`; Setter: `SetProperty` + `_debugLogService?.SetEnabled(value)` (unter `_isLoading`-Guard — kein Seiteneffekt beim Laden, Muster `PersistOnChange`) + `OnPropertyChanged(nameof(DebugSendEnabled))` + `PersistOnChange()`); `DebugEmailSupported` (`bool`, get-only: `_debugReportService?.IsSupported == true`); `DebugSendEnabled` (`bool`, get-only: `DebugEmailSupported && DebugCollectionEnabled`).
- **Neue Commands:** `SendDebugReportCommand` (`AsyncRelayCommand` → `SendDebugReportAsync`).
- **Neue Methoden:** `SendDebugReportAsync` (`private`) — Guards: `_debugReportService is null` oder `!DebugCollectionEnabled` → Rückkehr ohne Versand und ohne Event (kein `CanExecute`-Guard, da `IAsyncRelayCommand.ExecuteAsync` `CanExecute` nicht auswertet); ruft `_debugReportService.SendReportAsync()`; bei `false` `DebugReportFailed`-Event auslösen; bei unerwarteter Exception zusätzlich `Debug.WriteLine` + `_debugLogService?.LogAsync(DebugLogCategory.Report, …, ex.ToString(), DebugLogLevel.Error)` und danach `DebugReportFailed` auslösen.
- **Neue Events:** `DebugReportFailed` (`Func<Task>?`) — wird ausgelöst, wenn der Versand nicht möglich/fehlgeschlagen ist; von `SettingsPage` abonniert.
- **Geänderte Methoden:** `LoadAsync` — `DebugCollectionEnabled = settings.DebugCollectionEnabled` ergänzen; `PersistAsync` — `DebugCollectionEnabled = DebugCollectionEnabled` im `new Settings`-Initializer ergänzen.

### `FeedSyncService` (`src/Reporter.Core/Services/FeedSyncService.cs`)

- **Neue Abhängigkeit:** optionaler Konstruktor-Parameter `IDebugLogService? debugLogService = null` (letzter Parameter), Feld `_debugLogService`.
- **Geänderte Methoden:** `SyncFeedAsync` — im Catch-Pfad nach `UpdateLogAsync(…, FeedHealth.Error, …)` zusätzlich `_ = _debugLogService?.LogAsync(DebugLogCategory.Sync, …, ex.ToString(), DebugLogLevel.Error)`; der Notification-Catch (bisher nur `Debug.WriteLine`) loggt mit `DebugLogLevel.Warning`.

### `AutoRefreshService` (`src/Reporter.Core/Services/AutoRefreshService.cs`)

- **Neue Abhängigkeit:** optionaler Konstruktor-Parameter `IDebugLogService? debugLogService = null` (letzter Parameter), Feld `_debugLogService`.
- **Geänderte Methoden:** `RunStartupSyncAsync` und `RunLoopAsync` — in den allgemeinen Catch-Blöcken zusätzlich zu `Debug.WriteLine` `_ = _debugLogService?.LogAsync(DebugLogCategory.Sync, "Auto refresh sync failed", ex.ToString(), DebugLogLevel.Error)`.

### `ArticleDetailViewModel` (`src/Reporter/ViewModels/ArticleDetailViewModel.cs`)

- **Geänderte Methoden:** `LoadAsync` — der Fallback-`new Settings`-Initializer (Zeile ~263, bei Ladefehler der Settings) muss `DebugCollectionEnabled = false` ergänzen, da die Property `required` ist.

### `App` (`src/Reporter/App.xaml.cs`)

- **Geänderte Methoden:** `OnStart` — nach `context.Database.MigrateAsync()` `IDebugLogService` auflösen, `await BeginSessionAsync()` aufrufen (Session-Reset + Aktivierung) und die globalen Handler `AppDomain.CurrentDomain.UnhandledException`/`TaskScheduler.UnobservedTaskException` abonnieren; die vier bestehenden Catch-Blöcke loggen zusätzlich via `_ = debugLogService.LogAsync(DebugLogCategory.Lifecycle, …, ex.ToString(), DebugLogLevel.Error)`.
- **Neue Methoden:** `OnSleep`/`OnResume` (Overrides) — `Lifecycle`-Logeinträge „App suspended"/„App resumed"; `OnUnhandledException`/`OnUnobservedTaskException` (private Handler) — fire-and-forget `LogAsync(DebugLogCategory.Exception, …, DebugLogLevel.Error)`.

### `SettingsPage.xaml` (`src/Reporter/Views/SettingsPage.xaml`)

- Neuer Abschnitt am Ende des `ScrollView` (nach „Sprache"), exakt in der bestehenden Sektions-Konvention: `VerticalStackLayout Spacing="6"` → Section-Header-`Label` (`UiLabelStyle`, `SettingsSectionDebug`) → `Border Padding="16"`, `RoundRectangle 12`, `SurfaceCard`-`AppThemeBinding` mit `VerticalStackLayout Spacing="12"`:
  - `Switch`-Zeile (`Grid ColumnDefinitions="*,Auto"`): Label `SettingsDebugCollectionLabel` + `MetaStyle`-Hint `SettingsDebugCollectionHint`, `Switch IsToggled="{Binding DebugCollectionEnabled}"` mit `SemanticProperties.Description`, 44-pt-Mindestmaß.
  - Senden-Bereich (`Border` `SurfaceSubtle`/`RoundRectangle 8` mit `VerticalStackLayout`, `IsEnabled="{Binding DebugSendEnabled}"` + `Opacity=0.4`-`DataTrigger` auf `DebugSendEnabled == False`): Label `SettingsDebugSendLabel` + `MetaStyle`-Hint `SettingsDebugSendHint`, darunter `Button` (`SettingsDebugSendButton`, `Command="{Binding SendDebugReportCommand}"`, `SemanticProperties.Description`, ≥ 44 pt) über volle Breite. **Abweichung vom ursprünglichen `Grid ColumnDefinitions="*,Auto"`-Entwurf:** Ein Aktions-Button neben einer zweizeiligen Label/Hint-Spalte wäre auf 390 pt eine überladene Zeile (AGENTS.md-Mobile-Regeln); der vertikale Stack mit vollbreitem Button entspricht der Karten-Konvention der übrigen Aktions-Border.
  - Hinweis-`Border` (Muster `NotificationsIosOnlyHint`): `IsVisible="False"` + `DataTrigger` `DebugCollectionEnabled == False` → `IsVisible = True`, Text `SettingsDebugCollectionRequiredHint` — erklärt, warum die Senden-Aktion deaktiviert ist.
  - Hinweis-`Border` (Muster `NotificationsIosOnlyHint`): `IsVisible="False"` + `DataTrigger` `DebugEmailSupported == False` → `IsVisible = True`, Text `SettingsDebugEmailUnsupportedHint`.
- Referenz-Layout: `design-draft/stitch_local_rss_feed_reader/einstellungen_filter[_dark_mode]/screen.png` (Kartenoptik); ein eigener Entwurfs-Screen existiert nicht — bestehende Konvention übernehmen.

### `SettingsPage.xaml.cs` (`src/Reporter/Views/SettingsPage.xaml.cs`)

- **Geänderte Methoden:** `OnAppearing` — `viewModel.DebugReportFailed += OnDebugReportFailed`; `OnDisappearing` — entsprechend deabonnieren.
- **Neue Event-Handler:** `OnDebugReportFailed` — `DisplayAlertAsync(AppResources.DebugReportFailedTitle, AppResources.DebugReportFailedMessage, AppResources.ButtonOk)` (Muster `OnNotificationAuthorizationDenied`, ohne „Einstellungen öffnen"-Aktion).

### `MauiProgram` (`src/Reporter/MauiProgram.cs`)

- **Geänderte Methoden:** `CreateMauiApp` — im `builder.Services`-Block ergänzen: `AddSingleton<IDebugLogRepository, DebugLogRepository>()`, `AddSingleton<IDebugLogService, DebugLogService>()`, `AddSingleton<IEmailService, EmailService>()`, `AddSingleton<IDeviceInfoProvider, DeviceInfoProvider>()`, `AddSingleton<IDebugReportService, DebugReportService>()`. DI löst die optionalen `IDebugLogService?`-Parameter von `FeedSyncService`/`AutoRefreshService`/`SettingsViewModel` automatisch auf.

### `AppResources.resx` / `AppResources.de.resx` (`src/Reporter.Core/Resources/Strings/`)

- Neue Schlüssel nach `Settings*`/`NotificationDenied*`-Konvention (englischer Default + deutsche Übersetzung; `AppResources.Designer.cs` wird durch `PublicResXFileCodeGenerator` neu generiert):
  - `SettingsSectionDebug` — Abschnittstitel („Diagnose & Support" / „Diagnostics & support")
  - `SettingsDebugCollectionLabel`, `SettingsDebugCollectionHint` — Schalter-Label/-Hint; der Hint benennt die Session-Semantik („aktuelle Sitzung; Absturz-Einträge der vorherigen Sitzung bleiben erhalten")
  - `SettingsDebugSendLabel`, `SettingsDebugSendHint`, `SettingsDebugSendButton` — Versand-Zeile/-Button; der Sende-Hint benennt den Umfang („Diagnosedaten der aktuellen Sitzung")
  - `SettingsDebugCollectionRequiredHint` — Hinweis, dass die Sammlung vor dem Versand aktiviert werden muss und dass das Protokoll die aktuelle Sitzung plus übernommene Absturzinformationen umfasst — ein zu meldendes Problem muss ggf. in dieser Sitzung erneut auftreten
  - `SettingsDebugEmailUnsupportedHint` — permanenter Hinweis ohne Mail-Client
  - `DebugReportFailedTitle`, `DebugReportFailedMessage` — Alert im Fehlerpfad
  - `DebugReportEmailSubject` — Betreff des Reports (kann `{0}` für App-Name/Version enthalten)
  - `DebugReportSectionAppInfo`, `DebugReportSectionDevice`, `DebugReportSectionNetwork`, `DebugReportSectionSettings`, `DebugReportSectionFeedHealth`, `DebugReportSectionSyncLog`, `DebugReportSectionSessionLog` — lokalisierte Abschnitts-Header aller sieben Report-Body-Sektionen (App-Info, Gerät/OS, Online-Status, Settings-Snapshot, Feed-Health, Sync-Verlauf, Session-Debug-Log); Entscheidung: lokalisierte Schlüssel statt Literale, konsistent zum lokalisierten Betreff und zur `Settings*`-Konvention

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `AddSettingsDebugCollection` | `settings.debug_collection_enabled` | Neue `bool`-Spalte, `NOT NULL`, `DEFAULT false`; per `dotnet ef migrations add AddSettingsDebugCollection` über `ReporterDbContextFactory` erzeugt; Namenskonvention `AddSettings*`; Anwendung beim App-Start durch `context.Database.Migrate()`/`MigrateAsync()` |
| `AddDebugLogEntries` | neue Tabelle `debug_log_entries` (`id`, `timestamp`, `level`, `category`, `message`, `details`) | Neue Log-Tabelle für das Session-Debug-Log inkl. Index auf `timestamp`; per `dotnet ef migrations add AddDebugLogEntries` erzeugt |

## Validierungsregeln

Keine — das Feature führt keine Text-/Zahleneingaben ein (nur `Switch` und `Button`); die Empfängeradresse ist eine Konstante, keine Benutzereingabe.

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `Settings.DebugCollectionEnabled` (`settings.debug_collection_enabled`) | `bool` | `false` | Opt-in-Schalter der Debug-Sammlung und Session-Protokollierung; gesteuert über `Switch` auf der `SettingsPage`, persistiert über `PersistOnChange`/`PersistAsync` — dieselbe Konfigurationsebene wie `NotificationsEnabled`/`AutoRefreshEnabled` |
| `DebugReportService.DebugReportRecipient` | `string`-Konstante | Platzhalter-Adresse (vom Maintainer zu ersetzen) | Empfänger der Report-E-Mail; nicht benutzerkonfigurierbar |
| `DebugReportService.MaxSyncLogEntries` | `int`-Konstante | `50` | Begrenzung der `SyncLog`-Einträge im Report-Body |
| `DebugReportService.MaxDebugLogEntries` | `int`-Konstante | `200` | Begrenzung der Session-Log-Einträge im Report-Body |
| `DebugLogService.MaxStoredEntries` | `int`-Konstante | `500` | Begrenzung des Tabellenwachstums von `debug_log_entries` innerhalb einer Session (`TrimToLatestAsync` nach jedem Eintrag) |

## Seiteneffekte und Risiken

- **`required`-Property `Settings.DebugCollectionEnabled`:** Alle `new Settings`-Initializer müssen die Property setzen — betroffen sind `SettingsViewModel.PersistAsync`, `SettingsRepository.MapToModel`, `ArticleDetailViewModel` (Fallback) sowie die Test-Initializer in `TestSettingsHelper`, `SettingsViewModelTests_Load` (2×), `SettingsViewModelTests_Persist` (4×), `SettingsRepositoryTests` (5×), `RetentionCleanupServiceTests` (1×), `AutoRefreshServiceTests` (2×). Ohne Ergänzung bricht die Kompilierung.
- **`sync_logs`-Vollscan:** Entschärft — der `DebugReportService` nutzt `ISyncLogRepository.GetLatestAsync(MaxSyncLogEntries)` mit `Take` in der Abfrage; `GetAllAsync` bleibt für andere Aufrufer unverändert.
- **Migration beim Start:** `Migrate()` läuft synchron in `ApplyPersistedLanguage` (und `MigrateAsync()` in `App.OnStart`); neue Spalte und neue Tabelle sind additiv mit Default — kein Datenverlust-Risiko, aber der Upgrade-Pfad muss im Schema-Test abgesichert werden. `BeginSessionAsync` muss zwingend nach der Migration laufen.
- **Log-Schreibkosten:** Jeder `LogAsync` verursacht `AddAsync` + `TrimToLatestAsync` — der Trim ist per `CountAsync`-Prüfung ein No-op, solange die Tabelle `MaxStoredEntries` nicht überschreitet (die teure Select-Ids-+-Delete-Sequenz läuft nur oberhalb des Limits). Bei der erwarteten Ereignisfrequenz (Exceptions, Sync-Fehler, Lifecycle — einstellig bis zweistellig pro Session) vernachlässigbar; eine Fehlerschleife wird durch `MaxStoredEntries` begrenzt.
- **Frühe Exceptions:** Unbehandelte Fehler vor `BeginSessionAsync` (z. B. in `MauiProgram.CreateMauiApp`) können nicht persistiert werden; das Zeitfenster ist minimal, da die Migration vorher läuft.
- **Fire-and-forget in Exception-Handlern:** `UnhandledException`/`UnobservedTaskException`-Handler rufen `LogAsync` ohne `await` — bei einem sofortigen harten Absturz kann der letzte Eintrag verloren gehen. Bewusste Einschränkung; blockierendes `.Wait()` in einem Crash-Handler wäre riskanter.
- **`SettingsViewModel`-/Service-Konstruktoren:** Neue optionale Parameter sind abwärtskompatibel; DI löst `IDebugReportService`/`IDebugLogService` über die `MauiProgram`-Registrierungen auf.
- **`Email.ComposeAsync`-Aufrufkontext:** Muss auf dem UI-/Hauptthread laufen; `SendDebugReportCommand` wird aus der UI ausgelöst — kein Hintergrundaufruf vorgesehen.
- **Datenschutz:** Der Report enthält Feed-URLs/-Titel, `SyncLog.Message`-Texte und Session-Log-Einträge (Exception-Details können Pfade/Stacktraces enthalten); keine Artikelinhalte. Transparenz ergibt sich aus dem sichtbaren Mail-Entwurf.
- **`Debug.WriteLine` bleibt:** Die nicht instrumentierten `Debug.WriteLine`-Stellen (Artikeldetail, Suche, Page-`OnAppearing`) erscheinen nicht im Session-Log — bewusste Umfangsbegrenzung; bei Bedarf später nachrüstbar.
- **Übernommene `Error`-Einträge altern nicht per Session:** `DeleteAllExceptErrorsAsync` erhält alle `Error`-Einträge — auch ältere als eine Session — bis `TrimToLatestAsync(MaxStoredEntries = 500)` die ältesten Einträge entfernt. Bei der erwarteten Eintragsfrequenz praktisch irrelevant; engeres „nur die letzte Sitzung"-Verhalten würde eine Session-Markierung erfordern und wurde bewusst nicht eingeführt.
- **Testsuite:** Das MAUI-Projekt ist nicht Teil von `dotnet test`; `EmailService`/`DeviceInfoProvider`/`App.xaml.cs`/XAML bleiben ungetestet — manuelle Verifikation gemäß `AGENTS.md`.

## Umsetzungsreihenfolge

1. **Core-Datenmodelle, Konstanten und Interfaces anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: `AppDeviceInfo` und `DebugLogEntry` in `src/Reporter.Core/Models/`; `DebugLogLevel`/`DebugLogCategory` in `src/Reporter.Core/Services/`; `IEmailService`, `IDeviceInfoProvider`, `IDebugLogRepository`, `IDebugLogService`, `IDebugReportService` in `src/Reporter.Core/Interfaces/` — jeweils mit vollständigen XML-Docs nach dem Muster von `ILocalNotificationService`/`ISyncLogRepository`.

2. **Lokalisierungsschlüssel ergänzen**
   - Voraussetzungen: Keine.
   - Beschreibung: Alle neuen Schlüssel in `AppResources.resx` (EN) und `AppResources.de.resx` (DE) eintragen; `AppResources.Designer.cs` wird beim Build neu generiert.

3. **Persistenzpfad: `Settings`-Erweiterung + `debug_log_entries`**
   - Voraussetzungen: Schritt 1 (`DebugLogEntry`-Modell); `dotnet-ef`-Tooling und `ReporterDbContextFactory` sind im Repo vorhanden.
   - Beschreibung: `Settings` (Core-Modell, `required bool`) und `Settings` (Entity, Default `false`) erweitern; `DebugLogEntry`-Entity anlegen; `ReporterDbContext`: `DbSet<DebugLogEntry>`, `ConfigureDebugLogEntry`, `ConfigureSettings`-Mapping (`debug_collection_enabled`, `IsRequired().HasDefaultValue(false)`); `dotnet ef migrations add AddSettingsDebugCollection --project src/Reporter.Data` und `dotnet ef migrations add AddDebugLogEntries --project src/Reporter.Data`; `SettingsRepository.SaveAsync`/`MapToModel` ergänzen; `DebugLogRepository` implementieren; Fallback-Initializer in `ArticleDetailViewModel` um `DebugCollectionEnabled = false` ergänzen.

4. **`DebugLogService` in `Reporter.Core` implementieren**
   - Voraussetzungen: Schritte 1 und 3 (`IDebugLogService`, `IDebugLogRepository`, `DebugLogEntry`, `Settings.DebugCollectionEnabled`); `ISettingsRepository` vorhanden.
   - Beschreibung: `_enabled`-Flag, `BeginSessionAsync` (`DeleteAllExceptErrorsAsync` + Setting laden + Start-Eintrag), `SetEnabled` (Übergangseintrag), `LogAsync` (No-op wenn deaktiviert; `AddAsync` + `TrimToLatestAsync(MaxStoredEntries)`; nie werfend), `MaxStoredEntries = 500`, optionaler `TimeProvider?`.

5. **`DebugReportService` in `Reporter.Core` implementieren**
   - Voraussetzungen: Schritte 1–4; vorhandene Contracts `ISettingsRepository`, `ISyncLogRepository`, `IFeedRepository`, `INetworkStatusService` sind bereits im Repo.
   - Beschreibung: `IsSupported`, `SendReportAsync` (Sammlung inkl. `IDebugLogRepository`-Sektion, Top-50-/Top-200-Begrenzungen, Plain-Text-Formatierung mit den sieben lokalisierten `DebugReportSection*`-Headern, `ComposeAsync`-Aufruf, `Report`-Logeintrag, unerwartete Exceptions propagieren zum Aufrufer), Konstanten `DebugReportRecipient`, `MaxSyncLogEntries`, `MaxDebugLogEntries`, optionale `TimeProvider?`/`IDebugLogService?`.

6. **MAUI-Implementierungen `EmailService` und `DeviceInfoProvider`**
   - Voraussetzungen: Schritt 1; .NET MAUI Essentials ist im `Reporter`-Projekt enthalten (kein NuGet-Paket nötig).
   - Beschreibung: Beide Klassen in `src/Reporter/Services/` nach dem Muster `NetworkStatusService`/`AppThemeService`; `EmailService` nutzt `Email.Default.ComposeAsync` mit `EmailMessage` (`BodyFormat.PlainText`) und kapselt Plattformfehler auf `false`; `DeviceInfoProvider` mappt `AppInfo.Current`/`DeviceInfo.Current` auf `AppDeviceInfo`.

7. **Instrumentierung der Core-Fehlerpfade**
   - Voraussetzungen: Schritt 4.
   - Beschreibung: `FeedSyncService` und `AutoRefreshService` um optionalen `IDebugLogService?`-Parameter erweitern und in den benannten Catch-Pfaden `LogAsync` aufrufen (Error/Warning wie in den Programmabläufen beschrieben); `Debug.WriteLine` bleibt parallel bestehen.

8. **`App.xaml.cs`: Session-Start, globale Exception-Hooks, Lifecycle**
   - Voraussetzungen: Schritte 4 und 11 (DI-Registrierung für die `IDebugLogService`-Auflösung — siehe Hinweis).
   - Beschreibung: In `OnStart` nach `MigrateAsync()` `IDebugLogService` auflösen + `BeginSessionAsync`; `UnhandledException`/`UnobservedTaskException` abonnieren; `OnSleep`/`OnResume`-Overrides; `LogAsync` in den OnStart-Catches. (DI-Registrierung aus Schritt 11 kann parallel erfolgen; `GetRequiredService` schlägt sonst fehl — daher im selben Arbeitsgang oder nach Schritt 11.)
   - Hinweis: Schritt vor oder zusammen mit Schritt 11 ausführen, damit `GetRequiredService<IDebugLogService>` auflösbar ist.

9. **`SettingsViewModel` erweitern**
   - Voraussetzungen: Schritte 1, 4 und 5.
   - Beschreibung: Optionale Ctor-Parameter `IDebugReportService?`/`IDebugLogService?`; Properties `DebugCollectionEnabled` (mit `SetEnabled`-Aufruf), `DebugEmailSupported`, `DebugSendEnabled`; `SendDebugReportCommand` + `SendDebugReportAsync`; Event `DebugReportFailed`; `LoadAsync`/`PersistAsync` ergänzen.

10. **`SettingsPage` (XAML + Code-Behind)**
    - Voraussetzungen: Schritte 2 und 9.
    - Beschreibung: Neuer Abschnitt „Diagnose & Support" in der bestehenden Karten-/Zeilen-Konvention (Switch-Zeile, Senden-Zeile mit `IsEnabled`-/`Opacity`-Muster, Unsupported-Hinweis-`Border` mit `DataTrigger`); Code-Behind: `DebugReportFailed` abonnieren/deabonnieren + `OnDebugReportFailed` → `DisplayAlertAsync`.

11. **DI-Registrierung in `MauiProgram`**
    - Voraussetzungen: Schritte 3–6.
    - Beschreibung: `AddSingleton<IDebugLogRepository, DebugLogRepository>()`, `AddSingleton<IDebugLogService, DebugLogService>()`, `AddSingleton<IEmailService, EmailService>()`, `AddSingleton<IDeviceInfoProvider, DeviceInfoProvider>()`, `AddSingleton<IDebugReportService, DebugReportService>()` im bestehenden `builder.Services`-Block.

12. **Test-Infrastruktur: Fakes und Helper**
    - Voraussetzungen: Schritt 1; Schritt 3 (für `TestSettingsHelper`-Erweiterung).
    - Beschreibung: `FakeEmailService` (`IsSupported` settable, `ComposeResult` settable, `ComposeCallCount`, Liste `ComposedEmails` mit Record `ComposedEmail(recipient, subject, body)` — Muster `FakeLocalNotificationService`/`ShownNotification`); `FakeDeviceInfoProvider` (setzbares `AppDeviceInfo`-Snapshot — Muster `FakeNetworkStatusService`); `FakeDebugLogService` (setzbares `IsEnabled`, `BeginSessionCallCount`, `SetEnabledCalls`-Liste, `LoggedEntries`-Liste mit Record `LoggedEntry(category, message, details, level)`); `TestSettingsHelper.SaveAsync` um optionalen Parameter `debugCollectionEnabled` erweitern; alle bestehenden `new Settings`-Initializer in Tests um `DebugCollectionEnabled` ergänzen.

13. **Neue und erweiterte Tests schreiben**
    - Voraussetzungen: Schritte 3–5, 7, 9, 12.
    - Beschreibung: `DebugLogRepositoryTests`, `DebugLogServiceTests`, `DebugReportServiceTests` neu; `SettingsViewModelTests_Load`/`_Persist`/`_E2E`, `FeedSyncServiceTests`, `AutoRefreshServiceTests`, `SettingsRepositoryTests`, `ReporterDbContextTests_Persistence`/`_Schema`, `ServiceCollectionTests` erweitern — Details im Abschnitt Tests.

14. **Verifikation**
    - Voraussetzungen: Schritte 1–13.
    - Beschreibung: `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release`; `.\scripts\Run-StaticChecks.ps1` (Format/Security/Static Analysis inkl. MAUI-Build); manuelle UI-Verifikation 390 × 844 pt (Light + Dark) mit Screenshots in `test-results.md`/`docs/help/anwendung/mobile-ui-design.md` gemäß `AGENTS.md`; manueller Versand-Test auf einem Gerät mit eingerichtetem Mail-Client; manuelle Prüfung, dass bei aktiviertem Schalter Session-Log-Einträge (Start, Sync-Fehler, Exception) im Report erscheinen und nach App-Neustart nur die neue Session enthalten ist.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `FakeEmailService` (+ Record `ComposedEmail`) | Test-Double (`src/Reporter.Tests/FakeEmailService.cs`) | `IsSupported`/`ComposeResult` settable; `ComposeException` (`Exception?` settable — `ComposeAsync` wirft sie, simuliert unerwartete Gateway-Fehler für den VM-Exception-Pfad); `ComposeCallCount`; `ComposedEmails`-Liste — Muster `FakeLocalNotificationService` |
| `FakeDeviceInfoProvider` | Test-Double (`src/Reporter.Tests/FakeDeviceInfoProvider.cs`) | Setzbares `AppDeviceInfo`-Snapshot — Muster `FakeNetworkStatusService` |
| `FakeDebugLogService` (+ Record `LoggedEntry`) | Test-Double (`src/Reporter.Tests/FakeDebugLogService.cs`) | `IsEnabled` settable; `BeginSessionCallCount`; `SetEnabledCalls`-Liste; `LoggedEntries`-Liste — Muster `FakeLocalNotificationService` |
| `TestSettingsHelper.SaveAsync` um `debugCollectionEnabled` | Hilfsmethode | Selektiver Override der neuen Property (Muster der vorhandenen Parameter) |
| `AddAsync_ThenGetAllAsync_ReturnsOrderedDescending` | `DebugLogRepositoryTests` (neu) | `AddAsync` + `GetAllAsync` liefert Einträge absteigend nach `Timestamp` (Muster `SyncLogRepositoryTests`) |
| `DeleteAllExceptErrorsAsync_RemovesNonErrorEntries` | `DebugLogRepositoryTests` | Session-Reset löscht `Info`/`Warning`-Einträge, `Error`-Einträge bleiben erhalten |
| `TrimToLatestAsync_KeepsNewestEntries` | `DebugLogRepositoryTests` | Bei N > max bleiben die jüngsten `maxEntries`, ältere werden gelöscht |
| `BeginSessionAsync_ResetsPreviousEntries_KeepsErrors` | `DebugLogServiceTests` (neu) | Vorhandene `Info`/`Warning`-Einträge werden beim Session-Start gelöscht, `Error`-Einträge (Absturzberichte) der Vor-Session bleiben erhalten |
| `BeginSession_LoadsEnabledFromSettings` | `DebugLogServiceTests` | `IsEnabled` spiegelt `Settings.DebugCollectionEnabled`; bei `true` Start-Eintrag geschrieben, bei `false` Tabelle leer |
| `LogAsync_Disabled_WritesNothing` | `DebugLogServiceTests` | `IsEnabled == false` → `AddAsync` wird nicht aufgerufen (echtes Repo, Tabelle bleibt leer) |
| `LogAsync_Enabled_WritesEntry` | `DebugLogServiceTests` | Eintrag enthält Timestamp (via `FakeTimeProvider`/`TimeProvider`), Level, Category, Message, Details |
| `LogAsync_TrimsToMaxStoredEntries` | `DebugLogServiceTests` | Nach Überschreiten von `MaxStoredEntries` bleiben nur die jüngsten Einträge |
| `LogAsync_RepositoryThrows_DoesNotThrow` | `DebugLogServiceTests` | Fehler im Repository werden geschluckt (Logger wirft nie) — z. B. über disposed Factory |
| `SetEnabled_EnableTransition_WritesLifecycleEntry` | `DebugLogServiceTests` | Wechsel `false`→`true` schreibt Übergangseintrag; unveränderter Aufruf ist No-op |
| `SendReport_Unsupported_ReturnsFalseAndDoesNotCompose` | `DebugReportServiceTests` (neu) | `IEmailService.IsSupported == false` → `false`, kein Compose-Aufruf |
| `SendReport_ComposesWithRecipientAndLocalizedSubject` | `DebugReportServiceTests` | `FakeEmailService.ComposedEmails` enthält genau einen Eintrag mit `DebugReportRecipient` und dem resx-Betreff |
| `SendReport_BodyContainsDeviceAppAndNetworkInfo` | `DebugReportServiceTests` | Body enthält App-Version/Build, Gerätemodell, Plattform, OS-Version und Online-Status (`FakeDeviceInfoProvider`, `FakeNetworkStatusService`) |
| `SendReport_BodyContainsSettingsSnapshot` | `DebugReportServiceTests` | Body enthält die persistierten Settings-Werte (inkl. `DebugCollectionEnabled`) |
| `SendReport_BodyContainsFeedHealth` | `DebugReportServiceTests` | Body enthält pro Feed Titel/URL, `HealthStatus`, `LastCheckedAt` (Feeds via `TestDataSeeder`/Repository anlegen) |
| `SendReport_BodyContainsLatestSyncLogs` | `DebugReportServiceTests` | Body enthält die jüngsten `SyncLog`-Einträge in absteigender `StartedAt`-Reihenfolge |
| `SendReport_LimitsSyncLogToMaxEntries` | `DebugReportServiceTests` | Bei > 50 Einträgen erscheinen nur die 50 jüngsten; ältere Einträge fehlen im Body |
| `SendReport_BodyContainsSessionDebugLog` | `DebugReportServiceTests` | Body enthält die Session-Log-Einträge (Level/Category/Message/Timestamp) aus `debug_log_entries` |
| `SendReport_LimitsDebugLogToMaxEntries` | `DebugReportServiceTests` | Bei > 200 Einträgen erscheinen nur die 200 jüngsten |
| `SendReport_EmptyLogs_StillComposes` | `DebugReportServiceTests` | Leere `sync_logs`/`debug_log_entries` → Compose wird trotzdem aufgerufen (Body ohne Log-Zeilen) |
| `SendReport_ComposeReturnsFalse_ReturnsFalse` | `DebugReportServiceTests` | `ComposeResult = false` → `SendReportAsync` liefert `false` |
| `SyncFeed_Failure_LogsErrorToDebugLog` | `FeedSyncServiceTests` | Sync-Fehler (z. B. HTTP-Fehler via `FakeHttpMessageHandler`) → `FakeDebugLogService.LoggedEntries` enthält `Error`/`Sync`-Eintrag mit Exception-Details |
| `SyncFailure_LogsErrorToDebugLog` | `AutoRefreshServiceTests` | `IFeedSyncService.SyncAllAsync` wirft → `FakeDebugLogService.LoggedEntries` enthält `Error`/`Sync`-Eintrag |
| `Load_PopulatesDebugCollectionEnabled` | `SettingsViewModelTests_Load` | Persistierter Wert wird in die VM-Property geladen |
| `Load_EmailUnsupported_ExposesDebugEmailSupportedFalse` | `SettingsViewModelTests_Load` | `FakeEmailService.IsSupported = false` → `DebugEmailSupported == false`, `DebugSendEnabled == false` (Muster `Load_NotificationsSupported_ReflectsPlatformSupport`) |
| `DebugCollectionEnabled_Change_Persists` | `SettingsViewModelTests_Persist` | Toggle persistiert sofort über `SaveAsync` (Muster `NotificationSummaryEnabled_Change_Persists`) |
| `DebugCollectionEnabled_Change_CallsSetEnabled` | `SettingsViewModelTests_Persist` | Toggle ruft `FakeDebugLogService.SetEnabled` mit neuem Wert |
| `SendDebugReport_Supported_CallsServiceAndComposes` | `SettingsViewModelTests_Persist` | `SendDebugReportCommand` → `SendReportAsync` → `FakeEmailService.ComposedEmails` gefüllt |
| `SendDebugReport_Unsupported_RaisesDebugReportFailed` | `SettingsViewModelTests_Persist` | `DebugCollectionEnabled == true`, `IsSupported = false` → Event `DebugReportFailed` wird ausgelöst (Muster `NotificationsEnabled_TurnedOn_Denied_RaisesNotificationAuthorizationDenied`) |
| `SendDebugReport_ServiceThrows_RaisesDebugReportFailedAndLogs` | `SettingsViewModelTests_Persist` | `DebugCollectionEnabled == true`, `FakeEmailService.ComposeException` gesetzt → `SendReportAsync` propagiert die Exception → `DebugReportFailed` wird ausgelöst **und** `FakeDebugLogService.LoggedEntries` enthält einen `Error`/`Report`-Eintrag mit `ex.ToString()` (VM-Exception-Pfad des Programmablaufs „Debugbericht versenden", Schritt 7) |
| `SendDebugReport_CollectionDisabled_DoesNotSend` | `SettingsViewModelTests_Persist` | `DebugCollectionEnabled == false` → `DebugSendEnabled == false`; `SendDebugReportCommand.ExecuteAsync` löst dank des `DebugCollectionEnabled`-Guards in `SendDebugReportAsync` keinen Versand und kein Event aus (`FakeEmailService.ComposeCallCount == 0`) |
| `E2E_DebugCollection_PersistRoundtrip` | `SettingsViewModelTests_E2E` | VM-Toggle → echtes Repository → In-Memory-SQLite → Reload → Wert erhalten (Muster `E2E_NotificationSummary_PersistRoundtrip`) |
| `E2E_SendDebugReport_ComposesCollectedReport` | `SettingsViewModelTests_E2E` | VM-Command → echter `DebugReportService` → echte Repositories (inkl. `debug_log_entries`-Seeding) → `FakeEmailService`/`FakeDeviceInfoProvider`/`FakeDebugLogService`: Compose-Aufruf mit gesammelten Settings-/Feed-/SyncLog-/Session-Log-Daten |
| `E2E_DebugSessionLog_WriteAndReset` | `SettingsViewModelTests_E2E` oder `DebugLogServiceTests` | `BeginSessionAsync` → `LogAsync` → Eintrag in `debug_log_entries` (echtes Repo/SQLite); zweites `BeginSessionAsync` löscht Nicht-`Error`-Einträge, übernimmt den `Error`-Eintrag der Vor-Session und schreibt den neuen Start-Eintrag |
| `SaveAsync_PersistsDebugCollectionEnabled` | `SettingsRepositoryTests` | `SaveAsync` → `GetAsync` Roundtrip der Property (Muster `SaveAsync_PersistsNotificationSummaryEnabled`) |
| `Settings_DebugCollectionEnabled_PersistRoundtrip` | `ReporterDbContextTests_Persistence` | Entity-Roundtrip inkl. Default `false` und explizitem `true` |
| `DebugLogEntry_PersistRoundtrip` | `ReporterDbContextTests_Persistence` | `DebugLogEntry`-Entity speichern/lesen inkl. `Details`-Feld |
| `AddReporterRepositories_ResolvesDebugLogRepository` | `ServiceCollectionTests` | `IDebugLogRepository` ist auflösbar (Registrierung in `AddTestRepositories` spiegelt `MauiProgram`) |
| `AddReporterServices_ResolvesDebugReportService` | `ServiceCollectionTests` | `IDebugReportService` mit vollständigem Ctor-Dependency-Set (Repos + `FakeEmailService`/`FakeDeviceInfoProvider`/`FakeNetworkStatusService`/`FakeDebugLogService`) auflösbar — spiegelt `MauiProgram`-Registrierung |
| `AddReporterServices_ResolvesDebugLogService` | `ServiceCollectionTests` | `IDebugLogService` mit `IDebugLogRepository` + `ISettingsRepository` auflösbar |

Hinweis zum Seeding: `sync_logs`/`debug_log_entries` werden in den Report-/E2E-Tests direkt über `ISyncLogRepository.AddAsync`/`IDebugLogRepository.AddAsync` befüllt; eine `TestDataSeeder`-Erweiterung ist nur bei wiederholendem Setup sinnvoll und nicht vorgeplant.

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `TestSettingsHelper` | `new Settings`-Initializer muss `DebugCollectionEnabled` setzen (`required`-Property); Signatur um optionalen Override erweitern |
| `SettingsViewModelTests_Load` (2 `new Settings`-Initializer) | `required`-Property ergänzen |
| `SettingsViewModelTests_Persist` (4 `new Settings`-Initializer) | `required`-Property ergänzen |
| `SettingsRepositoryTests` (5 `new Settings`-Initializer) | `required`-Property ergänzen |
| `RetentionCleanupServiceTests` (1 Initializer) | `required`-Property ergänzen |
| `AutoRefreshServiceTests` (2 Initializer) | `required`-Property ergänzen |
| `ServiceCollectionTests.AddReporterRepositories_ResolvesAllRepositories` | `AddTestRepositories` um `IDebugLogRepository`-Registrierung erweitern, damit `IFeedSyncService`/`IDebugLogService`-Auflösung die `MauiProgram`-Registrierung spiegelt |
| `ServiceCollectionTests.AddReporterServices_ResolvesFeedSyncService` | `FeedSyncService` erhält optionalen `IDebugLogService?`-Parameter — `FakeDebugLogService` registrieren, damit DI den Parameter befüllt (optional: Test bleibt ohne Anpassung grün, spiegelt dann aber nicht mehr die Produktivregistrierung) |
| `ReporterDbContextTests_Schema.EnsureCreatedAsync_CreatesQueryableTables` | Neue Tabelle `debug_log_entries` in die abgefragten Tabellen aufnehmen |

### E2E-Tests (primärer Funktionsnachweis)

Das Projekt hat keine automatisierte UI-Testinfrastruktur: das MAUI-Projekt `src/Reporter/Reporter.csproj` ist nicht Teil der Testsuite, und `Email.ComposeAsync` öffnet den System-Mail-Client — nur auf einem Gerät mit eingerichtetem Mail-Client verifizierbar. Als E2E-Schicht etabliert ist `SettingsViewModelTests_E2E` (ViewModel ↔ echte Repositories ↔ In-Memory-SQLite), ergänzt um Gateway-Fakes — sie deckt den Benutzerfluss von der UI-Aktion bis zur Persistenz/Mail-Übergabe ab; die letzte Strecke (sichtbarer Mail-Entwurf im System-Client, `App.xaml.cs`-Hooks) bleibt manuelle Verifikation.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|---------------------|
| Pflicht | Schalter „Debuginformationen sammeln" aktivieren → Wert persistiert, übersteht Reload und schaltet die Protokollierung | `SettingsViewModelTests_E2E.E2E_DebugCollection_PersistRoundtrip` + `SettingsViewModelTests_Persist.DebugCollectionEnabled_Change_CallsSetEnabled` | Opt-in-Schalter steuert `Settings.DebugCollectionEnabled` persistent und `IDebugLogService.SetEnabled` zur Laufzeit | Benutzerfluss über VM → Repository → SQLite — einzige automatisierbare End-to-End-Strecke des Toggles |
| Pflicht | Versand-Aktion auslösen → Report wird gesammelt (inkl. Session-Log) und an Mail-Gateway übergeben | `SettingsViewModelTests_E2E.E2E_SendDebugReport_ComposesCollectedReport` | `SendDebugReportCommand` → `DebugReportService` sammelt Settings/Feed-Health/Sync-Log/`debug_log_entries`/Geräteinfo → `IEmailService.ComposeAsync` mit Empfänger, Betreff, Body | Kernfluss der Anforderung; Prüfung der tatsächlich gesammelten Inhalte ist nur auf dieser Ebene möglich |
| Pflicht | Session-Log: App-Start setzt zurück, Ereignisse werden geschrieben | `DebugLogServiceTests`/`SettingsViewModelTests_E2E.E2E_DebugSessionLog_WriteAndReset` | `BeginSessionAsync` leert `debug_log_entries`; `LogAsync` schreibt nur bei aktiviertem Schalter; Reset-Nachweis über zweites `BeginSessionAsync` | Kernfunktion der neuen Infrastruktur — nur über echtes Repository + SQLite prüfbar |
| Pflicht | Versand ohne Mail-Client / mit unerwartetem Fehler → sichtbarer Fehlerhinweis | `SettingsViewModelTests_Persist.SendDebugReport_Unsupported_RaisesDebugReportFailed` + `SendDebugReport_ServiceThrows_RaisesDebugReportFailedAndLogs` (+ XAML-Hinweis-`Border` manuell) | Fehlerpfad bei `IsSupported == false` / `ComposeAsync == false` und bei unerwarteter Exception → `DebugReportFailed` → Alert | Anwendersichtbarer Fehlerfall, über UI auslösbar |
| Pflicht (manuell) | Mail-Client öffnet vorbefüllten Entwurf; Session-Log enthält Start-/Fehler-Einträge; Abschnitt in 390 × 844 pt Light + Dark | Manuelle Verifikation, Protokoll in `test-results.md` + `docs/help/anwendung/mobile-ui-design.md`, Screenshots unter `test-results/` | System-Mail-Client zeigt Entwurf mit Empfänger/Betreff/Body inkl. Session-Log-Sektion; UI entspricht Sektions-Konvention, Touch-Ziele ≥ 44 pt, `AppThemeBinding`; nach App-Neustart enthält das Log nur die neue Session | `Email.ComposeAsync`, `App.xaml.cs`-Hooks (`UnhandledException`, `OnSleep`/`OnResume`) und XAML-Rendering sind nur auf Gerät/Windows-Fenster prüfbar — AGENTS.md-Pflicht |

Bestehende E2E-Tests (`SettingsViewModelTests_E2E`, `KeywordFilterTests_E2E`) müssen nicht angepasst werden — sie brechen allenfalls an den `new Settings`-Initializern mit, die bereits unter „Betroffene bestehende Tests" gelistet sind.

## Offene Punkte

Keine.
