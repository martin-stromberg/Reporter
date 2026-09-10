# Artikeldetailansicht mit WebView und Lesestatus

## Aufgaben-ID
24aaf531-3a7e-4ae4-949c-1a4d0dd801ad

## Branch
task/issue-24-24aaf5313a7e4ae4949c1a4d0dd801ad-artikeldetailansicht-mit-webvi

## Erstellt
2026-09-10

## Verknüpftes Issue
- **Kennung:** #24
- **Titel:** Artikeldetailansicht mit WebView und Lesestatus

## Bezug
Gesamtanforderungen (Issue #1)

## Ziel
Artikel können in einer lesefreundlichen Detailansicht geöffnet werden, automatisch als gelesen markiert und für später gespeichert werden.

## Scope
- Detailseite mit Titel, Datum, Feed-Name und Link zur Quelle.
- WebView zur Darstellung des gespeicherten Volltext-HTMLs.
- Automatisches Markieren als gelesen beim Öffnen (sofort oder mit Verzögerung gemäß Einstellungen).
- `Für später bewahren`-Toggle.
- Button `Im Browser öffnen` für Original-Link.
- Floating Reader Control Bar (Zurück, Schriftgröße/Lesemodus, Lesezeichen, Gelesen-Status, Teilen), falls vom Design gefordert.

## Akzeptanzkriterien
- Artikelinhalt wird im WebView korrekt dargestellt.
- Öffnen eines Artikels setzt `is_read` auf true (optional mit Verzögerung).
- `Für später bewahren`-Toggle aktualisiert `is_saved_for_later`.
- Link zur Quelle ist verfügbar und öffnet externen Browser.
- Lesestatus und Lesezeichen werden persistiert.

## Lieferzustand
Nutzer kann Artikel lesen und verwalten.

## Notizen
- Verzögerung für automatisches Gelesen-Markieren kommt aus dem Einstellungen-Arbeitspaket.
- Offline-Modus für Links wird in einem separaten Arbeitspaket behandelt.
