# Umsetzungsplan: Lokale iOS-Benachrichtigungen mit Ruhezeiten (Issue #27)

## Übersicht

Nach jedem erfolgreichen Feed-Sync (`FeedSyncService`) prüft ein neuer `NotificationService` in `Reporter.Core`, ob neu gespeicherte `Item`s benachrichtigt werden dürfen (globaler Schalter, Pro-Feed-Schalter, Ruhezeit, Keyword-Filter), und delegiert die Anzeige an einen neuen plattformneutralen `ILocalNotificationService`, der unter iOS via `UserNotifications`-Framework arbeitet. Zusätzlich erhält der Feed-Datensatz ein neues `NotificationsEnabled`-Feld inkl. EF-Migration, Repository-Mapping und einem `Switch` im Feed-Bearbeitungsformular auf `FeedsPage`. Der Benachrichtigungsmodus — eine Benachrichtigung pro Artikel oder eine Sammel-Benachrichtigung pro Feed („n neue Artikel in Feed X") — ist über die neue Singleton-Einstellung `Settings.NotificationSummaryEnabled` auf der `SettingsPage` konfigurierbar.

Alle neun offenen Punkte der Anforderung wurden durch den Anwender entschieden: Die Punkte 1, 2 und 4–9 werden exakt mit den zuvor empfohlenen Vorschlägen umgesetzt; Punkt 3 (Aggregation) wird als einstellbare Option umgesetzt (`NotificationSummaryEnabled`, s. Designentscheidungen). Es verbleiben keine offenen Punkte.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Erweiterungspunkt Sync → Benachrichtigung | `INotificationService.NotifyNewItemsAsync(Feed, IReadOnlyList<Item>, CancellationToken)` direkt am Ende von `FeedSyncService.RunSyncAsync` aufrufen; `SyncResult` bleibt unverändert | Die neu gespeicherten `Item`s liegen nur in `RunSyncAsync` materialisiert vor. Alle Sync-Pfade (manuell via `FeedsViewModel`, periodisch via `AutoRefreshService`, gesamt via `SyncAllAsync`) laufen über diese Stelle — ein einziger Aufruf deckt alles ab, ohne `IFeedSyncService`-Signatur, `SyncResult`-Record, `FeedsViewModel` oder die `FakeFeedSyncService`-Implementierungen zu ändern. Die Alternative (`SyncResult` um `IReadOnlyList<Item>` erweitern und Caller rufen den Service) würde zwei Aufrufstellen (`RefreshAllAsync`-Pfad via `SyncAllAsync` aggregiert ohnehin ohne Feed-Kontext) und eine Verbreiterung des öffentlichen Sync-Vertrags erfordern. |
| Plattformdienst-Technologie | Natives `UserNotifications`-Framework hinter `ILocalNotificationService` (Gateway-Muster); kein `Plugin.LocalNotification`-NuGet | Keine externe Abhängigkeit; Android ist explizit optionales Folge-Issue. Das Interface bleibt austauschbar, falls später ein Plugin gewünscht wird. (Durch Anwender bestätigt.) |
| Struktur `LocalNotificationService` | Eine Klasse in `src/Reporter/Services/` mit `#if IOS`-Blöcken; auf anderen Targets No-Op | Folgt dem `AppThemeService`-Muster (Interface in `Reporter.Core/Interfaces`, Implementierung im App-Projekt). Der `UserNotifications`-Namespace existiert nur im `net10.0-ios`-Target — ein Compile-Guard ist zwingend. |
| Benachrichtigungsmodus (einzeln vs. Sammel) | Neue Singleton-Einstellung `Settings.NotificationSummaryEnabled` (`bool`, `required init`), persistiert als `settings.notification_summary_enabled`, **Default `false` = Einzelmodus**; pflegbar als `Switch` auf `SettingsPage` im Abschnitt „Benachrichtigungen & Ruhezeiten" | Anwenderentscheidung: Der Modus soll einstellbar sein. Der Default `false` entspricht der Anforderungsformulierung „Pro Artikel wird höchstens eine Benachrichtigung ausgelöst" und dem bisher geplanten Verhalten — bestehende Installationen ändern sich nicht. `required init` erzwingt das Setzen in `SettingsViewModel.PersistAsync`, dem `ArticleDetailViewModel`-Fallback und allen Test-Initializern per Compiler und verhindert stille Resets auf den Default. |
| Notification-Identifier | Einzelmodus: `Item.Id` (stabile GUID) als Identifier der `UNNotificationRequest`. Sammelmodus: Identifier aus `Feed.Id` + stabilem Hash der `Item.Id`s der benachrichtigten Artikel (z. B. `{feedId}-{sha256(sortierte ItemIds)}`) | iOS ersetzt Requests mit identischem Identifier → im Einzelmodus garantiert das zusammen mit dem DB-Unique-Index `(feed_id, guid_or_hash)` die Anforderung „pro Artikel höchstens eine Benachrichtigung". Im Sammelmodus erzeugt derselbe Artikelbestand denselben Identifier (keine Dopplung bei wiederholtem Aufruf oder erneutem Sync mit unverändertem Bestand), während ein neuer Artikelbestand einen neuen Identifier und damit eine neue Benachrichtigung erzeugt statt die alte still zu ersetzen. |
| `Feed.NotificationsEnabled` (Core-Modell) | `required init`-Eigenschaft ohne Default im Modell | Erzwingt das Setzen an allen `new Feed { … }`-Initializern (Produktivcode und Tests) per Compiler — verhindert, dass `FeedSyncService.UpdateFeedHealthAsync` oder `FeedsViewModel.SaveAsync` den Schalter beim Rekonstruieren des `Feed` versehentlich auf den Default zurücksetzen. DB-/Entity-Default bleibt `true`. |
| Inhalt der Sammel-Benachrichtigung | Titel = `Feed.Title`; Body = lokalisierte Zeichenfolge aus neuem `AppResources.NotificationSummaryFormat` (Anzahl + Auflistung der Artikeltitel, bei Bedarf gekürzt) | `AppResources` liegt in `Reporter.Core` und ist damit aus `NotificationService` nutzbar; die Anzahl bezieht sich ausschließlich auf die tatsächlich benachrichtigten (nicht gefilterten) Artikel. |

## Programmabläufe

### Benachrichtigungsprüfung nach Feed-Sync

1. Ein Sync wird ausgelöst — manuell über `FeedsViewModel.RefreshAsync`/`RefreshAllAsync` oder periodisch über `AutoRefreshService.RunLoopAsync` → `IFeedSyncService.SyncFeedAsync`/`SyncAllAsync`.
2. `FeedSyncService.RunSyncAsync` sammelt die neu eingefügten `Item`s zusätzlich zum bisherigen `newItems`-Zähler in einer Liste `newItemEntities`.
3. Nach `UpdateFeedHealthAsync` und `UpdateLogAsync`: bei `newItemEntities.Count > 0` Aufruf von `INotificationService.NotifyNewItemsAsync(feed, newItemEntities, cancellationToken)` — in einem eigenen `try/catch`, damit ein Fehler im Benachrichtigungspfad das `SyncResult` nicht verändert (Fehler nur loggen, analog `App.OnStart`-Muster).
4. `NotificationService.NotifyNewItemsAsync` wertet in dieser Reihenfolge aus:
   1. `feed.NotificationsEnabled == false` → Abbruch.
   2. `ISettingsRepository.GetAsync` → `settings.NotificationsEnabled == false` → Abbruch.
   3. Ruhezeit (nur wenn `QuietHoursStart` **und** `QuietHoursEnd` gesetzt sind): `now = TimeProvider.GetLocalNow().TimeOfDay`; `Start < End` → aktiv, wenn `now >= Start && now < End`; `Start > End` (über Mitternacht, z. B. 22:00–07:00) → aktiv, wenn `now >= Start || now < End`; `Start == End` → leeres Intervall → keine Ruhezeit. Ist nur einer der beiden Werte gesetzt, gilt keine Ruhezeit. Aktive Ruhezeit → alle Kandidaten werden verworfen — kein Nachholen (durch Anwender bestätigt).
   4. `IKeywordRepository.GetAllAsync` → `keywordTexts`; pro Item `IKeywordMatcher.MatchesAny(item.Title, item.ContentHtml, keywordTexts)` → Treffer verwirft das Item.
5. Modus-Verzweigung anhand `settings.NotificationSummaryEnabled`:
   - `false` (Einzelmodus): pro verbleibendem Item `ILocalNotificationService.ShowAsync(feed.Title, item.Title, item.Id.ToString(), cancellationToken)`. Dedup: Identifier = `Item.Id`; iOS ersetzt identische Identifier, und der DB-Unique-Index stellt sicher, dass ein Artikel ohnehin nur einmal als „neu" gespeichert wird.
   - `true` (Sammelmodus): genau ein `ILocalNotificationService.ShowAsync(feed.Title, summaryBody, summaryIdentifier, cancellationToken)`. `summaryBody` wird über `AppResources.NotificationSummaryFormat` aus der Anzahl der verbleibenden Items und deren Titeln formatiert; `summaryIdentifier` setzt sich aus `Feed.Id` und einem stabilen Hash der sortierten `Item.Id`s zusammen. Dedup: identischer Artikelbestand → identischer Identifier → iOS ersetzt die bestehende Sammel-Benachrichtigung statt zu duplizieren; neuer Bestand → neuer Identifier → neue Benachrichtigung. Von der Ruhezeit verworfene oder keyword-gefilterte Items fließen weder in Anzahl/Body noch in den Identifier-Hash ein.
6. `LocalNotificationService.ShowAsync` (iOS-Pfad): `UNUserNotificationCenter.Current.GetNotificationSettingsAsync` auswerten → bei `NotDetermined` `RequestAuthorizationAsync(Alert | Badge | Sound)` aufrufen; bei verweigerter Berechtigung abbrechen. Anschließend `UNMutableNotificationContent` (Title = Feed-Titel, Body = Artikeltitel bzw. formatierte Sammel-Zeichenfolge, Standard-Sound; `Item.Id`/`Link` bzw. `Feed.Id` in `UserInfo` für späteres Deep-Linking) + `UNNotificationRequest` mit dem modusabhängigen Identifier und sofortigem Trigger → `AddNotificationRequest`. Nicht-iOS-Targets: No-Op.

Beteiligte Klassen/Komponenten: `FeedSyncService`, `INotificationService`/`NotificationService`, `ILocalNotificationService`/`LocalNotificationService`, `ISettingsRepository`, `IKeywordRepository`, `IKeywordMatcher`, `TimeProvider`, `AppResources`.

### Pro-Feed-Schalter im Feed-Formular pflegen

1. `FeedsPage.xaml`: In der bestehenden Bearbeitungskarte (`Border` ab Zeile 15) unterhalb des `Picker` eine Zeile nach dem exakten Muster aus `SettingsPage.xaml` (Zeilen 240–254) einfügen: `Grid ColumnDefinitions="*,Auto"` — links `Label` + Hinweis-`Label` (`MetaStyle`), rechts `Switch` mit `IsToggled="{Binding FeedNotificationsEnabled}"`, `MinimumWidthRequest`/`MinimumHeightRequest="44"` und `SemanticProperties.Description`. Die Karten-Farben bleiben über das bestehende `AppThemeBinding` des `Border` abgedeckt.
2. `FeedsViewModel.EditAsync`: `FeedNotificationsEnabled = feed.NotificationsEnabled` (vorbefüllt aus `FeedListItem`).
3. `FeedsViewModel.SaveAsync`: Add- und Update-Pfad setzen `NotificationsEnabled = FeedNotificationsEnabled` am neu konstruierten `Feed`; nach erfolgreichem Speichern Reset des Formulars auf `FeedNotificationsEnabled = true`.
4. `FeedRepository` überträgt das Feld in `MapToModel`/`MapToEntity`/`UpdateAsync` und projiziert es in `GetAllWithDetailsAsync` auf `FeedListItem`.

Beteiligte Klassen/Komponenten: `FeedsViewModel`, `FeedsPage.xaml`, `FeedListItem`, `FeedRepository`, `AppResources`.

### Benachrichtigungsmodus in den Einstellungen pflegen

1. `SettingsPage.xaml`: In der Karte „Benachrichtigungen & Ruhezeiten" (Zeilen 232–326) innerhalb des `Border` mit `IsEnabled="{Binding NotificationsEnabled}"` (Zeilen 255–323) eine weitere Zeile im selben `Grid ColumnDefinitions="*,Auto"`-Muster wie die Ruhezeiten-Zeile (Zeilen 266–280) einfügen — vor dem Ruhezeiten-`Grid`: links `Label` (`SettingsNotificationSummaryLabel`) + Hinweis-`Label` (`SettingsNotificationSummaryHint`, `MetaStyle`), rechts `Switch` mit `IsToggled="{Binding NotificationSummaryEnabled}"`, `MinimumWidthRequest`/`MinimumHeightRequest="44"`, `SemanticProperties.Description`. Die Platzierung innerhalb des verschachtelten `Border` erbt die Deaktivierung und die 0,4-Abdunklung bei ausgeschaltetem globalem Schalter automatisch; Farben bleiben über die vorhandenen `AppThemeBinding`s abgedeckt.
2. `SettingsViewModel`: Property `NotificationSummaryEnabled` (Backing-Field `_notificationSummaryEnabled`, Startwert `false`, Setter ruft `PersistOnChange()` — Muster identisch zu `NotificationsEnabled`, Zeilen 267–277); `LoadAsync` befüllt aus `settings.NotificationSummaryEnabled`; `PersistAsync` setzt `NotificationSummaryEnabled` am rekonstruierten `Settings`.
3. `SettingsRepository.MapToModel`/`SaveAsync` übertragen das Feld; `Entities.Settings` erhält `NotificationSummaryEnabled` (Default `false`); `ReporterDbContext.ConfigureSettings` mappt die Spalte `notification_summary_enabled`; Migration `AddSettingsNotificationSummary`.

Beteiligte Klassen/Komponenten: `SettingsViewModel`, `SettingsPage.xaml`, `Settings` (Core/Entity), `SettingsRepository`, `ReporterDbContext`, `AppResources`.

### Berechtigungsanfrage beim App-Start

1. `App.OnStart`: nach dem Laden der Settings (`ISettingsRepository.GetAsync`, bestehender `try/catch`-Block um Zeilen 51–62) in einem eigenen `try/catch` prüfen: `settings.NotificationsEnabled == true` → `ILocalNotificationService.RequestAuthorizationAsync`. iOS zeigt den System-Dialog nur beim ersten Aufruf; die Anfrage ist idempotent. (Durch Anwender bestätigt: App-Start statt erstem Versand.)
2. Nicht-iOS-Targets: No-Op durch die Plattformimplementierung.

Beteiligte Klassen/Komponenten: `App`, `ILocalNotificationService`/`LocalNotificationService`, `ISettingsRepository`.

### Vordergrund-Darstellung auf iOS

1. `AppDelegate`: `FinishedLaunching` überschreiben und `UNUserNotificationCenter.Current.Delegate = new NotificationDelegate()` setzen, danach `base.FinishedLaunching` aufrufen.
2. `NotificationDelegate.WillPresentNotification` ruft die Completion mit `UNNotificationPresentationOptions.Banner | List | Sound` — Benachrichtigungen werden auch sichtbar, während die App geöffnet ist. Das ist funktional notwendig, weil `AutoRefreshService` nur bei laufender App synchronisiert. (Durch Anwender bestätigt.)

Beteiligte Klassen/Komponenten: `AppDelegate`, `NotificationDelegate`, `UNUserNotificationCenter`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `INotificationService` (`src/Reporter.Core/Interfaces/INotificationService.cs`) | Interface | Entscheidungsservice: `NotifyNewItemsAsync(Feed feed, IReadOnlyList<Item> newItems, CancellationToken cancellationToken = default)` |
| `ILocalNotificationService` (`src/Reporter.Core/Interfaces/ILocalNotificationService.cs`) | Interface | Plattformabstraktion: `RequestAuthorizationAsync(CancellationToken)` → `Task<bool>`; `ShowAsync(string title, string body, string identifier, CancellationToken)` → `Task` |
| `NotificationService` (`src/Reporter.Core/Services/NotificationService.cs`) | Klasse | Entscheidungslogik (globaler Schalter, Pro-Feed-Flag, Ruhezeit via `TimeProvider`, Keyword-Filter) und Modus-Verzweigung (Einzel- vs. Sammel-Benachrichtigung anhand `Settings.NotificationSummaryEnabled` inkl. modusspezifischer Identifier); Abhängigkeiten: `ISettingsRepository`, `IKeywordRepository`, `IKeywordMatcher`, `ILocalNotificationService`, optionaler `TimeProvider` (Fallback `TimeProvider.System`, Muster wie `AutoRefreshService`) |
| `LocalNotificationService` (`src/Reporter/Services/LocalNotificationService.cs`) | Klasse | Plattformimplementierung mit `#if IOS` (`UNUserNotificationCenter`); No-Op auf anderen Targets |
| `NotificationDelegate` (`src/Reporter/Platforms/iOS/NotificationDelegate.cs`) | Klasse | `UNUserNotificationCenterDelegate` für die Vordergrund-Darstellung |
| `FakeLocalNotificationService` (`src/Reporter.Tests/FakeLocalNotificationService.cs`) | Test-Hilfsklasse | Zeichnet `ShowAsync`-Aufrufe auf (Muster: `FakeAppThemeService`) |
| `FakeNotificationService` (`src/Reporter.Tests/FakeNotificationService.cs`) | Test-Hilfsklasse | Zeichnet `NotifyNewItemsAsync`-Aufrufe; optional konfigurierbar zum Werfen (Fehlerrobustheit des Sync) |

## Änderungen an bestehenden Klassen

### `Feed` (`Reporter.Core.Models`, Datenmodellklasse)

- **Neue Eigenschaften:** `NotificationsEnabled` (`bool`, `required init`) — Pro-Feed-Schalter für Benachrichtigungen.

### `Feed` (`Reporter.Data.Entities`, EF-Entität)

- **Neue Eigenschaften:** `NotificationsEnabled` (`bool`, `= true` Default) — neue Spalte `notifications_enabled`.

### `Settings` (`Reporter.Core.Models`, Datenmodellklasse)

- **Neue Eigenschaften:** `NotificationSummaryEnabled` (`bool`, `required init`) — Benachrichtigungsmodus: `false` = einzeln pro Artikel (Default), `true` = Sammel-Benachrichtigung pro Feed.

### `Settings` (`Reporter.Data.Entities`, EF-Entität)

- **Neue Eigenschaften:** `NotificationSummaryEnabled` (`bool`, `= false` Default) — neue Spalte `notification_summary_enabled`.

### `FeedListItem` (`Reporter.Core.Models`, Datenmodellklasse)

- **Neue Eigenschaften:** `NotificationsEnabled` (`bool`, `required init`) — Vorbefüllung des Schalters beim Bearbeiten.

### `ReporterDbContext` (`Reporter.Data`)

- **Geänderte Methoden:**
  - `ConfigureFeed` — Mapping `entity.Property(e => e.NotificationsEnabled).HasColumnName("notifications_enabled").IsRequired().HasDefaultValue(true)`.
  - `ConfigureSettings` — Mapping `entity.Property(e => e.NotificationSummaryEnabled).HasColumnName("notification_summary_enabled").IsRequired().HasDefaultValue(false)`; der `HasData`-Seed des Singletons übernimmt den Entity-Default `false` automatisch.

### `FeedRepository` (`Reporter.Data.Repositories`)

- **Geänderte Methoden:**
  - `MapToModel` — `NotificationsEnabled` übertragen.
  - `MapToEntity` — `NotificationsEnabled` übertragen.
  - `UpdateAsync` — `entity.NotificationsEnabled = feed.NotificationsEnabled` ergänzen.
  - `GetAllWithDetailsAsync` — `NotificationsEnabled` in die `FeedListItem`-Projektion aufnehmen.

### `SettingsRepository` (`Reporter.Data.Repositories`)

- **Geänderte Methoden:**
  - `MapToModel` — `NotificationSummaryEnabled` übertragen.
  - `SaveAsync` — `entity.NotificationSummaryEnabled = settings.NotificationSummaryEnabled` ergänzen.

### `FeedSyncService` (`Reporter.Core.Services`)

- **Neue Konstruktor-Abhängigkeit:** `INotificationService` (fünfter Parameter — bricht `CreateService`/`CreateFailingService` in `FeedSyncServiceTests` und erfordert DI-Registrierung).
- **Geänderte Methoden:**
  - `RunSyncAsync` — sammelt neu eingefügte `Item`s in `newItemEntities`; ruft nach `UpdateLogAsync` bei nicht-leerer Liste `INotificationService.NotifyNewItemsAsync` in eigenem `try/catch` auf.
  - `UpdateFeedHealthAsync` — setzt `NotificationsEnabled = feed.NotificationsEnabled` am rekonstruierten `Feed` (kritisch: sonst Reset des Schalters bei jedem Sync).

### `FeedsViewModel` (`Reporter.Core.ViewModels`)

- **Neue Eigenschaften:** `FeedNotificationsEnabled` (`bool`, Startwert `true`) — Formularfeld für den Pro-Feed-Schalter.
- **Geänderte Methoden:**
  - `SaveAsync` — `NotificationsEnabled = FeedNotificationsEnabled` in Add- und Update-Pfad; Reset auf `true` nach dem Speichern.
  - `EditAsync` — Vorbefüllung `FeedNotificationsEnabled = feed.NotificationsEnabled`.

### `SettingsViewModel` (`Reporter.Core.ViewModels`)

- **Neue Eigenschaften:** `NotificationSummaryEnabled` (`bool`, Startwert `false`) — Einstellungs-Schalter für den Sammel-Modus; Setter ruft `PersistOnChange()` (Muster `NotificationsEnabled`).
- **Geänderte Methoden:**
  - `LoadAsync` — Vorbefüllung `NotificationSummaryEnabled = settings.NotificationSummaryEnabled`.
  - `PersistAsync` — `NotificationSummaryEnabled = NotificationSummaryEnabled` am rekonstruierten `Settings` (durch `required init` ohnehin compilerverpflichtend — wird beim Datenmodell-Schritt zunächst mit `false` ergänzt und hier an das Property gebunden).

### `FeedsPage.xaml` (`Reporter.Views`)

- Switch-Zeile in der Bearbeitungskarte (Muster `SettingsPage.xaml` Zeilen 240–254; Touch-Target ≥ 44 pt, `SemanticProperties.Description`).

### `SettingsPage.xaml` (`Reporter.Views`)

- Neue Switch-Zeile für den Benachrichtigungsmodus innerhalb des von `NotificationsEnabled` gesteuerten `Border` in der Karte „Benachrichtigungen & Ruhezeiten" (vor dem Ruhezeiten-`Grid`, Muster Zeilen 266–280; `*,Auto`-Grid, Touch-Target ≥ 44 pt, `SemanticProperties.Description`).

### `ArticleDetailViewModel` (`src/Reporter/ViewModels`)

- **Geänderte Methoden:** `LoadAsync` — der `new Settings { … }`-Fallback (Zeile 250) wird um `NotificationSummaryEnabled` ergänzt (durch `required init` compilerverpflichtend).

### `App` (`src/Reporter/App.xaml.cs`)

- **Geänderte Methoden:** `OnStart` — neuer `try/catch`-Block: bei `settings.NotificationsEnabled` Aufruf von `ILocalNotificationService.RequestAuthorizationAsync`.

### `AppDelegate` (`src/Reporter/Platforms/iOS`)

- **Geänderte Methoden:** `FinishedLaunching` überschreiben — `UNUserNotificationCenter.Current.Delegate = new NotificationDelegate()` setzen.

### `MauiProgram` (`src/Reporter`)

- **Geänderte Methoden:** `CreateMauiApp` — `AddSingleton<INotificationService, NotificationService>()` und `AddSingleton<ILocalNotificationService, LocalNotificationService>()` in der bestehenden `AddSingleton`-Kette (vor den ViewModels).

### `AppResources` (`src/Reporter.Core/Resources/Strings`)

- Neue Schlüssel in `AppResources.resx` und `AppResources.de.resx`; `AppResources.Designer.cs` über `PublicResXFileCodeGenerator` regenerieren:
  - `FeedNotificationsLabel`, `FeedNotificationsHint` — Pro-Feed-Schalter im Feed-Formular.
  - `SettingsNotificationSummaryLabel`, `SettingsNotificationSummaryHint` — Modus-Schalter auf der `SettingsPage`.
  - `NotificationSummaryFormat` — Formatzeichenfolge für den Body der Sammel-Benachrichtigung (Platzhalter: Anzahl, Liste der Artikeltitel).
- Artikelbezogene Benachrichtigungstexte im Einzelmodus sind dynamisch (Feed-/Artikeltitel) und benötigen keine Ressourcen.

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `AddFeedNotificationsEnabled` | `feeds.notifications_enabled` | `AddColumn<bool>` (`INTEGER`, `nullable: false`, `defaultValue: true`) — Muster `20260911080630_AddSettingsAutoRefreshAndTheme`; erzeugt via `dotnet ef migrations add` gegen `ReporterDbContextFactory`. Bestehende Feeds erhalten `true` (durch Anwender bestätigt). |
| `AddSettingsNotificationSummary` | `settings.notification_summary_enabled` | `AddColumn<bool>` (`INTEGER`, `nullable: false`, `defaultValue: false`) plus `UpdateData` des Settings-Singletons (`HasData`-Seed) auf `false` — gleiches Muster wie die `UpdateData` des Singletons in `AddSettingsAutoRefreshAndTheme`. Bestehende Installationen behalten den Einzelmodus. |

Hinweis: Die Testsuite nutzt `EnsureCreated`/`EnsureCreatedAsync` statt `Migrate` — das Migrations-SQL wird nicht automatisch geprüft; beide Migrationen sind bei der Umsetzung manuell zu kontrollieren.

## Validierungsregeln

Keine neuen. Die Ruhezeiten bleiben bewusst unvalidiert (vgl. `docs/help/einstellungen/business-rules.md`, „Ruhezeiten ohne Start-vor-Ende-Validierung" — die Auswertung ist diesem Arbeitspaket vorbehalten). Die Behandlung von `Start == End` als leeres Intervall und von einseitig `null` als „keine Ruhezeit" ist Auswertungslogik, keine Eingabevalidierung. `NotificationSummaryEnabled` ist ein boolscher Schalter ohne Eingabefreiheit. Die bestehende Feed-Formularvalidierung (URL, Titel, Duplikat) bleibt unverändert.

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `Settings.NotificationSummaryEnabled` (Spalte `settings.notification_summary_enabled`) | `bool` | `false` (Einzelmodus) | Steuert, ob pro neuem Artikel eine eigene Benachrichtigung (`false`) oder eine Sammel-Benachrichtigung pro Feed (`true`) ausgelöst wird; pflegbar auf `SettingsPage`. |

Unverändert: `Platforms/iOS/Info.plist` benötigt für lokale Benachrichtigungen keine zusätzlichen Schlüssel (Runtime-Authorization genügt — durch Anwender bestätigt); `PrivacyInfo.xcprivacy` bleibt unverändert (`UserNotifications` ist keine Required-Reason-API); keine neuen NuGet-Pakete erforderlich.

## Seiteneffekte und Risiken

- **`Feed`-Initializer (Produktivcode + Tests):** `required init` auf `NotificationsEnabled` macht alle `new Feed { … }`-Stellen compilerverpflichtend — betroffen: `FeedRepository.MapToModel`, `FeedSyncService.UpdateFeedHealthAsync`, `FeedsViewModel.SaveAsync` (2×) sowie Initializer in `FeedRepositoryTests`, `FeedSyncServiceTests.SeedFeedAsync` und `FeedsViewModelTests`. Das ist gewollt: Der Compiler verhindert stille Resets des Schalters.
- **`Settings`-Initializer (Produktivcode + Tests):** `required init` auf `NotificationSummaryEnabled` macht alle `new Settings { … }`-Stellen compilerverpflichtend — betroffen: `SettingsRepository.MapToModel`, `SettingsViewModel.PersistAsync`, der `ArticleDetailViewModel`-Fallback (`src/Reporter/ViewModels/ArticleDetailViewModel.cs:250`) sowie Test-Initializer in `SettingsViewModelTests_Persist` (4×), `SettingsViewModelTests_Load` (2×), `SettingsRepositoryTests` (3×), `RetentionCleanupServiceTests` (1×) und `AutoRefreshServiceTests` (2×).
- **`FeedSyncService`-Konstruktor:** neue `INotificationService`-Abhängigkeit erfordert Anpassung der Test-Hilfsmethoden `CreateService`/`CreateFailingService` und der DI-Registrierung; die App ist ohne Registrierung nicht startfähig (DI-Auflösungsfehler beim ersten Sync).
- **Fehlerrobustheit:** Ein Ausfall des Benachrichtigungspfads (z. B. iOS-Berechtigungsfehler) darf `SyncResult`/Feed-Health nicht verfälschen → strikter `try/catch` um `NotifyNewItemsAsync` in `RunSyncAsync`.
- **Sammel-Modus und Mitteilungszentrum:** Ersetzt iOS eine Sammel-Benachrichtigung mit identischem Identifier, ist der frühere Stand im Mitteilungszentrum nicht mehr sichtbar — beabsichtigtes Verhalten (Dedup). Wechselt der Nutzer den Modus, wirkt die Änderung ab dem nächsten Sync; bereits zugestellte Benachrichtigungen werden nicht nachträglich umgruppiert (kein Entfernen alter Requests geplant).
- **Plattformcode nicht unit-testbar:** `UNUserNotificationCenter` kann in `Reporter.Tests` nicht geprüft werden; die Entscheidungslogik liegt deshalb vollständig in `Reporter.Core`. Die iOS-Verifikation per `scripts/iOS-Deployment.ps1` (Aktion `simulator`) ist nur auf macOS ausführbar — in dieser Windows-Umgebung entfällt sie bzw. ist als manueller Folgeschritt zu dokumentieren.
- **Zeitzone:** Die Ruhezeitauswertung nutzt `TimeProvider.GetLocalNow()` (lokale Gerätezeit) — in Tests deterministisch via `Microsoft.Extensions.Time.Testing.FakeTimeProvider` steuerbar.
- **`ServiceCollectionTests`/`ReporterDbContextTests_Schema`:** registrieren nur Repositories bzw. prüfen Tabellen über `EnsureCreated` — keine Brüche erwartet; die neuen Spalten sind in `EnsureCreated`-Schemas automatisch enthalten, der Settings-Seed bleibt ein einzelner Datensatz.

## Umsetzungsreihenfolge

1. **Datenmodell und Migrationen**
   - Voraussetzungen: `ReporterDbContextFactory` (vorhanden, `src/Reporter.Data/ReporterDbContextFactory.cs`), EF-Core-Tools.
   - Beschreibung: `Feed.NotificationsEnabled` und `Settings.NotificationSummaryEnabled` (je `required init`) in `Reporter.Core.Models`; `Entities.Feed.NotificationsEnabled` (`= true`) und `Entities.Settings.NotificationSummaryEnabled` (`= false`); `ConfigureFeed`-Mapping `notifications_enabled` und `ConfigureSettings`-Mapping `notification_summary_enabled`; Migrationen `AddFeedNotificationsEnabled` und `AddSettingsNotificationSummary` erzeugen. Alle dadurch brechenden `new Settings { … }`-Initializer im Produktivcode ergänzen: `SettingsViewModel.PersistAsync` (zunächst `false`, Bindung an das ViewModel-Property folgt in Schritt 8) und `ArticleDetailViewModel`-Fallback.
2. **Repository-Mapping und `FeedListItem`**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `FeedListItem.NotificationsEnabled` (`required init`); `FeedRepository`-Methoden `MapToModel`, `MapToEntity`, `UpdateAsync`, `GetAllWithDetailsAsync` erweitern; `SettingsRepository.MapToModel`/`SaveAsync` um `NotificationSummaryEnabled` erweitern.
3. **Interfaces anlegen**
   - Voraussetzungen: Keine (baut nur auf vorhandenen Modellen `Feed`/`Item` auf).
   - Beschreibung: `ILocalNotificationService` und `INotificationService` in `src/Reporter.Core/Interfaces/` anlegen.
4. **Plattformdienst und iOS-Delegate**
   - Voraussetzungen: Schritt 3. Kein NuGet nötig — `UserNotifications` ist Teil des iOS-Workloads.
   - Beschreibung: `LocalNotificationService` mit `#if IOS` (`RequestAuthorizationAsync`, `ShowAsync` via `UNMutableNotificationContent`/`UNNotificationRequest` inkl. `UserInfo`-Übergabe für späteres Deep-Linking), No-Op sonst; `NotificationDelegate` unter `Platforms/iOS/`; `AppDelegate.FinishedLaunching` mit Delegate-Zuweisung.
5. **`NotificationService` (Entscheidungslogik)**
   - Voraussetzungen: Schritt 3 (Interfaces); vorhandene `ISettingsRepository`, `IKeywordRepository`, `IKeywordMatcher`; etabliertes `TimeProvider`-Muster; `AppResources.NotificationSummaryFormat` (kann parallel zu Schritt 8 angelegt werden — der Schlüssel muss vor dem finalen Kompilieren existieren).
   - Beschreibung: Implementierung in `src/Reporter.Core/Services/` gemäß Ablauf „Benachrichtigungsprüfung nach Feed-Sync" (Schalter, Ruhezeit inkl. Wrap-around und Grenzfälle, Keyword-Filter, Modus-Verzweigung Einzel-/Sammel-Versand mit den in den Designentscheidungen definierten Identifiern).
6. **`FeedSyncService`-Integration**
   - Voraussetzungen: Schritte 1–5.
   - Beschreibung: Konstruktor-Abhängigkeit `INotificationService`; `newItemEntities`-Liste in `RunSyncAsync`; Aufruf nach `UpdateLogAsync` in `try/catch`; `UpdateFeedHealthAsync` reicht `NotificationsEnabled` durch.
7. **Feed-Formular (ViewModel + Page + Strings)**
   - Voraussetzungen: Schritte 1–2. UI-Muster `SettingsPage.xaml` (Switch-Zeile mit 44-pt-Touch-Target) ist vorhanden und wird wiederverwendet.
   - Beschreibung: `FeedsViewModel.FeedNotificationsEnabled` inkl. Save/Edit/Reset; `Switch`-Zeile in `FeedsPage.xaml`; `AppResources`-Schlüssel `FeedNotificationsLabel`/`FeedNotificationsHint` (en + de).
8. **Einstellungs-UI für den Benachrichtigungsmodus (ViewModel + Page + Strings)**
   - Voraussetzungen: Schritte 1–2. UI-Muster der Ruhezeiten-Zeile in `SettingsPage.xaml` (Zeilen 266–280) ist vorhanden und wird wiederverwendet.
   - Beschreibung: `SettingsViewModel.NotificationSummaryEnabled` inkl. `PersistOnChange`, `LoadAsync`-Vorbefüllung und `PersistAsync`-Bindung (ersetzt das interimistische `false` aus Schritt 1); `Switch`-Zeile in `SettingsPage.xaml` innerhalb des `NotificationsEnabled`-gesteuerten `Border`; `AppResources`-Schlüssel `SettingsNotificationSummaryLabel`/`SettingsNotificationSummaryHint`/`NotificationSummaryFormat` (en + de) inkl. `AppResources.Designer.cs`-Regenerierung (deckt auch die Schritt-7-Schlüssel ab).
9. **DI-Registrierung und `App.OnStart`**
   - Voraussetzungen: Schritte 3–6.
   - Beschreibung: `MauiProgram.CreateMauiApp` — `INotificationService`/`ILocalNotificationService` als Singleton; `App.OnStart` — Berechtigungsanfrage-Block bei aktivem globalem Schalter.
10. **Unit-/Integrationstests**
    - Voraussetzungen: Schritte 1–9; `Microsoft.Extensions.TimeProvider.Testing` (FakeTimeProvider), `TestDbContextFactory`, `FakeHttpMessageHandler` vorhanden.
    - Beschreibung: Neue Fakes und Testklassen/-methoden gemäß Abschnitt „Tests" (beide Benachrichtigungsmodi abdecken); betroffene `new Feed`-/`new Settings`-Initializer und `CreateService`-Hilfen anpassen; Suite `dotnet test src/Reporter.Tests` grün.
11. **Manuelle UI-/Plattform-Verifikation, statische Checks, Doku**
    - Voraussetzungen: Schritte 1–10.
    - Beschreibung: `FeedsPage` und `SettingsPage` im Windows-Handysize-Fenster 390 × 844 pt prüfen (beide neuen Switches sichtbar/bedienbar, Touch-Targets ≥ 44 pt, Modus-Schalter bei ausgeschaltetem globalem Schalter deaktiviert/abgedunkelt, Dark Mode via `AppThemeBinding`, Vergleich mit `design-draft/stitch_local_rss_feed_reader/feeds_health_status/screen.png`); Edit → Schalter → Speichern → Reload-Fluss sowie Modus-Umschalten → Persistenz manuell durchspielen; iOS-Simulator-Verifikation via `scripts/iOS-Deployment.ps1` (nur macOS — ggf. als Folgeschritt dokumentieren) inkl. Sammel-Benachrichtigung; Ergebnis in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md` dokumentieren; `scripts/Run-StaticChecks.ps1` muss mit Exit-Code 0 durchlaufen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `FakeLocalNotificationService` | — (Hilfsklasse) | Zeichnet `ShowAsync`-Aufrufe (Titel/Body/Identifier) auf; `RequestAuthorizationAsync` konfigurierbar |
| `FakeNotificationService` | — (Hilfsklasse) | Zeichnet `NotifyNewItemsAsync`-Aufrufe (Feed + Items); optional Exception zum Prüfen der Sync-Robustheit |
| `NotifyNewItemsAsync_FeedDisabled_SendsNothing` | `NotificationServiceTests` | `feed.NotificationsEnabled == false` → kein `ShowAsync` |
| `NotifyNewItemsAsync_GlobalDisabled_SendsNothing` | `NotificationServiceTests` | `Settings.NotificationsEnabled == false` → kein `ShowAsync` |
| `NotifyNewItemsAsync_WithinQuietHours_SendsNothing` / `OutsideQuietHours_Sends` | `NotificationServiceTests` (Theory mit `FakeTimeProvider`) | Ruhezeit 22:00–07:00, `now` = 23:00/06:00 → unterdrückt; `now` = 12:00/21:00 → sendet (Wrap-around) |
| `NotifyNewItemsAsync_QuietHoursStartEqualsEnd_Sends` | `NotificationServiceTests` | Grenzfall `Start == End` → leeres Intervall → sendet |
| `NotifyNewItemsAsync_OnlyOneQuietHoursBound_Sends` | `NotificationServiceTests` | Einseitig `null` → keine Ruhezeit |
| `NotifyNewItemsAsync_KeywordMatch_SkipsItem` | `NotificationServiceTests` | Keyword trifft `Title` bzw. `ContentHtml` → Item übersprungen, andere Items senden |
| `NotifyNewItemsAsync_SendsPerItem_WithItemIdAsIdentifier` | `NotificationServiceTests` | Einzelmodus (Default `false`): pro Item ein `ShowAsync`; Identifier == `Item.Id`; Titel = `Feed.Title`, Body = `Item.Title` |
| `NotifyNewItemsAsync_SummaryEnabled_SendsSingleSummary` | `NotificationServiceTests` | Sammelmodus (`NotificationSummaryEnabled = true`): genau ein `ShowAsync` für mehrere Items; Titel = `Feed.Title`, Body enthält Anzahl; Identifier aus `Feed.Id` + Item-Hash |
| `NotifyNewItemsAsync_SummaryEnabled_SameItems_SameIdentifier` | `NotificationServiceTests` | Dedup Sammelmodus: zwei Aufrufe mit identischem Item-Bestand → identischer Identifier (iOS ersetzt statt dupliziert); veränderte Menge → anderer Identifier |
| `NotifyNewItemsAsync_SummaryEnabled_KeywordFiltered_ExcludedFromSummary` | `NotificationServiceTests` | Sammelmodus: keyword-gefilterte Items fließen nicht in Anzahl/Identifier ein; alle Items gefiltert → kein `ShowAsync` |
| `SyncFeedAsync_NewItems_NotifiesWithFeedAndItems` | `FeedSyncServiceTests` | Echter `NotificationService` + `FakeLocalNotificationService` am echten Sync: neue RSS-Items → Benachrichtigungen ausgelöst (primärer Funktionsnachweis Ende-zu-Ende auf Service-Ebene) |
| `SyncFeedAsync_SummaryMode_SendsSingleSummaryNotification` | `FeedSyncServiceTests` | Derselbe Integrationspfad mit `NotificationSummaryEnabled = true` in den persistierten Settings → genau eine Sammel-Benachrichtigung für mehrere neue Items |
| `SyncFeedAsync_NoNewItems_DoesNotNotify` | `FeedSyncServiceTests` | Zweiter Sync ohne neue Items → keine Benachrichtigung (Dedup auf DB-Ebene) |
| `SyncFeedAsync_NotificationThrows_SyncStillSucceeds` | `FeedSyncServiceTests` | `FakeNotificationService` wirft → `SyncResult` bleibt `Ok`, Items/Health unverändert |
| `AddAsync_PersistsNotificationsEnabled` / `UpdateAsync_PersistsNotificationsEnabled` / `GetAllWithDetailsAsync_ProjectsNotificationsEnabled` | `FeedRepositoryTests` | Feld-Roundtrip inkl. `false`-Wert und Projektion |
| `SaveAsync_PersistsNotificationSummaryEnabled` | `SettingsRepositoryTests` | Roundtrip des neuen Felds (beide Werte) über `SaveAsync` → `GetAsync` |
| `NotificationSummaryEnabled_Change_Persists` | `SettingsViewModelTests_Persist` | Property-Änderung löst `PersistOnChange` aus und persistiert den Wert |
| `Load_PopulatesNotificationSummaryEnabled` | `SettingsViewModelTests_Load` | `LoadAsync` befüllt das Property aus persistierten Settings |
| `E2E_NotificationSummary_PersistRoundtrip` | `SettingsViewModelTests_E2E` | Schalter über ViewModel setzen → neu laden → Wert persistiert (Repo-Muster `SettingsViewModelTests_E2E`) |
| `EditCommand_PrefillsFeedNotificationsEnabled` / `SaveCommand_PersistsNotificationsEnabled_AddAndUpdate` / `SaveCommand_ResetsToggleToTrue` | `FeedsViewModelTests` | Vorbefüllung aus `FeedListItem`, Persistenz beider Pfade, Formular-Reset |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `FeedRepositoryTests` (Initializer in `AddAsync_ThenGetByIdAsync_ReturnsFeed`, `GetAllAsync_ReturnsFeedsOrderedByTitle`, `UpdateAsync_PersistsChanges`, `DeleteAsync_RemovesFeed`, `DeleteAsync_CascadeDeletesSavedItems`, `GetByIdAsync_NonExisting_ReturnsNull`) | `new Feed { … }` benötigt `NotificationsEnabled` wegen `required init` |
| `FeedSyncServiceTests` — `SeedFeedAsync`, `CreateService`, `CreateFailingService` | `Feed`-Initializer um neues Feld ergänzen; `FeedSyncService`-Konstruktor erhält `INotificationService` (→ `FakeNotificationService`) |
| `FeedsViewModelTests` — `new Feed`-Initializer (Zeile 46) | `required init`-Feld ergänzen; privates `FakeFeedSyncService` bleibt unverändert (`IFeedSyncService`-Signatur unverändert) |
| `SettingsViewModelTests_Persist` (4× `new Settings`, Zeilen 207, 247, 285, 344) | `required init`-Feld `NotificationSummaryEnabled` ergänzen |
| `SettingsViewModelTests_Load` (2× `new Settings`, Zeilen 48, 88) | `required init`-Feld ergänzen |
| `SettingsRepositoryTests` (3× `new Settings`, Zeilen 71, 96, 138) | `required init`-Feld ergänzen |
| `RetentionCleanupServiceTests` (`new Settings`, Zeile 41) | `required init`-Feld ergänzen |
| `AutoRefreshServiceTests` (2× `new Settings`, Zeilen 41, 55) | `required init`-Feld ergänzen |

`SettingsViewModelTests_Keywords`, `SettingsViewModelTests_E2E` (bestehende Methoden), `UnreadViewModelTests`, `ItemRepositoryTests`, `TestDataSeeder` und `ReporterDbContextTests_Persistence` konstruieren `Entities.Feed`/`Entities.Settings` oder arbeiten über Repositories (nicht-required, Defaults `true`/`false`) — unverändert.

### E2E-Tests (primärer Funktionsnachweis)

Es existiert keine automatisierte UI-Testsuite; „E2E" wird in diesem Repo über Integrationstests mit realen Repositories/In-Memory-SQLite (Muster `SettingsViewModelTests_E2E`) plus manueller UI-Verifikation nach `AGENTS.md` abgedeckt. Die echte iOS-Benachrichtigungsanzeige ist plattformgebunden und nur manuell verifizierbar.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Sync legt neue Artikel an → Benachrichtigung wird über die komplette Entscheidungskette ausgelöst | `FeedSyncServiceTests.SyncFeedAsync_NewItems_NotifiesWithFeedAndItems` (echter `FeedSyncService` + echter `NotificationService` + reale Repos + `FakeLocalNotificationService`) | „Nach jedem erfolgreichen Feed-Sync werden berechtigte neue Artikel benachrichtigt" | Nur die Integration über `SyncFeedAsync` → `RunSyncAsync` → `NotifyNewItemsAsync` → `ShowAsync` beweist, dass der tatsächliche Benutzerfluss (manueller und periodischer Sync laufen identisch) funktioniert |
| Pflicht | Sammelmodus: `NotificationSummaryEnabled` in den Settings aktiv → Sync mit mehreren neuen Artikeln → genau eine Sammel-Benachrichtigung | `FeedSyncServiceTests.SyncFeedAsync_SummaryMode_SendsSingleSummaryNotification` | „Der Nutzer kann wählen, ob pro Artikel oder gesammelt benachrichtigt wird" | Der Modus-Schalter wirkt erst im Verbund Settings-Persistenz → `NotificationService`-Verzweigung → `ILocalNotificationService` |
| Pflicht | Pro-Feed-Schalter: Feed bearbeiten → Schalter aus → speichern → neu laden → Wert persistiert und beim nächsten Sync wirksam | `FeedsViewModelTests` (Edit/Save-Tests oben) + Integrationstest `SyncFeedAsync_FeedDisabled_NoNotifications` | „Der Feed hat eine neue Pro-Feed-Einstellung, die benachrichtigte Artikel steuert" | Der UI-nahe Pfad `EditAsync` → `SaveAsync` → `FeedRepository` → `FeedListItem`-Projektion ist nur im Verbund prüfbar |
| Pflicht | Modus-Schalter in den Einstellungen: umschalten → persistieren → neu laden → Wert erhalten | `SettingsViewModelTests_E2E.E2E_NotificationSummary_PersistRoundtrip` | „Der Modus ist einstellbar" | ViewModel → Repository → DB → Reload ist der tatsächliche Benutzerfluss der neuen Einstellung |
| Pflicht | Ruhezeit aktiv → Sync läuft, aber keine Benachrichtigung | `NotificationServiceTests` (Theory, `FakeTimeProvider`) | „Keine Benachrichtigungen innerhalb der konfigurierten Zeit" | Zeitabhängige Unterdrückung ist ein anwendersichtbarer Kernfall |
| Pflicht | Manuelle UI-Verifikation `FeedsPage` und `SettingsPage` (390 × 844 Handysize): beide Switches sichtbar, ≥ 44 pt, Modus-Schalter deaktiviert/abgedunkelt bei globalem Schalter aus, Dark Mode, Edit-/Save- und Toggle-/Persist-Fluss; Vergleich mit `design-draft/.../feeds_health_status/screen.png`; Doku in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md` | manuell (kein UI-Testframework vorhanden) | Mobile-Design-Regeln aus `AGENTS.md` | XAML-Layout, Touch-Targets, Trigger-Verhalten und `AppThemeBinding` sind nicht unit-testbar |
| Pflicht | iOS-Verifikation: Berechtigungsdialog erscheint, Banner/Sound im Vordergrund sichtbar (Einzel- und Sammelmodus), Ruhezeit unterdrückt | manuell via `scripts/iOS-Deployment.ps1` (Aktion `simulator`, nur macOS); Ergebnis in `test-results.md` | „Lokale iOS-Benachrichtigung" als Plattformnachweis | `UNUserNotificationCenter` ist nicht unit-testbar; in dieser Windows-Umgebung nicht ausführbar → als Folgeschritt auf macOS dokumentieren |
| Optional | Berechtigung verweigert → Sync bleibt erfolgreich, keine Exception für den Nutzer | `FeedSyncServiceTests.SyncFeedAsync_NotificationThrows_SyncStillSucceeds` + manuell iOS | Fehlerrobustheit | Über UI auslösbarer Edge Case (Systemdialog „Nicht erlauben") |

Welche bestehenden E2E-Tests müssen angepasst werden?

Keine — `SettingsViewModelTests_E2E` und `FeedsViewModelTests`-Refresh-Tests bleiben unverändert (`IFeedSyncService`-Signatur und `SyncResult` ändern sich nicht; die bestehenden E2E-Methoden brechen nicht, sie werden durch eine neue Methode ergänzt).

## Offene Punkte

Keine — alle neun Punkte der Anforderung wurden durch den Anwender entschieden: Punkt 3 wird als einstellbarer Benachrichtigungsmodus (`Settings.NotificationSummaryEnabled`, Default `false` = Einzelmodus) umgesetzt; die übrigen Punkte folgen den dokumentierten Vorschlägen (s. Übersicht, Designentscheidungen und Programmabläufe).
