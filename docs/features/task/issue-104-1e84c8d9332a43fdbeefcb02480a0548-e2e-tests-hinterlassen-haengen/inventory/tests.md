<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests — Ausgangszustand und bestehende E2E-Testinfrastruktur

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-09-18, ca. 07:51–07:59 MESZ (UTC+02:00)
- **Branch und Commit-ID:** `task/issue-104-1e84c8d9332a43fdbeefcb02480a0548-e2e-tests-hinterlassen-haengen` @ `92b115e3301772fe850569bbebb700b5b35ce3e5`
- **Uncommittete Änderungen im getesteten Stand:** ausschließlich das untracked Feature-Verzeichnis `docs/features/task/issue-104-1e84c8d9332a43fdbeefcb02480a0548-e2e-tests-hinterlassen-haengen/` (Anforderungs-/Lifecycle-Dateien, davon entstanden während der Bestandsaufnahme: diese `inventory/`-Dateien). Keine Änderungen an `src/`, `scripts/` oder Testkonfiguration.
- **Testumgebung und Runtime-/SDK-Versionen:** Microsoft Windows 11 Pro 10.0.26200, AMD64; .NET SDK 10.0.401; Node.js v24.15.0; npm 11.10.0; installierte Workloads: `maui-windows` 10.0.20/10.0.100, `ios` 26.5.10318/10.0.100, `android` 36.1.69/10.0.100, `maccatalyst` 26.5.10318/10.0.100; interaktive Desktop-Session.
- **Vor-Zustand der Prozess-/Temp-Landschaft:** vor allen Läufen 0 laufende `Reporter`-Prozesse (`Get-Process -Name Reporter` leer); **37 verwaiste `%TEMP%/reporter-e2e-*`-Verzeichnisse** aus früheren Läufen (16.–18.09.2026, davon 18 `reporter-e2e-demo-*`) — direktes Indiz für das beschriebene Leck bzw. gescheiterte Teardowns auf dieser Maschine.
- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - `CONTRIBUTING.md:22–23`: `dotnet test Reporter.sln --filter "Category!=E2E"`, `npm test`, E2E via `.\scripts\Run-E2ETests.ps1` (nur Windows, interaktive Session).
  - `package.json` → `"test": "node --test \"scripts/*.test.mjs\""`.
  - CI `.github/workflows/staging-ci.yml` (Job `build-and-test`, Z. 120–126): `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage"`; E2E-Tests laufen **nicht** in der CI.
  - `scripts/Run-StaticChecks.ps1` bündelt Format-/Lizenzheader-/Security-/Static-Analysis-Checks (CI-Job `static-checks`) — lokale Qualitätsprüfung, keine Testsuite; im Rahmen der Bestandsaufnahme nicht ausgeführt.
  - `Reporter.E2ETests`: alle 9 Tests tragen `[Trait("Category", "E2E")]` und werden vom `Category!=E2E`-Filter vollständig ausgeschlossen.

## Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 — Unit-Tests (Solution, ohne E2E) | `IncludeIosTarget=false IncludeAndroidTarget=false dotnet test Reporter.sln --filter "Category!=E2E" --logger "console;verbosity=normal"` | Repo-Root | 0 | 566 | 0 | 0 (E2E-Projekt: „Kein Test entspricht dem Filter") | [Log](test-results/dotnet-test-sln-category-ne-e2e.log) |
| 2 — Node-Tests | `npm test` | Repo-Root | 0 | 36 | 0 | 0 | [Log](test-results/npm-test.log) |
| 3 — E2E-Einzeltest | `REPORTER_APP_PATH=…\Reporter.exe IncludeIosTarget=false IncludeAndroidTarget=false dotnet test src/Reporter.E2ETests/Reporter.E2ETests.csproj -c Debug -f net10.0-windows10.0.19041.0 --filter "FullyQualifiedName~Reporter.E2ETests.SmokeTests.AppStarts_FeedListRenders"` | Repo-Root | 1 | 0 | 1 | 0 | [Log](test-results/e2e-smoke-single-test.log) |
| 4 — E2E-Einzeltest (Wiederholung) | identisch zu Lauf 3 | Repo-Root | 1 | 0 | 1 | 0 | [Log](test-results/e2e-smoke-single-test-retry.log) |
| 5 — E2E-Abbruch-Reproduktion | `REPORTER_APP_PATH=…\Reporter.exe IncludeIosTarget=false IncludeAndroidTarget=false dotnet test src/Reporter.E2ETests/Reporter.E2ETests.csproj -c Debug -f net10.0-windows10.0.19041.0 --filter "FullyQualifiedName~Reporter.E2ETests.SmokeTests"`; `testhost.exe` (PID 25992) während des Laufs hart beendet | Repo-Root | 1 („Der aktive Testlauf wurde abgebrochen. Grund: Der Testhostprozess ist abgestürzt.") | 6 | unbekannt (Lauf abgebrochen, „Gesamtzahl Tests: unbekannt") | unbekannt | [Log](test-results/e2e-abort-testhost-kill.log) |

Hinweise zu den Läufen:

- Lauf 1 nutzt dieselbe Aufrufkette wie `CONTRIBUTING.md`; die Env-Variablen `IncludeIosTarget`/`IncludeAndroidTarget` spiegeln die CI-Einstellung (`staging-ci.yml` setzt beide auf `false`), weil `Reporter.csproj` `IncludeIosTarget` unter Windows sonst auf `true` defaultet.
- Läufe 3–5 ersetzen den vollständigen `Run-E2ETests.ps1`-Durchlauf (UI-Automatisierung, lange Laufzeit): identischer Mechanismus (`REPORTER_APP_PATH` auf den bestehenden Debug-`win-x64`-Build, `dotnet test` auf das E2E-Projekt), aber auf kontrollierbare Teilmengen eingegrenzt. Die App wurde zuvor im Rahmen von Lauf 1 (Solution-Build) erzeugt.
- Nach Läufen 3 und 4 war `Get-Process -Name Reporter` **leer** — `DisposeAsync` → `App.Kill()` hat den Prozess beendet (Watcher: `[E2E] Reporter.exe exited … with code -1`), das Temp-Verzeichnis wurde gelöscht.
- Nach Lauf 5 lief `Reporter.exe` (**PID 38080**, gestartet 07:57:00 aus `src\Reporter\bin\Debug\…\win-x64\Reporter.exe`) **weiter**, nachdem der Test-Host hart beendet worden war — **Prozess-Leck im Abbruchpfad reproduziert**. Zusätzlich blieb das Temp-Verzeichnis `reporter-e2e-3eedbc27d48a43e9af5dbdde1fde3f57` (erzeugt 07:57:11) zurück. Der verwaiste Prozess wurde nach Beweissicherung manuell beendet (`Stop-Process -Id 38080 -Force`).

## Nachgewiesene bestehende Testfehler

| Test-ID inkl. Testfall | Suite / Dateipfad | Fehlerbild / Fehlermeldung | Lauf und Nachweis |
|-----------------------|-------------------|----------------------------|-------------------|
| `Reporter.E2ETests.SmokeTests.AppStarts_FeedListRenders` | `src/Reporter.E2ETests/SmokeTests.cs` (Z. 161–169) | `Assert.True` schlägt nach 20-s-Poll: „Neither feed cards nor the empty placeholder were rendered." — 2× reproduziert, **jeweils als isolierter Einzellauf direkt nach App-Start**. Derselbe Test bestand in Lauf 5 (Gruppenlauf, als späterer Test) in 25 ms — der Befund deutet auf ein Warm-up-/Timing-Problem des allerersten Tests nach App-Start hin, nicht auf einen fachlichen Defekt; die Ursache ist im Rahmen der Bestandsaufnahme nicht weiter untersucht. | Läufe 3+4: [Log 1](test-results/e2e-smoke-single-test.log), [Log 2](test-results/e2e-smoke-single-test-retry.log) |

