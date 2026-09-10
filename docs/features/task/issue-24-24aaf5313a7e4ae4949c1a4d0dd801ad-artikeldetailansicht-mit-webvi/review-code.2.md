# /review-code – Artikeldetailansicht mit WebView

## Review-Status

**Befunde vorhanden** (keine Build-Blocker, ausschließlich Optimierungen/Empfehlungen)

## Build

```
dotnet build /d/Repositories/softwareschmiede/24aaf531-3a7e-4ae4-949c-1a4d0dd801ad/src/Reporter/Reporter.csproj -v:q
```

Ergebnis: **0 Fehler, 0 Warnungen**

## Mobile UI Design Review

- `ArticleDetailPage.xaml` nutzt `Grid` mit `RowDefinitions="Auto,Auto,*,Auto,Auto"` – der `WebView` füllt die verbleibende Höhe (`*`).
- Kein verschachtelter `ScrollView` / `CollectionView`; `WebView` scrollt nativ.
- Touch-Ziele der Floating Bottom Action Bar sind `44 × 44 pt` (`WidthRequest="44"` / `HeightRequest="44"`).
- Der "Im Browser öffnen"-Button und der `Switch` nutzen `MinimumWidthRequest="44"` / `MinimumHeightRequest="44"`.
- Dark Mode verwendet durchgehend `AppThemeBinding` (`Light…` / `Dark…` Ressourcen).
- `mobile-ui-design.md` enthält bereits die UI-Verifizierung für Issue 24 (390 × 844 pt und 768 × 1024 pt).

## DI / Konstruktor-Injection

- `ArticleDetailViewModel` erhält `IItemRepository`, `IFeedRepository`, `ISettingsRepository` über den Konstruktor.
- `ArticleDetailPage` erhält `ArticleDetailViewModel` über den Konstruktor.
- `MauiProgram.cs` registriert `ArticleDetailViewModel` und `ArticleDetailPage` als `Transient`.
- `AppShell` wird mit `IServiceProvider` injiziert.
- `HttpClient` wird als Singleton erzeugt (Timeout 30 s).

## HTML-Sanitization

- `ArticleDetailViewModel.SanitizeHtml` entfernt `<script>`, `<iframe>`, Event-Handler-Attribute (`on…`), und `javascript:`.
- Der generierte WebView-Header setzt ein CSP (`default-src 'none'; style-src 'unsafe-inline'; img-src * data: blob:; script-src 'none';`).
- Siehe Befund 3 – die Regex-basierte Sanitierung ist nicht vollständig.

## Fehlerbehandlung, Threading, Ressourcen-Disposal

- `ArticleDetailPage.OnDisappearing` bricht `CancelAutoMarkRead()` ab.
- `CancellationTokenSource` wird in `MarkReadDelayedAsync` im `finally` block disposed.
- Siehe Befund 1 und 2 hinsichtlich Fire-and-Forget und möglichem Doppel-Disposal.

## RaiseUiActionRequested

Nicht zutreffend – `RaiseUiActionRequested` wird in den geänderten Dateien nicht verwendet.

---

## Befunde

### 1. Fire-and-Forget in `LoadAsync` / `OnAutoMarkReadChanged` ohne Exception-Handling

- **Datei:** `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- **Zeilen:** 242, 349
- **Beschreibung:** `LoadAsync` und `OnAutoMarkReadChanged` starten `_ = MarkReadDelayedAsync(...)` als "Fire-and-Forget"-Task. Sollte `MarkReadDelayedAsync` außerhalb des gecatchten `OperationCanceledException`-Blocks eine Exception werfen (z. B. ein Repository-Aufruf), wird diese nicht beobachtet und kann als `TaskScheduler.UnobservedTaskException` auftreten.
- **Empfehlung:** Innerhalb von `MarkReadDelayedAsync` alle nicht-cancellation-Ausnahmen abfangen (`try/catch`) oder den Task explizit überwachen/awaiten.

### 2. Mögliches Race / Doppel-Disposal des `CancellationTokenSource`

- **Datei:** `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- **Zeilen:** 254–258 (`CancelAutoMarkRead`) und 360–382 (`MarkReadDelayedAsync`)
- **Beschreibung:** `CancelAutoMarkRead` kann `_autoMarkCts` canceln, disposen und auf `null` setzen, während `MarkReadDelayedAsync` denselben lokalen `cts` noch in seinem `finally` disposen will. Der `IsCancellationRequested`-Check nach `Task.Delay` greift ebenfalls auf `cts` zu, nachdem es ggf. bereits disposet wurde.
- **Empfehlung:** Nach `Task.Delay` `cts.Token.ThrowIfCancellationRequested()` verwenden oder vor dem Zugriff prüfen, dass `_autoMarkCts` noch nicht `null`/disposed ist. Den `finally`-Disposal mit `if (_autoMarkCts == cts)` abzusichern, verhindert auch ein Doppel-Disposal.

### 3. Regex-basierte HTML-Sanitierung ist unvollständig

- **Datei:** `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- **Zeilen:** 274–287
- **Beschreibung:** `SanitizeHtml` entfernt zwar `<script>`, `<iframe>`, einfache Event-Handler-Attribute und `javascript:`, lässt aber andere gefährliche Tags/Attribute (z. B. `<object>`, `<embed>`, `<form>`, `<link>`, `<meta>`, `<style>` mit `@import`) sowie kodierte Event-Handler, SVG-onload oder HTML-Kommentare unberührt. Das CSP reduziert das Risiko, ersetzt aber keine robuste Sanitierung.
- **Empfehlung:** Für Produktionscode einen dedizierten HTML-Sanitizer einsetzen (z. B. NuGet `HtmlSanitizer`) oder zumindest eine Whitelist-basierte Filterung ergänzen.

### 4. `RegexOptions.Compiled` potenziell problematisch auf iOS/NativeAOT

- **Datei:** `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- **Zeilen:** 268, 281–285
- **Beschreibung:** Mehrere `Regex`-Aufrufe in `CalculateReadingTime` und `SanitizeHtml` verwenden `RegexOptions.Compiled`. Unter iOS/NativeAOT kann `RegexOptions.Compiled` zur Laufzeit zu `PlatformNotSupportedException` führen. Da das Projekt `net10.0-ios` als Ziel-Framework enthält, ist dies ein Risiko für den iOS-Release-Build.
- **Empfehlung:** `RegexOptions.Compiled` entfernen oder plattformspezifisch prüfen. Die statischen `Regex`-Objekte sollten außerdem idealerweise nur einmalig initialisiert werden, um keine wiederholte Kompilierung pro Artikel zu erzwingen.

---

*Review erstellt am: automatisiert / review-code*
