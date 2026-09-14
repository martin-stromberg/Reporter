<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung: HTTP-Feeds unter iOS zulassen (ATS) und Sync-Fehlerursachen in der UI anzeigen (Issue #87)

## Fachliche Zusammenfassung

Die Anforderung umfasst zwei Teilfeatures. **(1) HTTP-Feeds unter iOS funktionsfähig machen:** Apples App Transport Security (ATS) blockiert Klartext-HTTP des gemeinsam genutzten `HttpClient` (in `MauiProgram` als Singleton registriert, auf iOS/MacCatalyst standardmäßig `NSUrlSessionHandler`), sodass `http://`-Feed-URLs in `FeedSyncService.RunSyncAsync` (`GetStreamAsync(feed.Url)`) bei jedem Sync scheitern. Die App soll HTTP-Feeds entweder per `NSAppTransportSecurity`-Konfiguration in den `Info.plist`-Dateien zulassen oder die Feed-URL beim Hinzufügen bzw. nach einem ATS-Fehlschlag auf HTTPS hochstufen. **(2) Fehlerursache sichtbar machen:** Die konkrete Fehlermeldung wird bereits persistiert (`SyncLog.Message`, verknüpft über `SyncLog.FeedId`, geschrieben von `FeedSyncService` als `"Synchronization failed: {ex.Message}"`), ist aber in der UI unzugänglich — die Feed-Karte zeigt nur das `HealthStatus`-Badge, das `FeedsViewModel` blendet nur den generischen Text `AppResources.SyncStatusError` ein. Die letzte Fehlermeldung je Feed soll dem Anwender in der UI erreichbar sein, vorzugsweise als verständlicher lokalisierter Text statt der englischen Roh-Exception-Message.

## Betroffene Klassen und Komponenten

### Plattform-Konfiguration (Teil 1: ATS)

- `src/Reporter/Platforms/iOS/Info.plist` — enthält aktuell **keinen** `NSAppTransportSecurity`-Schlüssel; je nach Entscheid kommt hier `NSAllowsArbitraryLoads` oder ein `NSExceptionDomains`-Block hinzu. Nicht erforderlich bei der HTTPS-Upgrade-Variante.
- `src/Reporter/Platforms/MacCatalyst/Info.plist` — dieselbe ATS-Thematik gilt für MacCatalyst (Annahme: mitbehandeln, da dort derselbe `HttpClient`-Stack läuft).

### Datenmodell / Persistenz (Teil 2: Fehlersichtbarkeit — zwei Varianten)

- **Variante A — Fehlermeldung am Feed speichern (EF-Migration):**
  - `src/Reporter.Core/Models/Feed.cs` — neue Eigenschaft `LastErrorMessage` (`string?`, Arbeitsname).
  - `src/Reporter.Core/Models/FeedListItem.cs` — entsprechende Eigenschaft für die Karten-Anzeige.
  - `src/Reporter.Data/Entities/Feed.cs` — neue Property.
  - `src/Reporter.Data/ReporterDbContext.cs` (`ConfigureFeed`) — neue Spalte in snake_case (z. B. `last_error_message`, nullable, ggf. `HasMaxLength`).
  - `src/Reporter.Data/Migrations/` — neue Migration nach der `AddFeed*`-Namenskonvention (Referenz: `AddFeedFaviconUrl`); `Migrate()` läuft bereits beim App-Start.
  - `src/Reporter.Data/Repositories/FeedRepository.cs` — Mapping in `UpdateAsync`, `MapToModel`, `MapToEntity` und der `GetAllWithDetailsAsync`-Projektion ergänzen.
- **Variante B — letzte `SyncLog`-Meldung je Feed nachschlagen (ohne Migration):**
  - `src/Reporter.Core/Interfaces/ISyncLogRepository.cs` — neue Methode, z. B. `GetLatestByFeedAsync(Guid feedId, int maxEntries)` (Arbeitsname); die vorhandenen Methoden `GetAllAsync`/`GetLatestAsync(int)`/`GetByIdAsync` decken eine Feed-bezogene Abfrage nicht ab. `SyncLog.FeedId` und der `feed_id`-FK in `ConfigureSyncLog` existieren bereits.
  - `src/Reporter.Data/Repositories/SyncLogRepository.cs` — Implementierung der neuen Abfrage (`Where(s => s.FeedId == feedId)`, absteigend nach `StartedAt`).

### Logikklassen / Services

- `src/Reporter.Core/Services/FeedSyncService.cs` — betroffene Stelle `RunSyncAsync` (`_httpClient.GetStreamAsync(feed.Url, …)`): bei der HTTPS-Upgrade-Variante hier nach einem ATS-/Verbindungsfehlschlag ein `http → https`-Retry über `UriBuilder` (Scheme-Tausch) und ggf. Persistenz der hochgestuften URL via `UpdateFeedHealthAsync`. Für die Fehlersichtbarkeit muss `UpdateFeedHealthAsync` (Variante A) die Meldung am `Feed` mitschreiben bzw. bei Erfolg zurücksetzen. Zusätzlich ist hier der denkbar zentrale Punkt, die Roh-Exception-Message auf einen verständlichen Text zu mappen (z. B. Erkennung des ATS-Fehlertexts → lokalisierter Hinweis) — Annahme, siehe Offene Fragen.
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs` und `FeedsViewModel.Search.cs` — HTTPS-Upgrade an den Add-/Edit-Pfaden: `TryPersistNewFeedAsync` (gemeinsamer Persist-Schritt von `DirectAddAsync`, `OfferDirectAddAsync`, `SubscribeResultAsync`), `TryResolveSearchUrl`/`TryNormalizeDomainUrl` (normalisiert Domain-Eingaben bereits auf `https://`), sowie `SaveAsync` (Edit-Pfad). Für die Fehleranzeige neue bindbare Property/Command, z. B. `ShowFeedErrorAsync(FeedListItem)` bzw. ein Rückruf-Muster wie `ConfirmDirectAddAsync` (Func-Callback aus dem Code-Behind, damit das ViewModel frei von UI-Abhängigkeiten bleibt). Bei Variante B neue Abhängigkeit `ISyncLogRepository` im Konstruktor.
- `src/Reporter.Core/Services/FeedSearchService.cs` — liefert `http://`-Feed-URLs aus dem feedsearch.dev-Verzeichnis (`SearchDirectoryAsync`) und aus der Autodiscovery (`ExtractFeedLinks`, `ProbeStandardPathsAsync` akzeptieren http/https). Zu entscheiden: Ergebnisse beim Abonnieren upgraden/markieren oder unverändert lassen (Offene Frage).
- `src/Reporter.Core/Services/FeedIconService.cs` — `FindFaviconUrlAsync`/`TryFindFaviconUrlAsync` laden Site-HTML und Icon-Kandidaten; für `http`-Feeds sind diese Aufrufe unter iOS ebenfalls ATS-blockiert (Fehlschlag ist isoliert, führt nur zu fehlendem Favicon). Bei HTTPS-Upgrade der Feed-URL profitiert auch der Favicon-Lookup (`FeedSiteResolver` nutzt die Feed-URL-Authority).
- `src/Reporter/MauiProgram.cs` — `HttpClient`-Registrierung (`AddSingleton<HttpClient>`) nur dann betroffen, falls plattformspezifische Handler-Konfiguration nötig wird (Annahme: nicht erforderlich — `Info.plist`-ATS-Regeln greifen global auf `NSUrlSessionHandler`-Ebene).

### Interfaces

- `ISyncLogRepository` — neue per-Feed-Abfrage nur bei Variante B (siehe oben).
- `IFeedRepository`, `IFeedSyncService`, `IFeedSearchService`, `IFeedIconService` — voraussichtlich unverändert; Methodensignaturen bleiben bestehen (Annahme).

### Enums

- Keine neuen Enums erwartet. Der Health-Status nutzt weiterhin die `FeedHealth`-Konstanten (`Ok`/`Warning`/`Error`). Falls das Fehler-Mapping strukturierte Fehlerkategorien braucht, wäre ein Enum (z. B. `FeedSyncErrorKind` — ATS/Netzwerk/Parse/HTTP-Status, Arbeitsname) eine Option, um die Lokalisierung sauber von der Exception-Erkennung zu trennen (Annahme/Entwurfsvariante).

### UI-Komponenten (.NET MAUI)

- `src/Reporter/Views/FeedsPage.xaml` — Feed-Karte mit Health-Badge (`Border`-Pille + `HealthStatus*Label`, `DataTrigger` auf `HealthStatus`). Optionen: Tap-Geste auf das Badge (`TapGestureRecognizer`), zusätzliche Fehler-Detailzeile in der Karte (`Label`, `BodySmallStyle`/`MetaStyle`, nur sichtbar bei `HealthStatus == "Error"`), oder eigener Sync-Protokoll-Screen. Mobile-UI-Regeln aus `AGENTS.md` beachten: Touch-Ziele ≥ 44 × 44 pt, `AppThemeBinding`, keine verschachtelten `ScrollView`/`CollectionView`. Referenz-Layout: `design-draft/stitch_local_rss_feed_reader/feeds_health_status[_dark_mode]/screen.png` — ein Entwurfs-Screen für die Fehlerdetail-Anzeige existiert nicht (Annahme: bestehende Karten-Konvention erweitern).
- `src/Reporter/Views/FeedsPage.xaml.cs` — `OnFeedTapped` öffnet aktuell `DisplayActionSheetAsync` mit den Aktionen Aktualisieren/Umbenennen/Kategorie/Bearbeiten/Löschen; naheliegende Erweiterung: zusätzlicher Eintrag „Fehlerdetails anzeigen" (nur bei `HealthStatus == Error`) oder `DisplayAlertAsync` mit der Meldung, analog `ConfirmDeleteFeedAsync`/`OnSearchResultTapped`.
- `src/Reporter.Core/Resources/Strings/AppResources.resx` + `AppResources.de.resx` (+ generierter `AppResources.Designer.cs`) — neue Schlüssel für Fehlerdetail-Titel/-Button, verständliche ATS-Meldung (z. B. „Feed-URL ohne HTTPS wird von iOS blockiert") und ggf. weitere gemappte Fehlerkategorien; Konvention: bestehende `SyncStatus*`/`HealthStatus*`/`ErrorFeed*`-Schlüssel.

### Tests (`src/Reporter.Tests/`)

- `FeedSyncServiceTests` — HTTPS-Retry/Upgrade-Verhalten (ATS-artiger Fehlschlag auf `http`, Erfolg über `https`, Persistenz der upgegradeten URL), Mitschreiben/Rücksetzen der Fehlermeldung (Variante A), Mapping auf lokalisierte/verständliche Meldung.
- `FeedsViewModelTests` — HTTPS-Upgrade in `TryPersistNewFeedAsync`/`SaveAsync`, Fehlerdetails-Command/-Property, Behandlung von Feeds ohne gespeicherte Meldung.
- `SyncLogRepositoryTests` — neue per-Feed-Abfrage (Variante B).
- `FeedRepositoryTests`, `ReporterDbContextTests_Schema`, `ReporterDbContextTests_Persistence` — neue Spalte und Roundtrip (Variante A).
- `FeedSearchServiceTests` — ggf. Upgrade/Markierung von `http`-Ergebnissen.
- `ServiceCollectionTests` — falls neue Registrierungen hinzukommen.
- Plattform-/UI-Verifikation ohne Unit-Abdeckung: manueller Test auf iOS (`scripts/iOS-Deployment.ps1`, Simulator) mit einem `http`-Feed sowie Layout-Check am 390 × 844-pt-Windows-Fenster (Light + Dark) mit Screenshots in `test-results.md`/`docs/help/anwendung/mobile-ui-design.md` gemäß `AGENTS.md`.

## Implementierungsansatz

1. **ATS-Strategie festlegen** (Offene Frage, Vorentscheid nötig):
   - *Variante „Info.plist":* `NSAppTransportSecurity` in `Platforms/iOS/Info.plist` (und ggf. `Platforms/MacCatalyst/Info.plist`) ergänzen. `NSAllowsArbitraryLoads` erlaubt HTTP global, ist aber bei App-Store-Reviews begründungspflichtig; `NSExceptionDomains` ist eine statische Whitelist und für beliebige, vom Anwender eingegebene Feed-Domains praktisch ungeeignet (nur für fest bekannte Domains sinnvoll).
   - *Variante „HTTPS-Upgrade":* Feed-URLs beim Anlegen (`FeedsViewModel.Search.TryPersistNewFeedAsync`/`DirectAddAsync`/`SubscribeResultAsync`), beim Bearbeiten (`SaveAsync`) und/oder nach einem ATS-Fehlschlag im Sync (`FeedSyncService.RunSyncAsync`) per `UriBuilder` auf `https` hochstufen; bei Erfolg die upgegradete URL persistieren, bei Fehlschlag die ursprüngliche URL behalten. Zu klären: Duplikatprüfung `GetByUrlAsync` gegenüber http/https-Varianten und ob bestehende `http`-Feeds beim nächsten Sync migriert werden.
   - Kombinationen sind denkbar (HTTPS-Upgrade zuerst, HTTP-Fallback nur mit ATS-Ausnahme).
2. **Fehlerursache bereitstellen:** Entweder `Feed.LastErrorMessage` (Variante A, `FeedSyncService.UpdateFeedHealthAsync` schreibt die Meldung beim Fehler und leert sie bei Erfolg; `GetAllWithDetailsAsync` projiziert sie auf `FeedListItem`) oder Lookup des jüngsten `SyncLog`-Eintrags je Feed über eine neue `ISyncLogRepository`-Methode (Variante B, keine Migration; das ViewModel löst die Meldung erst beim Anzeigen auf).
3. **Fehlerursache anzeigen:** Interaktion auf der `FeedsPage` — Tap auf das Status-Badge oder zusätzlicher Action-Sheet-Eintrag → `DisplayAlertAsync` mit der Meldung (Muster: bestehende `DisplayAlertAsync`/`DisplayActionSheetAsync`-Aufrufe im Code-Behind, Callback-Muster wie `ConfirmDirectAddAsync`). Alternativ eine Inline-Detailzeile auf der Karte. Ein eigener Sync-Protokoll-Screen wäre der größte Umbau und nur zu wählen, wenn ein vollständiges Protokoll gewünscht ist (Offene Frage).
4. **Verständliche Meldungen:** `SyncLog.Message` enthält technische Roh-Texte (englische Exception-Messages). Mapping auf lokalisierte Texte in `AppResources` — mindestens für den ATS-Fall; die Rohmeldung kann im Debugbericht (`IDebugLogService`/`IDebugReportService`, schreibt bereits `ex.ToString()`) unverändert erhalten bleiben.
5. **Weitere HTTP-Aufrufe mitbehandeln:** `FeedIconService` (Site-HTML + Icon-Kandidaten) und `FeedSearchService` (Autodiscovery, Standard-Pfad-Probing auf der eingegebenen URL) nutzen denselben `HttpClient` und unterliegen derselben ATS-Policy — bei der Info.plist-Variante automatisch abgedeckt, bei der HTTPS-Upgrade-Variante konsistent upgraden (Annahme).
6. **Verifikation:** `dotnet test` (`Reporter.Tests`), `.\scripts\Run-StaticChecks.ps1`, manuelle UI-Verifikation 390 × 844 pt Light + Dark, iOS-Verifikation mit `http`-Feed über `scripts/iOS-Deployment.ps1`.

## Konfiguration

- **ATS-Freigabe ist Build-/Plattform-Ebene, keine Laufzeit-Einstellung:** `NSAppTransportSecurity` steht statisch in der `Info.plist` und lässt sich nicht zur Laufzeit umschalten. Eine benutzerseitige Option („Unsichere HTTP-Feeds zulassen") im `Settings`-Datensatz könnte allenfalls den HTTPS-Upgrade-/Warn-Pfad steuern, nicht die ATS-Policy selbst (Annahme — aus der Anforderung ist kein Konfigurationswunsch ableitbar).
- **Keine neue Einstellung vorgesehen:** Die `Settings`-Tabelle/`ISettingsRepository` bleibt voraussichtlich unberührt; einzige Schemaänderung wäre `feeds.last_error_message` bei Variante A.

## Offene Fragen

- **ATS-Strategie:** HTTP global zulassen (`NSAllowsArbitraryLoads`, App-Store-begründungspflichtig), HTTPS-Upgrade der Feed-URL, oder Kombination (Upgrade mit HTTP-Fallback + gezielte Ausnahme)? `NSExceptionDomains` ist statisch und für beliebige Nutzer-Domains faktisch ungeeignet — soll es trotzdem für eine feste Domain-Liste angeboten werden?
- **Bestehende HTTP-Feeds:** Sollen bereits gespeicherte `http`-URLs automatisch auf `https` migriert werden (beim nächsten Sync/App-Start) oder bleiben sie bis zum manuellen Edit unverändert? Gilt die Duplikatprüfung (`GetByUrlAsync`) scheme-sensitiv — darf ein `http`-Feed hinzugefügt werden, wenn die `https`-Variante bereits existiert?
- **Feed-Suche:** Sollen `http`-Ergebnisse aus dem Verzeichnis/der Autodiscovery weiterhin angeboten, beim Abonnieren still hochgestuft oder mit einer Warnung versehen werden?
- **Ort und Detailgrad der Fehleranzeige:** Tap auf das Status-Badge mit `DisplayAlert`, zusätzlicher Eintrag im bestehenden Action-Sheet, Inline-Detailzeile auf der Feed-Karte oder eigener Sync-Protokoll-Screen? Reicht die letzte Meldung oder soll ein Verlauf sichtbar sein?
- **Meldungsinhalt:** Roh-Exception-Text anzeigen oder auf lokalisierte, verständliche Texte mappen (z. B. ATS → „Feed-URL ohne HTTPS wird von iOS blockiert")? Soll die Rohmeldung zusätzlich/zweitstufig erreichbar bleiben?
- **Datenquelle der Meldung:** Neue Spalte `Feed.LastErrorMessage` inkl. EF-Migration (einfache Anzeige, redundanter Stand) oder Lookup des letzten `SyncLog`-Eintrags je Feed (keine Migration, neue Repository-Methode)?
- **Plattform-Scope:** Neben iOS auch MacCatalyst mitbehandeln (gleiche ATS-Beschränkung, eigene `Info.plist`)? Windows/Android sind unbetroffen.
- **Abgrenzung:** Der beobachtete Atom-0.3-Parsefehler bei `rss.golem.de` (`namespace 'http://purl.org/atom/ns#' is not an allowed feed format`) ist ein separates Problem in `System.ServiceModel.Syndication` — Bestätigung, dass es nicht Teil dieses Issues ist und als eigenes Issue geführt wird?
