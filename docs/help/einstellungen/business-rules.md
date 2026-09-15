<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Einstellungen — Business Rules

## Lösch-Invarianten (niemals ungelesen, niemals gemerkt)

**Beschreibung:** Die automatische Löschung darf weder ungelesene noch für später gemerkte Artikel entfernen. Diese Invarianten gelten für beide Löschregeln (allgemeine Frist und Keyword-Regel) und sind nicht abschaltbar.

**Bedingungen:**
- `IsRead == false` → Artikel bleibt erhalten.
- `IsSavedForLater == true` → Artikel bleibt erhalten.

**Umsetzung:** Beide Prädikate stecken in den Repository-Abfragen `ItemRepository.DeleteExpiredAsync` und `ItemRepository.GetExpiredKeywordCandidatesAsync` — sie können nicht umgangen werden. Neu abgerufene Keyword-Treffer werden zusätzlich bereits beim Feed-Abruf in `FeedSyncService.RunSyncAsync` verworfen (Filter beim Einspeichern) und erreichen die Löschregeln gar nicht — die Keyword-Löschregel bereinigt nur noch Artikel, die vor Anlage des Schlagworts gespeichert wurden.

## Unterschiedliche Fristbasis der beiden Löschregeln

**Beschreibung:** Beide Regeln teilen denselben `cutoff` (`UtcNow - RetentionDays`), nutzen aber unterschiedliche Zeitstempel:

| Regel | Bedingung | Fristbasis |
|-------|-----------|------------|
| Allgemeine Retention | `IsRead && !IsSavedForLater && (ReadAt ?? PublishedAt) < cutoff` | Lesezeitpunkt (`ReadAt`, Fallback `PublishedAt`) |
| Keyword-Regel | `IsRead && !IsSavedForLater && (PublishedAt ?? ReadAt) < cutoff` **und** Keyword-Match | Veröffentlichungsdatum (`PublishedAt`, Fallback `ReadAt`) |

**Begründung:** Nutzt die Keyword-Regel denselben Zeitstempel wie die allgemeine Regel, wäre sie eine leere Teilmenge davon. Die Filter-Semantik verlangt, dass unerwünschte Artikel nach Ablauf der Frist seit ihrer Veröffentlichung entfernt werden — unabhängig davon, wann sie zuletzt gelesen wurden. Für Neuzugänge ist die Regel gegenstandslos, weil Keyword-Treffer beim Abruf gar nicht erst gespeichert werden; sie bereinigt ausschließlich bereits gespeicherte Bestandstreffer.

**Umsetzung:** `RetentionCleanupService.CleanupAsync` (Orchestrierung), `ItemRepository.GetExpiredKeywordCandidatesAsync` (Kandidaten), `ItemRepository.DeleteRangeAsync` (Löschung per IDs).

## Keyword-Matching ist fest verdrahtet

**Beschreibung:** Die Match-Semantik ist Teilwort + case-insensitiv und **nicht** konfigurierbar. In der UI ist das als nicht-interaktives Badge „Immer aktiv" (`SettingsKeywordMatchStatus`) neben der Zeile „Teilwort, Groß-/Kleinschreibung egal" (`SettingsKeywordMatchLabel`) dargestellt.

**Bedingungen:**
- Match-Felder: `Item.Title` **und** `Item.ContentHtml`; `Item.Link` wird bewusst nicht gematcht (URLs sind opak, Zufallstreffer-Gefahr).
- Vergleich: `string.Contains(keyword, StringComparison.OrdinalIgnoreCase)`; leere/Whitespace-Keywords werden übersprungen.
- Matching-Zeitpunkt: zweifach — beim **Feed-Abruf** in `FeedSyncService.RunSyncAsync` gegen `SyndicationItem`-Titel und -Inhalt (Treffer werden gar nicht erst gespeichert; die Anzahl verworfener Artikel wird in der `SyncLog.Message` als `, N filtered` ausgewiesen) sowie zur **Cleanup-Zeit** in `RetentionCleanupService` gegen bereits gespeicherte Artikelinhalte. Es gibt kein persistiertes Filter-Flag am `Item`, sodass Keyword-Änderungen ohne Artikel-Neubewertung wirken: Neue Treffer werden ab dem nächsten Abruf verworfen, gespeicherte Bestandstreffer beim nächsten App-Start-Cleanup fristbasiert entfernt.

