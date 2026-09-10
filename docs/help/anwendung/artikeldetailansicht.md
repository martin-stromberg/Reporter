← [Zurück zur Übersicht](index.md)

# Artikeldetailansicht

Tippe auf einen Artikel in der Liste (z. B. unter **Ungelesen** oder **Später**), um die Detailansicht zu öffnen. Hier wird der gespeicherte Volltext-HTML im WebView angezeigt, zusammen mit Titel, Feed-Name, Veröffentlichungsdatum und geschätzter Lesezeit.

## Bedienelemente

- **WebView**: Zeigt den Artikelinhalt. Du kannst innerhalb des Artikels scrollen. Der WebView passt sich automatisch an Light/Dark Mode an.
- **Status-Pille**: Zeigt an, ob der Artikel bereits gelesen wurde. Der Schalter **Auto-Gelesen** steuert, ob der Artikel nach einer kurzen Verzögerung automatisch als gelesen markiert wird. Deaktivierst du den Schalter, bleibt der Lesestatus unverändert.
- **Lesezeichen**: Tippe auf das Lesezeichen-Symbol in der unteren Leiste, um den Artikel für später zu merken bzw. die Markierung zu entfernen. Gespeicherte Artikel findest du unter **Später**.
- **Teilen**: Tippe auf das Teilen-Symbol, um den Artikellink oder den Titel zu teilen.
- **Im Browser öffnen**: Tippe auf das Browser-Symbol, um den Originalartikel in der externen Browser-App zu öffnen.
- **Schriftgröße/Lesemodus**: Sobald verfügbar, passt dieses Symbol die Schriftgröße im WebView an.
- **Zurück**: Tippe auf den Zurück-Pfeil in der unteren Leiste oder nutze die System-Navigation, um zur vorherigen Seite zurückzukehren.

## Lesestatus

Wenn **Auto-Gelesen** aktiv ist, wird der Artikel nach dem Öffnen automatisch als gelesen markiert. Sobald das Einstellungen-Arbeitspaket umgesetzt ist, kann die Verzögerung in den Einstellungen konfiguriert werden. Bis dahin gilt ein Fallback von fünf Sekunden, solange der Schalter eingeschaltet ist.

## Dark Mode

Die Detailansicht verwendet `AppThemeBinding` und ein angepasstes WebView-CSS. Wechselt das System in den Dark Mode, ändern sich die nativen UI-Elemente und der WebView-Inhalt entsprechend. Beachte: Externe Bilder im Artikelinhalt behalten ihre ursprüngliche Darstellung und werden nicht automatisch invertiert.

## Mobile UI-Prüfung

Die Artikeldetailansicht ist für mobile Bildschirme optimiert:

- Getestet für 390 × 844 pt (iPhone-Handygröße) und 768 × 1024 pt.
- Keine verschachtelten `CollectionView`/`ScrollView`; der `WebView` übernimmt das Scrollen.
- Alle tippbaren Elemente in der Floating Bottom Action Bar und der Status-Pille haben mindestens 44 × 44 pt Touch-Target.
- Layout als `Grid` mit `RowDefinitions="Auto,Auto,*,Auto,Auto"`, damit der `WebView` den verfügbaren Raum füllt.

Die Screenshot-Dokumentation der manuellen UI-Prüfung (Light/Dark, 390 × 844 pt) ist in `docs/help/anwendung/mobile-ui-design.md` vorgesehen.
