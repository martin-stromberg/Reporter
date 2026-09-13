<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Triage (R0)

- [x] GitHub-Issue für R7 angelegt — Issue #81 „Debuginformationen sammeln und per E-Mail versenden" (via `gh issue view 81` verifiziert)
- [x] GitHub-Issue für R8 angelegt — Issue #82 „Systembenachrichtigungen nur bei Hintergrundabruf" (via `gh issue view 82` verifiziert)

### Neue Klassen / Objekte

- [x] `IFeedIconService` (Interface, `src/Reporter.Core/Interfaces/IFeedIconService.cs`) — angelegt, `FindFaviconUrlAsync(string, CancellationToken) → Task<string?>`
- [x] `FeedIconService` (Klasse, `src/Reporter.Core/Services/FeedIconService.cs`) — angelegt: `<link rel="icon|shortcut icon|apple-touch-icon">`-Parsing, relative-URL-Auflösung gegen Basis-URI, `/favicon.ico`-Fallback, Verifikation aller Kandidaten per Request
- [x] `SortOrderOption` (Datenmodellklasse, `src/Reporter.Core/ViewModels/SortOrderOption.cs`) — angelegt (`Value`/`Label`)
- [x] EF-Migration `AddFeedFaviconUrl` (`src/Reporter.Data/Migrations/20260913211308_AddFeedFaviconUrl.cs`) — Spalte `feeds.favicon_url` (TEXT, max. 2048, nullable)
- [x] EF-Migration `AddSettingsStartupRefreshAndSortOrder` (`src/Reporter.Data/Migrations/20260913211351_AddSettingsStartupRefreshAndSortOrder.cs`) — `settings.refresh_on_startup_enabled` (required, Default `true`), `settings.unread_sort_order` (max. 50, nullable) inkl. Seed-`UpdateData`
- [x] `FakeFeedIconService` (Test-Fake, `src/Reporter.Tests/FakeFeedIconService.cs`) — angelegt
- [x] `FeedIconServiceTests` (Testklasse, `src/Reporter.Tests/FeedIconServiceTests.cs`) — angelegt, 7 Tests

### Neue Felder / Eigenschaften

- [x] Feld `FaviconUrl` (`string?`) in `Feed` — vorhanden in `src/Reporter.Core/Models/Feed.cs` (Z. 53) und `src/Reporter.Data/Entities/Feed.cs` (Z. 53)
- [x] Feld `RefreshOnStartupEnabled` (`bool`, `required` im Modell / Default `true` im Entity) in `Settings` — vorhanden in `src/Reporter.Core/Models/Settings.cs` (Z. 64) und `src/Reporter.Data/Entities/Settings.cs` (Z. 66)
- [x] Feld `UnreadSortOrder` (`string?`, Default `SortOrderDescending`) in `Settings` — vorhanden in `src/Reporter.Core/Models/Settings.cs` (Z. 69) und `src/Reporter.Data/Entities/Settings.cs` (Z. 71)
- [x] Konstanten `SortOrderDescending` (`"desc"`) und `SortOrderAscending` (`"asc"`) in `SettingsValues` — vorhanden (`src/Reporter.Core/Models/SettingsValues.cs`, Z. 58/63)
- [x] Feld `FeedFaviconUrl` (`string?`) und berechnete `FeedInitial` in `ItemListItem` — vorhanden (`src/Reporter.Core/Models/ItemListItem.cs`, Z. 68/74); `CopyWith` zieht `FeedFaviconUrl` mit (Z. 107)
- [x] Feld `FaviconUrl` (`string?`) und berechnete `FeedInitial` (aus `Title`) in `FeedListItem` — vorhanden (`src/Reporter.Core/Models/FeedListItem.cs`, Z. 63/69)
- [x] Feld `FeedFaviconUrl` in privater Klasse `ItemListRow` — vorhanden (`src/Reporter.Data/Repositories/ItemRepository.cs`, Z. 427)
- [x] Eigenschaft `RefreshOnStartupEnabled` (Setter → `PersistOnChange`) in `SettingsViewModel` — vorhanden (Z. 280–290)
- [x] Eigenschaften `SortOrderOptions` und `SelectedSortOrder` (Setter → `PersistOnChange`) in `SettingsViewModel` — vorhanden (Z. 116–120, 184, 295–305)
- [x] Eigenschaft `LanguageRestartHintVisible` (`bool`, `private set`) in `SettingsViewModel` — vorhanden (Z. 524–528)

