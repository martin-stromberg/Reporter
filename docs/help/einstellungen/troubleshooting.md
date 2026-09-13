<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Einstellungen — Fehlerbehebung

## Einstellungen werden nicht gespeichert

**Symptom:** Änderungen auf der Einstellungen-Seite gehen nach App-Neustart verloren; es gibt keine sichtbare Fehlermeldung.

**Ursache:** `SettingsViewModel.PersistAsync` fängt alle Exceptions und protokolliert sie ausschließlich per `Debug.WriteLine` (`"Failed to save settings: …"`). Ein Datenbankfehler ist für den Anwender unsichtbar.

**Lösung:**
1. App unter einem Debugger starten (bzw. Debug-Ausgabe/Logcat anhängen) und die Meldung `Failed to save settings` suchen.
2. SQLite-Datei `reporter.db` im App-Datenverzeichnis (`FileSystem.AppDataDirectory`) auf Schreibzugriff und Migration `AddSettingsAutoRefreshAndTheme` prüfen.
3. Prüfen, ob `settings`-Datensatz mit `Settings.DefaultId` (`a1f5c6d2-4b3e-4c8f-9d2a-1b2c3d4e5f6a`) existiert — `SettingsRepository.GetAsync`/`SaveAsync` legen ihn sonst an.

## Automatische Aktualisierung läuft nicht

**Symptom:** Feeds werden nicht periodisch aktualisiert, obwohl „Automatische Hintergrund-Aktualisierung" eingeschaltet ist.

**Ursache:** Der `PeriodicTimer` in `AutoRefreshService` lebt nur innerhalb des laufenden App-Prozesses. Wird die App geschlossen oder vom System beendet, läuft kein Hintergrund-Abruf mehr. Fehler in `App.OnStart` beim `StartAsync` werden ebenfalls nur per `Debug.WriteLine` protokolliert (`"App.OnStart auto refresh start failed"`).

**Lösung:**
1. Debug-Ausgabe auf `auto refresh start failed` und `AutoRefreshService sync failed` prüfen.
2. In der Datenbank verifizieren: `settings.auto_refresh_enabled = 1` und plausibles `refresh_interval_minutes` (wird auf 1–1440 geclamppt).
3. Beachten: Sync-Fehler einzelner Ticks stoppen den Timer nicht — nur stille `SyncLog`-Einträge bzw. Debug-Ausgaben zeigen sie an.

## Start-Abruf läuft nicht

**Symptom:** Beim Öffnen der App werden die Feeds nicht automatisch abgerufen, obwohl „Beim Programmstart abrufen" eingeschaltet ist.

**Ursache:** Der Start-Abruf läuft in `AutoRefreshService.StartAsync` über `RunStartupSyncAsync` — fehlerisoliert und fire-and-forget; Fehler erscheinen nur als `Debug.WriteLine` (`"AutoRefreshService startup sync failed"`). Ohne Netzwerk (`INetworkStatusService.IsOnline == false`) wird er still übersprungen.

**Lösung:**
1. `settings.refresh_on_startup_enabled = 1` in der Datenbank verifizieren (Default `1`; Migration `AddSettingsStartupRefreshAndSortOrder`).
2. Sicherstellen, dass beim Start eine Internetverbindung bestand.
3. Debug-Ausgabe auf `AutoRefreshService startup sync failed` und `App.OnStart auto refresh start failed` prüfen.
4. Beachten: Der Start-Abruf ist unabhängig von `auto_refresh_enabled` — er läuft auch bei ausgeschalteter Hintergrund-Aktualisierung.

## Ungelesen-Liste zeigt falsche Reihenfolge

**Symptom:** Die Liste **Ungelesen** sortiert entgegen der gewählten Einstellung **Sortierung der ungelesenen Artikel**.

**Ursache:** `UnreadViewModel.LoadPageAsync` wertet `settings.unread_sort_order` aus (`"desc"`/`"asc"`); jeder andere Wert fällt auf `"desc"` zurück. Artikel ohne Veröffentlichungsdatum folgen dem SQLite-NULL-Verhalten (bei aufsteigender Sortierung zuerst, bei absteigender zuletzt).

**Lösung:**
1. `settings.unread_sort_order` prüfen — gültig sind nur `desc` und `asc`.
2. Beachten: Die Einstellung gilt nur für **Ungelesen**; die Liste **Später** ist bewusst fest absteigend sortiert.
3. Bei gemischter Reihenfolge Artikel ohne Veröffentlichungsdatum berücksichtigen.

## Gefilterte Artikel erscheinen weiterhin in den Listen

**Symptom:** Ein Artikel mit Keyword-Treffer ist trotz eingerichtetem Schlagwort in der Ungelesen-Liste sichtbar.

**Ursache:** Der Keyword-Filter wirkt beim Feed-Abruf (Ingest) — nur neue `SyndicationItem`s werden in `FeedSyncService.RunSyncAsync` gegen die Liste gematcht und vor dem Speichern verworfen. Artikel, die bereits vor Anlage des Schlagworts gespeichert wurden, bleiben sichtbar, bis sie gelesen wurden und die Keyword-Löschregel beim App-Start greift (`IsRead && !IsSavedForLater && (PublishedAt ?? ReadAt) < cutoff`).

**Lösung:**
1. Prüfen, ob das Keyword tatsächlich in `Title` oder `ContentHtml` vorkommt (Teilwort, `OrdinalIgnoreCase`; `Link` wird nicht gematcht).
2. Den Sync-Verlauf prüfen — die `SyncLog.Message` weist verworfene Treffer als `, N filtered` aus.
3. Für bereits gespeicherte Treffer: Artikel als gelesen markieren und App neu starten — die Löschregel läuft nur beim Start.
4. `settings.retention_days > 0` verifizieren — `<= 0` deaktiviert beide Löschregeln.
5. Debug-Ausgabe auf `App.OnStart retention cleanup failed` prüfen.

