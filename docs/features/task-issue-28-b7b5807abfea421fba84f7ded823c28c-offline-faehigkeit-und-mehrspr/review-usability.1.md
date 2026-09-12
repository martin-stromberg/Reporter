# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### ArticleDetailPage.xaml.cs (ArticleDetailPage)

- **Erreichbarkeit** — Die Kernaufgabe „Artikel ohne Netzwerkverbindung lesen" ist gefährdet: Der neue `OnWebViewNavigating`-Handler bricht im Offline-Zustand **jede** WebView-Navigation ab (`e.Cancel = true`) und zeigt sofort einen Alert „Keine Internetverbindung / Links sind im Offline-Modus deaktiviert". Auf Plattformen, bei denen `Navigating` auch beim initialen Laden des `HtmlWebViewSource`-Inhalts ausgelöst wird (iOS: `WKWebView.decidePolicyForNavigationAction`; Windows: `WebView2.NavigationStarting` – die App zielt auf beide), wird damit nicht nur ein Link-Klick blockiert, sondern das Rendern des lokal gespeicherten Artikels selbst. Für die Anwenderin heißt das: Beim Öffnen eines Artikels im Offline-Modus erscheint unvermittelt ein Hinweisdialog und der Artikelbereich bleibt leer — die zentrale Anforderung „ohne Netzwerk vollständig lesbar" wäre dort nicht erfüllt.

  Empfehlung: Nur echte Link-Navigationen abbrechen, nicht den initialen Inhalts-Load — z. B. nur canceln, wenn `e.Url` mit `http`/`https` beginnt (der HTML-Load meldet `about:blank`/keine externe URL), oder `e.NavigationType` auswerten. Zusätzlich sollte der Alert nur bei einem tatsächlichen Link-Klick erscheinen, nicht beim Seitenaufbau. Manuell auf iOS-Simulator und Windows (390 × 844 pt) im Flugmodus verifizieren.

### ArticleDetailViewModel.cs / ArticleDetailPage.xaml (ArticleDetailPage)

- **Erreichbarkeit** — Die rote Fehlermeldung unter dem Offline-Hinweis wird nie zurückgesetzt: `OpenInBrowserAsync` setzt offline `ErrorMessage = AppResources.OfflineHint`, aber kein Pfad leert die Meldung wieder — weder `OnConnectivityChanged` (Rebuild bei Rückkehr ins Netz) noch ein erfolgreicher `Browser.OpenAsync`-Aufruf. Die Anwenderin sieht „Keine Internetverbindung." als roten Fehlertext weiterhin, obwohl das Gerät längst wieder online ist und die Aktion jetzt funktionieren würde. Zusätzlich wird im `catch`-Zweig `ErrorMessage = ex.Message` gesetzt: eine rohe, technische Exception-Meldung (typischerweise englisch, z. B. vom Browser-Launcher), die eine nicht-technische Nutzerin nicht einordnen kann und die gerade eingeführte Mehrsprachigkeit unterläuft.

  Empfehlung: `ErrorMessage` in `OnConnectivityChanged` und bei erfolgreichem `Browser.OpenAsync` auf `string.Empty` zurücksetzen; im `catch` eine lokalisierte generische Meldung (z. B. neuer Schlüssel `ErrorOpenInBrowserFailed`) statt `ex.Message` anzeigen, technische Details nur ins Debug-Log.

### FeedsPage.xaml (FeedsPage)

