<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: App-Store-Einreichung iOS

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | Enum `WebViewNavigationAction` (`Proceed`/`CancelAndOpenExternally`/`CancelAndShowOfflineHint`) in `src/Reporter.Core/Services/` anlegen | Offen | — |
| 2 | Logik | `WebViewNavigationGuard.DecideAction(string? url, bool isOnline)` hinzufügen (nutzt intern `IsExternalUrl`) | Offen | — |
| 3 | Logik | `ArticleDetailViewModel`: `Browser.OpenAsync`-Kern aus `OpenInBrowserAsync` in private Hilfsmethode `OpenUrlInBrowserAsync(string url)` extrahieren | Offen | — |
| 4 | Logik | `ArticleDetailViewModel`: öffentliche Methode `OpenLinkInBrowserAsync(string url)` hinzufügen (Offline-Guard + `OpenUrlInBrowserAsync`) | Offen | — |
| 5 | UI | `ArticleDetailPage.OnWebViewNavigating` auf `switch` über `DecideAction` umbauen (extern online → `e.Cancel` + `OpenLinkInBrowserAsync`; extern offline → `e.Cancel` + bestehender Alert; sonst durchlassen) | Offen | — |
| 6 | Logik | POCO `DatabasePath` (Eigenschaft `FilePath`) in `src/Reporter.Core/Models/` anlegen (Muster `FirstRunState`) | Offen | — |
| 7 | Logik | Interface `IBackupExclusionService` mit `void ExcludeFromBackup(string filePath)` in `src/Reporter.Core/Interfaces/` anlegen | Offen | — |
| 8 | Logik | `BackupExclusionService` in `src/Reporter/Services/` anlegen: `#if IOS` `NSUrl.SetResource` mit `NSUrl.IsExcludedFromBackupKey` auf db + `-wal`/`-shm` (jeweils `File.Exists`-Guard), No-op auf anderen Targets | Offen | — |
| 9 | Logik | `MauiProgram.CreateMauiApp`: `.AddSingleton(new DatabasePath(databasePath))` und `.AddSingleton<IBackupExclusionService, BackupExclusionService>()` registrieren | Offen | — |
| 10 | Logik | `App.OnStart`: `IDebugLogService`-Auflösung vor `MigrateAsync` ziehen und `try/catch` um `MigrateAsync` ergänzen (`Debug.WriteLine` + `LogAsync(Lifecycle, …, Error)`) | Offen | — |
| 11 | Logik | `App.OnStart`: eigenen fehlerisolierten `try/catch`-Block für den Backup-Ausschluss (`DatabasePath` + `IBackupExclusionService` auflösen, `ExcludeFromBackup` für `reporter.db`/`-wal`/`-shm`) nach dem Migrations-Block einfügen | Offen | — |
| 12 | Plattform | `PrivacyInfo.xcprivacy`: `NSPrivacyAccessedAPICategoryUserDefaults`/`CA92.1` einkommentieren sowie `NSPrivacyTracking` = `false`, leeres `NSPrivacyTrackingDomains` und leeres `NSPrivacyCollectedDataTypes` ergänzen | Offen | — |
| 13 | Plattform | `Info.plist`: `CFBundleLocalizations`-Array mit `en` und `de` ergänzen | Offen | — |
| 14 | Plattform | `Info.plist`: beschlossene iPad-Variante B umsetzen — `UIDeviceFamily` auf `[1]` reduzieren und `UISupportedInterfaceOrientations~ipad` entfernen | Offen | — |
| 15 | Deployment | `iOS-Deployment.ps1`: `Invoke-IpaValidation` — `xcrun altool --validate-app` (Zeilen 567–568) durch `xcrun iTMSTransporter -m verify` ersetzen bzw. dokumentierten Fallback einbauen | Offen | — |
| 16 | Deployment | `iOS-Deployment.ps1`: `Invoke-StoreUpload` — `xcrun altool --upload-app` (Zeile 578) durch `xcrun iTMSTransporter -m upload` ersetzen | Offen | — |
| 17 | Tests | `WebViewNavigationGuardTests`: `DecideAction`-Tests ergänzen (extern online → `CancelAndOpenExternally`, extern offline → `CancelAndShowOfflineHint`, lokale/sonstige URLs → `Proceed`) | Offen | — |
| 18 | Tests | `FakeBackupExclusionService` in `src/Reporter.Tests/` anlegen (No-op, Muster `FakeLocalNotificationService`) | Offen | — |
| 19 | Tests | `ServiceCollectionTests`: Auflösung von `IBackupExclusionService` und `DatabasePath` prüfen | Offen | — |
| 20 | E2E-Tests | Fixture `src/Reporter.E2ETests/Fixtures/link-feed.xml` anlegen (Item mit `<a href="{baseUrl}/external-link">` in der Description) | Offen | — |
| 21 | E2E-Tests | `StubFeedServer`: Route `GET /external-link` mit Zähler `ExternalLinkHitCount` ergänzen und `/feeds/{name}.xml` um `{baseUrl}`-Ersetzung erweitern | Offen | — |
| 22 | E2E-Tests | `FeedDbAssertions.ItemExistsAsync(databasePath, title)` hinzufügen (Poll auf `items`-Tabelle) | Offen | — |
| 23 | E2E-Tests | `ArticleLinkTests.ExternalLinkInArticle_OpensSystemBrowser`: Feed per UI direkt hinzufügen → Sync über Refresh-Button auf `Ungelesen` → Artikelkarte antippen → `Hyperlink` im WebView-Subtree aktivieren → Assert `ExternalLinkHitCount > 0` und Detailansicht weiter offen | Offen | — |
| 24 | Dokumentation | `docs/privacy-policy.md` anlegen (zweisprachig de/en): Datenflüsse `FeedSearchService` (`feedsearch.dev`), `FeedSyncService`/`FeedIconService` (anwenderbestimmte Hosts), Artikel-Bilder, `DebugReportService` (E-Mail an `DebugReportRecipient`), lokale SQLite-Ablage, Kontakt | Offen | — |
| 25 | Dokumentation | `docs/app-store-review.md` anlegen: ATS-Begründung `NSAllowsArbitraryLoads`, App-Privacy-Label-Antworten, Altersfreigabe-Empfehlung, Review-Hinweis „kein Login — Demo-Feed via `DemoContentService`", iPad-Entscheidung, Icon-Verifikation (ITMS-90717) | Offen | — |
| 26 | Dokumentation | `docs/help/anwendung/artikeldetailansicht.md` aktualisieren (externe Links öffnen online den System-Browser; Offline-Dialog bleibt) | Offen | — |
| 27 | Dokumentation | `docs/help/anwendung/offline.md` aktualisieren (Zeile ~22: geändertes Online-Verhalten der Links) | Offen | — |
| 28 | Dokumentation | `docs/help/anwendung/architektur.md` aktualisieren (Zeile ~77: neues `DecideAction`-Verhalten; Zeile ~50: Verweis auf ATS-Begründung in `app-store-review.md`) | Offen | — |
| 29 | Dokumentation | `scripts/iOS-Deployment.md` aktualisieren (`altool`-Stellen Zeilen ~61, 81–82, 136–141 auf `iTMSTransporter`) | Offen | — |
| 30 | Dokumentation | `docs/help/ios-deployment/ablauf-technisch.md`, `einrichtung-anwender.md`, `troubleshooting.md` aktualisieren (`altool`-Referenzen, Suchpfad, Fehlerbilder) | Offen | — |
| 31 | Dokumentation | `docs/help/ios-deployment/ablauf-anwender.md` aktualisieren (iPad-Screenshot-Pflicht entfernen — Variante B beschlossen, Datenschutz-URL, Verweis auf Review-Notizen) | Offen | — |
| 32 | Dokumentation | `docs/help/index.md`: neue Dokumente verlinken | Offen | — |
| 33 | Plattform | App-Icon-Verifikation (ITMS-90717): generiertes PNG in `Assets.xcassets` auf fehlenden Alpha-Kanal prüfen (macOS-iOS-Build) oder Fallback statische SVG-/`MauiIcon`-Prüfung; Ergebnis in `docs/app-store-review.md` dokumentieren | Offen | — |
| 34 | UI | Manuelle UI-Verifikation der geänderten `ArticleDetailPage` am Windows-Handyfenster 390 × 844 pt (AGENTS.md); Nachweis in `docs/help/anwendung/mobile-ui-design.md` dokumentieren | Offen | — |
| 35 | Quality Gate | `dotnet test Reporter.sln --filter "Category!=E2E"` + `npm test` grün, `.\scripts\Run-E2ETests.ps1` (interaktive Session) ausführen, `.\scripts\Run-StaticChecks.ps1` mit Exit-Code 0 ohne Befund | Offen | — |
