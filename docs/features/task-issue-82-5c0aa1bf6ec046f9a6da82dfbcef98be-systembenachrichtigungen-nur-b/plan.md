<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Systembenachrichtigungen nur bei Hintergrundabruf (Issue #82)

## Übersicht

Lokale Benachrichtigungen über neue Feed-Artikel sollen künftig nur noch erscheinen, wenn der Feed-Abruf im OS-Hintergrund stattfindet. Die Umsetzung umfasst zwei Teilmaßnahmen: (1) die Vordergrund-Darstellung in `NotificationDelegate.WillPresentNotification` unterdrücken und (2) einen echten iOS-Hintergrundabruf über `BGTaskScheduler`/`BGAppRefreshTask` aufbauen (Registrierung in `AppDelegate`, Berechtigung über `Info.plist`, Scheduling- und Sync-Logik in einem neuen `IBackgroundRefreshService`-Gateway nach dem etablierten Gateway-Muster). Betroffen sind ausschließlich der iOS-Plattformcode sowie `AutoRefreshService` (Weiterleitung der Settings an das neue Gateway); Windows, Android und MacCatalyst bleiben No-Op — vom Anwender als außer Scope bestätigt.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Vordergrund-Unterdrückung | `NotificationDelegate.WillPresentNotification` liefert `UNNotificationPresentationOptions.None` (statt Auslöser-Unterscheidung per `SyncOrigin`-Parameter bis in `NotificationService`) — vom Anwender als `WillPresentNotification`-Variante entschieden | iOS ruft `WillPresentNotification` nur bei Vordergrund-App auf — die Methode ist damit bereits die exakte Semantik der Anforderung (vom Anwender festgelegt: nur der OS-Hintergrundabruf erzeugt sichtbare Systemmitteilungen; alle Abrufe bei laufender App — manuell, Timer, Start-Abruf — werden unterdrückt). Minimalinvasiv: keine Signaturänderungen an `IFeedSyncService`/`INotificationService`, keine Anpassung der Aufrufer (`FeedsViewModel`, `UnreadViewModel`, `AutoRefreshService`). Mitteilungen, die im Hintergrund-/Suspend-Zustand zugestellt werden, zeigt iOS automatisch an. `None` (vom Anwender entschieden) bedeutet: kein Banner, kein Sound und kein stiller Eintrag im Mitteilungszentrum bei Vordergrund-Syncs. |
| OS-Hintergrundabruf-Abstraktion | Neues Gateway `IBackgroundRefreshService` (Interface in `src/Reporter.Core/Interfaces/`, Implementierung `BackgroundRefreshService` in `src/Reporter/Services/` mit `#if IOS`, No-Op auf anderen Targets) | Folgt exakt dem etablierten Gateway-Muster (`ILocalNotificationService` → `LocalNotificationService`, `INetworkStatusService` → `NetworkStatusService`): Interface plattformneutral in Core, Implementierung im MAUI-Projekt, `IsSupported`-Kennung. |
| Kopplung des Hintergrundabrufs an Einstellungen | `BGAppRefreshTask` wird an `Settings.AutoRefreshEnabled`/`RefreshIntervalMinutes` gekoppelt; `EarliestBeginDate` = geclampptes Intervall (1–1440, gleiche Grenzen wie `AutoRefreshService`) — vom Anwender entschieden, kein neuer Schalter | Kein neuer `Settings`-Schalter, keine Migration; der bestehende Auto-Refresh-Schalter steuert dann beide Abrufmechanismen konsistent. iOS behandelt `EarliestBeginDate` ohnehin nur als Untergrenze — ein separates Intervall hätte keinen Garantievorteil. |
| Auslösepunkt der Scheduling-Aktualisierung | `AutoRefreshService` erhält `IBackgroundRefreshService` als neue Konstruktor-Abhängigkeit und leitet `ApplySettingsAsync(settings)` fehlerisoliert weiter | `AutoRefreshService` bleibt der einzige Orchestrierungspunkt für automatische Abrufe: `App.OnStart` (`StartAsync` → `ApplySettingsAsync`) und `SettingsViewModel.PersistAsync` (`ApplySettingsAsync` bei `AutoRefreshEnabled`/`RefreshIntervalMinutes`-Änderung) bleiben unverändert und decken Start- und Änderungsfall bereits ab. Alternative (eigene Aufrufe in `App.OnStart`/`SettingsViewModel`) hätte mehr Code-Stellen ohne Vorteil berührt. |
| Aufgabenverteilung Plattformcode vs. Service | `BGTaskScheduler`-Task-Lebenszyklus (Registrierung, `ExpirationHandler`, `SetTaskCompleted`) im `AppDelegate`; Scheduling (`Submit`/`Cancel`) und Sync-Ausführung in `BackgroundRefreshService` | Das Core-Interface bleibt frei von iOS-Typen (`BGTask`, `BGAppRefreshTaskRequest`); `AppDelegate` löst das Gateway lazy über `IPlatformApplication.Current.Services` im Launch-Handler auf. |

## Programmabläufe

### Vordergrund-Sync ohne Systemmitteilung

1. Ein Sync wird bei laufender App ausgelöst — manuell (`FeedsViewModel.RefreshAsync`/`RefreshAllAsync`, `UnreadViewModel.RefreshAsync`), per Timer (`AutoRefreshService.RunLoopAsync`) oder als Start-Abruf (`AutoRefreshService.RunStartupSyncAsync`).
2. `FeedSyncService.RunSyncAsync` speichert neue Items und ruft bei `newItemEntities.Count > 0` fehlerisoliert `INotificationService.NotifyNewItemsAsync` — unverändert.
3. `NotificationService` wertet das Regelwerk unverändert aus (`Feed.NotificationsEnabled`, `Settings.NotificationsEnabled`, Ruhezeiten, Keyword-Filter, `NotificationSummaryEnabled`) und ruft `ILocalNotificationService.ShowAsync` → `UNUserNotificationCenter.AddNotificationRequestAsync`.
4. iOS ruft `NotificationDelegate.WillPresentNotification` auf — ausschließlich, weil die App im Vordergrund läuft. Der Delegate antwortet mit `UNNotificationPresentationOptions.None`: kein Banner, kein Sound, kein Eintrag im Mitteilungszentrum.
5. `DidReceiveNotificationResponse` (Tap-Handling/Navigation) bleibt unverändert.

Beteiligte Klassen/Komponenten: `FeedsViewModel`, `UnreadViewModel`, `AutoRefreshService`, `IFeedSyncService`/`FeedSyncService`, `INotificationService`/`NotificationService`, `ILocalNotificationService`/`LocalNotificationService`, `NotificationDelegate`

### iOS-Hintergrundabruf (`BGAppRefreshTask`)

1. `AppDelegate.FinishedLaunching` ruft nach `base.FinishedLaunching` die neue private Methode `RegisterBackgroundFetchTask` auf: `BGTaskScheduler.Shared.Register(BackgroundRefreshService.RefreshTaskIdentifier, null, launchHandler)` — Registrierung muss vor Rückkehr aus `FinishedLaunching` erfolgen.
2. `App.OnStart` startet wie bisher `IAutoRefreshService.StartAsync`; `StartAsync` lädt `Settings` und ruft `ApplySettingsAsync`, das nun zusätzlich `_backgroundRefreshService.ApplySettingsAsync(settings)` fehlerisoliert aufruft — und zwar unabhängig vom `AutoRefreshEnabled`-Early-Return, damit ein deaktivierter Auto-Refresh den OS-Task abmeldet.
3. `BackgroundRefreshService.ApplySettingsAsync` (nur `#if IOS`): bei `AutoRefreshEnabled` `BGTaskScheduler.Shared.Submit(new BGAppRefreshTaskRequest(RefreshTaskIdentifier) { EarliestBeginDate = now + clamp(RefreshIntervalMinutes, 1, 1440) })`; bei deaktiviertem Schalter `BGTaskScheduler.Shared.Cancel(RefreshTaskIdentifier)`. Auf anderen Targets No-Op.
4. iOS führt den Task zu einem systembestimmten Zeitpunkt aus (Kaltstart im Hintergrund möglich): Der Launch-Handler im `AppDelegate` castet den `BGTask` zu `BGAppRefreshTask`, erzeugt ein `CancellationTokenSource`, setzt `task.ExpirationHandler = cts.Cancel`, löst `IBackgroundRefreshService` über `IPlatformApplication.Current.Services` auf und ruft `await RunScheduledSyncAsync(cts.Token)`.
5. `RunScheduledSyncAsync` ruft `IFeedSyncService.SyncAllAsync` fehlerisoliert auf; Fehler werden über `IDebugLogService` (`DebugLogCategory.Sync`, `Error`) protokolliert und als `false` an den Aufrufer gemeldet.
6. Neue Items im Sync lösen den bestehenden Benachrichtigungspfad aus (`NotificationService` → `ShowAsync`). Da die App nicht im Vordergrund ist, wird `WillPresentNotification` nicht aufgerufen — iOS zeigt die Mitteilung automatisch als Banner/List/Sound an.
7. Anschließend lädt `RunScheduledSyncAsync` die `Settings` über `ISettingsRepository` und ruft `ApplySettingsAsync(settings)` erneut auf — der Folgeabruf wird auch im Fehlerfall neu eingeplant.
8. Der Launch-Handler schließt mit `task.SetTaskCompleted(success)` ab; der `ExpirationHandler` stellt sicher, dass `SyncAllAsync` bei Ablauf des iOS-Zeitbudgets kancelliert wird.
9. Änderungen an `AutoRefreshEnabled`/`RefreshIntervalMinutes` in den Einstellungen laufen über `SettingsViewModel.PersistAsync` → `IAutoRefreshService.ApplySettingsAsync` → Weiterleitung → Neuplanung des OS-Tasks.

Beteiligte Klassen/Komponenten: `AppDelegate`, `IBackgroundRefreshService`/`BackgroundRefreshService`, `BGTaskScheduler`/`BGAppRefreshTask`/`BGAppRefreshTaskRequest` (`BackgroundTasks`-Framework), `IPlatformApplication.Current.Services`, `IFeedSyncService`, `ISettingsRepository`, `IDebugLogService`, `AutoRefreshService`, `SettingsViewModel`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `IBackgroundRefreshService` (`src/Reporter.Core/Interfaces/IBackgroundRefreshService.cs`) | Interface | Gateway für den OS-seitigen Hintergrundabruf: `IsSupported` (`bool`), `ApplySettingsAsync(Settings, CancellationToken)` (Task einplanen/abmelden), `RunScheduledSyncAsync(CancellationToken)` → `Task<bool>` (Sync ausführen + Folgeabruf planen; Rückgabewert für `SetTaskCompleted`) |
| `BackgroundRefreshService` (`src/Reporter/Services/BackgroundRefreshService.cs`) | Klasse | `#if IOS`-Implementierung über `BackgroundTasks.BGTaskScheduler`; hält die Konstante `RefreshTaskIdentifier` (`"de.martinstromberg.reporter.feedrefresh"`); Konstruktor-Abhängigkeiten `IFeedSyncService`, `ISettingsRepository`, optional `IDebugLogService?`; auf Windows/Android/MacCatalyst `IsSupported == false` und No-Op |
| `FakeBackgroundRefreshService` (`src/Reporter.Tests/FakeBackgroundRefreshService.cs`) | Test-Hilfsklasse | `IBackgroundRefreshService`-Fake nach dem Muster `FakeLocalNotificationService`/`FakeAutoRefreshService`: zeichnet `ApplySettingsAsync`-Aufrufe (übergebenes `Settings`) auf, `RunScheduledSyncAsync` mit konfigurierbarem Ergebnis und optionaler Exception |

## Änderungen an bestehenden Klassen

### `NotificationDelegate` (Klasse, `src/Reporter/Platforms/iOS/NotificationDelegate.cs`)

- **Geänderte Methoden:** `WillPresentNotification` — Rückgabe von `UNNotificationPresentationOptions.Banner | List | Sound` auf `UNNotificationPresentationOptions.None` reduzieren; Klassen-XML-Doc anpassen (Vordergrund-Darstellung wird unterdrückt, Tap-Handling bleibt).
- **Unverändert:** `DidReceiveNotificationResponse`.

### `AppDelegate` (Klasse, `src/Reporter/Platforms/iOS/AppDelegate.cs`)

- **Neue Methoden:** `RegisterBackgroundFetchTask` — `BGTaskScheduler.Shared.Register(RefreshTaskIdentifier, null, launchHandler)`; `HandleRefreshTaskAsync(BGAppRefreshTask)` — `CancellationTokenSource` + `ExpirationHandler`, `IBackgroundRefreshService` lazy via `IPlatformApplication.Current?.Services` auflösen, `RunScheduledSyncAsync` awaiten, `SetTaskCompleted(success)` im `finally`-Pfad bzw. `false` im Fehlerfall.
- **Geänderte Methoden:** `FinishedLaunching` — ruft `RegisterBackgroundFetchTask` zusätzlich auf (nach `base.FinishedLaunching`, damit `IPlatformApplication.Current` initialisiert ist; Registrierung erfolgt weiterhin vor Rückkehr aus `FinishedLaunching`).

### `AutoRefreshService` (Klasse, `src/Reporter.Core/Services/AutoRefreshService.cs`)

- **Neue Konstruktor-Abhängigkeit:** `IBackgroundRefreshService` als Pflichtparameter (vor den optionalen Parametern `TimeProvider?`/`IDebugLogService?`) — Feld `_backgroundRefreshService`.
- **Geänderte Methoden:** `ApplySettingsAsync` — nach `StopLoopAsync` und vor der `AutoRefreshEnabled`-Prüfung `_backgroundRefreshService.ApplySettingsAsync(settings)` in eigenem `try/catch` aufrufen (`Debug.WriteLine` + `IDebugLogService`, `DebugLogCategory.Sync`, `Warning`), damit ein Fehler des Gateways den Timer-Loop nicht beeinträchtigt und ein deaktivierter Auto-Refresh den OS-Task abmeldet.

### `MauiProgram` (statische Klasse, `src/Reporter/MauiProgram.cs`)

- **Geänderte Methoden:** `CreateMauiApp` — `AddSingleton<IBackgroundRefreshService, BackgroundRefreshService>` ergänzen (Reihenfolge nach `ILocalNotificationService`/`INetworkStatusService`).

### `Info.plist` (`src/Reporter/Platforms/iOS/Info.plist`)

- Siehe Abschnitt „Konfigurationsänderungen".

### Dokumentation (`docs/help/`)

- `docs/help/einstellungen/architektur.md` — Abschnitt „Skalierung und Zuverlässigkeit": Aussage „es gibt keinen OS-seitigen Background-Fetch" korrigieren (Timer-Lebensdauer vs. `BGAppRefreshTask`).
- `docs/help/einstellungen/ablauf-technisch.md` — Abschnitt 5 („Hintergrund-Aktualisierung") um den OS-Hintergrundabruf und die Weiterleitung in `AutoRefreshService.ApplySettingsAsync` ergänzen.
- `docs/help/benachrichtigungen/architektur.md` — Komponententabelle (`NotificationDelegate`-Rolle, `Info.plist`-Aussage „benötigt keine zusätzlichen Schlüssel" entfällt) und Abhängigkeiten/Datenfluss um `IBackgroundRefreshService`/`BGTaskScheduler` erweitern.
- `docs/help/benachrichtigungen/ablauf-technisch.md` — Abschnitt 4 (`WillPresentNotification` → `None` statt `Banner | List | Sound`) sowie neuen Abschnitt zum Hintergrundabruf-Ablauf ergänzen; Übersicht/Diagramm anpassen.

## Datenbankmigrationen

Keine. Es wird kein neues `Settings`-Feld benötigt — die Steuerung des Hintergrundabrufs nutzt die bestehenden Eigenschaften `AutoRefreshEnabled`/`RefreshIntervalMinutes` (vom Anwender entschieden: Kopplung an den bestehenden Auto-Refresh-Schalter).

## Validierungsregeln

Keine neuen Eingaben — der bestehende Clamp von `RefreshIntervalMinutes` auf 1–1440 wird für `EarliestBeginDate` in `BackgroundRefreshService` wiederverwendet (gleiche Grenzen wie `AutoRefreshService.MinRefreshIntervalMinutes`/`MaxRefreshIntervalMinutes`).

## Konfigurationsänderungen

Keine `Settings`-/`appsettings`-Änderungen. Neue Plattform-Konfiguration in `src/Reporter/Platforms/iOS/Info.plist`:

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `UIBackgroundModes` → `fetch` | `Info.plist`-Array-Eintrag (`string`) | — | Deklariert die App für iOS-Hintergrundabrufe (Voraussetzung für `BGAppRefreshTask`) |
| `BGTaskSchedulerPermittedIdentifiers` → `de.martinstromberg.reporter.feedrefresh` | `Info.plist`-Array-Eintrag (`string`) | — | Erlaubt die Registrierung des `BGAppRefreshTask`; muss exakt `BackgroundRefreshService.RefreshTaskIdentifier` entsprechen (Basis: `CFBundleIdentifier` `de.martinstromberg.reporter`) |

## Seiteneffekte und Risiken

- **Test-Konstruktoraufrufe:** Der neue Pflichtparameter `IBackgroundRefreshService` im `AutoRefreshService`-Konstruktor erfordert Anpassungen aller direkten `new AutoRefreshService(...)`-Aufrufe in `AutoRefreshServiceTests` und `AutoRefreshServiceTests_DebugLog` (`FakeBackgroundRefreshService` übergeben).
- **iOS-Code wird lokal nicht kompiliert:** `src/Reporter/Platforms/iOS/**` und `#if IOS`-Blöcke werden auf Windows nicht gebaut (CI baut mit `IncludeIosTarget=false`) — Syntax-/API-Fehler in `AppDelegate`, `NotificationDelegate` und den iOS-Abschnitten von `BackgroundRefreshService` fallen erst beim macOS-Build auf; sorgfältige Sichtprüfung und manuelle iOS-Verifikation erforderlich.
- **Systemgesteuerte Ausführungshäufigkeit:** iOS bestimmt den tatsächlichen Ausführungszeitpunkt von `BGAppRefreshTask` anhand von Nutzungsverhalten/Energiestatus — `EarliestBeginDate` ist nur eine Untergrenze. Benachrichtigungen können deutlich seltener als `RefreshIntervalMinutes` erscheinen; das ist systembedingt und in der Doku zu vermerken.
- **Systemschalter „Hintergrundaktualisierung":** Deaktiviert der Nutzer die Hintergrundaktualisierung in den iOS-Systemeinstellungen, läuft der Task nie — trotz aktivierter App-Schalter erscheinen dann keine Benachrichtigungen. (Optionaler Ausbau, nicht Teil dieses Plans: Status via `UIApplication.BackgroundRefreshStatus` auswerten.)
- **Vollständige Unterdrückung bei `None`:** Mit `UNNotificationPresentationOptions.None` verschwinden Mitteilungen aus Vordergrund-Syncs komplett — auch ohne Eintrag im Mitteilungszentrum. Das ist die vom Anwender entschiedene strikte Lesart („nur bei Hintergrundabruf"); stille Einträge via `List` sind ausdrücklich nicht vorgesehen.
- **Identifier-Konsistenz:** `BGTaskSchedulerPermittedIdentifiers` in `Info.plist` und `BackgroundRefreshService.RefreshTaskIdentifier` müssen identisch sein, sonst schlägt `Register`/`Submit` fehl.
- **Lebenszyklus-Sicherheit:** Fehler im Hintergrund-Task dürfen den App-Lebenszyklus nicht beeinträchtigen — `RunScheduledSyncAsync` ist fehlerisoliert, `ExpirationHandler` kancelliert den Sync, `SetTaskCompleted` läuft immer; die Neuplanung erfolgt auch im Fehlerfall, damit sich der Abruf nicht „totläuft".
- **Unveränderte Bereiche:** `IFeedSyncService`-/`INotificationService`-/`IAutoRefreshService`-Signaturen bleiben unverändert → `FeedsViewModelTests`, `UnreadViewModelTests`, `SettingsViewModelTests_*`, `FeedSyncServiceTests`, `NotificationServiceTests` und die Fakes `FakeFeedSyncService`/`FakeAutoRefreshService`/`FakeNotificationService` sind nicht betroffen. Windows/Android/MacCatalyst: vollständig No-Op.
- **Keine UI-Änderungen:** Es entstehen keine neuen Seiten, Controls oder Benutzerflüsse — die Einstellungs-UI bleibt unverändert (Wiederverwendung der bestehenden Auto-Refresh-/Benachrichtigungs-Schalter); die Mobile-UI-Design-Review-Regel ist nicht anwendbar.

## Umsetzungsreihenfolge

1. **`IBackgroundRefreshService`-Interface anlegen** (`src/Reporter.Core/Interfaces/`)
   - Voraussetzungen: Keine.
   - Beschreibung: Interface mit `IsSupported`, `ApplySettingsAsync(Settings, CancellationToken)`, `RunScheduledSyncAsync(CancellationToken)` → `Task<bool>` definieren.

2. **`BackgroundRefreshService` anlegen** (`src/Reporter/Services/`)
   - Voraussetzungen: Schritt 1 (`IBackgroundRefreshService`).
   - Beschreibung: Klasse mit `RefreshTaskIdentifier`-Konstante, `IsSupported` (nur `#if IOS` `true`), `ApplySettingsAsync` (`BGTaskScheduler.Shared.Submit`/`Cancel` mit `EarliestBeginDate` aus geclampptem `RefreshIntervalMinutes`), `RunScheduledSyncAsync` (fehlerisolierter `IFeedSyncService.SyncAllAsync`, `IDebugLogService`-Protokollierung, Neuplanung via `ISettingsRepository` + `ApplySettingsAsync`); No-Op-Implementierung für Nicht-iOS-Targets.

3. **`Info.plist` erweitern**
   - Voraussetzungen: Schritt 2 (Identifier-Konstante als Referenz).
   - Beschreibung: `UIBackgroundModes` mit `fetch` und `BGTaskSchedulerPermittedIdentifiers` mit dem `RefreshTaskIdentifier` ergänzen.

4. **`AppDelegate` erweitern**
   - Voraussetzungen: Schritte 1–3 (Interface, Service mit Identifier, Plist-Einträge).
   - Beschreibung: `RegisterBackgroundFetchTask` + `HandleRefreshTaskAsync` implementieren und in `FinishedLaunching` aufrufen (Registrierung, Expiration-Behandlung, lazy Service-Auflösung via `IPlatformApplication.Current.Services`, `SetTaskCompleted`).

5. **`NotificationDelegate.WillPresentNotification` auf `None` ändern**
   - Voraussetzungen: Keine.
   - Beschreibung: Präsentationsoptionen reduzieren, XML-Doc anpassen. (Unabhängig von den anderen Schritten möglich, sollte aber nicht ohne Schritt 4 ausgeliefert werden — sonst erscheinen nie wieder Benachrichtigungen.)

6. **`AutoRefreshService` erweitern**
   - Voraussetzungen: Schritt 1 (`IBackgroundRefreshService`).
   - Beschreibung: Pflicht-Konstruktorparameter `IBackgroundRefreshService` ergänzen; `ApplySettingsAsync` leitet `settings` fehlerisoliert weiter (auch beim `AutoRefreshEnabled`-Early-Return).

7. **DI-Registrierung in `MauiProgram`**
   - Voraussetzungen: Schritt 2 (`BackgroundRefreshService`), Schritt 6 (Konsument `AutoRefreshService`).
   - Beschreibung: `AddSingleton<IBackgroundRefreshService, BackgroundRefreshService>` in `CreateMauiApp`.

8. **Tests: `FakeBackgroundRefreshService` + neue/angepasste Tests**
   - Voraussetzungen: Schritte 1, 6, 7.
   - Beschreibung: Fake anlegen; `AutoRefreshServiceTests`/`AutoRefreshServiceTests_DebugLog`-Konstruktoraufrufe anpassen; neue Tests für Weiterleitung, Fehlerisolierung und DI-Auflösung (siehe Abschnitt „Tests").

9. **Dokumentation aktualisieren**
   - Voraussetzungen: Schritte 4–7 (implementiertes Verhalten).
   - Beschreibung: `docs/help/einstellungen/architektur.md` (Timer-Lebensdauer), `docs/help/einstellungen/ablauf-technisch.md` (Abschnitt 5), `docs/help/benachrichtigungen/architektur.md` und `ablauf-technisch.md` anpassen.

10. **Verifikation**
    - Voraussetzungen: Schritte 1–9.
    - Beschreibung: `dotnet test` (Release, Coverlet-Settings wie im CI-Befehl), `.\scripts\Run-StaticChecks.ps1`, manuelle iOS-Verifikation über `scripts/iOS-Deployment.ps1` (alle drei Vordergrund-Auslöser ohne Banner: manueller Sync, Timer-Abruf, Start-Abruf via `RefreshOnStartupEnabled`; simuliertes Task-Auslösen via lldb `_simulateLaunchForTaskWithIdentifier:@"de.martinstromberg.reporter.feedrefresh"` erzeugt Mitteilung; Expiration via `_simulateExpirationForTaskWithIdentifier`); Ergebnisse in `test-results.md` dokumentieren.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `FakeBackgroundRefreshService` | Hilfsklasse (`src/Reporter.Tests/`) | `IBackgroundRefreshService`-Fake: `AppliedSettings`-Aufzeichnung, `IsSupported`/`RunScheduledSyncResult` konfigurierbar, optionale `ApplySettingsException` für Fehlerisolierungs-Tests |
| `ApplySettings_ForwardsToBackgroundRefresh` | `AutoRefreshServiceTests` | `ApplySettingsAsync` leitet dasselbe `Settings`-Objekt an `IBackgroundRefreshService.ApplySettingsAsync` weiter |
| `ApplySettings_WhenAutoRefreshDisabled_StillForwardsToBackgroundRefresh` | `AutoRefreshServiceTests` | Weiterleitung erfolgt auch bei `AutoRefreshEnabled == false` (Early-Return-Pfad), damit der OS-Task abgemeldet werden kann |
| `ApplySettings_BackgroundRefreshThrows_TimerStillConfigured` | `AutoRefreshServiceTests` | Exception des Gateways wird geschluckt/geloggt; der `PeriodicTimer`-Loop startet trotzdem (Sync nach Intervall läuft) |
| `AddReporterServices_ResolvesAutoRefreshService` | `ServiceCollectionTests` | `AutoRefreshService` mit vollem Konstruktor-Set inkl. `FakeBackgroundRefreshService`-Registrierung auflösbar (spiegelt die `MauiProgram`-Registrierung) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `AutoRefreshServiceTests` (alle `new AutoRefreshService(...)`-Aufrufe) | Neuer Pflicht-Konstruktorparameter `IBackgroundRefreshService` → `FakeBackgroundRefreshService` übergeben |
| `AutoRefreshServiceTests_DebugLog` (alle `new AutoRefreshService(...)`-Aufrufe) | Gleiche Signaturänderung |

`FeedSyncServiceTests`, `NotificationServiceTests`, `FeedsViewModelTests`, `UnreadViewModelTests`, `SettingsViewModelTests_*` bleiben unverändert: Die Benachrichtigungs-Semantik in `Reporter.Core` ändert sich nicht (Unterdrückung erfolgt ausschließlich in `NotificationDelegate.WillPresentNotification`), und die Interface-Signaturen von `IFeedSyncService`/`INotificationService`/`IAutoRefreshService` bleiben stabil.

### E2E-Tests (primärer Funktionsnachweis)

Die sichtbare Verhaltensänderung ist iOS-Plattformverhalten (`UNNotificationPresentationOptions`, `BGTaskScheduler`) und liegt außerhalb der `Reporter.Tests`-Suite (referenziert nur `Reporter.Core` + `Reporter.Data`; es existiert keine UI-Test-Infrastruktur). Der Funktionsnachweis erfolgt daher als **manuelle E2E-Verifikation** auf einem iOS-Gerät per `scripts/iOS-Deployment.ps1` und lldb-Task-Simulation; die Ergebnisse werden in `test-results.md` dokumentiert. Ergänzend sichern die neuen Unit-Tests die Core-seitige Weiterleitung ab.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Manueller Sync (Pull-to-Refresh) bei laufender Vordergrund-App mit neuen Artikeln erzeugt **kein** Banner/keinen Sound/keinen Mitteilungszentrum-Eintrag | Manuell via `scripts/iOS-Deployment.ps1`, Ergebnis in `test-results.md` | Keine Systemmitteilung bei Abruf während laufender App | Nur auf iOS-Gerät prüfbar (`WillPresentNotification` ist Plattformcode) |
| Pflicht | Timer-Abruf (`AutoRefreshService`) bei laufender App erzeugt keine Systemmitteilung | Manuell, `test-results.md` | Keine Systemmitteilung bei In-App-Timer-Abruf | Plattformverhalten, nicht unit-testbar |
| Pflicht | Start-Abruf bei aktiviertem `RefreshOnStartupEnabled`: Kaltstart der App in den Vordergrund mit neuen Artikeln → `RunStartupSyncAsync` läuft (fire-and-forget aus `AutoRefreshService.StartAsync`, `AutoRefreshService.cs:49-52`), es erscheint **kein** Banner/kein Sound/kein Mitteilungszentrum-Eintrag | Manuell via `scripts/iOS-Deployment.ps1`, `test-results.md` | Keine Systemmitteilung beim `RefreshOnStartupEnabled`-Start-Abruf | Dritter in der Anforderung explizit genannter Vordergrund-Auslöser mit eigenem Trigger-Pfad; `WillPresentNotification`-Unterdrückung nur auf iOS-Gerät prüfbar |
| Pflicht | Simulierter `BGAppRefreshTask` (`lldb`: `_simulateLaunchForTaskWithIdentifier`) führt Sync aus und erzeugt bei neuen Artikeln eine sichtbare Mitteilung | Manuell, `test-results.md` | Systemmitteilung erscheint bei OS-Hintergrundabruf | Kernanforderung; `BGTaskScheduler` nur auf Gerät/simuliertem Launch prüfbar |
| Pflicht | Task-Expiration (`_simulateExpirationForTaskWithIdentifier`) → Sync wird kancelliert, `SetTaskCompleted` läuft, App bleibt stabil | Manuell, `test-results.md` | Fehlerisolierung/Lebenszyklus-Sicherheit des Hintergrund-Tasks | iOS-Zeitbudget-Verhalten nur auf Plattform prüfbar |
| Pflicht | `AutoRefreshEnabled` aus → kein `BGAppRefreshTask` eingeplant; Neuplanung nach Wiederaktivierung | Manuell, `test-results.md` | Settings-Kopplung des Hintergrundabrufs | `BGTaskScheduler`-Status nur plattformseitig sichtbar |
| Pflicht | Antippen einer Hintergrund-Mitteilung navigiert wie bisher (`articledetail`/`//unread`, Link-Fallback) | Manuell, `test-results.md` | Tap-Handling bleibt unverändert funktionsfähig | Regressionsschutz für `DidReceiveNotificationResponse` |

Welche bestehenden E2E-Tests müssen angepasst werden?

Keine — die bestehenden `*_E2E`-Testklassen (`DebugReportTests_E2E`, `KeywordFilterTests_E2E`, `SettingsViewModelTests_E2E`) berühren den Benachrichtigungs-/Hintergrundabruf-Pfad nicht.

## Offene Punkte

Keine — alle zuvor offenen Punkte wurden durch den Anwender entschieden und sind verbindlich in den Abschnitten „Designentscheidungen" und „Programmabläufe" eingearbeitet.