### Geänderte Methoden / Signaturen

- [x] `ReadingTimeEstimator.EstimateText` — liefert `string.Empty` bei Lesezeit ≤ 1 Minute (`src/Reporter.Core/Services/ReadingTimeEstimator.cs`, Z. 35–38)
- [x] `IItemRepository.GetUnreadByDateAsync(page, pageSize, categoryId, ascending = false)` — Signatur erweitert (`src/Reporter.Core/Interfaces/IItemRepository.cs`, Z. 62)
- [x] `ItemRepository.GetUnreadByDateAsync` — `OrderBy(PublishedAt).ThenByDescending(Id)` bei `ascending` (`src/Reporter.Data/Repositories/ItemRepository.cs`, Z. 123–125)
- [x] `ItemRepository.SelectListItemRows` — projiziert `i.Feed.FaviconUrl` (Z. 312); `MapToListItem` befüllt `FeedFaviconUrl` (Z. 330)
- [x] `FeedRepository.MapToModel`/`MapToEntity`/`UpdateAsync` — führen `FaviconUrl` mit (`src/Reporter.Data/Repositories/FeedRepository.cs`, Z. 72/140/156)
- [x] `FeedRepository.GetAllWithDetailsAsync` — projiziert `FaviconUrl` in `FeedListItem` (Z. 110)
- [x] `SettingsRepository.SaveAsync`/`MapToModel` — führen `RefreshOnStartupEnabled` und `UnreadSortOrder` mit (`src/Reporter.Data/Repositories/SettingsRepository.cs`, Z. 63–64/85–86)
- [x] `ReporterDbContext.ConfigureFeed`/`ConfigureSettings` — Spalten `favicon_url` (Z. 90), `refresh_on_startup_enabled` mit Default `true` (Z. 137), `unread_sort_order` (Z. 138)
- [x] `SettingsViewModel.LoadAsync` — lädt neue Felder, Sortier-Fallback auf `"desc"` (Z. 565–567), setzt `LanguageRestartHintVisible = false` (Z. 581)
- [x] `SettingsViewModel.PersistAsync` — schreibt `RefreshOnStartupEnabled` und `UnreadSortOrder` (Z. 752–753)
- [x] `SettingsViewModel.SelectedLanguage`-Setter — setzt/rücksetzt `LanguageRestartHintVisible` bei Abweichung von `_loadedLanguage` außerhalb `_isLoading` (Z. 506–515)
- [x] `UnreadViewModel` — neue `ISettingsRepository`-Abhängigkeit (Z. 24/47); `LoadPageAsync` wertet `UnreadSortOrder` aus und übergibt `ascending` (Z. 307–309)
- [x] `FeedsViewModel` — neue `IFeedIconService`-Abhängigkeit (Z. 22/53); `TryPersistNewFeedAsync(url, title, siteUrl)` mit fehlerisoliertem, `IsOnline`-geguardetem Icon-Abruf (`FeedsViewModel.Search.cs`, Z. 301–362); `SubscribeResultAsync` übergibt `result.SiteUrl` (Z. 391), `DirectAddAsync`/`OfferDirectAddAsync` übergeben `null` (Z. 261/292); `ToFeed` führt `FaviconUrl` mit (`FeedsViewModel.cs`, Z. 525)
- [x] `FeedSyncService` — neue `IFeedIconService`-Abhängigkeit (Z. 26/47); `UpdateFeedHealthAsync` holt `FaviconUrl` bei erfolgreichem Sync nach, wenn `null`, aus `alternate`-Site-Link bzw. Feed-URL-Authority, strikt fehlerisoliert (Z. 280–337)
- [x] `AutoRefreshService.StartAsync` — startet bei `RefreshOnStartupEnabled && IsOnline` fehlerisolierten, nicht abgewarteten `SyncAllAsync` (`src/Reporter.Core/Services/AutoRefreshService.cs`, Z. 46–64); `App.OnStart` unverändert
- [x] `ArticleDetailViewModel.LoadAsync` — `FeedIconUrl` aus `feed?.FaviconUrl` (Z. 296); Fallback-`new Settings` enthält `RefreshOnStartupEnabled` (Z. 273)

