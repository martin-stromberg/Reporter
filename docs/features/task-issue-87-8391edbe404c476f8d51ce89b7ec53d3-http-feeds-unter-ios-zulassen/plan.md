<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: HTTP-Feeds unter iOS zulassen (ATS) und Sync-Fehlerursachen in der UI anzeigen (Issue #87)

## Übersicht

Zwei Teilfeatures: **(1)** Klartext-HTTP-Feeds unter iOS/MacCatalyst funktionsfähig machen — die Apple-ATS-Policy blockiert aktuell alle `http://`-Abrufe des gemeinsamen `HttpClient` (`NSUrlSessionHandler`); geplant ist eine `NSAppTransportSecurity`-Freigabe in beiden `Info.plist`-Dateien. **(2)** Die bereits in `SyncLog.Message` persistierte Sync-Fehlerursache dem Anwender zugänglich machen — als strukturierte Fehlerkategorie plus technische Rohmeldung am `Feed` (neue Spalten, EF-Migration), erreichbar über einen zusätzlichen Eintrag im bestehenden Aktionsmenü der Feed-Karte auf `FeedsPage`. Betroffen sind `src/Reporter/Platforms` (Info.plist), `src/Reporter.Core` (Modelle, `FeedSyncService`, `FeedsViewModel`, `AppResources`), `src/Reporter.Data` (Entity, `ReporterDbContext`, `FeedRepository`, Migration) und `src/Reporter` (`FeedsPage.xaml.cs`).

**Vorbemerkung:** Die Anforderung enthält fachliche Alternativen (ATS-Strategie, Datenquelle der Fehlermeldung, Anzeigeort u. a.). Der Plan ist auf die jeweils empfohlene Variante ausgelegt; alle Alternativen stehen mit Empfehlung im Abschnitt „Offene Punkte" und werden dem Anwender zur Entscheidung vorgelegt.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| ATS-Freigabe | `NSAppTransportSecurity` → `NSAllowsArbitraryLoads` (`true`) in `Platforms/iOS/Info.plist` und `Platforms/MacCatalyst/Info.plist` | Einzige ATS-Option, die beliebige, vom Anwender eingegebene Feed-Domains abdeckt; `NSExceptionDomains` ist eine statische Whitelist und dafür ungeeignet. Ein automatischer HTTPS-Upgrade verändert gespeicherte Nutzer-URLs und bricht Feeds, die nur über `http` erreichbar sind (z. B. lokale Netze). Entspricht dem Issue-Titel „HTTP-Feeds zulassen". |
| Datenquelle der Fehlermeldung | Neue `Feed`-Eigenschaften `LastErrorKind` + `LastErrorMessage` inkl. EF-Migration (Variante A) | Konsistent mit dem bereits denormalisierten `HealthStatus` am Feed; Projektion auf `FeedListItem` ohne Zusatzabfrage; automatisches Zurücksetzen beim nächsten erfolgreichen Sync ist in `UpdateFeedHealthAsync` natürlich abbildbar. Variante B (Lookup des letzten `SyncLog`-Fehlereintrags) bräuchte eine neue Repository-Methode, eine neue ViewModel-Abhängigkeit und müsste Erfolgs-/Fehlereinträge auseinanderhalten. |
| Fehlerdarstellung | Strukturierte Fehlerkategorie (`FeedSyncErrorKind`-Konstanten, persistiert als String) + Rohmeldung; Lokalisierung erfolgt zur Anzeigezeit über `AppResources` | Folgt dem `FeedHealth`-Muster (statische Klasse mit String-Konstanten statt Enum, String-Persistenz). Eine zur Sync-Zeit lokalisiert gespeicherte Meldung würde bei Sprachwechsel veralten; die Kategorie trennt Exception-Erkennung sauber von der Lokalisierung. Die technische Rohmeldung bleibt für Diagnose erhalten (Spalte + `SyncLog` + Debugbericht). |
| Anzeigeort der Fehlerdetails | Zusätzlicher Eintrag „Fehlerdetails" im bestehenden `DisplayActionSheetAsync` von `OnFeedTapped` — nur wenn `HealthStatus == "Error"` —, der ein `DisplayAlertAsync` öffnet | Exakt das vorhandene Interaktionsmuster (`ConfirmDeleteFeedAsync`, `OnSearchResultTapped`); keine Layoutänderung an der Feed-Karte, keine Abweichung vom Design-Entwurf `design-draft/stitch_local_rss_feed_reader/feeds_health_status[_dark_mode]/screen.png`; Touch-Targets übernimmt das native Action Sheet. Ein eigener Sync-Protokoll-Screen wäre ein unverhältnismäßiger Umbau für „letzte Meldung je Feed". |
| `HttpClient`/DI | Unverändert | ATS greift auf `NSUrlSessionHandler`-Ebene global über die `Info.plist`; keine plattformspezifische Handler-Konfiguration in `MauiProgram` nötig. |
| Plattform-Scope | iOS **und** MacCatalyst | Gleicher `HttpClient`-Stack, gleiche ATS-Policy, eigene `Info.plist` — Behandlung ist identisch und nahezu ohne Mehraufwand. |

## Programmabläufe

### HTTP-Feed-Abruf unter iOS/MacCatalyst (ATS-Freigabe)

1. Beim App-Start liest der `NSUrlSessionHandler` (Default-Handler des in `MauiProgram` registrierten `HttpClient`-Singletons) die ATS-Policy aus der `Info.plist` des App-Bundles.
2. `FeedSyncService.RunSyncAsync` ruft `_httpClient.GetStreamAsync(feed.Url, …)` auf — für `http://`-URLs jetzt ohne ATS-Blockierung.
3. Alle weiteren Aufrufer desselben `HttpClient` (`FeedSearchService` — Autodiscovery/Standard-Pfad-Probing, `FeedIconService` — Site-HTML/Icon-Kandidaten) profitieren automatisch von derselben Freigabe; keine Codeänderung.
4. Gespeicherte `http`-Feeds bestehender Installationen funktionieren ohne Datenmigration sofort.

Beteiligte Klassen/Komponenten: `Info.plist` (iOS, MacCatalyst), `MauiProgram`, `FeedSyncService`, `FeedSearchService`, `FeedIconService`

### Fehlerklassifikation und Persistenz beim Sync

1. `FeedSyncService.SyncFeedAsync` lädt den Feed und ruft `RunSyncAsync`.
2. Schlägt der Sync fehl (bestehender `catch`, ohne `OperationCanceledException`), klassifiziert `FeedSyncErrorKind.Classify(ex, feed.Url)` die Exception:
   - `HttpRequestException` bei `http`-Feed-URL → `InsecureHttpBlocked` (deckt den ATS-Fehlertext ab; auf anderen Plattformen ein Hinweis auf unverschlüsselte Verbindung)
   - `HttpRequestException` mit gesetztem `StatusCode` → `HttpStatus`
   - sonstige `HttpRequestException` → `Network`
   - `XmlException` (inkl. Ableitungen beim `SyndicationFeed.Load`) → `Parse`
   - alles andere → `Unknown`
3. `UpdateFeedHealthAsync` schreibt `HealthStatus = Error` sowie `LastErrorKind` = Kategorie und `LastErrorMessage` = technische Rohmeldung (`$"Synchronization failed: {ex.Message}"`, identisch zum `SyncLog`-Text).
4. `UpdateLogAsync` schreibt wie bisher `SyncLog.Status`/`Message` (Verlauf bleibt unverändert); `IDebugLogService` erhält weiterhin `ex.ToString()`.
5. Beim nächsten erfolgreichen Sync ruft `RunSyncAsync` `UpdateFeedHealthAsync` mit `Ok`/`Warning` auf und setzt `LastErrorKind`/`LastErrorMessage` auf `null` zurück.

Beteiligte Klassen/Komponenten: `FeedSyncService`, `FeedSyncErrorKind`, `Feed`, `FeedRepository`, `SyncLog`

### Anzeige der Fehlerdetails (Benutzerfluss)

1. Anwender tippt auf eine Feed-Karte → `FeedsPage.OnFeedTapped` baut die Action-Sheet-Einträge.
2. Hat das `FeedListItem` `HealthStatus == "Error"`, wird zusätzlich `AppResources.ButtonShowErrorDetails` angeboten; sonst bleibt das Menü unverändert.
3. Bei Auswahl öffnet `ShowFeedErrorDetailsAsync` (Code-Behind) ein `DisplayAlertAsync` mit `AppResources.FeedErrorDetailsTitle` und dem vom ViewModel gelieferten Text.
4. `FeedsViewModel.GetFeedErrorMessage(FeedListItem)` mappt `LastErrorKind` auf den lokalisierten `FeedErrorKind*`-Text und hängt — sofern vorhanden — die technische `LastErrorMessage` als zweiten Absatz an; ist `LastErrorKind` leer/unbekannt, fällt der Text auf `FeedErrorKindUnknown` zurück.
5. Nach einem erfolgreichen Sync sind die Felder zurückgesetzt → der Menüeintrag verschwindet wieder mit dem Error-Badge.

Beteiligte Klassen/Komponenten: `FeedsPage.xaml.cs`, `FeedsViewModel`, `FeedListItem`, `AppResources`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `FeedSyncErrorKind` (`src/Reporter.Core/Services/FeedSyncErrorKind.cs`) | statische Klasse | String-Konstanten `InsecureHttpBlocked`, `HttpStatus`, `Network`, `Parse`, `Unknown` (Muster: `FeedHealth`) plus statische Hilfsmethode `Classify(Exception ex, string feedUrl)` → Fehlerkategorie (Muster: `FeedHealth.Changed`) |

## Änderungen an bestehenden Klassen

### `Feed` (Core-Modell, `src/Reporter.Core/Models/Feed.cs`)

- **Neue Eigenschaften:** `LastErrorKind` (`string?`, init) — persistierte Fehlerkategorie (`FeedSyncErrorKind`-Wert); `LastErrorMessage` (`string?`, init) — technische Rohmeldung des letzten Sync-Fehlers

### `FeedListItem` (`src/Reporter.Core/Models/FeedListItem.cs`)

- **Neue Eigenschaften:** `LastErrorKind` (`string?`, init), `LastErrorMessage` (`string?`, init) — für die Fehlerdetail-Anzeige über das Aktionsmenü

### `Feed` (Entity, `src/Reporter.Data/Entities/Feed.cs`)

- **Neue Eigenschaften:** `LastErrorKind` (`string?`), `LastErrorMessage` (`string?`)

### `ReporterDbContext` (`src/Reporter.Data/ReporterDbContext.cs`)

- **Geänderte Methoden:** `ConfigureFeed` — zwei neue Spalten auf `feeds`: `last_error_kind` (`HasMaxLength(50)`, nullable; analog `health_status`) und `last_error_message` (nullable, ohne Längenbegrenzung; analog `sync_logs.message`)

### `FeedRepository` (`src/Reporter.Data/Repositories/FeedRepository.cs`)

- **Geänderte Methoden:** `MapToEntity` und `MapToModel` — neue Felder beidseitig mappen; `UpdateAsync` — neue Felder beim Überschreiben übernehmen; `GetAllWithDetailsAsync` — Projektion um `LastErrorKind`/`LastErrorMessage` auf `FeedListItem` erweitern

### `FeedSyncService` (`src/Reporter.Core/Services/FeedSyncService.cs`)

- **Geänderte Methoden:**
  - `SyncFeedAsync` — im `catch`-Block die Exception via `FeedSyncErrorKind.Classify(ex, feed.Url)` kategorisieren und Kategorie + Rohmeldung an `UpdateFeedHealthAsync` durchreichen
  - `RunSyncAsync` — Erfolgspfad ruft `UpdateFeedHealthAsync` weiterhin mit Status auf; die Fehlerfelder werden dort auf `null` zurückgesetzt
  - `UpdateFeedHealthAsync` — Signatur um `string? errorKind = null, string? errorMessage = null` erweitern; beim Rekonstruieren des `Feed` die neuen Felder setzen (bei `Ok`/`Warning` `null`, bei `Error` die übergebenen Werte)

### `FeedsViewModel` (`src/Reporter.Core/ViewModels/FeedsViewModel.cs`)

- **Neue Methoden:** `GetFeedErrorMessage(FeedListItem feed)` → `string` — mappt `LastErrorKind` via `AppResources.FeedErrorKind*` auf einen lokalisierten Text und hängt `LastErrorMessage` (falls vorhanden) als technischen Detailabsatz an; Fallback `FeedErrorKindUnknown`
- **Geänderte Methoden:** `ToFeed` — die neuen Felder `LastErrorKind`/`LastErrorMessage` aus dem `FeedListItem` durchreichen, damit `RenameFeedAsync`, `ChangeFeedCategoryAsync` und `SaveAsync` die gespeicherte Fehlerursache nicht verwerfen

### `FeedsPage` (Code-Behind, `src/Reporter/Views/FeedsPage.xaml.cs`)

- **Geänderte Methoden:** `OnFeedTapped` — Action-Sheet-Buttons dynamisch zusammensetzen; bei `feed.HealthStatus == FeedHealth.Error` zusätzlich `AppResources.ButtonShowErrorDetails` anbieten und Auswahl an `ShowFeedErrorDetailsAsync` routen
- **Neue Methoden:** `ShowFeedErrorDetailsAsync(FeedsViewModel viewModel, FeedListItem feed)` — `DisplayAlertAsync` mit `FeedErrorDetailsTitle`, `viewModel.GetFeedErrorMessage(feed)`, `ButtonOk` (Muster: `ConfirmDeleteFeedAsync`)

### `AppResources` (`src/Reporter.Core/Resources/Strings/AppResources.resx` + `AppResources.de.resx`)

- **Neue Schlüssel** (Konvention `SyncStatus*`/`HealthStatus*`/`ErrorFeed*`):
  - `FeedErrorDetailsTitle` — Alert-Titel (EN „Sync error" / DE „Synchronisierungsfehler")
  - `ButtonShowErrorDetails` — Action-Sheet-Eintrag (EN „Show error details" / DE „Fehlerdetails anzeigen")
  - `FeedErrorKindInsecureHttpBlocked` — EN „The feed address uses unencrypted HTTP, which iOS blocked by default. Update the feed to HTTPS if possible." / DE „Die Feed-Adresse verwendet unverschlüsseltes HTTP und wurde von iOS blockiert. Stelle den Feed wenn möglich auf HTTPS um."
  - `FeedErrorKindHttpStatus` — Server meldet einen HTTP-Fehler
  - `FeedErrorKindNetwork` — Netzwerk-/Verbindungsfehler
  - `FeedErrorKindParse` — Feed-Format nicht lesbar
  - `FeedErrorKindUnknown` — generischer Fehlertext
- Der generierte `AppResources.Designer.cs` wird durch den Build neu erzeugt

### `Info.plist` iOS (`src/Reporter/Platforms/iOS/Info.plist`)

- **Neuer Schlüssel:** `NSAppTransportSecurity` → `dict` mit `NSAllowsArbitraryLoads` = `true`

### `Info.plist` MacCatalyst (`src/Reporter/Platforms/MacCatalyst/Info.plist`)

- **Neuer Schlüssel:** `NSAppTransportSecurity` → `dict` mit `NSAllowsArbitraryLoads` = `true`

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `AddFeedLastError` (Konvention: `AddFeed*`, Referenz `AddFeedFaviconUrl`; Zeitstempel-Präfix per `dotnet ef migrations add`) | `feeds.last_error_kind` (TEXT/VARCHAR(50), nullable), `feeds.last_error_message` (TEXT, nullable) | Zwei neue Spalten für die letzte Sync-Fehlerursache je Feed; `Migrate()` läuft bereits beim App-Start (`MauiProgram.ApplyPersistedLanguage`) |

## Validierungsregeln

Keine — es kommen keine neuen oder geänderten Benutzereingaben hinzu. Die URL-Validierung (`IsValidFeedUrl`, akzeptiert `http`/`https`) bleibt unverändert.

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `NSAppTransportSecurity` → `NSAllowsArbitraryLoads` in `Platforms/iOS/Info.plist` | plist-Bool | `true` | Klartext-HTTP für den App-weiten `NSUrlSessionHandler`-Stack zulassen |
| `NSAppTransportSecurity` → `NSAllowsArbitraryLoads` in `Platforms/MacCatalyst/Info.plist` | plist-Bool | `true` | Dieselbe Freigabe für MacCatalyst |

Keine Änderungen an `appsettings`, `Settings`-Tabelle oder `ISettingsRepository`.

## Seiteneffekte und Risiken

- **App-Store-Review:** `NSAllowsArbitraryLoads` ist bei Apple-Reviews begründungspflichtig; eine Rejection kann eine Begründung (beliebige nutzerdefinierte Feed-Quellen) verlangen. Risiko, kein Code-Problem.
- **FeedSearchService/FeedIconService:** profitieren automatisch von der ATS-Freigabe (gleicher `HttpClient`); `http`-Suchergebnisse und Favicon-Lookups funktionieren ohne Codeänderung.
- **Redundanz `SyncLog.Message` ↔ `Feed.LastErrorMessage`:** bewusst denormalisiert — `SyncLog` bleibt der Verlauf, `Feed` der aktuelle Stand; Inkonsistenz ist unkritisch, da beide im selben Schreibpfad (`SyncFeedAsync`-catch bzw. `RunSyncAsync`) gesetzt werden.
- **Fehlerverlust bei Partial-Updates:** `FeedsViewModel.ToFeed` rekonstruiert den `Feed` für Rename/Kategorie/Edit — ohne Durchreichen der neuen Felder würde die gespeicherte Fehlerursache stillschweigend gelöscht. Im Plan explizit erfasst.
- **Duplikatprüfung bleibt scheme-sensitiv:** `GetByUrlAsync` (exakter Vergleich) erkennt `http`- vs. `https`-Varianten weiterhin nicht als Duplikat — bestehendes Verhalten, unverändert.
- **Atom-0.3-Parsefehler** (`rss.golem.de`): wird durch die Klassifikation lediglich als `Parse`-Fehler angezeigt; die Fehlerursache in `System.ServiceModel.Syndication` bleibt bestehen (siehe Offene Punkte).
- **Timeout-Verhalten unverändert:** `OperationCanceledException` (inkl. HttpClient-Timeout) wird bewusst nicht als Sync-Fehler protokolliert — bestehender `catch`-Filter bleibt.

## Umsetzungsreihenfolge

1. **ATS-Freigabe in `Info.plist` iOS + MacCatalyst**
   - Voraussetzungen: Keine
   - Beschreibung: `NSAppTransportSecurity`-Dictionary mit `NSAllowsArbitraryLoads` = `true` in beide plist-Dateien einfügen.

2. **`FeedSyncErrorKind` anlegen**
   - Voraussetzungen: Keine
   - Beschreibung: Statische Klasse mit den fünf String-Konstanten und `Classify(Exception, string)` in `src/Reporter.Core/Services/`.

3. **Core-Modelle `Feed` und `FeedListItem` erweitern**
   - Voraussetzungen: Keine
   - Beschreibung: `LastErrorKind`/`LastErrorMessage` (`string?`, init) hinzufügen.

4. **Entity `Feed` und `ReporterDbContext.ConfigureFeed` erweitern**
   - Voraussetzungen: Keine
   - Beschreibung: Entity-Properties und Spaltenmapping `last_error_kind` (max 50) / `last_error_message` ergänzen.

5. **EF-Migration `AddFeedLastError` erstellen**
   - Voraussetzungen: Schritt 4; vorhandene EF-Migrations-Infrastruktur (`src/Reporter.Data/Migrations/`, Referenz `AddFeedFaviconUrl`)
   - Beschreibung: Migration nach `AddFeed*`-Konvention generieren/ablegen.

6. **`FeedRepository`-Mapping und Projektion erweitern**
   - Voraussetzungen: Schritte 3–5
   - Beschreibung: `MapToEntity`, `MapToModel`, `UpdateAsync`, `GetAllWithDetailsAsync`-Projektion um die neuen Felder ergänzen.

7. **`FeedSyncService`: Klassifikation + Persistenz der Fehlerursache**
   - Voraussetzungen: Schritte 2, 3, 6
   - Beschreibung: `UpdateFeedHealthAsync`-Signatur erweitern, `catch`-Block klassifiziert und schreibt Felder, Erfolgspfad setzt sie zurück.

8. **`AppResources`-Schlüssel ergänzen (EN + DE)**
   - Voraussetzungen: Keine
   - Beschreibung: `FeedErrorDetailsTitle`, `ButtonShowErrorDetails`, `FeedErrorKind*` in beide resx-Dateien; Designer-Datei wird beim Build neu generiert.

9. **`FeedsViewModel`: `GetFeedErrorMessage` + `ToFeed`-Durchreichen**
   - Voraussetzungen: Schritte 3, 8
   - Beschreibung: Mapping `LastErrorKind` → lokalisierter Text inkl. technischem Anhang; `ToFeed` reicht die neuen Felder durch.

10. **`FeedsPage.xaml.cs`: Fehlerdetails-Interaktion**
    - Voraussetzungen: Schritte 8, 9
    - Beschreibung: `OnFeedTapped` bietet bei `HealthStatus == "Error"` den Eintrag `ButtonShowErrorDetails`; `ShowFeedErrorDetailsAsync` zeigt `DisplayAlertAsync`. Kein XAML-Eingriff nötig (bestehendes Action-Sheet-Muster, entspricht den Mobile-UI-Regeln).

11. **Unit-/Integrationstests schreiben**
    - Voraussetzungen: Schritte 2–10; vorhandene Testinfrastruktur (`TestDbContextFactory`, `FakeHttpMessageHandler`, `TestFeedXml`, Fakes)
    - Beschreibung: Neue Tests gemäß Abschnitt „Tests".

12. **Verifikation**
    - Voraussetzungen: Schritte 1–11
    - Beschreibung: `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release`, `.\scripts\Run-StaticChecks.ps1`, manuelle UI-Verifikation (390 × 844 pt, Light + Dark, Screenshots in `test-results.md`/`docs/help/anwendung/mobile-ui-design.md`), iOS-Verifikation eines `http`-Feeds via `scripts/iOS-Deployment.ps1`.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `SyncFeedAsync_Failure_PersistsErrorKindAndMessage` | `FeedSyncServiceTests` | Fehlschlag schreibt `Feed.LastErrorKind` + `LastErrorMessage` (über `FeedRepository.GetByIdAsync` prüfbar) |
| `SyncFeedAsync_Success_ClearsLastError` | `FeedSyncServiceTests` | Erfolgreicher Sync setzt zuvor gesetzte Fehlerfelder auf `null` |
| `SyncFeedAsync_HttpFeedNetworkFailure_ClassifiedAsInsecureHttpBlocked` | `FeedSyncServiceTests` | `HttpRequestException` bei `http`-Feed-URL → `InsecureHttpBlocked` |
| `SyncFeedAsync_HttpStatusError_ClassifiedAsHttpStatus` | `FeedSyncServiceTests` | Nicht-2xx-Antwort → `HttpStatus` |
| `SyncFeedAsync_InvalidXml_ClassifiedAsParse` | `FeedSyncServiceTests` | Parse-Fehlschlag → `Parse` (Erweiterung von `..._InvalidXml_SetsError`) |
| `SyncFeedAsync_HttpsFeedNetworkFailure_ClassifiedAsNetwork` | `FeedSyncServiceTests` | `HttpRequestException` bei `https`-URL ohne Statuscode → `Network` |
| `FeedSyncErrorKind_Classify_*` | `FeedSyncErrorKindTests` (neu) oder `FeedSyncServiceTests` | Mapping-Tabelle der `Classify`-Hilfsmethode je Exception-Typ |
| `UpdateAsync_PersistsLastError` / `GetAllWithDetailsAsync_ProjectsLastError` | `FeedRepositoryTests` | Roundtrip und Projektion der neuen Felder (Muster: `..._ProjectsFaviconUrl`) |
| `Feed_PersistRoundtrip_LastError` | `ReporterDbContextTests_Persistence` | Spalten-Mapping/Roundtrip unter `EnsureCreated` |
| `GetFeedErrorMessage_MapsKindToLocalizedText` / `_FallsBackToUnknown` / `_AppendsTechnicalMessage` | `FeedsViewModelTests` | Kind→`AppResources`-Mapping, Fallback, Anhang der Rohmeldung |
| `RenameFeedAsync_PreservesLastError` | `FeedsViewModelTests` | `ToFeed` verwirft die Fehlerfelder nicht (Muster: `..._PreservesFaviconUrl`) |

### Betroffene bestehende Tests

Keine — alle Änderungen sind additiv (optionale Properties, neue Methode, neue plist-Schlüssel). `UpdateFeedHealthAsync` ist privat; `OnFeedTapped` liegt im MAUI-Projekt außerhalb der Testsuite. Die bestehenden Fehlerpfad-Tests (`SyncFeedAsync_Unreachable_KeepsItemsAndLogsError`, `..._InvalidXml_SetsError`) prüfen `HealthStatus`/`SyncLog` und bleiben gültig.

### E2E-Tests (primärer Funktionsnachweis)

Es existiert keine automatisierte UI-Testinfrastruktur (siehe `inventory/tests.md`); die E2E-Abdeckung erfolgt daher als **dokumentierte manuelle Verifikation** gemäß `AGENTS.md` (Screenshots + getestete Fenstergrößen in `test-results.md` bzw. `docs/help/anwendung/mobile-ui-design.md`). ATS/`Info.plist`-Wirkung ist ausschließlich auf einem iOS-Ziel prüfbar.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | `http://`-Feed auf iOS-Gerät/Simulator hinzufügen und synchronisieren → Items erscheinen, Health `OK` | Manuell via `scripts/iOS-Deployment.ps1`, Protokoll in `test-results.md` | HTTP-Feeds unter iOS funktionsfähig | ATS-Blockierung tritt nur im `NSUrlSessionHandler` auf iOS auf — nicht per Unit-Test auf Windows prüfbar |
| Pflicht | Feed mit `HealthStatus == Error` antippen → Action Sheet enthält „Fehlerdetails" → Alert zeigt lokalisierten Text (+ technische Meldung) | Manuell, 390 × 844 pt Light + Dark, Screenshots | Fehlerursache in der UI erreichbar | Benutzerfluss über `DisplayActionSheet`/`DisplayAlert` nur manuell verifizierbar |
| Pflicht | Feed ohne Fehler antippen → Action Sheet enthält **keinen** Fehlerdetails-Eintrag | Manuell, 390 × 844 pt | Sichtbarkeitsregel des Menüeintrags | UI-Bedingung an `HealthStatus`, nur über die Oberfläche nachweisbar |
| Pflicht | Nach erfolgreichem Re-Sync: Badge wechselt auf `OK`, Fehlerdetails-Eintrag verschwindet | Manuell | Zurücksetzen der Fehlerursache bei Erfolg | End-to-end-Übergang Error → OK inkl. UI |
| Optional | Fehlerdetails-Alert auf Deutsch und Englisch prüfen (Sprachwechsel) | Manuell | Lokalisierung der `FeedErrorKind*`-Texte | Anzeigezeit-Lokalisierung sichtbar prüfen |

Bestehende E2E-/UI-Verifikationsnachweise, die angepasst werden müssen: Keine.

## Offene Punkte

| # | Offener Punkt | Empfohlener Vorschlag |
|---|---------------|----------------------|
| 1 | **ATS-Strategie:** HTTP global zulassen (`NSAllowsArbitraryLoads`, App-Store-begründungspflichtig), HTTPS-Upgrade der Feed-URLs, oder Kombination? | `NSAllowsArbitraryLoads` für iOS + MacCatalyst (Plan-Annahme). Nur so funktionieren beliebige `http`-Feed-Domains inkl. reiner HTTP-Quellen (lokale Netze). Kein `NSExceptionDomains` — statische Whitelist ist für nutzerdefinierte Domains ungeeignet. Falls das App-Store-Risiko vermieden werden soll: HTTPS-Upgrade in `RunSyncAsync` nach Fehlschlag + beim Hinzufügen, dann entfällt der plist-Eingriff und der Plan muss angepasst werden. |
| 2 | **Bestehende HTTP-Feeds:** Automatische Migration gespeicherter `http`-URLs auf `https`? Scheme-sensitivität der Duplikatprüfung? | Bei ATS-Freigabe entfällt die Migration — gespeicherte `http`-Feeds funktionieren unverändert. `GetByUrlAsync` bleibt scheme-sensitiv (bestehendes Verhalten). Nur bei Wahl der HTTPS-Upgrade-Variante: scheme-insensitive Duplikatprüfung und Bestandsmigration klären. |
| 3 | **Feed-Suche:** `http`-Ergebnisse aus Verzeichnis/Autodiscovery weiterhin anbieten, hochstufen oder warnen? | Unverändert anbieten — mit ATS-Freigabe funktionieren sie direkt; keine Markierung nötig. |
| 4 | **Ort und Detailgrad der Fehleranzeige:** Badge-Tap, Action-Sheet-Eintrag, Inline-Zeile auf der Karte oder eigener Sync-Protokoll-Screen? | Action-Sheet-Eintrag + `DisplayAlert` mit letzter Meldung (Plan-Annahme) — folgt dem bestehenden `OnFeedTapped`/`ConfirmDeleteFeedAsync`-Muster, kein Karten-Layout-Umbau, kein neuer Screen. Ein Protokoll-Screen nur, wenn ein kompletter `SyncLog`-Verlauf gewünscht ist. |
| 5 | **Meldungsinhalt:** Roh-Exception-Text oder lokalisierte Kategorietexte? Rohmeldung zusätzlich erreichbar? | Lokalisierte `FeedErrorKind*`-Texte als Primärtext, technische `LastErrorMessage` als zweiten Absatz im selben Alert (Plan-Annahme); vollständiger Stack bleibt im Debugbericht (`IDebugLogService`/`IDebugReportService`). |
| 6 | **Datenquelle der Meldung:** `Feed.LastError*` + Migration (Variante A) oder `SyncLog`-Lookup (Variante B)? | Variante A (Plan-Annahme) — Projektion ohne Zusatzabfrage, natürliches Reset bei Erfolg, konsistent zu `HealthStatus`. Variante B vermeidet die Migration, braucht aber neue `ISyncLogRepository`-Methode + ViewModel-Abhängigkeit und muss Erfolgs-/Fehlereinträge filtern. |
| 7 | **Plattform-Scope:** MacCatalyst mitbehandeln? | Ja (Plan-Annahme) — gleiche ATS-Policy, eigene `Info.plist`, identischer Eingriff. |
| 8 | **Abgrenzung Atom-0.3-Parsefehler** (`rss.golem.de`): Teil dieses Issues? | Nein — separates Issue führen. Dieser Plan zeigt den Fehler lediglich als `Parse`-Kategorie in der UI an. |
