# Plan-Check: Repository-Schicht und Domänenmodelle

Status: **Plan lückenhaft**

## Kritische Probleme

1. **Lebensdauerkonflikt zwischen Repository und DbContext**
   - Der Plan sieht `AddSingleton` für die Repositories vor.
   - `AddDbContext<ReporterDbContext>` registriert den Kontext standardmäßig als **Scoped**.
   - Singleton-Repositories können einen scoped `DbContext` nicht konsumieren (Ausnahme bei `ValidateScopes=true`).
   - **Lösung:** Entweder `AddDbContext(..., ServiceLifetime.Singleton)` wählen oder `IDbContextFactory<ReporterDbContext>` injizieren und pro Operation einen neuen Kontext erzeugen. Für MAUI ist `IDbContextFactory` die robustere Variante (Thread-Safety, kurzlebige Kontexte).

2. **Fehlende Teststrategie für DI-Verfügbarkeit**
   - AK 4 („Repositories sind über DI in den ViewModels verfügbar“) wird im Plan nur mit einem „Kompilierungs-Check“ abgedeckt.
   - Das ist zu schwach; es sollte mindestens ein Unit-Test oder ein manueller DI-Aufbau (`ServiceCollection`) nachweisen, dass `IItemRepository` etc. auflösbar sind.

## Wesentliche Schwächen

3. **Unklare Vorgehensweise bei bestehendem `ArticleRepositoryTests.cs`**
   - Der Plan ersetzt `IArticleRepository`/`ArticleRepository` durch `IItemRepository`/`ItemRepository`.
   - Es fehlt die explizite Erwähnung, dass `ArticleRepositoryTests.cs` in `ItemRepositoryTests.cs` überführt oder entfernt wird.

4. **Mapping-Strategie nur implizit**
   - Zwar wird „Mapping in privaten Hilfsmethoden“ genannt, aber nicht, ob `init`-only Properties manuell gesetzt oder ein Mapper (z. B. `Mapster`, `AutoMapper`) verwendet wird. Für Konsistenz und Testbarkeit reicht manuelles Mapping; das sollte explizit festgelegt werden.

5. **Settings-Singleton ohne Konfliktlösung**
   - `SaveAsync` soll immer denselben Datensatz überschreiben. Was passiert, wenn ein Aufrufer ein `Settings`-Objekt mit einer anderen `Id` übergibt? Das muss ignoriert oder auf `Settings.DefaultId` normalisiert werden.

6. **Fehlende Negative Tests für Filter-Queries**
   - `GetByFeedAsync`/`GetByCategoryAsync` mit nicht existierender Id sollten getestet werden.
   - `GetUnreadByDateAsync` bei ausschließlich gelesenen Items muss leere Liste liefern.

## Offene Fragen an den Anwender

Keine — alle gefundenen Punkte lassen sich aus dem bestehenden Codebase und der Architektur ableiten.

## Empfohlene Plan-Anpassungen

- Lebensdauer/Factory-Ansatz für `DbContext` und Repositories klären und im Plan verankern.
- `ArticleRepositoryTests.cs` explizit durch `ItemRepositoryTests.cs` ersetzen.
- DI-Verfügbarkeit mit einem einfachen `ServiceCollection`-Test nachweisen.
- Negative Filter-Testfälle ergänzen.
- Settings-Id-Normalisierung dokumentieren.
