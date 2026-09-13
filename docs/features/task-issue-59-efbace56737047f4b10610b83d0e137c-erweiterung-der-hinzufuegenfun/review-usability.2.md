<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedsPage.xaml (FeedsPage — Hinzufügen-/Bearbeiten-Sheet)

- **Erreichbarkeit** — Im Bearbeiten-Modus („Feed bearbeiten") wird der Offline-Hinweis im Sheet (`FeedSearchOfflineHint`, XAML Zeilen 264–273) weiterhin eingeblendet, sobald das Gerät offline ist: „Feed-Suche offline nicht verfügbar — URL direkt hinzufügen bleibt möglich." Gleichzeitig sind in diesem Modus sowohl „Suchen" als auch „URL direkt hinzufügen" per `IsEditMode`-Trigger ausgeblendet (XAML Zeilen 282–297). Eine nicht-technische Anwenderin, die offline einen Feed bearbeitet, liest also einen Hinweis, der eine Aktion verspricht, die im sichtbaren Dialog gar nicht angeboten wird — sie sucht einen Button, den es dort nicht gibt.

  Empfehlung: Den Offline-Hinweis im Sheet ebenfalls an den Modus koppeln (z. B. zusätzlicher `DataTrigger` auf `IsEditMode` bzw. kombiniertes ViewModel-Flag wie `ShowSearchHints`), sodass er nur im Hinzufügen-Modus erscheint, oder einen modusneutralen Hinweistext verwenden.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:

- „+"-Button „Feed per URL hinzufügen" öffnet das Hinzufügen-Formular als Bottom-Sheet (volle Breite über der Liste, Klartext-Beschriftung, ≥ 44 pt) → unauffällig
- Sheet schließen über „Abbrechen", Tippen auf den abgedunkelten Hintergrund und Zurück-Taste → unauffällig
- Feed-URL/Website-Adresse eintippen und per „Suchen" bzw. Eingabetaste suchen (Placeholder „Feed-URL oder Website-Adresse…", URL-Tastatur) → unauffällig
- Suchergebnisse als Liste antippen und per Ja/Nein-Dialog „„{Titel}" abonnieren?" hinzufügen; „Zurück zu meinen Feeds" → unauffällig
- URL direkt hinzufügen ohne Suche über „URL direkt hinzufügen" (auch offline aktiv, Dubletten- und URL-Fehler in Klartext im Sheet) → unauffällig
- Offline-Verhalten: „Suchen" deaktiviert, „URL direkt hinzufügen" bleibt aktiv, Hinweistext im Sheet erklärt das → unauffällig (Ausnahme: Bearbeiten-Modus, siehe Befund)
- Titelloser Feed bekommt Dateinamen/Host der URL als Namen, der beim ersten Sync durch den echten Feed-Titel ersetzt wird → unauffällig (kein Anwender-Eingriff nötig)
- Feed antippen → Kontextmenü „Feed-Aktionen" mit Klartext-Einträgen „Aktualisieren", „Umbenennen", „Kategorie ändern", „Bearbeiten", „Löschen" → unauffällig
- „Umbenennen" öffnet Eingabedialog mit vorbefülltem aktuellem Titel, OK/Abbrechen → unauffällig
- „Kategorie ändern" öffnet Auswahl über Kategorienamen inkl. Klartext-Pseudoeintrag „Keine Kategorie" / „No category" (keine IDs, keine technischen Werte) → unauffällig
- „Bearbeiten" öffnet dasselbe Sheet im Edit-Modus: Titel „Feed bearbeiten", URL-Feld vorbefüllt, Benachrichtigungen-Schalter mit Klartext-Label/-Hinweis, „Speichern"; „Suchen"/„URL direkt hinzufügen" ausgeblendet → unauffällig
- Keine internen Kennungen erforderlich: Sämtliche Auswahlen erfolgen über Namen/Suche, keine GUID-/Id-Eingabe an irgendeiner Stelle → unauffällig

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:

- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs` (bedienrelevante Zustände/Commands)
- `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs` (bedienrelevante Zustände/Commands)
- `src/Reporter.Core/Resources/Strings/AppResources.resx` (Beschriftungen EN)
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx` (Beschriftungen DE)
- `src/Reporter.Core/Services/FeedTitleFallback.cs` (Titel-Fallback-Logik, indirekt sichtbar)

Manuelle Verifikation (Screenshots vorhanden, nicht ausgewertet — Review aus Code/Anforderung): `test-results/issue-59/manual-2-*.png`, `manual-3-*.png`, `manual-4-*.png`.
