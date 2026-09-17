<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: App-Store-Einreichung iOS

## Übersicht

Die .NET-MAUI-App Reporter wird für die Einreichung im iOS App Store vorbereitet. Umgesetzt werden zwölf Review-Punkte: drei echte Code-/Verhaltensänderungen (WebView-Navigation in `ArticleDetailPage` einschränken und externe Links im System-Browser öffnen; `try/catch` um `context.Database.MigrateAsync()` in `App.OnStart`; iCloud-Backup-Ausschluss der SQLite-Datei unter iOS), Plist-/Manifest-Ergänzungen (`CFBundleLocalizations`, `NSPrivacy*`-Schlüssel, `UIDeviceFamily`-Entscheidung), der Ersatz des deprecated `xcrun altool` in `scripts/iOS-Deployment.ps1` sowie mehrere Dokumentationsartefakte (Datenschutzerklärung, App-Privacy-Label, Review-Notizen mit ATS-Begründung, iPad-Entscheidung, App-Icon-Verifikation). Datenmodell, Enums und EF-Core-Migrationen bleiben unverändert.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| WebView-Navigationsentscheidung (Punkt 3) | Neue statische Methode `WebViewNavigationGuard.DecideAction(string? url, bool isOnline)` in `Reporter.Core`, die ein Enum `WebViewNavigationAction` (`Proceed`, `CancelAndOpenExternally`, `CancelAndShowOfflineHint`) zurückgibt | Die Entscheidung ist heute im nicht testbaren Code-Behind; `WebViewNavigationGuard` bleibt die einzige Klassifikationsstelle, und das Enum bildet alle drei Zweige (lokal durchlassen / online extern umleiten / offline Hinweis) vollständig unit-testbar ab — `Reporter.Tests` referenziert nur `Reporter.Core`/`Reporter.Data`, daher muss die Logik dort liegen. |
| Browser-Öffnen aus `OnWebViewNavigating` | Neuer öffentlicher Einstieg `ArticleDetailViewModel.OpenLinkInBrowserAsync(string url)`; `OpenInBrowserAsync` wird auf eine gemeinsame private Hilfsmethode `OpenUrlInBrowserAsync(string url)` refaktorisiert | `Browser.OpenAsync` (Essentials) und der Fehlerpfad `ErrorMessage = AppResources.ErrorOpenInBrowserFailed` bleiben im vorhandenen ViewModel-Muster (`OpenInBrowserAsync`, Zeilen 489–513); das Code-Behind setzt nur noch `e.Cancel` und delegiert — kein Essentials-Aufruf in der View. |
| iCloud-Backup-Ausschluss (Punkt 8) | Gateway-Muster wie `ILocalNotificationService`/`IBackgroundRefreshService`: Interface `IBackupExclusionService` in `Reporter.Core/Interfaces`, Implementierung `BackupExclusionService` in `src/Reporter/Services/` mit `#if IOS` (`NSUrl.SetResource` + `NSUrl.IsExcludedFromBackupKey`) und No-op auf anderen Targets; Transport des effektiven DB-Pfads über ein neues DI-Singleton `DatabasePath` (POCO, Muster `FirstRunState`), registriert in `MauiProgram.CreateMauiApp` | Der effektive Pfad (inkl. `REPORTER_DB_PATH`-Override) existiert nur als lokale Variable in `CreateMauiApp` — ein Singleton vermeidet Logik-Duplikation im Service; das Gateway-Muster hält `App` plattformneutral und die Registrierung konsistent zu den übrigen Plattformdiensten. |
| Upload-Tooling (Punkt 11) | `xcrun iTMSTransporter` ersetzt `xcrun altool`: Upload per `-m upload`, Validierung per `-m verify` (soweit vom Tool unterstützt); die lokalen Vorprüfungen (`codesign --verify`, `embedded.mobileprovision`-Check) bleiben | `iTMSTransporter` nutzt dieselbe API-Key-Authentifizierung (`-apiKey`/`-apiIssuer`) und denselben Suchpfad `~/.appstoreconnect/private_keys/` wie `altool` — `Copy-ApiKeyToMac` bleibt kompatibel; kein zusätzliches Tooling (fastlane o. ä.) wird eingeführt. |
| Ort der neuen Dokumentationsartefakte | `docs/privacy-policy.md` (öffentlich referenzierbare Datenschutzerklärung) und `docs/app-store-review.md` (Review-Notizen: ATS-Begründung, App-Privacy-Antworten, Altersfreigabe-Empfehlung, Demo-Feed-Hinweis, iPad-Entscheidung, Icon-Verifikation); Verlinkung aus `docs/help/index.md` | Deckt sich mit den in der Anforderung genannten Ablageoptionen; Review-Notizen sind ein Arbeitsdokument für die Einreichung, die Datenschutzerklärung ein separat verlinkbares öffentliches Dokument. |
| iPad-Strategie (Punkt 7) | **Variante B (beschlossen):** `UIDeviceFamily` = `[1]` (nur iPhone), `UISupportedInterfaceOrientations~ipad` aus der `Info.plist` entfernen; Entscheidung in `docs/app-store-review.md` dokumentieren, iPad-Screenshot-Pflicht in `ablauf-anwender.md` streichen | Ohne verifizierbaren macOS-/iPad-Zugriff ist ungeprüfter iPad-Support (v. a. `DisplayActionSheetAsync`-Popover in `FeedsPage`/`CategoriesPage`) das größere Review-Risiko; die App ist noch nicht veröffentlicht, `2` kann mit durchgeführter Verifikation später zurückkehren. |
| Datenschutzerklärung — Sprache, Hosting, Kontakt (Punkt 1) | **Beschlossen:** zweisprachig (deutsch primär, englische Sektion) in `docs/privacy-policy.md`; Hosting über die GitHub-URL des Dokuments auf dem Default-Branch (als Datenschutz-URL in App Store Connect hinterlegen); Kontakt-/Verantwortlichen-Adresse = `DebugReportRecipient` (`mstromberg84+reporter@gmail.com`, `Directory.Build.props`) | App Store Connect verlangt eine öffentlich erreichbare URL — die Repo-Datei liefert sie ohne zusätzliche Infrastruktur; `DebugReportRecipient` ist die bereits konfigurierte, dokumentierte Kontaktadresse der App. |
| Altersfreigabe-Empfehlung (Punkt 12) | **Beschlossen:** Empfehlung 12+ (ggf. 17+ je nach Apples UGC-Fragen); die Fragen zu nutzergenerierten/unkontrollierten Inhalten wahrheitsgemäß bejahen | Nach Punkt 3 entfällt „unrestricted web access"; anwenderbestimmte Feed-Inhalte bleiben jedoch nicht kontrollierbar — die finale Einstufung obliegt dem Einreichenden, die Empfehlung wird in `docs/app-store-review.md` festgehalten. |
| Bestätigungsdialog beim externen Link-Öffnen (Punkt 3) | **Beschlossen:** kein Zwischendialog — externe Links öffnen online still im System-Browser | Abgeleitete Annahme der Anforderung, konsistent zum bestehenden „Im Browser öffnen"-Button; der Offline-Hinweisdialog bleibt bestehen. |
| SQLite-Sidecar-Dateien (Punkt 8) | **Beschlossen:** `reporter.db-wal` und `reporter.db-shm` werden in den iCloud-Backup-Ausschluss eingeschlossen (jeweils hinter `File.Exists`-Guard) | Die Sidecars sind Bestandteil der Datenbank — ein Ausschluss nur der Hauptdatei würde WAL-Inhalte weiter ins Backup lassen. |
| Testbarkeit des Migrationsfehler-Pfads (Punkt 6) | **Beschlossen:** kein Code-Umbau — dokumentierte Begründung im Tests-Abschnitt statt Extraktion nach `Reporter.Core` | Der `try/catch` folgt exakt dem Muster der übrigen Start-Schritte und enthält keine Entscheidungslogik (anders als Punkt 3, wo `DecideAction` testbar gemacht wurde); `App.OnStart` liegt im MAUI-Projekt, das `Reporter.Tests` nicht referenziert — eine Extraktion wäre Aufwand ohne prüfbare Logik. |

