# Usability-Review: Artikeldetailansicht mit WebView und Lesestatus

**Geprüfte Dateien**

- `src/Reporter/Views/ArticleDetailPage.xaml`
- `src/Reporter/Views/ArticleDetailPage.xaml.cs`
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter/Views/UnreadPage.xaml`

**Bezug:** `requirement.md` (gelesen) – `plan.md`, `review.md`, `review-code.md` wurden nicht gelesen.

---

## 1. Beschriftungen

### 1.1 "Auto-Gelesen (5 s)"

- **VM**, `ArticleDetailViewModel.cs`, Zeile 31: Default-Wert `"Auto-Gelesen (5 s)"`.
- **VM**, Zeile 222: Wird zur Laufzeit aus der Verzögerungseinstellung gebildet: `AutoMarkReadLabel = $"Auto-Gelesen ({_autoMarkReadDelaySeconds} s)"`.
- **XAML**, `ArticleDetailPage.xaml`, Zeile 33: `Label Text="{Binding AutoMarkReadLabel}"`.

**Bewertung:** Die Beschriftung ist verständlich, aber hartkodiert und nicht lokalisierbar. Die Schreibweise "5 s" entspricht der deutschen SI-Konvention, wirkt aber im UI etwas technisch. Für Nicht-Techniker wäre "Auto: Gelesen (5 Sek.)" oder "Nach 5 s als gelesen markieren" sprechender.

### 1.2 "Im Browser öffnen"

- **XAML**, `ArticleDetailPage.xaml`, Zeile 130: `Button Text="Im Browser öffnen"`.

**Bewertung:** Der Text ist eindeutig. Allerdings ist er hartkodiert (`Button`-Text statt Ressource). Bei kleineren Displays (z. B. 390 pt Breite) kann der Button schnell umbrechen oder abgeschnitten wirken, weil er im rechten Teil der Quellen-Card sitzt (Zeile 115–137).

**Empfehlung:** Beide Labels in String-Ressourcen auslagern und Laufzeit-Plural/Format besser steuern.

---

## 2. Touch-Targets 44 × 44 pt

### 2.1 Artikeldetailansicht

- **XAML**, `ArticleDetailPage.xaml`, Zeile 36–41: `Switch` hat `MinimumHeightRequest="44"` und `MinimumWidthRequest="44"` – erfüllt die 44-pt-Regel.
- Zeile 129–135: `Button` "Im Browser öffnen" hat ebenfalls `MinimumHeightRequest="44"` / `MinimumWidthRequest="44"`.
- Zeile 144–280: Sechs `Border`-Elemente in der unteren Action Bar mit `WidthRequest="44"` und `HeightRequest="44"` (z. B. Zeile 145–146, 167–168, 195–196, 210–211, 238–239, 260–261). Jedes enthält einen `TapGestureRecognizer`, sodass die hit area exakt 44 × 44 pt ist.
- Innerhalb der `Border` liegen die Icons (`Path`) 18–24 pt, also komfortabel kleiner als der Tappable-Bereich.

**Bewertung:** Die primären Touch-Targets erfüllen die 44-pt-Vorgabe. Kritisch: Die `Border` in den sechs Spalten berühren sich ohne `ColumnSpacing` in der Action-Bar (Zeile 142: `ColumnDefinitions="*,*,*,*,*,*"`). Bei 44-pt-Elementen in sechs Spalten auf 390 pt bleibt etwas Luft, aber auf noch kleineren Displays oder bei größeren Accessibility-Texten kann das zu Fehltippen führen.

### 2.2 UnreadPage

- **XAML**, `UnreadPage.xaml`, Zeile 21–41: Refresh- und Filter-Icons in `Border` 44 × 44.
- Zeile 70–98: "Alles gelesen"-Button als `Border` mit `MinimumHeightRequest="44"` und `MinimumWidthRequest="44"`.

**Bewertung:** Auch hier sind die Touch-Targets korrekt.

---

## 3. Header-Layout

- **XAML**, `ArticleDetailPage.xaml`, Zeile 45–96: Der Header ist ein `VerticalStackLayout` mit Padding `16,8`.
- Zeile 49–91: Metadaten (Feed-Icon, Punkt, Feed-Name, Punkt, Datum, Punkt, Lesezeit) sind in einem `FlexLayout` mit `Wrap="Wrap"` und `Direction="Row"`. Das ist mobilfreundlich, weil es bei kleinem Viewport umbricht, anstatt horizontal abzuschneiden.
- Zeile 92–95: Der Titel ist als `Label` mit `WordWrap` und `MaxLines="0"` umgesetzt.

**Bewertung:** Das Header-Layout ist prinzipiell robust. Verbesserungspotenzial:

- `FlexLayout` enthält mehrere hartkodierte Textelemente (Bullet `•`), die bei eingestellter großer Schriftart nicht zentriert aussehen können.
- Keine visuelle Trennlinie zwischen Header und WebView; das kann auf Dichte-Inhalten hart wirken.

---

## 4. Safe-Area

- **XAML**, `ArticleDetailPage.xaml`, Zeile 7: `Shell.NavBarIsVisible="False"`.
- **XAML**, `ArticleDetailPage.xaml`, Zeile 9: `Padding="0"` am `Grid`.
- **XAML**, `ArticleDetailPage.xaml`, Zeile 13–43: Status-Pill mit `Padding="16,12,16,0"`.
- **XAML**, `ArticleDetailPage.xaml`, Zeile 140–141: Action Bar mit `Padding="8,8,8,24"`.
- **XAML**, `UnreadPage.xaml`, Zeile 11: `Padding="16,8"`.
- **XAML**, `UnreadPage.xaml`, Zeile 10: `Shell.NavBarIsVisible="False"`.

**Bewertung:** Auf iOS fehlt expliziter Safe-Area-Schutz. Bei `Shell.NavBarIsVisible="False"` muss `ios:Page.UseSafeArea="True"` oder eine plattformspezifische Padding-Logik gesetzt werden, damit der Notch-/Status-Bar-Bereich sowie der Home-Indicator-Bereich nicht überdeckt werden. Die händisch gesetzten unteren `24 pt` reichen nicht für alle Geräte und Orientierungen.

**Empfehlung:**

```xml
xmlns:ios="clr-namespace:Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;assembly=Microsoft.Maui.Controls"
ios:Page.UseSafeArea="True"
```

---

## 5. Gelesen-Status-Erkennbarkeit

- **XAML**, `ArticleDetailPage.xaml`, Zeile 19–29: Oben links erscheint ein Status-Pill mit grünem Dot und dem Label "Gelesen", sobald `Item.IsRead == true`.
- **VM**, `ArticleDetailViewModel.cs`, Zeile 373: `Item = CreateItemCopy(isRead: true, ...)` aktualisiert den Status.
- **XAML**, `ArticleDetailPage.xaml`, Zeile 230–234: Der Haken-Icon in der Action Bar ändert bei `IsRead == true` seine `Stroke`-Farbe zu Primary.
- **XAML**, `ArticleDetailPage.xaml`, Zeile 219–235: Der manuelle "Gelesen"-Button in der Action Bar.

**Bewertung:** Der Status ist an zwei Stellen erkennbar: oben als Text-Pill und unten als farbiger Haken. Das ist gut. Allerdings:

- Beide Indikatoren sind eher dezent. Eine zusätzliche, barrierefreie Änderung (z. B. `AutomationProperties.Name` für Screen Reader oder ein kurzes visuelles Highlight) würde die Rückmeldung verbessern.
- Die Pill erscheint nur bei `IsRead == true`; solange der Artikel ungelesen ist, ist oben links kein visueller Indikator. Das ist beabsichtigt, kann aber den Unterschied zwischen "wird bald als gelesen markiert" und "noch nicht" schwer erkennbar machen.

---

## 6. Lesezeichen-Rückmeldung

- **XAML**, `ArticleDetailPage.xaml`, Zeile 176–193: Das Lesezeichen-Icon (`Path` mit Bookmark-Shape) erhält über einen `DataTrigger` bei `Item.IsSavedForLater == true` eine goldene Füllfarbe (`LightBookmarkGold` / `DarkBookmarkGold`).
- **VM**, `ArticleDetailViewModel.cs`, Zeile 385–394: `ToggleSavedForLaterAsync` aktualisiert `is_saved_for_later` und erstellt eine neue Item-Kopie.

**Bewertung:** Die visuelle Rückmeldung funktioniert und unterstützt beide Theme-Varianten. Aber:

- Es gibt keine nicht-visuelle Rückmeldung (z. B. Haptic Feedback oder Announcement) für Screen-Reader-Nutzer.
- Der `Border` bzw. das `Path` besitzt kein `AutomationProperties.Name`/`HelpText` – Screen Reader wissen nicht, um welchen Zustand es sich handelt und was der Button tut.
- Der Farbwechsel auf Gold ist stimmig, aber im Light Mode bei hellen Themes sollte der Kontrast geprüft werden.

**Empfehlung:**

```xml
<Border ...>
    <Border.GestureRecognizers>
        <TapGestureRecognizer Command="{Binding ToggleSavedForLaterCommand}" />
    </Border.GestureRecognizers>
    <Border.AutomationProperties>
        <AutomationProperties.Name>
            <MultiBinding StringFormat="{}Lesezeichen: {0}">
                <Binding Path="Item.IsSavedForLater" />
            </MultiBinding>
        </AutomationProperties.Name>
    </Border.AutomationProperties>
    ...
