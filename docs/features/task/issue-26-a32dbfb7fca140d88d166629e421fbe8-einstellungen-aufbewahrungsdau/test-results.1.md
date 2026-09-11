# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

## E2E-Abdeckung

Alle sechs Pflicht-Szenarien aus `plan.md` sind durch ausgeführte, bestandene Tests abgedeckt. Drei Szenarien laufen unter abweichenden Testnamen (siehe Spalte „Test / Testklasse").

| Szenario (plan.md) | Test / Testklasse | Ergebnis |
|--------------------|-------------------|----------|
| `E2E_ChangeSettings_PersistRoundtrip` | `SettingsViewModelTests_E2E.E2E_ChangeSettings_PersistRoundtrip` | Bestanden |
| `E2E_KeywordAddRemove_Persists` | `SettingsViewModelTests_E2E.E2E_KeywordAddRemove_Persists` | Bestanden |
| `E2E_KeywordDuplicateAndEmpty_Rejected` | `SettingsViewModelTests_E2E.E2E_KeywordDuplicateAndEmpty_Rejected` | Bestanden |
| `E2E_RetentionOutOfRange_Clamped` | `SettingsViewModelTests_Persist.RetentionDays_OutOfRange_Clamped` (Theory: 0 → 1, 400 → 365; persistiert via `SaveRetentionCommand` bis in die DB) | Bestanden |
| `E2E_KeywordFilterCleanup` | `RetentionCleanupServiceTests.CleanupAsync_DeletesKeywordMatchedExpired` + `CleanupAsync_KeywordMatch_KeepsUnreadAndSaved` + `CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt` (Keyword „Werbung"/„Sponsoring" angelegt, gematchte/ungelesene/gespeicherte/ungematchte Artikel geseedet, `CleanupAsync` löscht nur erwartete Artikel) | Bestanden |
| `E2E_AutoRefresh_TicksAndStops` | `AutoRefreshServiceTests.StartAsync_InvokesSyncAfterInterval` + `ApplySettings_Disabled_Stops` + `ApplySettings_ChangesInterval` (`FakeTimeProvider` + `FakeFeedSyncService`) | Bestanden |

## Zusammenfassung

- Gesamt: 128
- Bestanden: 128
- Fehlgeschlagen: 0
- Übersprungen: 0

Ausführung: `dotnet build Reporter.sln --configuration Release` (0 Warnungen, 0 Fehler), danach CI-Befehl `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger trx --logger console`. Dauer Testlauf: ~3 s.

## Testabdeckung

**Abdeckung:** 85,4 % Zeilenabdeckung gesamt (1404/1644 Zeilen; Branch-Abdeckung 74,7 %). Instrumentiert sind `Reporter.Core` und `Reporter.Data` (das Testprojekt referenziert das MAUI-Projekt `src/Reporter` nicht — dessen Code ist grundsätzlich nicht messbar).

Dateien unter 80 % Zeilenabdeckung:

| Datei | Abdeckung |
|-------|-----------|
| `Reporter.Core\Resources\Strings\AppResources.Designer.cs` | 23,6 % (generierte Datei) |
| `Reporter.Core\ViewModels\FeedsViewModel.cs` | 46,9 % |

## Fehlende Tests

Quelle: `Coverage-Daten`

Keine — keine Quelldatei mit ausführbarem Code hat 0 % Abdeckung. Nicht instrumentierte Dateien ohne Abdeckungsdaten sind ausschließlich Interfaces (`Reporter.Core\Interfaces\*.cs`) und die leere abstrakte Klasse `BaseViewModel` ohne ausführbare Zeilen.