## Programmabläufe

### Externe Link-Navigation im Artikel-WebView (Punkt 3)

1. Der `WebView` löst `Navigating` aus; `ArticleDetailPage.OnWebViewNavigating` ruft `WebViewNavigationGuard.DecideAction(e.Url, _viewModel.IsOnline)` auf.
2. Ergebnis `Proceed` (keine externe http/https-URL — initiales `HtmlWebViewSource`, `about:blank`, `data:`): sofortige Rückkehr, Navigation läuft unverändert durch.
3. Ergebnis `CancelAndOpenExternally` (externe URL, online): `e.Cancel = true` setzen und fire-and-forget `_viewModel.OpenLinkInBrowserAsync(e.Url)` aufrufen — kein zusätzlicher Bestätigungsdialog.
4. `OpenLinkInBrowserAsync` ruft `OpenUrlInBrowserAsync(url)`: `Browser.OpenAsync(url, BrowserLaunchMode.SystemPreferred)` im `try/catch`; im Fehlerfall `Debug.WriteLine` + `ErrorMessage = AppResources.ErrorOpenInBrowserFailed` (sichtbar als Fehlerzeile oberhalb des Artikelinhalts).
5. Ergebnis `CancelAndShowOfflineHint` (externe URL, offline): `e.Cancel = true` und der bestehende lokalisierte `DisplayAlertAsync` (`AppResources.OfflineHint` + `AppResources.ArticleOfflineLinksDisabled` + `AppResources.ButtonOk`) bleibt.

Beteiligte Klassen/Komponenten: `ArticleDetailPage`, `ArticleDetailViewModel`, `WebViewNavigationGuard`, `WebViewNavigationAction`, `Browser` (Essentials)

### Abgesicherte Start-Migration (Punkt 6)

