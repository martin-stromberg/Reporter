<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenmodell

## `Feed`
Datei: `src/Reporter.Core/Models/Feed.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Eindeutige Feed-ID; wird als `feedId` im `userInfo`-Payload der Zusammenfassungs-Benachrichtigung verwendet (`NotificationService`). |
| `Url` | `string` (required, init) | Feed-URL; Grundlage des HTTP-Abrufs und der Fehlerklassifizierung (`FeedSyncErrorKind`). |
| `Title` | `string` (required, init) | Feed-Titel; wird als Titel der lokalen Mitteilung verwendet. |
| `CategoryId` | `Guid?` (init) | Optionale Kategorie-Zuordnung. |
| `LastCheckedAt` | `DateTime?` (init) | Zeitpunkt des letzten Abrufs; wird von `FeedSyncService.UpdateFeedHealthAsync` gesetzt. |
| `HealthStatus` | `string?` (init) | Aktueller Gesundheitsstatus (`FeedHealth`-Konstanten). |
| `HealthLastChange` | `DateTime?` (init) | Zeitpunkt der letzten Statusänderung. |
| `NotificationsEnabled` | `bool` (required, init) | Feed-spezifischer Benachrichtigungs-Schalter; erste Prüfung in `NotificationService.NotifyNewItemsAsync`. |
| `FaviconUrl` | `string?` (init) | Favicon-URL; wird beim Sync nachgezogen, nicht benachrichtigungsrelevant. |
| `LastErrorKind` | `string?` (init) | Kategorie des letzten Sync-Fehlers (`FeedSyncErrorKind`-Wert). |
| `LastErrorMessage` | `string?` (init) | Technische Meldung des letzten Sync-Fehlers. |

## `Settings`
Datei: `src/Reporter.Core/Models/Settings.cs`

Singleton-Einstellungsdatensatz (`DefaultId`). Benachrichtigungs- und abrufrelevante Eigenschaften:

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `NotificationsEnabled` | `bool` (required, init) | Globaler Benachrichtigungs-Hauptschalter; wird in `NotificationService.NotifyNewItemsAsync` geprüft; das Einschalten löst in `SettingsViewModel` die iOS-Berechtigungsanfrage aus. |
| `QuietHoursStart` | `TimeSpan?` (init) | Beginn der Ruhezeit; `NotificationService.IsQuietHoursActive` unterdrückt Mitteilungen innerhalb des Intervalls. |
| `QuietHoursEnd` | `TimeSpan?` (init) | Ende der Ruhezeit. |
| `AutoRefreshEnabled` | `bool` (required, init) | Schalter für den In-App-Timer (`AutoRefreshService`); möglicher Anknüpfungspunkt für den OS-Hintergrundabruf (laut Anforderung offene Frage). |
| `RefreshIntervalMinutes` | `int` (required, init) | Timer-Intervall (geclamppt auf 1–1440 in `AutoRefreshService`); könnte als `EarliestBeginDate`-Untergrenze dienen. |
| `RefreshOnStartupEnabled` | `bool` (required, init) | Schalter für den einmaligen Start-Abruf in `AutoRefreshService.StartAsync`/`RunStartupSyncAsync`. |
| `NotificationSummaryEnabled` | `bool` (required, init) | Steuert in `NotificationService`, ob eine Zusammenfassungs-Mitteilung pro Feed oder eine Mitteilung pro Item erzeugt wird. |
| `RetentionDays` | `int` (required, init) | Aufbewahrungsfrist für Items (nicht benachrichtigungsrelevant). |
| `AutoMarkReadMode` | `string?` (init) | Auto-Gelesen-Modus (`SettingsValues`-Konstanten). |
| `AutoMarkReadDelaySeconds` | `int` (required, init) | Verzögerung für Auto-Gelesen. |
| `UnreadSortOrder` | `string?` (init) | Sortierrichtung der Ungelesen-Liste (Default `SettingsValues.SortOrderDescending`). |
| `Theme` | `string?` (init) | Erscheinungsbild (Default `SettingsValues.ThemeSystem`). |
| `Language` | `string?` (init) | Sprachwahl (Default `SettingsValues.LanguageSystem`). |
| `DebugCollectionEnabled` | `bool` (required, init) | Schalter für das Session-Debug-Log. |

Persistenz: `src/Reporter.Data/Entities/Settings.cs` (EF-Entity), `src/Reporter.Data/Repositories/SettingsRepository.cs`; Migrationen u. a. `AddSettingsAutoRefreshAndTheme`, `AddSettingsStartupRefreshAndSortOrder`, `AddSettingsNotificationSummary`.

## `Item`
Datei: `src/Reporter.Core/Models/Item.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Item-ID; dient im Einzelmodus als Notification-Identifier und `itemId` im `userInfo`-Payload (Tap-Navigation). |
| `FeedId` | `Guid` (required, init) | Zugehöriger Feed. |
| `Title` | `string` (required, init) | Titel; wird als Mitteilungs-Body verwendet. |
| `Link` | `string?` (init) | Artikel-URL; wird als `link` im `userInfo`-Payload übergeben (Fallback-Öffnung im `NotificationDelegate`). |
| `PublishedAt` | `DateTime?` (init) | Veröffentlichungszeitpunkt. |
| `GuidOrHash` | `string` (required, init) | Dedup-Schlüssel beim Sync. |
| `IsRead` | `bool` (required, init) | Gelesen-Flag. |
| `IsSavedForLater` | `bool` (required, init) | Später-lesen-Flag. |
| `ReadAt` | `DateTime?` (init) | Lesezeitpunkt. |
| `ContentHtml` | `string?` (init) | HTML-Inhalt; geht in den Keyword-Match für Benachrichtigungen ein. |
