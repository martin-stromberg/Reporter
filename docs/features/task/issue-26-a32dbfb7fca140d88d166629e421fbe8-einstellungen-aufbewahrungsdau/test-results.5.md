# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

## E2E-Abdeckung

Geplante Pflicht-Szenarien aus `plan.md` (Abschnitt „E2E-Tests"). Die Testnamen weichen teilweise vom Plan ab bzw. sind auf mehrere Tests aufgeteilt; die tatsächliche Abdeckung wurde in `src/Reporter.Tests/` verifiziert. Alle Szenarien liefen im Testlauf und sind bestanden.

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Einstellungen öffnen → Werte ändern → sofort persistiert → beim nächsten Laden wieder da | `SettingsViewModelTests_E2E.E2E_ChangeSettings_PersistRoundtrip` (VM → echte Repositories → SQLite In-Memory) | Bestanden |
| Keyword eingeben → Hinzufügen → sichtbar + in DB → Entfernen → weg | `SettingsViewModelTests_E2E.E2E_KeywordAddRemove_Persists` | Bestanden |
| Keyword-Dublette (case-insensitiv) / leere Eingabe → sichtbarer Fehler, kein Datensatz | Aufgeteilt in `SettingsViewModelTests_E2E.E2E_KeywordDuplicate_Rejected` + `SettingsViewModelTests_E2E.E2E_KeywordEmpty_Rejected` | Bestanden |
| `RetentionDays` außerhalb 1–365 → geclamppt, kein ungültiger Wert in DB | `SettingsViewModelTests_Persist.RetentionDays_OutOfRange_Clamped` (Theory: 0 → 1, 400 → 365; VM → echtes Repository → SQLite In-Memory) | Bestanden |
| Keyword-Filter → Cleanup löscht nur erwartete Artikel (Invarianten intakt) | `RetentionCleanupServiceTests.CleanupAsync_DeletesKeywordMatchedExpired` + `CleanupAsync_KeywordMatch_KeepsUnreadAndSaved` + `CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt` (echte Repositories, SQLite In-Memory) | Bestanden |
| Auto-Refresh aktiviert + Intervall → `SyncAllAsync` periodisch; Deaktivieren stoppt | `AutoRefreshServiceTests.StartAsync_InvokesSyncAfterInterval` + `ApplySettings_Disabled_Stops` + `ApplySettings_ChangesInterval` (`FakeTimeProvider` + `FakeFeedSyncService`) | Bestanden |

Nicht automatisierbarer Rest (gemäß Plan begründet, Abdeckung über manuelle UI-Verifikation in Schritt 16): `SettingsPage`-Darstellung, Theme-Umschaltung zur Laufzeit (`UserAppTheme` ist MAUI-API), `AutoMarkReadMode`-Auswertung im `ArticleDetailViewModel` (nicht referenzierbares MAUI-Projekt).

## Zusammenfassung

- Gesamt: 144
- Bestanden: 144
- Fehlgeschlagen: 0
- Übersprungen: 0

Ausgeführt mit: `dotnet build Reporter.sln --configuration Release` (0 Fehler, 0 Warnungen) und `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger trx --logger "console;verbosity=normal"`.

Hinweis: Der bekannte potenziell flaky Test `SettingsViewModelTests_Persist.Persist_QueuedBehindRunningSave_AppliesThemeOnce` ist in diesem Lauf fehlerfrei durchgelaufen; keine Wiederholung nötig.

## Testabdeckung

**Abdeckung:** 85,35 % Zeilen (1422/1666), 75,63 % Branches (360/476) — gemessen über `Reporter.Core` + `Reporter.Data` (Cobertura, `Reporter.Data.Migrations.*` per runsettings ausgeschlossen).

| Datei | Abdeckung |
|-------|-----------|
| `src/Reporter.Core/ViewModels/FeedsViewModel.cs` | 46,9 % (68/145 Zeilen) |

Hinweis: `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs` (22,7 %) liegt ebenfalls unter 80 %, ist aber eine generierte Datei und daher nicht als Abdeckungslücke gewertet. Alle anderen Quelldateien ≥ 80 %.

## Fehlende Tests

Keine — keine Quelldatei mit 0 % Zeilenabdeckung gefunden.
