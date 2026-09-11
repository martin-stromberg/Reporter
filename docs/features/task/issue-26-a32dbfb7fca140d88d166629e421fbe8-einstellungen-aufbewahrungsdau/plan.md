# Umsetzungsplan: Einstellungen, Aufbewahrungsdauer, Keyword-Filter und Löschlogik (Issue #26)

## Übersicht

Die Platzhalter-`SettingsPage` wird zur vollständigen Einstellungsseite ausgebaut (Aufbewahrungsdauer, Keyword-Filter, Synchronisation & Lesefluss, Benachrichtigungen & Ruhezeiten, Erscheinungsbild) mit Sofort-Persistierung über `ISettingsRepository.SaveAsync`. Der `RetentionCleanupService` wird um die Keyword-Löschregel erweitert; die Invarianten (niemals ungelesene, niemals `IsSavedForLater`-Artikel) bleiben absolut. Neu sind außerdem ein `AutoRefreshService` (zeitgesteuerter `IFeedSyncService.SyncAllAsync`-Aufruf) und eine Theme-Verwaltung über `Application.UserAppTheme`.

**Scope-Grenze:** Die im Design-Entwurf gezeigten Elemente „Lokalen Cache leeren", „Datenbank & Datensicherung" (OPML-Export, JSON-Backup, DB-Größenanzeige) sowie der Ruhezeiten-Status-Badge „Aktiv" sind **nicht** Teil der Anforderung und werden nicht umgesetzt. Ruhezeiten-Auswertung, pro-Feed-Benachrichtigungen und Keyword-Einfluss auf Benachrichtigungen gehören zum separaten Benachrichtigungs-Arbeitspaket — hier werden nur die Felder persistiert.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Keyword-Matching-Zeitpunkt | Matching zur **Cleanup-Zeit** gegen gespeicherte Artikelinhalte; **kein** `IsKeywordFiltered`-Flag am `Item` | Die Lösch-Invarianten bleiben absolut (geklärt: niemals ungelesene, niemals `Später`-Artikel). Ein Sync-Zeit-Flag müsste bei jeder Keyword-Änderung alle Artikel neu bewerten; Cleanup-Zeit-Matching braucht keine `items`-Schemaänderung und wertet immer den aktuellen Keyword-Stand aus. |
| Match-Felder des Keyword-Filters | `Item.Title` **und** `Item.ContentHtml` (case-insensitiv, Teilwort via `Contains` mit `StringComparison.OrdinalIgnoreCase`); `Link` wird **nicht** gematcht | Blacklist-Semantik: Artikel werden anhand ihres Inhalts gefiltert, nicht nur der Überschrift. URLs in `Link` sind opak und würden Zufallstreffer erzeugen. Semantik ist per Anforderung fix (Teilwort + Case-Insensitive). |
| Löschregel für gefilterte Artikel | Kandidaten: `IsRead && !IsSavedForLater && (PublishedAt ?? ReadAt) < cutoff` — d. h. Fristbasis ist das **Artikelalter** (`PublishedAt`), nicht der Lesezeitpunkt | Würde die Keyword-Regel denselben Zeitstempel `(ReadAt ?? PublishedAt)` nutzen, wäre sie eine leere Teilmenge der bestehenden Regel und damit wirkungslos. Sinn der Blacklist: unerwünschte Artikel werden nach Ablauf der Aufbewahrungsfrist entfernt, auch wenn sie zuletzt gelesen wurden. Invarianten bleiben unverändert. |
| Orchestrierung Keyword-Löschung | `RetentionCleanupService` lädt Keywords via `IKeywordRepository`, filtert Kandidaten im Speicher via `IKeywordMatcher`, löscht per neuem `IItemRepository.DeleteRangeAsync` | `OrdinalIgnoreCase`-Teilwort-Matching ist in SQL/EF nicht zuverlässig übersetzbar (SQLite-`LIKE` ist nur ASCII-case-insensitiv). Die Kandidatenmenge (abgelaufene gelesene Artikel) ist klein und lokal. `ItemRepository`-Konstruktor bleibt unverändert → keine Brüche in `ItemRepositoryTests` u. a. |
| `AutoMarkReadMode` vs. Bool | Vorhandenes String-Feld `AutoMarkReadMode` weiterverwenden; UI-Toggle schreibt `"on_open"`/`"off"`; `ArticleDetailViewModel` behandelt nur `"off"` als deaktiviert | Projekt-Konvention sind String-Konstanten statt Enums (`FeedHealth`, `"on_scroll"`). `AutoMarkReadMode != "off"` hält den Seed-Default `"on_scroll"` und den bestehenden Fallback `"on_open"` rückwärtskompatibel aktiviert. Kein neues Feld/Migrationsspalte nötig. |
| `Theme`-Darstellung | Neues Feld `Theme` (`string?`, Werte `"system"`/`"light"`/`"dark"`, Standard `"system"`) — String-Konvention, kein Enum | Projekt kennt keine Enums (`models.md`, Enum-Konvention). Mapping auf `AppTheme.Unspecified`/`Light`/`Dark` erfolgt in `AppThemeService`. |
| Theme-Anwendung aus Core-ViewModel | Neues Interface `IAppThemeService` (Core) + Implementierung `AppThemeService` im MAUI-Projekt | `Reporter.Core` ist `net10.0` ohne MAUI-Referenz — `Application.Current.UserAppTheme` ist dort nicht verfügbar. Abstraktion hält `SettingsViewModel` testbar (Tests referenzieren nur Core/Data). |
| Hintergrund-Aktualisierung | `AutoRefreshService` in `Reporter.Core` mit `PeriodicTimer` über injiziertem `TimeProvider` (Standard `TimeProvider.System`); Overlap-Guard via `SemaphoreSlim`/`Interlocked` | `PeriodicTimer` ist reine BCL — funktioniert ohne MAUI-Abhängigkeit und ohne `IDispatcherTimer`. `TimeProvider`-Injection macht den Timer mit `FakeTimeProvider` testbar (sonst müssten Tests Minuten warten). Timer läuft nur, solange die App läuft (kein OS-Background-Fetch — nicht angefordert). |
| `RefreshIntervalMinutes`-Eingabe | Feste Auswahlliste {15, 30, 60, 240} Minuten als `Picker` mit Klartext-Labels; persistiert als `int` Minuten, Standard 30 | Design-Entwurf zeigt exakt diese vier Optionen; Auswahl benannter Werte über Klartext-Dropdown statt freier Zahleneingabe (AGENTS.md/Plan-Konvention). |
| `RetentionDays`-Eingabe | `Slider` (1–365) mit Live-Wertanzeige; Persistierung erst über `DragCompletedCommand` | Design-Entwurf zeigt Slider mit Skala 1/90/180/365. Sofort-Speichern bei jedem Slider-Schritt würde die DB bei jedem Pixel fluten — DragCompleted ist der natürliche Commit-Punkt; alle anderen Controls persistieren sofort bei Änderung. |
| Keyword-Darstellung/-Entfernung | Chips via `FlexLayout Wrap="Wrap"` + `BindableLayout.ItemsSource`; Entfernen-„×"-`Button` im Chip mit `MinimumWidthRequest`/`MinimumHeightRequest` 44 | Design-Entwurf zeigt Chips mit ×-Button. 44-pt-Touch-Ziel ist Pflicht (AGENTS.md); der ×-Button wird daher auf 44×44 pt dimensioniert, Chip-Mindesthöhe 44 pt. |
| Cleanup-Auslöser | Bleibt ausschließlich `App.OnStart` mit bestehender Fehlerisolierung; **kein** zusätzlicher periodischer/Post-Sync-Lauf | Anforderung: „der Aufrufpunkt `App.OnStart` mit Fehlerisolierung bleibt bestehen". Ein Post-Sync-Cleanup ist nicht angefordert (kein Extra). |
| Ruhezeiten über Mitternacht | Zulässig; UI speichert `QuietHoursStart`/`QuietHoursEnd` ohne Start<Ende-Validierung | Design-Entwurf zeigt 22:00–07:00. Die Auswertung (Wrap-around-Logik) gehört ins Benachrichtigungs-Paket; hier nur persistieren. |
| „Teilwort & Case-Insensitive"-Toggle | Als `Switch IsToggled="True" IsEnabled="False"` + Erläuterungstext darstellen | Design zeigt aktivierten Toggle; Anforderung: feste Semantik, keine konfigurierbare Option. Deaktivierter aktiver Switch visualisiert „immer an" ohne Fake-Interaktion. |
| Toast „Änderungen gespeichert" | Kein Toolkit-Toast; Sofortwirkung der Controls ist die Rückmeldung; optional kurz einblendbares `Label` „Gespeichert" per `FadeTo` im Code-Behind | `CommunityToolkit.Maui` ist nicht referenziert (kein `Toast.Make`); kein neues NuGet-Paket für einen Hinweis-Toast. |

