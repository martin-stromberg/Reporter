# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

## Laufumgebung und Befehle (Iteration 3)

| Schritt | Befehl | Ergebnis |
|---------|--------|----------|
| Solution-Build inkl. iOS | `dotnet build Reporter.sln -c Release` (ohne `IncludeIosTarget=false`) | Erfolgreich: **0 Fehler, 1 Warnung** — beide Targets `net10.0-windows10.0.19041.0` **und `net10.0-ios`** (iossimulator-x64) kompilieren auf diesem Windows-Rechner (installiertes Workload `ios 26.5.10318/10.0.100`, SDK 10.0.401) |
| Testlauf mit Coverage | `dotnet test src/Reporter.Tests -c Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage"` | **185/185 bestanden** (2,3 s) |
| Stabilitäts-Stichprobe | `dotnet test src/Reporter.Tests -c Release --no-build --filter "FullyQualifiedName~SettingsViewModelTests_Persist"` | 19/19 bestanden — kein `SQLite Error 5`, Flakiness bleibt beseitigt (Shared-Cache-In-Memory in `TestDbContextFactory`) |

**iOS-Build-Befund (neu in Iteration 3):** Die Solution baut erstmals inkl. `net10.0-ios`-Target
durch — `LocalNotificationService` (`#if IOS`-Pfad mit `UNUserNotificationCenter`),
`NotificationDelegate` und `AppDelegate.FinishedLaunching` kompilieren. Einzige Warnung:
`CS8765` in `src/Reporter/Platforms/iOS/AppDelegate.cs:24` — die NULL-Zulässigkeit des
Parameters `launchOptions` (`NSDictionary` statt `NSDictionary?`) entspricht nicht exakt dem
überschriebenen Member; rein kosmetisch, kein Buildbruch.

**Neue Tests seit Iteration 2 (181 → 185, +4):**
`SettingsViewModelTests_Load.Load_PermissionDenied_SetsNotificationPermissionDenied`,
`Load_PermissionGranted_ClearsNotificationPermissionDenied`,
`Load_NotificationsDisabled_HidesNotificationPermissionDenied`,
`SettingsViewModelTests_Persist.NotificationsEnabled_TurnedOff_ClearsNotificationPermissionDenied`
(sowie `NotificationsEnabled_TurnedOn_UnsupportedPlatform_SkipsRequest`) — alle bestanden.