### UI / XAML

- [x] `ArticleCardView.xaml` — Standardbild-Kaskade im Thumbnail-`Border`: `Image` auf `ImageUrl`, `Image` auf `FeedFaviconUrl` (MultiTrigger: `ImageUrl` leer + `FeedFaviconUrl` nicht leer), Kreis-`Border` (`RoundRectangle 40`) mit `FeedInitial`-`Label` (MultiTrigger: beide leer); `IsOnline`-Trigger unverändert (Z. 76–132)
- [x] `FeedsPage.xaml` — Bild-Spalte im Feed-Karten-`Grid` (Z. 161–199): `Image` auf `FaviconUrl` (MultiTrigger: nicht leer + `IsOnline` via `x:Reference Page`), Kreis-`Border` mit `FeedInitial`-`Label` bei leerem `FaviconUrl`
- [x] `SettingsPage.xaml` — `Switch`-Zeile „Beim Programmstart abrufen" in **Synchronisation & Lesefluss** (Z. 190–204); `Picker`-Zeile für Sortierrichtung (Z. 205–217); `IsVisible="{Binding LanguageRestartHintVisible}"` auf dem Sprach-Hinweis-`Border` (Z. 501)

### Konfiguration / Assets / Ressourcen

- [x] `AppResources`-Schlüssel EN+DE: `SettingsRefreshOnStartupLabel`/`Hint`, `SettingsSortOrderLabel`, `SettingsSortOrderNewest`/`Oldest` — in `AppResources.resx`, `AppResources.de.resx` und `AppResources.Designer.cs` vorhanden
- [x] `IFeedIconService` als Singleton in `MauiProgram.CreateMauiApp` registriert (`src/Reporter/MauiProgram.cs`, Z. 56)
- [x] `Resources/AppIcon/appiconfg.svg` — Icon-Variante (Badge ohne Text) ersetzt
- [x] `Resources/Splash/splash.svg` — Splash-Variante mit „Reporter"-Wortmarke als `<path>`-Daten (kein `<text>`/`@import` mehr); Füllung `#ffffff` (dokumentierte Abweichung für Sichtbarkeit auf dunklem Hintergrund)
- [x] `Reporter.csproj` — `MauiIcon`/`MauiSplashScreen`-Verweise, `Color="#1e293b"`, `BaseSize="128,128"` unverändert und verifiziert
- [x] `Platforms/iOS/Info.plist` und `Package.appxmanifest` — unverändert

### Tests

