<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# OS-Interface-Tests

Die OS-Interface-Tests sind eine eigene Testkategorie (`Category=OsInterface`) in `src/Reporter.Tests`. Sie decken Verhalten ab, das vom Host-Betriebssystem abhängt, und laufen deshalb getrennt von den deterministischen Unit-Tests — lokal per Trait-Filter und in der CI als eigene, nicht blockierende Best-Effort-Lane mit einmaligem Flaky-Retry und Flake-Statistik. Diese Seite richtet sich an Entwickler und Maintainer: Sie beschreibt die Klassifikationsregeln, die lokalen Befehle, die CI-Trennung, die Retry- und Re-Run-Regeln sowie die Auswertung über mehrere Läufe.

## Klassifikation

### Wann `Category=OsInterface`

Ein Test bekommt `Category=OsInterface` (Attribut `[OsInterface]` auf Methode oder Klasse, `src/Reporter.Tests/OsInterfaceAttribute.cs`), wenn seine Assertion oder sein zuverlässiger Teardown von realen Betriebssystemressourcen abhängt. Kriterien:

- **Prozess-/OS-API:** Der Test startet oder beendet echte Prozesse bzw. nutzt Betriebssystem-APIs (z. B. Windows-Job-Objekte, exklusive Dateisperren).
- **UI-Automation:** Der Test steuert reale Fenster/Controls über UIA, statt ausschließlich In-Memory-Objekte zu prüfen.
- **Echte Temp-FS-/Netzwerk-/Desktop-Abhängigkeit:** Der Test braucht echtes Dateisystem-Verhalten (Temp-Verzeichnisse, Locks, Lösch-Retries), echte Sockets oder eine sichtbare Desktop-Session.
- **Deadlines gegen Kindprozesse:** Der Test wartet mit Timeouts/Deadlines auf externe Prozesse — Timing und Ressourcen des Hosts entscheiden über das Ergebnis.

Aktuell tragen die vier Tests der `TempDirectoryCleanupTests` in `Reporter.Tests` die Kategorie (echte Temp-Verzeichnis-Löschung gegen Dateisperren); in `Reporter.E2ETests` tragen alle Tests `OsInterface` zusätzlich zu ihrem `E2E`-Trait.

### Wann `Category=Flaky`

`Category=Flaky` (Attribut `[Flaky]`, `src/Reporter.Tests/FlakyAttribute.cs`) ist eine explizite Ausnahmemarkierung für OS-sensitive Tests, die trotz Härtung beobachtbar umgebungsabhängig ausfallen. Regeln:

- `Flaky` darf **niemals ohne `OsInterface`** vergeben werden — `TestClassificationConventionTests` erzwingt das per Reflection über die gesamte Test-Assembly. Begründung: `Flaky` beschreibt OS-Rauschen, keinen generellen Freifahrtschein; ein deterministischer Fehler darf sich nicht hinter der Kategorie verstecken.
- Vor dem Setzen zuerst härten (Timeouts, Retries, Isolation). `Flaky` dokumentiert Restinstabilität, die sich nicht deterministisch beheben lässt.
- Nur die `Flaky`-Menge wird in der CI einmalig wiederholt — ohne Markierung gibt es keinen Retry.

## Lokale Befehle (Microsoft Testing Platform)

`Reporter.Tests` läuft über die Microsoft Testing Platform (xUnit v3): Trait-Filter stehen hinter `--` als `--filter-trait`/`--filter-not-trait`, TRX wird über `--report-trx` erzeugt.

