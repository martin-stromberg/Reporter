# Plan-Check – Artikeldetailansicht mit WebView und Lesestatus

**Status: Plan lückenhaft**

Der Plan deckt die Keranforderungen und die wesentlichen Bestandsaufnahme-Ergebnisse ab, verpasst aber einige konkrete Detailvorgaben aus dem Design-Draft und der Bestandsaufnahme, die für eine vollständige Umsetzung notwendig sind.

## Abdeckung der Anforderungen (`requirement.md`)

| Anforderung | Quelle | Plan-Referenz | Status |
|-------------|--------|---------------|--------|
| Detailseite mit Titel, Datum, Feed-Name und Link | `requirement.md` Z. 23 | `plan.md` Z. 48-53 | Abgedeckt |
| WebView für gespeichertes `ContentHtml` | `requirement.md` Z. 24 | `plan.md` Z. 50-55, 62-66 | Abgedeckt |
| Automatisches Markieren als gelesen (sofort/verzögert) | `requirement.md` Z. 25 | `plan.md` Z. 44, 87 | Abgedeckt |
| `Für später bewahren`-Toggle | `requirement.md` Z. 26 | `plan.md` Z. 39 | Abgedeckt |
| `Im Browser öffnen`-Button | `requirement.md` Z. 27 | `plan.md` Z. 41 | Abgedeckt |
| Floating Reader Control Bar | `requirement.md` Z. 28 | `plan.md` Z. 51-52 | Abgedeckt |
| WebView-Darstellung korrekt | `requirement.md` Z. 31 | `plan.md` Z. 77 | Abgedeckt |
| Lesestatus/Lesezeichen persistiert | `requirement.md` Z. 35 | `plan.md` Z. 30-32 | Abgedeckt |

## Abdeckung der Bestandsaufnahme (`inventory.md` und Detaildokumente)

| Bestandteil | Quelle | Plan-Referenz | Status |
|-------------|--------|---------------|--------|
| Domain-Modell und `ContentHtml` vorhanden | `inventory/02-domain-and-data.md` Z. 4-24 | `plan.md` Z. 30-32 | Abgedeckt |
| `MarkAsReadAsync` / `ToggleSavedForLaterAsync` | `inventory/02-domain-and-data.md` Z. 13-14 | `plan.md` Z. 39-40 | Abgedeckt |
| Fehlende Detailseite/ViewModel | `inventory/03-existing-ui.md` Z. 36-37 | `plan.md` Z. 35, 48 | Abgedeckt |
| `AppShell`-Routing notwendig | `inventory/03-existing-ui.md` Z. 27-29 | `plan.md` Z. 57-60 | Abgedeckt |
| 44 × 44 pt Touch-Targets | `inventory/04-mobile-ui-review.md` Z. 19-20 | `plan.md` Z. 25, 52 | Gedeckt (Vorgabe) |
| Mobile UI Design Review (Vergleich, Größe, Dark Mode) | `inventory/04-mobile-ui-review.md` Z. 5-30 | `plan.md` Z. 19-27, 68-74 | Abgedeckt |

## Identifizierte Lücken

1. **Status-Pille und `Auto-Gelesen`-Schalter fehlen im Plan**
   - `inventory/01-design-draft.md` Z. 12-13 beschreibt oben eine Status-Pille mit Gelesen-Dot und einem `Auto-Gelesen (5s)`-Toggle.
   - `plan.md` Z. 51-52 sieht nur einen `Gelesen`-Button in der unteren Floating Bar vor; der obere Schalter und die visuelle Gelesen-Anzeige werden nicht als Aufgabe erfasst.

2. **Lesezeit ist nicht konkret eingeplant**
   - `inventory/01-design-draft.md` Z. 18 und `inventory/02-domain-and-data.md` Z. 25-27 nennen die geschätzte Lesezeit als gewünschtes Element, das aktuell nicht im Domain-Modell existiert.
   - `plan.md` Z. 36 listet `ReadingTime` als Property, aber es gibt keine Aufgabe zur Berechnung/Ermittlung und keinen klaren Bezug zur Datenquelle.

3. **Quellen-Footer-Karte aus dem Design-Draft nicht berücksichtigt**
   - `inventory/01-design-draft.md` Z. 27-29 sieht eine Karte "Vollständiger Artikel verfügbar" mit Original-Link vor.
   - `plan.md` Z. 41-52 platziert den "Im Browser öffnen"-Button lediglich in der unteren Aktionleiste, ohne die Design-Draft-Karte im HTML-Wrapper oder oberhalb des WebView zu adressieren.

4. **Feed-Icon und Artikel-Metadaten (Untertitel, Autor/Tags) unberücksichtigt**
   - `inventory/01-design-draft.md` Z. 17-19 nennt Feed-Icon, Untertitel und Autor/Tag-Leiste.
   - `plan.md` Z. 48-53 beschränkt den Header auf "Feed-Name, Datum, Titel"; diese weiteren Metadaten werden nicht explizit berücksichtigt oder als Nicht-Scope gekennzeichnet.

5. **Verzögerungslogik ist nicht vollständig spezifiziert**
   - `plan.md` Z. 87 reduziert die Verzögerung auf einen statischen 5-Sekunden-Fallback.
   - `requirement.md` Z. 41 und `inventory/05-open-questions.md` Z. 16 betonen, dass die Einstellung aus dem Einstellungen-Arbeitspaket bezogen werden soll; der Fallback ist akzeptabel, aber im Plan fehlt ein Hinweis auf den späteren Integrationspunkt.

## Empfohlene Ergänzungen

- Im `ArticleDetailPage` eine obere Status-Pille (Gelesen-Dot + `Auto-Gelesen`-Toggle) ergänzen (`inventory/01-design-draft.md` Z. 12-13).
- `ReadingTime` entweder aus `ContentHtml` berechnen oder explizit als spätere Ergänzung kennzeichnen (`inventory/02-domain-and-data.md` Z. 25-27).
- Quellen-Footer-Karte im HTML-Wrapper oder als separates natives `Grid` oberhalb/unterhalb des WebView ergänzen (`inventory/01-design-draft.md` Z. 27-29).
- Klären, ob Feed-Icon, Untertitel und Autor/Tags in Scope sind, und gegebenenfalls entsprechende Properties/Tasks ergänzen (`inventory/01-design-draft.md` Z. 17-19).
- Hinweis im Plan ergänzen, an welchem Punkt das Einstellungen-Arbeitspaket für die konfigurierbare Gelesen-Verzögerung angebunden wird (`requirement.md` Z. 41, `inventory/05-open-questions.md` Z. 16).

## Fazit

Der Plan ist umsetzbar und deckt die zwingenden Akzeptanzkriterien ab. Für eine vollständige, design-konforme Implementierung sollten jedoch die oben genannten Design-Draft-Elemente und die Lesezeit noch konkret in Aufgaben überführt werden.
