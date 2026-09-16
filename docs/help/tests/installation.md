<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Tests — Installation und Konfiguration

## Voraussetzungen

- **Windows** mit installiertem .NET SDK inkl. **.NET-MAUI-Workload** (für den App-Build).
- **Interaktive Desktop-Session:** Die Suite steuert die echte App über UIA3 — ein sichtbares Fenster muss möglich sein (kein Headless-/SSH-/Build-Server-Betrieb).
- Ausführung aus dem **Repository-Root** (`Run-E2ETests.ps1` prüft auf `Reporter.sln`).
- Während des Laufs keine parallele Bedienung derselben Desktop-Session — die Automation übernimmt Fokus und Maus/Tastatur.

## Installationsschritte

Kein separates Setup nötig — das Skript erledigt Build und Ausführung:

```powershell
.\scripts\Run-E2ETests.ps1
```

Das Skript:

1. setzt `IncludeIosTarget=false`,
2. baut `src/Reporter/Reporter.csproj` (`Debug`, `net10.0-windows10.0.19041.0`, `win-x64`),
3. prüft, dass `src\Reporter\bin\Debug\net10.0-windows10.0.19041.0\win-x64\Reporter.exe` erzeugt wurde,
4. setzt `REPORTER_APP_PATH` auf diese Exe,
5. führt `dotnet test src/Reporter.E2ETests/Reporter.E2ETests.csproj -c Debug -f net10.0-windows10.0.19041.0` aus und reicht den Exit-Code durch.

Alternativ kann das Testprojekt direkt getestet werden, wenn die App bereits gebaut ist (`REPORTER_APP_PATH` oder Konventionspfad, siehe unten).

## Konfiguration

Die Suite selbst hat keine Konfigurationsdateien; das Testprojekt ist in `Reporter.sln` aufgenommen und alle E2E-Tests tragen `[Trait("Category", "E2E")]`. Für lokale Solution-Läufe gilt deshalb:

```powershell
dotnet test Reporter.sln --filter "Category!=E2E"   # ohne E2E-Suite
dotnet test src/Reporter.E2ETests/Reporter.E2ETests.csproj -f net10.0-windows10.0.19041.0   # nur E2E
```

| Einstellung | Standardwert | Beschreibung |
|-------------|--------------|--------------|
| Build-Konvention | `Debug` / `net10.0-windows10.0.19041.0` / `win-x64` | Feste Konvention in `Run-E2ETests.ps1` und `ResolveAppPath` |
| Test-Filter | `[Trait("Category", "E2E")]` auf allen Tests | Trennung vom regulären `dotnet test`-Lauf |
| Stub-Port | `0` (OS vergibt freien Port) | `StubFeedServer` bindet `http://127.0.0.1:0`, `BaseUrl` nach `StartAsync` ausgelesen |

## Umgebungsvariablen

| Variable | Pflicht | Beispielwert | Beschreibung |
|----------|---------|--------------|--------------|
| `REPORTER_APP_PATH` | Nein | `D:\...\Reporter.exe` | Pfad zur zu testenden Exe; wenn leer/Datei fehlt, sucht `ReporterAppFixture.ResolveAppPath` den Debug-Output-Konventionspfad vom Test-Output aufwärts. `Run-E2ETests.ps1` setzt die Variable selbst. |
| `REPORTER_FEEDSEARCH_ENDPOINT` | Nein | `http://127.0.0.1:5123/directory` | App-seitiger Override des Feed-Verzeichnis-Endpunkts; nur absolute `http`/`https`-URIs wirken, sonst greift der Standard `https://feedsearch.dev/api/v1/search`. Wird vom Fixture automatisch auf den Stub gesetzt — **nicht dauerhaft auf dem Entwicklungsrechner setzen.** |
| `REPORTER_DB_PATH` | Nein | `C:\Temp\e2e\reporter.db` | App-seitiger Override des SQLite-Dateipfads (Default `FileSystem.AppDataDirectory/reporter.db`); das Verzeichnis wird angelegt, ein ungültiger Pfad führt zu einer Ausnahme beim App-Start. Wird vom Fixture automatisch auf ein Temp-Verzeichnis gesetzt — **nicht dauerhaft setzen.** |
| `REPORTER_DISABLE_DEMO_SEED` | Nein | `1` | App-seitiger Schalter: jeder gesetzte Wert außer `0`/`false` (Groß-/Kleinschreibung egal) unterdrückt den Demo-Seed, den die App beim allerersten Start auf einer frischen Datenbank anlegt. Wird vom Fixture automatisch gesetzt, damit die Smoke-Tests hermetisch bleiben — `DemoSeedTests` entfernt ihn gezielt für den Seed-Nachweis. **Nicht dauerhaft setzen.** |

> **Hinweis:** `REPORTER_FEEDSEARCH_ENDPOINT`, `REPORTER_DB_PATH` und `REPORTER_DISABLE_DEMO_SEED` wirken in jedem Build (auch Release) — sie sind dokumentierte Test-/Dev-Overrides auf Prozess-Start-Ebene, kein Anwender-Setting.

## Überprüfung

- `.\scripts\Run-E2ETests.ps1` endet mit Exit-Code `0` und acht grünen Tests.
- Beim Lauf öffnet sich das App-Fenster sichtbar; die Suite steuert es automatisch und schließt es am Ende.
- Die App-Datenbank unter `FileSystem.AppDataDirectory` bleibt unverändert — die Suite arbeitet ausschließlich in `%TEMP%/reporter-e2e-*`, das im Teardown gelöscht wird.
- Unit-Tests bleiben unverändert lauffähig: `dotnet test Reporter.sln --filter "Category!=E2E"`.
