<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung — App-Store-Einreichung iOS

## Fachliche Zusammenfassung

Die .NET-MAUI-App Reporter soll für die Einreichung im iOS App Store vorbereitet werden; ein Review hat zwölf Punkte aus den Bereichen Datenschutz-Dokumentation, Plattform-Compliance (Info.plist, Privacy Manifest, ATS, Backup-Attribute, Upload-Tooling) und Stabilität identifiziert. Inhaltlich umfasst das drei echte Code-/Verhaltensänderungen — Einschränkung der WebView-Navigation in `ArticleDetailPage` auf lokale Inhalte mit Öffnen externer Links im System-Browser, Fehlerbehandlung um `context.Database.MigrateAsync()` in `App.OnStart` sowie den iCloud-Backup-Ausschluss der SQLite-Datenbankdatei unter iOS — dazu Plist-/Manifest-Ergänzungen (`CFBundleLocalizations`, `NSPrivacy*`-Schlüssel, `UIDeviceFamily`-Entscheidung), den Ersatz des deprecated `xcrun altool` in `scripts/iOS-Deployment.ps1` und mehrere Dokumentationsartefakte (Datenschutzerklärung, App-Privacy-Label-Angaben, ATS-Begründung, Review-Notizen, iPad-Entscheidung, App-Icon-Verifikation). Die Anforderung enthält keine Auswahl-/Identifikationsanforderungen im Sinne einer Datensatz-Auswahl durch den Anwender; Punkt 7 verlangt jedoch eine zu treffende und zu dokumentierende Produktentscheidung (iPad-Support ja/nein). Zielzustand: Die App steht ohne erkennbare Ungereimtheiten für den App-Store-Upload bereit; bestehende Tests bleiben grün und `.\scripts\Run-StaticChecks.ps1` läuft fehlerfrei.

## Betroffene Klassen und Komponenten

### Datenmodellklassen

- Keine Änderungen am Datenmodell und keine neue EF-Core-Migration erforderlich. Betroffen ist nur ein Laufzeit-Attribut der vorhandenen Datenbankdatei `reporter.db` (iCloud-Backup-Ausschluss, Punkt 8).

### Logikklassen / Services

