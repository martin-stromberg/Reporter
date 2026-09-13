<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik

## `SettingsRepository`

Datei: `src/Reporter.Data/Repositories/SettingsRepository.cs`

Implementiert `ISettingsRepository`; arbeitet über `IDbContextFactory<ReporterDbContext>`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SettingsRepository(IDbContextFactory<ReporterDbContext>)` | public ctor | Speichert die Context-Factory |
| `GetAsync(CancellationToken)` | public | Lädt den Singleton-Datensatz per `FirstOrDefaultAsync(s => s.Id == SettingsEntity.DefaultId)` (`AsNoTracking`); legt bei Fehlen ein `new SettingsEntity()` an und speichert es; gibt `MapToModel(entity)` zurück (Zeilen 27–42) |
| `SaveAsync(Settings)` | public | Lädt/erstellt die Singleton-Entity und kopiert **alle Felder einzeln** (Zeilen 55–64: `RetentionDays`, `AutoMarkReadMode`, `AutoMarkReadDelaySeconds`, `NotificationsEnabled`, `QuietHoursStart`, `QuietHoursEnd`, `AutoRefreshEnabled`, `RefreshIntervalMinutes`, `Theme`, `NotificationSummaryEnabled`); kein `Language`-Feld |
| `MapToModel(SettingsEntity)` | private static | Feldweises Mapping Entity → `Core.Models.Settings` (Zeilen 69–85); kein `Language`-Feld |

Abonnierte Events: keine. Publizierte Events: keine.
Querverweise: Registriert als Singleton `ISettingsRepository` in `MauiProgram.cs` Zeile 50; aufgerufen von `SettingsViewModel` und `App.OnStart`.

## `SettingsViewModel`

Datei: `src/Reporter.Core/ViewModels/SettingsViewModel.cs` (738 Zeilen), `ObservableObject` (CommunityToolkit.Mvvm)

Konstruktor-Abhängigkeiten (Zeilen 68–81): `ISettingsRepository`, `IKeywordRepository`, `IAutoRefreshService`, `IAppThemeService`, optional `TimeProvider`, optional `ILocalNotificationService`.

Relevante Felder: `_isLoading` (volatile bool, Zeile 55), `_persistLock` (`SemaphoreSlim(1,1)`, Zeile 56), `_retentionDebounceCts` (Debounce für `RetentionDays`, 500 ms via `TimeProvider`, Zeile 27/57).

| Methode / Member | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ThemeOptions` | public `IReadOnlyList<ThemeOption>` | Im Konstruktor mit lokalisierten Labels befüllt (Zeilen 99–104): `ThemeSystem`/`ThemeLight`/`ThemeDark` → `AppResources.SettingsTheme*` |
| `RefreshIntervalOptions`, `AutoMarkReadDelayOptions` | public `IReadOnlyList<…>` | Analoge Optionslisten (Zeilen 85–98) |
| `SelectedTheme` | public `ThemeOption?` | Bindbare Property; Setter ruft bei Änderung `PersistOnChange()` (Zeilen 430–440) |
| `AutoRefreshEnabled`, `SelectedRefreshInterval`, `AutoMarkReadEnabled`, `SelectedAutoMarkReadDelay`, `NotificationsEnabled`, `NotificationSummaryEnabled`, `QuietHoursEnabled`, `QuietHoursStart`, `QuietHoursEnd`, `RetentionDays` | public | Bindbare Properties mit `PersistOnChange()`- bzw. `ScheduleRetentionPersist()`-Muster |
| `LoadCommand` / `LoadAsync()` | public Command / private async | Liest `settings` via `_settingsRepository.GetAsync()` unter `_isLoading`-Schutz und mappt feldweise auf die Properties; `SelectedTheme` mit Fallback auf `ThemeSystem` bei unbekanntem Wert (Zeilen 465–507, Theme Zeilen 485–486) |
| `PersistOnChange()` | private | Guard `_isLoading` → fire-and-forget `_ = PersistAsync()` (Zeilen 509–517) |
| `PersistAsync()` | private async | Unter `_persistLock`: baut ein **neues `Settings`-Objekt feldweise** aus den ViewModel-Properties (Zeilen 645–658, inkl. `Theme = SelectedTheme?.Value ?? SettingsValues.ThemeSystem`); `_settingsRepository.SaveAsync(updated)`; `Settings = updated`; ruft `_appThemeService.ApplyTheme` nur bei geändertem `Theme` auf (Zeilen 663–666) und `_autoRefreshService.ApplySettingsAsync` bei geänderten Auto-Refresh-Werten (Zeilen 668–671). Ein `Language`-Feld wird nicht gesetzt — ein neues Feld müsste hier ergänzt werden, sonst ginge es bei jeder Persistierung verloren |
| `ScheduleRetentionPersist()` / `PersistRetentionDebouncedAsync` / `CancelRetentionDebounce` | private | 500-ms-Debounce nur für `RetentionDays` (Zeilen 578–637) |
| `NotificationAuthorizationDenied` | public event `Func<Task>?` | Wird ausgelöst, wenn das Einschalten der Benachrichtigungen auf verweigerte Systemberechtigung trifft; abonniert von `SettingsPage.OnAppearing` (Zeile 286) |
| `FormatRetentionDays` | private static | `string.Format(CultureInfo.CurrentCulture, AppResources.SettingsRetentionDaysFormat, …)` (Zeile 462) — Formatierung folgt `CurrentCulture` |

