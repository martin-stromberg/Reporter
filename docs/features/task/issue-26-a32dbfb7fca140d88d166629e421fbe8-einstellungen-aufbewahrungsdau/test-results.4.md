# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

## E2E-Abdeckung

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Einstellungen öffnen → Werte ändern → sofort persistiert → beim nächsten Laden wieder da | `SettingsViewModelTests_E2E.E2E_ChangeSettings_PersistRoundtrip` | Bestanden |
| Keyword eingeben → Hinzufügen → sichtbar + in DB → Entfernen → weg | `SettingsViewModelTests_E2E.E2E_KeywordAddRemove_Persists` | Bestanden |
| Keyword-Dublette (case-insensitiv) / leere Eingabe → sichtbarer Fehler, kein Datensatz | Aufgeteilt: `SettingsViewModelTests_E2E.E2E_KeywordDuplicate_Rejected` + `E2E_KeywordEmpty_Rejected` | Bestanden |
| `RetentionDays` außerhalb 1–365 → geclamppt | Abweichender Name: `SettingsViewModelTests_Persist.RetentionDays_OutOfRange_Clamped` (Theory: 0→1, 400→365; VM→Repo→DB-Roundtrip) | Bestanden |
| Keyword-Filter → `CleanupAsync` löscht nur erwartete Artikel (Invarianten intakt) | Aufgeteilt: `RetentionCleanupServiceTests.CleanupAsync_DeletesKeywordMatchedExpired` (gematcht/alt/gelesen gelöscht, ungematcht/alt/gelesen bleibt) + `CleanupAsync_KeywordMatch_KeepsUnreadAndSaved` (ungelesen + `IsSavedForLater` bleiben trotz Match) + `CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt` | Bestanden |
| `AutoRefreshEnabled` + Intervall → `SyncAllAsync` periodisch; Deaktivieren stoppt | Aufgeteilt: `AutoRefreshServiceTests.StartAsync_InvokesSyncAfterInterval` + `ApplySettings_Disabled_Stops` (ergänzt durch `ApplySettings_ChangesInterval`, `StartStop_RepeatedCycles_RestartsCleanly`) | Bestanden |

Alle 6 Pflicht-Szenarien aus `plan.md` sind abgedeckt; drei davon mit abweichenden/aufgeteilten Testnamen — die inhaltliche Abdeckung ist vollständig gegeben.

## Zusammenfassung

- Gesamt: 144
- Bestanden: 144
- Fehlgeschlagen: 0
- Übersprungen: 0
- Baseline: 144 erwartet (137 + 7 neu) — erreicht

**Hinweis (Flaky Test):** Im ersten CI-Lauf schlug `SettingsViewModelTests_Persist.Persist_QueuedBehindRunningSave_AppliesThemeOnce` einmalig mit `Microsoft.Data.Sqlite.SqliteException : SQLite Error 5: 'unable to delete/modify user-function due to active statements'` fehl. Ursache ist eine Race-Condition in der Testinfrastruktur: `TestDbContextFactory` teilt eine einzelne In-Memory-`SqliteConnection` über alle `DbContext`-Instanzen; der Test führt absichtlich einen blockierten `SaveAsync` parallel zu pollen den `GetAsync`-Aufrufen aus, sodass EFs `CreateFunction`-Registrierung bei der Verbindungsinitialisierung auf aktive Statements trifft. Der Test bestand sowohl im isolierten Wiederholungslauf als auch im vollständigen zweiten Suite-Lauf. Kein Produktfehler, aber potenzieller Verbesserungspunkt (z. B. Serialisierung des parallelen Zugriffs in `TestDbContextFactory` oder dem Test).

## Testabdeckung

**Abdeckung:** 85,4 % Zeilenabdeckung (1422/1665 Zeilen; Branch: 75,6 %) — Quelle: `coverage.cobertura.xml` (coverlet, Release)

| Datei | Abdeckung |
|-------|-----------|
| `Reporter.Core\ViewModels\FeedsViewModel.cs` | 46,9 % |
| `Reporter.Core\Resources\Strings\AppResources.Designer.cs` | 22,9 % (generierte Datei) |

## Fehlende Tests

Keine — es existiert keine Quelldatei mit 0 % Zeilenabdeckung (auf Dateiebene aggregiert); generierte Dateien (`AppResources.Designer.cs`) ausgenommen.

Quelle: `Coverage-Daten`
