# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### SettingsPage.xaml (Einstellungsseite)

- **Erreichbarkeit** — Die vier Schalter (Automatische Hintergrund-Aktualisierung, Automatisch als gelesen markieren, Push-Benachrichtigungen, Ruhezeit), die drei Picker (Abruf-Intervall, Verzögerung bis Markierung, Farbschema) und die beiden TimePicker (Ruhezeit VON/BIS) haben weder `MinimumHeightRequest`/`MinimumWidthRequest` von 44 pt noch eine `SemanticProperties.Description`. Die native Höhe eines `Switch`/`Picker` liegt auf Mobilgeräten typischerweise deutlich unter 44 pt — eine Anwenderin trifft das Touch-Ziel auf dem Handy nur unzuverlässig, und Screenreader geben die Schalter ohne Bezug zur Beschriftung wieder. Das Projekt wendet das 44-pt-Minimum bereits konsequent an (Slider Z. 38, Keyword-„×"-Button Z. 99–102, Switch in `ArticleDetailPage.xaml` Z. 41–42) — auf der SettingsPage fehlt es durchgehend.

  Empfehlung: Auf allen Switches, Pickern und TimePickern der Seite `MinimumHeightRequest="44"` (Switches zusätzlich `MinimumWidthRequest="44"`) sowie eine `SemanticProperties.Description` aus dem jeweiligen Label setzen — analog zum Switch in `ArticleDetailPage.xaml`. Alternativ die komplette Zeile (Label + Control) tappbar machen.

### SettingsViewModel.cs (Einstellungsseite, Aufbewahrungs-Slider)

- **Erreichbarkeit** — Anzeige-/Persistenzabweichung beim Slider „Gelesene Artikel aufbewahren": `FormatRetentionDays` (Z. 363–366) schneidet Nachkommastellen ab (`(int)days`), gespeichert wird aber gerundet (`Math.Round` in Z. 421 und 475). Da der Slider gebrochene Werte liefert, kann die Anzeige „30 Tage" zeigen, während die App tatsächlich 31 Tage speichert — die Beschriftung verspricht der Anwenderin einen anderen Wert als angewendet wird.

  Empfehlung: In `FormatRetentionDays` ebenfalls `Math.Round` verwenden, damit angezeigter und gespeicherter Wert immer übereinstimmen.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Aufbewahrungsdauer (1–365 Tage) per Slider einstellen → Befund vorhanden (Anzeige rundet abweichend vom gespeicherten Wert)
- Keyword-Filter: Schlagwort eingeben, hinzufügen, als Chip entfernen → unauffällig (Freitext-Eingabe mit Placeholder, „+ Hinzufügen"-Button und Return-Taste, „×"-Chips mit 44 pt und Screenreader-Label, Klartext-Fehlermeldungen bei leer/Dublette/zu lang; kein interner Schlüssel erforderlich)
- Automatische Hintergrund-Aktualisierung ein-/ausschalten und Intervall wählen → Befund vorhanden (Touch-Ziel/Barrierefreiheit von Switch und Picker; Klartext-Optionen „Alle 15 Minuten" usw. selbst unauffällig)
- „Automatisch als gelesen markieren" ein-/ausschalten und Verzögerung wählen → Befund vorhanden (Touch-Ziel/Barrierefreiheit von Switch und Picker)
- Push-Benachrichtigungen ein-/ausschalten und Ruhezeit (Von/Bis) konfigurieren → Befund vorhanden (Touch-Ziel/Barrierefreiheit von Switch und TimePickern; Dimmen bei deaktivierten Unteroptionen unauffällig)
- Erscheinungsbild wählen (System/Hell/Dunkel) → Befund vorhanden (Picker ohne Mindest-Touch-Höhe; Klartext-Optionen unauffällig)
- Im Artikel: lokaler Auto-Gelesen-Schalter bei global deaktivierter Einstellung → unauffällig (Schalter deaktiviert, gedimmt und mit Klartext-Hinweis „in den Einstellungen deaktiviert")

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/Views/SettingsPage.xaml.cs`
- `src/Reporter/Views/ArticleDetailPage.xaml`
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Core/ViewModels/RefreshIntervalOption.cs`
- `src/Reporter.Core/ViewModels/AutoMarkReadDelayOption.cs`
- `src/Reporter.Core/ViewModels/ThemeOption.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter/App.xaml.cs` (Theme-Anwendung/Auto-Refresh-Start)
- `src/Reporter/AppShell.xaml.cs` (Erreichbarkeit der Seite über Tab „Einstellungen")
