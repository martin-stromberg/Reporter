<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Testergebnisse – Schritt 2

## Breaking-Change-Grep
Kein Treffer fuer OwnsOne/OwnsMany/AsSplitQuery/EFOptimizeContext/
CompileModel/AutoTransactionBehavior im Produktivcode.

## v0.7.0-Bestands-DB-Upgrade (maintenance/net10, dotnet-ef 10.0.12)
- Vollstand reporter.db (15 Migrationen, ProductVersion 10.0.12) +
  reporter-content.db (2): `dotnet ef database update` unter
  dotnet-ef 11.0.0/EF 11 -> "already up to date", fehlerfrei.
- Partiellstand (14/1 Migrationen): EF 11 wendete
  `20261017045445_AddItemTags` bzw. `20260919084558_AddItemContentImageColumns`
  fehlerfrei an (echte Migrationsarbeit auf EF-10-Migrationscode).
- Tabellen nach Upgrade: reporter.db = feeds, items, categories, keywords,
  settings, sync_logs, debug_log_entries, __EFMigrationsHistory,
  __EFMigrationsLock (neu, EF-11-Infra; fuer EF-10-Rollback unbedenklich –
  unbekannte Tabelle wird ignoriert); reporter-content.db = item_contents,
  History, Lock.

## Unit-Tests (net11.0, dotnet-ef 11, MTP)
805/805 gruen, 0 fehlgeschlagen (bereits Schritt-1-Lauf, unveraendert
gueltig; kein Code-Diff). Service-Tests decken:
- MigrateAsync-Reihenfolge Inhalt vor Schema + Fehlerpropagation
  (DatabaseMigrationServiceTests).
- PRAGMA-Pfad, Seitenweise-Copy, Idempotenz, No-Op ohne content_html
  (ItemContentMigrationServiceTests).
- Test-Factories nutzen Migrate() (History-Tabelle); EnsureCreatedAsync
  nur in isolierten Schema-Tests – wie geplant.

## UseInterpreter (iOS)
Condition auf net11.0-ios gesetzt (Code-Pruefung, Reporter.csproj).
Geraete-/TestFlight-Verifikation: U2-Restpunkt (lokal nicht moeglich,
Windows-Host ohne angeschlossenes iOS-Geraet/Store-Pipeline).

## Kein Schema-Eingriff
Diff enthaelt keine Datei unter Migrations/; historische
ProductVersion-10.0.12-Annotationen unveraendert.
