# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### SettingsPage.xaml (Einstellungsseite — Sektion „Benachrichtigungen & Ruhezeiten")

- **Erreichbarkeit** — Die Anforderung sieht `QuietHoursStart`/`QuietHoursEnd` als nullable Felder vor — „keine Ruhezeit" ist also ein fachlich vorgesehener, sinnvoller Zustand. Die Oberfläche bietet dafür zwei `TimePicker` (VON/BIS, Zeilen 244–267), die jedoch immer eine Uhrzeit anzeigen und keinerlei Möglichkeit bieten, eine einmal eingestellte Ruhezeit wieder zu entfernen. Sobald eine Anwenderin — auch nur versehentlich — eine Zeit antippt, ist die Ruhezeit dauerhaft gesetzt; der einzige „Ausweg" wäre, Push-Benachrichtigungen komplett zu deaktivieren, was aber zugleich alle Benachrichtigungen abschaltet. Eine Laiin kann den Ausgangszustand „keine Ruhezeit" nicht wiederherstellen.

  Empfehlung: Expliziten Ein/Aus-Schalter „Ruhezeit aktivieren" vor die VON/BIS-Auswahl setzen (analog zum etablierten Muster Intervall-Picker hinter „Automatische Hintergrund-Aktualisierung"), oder eine „Zurücksetzen"-Aktion ergänzen, die `QuietHoursStart`/`QuietHoursEnd` wieder auf `null` setzt.

### SettingsPage.xaml (Einstellungsseite — Sektion „Keyword-Filter")

- **Erreichbarkeit** — Der Schalter „Teilwort & Case-Insensitive" (Zeilen 123–126) ist als `IsToggled="True"` + `IsEnabled="False"` gerendert. Er sieht aus wie ein bedienbares Steuerelement, reagiert aber nicht. Eine nicht-technische Anwenderin, die das Verhalten ändern möchte oder den Schalter versehentlich antippt, erhält keinerlei Rückmeldung und kann die Seite für defekt halten — aus ihr geht nicht hervor, *warum* der Schalter gesperrt ist.

  Empfehlung: Das feste Verhalten nicht als gesperrten Schalter, sondern als Status darstellen — z. B. denselben Info-Stil wie die Hinweis-Box darüber verwenden (Textzeile/Badge „Immer aktiv: erkennt auch Varianten innerhalb von Wörtern") oder den Hinweistext um eine Begründung ergänzen.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Aufbewahrungsdauer (1–365 Tage) einstellen → unauffällig (Slider mit Wertanzeige „X Tage", Bereich erzwungen, Klartext-Hinweis zu den Lösch-Invarianten)
- Keyword/Schlagwort hinzufügen → unauffällig (Freitext-Eingabe mit Placeholder „Schlagwort eingeben…", Button „+ Hinzufügen", Return-Taste; keine internen Kennungen nötig)
- Keyword entfernen → unauffällig (Chip mit ×-Button, 44×44 pt Touch-Target)
- Fehleingaben verstehen (leer, Dublette, zu lang) → unauffällig (lokalisierte Klartext-Fehlermeldungen in Rot)
- Keyword-Match-Verhalten nachvollziehen → Befund vorhanden (gesperrter Schalter wirkt wie defektes Bedienelement)
- Automatische Hintergrund-Aktualisierung ein-/ausschalten → unauffällig (beschrifteter Switch mit Hinweistext)
- Abruf-Intervall wählen → unauffällig (Picker mit Klartext-Optionen „Alle 15 Minuten" bis „Alle 4 Stunden", abgedunkelt/deaktiviert wenn Schalter aus)
- „Automatisch als gelesen markieren" ein-/ausschalten → unauffällig (Switch mit Hinweis „Beim Öffnen eines Artikels")
- Verzögerung bis Markierung wählen → unauffällig (Picker mit Klartext „Sofort"/„1 Sekunde"/„3 Sekunden"/„5 Sekunden")
- Push-Benachrichtigungen ein-/ausschalten → unauffällig
- Ruhezeiten (VON/BIS) einstellen → Befund vorhanden (einstellbar, aber nicht rücksetzbar auf „keine Ruhezeit")
- Erscheinungsbild wählen → unauffällig (Picker „System"/„Hell"/„Dunkel", wirkt sofort über `AppThemeBinding`/`UserAppTheme`)
- Einstellungsseite erreichen → unauffällig (eigener Tab „Einstellungen" in der Shell-TabBar)

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/Views/SettingsPage.xaml.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Core/ViewModels/RefreshIntervalOption.cs`
- `src/Reporter.Core/ViewModels/AutoMarkReadDelayOption.cs`
- `src/Reporter.Core/ViewModels/ThemeOption.cs`
- `src/Reporter/Services/AppThemeService.cs`
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs` (Diff: Auswertung der globalen Auto-Gelesen-Einstellung)
- `src/Reporter/App.xaml.cs` (Diff: Theme beim Start, Auto-Refresh-Start)
- `src/Reporter/AppShell.xaml.cs` (Erreichbarkeit der Seite als Tab)
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx` / `AppResources.resx` (Beschriftungen)

Zusätzliche Prüfgrundlagen: Design-Entwurf `design-draft/stitch_local_rss_feed_reader/einstellungen_filter/code.html` sowie manuelle Verifikations-Screenshots `test-results/issue-26-manual-*.png`. Mobile-Vorgaben aus `AGENTS.md` erfüllt: keine horizontalen Tabellen, keine verschachtelten `CollectionView`/`ScrollView`, Touch-Targets ≥ 44 pt, durchgehend `AppThemeBinding`.