- `ArticleDetailPage.OnWebViewNavigating` (`src/Reporter/Views/ArticleDetailPage.xaml.cs`, Zeilen 72–81) — Kernänderung (Punkt 3): Aktuell wird externe Navigation bei `IsOnline` durchgelassen und nur offline per `e.Cancel = true` abgebrochen (mit `DisplayAlertAsync`/`AppResources.OfflineHint` + `AppResources.ArticleOfflineLinksDisabled`). Neu: externe http/https-Navigation grundsätzlich abbrechen und die URL im System-Browser öffnen; der Offline-Hinweis-Dialog bleibt für den Offline-Fall erhalten bzw. wird konsistent angepasst.
- `WebViewNavigationGuard` (`src/Reporter.Core/Services/WebViewNavigationGuard.cs`) — bestehende statische Klassifikation `IsExternalUrl` (nur http/https = extern) bleibt die zentrale Entscheidungslogik und wird wiederverwendet.
- `ArticleDetailViewModel.OpenInBrowserAsync` (`src/Reporter/ViewModels/ArticleDetailViewModel.cs`, Zeilen 489–513) — Referenzmuster für das Öffnen externer Links: `Browser.OpenAsync(url, BrowserLaunchMode.SystemPreferred)` mit Offline-Guard (`ErrorMessage = AppResources.OfflineHint`) und `try/catch` mit `AppResources.ErrorOpenInBrowserFailed`. Alternativ steht `Launcher.Default.OpenAsync` als Muster in `Platforms/iOS/NotificationDelegate.cs` (Zeile 73).
- `App.OnStart` (`src/Reporter/App.xaml.cs`, Zeile 42) — `context.Database.MigrateAsync()` ohne `try/catch` (Punkt 6). Anzugleichen an das Muster der übrigen Start-Schritte: `try/catch` + `Debug.WriteLine` + `debugLogService.LogAsync(DebugLogCategory.Lifecycle, <Meldung>, ex.ToString(), DebugLogLevel.Error)`. Zu beachten: `IDebugLogService` wird erst nach `MigrateAsync` aufgelöst (Zeilen 44–46) — die Fehlerbehandlung muss den Service im `catch` auflösen oder die Reihenfolge anpassen; `DebugLogService.LogAsync` ist selbst fehlerisoliert (`Debug.WriteLine`-Fallback, `src/Reporter.Core/Services/DebugLogService.cs` Zeile 105). Zweiter Migrationspfad: `MauiProgram.ApplyPersistedLanguage` ruft bereits synchron `context.Database.Migrate()` auf — dort mit `try/catch` abgesichert (`src/Reporter/MauiProgram.cs`, Zeilen 163–179).
- `MauiProgram.CreateMauiApp` (`src/Reporter/MauiProgram.cs`, Zeilen 46–61) — Ermittlung des effektiven DB-Pfads (`FileSystem.AppDataDirectory/reporter.db`, Override `REPORTER_DB_PATH`). Anknüpfungspunkt für Punkt 8: `NSURL.IsExcludedFromBackupKey` auf die DB-Datei setzen — zeitlich nach der Datei-Erzeugung durch die Migration (in `ApplyPersistedLanguage` bzw. `App.OnStart` nach `MigrateAsync`).
- Dokumentationsquellen für die Datenschutzerklärung (Punkte 1/2): `FeedSearchService` sendet Suchbegriffe an `feedsearch.dev` (Endpunkt-Override `REPORTER_FEEDSEARCH_ENDPOINT`); `FeedSyncService`/`FeedIconService`/`FeedSearchService`-Autodiscovery rufen beliebige, anwenderbestimmte Feed-URLs, Webseiten und Favicons ab — dabei geht die Geräte-IP an Fremdhosts; Artikel-Bilder werden im WebView/Listenthumbnails von Fremdhosts geladen. `DebugReportService` (`src/Reporter.Core/Services/DebugReportService.cs`) stellt den Debugbericht zusammen (App-/Geräteinformationen via `IDeviceInfoProvider`, `Settings`-Snapshot, Feed-Liste inkl. URLs, `SyncLog`, Session-`DebugLogEntry`s) und übergibt ihn via `IEmailService`/`EmailService` an den System-Mail-Client an `DebugReportRecipient` (`Directory.Build.props`: `mstromberg84+reporter@gmail.com`). Alle abonnierten Inhalte und Einstellungen liegen ausschließlich lokal in SQLite (`ReporterDbContext`).

### Interfaces

- Keine neuen Interfaces erwartet. Für den iOS-Backup-Ausschluss (Punkt 8) bietet sich das bestehende Gateway-/Plattform-Muster an: kleine plattformspezifische Implementierung (`#if IOS` bzw. Datei unter `src/Reporter/Platforms/iOS/`) mit No-Op auf anderen Targets — analog `LocalNotificationService`/`BackgroundRefreshService` (`src/Reporter/Services/`).

### Enums

- Keine neuen oder geänderten Enums.

### UI-Komponenten / Views

- `ArticleDetailPage` (Punkt 3, s. o.) — die einzige Verhaltensänderung mit UI-Bezug; nach AGENTS.md ist die manuelle UI-Verifikation zu dokumentieren (`docs/help/anwendung/mobile-ui-design.md` oder `test-results.md`).
- `FeedsPage` (`src/Reporter/Views/FeedsPage.xaml.cs`, Zeilen 79 und 154) und `CategoriesPage` (`src/Reporter/Views/CategoriesPage.xaml.cs`, Zeile 48) — `DisplayActionSheetAsync`-Aufrufe, die auf iPad als Popover dargestellt werden; relevant nur bei Variante „iPad-Support beibehalten" (Punkt 7).

### Plattform-Metadaten, Skripte und Dokumentation

