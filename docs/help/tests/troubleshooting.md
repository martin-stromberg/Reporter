<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests — Fehlerbehebung

## Reporter.exe nicht gefunden

**Symptom:** Der Testlauf bricht sofort mit `FileNotFoundException: Reporter.exe not found. Build the app first (see scripts/Run-E2ETests.ps1) or set REPORTER_APP_PATH to a built executable.` ab.

**Ursache:** `ReporterAppFixture.ResolveAppPath` findet weder die Datei unter `REPORTER_APP_PATH` noch den Konventionspfad `src/Reporter/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Reporter.exe` — die App wurde nicht für das Windows-TFM gebaut.

**Lösung:**
1. `.\scripts\Run-E2ETests.ps1` aus dem Repository-Root ausführen (baut die App selbst), oder
2. die App manuell bauen: `dotnet build src/Reporter/Reporter.csproj -c Debug -f net10.0-windows10.0.19041.0 -r win-x64`, oder
3. `REPORTER_APP_PATH` auf eine bestehende Exe zeigen lassen.

## Kein Hauptfenster / Timeout beim Suite-Start

**Symptom:** Das Fixture wirft `InvalidOperationException: Reporter.exe did not show a main window within two minutes. The suite requires an interactive Windows desktop session.`

**Ursache:** UIA3 benötigt eine sichtbare Desktop-Session — der Lauf findet auf einem Headless-Agent, in einer RDP-getrennten/verriegelten Session oder auf einem Nicht-Windows-System statt.

**Lösung:**
1. Lauf in einer aktiven, entsperrten Windows-Desktop-Session ausführen.
2. Prüfen, ob `Reporter.exe` beim manuellen Start überhaupt ein Fenster zeigt (erster Start inkl. Migration kann dauern — das Fixture wartet bis zu 2 Minuten).

## Elemente werden nicht gefunden (TimeoutException)

**Symptom:** Einzelne Tests schlagen mit `TimeoutException: Element not found within …` fehl.

**Ursache:** UIA-Zugriff ist timing- und fokus-sensitiv — parallele Bedienung, Fensterwechsel oder langsame Systeme können das Rendern verzögern.

**Lösung:**
1. Während des Laufs nichts am Rechner bedienen (kein Fokus-Klau, keine Eingaben) — die Automation übernimmt Maus und Tastatur.
2. Erneut ausführen; alle Lookups pollen bereits 20 s über `UiRetry` — wiederholte Timeouts deuten auf ein echtes Problem hin (z. B. geänderte `SemanticProperties.Description`/Beschriftungen).
3. Konsolenausgabe auf `[E2E] Reporter.exe exited …` prüfen — ist die App abgestürzt, schlagen alle Folge-Lookups.

## Tab-Einträge nicht erreichbar (Overflow)

**Symptom:** `Tab '…' was not found` obwohl der Tab existiert.

**Ursache:** Bei schmalen Fenstern rendert die Shell-`TabBar` nur die ersten Einträge; die übrigen liegen hinter dem NavigationView-Overflow-Button (`TopNavOverflowButton`) und betreten den UIA-Baum erst nach Öffnen des Flyouts.

**Lösung:** Bereits in `SmokeTests.SelectTab` behandelt — der Helfer öffnet das Overflow-Flyout automatisch und versucht die Auswahl erneut. Tritt der Fehler trotzdem auf, Fenster vergrößern bzw. prüfen, ob die lokalisierten Tab-Titel (`AppResources.Tab*`) geändert wurden.

## Erwartete Beschriftungen passen nicht

**Symptom:** Tests finden Buttons/Einträge nicht, obwohl die App korrekt aussieht.

**Ursache:** Die Suite löst erwartete Texte über `AppResources.*` in der Kultur des **Testprozesses** auf; die App nutzt `language="system"`-Default ebenfalls die OS-Kultur. Bei abweichender OS-Kultur oder geänderten RESX-Schlüsseln divergieren die Namen.

**Lösung:**
1. Suite auf einer Maschine mit unterstützter OS-Kultur (Deutsch/Englisch) ausführen.
2. Nach RESX-Umbenennungen die verwendeten Schlüssel in `SmokeTests`/`UiRetry`-Aufrufen prüfen (`ButtonRename`, `CategoryNone`, `PlaceholderFeeds` u. a.).

## `dotnet test Reporter.sln` schlägt ohne gebaute App fehl

**Symptom:** Ein Solution-weiter Testlauf meldet elf fehlgeschlagene `Reporter.E2ETests`-Tests.

**Ursache:** Das E2E-Projekt ist Teil der Solution; ohne gebaute `Reporter.exe` und interaktive Session kann es nicht laufen.

