<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Verbesserungen der App (Issue #77)

## Übersicht

Issue #77 bündelt acht Verbesserungen an der .NET-MAUI-App „Reporter". Gemäß der Meta-Anforderung R0 wurden die Punkte triagiert: In diesem Lauf werden **R1** (Lesezeit bei 1 Min. ausblenden — in Auflistung **und** Artikeldetailansicht), **R2** (Standardbild aus Feed-Favicon/Initialen-Kreis — auf Artikelkarten **und** der Feeds-Seite), **R3** (Feed-Abruf beim Programmstart, Default `true`), **R4** (konfigurierbare Sortierung der Startseite), **R5** (Neustart-Hinweis erst nach Sprachänderung) und **R6** (neues Programmsymbol) umgesetzt. **R7** (Debug-Versand per E-Mail) und **R8** (Benachrichtigungen nur bei Hintergrundabruf) werden in separate GitHub-Issues abgespalten. Betroffen sind `Reporter.Core` (Modelle, Services, ViewModels), `Reporter.Data` (Entities, Repositories, EF-Migrationen), die MAUI-App `Reporter` (Settings-UI, Artikelkarte, Feeds-Seite, App-Assets) sowie `Reporter.Tests`.

## Triage (R0)

| Punkt | Entscheidung | Begründung |
|-------|--------------|------------|
| R1 — Lesezeit bei 1 Min. | **Umsetzen** | Sehr klein, risikoarm: Unterdrückung zentral in `ReadingTimeEstimator.EstimateText` — greift ohne XAML-Eingriff in Liste und Detailansicht. |
| R2 — Standardbild/Favicon | **Umsetzen** | Mittlerer Umfang (neue `feeds`-Spalte, neuer `FeedIconService`, UI-Fallback auf Artikelkarte und Feeds-Seite), aber fachlich klar und vollständig auf bestehende Muster abbildbar. |
| R3 — Abruf bei Programmstart | **Umsetzen** | Klein: Settings-Flag + Schalter; `AutoRefreshService.StartAsync` lädt die Settings bereits und besitzt alle nötigen Abhängigkeiten. Default `true`. |
| R4 — Sortierung Startseite | **Umsetzen** | Mittelklein: Signaturerweiterung an `IItemRepository` mit überschaubaren Folgeanpassungen (Fakes, Tests); Optionsklassen-Muster vorhanden. |
| R5 — Neustart-Hinweis | **Umsetzen** | Sehr klein: ein boolsches Flag in `SettingsViewModel` + `IsVisible`-Binding. |
| R6 — Programmsymbol | **Umsetzen** | Beide Issue-Attachments wurden verifiziert: Sie sind SVGs und über authentifizierten Download (`curl -H "Authorization: Bearer $(gh auth token)"` auf die `user-attachments`-URLs; anonyme Requests liefern 404, da das Repo privat ist) beschaffbar. Variante mit „Reporter"-Schriftzug = SplashScreen, Variante ohne = Geräte-Icon. |
| R7 — Debug-Versand per E-Mail | **Abspalten** | Mittlerer Umfang mit offenem fachlichem Rahmen (Bedeutung von „gesammelt werden": dauerhafte Protokollierung vs. On-Demand-Momentaufnahme; Empfängeradresse unklar). Eigene Issue-Klärung sinnvoll. |
| R8 — Benachrichtigungen nur bei Hintergrundabruf | **Abspalten** | Erfordert nicht existierende iOS-Hintergrundinfrastruktur (`BGTaskScheduler`/`BGAppRefreshTask`, `UIBackgroundModes` in `Info.plist`); Windows besitzt keine Hintergrundinfrastruktur. Ein reines Unterdrücken der Vordergrund-Darstellung würde bedeuten, dass **nie wieder** Benachrichtigungen erscheinen — der Punkt ist ohne den Infrastruktur-Ausbau nicht sinnvoll umsetzbar und ist der größte Einzelumfang. |

Für R7 und R8 werden als erster Umsetzungsschritt eigenständige GitHub-Issues mit fachlicher Beschreibung angelegt (`gh issue create`; `gh`-CLI ist installiert und als `martin-stromberg` mit `repo`-Scope authentifiziert). **Erledigt:** R7 → Issue #81, R8 → Issue #82.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| `ReadingTimeEstimator` (R1) | `EstimateText` liefert `string.Empty`, sobald die geschätzte Lesezeit ≤ 1 Minute beträgt (zusätzlich zum bisherigen Leer-Fall). Keine neue `EstimateMinutes`-Methode, keine Aufrufer-Änderung. | `EstimateText` ist die einzige Anzeigetext-Quelle und hat exakt zwei Aufrufer — `ItemRepository.MapToListItem` (Auflistung) und `ArticleDetailViewModel.LoadAsync` (Detailansicht) — die laut Klärung identisch reagieren sollen. Beide Oberflächen blenden leeren Text bereits über `StringNotEmptyToBoolConverter` aus. Die Unterdrückung an der Quelle verhindert duplizierte Schwellwert-Logik und Divergenz zwischen den Ansichten. |
| Favicon-Persistenz (R2) | `Feed.FaviconUrl` als `string?`-Spalte auf `feeds` (URL, kein lokaler Datei-Download). | Konsistent mit `ItemListItem.ImageUrl` (ebenfalls Remote-URL); die Karten blenden Bilder offline bereits über den `IsOnline`-Trigger aus. Kein zusätzliches Datei-/Cache-Management nötig. |
| Quelle der „Webseite des Feeds" (R2) | Kaskade: `FeedSearchResult.SiteUrl` (Anlage via Suche) → Schema+Host der Feed-URL (Direkt-Anlage) → Site-Link (`<link>`/`alternate`) des Feed-Dokuments beim ersten erfolgreichen Sync (Nachholen für Feeds ohne Favicon). | `SiteUrl` ist die fachlich präziseste Quelle, existiert aber nur im Such-Pfad; Host-Fallback deckt die Direkt-Anlage ab; der Sync-Pfad schließt die Lücke für Bestandsfeeds und offline angelegte Feeds. |
| `FeedIconService` (R2) | Neuer `HttpClient`-basierter Service in `Reporter.Core` (Gateway-frei, da reines HTTP+Parsing): HTML der Webseite laden, `<link rel="icon|shortcut icon|apple-touch-icon">` auswerten (Regex-Muster aus `FeedSearchService.ExtractFeedLinks`), relative URLs auflösen, Fallback `{Host}/favicon.ico`. Kandidaten werden per Request verifiziert (erfolgreiche Antwort), sonst `null`. | Ein ungeprüftes `/favicon.ico` würde bei 404 eine defekte Bild-URL speichern und den Initialen-Fallback verhindern. Service ist analog `FeedSearchService` unit-testbar (`FakeHttpMessageHandler`). |
| Generiertes Ersatzbild (R2) | Kein echtes Bitmap: Kreis-`Border` mit `Label` (Initialbuchstabe) in `ArticleCardView` **und** im Feed-Karten-Template der `FeedsPage`, analog den bestehenden Badge-/Kreis-Mustern (Aktions-`Border`s mit `RoundRectangle`). Der Initialbuchstabe kommt als berechnete Eigenschaft `FeedInitial` aus `ItemListItem.FeedTitle` bzw. `FeedListItem.Title`. | Erfüllt die Anforderung („Kreis mit dem ersten Buchstaben des Feed-Namens") ohne Bildgenerierung; Dark-Mode-fähig via `AppThemeBinding`. |
| Standardbild auf der Feeds-Seite (R2) | `FeedListItem` erhält `FaviconUrl` + `FeedInitial`; `FeedRepository.GetAllWithDetailsAsync` projiziert `f.FaviconUrl`; `FeedsViewModel.ToFeed` führt `FaviconUrl` beim Rekonstruieren des `Feed` mit. | Laut Klärung soll das Standardbild auch auf der Feeds-Seite erscheinen. Die `ToFeed`-Mitnahme ist zwingend: `RenameFeedAsync`, `ChangeFeedCategoryAsync` und `SaveAsync` (Edit) schreiben über `UpdateAsync` den kompletten Datensatz — ohne Mitnahme ginge das Favicon bei jeder Teil-Aktualisierung verloren. |
| Favicon-Nachrüstung für Bestandsfeeds (R2) | Nachhole-Pfad in `FeedSyncService.RunSyncAsync`: bei erfolgreichem Sync und `feed.FaviconUrl == null` wird das Favicon fehlerisoliert ermittelt, an `UpdateFeedHealthAsync` übergeben und mit demselben `UpdateAsync` persistiert (Review-Nacharbeit: die Auflösung liegt im Aufrufer, damit `UpdateFeedHealthAsync` reine Persistenz bleibt). Kein separater Backfill-Job. | Laut Klärung; nutzt den vorhandenen erfolgreichen Sync als natürlichen Trigger und bleibt strikt fehlerisoliert. |
| Start-Abruf (R3) | Umsetzung in `AutoRefreshService.StartAsync`: nach dem Laden der Settings wird bei `RefreshOnStartupEnabled == true` und `IsOnline` ein `SyncAllAsync` fehlerisoliert und nicht-blockierend (fire-and-forget mit `Debug.WriteLine`-Logging) gestartet. Defaultwert `RefreshOnStartupEnabled = true`. | `StartAsync` lädt die Settings ohnehin und besitzt bereits `ISettingsRepository`-, `IFeedSyncService`- und `INetworkStatusService`-Abhängigkeiten — kein Eingriff in `App.OnStart` nötig und über `AutoRefreshServiceTests` mit `TestWaitHelper`/`TimeProvider` unit-testbar. Default `true` laut Klärung (analog `AutoRefreshEnabled = true`). |
| Sortierparameter (R4) | `IItemRepository.GetUnreadByDateAsync(page, pageSize, categoryId, bool ascending = false)`; aufsteigend = `OrderBy(PublishedAt).ThenByDescending(Id)` (exakte Umkehr inkl. Tiebreaker), absteigend = unverändert. | Optionaler Parameter hält den Bruch minimal; die String-Einstellung wird ausschließlich in `UnreadViewModel` ausgewertet und als Bool an das Repository weitergereicht. |
| Settings-Speicher (R4) | `Settings.UnreadSortOrder` als `string?` mit `SettingsValues`-Konstanten `SortOrderDescending`/`SortOrderAscending` (`"desc"`/`"asc"`), Default `"desc"`. | Folgt dem `Theme`-/`Language`-Muster (stringbasierte Konstanten statt Enum), ungültige persistierte Werte fallen in `LoadAsync` auf den Default zurück. |
| `PublishedAt == null` bei aufsteigender Sortierung (R4) | Bestehendes SQLite-NULL-Verhalten unverändert beibehalten — kein gesonderter Handling-Code; das Verhalten wird im Test dokumentiert. | Laut Klärung; Spezialfall-Logik würde die Abfrage ohne fachlichen Mehrwert komplizieren. |
| E2E-Nachweisstrategie | Bestehende In-Memory-E2E-Tests (`SettingsViewModelTests_E2E` mit echten SQLite-Repositories) für Persistenz-Roundtrips ergänzen; UI-Verifikation manuell gemäß `AGENTS.md` (390 × 844-pt-Fenster, Light + Dark, Screenshots). | Das Projekt besitzt keine UI-Testautomatisierung (Bestandsaufnahme: „Es existieren keine UI-/Plattform-Tests"); die manuelle Verifikation mit Screenshot-Doku ist die etablierte Konvention. |

## Programmabläufe

### R1 — Lesezeit-Unterdrückung bei 1-Minuten-Beiträgen

1. `ReadingTimeEstimator.EstimateText` liefert zusätzlich zum bisherigen Leer-Fall `string.Empty`, wenn die geschätzte Lesezeit ≤ 1 Minute beträgt (die interne Minuten-Klemme `Math.Max(1, …)` bleibt; Ergebnis ≤ 1 → kein Anzeigetext).
2. `ItemRepository.MapToListItem` übernimmt den Rückgabewert unverändert in `ItemListItem.ReadingTimeText` — `ArticleCardView` blendet die Lesezeit-Zeile über den vorhandenen `StringNotEmptyToBoolConverter` automatisch aus; keine XAML-Änderung, keine Repository-Änderung für R1.
3. `ArticleDetailViewModel.LoadAsync` übernimmt den Rückgabewert unverändert in `ReadingTime` — `ArticleDetailPage` blendet die Zeile ebenfalls per `StringNotEmptyToBoolConverter` aus; keine ViewModel-Änderung für R1.
4. Die Regel gilt damit identisch auf **Ungelesen**, **Später** und in der Artikeldetailansicht.

Beteiligte Klassen/Komponenten: `ReadingTimeEstimator`, `ItemRepository`, `ItemListItem`, `ArticleCardView`, `ArticleDetailViewModel`, `ArticleDetailPage`

### R2 — Favicon-Ermittlung bei der Feed-Anlage

1. `FeedsViewModel.SubscribeResultAsync` übergibt zusätzlich `result.SiteUrl` an `TryPersistNewFeedAsync` (neue Signatur `(string url, string title, string? siteUrl)`); `DirectAddAsync` und `OfferDirectAddAsync` übergeben `null`.
2. `TryPersistNewFeedAsync` bestimmt die Site-URL: übergebenes `siteUrl`, sonst Schema+Host der Feed-URL.
3. Bei `INetworkStatusService.IsOnline` wird `IFeedIconService.FindFaviconUrlAsync(siteUrl)` fehlerisoliert aufgerufen (eigener `try/catch`, Fehler → `null`, die Anlage wird nie blockiert); offline wird der Schritt übersprungen.
4. Der neue `Feed`-Datensatz wird mit `FaviconUrl` persistiert.
5. Nachhole-Pfad: `FeedSyncService.RunSyncAsync` — war der Sync erfolgreich und `feed.FaviconUrl` ist `null`, wird die Site-URL aus dem Feed-Dokument (`SyndicationFeed.Links`, `RelationshipType == "alternate"`, Fallback Host der Feed-URL via `FeedSiteResolver`) bestimmt, `IFeedIconService` fehlerisoliert aufgerufen und die aufgelöste `FaviconUrl` an `UpdateFeedHealthAsync` übergeben, die sie mit persistiert. `FeedSyncService` erhält dafür eine `IFeedIconService`-Abhängigkeit.

Beteiligte Klassen/Komponenten: `IFeedIconService`/`FeedIconService`/`FeedSiteResolver` (neu), `FeedsViewModel`, `FeedSyncService`, `IFeedRepository`/`FeedRepository`, `Feed`

### R2 — Standardbild-Anzeige in der Artikelkarte

1. `ItemRepository.SelectListItemRows` projiziert zusätzlich `i.Feed.FaviconUrl` in `ItemListRow.FeedFaviconUrl`; `MapToListItem` befüllt `ItemListItem.FeedFaviconUrl`.
2. `ItemListItem` erhält `FeedFaviconUrl` (`string?`, in `CopyWith` mitziehen) und die berechnete Eigenschaft `FeedInitial` (erster Buchstabe von `FeedTitle`, großgeschrieben; Fallback `?` bei leerem Titel).
3. `ArticleCardView`-Thumbnail (80×80-`Border`): Kaskade per `DataTrigger` —
   - `Image Source="{Binding ImageUrl}"` sichtbar, wenn `ImageUrl` nicht leer (bestehendes Verhalten),
   - `Image Source="{Binding FeedFaviconUrl}"` sichtbar, wenn `ImageUrl` leer/`null` und `FeedFaviconUrl` nicht leer,
   - Kreis-`Border` (`RoundRectangle 40`) mit `Label Text="{Binding FeedInitial}"` sichtbar, wenn beide leer/`null`.
   Der bestehende `IsOnline`-Trigger (gesamter Thumbnail-`Border` offline ausgeblendet) bleibt unverändert.
4. `ArticleDetailViewModel.LoadAsync` befüllt `FeedIconUrl` mit `feed?.FaviconUrl` statt `string.Empty` — der Feed wird dort bereits via `_feedRepository.GetByIdAsync(item.FeedId)` geladen (`ArticleDetailViewModel.cs` Zeile 291), kein zusätzlicher Repository-Aufruf nötig. Das `Image` auf `ArticleDetailPage` existiert bereits und ist per `StringNotEmptyToBoolConverter` abgesichert.

Beteiligte Klassen/Komponenten: `ItemRepository`, `ItemListItem`, `ArticleCardView`, `ArticleDetailViewModel`

### R2 — Standardbild auf der Feeds-Seite

1. `FeedListItem` erhält `FaviconUrl` (`string?`) und die berechnete Eigenschaft `FeedInitial` (erster Buchstabe von `Title`, großgeschrieben; Fallback `?` bei leerem Titel).
2. `FeedRepository.GetAllWithDetailsAsync` projiziert `FaviconUrl = f.FaviconUrl` in das `FeedListItem`; `FeedsViewModel.LoadAsync` benötigt keine Änderung (befüllt `Feeds` bereits aus dieser Projektion).
3. `FeedsViewModel.ToFeed` übernimmt `FaviconUrl = feed.FaviconUrl`, damit `RenameFeedAsync`, `ChangeFeedCategoryAsync` und `SaveAsync` (Edit) das Favicon beim Teil-Update nicht verlieren.
4. `FeedsPage`-Feed-Karten-Template (Zeilen 143–254): neue Bild-Spalte vor dem Titel im Karten-`Grid` —
   - `Image Source="{Binding FaviconUrl}"` sichtbar, wenn `FaviconUrl` nicht leer und online (`IsOnline`-`DataTrigger` via `x:Reference` auf die Page; `FeedsViewModel` erbt `IsOnline` aus `BaseViewModel`),
   - Kreis-`Border` mit `Label Text="{Binding FeedInitial}"` sichtbar, wenn `FaviconUrl` leer/`null`.
   Muster: `ArticleCardView`-Kaskade; `StringNotEmptyToBoolConverter` ist in `Styles.xaml` registriert.

Beteiligte Klassen/Komponenten: `FeedListItem`, `FeedRepository`, `FeedsViewModel`, `FeedsPage`

### R3 — Automatischer Abruf bei Programmstart

1. `SettingsViewModel` erhält die bindbare Option `RefreshOnStartupEnabled` (Setter → `PersistOnChange`); `LoadAsync` liest `Settings.RefreshOnStartupEnabled`, `PersistAsync` schreibt es.
2. `SettingsPage.xaml` erhält in der Sektion **Synchronisation & Lesefluss** eine neue `Switch`-Zeile (Label + Hint, Muster der `AutoRefreshEnabled`-Zeile).
3. `AutoRefreshService.StartAsync`: nach `ApplySettingsAsync` wird bei `settings.RefreshOnStartupEnabled && _networkStatusService.IsOnline` ein fehlerisolierter, nicht abgewarteter `SyncAllAsync` gestartet.
4. Der App-Start wird nicht blockiert; `App.OnStart` bleibt unverändert (ruft `IAutoRefreshService.StartAsync` bereits im isolierten Block).

Beteiligte Klassen/Komponenten: `AutoRefreshService`, `SettingsViewModel`, `SettingsPage`, `IFeedSyncService`, `INetworkStatusService`

### R4 — Konfigurierbare Sortierung der Startseite

1. `SettingsViewModel` erhält `SortOrderOptions` (`SortOrderOption` mit `Value`/`Label`, Labels lokalisiert: „Neueste zuerst"/„Älteste zuerst") und `SelectedSortOrder`; `LoadAsync`/`PersistAsync` mappen auf `Settings.UnreadSortOrder` (Fallback `"desc"`).
2. `SettingsPage.xaml` erhält in **Synchronisation & Lesefluss** einen `Picker` (Muster `SelectedRefreshInterval`-Zeile).
3. `UnreadViewModel` erhält eine `ISettingsRepository`-Abhängigkeit; `LoadPageAsync` lädt die Settings, wertet `UnreadSortOrder` aus und ruft `_itemRepository.GetUnreadByDateAsync(page, PageSize, categoryId, ascending)`.
4. `ItemRepository.GetUnreadByDateAsync` wendet bei `ascending == true` `OrderBy(PublishedAt).ThenByDescending(Id)` an, sonst unverändert `OrderByDescending(PublishedAt).ThenBy(Id)`.
5. Artikel ohne `PublishedAt`: bestehendes SQLite-NULL-Verhalten bleibt unverändert (kein gesonderter Handling-Code); das Verhalten wird im Test dokumentiert.
6. `GetSavedForLaterAsync` (**Später**) bleibt fest absteigend — die Einstellung gilt nur für **Ungelesen**.

Beteiligte Klassen/Komponenten: `SettingsViewModel`, `SettingsPage`, `SortOrderOption` (neu), `SettingsValues`, `UnreadViewModel`, `IItemRepository`/`ItemRepository`, `DelegatingItemRepository`

### R5 — Neustart-Hinweis erst nach Sprachänderung

1. `SettingsViewModel` erhält `LanguageRestartHintVisible` (`bool`, `private set` mit `SetProperty`).
2. `LoadAsync` setzt das Flag auf `false` (nach dem Setzen von `SelectedLanguage`).
3. Der `SelectedLanguage`-Setter setzt — außerhalb von `_isLoading` — `LanguageRestartHintVisible = true`, wenn `value?.Value` vom geladenen `Settings.Language` (bzw. dem zuletzt persistierten Wert) abweicht; bei Rückkehr zum persistierten Wert wird es wieder `false`.
4. `SettingsPage.xaml`: der Hinweis-`Border` unter dem Sprach-Picker (Zeilen 470–477) erhält `IsVisible="{Binding LanguageRestartHintVisible}"`.

Beteiligte Klassen/Komponenten: `SettingsViewModel`, `SettingsPage`

### R6 — Austausch des Programmsymbols

1. Die beiden Issue-Attachments werden authentifiziert heruntergeladen (`curl -H "Authorization: Bearer $(gh auth token)" <user-attachments-URL>`).
2. Die Splash-Variante (mit „Reporter"-Schriftzug) wird vor dem Einchecken auf Resizetizer-Kompatibilität vorbereitet: das `<text>`-Element verwendet einen Google-Fonts-`@import`, den der MAUI-Resizetizer nicht auflöst — der Text wird daher vor dem Einchecken in SVG-Pfade umgewandelt (z. B. Inkscape „Objekt zu Pfad"). Icon-Variante ohne Text ist unverändert nutzbar.
3. `Resources/AppIcon/appiconfg.svg` ← Icon-Variante (ohne Text); `Resources/Splash/splash.svg` ← Splash-Variante (mit Schriftzug, Text als Pfade); `Resources/AppIcon/appicon.svg` (Hintergrund `#1e293b`) bleibt bestehen.
4. `Reporter.csproj`: `MauiIcon`/`MauiSplashScreen`-Verweise und `Color="#1e293b"` bleiben bestehen (die neue Grafik trägt die Badge-Optik selbst); `BaseSize` wird bei der Sichtprüfung verifiziert.
5. `Platforms/iOS/Info.plist` (`XSAppIconAssets`) und `Platforms/Windows/Package.appxmanifest` bleiben unverändert — sie verweisen auf die zur Build-Zeit generierten Assets.
6. Manuelle Sichtprüfung der generierten Icon-/Splash-Ausgabe (Windows-Build; iOS über `scripts/iOS-Deployment.ps1`, sofern verfügbar).

Beteiligte Klassen/Komponenten: `Reporter.csproj`, `Resources/AppIcon/*`, `Resources/Splash/splash.svg`

### R0 — Abspaltung in GitHub-Issues

1. Vor der Implementierung werden zwei Issues im Repo `martin-stromberg/Reporter` angelegt (via `gh issue create`):
   - **R7 — Debuginformationen sammeln und per E-Mail versenden:** fachliche Beschreibung inkl. der offenen Klärung (On-Demand-Snapshot vs. dauerhafte Protokollierung; Empfängeradresse; Gateway-Muster `IEmailService` → `Email.ComposeAsync` als technischer Anker).
   - **R8 — Benachrichtigungen nur bei Hintergrundabruf:** fachliche Beschreibung inkl. des Befunds, dass ein OS-seitiger Hintergrundabruf (`BGTaskScheduler`, `UIBackgroundModes`) erst aufgebaut werden muss und Vordergrund-Unterdrückung allein Benachrichtigungen komplett entfernen würde.
2. Die Issue-Nummern werden in `plan.md`/`todo.md` vermerkt.

Beteiligte Klassen/Komponenten: keine (Meta-Aufgabe)

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `IFeedIconService` (`src/Reporter.Core/Interfaces/`) | Interface | Contract der Favicon-Ermittlung: `FindFaviconUrlAsync(string siteUrl, CancellationToken)` → `Task<string?>` (R2) |
| `FeedIconService` (`src/Reporter.Core/Services/`) | Klasse | `HttpClient`-basierte Favicon-Discovery: `<link rel="icon…">`-Parsing (Muster aus `FeedSearchService.ExtractFeedLinks`), relative-URL-Auflösung, `/favicon.ico`-Fallback, Verifikation der Kandidaten (R2) |
| `FeedSiteResolver` (`src/Reporter.Core/Services/`) | Hilfsklasse (statisch) | Gemeinsame Site-URL-Auflösung (`siteUrl`, Fallback: Authority der Feed-URL) für `FeedIconService` und Test-Fakes — in der Review-Nacharbeit ergänzt, um die Duplikation zwischen `FeedIconService.TryFindFaviconUrlAsync` und `FakeFeedIconService` zu beseitigen (R2) |
| `SortOrderOption` (`src/Reporter.Core/ViewModels/`) | Datenmodellklasse | Picker-Option (`Value`/`Label`) analog `LanguageOption`/`RefreshIntervalOption` (R4) |
| EF-Migration `AddFeedFaviconUrl` | Migration | Spalte `feeds.favicon_url` (R2) |
| EF-Migration `AddSettingsStartupRefreshAndSortOrder` | Migration | Spalten `settings.refresh_on_startup_enabled`, `settings.unread_sort_order` (R3/R4) |

## Änderungen an bestehenden Klassen

### `ReadingTimeEstimator` (`src/Reporter.Core/Services/`)

- **Geänderte Methoden:** `EstimateText` — liefert `string.Empty`, wenn die geschätzte Lesezeit ≤ 1 Minute beträgt (zusätzlich zum bisherigen Leer-Fall); die Minuten-Klemme bleibt intern. Es wird **kein** `EstimateMinutes` ergänzt (R1).

### `ItemRepository` (`src/Reporter.Data/Repositories/`)

- **Geänderte Methoden:**
  - `GetUnreadByDateAsync(page, pageSize, categoryId, ascending)` — neuer Parameter `ascending` (Default `false`); bei `true` `OrderBy(PublishedAt).ThenByDescending(Id)` (R4).
  - `SelectListItemRows` — projiziert zusätzlich `i.Feed.FaviconUrl` (R2).
  - `MapToListItem` — befüllt `FeedFaviconUrl` (R2). Keine R1-Änderung nötig — `EstimateText` liefert bei ≤ 1 Minute bereits `string.Empty`.
- **Private Klasse `ItemListRow`:** neue Eigenschaft `FeedFaviconUrl` (`string?`).

### `IItemRepository` (`src/Reporter.Core/Interfaces/`)

- **Geänderte Signaturen:** `GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId = null, bool ascending = false)` (R4). `DelegatingItemRepository` und betroffene Test-Fakes werden mitgezogen.

### `ItemListItem` (`src/Reporter.Core/Models/`)

- **Neue Eigenschaften:** `FeedFaviconUrl` (`string?`, init) — Favicon-URL des Feeds für die Standardbild-Kaskade (R2); `FeedInitial` (`string`, get-only, berechnet aus `FeedTitle`) — Initialbuchstabe für den Kreis-Fallback (R2).
- **Geänderte Methoden:** `CopyWith` — kopiert `FeedFaviconUrl` mit (R2).

### `FeedListItem` (`src/Reporter.Core/Models/`)

- **Neue Eigenschaften:** `FaviconUrl` (`string?`, init) — Favicon-URL für die Bild-Spalte der Feed-Karte (R2); `FeedInitial` (`string`, get-only, berechnet aus `Title`) — Initialbuchstabe für den Kreis-Fallback (R2).

### `Feed` (Domänenmodell, `src/Reporter.Core/Models/`) und `Feed` (Entity, `src/Reporter.Data/Entities/`)

- **Neue Eigenschaften:** `FaviconUrl` (`string?`) — persistierte Favicon-URL der Feed-Webseite (R2).

### `FeedRepository` (`src/Reporter.Data/Repositories/`)

- **Geänderte Methoden:** `MapToModel`, `MapToEntity`, `UpdateAsync` — `FaviconUrl` mitführen (R2); `GetAllWithDetailsAsync` — projiziert `FaviconUrl = f.FaviconUrl` in `FeedListItem` (R2).

### `Settings` (Domänenmodell) und `Settings` (Entity)

- **Neue Eigenschaften:** `RefreshOnStartupEnabled` (`bool`, required; Entity-Default `true`, analog `AutoRefreshEnabled`) (R3); `UnreadSortOrder` (`string?`, Default `SettingsValues.SortOrderDescending`) (R4).

### `SettingsRepository` (`src/Reporter.Data/Repositories/`)

- **Geänderte Methoden:** `SaveAsync`, `MapToModel` — neue Felder mitführen (R3/R4).

### `ReporterDbContext` (`src/Reporter.Data/`)

- **Geänderte Methoden:** `ConfigureFeed` — Spalte `favicon_url` (max. 2048, nullable); `ConfigureSettings` — Spalten `refresh_on_startup_enabled` (required, Default `true`) und `unread_sort_order` (max. 50, nullable) (R2/R3/R4).

### `SettingsValues` (`src/Reporter.Core/Models/`)

- **Neue Konstanten:** `SortOrderDescending` (`"desc"`), `SortOrderAscending` (`"asc"`) (R4).

### `SettingsViewModel` (`src/Reporter.Core/ViewModels/`)

- **Neue Eigenschaften:** `RefreshOnStartupEnabled` (`bool`, Setter → `PersistOnChange`) (R3); `SortOrderOptions` (`IReadOnlyList<SortOrderOption>`) und `SelectedSortOrder` (Setter → `PersistOnChange`) (R4); `LanguageRestartHintVisible` (`bool`, `private set`) (R5).
- **Geänderte Methoden:**
  - `LoadAsync` — lädt die neuen Settings-Felder; setzt `LanguageRestartHintVisible = false` (R3/R4/R5).
  - `PersistAsync` — schreibt `RefreshOnStartupEnabled` und `UnreadSortOrder` in das `Settings`-Objekt (R3/R4).
  - `SelectedLanguage`-Setter — setzt/rücksetzt `LanguageRestartHintVisible` bei Abweichung vom persistierten Wert (R5).

### `UnreadViewModel` (`src/Reporter.Core/ViewModels/`)

- **Neue Abhängigkeit:** `ISettingsRepository` im Konstruktor (R4) — Anpassung in `MauiProgram` nicht nötig (DI löst auf), aber `UnreadViewModelTests`-Konstruktoraufrufe.
- **Geänderte Methoden:** `LoadPageAsync` — lädt Settings, wertet `UnreadSortOrder` aus, übergibt `ascending` an `GetUnreadByDateAsync` (R4).

### `FeedsViewModel` (`src/Reporter.Core/ViewModels/FeedsViewModel.cs` + `FeedsViewModel.Search.cs`)

- **Neue Abhängigkeit:** `IFeedIconService` im Konstruktor (in `FeedsViewModel.cs` ergänzen; `MauiProgram`-Registrierung nötig) (R2).
- **Geänderte Methoden:**
  - `TryPersistNewFeedAsync` — neue Signatur `(string url, string title, string? siteUrl)`; fehlerisolierter Favicon-Abruf bei `IsOnline`, Befüllung von `Feed.FaviconUrl` (R2).
  - `SubscribeResultAsync` — übergibt `result.SiteUrl`; `DirectAddAsync`/`OfferDirectAddAsync` übergeben `null` (R2).
  - `ToFeed` — führt `FaviconUrl = feed.FaviconUrl` mit, damit `RenameFeedAsync`/`ChangeFeedCategoryAsync`/`SaveAsync` (Edit) das Favicon nicht verlieren (R2).

### `FeedSyncService` (`src/Reporter.Core/Services/`)

- **Neue Abhängigkeit:** `IFeedIconService` (R2-Nachhole-Pfad).
- **Geänderte Methoden:**
  - `RunSyncAsync` — bei erfolgreichem Sync und `feed.FaviconUrl == null` Site-Link des Feed-Dokuments auswerten (Fallback: Host der Feed-URL), Favicon fehlerisoliert ermitteln und an `UpdateFeedHealthAsync` übergeben (R2).
  - `UpdateFeedHealthAsync` — neuer Parameter `faviconUrl`; persistiert die übergebene bzw. die bereits gespeicherte URL — reine Persistenz ohne Netzwerk-Seiteneffekt (R2, Review-Nacharbeit).

### `AutoRefreshService` (`src/Reporter.Core/Services/`)

- **Geänderte Methoden:** `StartAsync` — nach `ApplySettingsAsync` bei `settings.RefreshOnStartupEnabled && IsOnline` einen fehlerisolierten, nicht abgewarteten `SyncAllAsync` starten (R3).

### `ArticleDetailViewModel` (`src/Reporter/ViewModels/`)

- **Geänderte Methoden:** `LoadAsync` — `FeedIconUrl` aus `feed?.FaviconUrl` befüllen statt `string.Empty`; der `feed`-Datensatz wird in `LoadAsync` bereits geladen (Zeile 291) (R2). Keine R1-Änderung — `EstimateText` liefert bei ≤ 1 Minute `string.Empty`, die Anzeige blendet aus.
- **Pflicht-Folgeänderung:** der Fallback-`new Settings`-Initializer im `catch`-Block (Zeile 263) muss `RefreshOnStartupEnabled` setzen — sonst Kompilierbruch durch das `required`-Member (R3).

### `ArticleCardView` (`src/Reporter/Views/ArticleCardView.xaml`)

- Thumbnail-`Border` (Zeilen 76–93): Standardbild-Kaskade — `Image` auf `ImageUrl`, `Image` auf `FeedFaviconUrl` (Trigger: `ImageUrl` leer/null), Kreis-`Border` mit `FeedInitial`-`Label` (Trigger: beide leer/null). `IsOnline`-Trigger unverändert (R2).

### `FeedsPage` (`src/Reporter/Views/FeedsPage.xaml`)

- Feed-Karten-Template (Zeilen 143–254): neue Bild-Spalte im Karten-`Grid` — `Image` auf `FaviconUrl` (Trigger: nicht leer + `IsOnline` via `x:Reference` auf die Page), Kreis-`Border` mit `FeedInitial`-`Label` (Trigger: `FaviconUrl` leer/null). Muster: `ArticleCardView`-Kaskade (R2).

### `SettingsPage` (`src/Reporter/Views/SettingsPage.xaml`)

- Sektion **Synchronisation & Lesefluss**: neue `Switch`-Zeile „Beim Programmstart abrufen" (Muster `AutoRefreshEnabled`-Zeile, Zeilen 150–164) (R3); neue `Picker`-Zeile für die Sortierrichtung (Muster `SelectedRefreshInterval`-Zeile, Zeilen 175–188) (R4).
- Sektion **Sprache**: Hinweis-`Border` (Zeilen 470–477) erhält `IsVisible="{Binding LanguageRestartHintVisible}"` (R5).

### `AppResources` (`src/Reporter.Core/Resources/Strings/`)

- Neue Schlüssel in `AppResources.resx` (EN) + `AppResources.de.resx` (DE), `AppResources.Designer.cs` regenerieren: `SettingsRefreshOnStartupLabel`/`Hint` (R3), `SettingsSortOrderLabel`, `SettingsSortOrderNewest`/`Oldest` (R4). RESX-Konsistenz wird vom `translation-check`-Hook geprüft.

### `Reporter.csproj` / App-Assets

- `Resources/AppIcon/appiconfg.svg` und `Resources/Splash/splash.svg` ersetzen; `MauiIcon`/`MauiSplashScreen`-Einträge und `Color="#1e293b"` bestehen lassen, `BaseSize` verifizieren (R6).

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `AddFeedFaviconUrl` | `feeds.favicon_url` | Neue nullable String-Spalte (max. 2048) für die Favicon-URL des Feeds (R2) |
| `AddSettingsStartupRefreshAndSortOrder` | `settings.refresh_on_startup_enabled`, `settings.unread_sort_order` | Neuer boolescher Schalter für den Start-Abruf (R3, Default `true`) und nullable String-Spalte (max. 50) für die Sortierrichtung der Startseite (R4, Default `"desc"`) |

Hinweis: Die Migrationen werden mit `dotnet ef migrations add` gegen `Reporter.Data` erzeugt; der Seed (`entity.HasData(new Settings())`) erhält die neuen Defaults über die Entity-Property-Defaults.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `Settings.UnreadSortOrder` | Nur `"desc"`/`"asc"` zulässig (über `SortOrderOptions` eingeschränkt) | Ungültiger persistierter Wert → Fallback auf `"desc"` in `SettingsViewModel.LoadAsync` (Muster: `Language`-/`Theme`-Fallback) |
| `Feed.FaviconUrl` | Nur absolute `http(s)`-URLs werden vom `FeedIconService` zurückgegeben; Kandidaten müssen erfolgreich abrufbar sein | Kein Favicon gefunden/abrufbar oder Fehler → `null` speichern, Initialen-Fallback greift; Fehler blockieren Feed-Anlage/Sync nie |
| `SelectedLanguage`-Änderung | `LanguageRestartHintVisible` nur bei tatsächlicher Abweichung vom persistierten Wert | Gleiche Auswahl erneut getroffen → Hinweis verschwindet wieder |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `Settings.RefreshOnStartupEnabled` | `bool` | `true` | Schalter „Feeds beim Programmstart abrufen" (R3) |
| `Settings.UnreadSortOrder` | `string?` | `"desc"` (`SettingsValues.SortOrderDescending`) | Sortierrichtung der Startseite **Ungelesen** (R4) |
| `Feed.FaviconUrl` | `string?` | `null` | Pro Feed automatisch ermittelte Favicon-URL — kein Nutzer-Setting (R2) |
| `AppResources.*` (EN/DE) | RESX-Schlüssel | — | Lokalisierte Labels/Hints für die neuen Einstellungen (R3/R4) |

## Seiteneffekte und Risiken

- **R1 — Später-Liste und Detailansicht:** `MapToListItem` wird von `GetUnreadByDateAsync` **und** `GetSavedForLaterAsync` genutzt, `EstimateText` zusätzlich von `ArticleDetailViewModel` — die 1-Minuten-Unterdrückung gilt damit auf beiden Listen **und** in der Detailansicht. Das ist laut Klärung gewünscht; es ist dennoch eine Verhaltensänderung auf **Später** gegenüber dem Wortlaut „Auflistung" (dort aber konsistent).
- **R1 — Bestehender Test bricht bewusst:** `ReadingTimeEstimatorTests.EstimateText_ShortContent_ReturnsOneMinute` erwartet bisher „1 Min." für kurze Inhalte und wird auf `string.Empty` umgestellt.
- **R2 — Zusätzlicher HTTP-Request bei Feed-Anlage:** Der Favicon-Abruf verlängert die Anlage geringfügig (gebunden über Timeout des `HttpClient`, 30 s — ggf. kürzeres Zeitbudget für den Icon-Service festlegen); bei Offline-Anlage wird er übersprungen.
- **R2 — `FeedSyncService`-Abhängigkeit:** Der Nachhole-Pfad fügt dem Sync einen fehlerisolierten HTTP-Call hinzu; muss strikt so abgesichert werden, dass ein Favicon-Fehler den Sync-Erfolg nicht beeinflusst (bestehende Fehlerisolierung in `RunSyncAsync`/`UpdateFeedHealthAsync` beachten).
- **R2 — `FeedsViewModel.ToFeed`:** Rekonstruiert den `Feed` aus dem `FeedListItem` für `RenameFeedAsync`/`ChangeFeedCategoryAsync`/`SaveAsync` — ohne Mitnahme von `FaviconUrl` würde das Favicon bei jeder Teil-Aktualisierung gelöscht; im Plan als Pflichtänderung verankert.
- **R4 — Signaturbruch `IItemRepository`:** `DelegatingItemRepository`, Test-Fakes und `UnreadViewModelTests` (neue `ISettingsRepository`-Abhängigkeit) müssen mitgezogen werden; Build-Fehler zeigen alle Stellen.
- **R3 — Startlast:** Der Start-Sync läuft fire-and-forget parallel zum ersten Seitenaufbau; `FeedSyncService.SyncAllAsync` hat einen eigenen Offline-Guard. Keine Blockierung des Starts. Da der Default `true` ist, startet der Sync nach dem Update auf bestehenden Installationen sofort beim nächsten Start.
- **R3 — `required`-Member `Settings.RefreshOnStartupEnabled`:** Alle `new Settings`-Initializer ohne das Feld brechen kompiliertechnisch — im Produktivcode der Fallback-Initializer in `ArticleDetailViewModel.LoadAsync` (Zeile 263; `SettingsRepository.MapToModel` und `SettingsViewModel.PersistAsync` sind ohnehin geplante Änderungen), in den Tests die Stellen aus „Betroffene bestehende Tests". Die `TestSettingsHelper`-Erweiterung ist Pflicht, nicht optional.
- **R6 — Resizetizer-Kompatibilität:** Das Splash-SVG importiert eine Google-Font per CSS-`@import`, die beim Build-Rasterisieren nicht aufgelöst wird — die Umwandlung des `<text>`-Elements in Pfade vor dem Einchecken ist fest eingeplant.
- **Unverändert bleiben:** Benachrichtigungsverhalten (R8 abgespalten), **Später**-Sortierung (R4), `Package.appxmanifest`/`Info.plist` (R6).

## Umsetzungsreihenfolge

1. **GitHub-Issues für R7 und R8 anlegen (R0)**
   - Voraussetzungen: Keine (`gh`-CLI installiert und authentifiziert — verifiziert).
   - Beschreibung: Zwei Issues via `gh issue create` mit den fachlichen Beschreibungen aus `requirement.md` anlegen; Issue-Nummern in `todo.md`/`plan.md` vermerken.

2. **Persistenzgrundlagen: Modelle, Entities, Mappings, Migrationen**
   - Voraussetzungen: Keine (EF Core + `dotnet ef`-Tooling projektseitig vorhanden; Migrationen unter `src/Reporter.Data/Migrations/`).
   - Beschreibung: `Feed.FaviconUrl` (Modell + Entity), `Settings.RefreshOnStartupEnabled` (Default `true`), `Settings.UnreadSortOrder` (Modell + Entity), `SettingsValues`-Sortierkonstanten, `ReporterDbContext`-Spalten-Mappings, `FeedRepository`/`SettingsRepository`-Mappings und `UpdateAsync`/`SaveAsync` nachziehen, beide Migrationen erzeugen. XML-Dokumentation für alle neuen öffentlichen APIs (CS1591 ist Fehler).

3. **R1: Lesezeit-Unterdrückung in `EstimateText`**
   - Voraussetzungen: Keine.
   - Beschreibung: `ReadingTimeEstimator.EstimateText` liefert `string.Empty` bei geschätzter Lesezeit ≤ 1 Minute; Aufrufer (`ItemRepository.MapToListItem`, `ArticleDetailViewModel.LoadAsync`) bleiben unverändert.

4. **R2a: `IFeedIconService`/`FeedIconService` anlegen und registrieren**
   - Voraussetzungen: Schritt 2 (Favicon-Spalte nicht zwingend nötig, aber konsistente Reihenfolge); `HttpClient`-DI besteht.
   - Beschreibung: Interface in `Reporter.Core/Interfaces/`, Implementierung in `Reporter.Core/Services/` (Link-Tag-Parsing-Muster aus `FeedSearchService.ExtractFeedLinks` wiederverwenden, `/favicon.ico`-Fallback mit Verifikation), Singleton-Registrierung in `MauiProgram.CreateMauiApp`.

5. **R2b: Favicon-Erfassung bei Feed-Anlage und Sync-Nachholung**
   - Voraussetzungen: Schritte 2 und 4.
   - Beschreibung: `FeedsViewModel.TryPersistNewFeedAsync`-Signatur um `siteUrl` erweitern, `SubscribeResultAsync`/`DirectAddAsync`/`OfferDirectAddAsync` anpassen, fehlerisolierten Icon-Abruf mit `IsOnline`-Guard einbauen; `FeedSyncService` um den Nachhole-Pfad erweitern (Auflösung in `RunSyncAsync`, Persistenz über `UpdateFeedHealthAsync`).

6. **R2c: Standardbild-Anzeige auf Artikelkarte, Detailseite und Feeds-Seite**
   - Voraussetzungen: Schritte 2 und 5.
   - Beschreibung: `ItemListItem.FeedFaviconUrl`/`FeedInitial`, `ItemListRow`/Projektion in `ItemRepository`, `CopyWith` mitziehen; `ArticleCardView`-Kaskade (ImageUrl → Favicon → Initialen-Kreis); `ArticleDetailViewModel.FeedIconUrl` aus dem bereits geladenen `feed` befüllen; `FeedListItem.FaviconUrl`/`FeedInitial`, `GetAllWithDetailsAsync`-Projektion, `ToFeed`-Mitnahme, `FeedsPage`-Bild-Spalte (Favicon → Initialen-Kreis).

7. **R3: Start-Abruf-Schalter**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: `SettingsViewModel.RefreshOnStartupEnabled`, `LoadAsync`/`PersistAsync`-Mapping, `Switch`-Zeile in `SettingsPage`, `AutoRefreshService.StartAsync` um den fehlerisolierten Sofort-Sync erweitern; neue `AppResources`-Schlüssel.

8. **R4: Sortier-Einstellung**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: `SortOrderOption` anlegen, `SettingsViewModel.SortOrderOptions`/`SelectedSortOrder`, `Picker` in `SettingsPage`, `IItemRepository`-Signatur erweitern, `ItemRepository`-Implementierung, `DelegatingItemRepository`/Fakes/`UnreadViewModelTests` mitziehen, `UnreadViewModel`-Settings-Abhängigkeit; neue `AppResources`-Schlüssel.

9. **R5: Neustart-Hinweis-Sichtbarkeit**
   - Voraussetzungen: Keine (unabhängig von Schritt 2).
   - Beschreibung: `LanguageRestartHintVisible` in `SettingsViewModel` (Setzen im `SelectedLanguage`-Setter, Rücksetzen in `LoadAsync`), `IsVisible`-Binding auf den Hinweis-`Border` in `SettingsPage.xaml`.

10. **R6: Programmsymbol austauschen**
    - Voraussetzungen: Keine (Assets verifiziert beschaffbar: authentifizierter Download via `curl -H "Authorization: Bearer $(gh auth token)"`).
    - Beschreibung: Attachments herunterladen, Splash-`<text>` in Pfade umwandeln, `appiconfg.svg`/`splash.svg` ersetzen, `csproj`-Verweise/Farbe/`BaseSize` prüfen, generierte Plattform-Assets sichtprüfen.

11. **Tests schreiben und bestehende anpassen**
    - Voraussetzungen: Schritte 3–9.
    - Beschreibung: Neue und angepasste Tests gemäß Abschnitt „Tests" — inklusive der Pflicht-Anpassungen an den `new Settings`-Initializern (`TestSettingsHelper`, `AutoRefreshServiceTests`, `SettingsRepositoryTests`, `RetentionCleanupServiceTests`, `SettingsViewModelTests_Load`/`_Persist`); in `AutoRefreshServiceTests` steuern die Test-Settings `RefreshOnStartupEnabled` explizit (`false` für die bisherigen Zähler-Tests, `true` nur für die neuen Startup-Tests).

12. **Verifikation und Dokumentation**
    - Voraussetzungen: Alle vorherigen Schritte.
    - Beschreibung: `dotnet test Reporter.sln`, `.\scripts\Run-StaticChecks.ps1`, manuelle UI-Verifikation (390 × 844-pt-Windows-Fenster, Light + Dark) mit Screenshots in `test-results.md`/`docs/help/anwendung/mobile-ui-design.md` gemäß `AGENTS.md`; Hilfeseiten `docs/help/anwendung/synchronisation.md`, `ungelesen.md`, `sprache.md` (Neustart-Hinweis), `feed-suche.md`/`datenmodell.md` (Favicon inkl. Feeds-Seite) nachziehen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `EstimateText_OneMinuteContent_ReturnsEmpty` | `ReadingTimeEstimatorTests` | Inhalt mit geschätzter Lesezeit ≤ 1 Minute → `string.Empty` (R1) |
| `EstimateText_TwoMinuteContent_ReturnsText` | `ReadingTimeEstimatorTests` | Grenzfall oberhalb der Schwelle → formatierter Text (R1) |
| `GetUnreadByDateAsync_OneMinuteReadingTime_ProjectsEmpty` | `ItemRepositoryTests` | `ReadingTimeText` leer bei 1-Minuten-Items (R1) |
| `GetUnreadByDateAsync_ProjectsFeedFaviconUrl` | `ItemRepositoryTests` | Favicon-Projektion aus `Feed` (R2) |
| `GetUnreadByDateAsync_Ascending_OrdersByPublishedAtAscending` | `ItemRepositoryTests` | Aufsteigende Sortierung inkl. Tiebreaker-Umkehr (R4) |
| `GetUnreadByDateAsync_Ascending_NullPublishedAt_*` | `ItemRepositoryTests` | Dokumentiert das unveränderte SQLite-NULL-Verhalten bei `PublishedAt == null` (R4) |
| `GetAllWithDetailsAsync_ProjectsFaviconUrl` | `FeedRepositoryTests` | `FaviconUrl`-Projektion in `FeedListItem` für die Feeds-Seite (R2) |
| `FindFaviconUrlAsync_ParsesLinkTag` / `_ResolvesRelativeUrl` / `_FallsBackToFaviconIco` / `_ReturnsNullOnFailure` | `FeedIconServiceTests` (neu) | Link-Tag-Auswertung, relative URLs, Fallback-Pfad, Fehlertoleranz — mit `FakeHttpMessageHandler` (R2) |
| `TryPersistNewFeed_StoresFaviconUrl` / `_IconLookupFails_FeedStillAdded` / `_Offline_SkipsIconLookup` | `FeedsViewModelTests` | Favicon-Befüllung über `siteUrl`/Host, Fehlertoleranz, Offline-Guard (R2) — Fake für `IFeedIconService` nötig |
| `RenameFeedAsync_PreservesFaviconUrl` | `FeedsViewModelTests` | `ToFeed` führt `FaviconUrl` bei Teil-Updates mit (R2) |
| `SyncFeed_SetsFaviconWhenMissing` | `FeedSyncServiceTests` | Nachhole-Pfad bei erfolgreichem Sync ohne `FaviconUrl` (R2) |
| `StartAsync_StartupRefreshEnabled_SyncsImmediately` / `_Disabled_DoesNotSyncImmediately` / `_Offline_SkipsStartupSync` | `AutoRefreshServiceTests` | Sofort-Sync beim Start, Schalter aus, Offline-Guard (R3) — mit `FakeFeedSyncService`/`FakeNetworkStatusService` |
| `LoadPage_PassesSortOrderFromSettings` | `UnreadViewModelTests` | Übergabe `ascending` entsprechend `UnreadSortOrder` (R4) |
| `Load_PopulatesRefreshOnStartup` / `RefreshOnStartup_Change_Persists` | `SettingsViewModelTests_Load` / `_Persist` | Neues Flag laden/persistieren (R3) |
| `Load_PopulatesSelectedSortOrder` / `InvalidSortOrder_UsesDescFallback` / `SelectedSortOrder_Change_Persists` | `SettingsViewModelTests_Load` / `_Persist` | Sortier-Option laden, Fallback, Persistieren (R4) |
| `LanguageChange_SetsRestartHint` / `LanguageReverted_ClearsRestartHint` / `Load_ResetsRestartHint` | `SettingsViewModelTests_Persist` / `_Load` | `LanguageRestartHintVisible`-Verhalten (R5) |
| `SaveAsync_PersistsNewFields` | `SettingsRepositoryTests` | `RefreshOnStartupEnabled`/`UnreadSortOrder`-Roundtrip (R3/R4) |
| `UpdateAsync_PersistsFaviconUrl` | `FeedRepositoryTests` | `FaviconUrl`-CRUD (R2) |
| `E2E_RefreshOnStartup_PersistRoundtrip` / `E2E_SortOrder_PersistRoundtrip` | `SettingsViewModelTests_E2E` | Persistenz-Roundtrips über echte Repositories (R3/R4) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `ReadingTimeEstimatorTests.EstimateText_ShortContent_ReturnsOneMinute` | Verhaltensänderung: kurze Inhalte liefern jetzt `string.Empty` statt „1 Min." (R1) |
| `DelegatingItemRepository` | Neue `GetUnreadByDateAsync`-Signatur (`ascending`-Parameter) mitziehen (R4) |
| `UnreadViewModelTests` (Konstruktoraufrufe) | Neue `ISettingsRepository`-Abhängigkeit (R4) |
| `FeedsViewModelTests` (Konstruktoraufrufe) | Neue `IFeedIconService`-Abhängigkeit (R2) |
| `FeedSyncServiceTests` (Konstruktoraufrufe) | Neue `IFeedIconService`-Abhängigkeit (R2) |
| `KeywordFilterTests_E2E` (`CreateService`, `src/Reporter.Tests/KeywordFilterTests_E2E.cs` Zeile 60) | Kompilierbruch: `FeedSyncService` wird per `new FeedSyncService(...)` direkt instanziiert — Konstruktoraufruf um ein `IFeedIconService`-Fake erweitern (z. B. minimales Fake, das `null` zurückgibt) (R2) |
| `AutoRefreshServiceTests` | Kompilierbruch: `new Settings` in `BuildSettings` (Zeile 45) und `SaveSettingsAsync` (Zeile 60) ohne `required`-Member `RefreshOnStartupEnabled`. Semantischer Bruch: Der Sofort-Sync beim Start (Default `true`) erhöht `SyncAllCallCount` um eins — alle Zähler-Assertions (u. a. `StartAsync_InvokesSyncAfterInterval`, Zeilen 81–91, mit `== 1`) wären falsch. Beide Helper setzen `RefreshOnStartupEnabled` explizit (`false` in den Bestandstests, `true` in den neuen Startup-Tests) (R3) |
| `SettingsRepositoryTests` (`new Settings`, Zeilen 73, 100, 143, 170) | Kompilierbruch durch `required`-Member `RefreshOnStartupEnabled` — Initializer um das Feld ergänzen (R3) |
| `RetentionCleanupServiceTests.SetRetentionDaysAsync` (`new Settings`, Zeile 43) | Kompilierbruch durch `required`-Member `RefreshOnStartupEnabled` — Wert aus den geladenen Settings übernehmen (R3) |
| `TestSettingsHelper.SaveAsync` (`new Settings`, Zeile 34) | **Pflicht**-Erweiterung: `required`-Member `RefreshOnStartupEnabled` setzen und `UnreadSortOrder` aus dem geladenen Datensatz übernehmen — sonst würde jeder Helper-Aufruf die neuen Felder verlieren (R3/R4) |
| `SettingsViewModelTests_*` (Persist-Erwartungen und direkte Initializer) | `PersistAsync` baut `Settings` mit neuen Feldern; direkte `new Settings`-Initializer in `SettingsViewModelTests_Load` (Zeilen 50, 92) und `SettingsViewModelTests_Persist` (Zeilen 387, 428, 467, 527) brechen bei `required` — `RefreshOnStartupEnabled` ergänzen; `TestSettingsHelper`-Erweiterung ist Pflicht (R3/R4) |
| `ServiceCollectionTests` | `IFeedIconService`-Registrierung ergänzen (R2) |
| `ItemRepositoryTests.GetUnreadByDateAsync_*` | Weiterhin grün bei Default `ascending = false`; neue Ascending-Fälle ergänzen (R4) |

### E2E-Tests (primärer Funktionsnachweis)

Das Projekt besitzt keine UI-Testautomatisierung — als E2E-Nachweis dienen die etablierten In-Memory-E2E-Tests (`SettingsViewModelTests_E2E` mit echten SQLite-Repositories) für Persistenz-Roundtrips sowie die projektkonforme manuelle UI-Verifikation gemäß `AGENTS.md` (390 × 844-pt-Windows-Fenster, Light + Dark, Screenshots in `test-results.md`/`docs/help/anwendung/mobile-ui-design.md`). Eine rein unit-testbasierte Absicherung der sichtbaren Benutzerflüsse reicht nicht; daher ist die manuelle Verifikation als Pflichtschritt eingeplant.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Startseite **und** Artikeldetailansicht zeigen bei kurzem Beitrag keine Lesezeit | `ItemRepositoryTests` (Datenebene) + manuelle Verifikation `UnreadPage`/`ArticleDetailPage` | R1 | Sichtbarkeit entsteht erst im Zusammenspiel `EstimateText` → Converter → XAML |
| Pflicht | Artikelkarte ohne `ImageUrl` zeigt Favicon bzw. Initialen-Kreis | Manuelle Verifikation (Light + Dark, Screenshot) | R2 | Rein visueller Zustand, kein automatisierbarer UI-Test vorhanden |
| Pflicht | Feeds-Seite zeigt Favicon bzw. Initialen-Kreis in der Feed-Karte | Manuelle Verifikation (Light + Dark, Screenshot) | R2 | Rein visueller Zustand, kein automatisierbarer UI-Test vorhanden |
| Pflicht | Schalter „Beim Programmstart abrufen" persistiert und triggert Sync beim Start | `E2E_RefreshOnStartup_PersistRoundtrip` + `AutoRefreshServiceTests` + manuelle Verifikation | R3 | Zusammenspiel Settings → Service → Sync |
| Pflicht | Sortier-Picker ändert Reihenfolge der Startseite | `E2E_SortOrder_PersistRoundtrip` + `ItemRepositoryTests` + manuelle Verifikation | R4 | Einstellung → Repository → sichtbare Reihenfolge |
| Pflicht | Neustart-Hinweis erscheint erst nach Sprachänderung und verschwindet bei Rückwahl | `SettingsViewModelTests` + manuelle Verifikation | R5 | Sichtbarkeits-Binding nur UI-seitig prüfbar |
| Pflicht | Neues Icon/Splash wird im Build verwendet | Manuelle Sichtprüfung Windows-Build (+ iOS via `scripts/iOS-Deployment.ps1`, sofern verfügbar) | R6 | Generierte Assets nur visuell verifizierbar |

Welche bestehenden E2E-Tests müssen angepasst werden?

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `SettingsViewModelTests_E2E` | Neue Settings-Felder fließen in `PersistAsync`-Roundtrips ein; neue Roundtrip-Tests ergänzen (R3/R4) |

## Offene Punkte

Keine.

## Umsetzungsstand (Abschluss)

- **R0 erledigt:** R7 -> Issue #81, R8 -> Issue #82 (fachliche Beschreibung
  inkl. offener Klaerungspunkte jeweils im Issue-Body).
- **R1-R6 umgesetzt** wie geplant. Abweichung in R6: Die Wortmarke im Splash
  wurde nach Pfadkonvertierung weiss (`#ffffff`) statt dunkel (`#1e293b`)
  eingefaerbt, damit sie auf dem dunklen Splash-Hintergrund `Color="#1e293b"`
  sichtbar bleibt.
- **Verifikation:** `dotnet build Reporter.sln -c Release` 0 Warnungen/
  0 Fehler; `dotnet test` 400/400 bestanden; `Run-StaticChecks.ps1`
  Exit-Code 0 ohne Befund. Manuelle UI-Verifikation am 390 x 844-pt-
  Windows-Fenster (Dark + Light) durchgefuehrt und inkl. Screenshots in
  `test-results.md` dokumentiert (`test-results/issue-77/manual-*.png`).
- **Hilfeseiten** aktualisiert: `synchronisation.md`, `ungelesen.md`,
  `sprache.md`, `feed-suche.md`, `datenmodell.md`,
  `einstellungen/beschreibung.md`.
- **Bekannte Einschraenkung:** `.ico`-Favicons ohne PNG-Alternative werden
  gespeichert; die Decodierung obliegt dem Plattform-Renderer (unter Windows
  auf der Feeds-Seite verifiziert gerendert).