## E2E-Abdeckung

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Sync legt neue Artikel an → Benachrichtigung über die komplette Entscheidungskette (`SyncFeedAsync` → `RunSyncAsync` → `NotificationService` → `ILocalNotificationService`) | `FeedSyncServiceTests.SyncFeedAsync_NewItems_NotifiesWithFeedAndItems` | Bestanden |
| Sammelmodus: `NotificationSummaryEnabled` aktiv → ein Sync mit mehreren Artikeln erzeugt genau eine Sammel-Benachrichtigung | `FeedSyncServiceTests.SyncFeedAsync_SummaryMode_SendsSingleSummaryNotification` | Bestanden |
| Pro-Feed-Schalter: Feed bearbeiten → aus → speichern → neu laden → persistiert und beim Sync wirksam | `FeedsViewModelTests.EditCommand_PopulatesFeedNotificationsEnabled`, `SaveCommand_NewFeed_PersistsNotificationsEnabledFalse`, `SaveCommand_ExistingFeed_PersistsNotificationsEnabled`, `SaveCommand_ResetsFeedNotificationsEnabled` + `FeedSyncServiceTests.SyncFeedAsync_FeedDisabled_NoNotifications` | Bestanden |
| Modus-Schalter in den Einstellungen: umschalten → persistieren → neu laden | `SettingsViewModelTests_E2E.E2E_NotificationSummary_PersistRoundtrip` (+ `SettingsViewModelTests_Persist.NotificationSummaryEnabled_Change_Persists`, `SettingsViewModelTests_Load.Load_PopulatesNotificationSummaryEnabled`, `SettingsRepositoryTests.SaveAsync_PersistsNotificationSummaryEnabled`) | Bestanden |
| Ruhezeit aktiv → Sync läuft, aber keine Benachrichtigung (inkl. Wrap-around 22:00–07:00, `Start == End`, einseitig `null`) | `NotificationServiceTests`: `WithinQuietHours_SendsNothing`/`OutsideQuietHours_Sends` (Theory, `FakeTimeProvider`), `QuietHoursStartEqualsEnd_Sends`, `OnlyOneQuietHoursBound_Sends` | Bestanden |
| Dedup: keine Benachrichtigung ohne neue Items; Identifier-Stabilität beider Modi | `FeedSyncServiceTests.SyncFeedAsync_NoNewItems_DoesNotNotify`, `NotificationServiceTests.NotifyNewItemsAsync_SendsPerItem_WithItemIdAsIdentifier`, `NotifyNewItemsAsync_SummaryEnabled_SameItems_SameIdentifier` | Bestanden |
| Berechtigungsstatus in den Einstellungen: verweigert → Hinweis gesetzt; erteilt/deaktiviert → Hinweis zurückgesetzt | `SettingsViewModelTests_Load.Load_PermissionDenied_SetsNotificationPermissionDenied`, `Load_PermissionGranted_ClearsNotificationPermissionDenied`, `Load_NotificationsDisabled_HidesNotificationPermissionDenied`, `SettingsViewModelTests_Persist.NotificationsEnabled_TurnedOff_ClearsNotificationPermissionDenied` | Bestanden |
| UI-Verifikation `FeedsPage`/`SettingsPage` (390 × 844 Handysize, Touch-Targets ≥ 44 pt, Abdunklung bei ausgeschaltetem globalem Schalter, Dark Mode, Edit-/Save- und Toggle-/Persist-Fluss) | Manuelle Verifikation — dokumentiert im Repository-`test-results.md` (Abschnitt „Manuelle UI-Verifikation (durchgeführt)"), Screenshots `test-results/issue-27/manual-01…07-*.png`, UIA-Skript `test-results/issue-27/uia.ps1` | Bestanden (manuell verifiziert und dokumentiert) |
| iOS-Plattformnachweis: Berechtigungsdialog, Banner/Sound im Vordergrund, Einzel- und Sammelmodus, Tap-Navigation | Manuell via `scripts/iOS-Deployment.ps1` (nur macOS) — plangemäß als dokumentierter Folgeschritt deklariert; iOS-**Kompilierung** in dieser Iteration erstmals erfolgreich nachgewiesen (s. oben) | Nicht ausgeführt (plangemäß dokumentierter macOS-Folgeschritt — Erfüllungsmaßstab laut Plan: Dokumentation des Folgeschritts, vorhanden) |
| Berechtigung verweigert / Notification-Pfad wirft → Sync bleibt erfolgreich | `FeedSyncServiceTests.SyncFeedAsync_NotificationThrows_SyncStillSucceeds` (+ iOS-Anteil im manuellen Folgeschritt) | Bestanden (automatisierter Anteil) |

## Zusammenfassung

- Gesamt: 185
- Bestanden: 185
- Fehlgeschlagen: 0
- Übersprungen: 0

## Testabdeckung

**Abdeckung:** 89,4 % (Cobertura-Zeilenabdeckung gesamt; `Reporter.Core` 84,6 %,
`Reporter.Data` 97,7 %; gemessen mit `coverlet.runsettings`, die
`Reporter.Data.Migrations.*` ausschließt — CI-konform. Das MAUI-App-Projekt `src/Reporter`
ist nicht Teil der Testausführung und nicht instrumentiert — Plattformcode wie
`LocalNotificationService`/`UNUserNotificationCenter` ist begründet nicht unit-testbar und
durch den dokumentierten manuellen Folgeschritt abgedeckt; die iOS-Kompilierung ist
zusätzlich durch den Solution-Build nachgewiesen.)

| Datei | Abdeckung |
|-------|-----------|
| `Reporter.Core\Resources\Strings\AppResources.Designer.cs` | 22,0 % (generierte Datei) |

Feature-relevante Dateien (alle ≥ 80 %): `NotificationService.cs` 98,0 %,
`FeedSyncService.cs` 93,8 %, `SettingsViewModel.cs` 91,9 %, `FeedsViewModel.cs` 89,4 %,
`FeedRepository.cs` 100 %, `SettingsRepository.cs` 95,5 %, Modelle/Entities 100 %.

## Fehlende Tests

Quelle: `Coverage-Daten`

- Keine Quelldatei mit 0 % Zeilenabdeckung (im instrumentierten Umfang). Einzige Datei unter
  80 % ist die generierte `AppResources.Designer.cs` (22,0 %) — generierte
  Ressourcen-Designer-Datei, keine separate Testabdeckung erforderlich.
- `src/Reporter` (MAUI-App, u. a. `Services/LocalNotificationService.cs`,
  `Platforms/iOS/NotificationDelegate.cs`, `AppDelegate.cs`) ist nicht in der Coverage
  enthalten, da `Reporter.Tests` das App-Projekt nicht referenziert; der Plattformpfad ist
  laut Plan/`inventory` begründet nicht unit-testbar, kompiliert aber nachweislich für
  `net10.0-ios` und ist über den dokumentierten manuellen iOS-Folgeschritt (macOS) abgedeckt.
