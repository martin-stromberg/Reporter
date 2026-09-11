# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

## E2E-Abdeckung

Geplante Pflicht-Szenarien aus `plan.md` (Abschnitt „E2E-Tests"). Hinweis: Drei Szenarien wurden unter abweichenden Testnamen bzw. auf mehrere Tests aufgeteilt umgesetzt — der beschriebene Fluss ist jeweils vollständig abgedeckt (VM/Service → echte Repositories → SQLite In-Memory).

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Einstellungen öffnen → Werte ändern → sofort persistiert → beim nächsten Laden wieder da | `SettingsViewModelTests_E2E.E2E_ChangeSettings_PersistRoundtrip` | Bestanden |
| Keyword eingeben → Hinzufügen → sichtbar + in DB → Entfernen → weg | `SettingsViewModelTests_E2E.E2E_KeywordAddRemove_Persists` | Bestanden |
| Keyword-Dublette (case-insensitiv) / leere Eingabe → sichtbarer Fehler, kein Datensatz | `SettingsViewModelTests_E2E.E2E_KeywordEmpty_Rejected` + `SettingsViewModelTests_E2E.E2E_KeywordDuplicate_Rejected` (aufgeteilt aus `E2E_KeywordDuplicateAndEmpty_Rejected`) | Bestanden |
| `RetentionDays` außerhalb 1–365 → geclamppt | `SettingsViewModelTests_Persist.RetentionDays_OutOfRange_Clamped` (Theory: 0→1, 400→365; prüft VM→Repository→DB-Roundtrip) | Bestanden |
| Keyword-Filter → Cleanup löscht nur erwartete Artikel (Invarianten intakt) | `RetentionCleanupServiceTests.CleanupAsync_DeletesKeywordMatchedExpired` + `CleanupAsync_KeywordMatch_KeepsUnreadAndSaved` + `CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt` (statt eines einzelnen `E2E_KeywordFilterCleanup`; gemeinsam alle vier geplanten Artikel-Varianten: gematcht/alt/gelesen gelöscht, ungematcht bleibt, ungelesen + `IsSavedForLater` bleiben trotz Match) | Bestanden |
| `AutoRefreshEnabled` + Intervall → `SyncAllAsync` periodisch; Deaktivieren stoppt | `AutoRefreshServiceTests.StartAsync_InvokesSyncAfterInterval` + `ApplySettings_Disabled_Stops` + `ApplySettings_ChangesInterval` (statt `E2E_AutoRefresh_TicksAndStops`; `FakeTimeProvider` + `FakeFeedSyncService`) | Bestanden |

## Zusammenfassung

- Gesamt: 132
- Bestanden: 132
- Fehlgeschlagen: 0
- Übersprungen: 0

Baseline 132 Tests erfüllt (Iteration 2: +4 Tests — 3 Regressionstests + 1 durch Aufteilung von `E2E_KeywordDuplicateAndEmpty_Rejected`).

Ausgeführte Befehle:

- `dotnet build Reporter.sln --configuration Release` → 0 Fehler, 0 Warnungen
- `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger trx --logger "console;verbosity=normal"` → erfolgreich

## Testabdeckung

**Abdeckung:** 85,5 % (2822/3300 Zeilen; Assemblies `Reporter.Core` + `Reporter.Data`; `Reporter.Data.Migrations.*` per `coverlet.runsettings` ausgeschlossen; das MAUI-Projekt `Reporter` ist von der Testsuite nicht referenzierbar und daher nicht instrumentiert)

| Datei | Abdeckung |
|-------|-----------|
| `Reporter.Core\ViewModels\FeedsViewModel.cs` | 46,9 % |
| `Reporter.Core\Resources\Strings\AppResources.Designer.cs` | 23,8 % (generierte Datei — nicht als Testlücke gewertet) |

## Fehlende Tests

Keine — es existiert keine Quelldatei mit 0 % Zeilenabdeckung und keine Quelldatei ohne korrespondierende Testdatei.

Quelle: `Coverage-Daten`
