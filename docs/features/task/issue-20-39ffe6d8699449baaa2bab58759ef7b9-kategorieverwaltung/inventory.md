# Bestandsaufnahme: Kategorieverwaltung

## Architektur
- .NET MAUI-App mit Shell-Navigation (`AppShell.xaml` / `AppShell.xaml.cs`).
- MVVM: `ViewModels` leiten von `BaseViewModel` (CommunityToolkit.Mvvm `ObservableObject`) ab.
- DI in `MauiProgram.cs` registriert Repositories und ViewModels/Seiten.
- Datenlayer: `ReporterDbContext` (EF Core SQLite), `CategoryRepository` implementiert `ICategoryRepository`.

## Domain-Modell
- `Reporter.Core.Models.Category`: `{ Id, Name }` (init-only).
- `Reporter.Data.Entities.Category`: `{ Id, Name }` (mutable).
- Datenbanktabelle `categories`: `id` PK, `name` required, max 500, unique index.
- `feeds.category_id` ist optional (FK to `categories.id`, `DeleteBehavior.SetNull`).

## Bestehende UI
- `AppShell` erzeugt 4 Tabs: Unread, Feeds, Later, Settings.
- Seiten/XAML bisher Platzhalter; `SettingsPage` am ehesten als Ausgangspunkt für verwaltende Seiten.
- `AppResources` (RESX) enthält lokalisierte Titel/Platzhalter.

## Repository-Methoden (`ICategoryRepository`)
- `GetAllAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`.
- `DeleteAsync` entfernt Kategorie ohne Behandlung zugeordneter Feeds; DB-Constraint setzt `category_id` automatisch auf NULL.

## Offene Punkte
1. Löschverhalten: DB-Schema setzt `category_id` bereits auf NULL – Ausgabe soll dies transparent behandeln.
2. Navigation: Einstieg über neuen Tab oder über Settings? → Lösung im Plan.