1. `App.OnStart` löst `IDebugLogService` vor der Migration auf (Reihenfolge der Zeilen 42–46 wird getauscht: Service-Auflösung benötigt keine migrierte DB, erst `BeginSessionAsync` greift lesend zu).
2. `await context.Database.MigrateAsync()` erhält ein eigenes `try/catch` nach dem Muster der übrigen Start-Schritte: `Debug.WriteLine` + `debugLogService.LogAsync(DebugLogCategory.Lifecycle, <Meldung>, ex.ToString(), DebugLogLevel.Error)`; der App-Start wird nicht abgebrochen.
3. `BeginSessionAsync` läuft nach dem Migrations-Block weiter (es ist selbst fehlerisoliert; schlägt die Migration fehl, schlägt `BeginSessionAsync` intern fehlerisoliert fehl).
4. Die übrigen Start-Schritte (Demo-Seed, Retention-Cleanup, Theme, `INetworkStatusService`, `IAutoRefreshService`) bleiben unverändert.
5. Der zweite Migrationspfad `MauiProgram.ApplyPersistedLanguage` (`context.Database.Migrate()`, bereits mit `try/catch`) bleibt unverändert.

Beteiligte Klassen/Komponenten: `App`, `IDebugLogService`, `DebugLogService`, `ReporterDbContext`, `MauiProgram`

### iCloud-Backup-Ausschluss der Datenbankdatei (Punkt 8)

1. `MauiProgram.CreateMauiApp` registriert zusätzlich `.AddSingleton(new DatabasePath(databasePath))` (nach der Pfad-Ermittlung, Zeilen 46–54) und `.AddSingleton<IBackupExclusionService, BackupExclusionService>()`.
2. `App.OnStart` erhält nach dem Migrations-Block einen eigenen fehlerisolierten `try/catch`-Block: Auflösen von `DatabasePath` und `IBackupExclusionService`, dann Aufruf von `ExcludeFromBackup` für `reporter.db` sowie — jeweils nur bei `File.Exists` — die Sidecar-Dateien `reporter.db-wal` und `reporter.db-shm`.
3. `BackupExclusionService.ExcludeFromBackup` setzt unter `#if IOS` per `NSUrl.FromFilename(path)` + `SetResource` den Schlüssel `NSUrl.IsExcludedFromBackupKey` auf `NSNumber.FromBoolean(true)`; auf anderen Targets No-op.
4. Fehler werden wie üblich per `Debug.WriteLine` + `LogAsync(DebugLogCategory.Lifecycle, …, DebugLogLevel.Error)` protokolliert und verhindern den Start nicht.

Beteiligte Klassen/Komponenten: `MauiProgram`, `App`, `DatabasePath`, `IBackupExclusionService`, `BackupExclusionService`

### Store-Upload über `iTMSTransporter` (Punkt 11)

1. `Invoke-IpaValidation` in `scripts/iOS-Deployment.ps1` ersetzt den `xcrun altool --validate-app`-Aufruf (Zeilen 567–568) durch `xcrun iTMSTransporter -m verify` mit `-f "$IPA" -apiKey "$ApiKeyId" -apiIssuer "$ApiIssuerId"`; die lokalen Vorprüfungen (`ditto`-Entpacken, `codesign --verify --deep --strict -vvv`, `get-task-allow`-Check) bleiben unverändert. Falls `-m verify` für iOS-IPAs nicht unterstützt wird, entfällt der Remote-Validierungsschritt dokumentiert — der Upload selbst validiert serverseitig.
2. `Invoke-StoreUpload` ersetzt `xcrun altool --upload-app` (Zeile 578) durch `xcrun iTMSTransporter -m upload` mit identischer API-Key-Authentifizierung.
3. `Copy-ApiKeyToMac` bleibt unverändert — `iTMSTransporter` sucht den Schlüssel ebenfalls unter `~/.appstoreconnect/private_keys/`.
4. Alle `xcrun`-Aufrufe laufen weiterhin auf dem Mac (`Invoke-OnMac`, lokal oder per SSH).

Beteiligte Klassen/Komponenten: `scripts/iOS-Deployment.ps1` (`Invoke-IpaValidation`, `Invoke-StoreUpload`, `Invoke-Store`, `Invoke-Upload`)

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `WebViewNavigationAction` (`src/Reporter.Core/Services/`) | Enum | Entscheidungsergebnis von `WebViewNavigationGuard.DecideAction`: `Proceed`, `CancelAndOpenExternally`, `CancelAndShowOfflineHint` |
| `DatabasePath` (`src/Reporter.Core/Models/`) | Datenmodellklasse (POCO) | Transportiert den effektiven DB-Dateipfad (`FilePath`-Eigenschaft, inkl. `REPORTER_DB_PATH`-Override) aus `MauiProgram` per DI zu `App`/`BackupExclusionService` — Muster `FirstRunState` |
| `IBackupExclusionService` (`src/Reporter.Core/Interfaces/`) | Interface | Gateway-Vertrag: `void ExcludeFromBackup(string filePath)` — schließt eine Datei vom iCloud-Backup aus |
| `BackupExclusionService` (`src/Reporter/Services/`) | Klasse | Plattform-Implementierung: `#if IOS` setzt `NSUrl.IsExcludedFromBackupKey` per `NSUrl.SetResource`; No-op auf anderen Targets; `File.Exists`-Guard, wirft nicht bei fehlender Datei |
| `ArticleLinkTests` (`src/Reporter.E2ETests/`) | Testklasse (xUnit, `[Trait("Category","E2E")]`) | E2E-Nachweis: Klick auf externen Link im Artikel-WebView öffnet den System-Browser statt interner Navigation |

