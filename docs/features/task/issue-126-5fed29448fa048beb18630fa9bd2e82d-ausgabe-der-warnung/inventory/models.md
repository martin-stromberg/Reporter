<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenmodell — Bestandsaufnahme

Betroffene Datenmodellklassen für die Anforderung „Ausgabe der Warnung" (Issue #126). Keine Warnungs-Felder existieren bisher; der Stand zeigt nur den Fehler-Pendant-Mechanismus (`LastErrorKind`/`LastErrorMessage`).

## `Feed` (Entity)

Datei: `src/Reporter.Data/Entities/Feed.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel. |
| `Url` | `string` | Feed-URL (Pflicht, max. 2048, Unique-Index). |
| `Title` | `string` | Feed-Titel (Pflicht, max. 500). |
| `CategoryId` | `Guid?` | Optionale Kategorie-Zuordnung (FK `categories`, `SetNull` beim Löschen). |
| `LastCheckedAt` | `DateTime?` | Zeitpunkt des letzten Abrufs. |
| `HealthStatus` | `string?` | Gesundheitsstatus (`FeedHealth`-Konstante, max. 50). |
| `HealthLastChange` | `DateTime?` | Zeitpunkt der letzten Statusänderung. |
| `NotificationsEnabled` | `bool` | Benachrichtigungen aktiv (Default `true`). |
| `FaviconUrl` | `string?` | Favicon-URL der Feed-Website (max. 2048). |
| `LastErrorKind` | `string?` | Kategorie des letzten Sync-Fehlers (`FeedSyncErrorKind`-Wert, max. 50), `null` bei Erfolg. |
| `LastErrorMessage` | `string?` | Technische Meldung des letzten Sync-Fehlers, `null` bei Erfolg. |
| `Category` | `Category?` | Navigation zur Kategorie. |

Keine `LastWarning*`-Eigenschaften vorhanden. EF-Core-Mapping in `ReporterDbContext.ConfigureFeed` (`src/Reporter.Data/ReporterDbContext.cs`, Zeilen 84–102); Spalten `last_error_kind`/`last_error_message` wurden per Migration `20260914183510_AddFeedLastError` (`src/Reporter.Data/Migrations/20260914183510_AddFeedLastError.cs`) hinzugefügt und stehen im `ReporterDbContextModelSnapshot` (Zeilen 108–115).

## `Feed` (Domänenmodell)

Datei: `src/Reporter.Core/Models/Feed.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Feed-ID. |
| `Url` | `string` (required, init) | Feed-URL. |
| `Title` | `string` (required, init) | Feed-Titel. |
| `CategoryId` | `Guid?` (init) | Optionale Kategorie-ID. |
| `LastCheckedAt` | `DateTime?` (init) | Letzter Abruf. |
| `HealthStatus` | `string?` (init) | Gesundheitsstatus (`FeedHealth`-Wert). |
| `HealthLastChange` | `DateTime?` (init) | Letzte Statusänderung. |
| `NotificationsEnabled` | `bool` (required, init) | Benachrichtigungen aktiv. |
| `FaviconUrl` | `string?` (init) | Favicon-URL. |
| `LastErrorKind` | `string?` (init) | `FeedSyncErrorKind`-Wert des letzten Fehlers. |
| `LastErrorMessage` | `string?` (init) | Technische Fehlermeldung. |

Keine `LastWarning*`-Eigenschaften vorhanden.

## `FeedListItem`

