# Detaildokument: Offene Punkte und Annahmen

## Offene Punkte
1. **Detail-Navigation**
   - Wird `ArticleDetailPage` als modale Seite, `Shell` `GoToAsync`-Route oder `Navigation.PushAsync` geöffnet?
   - `AppShell.xaml` ist aktuell leer; Routing-Registrierung in `AppShell.xaml.cs` oder `MauiProgram.cs` nötig.

2. **WebView-Inhalt**
   - Soll `ContentHtml` direkt als HTML in `Microsoft.Maui.Controls.WebView` geladen werden, oder wird ein Wrapper-HTML (`<html><head><style>...</style></head><body>...</body></html>`) generiert?
   - Bilder im `ContentHtml` sind teilweise externe URLs; Offline-Darstellung separat geplant.

3. **Schriftgröße / Lesemodus**
   - Design-Draft enthält A/A+ Toggle. Soll die Schriftgröße global (Einstellungen) oder lokal im WebView per JavaScript/CSS verändert werden?

4. **Automatisch als gelesen markieren**
   - Verzögerung wird aus Einstellungen-Arbeitspaket bezogen; aktuell noch kein Setting verfügbar.
   - Soll die Verzögerung beim Öffnen der Seite starten oder beim Scrollen/Verweilen?

5. **Floating Reader Control Bar**
   - Soll als natives MAUI-`Grid` über die WebView-Inhalte gelegt werden, oder als HTML-Bottom-Bar innerhalb des WebViews?
   - „Teilen“-Button: Nutzung von `Microsoft.Maui.ApplicationModel.DataTransfer.Share` voraussetzen.

6. **UI-Verifikation**
   - Manuelles Testen mit Screenshot-Dokumentation noch nicht durchgeführt; automatisierte UI-Tests existieren nicht.

## Annahmen für die Planung
- Eine neue `ArticleDetailPage` plus `ArticleDetailViewModel` wird erstellt.
- `IItemRepository.GetByIdAsync` liefert `ContentHtml` an die ViewModel.
- `IItemRepository.MarkAsReadAsync`/`ToggleSavedForLaterAsync` decken die Status-Änderungen ab.
- `AppThemeBinding` und 44 × 44 pt Touch-Targets werden berücksichtigt.