## Änderungen an bestehenden Klassen

### `WebViewNavigationGuard` (statische Klasse)

- **Neue Methoden:** `DecideAction(string? url, bool isOnline)` — Parameter: zu prüfende URL und aktueller Online-Status; Rückgabewert `WebViewNavigationAction`: `Proceed` bei nicht-externer URL (`IsExternalUrl` == `false`), `CancelAndOpenExternally` bei externer URL + `isOnline == true`, `CancelAndShowOfflineHint` bei externer URL + `isOnline == false`. `IsExternalUrl` bleibt unverändert und wird intern weitergenutzt.

### `ArticleDetailViewModel` (ViewModel)

- **Neue Methoden:** `OpenLinkInBrowserAsync(string url)` — public; öffnet eine beliebige externe URL (aus dem WebView-Navigating-Pfad) über `OpenUrlInBrowserAsync`; enthält denselben Offline-Guard (`ErrorMessage = AppResources.OfflineHint`) als Tiefenverteidigung gegen einen zwischen Entscheidung und Aufruf gekippten Netzwerkzustand.
- **Geänderte Methoden:** `OpenInBrowserAsync` — der `Browser.OpenAsync`-/`try/catch`-Kern wird in die neue private Hilfsmethode `OpenUrlInBrowserAsync(string url)` extrahiert; Verhalten des bestehenden `OpenInBrowserCommand` bleibt identisch.

### `ArticleDetailPage` (Code-Behind)

- **Geänderte Methoden:** `OnWebViewNavigating` — ersetzt die bisherige Online-Durchlass-Logik durch den `switch` auf `WebViewNavigationGuard.DecideAction(e.Url, _viewModel.IsOnline)`: `Proceed` → Rückkehr; `CancelAndOpenExternally` → `e.Cancel = true` + `_ = _viewModel.OpenLinkInBrowserAsync(e.Url)`; `CancelAndShowOfflineHint` → `e.Cancel = true` + bestehender `DisplayAlertAsync`.

### `App` (Application)

- **Geänderte Methoden:** `OnStart` — `IDebugLogService`-Auflösung (Zeilen 44–46) wird vor `MigrateAsync` (Zeile 42) gezogen; `MigrateAsync` erhält ein eigenes `try/catch` mit `Debug.WriteLine` + `LogAsync(DebugLogCategory.Lifecycle, "Database migration failed on start"`, `ex.ToString()`, `DebugLogLevel.Error)`; nach dem Migrations-Block folgt ein eigener `try/catch`-Block für den iCloud-Backup-Ausschluss (Auflösen von `DatabasePath` + `IBackupExclusionService`, Aufruf `ExcludeFromBackup` für `reporter.db`, `-wal`, `-shm`).

### `MauiProgram` (statische Klasse)

- **Geänderte Methoden:** `CreateMauiApp` — zwei zusätzliche DI-Registrierungen: `.AddSingleton(new DatabasePath(databasePath))` und `.AddSingleton<IBackupExclusionService, BackupExclusionService>()`.

### `StubFeedServer` (E2E-Testinfrastruktur)

- **Neue Eigenschaften:** `ExternalLinkHitCount` (`int`, per `Interlocked` gezählt) — Anzahl der GET-Aufrufe auf `/external-link`.
- **Geänderte Routen:** `/feeds/{name}.xml` ersetzt zusätzlich den Platzhalter `{baseUrl}` durch `BaseUrl` (neben `{name}`).
- **Neue Routen:** `GET /external-link` — inkrementiert `ExternalLinkHitCount` und liefert eine kleine HTML-Seite (Ziel des externen Artikellinks im E2E-Test; der System-Browser ruft diese URL nach `Browser.OpenAsync` ab).

### `FeedDbAssertions` (E2E-Testinfrastruktur)

- **Neue Methoden:** `ItemExistsAsync(string databasePath, string title, TimeSpan? timeout = null)` — pollt `SELECT COUNT(*) FROM items WHERE title = $title` (Nachweis, dass der Stub-Feed synchronisiert wurde, bevor die Detailansicht geöffnet wird).

## Datenbankmigrationen

Keine — das Schema bleibt unverändert; betroffen ist nur ein Laufzeit-Attribut (`NSUrl.IsExcludedFromBackupKey`) der Datei `reporter.db`.

## Validierungsregeln

Keine — es gibt keine neuen oder geänderten Anwendereingaben. `WebViewNavigationGuard.DecideAction` behandelt `null`/leere URLs wie `IsExternalUrl` (→ `Proceed`); `BackupExclusionService` toleriert fehlende Dateien (`File.Exists`-Guard, No-op).

## Konfigurationsänderungen

