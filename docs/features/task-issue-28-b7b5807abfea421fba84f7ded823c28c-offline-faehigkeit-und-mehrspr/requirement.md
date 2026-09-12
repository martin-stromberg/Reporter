# Übersetzte Anforderung – Offline-Fähigkeit und Mehrsprachigkeit (Issue #28)

## Fachliche Zusammenfassung

Die App soll ohne Netzwerkverbindung vollständig lesbar bleiben: Artikel-Inhalte, Metadaten, Kategorien und Feeds liegen bereits lokal in der SQLite-Datenbank (`reporter.db`, EF Core), sodass es primär darum geht, die netzwerkabhängigen Abläufe (Feed-Synchronisation, WebView-Navigation, „Im Browser öffnen") offline robust zu gestalten und den Offline-Zustand in der UI sichtbar zu machen. Zusätzlich wird die bereits angelegte RESX-Lokalisierung (`AppResources`, neutral = Englisch, `AppResources.de.resx` = Deutsch) vervollständigt: Alle noch hartcodierten UI-Texte werden in Sprachressourcen überführt, die Sprache folgt der Systemeinstellung (`CurrentUICulture`), ein manueller Sprachwechsel ist optional. Der Synchronisations-Button soll den Offline-Status anzeigen.

## Betroffene Klassen und Komponenten

### Neu zu erstellende Artefakte

- `INetworkStatusService` (Arbeitsname, z. B. `IConnectivityService`) unter `src/Reporter.Core/Interfaces/` — Abstraktion des Netzwerkzustands nach dem bestehenden Gateway-Muster (Interface in `Reporter.Core`, Implementierung im MAUI-Projekt, analog `ILocalNotificationService`/`IAppThemeService`). Erwartete Oberfläche: `bool IsOnline` (oder `NetworkAccess`-Enum) plus Änderungs-Event.
- `NetworkStatusService` unter `src/Reporter/Services/` — Implementierung über `Microsoft.Maui.Networking.IConnectivity` / `Connectivity.Current` inkl. `ConnectivityChanged`-Event.
- Neue Ressourcenschlüssel in `src/Reporter.Core/Resources/Strings/AppResources.resx` (Englisch, neutral) und `AppResources.de.resx` (Deutsch) für alle hartcodierten Texte sowie neue Offline-Hinweise (z. B. `OfflineHint`, `ArticleReadLabel`, `ArticleOpenInBrowser`, `ArticleFullContentAvailable`, `ArticleOfflineLinksDisabled`, `AccessibilityBack`, `AccessibilityFontSize`, `AccessibilityShare`, `SettingsLanguage*` falls manueller Wechsel umgesetzt wird).
- Optional (manueller Sprachwechsel): `LanguageOption` unter `src/Reporter.Core/ViewModels/` analog `ThemeOption`, Konstanten `SettingsValues.LanguageSystem`/`LanguageGerman`/`LanguageEnglish`, neues Feld `Settings.Language` sowie EF-Core-Migration für die Tabelle `settings`.
- Tests in `Reporter.Tests` (xUnit, handgeschriebene Fakes): u. a. `FakeNetworkStatusService`, ViewModel-Tests für das Offline-Verhalten der Refresh-Commands (`UnreadViewModel`/`FeedsViewModel`), ggf. Test für den Link-Deaktivierungs-Pfad in `ArticleDetailViewModel`.

### Zu ändernde Artefakte

- `src/Reporter.Core/ViewModels/UnreadViewModel.cs` — `IsOnline`-Eigenschaft, `RefreshCommand` reagiert auf Offline (CanExecute bzw. kurzer Abbruch mit lokalisiertem Hinweis statt `SyncResult`-Fehlertext).
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs` — analog für `RefreshCommand`/`RefreshAllCommand`.
- `src/Reporter/Views/UnreadPage.xaml` — Offline-Status am Sync-Button (Border/Path mit `TapGestureRecognizer Command="{Binding RefreshCommand}"`, Zeilen 21–41): z. B. deaktiviertes Icon, geänderte Farbe oder zusätzlicher Hinweis-Label.
- `src/Reporter/Views/FeedsPage.xaml` — Offline-Hinweis am `RefreshView`/`RefreshAllCommand` (Pull-to-Refresh), falls im Scope.
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs` — hartcodierte deutsche Texte ersetzen: `BookmarkButtonLabel` (Zeile 191), `MarkAsReadButtonLabel` (Zeile 196), `CalculateReadingTime` („Min. Lesezeit", Zeile 320); Offline-Zustand berücksichtigen (Dependency auf `INetworkStatusService`, Rebuild von `HtmlSource` bei Statuswechsel).
- `src/Reporter/Views/ArticleDetailPage.xaml` — hartcodierte Texte ersetzen: „Gelesen" (Zeile 26), „Vollständiger Artikel verfügbar" (Zeile 128), „Im Browser öffnen" (Zeile 139), `SemanticProperties.Description` „Zurück"/„Schriftgröße wechseln"/„Teilen"/„Im Browser öffnen" (Zeilen 160, 212, 257, 280), `TargetNullValue='Artikel'` (Zeile 9); `Navigating`-Handler am `ArticleWebView` (Zeile 108 ff.) zum Unterbinden der Link-Navigation im Offline-Modus.
- `src/Reporter/Views/ArticleDetailPage.xaml.cs` — `Navigating`-Event-Handler: Navigation abbrechen (`e.Cancel = true`), wenn offline; alternativ Hinweis anzeigen.
- `src/Reporter.Core/Services/FeedSyncService.cs` — keine Abstürze bei fehlendem Netz sicherstellen (Exceptions werden bereits zu `SyncResult(FeedHealth.Error, …)` gefangen, Zeilen 68–74); ggf. frühzeitiger Offline-Abbruch. Hinweis: Die persistierten `SyncLog`-Meldungen sind aktuell englisch hartcodiert.
- `src/Reporter.Core/Services/AutoRefreshService.cs` — periodischen Sync bei Offline überspringen, um Fehler-Rauschen im `SyncLog` zu vermeiden.
- `src/Reporter/MauiProgram.cs` — DI-Registrierung des neuen Services (`AddSingleton`).
- `src/Reporter/App.xaml.cs` — beim Start ggf. persistierte Sprache anwenden (analog `IAppThemeService.ApplyTheme`, Zeilen 51–62) und Connectivity-Monitoring initialisieren.
- `src/Reporter/AppShell.xaml.cs` — Tab-Titel nutzen bereits `AppResources` (Zeilen 22–35); bei manuellem Sprachwechsel ggf. Neuaufbau nötig.
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`, `src/Reporter/Views/SettingsPage.xaml`, `src/Reporter.Core/Models/Settings.cs`, `src/Reporter.Core/Models/SettingsValues.cs` — nur bei optionalem manuellem Sprachwechsel (neue Sektion/Picker nach dem `Theme`/`ThemeOption`-Muster).
- `src/Reporter.Data` — EF-Core-Migration für `Settings.Language`, falls umgesetzt.

### Bereits abgedeckt / nicht betroffen

- Lesepfade auf Artikel, Kategorien und Feeds sind bereits vollständig lokal (Repositories über `IDbContextFactory<ReporterDbContext>`); die Akzeptanzkriterium „zeigt synchronisierte Artikel ohne Netzwerk" ist damit weitgehend erfüllt und vor allem zu verifizieren.
- Großteil der Seiten ist bereits an `AppResources` gebunden (`{x:Static strings:AppResources.*}` in XAML, `AppResources.*` in Code-Behind, z. B. `CategoriesPage.xaml.cs`, `FeedsPage.xaml.cs`).

## Implementierungsansatz

- **Offline-Erkennung:** Neuer Gateway-Service `INetworkStatusService` in `Reporter.Core`, Implementierung im MAUI-Projekt über `Connectivity.Current` und `ConnectivityChanged`. Registrierung in `MauiProgram.CreateMauiApp()` (Zeilen 42–70). ViewModels injizieren den Service, exponieren `IsOnline` und aktualisieren sich über das Änderungs-Event (Basis: `BaseViewModel`/`CommunityToolkit.Mvvm`).
- **Sync-Button mit Offline-Status:** `UnreadViewModel.RefreshCommand` (und `FeedsViewModel.RefreshCommand`/`RefreshAllCommand`) erhalten eine Offline-Bedingung — entweder `CanExecute` erweitern (Button deaktiviert + visueller Zustand über `DataTrigger`) oder bei Tap eine lokalisierte Offline-Meldung über `ErrorMessage` ausgeben. `AutoRefreshService` fragt `IsOnline` vor jedem `PeriodicTimer`-Tick ab und überspringt den Sync.
- **WebView-Links offline deaktivieren:** `ArticleDetailPage` behandelt das `WebView.Navigating`-Event: Im Offline-Zustand wird jede Navigation abgebrochen (`e.Cancel = true`) oder durch einen lokalisierten Hinweis ersetzt (Anforderung lässt beide Varianten zu). Alternativ/ergänzend kann `ArticleDetailViewModel.RebuildHtml()` (Zeilen 348–400) im Offline-Modus `href`-Attribute entfernen bzw. `<a>`-Tags neutralisieren — die vorhandene `SanitizeHtml`-Regex-Pipeline (Zeilen 329–346) ist der dafür vorgesehene Erweiterungspunkt. `OpenInBrowserCommand`/`ShareAsync` bleiben davon zunächst unberührt (siehe Offene Fragen).
- **Lokalisierung vervollständigen:** Alle verbleibenden Literal-Strings (siehe Liste oben) werden als Schlüssel in `AppResources.resx` (EN) und `AppResources.de.resx` (DE) angelegt und per `{x:Static strings:AppResources.*}` bzw. `AppResources.*` gebunden. Formatierbare Texte folgen dem bestehenden `string.Format(CultureInfo.CurrentCulture, AppResources.*Format, …)`-Muster. Der Git-Hook `translation-check.py` erzwingt DE/EN-Konsistenz.
- **Sprachwechsel gemäß System:** Neutrale RESX = Englisch, deutsche Satellitenassembly via `AppResources.de.resx`; `ResourceManager` wählt anhand `CultureInfo.CurrentUICulture` automatisch — zu verifizieren, dass auf Windows und iOS die Systemsprache korrekt greift und Englisch als Fallback für andere Sprachen dient.
- **Optionaler manueller Wechsel:** `Settings.Language` (`"system"`/`"de"`/`"en"`) im `settings`-Singleton, Picker in `SettingsPage` nach `ThemeOption`-Muster, Anwendung in `App.OnStart` über `AppResources.Culture` / `CultureInfo.CurrentUICulture` — analog zum Theme-Apply-Block (Zeilen 51–62). Da `{x:Static}`-Bindungen einmalig auflösen, ist ein App-Neustart oder Neuaufbau der Shell-Seiten für eine wirksame Umschaltung nötig (zu klären).
- **Crash-Freiheit:** `FeedSyncService` fängt Netzwerkfehler bereits in `SyncResult` mit `FeedHealth.Error` ab; `App.OnStart` und `NotificationService`-Aufrufe sind try/catch-geschützt. Zu prüfen sind restliche unbehandelte Pfade (z. B. `Browser.OpenAsync` bei Offline).

## Konfiguration

- **Offline-Verhalten:** nicht konfigurierbar — immer aktiv, rein zustandsabhängig vom Netzwerkstatus.
- **Sprache:** Standard „System". Falls der optionale manuelle Wechsel umgesetzt wird: benutzerweite Einstellung `Settings.Language` in der bestehenden `settings`-Tabelle (Singleton, `Settings.DefaultId`), gesteuert über einen neuen Picker auf der **Einstellungen**-Seite — Muster wie `Settings.Theme`/`ThemeOption`/`SettingsValues`.

## Offene Fragen

1. Soll der optionale manuelle Sprachwechsel in diesem Issue umgesetzt oder bewusst zurückgestellt werden? Falls ja: Ist ein erforderlicher App-Neustart nach Sprachwechsel akzeptabel (statische `{x:Static}`-Bindungen und Singleton-ViewModels lösen nicht zur Laufzeit neu auf), oder soll eine Neuinitialisierung der Shell/Seiten implementiert werden?
2. Offline-Link-Behandlung im WebView: Soll die Navigation still abgebrochen werden (Anforderung: „deaktiviert") oder ein lokalisierter Hinweis erscheinen („durch einen Hinweis ersetzt")? Gilt das nur für die Detailansicht oder auch für die `Navigating`-Navigation beim Online-Modus (aktuell navigiert das WebView intern auf Link-Klick)?
3. Sollen die Aktionen **Im Browser öffnen** (`OpenInBrowserCommand`) und **Teilen** (`ShareAsync`) in `ArticleDetailPage` bei Offline ebenfalls deaktiviert bzw. mit Hinweis versehen werden? Die Anforderung nennt explizit nur Links im Artikelinhalt.
4. Welche Sync-Einstiegspunkte benötigen den Offline-Status: nur der Refresh-Button auf **Ungelesen** (`UnreadPage`), oder zusätzlich Pull-to-Refresh/„Alle aktualisieren" auf **Feeds** (`FeedsPage`)? Der Scope-Text „Synchronisations-Button" ist singular formuliert.
5. Die in `SyncLog` persistierten Meldungen (`FeedSyncService`, z. B. „Synchronization failed: …") sind englisch hartcodiert und werden in der DB gespeichert: Sollen sie nachträglich lokalisiert werden (Anzeige-Zeitpunkt) oder bleiben sie bewusst technisch/englisch?
6. Externe Bilder im Artikel-HTML (`img-src *` in der CSP von `RebuildHtml`) laden offline nicht — ist die Darstellung mit fehlenden Bildern akzeptabel, oder sollen `<img>`-Tags offline entfernt werden?
7. Fallback-Sprache: Bestätigung, dass Englisch (neutrale `AppResources.resx`) für alle Systemsprachen außer Deutsch gelten soll.