Es gibt **keine** Member `LanguageOptions`, `SelectedLanguage` o. ä.

## `AppThemeService`

Datei: `src/Reporter/Services/AppThemeService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ApplyTheme(string? theme)` | public | Setzt `Application.Current.UserAppTheme`: `ThemeLight`→`AppTheme.Light`, `ThemeDark`→`AppTheme.Dark`, **alle anderen Werte (inkl. `null`/unbekannt) → `AppTheme.Unspecified`** (Zeilen 21–26) — Konvention „unbekannter Wert fällt auf System zurück" |

## `App` (App-Start)

Datei: `src/Reporter/App.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `App(IServiceProvider)` | public ctor | `InitializeComponent()`, merged `Colors`/`Styles`-Resourcen (Zeilen 22–29) |
| `OnStart()` | protected override async void | Reihenfolge (Zeilen 34–87): 1. `context.Database.MigrateAsync()`, 2. `IRetentionCleanupService.CleanupAsync()`, 3. **`ISettingsRepository.GetAsync()` + `IAppThemeService.ApplyTheme(settings.Theme)`** (Zeilen 53–64 — hier würde die Sprachanwendung anknüpfen), 4. `INetworkStatusService` auflösen, 5. `IAutoRefreshService.StartAsync()`. Jeder Schritt einzeln try/catch-geschützt. **Es wird keine Culture gesetzt** |
| `CreateWindow(IActivationState?)` | protected override | `new Window(_services.GetRequiredService<AppShell>())`; unter Windows Fenster 390×844 (Zeilen 94–107). Läuft **vor** `OnStart` |

## `MauiProgram`

Datei: `src/Reporter/MauiProgram.cs`

`CreateMauiApp()` registriert u. a.: `AddDbContextFactory<ReporterDbContext>` (SQLite `reporter.db` im `FileSystem.AppDataDirectory`), `ISettingsRepository→SettingsRepository`, `IAppThemeService→AppThemeService` (Zeile 58), **`SettingsViewModel` als Singleton** (Zeile 66 — Options-Labels werden damit einmalig beim ersten Resolve aus `AppResources` gelesen), Pages als Transient, `AppShell` als Transient (Zeile 74). Kein sprachbezogener Service registriert; keine Culture-Setzung.

## `SettingsPage` (Code-Behind)

Datei: `src/Reporter/Views/SettingsPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SettingsPage(SettingsViewModel)` | public ctor | `InitializeComponent()`, `BindingContext = viewModel` |
| `OnAppearing()` | protected override | Abonniert `NotificationAuthorizationDenied` und führt `LoadCommand.Execute(null)` aus |
| `OnDisappearing()` | protected override | Deabonniert `NotificationAuthorizationDenied` |
| `OnNotificationAuthorizationDenied()` | private async | `DisplayAlertAsync` mit `AppResources`-Texten; bei Bestätigung `AppInfo.Current.ShowSettingsUI()` |
| `OnOpenNotificationSettingsClicked` | private | `AppInfo.Current.ShowSettingsUI()` |