## Programmabläufe

### Einstellungen laden und anzeigen

1. `SettingsPage.OnAppearing` führt `SettingsViewModel.LoadCommand` aus (bestehendes Muster).
2. `LoadAsync` lädt `Settings` via `ISettingsRepository.GetAsync` und die Keyword-Liste via `IKeywordRepository.GetAllAsync`.
3. ViewModel setzt unter `_isLoading`-Guard (verhindert Persistierung während des Befüllens) alle bindbaren Eigenschaften: `RetentionDays`, `AutoRefreshEnabled`, `SelectedRefreshInterval`, `AutoMarkReadEnabled` (`AutoMarkReadMode != "off"`), `SelectedAutoMarkReadDelay`, `NotificationsEnabled`, `QuietHoursStart`, `QuietHoursEnd`, `SelectedTheme`, `Keywords` (`ObservableCollection<Keyword>`).

Beteiligte Klassen/Komponenten: `SettingsPage`, `SettingsViewModel`, `ISettingsRepository`, `IKeywordRepository`

### Einstellung ändern (Sofort-Persistierung)

1. Setter einer Optionseigenschaft (z. B. `AutoRefreshEnabled`, `SelectedTheme`) erkennt Änderung und ruft bei `!_isLoading` `PersistAsync` auf.
2. `PersistAsync` baut aus den ViewModel-Eigenschaften das immutable Core-`Settings`-Objekt (`init`-Kopie des geladenen Datensatzes mit geänderten Feldern) und ruft `ISettingsRepository.SaveAsync`.
3. Nach erfolgreichem Speichern: bei Theme-Änderung `IAppThemeService.ApplyTheme(Theme)`; bei Auto-Refresh-Änderung `IAutoRefreshService.ApplySettingsAsync(settings)` (Timer neu konfigurieren).
4. Fehler werden per `Debug.WriteLine` protokolliert und optional über `ErrorMessage`/`HasError` angezeigt.

Beteiligte Klassen/Komponenten: `SettingsViewModel`, `ISettingsRepository`, `IAppThemeService`, `IAutoRefreshService`

### Keyword hinzufügen

