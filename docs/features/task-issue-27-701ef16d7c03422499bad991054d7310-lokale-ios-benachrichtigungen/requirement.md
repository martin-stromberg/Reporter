# Übersetzte Anforderung: Lokale iOS-Benachrichtigungen mit Ruhezeiten (Issue #27)

## Fachliche Zusammenfassung

Nach jedem erfolgreichen Feed-Sync (`FeedSyncService.SyncFeedAsync`/`SyncAllAsync`) prüft ein neuer Benachrichtigungs-Service, ob neue `Item`-Datensätze gespeichert wurden, und löst für berechtigte Artikel eine lokale iOS-Benachrichtigung aus. Berechtigt ist ein Artikel nur, wenn der globale Schalter `Settings.NotificationsEnabled` aktiv ist, der Feed die neue Pro-Feed-Einstellung aktiviert hat, die aktuelle lokale Uhrzeit außerhalb der konfigurierten Ruhezeit (`Settings.QuietHoursStart`/`QuietHoursEnd`, inkl. Bereiche über Mitternacht) liegt und der Artikel keinen konfigurierten Keyword-Filter trifft. Pro Artikel wird höchstens eine Benachrichtigung ausgelöst.

## Betroffene Klassen und Komponenten

### Datenmodellklassen

- `Reporter.Core.Models.Feed` — neue Eigenschaft `NotificationsEnabled` (`bool`).
- `Reporter.Data.Entities.Feed` — neue Spalte `notifications_enabled` (`bool`, Default `true` — Annahme, siehe Offene Fragen).
- `Reporter.Data.ReporterDbContext.ConfigureFeed` — Spalten-Mapping für das neue Feld.
- Neue EF-Core-Migration (z. B. `AddFeedNotificationsEnabled`) analog zu `AddSettingsAutoRefreshAndTheme`.
- `Reporter.Core.Models.FeedListItem` — neue Eigenschaft `NotificationsEnabled`, damit der Schalter beim Bearbeiten vorbefüllt werden kann.
- `Reporter.Core.Models.Settings` — **unverändert**: `NotificationsEnabled`, `QuietHoursStart`, `QuietHoursEnd` sind bereits vorhanden und persistiert (Einstellungen-Arbeitspaket).

### Logikklassen / Services

- Neu: `INotificationService` (oder vergleichbar) in `Reporter.Core/Interfaces` — enthält die Entscheidungslogik: Auswertung globaler Schalter, Pro-Feed-Flag, Ruhezeit und Keywords; delegiert die eigentliche Anzeige an den Plattformdienst.
- Neu: `ILocalNotificationService` in `Reporter.Core/Interfaces` — plattformneutrales Interface für die Berechtigungsanfrage und das Anzeigen lokaler Benachrichtigungen.
- Neu: Implementierung in `src/Reporter/Services/` (Muster wie `AppThemeService`) mit iOS-Ausprägung über `UserNotifications.UNUserNotificationCenter`; auf Nicht-iOS-Plattformen No-Op (Android ist explizit optionales Folge-Issue).
- `Reporter.Core.Services.FeedSyncService` — muss die Liste der tatsächlich neu gespeicherten `Item`s bereitstellen; `SyncResult` trägt aktuell nur die Anzahl (`NewItems`). Optionen: `SyncResult` um `IReadOnlyList<Item>` erweitern oder `FeedSyncService` ruft den `INotificationService` am Ende von `RunSyncAsync` direkt mit Feed und neuen Artikeln.
- Wiederverwendete Bausteine: `ISettingsRepository.GetAsync` (Singleton-Einstellungen), `IKeywordRepository.GetAllAsync`, `IKeywordMatcher.MatchesAny` (Teilwort, `OrdinalIgnoreCase`, auf `Title` und `ContentHtml`), `IItemRepository.GetByGuidOrHashAsync` (Grundlage der Dublettenerkennung), `TimeProvider` (bereits etabliertes Muster für testbare Zeitauswertung, vgl. `AutoRefreshService`/`SettingsViewModel`).

### UI-Komponenten

