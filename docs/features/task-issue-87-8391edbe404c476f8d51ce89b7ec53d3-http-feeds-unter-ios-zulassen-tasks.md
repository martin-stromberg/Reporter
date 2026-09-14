<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: HTTP-Feeds unter iOS zulassen (ATS) und Sync-Fehlerursachen in der UI anzeigen (Issue #87)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Plattform-Konfiguration | `NSAppTransportSecurity` mit `NSAllowsArbitraryLoads` (`true`) in `src/Reporter/Platforms/iOS/Info.plist` ergänzen | Offen | — |
| 2 | Plattform-Konfiguration | `NSAppTransportSecurity` mit `NSAllowsArbitraryLoads` (`true`) in `src/Reporter/Platforms/MacCatalyst/Info.plist` ergänzen | Offen | — |
| 3 | Datenmodell | `LastErrorKind` (`string?`, init) und `LastErrorMessage` (`string?`, init) in `src/Reporter.Core/Models/Feed.cs` hinzufügen | Offen | — |
| 4 | Datenmodell | `LastErrorKind`/`LastErrorMessage` (`string?`, init) in `src/Reporter.Core/Models/FeedListItem.cs` hinzufügen | Offen | — |
| 5 | Datenmodell | `LastErrorKind`/`LastErrorMessage` (`string?`) in `src/Reporter.Data/Entities/Feed.cs` hinzufügen | Offen | — |
| 6 | Datenmodell | `ConfigureFeed` in `src/Reporter.Data/ReporterDbContext.cs` um Spalten `last_error_kind` (max 50, nullable) und `last_error_message` (nullable) erweitern | Offen | — |
| 7 | Datenmodell | EF-Migration `AddFeedLastError` in `src/Reporter.Data/Migrations/` erstellen (Konvention `AddFeed*`) | Offen | — |
| 8 | Datenmodell | `MapToEntity`/`MapToModel`/`UpdateAsync`/`GetAllWithDetailsAsync`-Projektion in `src/Reporter.Data/Repositories/FeedRepository.cs` um die neuen Felder ergänzen | Offen | — |
| 9 | Logik | `FeedSyncErrorKind` in `src/Reporter.Core/Services/FeedSyncErrorKind.cs` anlegen (Konstanten `InsecureHttpBlocked`, `HttpStatus`, `Network`, `Parse`, `Unknown` + `Classify(Exception, string)`) | Offen | — |
| 10 | Logik | `UpdateFeedHealthAsync` in `FeedSyncService` um `errorKind`/`errorMessage`-Parameter erweitern und Fehlerfelder schreiben/zurücksetzen | Offen | — |
| 11 | Logik | `catch`-Block in `FeedSyncService.SyncFeedAsync`: Exception klassifizieren und Kategorie + Rohmeldung an `UpdateFeedHealthAsync` durchreichen | Offen | — |
| 12 | Logik | `GetFeedErrorMessage(FeedListItem)` in `FeedsViewModel` hinzufügen (`LastErrorKind` → `AppResources.FeedErrorKind*`, technische Meldung anhängen, Fallback `Unknown`) | Offen | — |
| 13 | Logik | `ToFeed` in `FeedsViewModel` um Durchreichen von `LastErrorKind`/`LastErrorMessage` erweitern (Rename/Kategorie/Edit dürfen Fehlerursache nicht verwerfen) | Offen | — |
| 14 | Lokalisierung | `FeedErrorDetailsTitle`, `ButtonShowErrorDetails` in `AppResources.resx` (EN) und `AppResources.de.resx` (DE) ergänzen | Offen | — |
| 15 | Lokalisierung | `FeedErrorKindInsecureHttpBlocked`, `FeedErrorKindHttpStatus`, `FeedErrorKindNetwork`, `FeedErrorKindParse`, `FeedErrorKindUnknown` in beiden resx-Dateien ergänzen | Offen | — |
| 16 | UI | `OnFeedTapped` in `src/Reporter/Views/FeedsPage.xaml.cs`: Action-Sheet-Eintrag `ButtonShowErrorDetails` nur bei `HealthStatus == "Error"` anbieten | Offen | — |
| 17 | UI | `ShowFeedErrorDetailsAsync` in `FeedsPage.xaml.cs` hinzufügen (`DisplayAlertAsync` mit `FeedErrorDetailsTitle` + `viewModel.GetFeedErrorMessage(feed)` + `ButtonOk`) | Offen | — |
| 18 | Tests | `FeedSyncServiceTests`: Fehlschlag persistiert `LastErrorKind`/`LastErrorMessage`; Erfolg setzt sie zurück | Offen | — |
| 19 | Tests | `FeedSyncServiceTests`: Klassifikation — `http`-Fehlschlag → `InsecureHttpBlocked`, HTTP-Status → `HttpStatus`, `XmlException` → `Parse`, `https`-Netzwerkfehler → `Network` | Offen | — |
| 20 | Tests | `FeedSyncErrorKindTests` (neu): `Classify`-Mapping je Exception-Typ prüfen | Offen | — |
| 21 | Tests | `FeedRepositoryTests`: `UpdateAsync_PersistsLastError`, `GetAllWithDetailsAsync_ProjectsLastError` | Offen | — |
| 22 | Tests | `ReporterDbContextTests_Persistence`: Roundtrip der neuen `feeds`-Spalten | Offen | — |
| 23 | Tests | `FeedsViewModelTests`: `GetFeedErrorMessage` (Kind→lokalisierter Text, Fallback `Unknown`, technischer Anhang) | Offen | — |
| 24 | Tests | `FeedsViewModelTests`: `RenameFeedAsync_PreservesLastError` (ToFeed-Durchreichen) | Offen | — |
| 25 | E2E-Tests | Manuell iOS (`scripts/iOS-Deployment.ps1`): `http://`-Feed hinzufügen + synchronisieren → Items + Health `OK`; Ergebnis in `test-results.md` dokumentieren | Offen | — |
| 26 | E2E-Tests | Manuell UI (390 × 844 pt, Light + Dark): Action-Sheet-Eintrag „Fehlerdetails" nur bei Error-Feeds, Alert mit lokalisiertem Text + Rohmeldung, Verschwinden nach erfolgreichem Re-Sync; Screenshots in `test-results.md`/`docs/help/anwendung/mobile-ui-design.md` | Offen | — |
| 27 | Verifikation | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release` und `.\scripts\Run-StaticChecks.ps1` grün | Offen | — |
