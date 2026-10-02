<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Ausgabe der Warnung (Issue #126)

Analysiert wurde der Feed-Sync-/Health-Bereich (`FeedSyncService`, `Feed`-Persistenz, `FeedDetailPage`/`FeedDetailViewModel`) bezogen auf die Anforderung, den Grund des Status `FeedHealth.Warning` für den Anwender einsehbar zu machen.

## Zusammenfassung

- **Fehlerdetails existieren vollständig**, Warnungsdetails fehlen komplett: Für `FeedHealth.Error` ist die Kette `FeedSyncErrorKind.Classify` → `Feed.LastErrorKind`/`LastErrorMessage` (Spalten `last_error_kind`/`last_error_message`, Migration `AddFeedLastError`) → `FeedRepository`-Mapping/Projektion → `FeedDetailViewModel.GetFeedErrorMessage` → Aktionsblatt-Eintrag `ButtonShowErrorDetails` → `DisplayAlertAsync` durchgängig implementiert.
- **Warnungsursache geht verloren:** `FeedSyncService.DetermineStatus` (`src/Reporter.Core/Services/FeedSyncService.cs`, Zeilen 409–423) entscheidet zwischen den zwei Auslösern (Artikel-Einbruch <50 %, veralteter Feed >30 Tage) und `Ok`, liefert aber nur den Statusstring. Auf dem Warnungspfad wird `FeedHealthUpdate` ohne Fehlerfelder erzeugt — es gibt keine `LastWarning*`-Persistenz; die einzige Spur ist `SyncLog.Message` („… Health warning triggered.").
- **UI:** Das Status-Badge zeigt `Warning` bereits auf `FeedsPage` und `FeedDetailPage` (Pill + `HealthStatusWarningLabel`). Der Aktionsblatt-Eintrag `ButtonShowErrorDetails` ist strikt an `FeedHealth.Error` gebunden (`FeedDetailPage.xaml.cs`, Zeilen 116–119).
- **Datenmodell:** `Feed` (Entity/Model), `FeedListItem` und `FeedHealthUpdate` kennen nur die `LastError*`-Felder; `FeedDetailViewModel.ToFeed` und `DemoContentService` müssten neue Felder mitführen. Eine Migration nach dem Muster `AddFeedLastError` fehlt für Warnungen.
- **Konstanten/Lokalisierung:** `FeedHealth.Warning` existiert; ein `FeedSyncWarningKind`-Pendant zu `FeedSyncErrorKind` existiert nicht. `AppResources` (EN/DE/Designer) enthält `FeedErrorKind*`, `FeedErrorDetailsTitle`, `ButtonShowErrorDetails` — keine `FeedWarning*`-Schlüssel.
- **Debug-Bericht:** `DebugReportService` gibt im Abschnitt „Feed health" nur `Status`/Zeitstempel aus — weder Fehler- noch Warnungsdetails.
- **Test-Ausgangszustand:** Unit-Suite `Reporter.Tests` vollständig grün (668/668); E2E-Suite `Reporter.E2ETests` mit 29/31 bestanden und 2 dokumentierten Fehlschlägen (`DemoSeedTests.FirstStart_SeedsNewsCategoryAndDemoFeed`, `ArticleImageTests.BrokenImage_DoesNotFailSync_StoresNoImage` — beide DB-Polling-/Timing-Phänomene). Details und Nachweise: [inventory/tests.md](inventory/tests.md). Für den Warnungsgrund gibt es keine Tests — die bestehenden Warnungstests prüfen nur den Status.

## Details

- [Datenmodell](inventory/models.md) — `Feed` (Entity + Domänenmodell), `FeedListItem`, `SyncLog` (Entity + Domänenmodell), `SyncResult`, `FeedHealthUpdate`
- [Logik](inventory/logic.md) — `FeedSyncService`, `FeedRepository`, `FeedDetailViewModel`, `FeedDetailPage`, `BaseViewModel`, `DebugReportService`, `DemoContentService`, `ScheduledSyncRunner`, `UnreadViewModel`, `FeedsViewModel`
- [Enums und Konstanten](inventory/enums.md) — `FeedHealth`, `FeedSyncErrorKind`, relevante `AppResources`-Schlüssel
- [Interfaces](inventory/interfaces.md) — `IFeedRepository`, `IFeedSyncService`, `ISyncLogRepository`
- [Tests](inventory/tests.md) — Test-Ausgangszustand, Testklassen und Hilfsmethoden
