<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Ausgabe der Warnung (Issue #126)

## Übersicht

Für Feeds mit `HealthStatus == FeedHealth.Warning` wird der Warnungsgrund persistiert und in der `FeedDetailPage` einsehbar — in denselben Feldern wie der letzte Sync-Fehler: Die bisherigen `last_error_*`-Spalten werden datenerhaltend zu `last_message_*` verallgemeinert (EF-`RenameColumn`-Migration). Der Aktionsblatt-Eintrag heißt neutral „Meldung anzeigen" (`ButtonShowMessage`) und erscheint bei `Error` und `Warning` gleichermaßen. Betroffen sind die `Feed`-Persistenz (Entity, Domänenmodell, `FeedListItem`, `FeedRepository`, `ReporterDbContext`, Migration + `ReporterDbContextModelSnapshot`), `FeedSyncService` (Ursache klassifizieren und persistieren), `FeedHealthUpdate`, `FeedDetailViewModel`/`FeedDetailPage` sowie die lokalisierten Ressourcen in EN/DE.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Persistenz von Fehler- und Warnungsgrund | **Gemeinsame Felder:** `Feed.LastErrorKind`/`LastErrorMessage` werden zu `LastMessageKind`/`LastMessage` umbenannt; Spalten `last_error_kind`/`last_error_message` zu `last_message_kind`/`last_message` per `RenameColumn`-Migration | Anwender-Entscheidung: Fehler und Warnung identisch persistieren. Umbenennung statt Belassung der `last_error_*`-Namen, damit Schema und Code die gemeinsame Semantik tragen; `RenameColumn` erhält Bestandsdaten (kein `DropColumn`/`AddColumn`). Keine `last_warning_*`-Spalten, kein separates Severity-Feld: `HealthStatus` trägt die Schwere bereits — die Meldungsfelder existieren nur, solange der zugehörige Status steht. |
| Klassifikation der Ursache | `FeedSyncErrorKind` (mit `Classify`) bleibt; neue statische Konstantenklasse `FeedSyncWarningKind` mit `FewerItems` und `NoRecentItems` — beide schreiben ihre disjunkten String-Werte in dasselbe `LastMessageKind` | Kein gemeinsames Kind-Enum nötig: Die Wertemengen sind disjunkt und entstehen an getrennten Stellen (`catch`-Pfad vs. `DetermineStatus`). `FeedSyncErrorKind`/`FeedSyncErrorKindTests` bleiben unverändert; Severity ergibt sich aus `HealthStatus`, nicht aus dem Kind-Typ. |
| Kind→Text-Mapping in der UI | `GetFeedMessage` mappt `feed.LastMessageKind` flach per `switch`: `FeedSyncErrorKind.*` → `FeedErrorKind*`, `FeedSyncWarningKind.*` → `FeedWarningKind*`; `default` wählt per `feed.HealthStatus` zwischen `FeedWarningKindUnknown` (`Warning`) und `FeedErrorKindUnknown` (sonst) | Die disjunkten Kind-Werte machen ein severity-spezifisches Mapping-Feld überflüssig; der Fallback bleibt severity-gerecht, weil `HealthStatus` am `FeedListItem` vorliegt (relevant für Bestands-Feeds mit `Warning`-Status ohne gespeicherte Meldung). |
| Rückgabe aus `DetermineStatus` | Rückgabetyp wird private `sealed record HealthDecision(string Status, string? Kind, string? Message)` | Der bisherige `string`-Rückgabewert kann die Ursache nicht transportieren; ein Record folgt dem etablierten `CollectContext`/`CollectResult`-Muster im selben Service. |
| UI-Einstieg | **Ein einziger** Aktionsblatt-Eintrag `ButtonShowMessage` („Show message" / „Meldung anzeigen") bei `HealthStatus is FeedHealth.Error or FeedHealth.Warning`; `DisplayAlertAsync` mit neutralem Titel `FeedMessageDetailsTitle` („Sync message" / „Synchronisierungsmeldung") | Anwender-Vorgabe: Der Kontextmenüeintrag nennt es nur „Meldung". Die Sichtbarkeitsregel vereinfacht sich (`Error || Warning` statt nur `Error`); der etablierte `DisplayActionSheetAsync`/`DisplayAlertAsync`-Interaktionsweg bleibt unverändert (Mobile-UI-Regel eingehalten). Bestehende Schlüssel `ButtonShowErrorDetails`/`FeedErrorDetailsTitle` werden umbenannt und mit neutralen Werten belegt. |
| `FeedHealthUpdate`-Member | `ErrorKind`/`ErrorMessage` werden zu `MessageKind`/`Message` umbenannt | Neutraler Member-Name passt zur gemeinsamen Semantik; der Record hat nur zwei interne Erzeuger (`RunSyncAsync`, `catch`-Pfad) — Umbenennung ist risikofrei. |
| Technische Detailmeldung | `LastMessage` enthält bei Warnungen eine englische Rohtext-Kennzahl (gelieferte/gespeicherte Artikelzahl bzw. Datum des jüngsten Artikels) und wird wie bisher als zweiter Absatz im Dialog angezeigt | Übernommene Empfehlung (geklärt): konkrete Werte ohne die Lokalisierung der Ursache zu gefährden. |
| Zugang | Nur über `Feed-Aktionen` auf der `FeedDetailPage` | Übernommene Empfehlung (geklärt): identisch zum bisherigen Fehlerdetails-Weg; kein zusätzlicher `TapGestureRecognizer` auf der `FeedsPage`-Karte. |
| Debug-Bericht | `DebugReportService` bleibt unverändert — keine Meldungsfelder im Bericht | Die Anforderung verlangt nur die UI-Einsicht; die Felder stehen auch heute nicht im Bericht — konsistent und kein Mehrumfang. |

## Programmabläufe

### Meldung (Fehler oder Warnung) beim Sync ermitteln und persistieren

1. `RunSyncAsync` ruft `DetermineStatus(newItems, feedItems.Count, existingCount, lastPublishedAt)` auf — neu mit Rückgabewert `HealthDecision` statt `string`.
2. `DetermineStatus` liefert beim Artikel-Einbruch (`fetchedCount < existingCount * 0.5 && existingCount > 0`) `HealthDecision(FeedHealth.Warning, FeedSyncWarningKind.FewerItems, <technische Meldung mit `fetchedCount`/`existingCount`>)`, beim veralteten Feed (`newItems == 0 && lastPublishedAt != default && lastPublishedAt < now − 30 d`) `HealthDecision(FeedHealth.Warning, FeedSyncWarningKind.NoRecentItems, <technische Meldung mit `lastPublishedAt`>)` und sonst `HealthDecision(FeedHealth.Ok, null, null)`.
3. `RunSyncAsync` baut die `SyncLog`-Meldung weiterhin unverändert (`"... Health warning triggered."` bleibt die Rohspur im Log).
4. `RunSyncAsync` ruft `UpdateFeedHealthAsync(feed, decision.Status, new FeedHealthUpdate(resolvedTitle, faviconUrl, MessageKind: decision.Kind, Message: decision.Message))` auf — bei `Ok` sind beide Meldungs-Felder `null` und werden damit geleert; bei `Warning` tragen sie den Warnungskind.
5. `UpdateFeedHealthAsync` schreibt `LastMessageKind = update.MessageKind` und `LastMessage = update.Message` zusätzlich zu den bestehenden Feldern per `IFeedRepository.UpdateAsync`.
6. Der Fehlerpfad in `SyncFeedAsync` (`catch`) bleibt fachlich unverändert: `FeedSyncErrorKind.Classify` liefert den Kind, `new FeedHealthUpdate(MessageKind: errorKind, Message: message)` **überschreibt** einen vorherigen Warnungskind — im gemeinsamen Feld gilt „letzter Status gewinnt" (vorher: getrennte Reset-Logik).
7. `FeedRepository.UpdateAsync` überträgt die umbenannten Eigenschaften auf die Entity; `MapToModel`, `MapToEntity` und die Projektion in `GetAllWithDetailsAsync` führen `LastMessageKind`/`LastMessage` bis zum `FeedListItem` durch.

Beteiligte Klassen/Komponenten: `FeedSyncService`, `FeedSyncWarningKind`, `FeedSyncErrorKind`, `FeedHealthUpdate`, `Feed` (Entity + Domänenmodell), `FeedListItem`, `FeedRepository`, `ReporterDbContext`, `IFeedRepository` (unverändert).

### Meldung in der Feeddetailansicht anzeigen

1. `FeedDetailPage.OnFeedActionsClicked` prüft `feed.HealthStatus is FeedHealth.Error or FeedHealth.Warning` (ersetzt den bisherigen `Error`-nur-Block) und fügt dann `AppResources.ButtonShowMessage` der Aktionsliste hinzu (vor `ButtonDelete`).
2. Der Dispatch-Zweig `action == AppResources.ButtonShowMessage` ruft `ShowFeedMessageAsync(feed)` auf (umbenannt aus `ShowFeedErrorDetailsAsync`).
3. `ShowFeedMessageAsync` zeigt `DisplayAlertAsync` mit Titel `AppResources.FeedMessageDetailsTitle`, Body `_viewModel.GetFeedMessage(feed)` und `AppResources.ButtonOk`.
4. `FeedDetailViewModel.GetFeedMessage` (umbenannt aus `GetFeedErrorMessage`) mappt `feed.LastMessageKind` flach per `switch` auf `FeedErrorKind*` bzw. `FeedWarningKind*` (Fallback je nach `feed.HealthStatus`: `FeedWarningKindUnknown` bei `Warning`, sonst `FeedErrorKindUnknown`) und hängt `feed.LastMessage` bei vorhandenem Wert als zweiten Absatz (`"\n\n"`-getrennt) an.
5. `FeedDetailViewModel.ToFeed` übernimmt `LastMessageKind`/`LastMessage` unverändert aus dem `FeedListItem`, damit Umbenennen, Kategorie-Wechsel und Edit-Sheet die gespeicherte Meldung nicht verwerfen.

Beteiligte Klassen/Komponenten: `FeedDetailPage`, `FeedDetailViewModel`, `FeedListItem`, `AppResources`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `FeedSyncWarningKind` (`src/Reporter.Core/Services/FeedSyncWarningKind.cs`) | Statische Konstantenklasse | Kategorien der Warnungsursache: `FewerItems` = `"FewerItems"` (deutlich weniger gelieferte als gespeicherte Artikel), `NoRecentItems` = `"NoRecentItems"` (keine neuen Artikel, jüngster Artikel >30 Tage alt). Muster: `FeedSyncErrorKind`. Schreibt in dasselbe `LastMessageKind`-Feld wie die Fehlerkinds. |
| `HealthDecision` (private, in `FeedSyncService.cs`) | `sealed record` | Bündelt `Status`, `Kind` und `Message` als Rückgabe von `DetermineStatus`. Muster: `CollectContext`/`CollectResult` im selben Service. |
| `RenameFeedLastErrorToLastMessage` (`src/Reporter.Data/Migrations/`) | EF-Core-Migration | Benennt `feeds.last_error_kind` → `last_message_kind` und `feeds.last_error_message` → `last_message` per `RenameColumn` um (datenerhaltend); aktualisiert `ReporterDbContextModelSnapshot`. |
| `stale-feed.xml` (`src/Reporter.E2ETests/Fixtures/stale-feed.xml`) | RSS-Testfixture | Wenige Items mit festen, weit zurückliegenden `pubDate`-Werten (z. B. 2024) und stabilen `<guid>`-Elementen — der zweite Sync löst die `NoRecentItems`-Warnung aus. Wird unter `/feeds/stale-feed.xml` automatisch vom `StubFeedServer` ausgeliefert (Named-Fixture-Mechanismus) und per `PreserveNewest` ins Test-Output kopiert. |

## Änderungen an bestehenden Klassen

### `Feed` (Entity, `src/Reporter.Data/Entities/Feed.cs`)

- **Umbenannte Eigenschaften:** `LastErrorKind` → `LastMessageKind` (`string?`), `LastErrorMessage` → `LastMessage` (`string?`) — gemeinsame Felder für den Kind (`FeedSyncErrorKind`- oder `FeedSyncWarningKind`-Wert) und die technische Meldung des letzten `Error`- bzw. `Warning`-Status; `null` bei `Ok`.

### `Feed` (Domänenmodell, `src/Reporter.Core/Models/Feed.cs`)

- **Umbenannte Eigenschaften:** `LastErrorKind` → `LastMessageKind` (`string?`, init), `LastErrorMessage` → `LastMessage` (`string?`, init) — Spiegel der Entity-Felder.

### `FeedListItem` (`src/Reporter.Core/Models/FeedListItem.cs`)

- **Umbenannte Eigenschaften:** `LastErrorKind` → `LastMessageKind` (`string?`, init), `LastErrorMessage` → `LastMessage` (`string?`, init) — Transport bis zur UI-Projektion; Basis von `GetFeedMessage`.

### `FeedHealthUpdate` (Record, `src/Reporter.Core/Services/FeedHealthUpdate.cs`)

- **Umbenannte Member:** `ErrorKind` → `MessageKind`, `ErrorMessage` → `Message` (`string?`, Default `null` — `null` = leeren). Positionale Aufrufe (`new FeedHealthUpdate(resolvedTitle, faviconUrl)`) bleiben kompilierbar; der benannte Aufruf im `catch`-Pfad wird auf `MessageKind:`/`Message:` umgestellt.

### `FeedSyncService` (`src/Reporter.Core/Services/FeedSyncService.cs`)

- **Geänderte Methoden:** `DetermineStatus` — Rückgabetyp `string` → `HealthDecision`; die zwei Warnungszweige liefern zusätzlich `FeedSyncWarningKind`-Wert und technische Meldung (Kennzahlen `fetchedCount`/`existingCount` bzw. `lastPublishedAt`), der `Ok`-Zweig `null`-Werte. Schwellen und Bedingungen bleiben unverändert.
- **Geänderte Methoden:** `RunSyncAsync` — nutzt `decision.Status` für Nachricht, `UpdateFeedHealthAsync` und `SyncResult`; übergibt bei `Warning` `Kind`/`Message` in `FeedHealthUpdate` (bei `Ok` `null` → leeren).
- **Geänderte Methoden:** `UpdateFeedHealthAsync` — schreibt `LastMessageKind = update.MessageKind` und `LastMessage = update.Message` (umbenannte Felder) in den `Feed`-Update-Datensatz.
- **Geänderte Methoden:** `SyncFeedAsync` (`catch`-Pfad) — `new FeedHealthUpdate(MessageKind: errorKind, Message: message)` statt `ErrorKind:`/`ErrorMessage:`.
- **Neue Typen:** private `sealed record HealthDecision` (siehe „Neue Klassen").

### `FeedRepository` (`src/Reporter.Data/Repositories/FeedRepository.cs`)

- **Geänderte Methoden:** `UpdateAsync` — überträgt `entity.LastMessageKind`/`entity.LastMessage`; `MapToModel` und `MapToEntity` — mappen die umbenannten Felder in beide Richtungen; `GetAllWithDetailsAsync` — projiziert sie in `FeedListItem`.

### `ReporterDbContext` (`src/Reporter.Data/ReporterDbContext.cs`)

- **Geänderte Methoden:** `ConfigureFeed` — `entity.Property(e => e.LastMessageKind).HasColumnName("last_message_kind").HasMaxLength(50)` und `entity.Property(e => e.LastMessage).HasColumnName("last_message")` (ersetzt das `last_error_*`-Mapping).

### `FeedDetailViewModel` (`src/Reporter.Core/ViewModels/FeedDetailViewModel.cs`)

- **Umbenannte/erweiterte Methoden:** `GetFeedErrorMessage` → `GetFeedMessage(FeedListItem feed)` → `string` — flaches `switch` auf `feed.LastMessageKind`: `FeedSyncErrorKind.*` → `FeedErrorKind*`-Ressourcen (unverändert), `FeedSyncWarningKind.FewerItems`/`NoRecentItems` → `FeedWarningKindFewerItems`/`FeedWarningKindNoRecentItems`; `default` → `feed.HealthStatus == FeedHealth.Warning ? FeedWarningKindUnknown : FeedErrorKindUnknown`; `feed.LastMessage` als zweiter Absatz.
- **Geänderte Methoden:** `ToFeed` — übernimmt `LastMessageKind = feed.LastMessageKind` und `LastMessage = feed.LastMessage` (umbenannt), damit Teil-Updates (`RenameFeedAsync`, `ChangeFeedCategoryAsync`, `SaveEditAsync`) die Meldung erhalten.

### `FeedDetailPage` (Code-Behind, `src/Reporter/Views/FeedDetailPage.xaml.cs`)

- **Geänderte Methoden:** `OnFeedActionsClicked` — Sichtbarkeitsbedingung `feed.HealthStatus is FeedHealth.Error or FeedHealth.Warning` statt `== FeedHealth.Error`; Eintrag `AppResources.ButtonShowMessage` statt `ButtonShowErrorDetails`; Dispatch-Zweig entsprechend.
- **Umbenannte Methoden:** `ShowFeedErrorDetailsAsync` → `ShowFeedMessageAsync(FeedListItem feed)` — `DisplayAlertAsync` mit `FeedMessageDetailsTitle`, `GetFeedMessage(feed)`, `ButtonOk`.
- XAML bleibt unverändert: Das Status-Badge zeigt `Warning` bereits; es kommt kein neues visuelles Element hinzu, nur ein umbenannter, erweitert sichtbarer Aktionsblatt-Eintrag.

### `DemoContentService` (`src/Reporter.Core/Services/DemoContentService.cs`)

- **Geänderte Methoden:** Seed-Block — `LastMessageKind = null` und `LastMessage = null` statt `LastErrorKind`/`LastErrorMessage` (lesbare Vollständigkeit der Defaults).

### `AppResources` (`src/Reporter.Core/Resources/Strings/`)

- `AppResources.resx` (EN) und `AppResources.de.resx` (DE):
  - Schlüssel umbenennen + neue Werte: `ButtonShowErrorDetails` → `ButtonShowMessage` = „Show message" / „Meldung anzeigen"; `FeedErrorDetailsTitle` → `FeedMessageDetailsTitle` = „Sync message" / „Synchronisierungsmeldung".
  - Neue Schlüssel: `FeedWarningKindFewerItems` = „The feed returned far fewer articles than are stored — the source may be incomplete." / „Der Feed liefert deutlich weniger Artikel als gespeichert sind — die Quelle ist möglicherweise unvollständig."; `FeedWarningKindNoRecentItems` = „The feed has not published new articles for more than 30 days — it may be abandoned." / „Der Feed hat seit über 30 Tagen keine neuen Artikel veröffentlicht — er ist möglicherweise verwaist."; `FeedWarningKindUnknown` = „The synchronization reported a warning." / „Die Synchronisation hat eine Warnung gemeldet.".
  - `FeedErrorKind*` bleiben unverändert.
- `AppResources.Designer.cs` — die zwei Properties umbenennen und die drei neuen ergänzen (PublicResXFileCodeGenerator erzeugt sie unter `dotnet build` nicht automatisch — nach Muster `FeedErrorKind*` manuell anpassen oder in Visual Studio regenerieren).

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `RenameFeedLastErrorToLastMessage` | `feeds.last_error_kind` → `last_message_kind` (TEXT, max. 50, nullable), `feeds.last_error_message` → `last_message` (TEXT, nullable) | Datenerhaltende Umbenennung der gemeinsamen Meldungs-Spalten. `Up`: zwei `RenameColumn`; `Down`: zwei `RenameColumn` zurück. Scaffold via `dotnet ef migrations add RenameFeedLastErrorToLastMessage --project src/Reporter.Data`; `ReporterDbContextModelSnapshot` wird dabei aktualisiert. **Verifikation:** Der generierte Code muss `RenameColumn` enthalten — entstehen stattdessen `DropColumn` + `AddColumn` (Datenverlust!), ist die Migration manuell auf `RenameColumn` umzuschreiben. |

## Validierungsregeln

Keine — die Anforderung führt keine neuen Benutzereingaben ein; die Warnungsschwellen bleiben fest codiert.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Umbenennungs-Breite:** `LastError*` → `LastMessage*` betrifft neun Quelldateien (`Entities/Feed`, `Models/Feed`, `FeedListItem`, `FeedHealthUpdate`, `FeedSyncService`, `FeedRepository`, `ReporterDbContext`, `FeedDetailViewModel`, `DemoContentService`), fünf Testdateien, die E2E-Datei `FeedDetailTests.cs`, die drei `AppResources`-Dateien, den `ReporterDbContextModelSnapshot` **und vierzehn Hilfedokumente** unter `docs/help/` mit invalidierten Bezeichnern bzw. Oberflächen-Texten (vollständige Liste in Schritt 11 — darunter `feed-suche-technisch.md`, dessen Abschnitt 9 genau den umbenannten Mechanismus dokumentiert; zusätzlich erhält `mobile-ui-design.md` den neuen Verifikationseintrag). Bestehende Migrationsdateien (`AddFeedLastError` samt Designer-Snapshots) bleiben unangetastet — sie bilden den historischen Schema-Stand.
- **Reset-Semantik vereinfacht sich:** Ein gemeinsames Feldpaar — der letzte `Error`-/`Warning`-Status gewinnt, `Ok` leert. Die frühere Koexistenzfrage (Fehlerdetails einsehbar bei `Warning`?) entfällt: `Warning` überschreibt den Fehlerkind und umgekehrt, exakt wie beim bisherigen Leeren — nur dass der neue Wert statt `null` gespeichert wird.
- **Bestehende Datenbanken:** `RenameColumn` erhält gespeicherte Fehlerkinds/-meldungen — ein Feed im `Error`-Status zeigt nach dem Update unverändert seine Fehlermeldung. Ein Feed im `Warning`-Status aus einer Altversion hat `last_message_kind = NULL` → die UI zeigt den `FeedWarningKindUnknown`-Fallback.
- **In-Memory-Tests:** `TestDbContextFactory` nutzt `EnsureCreated` (Schema aus dem Modell, ohne Migrationen) — die umbenannten Spalten stehen in Tests sofort zur Verfügung.
- **Ressourcen-Umbenennung:** `FeedDetailTests.FeedDetail_ErrorDetails_OnlyForErrorFeed` referenziert `AppResources.ButtonShowErrorDetails` — ohne Umstellung auf `ButtonShowMessage` kompiliert die E2E-Assembly nicht.
- **`SyncAllAsync`/`ScheduledSyncRunner`/`BaseViewModel.RunFeedSyncAsync`:** Unverändert — `Warning` bleibt kein `SyncStatusError`, die Status-Aggregation ändert sich nicht. Der Detailweg funktioniert unabhängig davon, ob der Sync einzeln oder gesamt lief.

## Umsetzungsreihenfolge

1. **`FeedSyncWarningKind` anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: Neue statische Konstantenklasse in `src/Reporter.Core/Services/` mit `FewerItems` und `NoRecentItems` nach Muster `FeedSyncErrorKind`.

2. **Datenmodell verallgemeinern (Umbenennung)**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `LastErrorKind`/`LastErrorMessage` → `LastMessageKind`/`LastMessage` in `Feed` (Entity), `Feed` (Domänenmodell) und `FeedListItem` umbenennen; `FeedHealthUpdate`-Member `ErrorKind`/`ErrorMessage` → `MessageKind`/`Message`; `ReporterDbContext.ConfigureFeed` auf `last_message_kind`/`last_message` umstellen; `FeedRepository` (`UpdateAsync`, `MapToModel`, `MapToEntity`, `GetAllWithDetailsAsync`) auf die neuen Namen umstellen.

3. **EF-Migration scaffolden und verifizieren**
   - Voraussetzungen: Schritt 2; `dotnet-ef`-Tool auf dem Entwicklungsrechner und `Microsoft.EntityFrameworkCore.Design` (bereits Paketreferenz in `Reporter.Data.csproj`).
   - Beschreibung: `dotnet ef migrations add RenameFeedLastErrorToLastMessage --project src/Reporter.Data` ausführen; generierte `Up`/`Down` prüfen — müssen `RenameColumn` enthalten (sonst manuell umschreiben, kein `DropColumn`/`AddColumn`); `ReporterDbContextModelSnapshot` wird automatisch aktualisiert.

4. **`FeedSyncService` erweitern**
   - Voraussetzungen: Schritt 1–3.
   - Beschreibung: Privates Record `HealthDecision` anlegen; `DetermineStatus` auf `HealthDecision`-Rückgabe umstellen (beide Warnungszweige liefern Kind + technische Meldung); `RunSyncAsync` reicht die Felder in `FeedHealthUpdate` durch; `UpdateFeedHealthAsync` schreibt `LastMessage*`; `catch`-Pfad nutzt `MessageKind:`/`Message:`.

5. **`DemoContentService` anpassen**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: Seed-Feed auf `LastMessageKind = null` / `LastMessage = null` umstellen.

6. **Ressourcen EN/DE + Designer**
   - Voraussetzungen: Keine.
   - Beschreibung: `ButtonShowErrorDetails` → `ButtonShowMessage` und `FeedErrorDetailsTitle` → `FeedMessageDetailsTitle` umbenennen (neue neutrale Werte), drei `FeedWarningKind*`-Schlüssel neu — je in `AppResources.resx` und `AppResources.de.resx`; `AppResources.Designer.cs` entsprechend anpassen (manuell nach `FeedErrorKind*`-Muster oder VS-Regenerierung — der Generator läuft unter `dotnet build` nicht).

7. **`FeedDetailViewModel` erweitern**
   - Voraussetzungen: Schritt 2 und 6.
   - Beschreibung: `GetFeedErrorMessage` → `GetFeedMessage` umbenennen und um das `FeedSyncWarningKind`-Mapping sowie den severity-abhängigen Fallback erweitern; `ToFeed` auf die umbenannten Felder umstellen.

8. **`FeedDetailPage` erweitern (UI)**
   - Voraussetzungen: Schritt 6 und 7. UI-Muster ist vorgegeben: `DisplayActionSheetAsync`-Eintrag + `DisplayAlertAsync`-Dialog exakt wie bisher `ButtonShowErrorDetails`/`ShowFeedErrorDetailsAsync` — kein neuer Screen, kein neues UI-Element; das Status-Badge bleibt unverändert.
   - Beschreibung: `ButtonShowMessage`-Eintrag bei `FeedHealth.Error or FeedHealth.Warning` ins Aktionsblatt aufnehmen, Dispatch auf `ShowFeedMessageAsync`. Mobile-UI-Review: Vergleich mit `design-draft/stitch_local_rss_feed_reader/feeds_health_status/screen.png` (Badge unverändert, kein Draft-Eingriff), Prüfung im Handysize-Fenster 390 × 844 pt, Touch-Target des Aktions-Buttons ≥44 × 44 pt bereits gegeben; Verifikation in `docs/help/anwendung/mobile-ui-design.md` bzw. `test-results.md` dokumentieren.

9. **Bestehende Tests anpassen + neue Unit-/Integrationstests**
   - Voraussetzungen: Schritt 4 und 7.
   - Beschreibung: Alle `LastError*`-Assertions/-Testnamen auf `LastMessage*` umstellen und `GetFeedErrorMessage`-Tests auf `GetFeedMessage` umbenennen; neue Tests gemäß „Tests"-Abschnitt (Sync-Persistenz beider Warnungs-Kinds, Leeren bei `Ok`, Überschreiben bei `Error`, Schema-Mapping, ViewModel-Textbildung, `ToFeed`-Preserve, Demo-Seed-Defaults).

10. **E2E-Tests implementieren**
    - Voraussetzungen: Schritt 8; vorhandene E2E-Infrastruktur (`ReporterAppFixture`, `E2EPageHelpers`, `FeedDbAssertions`, `StubFeedServer`, `E2ETestCollection`).
    - Beschreibung: Fixture `stale-feed.xml` anlegen; `FeedDetail_ErrorDetails_OnlyForErrorFeed` → `FeedDetail_Message_OnlyForErrorFeed` auf `ButtonShowMessage` umstellen; `FeedDetail_Message_OnlyForWarningFeed` neu implementieren; Lauf via `.\scripts\Run-E2ETests.ps1` in interaktiver Desktop-Session.

11. **Dokumentation aktualisieren**
    - Voraussetzungen: Schritt 8 und 10.
    - Beschreibung: Neben der fachlichen Neuerung (Warnungsgrund einsehbar) sind alle durch die Umbenennung `LastError*`/`last_error_*` → `LastMessage*`/`last_message_*`, `ButtonShowErrorDetails` → `ButtonShowMessage`, `FeedErrorDetailsTitle` → `FeedMessageDetailsTitle`, `GetFeedErrorMessage` → `GetFeedMessage`, `ShowFeedErrorDetailsAsync` → `ShowFeedMessageAsync` und `FeedDetail_ErrorDetails_OnlyForErrorFeed` → `FeedDetail_Message_OnlyForErrorFeed` invalidierten Hilfedokumente mitzuführen. Grep-verifizierte Liste:
      - `docs/help/anwendung/synchronisation.md` — Abschnitt „Gesundheitsstatus" um Warnungsgrund erweitern; Abschnitt „Fehlerdetails eines Feeds anzeigen" (Zeilen 72, 80–82) auf „Meldung anzeigen"/„Synchronisierungsmeldung" und beide Status umstellen.
      - `docs/help/anwendung/feeddetailansicht.md` (Zeile 46) und `docs/help/anwendung/feed-suche.md` (Zeile 73) — Aktionsblatt-Eintrag heißt „Meldung anzeigen", erscheint bei **Fehler** und **Warnung**, Dialog „Synchronisierungsmeldung".
      - `docs/help/anwendung/beschreibung.md` (Zeile 16) und `docs/help/anwendung/ablauf-anwender.md` (Zeile 52) — dieselbe Umbenennung des Menüeintrags/Dialogs in den Anwenderübersichten; Sichtbarkeit um **Warnung** erweitern.
      - `docs/help/anwendung/feeddetailansicht-technisch.md` — Ablaufbeschreibung (Zeilen 9, 16, 49, 54, 82, 86, 90, 94): `ShowFeedMessageAsync`, `GetFeedMessage`, `ButtonShowMessage`, `FeedMessageDetailsTitle`, `LastMessage*`, Sichtbarkeit `Error or Warning`, neue `FeedWarningKind*`-Schlüssel, umbenannte Testnamen.
      - `docs/help/anwendung/feed-suche-technisch.md` — Abschnitt 9 „Fehlerdetails anzeigen (`FeedDetailPage` → `ShowFeedErrorDetailsAsync`)" komplett überarbeiten (Zeilen 97–113): `FeedHealthUpdate(MessageKind:…)`, `feeds.last_message_*`, `LastMessage*`, `GetFeedMessage`, `ButtonShowMessage`, `FeedMessageDetailsTitle`, Warnungs-Persistenzpfad ergänzen; weitere Referenzen in Zeilen 11, 27, 29, 167, 168 (Regeln im Überblick), 176 (Ressourcenschlüssel-Historie — Issue-#87-Nennung bleibt historisch korrekt, Umbenennung kurz vermerken) und den Testnamen in Zeilen 181–185.
      - `docs/help/anwendung/architektur.md` (Zeilen 64, 66, 94) — `GetFeedMessage`, `Feed.LastMessage*`/`FeedListItem.LastMessage*`, Meldungs- statt Fehlerdetails-Anzeige.
      - `docs/help/anwendung/datenmodell.md` (Zeilen 29, 30, 145) — `LastMessageKind`/`LastMessage`, Spalten `last_message_kind`/`last_message`, Migration `RenameFeedLastErrorToLastMessage` nach `AddFeedLastError` dokumentieren; beide Kinds-Wertemengen (`FeedSyncErrorKind`/`FeedSyncWarningKind`) nennen.
      - `docs/help/einstellungen/troubleshooting.md` (Zeile 42) — Dialog „Synchronisierungsmeldung" (über „Meldung anzeigen") und Spalte `feeds.last_message_kind`.
      - `docs/help/einstellungen/fehlerbehebung-anwender.md` (Zeilen 65, 70) — Eintrag „Meldung anzeigen" und Dialog „Synchronisierungsmeldung".
      - `docs/help/tests/ablauf-technisch.md` (Zeile 60) — Testname `FeedDetail_Message_OnlyForErrorFeed`, Eintrag `ButtonShowMessage`; den neuen `FeedDetail_Message_OnlyForWarningFeed`-Test beschreiben.
      - `docs/help/tests/architektur.md` (Zeilen 13, 20) — `FeedDetailTests`-Beschreibung: „Meldung" statt „Fehlerdetails", Sichtbarkeit `Error or Warning`, neuer Warnungs-Test.
      - `docs/help/tests/beschreibung.md` (Zeile 16) — Kernablauf-Text: „Meldung anzeigen" erscheint bei Fehler **und** Warnung.
      - **Entscheidung `docs/help/anwendung/mobile-ui-design.md`:** Zeilen 168–173 (issue-87) und 175–182 (issue-115) sind historische Verifikations-Notizen und bleiben als Zeitdokumente unverändert stehen; das Ergebnis der neuen Mobile-UI-Verifikation (Schritt 8, 390 × 844 pt) wird als eigener Abschnitt für dieses Issue ergänzt bzw. in `test-results.md` festgehalten.
      - `docs/help/einstellungen/beschreibung.md` (Zeile 33, „Fehlerdetails aus dieser Sitzung" im Debugbericht) referenziert keinen umbenannten Bezeichner — keine Änderung.

12. **Statische Prüfung und Gesamtlauf**
    - Voraussetzungen: Alle vorherigen Schritte.
    - Beschreibung: `.\scripts\Run-StaticChecks.ps1` muss mit Exit-Code 0 ohne Befunde durchlaufen (Format, Lizenzheader, Security, Release-Build mit `TreatWarningsAsErrors`); Unit-Suite und `.\scripts\Run-E2ETests.ps1` ausführen und dokumentierte Bestandsfehler (`DemoSeedTests`, `ArticleImageTests`) als bekannte Ausreißer einordnen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `SyncFeedAsync_FewerItems_PersistsWarningMessage` | `FeedSyncServiceTests` | Artikel-Einbruch schreibt `LastMessageKind = FeedSyncWarningKind.FewerItems` und eine nichtleere `LastMessage` am gespeicherten Feed; `HealthStatus == Warning`. |
| `SyncFeedAsync_NoNewItemsForThirtyDays_PersistsWarningMessage` | `FeedSyncServiceTests` | Veralteter Feed schreibt `LastMessageKind = FeedSyncWarningKind.NoRecentItems` und `LastMessage` am gespeicherten Feed. |
| `SyncFeedAsync_SuccessAfterWarning_ClearsLastMessage` | `FeedSyncServiceTests` | Ein anschließender `Ok`-Sync leert `LastMessageKind`/`LastMessage` wieder. |
| `SyncFeedAsync_ErrorAfterWarning_OverwritesLastMessage` | `FeedSyncServiceTests` | Ein anschließender Fehlschlag ersetzt den Warnungskind durch den `FeedSyncErrorKind`-Wert (gemeinsames Feld: letzter Status gewinnt). |
| `Feed_LastMessage_MappedToExpectedColumns` | `ReporterDbContextTests_Schema` | Spaltenname `last_message_kind`/`last_message` und MaxLength 50 (Muster `DebugLogEntries_MappedToExpectedTable`). |
| `GetFeedMessage_MapsWarningKindToLocalizedText` | `FeedDetailViewModelTests` | `FewerItems`/`NoRecentItems` → jeweilige `FeedWarningKind*`-Ressource. |
| `GetFeedMessage_WarningFallsBackToUnknown` | `FeedDetailViewModelTests` | `HealthStatus == Warning` + `null`/unbekannter Kind → `FeedWarningKindUnknown`. |
| `stale-feed.xml` | — (E2E-Fixture) | RSS-Feed mit Items, deren `pubDate` fest >30 Tage in der Vergangenheit liegt (z. B. 2024) und stabile `<guid>`-Elemente tragen — liefert beim zweiten Sync `newItems == 0` + altem `lastPublishedAt`. |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `FeedSyncServiceTests` — `SyncFeedAsync_Failure_PersistsErrorKindAndMessage`, `SyncFeedAsync_Success_ClearsLastError`, alle `*_ClassifiedAs*`-Tests | `LastErrorKind`/`LastErrorMessage`-Assertions auf `LastMessageKind`/`LastMessage` umstellen; Testnamen sinngemäß anpassen (z. B. `..._ClearsLastMessage`). |
| `FeedRepositoryTests` — `UpdateAsync_PersistsLastError`, `GetAllWithDetailsAsync_ProjectsLastError` | Feldumbenennung in Arrange/Assert; Testnamen → `UpdateAsync_PersistsLastMessage`, `GetAllWithDetailsAsync_ProjectsLastMessage`. |
| `ReporterDbContextTests_Persistence.Feed_PersistRoundtrip_LastError` | Feldumbenennung; Testname → `Feed_PersistRoundtrip_LastMessage`. |
| `FeedDetailViewModelTests` — `GetFeedErrorMessage_MapsKindToLocalizedText`, `..._FallsBackToUnknown`, `..._AppendsTechnicalMessage`, `RenameFeedAsync_PreservesLastError` | Methoden- und Feldumbenennung: `GetFeedErrorMessage_*` → `GetFeedMessage_*`, `RenameFeedAsync_PreservesLastError` → `RenameFeedAsync_PreservesLastMessage`; der bestehende `FallsBackToUnknown`-Test deckt weiterhin den `Error`-Fallback ab, der neue `Warning`-Fallback kommt als separater Test hinzu. |
| `DemoContentServiceTests.EnsureSeededAsync_FirstRun_SeedsExpectedFeedDefaults` | Assertions `LastMessageKind == null` / `LastMessage == null` statt `LastError*`. |
| `FeedSyncServiceTests.SyncFeedAsync_FewerItems_SetsWarning`, `SyncFeedAsync_NoNewItemsForThirtyDays_SetsWarning` | Können um `LastMessageKind`-Assertion erweitert werden — werden durch die neuen Persistenz-Tests abgelöst oder ergänzt. |
| `FeedSyncErrorKindTests` | Keine Anpassung — `FeedSyncErrorKind` bleibt unverändert. |

### E2E-Tests (primärer Funktionsnachweis)

Der Benutzerfluss „Feed-Aktionen → Meldung → Dialog" ist nur über die UI erreichbar und bedarf daher eines E2E-Tests in `src/Reporter.E2ETests` (FlaUI/UIA3, Lauf via `scripts/Run-E2ETests.ps1`).

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | `FeedDetail_Message_OnlyForWarningFeed`: Feed `stale-feed` über die UI anlegen → `SyncAllViaUnreadTab` (1. Sync speichert alte Items, Status `Ok`) → Detailseite öffnen → Aktionsblatt → **Aktualisieren** (2. Sync: `newItems == 0`, `lastPublishedAt` >30 Tage alt → Status `Warning`) → Warten auf Badge `HealthStatusWarningLabel` → Aktionsblatt enthält `ButtonShowMessage` → Eintrag wählen → Alert zeigt `FeedWarningKindNoRecentItems`-Text (plus technischen Absatz) via `ScopedTextContains` → anschließend gesunder Feed bietet den Eintrag nicht | `src/Reporter.E2ETests/FeedDetailTests.cs` (+ neues Fixture `Fixtures/stale-feed.xml`) | Warnungsgrund ist aus der UI heraus einsehbar; der neutrale „Meldung"-Eintrag erscheint bei `Warning`-Status | Der komplette Fluss (Sync → Statuswechsel → Aktionsblatt-Eintrag → Dialogtext) lässt sich nur gegen die laufende App belegen; Unit-Tests decken weder das Aktionsblatt noch den Dialog ab. Spiegelt den bestehenden Fehlerdetails-Test. |
| Pflicht (Anpassung) | `FeedDetail_Message_OnlyForErrorFeed` (umbenannt aus `FeedDetail_ErrorDetails_OnlyForErrorFeed`): gleiches Szenario wie bisher, aber `AppResources.ButtonShowMessage` statt `ButtonShowErrorDetails` — belegt, dass derselbe neutrale Eintrag auch den `Error`-Fall abdeckt und `FeedErrorKindHttpStatus` im Alert erscheint | `src/Reporter.E2ETests/FeedDetailTests.cs` | Fehlerfall weiterhin über den gemeinsamen „Meldung"-Eintrag einsehbar; gesunder Feed ohne Eintrag | Kompilierbarkeit (Ressourcen-Umbenennung) + Nachweis, dass der vereinheitlichte Eintrag beide Status abdeckt. |
| Optional | `FewerItems`-Variante: Feed mit vielen Items syncen, im Edit-Sheet die URL auf ein 1-Item-Fixture umbiegen, erneut aktualisieren → `FeedWarningKindFewerItems` im Alert | `src/Reporter.E2ETests/FeedDetailTests.cs` | Zweiter Warnungsauslöser ebenfalls über UI sichtbar | Nicht zwingend — der zweite Kind ist über `FeedSyncServiceTests`/`FeedDetailViewModelTests` abgesichert; der UI-Weg ist identisch. |

## Offene Punkte

Keine.
