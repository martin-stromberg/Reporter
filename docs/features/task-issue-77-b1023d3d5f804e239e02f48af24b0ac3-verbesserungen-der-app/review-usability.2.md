<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Usability-Review

## Ergebnis

**Status:** Keine Befunde

## Befunde

Keine.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:

- **R1 — Lesezeit-Anzeige bei 1-Minuten-Beiträgen:** `ReadingTimeEstimator.EstimateText` liefert bei ≤ 1 Minute einen leeren String; die Artikelkarte blendet die Zeile über `StringNotEmptyToBoolConverter` vollständig aus (Icon + Text). In der Detailansicht werden Trenn-Punkt und Label ebenfalls konsistent ausgeblendet — kein UI-Fragment ohne Inhalt. → unauffällig
- **R2 — Standardbild-Kaskade (Beitragsbild → Feed-Favicon → Initialen-Kreis):** `ArticleCardView` (geteilt zwischen **Ungelesen** und **Später**) zeigt per MultiTrigger zuerst `ImageUrl`, dann `FeedFaviconUrl`, zuletzt einen Kreis mit `FeedInitial` (erster Buchstabe des Feed-Titels, „?" bei leerem Titel). Auf der **Feeds**-Seite wird das Favicon bzw. offline/fehlend der Initialen-Kreis gezeigt. Die Favicon-Ermittlung läuft automatisch bei der Feed-Anlage (`FeedsViewModel.TryPersistNewFeedAsync` → `IFeedIconService`, bei Suche mit `SiteUrl`, bei Direkt-Anlage über den Host) und wird für Bestandsfeeds beim ersten erfolgreichen Sync nachgeholt — der Anwender muss nichts eingeben oder konfigurieren. Offline-Verhalten: Auf der Feeds-Seite greift der Initialen-Fallback (kein kaputtes Bild); auf der Artikelkarte wird der gesamte Thumbnail-Container ausgeblendet — das ist das dokumentierte Vorverhalten („Thumbnails werden offline bereits ausgeblendet") und blockiert keine Interaktion. → unauffällig
- **R3 — Schalter „Beim Programmstart abrufen":** Sichtbar in den Einstellungen, Sektion „Synchronisation & Lesefluss", mit verständlichem Hinweistext („Feeds beim Start der App laden" / „Load feeds when the app starts"), 44-pt-Touch-Target, `SemanticProperties.Description`, Sofort-Persistierung. Start-Sync läuft isoliert in `AutoRefreshService.StartAsync` mit Online-Guard. → unauffällig
- **R4 — Sortierrichtung der Startseite:** `Picker` „Sortierung der ungelesenen Artikel" mit Klartext-Optionen „Neueste zuerst" / „Älteste zuerst" (EN: „Newest first" / „Oldest first") — kein technischer Wert („asc"/„desc") für den Anwender sichtbar. Wirksamkeit: `UnreadPage.OnAppearing` löst `LoadCommand` aus, `LoadPageAsync` liest die Einstellung bei jedem Laden frisch — die geänderte Sortierung ist bei Rückkehr zur Startseite sofort sichtbar, ohne Neustart oder manuelles Refresh. → unauffällig
- **R5 — Neustart-Hinweis bei Sprachänderung:** Der Hinweis-`Border` ist an `LanguageRestartHintVisible` gebunden und erscheint nur, wenn die Auswahl vom persistierten Wert abweicht; bei Rückwahl der ursprünglichen Sprache oder nach `LoadAsync` wird er wieder ausgeblendet. → unauffällig
- **R6 — Neues Programmsymbol:** `appiconfg.svg` (Badge ohne App-Name) und `splash.svg` (Badge mit „Reporter"-Schriftzug als Pfad) ersetzt; `Reporter.csproj`-Verweise (`MauiIcon`/`MauiSplashScreen`) unverändert gültig. Rein passive Änderung ohne Benutzerinteraktion. → unauffällig
- **R7 — Debuginformationen per E-Mail:** Nicht im Scope dieses Laufs — gemäß Meta-Anforderung R0 in ein separates Issue abgespalten; keine Oberfläche gebaut. → nicht geprüft (abgespalten)
- **R8 — Benachrichtigungen nur bei Hintergrundabruf:** Nicht im Scope dieses Laufs — gemäß R0 abgespalten; keine Oberflächenänderung. → nicht geprüft (abgespalten)

Querschnittsprüfung nach `AGENTS.md`: Alle neuen Bedienelemente (Schalter, Picker) haben `MinimumWidthRequest`/`MinimumHeightRequest` von 44 pt, alle neuen Oberflächenelemente nutzen `AppThemeBinding` für Dark Mode, keine horizontalen Datentabellen eingeführt, die neuen Zeilen liegen im bestehenden `ScrollView`/`VerticalStackLayout` der `SettingsPage`. Keine Stelle verlangt die Eingabe oder Kenntnis interner/technischer Kennungen (Ids, GUIDs, Dateinamen, technische Schlüssel).

## Geprüfte Dateien

Liste aller geprüften UI-Dateien (geänderte/neue Dateien des Branches gegenüber `origin/staging`, inkl. uncommitted Working-Tree-Änderungen):

- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/Views/ArticleCardView.xaml`
- `src/Reporter/Views/ArticleCardView.xaml.cs`
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Core/ViewModels/UnreadViewModel.cs`
- `src/Reporter.Core/ViewModels/SortOrderOption.cs` (neu)
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs`
- `src/Reporter.Core/Models/ItemListItem.cs`
- `src/Reporter.Core/Models/FeedListItem.cs`
- `src/Reporter.Core/Models/FeedAvatar.cs` (neu)
- `src/Reporter.Core/Models/Settings.cs`
- `src/Reporter.Core/Models/SettingsValues.cs`
- `src/Reporter.Core/Services/ReadingTimeEstimator.cs`
- `src/Reporter.Core/Services/AutoRefreshService.cs`
- `src/Reporter.Core/Services/FeedSyncService.cs`
- `src/Reporter.Core/Services/FeedIconService.cs` (neu)
- `src/Reporter.Core/Interfaces/IFeedIconService.cs` (neu)
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Data/Repositories/ItemRepository.cs`
- `src/Reporter/MauiProgram.cs`
- `src/Reporter/Resources/AppIcon/appiconfg.svg`
- `src/Reporter/Resources/Splash/splash.svg`
