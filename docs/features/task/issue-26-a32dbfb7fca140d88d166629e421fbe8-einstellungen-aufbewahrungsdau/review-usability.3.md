# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### ArticleDetailViewModel.cs / ArticleDetailPage.xaml (Artikeldetailansicht)

- **Erreichbarkeit** — In der Artikeldetailansicht gibt es weiterhin den lokalen Schalter „Auto-Gelesen (X s)" (ArticleDetailPage.xaml, Zeilen 34–42). Seit diesem Branch wird er zusätzlich durch die neue globale Einstellung „Automatisch als gelesen markieren" gegattert (`AutoMarkReadMode != "off"` in `OnAutoMarkReadChanged`, Zeile 392, und beim Öffnen, Zeile 273). Hat eine Anwenderin die globale Option in den Einstellungen ausgeschaltet, lässt sich der lokale Schalter zwar noch sichtbar ein- und ausschalten, bewirkt aber schlicht nichts — ohne Hinweis, warum der Artikel nicht als gelesen markiert wird. Eine Laiin kann nicht wissen, dass eine zweite, übergeordnete Einstellung auf einer anderen Seite diesen Schalter neutralisiert.

  Empfehlung: Bei global deaktiviertem Auto-Gelesen den lokalen Schalter deaktiviert/abgedimmt darstellen oder den Label-Text um einen Hinweis ergänzen (z. B. „in den Einstellungen deaktiviert"), alternativ den Schalter in diesem Zustand ausblenden.

### SettingsPage.xaml (Einstellungen — Benachrichtigungen & Ruhezeiten)

- **Erreichbarkeit** — Der Schalter „Ruhezeit (Nicht stören)" verwirft beim Ausschalten stillschweigend die konfigurierten Zeiten: `QuietHoursEnabled = false` setzt `QuietHoursStart`/`QuietHoursEnd` auf `null` und persistiert sofort (SettingsViewModel.cs, Zeilen 280–284). Beim erneuten Einschalten erscheinen die festen Standardwerte 22:00–07:00, nicht die zuvor eingegebenen Zeiten. Eine Anwenderin, die ihre Ruhezeit z. B. auf 23:30–06:00 eingestellt hat und sie kurzzeitig pausiert, verliert ihre Konfiguration durch einen einzigen versehentlichen Tipp — ohne Rückfrage oder Warnung.

  Empfehlung: Beim Ausschalten die zuletzt gewählten Zeiten im ViewModel zwischenspeichern und beim erneuten Einschalten wiederherstellen (statt der festen Defaults), oder die Zeiten persistiert lassen und den Schalter nur als Aktiv-Flag führen.

### SettingsPage.xaml (Einstellungen — Keyword-Filter)

- **Erreichbarkeit** — Der Entfernen-Button in den Keyword-Chips besteht nur aus dem Zeichen „×" (SettingsPage.xaml, Zeilen 91–101) und hat kein `SemanticProperties.Description`. Sehende Nutzer erkennen die Funktion, Screenreader-Nutzer erhalten dagegen keinen sprechbaren Namen — anders als die übrigen Icon-Buttons der App (z. B. ArticleDetailPage.xaml mit `SemanticProperties.Description` auf allen Aktions-Buttons).

  Empfehlung: `SemanticProperties.Description` ergänzen, z. B. „Schlagwort {KeywordText} entfernen".

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:

- Einstellungsseite erreichen (Tab „Einstellungen" in der Shell-TabBar) → unauffällig
- Aufbewahrungsdauer per Schieberegler auf 1–365 Tage einstellen, Wert wird live als „X Tage" angezeigt, Speichern beim Loslassen → unauffällig
- Keyword-Filter hinzufügen (Freitext-Feld + „+ Hinzufügen"-Button bzw. Eingabetaste, Chips-Darstellung, Fehlermeldungen bei leer/Duplikat/zu lang) → unauffällig
- Keyword-Filter entfernen (×-Button pro Chip, 44×44 pt) → Befund vorhanden (fehlende Screenreader-Beschreibung; Bedienung selbst unauffällig)
- Keyword-Match-Verhalten „Teilwort & Case-Insensitive" als festen Status „Immer aktiv" ablesen → unauffällig
- Automatische Hintergrund-Aktualisierung ein-/ausschalten und Abruf-Intervall wählen (Picker mit Klartext „Alle 15 Minuten" bis „Alle 4 Stunden", Intervall-Auswahl wird bei ausgeschalteter Aktualisierung abgedimmt) → unauffällig
- „Automatisch als gelesen markieren" global ein-/ausschalten und Verzögerung wählen (Picker „Sofort" bis „5 Sekunden") → unauffällig
- Lokalen Auto-Gelesen-Schalter im Artikeldetail bei global deaktivierter Einstellung betätigen → Befund vorhanden (Schalter wirkt ohne Hinweis nicht)
- Push-Benachrichtigungen ein-/ausschalten → unauffällig (wird laut Anforderung in diesem Paket nur persistiert)
- Ruhezeit aktivieren und Von-/Bis-Zeit über TimePicker setzen → Befund vorhanden (Zeiten gehen beim Ausschalten verloren)
- Erscheinungsbild wählen (Picker „Farbschema": System/Hell/Dunkel, wirkt sofort) → unauffällig

Hinweis: Keine Stelle verlangt interne Kennungen (Ids, GUIDs, Dateinamen) oder eine Auswahl aus benannten Entitäten ohne Such-/Auswahlmöglichkeit — alle Eingaben sind Freitext (Schlagwort), Schieberegler, Schalter oder Auswahllisten mit Klartext.

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:

- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/Views/SettingsPage.xaml.cs`
- `src/Reporter/Views/ArticleDetailPage.xaml` (Kontext des lokalen Auto-Gelesen-Schalters)
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Core/ViewModels/ThemeOption.cs`
- `src/Reporter.Core/ViewModels/RefreshIntervalOption.cs`
- `src/Reporter.Core/ViewModels/AutoMarkReadDelayOption.cs`
- `src/Reporter.Core/Models/SettingsValues.cs`
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter/App.xaml.cs` (Theme-Anwendung beim Start)
- `src/Reporter.Core/Resources/Strings/AppResources.resx` / `AppResources.de.resx` (Beschriftungen)
- `design-draft/stitch_local_rss_feed_reader/einstellungen_filter/code.html` (Soll-Layout zum Abgleich)
