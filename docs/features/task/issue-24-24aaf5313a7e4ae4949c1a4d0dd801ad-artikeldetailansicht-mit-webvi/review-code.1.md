# Code-Review: Issue #24 – Artikeldetailansicht mit WebView

## Status

**Befunde vorhanden**

## Build- und Teststatus

- `dotnet build Reporter.sln`: **Erfolgreich** (0 Warnungen, 0 Fehler)
- Tests: Nicht separat ausgeführt; es sind keine neuen Unit-Tests für `ArticleDetailViewModel`/`ArticleDetailPage` erkennbar.

## Nichtbefund: RaiseUiActionRequested

Im Review-Umfang sind keine `RaiseUiActionRequested`-Aufrufe in Blazor-Komponenten vorhanden, die einen Handler erfordern würden.

## Mobile-UI-Design-Zusammenfassung

- `ArticleDetailPage` als `Grid` mit `RowDefinitions="Auto,Auto,*,Auto,Auto"`; der `WebView` füllt die verbleibende Höhe (`*`)
  und übernimmt das Scrolling nativ. Keine verschachtelten `CollectionView`/`ScrollView`.
- Touch-Ziele der Floating Bottom Action Bar und des `Switch` sind explizit 44 × 44 pt (`WidthRequest="44"` / `HeightRequest="44"` bzw. `MinimumHeightRequest="44"` / `MinimumWidthRequest="44"`).
- Farben und Symbole nutzen durchgehend `AppThemeBinding` (`Light...` / `Dark...`).
- `WebView`-Inhalte verwenden `color-scheme: light dark` und `prefers-color-scheme` CSS für den Dark Mode.
- Dokumentation in `docs/help/anwendung/mobile-ui-design.md` für Issue #24 vorhanden.

## Befunde

| Datei | Zeile | Kategorie | Befund | Empfehlung |
|-------|-------|-----------|--------|------------|
| `src/Reporter/ViewModels/ArticleDetailViewModel.cs` | 208–212 | Fehlerbehandlung | `catch { }` fängt alle Exceptions in `LoadAsync` still und ohne Logging. | Mindestens Logging (`ILogger`) oder eine User-Feedback-Meldung; spezifische Exception-Typen fangen. |
| `src/Reporter/ViewModels/ArticleDetailViewModel.cs` | 296–317 | Ressourcen / Threading | `CancellationTokenSource` in `MarkReadDelayedAsync` wird neu erzeugt, aber nie `Dispose()` aufgerufen. | `using` oder explizites `Dispose()` beim Beenden/ersetzen der CTS verwenden. |
| `src/Reporter/ViewModels/ArticleDetailViewModel.cs` | 282–289 | Threading / Architektur | Beim Verlassen der Seite wird der Auto-Mark-Timer nicht abgebrochen; `MarkReadDelayedAsync` läuft weiter. | `OnDisappearing` im `ContentPage` einfügen und `AutoMarkRead` bzw. `_autoMarkCts` canceln, oder `IDisposable` im ViewModel implementieren. |
| `src/Reporter/ViewModels/ArticleDetailViewModel.cs` | 228–279 | Sicherheit / Lesbarkeit | `RebuildHtml` fügt `Item.ContentHtml` ungefiltert in den HTML-Body ein. | HTML sanitizer oder CSP-Header im WebView in Betracht ziehen, um XSS-Angriffe per schädlichem Feed zu minimieren. |
| `src/Reporter/ViewModels/ArticleDetailViewModel.cs` | 366–384 | Lesbarkeit | `IncreaseFontSizeCommand` und `DecreaseFontSizeCommand` sind öffentlich, werden aber in der UI nicht verwendet. | Entweder entfernen oder in `ArticleDetailPage.xaml` echten Steuerungen zuweisen. |
| `src/Reporter/Views/ArticleDetailPage.xaml` | 105–109 | Mobile UI | Der `"Öffnen"`-Button im Source-Footer hat keine `MinimumHeightRequest`/`MinimumWidthRequest`. | `MinimumHeightRequest="44"` `MinimumWidthRequest="44"` setzen, um 44 × 44 pt Touch-Ziel zu garantieren. |
| `src/Reporter/Views/ArticleDetailPage.xaml.cs` | 36–44 | Architektur | `ArticleDetailViewModel` wird über `Application.Current?.Handler?.MauiContext?.Services` aufgelöst. | `ArticleDetailPage` in `MauiProgram.cs` als `AddTransient<ArticleDetailPage>()` registrieren und `ArticleDetailViewModel` via Konstruktor-Injection erhalten. |
| `src/Reporter/Views/ArticleDetailPage.xaml.cs` | 44 | Threading | `LoadAsync` wird als Fire-and-Forget (`_ = ...`) gestartet. | Aufgabe `await`en oder ein explizites Fehlerhandling für den Fire-and-Forget-Aufruf ergänzen. |
| `src/Reporter/Views/UnreadPage.xaml` | 70–96 | Mobile UI | Die "Alles gelesen"-Pill (`Border Padding="12,8"`) hat kein explizites `MinimumHeightRequest`/`MinimumWidthRequest` und kann unter 44 pt fallen. | `MinimumHeightRequest="44"` `MinimumWidthRequest="44"` am `Border` ergänzen. |
| `src/Reporter/Views/UnreadPage.xaml.cs` | 22, 67–75 | Fehlerbehandlung / Architektur | `OpenArticleAsync` ist `async void` und wird in einem `Command` verwendet; unbehandelte Ausnahmen können die App zum Absturz bringen. | `OpenArticleAsync` in `async Task OpenArticleAsync(...)` ändern und mit `try/catch` sichern, oder `AsyncCommand` verwenden. |
| `src/Reporter/Views/UnreadPage.xaml.cs` | 31–39 | Fehlerbehandlung | `OnAppearing` ruft `await viewModel.LoadCommand.ExecuteAsync(null)`; unbehandelte Fehler können in `async void` nicht gefangen werden. | `try/catch` um den Aufruf legen. |
| `src/Reporter/MauiProgram.cs` | 56–62 | Architektur | `ArticleDetailPage` ist nicht im DI-Container registriert, obwohl die Seite per `Shell`-Route navigiert wird. | Konsistent zu den anderen Pages `AddTransient<ArticleDetailPage>()` ergänzen, falls DI gewünscht ist. |

## Anmerkungen

- XML-Dokumentation ist in allen geprüften C#-Dateien vorhanden und vollständig für öffentliche Members.
- Keine horizontalen Daten-Tabellen oder mehreren Text-Buttons pro Zeile in den mobilen Ansichten.
- `AppShell.xaml.cs` registriert die Route `articledetail` korrekt.
