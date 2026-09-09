# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### `src/Reporter/Reporter.csproj`

- **Portabilität / Build-Ziele** — Der `<TargetFrameworks>`-Block ist ausschließlich für `windows` und `osx` bedingt. Auf einem Linux-Build-Agenten bleibt `TargetFrameworks` leer und `dotnet build` schlägt fehl.
  - Empfehlung: Für CI-/Linux-Umgebungen entweder ein `net10.0`-Fallback oder eine dokumentierte Einschränkung „nur Windows/macOS“ hinterlegen.

- **RESX-Code-Generator** — Die `AppResources.resx`/`AppResources.de.resx`-Einträge verwenden `Generator=ResXFileCodeGenerator` mit `LastGenOutput=*.Designer.cs`. In einem CLI-Build (`dotnet build`) werden die `*.Designer.cs`-Dateien nicht automatisch erzeugt; zudem verweisen die RESX-ResHeader auf `System.Windows.Forms`, das auf iOS/Android/macOS nicht verfügbar ist.
  - Empfehlung: `Generator`/`LastGenOutput` aus `.csproj` entfernen, Ressourcen über `ResourceManager` oder `IStringLocalizer` konsumieren und die RESX-`reader`/`writer`-Header auf `System.Resources.ResXResourceReader` bzw. `Writer` aus `System.Resources.Extensions` umstellen.

### `src/Reporter/App.xaml.cs`

- **Kopplung / Service Locator** — `public static IServiceProvider? Services` und der Zugriff `App.Services?.GetRequiredService<...>()` in den Pages (`UngelesenPage`, `FeedsPage`, `SpaeterPage`, `EinstellungenPage`) machen die Pages von einem globalen Zustand abhängig und erschweren Unit-Tests.
  - Empfehlung: Pages im DI-Container registrieren (`services.AddTransient<UngelesenPage>()`) und das ViewModel über Konstruktor-Injection übergeben. Alternativ eine `IServiceProvider`-Abstraktion für Pages injizieren.

### `src/Reporter/ViewModels/FeedsViewModel.cs`

- **Toter Code / Irreführende Abhängigkeit** — Der Konstruktor nimmt `IArticleService articleService` entgegen, speichert sie aber nicht und nutzt sie nicht. Das suggeriert eine Abhängigkeit, die nicht existiert.
  - Empfehlung: Entweder `_articleService` als Feld speichern und für zukünftige `Load`-Methoden vormerken, oder den Parameter entfernen.

### `src/Reporter.Tests/UnitTest1.cs`

- **Testqualität** — Der einzige Test ist leer. `Reporter.Tests` enthält aktuell keine aussagekräftige Unit- oder Integrationstestabdeckung für `Reporter.Core` oder `Reporter.Data`.
  - Empfehlung: Mindestens einen Test hinzufügen, der `ArticleService.GetUnreadAsync()` bzw. `ArticleRepository.GetArticlesAsync()` aufruft und ein leeres Ergebnis prüft, um die Verkabelung zu validieren.

### `src/Reporter/ViewModels/UngelesenViewModel.cs`, `FeedsViewModel.cs`, `SpaeterViewModel.cs`, `EinstellungenViewModel.cs`

- **MVVM-Source-Generator / AOT-Kompatibilität** — `[ObservableProperty]` wird auf Feldern (z. B. `private string _title;`) anstatt auf `partial`-Properties verwendet. Das erzeugt Build-Warnungen `MVVMTK0045` und kann unter WinUI/AOT zu Marshalling-Problemen führen.
  - Empfehlung: Klassen als `partial` deklarieren und `[ObservableProperty]` auf `public partial string Title { get; set; } = ...;` anwenden.

### `src/Reporter/Resources/Strings/AppResources.resx` und `AppResources.de.resx`

- **Plattformportabilität** — Die `resheader`-Einträge `reader` und `writer` verweisen auf `System.Windows.Forms`, was auf mobilen Plattformen nicht verfügbar ist.
  - Empfehlung: Header auf `System.Resources.ResXResourceReader, System.Resources.Extensions, ...` bzw. den passenden Assembly-Namen anpassen oder zu `IStringLocalizer` migrieren.

### `src/Reporter/Views/UngelesenPage.xaml`, `FeedsPage.xaml`, `SpaeterPage.xaml`, `EinstellungenPage.xaml`

- **Lokalisierung** — Die Platzhalter-Labels enthalten hardcodierte deutsche Texte (z. B. "Hier erscheinen ungelesene Artikel."). Diese sollten später durch lokalisierbare Bindings ersetzt werden.
  - Empfehlung: Als bewusster Scaffold-Platzhalter akzeptabel; im nächsten Feature-Schritt auf `{x:Static ...}` oder `IStringLocalizer`-Binding umstellen.

## Geprüfte Dateien

- `Reporter.sln`
- `src/Reporter/Reporter.csproj`
- `src/Reporter/App.xaml`
- `src/Reporter/App.xaml.cs`
- `src/Reporter/AppShell.xaml`
- `src/Reporter/AppShell.xaml.cs`
- `src/Reporter/MauiProgram.cs`
- `src/Reporter/Resources/Strings/AppResources.resx`
- `src/Reporter/Resources/Strings/AppResources.de.resx`
- `src/Reporter/Resources/Styles/Colors.xaml`
- `src/Reporter/Resources/Styles/Styles.xaml`
- `src/Reporter/ViewModels/BaseViewModel.cs`
- `src/Reporter/ViewModels/UngelesenViewModel.cs`
- `src/Reporter/ViewModels/FeedsViewModel.cs`
- `src/Reporter/ViewModels/SpaeterViewModel.cs`
- `src/Reporter/ViewModels/EinstellungenViewModel.cs`
- `src/Reporter/Views/UngelesenPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/SpaeterPage.xaml`
- `src/Reporter/Views/EinstellungenPage.xaml`
- `src/Reporter/Views/UngelesenPage.xaml.cs`
- `src/Reporter/Views/FeedsPage.xaml.cs`
- `src/Reporter/Views/SpaeterPage.xaml.cs`
- `src/Reporter/Views/EinstellungenPage.xaml.cs`
- `src/Reporter.Core/Reporter.Core.csproj`
- `src/Reporter.Core/Models/Article.cs`
- `src/Reporter.Core/Interfaces/IArticleRepository.cs`
- `src/Reporter.Core/Services/IArticleService.cs`
- `src/Reporter.Core/Services/ArticleService.cs`
- `src/Reporter.Data/Reporter.Data.csproj`
- `src/Reporter.Data/Repositories/ArticleRepository.cs`
- `src/Reporter.Tests/Reporter.Tests.csproj`
- `src/Reporter.Tests/UnitTest1.cs`

Plattform-Boilerplate unter `src/Reporter/Platforms/` wurde stichprobenartig auf offensichtliche Probleme geprüft; keine Befunde.