Keine Änderungen an Konfigurationsklassen oder `appsettings` — alle Konfigurationswirkungen liegen auf Build-/Plattform-Ebene:

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `CFBundleLocalizations` (`Platforms/iOS/Info.plist`) | plist-Array `string` | `["en", "de"]` | Deklarierte Lokalisierungen für App Store Connect (Punkt 9; deckt sich mit `AppResources.resx`/`AppResources.de.resx` und `Settings.Language` `system`/`de`/`en`) |
| `NSPrivacyAccessedAPICategoryUserDefaults` mit Reason `CA92.1` (`PrivacyInfo.xcprivacy`) | plist-Dict (Accessed API Type) | aktiviert (bisher auskommentiert, Zeilen 41–50) | .NET-Laufzeit/MAUI berühren UserDefaults-APIs (Punkt 5) |
| `NSPrivacyTracking` (`PrivacyInfo.xcprivacy`) | plist-Bool | `false` | App trackt nicht (Punkt 5) |
| `NSPrivacyTrackingDomains` (`PrivacyInfo.xcprivacy`) | plist-Array `string` | leer `[]` | Pflichtschlüssel neben `NSPrivacyTracking` (Punkt 5) |
| `NSPrivacyCollectedDataTypes` (`PrivacyInfo.xcprivacy`) | plist-Array | leer `[]` | Explizite Deklaration „keine Datensammlung" (Punkt 5) |
| `UIDeviceFamily` (`Platforms/iOS/Info.plist`) | plist-Array `integer` | `[1, 2]` → `[1]` (nur iPhone — beschlossene Variante B, Punkt 7) | iPad-Support entfällt in dieser Einreichung; `UISupportedInterfaceOrientations~ipad` wird entfernt |
| `NSAppTransportSecurity`/`NSAllowsArbitraryLoads` (`Platforms/iOS/Info.plist`) | plist-Dict/Bool | unverändert `true` | bleibt bestehen; Begründung wird in `docs/app-store-review.md` dokumentiert (Punkt 4) |

## Seiteneffekte und Risiken

- **Artikeldetailansicht (Punkt 3):** Anwendersichtbare Verhaltensänderung — externe Links im Artikeltext öffnen jetzt immer den System-Browser statt im WebView zu navigieren. Die Hilfedoku (`artikeldetailansicht.md`, `offline.md`, `architektur.md` Zeile 77) beschreibt das bisherige Verhalten und muss aktualisiert werden; die manuelle UI-Verifikation (AGENTS.md) ist in `docs/help/anwendung/mobile-ui-design.md` zu dokumentieren. Der Offline-Hinweisdialog bleibt bestehen.
- **Start-Absicherung (Punkt 6):** Bei Migrationsfehler startet die App jetzt weiter — Folgeoperationen (Repositories) können weiterhin fehlschlagen, werden aber durch die bestehenden `try/catch`-Blöcke bzw. die Fehlerisolierung der Services abgefedert. Bewusstes Verhalten laut Anforderung; der Fehler ist über das Session-Debuglog/`Debug.WriteLine` nachvollziehbar.
- **Backup-Ausschluss (Punkt 8):** Neuer DI-Eintrag `DatabasePath`; das Attribut wirkt erst ab dem nächsten regulären Start auf bestehende Installationen (DB-Datei existiert bereits → Attribut wird beim Start gesetzt). Nur-iOS-Code ist durch die Unit-Suite nicht abgedeckt (wird unter `net10.0` nicht kompiliert) — Verifikation nur auf macOS-Gerät/Simulator möglich.
- **Upload-Tooling (Punkt 11):** `iTMSTransporter`-Verfügbarkeit und exakte Aufrufsyntax (`-m verify` für iOS-IPAs) können nur auf dem Ziel-Mac verifiziert werden; die Änderung ist ohne macOS-Zugriff nur statisch prüfbar. Risiko: `-m verify` wird nicht unterstützt → dokumentierter Fallback (lokale `codesign`-Prüfung + serverseitige Validierung beim Upload). `scripts/iOS-Deployment.md` sowie `docs/help/ios-deployment/ablauf-technisch.md`, `einrichtung-anwender.md`, `troubleshooting.md` referenzieren `altool` und müssen nachgezogen werden.
- **iPad-Entscheidung (Punkt 7, beschlossene Variante B):** Mit `UIDeviceFamily = [1]` entfällt die iPad-Screenshot-Pflicht in App Store Connect (`docs/help/ios-deployment/ablauf-anwender.md` Zeile ~86 muss angepasst werden); bestehende iPad-Nutzer wären betroffen (aktuell irrelevant — App noch nicht veröffentlicht). Die iPad-Layout-Verifikation (v. a. `DisplayActionSheetAsync`-Popover in `FeedsPage`/`CategoriesPage`) entfällt, solange iPad kein deklariertes Target ist.
- **E2E-Neutest:** Der Link-Klick im WebView hängt davon ab, dass WebView2 das DOM über UIA3 exponiert (bestehender XAML-Kommentar belegt, dass WebView2-Subtree im UIA-Baum erscheinen) und dass auf dem Testhost ein Standard-Browser konfiguriert ist, der die Stub-URL abruft. Fallback bei fehlendem `Hyperlink`-Element: echter Mausklick auf die Link-Position (`element.Click(moveMouse: true)` in `UiRetry.InvokeOrClick` bereits vorhanden); als letzter Ausweg dokumentierte manuelle Verifikation statt automatisierter Test.
- **MacCatalyst-`Info.plist`:** enthält ebenfalls `NSAllowsArbitraryLoads` und `UIDeviceFamily`; MacCatalyst ist kein konfiguriertes Target — die Datei bleibt unverändert, keine Abhängigkeit.

