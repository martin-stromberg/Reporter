<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### src/Reporter.Core/Services/DebugReportService.cs (Versand-Aktion „Debugbericht senden")

- **Erreichbarkeit** — Die Anforderung verlangt, dass der Bericht „an den Entwickler" per vorbefüllter E-Mail versendet wird. Der E-Mail-Entwurf wird jedoch mit der Empfängeradresse `debug@example.com` geöffnet (`public const string DebugReportRecipient = "debug@example.com"`, Zeile 23 — im Code ausdrücklich als Platzhalter markiert: „to be replaced by the maintainer"). Eine nicht-technische Anwenderin tippt auf „Senden", sieht einen formal korrekten Entwurf und schickt ihn ab — der Bericht erreicht niemals den Entwickler, und die Anwenderin kann das nicht erkennen oder korrigieren, weil sie die richtige Adresse nicht kennt. Die zentrale geforderte Aufgabe (Bericht zustellen) ist mit der gebauten Oberfläche faktisch nicht erledigbar.

  Empfehlung: Vor Auslieferung die tatsächliche Support-Adresse des Entwicklers als Konstante eintragen. Falls zum jetzigen Zeitpunkt keine echte Adresse existiert, muss dies als Blocker vor dem Release behandelt werden — alternativ die Adresse konfigurierbar machen.

### src/Reporter/Views/SettingsPage.xaml (Abschnitt „Diagnose & Support")

- **Erreichbarkeit** — Die Versand-Aktion ist deaktiviert und auf 40 % Opazität abgeblendet, solange der Schalter „Debuginformationen sammeln" aus ist (`IsEnabled="{Binding DebugSendEnabled}"`, wobei `DebugSendEnabled = DebugEmailSupported && DebugCollectionEnabled`). Da `DebugCollectionEnabled` per Default `false` ist, sieht eine Anwenderin im Auslieferungszustand einen ausgegrauten „Senden"-Button, ohne dass irgendwo erklärt wird, warum er nicht funktioniert oder dass zuerst der Schalter darüber aktiviert werden muss. Der Hint-Text „Öffnet einen E-Mail-Entwurf mit den Diagnosedaten zur Prüfung" beschreibt nur die Aktion, nicht die Voraussetzung. Typischer Fall: Der Support bittet die Anwenderin „schick uns bitte einen Debugbericht" — sie öffnet die Einstellungen, findet einen toten Button und kann die Aufgabe ohne Rückfrage nicht erledigen. Das Projekt hat dafür bereits ein etabliertes Muster: Im Benachrichtigungs-Abschnitt derselben Seite werden deaktivierte/verfügbare Zustände stets mit erklärenden Hinweis-Bordern versehen (z. B. `NotificationsIosOnlyHint`, `NotificationDeniedMessage` mit Aktionsbutton).

  Empfehlung: Entweder das Versenden unabhängig vom Schalter erlauben (der Bericht enthält ohnehin Sync-Verlauf, Einstellungen und Feed-Status) oder — analog zum Benachrichtigungs-Abschnitt — einen Hinweis-Border einblenden, solange `DebugCollectionEnabled == false`, etwa „Aktiviere zuerst ‚Debuginformationen sammeln', um einen Bericht senden zu können".

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Schalter „Debuginformationen sammeln" aktivieren/deaktivieren (Sektion „Diagnose & Support" auf der `SettingsPage`) → unauffällig (verständliches Label + erklärender Hint, persistiert sofort, Touch-Ziel ≥ 44 pt, `SemanticProperties`, lokalisiert de/en, Dark Mode via `AppThemeBinding`)
- Debugbericht als vorbefüllte E-Mail an den Entwickler senden (Button „Senden") → Befund vorhanden (Platzhalter-Empfänger `debug@example.com`; Button bei ausgeschalteter Sammlung deaktiviert ohne Erklärung)
- E-Mail-Entwurf vor dem Absenden selbst prüfen (kein Direktversand) → unauffällig (System-Mail-Client öffnet sichtbaren Entwurf; Hint-Text erklärt das Vorgehen)
- Reaktion der App, wenn kein Mail-Client verfügbar ist → unauffällig (Senden-Bereich deaktiviert + erklärender Hinweis-Border `SettingsDebugEmailUnsupportedHint`; bei Versandfehler lokalisierter `DisplayAlert` „Senden fehlgeschlagen")

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/Views/SettingsPage.xaml.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Services/DebugReportService.cs` (bestimmt Inhalt und Empfänger des vorbefüllten Entwurfs)
- `src/Reporter/Services/EmailService.cs`
- `src/Reporter.Core/Services/DebugLogService.cs`
- `src/Reporter/MauiProgram.cs` (DI-Registrierung der neuen Services)
