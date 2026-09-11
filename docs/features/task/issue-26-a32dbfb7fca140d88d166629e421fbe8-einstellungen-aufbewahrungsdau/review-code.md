# Code-Review
Status: Befunde vorhanden

> **Nachtrag (2026-09-11):** Alle 5 Befunde wurden anschließend behoben —
> CompareExchange statt Exchange (Befund 1), atomare Zuweisung mit
> Vorgänger-Dispose in `ScheduleRetentionPersist` (Befund 2), `_isLoading`
> als `volatile` (Befund 3), `ConcurrentQueue<int>` im Test-Repository
> (Befund 4), `SemanticProperties.Description` am Slider (Befund 5).
> Verifikation: Build 0 Warnungen, 147/147 Tests, Static Checks Exit 0.

## Befund 1 — mittel
**Datei:** `src/Reporter.Core/ViewModels/SettingsViewModel.cs`, Z. 471 (in `PersistRetentionDebouncedAsync`)

**Beschreibung:** Die Besitzübernahme per `Interlocked.Exchange(ref _retentionDebounceCts, null)` nullt das Feld **unbedingt** — auch wenn zwischenzeitlich ein neuerer Debounce-CTS (`cts2`) eingetragen wurde. Verlorener Persist möglich:

1. Debounce-Task T1 (für Wert A): `Task.Delay` ist abgelaufen, die Fortsetzung läuft auf dem Threadpool, ist aber noch vor dem `Exchange` (Scheduling-Lücke).
2. UI-Thread: `RetentionDays` ändert sich → `CancelRetentionDebounce` (Exchange→cts1, Cancel+Dispose), dann `_retentionDebounceCts = cts2`, T2 startet.
3. T1 führt `Exchange` aus → liefert **cts2** (≠ cts1) → T1 bricht korrekt ab, hat aber das Feld auf `null` gesetzt.
4. T2 läuft ab → `Exchange` liefert `null` ≠ cts2 → T2 bricht ab → **die letzte Änderung wird nie persistiert**; zudem wird cts2 nie disposed (Leak).

Die Race ist selten (Fortsetzung muss exakt zwischen Delay-Ende und Exchange verzögert werden), aber real — mit `Task.Delay` auf dem System-Threadpool nicht ausgeschlossen. In Tests mit `FakeTimeProvider` (synchrone `Advance`-Callbacks) nicht reproduzierbar.

**Empfehlung:** `CompareExchange` statt `Exchange` verwenden, damit nur der eigene Slot genullt wird:
```csharp
if (Interlocked.CompareExchange(ref _retentionDebounceCts, null, cts) != cts)
{
    return;
}
```
Damit entfällt sowohl der Lost-Persist als auch das cts2-Leck.

## Befund 2 — niedrig
**Datei:** `src/Reporter.Core/ViewModels/SettingsViewModel.cs`, Z. 435 (`ScheduleRetentionPersist`)

**Beschreibung:** `_retentionDebounceCts = cts;` ist eine nicht-atomare Zuweisung — inkonsistent zur sonst konsequent verwendeten `Interlocked`-Disziplin. Bei konkurrierenden Setter-Aufrufen (z. B. Programm- statt UI-Thread) können zwei Aufrufe beide `CancelRetentionDebounce` mit `null` sehen und dann beide zuweisen: der zuerst zugewiesene CTS wird verwaist (nie disposed, dessen Task persistiert nichts). In der Praxis laufen Setter auf dem UI-Thread — daher niedrig.

**Empfehlung:** Zuweisung dokumentieren (UI-Thread-Annahme) oder ebenfalls über `Interlocked.Exchange`/`lock` absichern.

## Befund 3 — niedrig
**Datei:** `src/Reporter.Core/ViewModels/SettingsViewModel.cs`, Z. 478–481 (`PersistRetentionDebouncedAsync`), Z. 48 (`_isLoading`)

**Beschreibung:** Der `_isLoading`-Check nach der Debounce-Verzögerung verwirft den Persist kommentarlos — ohne Neuplanung. Läuft gerade ein `LoadAsync` (z. B. erneutes `OnAppearing`), geht eine zwischenzeitliche Slider-Änderung verloren (sie wird allerdings ohnehin von `LoadAsync` Z. 376 überschrieben). Zusätzlich ist `_isLoading` ein einfaches `bool` ohne `volatile`; gelesen wird es auf dem Threadpool-Thread — ein veralteter `false`-Wert erlaubt einen Persist aus halb geladenem VM-Zustand (PersistAsync baut den Snapshot aus Live-Properties innerhalb des Locks).

