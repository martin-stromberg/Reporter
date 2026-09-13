<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedsPage.xaml (Feeds-Übersicht)

- **Abweichendes Muster** — Die in R2 geforderte Standardbild-Kaskade (Favicon → generierter Initialen-Kreis) greift auf der Feeds-Seite offline nicht durchgängig: Das Favicon-`Image` wird per `MultiTrigger` nur gezeigt, wenn `FaviconUrl` gesetzt **und** `IsOnline` `true` ist (`FeedsPage.xaml`, Zeilen 167–181). Der Initialen-`Border` dahinter wird dagegen nur eingeblendet, wenn `FaviconUrl` leer ist (Zeilen 182–198). Folge: Ist die App offline und ein Feed hat ein erfasstes Favicon, bleibt die 40×40-Avatar-Fläche vollständig leer — während Feeds ohne Favicon ihren Initialen-Kreis zeigen. Eine nicht-technische Anwenderin sieht eine scheinbar defekte, leere Kachel neben korrekt befüllten; der geforderte Fallback (Initialen-Kreis benötigt kein Netzwerk) wird nicht gezeigt.

  Empfehlung: Den `DataTrigger` des Initialen-`Border` zu einem `MultiTrigger` erweitern, der den Kreis auch einblendet, wenn `FaviconUrl` gesetzt, aber `BindingContext.IsOnline` `false` ist — analog zur Sichtbarkeitslogik des Favicon-`Image`, nur invertiert. Alternativ den Offline-Fall wie in `ArticleCardView` behandeln (dort wird die komplette Thumbnail-Fläche offline ausgeblendet), dann aber konsistent auch für den Initialen-Kreis.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen (im Scope dieses Branches liegen R1–R6; R7/R8 wurden per Triage abgespalten und haben keine Oberfläche):

- **Artikelliste lesen (Ungelesen/Später), Lesezeit-Anzeige (R1):** `ReadingTimeEstimator.EstimateText` liefert bei ≤ 1 Minute leeren Text; `ArticleCardView` blendet Zeile samt Uhren-Icon über `StringNotEmptyToBoolConverter` sauber aus. Detailansicht blendet Lesezeittext und Trenn-„•" ebenfalls sauber aus — die dortige Unterdrückung geht über den Wortlaut der Anforderung („Auflistung") hinaus, erzeugt aber kein Layout-Problem und ist konsistent. → unauffällig
- **Standardbild bei Beiträgen ohne Bild (R2):** Kaskade `ImageUrl` → `FeedFaviconUrl` → Initialen-Kreis (`FeedInitial`) in `ArticleCardView` korrekt per Multi-Triggern umgesetzt; offline wird die komplette Thumbnail-Fläche ausgeblendet (bestehendes Verhalten). → unauffällig (Einschränkung: Feeds-Seite offline, siehe Befund)
- **Feed anlegen (R2):** Favicon-Ermittlung läuft automatisch in `TryPersistNewFeedAsync` über `IFeedIconService` — für alle drei Anlage-Wege (Suche, Direkt-URL, Abo aus Suchergebnis), fehlertolerant und offline übersprungen. Die Anwenderin muss keine technische Kennung kennen; Eingabe bleibt Feed-URL/Suchbegriff. → unauffällig
- **Schalter „Beim Programmstart abrufen" (R3):** Sichtbar in Sektion „Synchronisation & Lesefluss", mit Label und erklärendem Hint (DE/EN), `Switch` mit ≥ 44 pt Touch-Target, `SemanticProperties.Description`, Sofort-Persistierung über `PersistAsync`; Wirkung über `AutoRefreshService.StartAsync` mit `IsOnline`-Guard, blockiert den Start nicht. → unauffällig
- **Sortierung der Startseite wählen (R4):** `Picker` „Sortierung der ungelesenen Artikel" mit Klartext-Optionen „Neueste zuerst"/„Älteste zuerst" (DE/EN), keine technischen Werte sichtbar; Auswahl wird sofort persistiert und von `UnreadViewModel.LoadPageAsync` beim nächsten Laden/Pull-to-Refresh angewendet; Tiebreaker `Id` wird mit umgekehrt. → unauffällig
- **Sprache ändern → Neustart-Hinweis (R5):** Hinweis-`Border` an `LanguageRestartHintVisible` gebunden; erscheint nur bei Abweichung vom persistierten Wert, verschwindet bei Rückwahl und nach `LoadAsync`. → unauffällig
- **Neues Programmsymbol (R6):** `appiconfg.svg` (Badge + RSS-Signatur, ohne App-Name) und `splash.svg` (mit „Reporter"-Schriftzug als Pfaddaten) ausgetauscht; `MauiIcon`/`MauiSplashScreen`-Verweise mit Hintergrundfarbe `#1e293b` vorhanden. Keine Benutzerinteraktion. → unauffällig

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:

- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/Views/ArticleCardView.xaml`
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/ArticleDetailPage.xaml` (Kontext: Lesezeit-/FeedIcon-Darstellung)
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Core/ViewModels/UnreadViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs`
- `src/Reporter.Core/ViewModels/SortOrderOption.cs`
- `src/Reporter.Core/Models/ItemListItem.cs`
- `src/Reporter.Core/Models/FeedListItem.cs`
- `src/Reporter.Core/Services/ReadingTimeEstimator.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx` / `AppResources.de.resx` (Beschriftungen der neuen Bedienelemente)
- `src/Reporter/Resources/AppIcon/appicon.svg`, `appiconfg.svg`
- `src/Reporter/Resources/Splash/splash.svg`
- `src/Reporter/Reporter.csproj` (`MauiIcon`/`MauiSplashScreen`)