- `src/Reporter/Platforms/iOS/Info.plist` — `CFBundleLocalizations` mit `en` und `de` ergänzen (Punkt 9; deckt sich mit `AppResources.resx`/`AppResources.de.resx` unter `src/Reporter.Core/Resources/Strings/`); `UIDeviceFamily` (aktuell `1` iPhone + `2` iPad, Zeilen 9–13) je nach Entscheidung anpassen, ggf. inkl. `UISupportedInterfaceOrientations~ipad` (Zeilen 24–30); `NSAppTransportSecurity`/`NSAllowsArbitraryLoads = true` (Zeilen 43–47) bleibt — Begründung dokumentieren (Punkt 4).
- `src/Reporter/Platforms/iOS/Resources/PrivacyInfo.xcprivacy` — auskommentierten Eintrag `NSPrivacyAccessedAPICategoryUserDefaults` mit Reason `CA92.1` aktivieren; `NSPrivacyTracking` = `false` mit leerem `NSPrivacyTrackingDomains`-Array und leeres `NSPrivacyCollectedDataTypes`-Array ergänzen (Punkt 5).
- `scripts/iOS-Deployment.ps1` — `Invoke-IpaValidation` (Zeilen 567–568: `xcrun altool --validate-app`) und `Invoke-StoreUpload` (Zeile 578: `xcrun altool --upload-app`) auf unterstütztes Tooling umstellen (z. B. `xcrun iTMSTransporter`) oder Abhängigkeit/Risiko dokumentieren (Punkt 11). `Copy-ApiKeyToMac` spiegelt `AuthKey_<ApiKeyId>.p8` nach `~/.appstoreconnect/private_keys/` (Zeilen 520–536) — Suchpfad-Kompatibilität des Ersatztools prüfen. Begleitdoku: `scripts/iOS-Deployment.md`, `docs/help/ios-deployment/ablauf-technisch.md`, `einrichtung-anwender.md`, `troubleshooting.md`.
- `src/Reporter/Resources/AppIcon/appicon.svg` + `appiconfg.svg` und `MauiIcon`-Eintrag in `src/Reporter/Reporter.csproj` (Zeile 60, `Color="#1e293b"`) — Alpha-Kanal-Verifikation des generierten Icons (ITMS-90717, Punkt 10) und Dokumentation.
- Neue Dokumente: Datenschutzerklärung (`docs/privacy-policy.md` oder unter `docs/help/`), App-Privacy-Label-Angaben (eigenes Dokument oder Abschnitt der Datenschutzerklärung), Review-Notizen für die Einreichung (`docs/app-store-review.md` oder unter `docs/help/ios-deployment/`) mit ATS-Begründung, Hinweis „kein Login erforderlich — Demo-Feed wird beim ersten Start angelegt" (`DemoContentService`/`FirstRunState`, Seed: Kategorie „News" + Feed `https://www.apple.com/newsroom/rss-feed.rss`), Altersfreigabe-Empfehlung und Verweis auf die Datenschutzerklärung; Dokumentation der iPad-Entscheidung und der Icon-Prüfung.
- Zu aktualisierende Bestandsdoku: `docs/help/anwendung/artikeldetailansicht.md`, `offline.md` und `architektur.md` (geändertes WebView-Navigationsverhalten — dort ist das bisherige Offline-only-Blocking beschrieben, `architektur.md` Zeile 77).

### Tests

- `src/Reporter.Tests/WebViewNavigationGuardTests.cs` — bestehende Tests zur URL-Klassifikation bleiben unverändert gültig. Für das geänderte Navigationsverhalten (Punkt 3) ist die Entscheidungslogik (extern → abbrechen + System-Browser; offline → Hinweis) heute im Code-Behind nicht direkt unit-testbar; sinnvoll ist die Verlagerung in testbaren Code in `Reporter.Core` plus ergänzende Unit-Tests.
- Gesamte Suite `dotnet test Reporter.sln --filter "Category!=E2E"` muss grün bleiben; `.\scripts\Run-StaticChecks.ps1` fehlerfrei (AGENTS.md).

