# Usability-Review: Artikeldetailansicht mit WebView und Lesestatus

**Feature:** Artikeldetailansicht mit WebView und Lesestatus  
**Aufgaben-ID:** 24aaf531-3a7e-4ae4-949c-1a4d0dd801ad  
**Branch:** task/issue-24-24aaf5313a7e4ae4949c1a4d0dd801ad-artikeldetailansicht-mit-webvi  
**Geprüft aus Sicht:** Nicht-technische Endanwenderin, primär Mobile-Formfaktor

## Kurzbewertung

Die Artikeldetailansicht erfüllt die wichtigsten Anforderungen aus Endanwendersicht: Artikel werden im WebView lesbar dargestellt, der Gelesen-Status, Lesezeichen, Teilen und das Öffnen im Browser sind erreichbar, und der Dark Mode ist grundsätzlich berücksichtigt. Einige Details — besonders Beschriftungen, der Schutz vor Bedienfehlern auf kleinen Displays und die Mindestgröße einzelner Touch-Flächen — sollten vor dem Abschluss noch verbessert werden.

## Befund nach Usability-Kriterien

### 1. Lesefreundlichkeit — größtenteils gut, mit Ausbaufähigkeit

**Stärken**
- Der Artikelinhalt wird in einer serifenbetonten Leseschrift (`Newsreader`) mit ausreichender Zeilenhöhe (`line-height: 1.6`) und einem großen Basisschriftgrad (19 px / 22 px) gerendert. Das reduziert Ermüdung beim Lesen.
- Bilder werden auf `max-width: 100%` begrenzt, sodass sie kleinen Displays nicht überragen.
- Der Titel umbricht mehrzeilig (`MaxLines="0"`), lange Überschriften werden nicht einfach abgeschnitten.
- Zitate (`blockquote`) und Überschriften (`h1-h6`) haben eigene, konsistente Stile.

**Verbesserungspotenzial**
- Es gibt nur **zwei Schriftstufen** (`A` = 19 px, `A+` = 22 px). Der Design-Entwurf sieht drei fließende Stufen vor (17/19/21 px). Für Menschen mit leicht eingeschränkter Sicht wäre eine zusätzliche, größere Stufe hilfreich.
- **Keine Lesefortschrittsanzeige.** Der Design-Entwurf zeigt oberhalb des Artikels einen dünnen Fortschrittsbalken, der Orientierung beim Scrollen gibt. Dieser fehlt in der Implementierung vollständig.
- Der Header mit Feed-Name, Datum und Lesedauer sitzt in einem `HorizontalStackLayout` und wird nicht umgebrochen (`ArticleDetailPage.xaml`, Zeilen 41-67). Bei langen Feed-Namen auf 390 pt Displays kann die Zeile ihre maximale Breite überschreiten, anstatt sauber zu kürzen oder umzubrechen. Hier wäre ein flexibles Layout (z. B. `Grid` mit `*`/`Auto`-Spalten) besser.

### 2. Mobile Touch-Targets (mindestens 44 × 44 pt) — gemischt

**Passend**
- Die sechs Symbole in der schwebenden Leserleiste sind jeweils in einem 44 × 44 pt `Border` untergebracht (`ArticleDetailPage.xaml`, Zeilen 114-254). Das erfüllt die geforderte Mindestgröße.
- Der `Switch` für das Auto-Gelesen-Markieren hat `MinimumHeightRequest="44"` und `MinimumWidthRequest="44"` (`ArticleDetailPage.xaml`, Zeilen 31-33).
- Die Aktualisieren- und Filter-Symbole in der Ungelesen-Ansicht sind 44 × 44 pt (`UnreadPage.xaml`, Zeilen 21-59).

**Zu klein / unklar**
- Der Button **„Alle gelesen“** in der `UnreadPage` hat nur ein Padding von `12,8` und **kein** `HeightRequest`/`WidthRequest` (`UnreadPage.xaml`, Zeilen 70-96). Er dürfte deutlich unter 44 pt Höhe bleiben und ist damit am Smartphone schwer treffbar.
- Die Schaltfläche **„Öffnen“** im Quellen-Footer ist ein Standard-`Button` ohne `MinimumHeightRequest`/`MinimumWidthRequest` (`ArticleDetailPage.xaml`, Zeilen 105-109). Je nach Plattform-Stil kann er unter 44 pt liegen. Zusätzlich ist die Beschriftung sehr allgemein; der Endanwenderin ist nicht sofort klar, dass sie den **Originalartikel im Browser** öffnet.

