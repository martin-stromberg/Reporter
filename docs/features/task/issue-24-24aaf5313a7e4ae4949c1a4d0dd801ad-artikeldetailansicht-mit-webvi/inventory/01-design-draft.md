# Detaildokument: Design-Draft für die Artikeldetailansicht

## Lage
- `design-draft/stitch_local_rss_feed_reader/artikel_lesemodus/screen.png`
- `design-draft/stitch_local_rss_feed_reader/artikel_lesemodus/code.html`
- `design-draft/stitch_local_rss_feed_reader/artikel_lesemodus_dark_mode/screen.png`
- `design-draft/stitch_local_rss_feed_reader/artikel_lesemodus_dark_mode/code.html`

## Darstellung (aus code.html)
Der Design-Draft zeigt eine native mobile Leseseite mit folgenden Elementen:

1. **Status-Pille** oben:
   - Gelesen-Status (grüner Dot + "Gelesen").
   - Toggle "Auto-Gelesen (5s)".

2. **Artikel-Header**:
   - Feed-Icon, Feed-Name, Trennzeichen, Datum/Uhrzeit.
   - Geschätzte Lesezeit (z. B. "4 Min. Lesezeit").
   - Überschrift (`<h1>`), Untertitel, Autor/Tag-Leiste.

3. **Lead-Bild** mit Bildunterschrift.

4. **Artikel-Body**:
   - Serif-Body-Text (Newsreader), Zitate, Boxen, Zwischenüberschriften.
   - Schriftgrößen-Schalter (A / A+) im Floating-Bottom-Bar.

5. **Quellen-Footer**:
   - "Vollständiger Artikel verfügbar"-Karte mit Button zum Original.
   - Quellenangabe & Verweis.

6. **Floating Bottom Action Bar**:
   - Lesezeichen, Schriftgröße, Gelesen-Status, Teilen, "Im Browser öffnen".

7. **Dark-Mode-Variante**:
   - Dunkle Hintergründe, passende `surface`-Farben, Schriftfarben auf `#dfe2ee` etc.

## Relevanz für die Anforderung
- Detailseite mit Titel, Datum, Feed-Name und Link zur Quelle ist vorgegeben.
- WebView muss das gespeicherte `ContentHtml` in dieser Lesemodus-Optik darstellen.
- Floating Reader Control Bar ist im Draft explizit enthalten.
- Dark-Mode-Optik muss bei MAUI-Implementierung via `AppThemeBinding` umgesetzt werden.

## Offene Punkte
- Keine `.NET MAUI`-Entsprechung für die HTML/CSS-Fonts (Newsreader/Inter) definiert; muss ggf. im WebView-HTML nachgestellt werden.
- Lesezeit-Berechnung ist im Draft statisch, aktuell nicht in `Item`/`ItemListItem` vorhanden.
