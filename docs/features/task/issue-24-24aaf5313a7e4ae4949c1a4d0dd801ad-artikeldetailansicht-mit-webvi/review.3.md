# Review – Artikeldetailansicht mit WebView und Lesestatus

## Status

**Offene Aufgaben vorhanden**

## Zusammenfassung

Der `plan.md` basiert auf der `inventory.md` und deckt die wesentlichen Aspekte der Artikeldetailansicht ab. Es sind jedoch noch Implementierungs-, Abstimmungs- und Verifikationsaufgaben offen, bevor die Funktion als vollständig umgesetzt gelten kann.

## Review-Kriterien

### 1. Vollständigkeit des Plans gegenüber der Bestandsaufnahme

| Bereich | Inventar-Befund | Plan-Abdeckung | Bewertung |
|---|---|---|---|
| Domain-/Daten-Layer | `Item.ContentHtml`, `IsRead`, `IsSavedForLater` vorhanden; `GetByIdAsync`, `MarkAsReadAsync`, `ToggleSavedForLaterAsync` implementiert | Schritt 1 prüft Modelle und Repository; Verwendung der Methoden vorgesehen | OK |
| Fehlende UI-Komponenten | `ArticleDetailPage` und `ArticleDetailViewModel` fehlen | Schritt 2 (ViewModel) und Schritt 3 (Detailseite) definieren beide Komponenten | OK |
| Navigation / Routing | `AppShell.xaml` leer; Detail-Navigation fehlt | Schritt 4 regelt Registrierung in `AppShell.xaml.cs`/`MauiProgram.cs` und `GoToAsync` aus Karte/UnreadViewModel | OK |
| Design-Draft | Light- und Dark-Mode-Screenshots sowie `code.html` liegen vor | Designvorgaben in Plan und Schritt 7/Mobile UI Design Review berücksichtigt | OK |
| Mobile UI Design Review | Checkliste aus `04-mobile-ui-review.md` | Schritt 7 führt Screenshot-basierte Prüfung durch | OK |

### 2. Konsistenz zwischen Plan und Detaildokumenten

- **Design-Draft-Übereinstimmung**: Der Plan reflektiert alle wichtigen Elemente des Design-Drafts (Status-Pille, Header, WebView, Quellen-Footer, Floating Bottom Action Bar, Dark Mode).
- **Domain-Daten**: `Item.ContentHtml`, `MarkAsReadAsync` und `ToggleSavedForLaterAsync` werden korrekt als verfügbar eingestuft.
- **Feed-Icon**: Wird im Plan explizit als Out-of-Scope markiert (kein `Icon`/Image-Property in `Feed`), was mit `05-open-questions.md` konsistent ist.
- **Lesezeit**: Keine eigene Eigenschaft im `Item`; Plan sieht Berechnung aus Wortanzahl vor.

### 3. Offene Punkte und Risiken

- **Einstellungen-Arbeitspaket**: Die konfigurierbare Gelesen-Verzögerung ist noch nicht verfügbar; Plan nutzt einen 5-Sekunden-Fallback. Das ist dokumentiert, aber technisch noch abhängig.
- **UI-Verifikation**: Keine automatisierten UI-Tests; manuelle Screenshot-Dokumentation in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md` ist Pflicht und noch nicht erledigt.
- **HTML-Wrapper/Fonts**: Ob `Newsreader`/`Inter` im WebView-HTML korrekt geladen werden, muss während der Implementierung geprüft werden.
- **Externe Bilder**: Offline-Darstellung ausgeklammert; für das Review klarstellbar, aber für die Detailansicht online-optimiert.
- **Schriftgrößen-Toggle**: Lokale WebView-Implementierung über JavaScript/CSS geplant, bis Einstellungen-Arbeitspaket bereit.

### 4. Noch nicht abgearbeitete Plan-Schritte

Alle 8 Implementierungsschritte im `plan.md` sind noch offen:

1. Domain-/Daten-Layer prüfen und ergänzen
2. ViewModel erstellen (`ArticleDetailViewModel`)
3. Detailseite erstellen (`ArticleDetailPage.xaml` / `.xaml.cs`)
4. Navigation und Routing einrichten
5. HTML-/WebView-Styling
6. Automatisch als gelesen markieren (mit 5-Sekunden-Fallback)
7. Mobile UI Design Review durchführen
8. Akzeptanzkriterien final prüfen

### 5. Empfehlung

Der Plan ist vollständig, konsistent und umsetzungsbereit. Die offenen Aufgaben liegen in der konkreten Implementierung sowie der manuellen UI-Verifikation. Es wird empfohlen, mit der Umsetzung zu beginnen, sobald die folgenden Punkte als klar gelten:

- Routing-Strategie (`Shell.GoToAsync`) ist mit der bestehenden `AppShell`-Navigation abgestimmt.
- Der 5-Sekunden-Auto-Gelesen-Fallback wird akzeptiert, bis das Einstellungen-Arbeitspaket bereitsteht.
- Manuelle UI-Verifikation einschließlich Screenshot-Dokumentation ist eingeplant und wird vor dem finalen Commit durchgeführt.
