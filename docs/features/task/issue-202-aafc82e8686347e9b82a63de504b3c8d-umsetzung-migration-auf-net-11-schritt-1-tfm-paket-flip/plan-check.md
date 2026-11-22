<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Planprüfung – Schritt 1

## Ergebnis

**Status:** Plan vollständig

## Abgleich Anforderung ↔ Planelemente

Alle Anforderungen 1–7 der `requirement.md` sind im Plan abgedeckt;
sämtliche Datei:Zeile-Referenzen wurden gegen den Repo-Stand verifiziert
und stimmen (`Reporter.csproj:8-13/52/45-49/76-77`, `Core:6/15`,
`Data:6/18-22`, `Tests:6/17-18`, `E2ETests:6/17/24`,
`ReporterAppFixture.cs:143`, `Run-E2ETests.ps1:13/24`,
`iOS-Deployment.ps1:94`, `global.json`, `Package.appxmanifest:22-23`).
Zielversionen konsistent (EF/Sqlite/Logging.Debug/Syndication → 11.0.0,
TimeProvider.Testing → 11.0.1).

Nicht im Plan genannte `net10.0`-/`10.0.x`-Vorkommen — alle korrekt
ausgeschlossen: `src/Reporter.Data/Migrations/**`
(`HasAnnotation "10.0.12"` — historisch, nicht anfassen),
`Reporter.sln:5` (`MinimumVisualStudioVersion`), `ci-instructions.md`
(Schritt 3), Doku-`.md` (Schritt 4), `.github/` (Schritt 3),
`package-lock.json` (npm), `Directory.Build.props` (keine Versionen).
`Directory.Packages.props` existiert nicht (kein CPM).

## Fehlende oder unvollständige Punkte

Keine harten Lücken.

## Hinweise

1. Test-Stack-Kompatibilität (2.6) wird über den `dotnet test`-Lauf
   indirekt verifiziert — im Test-Artefakt explizit dokumentieren.
2. „Keine neue EF-Schema-Migration": passiv erfüllt — im Abschluss-Diff
   `src/Reporter.Data/Migrations/` gegen Basis prüfen und vermerken.
3. MTP-Aufruf muss dokumentiert *laufen*, nicht nur geprüft werden.
4. `Package.appxmanifest` `10.0.17763.0` ist eine Windows-OS-Version —
   beim TFM-Grep nicht mitflippen; `Reporter.sln`
   `MinimumVisualStudioVersion` ebenfalls unberührt lassen.