</Border>
```

(oder eine vereinfachte `AutomationProperties.Name` dynamisch im VM setzen).

---

## Zusammenfassung und Empfohlene Maßnahmen

| Bereich | Status | Maßnahme |
|---------|--------|----------|
| Touch-Targets 44 × 44 pt | Erfüllt | Spacing in Action Bar erhöhen, um Fehltippen zu reduzieren |
| Beschriftungen "Auto-Gelesen (5 s)" / "Im Browser öffnen" | Teilweise | In lokalisierbare Ressourcen auslagern, ggf. Text kürzen |
| Header-Layout | Erfüllt | Bullet-Trenner durch marginbasierte Trennung ersetzen |
| Safe-Area | Nicht erfüllt | `ios:Page.UseSafeArea="True"` setzen |
| Gelesen-Status-Erkennbarkeit | Erfüllt | `AutomationProperties.Name` für Screen Reader ergänzen |
| Lesezeichen-Rückmeldung | Erfüllt visuell | Zugänglichkeit (Name, Haptic) ergänzen |

**Gesamteinschätzung:** Die Detailansicht ist mobil nutzbar und die wichtigsten Interaktionsflächen erfüllen die 44-pt-Vorgabe. Vor dem finalen Commit sollte die Safe-Area auf iOS korrigiert und die hartkodierten Texte / Screen-Reader-Labels ergänzt werden.
