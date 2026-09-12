# Offene Aufgaben

Erstellt am: 2026-09-12
Abbruchgrund: Maximale Iterationsanzahl erreicht (3 Iterationen; offene Punkte 8 → 6 → 7, kein weiterer Fortschritt)

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine — `review.md` trägt den Status `Vollständig umgesetzt`.

## Code-Review-Befunde

- [ ] `UnreadViewModel.RefreshAsync` (Z. 325–330): `!IsOnline`-Frühreturn lässt `IsSyncing = true` hängen — die TwoWay-`IsRefreshing`-Bindung setzt `IsSyncing` vor der Command-Ausführung; endloser Spinner, `OnConnectivityChanged` räumt nicht auf. Regression gegenüber HEAD. Empfehlung: `IsSyncing = false` im Offline-Abbruchpfad (keine `ErrorMessage` — `…_SkipsSyncWithoutError`-Tests verlangen stilles Überspringen).
- [ ] `FeedsViewModel.RefreshAllAsync` (Z. 379–413): `if (IsSyncing) return;` (Z. 381) greift bei JEDER Pull-Geste — auch online —, weil die Bindung `IsSyncing` vorbelegt → Sync läuft nie an, `RefreshAllCommand`/`RefreshCommand` (`CanExecute = !IsSyncing`) dauerhaft deaktiviert; `!IsOnline`-Return (Z. 386–389) folgt demselben Leckmuster. Empfehlung: Guard auf nicht-gebundenes Laufzeitflag umstellen bzw. entfernen + `IsSyncing = false` im Offline-Pfad.
- [ ] `UnreadViewModel.RefreshAsync` (Z. 355): `ErrorMessage = syncError` wird bedingungslos nach `LoadAsync()` zugewiesen — ein in `LoadPageAsync` gesetzter `ErrorLoadFailed` wird bei leerem `syncError` gelöscht. Empfehlung: nur bei nicht-leerem `syncError` zuweisen.
- [ ] `UnreadViewModel.OnConnectivityChanged` (Z. 452–456): leert `ErrorMessage` bei jedem Statuswechsel — entfernt auch legitime `ErrorLoadFailed`-Meldungen (ein Kanal für Sync- und Ladefehler). Empfehlung: getrennter Sync-Fehler-Kanal wie in `FeedsViewModel` oder gezieltes Leeren.
- [ ] `FeedsViewModel` `RefreshAsync`/`RefreshAllAsync`: nahezu identischer ~30-zeiliger Ablauf dupliziert (`SyncFeedAsync(feed.Id)` vs. `SyncAllAsync()`). Empfehlung: gemeinsame private Hilfsmethode, z. B. `SyncAsync(Func<Task<SyncResult>>)`.

## Usability-Befunde

- [ ] `UnreadPage` Pull-to-Refresh offline → endlose Ladeanzeige (gleiche Ursache wie Code-Befund 1). Empfehlung des Reviews: `IsSyncing` zurücksetzen und `OfflineHint` als `ErrorMessage` anzeigen. Hinweis: Steht im Widerspruch zum Usability-Befund aus Runde 2 („keine rote Doppelmeldung offline") — bei Umsetzung beide Bewertungen abwägen (persistentes Banner vs. konkrete Rückmeldung auf aktive Nutzeraktion).
- [ ] `FeedsPage` Pull-to-Refresh → endlose Ladeanzeige + dauerhaft deaktivierter `RefreshAllCommand`; pro-Feed `RefreshCommand` schweigt offline. Empfehlung: `IsSyncing` zurücksetzen und `OfflineHint` als `SyncErrorMessage` anzeigen (gleiche Abwägung wie oben).

## Fehlgeschlagene Tests

Keine fehlgeschlagenen automatisierten Tests (231/231 grün, Static Checks Exit 0).

Offen bleiben jedoch die 10 manuellen E2E-Verifikationsszenarien aus `plan.md` (9 Pflicht + 1 Soll iOS) — in dieser Umgebung nicht ausführbar, manuell nachzuholen und in `test-results.md`/`docs/help/anwendung/mobile-ui-design.md` zu dokumentieren (Windows 390×844 pt, iOS-Simulator falls macOS-Host verfügbar).