### 3. Dark Mode — grundsätzlich gut

- In der XAML-UI werden fast alle Farben über `AppThemeBinding` zwischen `Light...` und `Dark...` Ressourcen umgeschaltet (`ArticleDetailPage.xaml`, u. a. Zeilen 19, 29-30, 89-90).
- Der WebView-Inhalt enthält `color-scheme: light dark` sowie eine `@media (prefers-color-scheme: dark)`-Regel (`ArticleDetailViewModel.cs`, Zeilen 241-259). Die Farbwahl (Hintergrund `#0f131c`, Text `#f1f5f9`, Links `#4edea3`) ist der Design-Vorgabe sehr nah.
- **Kleines Risiko:** Der WebView reagiert auf die **System**-Einstellung, nicht unbedingt auf eine eigene, in der App getroffene Theme-Auswahl. Wenn die App später ein separates Theme-Setting erhält, kann der Artikelinhalt im falschen Erscheinungsbild erscheinen. Für den Moment ist das akzeptabel.

### 4. Navigation — funktional, aber nicht optimal auffindbar

- Zurück-Navigation ist als Symbol in der **schwebenden Leserleiste** realisiert (`ArticleDetailPage.xaml`, Zeilen 118-139). Das funktioniert, weicht aber von der Design-Vorgabe ab, die einen deutlichen Pfeil in der oberen Leiste erwartet.
- `Shell.NavBarIsVisible="False"` (`ArticleDetailPage.xaml`, Zeile 7) entfernt die nativen Titel-/Zurück-Elemente. Am iOS-Gerät hilft die Wischgeste von links, am Android-Gerät die System-Zurück-Taste. Trotzdem sollte ein oben sichtbarer Zurück-Pfeil ergänzt oder zumindest der untere Pfeil besser beschriftet werden, damit die Anwenderin ihn sofort als „Zurück“ erkennt.
- Der Übergang von der Ungelesen-Ansicht zur Detailansicht funktioniert über `Shell.Current.GoToAsync($"articledetail?itemId={item.Id}")` (`UnreadPage.xaml.cs`, Zeile 74).

### 5. Gelesen-Status — logisch, aber Etikettierung schwach

- Der automatische Gelesen-Zähler (5 Sekunden) kann mit einem `Switch` an- und abgestellt werden (`ArticleDetailViewModel.cs`, Zeilen 296-317).
- Sobald der Artikel als gelesen gilt, erscheint die **„Gelesen“-Pille** (`ArticleDetailPage.xaml`, Zeilen 16-21).
- Der Häkchen-Button in der Leserleiste markiert Artikel manuell als gelesen (`ArticleDetailViewModel.cs`, Zeilen 330-340). Er **entmarkiert** sie jedoch nicht wieder — nach dem ersten Antippen ist der Status dauerhaft. Für eine Endanwenderin wirkt das eher wie eine Bestätigung denn wie ein Schalter.
- **Kritisch für Nicht-Technikerinnen:** Das Label neben dem Auto-Schalter lautet nur **„Auto“** (`ArticleDetailPage.xaml`, Zeile 25). Das ist unverständlich. Es sollte „Auto-Gelesen (5 s)“ oder „In 5 s als gelesen markieren“ heißen, wie im Design-Entwurf vorgesehen.

### 6. Lesezeichen (Für später bewahren) — sichtbar, aber ohne Rückmeldung

- Das Lesezeichen-Symbol in der schwebenden Leiste füllt sich gold, sobald der Artikel gespeichert ist (`ArticleDetailPage.xaml`, Zeilen 140-167; `ArticleDetailViewModel.cs`, Zeilen 319-328).
- Es fehlt jegliche textliche oder temporäre Rückmeldung (z. B. ein Toast oder eine kurze Meldung „Gespeichert für später“). Gerade für Farbblinde oder ältere Anwenderinnen ist reine Farbänderung unzureichend.

