# Plan-Check – Artikeldetailansicht mit WebView und Lesestatus

**Status:** Plan vollständig

## Zusammenfassung

Der Implementierungsplan `plan.md` deckt die fachlichen Anforderungen aus `requirement.md` und die technischen Erkenntnisse aus der Bestandsaufnahme (`inventory.md` inkl. Detaildokumente) vollständig ab. Er baut auf den bereits vorhandenen Klassen und Repositories auf, definiert notwendige Neuentwicklungen (`ArticleDetailPage`/`ArticleDetailViewModel`) und berücksichtigt die Mobile-UI-Design-Review-Richtlinien aus `AGENTS.md`.

## Prüfung gegen `requirement.md`

| Anforderung | Abdeckung im Plan | Bemerkung |
| --- | --- | --- |
| Detailseite mit Titel, Datum, Feed-Name und Link zur Quelle | Vollständig | Schritt 3 beschreibt Header mit Feed-Icon, Feed-Name, Trennzeichen, Datum, Lesezeit und Titel sowie Quellen-Footer-Karte. |
| WebView zur Darstellung des gespeicherten Volltext-HTMLs | Vollständig | Schritt 3 / 5 definieren `WebView` mit `HtmlWebViewSource`, HTML-Wrapper, CSS für Light/Dark und Schriftgrößen. |
| Automatisches Markieren als gelesen beim Öffnen | Vollständig | Schritt 6 startet Verzögerungslogik; nutzt 5-Sekunden-Fallback bis Einstellungen-Arbeitspaket verfügbar. |
| `Für später bewahren`-Toggle | Vollständig | Schritt 2: `ToggleSavedForLaterCommand` → `IItemRepository.ToggleSavedForLaterAsync`. |
| `Im Browser öffnen` für Original-Link | Vollständig | Schritt 2: `OpenInBrowserCommand` → `Launcher.OpenAsync(Item.Link)`; Schritt 3: Footer-Karte und Floating Bar enthalten Browser-Button. |
| Floating Reader Control Bar (Zurück, Schriftgröße/Lesemodus, Lesezeichen, Gelesen-Status, Teilen) | Vollständig | Schritt 3 Row 4 und Schritt 2 Commands decken Zurück, Schriftgröße, Lesezeichen, Gelesen, Teilen und Browser ab. |
| Akzeptanzkriterien | Vollständig | Schritt 8 führt alle Akzeptanzkriterien als finale Prüfliste. |

## Prüfung gegen `inventory.md`

| Bestand | Berücksichtigung im Plan | Bemerkung |
| --- | --- | --- |
| `Item.ContentHtml` und Repository-Methoden (`GetByIdAsync`, `MarkAsReadAsync`, `ToggleSavedForLaterAsync`) | Ja | Schritt 1 prüft Verfügbarkeit, Schritt 2 nutzt Methoden direkt. |
| `ArticleCardView` / `UnreadPage` mit etablierten Patterns | Ja | Plan verweist auf 44 × 44 pt Touch-Targets und `AppThemeBinding` als etablierte Pattern. |
| Fehlende `ArticleDetailPage` / `ArticleDetailViewModel` | Ja | Schritt 2 und 3 legen beide explizit an. |
| Leeres `AppShell.xaml` / Routing notwendig | Ja | Schritt 4 regelt Route-Registrierung und Navigation von `UnreadViewModel`/`ArticleCardView`. |
| Design-Draft (Light/Dark) | Ja | Schritt 7 und 5 nehmen Design-Draft-Vergleich, Dark-Mode-CSS und WebView-Styling auf. |
| Mobile UI Design Review-Checkliste | Ja | Schritt 7 wiederholt alle Review-Punkte (390 × 844 pt, Touch-Targets, kein verschachteltes ScrollView, Dark Mode, Screenshot-Doku). |

## Feststellungen und Hinweise

1. **Feed-Name/Feed-Icon**: Der Plan berücksichtigt, dass `FeedName` nicht direkt im `Item`-Modell vorhanden ist, und plant eine Prüfung/Ergänzung über `FeedId` (Schritt 1). Diese Stelle bleibt bewusst flexibel, weil der genaue Datenbestand erst beim Implementieren final geprüft wird.
2. **Verzögerungslogik**: Der 5-Sekunden-Fallback ist konsistent dokumentiert und der spätere Übergang zum Einstellungen-Arbeitspaket als Risiko/Offener Punkt festgehalten.
3. **Schriftgröße / Lesemodus**: Lokale A/A+-Steuerung im WebView ist vorgesehen, bis Einstellungen-Arbeitspaket bereit ist.
4. **UI-Verifikation**: Manuelle Verifikation mit Screenshots ist im Plan vorgesehen und entspricht der Project Rule `Mobile UI Design Review`.

## Fazit

Der Plan ist ausreichend detailliert, zielgerichtet und anforderungskonform. Es sind keine zusätzlichen Planungsinformationen erforderlich, bevor die Implementierung beginnen kann.
