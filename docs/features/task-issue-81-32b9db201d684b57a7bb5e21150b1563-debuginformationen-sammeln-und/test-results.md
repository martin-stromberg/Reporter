<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ergebnisse

## Ergebnis

**Status:** Fehler vorhanden

Alle automatisierten Tests (461/461) sind im Release-Modus bestanden, und alle automatisierbaren Pflicht-E2E-Szenarien existieren und laufen erfolgreich — einschließlich der in Iteration 3 ergänzten Session-Reset-Tests (`BeginSessionAsync_ResetsPreviousEntries_KeepsErrors`, `DeleteAllExceptErrorsAsync_RemovesNonErrorEntries`). Die manuelle UI-Verifikation (Abschnitt „Diagnose & Support" auf 390 × 844 pt, Light + Dark) ist erfolgt und bestanden — dokumentiert in `docs/help/anwendung/mobile-ui-design.md` (Abschnitt „Diagnose & Support (issue-81)") mit Screenshots unter `test-results/issue-81/manual-*.png`. Nicht ausgeführt sind nur der Versand mit einem echten registrierten Mail-Client (auf dem Prüf-PC ist kein Mail-Client eingerichtet) und die iOS-Verifikation (Umgebungslimitation, macOS erforderlich); beide sind unten als nicht ausgeführte Tests dokumentiert.

## Fehlgeschlagene Tests

### Manuelle Verifikation (plan.md, E2E-Tabelle „Pflicht (manuell)", Schritt 14)

- **Manuell: Mail-Client zeigt vorbefüllten Entwurf (Empfänger, Betreff, Plain-Text-Body inkl. Session-Log-Sektion) mit echtem registriertem Mail-Client** — Nicht ausgeführt: auf dem Prüf-PC ist kein Mail-Client registriert (Umgebungslimitation); Betreff/Empfänger/Body sind über `DebugReportServiceTests`/`DebugReportTests_E2E` automatisiert abgedeckt.
- **Manuell: iOS-Verifikation (Versand und Layout auf `net10.0-ios`)** — Nicht ausgeführt: erfordert macOS/iOS-Simulator (`scripts/iOS-Deployment.ps1`), in dieser Umgebung nicht verfügbar.