- [x] `ReadingTimeEstimatorTests` — `EstimateText_OneMinuteContent_ReturnsEmpty`, `EstimateText_TwoMinuteContent_ReturnsText` (der frühere Kurzinhalt-Test ist in `EstimateText_OneMinuteContent_ReturnsEmpty` aufgegangen)
- [x] `ItemRepositoryTests` — `GetUnreadByDateAsync_OneMinuteReadingTime_ProjectsEmpty`, `_ProjectsFeedFaviconUrl`, `_Ascending_OrdersByPublishedAtAscending`, `_Ascending_NullPublishedAt_SortsFirst` (dokumentiertes NULL-Verhalten)
- [x] `FeedsViewModelTests` — `DirectAddCommand_StoresFaviconUrl`, `SubscribeResultCommand_UsesSiteUrlForFaviconLookup`, `DirectAddCommand_IconLookupFails_FeedStillAdded`, `DirectAddCommand_Offline_SkipsIconLookup`, `RenameFeedAsync_PreservesFaviconUrl`; `FakeFeedIconService`-Abhängigkeit
- [x] `FeedSyncServiceTests` — `SyncFeedAsync_MissingFavicon_BackfillsFromFeedAuthority`, `_ExistingFavicon_SkipsLookup`, `_FaviconLookupFails_SyncStillSucceeds`; Konstruktoraufrufe um `_feedIconService` erweitert
- [x] `KeywordFilterTests_E2E` — `new FeedSyncService(...)` um `new FakeFeedIconService()` erweitert (Z. 60)
- [x] `AutoRefreshServiceTests` — `BuildSettings`/`SaveSettingsAsync` setzen `RefreshOnStartupEnabled` explizit (`false` in Bestandstests); neue Tests `StartAsync_StartupRefreshEnabled_SyncsImmediately`, `_StartupRefreshDisabled_DoesNotSyncImmediately`, `_Offline_SkipsStartupSync`, `_StartupSyncThrows_StartStillCompletes`
- [x] `UnreadViewModelTests` — `ISettingsRepository`-Abhängigkeit; `LoadPage_PassesSortOrderFromSettings`, `LoadPage_InvalidSortOrder_UsesDescending`
- [x] `DelegatingItemRepository` — neue `GetUnreadByDateAsync`-Signatur delegiert (Z. 45–46)
- [x] `SettingsViewModelTests_Load`/`_Persist` — `Load_PopulatesRefreshOnStartup`, `Load_PopulatesSelectedSortOrder`, `Load_InvalidPersistedValues_UsesFallbacks`, `RefreshOnStartup_Change_Persists`, `SelectedSortOrder_Change_Persists`, `LanguageChange_SetsRestartHint`, `LanguageReverted_ClearsRestartHint`, `Load_ResetsRestartHint`; direkte `new Settings`-Initializer um `RefreshOnStartupEnabled` ergänzt
- [x] `SettingsViewModelTests_E2E` — `E2E_RefreshOnStartup_PersistRoundtrip`, `E2E_SortOrder_PersistRoundtrip`
- [x] `SettingsRepositoryTests` — `SaveAsync_PersistsStartupRefreshAndSortOrder`; alle `new Settings`-Initializer mit `RefreshOnStartupEnabled`
- [x] `FeedRepositoryTests` — `UpdateAsync_PersistsFaviconUrl`, `GetAllWithDetailsAsync_ProjectsFaviconUrl`
- [x] `RetentionCleanupServiceTests` — `new Settings`-Initializer übernimmt `RefreshOnStartupEnabled` aus geladenem Datensatz (Z. 55)
- [x] `TestSettingsHelper.SaveAsync` — übernimmt `RefreshOnStartupEnabled` (`required`) und `UnreadSortOrder` (Z. 50–51)
- [x] `ServiceCollectionTests` — `IFeedIconService`-Registrierung in `AddReporterServices_ResolvesFeedSyncService` assertiert (Z. 83)

### Verifikation / Dokumentation

- [x] `dotnet test` + `Run-StaticChecks.ps1` — dokumentiert in `test-results.md` (400/400 Tests, Exit-Code 0)
- [x] Manuelle UI-Verifikation — 390 × 844 pt, Light + Dark, 16 Screenshots unter `test-results/issue-77/manual-*.png`, dokumentiert in `test-results.md`
- [x] Icon-/Splash-Sichtprüfung — generierte Resizetizer-Assets im Windows-Build gesichtet (`test-results.md`); iOS auf Windows-Arbeitsplatz nicht möglich, als Nachholbedarf dokumentiert (Plan: „sofern verfügbar")
- [x] Hilfeseiten — `synchronisation.md`, `ungelesen.md`, `sprache.md`, `feed-suche.md`, `datenmodell.md` sowie `einstellungen/beschreibung.md` inhaltlich aktualisiert

## Hinweise

- `src/Reporter/Resources/AppIcon/appiconfg.svg` enthält noch einen toten `<style>`-Block mit `@import` der Google-Fonts-Newsreader und der ungenutzten Klasse `.brand-text` (Zeilen 5–16) — die Icon-Variante wurde wie geplant „unverändert" übernommen und enthält kein `<text>`-Element. Da der Release-Build und die Resizetizer-Ausgabe laut `test-results.md` erfolgreich waren, ist das unkritisch; bei zukünftigen Asset-Änderungen kann der Block entfernt werden.
- Dokumentierte Abweichung R6: Die Wortmarke im Splash wurde nach Pfadkonvertierung weiß (`#ffffff`) statt dunkel (`#1e293b`) eingefärbt, damit sie auf dem dunklen Splash-Hintergrund sichtbar bleibt (in `plan.md` Abschnitt „Umsetzungsstand" vermerkt).
- Die Migrationen werden in den Tests nicht ausgeführt (Test-DBs nutzen `EnsureCreated`); sie wurden auf der migrierten App-DB in der manuellen Verifikation mitbenutzt (`settings.refresh_on_startup_enabled=1`, `feeds.favicon_url` befüllt).
