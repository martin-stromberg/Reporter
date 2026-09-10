# Usability-Review: Artikeldetailansicht mit WebView

## Geprüfte Dateien
- `src/Reporter/Views/ArticleDetailPage.xaml`
- `src/Reporter/Views/ArticleDetailPage.xaml.cs`
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter/Views/UnreadPage.xaml`
- `src/Reporter/Views/UnreadPage.xaml.cs`

## Fokus
Mobile 390 × 844 pt, Touch-Targets, Beschriftungen, Safe-Area, Accessibility, Gelesen/Lesezeichen/Teilen/Browser, Dark Mode.

## Bewertung

### 1. Mobile Layout (390 × 844 pt)

| Aspekt | Bewertung | Bemerkung |
|--------|-----------|-----------|
| Verwendung des verfügbaren Raums | OK | `Grid` mit `*` für `WebView`; Header und Action Bar sind `Auto`. |
| Keine horizontalen Datentabellen | OK | Es werden keine Tabellen verwendet. |
| Horizontale Scroll-Vermeidung | **Problem** | `FlexLayout` im Header kann bei langem Feed-Namen oder Lesezeit in eine zweite Zeile umbrechen, was akzeptabel ist. Die untere Action Bar hat 6 gleichgewichtete Spalten, die bei 390 pt Breite ca. 65 pt pro Zelle ergeben. |
| Text-Button im Footer | **Problem** | Der Button `Im Browser öffnen` im Source-Footer neben einer URL kann bei 390 pt in eine sehr schmale Darstellung geraten, weil `MinimumWidthRequest="44"` zwar das Mindestmaß sichert, aber der Text im schmalen Bereich umbrechen oder ellipsiert werden kann. |

### 2. Touch-Targets (mindestens 44 × 44 pt)

| Element | Größe | Bewertung |
|---------|-------|-----------|
| `Switch` Auto-Gelesen | `MinimumHeightRequest="44"`, `MinimumWidthRequest="44"` | OK |
| Zurück-Icon | `WidthRequest="44"`, `HeightRequest="44"` | OK |
| Lesezeichen-Icon | `WidthRequest="44"`, `HeightRequest="44"` | OK |
| Schriftgröße-Icon | `WidthRequest="44"`, `HeightRequest="44"` | OK |
| Gelesen-Icon | `WidthRequest="44"`, `HeightRequest="44"` | OK |
| Teilen-Icon | `WidthRequest="44"`, `HeightRequest="44"` | OK |
| Browser-Icon | `WidthRequest="44"`, `HeightRequest="44"` | OK |
| Button `Im Browser öffnen` | `MinimumHeightRequest="44"`, `MinimumWidthRequest="44"` | OK |
| Refresh/Filter/Mark-All-Read in `UnreadPage` | 44 × 44 bzw. `MinimumHeightRequest="44"` | OK |

**Anmerkung:** Die visuellen Icons sind teilweise nur 18–24 pt groß, die Hit-Box selbst ist aber 44 × 44 pt.

### 3. Beschriftungen & Icons

| Element | Bewertung | Bemerkung |
|---------|-----------|-----------|
| Feed-Name + Datum + Lesezeit | OK | Meta-Labels sind gut lesbar. |
| `Gelesen`-Pille | **Verbesserungspotenzial** | Wird nur angezeigt, wenn der Artikel gelesen ist. Nicht-gelesen hat keinen visuellen Status, was in Ordnung, aber die Pill-Position im oberen Bereich kann visuell mit dem `Switch` kollidieren. |
| Auto-Gelesen-Label | OK | Text `Auto-Gelesen (5 s)` ist kurz und verständlich. |
| `Im Browser öffnen` | OK | Textbutton ist klar beschriftet, erscheint aber an zwei Stellen (Source-Footer und Action Bar). |
| Schriftgrößen-Label | OK | `A` / `A+` ist als Toggle-Läbelelement erkennbar, aber nicht selbsterklärend für neue Nutzer. |
| Share-Icon | **Verbesserungspotenzial** | Das verwendete Path-Data (`M20,8 L30,16 L20,24 M30,16 L8,16`) sieht aus wie ein Pfeil, nicht wie das gängige Share-Symbol. Nutzer könnten die Bedeutung nicht sofort erkennen. |

### 4. Safe-Area

- `ArticleDetailPage.xaml` Zeile 8: `SafeAreaEdges="All"` ist gesetzt. Das ist korrekt für iOS-Notch/Dynamic Island.
- Die Bottom Action Bar verwendet `Padding="8,8,8,24"`. Die zusätzlichen 24 pt am unteren Rand können bei bestehendem Safe-Area-Inset zu viel unterem Leerraum führen, besonders auf Geräten mit Home-Indicator. **Empfehlung:** Auf reine Safe-Area-Insets zurückgreifen statt zusätzlichem Padding, um konsistentes Erscheinungsbild zu gewährleisten.
- `UnreadPage.xaml` hat keine Safe-Area-Einstellung. Da `Shell.NavBarIsVisible="False"` gesetzt ist, sollte zumindest das `Grid` oben und unten Safe-Area-Ränder respektieren. **Empfehlung:** `SafeAreaEdges="All"` ergänzen.

### 5. Accessibility

- `SemanticProperties.Description` ist an allen Action-Bar-Buttons in `ArticleDetailPage` gesetzt (Z. 152, 175, 204, 220, 249, 272). Das ist gut für Screenreader.
- **Verbesserungspotenzial:** Die Action-Items sind `Border`-Elemente mit `TapGestureRecognizer` anstelle von `Button`. `TapGestureRecognizer` wird von Screenreadern schlechter unterstützt als echte `Button`-Controls. **Empfehlung:** Wo möglich `Button` mit `ImageSource` verwenden oder `AutomationProperties.Name` ergänzen.
- Der `Switch` in der oberen Statusleiste hat kein `SemanticProperties.Description` und kein `AutomationProperties.Name`. Screenreader-Nutzer hören ggf. nur "Schalter, aus/an" ohne Kontext. **Empfehlung:** `SemanticProperties.Description="Artikel automatisch nach {_autoMarkReadDelaySeconds} Sekunden als gelesen markieren"` ergänzen.
- `ArticleDetailPage` Zeile 9: `Title` an `ContentPage` ist via Binding gesetzt (`Title="{Binding Item.Title, TargetNullValue='Artikel'}"`). Screenreader können das auslesen.
- `UnreadPage`: Die oberen Toolbar-Icons (`Border` mit `TapGestureRecognizer`) haben keine `SemanticProperties.Description`. **Empfehlung:** Beschreibungen ergänzen, um die Funktionen "Aktualisieren", "Filtern" und "Alle als gelesen markieren" zugänglich zu machen.

### 6. Gelesen / Lesezeichen / Teilen / Browser

| Funktion | Umgesetzt | Usability-Bemerkung |
|----------|-----------|---------------------|
| Gelesen-Markierung | Ja, via `ToggleMarkReadCommand` + Status-Pille | Der Haken-Icon-Button ändert die Farbe auf Primary, wenn gelesen. Gute visuelle Rückmeldung. |
| Lesezeichen | Ja, via `ToggleSavedForLaterCommand` | Füllt das Lesezeichen-Symbol in Gold, wenn aktiv. Die Beschriftung wechselt zwischen "Lesezeichen setzen" und "Lesezeichen entfernen". |
| Teilen | Ja, via `ShareCommand` | Funktion vorhanden, aber Icon ist ungewöhnlich (siehe 3. Beschriftungen). |
| Im Browser öffnen | Ja, im Source-Footer und in Action Bar | Doppelte Möglichkeit ist hilfreich, führt aber zu Redundanz. |

### 7. Dark Mode

- `AppThemeBinding` wird konsequent in `ArticleDetailPage.xaml` und `UnreadPage.xaml` verwendet (Hintergründe, Textfarben, Strokes).
- `ArticleDetailViewModel.cs` Zeile 342–360: Der WebView-Inhalt beachtet `prefers-color-scheme: dark` und setzt `color-scheme: light dark`. Farben und Links werden im Dark Mode angepasst.
- **Mögliches Problem:** Externe Bilder im Artikel-Inhalt (HTML) können helle Hintergründe haben und im Dark Mode hart kontrastieren. Das ist im Scope des WebView-Contents schwer vermeidbar, sollte aber im Handbuch erwähnt werden.
- `LightBookmarkGold` / `DarkBookmarkGold` und `LightStatusOk` / `DarkStatusOk` sind als Theming-Ressourcen vorausgesetzt. Eine Prüfung der Ressourcendefinitionen war nicht Teil dieser Dateien.

## Zusammenfassung der kritischen Punkte

1. **Share-Icon** ist nicht als Standard-Share-Symbol erkennbar.
2. **Action Bar** nutzt `Border` + `TapGestureRecognizer` statt `Button`, was die Accessibility reduziert.
3. **Safe Area** in `ArticleDetailPage` kombiniert internes Padding (24 pt unten) mit `SafeAreaEdges="All"`, was zu ungleichmäßigem unteren Abstand führen kann.
4. **UnreadPage** hat keine Safe-Area-Einstellung.
5. **Source-Footer-Button** `Im Browser öffnen` kann auf 390 pt in der Breite knapp werden.
6. Fehlende `SemanticProperties.Description` / `AutomationProperties.Name` am oberen `Switch` und den Toolbar-Icons in `UnreadPage`.

## Test-Empfehlungen

- Manuelle Überprüfung auf einem 390 × 844 pt iOS-Simulator oder einem entsprechend großen Android-Gerät.
- Screenreader-Test (iOS VoiceOver / Android TalkBack) für alle Icon-Buttons und den Auto-Gelesen-Schalter.
- Dark-Mode-Test in iOS- und Android-Systemeinstellungen prüfen, insbesondere Artikel mit Bildern.
