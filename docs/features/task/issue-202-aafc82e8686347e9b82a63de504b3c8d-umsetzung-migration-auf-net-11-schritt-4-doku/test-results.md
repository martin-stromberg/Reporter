<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Testergebnisse – Schritt 4

- Volltextsuche `net10.0|.NET-10|.NET 10|10.0.x` ueber README,
  ci-instructions.md, docs/, scripts/, .github/, src/:
  **0 Treffer** ausserhalb legitimer Ausschluesse (obj/bin,
  EF-Migrations-Historie mit ProductVersion 10.0.12,
  Projekt-/Feature-Artefakte, changes.log-Historie,
  dotnet-11-migration.md selbst).
- Migrationsplan: Status „durchgeführt", Umsetzungsstand dokumentiert
  (dotnet-tools.json-Abweichung, iOS-Pin 27.2.11039, G5-Anhebungen,
  DB-Upgrade-Nachweis, U2-Restpunkt TestFlight/Geraet); Hinweis zu
  veralteten Zeilenreferenzen ergaenzt.
- RELEASE_NOTES.md: x86-64-v2-Hinweis EN+DE;
  ablauf-anwender.md: Voraussetzung + CLR-Fehlercode 0xC000001D;
  changes.log: Migrations-Eintrag 2026-12-07.
- Kein UI-/Code-Eingriff.
