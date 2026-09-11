# Offene Aufgaben

Erstellt am: 2026-09-11 (Fortsetzungslauf 2, 3. Abbruch)
Abbruchgrund: Kein Fortschritt zwischen den letzten zwei Iterationen (1 offener Punkt → 5 offene Punkte; jede Review-Runde findet neue Kleinigkeiten)
Aktualisiert am: 2026-09-11 — alle 5 Restbefunde manuell behoben und verifiziert
(Build Release 0 Warnungen, 147/147 Tests bestanden, `Run-StaticChecks.ps1` Exit-Code 0).
Keine neue Review-Runde gestartet — Status daher „behoben, Review ausstehend".

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und wurden nachträglich manuell bearbeitet.

## Offene Planelemente

Keine — `review.md` trägt den Status `Vollständig umgesetzt`.

## Code-Review-Befunde

- [x] `SettingsViewModel.cs` (`PersistRetentionDebouncedAsync`/`CancelRetentionDebounce`) — **CTS-Lebenszyklus (niedrig):** Behoben. Besitzverhältnis wird jetzt atomar über `Interlocked.Exchange` übertragen: Nur die noch aktuelle Debounce persistiert und disposed ihr CTS; `CancelRetentionDebounce` nullt die Referenz vor `Cancel()`/`Dispose()` und fängt `ObjectDisposedException` defensiv ab.
- [x] `SettingsViewModel.cs` (`FormatRetentionDays`) — **Inkonsistente Rundung (niedrig):** Behoben. Format-Helper nutzt jetzt `(int)Math.Round(days)`, identisch zur Persistierung.
- [x] `SettingsViewModelTests_Persist.cs` (`RetentionDays_RapidChanges_PersistOnlyLastValue`) — **Test-Robustheit (niedrig):** Behoben. Zusätzliches `Advance(5s)` + `Task.Delay(50)` vor `Assert.Single` wie im Schwestertest.

## Usability-Befunde

- [x] `SettingsPage.xaml` — **Erreichbarkeit/Touch-Targets (niedrig):** Behoben. Alle vier `Switch`-Elemente, drei `Picker` und zwei `TimePicker` haben jetzt `MinimumWidthRequest`/`MinimumHeightRequest` 44 pt und `SemanticProperties.Description` (Wiederverwendung der vorhandenen lokalisierten Label-Keys).
- [x] `SettingsViewModel.cs` (`FormatRetentionDays`) — **Anzeige ≠ gespeicherter Wert (niedrig):** Behoben, identisch zum Code-Befund oben.

## Fehlgeschlagene Tests

Keine — `test-results.md` trägt den Status `Keine Fehler` (147/147 bestanden).

## Hinweise (keine offenen Punkte, für Folgearbeiten)

- `SettingsViewModelTests_Persist.Persist_QueuedBehindRunningSave_AppliesThemeOnce` ist latent flaky: präexistente Race in `TestDbContextFactory` (geteilte In-Memory-`SqliteConnection` über Contexts; `SQLite Error 5: unable to delete/modify user-function due to active statements`). Auf Baseline ohne Feature-Änderungen reproduziert (`git stash`-Verifikation). Mögliche Behebung: transiente Exceptions im Poll-Loop von `TestWaitHelper` tolerieren oder Context-Erzeugung serialisieren.
- `FeedsViewModel.cs` hat nur 46,9 % Zeilenabdeckung (`SaveAsync`/`DeleteAsync` ungetestet) — älteres Feature, nicht Issue #26.
