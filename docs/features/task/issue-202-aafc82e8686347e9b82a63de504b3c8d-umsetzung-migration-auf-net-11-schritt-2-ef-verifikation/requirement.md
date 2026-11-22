<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Anforderung – Schritt 2: EF-Core-11-Verifikation (Phase 3)

Verifizieren statt aendern: Kein produktiver Code-Eingriff geplant.

1. EF-11-Breaking-Change-Muster pruefen (OwnsOne/OwnsMany, AsSplitQuery,
   Compiled Models, SaveChanges-Concurrency, Provider-Verhalten).
2. MigrateAsync-Pfad: `DatabaseMigrationService.MigrateAsync` (Inhalt-
   Kopie vor Schema) und `ItemContentMigrationService.MigrateLegacyContentAsync`
   inkl. PRAGMA-Pfad unter EF 11 verifizieren.
3. Test-Factories: `EnsureCreated` vs `Migrate` bewusst dokumentiert
   (TestDbContextFactory nutzt Migrate zwecks History-Tabelle).
4. Bestands-DB-Upgrade: `reporter.db` + `reporter-content.db` einer
   v0.7.0-Installation (maintenance/net10, EF 10.0.12 erzeugt) unter dem
   .NET-11-Build fehlerfrei migrieren.
5. Keine neue EF-Schema-Migration in dieser Runtime-Migration.
6. iOS-UseInterpreter-Verhalten: Code-Pruefung (Condition net11.0-ios),
   Geraeteverifikation ist U2-Restpunkt.
