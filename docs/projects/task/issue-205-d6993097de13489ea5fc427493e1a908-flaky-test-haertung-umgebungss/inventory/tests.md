<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ausgangszustand

## Rahmen

- Zeitpunkt: 2026-11-22T10:58:58+01:00
- Branch: `task/issue-205-d6993097de13489ea5fc427493e1a908-flaky-test-haertung-umgebungss`
- Commit: `82c9bbdd83ac0a558a6b266ac17d4877b74c9806`
- Uncommittete Änderungen zum Baseline-Zeitpunkt: keine relevanten Quell-/Teständerungen
- .NET SDK: `11.0.100`
- PowerShell: `7.6.10`
- Testprojekt: `src/Reporter.Tests/Reporter.Tests.csproj`, xUnit v3 über Microsoft.Testing.Platform

## Testläufe

| Lauf | Befehl | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test --project src\Reporter.Tests\Reporter.Tests.csproj --no-build` | Repo-Root | 0 | 798 | 0 | 0 | [Log](test-results/baseline-reporter-tests.txt) |
| MTP-Filterprobe | `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj --no-build --list-tests -- --filter "FullyQualifiedName~AppCultureTests.Apply_SetsCultures"` | Repo-Root | 0 | 1 gelistet | 0 | – | Konsolenausgabe |

## Nachgewiesene bestehende Testfehler

Keine.

## Testlücken und Ausführungsprobleme

- `Reporter.E2ETests` wurde für die Baseline nicht ausgeführt, weil die Suite eine interaktive Windows-Desktop-Session benötigt und bewusst nicht Teil der CI ist.
- Vor der Umsetzung existiert kein `Category=OsInterface`; eine Include-/Exclude-Filterung kann daher erst nach Einführung der Traits mit realer Treffermenge verifiziert werden.