Keine Fehlschläge in Lauf 1 (566/566) und Lauf 2 (36/36). Zu nicht ausgeführten Tests wird keine Aussage getroffen (siehe Testlücken).

## Testlücken und Ausführungsprobleme

- **`SmokeTests.AddButton_OpensSheet_FocusesUrlEntry`** — im Abbruch-Lauf 5 zum Kill-Zeitpunkt noch nicht abgeschlossen; Ergebnis unbekannt (Testlauf „abgebrochen", Gesamtzahl unbekannt).
- **`ArticleLinkTests.ExternalLinkInArticle_OpensSystemBrowser`** — nicht ausgeführt: erfordert WebView2-/Browser-Interaktion und längere Laufzeit; für die kontrollierte Reproduktion nicht benötigt.
- **`DemoSeedTests.FirstStart_SeedsNewsCategoryAndDemoFeed`** — nicht ausgeführt: startet eine zweite App-Instanz mit realem Demo-Seed, der Startup-Sync kann apple.com tatsächlich aufrufen; Laufzeit nicht vertretbar eingrenzbar.
- **Vollständiger `scripts/Run-E2ETests.ps1`-Durchlauf (alle 9 E2E-Tests)** — nicht ausgeführt (UI-Automatisierung über mehrere Minuten, Fokus-/Eingabe-Übernahme); stattdessen die dokumentierten Teilläufe 3–5 mit identischem Aufrufmechanismus.
- **`scripts/Run-StaticChecks.ps1`** — nicht ausgeführt: statische Prüfung (Format/Security/Static Analysis), keine Testsuite; relevant erst vor Abschluss der Umsetzung.
- **Beobachtetes, nicht aufgeklärtes Phänomen:** `AppStarts_FeedListRenders` schlägt als allererster Test nach App-Start reproduzierbar fehl (Läufe 3+4), besteht aber im Gruppenlauf (Lauf 5). Für spätere Vergleiche gilt: Der Fehlschlag ist **unter der Bedingung „isolierter Einzellauf" nachgewiesen**, nicht als generell bestehender Fehler der vollständigen Suite.

## Prozess-Leck — Reproduktionsnachweis

| Szenario | Beobachtung | Nachweis |
|----------|-------------|----------|
| Normaler Testlauf-Ende (auch mit Test-Fehlschlag): `DisposeAsync` läuft | `Reporter.exe` wird via `App.Kill()` beendet (Exit-Code −1 laut Fixture-Watcher); `Get-Process Reporter` leer; Temp-Dir gelöscht | Läufe 3+4, Konsolenzeile `[E2E] Reporter.exe exited … with code -1` |
| Harter Abbruch des Test-Hosts (`testhost.exe`-Kill) | `Reporter.exe` (PID 38080) läuft nach dem Kill weiter — **Leck reproduziert**; Temp-Dir `reporter-e2e-3eedbc27…` verwaist | Lauf 5, Prozess-Check im Sitzungsprotokoll |
| Historische Läufe auf dieser Maschine | 37 verwaiste `%TEMP%/reporter-e2e-*`-Verzeichnisse (16.–18.09.2026) — konsistent mit Teardowns, die den Prozess nicht erreichten bzw. an Dateilocks scheiterten | Temp-Verzeichnis-Listing im Sitzungsprotokoll |

Nicht reproduziert bzw. nicht geprüft: ein Leck **nach regulärem, erfolgreichem** Suite-Ende (die beiden Läufe 3+4 endeten sauber; ob `App.Kill()` in anderen Konstellationen ohne `WaitForExit` entkommen kann, ist aus dem Ist-Code nicht ausgeschlossen, aber unbelegt).

## Testklassen

### `SmokeTests` (`src/Reporter.E2ETests/SmokeTests.cs`)

- `AppStarts_FeedListRenders` — App startet auf Unread; Feeds-Tab rendert entweder Feed-Cards (`ByHelpText AccessibilityTapForActions`) oder den Empty-Placeholder (`ByName PlaceholderFeeds`).
- `AddButton_OpensSheet_FocusesUrlEntry` — „+" öffnet das Add-Sheet, URL-Entry erhält Tastaturfokus.
- `DirectAdd_FeedAppearsInListAndDatabase` — Direct-Add persistiert: Card sichtbar + Feed-Zeile in der isolierten DB.
- `FeedActionSheet_Rename_UpdatesTitle` — Action-Sheet „Umbenennen" aktualisiert den Card-Titel.
- `FeedActionSheet_ChangeCategory_IncludingNone` — Kategorie zuweisen (Card zeigt Namen) und über „Keine" wieder entfernen.
- `Search_SubscribesResult_PersistsFeed` — Suche gegen Stub-`/directory`, Treffer antippen, Abo-Alert bestätigen, Feed-Zeile in DB.
- `Search_SiteUrl_DiscoversFeedViaLinkTag` — unbekannte Site-URL → HTML-Autodiscovery via `<link rel="alternate">` auf der Stub-Site.

Private Helfer in der Klasse: `Window`-Re-Resolve, `WaitForElement*`-Wrapper, `EnterUrl`, `AddFeedViaUi`, `WaitForCard`, `OpenFeedActions`, `WaitForFocus`, `ResetUiState` (Popup-/Dialog-Reset zwischen Tests), `AddCategoryViaUi`, `CardShowsCategory`, `ChangeFeedCategoryViaActionSheet`.

### `ArticleLinkTests` (`src/Reporter.E2ETests/ArticleLinkTests.cs`)

- `ExternalLinkInArticle_OpensSystemBrowser` — externer Link in der Artikel-WebView wird abgebrochen und an den Systembrowser gereicht (Nachweis über `StubFeedServer.ExternalLinkHitCount`); Detailansicht bleibt offen.

### `DemoSeedTests` (`src/Reporter.E2ETests/DemoSeedTests.cs`)

- `FirstStart_SeedsNewsCategoryAndDemoFeed` — Erststart gegen frische DB seedet Kategorie „News" + Apple-Newsroom-Demofeed; Feed-Card auf Feeds-Tab sichtbar. Startet eine **eigene zweite `Reporter.exe`** mit eigenem `finally`-Cleanup (siehe `logic.md`).

## Hilfsmethoden

### `ReporterAppFixture`

- `InitializeAsync` / `DisposeAsync` — Lebenszyklus von Stub-Server, App-Prozess, UIA-Attach, Temp-DB (Details und Lücken in `logic.md`).
- `GetMainWindow` — Hauptfenster-Re-Resolve für Tests.
- `ResolveAppPath` — `REPORTER_APP_PATH`-Override bzw. Konventionspfad-Suche.

### `StubFeedServer`

- `InitializeAsync` / `DisposeAsync` — Kestrel-Stub auf freiem Loopback-Port; `BaseUrl`, `DirectoryUrl`, `ExternalLinkHitCount`.

### `UiRetry`

- `WaitForElement` / `TryFindElement` / `WaitForElementByName` / `TryFindElementByName` / `WaitForElementInScope` / `TryFindElementInScope` — UIA-Polling (20 s, 200 ms).
- `SelectTab` — Shell-Tab-Auswahl inkl. NavigationView-Overflow-Fallback.
- `WaitForCard` — Card-Suche (Group-first, Fallback).
- `WaitFor` — generisches Bedingungs-Polling.
- `InvokeOrClick` — Invoke → SelectionItem → Maus-Klick.
- `SetText` — Value-Pattern → Tastatur-Fallback.

### `E2EPageHelpers`

- `SelectTab` — Tab-Auswahl mit Seiten-Anker-Wait.
- `OpenAddSheet` / `WaitForUrlEntry` — Add-Sheet-Flow.

### `FeedDbAssertions`

- `FeedExistsAsync` / `CategoryExistsAsync` / `ItemExistsAsync` — read-only SQLite-Polling gegen die isolierte `reporter.db` (15-s-Default).
