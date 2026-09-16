<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Demo-Feed beim ersten Start (Issue #96)

## Übersicht

Beim erstmaligen Start der Anwendung (frische SQLite-Datenbank) wird automatisch eine Kategorie „News" mit dem Feed `https://www.apple.com/newsroom/rss-feed.rss` vorbefüllt. Es gibt keine UI-Änderung: Der geseedete Feed erscheint als normale Karte in `FeedsPage` und wird vom bestehenden Start-Abruf (`AutoRefreshService.RunStartupSyncAsync`) direkt synchronisiert. Betroffen sind `MauiProgram` (First-Run-Erkennung, DI), `App.OnStart` (Einhängepunkt), ein neuer plattformneutraler Seed-Service in `Reporter.Core` sowie die E2E-Infrastruktur (Unterdrückung des Seeds im bestehenden Fixture, neuer Demo-Seed-Test).

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| First-Start-Erkennung | `!File.Exists(databasePath)` in `MauiProgram.CreateMauiApp`, ausgewertet direkt nach der Pfadauflösung (vor `builder.Build()`/`ApplyPersistedLanguage`) | `databasePath` ist nur in `MauiProgram` bekannt (`MauiProgram.cs:45-48`). Die in der Anforderung vorgeschlagene Migrationshistorie-Prüfung in `App.OnStart` ist nicht mehr möglich, weil `MauiProgram.ApplyPersistedLanguage` (`MauiProgram.cs:141`) bereits vor `OnStart` `context.Database.Migrate()` ausführt und `__EFMigrationsHistory` dann befüllt ist. `File.Exists` erfasst „leere Datenbank" exakt, benötigt keinen DbContext und seedet nach einer DB-Löschung korrekt erneut — semantisch passend, weil der Demo-Content dann ebenfalls weg ist. `VersionTracking.IsFirstLaunchEver` würde nach DB-Löschung nicht erneut seeden und ist im Core nicht verfügbar. Geklärter Scope: Nur frische DB — Bestandsinstallationen nach dem Update erhalten den Demo-Feed bewusst nicht; die `File.Exists`-Prüfung vor dem ersten `Migrate` ist die gewollte Lesart. |
| Transport des First-Run-Signals | `FirstRunState`-POCO (`IsFirstRun`, `DemoSeedSuppressed`, `ShouldSeedDemoContent`) als Singleton-Instanz registriert (`AddSingleton(new FirstRunState { ... })`) und in `DemoContentService` injiziert | Minimaler Transport von `MauiProgram` nach `App.OnStart`; der Core-Service bleibt frei von MAUI-, `FileSystem`- und DbContext-Abhängigkeiten und ist damit vollständig unit-testbar (Service-Layer-Muster). |
| Seed-Service | `IDemoContentService.EnsureSeededAsync(CancellationToken)` in `src/Reporter.Core/Interfaces/`, Implementierung `DemoContentService` in `src/Reporter.Core/Services/` | Folgt exakt dem `IRetentionCleanupService`/`RetentionCleanupService`-Muster: ein Methoden-Contract, nur Repository-Abhängigkeiten (`ICategoryRepository`, `IFeedRepository`, optional `IDebugLogService`), Singleton-Registrierung. |
| Feed-Titel | Fester Titel „Apple Newsroom" | Der `FeedTitleFallback`-Platzhalter ergäbe `"rss-feed.rss"` — bei einem ersten Start ohne Netzwerk bliebe dieser unfertig wirkende Titel sichtbar. Der feste Titel ist von Anfang an korrekt und wird von `FeedSyncService.ResolveFeedTitle` (`FeedSyncService.cs:276`) nicht überschrieben, da er kein Platzhalter ist. |
| Seed-Definitionen | Konstanten im Service (`DemoCategoryName`, `DemoFeedUrl`, `DemoFeedTitle`) | Die Anforderung verlangt genau eine Kategorie und einen Feed — keine generische Seed-Definitions-Infrastruktur. Konstanten halten die Werte an einer Stelle und erlauben spätere Erweiterung ohne API-Änderung. |
| Unterdrückung in E2E/CI | Neue Umgebungsvariable `REPORTER_DISABLE_DEMO_SEED` (truthy → kein Seed), ausgewertet in `MauiProgram`; `ReporterAppFixture` setzt sie | Expliziter als eine Kopplung an das Vorhandensein von `REPORTER_DB_PATH` (das auch Entwickler für manuelle Tests mit frischer DB setzen könnten — dort soll der Seed greifen). Folgt der bestehenden `REPORTER_*`-Konvention (`MauiProgram.cs:43-57`) und erlaubt einem dedizierten E2E-Test, den Seed gezielt zu aktivieren. |
| `NotificationsEnabled` des Demo-Feeds | `false` (nicht der Referenz-Default `true` aus `TryPersistNewFeedAsync`) | Geklärt: Beim allerersten Start soll weder ein iOS-Systemberechtigungs-Prompt (`LocalNotificationService.EnsureAuthorizedAsync`) noch eine Benachrichtigungsflut entstehen — beim ersten Sync des Demo-Feeds wären alle Artikel „neu" und bei `NotificationSummaryEnabled = false` (Default) würde eine Einzel-Benachrichtigung pro Artikel ausgelöst. Der Anwender kann den Schalter am Feed jederzeit einschalten. |
| E2E-Netzverkehr zu apple.com | `DemoSeedTests` akzeptiert echten Netzverkehr (Start-Abruf ist nicht stubbar, nur `REPORTER_FEEDSEARCH_ENDPOINT` ist gestubbt) | Geklärt: Die Assertions (DB-Zeilen, sichtbare Karte) hängen nicht vom Sync-Erfolg ab; ein Offline-/Fehlerfall ändert das Testergebnis nicht. |
| `ResolveAppPath`-Sichtbarkeit | `internal static` in `ReporterAppFixture` | Geklärt: Wiederverwendung der App-Pfad-Auflösung in `DemoSeedTests` statt ~20 Zeilen Duplikation; das E2E-Projekt enthält ohnehin nur Testcode. |
| Kategorie-Kollision | Vorhandene Kategorie „News" per `GetAllAsync` + `OrdinalIgnoreCase`-Vergleich wiederverwenden (deren `Id` übernehmen), nur bei Fehlen `AddAsync` | Der Unique-Index auf `categories.name` (`ReporterDbContext.cs:81`) würde eine `DbUpdateException` werfen; `OrdinalIgnoreCase` entspricht der Duplikatprüfung in `CategoriesViewModel.SaveAsync` (`CategoriesViewModel.cs:127-133`). Bei strenger Fresh-DB-Erkennung defensiv, aber notwendig für Idempotenz bei Wiederholungsaufruf. |

## Programmabläufe

### Erster Start mit Demo-Seed

1. `MauiProgram.CreateMauiApp` löst `databasePath` auf (inkl. `REPORTER_DB_PATH`-Override) und erfasst `isFirstRun = !File.Exists(databasePath)` — vor `builder.Build()`, also bevor `ApplyPersistedLanguage` die DB-Datei per `Migrate()` anlegt.
2. `MauiProgram` wertet `REPORTER_DISABLE_DEMO_SEED` aus und registriert `FirstRunState { IsFirstRun, DemoSeedSuppressed }` sowie `IDemoContentService`/`DemoContentService` als Singletons.
3. `builder.Build()` → `ApplyPersistedLanguage` migriert die (neu angelegte) DB wie bisher.
4. `App.OnStart`: `MigrateAsync` → `IDebugLogService.BeginSessionAsync` → Exception-Handler registrieren → **neuer fehlerisolierter Block**: `IDemoContentService` auflösen und `EnsureSeededAsync()` aufrufen; `catch` → `Debug.WriteLine` + `LogAsync(DebugLogCategory.Lifecycle, "Demo content seed failed", ex, DebugLogLevel.Error)`.
5. `DemoContentService.EnsureSeededAsync`:
   - `FirstRunState.ShouldSeedDemoContent` false → sofort zurück (Zweitstart, Bestandsinstallation, E2E).
   - `IFeedRepository.GetByUrlAsync(DemoFeedUrl)` → Feed bereits vorhanden → zurück (Idempotenz).
   - `ICategoryRepository.GetAllAsync` → Kategorie `DemoCategoryName` per `OrdinalIgnoreCase` suchen; gefunden → deren `Id` verwenden; nicht gefunden → `AddAsync(new Category { Id = Guid.NewGuid(), Name = "News" })`.
   - `IFeedRepository.AddAsync(new Feed { Id = Guid.NewGuid(), Url = DemoFeedUrl, Title = "Apple Newsroom", CategoryId = newsCategoryId, LastCheckedAt = null, HealthStatus = FeedHealth.Ok, HealthLastChange = null, NotificationsEnabled = false, FaviconUrl = null, LastErrorKind = null, LastErrorMessage = null })` — `NotificationsEnabled = false` ist festgelegt (kein Permission-Prompt, keine Benachrichtigungsflut beim allerersten Start; siehe Designentscheidungen).
   - `IDebugLogService.LogAsync(DebugLogCategory.Lifecycle, "Demo content seeded", level: DebugLogLevel.Info)`.
6. Der bestehende `IAutoRefreshService.StartAsync`-Block am Ende von `OnStart` löst bei `RefreshOnStartupEnabled` (Default `true`) und Online-Status `RunStartupSyncAsync` aus → `SyncAllAsync` synchronisiert den Demo-Feed: `ResolveFeedTitle`, Favicon-Nachzug (`FeedSyncService.cs:196`), Item-Import, Benachrichtigungs-Pfad (`NotifyNewItemsAsync`).

Beteiligte Klassen/Komponenten: `MauiProgram`, `FirstRunState`, `App`, `IDemoContentService`/`DemoContentService`, `ICategoryRepository`, `IFeedRepository`, `IDebugLogService`, `IAutoRefreshService`, `IFeedSyncService`

### Zweiter Start / Bestandsinstallation / unterdrückter Lauf

1. `FirstRunState.IsFirstRun` ist `false` (DB-Datei existierte bereits) oder `DemoSeedSuppressed` ist `true` (`REPORTER_DISABLE_DEMO_SEED` gesetzt).
2. `EnsureSeededAsync` ist ein No-op; keine DB-Schreibzugriffe, kein Log-Eintrag.

Beteiligte Klassen/Komponenten: `FirstRunState`, `DemoContentService`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `FirstRunState` (`src/Reporter.Core/Models/FirstRunState.cs`) | Datenmodellklasse (POCO) | Transportiert die in `MauiProgram` erkannte First-Run-Information in die DI: `IsFirstRun` (init), `DemoSeedSuppressed` (init), berechnete `ShouldSeedDemoContent => IsFirstRun && !DemoSeedSuppressed`. |
| `IDemoContentService` (`src/Reporter.Core/Interfaces/IDemoContentService.cs`) | Interface | Contract `Task EnsureSeededAsync(CancellationToken cancellationToken = default)` — analog `IRetentionCleanupService`. |
| `DemoContentService` (`src/Reporter.Core/Services/DemoContentService.cs`) | Klasse | Seed-Logik gemäß Ablauf oben; Konstanten `DemoCategoryName = "News"`, `DemoFeedUrl = "https://www.apple.com/newsroom/rss-feed.rss"`, `DemoFeedTitle = "Apple Newsroom"`; Abhängigkeiten `FirstRunState`, `ICategoryRepository`, `IFeedRepository`, optional `IDebugLogService`. Wirft bei Repository-Fehlern — Isolation erfolgt in `App.OnStart`. |
| `DemoContentServiceTests` (`src/Reporter.Tests/DemoContentServiceTests.cs`) | Testklasse | Unit-/Integrationstests des Seed-Service über `TestDbContextFactory` + echte Repositories. |
| `DemoSeedTests` (`src/Reporter.E2ETests/DemoSeedTests.cs`) | Testklasse | E2E-Nachweis des Seeds über eine eigene `Reporter.exe`-Instanz in der bestehenden seriellen `E2E`-Collection. |

## Änderungen an bestehenden Klassen

### `MauiProgram` (Klasse, `src/Reporter/MauiProgram.cs`)

- **Neue Logik in `CreateMauiApp`:** `isFirstRun = !File.Exists(databasePath)` direkt nach der `databasePath`-Auflösung/`Directory.CreateDirectory` (vor `builder.Build()`); Auswertung von `REPORTER_DISABLE_DEMO_SEED` über eine neue private Hilfsmethode `ResolveDemoSeedSuppressed()` (gesetzt und nicht `"0"`/`"false"` → `true`), analog `ResolveFeedSearchEndpoint`.
- **Neue DI-Registrierungen:** `.AddSingleton(new FirstRunState { IsFirstRun = isFirstRun, DemoSeedSuppressed = demoSeedSuppressed })` und `.AddSingleton<IDemoContentService, DemoContentService>()` (nach dem Muster `AddSingleton<IRetentionCleanupService, RetentionCleanupService>`, Zeile 73).
- **Geänderte Methoden:** `CreateMauiApp` — siehe oben; Reihenfolge der Env-Auswertung bleibt bei den bestehenden `REPORTER_*`-Blöcken.

### `App` (Klasse, `src/Reporter/App.xaml.cs`)

- **Geänderte Methoden:** `OnStart` — neuer fehlerisolierter `try/catch`-Block direkt nach der Registrierung der Exception-Handler (Zeile 48-49), also vor dem `IRetentionCleanupService`-Block: `IDemoContentService` aus dem Scope auflösen, `EnsureSeededAsync()` aufrufen; im `catch` `Debug.WriteLine` + `_debugLogService`/`debugLogService.LogAsync(DebugLogCategory.Lifecycle, "Demo content seed failed", ex.ToString(), DebugLogLevel.Error)` — exakt das Muster der bestehenden Blöcke. Die Platzierung stellt sicher, dass der Seed vor dem Start-Abruf (`IAutoRefreshService.StartAsync`) liegt und Fehler in die bereits begonnene Debug-Session geschrieben werden.

### `ReporterAppFixture` (Klasse, `src/Reporter.E2ETests/ReporterAppFixture.cs`)

- **Geänderte Methoden:** `InitializeAsync` — `startInfo.Environment["REPORTER_DISABLE_DEMO_SEED"] = "1"` ergänzen (neben `REPORTER_FEEDSEARCH_ENDPOINT`/`REPORTER_DB_PATH`, Zeile 58-59), damit die bestehende Suite hermetisch bleibt (keine zusätzliche Karte/Kategorie, kein externer apple.com-Abruf).
- **Geänderte Sichtbarkeit:** `ResolveAppPath` auf `internal static` heben, damit `DemoSeedTests` die App-Pfad-Auflösung wiederverwenden kann.

### `FeedDbAssertions` (Klasse, `src/Reporter.E2ETests/FeedDbAssertions.cs`)

- **Neue Methoden:** `CategoryExistsAsync(string databasePath, string name, TimeSpan? timeout = null)` — Polling-Read auf die `categories`-Tabelle nach dem Muster von `FeedExistsAsync` (gleiche Timeout-/Polling-Struktur).

## Datenbankmigrationen

Keine. Der Seed läuft über die bestehenden Repositories; `HasData` wird bewusst nicht verwendet (würde Bestandsinstallationen treffen und alle `EnsureCreated`-Test-Datenbanken vorbelegen).

## Validierungsregeln

Keine. Es gibt keine neue oder geänderte Eingabe — Kategoriename, Feed-URL und Titel sind fest kodiert.

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `REPORTER_DISABLE_DEMO_SEED` | Umgebungsvariable (`REPORTER_*`-Konvention) | nicht gesetzt → Seed aktiv | Unterdrückt den Demo-Seed in E2E-/CI-Läufen; wird von `ReporterAppFixture` gesetzt. Kein Anwender-Schalter, keine `appsettings`-Änderung. |

## Seiteneffekte und Risiken

- **Start-Abruf lädt apple.com:** Beim ersten Start löst `RunStartupSyncAsync` einen echten externen Abruf des Demo-Feeds aus (sofern online). Offline bleibt der Feed mit `LastCheckedAt = null` und Titel „Apple Newsroom" bestehen; Favicon/Titel-Prüfung erfolgt beim nächsten erfolgreichen Sync.
- **Benachrichtigungen (iOS):** Entschieden: Der Demo-Feed wird mit `NotificationsEnabled = false` geseedet. Beim ersten Sync wären sonst alle Artikel „neu" — `LocalNotificationService.EnsureAuthorizedAsync` würde beim allerersten Start eine Systemberechtigung anfordern und bei `NotificationSummaryEnabled = false` (Default) eine Einzel-Benachrichtigung pro Artikel auslösen. Auf Windows/MacCatalyst ist `LocalNotificationService.IsSupported == false` (No-op) — dort unkritisch.
- **E2E-Suite:** Ohne Unterdrückung würde der Seed in jedem E2E-Lauf greifen (frische Temp-DB): zusätzliche Feed-Karte, zusätzliche Kategorie „News" im Kategorie-Actionsheet (`FeedActionSheet_ChangeCategory_IncludingNone`) und externer Netzverkehr. `ReporterAppFixture` setzt daher `REPORTER_DISABLE_DEMO_SEED`; die bestehenden Smoke-Tests bleiben unverändert gültig.
- **DB-Löschung durch Anwender:** `File.Exists`-Erkennung seedet nach dem Löschen der DB-Datei erneut — gewollt, da der Demo-Content mit der DB verloren ging.
- **Unique-Index `categories.name`:** Bei (defensiv abgedeckter) vorhandener Kategorie „News" muss deren `Id` wiederverwendet werden; die Idempotenz-Prüfung über `GetByUrlAsync` verhindert doppelte Feeds.
- **Löschbarkeit:** Demo-Kategorie und -Feed verhalten sich wie normale Daten — der Feed ist löschbar/editierbar, die Kategorie löschbar (`DeleteBehavior.SetNull` auf `feeds.category_id`, `ReporterDbContext.cs:101`). Ein erneutes Seeden im selben DB-Bestand ist nicht vorgesehen.

## Umsetzungsreihenfolge

1. **`FirstRunState` anlegen** (`src/Reporter.Core/Models/FirstRunState.cs`)
   - Voraussetzungen: Keine.
   - Beschreibung: POCO mit `IsFirstRun`, `DemoSeedSuppressed` (init-Properties) und berechneter `ShouldSeedDemoContent`.

2. **`IDemoContentService` anlegen** (`src/Reporter.Core/Interfaces/IDemoContentService.cs`)
   - Voraussetzungen: Keine.
   - Beschreibung: Interface mit `Task EnsureSeededAsync(CancellationToken cancellationToken = default)` nach dem Muster `IRetentionCleanupService`.

3. **`DemoContentService` implementieren** (`src/Reporter.Core/Services/DemoContentService.cs`)
   - Voraussetzungen: `FirstRunState` und `IDemoContentService` (Schritte 1–2); `ICategoryRepository`, `IFeedRepository`, `FeedHealth`, `IDebugLogService`, `DebugLogCategory`, `DebugLogLevel` (alle vorhanden).
   - Beschreibung: Konstanten für Kategorie/URL/Titel; `EnsureSeededAsync` gemäß Ablauf (Gating → `GetByUrlAsync`-Idempotenz → Kategorie suchen/anlegen → Feed anlegen → Info-Log). Abhängigkeiten `FirstRunState`, `ICategoryRepository`, `IFeedRepository`, optional `IDebugLogService`.

4. **`MauiProgram` erweitern** (First-Run-Erkennung + Env-Auswertung + DI)
   - Voraussetzungen: `FirstRunState`, `IDemoContentService`/`DemoContentService` (Schritte 1–3).
   - Beschreibung: `isFirstRun = !File.Exists(databasePath)` vor `builder.Build()` erfassen; `ResolveDemoSeedSuppressed()` für `REPORTER_DISABLE_DEMO_SEED`; beide Singleton-Registrierungen in der bestehenden `AddSingleton`-Kette.

5. **`App.OnStart` erweitern** (Seed-Block)
   - Voraussetzungen: `IDemoContentService` registriert (Schritt 4).
   - Beschreibung: Fehlerisolierter `try/catch`-Block nach der Exception-Handler-Registrierung; `IDemoContentService` auflösen, `EnsureSeededAsync()` aufrufen; Fehler via `Debug.WriteLine` + `LogAsync(DebugLogCategory.Lifecycle, ..., DebugLogLevel.Error)`.

6. **E2E-Fixture absichern** (`ReporterAppFixture`)
   - Voraussetzungen: `REPORTER_DISABLE_DEMO_SEED`-Auswertung (Schritt 4).
   - Beschreibung: `REPORTER_DISABLE_DEMO_SEED=1` im `ProcessStartInfo` setzen; `ResolveAppPath` auf `internal` heben.

7. **Unit-Tests schreiben** (`DemoContentServiceTests`, `ServiceCollectionTests`-Erweiterung)
   - Voraussetzungen: `DemoContentService` (Schritt 3); `TestDbContextFactory`, `CategoryRepository`, `FeedRepository`, `FakeDebugLogService`, `TestDataSeeder` (alle vorhanden).
   - Beschreibung: Testfälle gemäß Abschnitt „Neue Tests"; `ServiceCollectionTests` um `AddReporterServices_ResolvesDemoContentService` ergänzen.

8. **E2E-Test schreiben** (`DemoSeedTests`, `FeedDbAssertions.CategoryExistsAsync`)
   - Voraussetzungen: Seed-Pfad vollständig (Schritte 1–5); `FeedDbAssertions`, `UiRetry`, `E2ETestCollection` (vorhanden); `ResolveAppPath` internal (Schritt 6).
   - Beschreibung: Eigener `Reporter.exe`-Prozess mit frischer Temp-`REPORTER_DB_PATH` ohne Disable-Flag; Assertions per `FeedExistsAsync`/`CategoryExistsAsync` und Feed-Karte im UI; Prozess danach killen, Temp-Verzeichnis löschen.

9. **Dokumentation und Verifikation**
   - Voraussetzungen: Schritte 1–8.
   - Beschreibung: `docs/help/anwendung/beschreibung.md` bzw. `datenmodell.md` um den Demo-Seed beim ersten Start ergänzen; manuelle Verifikation (frische DB → Start → Karte + Kategorie „News"); `dotnet test`, `npm test`, `.\scripts\Run-StaticChecks.ps1`; Ergebnisse in `test-results.md` festhalten.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `EnsureSeededAsync_FirstRun_CreatesNewsCategoryAndDemoFeed` | `DemoContentServiceTests` | `FirstRunState.ShouldSeedDemoContent = true` → Kategorie „News" per `GetAllAsync` vorhanden, Feed per `GetByUrlAsync` vorhanden, `CategoryId` zeigt auf die Kategorie |
| `EnsureSeededAsync_FirstRun_SeedsExpectedFeedDefaults` | `DemoContentServiceTests` | `Title = "Apple Newsroom"`, `HealthStatus = FeedHealth.Ok`, `LastCheckedAt = null`, `FaviconUrl = null`, `LastErrorKind`/`LastErrorMessage = null`, `NotificationsEnabled = false` |
| `EnsureSeededAsync_NotFirstRun_CreatesNothing` | `DemoContentServiceTests` | `IsFirstRun = false` → keine Kategorien/Feeds in der DB |
| `EnsureSeededAsync_SeedSuppressed_CreatesNothing` | `DemoContentServiceTests` | `IsFirstRun = true`, `DemoSeedSuppressed = true` → keine Daten |
| `EnsureSeededAsync_ExistingNewsCategory_ReusesCategory` | `DemoContentServiceTests` | Vorab per `ICategoryRepository.AddAsync` Kategorie „News" anlegen → Seed verwendet deren `Id`, es existiert weiterhin nur eine „News"-Kategorie |
| `EnsureSeededAsync_CalledTwice_IsIdempotent` | `DemoContentServiceTests` | Zweitaufruf legt kein Dublett an (`GetByUrlAsync`-Prüfung) |
| `EnsureSeededAsync_ExistingDemoFeed_SkipsSeed` | `DemoContentServiceTests` | Feed mit Demo-URL bereits vorhanden → keine neue Kategorie, kein zweiter Feed, keine Exception |
| `EnsureSeededAsync_FirstRun_LogsInfoEntry` | `DemoContentServiceTests` | `FakeDebugLogService` erhält `Lifecycle`-/`Info`-Eintrag „Demo content seeded" |
| `AddReporterServices_ResolvesDemoContentService` | `ServiceCollectionTests` | `IDemoContentService` ist mit registriertem `FirstRunState` + `AddTestRepositories` + `FakeDebugLogService` auflösbar (spiegelt `MauiProgram`-Registrierung) |
| `CategoryExistsAsync` | `FeedDbAssertions` (E2E-Hilfsmethode) | Polling-Read auf `categories`-Tabelle nach Namen — Analogon zu `FeedExistsAsync` |

### Betroffene bestehende Tests

Keine. `DemoContentService` ist neu, `EnsureCreated`-Test-Datenbanken bleiben unbefüllt (kein `HasData`), und die E2E-Fixture unterdrückt den Seed, sodass die bestehenden Smoke-Tests unverändert weiterlaufen.

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Erster Start mit frischer DB: `DemoSeedTests` startet eine eigene `Reporter.exe`-Instanz mit frischer `REPORTER_DB_PATH` und `REPORTER_FEEDSEARCH_ENDPOINT` (Stub), **ohne** `REPORTER_DISABLE_DEMO_SEED`; prüft per `FeedDbAssertions.FeedExistsAsync` die Feed-Zeile, per `CategoryExistsAsync` die Kategorie „News" und per FlaUI-Attach + `UiRetry` die sichtbare Feed-Karte | `src/Reporter.E2ETests/DemoSeedTests.cs` (Collection `E2E`, läuft seriell zu den Smoke-Tests) | Kategorie „News" und Feed `https://www.apple.com/newsroom/rss-feed.rss` werden beim erstmaligen Start automatisch vorbefüllt und sind für den Anwender sichtbar | Der Nachweis „App-Start auf frischer DB → Demo-Content sichtbar" ist nur im echten App-Prozess mit realem `MauiProgram`/`App.OnStart`-Pfad prüfbar; Unit-Tests decken nur den isolierten Service ab |

Bestehende E2E-Tests müssen nicht angepasst werden — `ReporterAppFixture` unterdrückt den Seed über `REPORTER_DISABLE_DEMO_SEED`, sodass `AppStarts_FeedListRenders` und die übrigen Smoke-Tests die bisherigen Ausgangszustände (keine Karten, keine Kategorien) behalten. `ReporterAppFixture` selbst erhält eine Infrastruktur-Änderung (Env-Variable + Sichtbarkeit von `ResolveAppPath` auf `internal`), keine Testlogik-Änderung.

## Offene Punkte

Keine.
