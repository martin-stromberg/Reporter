<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Inventar: EF-Core-Nutzung

## DbContexts und Provider

Zwei SQLite-Datenbanken, beide via `Microsoft.EntityFrameworkCore.Sqlite` (10.0.12):

| Context | Datei | DB-Datei | Zweck |
|---|---|---|---|
| `ReporterDbContext` | `src/Reporter.Data/ReporterDbContext.cs:12` | `reporter.db` | Nutzerdaten (Feeds, Items, Settings, Logs) |
| `ContentDbContext` | `src/Reporter.Data/ContentDbContext.cs:14` | `reporter-content.db` | Artikel-Inhalte (separat ausgelagert) |

### Laufzeit-Konfiguration (`src/Reporter/MauiProgram.cs`)

- Zeilen 46–51: `REPORTER_DB_PATH`-Env-Override; Default
  `Path.Combine(FileSystem.AppDataDirectory, "reporter.db")` (plattformspezifischer Pfad).
- Zeilen 67–69: `reporter-content.db` liegt im selben Verzeichnis wie `reporter.db`.
- Zeilen 81–82: `AddDbContextFactory<ReporterDbContext>(o => o.UseSqlite(...))` und
  `AddDbContextFactory<ContentDbContext>(o => o.UseSqlite(...))` – Factory-Pattern, keine
  `DbContext`-Pooled-Registrierung.
- Zeilen 139/179–192: `MigrateDatabases(app)` synchron beim `CreateMauiApp()` (Fehler nur
  `Debug.WriteLine`, blockiert Start nicht).
- `src/Reporter/App.xaml.cs:43,108-117`: `MigrateDatabasesAsync` als Startup-Step – Retry
  des gleichen Passes in `App.OnStart`.

### Design-Time-Factories

- `src/Reporter.Data/ReporterDbContextFactory.cs:18-22` → `UseSqlite("Data Source=reporter.db")`
- `src/Reporter.Data/ContentDbContextFactory.cs:18-22` → `UseSqlite("Data Source=reporter-content.db")`
- Beide implementieren `IDesignTimeDbContextFactory<T>` – werden von `dotnet-ef`-Tools
  (und `Microsoft.EntityFrameworkCore.Design` 10.0.12, PrivateAssets=all) genutzt.
  `dotnet-ef` ist nicht als Repo-Tool gepinnt (keine `.config/dotnet-tools.json`) →
  bei Migration die passende `dotnet-ef`-11-Version voraussetzen/dokumentieren.

## Migrationen

`src/Reporter.Data/Migrations/` (ReporterDbContext): **15 Migrationen** +
`ReporterDbContextModelSnapshot.cs`.
`src/Reporter.Data/Migrations/Content/` (ContentDbContext): **2 Migrationen** +
`ContentDbContextModelSnapshot.cs`.

Ausführung ausschließlich über **`Database.MigrateAsync()`** (kein `EnsureCreated`):

- `src/Reporter.Data/DatabaseMigrationService.cs:34-44`: `MigrateAsync()` – zuerst
  `IContentMigrationService.MigrateLegacyContentAsync()`, dann
  `context.Database.MigrateAsync()` auf `reporter.db` (Reihenfolge-Invariante:
  `items.content_html` muss kopiert sein, bevor die Schema-Migration die Spalte löscht).
- `src/Reporter.Data/ItemContentMigrationService.cs:42-50`: migriert zuerst das
  Content-Schema (`contentContext.Database.MigrateAsync()`), dann Legacy-Kopie.
- `ItemContentMigrationService.cs:97`: rohes `PRAGMA table_info('items')` via
  `GetDbConnection()`/`DbCommand` – SQLite-spezifischer Raw-SQL-Pfad, bei EF-Core-11-
  Verhaltensänderungen (Command-Behandlung, Sync-I/O) mitprüfen.

CI-Coverage-Ausschluss `[*]Reporter.Data.Migrations.*` in
`.github/workflows/staging-ci.yml:125` und `pr-staging-ci.yml:128` – bleibt unverändert,
aber Namenskonvention prüfen falls Migrations neu generiert werden.

## AOT / Interpreter (iOS)

`src/Reporter/Reporter.csproj:52-56`:

```xml
<PropertyGroup Condition="'$(TargetFramework)'=='net10.0-ios'">
  <!-- EF Core nutzt Expression.Compile()/Reflection.Emit (z. B. DbSetInitializer) —
       auf dem AOT-only-Device stuerzt die App ohne Interpreter mit
       ExecutionEngineException ab. -->
  <UseInterpreter>true</UseInterpreter>
</PropertyGroup>
```

→ Condition-String `net10.0-ios` → `net11.0-ios` anpassen **und** per Issue-Vorgabe erneut
verifizieren, ob EF Core 11 auf iOS weiterhin `Expression.Compile()` nutzt bzw. ob MAUI 11
das `UseInterpreter`-Handling geändert hat. Kein weiteres `PublishAot`/`TrimMode`-Setting
im Repo (`PublishReadyToRun=false` nur im Windows-Publish, `build-and-package/action.yml:49`).

## SQLite-Reinigungsbedarf bei EF Core 11 (für den Plan)

- EF Core 11 erfordert .NET 11 – die Paketanhebung ist untrennbar an die TFM-Umstellung gekoppelt.
- Zu prüfen: EF-Core-11-Breaking-Changes (Migrations-Snapshot-Format, SQLite-Provider,
  `MigrateAsync`-Verhalten, `Microsoft.Data.Sqlite`-Native-Asset-Handling auf iOS/Arm64,
  `e_sqlite3`-Bundle-Version).
- Bestandsdatenbanken der Nutzer müssen weiter migrierbar sein (Upgrade-Pfad
  EF 10-Migrations-History → EF 11-Runtime), inkl. der `PRAGMA`-Legacy-Content-Kopie.