## E2E-Abdeckung

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Schalter „Debuginformationen sammeln" aktivieren → persistiert, übersteht Reload, schaltet Protokollierung zur Laufzeit | `DebugReportTests_E2E.DebugCollection_PersistedAcrossSessions` (entspricht geplantem `E2E_DebugCollection_PersistRoundtrip`), `SettingsViewModelTests_Debug.DebugCollectionEnabled_Toggle_PersistsAndSwitchesLog` (entspricht `DebugCollectionEnabled_Change_CallsSetEnabled`), `Load_ReadsPersistedDebugSwitch`, `Load_PersistedDebugSwitch_DoesNotCallSetEnabled` | Bestanden |
| Versand-Aktion → Report gesammelt (Settings, Feeds, SyncLog, Session-Log, Geräteinfo) und an `IEmailService.ComposeAsync` übergeben | `DebugReportTests_E2E.Report_ComposesThroughRealServicesAndSqlite` (entspricht geplantem `E2E_SendDebugReport_ComposesCollectedReport`), `SettingsViewModelTests_Debug.SendDebugReport_Enabled_ComposesEmail` | Bestanden |
| Session-Log: App-Start-Reset unter Erhalt der `Error`-Einträge, Schreiben nur bei aktiviertem Schalter | `DebugReportTests_E2E.SessionLog_WritesAndResetsAcrossSessions` (entspricht geplantem `E2E_DebugSessionLog_WriteAndReset`), `DebugLogServiceTests.BeginSessionAsync_ResetsPreviousEntries_KeepsErrors`, `BeginSessionAsync_LoadsEnabledState`, `BeginSessionAsync_Enabled_WritesStartEntry`, `LogAsync_Disabled_WritesNothing`, `LogAsync_Enabled_WritesEntry`, `DebugLogRepositoryTests.DeleteAllExceptErrorsAsync_RemovesNonErrorEntries` | Bestanden |
| Versand ohne Mail-Client / mit unerwartetem Fehler → sichtbarer Fehlerhinweis (`DebugReportFailed`) | `SettingsViewModelTests_Debug.SendDebugReport_ComposeFalse_RaisesDebugReportFailed` (entspricht `SendDebugReport_Unsupported_RaisesDebugReportFailed`), `SendDebugReport_ComposeThrows_RaisesDebugReportFailed_AndLogs` (entspricht `SendDebugReport_ServiceThrows_RaisesDebugReportFailedAndLogs`), `DebugSendEnabled_RequiresCollectionAndEmailSupport` (`DebugEmailSupported == false`), `DebugReportServiceTests.SendReportAsync_Unsupported_ReturnsFalse_AndDoesNotCompose`, `SendReportAsync_ComposeReturnsFalse_LogsErrorEntry` | Bestanden |
| Deaktivierte Senden-Aktion bei ausgeschalteter Sammlung (Guard gegen programmatische Auslösung) | `SettingsViewModelTests_Debug.SendDebugReport_Disabled_DoesNotCompose` (entspricht `SendDebugReport_CollectionDisabled_DoesNotSend`), `WithoutDebugServices_SendIsDisabledAndToggleStillPersists` | Bestanden |
| Pflicht (manuell): Abschnitt „Diagnose & Support" 390 × 844 pt Light + Dark, Touch-Ziele ≥ 44 pt, `AppThemeBinding`; Opt-in-Toggle persistiert und schreibt Übergangseintrag; Session-Reset nach App-Neustart | Manuelle Laufzeit-Verifikation am Windows-Handy-Fenster 390 × 844 pt, dokumentiert in `docs/help/anwendung/mobile-ui-design.md` Abschnitt „Diagnose & Support (issue-81)", Screenshots `test-results/issue-81/manual-*.png` | Bestanden |
| Pflicht (manuell): Mail-Client zeigt vorbefüllten Entwurf mit echtem registriertem Mail-Client | Manuelle Verifikation auf Gerät/PC mit eingerichtetem Mail-Client (plan.md Schritt 14) | Nicht ausgeführt |
| Pflicht (manuell): iOS-Verifikation (`net10.0-ios`) | `scripts/iOS-Deployment.ps1` auf macOS (plan.md Schritt 14) | Nicht ausgeführt |

## Zusammenfassung

- Gesamt: 461
- Bestanden: 461
- Fehlgeschlagen: 0
- Übersprungen: 0

Ausgeführt: `dotnet build Reporter.sln --configuration Release` (0 Fehler, 1 Warnung — bekannte iOS-Nullability-Warnung CS8765 in `AppDelegate.cs`) → `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --no-build --configuration Release --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"` (Release-Modus gemäß plan.md Schritt 14; Iteration 3).

## Testabdeckung

**Abdeckung:** 91,82 % (Zeilen, gesamt; Reporter.Core 89,38 % / Reporter.Data 98,94 % — EF-Core-Migrationsdateien über `coverlet.runsettings` ausgeschlossen)

| Datei | Abdeckung |
|-------|-----------|
| Reporter.Core\Resources\Strings\AppResources.Designer.cs | 25,6 % (generiert) |
| Reporter.Core\Models\CategoryFilterItem.cs | 28,6 % |
| Reporter.Core\Services\FeedSearchUnavailableException.cs | 33,3 % |

## Fehlende Tests

Quelle: `Coverage-Daten`

- Keine nicht-generierten Quelldateien mit 0 % Abdeckung. `AppResources.Designer.cs` (25,6 %) ist generiert und wird ignoriert; sämtliche EF-Core-Migrationsdateien (`src/Reporter.Data/Migrations/`, inkl. `AddSettingsDebugCollection` und `AddDebugLogEntries`) sind per `coverlet.runsettings` von der Messung ausgeschlossen.