## „Automatisch als gelesen markieren“ greift nicht

**Symptom:** Geöffnete Artikel bleiben ungelesen, obwohl die Option eingeschaltet ist.

**Ursache:** `ArticleDetailViewModel` prüft zwei Bedingungen: global `settings.auto_mark_read_mode != "off"` **und** der sitzungsbezogene `IsAutoMarkRead`-Toggle in der Detailansicht. Zusätzlich überspringt `MarkReadDelayedAsync` bereits gelesene Artikel.

**Lösung:**
1. Globalen Schalter in den Einstellungen und den lokalen „Auto-Gelesen"-Schalter in der Detailansicht prüfen.
2. `settings.auto_mark_read_delay_seconds` prüfen — negative Werte fallen auf den Fallback (5 s) zurück.
3. Beim Öffnen schlägt fehlgeschlagener Settings-Zugriff fehl auf ein Fallback-Objekt (`"on_open"`, 5 s) — im Debug-Log `Failed to load settings` suchen.

## Benachrichtigungen kommen nicht an / Hinweiszeile „Einstellungen öffnen"

**Symptom:** Trotz eingeschaltetem Schalter **Benachrichtigungen** erscheinen keine Benachrichtigungen, oder die rote Hinweiszeile mit **Einstellungen öffnen** ist sichtbar.

**Ursache:** Neben dem globalen Schalter (`settings.notifications_enabled`) greifen der Pro-Feed-Schalter (`feeds.notifications_enabled`), die Ruhezeit (`quiet_hours_start`/`quiet_hours_end`, inkl. Wrap-around) und die Keyword-Filter; auf iOS kann zusätzlich die System-Berechtigung verweigert sein (`UNAuthorizationStatus.Denied` — Versand bricht in `LocalNotificationService.EnsureAuthorizedAsync` still ab) oder noch nicht angefragt worden sein (`NotDetermined` → neutrale Zeile „Benachrichtigungen erlauben"). `NotificationPermissionDenied`/`NotificationPermissionNotDetermined` werden nur beim Laden der Seite (`GetAuthorizationStatusAsync`) und beim Umschalten aktualisiert.

**Lösung:**
1. Detaillierte Prüfkette siehe [Benachrichtigungen — Fehlerbehebung](../benachrichtigungen/troubleshooting.md).
2. Debug-Ausgaben: `FeedSyncService notification failed`, `Failed to request notification authorization`, `Failed to query notification authorization`.
3. Auf Nicht-iOS-Targets ist `ILocalNotificationService.IsSupported == false` — die Berechtigungs-Hinweiszeilen können dort nicht erscheinen; stattdessen sind die Schalter deaktiviert (`NotificationsSupported == false`, Hinweis „derzeit nur auf iOS verfügbar") und `ShowAsync` ist ein No-Op.

## Theme-Wechsel wirkt nicht

**Symptom:** Der „Farbschema"-Picker ändert das Erscheinungsbild nicht.

**Ursache:** `AppThemeService.ApplyTheme` bricht still ab, wenn `Application.Current is null`. Mapping: nur exakt `"light"`/`"dark"` erzwingen ein Theme; alle anderen Werte (inkl. `null`) setzen `AppTheme.Unspecified` (System).

**Lösung:**
1. `settings.theme` in der Datenbank prüfen (`system`/`light`/`dark`).
2. Debug-Ausgabe auf `App.OnStart theme apply failed` (Start) bzw. `Failed to save settings` (Änderung über Picker) prüfen.
3. Sicherstellen, dass die Styles `AppThemeBinding` verwenden — hart codierte Farben reagieren nicht auf Theme-Wechsel.

## Sprachwechsel wirkt nicht

**Symptom:** Nach Auswahl von „Deutsch"/„English" im `Sprache`-Picker bleibt die App in der bisherigen Sprache.

**Ursache:** Es gibt keine Laufzeit-Umschaltung — `SettingsViewModel.PersistAsync` schreibt `Language` nur in den Datensatz; die Kultur wird ausschließlich beim App-Start in `MauiProgram.ApplyPersistedLanguage` angewendet (vor `CreateWindow`, da `AppShell` lokalisierte Tab-Titel und alle Pages eager erzeugt). Schlägt dieser Block fehl, wird er per `try/catch` + `Debug.WriteLine` (`MauiProgram.ApplyPersistedLanguage failed`) geschluckt und die Systemkultur bleibt wirksam.

**Lösung:**
1. App vollständig neu starten — ohne Neustart kann die Auswahl nicht wirken.
2. `settings.language` in der Datenbank prüfen (`system`/`de`/`en`); unbekannte Werte fallen in `AppCulture.ResolveCulture` auf `null` → No-Op → Systemkultur.
3. Debug-Ausgabe auf `MauiProgram.ApplyPersistedLanguage failed` prüfen — dort werden Fehler beim synchronen `Database.Migrate()` oder `GetAsync` sichtbar (z. B. fehlende `language`-Spalte bei abgelaufener Migration `AddSettingsLanguage`).
4. Beachten: `AppCulture.Apply` setzt auch `CurrentCulture` und die `DefaultThreadCurrent*`-Defaults — bei teilweise falscher Formatierung (z. B. Datumsformat) ist der persistierte Wert zu prüfen.
