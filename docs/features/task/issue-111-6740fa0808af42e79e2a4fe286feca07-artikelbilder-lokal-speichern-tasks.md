<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Artikelbilder lokal speichern für Offline-Verfügbarkeit — Issue #111

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `ItemImage`-Record anlegen (`src/Reporter.Core/Models/ItemImage.cs`: `Data` `byte[]`, `ContentType` `string?`, `Url` `string?`) | Offen | — |
| 2 | Datenmodell | `ItemContent`-Entity um `ImageData` (`byte[]?`), `ImageContentType` (`string?`), `ImageUrl` (`string?`) erweitern | Offen | — |
| 3 | Datenmodell | `ContentDbContext.ConfigureItemContent`: Spalten `image_data`, `image_content_type`, `image_url` mappen | Offen | — |
| 4 | Datenmodell | `ItemContentEntry`-Record um optionalen Positionsparameter `Image` (`ItemImage?`, Default `null`) erweitern; XML-Kommentar an feldweise Upsert-Semantik anpassen (`ContentHtml` `null` entfernt nur bei `Image == null`) | Offen | — |
| 5 | Datenmodell | `Item` (Domain-Modell) um `Image` (`ItemImage?`, `set` — dokumentierte Ausnahme von den `init`-Accessoren, da die Zuweisung erst nach der Download-Schleife erfolgt) erweitern | Offen | — |
| 6 | Datenmodell | `ItemListItem` um `LocalImageData` (`byte[]?`) und berechnetes `HasLocalImage` erweitern; `CopyWith` mitführen | Offen | — |
| 7 | Interfaces | `IItemContentStore` um `GetImageAsync`, `GetImagesAsync`, `GetImageIdsAsync` und optionalen `SetAsync`-Bild-Parameter erweitern; Upsert-/Entfern-Regel im Contract dokumentieren | Offen | — |
| 8 | Interfaces | `IItemImageService`-Interface anlegen (`src/Reporter.Core/Interfaces/IItemImageService.cs`: `ResolveImageUrl`, `TryDownloadImageAsync`) | Offen | — |
| 9 | Logik | `ItemContentRepository`: Bild-Lesemethoden `GetImageAsync`/`GetImagesAsync`/`GetImageIdsAsync` implementieren | Offen | — |
| 10 | Logik | `ItemContentRepository`: `SetAsync`/`SetRangeAsync` um feldweisen Bild-Upsert erweitern (`Image == null` lässt Bilddaten unberührt; `ContentHtml` `null`/leer lässt Inhalt bei `Image != null` unberührt; Zeile nur entfernen, wenn weder Inhalt noch Bild verbleiben) | Offen | — |
| 11 | Logik | `ItemImageService` implementieren (`src/Reporter.Core/Services/ItemImageService.cs`): Kandidaten-Priorität Enclosure → MediaRSS → `itunes:image` → erstes `<img src>`; relative-URL-Auflösung | Offen | — |
| 12 | Logik | `ItemImageService.TryDownloadImageAsync`: `image/*`-MIME-Prüfung, `Content-Length`-Vorabprüfung + Streaming-Cap (`MaxImageBytes` = 5 MB), `Try…`-Fehlerisolation | Offen | — |
| 13 | Logik | `ItemRepository`: `GetByIdAsync` + `MapToModel` hydratisieren `Item.Image` via `GetImageAsync` | Offen | — |
| 14 | Logik | `ItemRepository`: `AddAsync`/`AddRangeAsync` schreiben Bilddaten (`ItemContentEntry` mit `Image`); `AddAsync`-Guard und `AddRangeAsync`-Filter um `|| item.Image is not null` erweitern (bild-only-Items) | Offen | — |
| 15 | Logik | `ItemRepository`: paged Listenpfade (`GetUnreadByDateAsync(page,…)`, `GetSavedForLaterAsync`) laden `GetImagesAsync`; `MapToListItem` setzt `LocalImageData`; `ExtractImageUrl` auf gemeinsame Extraktion delegieren | Offen | — |
| 16 | Logik | `FeedSyncService`: `IItemImageService`-Konstruktorparameter aufnehmen | Offen | — |
| 17 | Logik | `FeedSyncService.CollectNewItems`: Bildkandidaten je neuem Item und je Bestandsitem ohne gespeichertes Bild ermitteln (`ResolveImageUrl`, `GetImageIdsAsync`-Menge) | Offen | — |
| 18 | Logik | `FeedSyncService.RunSyncAsync`: sequentielle Download-Schleife, `Item.Image` per `set`-Accessor setzen, Bild-Backfill-Treffer pro `ItemId` in `contentBackfill` einmergen (mit Content-Backfill zu `ItemContentEntry(id, content, image)` zusammenführen, sonst `(id, null, image)` anhängen), aggregierte `IDebugLogService`-Warnung | Offen | — |
| 19 | Logik | `ArticleHtmlSanitizer.Sanitize`: optionalen Parameter `localImage` — offline erstes `<img>` durch `data:`-URI ersetzen bzw. Header-Bild voranstellen, übrige `<img>` entfernen; `localImage`-Behandlung vor der `IsNullOrWhiteSpace`-Frührückkehr (leerer/`null`-Content + `localImage` → Header-Fragment für bild-only-Artikel) | Offen | — |
| 20 | Logik | `ArticleDetailViewModel`: `RebuildHtml` übergibt `Item?.Image` als `localImage`; `CreateItemCopy` führt `Image` | Offen | — |
| 21 | UI | `ArticleCardView.xaml`: neue Kaskaden-Stufe `Image Source="{Binding LocalImageData}"` (oberste Priorität) einfügen | Offen | — |
| 22 | UI | `ArticleCardView.xaml`: `HasLocalImage == False`-Bedingungen auf den Stufen `ImageUrl`, `FeedFaviconUrl`, `FeedInitial` ergänzen | Offen | — |
| 23 | UI | `ArticleCardView.xaml`: `IsOnline`-`DataTrigger` am Thumbnail-`Border` zum `MultiTrigger` erweitern (`IsOnline == False` und `HasLocalImage == False` → `IsVisible = False`) | Offen | — |
| 24 | Validierung | Bild-URL-Validierung in `ItemImageService`: nur absolute `http`/`https`-URIs, relative URLs gegen Item-Link dann Feed-URL auflösen | Offen | — |
| 25 | Validierung | Download-Antwort-Validierung in `ItemImageService`: `Content-Type` `image/*`, Größencap `MaxImageBytes` | Offen | — |
| 26 | Konfiguration/Start | `MauiProgram`: `.AddSingleton<IItemImageService, ItemImageService>()` registrieren | Offen | — |
| 27 | Migrationen | EF-Migration `AddItemContentImageColumns` für `ContentDbContext` scaffolden (`dotnet ef migrations add … --output-dir Migrations/Content`), Snapshot aktualisieren | Offen | — |
| 28 | Tests | `FakeItemImageService`-Test-Fake anlegen (`src/Reporter.Tests/FakeItemImageService.cs`, Muster `FakeFeedIconService`) | Offen | — |
| 29 | Tests | `FakeItemContentStore` um neue `IItemContentStore`-Member mit identischer Upsert-Semantik erweitern | Offen | — |
| 30 | Tests | `TestFeedXml` um Enclosure-/Bild-Fixture-Unterstützung erweitern (neue Überladung, bestehende Aufrufe unverändert) | Offen | — |
| 31 | Tests | `FeedSyncService`-Konstruktor-Aufrufe in `FeedSyncServiceTests` (×2), `FeedSyncServiceTests_DebugLog` (×2), `KeywordFilterTests_E2E` (×1) um `FakeItemImageService` ergänzen | Offen | — |
| 32 | Tests | `ServiceCollectionTests`: `IItemImageService` registrieren und Auflösbarkeit prüfen (`AddReporterServices_ResolvesFeedSyncService` erweitern oder neuer Test) | Offen | — |
| 33 | Tests | `ItemImageServiceTests` anlegen: Kandidaten-Priorität, relative URLs, MIME-Prüfung, Größencap, Fehlerisolation (via `FakeHttpMessageHandler`) | Offen | — |
| 34 | Tests | `ItemContentRepositoryTests` erweitern: Bild-Roundtrip, `GetImagesAsync`/`GetImageIdsAsync`, bild-only-Upsert, bild-only-Eintrag lässt gespeicherten `content_html` unberührt, Entfern-Regel, Löschpfade | Offen | — |
| 35 | Tests | `ItemRepositoryTests` erweitern: `Item.Image`-Hydratisierung, `LocalImageData`-Projektion, `AddAsync`/`AddRangeAsync` mit Bild inkl. bild-only-`AddAsync` (Guard-Nachweis), `UpdateAsync` lässt Bild unberührt, Mitlöschung in allen Löschpfaden | Offen | — |
| 36 | Tests | `FeedSyncServiceTests` erweitern: Enclosure-/`<img>`-Download, relative URL, Fehlerisolation (Sync erfolgreich, Remote-Fallback), Größenlimit integrativ via `FakeHttpMessageHandler` + echtem `ItemImageService` (nicht `FakeItemImageService`), Bild-Backfill, Item ohne Inhalt und ohne Bild erhält beides in einem Sync, kein Re-Download bei vorhandenem Bild | Offen | — |
| 37 | Tests | `RetentionCleanupServiceTests` erweitern: Waisen-Sweep entfernt bild-only-`item_contents`-Zeilen | Offen | — |
| 38 | Tests | `FeedRepositoryTests` erweitern: Feed-Kaskade entfernt Bilddaten der Feed-Artikel | Offen | — |
| 39 | Tests | `ArticleHtmlSanitizerTests` erweitern: `data:`-URI im ersten `<img>`, Header-Bild-Fallback ohne `<img>` und bei leerem/`null`-Content (bild-only-Artikel), übrige `<img>` entfernt, Online-Verhalten unverändert; `Sanitize_Offline_RemovesImages` ggf. präzisieren | Offen | — |
| 40 | Tests | `ContentDbContextTests` erweitern: `ItemContents_MappedToExpectedTable` zwingend um Bild-Spalten ergänzen (spaltenweise Prüfung) + Persistenz-Roundtrip | Offen | — |
| 41 | Tests | `ItemListItemTests` anlegen (Muster `FeedListItemTests`): `CopyWith_PreservesLocalImageData` — `LocalImageData`/`HasLocalImage` bleiben bei `CopyWith`-Kopien erhalten | Offen | — |
| 42 | E2E-Tests | `StubFeedServer` um `/images/{name}.png`-Endpunkt und Fixture-Feed `image-feed.xml` (mit `<enclosure>` bzw. `<img>`) erweitern | Offen | — |
| 43 | E2E-Tests | Content-DB-Assertion `ItemImageExistsAsync` anlegen (liest `reporter-content.db` neben `REPORTER_DB_PATH`, Muster `FeedDbAssertions`) | Offen | — |
| 44 | E2E-Tests | `ArticleImageTests` anlegen: Add + Sync → `image_data` in `reporter-content.db` gespeichert und Thumbnail auf Artikelkarte sichtbar (Happy Path) | Offen | — |
| 45 | E2E-Tests | `ArticleImageTests`: Detailansicht zeigt eingebettetes Bild (empfohlen); 404-Bild-URL → Sync erfolgreich ohne Thumbnail (optional) | Offen | — |
| 46 | Dokumentation | `docs/help/anwendung/offline.md`: lokale Artikelbilder offline sichtbar (Listenbilder + Detailansicht), Remote-Fallback dokumentieren | Offen | — |
| 47 | Dokumentation | `docs/help/anwendung/datenmodell.md`: neue `item_contents`-Spalten `image_data`/`image_content_type`/`image_url` dokumentieren | Offen | — |
| 48 | Qualitätssicherung | Manuelle UI-Verifikation der `ArticleCardView`-Änderung (390 × 844 pt, Dark Mode) mit Screenshot-Notiz gemäß `AGENTS.md` (`docs/help/anwendung/mobile-ui-design.md` oder `test-results.md`) | Offen | — |
| 49 | Qualitätssicherung | `dotnet test Reporter.sln --filter "Category!=E2E"` und `.\scripts\Run-StaticChecks.ps1` ohne Befund ausführen | Offen | — |
