<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung: Debuginformationen sammeln und per E-Mail versenden (Issue #81)

## Fachliche Zusammenfassung

Die `SettingsPage` erhält einen neuen Abschnitt für Diagnose/Support mit zwei Funktionen: einem Schalter, der die Sammlung von Debuginformationen aktiviert, und einer Versand-Aktion, die die gesammelten Informationen als vorbefüllte E-Mail an den Entwickler an den Standard-Mail-Client des Geräts übergibt — über `Microsoft.Maui.ApplicationModel.Communication.Email` (`Email.ComposeAsync`/`EmailMessage`), sodass der Anwender die E-Mail sieht und selbst abschickt (kein SMTP-Direktversand). Da `Reporter.Core` (`net10.0`) keine MAUI-Referenz hat, wird der Plattformzugriff über das bestehende Gateway-Muster gekapselt (Interface in `src/Reporter.Core/Interfaces/`, Implementierung in `src/Reporter/Services/`, vergleichbar `IAppThemeService`/`ILocalNotificationService`/`INetworkStatusService`). Die Report-Inhalte stammen aus dem Sync-Protokoll (`ISyncLogRepository`), dem `Settings`-Datensatz, dem Feed-Health-Status (`Feed.HealthStatus`, `FeedHealth`-Konstanten) sowie App-/Geräte-/OS-Versionsangaben (`AppInfo`/`DeviceInfo`).

## Betroffene Klassen und Komponenten

### Interfaces (neu, `src/Reporter.Core/Interfaces/`)

- `IEmailService` (Arbeitsname) — Gateway für den E-Mail-Versand über den System-Mail-Client; voraussichtlich eine `IsSupported`-Eigenschaft (Muster: `ILocalNotificationService.IsSupported`, da nicht jede Plattform/jedes Gerät einen eingerichteten Mail-Client hat) und eine Compose-Methode, die Empfänger, Betreff und Body entgegennimmt.
- `IDeviceInfoProvider` (Arbeitsname) — Gateway für App-/Geräte-/OS-Angaben (`AppInfo.Current.VersionString`/`BuildString`, `DeviceInfo.Model`/`Manufacturer`/`Platform`/`VersionString`), da `AppInfo`/`DeviceInfo` MAUI-only sind und `Reporter.Core` sie nicht referenzieren kann. Alternativ kann dieses Gateway mit `IEmailService` zu einem `IDebugReportGateway` zusammengefasst werden (Annahme/Entwurfsvariante — kein Referenzcode vorhanden).

### Logikklassen / Services

- `src/Reporter/Services/EmailService.cs` (neu) — Implementierung von `IEmailService` via `Email.Default.ComposeAsync(new EmailMessage { To = …, Subject = …, Body = … })`; erwartete Plattformfehler (`FeatureNotSupportedException` o. ä.) sind an das Gateway-Protokoll zu kapseln.
- `src/Reporter/Services/DeviceInfoProvider.cs` (neu) — liest `AppInfo.Current` und `DeviceInfo.Current` aus.
- `src/Reporter.Core/Services/DebugReportService.cs` (neu, Arbeitsname) — orchestriert die Sammlung und formatiert den Report-Body; Abhängigkeiten: `ISettingsRepository` (`GetAsync`), `ISyncLogRepository` (`GetAllAsync`, liefert alle Einträge absteigend nach `StartedAt`), `IFeedRepository` (`GetAllAsync`/`GetAllWithDetailsAsync` für `HealthStatus`, `LastCheckedAt`, `HealthLastChange`), `IDeviceInfoProvider`, `IEmailService` und ggf. `INetworkStatusService` für den aktuellen Online-Status. Orchestrierungsmuster analog `NotificationService` (Regelwerk in Core, Plattformwirkung über Gateway).
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs` — neue bindbare Property `DebugCollectionEnabled` (persistiert über das bestehende `PersistOnChange`/`PersistAsync`-Muster in `PersistAsync`), neues `SendDebugReportCommand` (`AsyncRelayCommand`), ggf. `DebugEmailSupported`-Property für Sichtbarkeit/Aktivierung der Versand-Aktion sowie ein Fehlerpfad für den Fall, dass kein Mail-Client verfügbar ist (Muster: `NotificationAuthorizationDenied`-Event bzw. Hinweis-`Border` auf der Page).
- `src/Reporter/MauiProgram.cs` — DI-Registrierungen `AddSingleton<…>` für die neuen Gateways und den Report-Service im bestehenden `builder.Services`-Block.

### Datenmodell / Persistenz

- `src/Reporter.Core/Models/Settings.cs` — neue Eigenschaft `DebugCollectionEnabled` (`bool`, `required`); Default `false` (Annahme: Opt-in-Verhalten wie bei `NotificationSummaryEnabled`).
- `src/Reporter.Data/Entities/Settings.cs` — entsprechende `DebugCollectionEnabled`-Property mit Default `false`.
- `src/Reporter.Data/ReporterDbContext.cs` — `ConfigureSettings`: neue Spalte (Konvention snake_case, z. B. `debug_collection_enabled`, `IsRequired().HasDefaultValue(false)`); `entity.HasData(new Settings())` erbt den Default.
- `src/Reporter.Data/Migrations/` — neue EF-Migration nach der bestehenden `AddSettings*`-Namenskonvention (z. B. `AddSettingsDebugCollection`); `ApplyPersistedLanguage` in `MauiProgram` führt `context.Database.Migrate()` bereits beim Start aus.
- `src/Reporter.Data/Repositories/SettingsRepository.cs` — Mapping in `SaveAsync` und `MapToModel` um die neue Property ergänzen.

### UI-Komponenten

- `src/Reporter/Views/SettingsPage.xaml` — neuer Karten-Abschnitt im Stil der bestehenden Sektionen (`VerticalStackLayout` + `Label` mit `UiLabelStyle` als Section-Header + `Border` mit `RoundRectangle 12` und `SurfaceCard`-`AppThemeBinding`): `Switch`-Zeile für `DebugCollectionEnabled` (Label + `MetaStyle`-Hint) und eine Senden-Aktion (Button oder tappable Zeile, Touch-Ziel ≥ 44 pt, `SemanticProperties.Description`). Referenz-Layout: `design-draft/stitch_local_rss_feed_reader/einstellungen_filter[_dark_mode]/screen.png` — ein eigener Entwurfs-Screen für den Debug-Abschnitt existiert nicht (Annahme: bestehende Sektionskonvention übernehmen).
- `src/Reporter/Views/SettingsPage.xaml.cs` — ggf. Event-Handler für den Fehlerhinweis analog `OnNotificationAuthorizationDenied` (z. B. `DisplayAlertAsync` bei nicht unterstütztem Mail-Versand).
- `src/Reporter.Core/Resources/Strings/AppResources.resx` + `AppResources.de.resx` (+ `AppResources.Designer.cs` generiert) — neue Schlüssel nach `Settings*`-Konvention: Section-Titel, Schalter-Label/-Hint, Senden-Label, Fehlermeldung ohne Mail-Client sowie Betreff-Zeile des Reports.

### Tests (`src/Reporter.Tests/`)

- `FakeEmailService`, `FakeDeviceInfoProvider` (neue Fakes nach dem Muster `FakeLocalNotificationService`/`FakeNetworkStatusService`).
- `DebugReportServiceTests` (neu) — Report-Aufbau aus Settings/SyncLog/Feed-Health/Device-Info, Verhalten bei leerem Protokoll, Begrenzung der Log-Ausgabe, Fehlerpfad bei `IsSupported == false`.
- `SettingsViewModelTests_Load`/`_Persist`/`_E2E` — Toggle lädt und persistiert `DebugCollectionEnabled`; `SendDebugReportCommand` ruft das Gateway und behandelt den nicht-unterstützten Fall.
- `SettingsRepositoryTests`, `ReporterDbContextTests_Schema`/`_Persistence` — neue Spalte und Default-Roundtrip.
- `ServiceCollectionTests` — ggf. um die neuen Registrierungen erweitern.
- UI-nahe Änderungen ohne Unit-Abdeckung: manuelle Verifikation am 390 × 844-pt-Windows-Fenster (Light + Dark) mit Screenshots in `test-results.md`/`docs/help/anwendung/mobile-ui-design.md` gemäß `AGENTS.md`; E-Mail-Compose nur manuell auf einem Gerät mit Mail-Client verifizierbar.

## Implementierungsansatz

1. **Gateway-Interfaces:** Neue Interfaces in `src/Reporter.Core/Interfaces/` nach dem etablierten Muster definieren (dokumentierte Members, `IsSupported`-Kennung für optionale Plattformfähigkeit, `CancellationToken`-Parameter wie bei `ILocalNotificationService`).
2. **MAUI-Implementierungen:** `EmailService`/`DeviceInfoProvider` in `src/Reporter/Services/` implementieren (`Microsoft.Maui.ApplicationModel.Communication.Email`, `AppInfo`, `DeviceInfo`); kein zusätzliches NuGet-Paket nötig (Teil von .NET MAUI Essentials).
3. **Report-Service in Core:** `DebugReportService` sammelt die Datenquellen und formatiert den Mail-Body. Achtung: `SyncLogRepository.GetAllAsync` liefert das vollständige, nie bereinigte `sync_logs`-Protokoll (`RetentionCleanupService` räumt nur `items`) — die Report-Ausgabe sollte auf die jüngsten Einträge begrenzt oder als Dateianhang ausgegeben werden (Annahme, siehe Offene Fragen).
4. **Settings-Erweiterung:** Vorgehen wie bei `Language`/`NotificationSummaryEnabled`: `Settings`-Modell + Entity + `ConfigureSettings`-Mapping + `dotnet ef migrations add` + `SettingsRepository`-Mapping + `SettingsViewModel`-Toggle (`PersistAsync` schreibt den Wert mit) + `SettingsPage`-Abschnitt.
5. **Versand-Interaktion:** Der Senden-Trigger ist eine explizite Benutzeraktion auf der `SettingsPage` (Button/Zeile) → `SendDebugReportCommand` → `DebugReportService` → `IEmailService` → `Email.ComposeAsync`. Der System-Mail-Client öffnet sich mit vorbefülltem Empfänger/Betreff/Body; der Anwender prüft und sendet selbst. Fehler ohne Mail-Client werden abgefangen und als lokalisierter Hinweis auf der Seite ausgegeben.
6. **Verifikation:** `dotnet test` (`Reporter.Tests`), `.\scripts\Run-StaticChecks.ps1` (Format/Security/Static Analysis inkl. MAUI-Build), manuelle UI-Verifikation 390 × 844 pt Light + Dark inkl. Screenshots, manueller Versand-Test auf einem Gerät mit eingerichtetem Mail-Client (Windows; iOS ggf. über `scripts/iOS-Deployment.ps1`).

## Konfiguration

- **Benutzerspezifisch über den Singleton-`Settings`-Datensatz** (`settings`-Tabelle, `ISettingsRepository`): neue bool-Eigenschaft `DebugCollectionEnabled`, Default `false`, gesteuert über einen `Switch` auf der `SettingsPage` und persistiert über das bestehende `PersistOnChange`/`PersistAsync`-Muster — dieselbe Konfigurationsebene wie `NotificationsEnabled`/`AutoRefreshEnabled`. (Annahme: Opt-in-Default; ein Default `true` wäre denkbar, ist aber bei Diagnosedaten unüblich.)
- **Empfängeradresse:** vermutlich eine feste Konstante (z. B. neben `SettingsValues` oder im `DebugReportService`); eine konfigurierbare Adresse ist aus der Anforderung nicht ableitbar → Offene Frage.

## Offene Fragen

- **„Sammlung" — dauerhaft oder On-Demand?** Was schaltet der Schalter konkret? (a) Nur die Freigabe des Versands einer On-Demand-Momentaufnahme — dann sammelt die App ohnehin bereits `SyncLog`, `Settings` und `Feed.HealthStatus` und der Schalter ist primär eine UI-Freigabe. (b) Eine dauerhafte, ausführlichere Protokollierung — dafür existiert heute **kein** persistiertes Laufzeit-Log außer `SyncLog` und `Debug.WriteLine`; Variante (b) würde eine neue Protokoll-Infrastruktur (z. B. eigene Log-Tabelle oder Datei mit Rotation) bedeuten und den Umfang deutlich vergrößern.
- **Empfängeradresse:** An welche E-Mail-Adresse geht der Bericht? Fest codiert (Konstante) oder über die Einstellungen änderbar?
- **Umfang und Datenschutz:** Dürfen Feed-URLs/-Titel, konfigurierte Keyword-Filter und `SyncLog.Message`-Texte (enthalten Feed-URLs und Fehlermeldungen) in den Bericht? Sind Artikeldaten (`Item.Title`, `ContentHtml`) ausdrücklich auszunehmen (Annahme: ja)? Ist dem Anwender vor dem Senden eine Vorschau oder ein Hinweis auf die enthaltenen Daten zu zeigen, oder genügt die Transparenz durch den sichtbaren Mail-Entwurf?
- **Begrenzung/Format des Reports:** `sync_logs` wächst unbegrenzt (keine Bereinigung) — wie viele Einträge sollen in den Body (z. B. letzte 50/100) und/oder soll der Bericht als `EmailAttachment` (Datei in `FileSystem.CacheDirectory`) versendet werden? Reicht Plain-Text-`Body`?
- **Semantik des Schalters bei ausgeschalteter Sammlung:** Ist die Senden-Aktion dann ausgeblendet, deaktiviert oder gar nicht vorhanden (On-Demand-Versand unabhängig vom Schalter)?
- **Fehlerpfad ohne Mail-Client:** Wie soll die App reagieren, wenn `Email.ComposeAsync` nicht unterstützt wird bzw. kein Mail-Account eingerichtet ist — stiller Hinweis-`Border`, `DisplayAlert` oder Zwischenablage-Fallback?
- **UI-Vorgabe:** Es existiert kein Entwurfs-Screen für den Debug-Abschnitt — gilt die Annahme, die bestehende Karten-/Zeilen-Konvention der `SettingsPage` (Section-Header + `Switch`-/`Button`-Zeilen) zu übernehmen?
