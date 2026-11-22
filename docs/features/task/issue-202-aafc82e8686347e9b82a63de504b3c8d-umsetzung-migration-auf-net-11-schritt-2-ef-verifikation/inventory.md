<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme – Schritt 2

- `DatabaseMigrationService.MigrateAsync`: Inhalt-Kopie (MigrateLegacyContentAsync
  inkl. ContentDbContext.MigrateAsync) vor ReporterDbContext.MigrateAsync;
  Fehler der Kopie propagiert und blockiert Schema-Migration.
- `ItemContentMigrationService`: PRAGMA `table_info('items')` auf `content_html`,
  seitenweiser Copy in Content-Store; idempotent, No-Op ohne Spalte.
- Test-Factories: `TestDbContextFactory`/`TestContentDbContextFactory` rufen
  `Migrate()` (History-Tabelle noetig); Einzel-Tests nutzen
  `EnsureCreatedAsync` fuer isolierte Schema-Tests – beabsichtigt.
- Migrations-Bestand: 15 ReporterDbContext- + 2 ContentDbContext-Migrationen,
  historische `ProductVersion 10.0.12`-Annotationen bleiben unveraendert.
- Breaking-Change-Checkliste (EF 11): keine Verwendung von OwnsOne/OwnsMany,
  AsSplitQuery, Compiled Models, AutoTransactionBehavior-Overrides.
- v0.7.0-DBs: via git-worktree `maintenance/net10` + dotnet-ef 10.0.12
  erzeugt (Vollstand + Partiellstand je 1 ausstehende Migration).
