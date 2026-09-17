<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Tests — Technischer Ablauf

## Übersicht

Die E2E-Suite läuft in drei Phasen: `scripts/Run-E2ETests.ps1` übersetzt die App und startet `dotnet test`; das xunit-Collection-Fixture `ReporterAppFixture` fährt den Kestrel-Stubserver `StubFeedServer` und die zuvor gebaute `Reporter.exe` mit den Umgebungsvariablen `REPORTER_FEEDSEARCH_ENDPOINT`, `REPORTER_DB_PATH` und `REPORTER_DISABLE_DEMO_SEED` hoch und attacht per FlaUI (UIA3) aufs Hauptfenster; die sieben `SmokeTests` steuern diese App-Instanz seriell über UIA und verifizieren UI-Zustände sowie die isolierte SQLite-Datenbank, während `DemoSeedTests` eine eigene `Reporter.exe`-Instanz mit frischer Temp-DB und bewusst ohne `REPORTER_DISABLE_DEMO_SEED` startet, um den Demo-Seed des allerersten Starts nachzuweisen. Am Ende killt das Fixture den App-Prozess, stoppt den Server und löscht das Temp-Verzeichnis.

## Ablauf

### 1. App-Build und Suite-Start (`scripts/Run-E2ETests.ps1`)

Das Skript setzt `IncludeIosTarget=false`, baut `src/Reporter/Reporter.csproj` in der Konfiguration `Debug` für `net10.0-windows10.0.19041.0`/`win-x64`, prüft, dass `Reporter.exe` im Output liegt, setzt `REPORTER_APP_PATH` auf diese Datei und führt `dotnet test src/Reporter.E2ETests/Reporter.E2ETests.csproj -c Debug -f net10.0-windows10.0.19041.0` aus. Der Exit-Code des Testlaufs wird weitergereicht.

Beteiligte Komponenten:
- `scripts/Run-E2ETests.ps1` — Build, `REPORTER_APP_PATH`, Testaufruf

### 2. Stubserver-Start (`StubFeedServer.InitializeAsync`)

`WebApplication.CreateSlimBuilder` (Framework-Referenz `Microsoft.AspNetCore.App`) startet Kestrel auf `http://127.0.0.1:0`; nach `StartAsync` wird der vergebene Port aus `IServerAddressesFeature` gelesen und als `BaseUrl` bereitgestellt (`DirectoryUrl` = `BaseUrl + "/directory"`). Die Routen liefern die Fixture-Dateien aus `Fixtures/` (als `Content` mit `CopyToOutputDirectory` im Output) bzw. dynamisch erzeugte Directory-JSON-Antworten; alle übrigen Pfade beantwortet `MapFallback` mit 404.

Beteiligte Komponenten:
- `StubFeedServer.InitializeAsync` / `DisposeAsync` — Lebenszyklus des Servers
- `StubFeedServer.BaseUrl` / `DirectoryUrl` — Basis- und Directory-Endpunkt des Stubs
- `Fixtures/stub-feed.xml`, `site-feed.xml`, `site.html`, `empty.html` — statische Antwortkörper

### 3. App-Start mit E2E-Overrides (`ReporterAppFixture.InitializeAsync`)

