<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung: Fehlerhafter Atom-Feed (Issue #91)

## Fachliche Zusammenfassung

Der gemeldete Feed `https://www.sportschau.de/index~atom.xml` verwendet den veralteten Atom-0.3-Namespace `http://purl.org/atom/ns#`, den `System.ServiceModel.Syndication` nicht akzeptiert — `SyndicationFeed.Load` unterstützt nur RSS 2.0 und Atom 1.0 (`http://www.w3.org/2005/Atom`). Der Sync schlägt daher in `FeedSyncService.RunSyncAsync` (`src/Reporter.Core/Services/FeedSyncService.cs:159`) mit `System.Xml.XmlException` fehl und wird über `FeedSyncErrorKind.Classify` als `Parse`-Fehler auf `FeedHealth.Error` gesetzt. Anforderung: Das Atom-0.3-Format soll zusätzlich unterstützt werden, sodass der Feed synchronisiert wird und Items wie bei RSS/Atom 1.0 gespeichert werden — ohne Datenmodell-, Interface- oder UI-Änderung.

## Betroffene Klassen und Komponenten

### Bestehende Klassen (Anpassung)

- `src/Reporter.Core/Services/FeedSyncService.cs` — `RunSyncAsync` (Zeilen 156–159): Die Parse-Stelle (`HttpClient.GetStreamAsync` → `XmlReader.Create` → `SyndicationFeed.Load`) ist so zu erweitern, dass Atom-0.3-Dokumente ebenfalls zu einem `SyndicationFeed` aufgelöst werden. Die gesamte nachgelagerte Pipeline (`CollectNewItems`/`NormalizeGuidOrHash`/`GetContentHtml`, `ResolveFeedTitle`, `TryFindFaviconUrlAsync`, `DetermineStatus`, `INotificationService.NotifyNewItemsAsync`, `UpdateFeedHealthAsync`/`UpdateLogAsync`) arbeitet unverändert auf `SyndicationFeed`/`SyndicationItem` und ist nicht anzupassen.
- `src/Reporter.Core/Services/FeedSyncErrorKind.cs` — keine Änderung ableitbar: `XmlException` → `FeedSyncErrorKind.Parse` bleibt für weiterhin nicht lesbare Dokumente korrekt.

### Neue Klassen (Arbeitsnamen, Variante abhängig — siehe Implementierungsansatz)

- `Atom03FeedFormatter` (neu, `src/Reporter.Core/Services/`) — eigener `SyndicationFeedFormatter`, der Atom 0.3 liest und auf `SyndicationFeed`/`SyndicationItem` abbildet; **oder**
- ein normalisierender `XmlReader`-Wrapper (Delegating-Reader), der den Namespace `http://purl.org/atom/ns#` auf `http://www.w3.org/2005/Atom` umbiegt und umbenannte Elemente übersetzt (`tagline`→`subtitle`, `modified`→`updated`, `issued`→`published`, `copyright`→`rights`), sodass `SyndicationFeed.Load` unverändert weiterverwendet werden kann.

### Tests (`src/Reporter.Tests/`)

- `FeedSyncServiceTests` — neuer Test mit einem Atom-0.3-Dokument (Root-Element `feed` im Namespace `http://purl.org/atom/ns#`, typischerweise mit `version="0.3"` und `dc`-Namespace): Erwartung `FeedHealth.Ok`, Items persistiert, `Title`/`Link`/`PublishedAt`/`GuidOrHash` korrekt befüllt, `SyncLog` abgeschlossen. Bestehende RSS-/Atom-1.0-Tests müssen unverändert grün bleiben (kein Verhaltenbruch).
- `TestFeedXml` (`src/Reporter.Tests/TestFeedXml.cs`) — neue Hilfsmethode `Atom03(...)` analog zu `Rss(...)`/`Atom(...)`, inklusive der Atom-0.3-spezifischen Datumselemente (`issued`/`modified`), damit die Datumsabbildung prüfbar ist.
- Ggf. separate Unit-Tests für die neue Formatter-/Reader-Klasse (Format-Erkennung: Atom 0.3 wird erkannt, RSS 2.0/Atom 1.0/ungenügende Dokumente laufen unverändert weiter bzw. in den bestehenden `Parse`-Fehlerpfad).
- `ServiceCollectionTests` — nur falls die Umsetzung eine neue DI-Registrierung erfordert (nicht zu erwarten, siehe Implementierungsansatz).

## Implementierungsansatz

1. **Format-Erkennung/Pufferung:** Der `HttpClient`-Stream ist nicht seekbar — für eine Erkennung vor dem Parse bzw. einen zweiten Parse-Versuch muss das Dokument gepuffert werden (z. B. `GetByteArrayAsync`/`ReadAsStream` in `MemoryStream`, oder `XmlReader`-basierte Root-Element-Prüfung). Erkannt wird Atom 0.3 am Root-Element `feed` im Namespace `http://purl.org/atom/ns#`. Annahme: Pufferung in `RunSyncAsync`, da Dokumente feed-typisch klein sind.
2. **Variante A — eigener `Atom03FeedFormatter`:** `SyndicationFeedFormatter`-Ableitung, die per `CanRead` den Atom-0.3-Namespace erkennt und per `ReadFrom` ein `SyndicationFeed` erzeugt. `RunSyncAsync` wählt anhand des Root-Namespace zwischen `SyndicationFeed.Load` und dem neuen Formatter.
3. **Variante B — normalisierender `XmlReader`:** Delegating-Wrapper um den bestehenden `XmlReader`, der Namespace-URI und Elementnamen on-the-fly auf Atom 1.0 abbildet; `SyndicationFeed.Load` bleibt die einzige Parse-Stelle.
4. **Datums- und Inhaltsabbildung (beide Varianten):** Atom 0.3 verwendet `issued`/`modified`/`created` statt `published`/`updated` sowie `tagline` statt `subtitle`. Ohne Element-Mapping bliebe `SyndicationItem.PublishDate` auf `DateTimeOffset.MinValue` → `Item.PublishedAt = null`, was `DetermineStatus` (30-Tage-Warning über `lastPublishedAt`) und den Fallback-Hash in `NormalizeGuidOrHash` beeinflusst — die Abbildung ist daher fachlich erforderlich. Annahme: `issued`→`PublishDate`, `modified`→`LastUpdatedTime`. Atom-0.3-`link`-Elemente nutzen bereits `href`/`rel`/`type` wie Atom 1.0 (u. a. `alternate`-Site-Link für `TryFindFaviconUrlAsync` und Item-`Link`). `content`/`summary` mit `mode`-Attribut (`escaped`/`xml`/`base64`) mindestens so abbilden, dass `GetContentHtml` weiterhin Text liefert.
5. **Abhängigkeiten/DI:** Rein interne Erweiterung von `Reporter.Core` — keine Interface-Änderung, keine neue DI-Registrierung, `MauiProgram` (`src/Reporter/MauiProgram.cs:72`), `IFeedSyncService`-Aufrufer (`FeedsViewModel`, `UnreadViewModel`, `AutoRefreshService`, `ScheduledSyncRunner`, `IBackgroundRefreshService`-Pfad) unverändert.
6. **Dokumentation:** `docs/help/einstellungen/ablauf-technisch.md` beschreibt den Ingest-Pfad um `SyndicationFeed.Load` — Abschnitt zur Formatunterstützung um Atom 0.3 ergänzen; ggf. Liste unterstützter Formate in den Anwender-Docs (`docs/help/anwendung/synchronisation.md`) präzisieren.

## Konfiguration

- Kein Konfigurationsbedarf aus der Anforderung ableitbar — die Atom-0.3-Unterstützung soll implizit für alle Feeds gelten (Annahme: kein Schalter, keine `Settings`-Erweiterung, keine Migration).
- Fehlerbehandlung bleibt unverändert: Nicht lesbare Dokumente laufen weiterhin in den bestehenden `catch` in `SyncFeedAsync` → `FeedHealth.Error` + `FeedSyncErrorKind.Parse` + `SyncLog` + `IDebugLogService`-Eintrag (`DebugLogCategory.Sync`).

## Offene Fragen

- **Scope der Atom-0.3-Abdeckung:** Soll die volle Atom-0.3-Feature-Teilmenge unterstützt werden (u. a. `content mode="base64"`, `author`-Konstrukte, `dc:`-Erweiterungselemente) oder genügt die vom Sportschau-Feed verwendete Teilmenge (`title`, `link`, `id`, `issued`/`modified`, `summary`/`content`)? Annahme: Teilmenge anhand des realen Feeds plus robuste Defaults.
- **Datumsabbildung:** Ist die Zuordnung `issued`→`PublishDate`/`modified`→`LastUpdatedTime` fachlich korrekt, oder soll bei fehlendem `issued` auf `modified`/`created` zurückgefallen werden?
- **Umsetzungsvariante:** Eigener `Atom03FeedFormatter` (explizit, aber Parse-Logik in Eigenregie) vs. normalisierender `XmlReader`-Wrapper (`SyndicationFeed.Load` bleibt zentral, aber Namespace-/Namensübersetzung als implizite Magie) — interne Designentscheidung, ggf. in der Planung festlegen.
- **Format-Erkennung bei `version`-Attribut:** Atom-0.3-Dokumente tragen üblicherweise `version="0.3"` am Root — soll die Erkennung allein am Namespace hängen (robuster gegenüber fehlendem/abweichendem `version`) oder zusätzlich das Attribut geprüft werden?
