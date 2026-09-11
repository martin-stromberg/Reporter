# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

## E2E-Abdeckung

Alle sechs Pflicht-Szenarien aus `plan.md` (Abschnitt „E2E-Tests") sind durch tatsächlich ausgeführte und bestandene Tests abgedeckt. Die Testnamen weichen teilweise ab bzw. sind auf mehrere Tests aufgeteilt (siehe Spalte „Test / Testklasse").

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Einstellungen öffnen → Werte ändern → sofort persistiert → beim nächsten Laden wieder da | `SettingsViewModelTests_E2E.E2E_ChangeSettings_PersistRoundtrip` | Bestanden |
| Keyword eingeben → Hinzufügen → sichtbar + in DB → Entfernen → weg | `SettingsViewModelTests_E2E.E2E_KeywordAddRemove_Persists` | Bestanden |
| Keyword-Dublette (case-insensitiv) / leere Eingabe → sichtbarer Fehler, kein Datensatz | `SettingsViewModelTests_E2E.E2E_KeywordEmpty_Rejected` + `SettingsViewModelTests_E2E.E2E_KeywordDuplicate_Rejected` (aufgeteilt) | Bestanden |
| `RetentionDays` außerhalb 1–365 → geclamppt | `SettingsViewModelTests_Persist.RetentionDays_OutOfRange_Clamped` (Theory: 0→1, 400→365; voller Fluss VM→Repo→DB) | Bestanden |
| Keyword-Filter → Cleanup löscht nur erwartete Artikel (Invarianten intakt) | `RetentionCleanupServiceTests.CleanupAsync_DeletesKeywordMatchedExpired` + `CleanupAsync_KeywordMatch_KeepsUnreadAndSaved` + `CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt` (aufgeteilt) | Bestanden |
| Auto-Refresh aktiviert + Intervall → `SyncAllAsync` periodisch; Deaktivieren stoppt | `AutoRefreshServiceTests.StartAsync_InvokesSyncAfterInterval` + `ApplySettings_Disabled_Stops` (+ `ApplySettings_ChangesInterval`; `FakeTimeProvider` + `FakeFeedSyncService`) | Bestanden |

## Zusammenfassung

- Gesamt: 137
- Bestanden: 137
- Fehlgeschlagen: 0
- Übersprungen: 0

Baseline geprüft: 137 Tests erwartet (inkl. der 5 neuen Tests der Fortsetzungsrunde: `QuietHoursEnabled_*` ×3, `Load_WithoutQuietHours_QuietHoursDisabled`, `ThemeOptions_ExposePersistedValues`) — 137 tatsächlich ausgeführt, Abweichung: 0.

Build: `dotnet build Reporter.sln --configuration Release` → erfolgreich, 0 Fehler, 0 Warnungen.

## Testabdeckung

**Abdeckung:** 85,5 % (Zeilenabdeckung gesamt, Coverlet/Cobertura über `Reporter.Core` + `Reporter.Data`; MAUI-Projekt `src/Reporter` nicht im Testprojekt referenziert)

| Datei | Abdeckung |
|-------|-----------|
| `Reporter.Core\ViewModels\FeedsViewModel.cs` | 46,9 % (68/145) |

Hinweis: `Reporter.Core\Resources\Strings\AppResources.Designer.cs` (23,4 %) ist eine generierte Datei und wird gemäß Kommandodefinition ignoriert.

## Fehlende Tests

Quelle: `Coverage-Daten`

- Keine Quelldatei mit 0 % Zeilenabdeckung gefunden (vier 0-%-Klasseneinträge sind nicht ausgeführte Async-State-Machines `DeleteAsync`/`SaveAsync`/`LoadMoreAsync`/`GetByUrlAsync` innerhalb ansonsten abgedeckter Dateien — anteilig in den oben genannten Abdeckungswerten enthalten).
- `Reporter.Core\ViewModels\FeedsViewModel.cs` — 46,9 % Zeilenabdeckung (< 80 %); insbesondere die Methoden `SaveAsync` und `DeleteAsync` sind ungetestet.
