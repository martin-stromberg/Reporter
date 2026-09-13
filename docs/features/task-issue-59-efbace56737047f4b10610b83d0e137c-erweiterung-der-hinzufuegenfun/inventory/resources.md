<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Ressourcen (`AppResources`) — Bestandsaufnahme

Dateien: `src/Reporter.Core/Resources/Strings/AppResources.resx` (Neutral/EN), `AppResources.de.resx` (DE), generierte `AppResources.Designer.cs`. Zugriff aus XAML via `x:Static strings:AppResources.*`, aus Code via `AppResources.*`.

## Vorhandene, für die Anforderung relevante Schlüssel

| Schlüssel | EN | DE | Verwendung |
|-----------|-----|-----|------------|
| `PageTitleFeeds` | Feeds | Feeds | Seitentitel `FeedsPage` |
| `PlaceholderFeedSearch` | Feed URL or website address… | Feed-URL oder Website-Adresse… | `Entry NewUrl` |
| `PlaceholderFeedTitle` | Display title | Anzeigetitel | `Entry NewTitle` (entfällt laut Anforderung) |
| `LabelFeedCategory` | Category | Kategorie | `Picker`-Titel |
| `FeedNotificationsLabel` / `FeedNotificationsHint` | Notifications / Notify about new articles… | Benachrichtigungen / Bei neuen Artikeln… | Switch-Beschriftung |
| `NotificationsIosOnlyHint` | Notifications are currently only available on iOS. | Benachrichtigungen sind derzeit nur auf iOS verfügbar. | Hinweis-`Border` |
| `OfflineHint` | No internet connection. | Keine Internetverbindung. | Offline-Banner, `SyncResult` offline |
| `FeedSearchOfflineHint` | Feed search is not available offline — you can still add a URL directly. | Feed-Suche offline nicht verfügbar — URL direkt hinzufügen bleibt möglich. | Hinweis unter dem Formular |
| `ButtonSearch` / `ButtonSave` | Search / Save | Suchen / Speichern | Formular-Buttons (`Save` entfällt) |
| `ButtonCloseSearchResults` | Back to my feeds | Zurück zu meinen Feeds | Trefferansicht schließen |
| `FeedSearchNoResults` | No feeds found. | Keine Feeds gefunden. | `EmptyView` der Trefferliste |
| `FeedSearchNoResultsTitle` | No feed found | Kein Feed gefunden | Titel `ConfirmDirectAddAsync`-Dialog |
| `FeedSearchNoResultsAddUrl` | No feed found. Do you want to add "{0}" directly? | Kein Feed gefunden. Möchtest du die Adresse „{0}" direkt hinzufügen? | Text `ConfirmDirectAddAsync`-Dialog (Platzhalter `{0}` = URL) |
| `FeedSearchAttribution` | Search powered by feedsearch.dev | Suche powered by feedsearch.dev | Attribution unter Trefferliste |
| `FeedSearchUnavailable` | Feed search is unavailable. You can add the URL directly. | Die Feed-Suche ist nicht erreichbar. Du kannst die URL direkt hinzufügen. | `SearchErrorMessage` bei direkter URL |
| `FeedSearchUnavailableRetry` | Feed search is unavailable. Please try again later or enter a complete feed URL. | Die Feed-Suche ist nicht erreichbar. Bitte versuche es später erneut oder gib eine vollständige Feed-URL ein. | `SearchErrorMessage` bei Domain-Eingabe |
| `ActionSheetTitleFeed` | Feed actions | Feed-Aktionen | Titel des `OnFeedTapped`-ActionSheets |
| `ButtonRefresh` / `ButtonEdit` / `ButtonDelete` / `ButtonCancel` | Refresh / Edit / Delete / Cancel | Aktualisieren / Bearbeiten / Löschen / Abbrechen | ActionSheet-Einträge |
| `ButtonYes` / `ButtonNo` | Yes / No | Ja / Nein | Bestätigungsdialoge |
| `ConfirmDeleteFeedTitle` / `ConfirmDeleteFeedMessage` | Delete feed? / All articles belonging to this feed will also be deleted. Continue? | Feed löschen? / Alle zugehörigen Artikel werden ebenfalls gelöscht. Fortfahren? | Lösch-Bestätigung |
| `ConfirmSubscribeFeedTitle` / `ConfirmSubscribeFeedMessage` | Subscribe to feed? / Subscribe to "{0}"? | Feed abonnieren? / „{0}" abonnieren? | Treffer-Abonnement-Dialog |
| `ErrorFeedUrlInvalid` | Please enter a valid feed URL. | Bitte gib eine gültige Feed-URL ein. | `SaveAsync`-Validierung |
| `ErrorFeedTitleEmpty` | Please enter a display title. | Bitte gib einen Anzeigetitel ein. | `SaveAsync`-Validierung (entfällt mit `NewTitle`) |
| `ErrorFeedDuplicate` | A feed with this URL already exists. | Ein Feed mit dieser URL existiert bereits. | Dublettenfehler |
| `CategoryNone` | „—" (em dash) | „—" | Pseudo-Kategorie `Guid.Empty` in `LoadAsync` |
| `SyncStatusError` | Synchronization failed. | Synchronisation fehlgeschlagen. | `SyncErrorMessage` |
| `LabelFeedUnreadCount` | Unread | Ungelesen | Meta-Zeile Listeneintrag |
| `PlaceholderFeeds` | Feeds will appear here. | Hier erscheinen Feeds. | `EmptyView` Feed-Liste |
| `HealthStatusOkLabel` / `HealthStatusWarningLabel` / `HealthStatusErrorLabel` | OK / Warning / Error | In Ordnung / Warnung / Fehler | Status-Text-Trigger |

## Fehlende Schlüssel (nach Anforderung benötigt, nicht vorhanden)

Geprüft per Volltextsuche in `AppResources.resx`/`AppResources.de.resx` und Code (`ButtonRename|ButtonChangeCategory|Rename|ChangeCategory` — keine Treffer außerhalb der Anforderungsdoku):

- `ButtonRename` („Umbenennen") — neuer ActionSheet-Eintrag
- `ButtonChangeCategory` („Kategorie ändern") — neuer ActionSheet-Eintrag
- Beschriftung „+"-Button, z. B. `ActionAddFeed` bzw. „Feed per URL hinzufügen" (Design-Entwurf `btn-open-add-feed`)
- Titel/Dialogtexte für „Umbenennen" (Vorbelegung aktueller Titel) und „Kategorie ändern" (Kategorienliste inkl. `CategoryNone`), ggf. `FeedAddSheetTitle` für das Bottom-Sheet

Die Designer-Datei (`AppResources.Designer.cs`) wird aus den resx-Dateien generiert und muss nach dem Hinzufügen neuer Schlüssel regeneriert werden.
