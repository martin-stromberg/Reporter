<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### UnreadPage.xaml (Ungelesen-Übersicht / Kategoriefilter-Chips)

- **Erreichbarkeit** — Die Anforderung verlangt die Kategorieauswahl als sichtbare Chip-Leiste sowie Screenreader-Bedienbarkeit. Die Chips (Zeilen 108–174) tragen als `SemanticProperties.Description` nur den bloßen Kategorienamen (`{Binding Name}`). Eine Screenreader-Nutzerin hört eine Reihe von Namen („Alle", „Technik" …), erfährt aber weder, dass es sich um Filter-Schaltflächen handelt, noch welcher Chip gerade aktiv ist (`IsSelected` wird nur visuell über einen `DataTrigger` dargestellt), noch die Anzahl ungelesener Artikel (Count-Kapsel ist nicht Teil der Ansage). Der frühere `DisplayActionSheet`-Flow hatte wenigstens einen sprechenden Titel („Kategorie auswählen").

  Empfehlung: Beschreibung um Kontext und Zustand ergänzen, z. B. `SemanticProperties.Description` auf „{Name}, {Count} ungelesen" plus `SemanticProperties.Hint` „Filtert die Artikelliste" (neuer lokalisierter String) und den Auswahlzustand in die Beschreibung aufnehmen (z. B. „ausgewählt" via `DataTrigger` auf `IsSelected`).

### ArticleCardView.xaml (Artikelkarte auf Unread-/Later-Seiten)

- **Erreichbarkeit** — Das Karten-Tap-Overlay (Zeilen 191–202) trägt `SemanticProperties.Hint="{x:Static strings:AppResources.AccessibilityTapForActions}"` („Tippen für Aktionen" / „Double-tap for actions"). Tatsächlich öffnet der Tap über `OpenArticleCommand` die Artikel-Detailseite (`ArticleCardView.xaml.cs`, `OpenArticleAsync` → `Shell.Current.GoToAsync("articledetail…")`), kein Aktionsmenü. Eine Screenreader-Nutzerin wird auf eine nicht existierende Aktionen-Auswahl vorbereitet. (Auf `FeedsPage`/`CategoriesPage` ist derselbe Hint korrekt, weil dort wirklich ein ActionSheet aufgeht — auf der Artikelkarte ist er falsch übernommen.)

  Empfehlung: Eigenen Hint verwenden, z. B. „Öffnet den Artikel" (neuer String, z. B. `AccessibilityOpenArticle`), statt des Aktions-Hints.

### FeedsPage.xaml (Suchtreffer-Karte im Feed-Suchergebnis)

- **Erreichbarkeit** — Die Suchtreffer-Karte (Zeilen 70–77) nutzt `SemanticProperties.Description="{Binding FeedUrl}"`. Visuell wird als Überschrift der lesbare Feed-`Title` angezeigt, sobald vorhanden (`DataTrigger` Zeile 88 ff.); Screenreader-Nutzerinnen hören aber immer die technische URL, auch wenn ein sprechender Titel existiert. Damit wird ihnen eine technische Kennung vorgelesen, die sehende Anwender nicht sehen.

  Empfehlung: Beschreibung auf den Titel mit URL-Fallback legen, z. B. über `TargetNullValue`/`FallbackValue` auf `Title` oder einen formatierten Wert „{Title} ({FeedUrl})" aus dem ViewModel.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Kategoriefilter auf der Ungelesen-Seite über sichtbare Chips auswählen (inkl. Rückkehr zu „Alle") → Befund vorhanden (Screenreader: kein Zustand/Kontext)
- Artikel über Karten-Tap öffnen → Befund vorhanden (falscher Screenreader-Hint „Tippen für Aktionen")
- Artikel als gelesen markieren / für später merken (Karten-Buttons, Detail-Actionbar) → unauffällig (beschriftet, 44-pt-Ziele, Zustand wechselt Ansage)
- Feed-Gesundheitsstatus am Pill-Badge ablesen → unauffällig (Text-Label statt nur Farbpunkt)
- Reader-Aktionen über die schwebende Leiste (Zurück, Lesezeichen, Schriftgröße, Gelesen, Teilen, Browser) → unauffällig (alle icon-only Buttons mit `SemanticProperties.Description`)
- Feed hinzufügen (URL-Eingabe, Suche, Direkt-Hinzufügen mit Bestätigung) → unauffällig; Befund nur bei Screenreader-Ansage des Suchtreffers
- Kategorie anlegen/bearbeiten/löschen → unauffällig (Klartext-Name, ActionSheet mit lokalisierten Aktionen)
- Einstellungen ändern (Aufbewahrung, Keywords, Sync-Intervalle, Benachrichtigungen, Theme, Sprache) → unauffällig (alle Eingaben über Slider/Picker/Switch/TimePicker mit Klartext-Beschriftung, keine technischen Kennungen)
- Navigation über Tab-Leiste → unauffällig (Icons + lokalisierte Titel)

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `src/Reporter/Views/UnreadPage.xaml` (+ `UnreadPage.xaml.cs`)
- `src/Reporter/Views/ArticleCardView.xaml` (+ `ArticleCardView.xaml.cs`)
- `src/Reporter/Views/ArticleDetailPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml` (+ `FeedsPage.xaml.cs` zur Prüfung der Tap-Aktionen)
- `src/Reporter/Views/LaterPage.xaml`
- `src/Reporter/Views/CategoriesPage.xaml` (+ `CategoriesPage.xaml.cs`)
- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/AppShell.xaml.cs` (Tab-Leiste)
- `src/Reporter/Resources/Styles/Styles.xaml` (neue Typo-Styles)
- `src/Reporter/Resources/Styles/Colors.xaml` (Status-Tints, Chip-/Badge-Farben)
- `src/Reporter.Core/ViewModels/UnreadViewModel.cs` (`Categories`, `SelectCategoryCommand`)
- `src/Reporter.Core/ViewModels/LaterViewModel.cs` (Paging)
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs` (Actionbar-Labels)
- `src/Reporter.Core/Resources/Strings/AppResources.resx` / `AppResources.de.resx` (neue Accessibility-Strings)
