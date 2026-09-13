<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Schlagwortfilter greift nicht — Treffer-Artikel werden weiterhin aufgelistet (Issue #79)

Analysiert wurde der Schlagwortfilter-Bereich (Blacklist aus der `keywords`-Tabelle, `IKeywordMatcher`-Teilwort-Matching) entlang der Anforderung in `requirement.md`: Einspeicherungspfad (`FeedSyncService`), Auswertestellen (`NotificationService`, `RetentionCleanupService`), Listenabfragen (`ItemRepository`), Datenmodell, UI-Verwaltung und Testabdeckung.

## Zusammenfassung

- **Filter-Infrastruktur vollständig vorhanden:** `keywords`-Tabelle (`Keyword`-Entität mit Unique-Index auf `keyword_text`), `IKeywordRepository`/`KeywordRepository`, `IKeywordMatcher`/`KeywordMatcher` (Teilwort, `OrdinalIgnoreCase`, Felder `Title` + `ContentHtml`), Verwaltungs-UI in `SettingsPage.xaml` (Entry + `+ Hinzufügen` + Chip-Liste) und `SettingsViewModel` (`AddKeywordAsync`/`RemoveKeywordAsync`).
- **Der Bug ist im Code bestätigt:** `FeedSyncService.RunSyncAsync` (`src/Reporter.Core/Services/FeedSyncService.cs`, Zeilen 128–215) dedupliziert neue Feed-Items über `knownKeys`/`GuidOrHash` und speichert sie per `AddRangeAsync` — ohne jede Keyword-Prüfung. Der `FeedSyncService`-Konstruktor kennt weder `IKeywordRepository` noch `IKeywordMatcher`.
- **Keywords werden nur an zwei Stellen ausgewertet:** `NotificationService.NotifyNewItemsAsync` (Zeilen 68–76, unterdrückt nur Benachrichtigungen) und `RetentionCleanupService.CleanupAsync` (Zeilen 48–66, löscht nur `IsRead && !IsSavedForLater && (PublishedAt ?? ReadAt) < cutoff`-Kandidaten, ausgelöst einmalig in `App.OnStart`). Ein ungelesener Treffer bleibt daher dauerhaft in allen Listen sichtbar.
- **Lesezugriffe sind ungefiltert:** `ItemRepository.GetUnreadByDateAsync` (beide Overloads), `GetSavedForLaterAsync`, `GetByCategoryAsync` und `GetUnreadCountAsync` enthalten keinen Keyword-Filter; `UnreadViewModel`, `LaterViewModel`, `CategoriesViewModel` zeigen alles Gespeicherte.
- **DI ist vorbereitet:** `IKeywordRepository` und `IKeywordMatcher` sind bereits als Singletons in `MauiProgram` registriert (Zeilen 51, 58) — ein erweiterter `FeedSyncService`-Konstruktor wäre ohne neue Registrierung auflösbar.
- **Dokumentation beschreibt die alte Semantik:** `docs/help/einstellungen/beschreibung.md` („Der Keyword-Filter löscht nur gelesene Artikel"), `business-rules.md` (Lösch-Invarianten, Keyword-Regel), `ablauf-technisch.md` (Abschnitt 4), `troubleshooting.md`, `architektur.md`, `anwendung/aufbewahrung.md`, `benachrichtigungen/business-rules.md` sowie die Resource-Texte `SettingsKeywordInfo` (EN: „Filtered articles are deleted after the retention period…", DE entsprechend) beschreiben durchgehend die Frist-Lösch-Semantik.
- **Kein persistiertes Filter-Flag am `Item`** — ein Treffer ist an den gespeicherten Daten nicht erkennbar; kein Repository-Member liefert Keyword-Treffer ohne `IsRead`-/Frist-Bedingung (relevant für die offene Bestandsdaten-Frage).

**Test-Ausgangszustand:** Beide Testsuiten liefen im Baseline-Lauf vollständig grün — `dotnet test src/Reporter.Tests/Reporter.Tests.csproj`: 350 bestanden / 0 fehlgeschlagen / 0 übersprungen; `npm test` (Release-Skripte, fachlich nicht betroffen): 36/36. Es gibt keine nachgewiesenen Preexisting-Fehler. Zentrale Testlücke: kein Test prüft Keyword-Matching beim Sync-Ingest; `FeedSyncServiceTests.CreateService`/`CreateFailingService` übergeben keine Keyword-Abhängigkeiten, und `ServiceCollectionTests` löst `IFeedSyncService` nicht auf. Nachweis: [inventory/tests.md](inventory/tests.md) inkl. Logs/TRX unter [inventory/test-results/](inventory/test-results/).

## Details

- [Datenmodell](inventory/models.md) — `Keyword`, `Item`, `ItemListItem` (Domäne + EF-Entitäten), `ReporterDbContext`-Konfiguration
- [Logik](inventory/logic.md) — `FeedSyncService`, `KeywordMatcher`, `RetentionCleanupService`, `NotificationService`, `AutoRefreshService`, `SettingsViewModel`, Listen-ViewModels, `App`, `MauiProgram`
- [Enums / Statuswerte](inventory/enums.md) — `FeedHealth`-Konstanten, `SyncResult`-Record
- [Interfaces](inventory/interfaces.md) — `IFeedSyncService`, `IKeywordMatcher`, `IKeywordRepository`, `IItemRepository`, `INotificationService`, `IRetentionCleanupService`
- [Tests](inventory/tests.md) — Test-Ausgangszustand mit Nachweisen, Testklassen und Hilfsmethoden
