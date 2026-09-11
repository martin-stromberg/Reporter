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

## Gefilterte Artikel werden nicht gelöscht

**Symptom:** Gelesene Artikel mit Keyword-Treffer bleiben über die Frist hinaus erhalten.

**Ursache:** Mehrere Bedingungen müssen gleichzeitig erfüllt sein: `IsRead`, `!IsSavedForLater`, `(PublishedAt ?? ReadAt) < cutoff`, Keyword-Match auf `Title`/`ContentHtml`. Der Cleanup läuft ausschließlich in `App.OnStart` — nicht nach jedem Sync oder periodisch.

**Lösung:**
1. App neu starten — der Cleanup läuft nur beim Start.
2. Prüfen, ob das Keyword tatsächlich in `Title` oder `ContentHtml` vorkommt (Teilwort, `OrdinalIgnoreCase`; `Link` wird nicht gematcht).
3. `settings.retention_days > 0` verifizieren — `<= 0` deaktiviert beide Löschregeln.
4. Debug-Ausgabe auf `App.OnStart retention cleanup failed` prüfen.

## „Automatisch als gelesen markieren“ greift nicht

**Symptom:** Geöffnete Artikel bleiben ungelesen, obwohl die Option eingeschaltet ist.

**Ursache:** `ArticleDetailViewModel` prüft zwei Bedingungen: global `settings.auto_mark_read_mode != "off"` **und** der sitzungsbezogene `IsAutoMarkRead`-Toggle in der Detailansicht. Zusätzlich überspringt `MarkReadDelayedAsync` bereits gelesene Artikel.

**Lösung:**
1. Globalen Schalter in den Einstellungen und den lokalen „Auto-Gelesen"-Schalter in der Detailansicht prüfen.
2. `settings.auto_mark_read_delay_seconds` prüfen — negative Werte fallen auf den Fallback (5 s) zurück.
3. Beim Öffnen schlägt fehlgeschlagener Settings-Zugriff fehl auf ein Fallback-Objekt (`"on_open"`, 5 s) — im Debug-Log `Failed to load settings` suchen.

## Theme-Wechsel wirkt nicht

**Symptom:** Der „Farbschema"-Picker ändert das Erscheinungsbild nicht.

**Ursache:** `AppThemeService.ApplyTheme` bricht still ab, wenn `Application.Current is null`. Mapping: nur exakt `"light"`/`"dark"` erzwingen ein Theme; alle anderen Werte (inkl. `null`) setzen `AppTheme.Unspecified` (System).

**Lösung:**
1. `settings.theme` in der Datenbank prüfen (`system`/`light`/`dark`).
2. Debug-Ausgabe auf `App.OnStart theme apply failed` (Start) bzw. `Failed to save settings` (Änderung über Picker) prüfen.
3. Sicherstellen, dass die Styles `AppThemeBinding` verwenden — hart codierte Farben reagieren nicht auf Theme-Wechsel.