## Umsetzungsreihenfolge

1. **Enum `WebViewNavigationAction` + `WebViewNavigationGuard.DecideAction`**
   - Voraussetzungen: Keine.
   - Beschreibung: Neues Enum unter `src/Reporter.Core/Services/` anlegen; `DecideAction(url, isOnline)` in `WebViewNavigationGuard` ergänzen (nutzt intern `IsExternalUrl`).

2. **`ArticleDetailViewModel`: `OpenUrlInBrowserAsync` extrahieren + `OpenLinkInBrowserAsync`**
   - Voraussetzungen: Keine.
   - Beschreibung: `Browser.OpenAsync`-Kern aus `OpenInBrowserAsync` in private Hilfsmethode `OpenUrlInBrowserAsync(string url)` auslagern (Offline-Guard + `try/catch` + `ErrorOpenInBrowserFailed`); neue öffentliche Methode `OpenLinkInBrowserAsync(string url)` als Einstieg für den WebView-Pfad.

3. **`ArticleDetailPage.OnWebViewNavigating` umstellen**
   - Voraussetzungen: Schritte 1 und 2.
   - Beschreibung: Handler auf `switch` über `DecideAction` umbauen (siehe Programmablauf Punkt 3); Offline-Alert bleibt unverändert.

4. **Backup-Ausschluss: `DatabasePath`, `IBackupExclusionService`, `BackupExclusionService`, DI-Registrierung**
   - Voraussetzungen: Keine.
   - Beschreibung: POCO `DatabasePath` in `Reporter.Core/Models`, Interface in `Reporter.Core/Interfaces`, Implementierung in `src/Reporter/Services/` mit `#if IOS`/`NSUrl.IsExcludedFromBackupKey` (inkl. `-wal`/`-shm`-Sidecars per `File.Exists`-Guard) und No-op-Fallback; Registrierungen in `MauiProgram.CreateMauiApp` ergänzen.

5. **`App.OnStart` absichern und Backup-Ausschluss aufrufen**
   - Voraussetzungen: Schritt 4.
   - Beschreibung: `IDebugLogService`-Auflösung vor `MigrateAsync` ziehen, `try/catch` um `MigrateAsync` (Muster der übrigen Start-Schritte), danach eigener fehlerisolierter Block, der `IBackupExclusionService` auf `DatabasePath.FilePath` + Sidecars anwendet.

6. **`PrivacyInfo.xcprivacy` ergänzen**
   - Voraussetzungen: Keine.
   - Beschreibung: `UserDefaults`/`CA92.1`-Eintrag einkommentieren; `NSPrivacyTracking` = `false`, leeres `NSPrivacyTrackingDomains`-Array und leeres `NSPrivacyCollectedDataTypes`-Array ergänzen.

7. **`Info.plist` ergänzen (`CFBundleLocalizations`, `UIDeviceFamily`)**
   - Voraussetzungen: Keine — die iPad-Entscheidung ist getroffen (Variante B, siehe Designentscheidungen).
   - Beschreibung: `CFBundleLocalizations`-Array mit `en` + `de` ergänzen; `UIDeviceFamily` auf `[1]` reduzieren (nur iPhone) und `UISupportedInterfaceOrientations~ipad` entfernen.

8. **`iOS-Deployment.ps1`: `altool` durch `iTMSTransporter` ersetzen**
   - Voraussetzungen: Keine für die Skript-Änderung. Beschlossene Vorab-Prüfung auf dem Ziel-Mac: `xcrun iTMSTransporter --version` (bei Xcode/installierter Transporter-App vorhanden); falls nicht verfügbar, Transporter-App installieren oder als dokumentierte deprecated-Abhängigkeit belassen. Die Ausführungs-Verifikation erfordert macOS.
   - Beschreibung: `Invoke-IpaValidation` (Zeilen 567–568) und `Invoke-StoreUpload` (Zeile 578) umstellen; dokumentierter Fallback, falls `-m verify` nicht verfügbar ist (lokale `codesign`-Prüfung + serverseitige Validierung beim Upload).

9. **Unit-Tests ergänzen**
   - Voraussetzungen: Schritte 1 und 4.
   - Beschreibung: `WebViewNavigationGuardTests` um `DecideAction`-Fälle erweitern; `ServiceCollectionTests` um die `IBackupExclusionService`-/`DatabasePath`-Registrierung ergänzen (mit `FakeBackupExclusionService`, da `Reporter.Tests` das MAUI-Projekt nicht referenziert).

10. **E2E-Testinfrastruktur und `ArticleLinkTests`**
    - Voraussetzungen: Schritt 3; interaktive Windows-Desktop-Session für die Ausführung.
    - Beschreibung: Fixture `Fixtures/link-feed.xml` (Item mit `<a href="{baseUrl}/external-link">` in der Description), `StubFeedServer` um `/external-link`-Route + `ExternalLinkHitCount` + `{baseUrl}`-Ersetzung erweitern, `FeedDbAssertions.ItemExistsAsync` ergänzen, `ArticleLinkTests` schreiben (siehe E2E-Tabelle).

