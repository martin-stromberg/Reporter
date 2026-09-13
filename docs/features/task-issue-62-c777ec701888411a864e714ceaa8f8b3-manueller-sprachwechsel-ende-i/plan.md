<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Manueller Sprachwechsel (EN/DE) in den Einstellungen (#62)

## Übersicht

Der Anwender kann die App-Sprache unabhängig von der Systemsprache wählen: **System** (bisheriges Verhalten), **Deutsch** oder **English**. Die Auswahl wird als neue Spalte `language` der Singleton-`settings`-Tabelle persistiert (Muster: `Settings.Theme`), in der `SettingsPage` über einen `Picker` angeboten und beim App-Start — zwingend vor `CreateWindow`, da `AppShell` alle Pages und lokalisierten Tab-Titel eager erzeugt — auf `CultureInfo.CurrentUICulture`/`CurrentCulture` angewendet. Eine Laufzeit-Umschaltung findet nicht statt; die UI weist statisch auf den erforderlichen App-Neustart hin.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Zeitpunkt der Kultur-Anwendung | Synchroner Block in `MauiProgram.CreateMauiApp` **nach** `builder.Build()` und vor der `App`-Erzeugung: Scope öffnen → `context.Database.Migrate()` (synchron) → `ISettingsRepository.GetAsync().GetAwaiter().GetResult()` → `AppCulture.Apply(settings.Language)` — komplett try/catch-geschützt | `AppShell.xaml.cs` liest `AppResources.Tab*` und instanziiert alle fünf Pages (inkl. Singleton-`SettingsViewModel`, der seine Options-Labels im Konstruktor aus `AppResources` liest) **eager in `CreateWindow`**, also vor `OnStart`. `MauiProgram` ist der frühestmögliche Composition-Root-Punkt. Die synchrone Migration vor dem Lesen ist nötig, damit der Upgrade-Fall funktioniert: Bei bestehenden Datenbanken existiert die `language`-Spalte erst nach der Migration — ein Lesen davor würde eine Exception werfen und die gewählte Sprache beim ersten Start nach dem Update ignorieren. |
| Kapselung der Kultur-Logik | Neue statische Klasse `AppCulture` in `Reporter.Core` (`src/Reporter.Core/Localization/AppCulture.cs`) mit `ResolveCulture(string? language) → CultureInfo?` und `Apply(string? language)` — kein `ILanguageService`-Interface | `CultureInfo` ist reine BCL (`net10.0`); eine Service-Abstraktion analog `IAppThemeService` brächte keinen Plattformbezug und wäre im Testprojekt ohnehin nicht erreichbar (`Reporter.Tests` referenziert nur `Reporter.Core`/`Reporter.Data`, nicht das MAUI-Projekt). `ResolveCulture` ist eine reine Funktion und ohne Seiteneffekte testbar; `Apply` ist ein dünner Wrapper. |
| Umfang der gesetzten Kultur | `Apply` setzt `CultureInfo.CurrentUICulture`, `CurrentCulture`, `DefaultThreadCurrentUICulture` und `DefaultThreadCurrentCulture`; `AppResources.Culture` bleibt unangetastet (`null` → folgt `CurrentUICulture`) | Die Anforderung nennt nur `CurrentUICulture`, aber sämtliche Formatierungen (`SettingsViewModel.FormatRetentionDays`, `UnreadViewModel.LastSyncText`, `ArticleDetailViewModel`-Datumsformate, `FeedsPage.xaml.cs`, `NotificationService`) folgen `CurrentCulture` — bei manueller Sprachwahl wären Zahlen-/Datumsformate sonst inkonsistent systemsprachlich. Die `DefaultThreadCurrent*`-Properties decken Hintergrund-Threads ab (z. B. `NotificationService`), die nicht vom UI-Thread erben. |
| „System"-Verhalten | `ResolveCulture` liefert für `"system"`, `null` und jeden unbekannten Wert `null`; `Apply(null)` ist ein No-Op (kein Zurücksetzen, keine Berührung der Kultur) | Prozessweite Defaults sind ohnehin die Systemkultur; die Konvention „unbekannter persistierter Wert fällt auf System zurück" entspricht `AppThemeService.ApplyTheme`. |
| Persistierte Werte | `SettingsValues.LanguageSystem = "system"`, `LanguageGerman = "de"`, `LanguageEnglish = "en"` (neutrale BCP-47-Codes) | `new CultureInfo("de")`/`("en")` sind valide neutrale Kulturen; Schema analog `ThemeSystem`/`ThemeLight`/`ThemeDark`. `de-DE`/`en-US` wären spezifischer ohne Mehrwert (es gibt nur je eine Resx-Variante). |
| Platzierung in `SettingsPage` | Eigene Sektion „Sprache" (`SettingsSectionLanguage`) **nach** der Sektion „Erscheinungsbild", mit eigener Karte: Picker-Zeile (`Grid ColumnDefinitions="*,Auto"`, Muster Zeilen 433–445) + Info-`Border` mit Neustart-Hinweis (Muster `SettingsRetentionInfo`, Zeilen 47–54) | Sprache ist kein Erscheinungsbild-Aspekt; eine eigene Sektion folgt dem vorhandenen Sektions-/Kartenmuster und bietet Platz für den Hinweis-`Border`, ohne die Erscheinungsbild-Karte aufzublähen. |
| Neustart-Hinweis | Statischer Info-`Border`, immer unter dem Picker sichtbar | Einfachste Lesart, konsistent mit den übrigen statischen Info-Texten der Seite; keine zusätzliche Vergleichslogik „gewählt vs. wirksam" nötig. |
| Kein Service-Aufruf bei Sprachwechsel | `PersistAsync` schreibt `Language` nur; es wird **kein** Apply beim Ändern aufgerufen (anders als `IAppThemeService.ApplyTheme` bei `Theme`) | Keine Laufzeit-Umschaltung — Wirksamkeit erst nach Neustart. Die Kultur steht dann ohnehin schon prozessweit aus dem Start-Block. |

## Programmabläufe

### Sprache in den Einstellungen wählen (Persistierung)

1. `SettingsPage.OnAppearing` führt `LoadCommand` aus → `SettingsViewModel.LoadAsync` liest `settings` via `ISettingsRepository.GetAsync` unter `_isLoading`-Schutz und mappt `settings.Language` auf `SelectedLanguage` (`LanguageOptions.FirstOrDefault(o => o.Value == settings.Language)`, Fallback: Option mit `SettingsValues.LanguageSystem`).
2. Der Anwender wählt im `Picker` (`ItemsSource="{Binding LanguageOptions}"`, `SelectedItem="{Binding SelectedLanguage}"`, `ItemDisplayBinding="{Binding Label}"`) eine Option.
3. Der `SelectedLanguage`-Setter ruft `PersistOnChange()` → fire-and-forget `PersistAsync()` unter `_persistLock`.
4. `PersistAsync` baut ein neues `Settings`-Objekt feldweise (inkl. `Language = SelectedLanguage?.Value ?? SettingsValues.LanguageSystem`) und ruft `_settingsRepository.SaveAsync(updated)`; kein Service-Aufruf, kein UI-Refresh.
5. Der statische Info-`Border` (`SettingsLanguageRestartHint`) weist darauf hin, dass die Änderung erst nach einem Neustart wirkt.

Beteiligte Klassen/Komponenten: `SettingsPage`, `SettingsViewModel`, `LanguageOption`, `ISettingsRepository`, `SettingsRepository`, `SettingsValues`.

### Kultur-Anwendung beim App-Start (vor `CreateWindow`)

1. `MauiProgram.CreateMauiApp` baut die App (`builder.Build()`).
2. Vor `return`: privater Hilfsaufruf `ApplyPersistedLanguage(app)` — in eigenem try/catch (Fehler dürfen den Start nicht verhindern, `Debug.WriteLine` wie in `App.OnStart`):
   a. `using var scope = app.Services.CreateScope();`
   b. `scope.ServiceProvider.GetRequiredService<ReporterDbContext>().Database.Migrate()` — synchron; stellt sicher, dass die `language`-Spalte existiert (Upgrade- und Erstinstallationsfall).
   c. `var settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>().GetAsync().GetAwaiter().GetResult();`
   d. `AppCulture.Apply(settings.Language)` — bei `"de"`/`"en"` werden `CurrentUICulture`/`CurrentCulture` (UI-Thread) und `DefaultThreadCurrentUICulture`/`DefaultThreadCurrentCulture` (Hintergrund-Threads) auf `new CultureInfo(value)` gesetzt; bei `"system"`/`null`/unbekannt: No-Op.
3. Die MAUI-Runtime erzeugt danach `App`, `CreateWindow` löst `AppShell` auf — `AppShell.xaml.cs` liest `AppResources.Tab*` und instanziiert alle Pages/ViewModels nun bereits in der gewählten Kultur.
4. `App.OnStart` bleibt unverändert: `MigrateAsync` ist ein idempotenter No-Op-Zweitaufruf; `IAppThemeService.ApplyTheme(settings.Theme)` wie bisher. Es wird **keine** zweite Sprachanwendung ergänzt.

Beteiligte Klassen/Komponenten: `MauiProgram`, `AppCulture`, `ReporterDbContext`, `ISettingsRepository`, `App`, `AppShell`, `SettingsViewModel`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `LanguageOption` (`src/Reporter.Core/ViewModels/LanguageOption.cs`) | Klasse (Optionsdatenhalter) | `required string Value` (persistierter Wert `"system"`/`"de"`/`"en"`) + `required string Label` (lokalisiert) — identisch zu `ThemeOption` |
| `AppCulture` (`src/Reporter.Core/Localization/AppCulture.cs`, neuer Namespace `Reporter.Core.Localization`) | statische Klasse | `ResolveCulture(string? language) → CultureInfo?` (Mapping `"de"`/`"en"` → `CultureInfo`, sonst `null`) und `Apply(string? language)` (setzt die vier `CultureInfo`-Properties, No-Op bei `null`) — testbare Kapselung der Kultur-Anwendung |
| `AddSettingsLanguage` (`src/Reporter.Data/Migrations/`) | EF-Core-Migration | `AddColumn<string>("language", TEXT, maxLength 50, nullable)` auf `settings` + `UpdateData` `"system"` für den Singleton-Datensatz — Muster `20260911080630_AddSettingsAutoRefreshAndTheme.cs` |
| `AppCultureTests` (`src/Reporter.Tests/AppCultureTests.cs`) | Testklasse | Unit-Tests für `ResolveCulture` (und ggf. `Apply` mit Kultur-Restore) — siehe Abschnitt Tests |

## Änderungen an bestehenden Klassen

### `Reporter.Data.Entities.Settings` (`src/Reporter.Data/Entities/Settings.cs`)

- **Neue Eigenschaften:** `Language` (`string?`) — persistierte Sprachwahl, Default `SettingsValues.LanguageSystem` (analog `Theme`).

### `Reporter.Core.Models.Settings` (`src/Reporter.Core/Models/Settings.cs`)

- **Neue Eigenschaften:** `Language` (`string?`, `init`) — Default `SettingsValues.LanguageSystem` (analog `Theme`).

### `SettingsValues` (`src/Reporter.Core/Models/SettingsValues.cs`)

- **Neue Konstanten:** `LanguageSystem = "system"`, `LanguageGerman = "de"`, `LanguageEnglish = "en"` — Konvention der `Theme*`-Konstanten.

### `ReporterDbContext` (`src/Reporter.Data/ReporterDbContext.cs`)

- **Geänderte Methoden:** `ConfigureSettings` — neues Mapping `entity.Property(e => e.Language).HasColumnName("language").HasMaxLength(50);` (nullable TEXT, analog `theme`, Zeile 136). `entity.HasData(new Settings())` nimmt den neuen Default `"system"` automatisch auf.

### `SettingsRepository` (`src/Reporter.Data/Repositories/SettingsRepository.cs`)

- **Geänderte Methoden:** `SaveAsync` — Feldzuweisung `entity.Language = settings.Language` ergänzen (Zeilen 55–64). `MapToModel` — `Language = entity.Language` ergänzen (Zeilen 71–84). Interface `ISettingsRepository` bleibt unverändert.

### `SettingsViewModel` (`src/Reporter.Core/ViewModels/SettingsViewModel.cs`)

- **Neue Eigenschaften:** `LanguageOptions` (`IReadOnlyList<LanguageOption>`, get-only) — im Konstruktor befüllt, Reihenfolge System/Deutsch/English, Labels aus `AppResources.SettingsLanguageSystem`/`.SettingsLanguageGerman`/`.SettingsLanguageEnglish` (analog `ThemeOptions`, Zeilen 99–104). `SelectedLanguage` (`LanguageOption?`) — bindbar, Setter ruft `PersistOnChange()` (Muster `SelectedTheme`, Zeilen 430–440); neues Feld `_selectedLanguage`.
- **Geänderte Methoden:** `LoadAsync` — `SelectedLanguage = LanguageOptions.FirstOrDefault(o => o.Value == settings.Language) ?? LanguageOptions.First(o => o.Value == SettingsValues.LanguageSystem);` (Muster `SelectedTheme`, Zeilen 485–486). `PersistAsync` — `Language = SelectedLanguage?.Value ?? SettingsValues.LanguageSystem` im feldweise neu aufgebauten `Settings`-Objekt ergänzen (Zeilen 645–658) — **Pflicht**, sonst geht `Language` bei jeder anderen Persistierung verloren.
- **Neue Events:** keine. **Neue Event-Handler:** keine.

### `SettingsPage` (`src/Reporter/Views/SettingsPage.xaml`)

- Neue Sektion „Sprache" am Ende des `VerticalStackLayout` (vor Zeile 449): Sektions-`Label` `SettingsSectionLanguage` (`UiLabelStyle`) + Karten-`Border` (Padding 16, `RoundRectangle 12`, `AppThemeBinding` `LightSurfaceContainer`/`DarkSurfaceContainer`) mit
  - Picker-Zeile: `Grid ColumnDefinitions="*,Auto"` — `Label` `SettingsLanguageLabel` links, `Picker` rechts (`ItemsSource="{Binding LanguageOptions}"`, `SelectedItem="{Binding SelectedLanguage}"`, `ItemDisplayBinding="{Binding Label}"`, `SemanticProperties.Description="{x:Static strings:AppResources.SettingsLanguageLabel}"`, `MinimumWidthRequest="44"`, `MinimumHeightRequest="44"`, `HorizontalTextAlignment="End"` — exakt das Theme-Picker-Muster Zeilen 437–444);
  - Info-`Border` mit `SettingsLanguageRestartHint` (`Padding="10"`, `RoundRectangle 8`, `Stroke="Transparent"`, `AppThemeBinding` auf `*SurfaceSubtle`, `Label` mit `BodySmallStyle` — Muster `SettingsRetentionInfo`, Zeilen 47–54).

### `MauiProgram` (`src/Reporter/MauiProgram.cs`)

- **Geänderte Methoden:** `CreateMauiApp` — nach `builder.Build()` und vor `return` den privaten Hilfsaufruf `ApplyPersistedLanguage(app)` ausführen (synchron: Scope → `ReporterDbContext.Database.Migrate()` → `ISettingsRepository.GetAsync()` → `AppCulture.Apply(settings.Language)`; try/catch mit `Debug.WriteLine`).
- **Neue Methoden:** `ApplyPersistedLanguage(MauiApp app)` (private static) — Zweck: persistierte Sprachwahl wirksam setzen, bevor `CreateWindow`/`AppShell` lokalisierte Ressourcen lesen.

### `TestSettingsHelper` (`src/Reporter.Tests/TestSettingsHelper.cs`)

- **Geänderte Methoden:** `SaveAsync` — `Language = language ?? settings.Language` im feldweisen `Settings`-Neuaufbau ergänzen; **neuer optionaler Parameter** `string? language = null`. Ohne das Mitkopieren würden Helper-Aufrufe `Language` implizit auf `"system"` zurücksetzen.

### `AppResources.resx` / `AppResources.de.resx` / `AppResources.Designer.cs` (`src/Reporter.Core/Resources/Strings/`)

- **Neue Schlüssel** (en/de): `SettingsSectionLanguage` („Language"/„Sprache"), `SettingsLanguageLabel` („Language"/„Sprache"), `SettingsLanguageSystem` („System"/„System"), `SettingsLanguageGerman` („German"/„Deutsch"), `SettingsLanguageEnglish` („English"/„Englisch"), `SettingsLanguageRestartHint` (z. B. „The new language takes effect after restarting the app."/„Die neue Sprache wird nach einem Neustart der App wirksam."). `AppResources.Designer.cs` muss anschließend die neuen stark typisierten Properties enthalten (siehe Offene Punkte).

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `AddSettingsLanguage` | `settings.language` (TEXT, maxLength 50, nullable) | `AddColumn<string>` + `UpdateData` auf den Singleton-Datensatz (`id` = `a1f5c6d2-…`) mit Wert `"system"` — Muster `AddSettingsAutoRefreshAndTheme`. Erzeugung via `dotnet ef migrations add AddSettingsLanguage --project src/Reporter.Data` (Design-Time-Factory `src/Reporter.Data/ReporterDbContextFactory.cs` und Paket `Microsoft.EntityFrameworkCore.Design` vorhanden; `dotnet-ef` 10.0.10 installiert); `ReporterDbContextModelSnapshot.cs` wird dabei automatisch aktualisiert. |

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `Settings.Language` (persistiert) | Nur `"system"`/`"de"`/`"en"` sind bekannt; jeder andere Wert (inkl. `null`) fällt auf `system` zurück — in `SettingsViewModel.LoadAsync` (Options-Fallback) und `AppCulture.ResolveCulture` (`null`-Rückgabe → No-Op) | Manipulierte/alte DB-Werte führen nicht zu `CultureNotFoundException` oder leerer Auswahl — Konvention aus `AppThemeService.ApplyTheme` |
| `SelectedLanguage` (UI) | Auswahl ist auf die drei `LanguageOptions` beschränkt (geschlossener `Picker`) | Kein Freitext möglich — keine weitere Validierung nötig |

## Konfigurationsänderungen

Keine. Die Sprachwahl ist eine benutzerspezifische Laufzeiteinstellung in der `settings`-Tabelle, keine `appsettings`- oder csproj-Konfiguration. DI-Registrierungen sind nicht nötig (`AppCulture` ist statisch; `ISettingsRepository` bereits registriert).

## Seiteneffekte und Risiken

- **App-Start:** `CreateMauiApp` führt die EF-Migration nun synchron vor der Fenstererzeugung aus (bisher async in `OnStart`). Mehrkosten entstehen faktisch nur beim ersten Start bzw. beim Upgrade-Start; `OnStart.MigrateAsync` bleibt als idempotenter Sicherheitsaufruf erhalten. Das sync-over-async `GetAsync().GetAwaiter().GetResult()` ist unkritisch (kein `SynchronizationContext` zu diesem Zeitpunkt); der gesamte Block ist try/catch-geschützt — ein Fehler führt zum bisherigen Systemverhalten.
- **Bestehende Settings-Verlust-Gefahr:** Wird `Language` in `SettingsRepository.SaveAsync`, `MapToModel` **oder** `SettingsViewModel.PersistAsync` vergessen, wird die Sprachwahl bei jeder anderen Einstellungsänderung still zurückgesetzt — entsprechende Tests (Roundtrip, „Language geht nicht verloren") sind Pflicht.
- **Kultureller globaler Zustand in Tests:** `AppCulture.Apply` verändert prozessweite `CultureInfo`-Defaults. Tests sollen primär die reine Funktion `ResolveCulture` prüfen; ein `Apply`-Test muss die Kultur im `finally`/`Dispose` wiederherstellen (xunit läuft parallel — Seiteneffekte auf andere Tests, z. B. `AppResources`-Assertions, vermeiden).
- **Hilfedokumentation:** `docs/help/anwendung/sprache.md` beschreibt explizit, dass es keine Sprach-Auswahl gibt — muss mit dem Feature aktualisiert werden, sonst widerspricht die Doku der App.
- **Betroffene bestehende Features:** Keine — alle Änderungen sind additive Feld-/Sektions-Erweiterungen entlang des `Theme`-Musters. `ISettingsRepository` und die `SettingsViewModel`-Konstruktorsignatur bleiben unverändert (keine neue Abhängigkeit).

## Umsetzungsreihenfolge

1. **`SettingsValues`-Konstanten**
   - Voraussetzungen: Keine.
   - Beschreibung: `LanguageSystem`/`LanguageGerman`/`LanguageEnglish` in `src/Reporter.Core/Models/SettingsValues.cs` ergänzen.

2. **`Language` in Entity und Domain-Modell**
   - Voraussetzungen: Schritt 1 (Default-Werte referenzieren die Konstanten).
   - Beschreibung: `Settings.Language` (`string?`, Default `SettingsValues.LanguageSystem`) in `src/Reporter.Data/Entities/Settings.cs` und `src/Reporter.Core/Models/Settings.cs` (`init`-Property) ergänzen.

3. **`ConfigureSettings`-Mapping**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: `language`-Spalte (TEXT, `HasMaxLength(50)`, nullable) in `ReporterDbContext.ConfigureSettings` ergänzen.

4. **Migration `AddSettingsLanguage` erzeugen**
   - Voraussetzungen: Schritt 3; `dotnet-ef` (installiert, 10.0.10), `Microsoft.EntityFrameworkCore.Design` (referenziert), `ReporterDbContextFactory` (Design-Time-Factory, vorhanden).
   - Beschreibung: `dotnet ef migrations add AddSettingsLanguage --project src/Reporter.Data`; prüfen, dass `AddColumn` + `UpdateData` und der ModelSnapshot korrekt erzeugt wurden.

5. **`SettingsRepository` erweitern**
   - Voraussetzungen: Schritte 2–4.
   - Beschreibung: `Language` in `SaveAsync` (Entity-Zuweisung) und `MapToModel` (Modell-Mapping) ergänzen.

6. **Neue `AppResources`-Schlüssel + Designer**
   - Voraussetzungen: Keine.
   - Beschreibung: Die sechs `SettingsLanguage*`/`SettingsSectionLanguage`-Schlüssel in `AppResources.resx` und `AppResources.de.resx` eintragen; `AppResources.Designer.cs` regenerieren bzw. manuell ergänzen (siehe Offene Punkte).

7. **`LanguageOption` anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: `src/Reporter.Core/ViewModels/LanguageOption.cs` nach `ThemeOption`-Vorbild (`required Value`/`Label`).

8. **`SettingsViewModel` erweitern**
   - Voraussetzungen: Schritte 1, 2, 6, 7.
   - Beschreibung: `LanguageOptions` im Konstruktor befüllen; `SelectedLanguage`-Property mit `PersistOnChange()`; `LoadAsync`-Mapping mit `system`-Fallback; `PersistAsync` um `Language`-Feld ergänzen.

9. **`AppCulture` anlegen**
   - Voraussetzungen: Schritt 1 (Konstanten für das Mapping).
   - Beschreibung: `src/Reporter.Core/Localization/AppCulture.cs` mit `ResolveCulture`/`Apply` (vier `CultureInfo`-Properties; `system`/unbekannt/`null` → No-Op).

10. **Kultur-Anwendung in `MauiProgram` verdrahten**
    - Voraussetzungen: Schritte 4, 5, 9.
    - Beschreibung: `ApplyPersistedLanguage(app)` nach `builder.Build()` aufrufen (synchroner Migrate → GetAsync → Apply), try/catch-geschützt.

11. **`SettingsPage.xaml`: Sektion „Sprache"**
    - Voraussetzungen: Schritte 6, 8.
    - Beschreibung: Neue Sektion nach „Erscheinungsbild" mit Picker-Zeile und Neustart-Hinweis-`Border` exakt nach den dokumentierten Karten-/Picker-/Info-Mustern (`MinimumHeightRequest="44"`, `SemanticProperties.Description`, `AppThemeBinding`).

12. **`TestSettingsHelper` erweitern**
    - Voraussetzungen: Schritt 2.
    - Beschreibung: `Language` im Feld-Copy ergänzen + optionalen `language`-Parameter.

13. **Unit-/Integrationstests schreiben**
    - Voraussetzungen: Schritte 5, 8, 9, 12.
    - Beschreibung: Neue Tests gemäß Abschnitt „Tests" (`SettingsRepositoryTests`, `SettingsViewModelTests_Load`, `SettingsViewModelTests_Persist`, `AppCultureTests`).

14. **E2E-Test (ViewModel→Repository→SQLite→Reload)**
    - Voraussetzungen: Schritte 5, 8, 12.
    - Beschreibung: `E2E_ChangeLanguage_PersistRoundtrip` in `SettingsViewModelTests_E2E` (Muster `E2E_ChangeSettings_PersistRoundtrip`).

15. **Build + `dotnet test` + `Run-StaticChecks.ps1`**
    - Voraussetzungen: Schritte 1–14.
    - Beschreibung: `dotnet build Reporter.sln`, `dotnet test src/Reporter.Tests/Reporter.Tests.csproj`, `.\scripts\Run-StaticChecks.ps1` (muss Exit 0 liefern — AGENTS.md).

16. **Manuelle UI-Verifikation + Dokumentation**
    - Voraussetzungen: Schritte 11, 15; lauffähiges Windows-Target (Fenster 390×844 bereits in `App.CreateWindow` voreingestellt).
    - Beschreibung: App starten → Einstellungen → Sprach-Picker prüfen (Layout, Touch-Targets, Dark/Light via `AppThemeBinding`, Vergleich mit `design-draft/stitch_local_rss_feed_reader/einstellungen_filter/screen.png`), Sprache auf Deutsch/English stellen → App neu starten → Texte in gewählter Sprache (Tabs, Einstellungen); Screenshot + getestete Fenstergröße in `test-results.md` bzw. `docs/help/anwendung/mobile-ui-design.md` dokumentieren; offenen Checklistenpunkt in `test-results.md` (Zeile 538) aktualisieren; `docs/help/anwendung/sprache.md` anpassen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `SaveAsync_PersistsLanguage` | `SettingsRepositoryTests` | Roundtrip der `language`-Spalte: `SaveAsync` mit `Language="de"` → `GetAsync` liefert `"de"` (Muster `SaveAsync_PersistsNewFields`) |
| `LanguageOptions_ExposePersistedValues` | `SettingsViewModelTests_Load` | `LanguageOptions` liefert exakt `["system","de","en"]` in dieser Reihenfolge (Muster `ThemeOptions_ExposePersistedValues`) |
| `Load_InvalidLanguage_UsesSystemFallback` (oder Erweiterung von `Load_InvalidPersistedValues_UsesFallbacks`) | `SettingsViewModelTests_Load` | Persistiertes `Language="fr"` → `SelectedLanguage` fällt auf die `"system"`-Option |
| `Load_PopulatesSelectedLanguage` (oder Erweiterung von `Load_PopulatesAllOptions`) | `SettingsViewModelTests_Load` | Persistiertes `Language="de"` → `SelectedLanguage.Value == "de"` |
| `SelectedLanguage_Change_Persists` | `SettingsViewModelTests_Persist` | Setter-Änderung persistiert sofort (`TestWaitHelper.WaitUntilAsync` auf `GetAsync().Language == "en"`) — Muster `PropertyChange_PersistsImmediately` |
| `OtherChange_DoesNotLoseLanguage` | `SettingsViewModelTests_Persist` | Nach `Language="de"`-Persistierung ändert eine andere Property (z. B. `AutoRefreshEnabled`) die Einstellungen erneut → `Language` bleibt `"de"` (deckt die `PersistAsync`-Feld-Copy-Pflicht ab) |
| `E2E_ChangeLanguage_PersistRoundtrip` | `SettingsViewModelTests_E2E` | `SelectedLanguage`-Wechsel → Persist → Reload in neuer `SettingsViewModel`-Instanz → `SelectedLanguage.Value` korrekt (primärer automatisierter Funktionsnachweis des Benutzerflusses) |
| `ResolveCulture_ReturnsExpected` (Theory: `"de"`→`de`, `"en"`→`en`, `"system"`/`null`/`"fr"`/`""`→`null`) | `AppCultureTests` (neu) | Reine Mapping-Logik ohne Seiteneffekte |
| `Apply_SetsCultures` (optional, mit Kultur-Restore im `finally`/`Dispose`) | `AppCultureTests` | `Apply("de")` setzt `CurrentUICulture`/`CurrentCulture` auf `de`; `Apply("system")` verändert nichts |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `TestSettingsHelper` (Hilfsmethode) | Muss `Language` im feldweisen `Settings`-Copy mitführen (`language ?? settings.Language`) — sonst würden Helper-Aufrufe das neue Feld implizit auf `null` zurücksetzen |
| `E2E_ChangeSettings_PersistRoundtrip` (`SettingsViewModelTests_E2E`) | Optional: zusätzlich `SelectedLanguage` setzen und im Reload asserten — nicht zwingend, da ein eigener Language-Roundtrip-Test geplant ist |
| `Load_InvalidPersistedValues_UsesFallbacks` (`SettingsViewModelTests_Load`) | Optional: `Language="fr"` in den Seed-Overrides und `SelectedLanguage`-Fallback-Assert ergänzen (statt separatem Fallback-Test) |

Keine bestehenden Tests brechen: `SettingsViewModel`-Konstruktor und `ISettingsRepository`-Signaturen bleiben unverändert; `ReporterDbContextTests`/`EnsureCreated`-Tests erfassen die neue Spalte automatisch über das Modell.

### E2E-Tests (primärer Funktionsnachweis)

Das Repo besitzt **kein UI-/E2E-Testframework** (kein Appium o. ä.); die etablierte E2E-Stufe ist die ViewModel-getriebene Klasse `SettingsViewModelTests_E2E` (echte `SettingsRepository`-/`KeywordRepository`-Instanzen auf Shared-In-Memory-SQLite via `TestDbContextFactory`, Reload in einer neuen ViewModel-Instanz simuliert den App-Neustart). Die tatsächliche UI-/Startpfad-Wirkung (`{x:Static}`-Auflösung, `AppShell`-Tab-Titel) ist nur manuell verifizierbar — AGENTS.md verlangt dafür eine dokumentierte manuelle UI-Verifikation inkl. Screenshot und Formfaktor-Angabe.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Sprache im Einstellungen-Picker wechseln → sofort persistiert → nach „Neustart" (neue ViewModel-Instanz + `LoadCommand`) wieder selektiert | `SettingsViewModelTests_E2E.E2E_ChangeLanguage_PersistRoundtrip` | Sprachwahl wird dauerhaft gespeichert und beim nächsten Laden wiederhergestellt | Einzige automatisierbare Abdeckung des kompletten Benutzerflusses (ViewModel → Repository → SQLite → Reload); Unit-Tests prüfen Einzelglieder, nicht den Roundtrip |
| Pflicht | Sprache „Deutsch"/„English" wählen → App neu starten → sichtbare Texte (Tab-Titel, Einstellungsseite, Picker-Labels) in gewählter Sprache; `system` folgt der Gerätesprache | Manuelle UI-Verifikation, dokumentiert mit Screenshot in `test-results.md`/`docs/help/anwendung/mobile-ui-design.md` (AGENTS.md-Pflicht; Formfaktor 390×844, Light+Dark; Design-Vergleich `design-draft/stitch_local_rss_feed_reader/einstellungen_filter/screen.png`) | Kultur wird vor `CreateWindow` wirksam; `{x:Static}`-Bindungen und `AppShell`-Tab-Titel erscheinen in der gewählten Sprache; Neustart-Hinweis sichtbar | Startpfad (`MauiProgram`→`AppCulture`→`CreateWindow`→`AppShell`) liegt im MAUI-Projekt und ist von keinem Testprojekt erreichbar; nur ein realer App-Start beweist die tatsächliche UI-Wirkung |
| Empfohlen | Unbekannter persistierter `language`-Wert (manipulierte DB/Alt-Stand) → UI zeigt „System", Kultur bleibt Systemkultur, kein Crash | `SettingsViewModelTests_Load.Load_InvalidLanguage_UsesSystemFallback` + `AppCultureTests.ResolveCulture_ReturnsExpected` (`"fr"`→`null`) | Fallback-Konvention „unbekannt → system" | Über UI nicht direkt auslösbar (geschlossener Picker), aber anwendersichtbarer Fehlerfall bei Alt-Daten |

Bestehende E2E-Tests, die angepasst werden müssen: **Keine** — `SettingsViewModelTests_E2E` bleibt bestehen; der neue Language-Roundtrip wird als zusätzlicher Test ergänzt.

## Offene Punkte

| # | Offener Punkt | Empfohlener Vorschlag |
|---|---------------|----------------------|
| 1 | `AppResources.Designer.cs` wird design-time durch `PublicResXFileCodeGenerator` erzeugt (VS „Run Custom Tool"); ein CLI-Generierungsweg ist im Repo nicht eingerichtet | Properties für die neuen Schlüssel manuell im exakten Muster der bestehenden Designer-Properties ergänzen (XML-Kommentar + `ResourceManager.GetString("…", resourceCulture)`), falls kein Visual-Studio-Lauf verfügbar ist; vorhandene Datei ist eingecheckt und Compile-sicher |
| 2 | Kein UI-Testframework im Repo — die „echte" UI-E2E-Wirkung (Sprach-Picker in `SettingsPage`, Kultur-Setzung im realen Startpfad) ist automatisiert nicht prüfbar | Wie von AGENTS.md vorgeschrieben: dokumentierte manuelle Verifikation (Windows-Handysize 390×844, Light+Dark) mit Screenshot in `test-results.md`/`mobile-ui-design.md`; die automatisierbare E2E-Stufe (`SettingsViewModelTests_E2E`-Roundtrip) deckt Persistenz und Wiederherstellung ab |
