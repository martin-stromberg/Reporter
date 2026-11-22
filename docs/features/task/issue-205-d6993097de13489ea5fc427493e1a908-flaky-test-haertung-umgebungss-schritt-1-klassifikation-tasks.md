<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Testklassifikation und lokale Härtung

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Klassifikation | `TestCategories` in `Reporter.Tests` anlegen | Erledigt | `TestClassificationConventionTests.*` |
| 2 | Klassifikation | `OsInterfaceAttribute` in `Reporter.Tests` anlegen | Erledigt | `OsInterfaceAttribute_GetTraits_ReturnsOsInterfaceCategory` |
| 3 | Klassifikation | `FlakyAttribute` in `Reporter.Tests` anlegen | Erledigt | `FlakyAttribute_GetTraits_ReturnsFlakyCategory` |
| 4 | Klassifikation | `TestCategories` in `Reporter.E2ETests` anlegen | Erledigt | `Category=OsInterface`-Filterliste |
| 5 | Klassifikation | `OsInterfaceAttribute` in `Reporter.E2ETests` anlegen | Erledigt | `Category=OsInterface`-Filterliste |
| 6 | Klassifikation | `FlakyAttribute` in `Reporter.E2ETests` anlegen | Erledigt | `FlakyTests_AlwaysDeclareOsInterface` |
| 7 | Cleanup | `TempDirectoryCleanup` in `Reporter.Tests` implementieren | Erledigt | `TempDirectoryCleanupTests.*` |
| 8 | Tests | `TestClassificationConventionTests` schreiben | Erledigt | 3 Tests bestanden |
| 9 | Tests | `TempDirectoryCleanupTests` schreiben | Erledigt | 4 Tests bestanden |
| 10 | E2E-Klassifikation | `[OsInterface]` auf alle OS-sensitiven E2E-Methoden setzen | Erledigt | 39 Tests in gefilterter Liste |
| 11 | Verifikation | `OsInterface`-Trait-Filter lokal prüfen | Erledigt | 4 Tests `Reporter.Tests`, 39 Tests `Reporter.E2ETests` |
| 12 | Verifikation | `Reporter.Tests` vollständig ausführen | Erledigt | 805/805 bestanden |
