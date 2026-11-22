<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Testbestand und Baseline

## Reporter.Tests

- 92 Testklassen.
- Baseline: 798 bestanden, 0 fehlgeschlagen, 0 übersprungen.
- Vollständiger Lauf: `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj --no-build`.
- Nachweisdatei: `inventory/test-results/baseline-reporter-tests.txt`.

## Reporter.E2ETests

Testklassen:

- `SmokeTests`
- `FeedDetailTests`
- `UnreadPacketTests`
- `ArticleTagsTests`
- `ArticleLinkTests`
- `ArticleImageTests`
- `DemoSeedTests`
- `E2EProcessGuardTests`

Die ersten sieben Klassen starten oder prüfen die Anwendung über FlaUI/UIA, reale Prozesse, Fenster und/oder temporäre Datenbanken. `E2EProcessGuardTests` prüft die Prozess- und Cleanup-Infrastruktur mit echten Prozessen und Dateisperren.

## Filterbarkeit

- Bestehende UI-Tests besitzen `[Trait("Category", "E2E")]`.
- Microsoft Testing Platform wurde lokal mit `--list-tests -- --filter "FullyQualifiedName~..."` geprüft.
- Für Trait-Filter ist `-- --filter-trait "Category=OsInterface"` der später zu prüfende Mechanismus.
