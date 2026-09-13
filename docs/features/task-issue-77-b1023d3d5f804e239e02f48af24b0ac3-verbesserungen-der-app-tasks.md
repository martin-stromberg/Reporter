<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Verbesserungen der App (Issue #77)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Triage (R0) | GitHub-Issue für R7 (Debuginformationen sammeln und per E-Mail versenden) via `gh issue create` anlegen | Offen | — |
| 2 | Triage (R0) | GitHub-Issue für R8 (Benachrichtigungen nur bei Hintergrundabruf, inkl. Hinweis auf fehlende iOS-Background-Fetch-Infrastruktur) via `gh issue create` anlegen | Offen | — |
| 3 | Datenmodell | `Feed.FaviconUrl` (`string?`) in `src/Reporter.Core/Models/Feed.cs` und `src/Reporter.Data/Entities/Feed.cs` ergänzen (R2) | Offen | — |
| 4 | Datenmodell | `Settings.RefreshOnStartupEnabled` (`bool`, Default `true`) in `src/Reporter.Core/Models/Settings.cs` und `src/Reporter.Data/Entities/Settings.cs` ergänzen (R3) | Offen | — |
| 5 | Datenmodell | `Settings.UnreadSortOrder` (`string?`, Default `SettingsValues.SortOrderDescending`) in beiden `Settings`-Klassen ergänzen (R4) | Offen | — |
| 6 | Datenmodell | Konstanten `SortOrderDescending` (`"desc"`) und `SortOrderAscending` (`"asc"`) in `src/Reporter.Core/Models/SettingsValues.cs` ergänzen (R4) | Offen | — |
| 7 | Datenmodell | `ItemListItem.FeedFaviconUrl` (`string?`) und berechnete `FeedInitial`-Eigenschaft ergänzen; `CopyWith` mitziehen (R2) | Offen | — |
| 8 | Datenmodell | `FeedListItem.FaviconUrl` (`string?`) und berechnete `FeedInitial`-Eigenschaft (aus `Title`) ergänzen (R2) | Offen | — |
| 9 | Persistenz | Spalten-Mapping `feeds.favicon_url` in `ReporterDbContext.ConfigureFeed` ergänzen (R2) | Offen | — |
| 10 | Persistenz | Spalten-Mappings `settings.refresh_on_startup_enabled` (Default `true`) und `settings.unread_sort_order` in `ReporterDbContext.ConfigureSettings` ergänzen (R3/R4) | Offen | — |
| 11 | Persistenz | EF-Migration `AddFeedFaviconUrl` erzeugen (R2) | Offen | — |
| 12 | Persistenz | EF-Migration `AddSettingsStartupRefreshAndSortOrder` erzeugen (R3/R4) | Offen | — |
| 13 | Persistenz | `FeedRepository.MapToModel`/`MapToEntity`/`UpdateAsync` um `FaviconUrl` erweitern (R2) | Offen | — |
| 14 | Persistenz | `FeedRepository.GetAllWithDetailsAsync`: `FaviconUrl` in die `FeedListItem`-Projektion aufnehmen (R2) | Offen | — |
| 15 | Persistenz | `SettingsRepository.SaveAsync`/`MapToModel` um `RefreshOnStartupEnabled` und `UnreadSortOrder` erweitern (R3/R4) | Offen | — |
| 16 | Logik | `ReadingTimeEstimator.EstimateText`: `string.Empty` zurückgeben, wenn die geschätzte Lesezeit ≤ 1 Minute beträgt (deckt Auflistung und Detailansicht ab) (R1) | Offen | — |
| 17 | Logik | `ItemListRow.FeedFaviconUrl` + Projektion `i.Feed.FaviconUrl` in `SelectListItemRows`; Befüllung in `MapToListItem` (R2) | Offen | — |
| 18 | Logik | `IItemRepository.GetUnreadByDateAsync` um Parameter `bool ascending = false` erweitern; `ItemRepository` implementiert Richtungsumkehr inkl. `ThenByDescending(Id)`-Tiebreaker (R4) | Offen | — |
| 19 | Logik | Interface `IFeedIconService` in `src/Reporter.Core/Interfaces/` anlegen (`FindFaviconUrlAsync`) (R2) | Offen | — |
| 20 | Logik | `FeedIconService` in `src/Reporter.Core/Services/` implementieren: `<link rel="icon…">`-Parsing, relative-URL-Auflösung, `/favicon.ico`-Fallback mit Verifikation (R2) | Offen | — |
| 21 | Logik | `FeedsViewModel`: `IFeedIconService`-Abhängigkeit; `TryPersistNewFeedAsync(url, title, siteUrl)` erweitern, fehlerisolierter Icon-Abruf mit `IsOnline`-Guard; `SubscribeResultAsync` übergibt `SiteUrl` (R2) | Offen | — |
| 22 | Logik | `FeedsViewModel.ToFeed`: `FaviconUrl` aus `FeedListItem` mitführen (Schutz vor Datenverlust bei Rename/Kategorie/Edit) (R2) | Offen | — |
| 23 | Logik | `FeedSyncService`: `IFeedIconService`-Abhängigkeit; `UpdateFeedHealthAsync` holt `FaviconUrl` bei erfolgreichem Sync nach, wenn leer (R2) | Offen | — |
| 24 | Logik | `AutoRefreshService.StartAsync`: bei `RefreshOnStartupEnabled && IsOnline` fehlerisolierten, nicht abgewarteten `SyncAllAsync` starten (R3) | Offen | — |
| 25 | Logik | `UnreadViewModel`: `ISettingsRepository`-Abhängigkeit; `LoadPageAsync` wertet `UnreadSortOrder` aus und übergibt `ascending` (R4) | Offen | — |
| 26 | Logik | `SettingsViewModel.RefreshOnStartupEnabled` (bindbar, `PersistOnChange`) + `LoadAsync`/`PersistAsync`-Mapping (R3) | Offen | — |
| 27 | Logik | `SortOrderOption`-Klasse in `src/Reporter.Core/ViewModels/` anlegen; `SettingsViewModel.SortOrderOptions`/`SelectedSortOrder` + `LoadAsync`/`PersistAsync`-Mapping mit `"desc"`-Fallback (R4) | Offen | — |
| 28 | Logik | `SettingsViewModel.LanguageRestartHintVisible`: Setzen im `SelectedLanguage`-Setter bei Abweichung vom persistierten Wert, Rücksetzen in `LoadAsync` (R5) | Offen | — |
| 29 | Logik | `ArticleDetailViewModel.LoadAsync`: `FeedIconUrl` aus `feed?.FaviconUrl` befüllen (Feed wird bereits in `LoadAsync` geladen) (R2); Fallback-`new Settings` (Zeile 263) um `RefreshOnStartupEnabled` ergänzen (`required`-Member) (R3) | Offen | — |
| 30 | UI | `SettingsPage.xaml`: `Switch`-Zeile „Beim Programmstart abrufen" in Sektion **Synchronisation & Lesefluss** (Muster `AutoRefreshEnabled`-Zeile) (R3) | Offen | — |
| 31 | UI | `SettingsPage.xaml`: `Picker`-Zeile für Sortierrichtung in Sektion **Synchronisation & Lesefluss** (Muster `SelectedRefreshInterval`-Zeile) (R4) | Offen | — |
| 32 | UI | `SettingsPage.xaml`: `IsVisible="{Binding LanguageRestartHintVisible}"` auf den Neustart-Hinweis-`Border` (R5) | Offen | — |
| 33 | UI | `ArticleCardView.xaml`: Standardbild-Kaskade im Thumbnail-`Border` — `ImageUrl` → `FeedFaviconUrl` → Kreis-`Border` mit `FeedInitial`-`Label`, jeweils per `DataTrigger` (R2) | Offen | — |
| 34 | UI | `FeedsPage.xaml`: Bild-Spalte im Feed-Karten-Template — `Image` auf `FaviconUrl` (nicht leer + `IsOnline` via `x:Reference`), sonst Kreis-`Border` mit `FeedInitial`-`Label` (R2) | Offen | — |
| 35 | Assets | Issue-Attachments authentifiziert herunterladen (`curl -H "Authorization: Bearer $(gh auth token)"`) und Splash-`<text>` in Pfade umwandeln (R6) | Offen | — |
| 36 | Assets | `Resources/AppIcon/appiconfg.svg` (Icon-Variante ohne Text) und `Resources/Splash/splash.svg` (Variante mit Schriftzug) ersetzen; `Reporter.csproj`-Verweise/`Color`/`BaseSize` prüfen (R6) | Offen | — |
| 37 | Konfiguration | Neue `AppResources`-Schlüssel EN+DE (`SettingsRefreshOnStartupLabel`/`Hint`, `SettingsSortOrderLabel`, `SettingsSortOrderNewest`/`Oldest`); `AppResources.Designer.cs` regenerieren (R3/R4) | Offen | — |
| 38 | Konfiguration | `IFeedIconService` als Singleton in `MauiProgram.CreateMauiApp` registrieren (R2) | Offen | — |
| 39 | Tests | `ReadingTimeEstimatorTests`: `EstimateText_ShortContent_ReturnsOneMinute` auf `string.Empty`-Erwartung umstellen; neue Fälle ≤1 Min → leer / >1 Min → Text (R1) | Offen | — |
| 40 | Tests | `ItemRepositoryTests`: leerer `ReadingTimeText` bei 1 Minute, `FeedFaviconUrl`-Projektion, aufsteigende Sortierung inkl. dokumentiertem NULL-Verhalten bei `PublishedAt` (R1/R2/R4) | Offen | — |
| 41 | Tests | `FeedIconServiceTests` neu anlegen: Link-Tag-Parsing, relative URLs, `/favicon.ico`-Fallback, Fehler → `null` (R2) | Offen | — |
| 42 | Tests | `FeedsViewModelTests`: Favicon-Befüllung, Fehlertoleranz, Offline-Skip, `ToFeed`-Favicon-Erhalt bei Rename; Fake für `IFeedIconService` (R2) | Offen | — |
| 43 | Tests | `FeedSyncServiceTests`: Favicon-Nachholung bei erfolgreichem Sync; Konstruktoraufrufe anpassen (R2) | Offen | — |
| 44 | Tests | `KeywordFilterTests_E2E`: `CreateService`-Konstruktoraufruf (`new FeedSyncService`, Zeile 60) um `IFeedIconService`-Fake erweitern (R2) | Offen | — |
| 45 | Tests | `AutoRefreshServiceTests`: `BuildSettings`/`SaveSettingsAsync` (Zeilen 45/60) um `RefreshOnStartupEnabled` ergänzen — Bestandstests setzen `false`, damit die `SyncAllCallCount`-Zähler-Assertions nur Intervall-Ticks zählen; neue Tests: Sofort-Sync bei aktiviertem Schalter, kein Sync bei ausgeschaltetem/offline (R3) | Offen | — |
| 46 | Tests | `UnreadViewModelTests`: `ISettingsRepository`-Abhängigkeit, Übergabe der Sortierrichtung (R4) | Offen | — |
| 47 | Tests | `DelegatingItemRepository` und betroffene Fakes an neue `GetUnreadByDateAsync`-Signatur anpassen (R4) | Offen | — |
| 48 | Tests | `SettingsViewModelTests_Load`/`_Persist`: neue Optionen laden/persistieren, Sortier-Fallback, `LanguageRestartHintVisible`-Verhalten (R3/R4/R5) | Offen | — |
| 49 | Tests | `SettingsViewModelTests_E2E`: Persist-Roundtrips für `RefreshOnStartupEnabled` und `UnreadSortOrder` (R3/R4) | Offen | — |
| 50 | Tests | `SettingsRepositoryTests`/`FeedRepositoryTests`: neue Felder persistieren, `GetAllWithDetailsAsync`-Favicon-Projektion (R2/R3/R4) | Offen | — |
| 51 | Tests | `TestSettingsHelper.SaveAsync` (Pflicht): `new Settings`-Initializer um `RefreshOnStartupEnabled` (`required`) und `UnreadSortOrder`-Übernahme aus dem geladenen Datensatz erweitern (R3/R4) | Offen | — |
| 52 | Tests | Direkte `new Settings`-Initializer um `RefreshOnStartupEnabled` ergänzen (`required`-Kompilierbruch): `SettingsRepositoryTests` (Zeilen 73/100/143/170), `RetentionCleanupServiceTests` (Zeile 43), `SettingsViewModelTests_Load` (Zeilen 50/92), `SettingsViewModelTests_Persist` (Zeilen 387/428/467/527) (R3) | Offen | — |
| 53 | Tests | `ServiceCollectionTests`: `IFeedIconService`-Registrierung ergänzen (R2) | Offen | — |
| 54 | Verifikation | `dotnet test Reporter.sln` und `.\scripts\Run-StaticChecks.ps1` ausführen; Befund dokumentieren | Offen | — |
| 55 | Verifikation | Manuelle UI-Verifikation (390 × 844 pt, Light + Dark): Artikelkarte ohne Bild, Feed-Karte mit Favicon/Initialen, neue Einstellungen, Neustart-Hinweis, Detailansicht ohne Lesezeit; Screenshots in `test-results.md`/`docs/help/anwendung/mobile-ui-design.md` | Offen | — |
| 56 | Verifikation | Generierte Icon-/Splash-Assets sichtprüfen (Windows-Build; iOS via `scripts/iOS-Deployment.ps1`, sofern verfügbar) (R6) | Offen | — |
| 57 | Dokumentation | Hilfeseiten nachziehen: `synchronisation.md` (Start-Abruf), `ungelesen.md` (Sortierung, Lesezeit), `sprache.md` (Hinweis), `feed-suche.md`/`datenmodell.md` (Favicon inkl. Feeds-Seite) | Offen | — |