11. **App-Icon-Verifikation (ITMS-90717)**
    - Voraussetzungen: Für die Prüfung des generierten Assets ein iOS-Build (macOS); ohne macOS bleibt die statische Verifikation der SVG-Quellen + `MauiIcon Color="#1e293b"` (opaker Hintergrund).
    - Beschreibung: Generiertes PNG in `Assets.xcassets` auf fehlenden Alpha-Kanal prüfen (oder Fallback-Verifikation der Quellen) und Ergebnis in `docs/app-store-review.md` dokumentieren.

12. **Dokumentationsartefakte anlegen und Bestandsdoku aktualisieren**
    - Voraussetzungen: Schritte 3, 5, 7, 8, 11 für korrekte Inhalte; die Designentscheidungen sind getroffen (keine offenen Punkte mehr).
    - Beschreibung: `docs/privacy-policy.md` zweisprachig anlegen (deutsch primär, englische Sektion): Datenflüsse aus `FeedSearchService`/`FeedSyncService`/`FeedIconService`/`DebugReportService`/lokale SQLite-Ablage, Kontakt = `DebugReportRecipient` (`mstromberg84+reporter@gmail.com`); Hosting über die GitHub-URL des Dokuments auf dem Default-Branch (in App Store Connect als Datenschutz-URL hinterlegen). `docs/app-store-review.md` (ATS-Begründung `NSAllowsArbitraryLoads`, App-Privacy-Antworten „keine Datensammlung" + Privacy-Manifest-Bezug, Altersfreigabe-Empfehlung 12+/ggf. 17+, Review-Hinweis „kein Login — Demo-Feed via `DemoContentService`", iPad-Entscheidung Variante B, Icon-Verifikation). Updates: `docs/help/anwendung/artikeldetailansicht.md`, `offline.md`, `architektur.md` (Zeilen 50/77), `docs/help/ios-deployment/ablauf-anwender.md` (iPad-Screenshot-Pflicht entfernen, Privacy-URL), `ablauf-technisch.md`, `einrichtung-anwender.md`, `troubleshooting.md`, `scripts/iOS-Deployment.md` (`altool`-Stellen), `docs/help/index.md` (Verlinkung), `docs/help/anwendung/mobile-ui-design.md` (UI-Verifikationsnachweis).

13. **Quality Gates**
    - Voraussetzungen: Alle vorherigen Schritte.
    - Beschreibung: `dotnet test Reporter.sln --filter "Category!=E2E"` und `npm test` grün; `.\scripts\Run-E2ETests.ps1` in interaktiver Session ausführen; manuelle UI-Verifikation der geänderten `ArticleDetailPage` am Windows-Handyfenster 390 × 844 pt (AGENTS.md) dokumentieren; `.\scripts\Run-StaticChecks.ps1` muss mit Exit-Code 0 ohne Befund durchlaufen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `DecideAction_ExternalUrl_Online_ReturnsCancelAndOpenExternally` | `WebViewNavigationGuardTests` | http/https-URL + `isOnline: true` → `CancelAndOpenExternally` |
| `DecideAction_ExternalUrl_Offline_ReturnsCancelAndShowOfflineHint` | `WebViewNavigationGuardTests` | http/https-URL + `isOnline: false` → `CancelAndShowOfflineHint` |
| `DecideAction_LocalOrOtherUrl_ReturnsProceed` (Theory: `null`, leer, `about:blank`, `data:`, `file:`, `ftp:`) | `WebViewNavigationGuardTests` | Nicht-externe URLs werden online wie offline durchgelassen |
| `AddReporterServices_ResolvesBackupExclusion` | `ServiceCollectionTests` | `IBackupExclusionService` (via `FakeBackupExclusionService`) und `DatabasePath`-Instanz sind über DI auflösbar — spiegelt die `MauiProgram`-Registrierung |
| `FakeBackupExclusionService` (Hilfsklasse) | `src/Reporter.Tests` | No-op-Implementierung von `IBackupExclusionService` für den DI-Test (Muster `FakeLocalNotificationService`) |
| `ItemExistsAsync` (Hilfsmethode) | `FeedDbAssertions` | Pollt die `items`-Tabelle der isolierten E2E-Datenbank |
| `link-feed.xml` (Fixture) | `src/Reporter.E2ETests/Fixtures` | RSS-Feed mit einem Item, dessen `description` einen `<a href="{baseUrl}/external-link">`-Link enthält |
| `ExternalLinkHitCount` + Route `/external-link` | `StubFeedServer` | Zählt Abrufe der externen Link-URL durch den System-Browser |

**Kein Test für den Migrationsfehler-Pfad in `App.OnStart` (Punkt 6) — beschlossene Begründung:** Der `try/catch` um `MigrateAsync` enthält keine prüfbare Entscheidungslogik — er folgt exakt dem Muster der übrigen Start-Schritte (anders als Punkt 3, wo die Verlagerung von `DecideAction` nach `Reporter.Core` eine echte testbare Stelle schuf). `App.OnStart` liegt im MAUI-Projekt `src/Reporter`, das `Reporter.Tests` nicht referenziert; der Fehlerpfad wäre nur mit korruptem DB-Fixture simulierbar, und die Fehlerisolierung des Loggers ist bereits durch `DebugLogServiceTests.LogAsync_RepositoryError_DoesNotThrow` abgedeckt. Der Happy Path (Migration erfolgreich, App startet) wird indirekt durch den E2E-App-Start der bestehenden Suite (`SmokeTests`/`DemoSeedTests` starten die echte App mit Migration) mitabgedeckt. Daher wird — wie bei den `#if IOS`-Pfaden von Punkt 8 — bewusst kein Unit-/Integrationstest geplant; die Extraktion einer testbaren Stelle nach `Reporter.Core` wurde geprüft und verworfen (siehe Designentscheidungen).

