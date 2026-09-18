<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Nach erfolgreichem `Run-E2ETests.ps1`-Lauf kein `Reporter.exe` zurück | Fixture-Härtung: `E2EProcessGuard.KillAndWaitAsync` (Tree-Kill + Timeout-`WaitForExit`) in `DisposeAsync`, PID-Fallback `_appProcessId`; Skript-`finally` als zweite Linie (Umsetzungsschritte 1, 2, 4; Programmabläufe „App-Teardown", „Skript-Lauf") | Pflicht-Verifikationslauf 1 (E2E-Tabelle Z. 159 + Schritt 6): erfolgreicher Skript-Lauf, danach `Get-Process Reporter` leer, keine blockierten Build-Output-Dateien, kein neuer `%TEMP%/reporter-e2e-*`-Leichnam | Abgedeckt |
| Nach Test-Fehlschlag kein `Reporter.exe` zurück | Gleiche Absicherung wie Erfolgspfad (`DisposeAsync` läuft bei Assert-Fehlschlag; Backstop-`catch` in `InitializeAsync` für Init-Fehler) | Pflicht-Verifikationslauf 2 (Z. 160): Suite mit mindestens einem scheiternden Test, danach `Get-Process Reporter` leer | Abgedeckt |
| Nach hartem Abbruch des Test-Hosts kein `Reporter.exe` zurück | Windows Job Object `KILL_ON_JOB_CLOSE` via `E2EProcessGuard.TrackProcess` für Fixture- und `DemoSeedTests`-Instanz (Schritte 1–3; Programmablauf „Harter Test-Host-Abbruch"); zusätzlich Skript-`finally` | Pflicht-Verifikationslauf 3 (Z. 161): `testhost.exe` während des Laufs hart beenden, danach `Get-Process Reporter` leer — exakt das in der Bestandsaufnahme reproduzierte Leck-Szenario | Abgedeckt |
| Keine Dateien des Build-Ausgabeverzeichnisses blockiert | Verifizierter Prozess-Exit (`WaitForExit` mit Timeout + Warnung) statt unbestätigtem `Kill()` | Explizit im Akzeptanzkriterium des Pflicht-Laufs 1 genannt; implizit durch Prozess-Freiheit-Nachweis | Abgedeckt |
| Neun bestehende E2E-Tests unverändert und grün | „Betroffene bestehende Tests: Keine" — Signaturen/Verhalten unverändert, `DemoSeedTests` nur interne Härtung (Z. 149–151, 163–165) | Verifikationsschritt 6: `Run-E2ETests.ps1` mehrfach erfolgreich (= Suite grün); bekannter Warm-up-Flake als Restrisiko benannt (Z. 109) | Abgedeckt |
| Fachliches Verhalten der App unverändert | Designentscheidung „App-Shutdown-Pfad: Keine Änderung" mit Begründung (Z. 18); alle Änderungen in `src/Reporter.E2ETests/` + Skript | `dotnet test Reporter.sln --filter "Category!=E2E"` und `npm test` müssen grün bleiben (Schritt 6) | Abgedeckt |
| Gefundene Ursache in `docs/help/tests/troubleshooting.md` dokumentiert | Umsetzungsschritt 5 + Abschnitt „Änderungen an bestehenden Klassen → troubleshooting.md": Ursache (Test-Host-Tod ohne `DisposeAsync`, ungesicherte `InitializeAsync`-Fehlerpfade, fehlende Exit-Verifikation) + eingebaute Absicherung | Doku-Schritt, kein Test erforderlich | Abgedeckt |
| `scripts/Run-E2ETests.ps1` mit `try`/`finally` und auf `$appOutput` eingegrenztem `Stop-Process` | Umsetzungsschritt 4; Programmablauf „Skript-Lauf" inkl. Exit-Code-Merken; Scope-Entscheidung beantwortet die offene Frage der Anforderung | Nachweis über alle drei Pflicht-Verifikationsläufe (Skript-Ebene) | Abgedeckt |
| `DisposeAsync` härten (Kill/Tree-Kill + Exit-Verifikation statt catch-all) | Umsetzungsschritt 2; Programmablauf „App-Teardown": PID via `App?.ProcessId` mit `_appProcessId`-Fallback (FlaUI-Process-Ersetzung abgedeckt), `[E2E]`-Warnung bei unbestätigtem Exit | Läuft 1+2 decken den Pfad ab; Warnung macht Rest-Leck sichtbar | Abgedeckt |
| `InitializeAsync`-Fehlerpfade nach `Process.Start` absichern (nicht nur Attach) | Umsetzungsschritt 2: umschließender `catch` über UIA3Automation, Attach, Fenster-Wait, 3-s-Pause, `SetForeground` mit `KillAndWaitAsync`-Backstop + Rethrow — ersetzt Attach-only-Catch, runner-unabhängig vom xunit-Dispose-Verhalten | Teil des Fehlschlag-Laufs (Init-Fehler-Pfad); Hypothesen-Verifikation in Schritt 6 | Abgedeckt |
| `DemoSeedTests`-Zweitinstanz härten | Umsetzungsschritt 3: `TrackProcess` nach `Process.Start`, `finally` auf `KillAndWaitAsync` | Läuft in allen drei Pflicht-Läufen mit (Test gehört zur Suite) | Abgedeckt |
| `dotnet test --filter "Category!=E2E"`, `npm test`, `Run-StaticChecks.ps1` grün | Verifikationsschritt 6 verlangt alle drei explizit | Direkter Nachweis in Schritt 6 | Abgedeckt |
| Ursachenanalyse (Hypothesen aus der Anforderung) | Hauptursache bereits in der Bestandsaufnahme belegt (testhost-Kill → `DisposeAsync` läuft nicht → Leck; kein Skript-Cleanup); Resthypothesen (Leck im Erfolgspfad, geerbte Handles, hängender App-Shutdown) in Schritt 6 über `[E2E]`-Warnung/Watcher verifiziert; Designentscheidung Z. 18 begründet Nicht-Änderung an `src/Reporter/` | Schritt 6 „Verifikation und Rest-Analyse" | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Benutzerfluss (UI) | — | Nicht erforderlich mit Begründung: Plan begründet nachvollziehbar, dass kein Benutzerfluss berührt ist und ein In-Process-Assert den Zustand „kein Prozess nach Test-Host-Tod" definitionsgemäß nicht prüfen kann (Z. 155); Nachweis über externe Prozess-Checks nach kontrollierten Skript-Läufen |
| Erfolg-Lauf → kein `Reporter.exe` | Manueller Verifikationslauf (Pflicht, Z. 159) | Abgedeckt |
| Fehlschlag-Lauf → kein `Reporter.exe` | Manueller Verifikationslauf (Pflicht, Z. 160) | Abgedeckt |
| Abbruch-Lauf (testhost-Kill) → kein `Reporter.exe` | Manueller Verifikationslauf (Pflicht, Z. 161) | Abgedeckt |
| Neun E2E-Tests unverändert grün | Erfolgreiche `Run-E2ETests.ps1`-Durchläufe in Schritt 6; alle neun Tests namentlich über Klassen `SmokeTests`, `ArticleLinkTests`, `DemoSeedTests` referenziert | Abgedeckt |

## Hinweise

- Die Anforderung sieht „Ursachenanalyse vor dem Fix" als ersten Implementierungsschritt vor. Der Plan enthält keinen eigenen Analyse-Schritt vor der Umsetzung, weil die Bestandsaufnahme die Hauptursache bereits belegt hat (harter `testhost.exe`-Kill → `DisposeAsync` läuft nicht; kein Skript-Cleanup vorhanden). Die verbleibenden Hypothesen (Leck im Erfolgspfad, geerbte Konsolen-/Pipe-Handles, hängender App-Shutdown) sind in Verifikationsschritt 6 eingeplant und werden über die neue `[E2E]`-Warnung bzw. den Exit-Watcher sichtbar. Keine Lücke, aber bei der Umsetzung sicherstellen, dass Schritt 6 diese Rest-Verifikation tatsächlich dokumentiert.
- Die offenen Fragen der Anforderung (Job-Object-Zulässigkeit, Cleanup-Scope) sind im Plan als Designentscheidungen beantwortet: Job Object mit best-effort-`TrackProcess` als Fallback-Strategie (Z. 13, 17), Pfad-Eingrenzung auf `$appOutput` (Z. 14).
- Restrisiken sind vollständig benannt (Z. 104–110): fehlschlagende Job-Zuweisung in Job-Umgebungen, gegenseitiges Aufräumen paralleler E2E-Läufe, Tree-Kill von Kindprozessen, bekannter `AppStarts_FeedListRenders`-Warm-up-Flake (muss bei „mehrfach erfolgreichen" Verifikationsläufen vom Prozess-Leck unterschieden werden).
- Voraussetzungen der Umsetzungsschritte sind konsistent (Schritt 1 vor 2/3, Schritt 4 unabhängig, Doku nach 1–4, Verifikation zuletzt). Notwendige Hilfskonstrukte (`E2EProcessGuard`, `_appProcessId`) sind vollständig benannt; keine neuen Testdaten/Fixtures erforderlich.