| Ziel | Befehl |
|------|--------|
| Deterministische Lane (spiegelt `build-and-test`) | `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj -c Release -- --filter-not-trait "Category=OsInterface"` |
| Nur OS-Interface-Tests | `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj -c Release -- --filter-trait "Category=OsInterface"` |
| Nur Flaky-Tests | `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj -c Release -- --filter-trait "Category=Flaky"` |
| OS-Lauf mit TRX (spiegelt den CI-Schritt „Test (OsInterface)") | `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj -c Release -- --filter-trait "Category=OsInterface" --report-trx --report-trx-filename test-results-os-interface.trx --results-directory TestResults` |

Zwei MTP-Eigenheiten sind relevant: `--filter` und `--filter-trait` dürfen **nicht** in denselben `--`-Argumenten kombiniert werden (das Retry-Skript nutzt für seinen Re-Run daher eine reine VSTest-Query), und ein Trait-Filter ohne Treffer meldet „0 Tests" mit einem eigenen Exit-Code — aktuell ist noch kein Test als `Flaky` markiert, die Flaky-Liste ist daher erwartungsgemäß leer.

## CI-Trennung: blockierende Lane vs. Best-Effort-Lane

Beide Staging-Workflows (`pr-staging-ci.yml`, `staging-ci.yml`) führen `Reporter.Tests` zweigeteilt aus:

| Lane | Job | Filter | Timeout | Wirkung |
|------|-----|--------|---------|---------|
| Deterministisch | `build-and-test` | `--filter-not-trait "Category=OsInterface"` | 30 min | **Blockierend:** Ein Fehlschlag bricht PR/Staging; der Job enthält weiterhin das Coverage-Gate und lädt `test-results-pr`/`test-results-staging` hoch. |
| OS-sensitiv | `os-interface-tests` | `--filter-trait "Category=OsInterface"` | 45 min | **Best effort:** `continue-on-error: true` macht Fehlschläge sichtbar, ohne das Gate zu blockieren; eigenes Artefakt `test-results-os-interface-pr`/`test-results-os-interface-staging` (Glob `TestResults/*os-interface*.trx`, 14 Tage Retention). |

Der OS-Job hängt ausschließlich an `detect-backmerge` und steht bewusst in keiner `needs`-Kette der Gate-/Release-Jobs (`version`, `package-*`, `prerelease`). Die Artefaktnamen beider Lanes kollidieren nicht.

## Retry-Regel: genau einmal, nur `Flaky`, niemals Blanket-Retry

Schlägt der OS-Testschritt fehl, ruft der Folgeschritt „Retry flaky tests once" (`if: failure()`) `scripts/Retry-FlakyTests.ps1` auf. Das Skript:

1. liest die fehlgeschlagenen Tests aus dem Primär-TRX (Theory-Datenzeilen werden auf den Methoden-FQN normalisiert),
2. ermittelt die Flaky-Menge des Projekts über `dotnet test --no-build --list-tests -- --filter-trait "Category=Flaky"` (`scripts/lib/TestDiscovery.ps1`),
3. wiederholt **nur die Schnittmenge** — fehlgeschlagen **und** Flaky-markiert — **genau einmal** gefiltert über `dotnet test -- --filter "(Category=Flaky)&(FullyQualifiedName~…)"`. Niemals wird die ganze Suite wiederholt („Blanket-Retry") und niemals ein deterministischer Test,
4. wertet das Retry-TRX (`<primaername>-flaky-retry.trx` im selben Verzeichnis) aus: bestanden → **recovered**; erneut fehlgeschlagen → **persistent**; kein bestimmbares Ergebnis (fehlendes TRX, `NotExecuted`, Discovery-Ausfall) → **nicht recovered**. Persistent oder nicht recovered → Exit 1.

Fehlendes/unauswertbares Primär-TRX oder Fehlschläge ohne Flaky-Markierung gelten ehrlich als „keine Flaky-Fehlschläge" (Exit 0) — der ursprünglich fehlgeschlagene Schritt bleibt das führende Signal. Auch Exit 1 des Retry-Schritts blockiert nichts: Der ganze Job steht unter `continue-on-error`, das Signal dient allein der Sichtbarkeit inkl. `::warning::`-Annotationen.

## Manueller Re-Run in GitHub Actions

Soll ein OS-Interface-Fehlschlag komplett erneut geprüft werden (z. B. nach Infrastruktur-Problemen auf dem Runner), darf **nur der OS-Interface-Job** gezielt neu gestartet werden:

```bash
gh run view <run-id>              # Job-ID des Jobs "os-interface tests" ermitteln
gh run rerun <run-id> --job <job-id>
```

**Niemals `gh run rerun --failed`** verwenden: Das startet sämtliche fehlgeschlagenen Jobs des Runs neu und kann damit auch blockierende Jobs (`build-and-test`, `static-checks`) treffen — ein erneuter Lauf würde dort ein echtes Fehlersignal verwässern. Die OS-Lane ist genau dafür isoliert, damit sie allein wiederholt werden kann.

## TRX-Flake-Statistik (`Analyze-FlakyTestResults.ps1`)

Der Schritt „Analyze flaky test results" (`if: always()`) ruft `scripts/Analyze-FlakyTestResults.ps1 -Path TestResults` auf — ein reiner Sichtbarkeits-Report, der immer mit Exit 0 endet. Ein „Lauf" wird über die Dateinamens-Konvention gruppiert: `<name>.trx` ist der Primärlauf, `<name>-flaky-retry.trx` im selben Verzeichnis der zugehörige Retry.

### Flake-Ereignisse

Als Flake-Ereignis zählt pro Testname:

- **fail → retry-pass:** Fehlschlag im Primär-TRX, grün im Retry-TRX desselben Laufs.
- **fail → nächster-Lauf-pass:** Fehlschlag im Primär-TRX von Lauf *N*, grün im Primär-TRX des nächsten Laufs, der den Test enthält. Die Reihenfolge bestimmt der Lauf-Zeitstempel im TRX (`<Times start>`); `LastWriteTimeUtc` ist nur Fallback, weil heruntergeladene Artefakte sonst den Download-Zeitpunkt spiegeln würden.

Primär-Outcomes außerhalb `Passed`/`Failed` (z. B. `NotExecuted` durch einen Skip) gelten als fehlende Beobachtung: Sie erhöhen weder den Ausführungszähler noch die Fehlrate und unterbrechen die Lauf-Sequenz nicht — kein Phantom-„grün im Folgelauf" über einen Skip hinweg, kein verlorenes fail→pass über einen Skip.

### Klassifikation und Schwellen

Pro Testname wird über die letzten `MaxRuns` Primär-Läufe (Standard: **20**) klassifiziert:

| Klassifikation | Bedingung (Standard-Schwellen, kalibrierbar) |
|----------------|--------------------------------------------|
| `Quarantaene-Kandidat` | ≥ **2 Flake-Ereignisse** (`-QuarantineMinFlakeEvents`) **oder** Fehlrate ≥ **20 %** (`-QuarantineMinFailureRate`) bei ≥ **5 Ausführungen** (`-QuarantineMinExecutions`) |
| `beobachten` | ≥ **1 Flake-Ereignis** **oder** Fehlrate ≥ **10 %** (`-WatchMinFailureRate`) |
| `stabil` | sonst |

Für Quarantäne-Kandidaten empfiehlt der Report — in GitHub Actions zusätzlich als `::warning::`-Annotation — ein **Quarantäne-Issue** anzulegen und das Fehlerbild unten unter „Bekannte Fehlerbilder" zu dokumentieren. Die Anlage bleibt Maintainer-Entscheidung; der Analyzer erstellt nichts automatisch. Fehlende TRX-Dateien erzeugen einen ehrlich leeren Report — nicht „alles grün".

## Lokale Nutzung der Skripte

### `scripts/Retry-FlakyTests.ps1`

```powershell
# Primaer-TRX eines OS-Laufs auswerten und Flaky-Fehlschlaege genau einmal wiederholen
pwsh -File scripts/Retry-FlakyTests.ps1 `
  -TestProject src/Reporter.Tests/Reporter.Tests.csproj `
  -PrimaryTrx TestResults/test-results-os-interface.trx `
  -Configuration Release
```

Der Re-Run nutzt `--no-build` und setzt daher einen vorangegangenen Build in `-Configuration` (Standard `Release`) voraus. Eine etwaige Ziel-TRX aus einem früheren Lauf wird vorher entfernt, damit nur Ergebnisse des aktuellen Re-Runs ausgewertet werden. Exit-Codes: `0` = keine Flaky-Fehlschläge oder alle recovered; `1` = persistente oder unbestimmbare Fehlschläge.

### `scripts/Analyze-FlakyTestResults.ps1`

```powershell
# Lokale Laeufe auswerten
pwsh -File scripts/Analyze-FlakyTestResults.ps1 -Path TestResults

# OS-Artefakte der CI herunterladen und auswerten
gh run download <run-id> -n test-results-os-interface-pr -D artifacts/os-pr
gh run download <run-id> -n test-results-os-interface-staging -D artifacts/os-staging
pwsh -File scripts/Analyze-FlakyTestResults.ps1 -Path artifacts/os-pr,artifacts/os-staging
```

`-Path` akzeptiert mehrere Verzeichnisse und durchsucht sie rekursiv nach `*.trx`; für statistisch belastbare Aussagen über mehrere Läufe die OS-Artefakte mehrerer Runs sammeln (das Auswertungsfenster deckt standardmäßig die letzten 20 Primär-Läufe ab).

## Warum `Reporter.E2ETests` nicht in der CI läuft

Die Desktop-Suite `src/Reporter.E2ETests` (Trait `Category=E2E`, zusätzlich `OsInterface`) bleibt bewusst lokal/on-demand: Sie steuert die echte Windows-App über UIA3 und benötigt eine sichtbare, interaktive Desktop-Session — die `windows-latest`-Runner bieten keine solche Session, und ein Self-hosted-/Display-Runner wäre ein eigener, deutlich größerer Scope. Die CI-OS-Lane führt daher nur die `OsInterface`-Tests aus `Reporter.Tests` aus; die E2E-Suite wird über `scripts/Run-E2ETests.ps1` gestartet (Details: [Technischer Ablauf](ablauf-technisch.md), [Installation & Konfiguration](installation.md)).

## Bekannte Fehlerbilder

(Noch keine dokumentiert — beobachtete OS-/Umgebungs-Effekte einzelner Flaky-Tests hier ergänzen, sobald der Analyzer sie meldet oder ein Quarantäne-Issue sie beschreibt.)
