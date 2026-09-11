# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### SettingsPage.xaml / AppResources.de.resx (Einstellungen – Sektion Aufbewahrungsdauer)

- **Erreichbarkeit** — Der Hinweistext unter dem Aufbewahrungs-Slider lautet „Ungelesene und mit Sternchen markierte gespeicherte Artikel bleiben dauerhaft erhalten." (`SettingsRetentionInfo`, `src/Reporter.Core/Resources/Strings/AppResources.de.resx`, Zeile 204–206). In der App gibt es jedoch keinerlei Sternchen: Artikel werden über ein Lesezeichen-/Band-Symbol gemerkt (`ArticleCardView.xaml` Zeile 125, `ArticleDetailPage.xaml` Zeile 187), die Accessibility-Beschriftung lautet „Lesezeichen setzen/entfernen" und der zugehörige Tab heißt „Später". Eine nicht-technische Anwenderin, die wissen will, welche Artikel vor der Löschung geschützt sind, sucht vergeblich nach einer Sternchen-Funktion und kann den Hinweis nicht mit der tatsächlichen Bedienung in Verbindung bringen.

  Empfehlung: Formulierung an die tatsächliche App-Terminologie anpassen, z. B. „Ungelesene und mit Lesezeichen versehene Artikel (Tab „Später") bleiben dauerhaft erhalten."

### SettingsPage.xaml / AppResources.de.resx (Einstellungen – Sektion Keyword-Filter)

- **Erreichbarkeit** — Die Statuszeile zum festen Match-Verhalten trägt die Beschriftung „Teilwort & Case-Insensitive" (`SettingsKeywordMatchLabel`, `src/Reporter.Core/Resources/Strings/AppResources.de.resx`, Zeile 216–218; eingeblendet in `src/Reporter/Views/SettingsPage.xaml`, Zeile 120). „Case-Insensitive" ist englischer Fachjargon in einer sonst konsequent deutschen Oberfläche; eine Laiin kann daraus nicht ableiten, dass Groß-/Kleinschreibung beim Filtern keine Rolle spielt. Der Hinweis darunter („Erkennt auch Varianten innerhalb von Wörtern") erklärt nur den Teilwort-Aspekt, nicht die Case-Regel. Betroffen ist die Interaktion „Keyword-Filter pflegen", weil die Anwenderin das tatsächliche Verhalten des Filters nicht sicher einschätzen kann (z. B. ob sie „News" und „news" getrennt eintragen muss).

  Empfehlung: Beschriftung in Klartext formulieren, z. B. „Teilwort, Groß-/Kleinschreibung egal" bzw. Hinweistext zu „Erkennt Teilwörter und ignoriert Groß-/Kleinschreibung" erweitern.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Aufbewahrungsdauer für gelesene Artikel ändern (1–365 Tage) → unauffällig (Slider mit Live-Anzeige „X Tage", Markierungen, Sofort-Persistierung)
- Keyword-Filter hinzufügen → Befund vorhanden (Eingabefeld + „+ Hinzufügen"-Button + Return-Taste funktionieren; Dubletten-/Leer-/Längen-Fehlermeldungen lokalisiert; aber Match-Verhalten-Beschriftung unverständlich, siehe Befund 2)
- Keyword-Filter entfernen → unauffällig (Chips mit „×"-Button, 44×44 pt, Accessibility-Beschreibung „Schlagwort {0} entfernen")
- Automatische Hintergrund-Aktualisierung ein-/ausschalten und Intervall wählen → unauffällig (Switch + Picker mit Klartext-Optionen „Alle 15 Minuten" bis „Alle 4 Stunden"; deaktivierter Zustand ausgegraut)
- „Automatisch als gelesen markieren" ein-/ausschalten und Verzögerung wählen → unauffällig (Switch + Picker „Sofort/1/3/5 Sekunden")
- Push-Benachrichtigungen ein-/ausschalten → unauffällig (Switch mit Titel + Hinweistext)
- Ruhezeit ein-/ausschalten und Von/Bis-Zeiten festlegen → unauffällig (eigener Switch, TimePicker mit VON/BIS-Beschriftung, Zeiten über Mitternacht möglich, Standard 22:00–07:00)
- Erscheinungsbild wählen → unauffällig (Picker mit „System"/„Hell"/„Dunkel", wirkt sofort)
- Einstellungsseite erreichen → unauffällig (eigener Tab „Einstellungen" in der TabBar)
- Auto-Gelesen-Toggle in der Artikeldetailansicht bei global deaktivierter Option → unauffällig (Schalter deaktiviert und ausgegraut, Label „Auto-Gelesen (in den Einstellungen deaktiviert)" verweist auf den Ort der Aktivierung)
- Interne/technische Kennungen eingeben → unauffällig (keine Id-/GUID-/Schlüssel-Eingaben an keiner Stelle erforderlich)
- Auswahl aus benannten Mengen ohne Suche → unauffällig (alle Auswahlen sind kurze, feste Optionslisten mit Klartext; Keywords sind Freitext der Anwenderin, keine Auswahl aus bestehenden Entitäten)

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/Views/SettingsPage.xaml.cs`
- `src/Reporter/Views/ArticleDetailPage.xaml`
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs` (soweit Bedienelemente/Labels betroffen)
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Core/ViewModels/RefreshIntervalOption.cs`
- `src/Reporter.Core/ViewModels/AutoMarkReadDelayOption.cs`
- `src/Reporter.Core/ViewModels/ThemeOption.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter/App.xaml.cs` (Theme-Anwendung beim Start)
- `src/Reporter/AppShell.xaml.cs` (Erreichbarkeit der Seite über TabBar)
