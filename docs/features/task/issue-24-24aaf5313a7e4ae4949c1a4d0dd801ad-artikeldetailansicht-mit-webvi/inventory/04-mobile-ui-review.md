# Detaildokument: Mobile UI Design Review-Checkliste

> Gilt gemäß Project Rule "Mobile UI Design Review" für jede neue .NET MAUI-Seite/Steuerung.

## Design-Draft-Vergleich
- [ ] Neuer `ArticleDetailPage` gegen `design-draft/stitch_local_rss_feed_reader/artikel_lesemodus/screen.png` prüfen.
- [ ] Dark-Mode-Variante gegen `artikel_lesemodus_dark_mode/screen.png` prüfen.

## Mobile Formfaktor
- [ ] Fenstergröße 390 × 844 pt (Windows handysize) oder iOS-Simulator aus `scripts/iOS-Deployment.ps1` testen.
- [ ] Keine horizontalen Datentabellen oder mehrere Text-Buttons in einer Zeile.

## Layout-Regeln
- [ ] `ScrollView` nicht innerhalb eines `CollectionView` verschachteln; Detailseite sollte einzeln scrollen.
- [ ] Inhalte im Grid mit `Row="*"` nutzen, sodass Inhalte verbleibenden Platz füllen.
- [ ] WebView bzw. WebView-Hülle in `Grid`/`ScrollView` so platzieren, dass Scroll nicht doppelt auftritt.

## Touch-Targets
- [ ] Alle klickbaren Buttons (Zurück, Lesezeichen, Schriftgröße, Gelesen, Teilen, Browser, Quelle) mindestens 44 × 44 pt.
- [ ] Floating Bottom Action Bar: Buttonhöhe/-breite entsprechend prüfen.

## Dark Mode
- [ ] Alle Farben mit `AppThemeBinding` (`Light...` / `Dark...`) referenzieren.
- [ ] WebView-Inhalte müssen Dark-Mode-CSS via ` prefers-color-scheme` oder injected Theme-Modus unterstützen.
- [ ] Symbole/Paths ebenfalls `AppThemeBinding` für Stroke/Fill nutzen.

## UI-Verifikation
- [ ] Manuelle Verifikation auf Windows handysize 390 × 844 pt dokumentieren.
- [ ] Alternativ automatisierter UI-Test ergänzen.
- [ ] Testergebnisse und ggf. Screenshots in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md` festhalten, wenn kein automatisierter UI-Test existiert.

## Hinweis zur WebView-Seite
- WebView selbst ist ein natives Control und sollte den Design-Draft-HTML-Look via `HtmlWebViewSource` oder `Source`-HTML nachbilden.
- Native Bottom-Bar-Buttons bleiben MAUI-Controls mit `AppThemeBinding`.
