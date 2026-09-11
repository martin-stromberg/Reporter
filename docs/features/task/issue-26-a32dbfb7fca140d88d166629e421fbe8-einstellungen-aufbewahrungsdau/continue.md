# Offene Aufgaben

Erstellt am: 2026-09-11
Abbruchgrund: Kein Fortschritt zwischen den letzten zwei Iterationen (6 offene Punkte in Iteration 1, 6 offene Punkte in Iteration 2)

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine — `review.md` trägt den Status `Vollständig umgesetzt`.

## Code-Review-Befunde

- [ ] `AutoRefreshService.cs` (`AutoRefreshService`) — **Toter Code / Testqualität:** Der `_syncRunning`-Guard (Zeilen 21, 117–120, 134–137) ist strukturell unerreichbar; `RunLoopAsync` arbeitet `WaitForNextTickAsync` und `SyncAllAsync` streng sequenziell ab und `PeriodicTimer` koalesciert verpasste Ticks. Der Test `OverlappingTick_SkipsSync` (`AutoRefreshServiceTests.cs`, Zeilen 157–174) würde auch ohne Guard grün sein. Empfehlung: Guard entfernen und Test umbenennen/umdokumentieren, oder Guard als defensive Absicherung mit Kommentar behalten und XML-Kommentar des Tests klarstellen.
- [ ] `AutoRefreshServiceTests.cs` (`AutoRefreshServiceTests`) — **Testqualität:** `StopAsync_DisposesLoopCancellationTokenSource` (Zeilen 203–216) liest per Reflection das private Feld `_loopCts` und prüft `ObjectDisposedException` — koppelt sich an Implementierungsdetails statt an fachliches Verhalten. Empfehlung: verhaltensnah prüfen (wiederholte Start/Stop-Zyklen) oder Reflection-Zugriff in Hilfsmethode kapseln.
- [ ] `ArticleDetailViewModel.cs` / `SettingsViewModel.cs` / `AppThemeService.cs` — **Hardcodierte Werte / Doppelter Code:** Die Settings-Werte `"on_open"`/`"off"`/`"system"`/`"light"`/`"dark"` sind als Literale dupliziert, obwohl teils private Konstanten existieren (`SettingsViewModel` Zeilen 21–23; Zeile 84 nutzt `"system"` als Literal trotz `ThemeSystem`; `ArticleDetailViewModel` Zeilen 243, 273, 392; `AppThemeService` Zeilen 20–21). Empfehlung: zentrale Konstanten (z. B. `public const` auf `Reporter.Core.Models.Settings` oder `SettingsValues`-Klasse in `Reporter.Core`) und alle Literale darauf umstellen.
- [ ] `TestWaitHelper.cs` (`TestWaitHelper`) — **Doppelter Code:** Die beiden `WaitUntilAsync`-Overloads (Zeilen 14–28 und 36–50) duplizieren die komplette Polling-Schleife. Empfehlung: synchroner Overload delegiert an asynchronen (`WaitUntilAsync(() => Task.FromResult(condition()), timeoutMilliseconds)`).

## Usability-Befunde

- [ ] `SettingsPage.xaml` (Sektion „Benachrichtigungen & Ruhezeiten") — **Erreichbarkeit:** `QuietHoursStart`/`QuietHoursEnd` sind fachlich nullable („keine Ruhezeit" ist vorgesehener Zustand), aber die `TimePicker` (Zeilen 244–267) bieten keine Möglichkeit, eine einmal gesetzte Ruhezeit wieder zu entfernen. Empfehlung: Ein/Aus-Schalter „Ruhezeit aktivieren" vor die VON/BIS-Auswahl (analog zum Intervall-Picker-Muster) oder „Zurücksetzen"-Aktion, die beide Felder auf `null` setzt.
- [ ] `SettingsPage.xaml` (Sektion „Keyword-Filter") — **Erreichbarkeit:** Der Schalter „Teilwort & Case-Insensitive" (Zeilen 123–126) ist `IsToggled="True"` + `IsEnabled="False"` und wirkt wie ein defektes Bedienelement ohne Rückmeldung, warum er gesperrt ist. Empfehlung: festes Verhalten als Status-Text/Badge darstellen (z. B. „Immer aktiv: erkennt auch Varianten innerhalb von Wörtern") statt als gesperrten Switch.

## Fehlgeschlagene Tests

Keine — `test-results.md` trägt den Status `Keine Fehler` (132/132 bestanden).
