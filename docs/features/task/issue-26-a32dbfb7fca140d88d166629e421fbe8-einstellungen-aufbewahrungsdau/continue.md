# Offene Aufgaben

Erstellt am: 2026-09-11 (Fortsetzungslauf 2, 3. Abbruch)
Abbruchgrund: Kein Fortschritt zwischen den letzten zwei Iterationen (1 offener Punkt → 5 offene Punkte; jede Review-Runde findet neue Kleinigkeiten)

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine — `review.md` trägt den Status `Vollständig umgesetzt`.

## Code-Review-Befunde

- [ ] `SettingsViewModel.cs` (`PersistRetentionDebouncedAsync`, Zeilen 447–464) — **CTS-Lebenszyklus (niedrig):** Das Debounce-`CancellationTokenSource` wird auf dem Erfolgspfad nicht disposed und `_retentionDebounceCts` nicht genullt — das Singleton-VM-Feld zeigt dauerhaft auf ein abgelaufenes CTS. Inkonsistent zu `ArticleDetailViewModel.MarkReadDelayedAsync` (Z. 446–457, `finally` + Feld-Nullung). Beim Fix Warnung vor `Cancel()`-auf-disposed-CTS-Race beachten (`IsCancellationRequested` vor `Cancel()` prüfen bzw. Referenz vorher nullen).
- [ ] `SettingsViewModel.cs` (`FormatRetentionDays`, Zeilen 363–366) — **Inkonsistente Rundung (niedrig):** Anzeige schneidet via `(int)days` ab, `PersistAsync`/`SaveRetention` runden via `Math.Round` → z. B. 42,9 zeigt „42 Tage", gespeichert wird 43. Empfehlung: `(int)Math.Round(days)` im Format-Helper.
- [ ] `SettingsViewModelTests_Persist.cs` (`RetentionDays_RapidChanges_PersistOnlyLastValue`, Zeilen 112–127) — **Test-Robustheit (niedrig):** Wartet nur bis `Count == 1`; ein fehlerhafter zweiter Save bliebe unentdeckt. Empfehlung: zusätzliches `Advance` + Delay vor `Assert.Single`, wie im Schwestertest.

## Usability-Befunde

- [ ] `SettingsPage.xaml` — **Erreichbarkeit/Touch-Targets (niedrig):** Alle vier `Switch`-Elemente, drei `Picker` und zwei `TimePicker` haben kein `MinimumHeightRequest`/`MinimumWidthRequest` von 44 pt und keine `SemanticProperties.Description` — Verstoß gegen AGENTS.md (Touch-Targets ≥ 44×44 pt) und inkonsistent zum Rest (Slider Z. 38, ×-Button Z. 99–102, `ArticleDetailPage.xaml` Z. 41–42).
- [ ] `SettingsViewModel.cs` (`FormatRetentionDays`) — **Anzeige ≠ gespeicherter Wert (niedrig):** identisch zum Code-Befund oben — Anzeige schneidet ab, Persistierung rundet.

## Fehlgeschlagene Tests

Keine — `test-results.md` trägt den Status `Keine Fehler` (147/147 bestanden).

## Hinweise (keine offenen Punkte, für Folgearbeiten)

- `SettingsViewModelTests_Persist.Persist_QueuedBehindRunningSave_AppliesThemeOnce` ist latent flaky: präexistente Race in `TestDbContextFactory` (geteilte In-Memory-`SqliteConnection` über Contexts; `SQLite Error 5: unable to delete/modify user-function due to active statements`). Auf Baseline ohne Feature-Änderungen reproduziert (`git stash`-Verifikation). Mögliche Behebung: transiente Exceptions im Poll-Loop von `TestWaitHelper` tolerieren oder Context-Erzeugung serialisieren.
- `FeedsViewModel.cs` hat nur 46,9 % Zeilenabdeckung (`SaveAsync`/`DeleteAsync` ungetestet) — älteres Feature, nicht Issue #26.
