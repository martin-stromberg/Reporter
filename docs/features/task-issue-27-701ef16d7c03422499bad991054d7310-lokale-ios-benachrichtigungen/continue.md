# Offene Aufgaben

Erstellt am: 2026-09-11
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine — `review.md` (Iteration 3) trägt den Status `Vollständig umgesetzt`.

## Code-Review-Befunde

- [ ] `src/Reporter/Platforms/iOS/NotificationDelegate.cs` — `DidReceiveNotificationResponse` (Zeilen 24–68) prüft `response.ActionIdentifier` nicht: Ein Wegwischen (`UNNotificationDismissActionIdentifier`) löst ebenfalls Navigation/Browser-Fallback aus. Fix: Zu Beginn `if (!response.IsDefaultAction) { completionHandler(); return; }` ergänzen.
- [ ] `src/Reporter/Platforms/iOS/NotificationDelegate.cs` — Launcher-Fallback `await Launcher.Default.OpenAsync(link)` (Zeile 58) läuft nicht auf dem Main-Thread (nur die Shell-Navigation ist via `MainThread.InvokeOnMainThreadAsync` gemarshaled). Fix: Aufruf ebenfalls über `MainThread.InvokeOnMainThreadAsync` ausführen.
- [ ] `%TEMP%foundation.cs` im Repository-Root — versehentliches, untracked Artefakt (23.452 Zeilen, Kopie der xamarin-macios-Binding-Definition). Vor dem Commit löschen.

## Usability-Befunde

- [ ] `SettingsPage.xaml`/`SettingsViewModel.cs` — Irreführender „deaktiviert"-Hinweis bei iOS-Status `NotDetermined`: `IsAuthorizedAsync` liefert `false` für `NotDetermined` → `NotificationPermissionDenied = true`, obwohl nie etwas verweigert wurde; der Button „Einstellungen öffnen" führt in eine Sackgasse (iOS zeigt den „Mitteilungen"-Eintrag erst nach erster Berechtigungsanfrage). Empfehlung: `NotDetermined` von `Denied` unterscheiden (Status über `ILocalNotificationService` exponieren; bei `NotDetermined` neutrale Zeile mit „Benachrichtigungen erlauben"-Button, der `RequestAuthorizationAsync` auslöst).
- [ ] `SettingsPage.xaml`/`FeedsPage.xaml` — Funktionslose Schalter auf Nicht-iOS-Plattformen (`IsSupported == false`, Windows-Target existiert): Benachrichtigungs-Schalter persistieren, ohne dass je Benachrichtigungen kommen. Empfehlung: Sektion bei `!IsSupported` ausblenden/deaktivieren mit Hinweis „derzeit nur auf iOS verfügbar"; Pro-Feed-Schalter entsprechend behandeln.

## Fehlgeschlagene Tests

Keine — `test-results.md` (Iteration 3) trägt den Status `Keine Fehler` (185/185 bestanden, inkl. `net10.0-ios`-Build).
