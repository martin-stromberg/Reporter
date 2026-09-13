<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Artikeldetailansicht

Tippe auf einen Artikel in der Liste (z. B. unter **Ungelesen** oder **Später**), um die Detailansicht zu öffnen. Hier wird der gespeicherte Volltext im Lesefenster angezeigt, zusammen mit Titel, Feed-Name (mit dem Favicon des Feeds, sofern bekannt), Veröffentlichungsdatum und geschätzter Lesezeit — bei sehr kurzen Artikeln von höchstens einer Minute Lesezeit entfällt die Lesezeit-Angabe.

## Bedienelemente

- **Artikelbereich**: Zeigt den Artikelinhalt. Du kannst innerhalb des Artikels scrollen; die Darstellung folgt dem Farbschema des Geräts.
- **Status-Pille**: Zeigt an, ob der Artikel bereits gelesen wurde. Der Schalter **Auto-Gelesen** steuert, ob der Artikel nach einer kurzen Verzögerung automatisch als gelesen markiert wird. Deaktivierst du den Schalter, bleibt der Lesestatus unverändert. Ist die automatische Markierung in den **Einstellungen** ausgeschaltet, ist der Schalter abgedunkelt und nicht bedienbar; die Beschriftung lautet dann *Auto-Gelesen (in den Einstellungen deaktiviert)*.
- **Schwebende Aktionsleiste**: Am unteren Rand schwebt eine abgerundete, leicht durchscheinende Leiste über dem Inhalt. Sie bündelt sechs Symbole — von links: **Zurück**, **Lesezeichen**, **Schriftgröße**, **Gelesen-Markierung**, **Teilen** und **Im Browser öffnen**.
- **Lesezeichen**: Tippe auf das Lesezeichen-Symbol in der schwebenden Leiste, um den Artikel für später zu merken (**Lesezeichen setzen**) bzw. die Markierung zu entfernen (**Lesezeichen entfernen**). Gespeicherte Artikel findest du unter **Später**.
- **Teilen**: Tippe auf das Teilen-Symbol, um den Artikellink oder den Titel zu teilen.
- **Im Browser öffnen**: Tippe auf das Browser-Symbol, um den Originalartikel in der externen Browser-App zu öffnen. Ohne Internetverbindung erscheint stattdessen der Hinweis **„Keine Internetverbindung."** oberhalb des Artikelinhalts.
- **Schriftgröße/Lesemodus**: Sobald verfügbar, passt dieses Symbol die Schriftgröße im Artikelbereich an.
- **Zurück**: Tippe auf den Zurück-Pfeil in der schwebenden Leiste oder nutze die System-Navigation, um zur vorherigen Seite zurückzukehren.

## Lesestatus

Wenn **Auto-Gelesen** aktiv ist, wird der Artikel nach dem Öffnen automatisch als gelesen markiert. Zwei Bedingungen müssen erfüllt sein: Der globale Schalter **Automatisch als gelesen markieren** auf der Seite **Einstellungen** muss eingeschaltet sein, und der lokale **Auto-Gelesen**-Schalter in der Detailansicht darf für diese Sitzung nicht abgewählt worden sein. Bei ausgeschalteter globaler Option ist der lokale Schalter deaktiviert und abgedunkelt und trägt die Beschriftung *Auto-Gelesen (in den Einstellungen deaktiviert)*. Die **Verzögerung bis Markierung** (*Sofort*, *1 Sekunde*, *3 Sekunden*, *5 Sekunden*) wird ebenfalls in den Einstellungen gewählt — siehe [Einstellungen](../einstellungen/index.md). Konnten die Einstellungen nicht geladen werden, gilt ein Fallback von fünf Sekunden.

## Offline-Verhalten

Der gespeicherte Artikeltext bleibt ohne Internetverbindung vollständig lesbar:

- Oberhalb des Artikelinhalts erscheint das Hinweis-Banner **„Links sind im Offline-Modus deaktiviert."**.
- Links im Artikeltext werden als normaler Text dargestellt und sind nicht anklickbar. Wird dennoch eine externe Verknüpfung angesteuert, zeigt die App einen Hinweisdialog (bestätigen mit **OK**).
- Externe Bilder im Artikelinhalt werden offline nicht geladen — sie werden ausgeblendet, damit keine leeren Platzhalter entstehen.
- Kehrt die Verbindung zurück, werden Links und Bilder automatisch wiederhergestellt, ohne dass der Artikel erneut geöffnet werden muss.

Details siehe [Offline lesen](offline.md).

## Dark Mode

Die Detailansicht folgt dem Farbschema des Geräts (Light/Dark); der Artikelinhalt wird entsprechend umgefärbt. Beachte: Externe Bilder im Artikelinhalt behalten ihre ursprüngliche Darstellung und werden nicht automatisch invertiert.

## Mobile UI-Prüfung

Die Artikeldetailansicht ist für mobile Bildschirme optimiert:

- Getestet für 390 × 844 pt (iPhone-Handygröße) und 768 × 1024 pt.
- Der Artikelbereich scrollt eigenständig; die schwebende Aktionsleiste bleibt am unteren Rand sichtbar.
- Alle tippbaren Elemente in der schwebenden Aktionsleiste und der Status-Pille sind mindestens 44 × 44 pt groß.
- Die Aktionen der schwebenden Leiste sind für Screenreader benannt — u. a. **Lesezeichen setzen**/**Lesezeichen entfernen** und **Als gelesen markieren**/**Bereits gelesen**, die ihre Beschriftung je nach Zustand wechseln. Details siehe [Barrierefreiheit](barrierefreiheit.md).

Die Screenshot-Dokumentation der manuellen UI-Prüfung (Light/Dark, 390 × 844 pt) steht in [Mobile-UI-Design](mobile-ui-design.md).
