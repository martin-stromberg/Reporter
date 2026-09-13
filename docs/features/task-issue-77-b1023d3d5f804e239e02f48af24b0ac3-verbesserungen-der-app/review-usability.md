<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Usability-Review

## Ergebnis

**Status:** Keine Befunde

## Befunde

Keine.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:

- R1 — Lesezeit-Anzeige in der Artikelauflistung bei 1-Minuten-Beiträgen entfällt (reine Anzeige, keine Interaktion): `ReadingTimeEstimator.EstimateText` liefert bei ≤ 1 Minute einen leeren String, die vorhandene `StringNotEmptyToBoolConverter`-Sichtbarkeit der Lesezeit-Zeile in `ArticleCardView.xaml` greift unverändert → unauffällig
- R2 — Standardbild bei fehlendem Beitragsbild (reine Anzeige, keine Interaktion): `ArticleCardView.xaml` zeigt per MultiTrigger die Kaskade `ImageUrl` → `FeedFaviconUrl` (48 pt, zentriert) → Kreis-Border mit `FeedInitial`; `FeedsPage.xaml` zeigt dasselbe Muster für die Feed-Liste (Favicon 40 pt bzw. Initialen-Kreis, offline bleibt der Kreis sichtbar) → unauffällig
- R3 — Schalter „Beim Programmstart abrufen" in den Einstellungen betätigen: `SettingsPage.xaml`, Sektion „Synchronisation & Lesefluss" — `Switch` mit Label + verständlichem Hinweistext („Feeds beim Start der App laden"), `SemanticProperties.Description`, Touch-Target ≥ 44 pt, Sofort-Persistierung über `RefreshOnStartupEnabled`/`PersistAsync`; Start-Sync läuft fehlerisoliert in `AutoRefreshService.StartAsync` mit Online-Guard → unauffällig
- R4 — Sortierrichtung der Startseite **Ungelesen** in den Einstellungen wählen: `Picker` „Sortierung der ungelesenen Artikel" mit Klartext-Optionen „Neueste zuerst"/„Älteste zuerst" (keine technischen Werte wie `desc`/`asc` sichtbar), `SortOrderOption` folgt dem etablierten Optionsklassen-Muster (`RefreshIntervalOption`/`LanguageOption`); wirksam ab dem nächsten Laden — `UnreadPage.OnAppearing` führt `LoadCommand` aus, `UnreadViewModel.LoadPageAsync` liest die Einstellung → unauffällig
- R5 — Neustart-Hinweis unter der Sprachwahl: Info-`Border` ist an `LanguageRestartHintVisible` gebunden und erscheint erst, wenn `SelectedLanguage` vom persistierten Wert `_loadedLanguage` abweicht (Rückwahl der Originalsprache blendet den Hinweis wieder aus; `LoadAsync` setzt das Flag zurück) → unauffällig
- R6 — Neues Programmsymbol (reine Anzeige, keine Interaktion): `appiconfg.svg` (Badge-Variante ohne Schriftzug) als `MauiIcon`-Vordergrund über unverändertem Hintergrund `#1e293b`, `splash.svg` (Variante mit „Reporter"-Schriftzug) als `MauiSplashScreen` → unauffällig

R7 (Debuginformationen per E-Mail) und R8 (Benachrichtigungen nur bei Hintergrundabruf) wurden per Triage (R0) aus diesem Lauf abgespalten und sind hier nicht zu prüfen.

Hinweise zur Prüftiefe: Keine internen/technischen Kennungen erforderlich, keine Auswahl aus großen Mengen ohne Suche, keine abweichenden Bedienmuster erfunden — alle neuen Bedienelemente nutzen die vorhandenen Settings-Muster (Switch/Picker mit lokalisierten Labels, `SemanticProperties`, `AppThemeBinding`, `MinimumHeightRequest="44"`).

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:

- `src/Reporter/Views/ArticleCardView.xaml`
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/Resources/AppIcon/appiconfg.svg`
- `src/Reporter/Resources/Splash/splash.svg`
- `src/Reporter/Reporter.csproj` (`MauiIcon`/`MauiSplashScreen`-Verweise)
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Core/ViewModels/SortOrderOption.cs`
- `src/Reporter.Core/ViewModels/UnreadViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs` / `FeedsViewModel.Search.cs`
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter/Views/UnreadPage.xaml.cs`
- `src/Reporter.Core/Models/ItemListItem.cs` / `FeedListItem.cs` / `FeedAvatar.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx` / `AppResources.de.resx`
