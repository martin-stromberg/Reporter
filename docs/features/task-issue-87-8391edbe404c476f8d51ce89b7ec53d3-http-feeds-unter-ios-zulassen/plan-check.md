<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

Geprüft: `plan.md` gegen `requirement.md` und `inventory.md` (inkl. Detaildokumente `inventory/models.md`, `logic.md`, `enums.md`, `interfaces.md`, `platform.md`, `ui.md`, `tests.md`). Der Anwender hat alle 8 offenen Punkte mit den empfohlenen Vorschlägen entschieden; der Plan wurde gegen diese bestätigten Entscheidungen geprüft.

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

Die `requirement.md` enthält keinen explizit nummerierten Akzeptanzkriterien-Block; die folgenden Kriterien sind aus der fachlichen Zusammenfassung, dem Implementierungsansatz, den fachlichen Nicht-Anforderungen und den bestätigten Entscheidungen abgeleitet.

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| HTTP-Feeds unter iOS funktionsfähig (ATS-Freigabe für beliebige nutzerdefinierte Domains — Entscheidung #1: `NSAllowsArbitraryLoads`) | `NSAppTransportSecurity` → `NSAllowsArbitraryLoads` = `true` in `Platforms/iOS/Info.plist` (Designentscheidungen, Programmablauf 1, Schritt 1, Konfigurationsänderungen) | Pflicht-E2E-Szenario: `http`-Feed auf iOS-Gerät/Simulator hinzufügen + synchronisieren via `scripts/iOS-Deployment.ps1`, Protokoll in `test-results.md` (Unit-Abdeckung technisch unmöglich, begründet) | Abgedeckt |
| HTTP-Feeds unter MacCatalyst funktionsfähig (Entscheidung #7) | Derselbe plist-Eingriff in `Platforms/MacCatalyst/Info.plist` (Schritt 1, Konfigurationsänderungen) | Kein separater Nachweis geplant — identischer Mechanismus wie iOS, durch die iOS-Verifikation implizit mitabgedeckt (siehe Hinweise) | Abgedeckt |
| Letzte Sync-Fehlerursache je Feed persistiert (Entscheidung #6: Variante A — `Feed.LastError*` + EF-Migration) | Neue Properties `LastErrorKind`/`LastErrorMessage` an Core-`Feed`, `FeedListItem`, Entity `Feed`; Spalten `last_error_kind` (max 50) / `last_error_message` in `ConfigureFeed`; Migration `AddFeedLastError`; `FeedRepository`-Mapping (`MapToEntity`, `MapToModel`, `UpdateAsync`, `GetAllWithDetailsAsync`-Projektion); `FeedSyncService` schreibt Felder im `catch` (Schritte 3–7) | `SyncFeedAsync_Failure_PersistsErrorKindAndMessage`, `UpdateAsync_PersistsLastError`, `GetAllWithDetailsAsync_ProjectsLastError`, `Feed_PersistRoundtrip_LastError` | Abgedeckt |
| Fehlerursache in der UI erreichbar (Entscheidung #4: Action-Sheet-Eintrag „Fehlerdetails" + `DisplayAlert`) | `OnFeedTapped` bietet bei `HealthStatus == Error` zusätzlich `ButtonShowErrorDetails`; `ShowFeedErrorDetailsAsync` öffnet `DisplayAlertAsync` mit `FeedErrorDetailsTitle` + `GetFeedErrorMessage`-Text (Schritt 10, Programmablauf 3) | Pflicht-E2E-Szenarien (manuell): Error-Feed antippen → Eintrag vorhanden → Alert zeigt lokalisierten Text + Rohmeldung | Abgedeckt |
| Verständlicher lokalisierter Fehlertext statt englischer Roh-Exception (Entscheidung #5, mindestens ATS-Fall) | Strukturierte Kategorien via `FeedSyncErrorKind.Classify` (`InsecureHttpBlocked`, `HttpStatus`, `Network`, `Parse`, `Unknown`) + lokalisierte `FeedErrorKind*`-Texte in EN/DE-`AppResources`; Mapping zur Anzeigezeit in `GetFeedErrorMessage` inkl. Fallback `FeedErrorKindUnknown` (Schritte 2, 8, 9) | `FeedSyncErrorKind_Classify_*` (Mapping je Exception-Typ), `GetFeedErrorMessage_MapsKindToLocalizedText`, `_FallsBackToUnknown`; optionaler manueller Sprachwechsel-Check | Abgedeckt |
| Technische Rohmeldung bleibt erreichbar (Entscheidung #5) | `LastErrorMessage` persistiert `"Synchronization failed: {ex.Message}"` (identisch zum `SyncLog`-Text) und wird als zweiter Absatz im Alert angehängt; `SyncLog`-Verlauf und `IDebugLogService`/`ex.ToString()` unverändert | `GetFeedErrorMessage_AppendsTechnicalMessage`; bestehende `SyncLog`-/Debug-Tests bleiben gültig | Abgedeckt |
| Fehlerfelder werden beim nächsten erfolgreichen Sync zurückgesetzt | `UpdateFeedHealthAsync` setzt `LastErrorKind`/`LastErrorMessage` bei `Ok`/`Warning` auf `null` (Programmablauf 2, Punkt 5; Schritt 7) | `SyncFeedAsync_Success_ClearsLastError`; Pflicht-E2E-Szenario Error → OK (Badge wechselt, Eintrag verschwindet) | Abgedeckt |
| Sichtbarkeitsregel: „Fehlerdetails"-Eintrag nur bei `HealthStatus == Error` | Bedingte Action-Sheet-Zusammensetzung in `OnFeedTapped` (Programmablauf 3, Punkt 2; Schritt 10) | Pflicht-E2E-Szenario (manuell): Feed ohne Fehler → Action Sheet ohne Fehlerdetails-Eintrag | Abgedeckt |
| Seiteneffekt: Fehlerursache darf bei Rename/Kategorie/Edit nicht verloren gehen | `ToFeed` reicht `LastErrorKind`/`LastErrorMessage` aus dem `FeedListItem` durch (Schritt 9; als Risiko explizit benannt) | `RenameFeedAsync_PreservesLastError` (Muster `..._PreservesFaviconUrl`) | Abgedeckt |
| Klassifikation unterscheidet Fehlerarten korrekt (ATS/HTTP-Status/Netzwerk/Parse/sonst) | `Classify(ex, feed.Url)`: `HttpRequestException` + `http`-URL → `InsecureHttpBlocked`; mit `StatusCode` → `HttpStatus`; sonst `Network`; `XmlException` → `Parse`; Rest → `Unknown` | `SyncFeedAsync_HttpFeedNetworkFailure_ClassifiedAsInsecureHttpBlocked`, `_HttpStatusError_ClassifiedAsHttpStatus`, `_InvalidXml_ClassifiedAsParse`, `_HttpsFeedNetworkFailure_ClassifiedAsNetwork` | Abgedeckt |
| Bestehende `http`-Feeds funktionieren ohne Datenmigration; Duplikatprüfung bleibt scheme-sensitiv (Entscheidung #2) | Programmablauf 1, Punkt 4 + Seiteneffekte: keine URL-Migration, `GetByUrlAsync` unverändert | Bestehende Duplikat-Tests (`DirectAddCommand_WhenDuplicate_*`) bleiben gültig; durch iOS-E2E-Szenario mitabgedeckt | Abgedeckt |
| `http`-Ergebnisse aus Feed-Suche/Autodiscovery und Favicon-Lookups funktionieren weiter (Entscheidung #3) | Seiteneffekte: gleicher `HttpClient`-Singleton → ATS-Freigabe gilt automatisch für `FeedSearchService`/`FeedIconService`, kein Code-Eingriff | Implizit durch ATS-Verifikation; `FeedIconService`-Fehlerpfade bereits durch Bestandstests isoliert | Abgedeckt |
| Nicht-Anforderung: Atom-0.3-Parsefehler (`rss.golem.de`) ist separates Issue (Entscheidung #8) | Seiteneffekte + Offener Punkt 8: wird lediglich als `Parse`-Kategorie angezeigt, keine `Syndication`-Änderung | `SyncFeedAsync_InvalidXml_ClassifiedAsParse` | Abgedeckt |
| Nicht-Anforderung: keine neue Laufzeit-Einstellung, keine Validierungsänderung | „Validierungsregeln: Keine" + „Keine Änderungen an `appsettings`, `Settings`-Tabelle oder `ISettingsRepository`" | — | Abgedeckt |
| Mobile-UI-Regeln (`AGENTS.md`: Touch-Targets, `AppThemeBinding`, kein verschachteltes Scrolling, Design-Referenz) | Natives Action Sheet/Alert übernimmt Touch-Targets; kein XAML-Eingriff nötig; `AppThemeBinding`-Konvention bei neuen Texten unverändert; Verweis auf `design-draft`-Screens | Manuelle Layout-Verifikation 390 × 844 pt Light + Dark mit Screenshots in `test-results.md`/`mobile-ui-design.md` (Schritt 12) | Abgedeckt |
| Verifikationsumfang (`dotnet test`, statische Checks, manuelle UI-/iOS-Verifikation) | Schritt 12: `dotnet test … --configuration Release`, `.\scripts\Run-StaticChecks.ps1`, UI-Verifikation, `scripts/iOS-Deployment.ps1` | — | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine.

## E2E-Abdeckung

Es existiert keine automatisierte UI-Testinfrastruktur (nachgewiesen in `inventory/tests.md`); `AGENTS.md` lässt für diesen Fall die dokumentierte manuelle Verifikation mit Screenshots als E2E-Nachweis zu. Der Plan begründet das nachvollziehbar und plant konkrete manuelle Szenarien mit auslösender Aktion und sichtbarem Ergebnis — daher hier als „Abgedeckt (manuell)" bewertet.

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| `http`-Feed unter iOS hinzufügen und synchronisieren → Items erscheinen, Health `OK` | Manuell via `scripts/iOS-Deployment.ps1`, Protokoll in `test-results.md` (Pflicht) | Abgedeckt (manuell — ATS-Wirkung nur auf iOS-Ziel prüfbar, nicht per Unit-Test) |
| Feed mit `HealthStatus == Error` antippen → Action Sheet mit „Fehlerdetails" → `DisplayAlert` mit lokalisiertem Text + Rohmeldung | Manuell, 390 × 844 pt, Light + Dark, Screenshots (Pflicht) | Abgedeckt (manuell — `DisplayActionSheet`/`DisplayAlert` nur über Oberfläche verifizierbar) |
| Feed ohne Fehler antippen → Action Sheet **ohne** Fehlerdetails-Eintrag (Sichtbarkeitsregel) | Manuell, 390 × 844 pt (Pflicht) | Abgedeckt (manuell — Bedingung liegt im Code-Behind, nicht in der Testsuite erreichbar) |
| Erfolgreicher Re-Sync → Badge `OK`, Fehlerdetails-Eintrag verschwindet | Manuell (Pflicht) | Abgedeckt (manuell — End-to-End-Übergang Error → OK inkl. UI) |
| Fehlerdetails-Alert auf Deutsch und Englisch (Anzeigezeit-Lokalisierung) | Manuell (Optional) | Abgedeckt (manuell) |
| `http`-Feed unter MacCatalyst | Kein separates Szenario eingeplant — identischer plist-Mechanismus, durch iOS-Verifikation implizit abgedeckt | Abgedeckt (siehe Hinweise) |

## Fehlende oder unvollständige Planbestandteile

Keine.

## Hinweise

- **`InsecureHttpBlocked` ist scheme-basiert, nicht ursachenbasiert:** Jede `HttpRequestException` an eine `http`-URL wird als „von iOS blockiert" klassifiziert — unter Windows/Android trifft der iOS-spezifische `FeedErrorKindInsecureHttpBlocked`-Text auch auf gewöhnliche Verbindungsfehler (DNS, Connection refused) zu. Bei der Umsetzung ggf. den Text plattformneutral formulieren oder die Klassifikation auf das ATS-Fehlerbild einschränken (der Plan benennt das in Programmablauf 2 bereits als „Hinweis auf unverschlüsselte Verbindung", die skizzierten resx-Texte sind jedoch iOS-spezifisch).
- **MacCatalyst-Verifikation nicht explizit eingeplant:** Die plist-Änderung ist identisch zur iOS-Variante; falls ein MacCatalyst-Ziel verfügbar ist, empfiehlt sich eine Mitverifikation beim iOS-Test — ansonsten ist die implizite Abdeckung vertretbar.
- **Sichtbarkeitsbedingung im nicht testbaren Code-Behind:** `feed.HealthStatus == FeedHealth.Error` wird in `OnFeedTapped` (MAUI-Projekt, außerhalb der Testsuite) ausgewertet; Abdeckung erfolgt nur manuell. Das folgt dem bestehenden `OnFeedTapped`-Muster, ist aber ein bewusster Verzicht auf Logikebenen-Abdeckung dieser Regel.
- **Testklassen-Zuordnung offen:** `FeedSyncErrorKind_Classify_*` ist als „`FeedSyncErrorKindTests` (neu) **oder** `FeedSyncServiceTests`" formuliert — bei der Umsetzung festlegen.
- **Kein automatisierter Test der EF-Migration selbst:** Entspricht der Projekt-Konvention (Migrations sind aus der Coverage ausgeschlossen, Tests laufen über `EnsureCreated`); das Spalten-Mapping ist durch `Feed_PersistRoundtrip_LastError` abgedeckt.
- **Offene Punkte bleiben im Plan dokumentiert:** Alle 8 Punkte wurden bestätigt wie vom Plan angenommen; der Abschnitt „Offene Punkte" kann beim Planungscommit als entschieden markiert oder belassen werden — inhaltlich ist der Plan bereits exakt auf die bestätigten Varianten ausgelegt.