1. `AddKeywordCommand` (Button „+ Hinzufügen" oder `Entry.ReturnCommand`) trimmt `NewKeywordText`.
2. Leerer Text → `ErrorMessage` (`ErrorKeywordEmpty`), `HasError = true`, Abbruch.
3. Text länger als 500 Zeichen (DB-Spalte `keyword_text` max. 500) → `ErrorMessage` (`ErrorKeywordTooLong`), `HasError = true`, Abbruch.
4. Case-insensitiver Dubletten-Check gegen `Keywords` (`OrdinalIgnoreCase`, da der Unique-Index auf `keyword_text` case-sensitiv ist) → bei Treffer `ErrorMessage` (`ErrorKeywordDuplicate`), Abbruch.
5. Sonst `IKeywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = text })`, Aufnahme in `Keywords`, `NewKeywordText` leeren, `HasError` zurücksetzen.

Beteiligte Klassen/Komponenten: `SettingsViewModel`, `IKeywordRepository`, `Keyword`

### Keyword entfernen

1. `RemoveKeywordCommand` (×-Button im Chip, `CommandParameter` = `Keyword`) ruft `IKeywordRepository.DeleteAsync(keyword.Id)`.
2. Keyword wird aus `Keywords` entfernt.

Beteiligte Klassen/Komponenten: `SettingsViewModel`, `IKeywordRepository`

### Retention-Cleanup mit Keyword-Regel

1. `App.OnStart` ruft wie bisher `IRetentionCleanupService.CleanupAsync` mit `try/catch` + `Debug.WriteLine` auf.
2. `CleanupAsync` lädt `Settings`; bei `RetentionDays <= 0` Rückgabe `0` (bestehende Regel gilt für beide Löschregeln).
3. `cutoff = UtcNow - RetentionDays`; `IItemRepository.DeleteExpiredAsync(cutoff)` löscht wie bisher (`IsRead && !IsSavedForLater && (ReadAt ?? PublishedAt) < cutoff`).
4. `IKeywordRepository.GetAllAsync` liefert die Keyword-Texte; bei leerer Liste Ende.
5. `IItemRepository.GetExpiredKeywordCandidatesAsync(cutoff)` lädt Kandidaten (`IsRead && !IsSavedForLater && (PublishedAt ?? ReadAt) < cutoff`) — kleine, lokale Menge.
6. `IKeywordMatcher.MatchesAny(item.Title, item.ContentHtml, keywordTexts)` filtert Treffer im Speicher (`Contains`, `OrdinalIgnoreCase`).
7. `IItemRepository.DeleteRangeAsync(matchedIds)` löscht per `ExecuteDeleteAsync` auf IDs; Rückgabewert = Summe beider Löschungen.

Beteiligte Klassen/Komponenten: `App`, `RetentionCleanupService`, `ISettingsRepository`, `IKeywordRepository`, `IItemRepository`, `IKeywordMatcher`

### Hintergrund-Aktualisierung

1. `App.OnStart` ruft nach Theme-Anwendung `IAutoRefreshService.StartAsync` mit Fehlerisolierung (`try/catch` + `Debug.WriteLine`) auf.
2. `StartAsync` lädt `Settings`; bei `AutoRefreshEnabled` startet ein Hintergrund-Task mit `PeriodicTimer` (`TimeSpan.FromMinutes(RefreshIntervalMinutes)`, über `TimeProvider`).
3. Jeder Timer-Tick: Overlap-Guard prüft, ob bereits ein Sync läuft — falls ja, Tick überspringen; sonst `IFeedSyncService.SyncAllAsync`. Exceptions im Loop werden abgefangen und per `Debug.WriteLine` protokolliert (Timer läuft weiter).
4. `ApplySettingsAsync(settings)` stoppt den laufenden Loop (`CancellationTokenSource`) und startet bei `AutoRefreshEnabled` einen neuen mit aktuellem Intervall; wird vom `SettingsViewModel` nach jeder Persistierung relevanter Felder aufgerufen.
5. `StopAsync` beendet Loop und CTS (für Dispose/Tests).

Beteiligte Klassen/Komponenten: `App`, `AutoRefreshService`, `ISettingsRepository`, `IFeedSyncService`, `TimeProvider`

### Theme beim App-Start anwenden

1. `App.OnStart` lädt nach `MigrateAsync` die `Settings` via `ISettingsRepository.GetAsync` (eigener `try/catch`-Block).
2. `IAppThemeService.ApplyTheme(settings.Theme)` setzt `Application.Current.UserAppTheme`: `"light"` → `AppTheme.Light`, `"dark"` → `AppTheme.Dark`, sonst (`"system"`/`null`/unbekannt) → `AppTheme.Unspecified`.
3. Bestehende `AppThemeBinding`-Styles schalten dadurch sofort um.

Beteiligte Klassen/Komponenten: `App`, `ISettingsRepository`, `IAppThemeService`, `AppThemeService`

### Auto-Gelesen beim Öffnen (globale Einstellung)

1. `ArticleDetailViewModel.LoadAsync` liest neben `AutoMarkReadDelaySeconds` neu `settings.AutoMarkReadMode`.
2. Timer `MarkReadDelayedAsync` startet nur, wenn `IsAutoMarkRead && settings.AutoMarkReadMode != "off" && !Item.IsRead`.
3. Verzögerung: `settings.AutoMarkReadDelaySeconds >= 0` zulassen (bisher `> 0`, sonst könnte die neue Option „Sofort" = 0 nicht greifen); Fallback bleibt `DefaultAutoMarkDelaySeconds`.
4. Der lokale `IsAutoMarkRead`-Toggle bleibt als sitzungsbezogene Abwahl bestehen.

Beteiligte Klassen/Komponenten: `ArticleDetailViewModel`, `ISettingsRepository`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `IKeywordMatcher` (`Reporter.Core/Interfaces/IKeywordMatcher.cs`) | Interface | Zentrales Keyword-Matching: `MatchesAny(string? title, string? contentHtml, IEnumerable<string> keywords)` → `bool`; Wiederverwendung durch Cleanup und späteres Benachrichtigungs-Paket |
| `KeywordMatcher` (`Reporter.Core/Services/KeywordMatcher.cs`) | Klasse | Implementierung: `Contains` mit `StringComparison.OrdinalIgnoreCase` auf Titel und Content |
| `IAutoRefreshService` (`Reporter.Core/Interfaces/IAutoRefreshService.cs`) | Interface | `StartAsync(CancellationToken)`, `ApplySettingsAsync(Settings)`, `StopAsync()` — Steuerung des Hintergrund-Sync-Timers |
| `AutoRefreshService` (`Reporter.Core/Services/AutoRefreshService.cs`) | Klasse | `PeriodicTimer`-Loop über `TimeProvider`, ruft `IFeedSyncService.SyncAllAsync`, Overlap-Guard, Fehlerisolierung im Loop |
| `IAppThemeService` (`Reporter.Core/Interfaces/IAppThemeService.cs`) | Interface | `ApplyTheme(string? theme)` — Abstraktion, damit Core-ViewModel ohne MAUI-Referenz das Theme schalten kann |
| `AppThemeService` (`src/Reporter/Services/AppThemeService.cs`) | Klasse | Setzt `Application.Current.UserAppTheme` anhand des Theme-Strings |
| `RefreshIntervalOption` (`Reporter.Core/ViewModels/`) | Datenmodellklasse | `int Minutes` + `string Label` (lokalisiert); `ItemsSource` für Intervall-`Picker` (Werte 15/30/60/240) |
| `AutoMarkReadDelayOption` (`Reporter.Core/ViewModels/`) | Datenmodellklasse | `int Seconds` + `string Label`; `ItemsSource` für Verzögerungs-`Picker` (Werte 0/1/3/5, „Sofort" = 0) |
| `ThemeOption` (`Reporter.Core/ViewModels/`) | Datenmodellklasse | `string Value` + `string Label`; `ItemsSource` für Theme-`Picker` (`"system"`/`"light"`/`"dark"`, lokalisierte Labels) |

## Änderungen an bestehenden Klassen

### `Reporter.Data.Entities.Settings` (Datenmodellklasse)

- **Neue Eigenschaften:** `AutoRefreshEnabled` (`bool`, Standard `true`) — automatische Hintergrund-Aktualisierung
- **Neue Eigenschaften:** `RefreshIntervalMinutes` (`int`, Standard `30`) — Abruf-Intervall in Minuten
- **Neue Eigenschaften:** `Theme` (`string?`, Standard `"system"`) — Erscheinungsbild (`"system"`/`"light"`/`"dark"`)

### `Reporter.Core.Models.Settings` (Datenmodellklasse, immutable)

- **Neue Eigenschaften:** `AutoRefreshEnabled` (`required bool init`) — Konvention: nicht-nullable Skalare sind `required`
- **Neue Eigenschaften:** `RefreshIntervalMinutes` (`required int init`)
- **Neue Eigenschaften:** `Theme` (`string? init`, Standard `"system"`) — optional wie `AutoMarkReadMode`
- Folge: Alle Konstruktionsstellen des Core-Modells müssen die neuen `required`-Felder setzen — `SettingsRepository.MapToModel` (Zeile 65), `ArticleDetailViewModel`-Fallback (Zeilen 238–246), `RetentionCleanupServiceTests.SetRetentionDaysAsync` (Zeilen 39–48), `SettingsRepositoryTests` (Zeilen 71–77, 93–99).

### `ReporterDbContext` (`ConfigureSettings`)

- **Geänderte Methoden:** `ConfigureSettings` — neue Spalten `auto_refresh_enabled` (required), `refresh_interval_minutes` (required), `theme` (max. 50, nullable); `HasData(new Settings())` übernimmt die Entity-Defaults automatisch.

### `SettingsRepository`

- **Geänderte Methoden:** `SaveAsync` — kopiert zusätzlich `AutoRefreshEnabled`, `RefreshIntervalMinutes`, `Theme` auf den Singleton-Datensatz
- **Geänderte Methoden:** `MapToModel` — mappt die drei neuen Felder ins Core-Modell

### `IItemRepository` (Interface)

- **Neue Methoden:** `GetExpiredKeywordCandidatesAsync(DateTime cutoff, CancellationToken cancellationToken = default)` → `Task<IReadOnlyList<Item>>` — gelesene, nicht gespeicherte Items mit `(PublishedAt ?? ReadAt) < cutoff`
- **Neue Methoden:** `DeleteRangeAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)` → `Task<int>` — Batch-Löschung per IDs via `ExecuteDeleteAsync`

### `ItemRepository`

- **Neue Methoden:** Implementierung der beiden Interface-Methoden; Konstruktor (`IDbContextFactory<ReporterDbContext>`) bleibt unverändert.

### `RetentionCleanupService`

- **Geänderte Methoden:** Konstruktor — zusätzlich `IKeywordRepository` und `IKeywordMatcher` injizieren
- **Geänderte Methoden:** `CleanupAsync` — nach bestehender `DeleteExpiredAsync`-Löschung Keyword-Regel ausführen (siehe Programmablauf); beide Löschzahlen summieren

### `SettingsViewModel`

- **Geänderte Methoden:** Konstruktor — zusätzlich `IKeywordRepository`, `IAutoRefreshService`, `IAppThemeService` injizieren; neue Commands anlegen
- **Neue Eigenschaften:** `Keywords` (`ObservableCollection<Keyword>`), `NewKeywordText` (`string`), `RetentionDays` (`double` für `Slider`), `RetentionDaysText` (lokalisierte Anzeige „n Tage"), `AutoRefreshEnabled` (`bool`), `RefreshIntervalOptions` + `SelectedRefreshInterval`, `AutoMarkReadEnabled` (`bool`), `AutoMarkReadDelayOptions` + `SelectedAutoMarkReadDelay`, `NotificationsEnabled` (`bool`), `QuietHoursStart`/`QuietHoursEnd` (`TimeSpan?` für `TimePicker`), `ThemeOptions` + `SelectedTheme`, `HasError` (`bool`), `ErrorMessage` (`string`)
- **Neue Methoden:** `AddKeywordCommand` (`AsyncRelayCommand`), `RemoveKeywordCommand` (`AsyncRelayCommand<Keyword>`), `SaveRetentionCommand` (`RelayCommand` für `Slider.DragCompleted`), `PersistAsync` (baut `Settings`-Kopie, `SaveAsync`, ruft `IAppThemeService`/`IAutoRefreshService`)
- **Geänderte Methoden:** `LoadAsync` — lädt zusätzlich Keywords und befüllt alle Optionseigenschaften unter `_isLoading`-Guard

### `ArticleDetailViewModel`

- **Geänderte Methoden:** `LoadAsync` — `AutoMarkReadMode`-Auswertung (Timer nur bei `!= "off"`); Delay-Bedingung von `> 0` auf `>= 0` (Option „Sofort"); Fallback-`Settings` um die neuen `required`-Felder ergänzen (Zeilen 238–246)

### `App` (`App.xaml.cs`)

- **Geänderte Methoden:** `OnStart` — nach `MigrateAsync`: Settings laden + `IAppThemeService.ApplyTheme(settings.Theme)`; danach `IAutoRefreshService.StartAsync` — beides jeweils mit eigenem `try/catch` + `Debug.WriteLine` analog zum Cleanup-Block

### `MauiProgram` (`CreateMauiApp`)

- **Neue Registrierungen:** `AddSingleton<IKeywordMatcher, KeywordMatcher>()`, `AddSingleton<IAutoRefreshService, AutoRefreshService>()`, `AddSingleton<IAppThemeService, AppThemeService>()` — analog zum bestehenden Schema

### `SettingsPage` (`SettingsPage.xaml` / `.xaml.cs`)

- Vollständiger Ausbau gemäß Design-Entwurf `design-draft/stitch_local_rss_feed_reader/einstellungen_filter/` (`screen.png` + `code.html`, Light) und `einstellungen_filter_dark_mode/` — Sektionen als `Border`-Karten (`RoundRectangle 12`, `AppThemeBinding SurfaceContainer`) im bestehenden `ScrollView` unter `Grid RowDefinitions="Auto,*"` (kein `CollectionView` nötig — Formularseite; kein Verschachteln von `ScrollView`/`CollectionView`).
- **Verbindliche Muster-Wiederverwendung aus `inventory/ui.md`:** Header-`Label` mit `HeadlineStyle` + `{x:Static strings:AppResources.*}`; `Entry` + `ReturnCommand` + `Button` und Fehler-`Label` mit `HasError`/`LightError`/`DarkError` nach `FeedsPage`-Formularmuster (Zeilen 19–35); `Picker` mit `ItemDisplayBinding` nach `FeedsPage` (Zeile 26–29); Touch-Ziele ≥ 44 pt (`MinimumHeightRequest`/`MinimumWidthRequest`, Muster `UnreadPage.xaml` Zeilen 22–24/74–75); `AppThemeBinding` auf allen Farben; `Shell.NavBarIsVisible="False"`; keine horizontalen Datentabellen.
- **Controls je Sektion:** `Slider` (1–365, `DragCompletedCommand`) + Wert-/Skalen-`Label`s + Info-`Label` (Invarianten-Hinweis); Keyword-`Entry` + Hinzufügen-`Button`, Chips via `FlexLayout Wrap="Wrap"` + `BindableLayout` mit ×-`Button` (44 pt); `Switch` für `AutoRefreshEnabled`/`AutoMarkReadEnabled`/`NotificationsEnabled` sowie deaktivierter aktiver `Switch` für „Teilwort & Case-Insensitive"; `Picker` für Intervall/Verzögerung/Theme mit `IsEnabled`- und `Opacity`-Binding an den jeweiligen Toggle (Design: ausgegraute Optionszeilen); `TimePicker` ×2 für VON/BIS.
- Code-Behind bleibt minimal (`LoadCommand` in `OnAppearing`); bei Bedarf Chip-Entfernen-Bestätigung per `DisplayActionSheetAsync` nach `CategoriesPage.OnCategoryTapped`-Muster (Zeile 46).

### `AppResources.resx` / `AppResources.de.resx` (+ `AppResources.Designer.cs`)

- Neue Keys für alle Labels/Hinweise/Fehlertexte, u. a.: `SettingsSectionRetention`, `SettingsSectionKeywords`, `SettingsSectionSync`, `SettingsSectionNotifications`, `SettingsSectionAppearance`, `SettingsRetentionLabel`, `SettingsRetentionDaysFormat`, `SettingsRetentionInfo`, `SettingsRetentionDayMarks`, `SettingsKeywordPlaceholder`, `SettingsKeywordAdd`, `SettingsKeywordMatchHint`, `SettingsAutoRefreshLabel`, `SettingsAutoRefreshHint`, `SettingsRefreshIntervalLabel`, `SettingsInterval15Min`/`30Min`/`Hourly`/`4Hours`, `SettingsAutoMarkReadLabel`, `SettingsAutoMarkReadHint`, `SettingsAutoMarkReadDelayLabel`, `SettingsDelayImmediate`/`1s`/`3s`/`5s`, `SettingsNotificationsLabel`, `SettingsNotificationsHint`, `SettingsQuietHoursLabel`, `SettingsQuietHoursFrom`, `SettingsQuietHoursTo`, `SettingsThemeLabel`, `SettingsThemeSystem`/`Light`/`Dark`, `ErrorKeywordEmpty`, `ErrorKeywordDuplicate`, `ErrorKeywordTooLong`, `SettingsSaved` (Designer-Datei wird durch `PublicResXFileCodeGenerator` regeneriert).

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `AddSettingsAutoRefreshAndTheme` (Arbeitsname, Zeitstempel via `dotnet ef migrations add`) | `settings.auto_refresh_enabled`, `settings.refresh_interval_minutes`, `settings.theme` | `auto_refresh_enabled` INTEGER NOT NULL DEFAULT 1; `refresh_interval_minutes` INTEGER NOT NULL DEFAULT 30; `theme` TEXT NULL (max. 50); `HasData`-Seed aktualisiert sich automatisch (Entity-Defaults `true`/`30`/`"system"` → `UpdateData` auf den Singleton-Datensatz). Keine `items`-Änderung (kein `IsKeywordFiltered`-Flag). |

Voraussetzung: `Microsoft.EntityFrameworkCore.Design` ist bereits referenziert (`Reporter.Data.csproj`), `ReporterDbContextFactory` als Design-Time-Factory vorhanden; `dotnet-ef`-Tool muss auf dem Entwicklungsrechner verfügbar sein (ggf. `dotnet tool install -g dotnet-ef` bzw. lokales Tool-Manifest anlegen — derzeit existiert keines).

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `RetentionDays` | 1–365 Tage (Anforderung: Standard 30); `Slider` begrenzt hart, ViewModel clampt `PersistAsync` defensiv auf [1, 365] | Wert außerhalb → geclamppt, kein Persist-Fehler nötig |
| `NewKeywordText` | Nach `Trim()` nicht leer | `HasError = true`, `ErrorMessage = ErrorKeywordEmpty` |
| `NewKeywordText` | Kein Dublett (case-insensitiv gegen `Keywords`, `OrdinalIgnoreCase`) — DB-Unique-Index `keyword_text` ist case-sensitiv | `HasError = true`, `ErrorMessage = ErrorKeywordDuplicate`, kein `AddAsync` |
| `NewKeywordText` | Max. 500 Zeichen (DB-Spalte `keyword_text` max. 500) | `HasError = true`, `ErrorMessage` (Keyword zu lang), kein `AddAsync` |
| `SelectedRefreshInterval` | Nur Werte aus `RefreshIntervalOptions` {15, 30, 60, 240}; unbekannter persistierter Wert → Fallback 30 | `Picker` verhindert ungültige Eingabe strukturell; Fallback beim Laden |
| `SelectedAutoMarkReadDelay` | Nur Werte {0, 1, 3, 5}; unbekannter Wert → Fallback 5 | Fallback beim Laden |
| `SelectedTheme` | Nur `ThemeOption`-Werte `"system"`/`"light"`/`"dark"`; unbekannt → `"system"` | Fallback beim Laden; `AppThemeService` mappt unbekannt auf `AppTheme.Unspecified` |
| `QuietHoursStart`/`QuietHoursEnd` | `TimePicker` liefert immer gültige `TimeSpan`; Start > Ende zulässig (über Mitternacht, Auswertung im Benachrichtigungs-Paket) | Kein Fehlerfall |
| `RefreshIntervalMinutes` (Service-Seite) | `AutoRefreshService` verwendet `Math.Clamp(RefreshIntervalMinutes, 1, 1440)` defensiv bei `StartAsync`/`ApplySettingsAsync` | Ungültiger persistierter Wert → geclamppt, kein Crash |

## Konfigurationsänderungen

Alle Optionen liegen im Singleton-`Settings`-Datensatz (Tabelle `settings`); keine `appsettings`-Einträge.

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `settings.auto_refresh_enabled` | `bool` | `true` | Hintergrund-Aktualisierung Ein/Aus (neu) |
| `settings.refresh_interval_minutes` | `int` | `30` | Abruf-Intervall in Minuten (neu; UI: 15/30/60/240) |
| `settings.theme` | `string?` | `"system"` | Erscheinungsbild `"system"`/`"light"`/`"dark"` (neu) |
| `settings.retention_days` | `int` | `30` | Vorhanden; neu UI-Validierung 1–365 |
| `settings.auto_mark_read_mode` | `string?` | `"on_scroll"` | Vorhanden; neu ausgewertet (`"off"` deaktiviert; UI schreibt `"on_open"`/`"off"`) |
| `settings.auto_mark_read_delay_seconds` | `int` | `5` | Vorhanden; neu UI-Auswahl {0, 1, 3, 5} |
| `settings.notifications_enabled` | `bool` | `true` | Vorhanden; neu UI-Toggle (nur Persistierung) |
| `settings.quiet_hours_start`/`quiet_hours_end` | `TimeSpan?` | `null` | Vorhanden; neu `TimePicker`-Eingabe (nur Persistierung) |
| `keywords.keyword_text` | Datensätze | leer | Vorhanden; neu UI-Verwaltung (Hinzufügen/Entfernen) |

## Seiteneffekte und Risiken

- **`required init`-Pflichtfelder:** Die neuen `required`-Felder im Core-`Settings` brechen alle Objektinitialisierer ohne sie: `SettingsRepository.MapToModel`, `ArticleDetailViewModel`-Fallback, `RetentionCleanupServiceTests`, `SettingsRepositoryTests`. Kompilierfehler zeigen alle Stellen.
- **`RetentionCleanupService`-Konstruktor:** Neue Parameter `IKeywordRepository`/`IKeywordMatcher` — betroffen: `RetentionCleanupServiceTests`-Konstruktor und `MauiProgram`-Registrierung (DI löst zur Laufzeit automatisch auf, sobald die neuen Singletons registriert sind).
- **Bestehendes Löschverhalten:** Die Keyword-Regel löscht zusätzlich gelesene, nicht gespeicherte Artikel, die inhaltlich matchen und deren `PublishedAt` älter als die Frist ist — auch wenn sie kürzlich gelesen wurden (anderer Zeitstempel als bei der Standardregel). Bewusste Semantik, siehe Designentscheidung; im UI-Infotext der Keyword-Sektion als Hinweis dokumentieren.
- **`ArticleDetailViewModel` ist nicht testbar:** Die Klasse liegt im MAUI-Projekt `src/Reporter`, das die Testsuite nicht referenziert — die `AutoMarkReadMode`-Auswertung ist nur manuell verifizierbar (UI-Verifikation, siehe E2E-Abschnitt).
- **`AutoRefreshService` zur Laufzeit:** Fehler im Sync-Loop dürfen den Timer nicht beenden (Catch im Loop). Beim App-Suspend läuft der Timer nicht weiter (In-Process-Timer, kein OS-Background-Fetch) — akzeptiert, da nicht angefordert.
- **Migration auf Bestandsdaten:** `NOT NULL`-Spalten mit `DEFAULT` sind SQLite-kompatibel; bestehender Singleton-Datensatz erhält per `UpdateData` die Defaults.
- **SettingsPage ohne CollectionView:** Die Seite bleibt `ScrollView` + Karten (Formularseite) — AGENTS-Regel zu `CollectionView`/`ScrollView`-Verschachtelung bleibt eingehalten, da keine `CollectionView` verschachtelt wird.

## Umsetzungsreihenfolge

1. **`Settings`-Datenmodell erweitern (Entity + Core-Modell)**
   - Voraussetzungen: Keine.
   - Beschreibung: Drei neue Eigenschaften in `src/Reporter.Data/Entities/Settings.cs` und `src/Reporter.Core/Models/Settings.cs` (`required` für die Skalare, optional `string?` für `Theme`).

2. **EF-Core-Migration für neue `settings`-Spalten**
   - Voraussetzungen: Schritt 1; `Microsoft.EntityFrameworkCore.Design` (vorhanden); `dotnet-ef`-Tool (im Repo nicht manifestiert — bei Bedarf installieren oder lokales Tool-Manifest anlegen).
   - Beschreibung: `ConfigureSettings` um die drei Spalten erweitern, `dotnet ef migrations add` mit `ReporterDbContextFactory` ausführen, generierte Migration + `ReporterDbContextModelSnapshot` prüfen.

3. **`IKeywordMatcher` + `KeywordMatcher` anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: Interface in `Reporter.Core/Interfaces/`, Implementierung in `Reporter.Core/Services/` (`OrdinalIgnoreCase`-`Contains` auf Titel + Content).

4. **`IItemRepository`/`ItemRepository` um Keyword-Lösch-Methoden erweitern**
   - Voraussetzungen: Keine (unabhängig von Schritt 3).
   - Beschreibung: `GetExpiredKeywordCandidatesAsync` + `DeleteRangeAsync` im Interface deklarieren und im Repository implementieren.

5. **`SettingsRepository`-Mapping erweitern**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `SaveAsync` und `MapToModel` um die drei neuen Felder ergänzen.

6. **`RetentionCleanupService` um Keyword-Regel erweitern**
   - Voraussetzungen: Schritte 3, 4.
   - Beschreibung: Konstruktor + `CleanupAsync` gemäß Programmablauf.

7. **`IAutoRefreshService` + `AutoRefreshService` anlegen**
   - Voraussetzungen: Keine (nutzt vorhandene `ISettingsRepository`/`IFeedSyncService`; `TimeProvider` ist BCL).
   - Beschreibung: Interface + `PeriodicTimer`-Implementierung mit Overlap-Guard und Fehlerisolierung.

8. **`IAppThemeService` + `AppThemeService` anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: Interface in Core; Implementierung in `src/Reporter/Services/` setzt `Application.Current.UserAppTheme`.

9. **Lokalisierte Strings ergänzen**
   - Voraussetzungen: Keine.
   - Beschreibung: Neue Keys in `AppResources.resx` + `AppResources.de.resx`; `AppResources.Designer.cs` regeneriert der `PublicResXFileCodeGenerator` beim Build.

10. **`SettingsViewModel` ausbauen**
    - Voraussetzungen: Schritte 1, 5, 7, 8, 9; Optionsklassen `RefreshIntervalOption`/`AutoMarkReadDelayOption`/`ThemeOption` in diesem Schritt anlegen.
    - Beschreibung: Alle bindbaren Eigenschaften, `AddKeywordCommand`/`RemoveKeywordCommand`/`SaveRetentionCommand`, `PersistAsync` mit `_isLoading`-Guard, Validierungen.

11. **`SettingsPage.xaml` gemäß Design-Entwurf ausbauen**
    - Voraussetzungen: Schritte 9, 10. Verbindlich: `design-draft/.../einstellungen_filter/screen.png` + `code.html` (Light + Dark), Mobile-UI-Regeln aus `AGENTS.md`, Muster-Wiederverwendung gemäß Änderungsliste oben.
    - Beschreibung: Fünf Sektions-Karten mit den geplanten Controls; 44-pt-Touch-Ziele; `AppThemeBinding`; `IsEnabled`/`Opacity`-Dimming der Optionszeilen.

12. **`App.OnStart` erweitern (Theme + Auto-Refresh-Start)**
    - Voraussetzungen: Schritte 7, 8.
    - Beschreibung: Theme anwenden und `StartAsync` aufrufen, jeweils fehlerisoliert.

13. **`ArticleDetailViewModel` an globale Einstellung anbinden**
    - Voraussetzungen: Schritt 1.
    - Beschreibung: `AutoMarkReadMode`-Gate, `>= 0`-Delay, Fallback-`Settings` um neue `required`-Felder ergänzen.

14. **DI-Registrierung in `MauiProgram`**
    - Voraussetzungen: Schritte 3, 7, 8.
    - Beschreibung: Drei `AddSingleton`-Einträge analog zum Schema.

15. **Unit-/E2E-Tests schreiben und betroffene Tests anpassen**
    - Voraussetzungen: Schritte 1–14; für `AutoRefreshServiceTests` NuGet `Microsoft.Extensions.TimeProvider.Testing` im Testprojekt (`FakeTimeProvider`) sowie handgeschriebene Fakes `FakeFeedSyncService`/`FakeAutoRefreshService`/`FakeAppThemeService` (kein Mocking-Framework im Projekt).
    - Beschreibung: Siehe Abschnitt „Tests".

16. **Manuelle UI-Verifikation + Abschluss-Checks**
    - Voraussetzungen: Schritte 1–15.
    - Beschreibung: `SettingsPage` auf Windows-Handysize 390 × 844 pt (ist bereits `App.CreateWindow`-Vorgabe) in Light + Dark gegen `screen.png` abgleichen; Screenshot + getestete Größen in `test-results.md` oder `docs/help/anwendung/mobile-ui-design.md` dokumentieren (AGENTS.md Regel 6); Funktions-Smoke-Tests gemäß E2E-Abschnitt inkl. Verzögerung „Sofort" (Delay 0 → Artikel wird beim Öffnen sofort als gelesen markiert); `.\scripts\Run-StaticChecks.ps1` muss mit Exit-Code 0 durchlaufen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Load_PopulatesAllOptions` | `SettingsViewModelTests` | `LoadCommand` befüllt alle Eigenschaften aus Seed-Settings + Keywords |
| `Load_InvalidPersistedValues_UsesFallbacks` | `SettingsViewModelTests` | Ungültige persistierte Werte werden beim Laden auf Defaults zurückgesetzt: `RefreshIntervalMinutes` außerhalb {15, 30, 60, 240} → `SelectedRefreshInterval` = 30; `AutoMarkReadDelaySeconds` außerhalb {0, 1, 3, 5} → 5; `Theme` unbekannt → `SelectedTheme` = `"system"` |
| `PropertyChange_PersistsImmediately` | `SettingsViewModelTests` | Setter-Änderung → `SettingsRepository.GetAsync` liefert neuen Wert (Roundtrip) |
| `RetentionDays_OutOfRange_Clamped` | `SettingsViewModelTests` | 0/400 → auf 1/365 geclamppt und so persistiert |
| `ThemeChange_AppliesTheme` | `SettingsViewModelTests` | `SelectedTheme`-Änderung → `FakeAppThemeService.AppliedThemes` enthält Wert |
| `AutoRefreshChange_AppliesSettings` | `SettingsViewModelTests` | `AutoRefreshEnabled`/Intervall-Änderung → `FakeAutoRefreshService.ApplySettingsAsync` aufgerufen |
| `AddKeyword_Valid_AddsToRepository` | `SettingsViewModelTests` | Keyword landet in `keywords`-Tabelle und `Keywords`-Collection |
| `AddKeyword_DuplicateCaseInsensitive_ShowsError` | `SettingsViewModelTests` | „WERBUNG" nach „Werbung" → `HasError`, kein Insert |
| `AddKeyword_Empty_ShowsError` | `SettingsViewModelTests` | Leer/Whitespace → `HasError`, kein Insert |
| `AddKeyword_TooLong_ShowsError` | `SettingsViewModelTests` | `NewKeywordText` > 500 Zeichen → `HasError`, `ErrorMessage` = `ErrorKeywordTooLong`, kein `AddAsync` |
| `RemoveKeyword_DeletesFromRepository` | `SettingsViewModelTests` | `RemoveKeywordCommand` → Datensatz weg, Collection aktualisiert |
| `MatchesAny_TitleCaseInsensitive` / `MatchesAny_ContentHtml` / `MatchesAny_Substring` / `MatchesAny_NoMatch` | `KeywordMatcherTests` | Case-Insensitivity, Teilwort-Match in Titel und Content, Ränder (null/leer) |
| `CleanupAsync_DeletesKeywordMatchedExpired` | `RetentionCleanupServiceTests` | Gelesener, alter, gematchter Artikel wird gelöscht |
| `CleanupAsync_KeywordMatch_KeepsUnreadAndSaved` | `RetentionCleanupServiceTests` | Ungelesene/`IsSavedForLater`-Treffer bleiben trotz Match + Alter |
| `CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt` | `RetentionCleanupServiceTests` | Gematchter Artikel mit altem `PublishedAt`, aber frischem `ReadAt` wird gelöscht |
| `GetExpiredKeywordCandidates_*` / `DeleteRangeAsync_DeletesOnlyGivenIds` | `ItemRepositoryTests` | Kandidaten-Prädikat (PublishedAt-Basis, Invarianten) und ID-Batch-Delete |
| `SaveAsync_PersistsNewFields` | `SettingsRepositoryTests` | `AutoRefreshEnabled`/`RefreshIntervalMinutes`/`Theme` Roundtrip |
| `StartAsync_InvokesSyncAfterInterval` / `ApplySettings_Disabled_Stops` / `ApplySettings_ChangesInterval` | `AutoRefreshServiceTests` | `FakeTimeProvider.Advance` triggert `SyncAllAsync`; deaktiviert → kein Sync; Intervallwechsel wirkt |
| `SyncThrows_LoopContinues` | `AutoRefreshServiceTests` | `FakeFeedSyncService.SyncAllAsync` wirft Exception → Exception-Isolation fängt sie (`Debug.WriteLine`), Timer läuft weiter: nächster `FakeTimeProvider.Advance` ruft `SyncAllAsync` erneut auf |
| `OverlappingTick_SkipsSync` | `AutoRefreshServiceTests` | Blockierter (noch laufender) `SyncAllAsync` → nächster Timer-Tick startet keinen parallelen Sync (Overlap-Guard; Aufrufzähler bleibt bei 1) |
| `InvalidInterval_Clamped` | `AutoRefreshServiceTests` | Persistierter `RefreshIntervalMinutes` außerhalb 1–1440 (z. B. 0 / 99999) → `Math.Clamp` wirkt in `StartAsync`/`ApplySettingsAsync`, Timer läuft mit geclampptem Intervall statt zu crashen |
| `FakeFeedSyncService` / `FakeAutoRefreshService` / `FakeAppThemeService` | Hilfsklassen in `Reporter.Tests` | Handgeschriebene Fakes (kein Mocking-Framework): zählen `SyncAllAsync`-Aufrufe, können konfiguriert Exceptions werfen bzw. blockieren (`TaskCompletionSource`), zeichnen `ApplySettingsAsync`/`ApplyTheme` auf |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `RetentionCleanupServiceTests` (Konstruktor + `SetRetentionDaysAsync`, Zeilen 23–25, 39–48) | Neuer `RetentionCleanupService`-Ctor (`IKeywordRepository`, `IKeywordMatcher`); Core-`Settings` erhält neue `required`-Felder |
| `SettingsRepositoryTests` (Zeilen 71–77, 93–99) | `new Settings { ... }`-Initialisierer um `AutoRefreshEnabled`/`RefreshIntervalMinutes` ergänzen |
| `ServiceCollectionTests` | Keine Codeanpassung nötig (Repositories ohne neue Ctor-Abhängigkeiten); optional neue Interfaces mit auflösen lassen — keine Pflicht |
| Alle übrigen `ItemRepositoryTests` | Keine — `ItemRepository`-Konstruktor unverändert; neue Methoden additiv |

### E2E-Tests (primärer Funktionsnachweis)

Es existiert **kein UI-Automatisierungs-Framework** im Repo (einzige Suite: `Reporter.Tests`, xUnit + EF-In-Memory; das MAUI-Projekt ist nicht referenzierbar). Als E2E-Nachweis dienen daher End-to-End-Flusstests in `Reporter.Tests`, die den realen Benutzerfluss vom ViewModel über die echten Repositories bis in die SQLite-In-Memory-Datenbank abbilden — ergänzt um die von `AGENTS.md` Regel 6 geforderte dokumentierte manuelle UI-Verifikation (Screenshot, 390 × 844 pt, Light + Dark). Das ViewModel ist dabei der letzte automatisiert erreichbare Punkt vor dem UI-Layer.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Einstellungen öffnen → Werte ändern → sofort persistiert → beim nächsten Laden wieder da | `SettingsViewModelTests.E2E_ChangeSettings_PersistRoundtrip` | Sofort-Persistierung aller Optionen; Singleton-Verhalten | Kern-Benutzerfluss der Anforderung; nur der Fluss VM→Repo→DB beweist das Zusammenspiel aus Laden, Guard, Mapping, Save |
| Pflicht | Keyword eingeben → Hinzufügen → in Liste sichtbar und in DB → Entfernen → weg | `SettingsViewModelTests.E2E_KeywordAddRemove_Persists` | Keyword-Verwaltung inkl. Chips-Collection | Benutzerfluss „Filter verwalten"; Dubletten-Check greift nur gegen echte Collection + Repo |
| Pflicht | Keyword-Dublett (case-insensitiv) und leere Eingabe → Fehlermeldung, kein Datensatz | `SettingsViewModelTests.E2E_KeywordDuplicateAndEmpty_Rejected` | Fehlerfall ist für Anwender sichtbar (`HasError`/`ErrorMessage`) | UI-sichtbarer Fehlerfall — Pflicht-E2E laut Plan-Konvention |
| Pflicht | `RetentionDays` außerhalb 1–365 → geclamppt, kein ungültiger Wert in DB | `SettingsViewModelTests.E2E_RetentionOutOfRange_Clamped` | Validierung 1–365 (Anforderung fix) | UI-sichtbarer Fehlerfall/Grenzfall |
| Pflicht | Keyword „Werbung" anlegen + Artikel seeden (gematcht/ungelesen, gematcht/gespeichert, gematcht/alt/gelesen, ungematcht/alt/gelesen) → `CleanupAsync` → nur erwartete Artikel gelöscht | `RetentionCleanupServiceTests.E2E_KeywordFilterCleanup` | Keyword-Löschregel + unverletzliche Invarianten | Nachweis, dass Keyword-Liste → Matching → Löschung über Repo-Grenzen hinweg korrekt funktioniert |
| Pflicht | `AutoRefreshEnabled` + Intervall setzen → `AutoRefreshService.StartAsync` → `SyncAllAsync` wird periodisch aufgerufen; Deaktivieren stoppt | `AutoRefreshServiceTests.E2E_AutoRefresh_TicksAndStops` (mit `FakeTimeProvider` + `FakeFeedSyncService`) | Zeitgesteuerte Hintergrund-Aktualisierung reagiert auf Settings | Einziger Weg, den Fluss Settings→Timer→Sync nachzuweisen, ohne die App zu starten |

Bestehende E2E-Tests, die angepasst werden müssen: **Keine** (keine E2E-Suite vorhanden).

Nicht automatisierbarer Rest: `SettingsPage`-Darstellung (Chips, Slider, Dimming, Dark Mode), Theme-Umschaltung zur Laufzeit und die `AutoMarkReadMode`-Auswertung im `ArticleDetailViewModel` (liegt im nicht testbaren MAUI-Projekt) — Abdeckung über die manuelle UI-Verifikation in Schritt 16 (Design-Abgleich + Funktions-Smoke-Test: Theme-Picker auf „Dunkel" → UI schaltet um; Artikel öffnen mit deaktiviertem Auto-Gelesen → kein Timer; Verzögerung „Sofort"/Delay 0 → Artikel wird beim Öffnen sofort als gelesen markiert).

## Offene Punkte

Keine.