**Umsetzung:** `KeywordFilter.MatchesAny` (delegiert an `KeywordMatcher.MatchesAny`)

## RetentionDays-Schutzregel

**Beschreibung:** `RetentionDays <= 0` deaktiviert das gesamte Aufräumen (beide Regeln). Ohne diesen Abbruch läge der `cutoff` in der Zukunft und alle gelesenen Artikel würden gelöscht.

**Bedingungen:**
- UI-`Slider` begrenzt die Eingabe auf 1–365 (`MinRetentionDays`/`MaxRetentionDays` im `SettingsViewModel`); `PersistAsync` und `SaveRetention` clampen zusätzlich.
- DB-Default: 30 Tage.

**Umsetzung:** `RetentionCleanupService.CleanupAsync` (Frühabbruch), `SettingsViewModel` (Clamping).

## `AutoMarkReadMode`-String-Konvention

**Beschreibung:** Der globale Ein/Aus-Schalter „Automatisch als gelesen markieren" schreibt kein Boolean, sondern den Modus-String: `"on_open"` bei Ein, `"off"` bei Aus.

**Verhalten:**
- `AutoMarkReadMode != "off"` → automatische Markierung aktiv (`SettingsValues.IsAutoMarkReadEnabled`). Damit bleiben der Seed-Default `"on_scroll"` und bestehende Fallbacks rückwärtskompatibel aktiviert.
- `AutoMarkReadMode == "off"` → `ArticleDetailViewModel` startet den `MarkReadDelayedAsync`-Timer nicht und setzt `IsAutoMarkReadAvailable = false`: Der lokale Schalter in der Detailansicht ist dann deaktiviert und abgedunkelt (`Opacity` 0,4), das Label lautet `ArticleAutoMarkReadDisabled` („Auto-Gelesen (in den Einstellungen deaktiviert)").
- Verzögerung `AutoMarkReadDelaySeconds >= 0` ist zulässig — die Option „Sofort" (0 s) markiert ohne spürbare Wartezeit.
- Der lokale `IsAutoMarkRead`-Toggle in der Detailansicht bleibt eine sitzungsbezogene Abwahl; beide Bedingungen müssen erfüllt sein.

**Umsetzung:** `SettingsValues` (Konstanten `AutoMarkReadOnOpen`/`AutoMarkReadOnScroll`/`AutoMarkReadOff`, Prüfmethode `IsAutoMarkReadEnabled`), `SettingsViewModel.AutoMarkReadEnabled`, `ArticleDetailViewModel.LoadAsync`/`OnAutoMarkReadChanged`/`IsAutoMarkReadAvailable`.

## Refresh-Intervall-Wertebereich

**Beschreibung:** Die UI bietet nur die vier benannten Intervalle 15/30/60/240 Minuten an (`RefreshIntervalOption`); persistiert wird der `int`-Minutenwert (`RefreshIntervalMinutes`, Default 30).

**Verhalten:**
- `AutoRefreshService.ApplySettingsAsync` clampet den gelesenen Wert defensiv auf `[1, 1440]` Minuten über `SettingsValues.ClampRefreshIntervalMinutes` (Konstanten `MinRefreshIntervalMinutes`/`MaxRefreshIntervalMinutes`, zentral in `Reporter.Core.Models` — gemeinsam genutzt vom Timer-Loop und der `EarliestBeginDate` des iOS-Hintergrundabrufs), falls die DB einen anderen Wert enthält.
- `AutoRefreshEnabled == false` → kein Timer-Loop; `ApplySettingsAsync` stoppt einen laufenden Loop und meldet zusätzlich den OS-Hintergrundabruf ab (`IBackgroundRefreshService.ApplySettingsAsync` → `BGTaskScheduler.Shared.Cancel` unter iOS).
- Änderungen am Toggle oder Intervall starten den Timer sofort neu und planen den OS-Hintergrundabruf mit (Aufruf aus `SettingsViewModel.PersistAsync`).

**Umsetzung:** `AutoRefreshService`, `BackgroundRefreshService` (iOS-`BGAppRefreshTask`-Scheduling), `SettingsValues.ClampRefreshIntervalMinutes`, `SettingsViewModel.RefreshIntervalOptions`.

## OS-Hintergrundabruf an Auto-Refresh gekoppelt

**Beschreibung:** Der iOS-`BGAppRefreshTask` besitzt keinen eigenen Schalter und kein eigenes Intervall — er folgt vollständig `AutoRefreshEnabled`/`RefreshIntervalMinutes`. Damit steuert ein einziger Schalter beide Abrufmechanismen konsistent: den In-App-`PeriodicTimer` und den OS-Task.

**Bedingungen:**
- `IBackgroundRefreshService.IsSupported` ist nur unter iOS `true` — die Weiterleitung aus `AutoRefreshService.ApplySettingsAsync` greift nur dort; auf Windows/Android/MacCatalyst ist das Gateway ein No-Op.
- Die Weiterleitung läuft **vor** der `AutoRefreshEnabled`-Prüfung — nur so meldet ein deaktivierter Schalter den Task zuverlässig ab (`Cancel` statt `Submit`).
- Der Task wird einmalig in `AppDelegate.FinishedLaunching` beim `BGTaskScheduler` registriert (`RegisterBackgroundFetchTask`) und nach jedem ausgeführten Lauf von `ScheduledSyncRunner.RunAsync` aus den persistierten Settings neu eingeplant — auch im Fehler- und Kancellierungsfall, damit sich der Abruf nicht „totläuft".
- `EarliestBeginDate` ist für iOS nur eine Untergrenze — die tatsächliche Ausführungshäufigkeit ist systemgesteuert und nicht garantiert; die Systemoption „Hintergrundaktualisierung" kann den Task zusätzlich komplett verhindern (wird nicht ausgewertet).

**Umsetzung:** `IBackgroundRefreshService`/`BackgroundRefreshService` (`ApplySettingsAsync` → `BGAppRefreshTaskRequest` mit `EarliestBeginDate` bzw. `Cancel`, Konstante `RefreshTaskIdentifier`), `IScheduledSyncRunner`/`ScheduledSyncRunner.RunAsync` (Sync + Neuplanung), `AutoRefreshService.ApplySettingsAsync` (fehlerisolierte Weiterleitung, `IsSupported`-Gate), `AppDelegate` (Registrierung, `ExpirationHandler`, `SetTaskCompleted`), `Info.plist` (`UIBackgroundModes`/`fetch`, `BGTaskSchedulerPermittedIdentifiers`).

## Start-Abruf ist opt-out und fehlerisoliert

**Beschreibung:** `Settings.RefreshOnStartupEnabled` (`bool`, Default `true`, Spalte `settings.refresh_on_startup_enabled`) steuert, ob beim App-Start einmalig alle Feeds abgerufen werden. Die Option ist bewusst unabhängig vom periodischen Intervall-Loop: Auch bei ausgeschalteter `AutoRefreshEnabled` läuft der Start-Abruf, solange der Schalter an ist.

**Verhalten:**
- `RefreshOnStartupEnabled == true` **und** `INetworkStatusService.IsOnline` → `AutoRefreshService.StartAsync` startet `RunStartupSyncAsync` fire-and-forget (`_ = …`) — der App-Start blockiert nie, ein Sync-Fehler wird per `Debug.WriteLine` protokolliert und geschluckt.
- `RefreshOnStartupEnabled == false` oder offline → kein Start-Abruf; der Intervall-Loop bleibt davon unberührt.
- Der Start-Abruf läuft zusätzlich zum Intervall-Loop — ein anschließender Timer-Tick ruft regulär erneut ab.

**Umsetzung:** `AutoRefreshService.StartAsync`/`RunStartupSyncAsync`, `SettingsViewModel.RefreshOnStartupEnabled` (Schalter **Beim Programmstart abrufen**, `SettingsRefreshOnStartupLabel`/`SettingsRefreshOnStartupHint`).

## `UnreadSortOrder`-String-Konvention

**Beschreibung:** `Settings.UnreadSortOrder` ist ein `string?` mit den Werten `"desc"` (Default, *Neueste zuerst*) und `"asc"` (*Älteste zuerst*) — Konstanten `SettingsValues.SortOrderDescending`/`SortOrderAscending`, Spalte `settings.unread_sort_order`. Die Einstellung gilt ausschließlich für die Liste **Ungelesen**; **Später** bleibt fest absteigend sortiert.

**Verhalten:**
- `UnreadViewModel.LoadPageAsync` mappt den String auf das Bool `ascending` (`== SortOrderAscending`) und reicht es an `IItemRepository.GetUnreadByDateAsync`; jeder andere Wert (inkl. `null` oder unbekannt) fällt auf absteigend zurück — sowohl im `SettingsViewModel.LoadAsync`-Fallback auf die `"desc"`-`SortOrderOption` als auch implizit beim Bool-Vergleich.
- `ascending == true` → `OrderBy(PublishedAt).ThenByDescending(Id)` (exakte Umkehr inkl. Tiebreaker); `false` → `OrderByDescending(PublishedAt).ThenBy(Id)`.
- Artikel ohne `PublishedAt` folgen dem SQLite-NULL-Verhalten ohne gesonderten Code (bei `asc` zuerst, bei `desc` zuletzt) — dokumentiert in `ItemRepositoryTests.GetUnreadByDateAsync_Ascending_NullPublishedAt_SortsFirst`.

**Umsetzung:** `SettingsValues` (Konstanten), `SortOrderOption` (Picker-Modell, Labels `SettingsSortOrderNewest`/`SettingsSortOrderOldest`), `SettingsViewModel.SelectedSortOrder`, `UnreadViewModel.LoadPageAsync`, `ItemRepository.GetUnreadByDateAsync`.

## Theme-String und Fallback

**Beschreibung:** `Settings.Theme` ist ein `string?` mit den Werten `"system"`/`"light"`/`"dark"` (Default `"system"`).

**Verhalten:**
- `"light"` → `AppTheme.Light`, `"dark"` → `AppTheme.Dark`, alles andere (`"system"`, `null`, unbekannt) → `AppTheme.Unspecified` (folgt dem Betriebssystem).
- `SettingsViewModel.LoadAsync` fällt bei unbekanntem gespeicherten Wert auf die `ThemeOption` `"system"` zurück.

**Umsetzung:** `AppThemeService.ApplyTheme`, `SettingsViewModel`.

## Language-String und Fallback

**Beschreibung:** `Settings.Language` ist ein `string?` mit den Werten `"system"`/`"de"`/`"en"` (Default `"system"`).

**Verhalten:**
- `"de"`/`"en"` → `AppCulture.Apply` setzt `CurrentUICulture`, `CurrentCulture`, `DefaultThreadCurrentUICulture` und `DefaultThreadCurrentCulture` auf `new CultureInfo(value)`; wirksam ab dem nächsten App-Start (Anwendung in `MauiProgram.ApplyPersistedLanguage` vor `CreateWindow`).
- `"system"`, `null`, unbekannt → `AppCulture.ResolveCulture` liefert `null`, `Apply` ist ein No-Op — die App folgt der Systemkultur (Konvention analog `AppThemeService.ApplyTheme`).
- `SettingsViewModel.LoadAsync` fällt bei unbekanntem gespeicherten Wert auf die `LanguageOption` `"system"` zurück; `PersistAsync` schreibt `SelectedLanguage?.Value ?? LanguageSystem`.
- Keine Laufzeit-Umschaltung: `PersistAsync` ruft keinen Service für `Language` auf — die UI weist per `SettingsLanguageRestartHint` auf den erforderlichen Neustart hin.
- Der Hinweis-`Border` ist kein statischer Text: `SettingsViewModel.LanguageRestartHintVisible` steuert die Sichtbarkeit (`IsVisible`-Binding). Das Flag wird im `SelectedLanguage`-Setter auf `value?.Value != _loadedLanguage` gesetzt (außerhalb `_isLoading`) — es erscheint erst nach einer Abweichung vom persistierten Wert und verschwindet bei Rückwahl; `LoadAsync` setzt es zurück.

**Umsetzung:** `AppCulture.ResolveCulture`/`Apply` (`Reporter.Core.Localization`), `MauiProgram.ApplyPersistedLanguage`, `SettingsViewModel.SelectedLanguage`, `SettingsValues.LanguageSystem`/`LanguageGerman`/`LanguageEnglish`.

## Ruhezeiten ohne Start-vor-Ende-Validierung

**Beschreibung:** `QuietHoursStart`/`QuietHoursEnd` werden unvalidiert gespeichert; Bereiche über Mitternacht (z. B. 22:00–07:00) sind zulässig. Die Auswertung (Wrap-around, Unterdrückung von Benachrichtigungen, Grenzfälle) erfolgt im Benachrichtigungs-Service — siehe [Benachrichtigungen — Business Rules](../benachrichtigungen/business-rules.md), Abschnitt „Ruhezeit-Auswertung".

**Verhalten:**
- `QuietHoursEnabled` existiert nur im `SettingsViewModel` — die Aktivierung ergibt sich beim Laden aus `QuietHoursStart is not null || QuietHoursEnd is not null`.
- Ausschalten persistiert `null` für beide Felder; die zuletzt gewählten Werte bleiben in den ViewModel-Feldern erhalten und werden beim Wiedereinschalten derselben Sitzung restauriert.
- Einschalten ohne gesetzte Werte befüllt die Defaults `DefaultQuietHoursStart` (22:00) / `DefaultQuietHoursEnd` (07:00).
- Die `TimePicker` sind per `IsEnabled`-Binding an den Schalter gekoppelt und bei ausgeschalteter Ruhezeit auf `Opacity` 0,4 abgedunkelt.

**Umsetzung:** `SettingsViewModel.QuietHoursEnabled`/`QuietHoursStart`/`QuietHoursEnd` (`TimePicker`-Bindung, `null`-Mapping in `PersistAsync`), `SettingsRepository.SaveAsync`.

## Session-Debug-Log ist Opt-in und sitzungsbezogen

**Beschreibung:** `Settings.DebugCollectionEnabled` (`bool`, Default `false`, Spalte `settings.debug_collection_enabled`) schaltet die Protokollierung in `debug_log_entries`. Das Log ist bewusst kein Langzeit-Protokoll: Es wird bei jedem App-Start zurückgesetzt, damit der Bericht nur relevante, aktuelle Daten enthält.

**Bedingungen:**
- `BeginSessionAsync` läuft in `App.OnStart` nach der Migration — der Reset erfolgt auch bei ausgeschalteter Sammlung, sodass die Tabelle immer genau eine Session plus übernommene Fehler enthält.
- `DeleteAllExceptErrorsAsync` erhält Einträge mit `DebugLogLevel.Error` (u. a. `UnhandledException`/`UnobservedTaskException` der Kategorie `Exception` sowie Sync-Fehler) — ein Absturz der Vor-Session bleibt nach dem Neustart meldbar; `Info`/`Warning`-Einträge werden gelöscht.
- Bei `IsEnabled == false` ist `LogAsync` ein sofortiger No-op — es werden keine Einträge geschrieben und kein DB-Zugriff ausgelöst.
- `SetEnabled` schaltet zur Laufzeit ohne App-Neustart um; bei der Aktivierung wird ein `Lifecycle`-Übergangseintrag („Debug collection enabled") geschrieben.
- `MaxStoredEntries = 500` begrenzt das Tabellenwachstum innerhalb einer Session (Fehlerschleifen-Schutz).

**Umsetzung:** `DebugLogService` (`_enabled`-Flag, `BeginSessionAsync`, `SetEnabled`, `LogAsync`), `DebugLogRepository.DeleteAllExceptErrorsAsync`/`TrimToLatestAsync`, `SettingsViewModel.DebugCollectionEnabled`-Setter.

## Senden erfordert aktive Sammlung und Mail-Unterstützung

**Beschreibung:** Die Senden-Aktion im Abschnitt **Diagnose & Support** ist nur bedienbar, wenn die Sammlung eingeschaltet ist **und** das Gerät einen Mail-Compose-Client anbietet.

**Verhalten:**
- `DebugSendEnabled = DebugEmailSupported && DebugCollectionEnabled` steuert `IsEnabled`/`Opacity` des Senden-`Border`; bei `false` zeigt ein Hinweis-`Border` die Ursache (`SettingsDebugCollectionRequiredHint` bzw. `SettingsDebugEmailUnsupportedHint`).
- Der eigentliche Guard sitzt zusätzlich in `SendDebugReportAsync` selbst (`_debugReportService is null || !DebugCollectionEnabled` → sofortige Rückkehr ohne Event) — bewusst nicht per `CanExecute`, da `IAsyncRelayCommand.ExecuteAsync` `CanExecute` nicht auswertet und Tests/programmatische Aufrufe ihn sonst umgehen würden.
- `IsSupported == false` bei eingeschalteter Sammlung wird bewusst **nicht** weggeguardet: `SendReportAsync` läuft → `false` → `DebugReportFailed`-Alert — der gewünschte sichtbare Fehlerpfad.

**Umsetzung:** `SettingsViewModel.DebugSendEnabled`/`DebugEmailSupported`/`SendDebugReportAsync`, `IDebugReportService.IsSupported` (delegiert an `IEmailService.IsSupported` → `Email.Default.IsComposeSupported`).

## Report-Umfang, Begrenzungen und Platzhalter-Empfänger

**Beschreibung:** Der Debugbericht ist ein Plain-Text-E-Mail-Entwurf mit fest umrissenem Inhalt — keine Anhänge, keine Artikeldaten.

**Bedingungen:**
- Sektionen (lokalisierte Header aus `AppResources.DebugReportSection*`): Anwendung (`AppDeviceInfo` + Report-Zeitstempel), Gerät, Netzwerk (`IsOnline`), Einstellungen (vollständiger `Settings`-Snapshot inkl. `DebugCollectionEnabled`), Feed-Status (`Title`, `Url`, `HealthStatus`, `LastCheckedAt`, `HealthLastChange` je Feed), Sync-Verlauf (jüngste `MaxSyncLogEntries = 50` `SyncLog`-Einträge), Session-Debug-Log (jüngste `MaxDebugLogEntries = 200` Einträge inkl. übernommener `Error`-Einträge der Vor-Session).
- Artikelinhalte (`Item.Title`, `ContentHtml`) sind ausdrücklich nicht Teil des Berichts; es gibt kein `EmailAttachment`.
- Empfänger ist die MSBuild-Property `DebugReportRecipient` (Default `"debug@example.com"` in `Directory.Build.props`, als `AssemblyMetadata` eingebettet und von `DebugReportService.DebugReportRecipient` gelesen) — dokumentierter Platzhalter, den der Maintainer vor der Auslieferung in `Directory.Build.props` durch die tatsächliche Support-Adresse ersetzt; Forks müssen die Property auf ihre eigene Adresse setzen; nicht benutzerkonfigurierbar.
- Die App versendet nichts selbst: `Email.ComposeAsync` öffnet nur den vorbefüllten Entwurf; der Anwender prüft und sendet aus dem Mail-Client.

**Umsetzung:** `DebugReportService.SendReportAsync`/`BuildBody`, `EmailService.ComposeAsync` (`EmailBodyFormat.PlainText`).