Das Fixture legt ein Temp-Verzeichnis an (`%TEMP%/reporter-e2e-{guid}`), setzt `DatabasePath` auf `{tempDir}/reporter.db`, löst den Exe-Pfad über `REPORTER_APP_PATH` bzw. den Konventionspfad `src/Reporter/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Reporter.exe` auf (Vorfahren-Suche ab dem Test-Output) und startet den Prozess mit `UseShellExecute = false` und den drei Env-Overrides — `REPORTER_DISABLE_DEMO_SEED=1` hält den Demo-Seed fern, der wegen der frischen Temp-DB sonst in jedem Lauf greifen würde (zusätzliche „News"-Kategorie und Feed-Karte plus echter apple.com-Abruf des Start-Syncs). FlaUI `Application.Attach` bindet den Prozess an eine `UIA3Automation`; ein Hintergrund-Task protokolliert einen unerwarteten Prozess-Exit (`[E2E] Reporter.exe exited …`). `GetMainWindow` wartet bis zu zwei Minuten aufs Hauptfenster (erster Start inkl. Migration), danach folgt eine feste 3-Sekunden-Bedienpause und `SetForeground()`.

App-seitig wirken die Overrides in `MauiProgram.CreateMauiApp`: `REPORTER_DB_PATH` ersetzt vor `Directory.CreateDirectory` den Standardpfad `FileSystem.AppDataDirectory/reporter.db`; `REPORTER_FEEDSEARCH_ENDPOINT` wird über `ResolveFeedSearchEndpoint` validiert (nur absolute `http`/`https`-URIs, sonst `null`) und per Factory-Lambda an den `FeedSearchService`-Konstruktor übergeben — `ApplyPersistedLanguage`/`Migrate` laufen dadurch automatisch gegen die isolierte Test-DB. `REPORTER_DISABLE_DEMO_SEED` wird über `ResolveDemoSeedSuppressed` ausgewertet (gesetzt und nicht `"0"`/`"false"` → unterdrückt) und fließt als `FirstRunState.DemoSeedSuppressed` in die DI — `IDemoContentService.EnsureSeededAsync` wird in `App.OnStart` damit zum No-op.

Beteiligte Komponenten:
- `ReporterAppFixture.InitializeAsync` / `ResolveAppPath` / `GetMainWindow` — Prozess-Start, Exe-Auflösung, Fenster-Attach
- `MauiProgram.CreateMauiApp` / `ResolveFeedSearchEndpoint` / `ResolveDemoSeedSuppressed` — Env-Override-Auswertung und DI-Registrierung
- `FeedSearchService(HttpClient, string?)` — übernimmt den Directory-Endpoint (`_directoryEndpoint`), `SearchDirectoryAsync` baut die Request-URI daraus

### 4. Testausführung (`SmokeTests` und `DemoSeedTests` in `E2ETestCollection`)

Alle acht Tests liegen in der einen xunit-`E2ETestCollection` und laufen daher seriell; es gibt keinen `ITestCaseOrderer`. Die `SmokeTests` teilen sich den Fixture-App-Prozess; `DemoSeedTests` startet für den Seed-Nachweis eine eigene `Reporter.exe`-Instanz. Elemente werden über den UIA-Namen gefunden — MAUI propagiert `SemanticProperties.Description` als UIA-`Name` — oder über AutomationIds; lokalisierte Beschriftungen lösen die Tests über `AppResources.*` auf. `UiRetry` kapselt das Polling (Standard-Timeout 20 s, Intervall 200 ms), `InvokeOrClick` nutzt Invoke- → SelectionItem-Pattern → echten Mausklick, `SetText` das Value-Pattern bzw. Tastatureingabe; die Tab-Auswahl (`SelectTab`, inkl. `TopNavOverflowButton`-Overflow) und die Karten-Suche (`WaitForCard`, Group-first mit Fallback) sind geteilte `UiRetry`-Helfer. Dialoge (`DisplayActionSheetAsync`, `DisplayPromptAsync`, `DisplayAlertAsync`) werden über `WaitForElementInScope` gesucht, das pro Poll das Hauptfenster neu auflöst und alle Top-Level-Elemente des Prozesses am Desktop durchsucht (Popup-Roots). Da die App auf dem Unread-Tab startet, aktiviert jeder Feeds-Test den Feeds-Tab als Arrange-Schritt über `SelectTab(AppResources.TabFeeds)` — liegen Tab-Einträge im Overflow (`TopNavOverflowButton`), wird das Flyout geöffnet. `ResetUiState` räumt am Testende Popups, Dialoge und das Add-Sheet auf.

Die acht Tests im Einzelnen:

- `AppStarts_FeedListRenders` — Feeds-Tab aktivieren; zustandsagnostisch auf Feed-Karten (`AccessibilityTapForActions`-HelpText) **oder** den `EmptyView`-Platzhalter (`PlaceholderFeeds`) warten.
- `AddButton_OpensSheet_FocusesUrlEntry` — „+"-Button (`ActionAddFeed`) → Add-Sheet; `WaitForUrlEntry` findet das Adressfeld per `PlaceholderFeedSearch`-Name bzw. `ControlType.Edit`; `WaitForFocus` prüft den Tastaturfokus.
- `DirectAdd_FeedAppearsInListAndDatabase` — `AddFeedViaUi("direct-add")` (Sheet öffnen, `{BaseUrl}/feeds/direct-add.xml` eintippen, `ButtonDirectAdd`) → Karte sichtbar; `FeedDbAssertions.FeedExistsAsync` pollt die `feeds`-Tabelle (bis 15 s, `Mode=ReadOnly`, rein lesend).
- `FeedActionSheet_Rename_UpdatesTitle` — eigener Feed via `AddFeedViaUi("rename-target")`; Karten-Tap → `DisplayActionSheetAsync`; `ButtonRename`-Eintrag → `DisplayPromptAsync`; neuen Titel ins Edit-Feld tippen, `ButtonOk` → Karte zeigt den neuen Titel.
- `FeedActionSheet_ChangeCategory_IncludingNone` — Kategorie per UI auf dem Kategorien-Tab anlegen (`LabelCategoryName`-Entry + `ButtonSave`), Feed via `AddFeedViaUi("category-target")`; über `ButtonChangeCategory` die Kategorie zuweisen (Name erscheint auf der Karte), danach `CategoryNone` wählen (Zuordnung verschwindet).
- `Search_SubscribesResult_PersistsFeed` — `NewUrlEntry` = `{BaseUrl}` → `ButtonSearch` → Directory-Treffer „Stub Search Hit" (`/feeds/search-hit.xml`); Karten-Tap → `DisplayAlertAsync`-Bestätigung `ButtonYes` → Karte in der Liste und `feeds`-Zeile in der Test-DB.
- `Search_SiteUrl_DiscoversFeedViaLinkTag` — `NewUrlEntry` = `{BaseUrl}/site` → `ButtonSearch` → Directory liefert `[]` → Autodiscovery lädt `/site`, wertet das `<link rel="alternate">` im `<head>` aus → Ergebniskarte „Stub Site Feed" sichtbar.
- `DemoSeedTests.FirstStart_SeedsNewsCategoryAndDemoFeed` — Startet eine eigene `Reporter.exe` mit frischer Temp-DB (`REPORTER_DB_PATH` auf `%TEMP%/reporter-e2e-demo-{guid}/reporter.db`), `REPORTER_FEEDSEARCH_ENDPOINT` auf den Stub und `REPORTER_DISABLE_DEMO_SEED` gezielt aus dem Prozess-Environment entfernt. `FeedDbAssertions.FeedExistsAsync`/`CategoryExistsAsync` pollen die `feeds`-/`categories`-Zeilen des Seeds; danach attacht FlaUI, aktiviert `UiRetry.SelectTab` den Feeds-Tab und `UiRetry.WaitForCard` findet die Karte „Apple Newsroom". Die Assertions hängen nicht am Sync-Erfolg — der Start-Abruf darf apple.com real erreichen oder offline scheitern. Prozess-Kill und Temp-Verzeichnis-Löschung laufen im `finally`.

Beteiligte Komponenten:
- `E2ETestCollection` (`CollectionDefinition("E2E")`, `ICollectionFixture<ReporterAppFixture>`) — geteiltes Fixture, serielle Ausführung
- `UiRetry` (`WaitForElement*`, `TryFindElement*`, `WaitFor`, `InvokeOrClick`, `SetText`, `SelectTab`, `WaitForCard`) — Polling und Interaktion
- `FeedDbAssertions.FeedExistsAsync` / `CategoryExistsAsync` — lesende SQLite-Verifikation (`PollUntilExistsAsync` + parametrisierbare `ExistsCoreAsync`-COUNT-Abfrage)
- `SmokeTests` (`OpenAddSheet`, `EnterUrl`, `AddFeedViaUi`, `ResetUiState` u. a.) — Testflüsse und private Helfer
- `DemoSeedTests` — Seed-Nachweis über eine eigene App-Instanz; nutzt `ReporterAppFixture.ResolveAppPath` (jetzt `internal`) und `fixture.Server.DirectoryUrl`

### 5. Teardown (`ReporterAppFixture.DisposeAsync`)

Der App-Prozess wird best effort gekillt (`App.Kill()`, `App.Dispose()`), `UIA3Automation` disposed, `StubFeedServer` gestoppt und das Temp-Verzeichnis rekursiv gelöscht — Fehler beim Löschen sind toleriert.

## Diagramm

```mermaid
flowchart TD
    A[Run-E2ETests.ps1] --> B[dotnet build Reporter.csproj]
    B --> C[dotnet test Reporter.E2ETests]
    C --> D[StubFeedServer: Kestrel auf 127.0.0.1:0]
    D --> E[ReporterAppFixture: Reporter.exe mit Env-Overrides starten]
    E --> F{Hauptfenster innerhalb 2 min?}
    F -- Ja --> G[UIA3-Attach + 3 s Pause + SetForeground]
    F -- Nein --> H[Fixture bricht mit Fehlermeldung ab]
    G --> I[SmokeTests + DemoSeedTests: 8 serielle UIA-Tests]
    I --> J[FeedDbAssertions: lesende SQLite-Prüfung]
    I --> K[Teardown: Prozess killen, Server stoppen, Temp-DB löschen]
    J --> K
```

## Fehlerbehandlung

- **Fehlende Exe:** `ResolveAppPath` wirft `FileNotFoundException` mit dem Hinweis, die App zuerst zu bauen bzw. `REPORTER_APP_PATH` zu setzen.
- **Kein Hauptfenster:** Nach zwei Minuten wirft das Fixture `InvalidOperationException` — typische Ursache ist eine fehlende interaktive Desktop-Session.
- **Ungültiger `REPORTER_FEEDSEARCH_ENDPOINT`:** `ResolveFeedSearchEndpoint` ignoriert den Wert still und fällt auf den Standard-Endpunkt zurück (kein App-Fehler).
- **Ungültiger `REPORTER_DB_PATH`:** Nur `IsNullOrWhiteSpace` wird geprüft — ein ungültiger Pfad führt bewusst zu einer sichtbaren Ausnahme beim App-Start.
- **Transiente UIA-Ausfälle:** Alle `UiRetry`-Find-Methoden schlucken Exceptions während des Pollings — der UIA-Baum kann beim Rendern oder bei Dialogwechseln kurzzeitig nicht erreichbar sein.
- **Unerwarteter App-Exit:** Ein Watcher-Task schreibt `[E2E] Reporter.exe exited at … with code N` auf die Konsole; `GetMainWindow` wirft bei beendetem Prozess eine `InvalidOperationException`.
- **Attach-Fehler vor `App`-Zuweisung:** Der Prozess wird best effort gekillt, damit keine verwaiste `Reporter.exe` zurückbleibt.