**Lösung:**
1. Solution-Tests mit Filter ausführen: `dotnet test Reporter.sln --filter "Category!=E2E"` (so auch in `CONTRIBUTING.md` dokumentiert).
2. Die E2E-Suite bewusst über `.\scripts\Run-E2ETests.ps1` starten.

## Versehentlich gesetzte Env-Overrides

**Symptom:** Die manuell gestartete App findet keine echten Feed-Verzeichnis-Treffer, schreibt in eine unerwartete Datenbankdatei oder legt auf einer frischen Datenbank keinen Demo-Feed an — oder `DemoSeedTests` schlägt fehl, weil der Seed unterdrückt bleibt.

**Ursache:** `REPORTER_FEEDSEARCH_ENDPOINT`, `REPORTER_DB_PATH` oder `REPORTER_DISABLE_DEMO_SEED` sind dauerhaft im Benutzer-/System-Environment gesetzt — die Overrides wirken in **jedem** Build, auch Release. `DemoSeedTests` entfernt das Flag zwar aus dem eigenen App-Prozess, doch ein dauerhaft gesetzter Wert ist ein starkes Indiz für weitere vererbte Overrides.

**Lösung:**
1. Variablen aus dem persistenten Environment entfernen; sie sind ausschließlich pro Prozess-Start für die Suite gedacht.
2. Endpoint-Werte, die keine absolute `http`/`https`-URI sind, werden ignoriert — der DB-Pfad-Override dagegen wirkt unvalidiert und kann den App-Start mit einer Ausnahme abbrechen. `REPORTER_DISABLE_DEMO_SEED` wirkt mit jedem Wert außer `0`/`false`.

## Verwaiste Prozesse oder Temp-Verzeichnisse

**Symptom:** Nach E2E-Läufen bleiben `Reporter.exe`-Prozesse oder `%TEMP%/reporter-e2e-*`-Ordner zurück; im Extremfall sind Dateien des Build-Outputs blockiert.

**Ursache:** Mehrere Lücken im Prozess-Lebenszyklus bzw. beim Aufräumen, die nun alle abgesichert sind:
1. Harter Abbruch des Testprozesses (`testhost.exe`-Kill, Crash), bevor `DisposeAsync` lief — die gestartete `Reporter.exe` blieb unbeaufsichtigt.
2. Fehlerpfade in `InitializeAsync` nach `Process.Start`, die der frühere Attach-only-Catch nicht abdeckte (z. B. `UIA3Automation`-Konstruktor, `GetMainWindow`-Timeout, `SetForeground`) — vor der `App`-Zuweisung griff auch der `App is not null`-Guard in `DisposeAsync` nicht.
3. `App.Kill()` ohne `WaitForExit`-Nachweis — ein überlebender Prozess wurde still geschluckt.
4. Temp-Verzeichnisse konnten trotz beendetem Prozess nicht gelöscht werden: Eine gepoolte `Microsoft.Data.Sqlite`-Verbindung der `FeedDbAssertions` hielt `reporter.db` im Test-Host nach `Dispose` offen, und eine frisch gekillte App gibt ihre Dateien erst einen Moment nach dem bestätigten Exit frei — das einmalige `Directory.Delete` scheiterte dann still.

**Eingebaute Absicherung:** Der Lebenszyklus ist dreifach abgesichert — `E2EProcessGuard` weist jede gestartete `Reporter.exe` einem Windows-Job-Objekt mit `KILL_ON_JOB_CLOSE` zu (endet der Test-Host, beendet das OS die App automatisch); `DisposeAsync`, der `InitializeAsync`-Backstop und das `finally` in `DemoSeedTests` töten per `Kill(entireProcessTree: true)` mit `WaitForExit`-Timeout und melden einen überlebenden Prozess per `[E2E]`-Warnung auf der Konsole; `scripts/Run-E2ETests.ps1` räumt im `finally` verbliebene `Reporter`-Prozesse auf, eingegrenzt auf den Build-Output-Pfad `$appOutput`, damit eine parallel laufende Nutzer-Installation nicht getroffen wird. Gegen die Dateisperren hilft `Pooling=False` im `FeedDbAssertions`-Connection-String (keine gepoolte Verbindung hält die DB-Datei offen) plus `E2EProcessGuard.TryDeleteDirectoryAsync` mit bis zu fünf Versuchen à 200 ms. Verbleibende Einschränkung: Bei einem harten Test-Host-Tod ist kein Teardown mehr möglich — das Job-Objekt schließt das Prozess-Leck, doch ein `reporter-e2e-*`-Temp-Leichnam kann zurückbleiben.

**Manuelle Bereinigung (Fallback):**
1. Prozess `Reporter` (aus dem Build-Output) manuell beenden.
2. `%TEMP%/reporter-e2e-*`-Ordner löschen — enthalten nur die isolierten Test-DBs (`reporter.db` und die abgeleitete `reporter-content.db` samt SQLite-Sidecars).
