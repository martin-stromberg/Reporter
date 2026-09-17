<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Usability-Review

## Ergebnis

**Status:** Keine Befunde

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:

- Schalter „Debuginformationen sammeln" auf der `SettingsPage` aktivieren/deaktivieren (persistiert) → unauffällig. `Switch` mit Klartext-Label und Laien-Hinweis („Protokolliert Fehler und App-Ereignisse der aktuellen Sitzung (Absturz-Einträge der vorherigen Sitzung bleiben erhalten)"), 44 × 44 pt, `SemanticProperties.Description`; Umschalten wirkt sofort (`IDebugLogService.SetEnabled` in `SettingsViewModel.DebugCollectionEnabled`, Zeilen 452–468) und wird beim App-Start aus den persistierten Settings wiederhergestellt (`DebugLogService.BeginSessionAsync`, Zeilen 46–66). Keine interne Kennung oder technische Eingabe erforderlich.
- Versand-Aktion „Debugbericht senden" auslösen → unauffällig. Sichtbarer `Button` „Senden" unter der beschrifteten Zeile „Debugbericht senden" mit Hinweis „Öffnet einen E-Mail-Entwurf mit den Diagnosedaten der aktuellen Sitzung zur Prüfung" (`SettingsPage.xaml`, Zeilen 545–555). `SendDebugReportCommand` → `DebugReportService.SendReportAsync` → `EmailService.ComposeAsync` → `Email.ComposeAsync`: Der System-Mail-Client öffnet sich mit vorbefülltem Empfänger, Betreff und Body; die Anwenderin sieht den Entwurf und schickt ihn selbst ab — exakt wie gefordert. Kein Auswählen/Eintippen technischer Werte nötig (Empfänger ist eine dokumentierte Konstante).
- Fehlerfall „kein Mail-Client auf dem Gerät" → unauffällig. Zweistufig abgedeckt: (a) `DebugEmailSupported == false` deaktiviert den Sende-Bereich (`IsEnabled`-Kaskade + Opacity 0.4) und blendet den Hinweis „Auf diesem Gerät ist keine E-Mail-App verfügbar, daher kann kein Debugbericht gesendet werden." ein (`SettingsPage.xaml`, Zeilen 571–584); (b) schlägt `ComposeAsync` zur Laufzeit fehl, zeigt die Page via `DebugReportFailed`-Event einen lokalisierten `DisplayAlert` „Senden fehlgeschlagen" mit Handlungsanleitung (`SettingsPage.xaml.cs`, Zeilen 61–67; `SettingsViewModel.SendDebugReportAsync`, Zeilen 896–930).
- Verständnis des Zusammenhangs Schalter ↔ Versand → unauffällig. Bei ausgeschalteter Sammlung ist der Sende-Bereich sichtbar abgedunkelt/deaktiviert und ein Hinweis-Border erklärt in Klartext „Aktiviere zuerst ‚Debuginformationen sammeln', um einen Debugbericht senden zu können" — inklusive der Session-Erklärung, dass ein zu meldendes Problem ggf. in dieser Sitzung erneut auftreten muss (`SettingsPage.xaml`, Zeilen 557–570). Die entschiedene Session-Log-Semantik (Reset pro Start, Übernahme von Absturz-Einträgen) wird der Anwenderin damit transparent vermittelt.
- Transparenz über gesendete Daten → unauffällig. Der Bericht wird nicht im Hintergrund verschickt, sondern als sichtbarer Mail-Entwurf zur Prüfung übergeben; der Sende-Hinweis sagt das explizit („zur Prüfung").
- Erreichbarkeit/Beschriftung allgemein → unauffällig. Neuer Abschnitt „Diagnose & Support" folgt der bestehenden Karten-/Zeilen-Konvention (Section-Header `UiLabelStyle` + `Border`/`RoundRectangle 12`/`SurfaceCard`), Touch-Ziele ≥ 44 pt, `AppThemeBinding` für Dark Mode, alle Texte in `AppResources.resx` und `AppResources.de.resx` lokalisiert. Die Begriffe „Debuginformationen"/„Debugbericht" stammen wörtlich aus der Anforderung und werden durch die Klartext-Hinweise sowie den Section-Titel „Diagnose & Support" für Nicht-Techniker eingeordnet.

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:

- `src/Reporter/Views/SettingsPage.xaml` (neuer Abschnitt „Diagnose & Support", Zeilen 511–587)
- `src/Reporter/Views/SettingsPage.xaml.cs` (Fehlerdialog `OnDebugReportFailed`, Zeilen 61–67)
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs` (`DebugCollectionEnabled`, `DebugEmailSupported`, `DebugSendEnabled`, `SendDebugReportCommand`, `DebugReportFailed`-Event)
- `src/Reporter.Core/Resources/Strings/AppResources.resx` (neue `SettingsDebug*`/`DebugReport*`-Texte, en)
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx` (neue `SettingsDebug*`/`DebugReport*`-Texte, de)
- `src/Reporter.Core/Services/DebugReportService.cs` (Versand-Orchestrierung, Empfänger-Konstante, Body-Aufbau — soweit für den sichtbaren Mail-Entwurf relevant)
- `src/Reporter/Services/EmailService.cs` (`IsSupported`/`ComposeAsync` → System-Mail-Client)
- `src/Reporter.Core/Services/DebugLogService.cs` (`BeginSessionAsync`/`SetEnabled` — Schalter-Wirksamkeit)
- `src/Reporter/MauiProgram.cs` (DI-Registrierung der neuen Services, Zeilen 67–70)
- `src/Reporter/App.xaml.cs` (Session-Beginn des Debug-Logs beim App-Start)
