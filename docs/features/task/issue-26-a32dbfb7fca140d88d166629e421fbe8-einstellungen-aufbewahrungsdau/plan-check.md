# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Settings-Seite mit allen Optionen (Aufbewahrungsdauer, Keyword-Filter, Auto-Refresh + Intervall, Auto-Gelesen + Verzögerung, Benachrichtigungen + Ruhezeiten, Erscheinungsbild) | Schritte 1, 5, 9, 10, 11; Controls je Sektion in `SettingsPage`-Änderungsliste | `Load_PopulatesAllOptions`, `E2E_ChangeSettings_PersistRoundtrip`, manuelle UI-Verifikation (Schritt 16) | Abgedeckt |
| Sofort-Persistierung aller Optionen via `ISettingsRepository.SaveAsync` | Schritt 10 (`PersistAsync` mit `_isLoading`-Guard), Programmablauf „Einstellung ändern" | `PropertyChange_PersistsImmediately`, `E2E_ChangeSettings_PersistRoundtrip`, `SaveAsync_PersistsNewFields` | Abgedeckt |
| Keyword-Verwaltung (Hinzufügen/Entfernen, Trim, Dubletten-Check, Längenlimit) | Schritt 10 (`AddKeywordCommand`/`RemoveKeywordCommand`), Programmabläufe + Validierungsregeln | `AddKeyword_Valid_AddsToRepository`, `AddKeyword_DuplicateCaseInsensitive_ShowsError`, `AddKeyword_Empty_ShowsError`, `AddKeyword_TooLong_ShowsError`, `RemoveKeyword_DeletesFromRepository`, `E2E_KeywordAddRemove_Persists`, `E2E_KeywordDuplicateAndEmpty_Rejected` | Abgedeckt |
| `RetentionDays`-Validierung 1–365 | Schritte 10, 11 (`Slider` + defensives Clamp in `PersistAsync`) | `RetentionDays_OutOfRange_Clamped`, `E2E_RetentionOutOfRange_Clamped` | Abgedeckt |
| Cleanup-Regel: keyword-gefilterte Artikel nach Fristablauf löschen | Schritte 3, 4, 6; Programmablauf „Retention-Cleanup mit Keyword-Regel" | `CleanupAsync_DeletesKeywordMatchedExpired`, `CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt`, `GetExpiredKeywordCandidates_*`, `DeleteRangeAsync_DeletesOnlyGivenIds`, `E2E_KeywordFilterCleanup` | Abgedeckt |
| Invarianten: niemals ungelesene, niemals `IsSavedForLater`-Artikel löschen | Schritt 6, Kandidaten-Prädikat `IsRead && !IsSavedForLater`, Designentscheidung „Löschregel" | `CleanupAsync_KeywordMatch_KeepsUnreadAndSaved` + bestehende `DeleteExpiredAsync_KeepsUnreadItems`/`KeepsSavedForLaterItems` (laufen unverändert weiter) | Abgedeckt |
| Keyword-Matching case-insensitiv + Teilwort (fixe Semantik) | Schritt 3 (`Contains` + `OrdinalIgnoreCase` auf `Title` + `ContentHtml`) | `KeywordMatcherTests`: `MatchesAny_TitleCaseInsensitive`, `MatchesAny_ContentHtml`, `MatchesAny_Substring`, `MatchesAny_NoMatch` (inkl. Ränder null/leer) | Abgedeckt |
| Persistierung neuer Settings-Felder inkl. EF-Migration | Schritte 1, 2 (`AddSettingsAutoRefreshAndTheme`), 5 (`SaveAsync`/`MapToModel`) | `SaveAsync_PersistsNewFields`; Schema in Tests via `EnsureCreated` abgedeckt | Abgedeckt |
| Zeitgesteuerte Hintergrund-Aktualisierung (`SyncAllAsync`, reagiert auf `AutoRefreshEnabled`/`RefreshIntervalMinutes`) | Schritte 7, 12, 14 (`PeriodicTimer` + `TimeProvider`, Overlap-Guard, Fehlerisolierung, Clamp) | `StartAsync_InvokesSyncAfterInterval`, `ApplySettings_Disabled_Stops`, `ApplySettings_ChangesInterval`, `SyncThrows_LoopContinues`, `OverlappingTick_SkipsSync`, `InvalidInterval_Clamped`, `E2E_AutoRefresh_TicksAndStops` | Abgedeckt |
| Globale Einstellung „Auto-Gelesen beim Öffnen" steuert `ArticleDetailViewModel` | Schritt 13 (`AutoMarkReadMode != "off"`-Gate, `>= 0`-Delay für Option „Sofort") | Kein automatisierter Test möglich (Klasse im nicht referenzierbaren MAUI-Projekt) — manuelle Verifikation in Schritt 16 benannt | Abgedeckt (begründet) |
| Sofortige Übernahme wie Theme (`UserAppTheme`, Auto-Refresh-Rekonfiguration) | Schritte 8, 10, 12 (`IAppThemeService`, `ApplySettingsAsync`) | `ThemeChange_AppliesTheme`, `AutoRefreshChange_AppliesSettings` + manuelle Verifikation | Abgedeckt |
| Benachrichtigungen/Ruhezeiten nur persistierbar (Auswertung im Folge-Paket) | Schritte 10, 11 (`Switch`, `TimePicker` ×2); Scope-Grenze explizit | Load-/Persist-Roundtrip-Tests | Abgedeckt |
| Lade-Fallbacks für ungültige persistierte Werte (Intervall→30, Delay→5, Theme→`"system"`) | Validierungsregeln | `Load_InvalidPersistedValues_UsesFallbacks` | Abgedeckt |
| Nicht-Anforderungen (kein Cache-Button, kein OPML/JSON-Backup, kein Ruhezeiten-Badge, kein periodischer Cleanup, kein `IsKeywordFiltered`-Flag) | Scope-Grenze + Designentscheidungen dokumentiert | n/a | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine — entfällt bei Status `Plan vollständig`. Die drei im ersten Lauf gemeldeten Testlücken sind geschlossen: `AddKeyword_TooLong_ShowsError` (inkl. `ErrorKeywordTooLong`-Key in Schritt 9), `SyncThrows_LoopContinues`/`OverlappingTick_SkipsSync`/`InvalidInterval_Clamped` sowie `Load_InvalidPersistedValues_UsesFallbacks`.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Einstellungen öffnen → Werte ändern → sofort persistiert → beim nächsten Laden wieder da | `SettingsViewModelTests.E2E_ChangeSettings_PersistRoundtrip` (VM→Repo→SQLite In-Memory) | Abgedeckt |
| Keyword eingeben → Hinzufügen → sichtbar + in DB → Entfernen → weg | `SettingsViewModelTests.E2E_KeywordAddRemove_Persists` | Abgedeckt |
| Keyword-Dublette (case-insensitiv) / leere Eingabe → sichtbarer Fehler, kein Datensatz | `SettingsViewModelTests.E2E_KeywordDuplicateAndEmpty_Rejected` | Abgedeckt |
| `RetentionDays` außerhalb 1–365 → geclamppt | `SettingsViewModelTests.E2E_RetentionOutOfRange_Clamped` | Abgedeckt |
| Keyword-Filter → Cleanup löscht nur erwartete Artikel (Invarianten intakt) | `RetentionCleanupServiceTests.E2E_KeywordFilterCleanup` | Abgedeckt |
| Auto-Refresh aktiviert + Intervall → `SyncAllAsync` periodisch; Deaktivieren stoppt | `AutoRefreshServiceTests.E2E_AutoRefresh_TicksAndStops` (`FakeTimeProvider` + `FakeFeedSyncService`) | Abgedeckt |
| Theme-Umschaltung zur Laufzeit sichtbar (AppThemeBinding greift) | Kein automatisierter Test — `UserAppTheme` ist MAUI-API, nicht testbar; dokumentierte manuelle UI-Verifikation in Schritt 16 (Theme-Picker „Dunkel" → UI schaltet um) | Abgedeckt (kein UI-Framework im Repo; Ersatz gemäß AGENTS.md Regel 6 korrekt begründet) |
| Globales Auto-Gelesen-Gate wirkt beim Artikelöffnen | Kein automatisierter Test — `ArticleDetailViewModel` liegt im nicht referenzierbaren MAUI-Projekt; manueller Smoke-Test in Schritt 16 benannt | Abgedeckt (begründet) |
| SettingsPage-Darstellung (Chips, Slider, Dimming, Light/Dark, 44-pt-Touch-Ziele) | Kein automatisierter Test — Design-Abgleich + Screenshot-Doku in Schritt 16 | Abgedeckt (AGENTS.md Regel 6) |

Bewertung des E2E-Ersatzes: Da kein UI-Automatisierungs-Framework im Repo existiert und die Testsuite das MAUI-Projekt nicht referenzieren kann, sind die geplanten Flusstests (ViewModel → echte Repositories → SQLite In-Memory) der letzte automatisierbar erreichbare Punkt. Zusammen mit der in Schritt 16 konkret beschriebenen manuellen UI-Verifikation (390 × 844 pt, Light + Dark, Screenshot-Doku in `test-results.md`/`mobile-ui-design.md`) ist das als E2E-Ersatz tragfähig und im Plan korrekt begründet.

## Fehlende oder unvollständige Planbestandteile

Keine — entfällt bei Status `Plan vollständig`.

## Hinweise

- Alle drei Lücken des ersten Laufs sind geschlossen: (1) `ErrorKeywordTooLong` steht nun im Programmablauf (Zeile 52) und in der `AppResources`-Key-Liste (Zeile 184) inkl. Test `AddKeyword_TooLong_ShowsError`; (2) die `AutoRefreshService`-Schutzverhalten sind mit `SyncThrows_LoopContinues`, `OverlappingTick_SkipsSync` und `InvalidInterval_Clamped` testseitig abgedeckt; (3) die Lade-Fallbacks sind mit `Load_InvalidPersistedValues_UsesFallbacks` abgedeckt.
- Voraussetzungen sind vollständig verankert: `Microsoft.Extensions.TimeProvider.Testing` + handgeschriebene Fakes in Schritt 15, EF-Migration inkl. `dotnet-ef`-Hinweis in Schritt 2, alle `required init`-Bruchstellen (`SettingsRepository.MapToModel`, `ArticleDetailViewModel`-Fallback, `RetentionCleanupServiceTests`, `SettingsRepositoryTests`) sind benannt.
- Alle sechs offenen Fragen der Anforderung sind in der Tabelle „Designentscheidungen" aufgelöst (Cleanup-Zeit-Matching, Match-Felder, `AutoMarkReadMode`-String-Konvention, Intervall-Auswahlliste, Cleanup-Auslöser, Ruhezeiten über Mitternacht).
- Implementierungsdetail (kein Plan-Mangel): `TimeProvider` muss im DI-Container verfügbar sein, wenn `AutoRefreshService` ihn als Pflichtparameter injiziert bekommt — bei optionalem Ctor-Parameter mit Standard `TimeProvider.System` (wie im Plan formuliert) reicht die vorhandene Registrierung; andernfalls ggf. `AddSingleton(TimeProvider.System)` in Schritt 14 ergänzen.
- Für den manuellen Smoke-Test in Schritt 16 ist der Delay-0-Fall („Sofort") bereits explizit aufgenommen worden.
