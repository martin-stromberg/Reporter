# Bestandsaufnahme: Einstellungen, Aufbewahrungsdauer, Keyword-Filter und Löschlogik (Issue #26)

Bestandsaufnahme des bestehenden Codes der .NET-MAUI-App „Reporter" bezogen auf die Anforderung in [`requirement.md`](requirement.md): Ausbau der Platzhalter-`SettingsPage`, Erweiterung des `RetentionCleanupService` um die Keyword-Löschregel sowie eine konfigurierbare Hintergrund-Aktualisierung.

## Zusammenfassung

- **`Settings` (Singleton) existiert bereits** in beiden Schichten: `Reporter.Data.Entities.Settings` und `Reporter.Core.Models.Settings` mit `DefaultId`, `RetentionDays` (30), `AutoMarkReadMode` (`"on_scroll"`), `AutoMarkReadDelaySeconds` (5), `NotificationsEnabled`, `QuietHoursStart`, `QuietHoursEnd`. Die in der Anforderung genannten neuen Felder (`AutoRefreshEnabled`, `RefreshIntervalMinutes`, `Theme`, ggf. `AutoMarkReadOnOpenEnabled`) existieren **noch nicht** — weder im Modell noch als Spalten in der einzigen Migration `20260909214617_InitialCreate`.
- **`Keyword` existiert bereits** als Entität und Core-Modell (`Id`, `KeywordText`), Tabelle `keywords` mit Unique-Index auf `keyword_text`. `IKeywordRepository`/`KeywordRepository` sind vollständig implementiert und registriert.
- **`RetentionCleanupService` existiert bereits** und löscht über `IItemRepository.DeleteExpiredAsync` ausschließlich gelesene, nicht gespeicherte Artikel (`IsRead && !IsSavedForLater && (ReadAt ?? PublishedAt) < cutoff`). Er wird in `App.OnStart` mit Fehlerisolierung (`try/catch` + `Debug.WriteLine`) aufgerufen. Eine Keyword-Auswertung oder ein `IKeywordRepository`-Zugriff ist **nicht** vorhanden.
- **`SettingsViewModel`/`SettingsPage` sind Platzhalter**: Das ViewModel lädt nur den `Settings`-Datensatz (`LoadCommand`, `Settings`-Property, `Title`); die Page zeigt eine Karte mit `PlaceholderSettings`-Text. Keine Commands für Keywords, keine bindbaren Optionen, keine Sofort-Persistierung.
- **Hintergrund-Aktualisierung existiert nicht**: Es gibt keinen Timer/Auto-Refresh-Service; `IFeedSyncService.SyncAllAsync` wird nur aus ViewModels (`RefreshCommand`/`RefreshAllCommand`) aufgerufen.
- **Theme-Verwaltung existiert nicht**: `Application.UserAppTheme` wird nirgends gesetzt; die App folgt dem System-Theme. Farbressourcen sind vollständig als `Light*`/`Dark*`-Paare mit `AppThemeBinding` angelegt (`src/Reporter/Resources/Styles/Colors.xaml`).
- **`ArticleDetailViewModel` liest `AutoMarkReadDelaySeconds`**, ignoriert aber `AutoMarkReadMode` und kennt nur den lokalen Toggle `IsAutoMarkRead` (Standard `true`). Fallback-`Settings` mit `required init`-Werten in Zeilen 238–246.
- **Keine Enums im Projekt**: Die Konvention sind String-Konstanten (`FeedHealth.Ok`/`Warning`/`Error`, `AutoMarkReadMode = "on_scroll"`).
- **UI-Muster sind etabliert**: `Grid RowDefinitions="Auto,*"`, kartenbasierte `CollectionView` mit `Border` + `RoundRectangle 12`, `TapGestureRecognizer` + `DisplayActionSheetAsync` für Aktionen, 44×44-pt-Touch-Ziele, `AppThemeBinding` überall, `{x:Static strings:AppResources.*}` für Texte, `Shell.NavBarIsVisible="False"`. Design-Entwurf für die Settings-Seite liegt vor: `design-draft/stitch_local_rss_feed_reader/einstellungen_filter/` (Light) und `einstellungen_filter_dark_mode/` mit je `screen.png` und `code.html`.
- **Test-Ausgangszustand:** `dotnet test src/Reporter.Tests/Reporter.Tests.csproj` (CI-Befehl inkl. Coverage-Settings und TRX-Logger) — **90 Tests, 90 bestanden, 0 fehlgeschlagen, 0 übersprungen, Exit-Code 0**. Details und Nachweise: [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md) — `Settings`, `Item`, `Keyword` (Entitäten + Core-Modelle), DbContext-Konfiguration und Migrationsstand
- [Logik](inventory/logic.md) — `RetentionCleanupService`, `SettingsRepository`, `ItemRepository.DeleteExpiredAsync`, `KeywordRepository`, `FeedSyncService`, `SettingsViewModel`, `ArticleDetailViewModel`, `App.OnStart`, DI-Registrierung
- [Interfaces](inventory/interfaces.md) — `ISettingsRepository`, `IKeywordRepository`, `IItemRepository`, `IRetentionCleanupService`, `IFeedSyncService`
- [UI und Design-Entwurf](inventory/ui.md) — Bestehende Page-/ViewModel-Muster, `AppThemeBinding`-Ressourcen, Mobile-UI-Regeln aus `AGENTS.md`, Inhalt des Design-Entwurfs `einstellungen_filter`
- [Tests](inventory/tests.md) — Test-Ausgangszustand mit Nachweisen, bestehende Testklassen und Hilfsmethoden

Hinweis: Es gibt keine Enums im Projekt; stattdessen werden String-Konstanten verwendet (`FeedHealth`, `AutoMarkReadMode`). Ein separates `enums.md` wurde daher nicht angelegt — die Konvention ist in [models.md](inventory/models.md) und [logic.md](inventory/logic.md) vermerkt.