- `Reporter.Core.ViewModels.FeedsViewModel` — neue Property (z. B. `FeedNotificationsEnabled`) im Anlage-/Bearbeitungsformular; Übernahme in `SaveAsync` (Add- und Update-Pfad) sowie Vorbefüllung in `EditAsync`.
- `Reporter.Views.FeedsPage.xaml` — `Switch` in der Feed-Bearbeitungskarte. Mobile-Regeln aus `AGENTS.md` beachten: Touch-Target ≥ 44 × 44 pt, `AppThemeBinding` für Farben, `SemanticProperties.Description` für Barrierefreiheit.
- `Reporter.Views.SettingsPage.xaml` / `SettingsViewModel` — **unverändert**: Karte „Benachrichtigungen & Ruhezeiten" mit globalem Schalter und `TimePicker`-Ruhezeiten existiert bereits.
- `Reporter.Core.Resources.Strings.AppResources` (`.resx` + `.de.resx`) — neue Strings für das Feed-Toggle-Label und ggf. Benachrichtigungstitel/-text.
- `Reporter.App.OnStart` — möglicher Ankerpunkt für Berechtigungsanfrage bzw. Registrierung des iOS-Delegates (Zeitpunkt offen).
- `Reporter.Platforms.iOS.AppDelegate` — ggf. `UNUserNotificationCenterDelegate`-Zuweisung für die Darstellung im Vordergrund; `Platforms/iOS/Info.plist` benötigt für lokale Benachrichtigungen keine zusätzlichen Schlüssel (Runtime-Authorization genügt).

### Tests (`Reporter.Tests`)

- Unit-Tests für die Entscheidungslogik des `INotificationService`: globaler Schalter aus, Pro-Feed-Flag aus, Ruhezeit aktiv (inkl. Wrap-around 22:00–07:00 und Grenzfälle wie `Start == End`), Keyword-Treffer, Dedup bei wiederholtem Sync.
- `FeedRepository`-Tests für das neue Feld `NotificationsEnabled`; `FeedsViewModel`-Tests für Speichern/Bearbeiten der Pro-Feed-Einstellung.
- Bestehende Testmuster wiederverwenden: In-Memory-SQLite über `ReporterDbContextFactory`, Fake-Implementierungen (`FakeFeedSyncService`, `FakeAutoRefreshService`) für das plattformspezifische Notification-Interface.
- Plattformcode (`UNUserNotificationCenter`) ist nicht unit-testbar — die Entscheidungslogik muss daher vollständig in `Reporter.Core` liegen; Manuelle Verifikation per iOS-Simulator-Screenshot über `scripts/iOS-Deployment.ps1` in `test-results.md` dokumentieren.

## Implementierungsansatz