Datei: `src/Reporter.Core/Models/FeedListItem.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Feed-ID. |
| `Title` | `string` (required, init) | Anzeigetitel. |
| `Url` | `string` (required, init) | Feed-URL. |
| `CategoryId` | `Guid?` (init) | Kategorie-ID. |
| `CategoryName` | `string?` (init) | Anzeigename der Kategorie. |
| `LastCheckedAt` | `DateTime?` (init) | Letzter Abruf. |
| `HealthStatus` | `string?` (init) | Gesundheitsstatus — steuert das Status-Badge auf `FeedsPage` und `FeedDetailPage` per `DataTrigger`. |
| `HealthLastChange` | `DateTime?` (init) | Letzte Statusänderung. |
| `UnreadCount` | `int` (required, init) | Anzahl ungelesener Artikel. |
| `NotificationsEnabled` | `bool` (required, init) | Benachrichtigungen aktiv. |
| `FaviconUrl` | `string?` (init) | Favicon-URL. |
| `LastErrorKind` | `string?` (init) | `FeedSyncErrorKind`-Wert des letzten Fehlers — wird von `FeedDetailViewModel.GetFeedErrorMessage` auf `FeedErrorKind*`-Ressourcen gemappt. |
| `LastErrorMessage` | `string?` (init) | Technische Fehlermeldung — zweiter Absatz im Fehlerdetails-Dialog. |
| `FeedInitial` | `string` (get) | Fallback-Avatar-Initial via `FeedAvatar.Initial(Title)`. |

Keine `LastWarning*`-Eigenschaften vorhanden.

## `SyncLog` (Entity)

Datei: `src/Reporter.Data/Entities/SyncLog.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel. |
| `FeedId` | `Guid?` | Optionaler Feed-Bezug (FK `feeds`, `SetNull` beim Löschen). |
| `StartedAt` | `DateTime?` | Startzeitpunkt. |
| `FinishedAt` | `DateTime?` | Endzeitpunkt. |
| `Status` | `string?` | Sync-Status (`FeedHealth`-Wert, max. 50). |
| `Message` | `string?` | Meldung/Fehlerdetails — hier landet aktuell `"... Health warning triggered."` als einzige Persistenz des Warnungsumstands. |
| `Feed` | `Feed?` | Navigation zum Feed. |

## `SyncLog` (Domänenmodell)

Datei: `src/Reporter.Core/Models/SyncLog.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Log-ID. |
| `FeedId` | `Guid?` (init) | Optionaler Feed-Bezug. |
| `StartedAt` | `DateTime?` (init) | Startzeitpunkt. |
| `FinishedAt` | `DateTime?` (init) | Endzeitpunkt. |
| `Status` | `string?` (init) | Sync-Status. |
| `Message` | `string?` (init) | Meldung/Fehlerdetails. |

## `SyncResult` (Record)

Datei: `src/Reporter.Core/Services/SyncResult.cs`

| Member | Typ | Beschreibung / Zweck |
|--------|-----|----------------------|
| `Status` | `string` | Ergebnis-Gesundheitsstatus (`FeedHealth`-Wert). |
| `NewItems` | `int` | Anzahl neu gespeicherter Artikel. |
| `Message` | `string?` | Optionale Status-/Fehlermeldung (Default `null`). |

Trägt keinen Warnungsgrund; `SyncAllAsync` aggregiert nur `Status`, `NewItems` und `Message`.

## `FeedHealthUpdate` (Record)

Datei: `src/Reporter.Core/Services/FeedHealthUpdate.cs`

| Member | Typ | Beschreibung / Zweck |
|--------|-----|----------------------|
| `ResolvedTitle` | `string?` | Aus dem Feed-Dokument aufgelöster Titel (Default `null`). |
| `FaviconUrl` | `string?` | Beim Sync entdeckte Favicon-URL (Default `null`). |
| `ErrorKind` | `string?` | Klassifizierte Fehlerkategorie; `null` = leeren (Default `null`). |
| `ErrorMessage` | `string?` | Technische Rohmeldung; `null` = leeren (Default `null`). |

Wird von `FeedSyncService.UpdateFeedHealthAsync` in `Feed.LastErrorKind`/`LastErrorMessage` geschrieben — auf dem Erfolgs-/Warnungspfad wird der Record ohne Error-Parameter erzeugt (`new FeedHealthUpdate(resolvedTitle, faviconUrl)`), wodurch die Fehlerfelder auf `null` zurückgesetzt werden. Keine Warnungs-Felder vorhanden.
