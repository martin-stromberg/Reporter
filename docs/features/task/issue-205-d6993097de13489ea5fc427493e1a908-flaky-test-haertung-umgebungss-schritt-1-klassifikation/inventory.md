<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Testklassifikation und lokale Härtung

## Ausgangslage

- `src/Reporter.Tests` enthält 92 `*Tests`-Klassen und hatte beim Baseline-Lauf 798 bestandene Tests.
- `src/Reporter.E2ETests` enthält acht Testklassen; sieben sind UI-/Suite-Tests, `E2EProcessGuardTests` ist eine Infrastruktur-Testklasse.
- Es existieren noch keine `OsInterface`- oder `Flaky`-Attribute.
- `Reporter.E2ETests` nutzt xUnit v3 und Microsoft Testing Platform (`UseMicrosoftTestingPlatformRunner`).
- `Reporter.Tests` nutzt ebenfalls xUnit v3.

## Relevante Detaildokumente

- [Testbestand und Baseline](inventory/tests.md)
- [OS-sensible Bereiche](inventory/os-interface.md)
- [Bestehende Cleanup-/Deadline-Infrastruktur](inventory/cleanup.md)

## Befunde

- Die Desktop-E2E-Tests besitzen bereits `Category=E2E`, aber keine weitere Filterdimension.
- `E2EProcessGuardTests` besitzt bewusst kein `Category=E2E`, arbeitet aber trotzdem mit echten Prozessen und Dateisperren.
- `E2EProcessGuard.TryDeleteDirectoryAsync` löst bereits einen robusten best-effort Cleanup mit fünf Versuchen und 200 ms Abstand.
- `Reporter.Tests` besitzt keine vergleichbare Temp-Cleanup-Hilfe.
- Eine `Flaky`-Konvention muss lokal abgesichert werden, bevor Schritt 2 sie in CI verwendet.

## Baseline

- `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj --no-build`: 798 bestanden, 0 fehlgeschlagen, 0 übersprungen.
- Nachweis: [baseline-reporter-tests.txt](inventory/test-results/baseline-reporter-tests.txt)

## Abweichung vom Lifecycle-Standard

Der Lifecycle sieht Subagenten vor. Diese CLI-Umgebung erlaubt Subagenten nur bei ausdrücklicher Nutzeranweisung; die Schritte werden daher direkt ausgeführt und die Ergebnisse als Artefakte dokumentiert.
