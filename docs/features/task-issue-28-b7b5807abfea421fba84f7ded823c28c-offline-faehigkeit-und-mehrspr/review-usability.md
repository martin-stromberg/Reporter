# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### UnreadPage.xaml / UnreadViewModel.cs (Seite „Ungelesen")

- **Erreichbarkeit** — Zieht eine Anwenderin im Offline-Zustand die Artikelliste zum Aktualisieren herunter (Pull-to-Refresh über `RefreshView`, `UnreadPage.xaml` Zeile 131–133), dreht die Ladeanzeige anschließend endlos und stoppt nie. Ursache: `RefreshView.IsRefreshing` ist zweiseitig an `IsSyncing` gebunden; das Ziehen setzt `IsSyncing = true`, aber `RefreshAsync()` bricht bei `!IsOnline` sofort ab (`UnreadViewModel.cs` Zeile 324–328), ohne `IsSyncing` wieder auf `false` zu setzen. Da auch `OnConnectivityChanged()` (Zeile 452–456) nur `ErrorMessage` leert, bleibt der Spinner selbst nach Wiederherstellung der Verbindung hängen — die Seite wirkt „eingefroren", eine Laiin versteht nicht, ob noch gearbeitet wird oder nicht.

  Empfehlung: Im Offline-Abbruchpfad `IsSyncing = false` setzen (bzw. den Abbruch in einen `try/finally`-Block verlagern) und zusätzlich den lokalisierten `AppResources.OfflineHint` als `ErrorMessage` ausgeben, damit der Nutzer eine konkrete Rückmeldung statt einer endlosen Ladeanzeige erhält.

### FeedsPage.xaml / FeedsViewModel.cs (Seite „Feeds")

- **Erreichbarkeit** — Derselbe Fehlerzustand auf der Feeds-Seite: Pull-to-Refresh (`RefreshView` → `RefreshAllCommand`, `FeedsPage.xaml` Zeile 93–95) bei Offline setzt `IsSyncing = true` über die Two-Way-Bindung, `RefreshAllAsync()` kehrt aber sofort zurück (`FeedsViewModel.cs` Zeile 386–389). Zusätzlich verschärft sich der Effekt, weil `RefreshAllCommand.CanExecute = !IsSyncing` ist (Zeile 57): Nach einem einmaligen Pull-to-Refresh im Offline-Zustand bleibt der Aktualisieren-Befehl dauerhaft deaktiviert und der Refresh-Indikator dreht endlos — auch dann noch, wenn das Gerät wieder online ist (`OnConnectivityChanged`, Zeile 417–420, setzt nur `SyncErrorMessage` zurück). Auch der pro Feed ausgelöste `RefreshCommand` (Aktionsmenü) schweigt offline kommentarlos.

  Empfehlung: Wie auf „Ungelesen": Beim Offline-Abbruch `IsSyncing = false` setzen und den lokalisierten `OfflineHint` als `SyncErrorMessage` anzeigen (das dafür bereits gebaute, rot eingefärbte `SyncErrorMessage`-Label liegt direkt über der Liste).

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Artikel, Feeds, Kategorien und „Später lesen"-Liste offline lesen → unauffällig (alle Daten lokal, Vorschaubilder werden offline ausgeblendet, Bilder im Artikel-HTML entfernt)
- Offline-Zustand in der UI erkennen → unauffällig (lokalisierte Hinweis-Boxen auf Ungelesen, Feeds, Später und Artikeldetail)
- Synchronisation per Sync-Button auslösen (Tipp auf das Icon bei Offline) → unauffällig (Button sichtbar abgedunkelt, Offline-Hinweis eingeblendet — von der Anforderung zugelassene Variante)
- Synchronisation per Pull-to-Refresh auslösen (Ungelesen + Feeds) → Befund vorhanden (endlose Ladeanzeige, siehe oben)
- Link im Artikelinhalt bei Offline antippen → unauffällig (Navigation wird abgebrochen, lokalisierter Hinweis-Dialog; Links werden offline zusätzlich aus dem HTML entfernt)
- „Im Browser öffnen" bei Offline antippen → unauffällig (lokalisierte Fehlermeldung „Keine Internetverbindung.")
- „Teilen" bei Offline antippen → unauffällig (nativer Teilen-Dialog funktioniert lokal, von der Anforderung ausdrücklich nicht gefordert)
- Alle sichtbaren Texte in Systemsprache (EN/DE) → unauffällig (alle zuvor hartcodierten Texte inkl. Accessibility-Beschreibungen über `AppResources` mit EN- und DE-Einträgen)
- Manueller Sprachwechsel in den Einstellungen → unauffällig (laut Anforderung optional, bewusst nicht umgesetzt; Sprache folgt der Systemeinstellung)
- Neue Kategorie / neuen Feed offline anlegen → unauffällig (rein lokale Validierung und Speicherung, keine Netzwerkabhängigkeit, Fehlermeldungen lokalisiert)

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `src/Reporter/Views/UnreadPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/LaterPage.xaml`
- `src/Reporter/Views/ArticleDetailPage.xaml`
- `src/Reporter/Views/ArticleDetailPage.xaml.cs`
- `src/Reporter/Views/ArticleCardView.xaml`
- `src/Reporter/Views/ArticleCardView.xaml.cs`
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter.Core/ViewModels/BaseViewModel.cs`
- `src/Reporter.Core/ViewModels/UnreadViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/LaterViewModel.cs`
- `src/Reporter.Core/ViewModels/CategoriesViewModel.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Interfaces/INetworkStatusService.cs`
- `src/Reporter.Core/Services/ArticleHtmlSanitizer.cs`
- `src/Reporter.Core/Services/WebViewNavigationGuard.cs`
- `src/Reporter/Services/NetworkStatusService.cs`
- `src/Reporter.Core/Services/FeedSyncService.cs` (nur soweit UI-sichtbare `SyncResult`-Meldungen betroffen)
- `src/Reporter.Core/Services/AutoRefreshService.cs` (kein direkter UI-Eingriff, auf Offline-Überspringen geprüft)
- `src/Reporter/App.xaml.cs` / `src/Reporter/MauiProgram.cs` (DI-/Startverdrahtung, kein UI)

Hinweis zur Mobile-Prüfung (AGENTS.md): Alle neuen bzw. geänderten Bedienelemente halten die 44×44-pt-Touch-Targets ein, sämtliche neuen Oberflächenfarben nutzen `AppThemeBinding`, es wurden keine verschachtelten `CollectionView`/`ScrollView` und keine Text-Button-Reihen eingeführt; die neuen Hinweis-Boxen sind kompatibel mit 390×844 pt.