### Betroffene bestehende Tests

Keine — die `WebViewNavigationGuardTests` zur URL-Klassifikation bleiben unverändert gültig; `OpenInBrowserAsync` behält sein Verhalten; keine Signatur bestehender öffentlicher APIs ändert sich.

### E2E-Tests (primärer Funktionsnachweis)

Die einzige anwendersichtbare Verhaltensänderung ist der Benutzerfluss „externer Link im Artikel" (Punkt 3); alle übrigen Punkte sind Build-/Plattform-Metadaten, Skripte oder Dokumentation ohne über die UI erreichbaren Ablauf.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Externer Link im Artikel-WebView (online): Feed `link-feed` über die UI direkt hinzufügen (`Feeds`-Tab → `+` → URL → **URL direkt hinzufügen**), auf dem `Ungelesen`-Tab über den Refresh-Button (`ButtonRefresh`) synchronisieren, Artikelkarte antippen (UIA-Name = Titel via `SemanticProperties.Description`), in der `ArticleDetailPage` das `Hyperlink`-Element im WebView-Subtree finden und per `UiRetry.InvokeOrClick` aktivieren. Assert: `StubFeedServer.ExternalLinkHitCount > 0` (der System-Browser hat die Stub-URL abgerufen) und die Detailansicht bleibt geöffnet (Anker `AppResources.ArticleOpenInBrowser` weiter auffindbar — keine In-App-Navigation). | `src/Reporter.E2ETests/ArticleLinkTests.cs` | Punkt 3: externe http/https-Navigation wird im WebView abgebrochen und die URL im System-Browser geöffnet | Nur E2E deckt den tatsächlichen Benutzerfluss ab — Klick im gerenderten WebView-Inhalt, `Navigating`-Abbruch und Übergabe an den System-Browser (`Browser.OpenAsync` ist eine statische Essentials-API und in Unit-Tests nicht prüfbar). |

Welche bestehenden E2E-Tests müssen angepasst werden? Keine — die bestehende Suite (`SmokeTests`, `DemoSeedTests`) navigiert nicht in die Artikeldetailansicht und berührt keinen der geänderten Abläufe.

Nicht-E2E-abgedeckte Teilfälle, begründet:

- **Offline-Hinweisdialog beim Link-Klick:** In der E2E-Suite nicht auslösbar — `IsOnline` hängt an der echten OS-Konnektivität (`Connectivity.Current`), ein Netzwerk-Umschalten ist in der Suite nicht verfügbar. Abdeckung durch die `DecideAction`-Unit-Tests (offline → `CancelAndShowOfflineHint`) plus dokumentierter manueller Verifikation in `mobile-ui-design.md`.
- **Fehlerfall „System-Browser nicht verfügbar":** `Browser.OpenAsync`-Fehler zeigen `ErrorOpenInBrowserFailed` — in E2E nicht deterministisch auslösbar; der Fehlerpfad teilt sich `OpenUrlInBrowserAsync` mit dem bereits bestehenden „Im Browser öffnen"-Button.
- **Migrationsfehler in `App.OnStart` (Punkt 6):** Kein über die UI auslösbarer Benutzerfluss — der Fehlerpfad ließe sich nur mit korruptem DB-Fixture provozieren. Es wird auch kein Unit-Test geplant (Begründung oben im Tests-Abschnitt: keine prüfbare Logik im Muster-`try/catch`, MAUI-Projekt nicht von `Reporter.Tests` referenziert); der Happy Path ist indirekt über den E2E-App-Start abgedeckt.
- **iOS-spezifische Effekte** (Backup-Attribut, Plist-Schlüssel, `iTMSTransporter`, App-Icon-PNG): laufen nicht unter dem Windows-E2E-Host; Nachweis über statische Prüfung und Dokumentation in `docs/app-store-review.md`.

## Offene Punkte

Keine — die sieben zuvor offenen Punkte wurden vom Anwender mit den empfohlenen Vorschlägen bestätigt und sind als Designentscheidungen bzw. in den Umsetzungsschritten eingearbeitet: iPad-Variante B (`UIDeviceFamily` = `[1]`, Schritt 7), zweisprachige `docs/privacy-policy.md` mit GitHub-URL-Hosting und `DebugReportRecipient` als Kontakt (Schritt 12), `iTMSTransporter` mit dokumentiertem Fallback und Mac-Vorab-Prüfung (Schritt 8), Sidecar-Dateien `-wal`/`-shm` im Backup-Ausschluss (Schritte 4–5), kein Bestätigungsdialog vor externem Link-Öffnen (Programmablauf Punkt 3) sowie die Altersfreigabe-Empfehlung 12+/ggf. 17+ (Schritt 12).
