# Plan-Check: Repository-Schicht und Domänenmodelle

Status: **Plan vollständig**

## Zusammenfassung
Alle aus `plan-check.1.md` identifizierten Lücken wurden im überarbeiteten `plan.md` adressiert.

## Geprüfte Aspekte

1. **Lebensdauer DbContext / Repositories**
   - Entschieden: Repositories als `Singleton`, `IDbContextFactory<ReporterDbContext>` pro Operation.
   - `MauiProgram` verwendet `AddDbContextFactory<ReporterDbContext>`.
   - Damit kein Konflikt zwischen Singleton-Repositories und scoped `DbContext`.

2. **DI-Verfügbarkeit**
   - `ServiceCollectionTests` löst alle sechs Repository-Schnittstellen aus einem `ServiceProvider` auf.
   - Test ist im Plan verankert.

3. **Vorläufiger `Article`-Code**
   - Plan beschreibt Ersetzen von `Article`, `IArticleRepository`, `ArticleRepository`, `IArticleService` und `ArticleService` durch `Item`/`IItemRepository`/`ItemRepository`.
   - `ArticleRepositoryTests.cs` wird durch `ItemRepositoryTests.cs` ersetzt.

4. **Mapping**
   - Manuelles Mapping in privaten statischen Hilfsmethoden festgelegt; keine zusätzliche Mapper-Abhängigkeit.

5. **Settings-Singleton**
   - `GetAsync` erzeugt Default-Datensatz bei Bedarf.
   - `SaveAsync` normalisiert `Id` auf `Settings.DefaultId`.
   - Negative Testfälle ergänzt.

6. **Filter-/Query-Tests**
   - Happy-Path- und Negative-Tests für `GetUnreadByDateAsync`, `GetByFeedAsync`, `GetByCategoryAsync`, `GetSavedForLaterAsync` vorhanden.

## Kritische Probleme
Keine.

## Wesentliche Schwächen
Keine.

## Offene Fragen an den Anwender
Keine.