**Empfehlung:** `_isLoading` als `volatile bool` deklarieren oder den Persist im Loading-Fall nicht verwerfen, sondern nach dem Lock ausführen (der Snapshot ist ohnehin live).

## Befund 4 — niedrig
**Datei:** `src/Reporter.Tests/SettingsViewModelTests_Persist.cs`, Z. 397 (`SavedRetentionDays`), Z. 124, 129–130

**Beschreibung:** `List<int> SavedRetentionDays` wird von der Persist-Fortsetzung (Threadpool) geschrieben und vom Test-Thread unsynchronisiert gelesen (`Count`, `Assert.Single`, Index `[0]`). In der Praxis unkritisch (`Add` schreibt das Element vor der Count-Erhöhung; `WaitUntilAsync` gated den Zugriff), aber formal nicht threadsicher und ein latenter Flaky-Punkt. Der „genau ein Save"-Nachweis hängt außerdem an einem 50-ms-Echzeitfenster (`Task.Delay(50)` nach `Advance(5s)`) — ein verspäteter zweiter Persist innerhalb des Fensters würde erkannt, danach nicht mehr; das Fenster ist klein, aber für die synchrone Fortsetzung nach `Advance` ausreichend.

**Empfehlung:** `ConcurrentQueue<int>` (oder `lock` um alle Zugriffe) verwenden; bei Bedarf Fenster vergrößern oder auf stabilen Endzustand pollen (z. B. Count bleibt 1 über mehrere Poll-Intervalle).

## Befund 5 — niedrig
**Datei:** `src/Reporter/Views/SettingsPage.xaml`, Z. 36–40 (`Slider`)

**Beschreibung:** Der Retention-`Slider` — das zentrale Control des Features — hat `MinimumHeightRequest="44"`, aber **kein** `SemanticProperties.Description`. Alle anderen interaktiven Controls (4 Switches, 3 Picker, 2 TimePicker, Entfernen-Button) wurden mit einer Description versehen; Screenreader bekommen für den Slider kein Label. `Entry` (Z. 66) und Add-`Button` (Z. 69) haben ebenfalls keine Description, verfügen aber über `Placeholder`/`Text` — vertretbar.

**Empfehlung:** `SemanticProperties.Description="{x:Static strings:AppResources.SettingsRetentionLabel}"` am Slider ergänzen (Key existiert in `AppResources.resx`/.de.resx).

## Verifiziert unauffällig
- `FormatRetentionDays` (Z. 363–366), `PersistAsync` (Z. 495) und `SaveRetention` (Z. 421) nutzen identisch `(int)Math.Round(...)` + `Math.Clamp` — Rundung konsistent.
- `SaveRetention` ↔ Debounce: Setter plant ggf. Debounce, `CancelRetentionDebounce` räumt ihn danach zuverlässig ab, danach sofortiger Persist — kein Doppel-Save, kein Lost-Update (Persist liest Live-Werte).
- `CancelRetentionDebounce` (Z. 439–456): Exchange-vor-Cancel/Dispose stellt sicher, dass die Debounce-Fortsetzung ein bereits dispostes CTS nie sieht (Exchange liefert ihr dann `null`); `ObjectDisposedException`-Catch ist korrekt defensiv. Doppel-Dispose nicht möglich (nur Exchange-Gewinner disposed).
- Persist-Reihenfolge vs. `_persistLock`: Snapshot wird innerhalb des Locks aus Live-Properties gebaut → Reihenfolge der Wartenden irrelevant, letzter Persist schreibt aktuellen Stand.
- XAML-Keys: alle referenzierten `AppResources`-Keys existieren in `.resx` und `.de.resx`; `SettingsKeywordRemoveFormat` enthält `{0}` und ist als `StringFormat` korrekt verwendbar; 44-pt-Touch-Targets konsistent gesetzt.
- `Task.Delay(TimeSpan, TimeProvider, CancellationToken)` korrekt für `FakeTimeProvider`-Tests (net10.0).
