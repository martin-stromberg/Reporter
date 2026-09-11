# Offene Aufgaben

Erstellt am: 2026-09-11 (Fortsetzungslauf, 2. Abbruch)
Abbruchgrund: Kein Fortschritt zwischen den letzten zwei Iterationen (3 offene Punkte in Fortsetzungs-Iteration 1, 3 offene Punkte in Fortsetzungs-Iteration 2)

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine — `review.md` trägt den Status `Vollständig umgesetzt`.

## Code-Review-Befunde

- [ ] `ArticleDetailViewModel.cs` (`ArticleDetailViewModel`) — **Hardcodierte Werte / inkonsistente Lokalisierung (niedrig):** `AutoMarkReadLabel` (Zeilen 266–268, Feldinitialisierung Zeile 43) mischt den hartcodierten deutschen Format-String `$"Auto-Gelesen ({delay} s)"` mit dem lokalisierten `AppResources.ArticleAutoMarkReadDisabled`. Dasselbe UI-Element erscheint je nach Zustand in unterschiedlicher Sprache. Empfehlung: zusätzlichen resx-Key für den aktivierten Zustand anlegen (z. B. `ArticleAutoMarkReadDelayFormat` „Auto-Gelesen ({0} s)" / „Auto-read ({0} s)") in beiden resx-Dateien + Designer.

## Usability-Befunde

- [ ] `AppResources.de.resx` / `SettingsPage.xaml` (Sektion „Aufbewahrungsdauer") — **Erreichbarkeit/Beschriftung (niedrig):** `SettingsRetentionInfo` (de.resx Z. 204–206, angezeigt in `SettingsPage.xaml` Z. 48) spricht von „mit Sternchen markierte" Artikel — die App verwendet jedoch ein Lesezeichen-Icon und den Tab „Später"; es gibt kein Sternchen. Empfehlung: „mit Lesezeichen versehene Artikel (Tab „Später")".
- [ ] `AppResources.de.resx` / `SettingsPage.xaml` (Sektion „Keyword-Filter") — **Erreichbarkeit/Beschriftung (niedrig):** `SettingsKeywordMatchLabel` „Teilwort & Case-Insensitive" (de.resx Z. 216–218, `SettingsPage.xaml` Z. 120) ist englischer Fachjargon; der Hinweis erklärt nur den Teilwort-Aspekt, nicht das Ignorieren der Groß-/Kleinschreibung. Empfehlung: Klartext wie „Teilwort, Groß-/Kleinschreibung egal" (englischen Key entsprechend nachziehen).

## Fehlgeschlagene Tests

Keine — `test-results.md` trägt den Status `Keine Fehler` (144/144 bestanden).

Hinweis (kein offener Punkt, aber für Folgearbeiten relevant): `SettingsViewModelTests_Persist.Persist_QueuedBehindRunningSave_AppliesThemeOnce` zeigte einmalig eine Test-Infrastruktur-Race (`TestDbContextFactory` teilt eine In-Memory-`SqliteConnection` über Contexts; `SqliteException: unable to delete/modify user-function due to active statements`). Bestand in allen Wiederholungsläufen — potenziell in CI wiederkehrend.
