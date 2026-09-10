# Review: Artikeldetailansicht mit WebView

## Status

**Befunde vorhanden**

## Build / Warnungen

- `dotnet build --no-restore` (ohne Framework) schlägt für `net10.0-ios` fehl, weil `project.assets.json` kein iOS-Ziel enthält (NETSDK1005).  
- `dotnet build --no-restore --framework net10.0-windows10.0.19041.0` ist erfolgreich: **0 Warnungen, 0 Fehler**.

## Befunde

### Threading
- Keine offensichtlichen Verletzungen. Alle Commands starten aus der UI und `await` erfasst den `SynchronizationContext`.  
- **Hinweis** (`ArticleDetailViewModel.cs:416-417`): `MarkReadDelayedAsync` aktualisiert `Item` nach einem `await`. Sollte das Repository jemals auf einem Nicht-UI-Kontext zurückkehren, wäre `MainThread.BeginInvokeOnMainThread(() => Item = ...)` defensiver.

### Ressourcen-Disposal
- `ArticleDetailViewModel.cs:285-289` (`CancelAutoMarkRead`): Bricht `_autoMarkCts` ab und setzt es auf `null`, ruft aber nicht `_autoMarkCts?.Dispose()` auf. Das `cts` wird zwar im `finally` von `MarkReadDelayedAsync` entsorgt, eine explizite Disposal im Lock wäre sauberer.
- `MauiProgram.cs:49` Ein Singleton-`HttpClient` funktioniert, ist aber in MAUI unkonventionell. Besser `IHttpClientFactory` oder typed clients verwenden, um DNS/Socket-Probleme langfristig zu vermeiden.

### HTML-Sanitization & Regex
- `ArticleDetailViewModel.cs:21-30` & `309-326`: Die Sanitierung basiert ausschließlich auf Regex. Regex kann kein HTML sicher parsen (SVG-`onload`, `data:`-URLs, fehlerhafte `>` in Attributen etc.). Der Kommentar weist darauf hin, aber eine dedizierte Bibliothek (z. B. `HtmlSanitizer`) wäre deutlich robuster.
- `ArticleDetailViewModel.cs:21`: `HtmlTagRegex` (`<[^>]+>`) ist für die Lesezeit nur angenommen, aber fehleranfällig, wenn Inhalte `>`-Zeichen enthalten.
- `ArticleDetailViewModel.cs:337-367`: Das CSS im WebView-HTML verwendet `prefers-color-scheme` und keine hartkodierten Farben – korrekt für Dark Mode.

### Mobile UI (44 × 44, AppThemeBinding)
- Keine Befunde.
- Alle Touch-Targets in `ArticleDetailPage.xaml` und `UnreadPage.xaml` sind mind. `44 × 44` (Bottom-Action-Border 44×44, Switch `MinimumWidthRequest/MinimumHeightRequest="44"`, Mark-all-read `MinimumHeightRequest/MinimumWidthRequest="44"`).
- `AppThemeBinding` ist durchgängig für Farben/Backgrounds verwendet.

### Dependency Injection
- `MauiProgram.cs:56-62`: `ArticleDetailViewModel`, `ArticleDetailPage` und weitere Pages korrekt als Transient / Singleton registriert.
- `AppShell.xaml.cs:20` & `ArticleCardView.xaml.cs:86`: Der Routenname `"articledetail"` ist hartkodiert. Eine zentrale Konstante würde Wartung und Tippfehler reduzieren.

### `RaiseUiActionRequested`
- Nicht im untersuchten Code vorhanden. Keine zutreffende Prüfung notwendig.

## Untersuchte Dateien

- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter/Views/ArticleDetailPage.xaml`
- `src/Reporter/Views/ArticleDetailPage.xaml.cs`
- `src/Reporter/Views/UnreadPage.xaml`
- `src/Reporter/Views/UnreadPage.xaml.cs`
- `src/Reporter/MauiProgram.cs`
- `src/Reporter/AppShell.xaml.cs`
- `src/Reporter/Views/ArticleCardView.xaml.cs`