## Implementierungsansatz

- **Punkte 1, 2, 4, 12 (Dokumentation):** Neue Markdown-Artefakte unter `docs/` bzw. `docs/help/ios-deployment/` anlegen und in `docs/help/index.md`/Index-Dateien verlinken. Inhalte direkt aus dem Code ableitbar (s. o. bei den Services). Die ATS-Begründung existiert bereits ansatzweise (`docs/help/anwendung/architektur.md` Zeile 50, `docs/help/ios-deployment/ablauf-anwender.md` Zeile 89) und ist zu einer Review-Notiz zu konsolidieren: `NSAllowsArbitraryLoads` ist nötig, weil anwenderdefinierte Feeds/Bilder teils nur per HTTP erreichbar sind und `NSExceptionDomains` beliebige Hosts nicht abdecken kann.
- **Punkt 3 (WebView):** In `OnWebViewNavigating` bei `WebViewNavigationGuard.IsExternalUrl(e.Url)` immer `e.Cancel = true` setzen; online die URL per `Browser.OpenAsync`/`Launcher` im System-Browser öffnen (Muster `OpenInBrowserAsync`), offline den bestehenden lokalisierten Alert zeigen. Nicht-http(s)-Navigationen (initiales `HtmlWebViewSource`, `about:blank`, `data:`) laufen weiter durch. Annahme: Das Öffnen im System-Browser erfolgt ohne zusätzlichen Bestätigungsdialog; der Fehlerfall folgt dem `ErrorOpenInBrowserFailed`-Muster. Für Unit-Tests die Cancel-/Redirect-Entscheidung in eine testbare Stelle (z. B. `WebViewNavigationGuard` oder eine kleine Entscheidungsklasse in `Reporter.Core`) verlagern — konkrete Form im Umsetzungsplan festlegen.
- **Punkt 5 (Privacy Manifest):** Reiner plist-Edit in `PrivacyInfo.xcprivacy` — kein Code, keine Abhängigkeiten.
- **Punkt 6 (Start-Absicherung):** `try/catch` um `MigrateAsync` in `App.OnStart`; Logging über `IDebugLogService` (`DebugLogCategory.Lifecycle`, `DebugLogLevel.Error`) und `Debug.WriteLine`; App-Start darf nicht verhindert werden. Reihenfolge-Abhängigkeit zur Service-Auflösung beachten (s. o.).
- **Punkt 7 (iPad):** Entscheidung treffen und dokumentieren. Variante A (iPad beibehalten): Layout-Verifikation auf iPad-Formfaktor inkl. `DisplayActionSheetAsync`-Popover in `FeedsPage`/`CategoriesPage` — erfordert macOS (iPad-Simulator via `iOS-Deployment.ps1 -Action simulator` oder Gerät). Variante B (iPad entfernen): `UIDeviceFamily` auf `1` reduzieren und `UISupportedInterfaceOrientations~ipad` entfernen.
- **Punkt 8 (Backup-Ausschluss):** Unter `#if IOS` `NSUrl.SetResource` mit `NSURL.IsExcludedFromBackupKey` auf den effektiven DB-Pfad anwenden (nach Datei-Erzeugung); Windows- und andere Targets unverändert; `REPORTER_DB_PATH`-Override respektieren.
- **Punkt 9 (Lokalisierung):** `CFBundleLocalizations`-Array (`en`, `de`) in `Info.plist` ergänzen.
- **Punkt 10 (App-Icon):** `MauiIcon` setzt bereits `Color="#1e293b"` als opaken Hintergrund; zu verifizieren ist das generierte Asset (PNG im `Assets.xcassets` des iOS-Builds) auf fehlenden Alpha-Kanal — Ergebnis dokumentieren, ggf. Hintergrundfarbe sicherstellen.
- **Punkt 11 (Upload-Tooling):** `altool`-Aufrufe in `Invoke-IpaValidation`/`Invoke-StoreUpload` durch `xcrun iTMSTransporter` (Upload; Validierung soweit vom Tool unterstützt) oder eine andere dokumentierte Alternative ersetzen; falls nicht umsetzbar, Abhängigkeit und Risiko dokumentieren. Begleitdoku in `scripts/iOS-Deployment.md` und `docs/help/ios-deployment/` nachziehen.
- **Qualitätssicherung:** Unit-Tests für geändertes Verhalten (v. a. Punkt 3) ergänzen, Suite grün halten, `Run-StaticChecks.ps1` fehlerfrei, Doku-/UI-Verifikationsregeln aus `AGENTS.md` einhalten.