- **Erweiterungspunkt:** `FeedSyncService.RunSyncAsync` sammelt bereits die neu angelegten `Item`s (heute nur als `newItems`-Zähler). Der Benachrichtigungs-Service wird nach erfolgreichem Sync mit Feed und neuen Artikeln aufgerufen — damit sind alle Sync-Pfade abgedeckt: manuell (`FeedsViewModel.RefreshAsync`/`RefreshAllAsync`) und periodisch (`AutoRefreshService.RunLoopAsync` → `SyncAllAsync`).
- **Ruhezeiten-Auswertung:** gegen die lokale Uhrzeit (über `TimeProvider` injizierbar), mit Wrap-around-Semantik: `Start <= End` → Ruhezeit aktiv, wenn `now ∈ [Start, End)`; `Start > End` → aktiv, wenn `now >= Start || now < End`. Sind `QuietHoursStart`/`QuietHoursEnd` `null`, gilt keine Ruhezeit. Die Werte werden bereits unvalidiert gespeichert (vgl. `docs/help/einstellungen/business-rules.md`, Abschnitt „Ruhezeiten ohne Start-vor-Ende-Validierung" — die Auswertung ist explizit diesem Arbeitspaket vorbehalten).
- **Keyword-Prüfung:** `IKeywordMatcher.MatchesAny(item.Title, item.ContentHtml, keywordTexts)` — dieselbe fest verdrahtete Match-Semantik wie der Löschfilter, aber zum Benachrichtigungszeitpunkt gegen die neu gespeicherten Artikel.
- **Dedup:** `Item`s werden aufgrund des Unique-Index `(feed_id, guid_or_hash)` nur einmal gespeichert und nur im Anlage-Durchlauf benachrichtigt — Benachrichtigungskandidaten sind ausschließlich die im jeweiligen Sync neu eingefügten Artikel. Zusätzlich sollte pro Artikel ein stabiler Notification-Identifier (aus `Item.Id`) verwendet werden, damit iOS keine Dubletten im Mitteilungszentrum erzeugt.
- **iOS-Registrierung:** `UNUserNotificationCenter.Current.RequestAuthorizationAsync` mit `Alert | Badge | Sound`; Anzeige über `UNMutableNotificationContent` + `UNNotificationRequest` mit sofortigem Trigger — oder alternativ das NuGet-Paket `Plugin.LocalNotification` (technische Entscheidung, siehe Offene Fragen).
- **DI:** Registrierung der neuen Services in `MauiProgram.CreateMauiApp` als Singleton, konsistent zu den bestehenden Service-Registrierungen.

## Konfiguration

- **Anwendungseinstellungen (Singleton):** `Settings.NotificationsEnabled` (globaler Ein/Aus-Schalter) sowie `Settings.QuietHoursStart`/`QuietHoursEnd` (Ruhezeit; `null` = deaktiviert) — beide bereits vorhanden und über `SettingsPage` pflegbar.
- **Pro Datensatz (Feed):** neues `Feed.NotificationsEnabled` — Pflege im Feed-Bearbeitungsformular auf `FeedsPage` (Anforderung: „im Feed-Management ergänzen").
- **Keyword-Filter:** bestehende `keywords`-Tabelle über `IKeywordRepository`/`SettingsViewModel` — keine neue Konfiguration nötig.

## Offene Fragen

1. **Ruhezeit-Verhalten:** Werden Benachrichtigungen während der Ruhezeit verworfen oder bis zum Ruhezeitende zurückgehalten und dann nachgeholt? Die Anforderung („keine Benachrichtigungen innerhalb der konfigurierten Zeit") legt Unterdrückung nahe — Nachholen ist nicht gefordert, aber zu bestätigen.
2. **Zeitpunkt der Berechtigungsanfrage:** Beim App-Start, beim erstmaligen Aktivieren des globalen Schalters in den Einstellungen oder erst vor dem ersten tatsächlichen Versand?
3. **Aggregation:** Eine Benachrichtigung pro neuem Artikel oder eine Sammel-Benachrichtigung („n neue Artikel in Feed X"), wenn ein Sync mehrere Artikel liefert?
4. **Interaktion:** Was soll beim Antippen der Benachrichtigung passieren — App öffnen auf der Seite **Ungelesen**, direkt in der Artikeldetailansicht oder ohne Navigation?
5. **Default des Pro-Feed-Flags:** Annahme `true` für Neu- und Bestands-Feeds (Migration mit `defaultValue: true`), damit bestehende Feeds benachrichtigen — zu bestätigen.
6. **Vordergrund-Darstellung:** Sollen Benachrichtigungen auch sichtbar sein, während die App geöffnet ist (der `AutoRefreshService` synct nur bei laufender App)? Dann ist `UNUserNotificationCenterDelegate.WillPresentNotification` im iOS-`AppDelegate` erforderlich.
7. **Sync-Status:** Benachrichtigen nur bei `FeedHealth.Ok` oder auch bei `Warning` (Annahme: bei `Ok` und `Warning`, nicht bei `Error`, da dann keine neuen Artikel gespeichert wurden)?
8. **Technologie-Wahl:** Natives `UserNotifications`-Framework oder `Plugin.LocalNotification`? Letzteres würde das optionale Android-Folge-Issue vereinfachen, zieht aber eine externe Abhängigkeit nach sich.
9. **Inhalt der Benachrichtigung:** Titel = Feed-Titel oder Artikeltitel? Ist ein Datenschutz-Hinweis in der `PrivacyInfo.xcprivacy` oder ein `NSUserNotificationsUsageDescription`-Eintrag im `Info.plist` erforderlich (abhängig von der gewählten API/Package-Version)?
