<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung — Ausgabe der Warnung (Issue #126)

## Ausgangsanforderung

> Tritt bei dem Abruf der Artikel eines Feeds ein Fehler auf, so erhält der Feed den Status "Fehler". Der Fehlertext kann angezeigt werden.
> Bei dem Status "Warnung" allerdings gibt es aktuell keine Möglichkeit einzusehen, weshalb dieser Status gesetzt wurde. Das soll nachgeholt werden.

## Fachliche Zusammenfassung

Für Feeds mit `HealthStatus == FeedHealth.Error` kann der Anwender den Grund des letzten Fehlschlags bereits einsehen (Aktionsblatt-Eintrag `ButtonShowErrorDetails` auf der `FeedDetailPage` → `DisplayAlertAsync` mit `GetFeedErrorMessage`, gespeist aus `Feed.LastErrorKind`/`LastErrorMessage`). Für `HealthStatus == FeedHealth.Warning` existiert kein Pendant: Der Warnungsgrund wird zwar technisch in `SyncLog.Message` protokolliert (`"... Health warning triggered."`, `FeedSyncService.RunSyncAsync`), aber weder auf dem `Feed`-Datensatz persistiert noch in der Oberfläche angezeigt. Die Anforderung verlangt, den Grund des Status `FeedHealth.Warning` für den Anwender einsehbar zu machen — analog zum bestehenden Fehlerdetails-Mechanismus.

Die möglichen Warnungsgründe werden aktuell in `FeedSyncService.DetermineStatus` entschieden und sind genau zwei (Stand des Codes):

1. **Drastisch gesunkene Artikelzahl:** Der Abruf liefert weniger als die Hälfte der bereits gespeicherten Artikel (`fetchedCount < existingCount * 0.5 && existingCount > 0`).
2. **Veralteter Feed:** Es gibt keine neuen Artikel und der jüngste bekannte Artikel ist älter als 30 Tage (`newItems == 0 && lastPublishedAt < now − 30 d`).

## Betroffene Klassen und Komponenten

- **Datenmodell:**
  - `Reporter.Data.Entities.Feed` — voraussichtlich neue Eigenschaft(en) für den letzten Warnungsgrund (z. B. `LastWarningKind`/`LastWarningMessage`, Annahme; Alternativen siehe „Offene Fragen").
  - `Reporter.Core.Models.Feed` und `Reporter.Core.Models.FeedListItem` — entsprechende neue Eigenschaften im Domänenmodell bzw. in der UI-Projektion.
  - `Reporter.Data.Repositories.FeedRepository` — `UpdateAsync`, `MapToModel`, `MapToEntity` und die Projektion in `GetAllWithDetailsAsync` um die neuen Felder erweitern.
  - **EF-Core-Migration** unter `src/Reporter.Data/Migrations/` (Namensmuster `AddFeedLastError` → z. B. `AddFeedLastWarning`) plus aktualisierter `ReporterDbContextModelSnapshot`.
- **Logik / Services:**
  - `FeedSyncService` (`src/Reporter.Core/Services/FeedSyncService.cs`) — `DetermineStatus` liefert derzeit nur den Statusstring; sie muss zusätzlich die konkrete Warnungsursache zurückgeben (z. B. Rückgabe als `record` oder `out`-Parameter), damit `UpdateFeedHealthAsync` sie am Feed persistieren kann.
  - `FeedHealthUpdate` (`src/Reporter.Core/Services/FeedHealthUpdate.cs`) — das Record-Bündel um Warnungs-Felder erweitern (Muster: `ErrorKind`/`ErrorMessage`).
  - Neue Konstanten-/Klassifikationsklasse nach Muster von `FeedSyncErrorKind` (z. B. `FeedSyncWarningKind` mit Werten für beide Auslöser), damit der Grund lokalisiert statt als Rohtext abgelegt wird (Annahme — konsistent mit dem Fehler-Mechanismus).
- **Interfaces:** Keine neuen Interfaces nötig; `IFeedRepository`/`ISyncLogRepository` bleiben unverändert (Annahme: Persistenz wie beim Fehler über `Feed`-Spalten).
- **Enums/Konstanten:** `FeedHealth.Warning` existiert bereits; neu sind ggf. die Warnungs-Kategorien (`FeedSyncWarningKind`).
- **UI-Komponenten:**
  - `FeedDetailPage.xaml.cs` (`OnFeedActionsClicked`) — der Detail-Eintrag im Aktionsblatt ist bisher an `FeedHealth.Error` gebunden; er muss auch bei `FeedHealth.Warning` angeboten werden (entweder derselbe Eintrag `ButtonShowErrorDetails` oder ein eigener Eintrag/Titel für Warnungen — siehe „Offene Fragen").
  - `FeedDetailViewModel` — neue Methode analog `GetFeedErrorMessage` (z. B. `GetFeedWarningMessage`), die `LastWarningKind` auf einen lokalisierten `FeedWarningKind*`-Ressourcentext mappt und optional eine technische Nachricht anhängt.
  - `AppResources.resx` + `AppResources.de.resx` + regenerierter `AppResources.Designer.cs` — neue Schlüssel (z. B. `FeedWarningDetailsTitle`, `ButtonShowWarningDetails` oder Wiederverwendung, `FeedWarningKindFewerItems`, `FeedWarningKindNoRecentItems`, `FeedWarningKindUnknown`).
- **Tests:**
  - `FeedSyncServiceTests` — Warnungsgrund wird korrekt klassifiziert und am Feed persistiert; Reset bei Status `OK`/`Error`.
  - `FeedRepositoryTests` / `ReporterDbContextTests_Schema` — neue Spalten speichern/lesen, Schema-Migration.
  - `FeedDetailViewModelTests` — Textbildung der Warnungsdetails (alle Kind-Werte + Fallback).
  - Ggf. neue `FeedSyncWarningKindTests` nach Muster `FeedSyncErrorKindTests`; `DebugReportServiceTests`, falls der Debug-Bericht die Warnungsfelder aufnehmen soll (optional).
- **Dokumentation (nach Implementierung):** `docs/help/anwendung/synchronisation.md` (Abschnitt „Gesundheitsstatus"), `docs/help/anwendung/feeddetailansicht.md` und `feeddetailansicht-technisch.md`.

## Implementierungsansatz

- **Erweiterungspunkt 1 — Ursachenermittlung:** `FeedSyncService.DetermineStatus` entscheidet bereits zwischen den zwei Warnungsauslösern und `FeedHealth.Ok`. Die Methode wird so erweitert, dass sie neben dem Status die konkrete Ursache (und ggf. Kennzahlen wie `fetchedCount`/`existingCount`) zurückgibt.
- **Erweiterungspunkt 2 — Persistenz:** `UpdateFeedHealthAsync` schreibt heute `FeedHealthUpdate.ErrorKind`/`ErrorMessage` in `Feed.LastErrorKind`/`LastErrorMessage` (auf dem Erfolgs-/Warnungspfad `null` → Felder werden geleert). Derselbe Mechanismus wird für den Warnungsgrund genutzt: `FeedHealthUpdate` erhält zusätzliche Felder, `Feed`-Entity/Modelle/`FeedRepository`-Mapping werden erweitert, plus EF-Migration.
- **Erweiterungspunkt 3 — Anzeige:** Das bestehende Muster `ButtonShowErrorDetails` → `ShowFeedErrorDetailsAsync` → `DisplayAlertAsync` → `ViewModel.GetFeedErrorMessage` wird für `FeedHealth.Warning` gespiegelt. Das Aktionsblatt auf der `FeedDetailPage` ist der etablierte Interaktionsweg (Mobile-UI-Regel: keine Tabellen/Buttons-Reihen — `DisplayActionSheetAsync` entspricht dem Projektstandard). Neue Ressourcentexte in EN und DE.
- **Abhängigkeiten:** `FeedSyncErrorKind` liefert das Klassifikationsmuster; `AppResources.FeedErrorKind*` das Lokalisierungsmuster; `FeedRepository.GetAllWithDetailsAsync` projiziert die Fehlerfelder bereits in `FeedListItem` — dort kommen die Warnungsfelder hinzu. `SyncAllAsync` aggregiert nur `SyncResult`-Status und bleibt unverändert; `BaseViewModel.RunFeedSyncAsync` meldet Warnungen bewusst nicht als `SyncStatusError` — daran ändert das Feature nichts.
- **Keine neue UI-Fläche nötig:** Die Anzeige erfolgt im Dialog wie bei Fehlerdetails; der `SyncLog` enthält weiterhin die technische Rohmeldung.

## Konfiguration

Nicht erforderlich — die Anforderung verlangt kein konfigurierbares Verhalten. Die Warnungsschwellen (50 %-Einbruch der Artikelzahl, 30-Tage-Grenze) sind bestehende, fest codierte Regeln in `FeedSyncService.DetermineStatus` und bleiben unverändert.

## Offene Fragen

1. **Persistenzort des Warnungsgrunds:** Neue Felder auf `Feed` (z. B. `LastWarningKind`/`LastWarningMessage`, konsistent zu `LastErrorKind`/`LastErrorMessage`, erfordert DB-Migration) oder Auslesen des letzten `SyncLog`-Eintrags des Feeds (keine Migration, aber nur technischer englischer Text und abhängig von der Log-Aufbewahrung)? Annahme: Feed-Felder wie beim Fehler.
2. **Koexistenz von Fehler- und Warnungsdetails:** Wechselt ein Feed von `Error` auf `Warning`, bleiben `LastErrorKind`/`LastErrorMessage` aktuell geleert. Soll bei `Warning` zusätzlich der letzte Fehler einsehbar bleiben oder genügt der Warnungsgrund allein?
3. **UI-Einstieg:** Soll bei `Warning` derselbe Aktionsblatt-Eintrag („Fehlerdetails anzeigen"/`ButtonShowErrorDetails`) wiederverwendet werden — mit anderem Dialogtitel/Inhalt — oder ein eigener Eintrag (z. B. „Warnungsdetails anzeigen") mit eigenem Titel (z. B. „Synchronisierungswarnung")? Annahme: eigener Eintrag/eigener Titel für klare Begriffe.
4. **Detailtiefe:** Sollen neben dem lokalisierten Grund auch konkrete Werte angezeigt werden (z. B. „nur noch 12 von 40 Artikeln im Feed", „letzter Artikel vom …"), analog zum zweiten Absatz der technischen Meldung bei Fehlerdetails?
5. **Zugang von der Feed-Liste:** Soll der Warnungsgrund ausschließlich über `Feed-Aktionen` in der `FeedDetailPage` erreichbar sein (wie die Fehlerdetails), oder zusätzlich direkt über Antippen des Status-Badges auf der `FeedsPage`-Karte? Annahme: nur Detailseite, wie beim Fehler.
6. **Lokalisierung:** Neue Ressourcentexte werden in EN (`AppResources.resx`) und DE (`AppResources.de.resx`) benötigt — welche Formulierungen für die zwei Warnungsgründe sind fachlich gewünscht?
