# Übersetzte Anforderung

## Titel
Einstellungen, Aufbewahrungsdauer, Keyword-Filter und Löschlogik (Issue #26)

## Fachliche Zusammenfassung

Die bisherige Platzhalter-`SettingsPage` wird zu einer vollständigen Einstellungsseite ausgebaut, über die alle nutzerkonfigurierbaren Optionen des Singleton-`Settings`-Datensatzes sowie die `Keyword`-Filterliste verwaltet werden. Der bereits beim App-Start laufende `RetentionCleanupService` wird um die Löschung keyword-gefilterter Artikel nach Fristablauf erweitert; die harten Lösch-Invarianten (niemals ungelesene Artikel, niemals `IsSavedForLater`-Artikel) bleiben bestehen. Ergänzend wird eine konfigurierbare, zeitgesteuerte Hintergrund-Aktualisierung eingeführt, die `IFeedSyncService.SyncAllAsync` periodisch aufruft.

## Betroffene Klassen und Komponenten

### Datenmodellklassen

- `Reporter.Data.Entities.Settings` und `Reporter.Core.Models.Settings` (Singleton, `DefaultId`): Bereits vorhanden sind `RetentionDays` (Standard 30), `AutoMarkReadMode` (string, Standard `"on_scroll"`), `AutoMarkReadDelaySeconds` (Standard 5), `NotificationsEnabled`, `QuietHoursStart`, `QuietHoursEnd`. Neu hinzuzufügen sind:
  - `AutoRefreshEnabled` (`bool`) — automatische Aktualisierung Ein/Aus
  - `RefreshIntervalMinutes` (`int`) — Aktualisierungsintervall (Einheit und Wertebereich offen, siehe Offene Fragen)
  - `Theme` (`string` oder Enum, z. B. `"system"`/`"light"`/`"dark"`) — Erscheinungsbild
  - Ggf. `AutoMarkReadOnOpenEnabled` (`bool`) als Ersatz/Ergänzung zu `AutoMarkReadMode` — siehe Offene Fragen
- `Reporter.Data.Entities.Item` und `Reporter.Core.Models.Item`: ggf. neues Flag (z. B. `IsKeywordFiltered`), falls das Keyword-Matching zur Sync-Zeit persistiert wird statt zur Cleanup-Zeit ausgewertet zu werden — abhängig von der Klärung der offenen Fragen
- `Keyword` (`Reporter.Data.Entities.Keyword` / `Reporter.Core.Models.Keyword`): unverändert; Entität und Unique-Index auf `keyword_text` existieren bereits
- Neue EF-Core-Migration in `src/Reporter.Data/Migrations/` für die neuen `settings`-Spalten (und ggf. `items`-Spalte); `ReporterDbContext.ConfigureSettings` entsprechend erweitern

### Logikklassen / Services

- `RetentionCleanupService` (`src/Reporter.Core/Services/RetentionCleanupService.cs`): Erweiterung um die Keyword-Regel — Artikel, die dem Keyword-Filter entsprechen, werden nach Ablauf von `RetentionDays` gelöscht; der Aufrufpunkt `App.OnStart` mit Fehlerisolierung bleibt bestehen
- `ItemRepository.DeleteExpiredAsync` (`src/Reporter.Data/Repositories/ItemRepository.cs`, Zeilen 305–311): Erweiterung der Löschbedingung oder neue Repository-Methode für keyword-gefilterte Artikel
- Neu: zentrale Keyword-Matching-Komponente in `Reporter.Core` (z. B. `KeywordMatcher` oder `IKeywordFilterService`): case-insensitiver Teilstring-Vergleich (z. B. `string.Contains` mit `StringComparison.OrdinalIgnoreCase`); wird vom Cleanup und später vom Benachrichtigungs-Arbeitspaket wiederverwendet
- Neu: Hintergrund-Aktualisierungs-Komponente (z. B. `IAutoRefreshService` / `AutoRefreshService`), die `IFeedSyncService.SyncAllAsync` zeitgesteuert aufruft und auf `AutoRefreshEnabled`/`RefreshIntervalMinutes` reagiert
- `SettingsRepository` (`src/Reporter.Data/Repositories/SettingsRepository.cs`): `SaveAsync`/`MapToModel` um die neuen Felder erweitern

### Interfaces

- `ISettingsRepository`, `IKeywordRepository`, `IItemRepository`, `IFeedSyncService`: bestehende Signaturen weitgehend ausreichend; `IItemRepository` erhält ggf. eine zusätzliche Lösch-/Abfragemethode für gefilterte Artikel
- Neu: Interface für die Keyword-Matching-Komponente und ggf. für den Auto-Refresh-Service (Konvention: `I<Name>` in `Reporter.Core.Interfaces`)

### Enums

- Ggf. Enum für das Erscheinungsbild (`System`/`Light`/`Dark`) in `Reporter.Core` — alternativ String-Konvention analog zu `AutoMarkReadMode`/`FeedHealth`

### UI-Komponenten

- `SettingsPage.xaml` (`src/Reporter/Views/SettingsPage.xaml`): Ausbau gemäß Design-Entwurf `design-draft/stitch_local_rss_feed_reader/einstellungen_filter` (und `_dark_mode`-Variante); Sektionen: Aufbewahrungsdauer, Keyword-Filter, Synchronisation & Lesefluss, Benachrichtigungen & Ruhezeiten, Erscheinungsbild; mobile Vorgaben aus `AGENTS.md` beachten (Touch-Targets ≥ 44×44 pt, `AppThemeBinding`, keine horizontale Tabellen)
- `SettingsViewModel` (`src/Reporter.Core/ViewModels/SettingsViewModel.cs`): bindbare Eigenschaften für alle Optionen, `AddKeywordCommand`/`RemoveKeywordCommand`, Sofort-Persistierung via `ISettingsRepository.SaveAsync`, Validierung `RetentionDays` 1–365
- `ArticleDetailViewModel` (`src/Reporter/ViewModels/ArticleDetailViewModel.cs`): berücksichtigt die globale Ein/Aus-Einstellung „automatisch beim Öffnen" (aktuell nur lokaler `IsAutoMarkRead`-Toggle; `AutoMarkReadDelaySeconds` wird bereits gelesen, `AutoMarkReadMode` wird ignoriert)
- `App.xaml.cs`: Theme beim Start anwenden (`Application.UserAppTheme`) und Start des Auto-Refresh-Timers
- `AppResources.resx` / `AppResources.de.resx` (`src/Reporter.Core/Resources/Strings/`): neue lokalisierte Strings für alle Einstellungs-Labels

### Tests

- Neu: `SettingsViewModelTests` (Laden, Speichern, Validierung 1–365, Keyword-Hinzufügen/Entfernen)
- Neu: Tests für die Keyword-Matching-Komponente (Case-Insensitivity, Teilwort, Ränder)
- `RetentionCleanupServiceTests`: Keyword-Löschregel und Bestätigung der Invarianten (ungelesen/`IsSavedForLater` bleiben erhalten)
- `SettingsRepositoryTests`: Persistenz der neuen Felder
- `ItemRepositoryTests`: erweiterte `DeleteExpiredAsync`-Bedingung
- Hinweis: `Settings` verwendet `required init`-Eigenschaften — neue Pflichtfelder betreffen alle Konstruktionsstellen (u. a. `RetentionCleanupServiceTests`, `SettingsRepositoryTests`, `ArticleDetailViewModel`-Fallback in Zeilen 238–246)

## Implementierungsansatz

- **Einstellungen-Seite:** `SettingsViewModel` lädt den Singleton über `ISettingsRepository.GetAsync`; jede Änderung wird sofort via `SaveAsync` persistiert und wirkt unmittelbar (z. B. Theme über `Application.Current.UserAppTheme`, das die vorhandenen `AppThemeBinding`-Styles umschaltet). Eingabemaske für Keywords mit Hinzufügen/Entfernen (Chips-Optik laut Design-Entwurf); der Unique-Index auf `keyword_text` verhindert Dubletten, Normalisierung/Trim im ViewModel.
- **Löschlogik:** `RetentionCleanupService.CleanupAsync` lädt zusätzlich zu den `Settings` die `Keyword`-Liste und erweitert die Löschmenge um Artikel, die dem Filter entsprechen und die Frist überschritten haben. Ob das Matching zur Cleanup-Zeit gegen `Title`/`ContentHtml` ausgewertet oder zur Sync-Zeit als Flag persistiert wird, hängt von der Klärung der offenen Fragen ab. Die Invarianten `!IsSavedForLater` (und ggf. `IsRead`) bleiben Teil der `ExecuteDeleteAsync`-Bedingung.
- **Keyword-Matching:** `IKeywordRepository.GetAllAsync` liefert die Filterliste; Matching case-insensitiv und als Teilwort — diese Semantik ist fix (der Toggle „Teilwort & Case-Insensitive" im Design-Entwurf gilt als Darstellung des festen Verhaltens, nicht als konfigurierbare Option — Annahme).
- **Hintergrund-Aktualisierung:** Neuer Timer-basierter Mechanismus (z. B. `IDispatcherTimer` oder `PeriodicTimer`), der bei gesetztem `AutoRefreshEnabled` alle `RefreshIntervalMinutes` `IFeedSyncService.SyncAllAsync` auslöst und bei Einstellungsänderung neu gestartet wird.
- **Lesestatus:** Die globale Einstellung steuert, ob `ArticleDetailViewModel` beim Öffnen den `MarkReadDelayedAsync`-Timer startet; die Verzögerung kommt aus `AutoMarkReadDelaySeconds` (bereits angebunden).
- **Benachrichtigungen:** In diesem Arbeitspaket werden nur `NotificationsEnabled` sowie `QuietHoursStart`/`QuietHoursEnd` persistiert und in der UI konfigurierbar gemacht; die Auswertung (Ruhezeiten, pro-Feed-Benachrichtigungen, Keyword-Einfluss auf Benachrichtigungen) erfolgt im separaten Benachrichtigungs-Arbeitspaket.
- **Registrierung:** Neue Services werden in `MauiProgram.CreateMauiApp` analog zum bestehenden Schema als Singletons registriert.

## Konfiguration

Alle Optionen werden im Singleton-`Settings`-Datensatz (Tabelle `settings`, `Settings.DefaultId`) persistiert — anwendungsweit, nicht pro Feed oder pro Datensatz. Keywords liegen als eigene Datensätze in der Tabelle `keywords`.

| Einstellung | Feld (Vorschlag) | Typ | Standard | Bemerkung |
|-------------|------------------|-----|----------|-----------|
| Automatische Aktualisierung | `AutoRefreshEnabled` | `bool` | `true` (Annahme) | Neu |
| Aktualisierungsintervall | `RefreshIntervalMinutes` | `int` | `30` (Annahme) | Neu; Wertebereich offen |
| Aufbewahrungsdauer | `RetentionDays` | `int` | `30` | Vorhanden; UI-Validierung 1–365 |
| Keyword-Filter | `keywords`-Tabelle | Datensätze | leer | Vorhanden; Hinzufügen/Entfernen |
| Auto-Gelesen beim Öffnen | `AutoMarkReadMode` bzw. `AutoMarkReadOnOpenEnabled` | `string?`/`bool` | – | Vorhanden bzw. neu; siehe Offene Fragen |
| Verzögerung | `AutoMarkReadDelaySeconds` | `int` | `5` | Vorhanden |
| Benachrichtigungen global | `NotificationsEnabled` | `bool` | `true` | Vorhanden |
| Ruhezeit Start/Ende | `QuietHoursStart`/`QuietHoursEnd` | `TimeSpan?` | `null` | Vorhanden; nur Speicherung |
| Erscheinungsbild | `Theme` | `string`/Enum | `"system"` (Annahme) | Neu |

## Offene Fragen

1. **Keyword-Regel vs. Ungelesen-Invariante:** Werden keyword-gefilterte Artikel nach Fristablauf auch dann gelöscht, wenn sie ungelesen sind (Keyword-Regel überstimmt die allgemeine Regel), oder bleibt „niemals ungelesene Artikel löschen" absolut und gefilterte Artikel werden erst gelöscht, sobald sie gelesen sind? Die Antwort bestimmt, ob das Matching zur Cleanup-Zeit gegen die Artikelinhalte ausgewertet oder zur Sync-Zeit als Flag am `Item` persistiert wird.
2. **Match-Felder:** Worauf wird der Keyword-Filter angewendet — nur auf `Item.Title` oder auch auf `ContentHtml`/`Link`?
3. **`AutoMarkReadMode` vs. Ein/Aus-Schalter:** Die Anforderung verlangt „automatisch beim Öffnen Ein/Aus". Das vorhandene Feld `AutoMarkReadMode` (Werte wie `"on_scroll"`/`"on_open"`) wird derzeit von `ArticleDetailViewModel` nicht ausgewertet. Soll das String-Feld weiterverwendet (z. B. `"off"`/`"on_open"`) oder durch eine boolesche Eigenschaft ersetzt werden?
4. **Aktualisierungsintervall:** Einheit und erlaubte Werte? Der Design-Entwurf schlägt feste Optionen vor (alle 15 Minuten / 30 Minuten / stündlich / alle 4 Stunden) — gilt das als Auswahlliste oder als freie Eingabe?
5. **Cleanup-Auslöser:** Der Cleanup läuft aktuell nur beim App-Start (`App.OnStart`). Genügt das als „Hintergrund-Cleanup-Service, der automatisch arbeitet", oder soll der Cleanup zusätzlich periodisch bzw. nach jeder Synchronisation laufen?
6. **Ruhezeiten über Mitternacht:** Sind Bereiche mit Start > Ende (z. B. 22:00–07:00) zulässig? Relevant für die UI-Eingabe (Auswertung erfolgt im Benachrichtigungs-Paket).