## Konfiguration

- Kein neuer Laufzeit-Konfigurationsbedarf und kein neuer `Settings`-Datensatz: Alle Änderungen liegen auf Build-/Plattform-Ebene (`Info.plist`, `PrivacyInfo.xcprivacy`, `Reporter.csproj`, Deployment-Skript) oder sind Repository-Dokumentation.
- Bestehende Prozess-Overrides bleiben zu respektieren: `REPORTER_DB_PATH` (der Backup-Ausschluss muss auf den effektiven, ggf. übersteuerten Pfad wirken) und `REPORTER_FEEDSEARCH_ENDPOINT` (relevant nur für die Dokumentation des Datenflusses).
- `DebugReportRecipient` ist bereits als Build-Property in `Directory.Build.props` konfigurierbar — für die Datenschutzerklärung nur dokumentarisch relevant.

## Offene Fragen

- **iPad-Strategie (Punkt 7):** Soll der iPad-Support beibehalten oder entfernt werden? Die geforderte Layout-Verifikation (insb. `DisplayActionSheetAsync` als Popover) setzt macOS-Zugriff (iPad-Simulator oder Gerät) voraus — ist das im aktuellen Umfeld verfügbar, oder wird `UIDeviceFamily` auf iPhone reduziert? Annahme: Ohne verifizierbaren iPad-Zugriff ist die Entfernung die risikoärmere Variante.
- **Altersfreigabe (Punkt 12):** Welche konkrete Einstufung bzw. welche Fragebogen-Antworten werden empfohlen? Nach Umsetzung von Punkt 3 entfällt „unrestricted web access"; anwenderbestimmte Feed-Inhalte können jedoch weiterhin nicht kontrollierte Inhalte enthalten — die finale Rating-Antwort ist festzulegen.
- **Datenschutzerklärung (Punkt 1):** In welcher Sprache/n (de, en oder beides) und an welchem Ort (`docs/privacy-policy.md` vs. `docs/help/`)? App Store Connect verlangt eine öffentlich erreichbare URL — wo wird das Dokument gehostet (z. B. GitHub-Repository-URL)? Genügt ein Repo-Verweis?
- **Verantwortlicher/Kontakt (Punkt 1):** Ist `DebugReportRecipient` (`mstromberg84+reporter@gmail.com`, `Directory.Build.props`) die in der Datenschutzerklärung zu nennende Kontakt-/Verantwortlichen-Adresse?
- **Upload-Tooling (Punkt 11):** Ist `iTMSTransporter` auf dem Ziel-Mac verfügbar (`xcrun iTMSTransporter` bzw. installierte Transporter-App)? Falls nein, bleibt nur die dokumentierte deprecated-Abhängigkeit als Ergebnis.
- **Backup-Ausschluss (Punkt 8):** Sollen neben `reporter.db` auch SQLite-Sidecar-Dateien (`reporter.db-wal`, `reporter.db-shm`) vom Backup ausgeschlossen werden? Annahme: ja — sie sind Bestandteil der Datenbank; in der Umsetzung zu prüfen.
- **WebView-Verhalten (Punkt 3):** Soll ein angehaltener externer Link online still im System-Browser geöffnet werden (abgeleitete Annahme: ja) oder zusätzlich ein Hinweis-/Bestätigungsdialog erscheinen? Im Offline-Fall bleibt der bestehende Hinweis-Dialog bestehen.