- **Erreichbarkeit** — Wenn die Anwenderin im Offline-Zustand Pull-to-Refresh an der Feed-Liste auslöst (oder über das Aktionsmenü einer Feed-Karte „Aktualisieren" wählt), erscheint die Meldung „Keine Internetverbindung." als roter Fehlertext **innerhalb der Eingabekarte zum Hinzufügen eines Feeds** (ErrorMessage-Label, Zeilen 65–68) — also an einer Stelle, die mit der ausgelösten Aktion nichts zu tun hat. Das Banner unter der Karte zeigt zwar denselben Hinweis in neutraler Form, die rote Fehlermeldung mitten im Formular kann aber als Formular-Validierungsfehler („Feed-URL ungültig") missverstanden werden.

  Empfehlung: Sync-/Refresh-Fehler außerhalb der Eingabekarte anzeigen — z. B. als eigenes Label direkt über der Feed-Liste (unterhalb des Offline-Banners) oder ein separates `SyncErrorMessage` im ViewModel, das am Listenkopf gerendert wird, während `ErrorMessage` der Formular-Validierung vorbehalten bleibt.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:

- Synchronisierte Artikel/Kategorien/Feeds ohne Netzwerk lesen (Listen + Detailansicht) → **Befund vorhanden** (WebView-`Navigating`-Abbruch kann den initialen Artikel-Load auf iOS/Windows blockieren)
- Sync-Button auf „Ungelesen" zeigt Offline-Status (Dimmung + Hinweis-Label; Tap erzeugt lokalisierte Fehlermeldung) → unauffällig
- Pull-to-Refresh / „Alle aktualisieren" auf „Feeds" im Offline-Zustand → **Befund vorhanden** (Fehlermeldung erscheint innerhalb der Eingabekarte statt am Auslöseort)
- Links im Artikelinhalt offline deaktiviert → unauffällig (Sanitizer neutralisiert `<a>` offline, Banner „Links sind im Offline-Modus deaktiviert." sichtbar; Navigating-Guard nur als Fallback nötig — siehe Befund 1)
- „Im Browser öffnen" offline robust → **Befund vorhanden** (lokalisierte Offline-Meldung vorhanden, bleibt aber nach Wiederkehr des Netzes stehen; technische `ex.Message` im Fehlerfall)
- Alle UI-Texte lokalisiert EN/DE, Sprache folgt Systemeinstellung → unauffällig (hartcodierte Texte auf `AppResources` umgestellt, DE/EN-Schlüssel konsistent, neutral = Englisch als Fallback)
- Optionaler manueller Sprachwechsel → nicht umgesetzt — laut Anforderung ausdrücklich optional, daher kein Befund
- Kategorie-Fehlermeldungen (leerer Name / Duplikat) → unauffällig (lokalisiert, werden im bestehenden Fehler-Label der Kategorien-Seite angezeigt)
- Artikel-Thumbnails in Karten offline ausgeblendet → unauffällig (Bilder wären offline ohnehin nicht ladbar; Kartenlayout kollabiert sauber)

Mobile-Regeln (AGENTS.md): Keine horizontalen Datentabellen, keine Mehrfach-Textbuttons in einer Zeile (Bottom-Bar nutzt Icon-Glyphen, einziger Textbutton „Im Browser öffnen" steht einzeln im Footer), keine CollectionView/ScrollView-Verschachtelung, Touch-Targets ≥ 44 × 44 pt (Icon-Borders 44 × 44, `MinimumHeightRequest`/`MinimumWidthRequest` an Switch/Button), alle neuen Farben über `AppThemeBinding` — ein `design-draft`-Screen für den Offline-Zustand existiert nicht, die Hinweis-Banner folgen dem etablierten `NotificationsIosOnlyHint`-Muster.

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:

- `src/Reporter/Views/UnreadPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml` (+ `src/Reporter/Views/FeedsPage.xaml.cs`)
- `src/Reporter/Views/LaterPage.xaml`
- `src/Reporter/Views/ArticleDetailPage.xaml` (+ `src/Reporter/Views/ArticleDetailPage.xaml.cs`)
- `src/Reporter/Views/ArticleCardView.xaml` (+ `src/Reporter/Views/ArticleCardView.xaml.cs`)
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter.Core/ViewModels/UnreadViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/LaterViewModel.cs`
- `src/Reporter.Core/ViewModels/CategoriesViewModel.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx` / `AppResources.de.resx` / `AppResources.Designer.cs`
- `src/Reporter.Core/Interfaces/INetworkStatusService.cs` (neu)
- `src/Reporter/Services/NetworkStatusService.cs` (neu)
- `src/Reporter.Core/Services/ArticleHtmlSanitizer.cs` (neu)
- `src/Reporter.Core/Services/FeedSyncService.cs`, `src/Reporter.Core/Services/AutoRefreshService.cs` (Offline-Frühabbruch, UI-relevante Meldung)
- `src/Reporter/App.xaml.cs`, `src/Reporter/MauiProgram.cs` (DI-/Start-Verdrahtung)