### 7. Teilen — korrekt angeschlossen

- Der Teilen-Button in der Leserleiste öffnet die native Teilen-Funktion mit Titel und Link (`ArticleDetailViewModel.cs`, Zeilen 352-364).
- Auch hier wäre eine kurze Rückmeldung wünschenswert, wenn der Teilen-Dialog erfolgreich geöffnet wird. Ein fehlender Link wird still ignoriert — in dem Fall sollte eine freundliche Meldung erscheinen.

### 8. Im Browser öffnen — doppelt vorhanden, aber ungleich verständlich

- Es gibt **zwei Einstiegspunkte:** die Welt/Monitor-Ikone in der schwebenden Leserleiste und der „Öffnen“-Button im Quellen-Footer (`ArticleDetailPage.xaml`, Zeilen 105-109 und 211-232; `ArticleDetailViewModel.cs`, Zeilen 342-350).
- Die untere Ikone ist **nicht selbsterklärend**: ein Endanwender ohne technischen Hintergrund erkennt am Symbol nicht sofort, dass der Originalartikel im Browser geöffnet wird.
- Der Quellen-Button „Öffnen“ ist besser auffindbar, aber die Beschriftung ist zu generisch. „Originalartikel öffnen“ oder „Im Browser lesen“ wäre klarer. Zudem sollte er mindestens 44 pt hoch sein.

## Weitere Auffälligkeiten

- **Keine Safe-Area-Berücksichtigung für die schwebende Leserleiste.** Die Leiste liegt am unteren Bildrand ohne zusätzlichen Abstand für die iOS-Home-Indikator-Geste (`ArticleDetailPage.xaml`, Zeile 114). Auf iPhones mit Face ID kann sie zu nah am Home-Bereich sitzen und schwerer zu treffen sein.
- **Kein Ladezustand / Empty-View.** Wenn der Artikel noch geladen wird, sieht die Anwenderin leere Felder, bis `LoadAsync` abgeschlossen ist. Ein kurzer Ladehinweis würde Unsicherheit vermeiden.

## Empfohlene Maßnahmen (priorisiert)

1. **Auto-Gelesen-Beschriftung korrigieren:** „Auto“ → „Auto-Gelesen (5 s)“ oder „In 5 s als gelesen markieren" (`ArticleDetailPage.xaml`, Zeile 25).
2. **Touch-Targets für Quellen-Button und „Alle gelesen“** auf mindestens 44 × 44 pt prüfen und ggf. `MinimumHeightRequest`/`MinimumWidthRequest` ergänzen (`ArticleDetailPage.xaml`, Zeile 105; `UnreadPage.xaml`, Zeile 70).
3. **Quellen-Button umbenennen:** „Öffnen“ → „Im Browser öffnen“ oder „Original lesen" (`ArticleDetailPage.xaml`, Zeile 106).
4. **Header-Layout flexibel machen:** Feed, Datum, Lesedauer sollten auf schmalen Displays umbrachen oder sauber kürzen (`ArticleDetailPage.xaml`, Zeilen 41-67).
5. **Safe-Area für die schwebende Leserleiste** berücksichtigen, z. B. durch plattformspezifisches Padding unten (`ArticleDetailPage.xaml`, Zeile 114).
6. **Rückmeldungen ergänzen:** Kurze Meldungen oder Toasts für „Gespeichert“, „Geteilt“ und „Als Gelesen markiert“ einführen.
7. **Lesefortschritt optional ergänzen**, falls das später innerhalb des Scopes des Features gewünscht ist.

## Gesamtfazit

Die Detailansicht ist aus Endanwendersicht **funktionsfähig und lesbar**, entspricht aber noch nicht vollständig der Klarheit und Zugänglichkeit des Design-Entwurfs. Die wichtigsten Korrekturen betreffen verständliche Beschriftungen (insbesondere „Auto“ und „Öffnen“), konsistente Touch-Target-Größen und ein etwas robusteres Header-Layout. Sobald diese Punkte angegangen sind, kann das Feature aus Sicht einer Nicht-Technikerin freigegeben werden.
