# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### SettingsPage.xaml (Einstellungen — Aufbewahrungsdauer)

- **Erreichbarkeit** — Die Aufbewahrungsdauer wird über einen Slider (1–365 Tage) eingestellt; gespeichert wird ausschließlich über `DragCompletedCommand`. Ändert eine Anwenderin den Wert ohne Ziehen — z. B. per Pfeiltasten auf dem Windows-Target oder über eine Maus-Einzelauswahl am Slider-Track, die kein Drag-Completed auslöst — springt der Slider zwar und die Anzeige „{0} Tage" aktualisiert sich, der neue Wert wird aber stillschweigend nicht persistiert. Beim nächsten Laden steht wieder der alte Wert. Die Anwenderin glaubt, die Einstellung sei übernommen, obwohl sie es nicht ist.

  Empfehlung: Persistierung zusätzlich an die fertige Wertänderung koppeln — z. B. `RetentionDays`-Setter beim Loslassen/Debounced-Persist (`PersistOnChange` nach kurzem Timeout) oder auf Windows den `ValueChanged`-Endstand gegen den gespeicherten Wert vergleichen und beim Verlassen der Seite speichern.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Aufbewahrungsdauer (RetentionDays 1–365) einstellen → Befund vorhanden (Persistierung nur bei DragCompleted)
- Keyword-Filter: Schlagwort hinzufügen (Freitext + „+ Hinzufügen", Return-Taste) → unauffällig
- Keyword-Filter: Schlagwort über ×-Chip entfernen → unauffällig
- Keyword-Match-Verhalten verstehen (fest, nicht konfigurierbar) → unauffällig („Teilwort, Groß-/Kleinschreibung egal" / „Immer aktiv")
- Automatische Hintergrund-Aktualisierung ein-/ausschalten und Intervall wählen → unauffällig
- „Automatisch als gelesen markieren" ein-/ausschalten und Verzögerung wählen → unauffällig
- Auto-Gelesen-Schalter in der Artikeldetailansicht bei global deaktivierter Option → unauffällig (Label „Auto-Gelesen (in den Einstellungen deaktiviert)" erklärt die Sperrung)
- Push-Benachrichtigungen ein-/ausschalten, Ruhezeit Von/Bis setzen → unauffällig
- Erscheinungsbild wählen (System/Hell/Dunkel, wirkt sofort) → unauffällig
- Einstellungen-Tab erreichen (Tab „Einstellungen" in der Tab-Leiste) → unauffällig

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/Views/SettingsPage.xaml.cs`
- `src/Reporter/Views/ArticleDetailPage.xaml`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Core/ViewModels/RefreshIntervalOption.cs`
- `src/Reporter.Core/ViewModels/AutoMarkReadDelayOption.cs`
- `src/Reporter.Core/ViewModels/ThemeOption.cs`
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter/AppShell.xaml.cs` (Erreichbarkeit des Einstellungen-Tabs)
