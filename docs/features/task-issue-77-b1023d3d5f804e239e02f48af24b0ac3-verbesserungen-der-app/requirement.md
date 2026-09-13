<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung: Verbesserungen der App (Issue #77)

## Fachliche Zusammenfassung

Das Issue bündelt acht eigenständige Verbesserungen an der .NET-MAUI-App `Reporter`: Unterdrückung der Lesezeit-Anzeige bei 1-Minuten-Kurzbeiträgen, ein Standardbild pro Beitrag (Feed-Favicon, sonst generierter Initialen-Kreis), ein konfigurierbarer Feed-Abgleich beim Programmstart, eine konfigurierbare Datums-Sortierung der Startseite **Ungelesen**, ein Neustart-Hinweis in den Einstellungen, der erst nach einer Sprachänderung erscheint, ein neues Programmsymbol (Icon- und SplashScreen-Variante), das Sammeln von Debuginformationen inklusive Versand per E-Mail über die Standard-Mail-Schnittstelle des Geräts sowie Systembenachrichtigungen ausschließlich bei Hintergrundabrufen. Die Anforderung enthält zudem eine Meta-Anweisung: Die acht Punkte sind zu bewerten, für diesen Lauf ist eine geeignete Teilmenge auszuwählen, und für die übrigen Punkte sind separate GitHub-Issues anzulegen, bevor die ausgewählten Aufgaben umgesetzt werden.

## Fachliche Einzelanforderungen

### R0 — Bewertung und Aufteilung (Meta-Anforderung)

Vor jeder Implementierung sind die acht Einzelanforderungen (R1–R8) hinsichtlich Umfang, Risiko und Zusammenhängen zu bewerten. Es ist festzulegen, welche Punkte in dieser Abarbeitung umgesetzt werden. Für alle abgespaltenen Punkte sind eigenständige GitHub-Issues mit fachlicher Beschreibung anzulegen; die getroffene Auswahl ist in den Arbeitsartefakten (`plan.md`, `todo.md`) nachvollziehbar zu dokumentieren.

### R1 — Lesezeit bei 1-Minuten-Beiträgen ausblenden

In der Auflistung der Feed-Beiträge (Artikelkarten `ArticleCardView` auf **Ungelesen** und **Später**) wird die geschätzte Lesezeit (`ItemListItem.ReadingTimeText`, befüllt über `ReadingTimeEstimator.EstimateText` in `ItemRepository.MapToListItem`) angezeigt. `ReadingTimeEstimator` klemmt das Ergebnis per `Math.Max(1, …)` auf mindestens 1 Minute — kurze Zusammenfassungen, die nur auf einen Hauptartikel verweisen, erhalten daher immer „1 Min.". Fachlich gefordert: Beträgt die geschätzte Lesezeit 1 Minute, wird die Lesezeit in der Auflistung nicht angezeigt. Annahme (zu verifizieren): Die Artikeldetailansicht (`ArticleDetailViewModel.ReadingTime`, `ArticleDetailPage`) bleibt unverändert, da die Anforderung explizit von der „Auflistung" spricht.

### R2 — Standardbild aus Favicon oder generiertem Initialen-Kreis

Hat ein Beitrag kein Bild (`ItemListItem.ImageUrl` ist `null` — aktuell aus dem ersten `<img>` des `ContentHtml` via `ItemRepository.ExtractImageUrl` extrahiert), soll ein Standardbild gezeigt werden. Kaskade:

1. **Feed-Favicon:** Bei der Anlage eines neuen Feeds ist für die Webseite des Feeds ein Favicon zu suchen und zu erfassen. Die Feed-Anlage läuft zentral über `FeedsViewModel.TryPersistNewFeedAsync` (`src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs`) — aufgerufen aus allen drei Anlage-Wegen (`DirectAddCommand`, `OfferDirectAddAsync`, `SubscribeResultCommand`). Als Quelle der „Webseite des Feeds" kommen `FeedSearchResult.SiteUrl` (bei Suche), der Host der Feed-URL (bei Direkt-Anlage) oder das `<link>`-Element des Feed-Dokuments beim ersten Sync in Betracht. Das Favicon ist persistent am Feed zu speichern (neue `Feed`-Eigenschaft, z. B. `FaviconUrl`, mit EF-Migration auf die `feeds`-Tabelle).
2. **Generiertes Ersatzbild:** Ist kein Favicon verfügbar, ist ein Bild zu generieren, das einen Kreis mit dem ersten Buchstaben des Feed-Namens zeigt. Annahme (zu verifizieren): Die einfachste Umsetzung ist kein echtes Bitmap, sondern ein in `ArticleCardView` gerenderter Kreis-`Border` mit `Label` (Initialbuchstabe aus `FeedTitle`), analog zu vorhandenen Badge-Mustern.

### R3 — Automatischer Abruf bei Programmstart

In den **Einstellungen** ist ein Schalter vorzusehen, der steuert, ob die Feeds beim Programmstart neu abgerufen werden. Neue `Settings`-Eigenschaft (z. B. `RefreshOnStartupEnabled`, `bool`) mit EF-Migration, `Switch` in der Sektion **Synchronisation & Lesefluss** der `SettingsPage` sowie Sofort-Persistierung über `SettingsViewModel`/`PersistAsync`. Die Umsetzung nutzt die vorhandenen Start-Hooks: `App.OnStart` ruft bereits fehlerisoliert `IRetentionCleanupService`, `IAppThemeService`, `INetworkStatusService` und `IAutoRefreshService.StartAsync` auf — der Start-Sync ist als weiterer isolierter Block (`try/catch`, `INetworkStatusService.IsOnline`-Guard) zu ergänzen, entweder direkt über `IFeedSyncService.SyncAllAsync` oder innerhalb von `AutoRefreshService.StartAsync`.

### R4 — Konfigurierbare Beitragssortierung der Startseite

In den **Einstellungen** ist wählbar, ob die Beiträge auf der Startseite (**Ungelesen**) nach Datum aufsteigend oder absteigend sortiert gelistet werden. Aktuell sortiert `IItemRepository.GetUnreadByDateAsync(page, pageSize, categoryId)` fest per `OrderByDescending(i => i.PublishedAt).ThenBy(i => i.Id)` (`ItemRepository`). Erforderlich: neue `Settings`-Eigenschaft für die Sortierrichtung (String-Konstanten in `SettingsValues`, z. B. `"desc"`/`"asc"`, mit EF-Migration), ein `Picker` in den Einstellungen (neue Optionsklasse analog `RefreshIntervalOption`/`LanguageOption`), ein Sortierparameter in der Repository-Abfrage (inklusive Richtungsumkehr des `ThenBy(Id)`-Tiebreakers) und die Auswertung der Einstellung in `UnreadViewModel.LoadPageAsync` — dafür benötigt `UnreadViewModel` eine neue `ISettingsRepository`-Abhängigkeit. Annahme (zu verifizieren): Die Sortierung gilt nur für **Ungelesen**; die **Später**-Liste bleibt absteigend sortiert.

### R5 — Neustart-Hinweis bei der Sprachwahl nur nach Änderung

Auf der `SettingsPage` ist unter dem Sprach-Picker ein statischer Info-`Border` mit `AppResources.SettingsLanguageRestartHint` immer sichtbar (`SettingsPage.xaml`). Fachlich gefordert: Der Hinweis erscheint erst, wenn die Spracheinstellung gegenüber dem persistierten Wert geändert wurde. Umsetzung über ein neues boolsches Flag in `SettingsViewModel` (z. B. `LanguageRestartHintVisible`), das im `SelectedLanguage`-Setter gesetzt wird (außerhalb von `_isLoading`, bei Abweichung vom geladenen `Settings.Language`) und in `LoadAsync` zurückgesetzt wird; Sichtbarkeits-Binding auf den Hinweis-`Border`.

### R6 — Neues Programmsymbol

Das Programmsymbol ist zu ersetzen; zwei Bildvarianten liegen als GitHub-Attachments im Issue vor: eine Variante mit App-Name für den SplashScreen und eine ohne für das Geräte-Icon. Betroffen sind `Resources/AppIcon/appicon.svg` + `appiconfg.svg` und `Resources/Splash/splash.svg` sowie die `MauiIcon`-/`MauiSplashScreen`-Einträge in `Reporter.csproj` (Hintergrundfarbe `#1e293b`, `BaseSize`). Abhängig vom gelieferten Format (PNG/SVG) sind die Assets unter `Resources/` abzulegen und die Verweise anzupassen; ggf. sind `Platforms/iOS/Info.plist` (`XSAppIconAssets`) und `Platforms/Windows/Package.appxmanifest` zu prüfen.

### R7 — Debuginformationen sammeln und per E-Mail versenden

In den **Einstellungen** soll die Sammlung von Debuginformationen aktivierbar sein; die gesammelten Informationen sollen per E-Mail an den Entwickler versendbar sein — über die Standard-Aufrufschnittstelle für den Mailversand des Geräts, sodass der Anwender die E-Mail sieht und selbst abschickt (kein direkter SMTP-Versand). Technisch naheliegend: `Microsoft.Maui.ApplicationModel.Communication.Email` (`Email.ComposeAsync`/`EmailMessage`, Bestandteil von .NET MAUI Essentials, kein zusätzliches Paket nötig). Da `Reporter.Core` (`net10.0`) keine MAUI-Referenz hat, ist das Gateway-Muster aus `IAppThemeService`/`ILocalNotificationService`/`INetworkStatusService` zu übernehmen: Interface in `Reporter.Core/Interfaces/`, Implementierung in `src/Reporter/Services/`. Als Informationsquellen bieten sich `ISyncLogRepository` (Sync-Protokoll), der `Settings`-Datensatz, der Feed-Health-Status sowie App-/Geräte-/OS-Versionsangaben (`AppInfo`/`DeviceInfo`) an. Fachlich offen ist, ob „gesammelt werden" eine dauerhafte Protokollierung (aktuell existiert außer `SyncLog` und `Debug.WriteLine` kein persistiertes Log) oder eine On-Demand-Momentaufnahme bedeutet — siehe Offene Fragen.

### R8 — Systembenachrichtigungen nur bei Hintergrundabruf

Findet ein Feed-Abruf statt, während die App ausgeführt wird, sollen keine Systemmitteilungen erscheinen; nur bei einem Hintergrundabruf sollen Benachrichtigungen ausgelöst werden. Aktueller Stand: `FeedSyncService.RunSyncAsync` ruft `INotificationService.NotifyNewItemsAsync` nach jedem Sync auf (manuell, Pull-to-Refresh, `AutoRefreshService` bei geöffneter App), und `NotificationDelegate.WillPresentNotification` (`Platforms/iOS`) zeigt Banner/List/Sound auch im Vordergrund. Es existiert **kein** OS-seitiger Hintergrundabruf (`docs/help/einstellungen/architektur.md`: „es gibt keinen OS-seitigen Background-Fetch"); ohne einen solchen würden Benachrichtigungen nach dieser Regel nie mehr erscheinen. Die Anforderung impliziert daher zwei Teilaspekte: (a) Unterdrückung der Vordergrund-Darstellung (z. B. `WillPresentNotification` → `UNNotificationPresentationOptions.None` oder Auswertung des App-Zustands im `NotificationService`) und (b) Aufbau eines echten Hintergrundabrufs (iOS: `BGTaskScheduler`/`BGAppRefreshTask`, `UIBackgroundModes` in `Info.plist`; Windows: keine entsprechende Infrastruktur vorhanden). Dies ist der umfangreichste Einzelpunkt und ein primärer Kandidat für die Abspaltung in ein separates Issue (R0).

## Betroffene Klassen und Komponenten

### Datenmodellklassen

- `src/Reporter.Core/Models/Settings.cs` + `src/Reporter.Data/Entities/Settings.cs` — neue Eigenschaften: Start-Abruf-Schalter (R3), Sortierrichtung (R4), Debug-Schalter (R7); jeweils mit EF-Core-Migration(en) auf die Singleton-Tabelle `settings`.
- `src/Reporter.Core/Models/Feed.cs` + `src/Reporter.Data/Entities/Feed.cs` — neue Eigenschaft `FaviconUrl`/`ImageUrl` (R2) mit Migration auf `feeds`.
- `src/Reporter.Core/Models/FeedListItem.cs`, `src/Reporter.Core/Models/ItemListItem.cs` — neue Anzeige-Felder für das Standardbild (Favicon-URL bzw. Initialen-Fallback; `CopyWith` in `ItemListItem` mitziehen) (R2).
- `src/Reporter.Core/Models/SettingsValues.cs` — Konstanten für die Sortierrichtung (R4).
- Neue Optionsklasse `SortOrderOption` in `src/Reporter.Core/ViewModels/` analog `RefreshIntervalOption`/`LanguageOption` (R4).

### Logikklassen / Services

- `src/Reporter.Core/Services/ReadingTimeEstimator.cs` — Erweiterung um Minuten-Ausgabe bzw. 1-Minuten-Unterdrückung für Listen (R1).
- `src/Reporter.Data/Repositories/ItemRepository.cs` — `MapToListItem`/`SelectListItemRows`: `ReadingTimeText`-Unterdrückung bei 1 Minute (R1), Favicon-Projektion aus `Feed` (R2), parametrisierbare Sortierrichtung in `GetUnreadByDateAsync` (R4).
- `src/Reporter.Core/Interfaces/IItemRepository.cs` — Signaturerweiterung um Sortierrichtung (R4); `src/Reporter.Tests/DelegatingItemRepository.cs` und Test-Fakes anpassen.
- `src/Reporter.Core/ViewModels/UnreadViewModel.cs` — neue `ISettingsRepository`-Abhängigkeit, Übergabe der Sortierrichtung in `LoadPageAsync` (R4).
- `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs` — `TryPersistNewFeedAsync` um die Favicon-Ermittlung bei Feed-Anlage erweitern (R2).
- Neuer Service `IFeedIconService`/`FeedIconService` (`src/Reporter.Core/`) — Favicon-Discovery: `<link rel="icon|shortcut icon|apple-touch-icon">` im HTML der Feed-Webseite auswerten (Parsing-Muster aus `FeedSearchService.ExtractFeedLinks` wiederverwenden), Fallback `/favicon.ico` am Host; `HttpClient`-basiert, unit-testbar (R2).
- `src/Reporter.Core/Services/AutoRefreshService.cs` bzw. `src/Reporter/App.xaml.cs` — optionaler einmaliger `SyncAllAsync` beim Start (R3).
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs` — neue bindbare Optionen (R3, R4, R7), `LanguageRestartHintVisible`-Flag (R5), E-Mail-Versand-Command (R7).
- `src/Reporter.Core/Services/NotificationService.cs` + `src/Reporter/Platforms/iOS/NotificationDelegate.cs` — Unterdrückung der Vordergrund-Benachrichtigung (R8); ggf. neue Hintergrundabruf-Infrastruktur unter `Platforms/iOS/` und `Info.plist`-`UIBackgroundModes`.
- Neues Interface `IDebugInfoService`/`IDiagnosticsService` + Implementierung sowie E-Mail-Gateway (z. B. `IEmailService`/`EmailService` in `src/Reporter/Services/`) nach dem Gateway-Muster (R7).

### Interfaces

- `IFeedIconService`, `IDebugInfoService`/`IDiagnosticsService`, `IEmailService` — neu in `src/Reporter.Core/Interfaces/`; Registrierung als Singletons in `MauiProgram.CreateMauiApp`.

### Enums / Konstanten

- `SettingsValues` — neue Konstanten `SortOrderDescending`/`SortOrderAscending` (o. ä.) (R4). Bestehende Enums (`FeedHealth`, `NotificationAuthorizationStatus`, `FeedSearchMatchKind`) bleiben unverändert.

### UI-Komponenten / Ressourcen

- `src/Reporter/Views/SettingsPage.xaml` — neue Sektions-Einträge: Schalter Start-Abruf (R3), Picker Sortierung (R4), Debug-Sektion mit Schalter und Versand-Aktion (R7); `IsVisible`-Binding des Sprach-Neustart-Hinweises (R5).
- `src/Reporter/Views/ArticleCardView.xaml` — Fallback-Darstellung des Thumbnails bei fehlendem `ImageUrl`: Favicon-`Image` bzw. Kreis-`Border` mit Initialen-`Label` (R2); Lesezeit-Sichtbarkeit unverändert über `StringNotEmptyToBoolConverter` (R1 greift bereits auf Datenebene).
- `src/Reporter/Resources/AppIcon/appicon.svg`, `appiconfg.svg`, `src/Reporter/Resources/Splash/splash.svg`, `src/Reporter/Reporter.csproj` (`MauiIcon`/`MauiSplashScreen`) — Austausch des Programmsymbols (R6); ggf. `Platforms/iOS/Info.plist`, `Platforms/Windows/Package.appxmanifest`.
- `src/Reporter.Core/Resources/Strings/AppResources.resx` + `AppResources.de.resx` — neue lokalisierte Schlüssel für sämtliche neuen Labels, Hints und Accessibility-Texte (EN/DE; RESX-Konsistenz wird durch den `translation-check`-Hook geprüft).

### Tests (`src/Reporter.Tests`)

- `ReadingTimeEstimatorTests` — 1-Minuten-Regel (R1).
- `ItemRepositoryTests` — Projektion mit unterdrückter Lesezeit und Favicon (R1/R2), auf-/absteigende Sortierung (R4).
- `UnreadViewModelTests` — Übergabe der Sortierrichtung, Settings-Abhängigkeit (R4).
- `SettingsViewModelTests_Load`/`_Persist`/`_E2E` — neue Optionen, Hint-Flag bei Sprachänderung (R3/R4/R5/R7).
- Neue Tests für `FeedIconService` (HTML-Parsing, Fallback-Pfad) (R2), `AutoRefreshServiceTests`/Start-Sync (R3), `NotificationServiceTests` (R8, sofern im Scope).
- Manuelle UI-Verifikation am 390 × 844-pt-Windows-Fenster (Light + Dark) mit Screenshots in `test-results.md` bzw. `docs/help/anwendung/mobile-ui-design.md` gemäß `AGENTS.md`; iOS-Verifikation über `scripts/iOS-Deployment.ps1`.

## Implementierungsansatz

1. **Triage (R0):** Die acht Punkte bewerten und aufteilen. Kleine, risikoarme Punkte (R1, R3, R4, R5) bilden den natürlichen Kern des Laufs; R6 ist vom externen Asset abhängig; R2 ist mittlerer Umfang (neue Persistenz + Discovery + UI-Fallback); R7 ist mittlerer Umfang mit offenem fachlichem Rahmen; R8 erfordert ggf. eine neue iOS-Hintergrundinfrastruktur und ist der stärkste Abspaltungskandidat. Für abgespaltene Punkte GitHub-Issues anlegen und die Entscheidung dokumentieren.
2. **Persistenz zuerst:** EF-Migration(en) für neue `settings`- und `feeds`-Spalten; Domänen-/Entity-/Repository-Mappings nachziehen (`Settings`, `Feed`, `FeedRepository`, `SettingsRepository`); XML-Dokumentation für alle neuen öffentlichen APIs (CS1591 ist Fehler).
3. **R1:** `ReadingTimeEstimator` um eine Minuten-Auswertung erweitern (z. B. `EstimateMinutes`); `ItemRepository.MapToListItem` setzt `ReadingTimeText` bei 1 Minute auf `null` — die bestehende Sichtbarkeitslogik der Karte greift ohne XAML-Änderung.
4. **R2:** `Feed.FaviconUrl` einführen; `IFeedIconService` bei `TryPersistNewFeedAsync` aufrufen (Fehler tolerant, darf die Anlage nicht blockieren; offline bleibt `FaviconUrl` leer); `ItemListItem`-Projektion um das Feed-Bild erweitern; `ArticleCardView` zeigt `ImageUrl` → `FaviconUrl` → Initialen-Kreis. Optional nachreichen: Favicon beim ersten erfolgreichen Sync nachholen, wenn bei der Anlage keines gefunden wurde (`FeedSyncService.UpdateFeedHealthAsync`-Pfad).
5. **R3:** `Settings`-Flag + Schalter in Sektion **Synchronisation & Lesefluss**; Start-Sync als isolierter Block in `App.OnStart` (oder in `AutoRefreshService.StartAsync`), mit `IsOnline`-Guard und ohne Blockierung des Starts.
6. **R4:** `Settings`-Sortierfeld + `Picker`; `IItemRepository.GetUnreadByDateAsync` um Richtungsparameter erweitern (Signaturbruch in Fakes/Tests mitziehen); `UnreadViewModel` liest die Einstellung beim Laden.
7. **R5:** `LanguageRestartHintVisible` in `SettingsViewModel` + `IsVisible`-Binding im Hinweis-`Border`.
8. **R6:** Attachments herunterladen, als `MauiIcon`/`MauiSplashScreen`-Assets einbinden, Hintergrundfarbe abstimmen, generierte Plattform-Assets verifizieren (manuelle Sichtprüfung Windows/iOS).
9. **R7:** Einstellungs-Schalter + Versand-Command; Debug-Paket aus `SyncLog`/`Settings`/Feed-Health/Geräteinfos zusammenstellen; Versand über `Email.ComposeAsync` mit vorbefülltem Empfänger/Betreff/Body — der Anwender prüft und sendet selbst.
10. **R8:** Entscheidung aus der Triage abhängig — minimal die Vordergrund-Unterdrückung in `NotificationDelegate`/`NotificationService`, vollständig erst mit OS-Hintergrundabruf.
11. **Verifikation:** `dotnet test Reporter.sln`, `.\scripts\Run-StaticChecks.ps1`, manuelle UI-Verifikation inkl. Screenshots; Hilfeseiten unter `docs/help/` nachziehen.

## Konfiguration

- `Settings.RefreshOnStartupEnabled` (`bool`) — Schalter „Feeds beim Programmstart abrufen" (R3); Default-Wert ist fachlich zu klären.
- `Settings.UnreadSortOrder`/`SortOrder` (`string`, `"desc"`/`"asc"`) — Sortierrichtung der Startseite, `Picker` in den Einstellungen (R4); Default `"desc"` (bisheriges Verhalten).
- `Settings.DebugInfoEnabled` o. ä. (`bool`) — Schalter „Debuginformationen sammeln" (R7); zusätzlich eine manuelle Versand-Aktion.
- `Feed.FaviconUrl` (`string?`) — pro Datensatz, automatisch bei der Feed-Anlage erfasst (kein Nutzer-Setting) (R2).
- Alle neuen Optionen folgen dem bestehenden Muster: Sofort-Persistierung über `SettingsViewModel.PersistAsync`, Singleton-`settings`-Tabelle via `ISettingsRepository`, lokalisierte Labels in `AppResources`.

## Offene Fragen

- **R8 — Hintergrundabruf:** Es existiert kein OS-seitiger Background-Fetch; ein reines Unterdrücken der Vordergrund-Banner würde bedeuten, dass nie wieder Benachrichtigungen erscheinen. Ist die Einrichtung eines echten iOS-Hintergrundabrufs (`BGTaskScheduler`/`BGAppRefreshTask`, `UIBackgroundModes`) Teil dieser Anforderung, oder soll der Punkt in ein separates Issue ausgelagert werden? Gibt es eine Erwartung für Windows (dort existiert keine Hintergrundinfrastruktur)?
- **R2 — Favicon:** Welche Quelle gilt als „Webseite des Feeds" — `SiteUrl` aus der Suche, der Host der Feed-URL oder das `<link>`-Ziel des Feed-Dokuments? Wird die Favicon-URL gespeichert oder die Bilddatei lokal abgelegt (Offline-Betrachtung: Thumbnails werden offline bereits ausgeblendet)? Soll das Standardbild auch auf der **Feeds**-Seite oder nur auf Artikelkarten erscheinen? Genügt für den „generierten" Kreis eine XAML-Darstellung (Kreis + Initialen-Label) statt einer echten Bitmap-Generierung? Soll das Favicon bei Bestandsfeeds nachträglich ermittelt werden?
- **R1 — Schwellwert:** Gilt die Regel exakt bei 1 Minute (die Untergrenze des Estimators) oder „≤ 1 Minute"? Bleibt die Lesezeit in der Artikeldetailansicht auch bei 1 Minute sichtbar?
- **R4 — Sortierung:** Gilt die Einstellung nur für **Ungelesen** (Startseite) oder auch für **Später**? Wie werden Artikel ohne `PublishedAt` einsortiert (aktuell fallen sie in SQLite ans Ende/Anfang je nach Richtung)?
- **R3 — Start-Abruf:** Soll der Schalter standardmäßig ein- oder ausgeschaltet sein? Soll der Start-Sync die erste Anzeige blockieren oder im Hintergrund laufen (naheliegend: im Hintergrund, Liste aktualisiert sich nach Abschluss)?
- **R5 — Hint-Logik:** Verschwindet der Hinweis wieder, wenn die Auswahl auf die gespeicherte Sprache zurückgestellt wird (naheliegend) — und bleibt er bis zum Neustart bestehen oder nur für die Sitzung?
- **R6 — Assets:** Welche der beiden Attachment-URLs ist die SplashScreen-Variante (mit App-Name) und welche das Icon? In welchem Format liegen sie vor (PNG/SVG), und welche Hintergrundfarbe/Zuschnitt-Vorgaben gelten für `MauiIcon`/`MauiSplashScreen`?
- **R7 — Debuginformationen:** An welche Empfängeradresse geht die E-Mail? Welcher Daten-Umfang ist gewünscht (Sync-Protokoll, Einstellungen, Geräte-/App-Version, Fehlerprotokoll)? Bedeutet „gesammelt werden" eine dauerhafte Protokollierung (derzeit existiert kein persistiertes Fehlerlog außer `SyncLog`) oder eine Momentaufnahme beim Versand? Ist der Versand auch bei ausgeschalteter Sammlung möglich? Verhalten, wenn auf dem Gerät kein Mail-Konto eingerichtet ist (`Email.ComposeAsync` wirft `FeatureNotSupportedException`)?
- **R0 — Triage-Kriterien:** Nach welchen Kriterien wird die „beste" Auswahl getroffen (Aufwand, Abhängigkeiten, Kundennutzen), und über welchen Weg werden die Folge-Issues angelegt (`gh`-CLI, GitHub-MCP, manuell)?
