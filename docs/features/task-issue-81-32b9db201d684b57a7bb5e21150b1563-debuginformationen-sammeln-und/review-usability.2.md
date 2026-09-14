<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### DebugLogService.cs / SettingsPage.xaml (Diagnose-&-Support-Abschnitt)

- **Erreichbarkeit** — *Hauptszenario „Fehler melden" schlägt stillschweigend fehl:* Die Anforderung beschreibt, dass die Anwenderin gesammelte Debuginformationen per E-Mail an den Entwickler sendet. Die gebaute Logik löscht das Session-Debug-Log bei **jedem** App-Start (`DebugLogService.BeginSessionAsync` → `DeleteAllAsync`, `src/Reporter.Core/Services/DebugLogService.cs:48`) und schreibt nur Einträge, solange der Schalter aktiv ist (`LogAsync` ist No-op bei `IsEnabled == false`, Zeile 79). Für eine nicht-technische Anwenderin entsteht daraus ein nicht durchschaubares Verhalten:
  1. Sie erlebt ein Problem (z. B. Absturz, Sync-Fehler) und öffnet die Einstellungen. Der Senden-Button ist deaktiviert; der Hinweis fordert sie auf, zuerst „Debuginformationen sammeln" zu aktivieren.
  2. Sie aktiviert den Schalter und tippt „Senden" — der nun versendete Bericht enthält im Abschnitt „Session-Debug-Log" lediglich den Eintrag „Debug collection enabled". Der Fehler, den sie melden wollte, ist nicht enthalten.
  3. Noch gravierender: Nach einem Absturz, der die App beendet, wird der im `UnhandledException`-Handler (`App.xaml.cs`) noch geschriebene Eintrag beim nächsten Start sofort wieder gelöscht — ein Absturzbericht kann so **nie** versendet werden.
  
  Die UI kommuniziert an keiner Stelle, dass die Sammlung nur ab Aktivierung läuft und bei jedem App-Start zurückgesetzt wird. Die Anwenderin erhält weder den Hinweis „das Problem muss nach der Aktivierung erneut ausgelöst werden" noch eine Rückmeldung darüber, wie viele/ welche Einträge der Bericht enthält. Sie sendet also womöglich einen inhaltlich leeren Bericht und glaubt, dem Entwickler geholfen zu haben.
  
  Empfehlung: (a) Das Session-Log nicht beim Sitzungsstart löschen, sondern erst nach erfolgreichem Versand oder nach Ablauf einer Frist bereinigen, damit ein Absturz der vorherigen Sitzung meldbar bleibt; (b) den Hinweis-Text unter dem Schalter bzw. beim Senden-Bereich klarstellen, z. B. „Protokolliert Fehler ab jetzt — löse das Problem danach erneut aus, bevor du den Bericht sendest"; (c) optional beim Senden kurz anzeigen, wie viele Protokolleinträge enthalten sind, damit ein leerer Bericht auffällt.

### DebugReportService.cs (Empfängeradresse des Berichts)

- **Erreichbarkeit** — *Bericht geht an eine nicht existierende Adresse:* Der vorbefüllte E-Mail-Entwurf adressiert an `debug@example.com` (`src/Reporter.Core/Services/DebugReportService.cs:27`). Die Anwenderin sieht im Mail-Client zwar die Empfängeradresse, kann aber nicht erkennen, dass `example.com` kein echtes Postfach ist — sie würde den Bericht absenden und nie eine Antwort erhalten, ohne dass ihr oder dem Entwickler der Verlust auffällt.
  
  Hinweis zur Einordnung: Die Platzhalter-Adresse ist eine bewusst getroffene, im Code dokumentierte fachliche Entscheidung (`DebugReportRecipient`-Konstante mit Kommentar, dass der Maintainer sie vor Release ersetzt) und keine Implementierungsversehen. Der Befund dokumentiert daher ausschließlich das Restrisiko für den Fall, dass die Ersetzung vor Veröffentlichung vergessen wird.
  
  Empfehlung: Vor Release durch die reale Support-Adresse ersetzen (Release-Checkliste). Optional kann der Betreff-/Body-Text oder ein Kommentar am Empfänger-Feld nichts ändern — die einzig wirksame Maßnahme ist die termingerechte Ersetzung der Konstante.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Schalter „Debuginformationen sammeln" aktivieren/deaktivieren → unauffällig (Switch mit Label, verständlichem Hint und `SemanticProperties.Description`, Touch-Ziel ≥ 44 pt, Persistenz über `PersistOnChange`)
- Debugbericht per E-Mail an den Entwickler senden → **Befund vorhanden** (Platzhalter-Empfänger `debug@example.com`; Versand sperrt sich zusätzlich hinter dem Schalter, sodass der erste Bericht faktisch leer ist — siehe Befund 1)
- Fehlerfall „kein Mail-Client eingerichtet" verstehen → unauffällig (deaktivierter, abgedunkelter Senden-Bereich plus eingeblendeter Hinweis „Auf diesem Gerät ist keine E-Mail-App verfügbar …"; zusätzlich lokalisierter `DisplayAlert` bei fehlgeschlagenem Versand)
- E-Mail-Entwurf vor dem Absenden selbst prüfen → unauffällig (System-Mail-Client zeigt Empfänger, Betreff und Body; der Hint „Öffnet einen E-Mail-Entwurf mit den Diagnosedaten zur Prüfung" beschreibt den Ablauf korrekt)
- Keine internen Kennungen nötig → unauffällig (kein Id-/Schlüssel-Eingabefeld, alle Aktionen über Schalter und Schaltfläche mit Klartext-Beschriftung erreichbar)

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/Views/SettingsPage.xaml.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter/Services/EmailService.cs`
- `src/Reporter.Core/Services/DebugReportService.cs`
- `src/Reporter.Core/Services/DebugLogService.cs`
- `src/Reporter/App.xaml.cs` (nur Prüfung der Log-Lebenszyklus-Interaktion, keine eigene Oberfläche)
