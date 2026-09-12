<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Feed-Suche über RSS Atlas statt reiner URL-Eingabe

Bestandsaufnahme des Hinzufügen-/Feed-Verwaltungsbereichs (`FeedsPage`/`FeedsViewModel`, Repositories, Sync-Service, DI-Registrierung, Ressourcen und Tests) bezogen auf die Anforderung in `requirement.md` (Issue #59).

## Zusammenfassung

- Der bisherige Hinzufügen-Pfad existiert vollständig: `FeedsViewModel` mit `NewUrl`/`NewTitle`/`SelectedCategory`/`FeedNotificationsEnabled` und `SaveCommand` → `SaveAsync` inkl. URL-Validierung (`Uri.TryCreate`, nur http/https), Titel-Pflicht und Dublettenprüfung über `IFeedRepository.GetByUrlAsync`.
- `Feed` besitzt kein Beschreibungsfeld; `Category` besteht nur aus `Id`/`Name`. Ein Modell für Suchtreffer (`FeedSearchResult`) existiert nicht.
- Es gibt weder ein `IFeedSearchService`-Interface noch einen RSS-Atlas-/Such-Service im Code; `HttpClient` ist bereits als Singleton mit 30-s-Timeout in `MauiProgram` registriert und wird aktuell nur von `FeedSyncService` genutzt.
- Offline-Infrastruktur ist vorhanden: `BaseViewModel.IsOnline`/`TrackConnectivity`/`OnConnectivityChanged` über `INetworkStatusService`; `FeedsViewModel` nutzt sie bereits (Sync-Skip + Offline-Banner in `FeedsPage.xaml`).
- Kein Enum für eine Trefferart vorhanden; Health-Status ist als String-Konstanten in der statischen Klasse `FeedHealth` realisiert. Der einzige Enum im Umfeld ist `NotificationAuthorizationStatus`.
- UI: `FeedsPage.xaml` enthält das Formular (Entry `NewUrl`, Entry `NewTitle`, Picker, Notification-Switch, Fehlerlabel, Save-Button) und eine kartenbasierte `CollectionView` mit `TapGestureRecognizer` → `OnFeedTapped` (`DisplayActionSheetAsync`/`DisplayAlertAsync` im Code-Behind). Es existiert keine Trefferliste/-CollectionView für die Suche.
- Lokalisierte Texte in `AppResources.resx`/`AppResources.de.resx` (je 131 Schlüssel, paritätisch); die von der Anforderung genannten neuen Texte (Such-Placeholder, „Kein Feed gefunden…“ etc.) existieren noch nicht.

Test-Ausgangszustand: Alle Tests erfolgreich — `dotnet test src/Reporter.Tests/Reporter.Tests.csproj` (Release): 239 bestanden, 0 fehlgeschlagen, 0 übersprungen; `npm test`: 36 bestanden, 0 fehlgeschlagen. Keine bekannten Fehlschläge. Nachweis und Details: [inventory/tests.md](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md) — `Feed`, `FeedListItem`, `Category`
- [Logik](inventory/logic.md) — `FeedsViewModel`, `BaseViewModel`, `FeedSyncService`, `FeedRepository`, `MauiProgram`, `FeedsPage`-Code-Behind, `FeedHealth`, `SyncResult`
- [Enums](inventory/enums.md) — `NotificationAuthorizationStatus`, `FeedHealth`-Konstanten; keine Trefferart vorhanden
- [Interfaces](inventory/interfaces.md) — `IFeedRepository`, `IFeedSyncService`, `INetworkStatusService`, `ICategoryRepository`, `ILocalNotificationService`
- [UI und Ressourcen](inventory/ui.md) — `FeedsPage.xaml`, `FeedsPage.xaml.cs`, relevante `AppResources`-Schlüssel
- [Tests](inventory/tests.md) — Test-Ausgangszustand, Testklassen und Hilfsmethoden
