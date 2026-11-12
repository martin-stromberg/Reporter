<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Lesepakete (GitHub Issue #164)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `UnreadPacket`-Record in `src/Reporter.Core/Models/UnreadPacket.cs` anlegen (`FromUtc`, `ToUtc`, `IncludeUndated`, `Days` als `IReadOnlyList<DateOnly?>` für das Dialog-Tageslabel) | Offen | — |
| 2 | Logik | `IItemRepository` erweitern: `ResolveUnreadPacketAsync`, `GetUnreadPacketCountAsync`, `MarkUnreadPacketAsReadAsync`; `GetUnreadByDateAsync` um optionalen `packet`-Parameter ergänzen | Offen | — |
| 3 | Logik | `ItemRepository`: `MinPacketSize = 5` Konstante und Paket-Prädikat-Hilfsmethode (Bereich + `IncludeUndated` + Kategoriefilter) implementieren | Offen | — |
| 4 | Logik | `ItemRepository.ResolveUnreadPacketAsync` implementieren (Anker-Ermittlung, Orts-Tagesgrenzen via `SpecifyKind(Utc).ToLocalTime()`, <5-Erweiterung auf Folgetag, Pseudo-Tag für `PublishedAt == null`, `Days` mit Anker-/Folgetag als lokale `DateOnly` befüllen) | Offen | — |
| 5 | Logik | `ItemRepository.GetUnreadByDateAsync` um Paket-Filter erweitern | Offen | — |
| 6 | Logik | `ItemRepository.MarkUnreadPacketAsReadAsync` und `GetUnreadPacketCountAsync` implementieren | Offen | — |
| 7 | Logik | `UnreadViewModel`: `_currentPacket`-Feld und Paket-Auflösung im `append == false`-Pfad von `LoadPageAsync` einbauen; dabei `_packetUnreadCount` (`GetUnreadPacketCountAsync`, 0 bei `null`-Paket) und `_packetDayLabel` (aus `UnreadPacket.Days`) setzen | Offen | — |
| 8 | Logik | `UnreadViewModel.MarkAllReadAsync` auf `MarkUnreadPacketAsReadAsync` umstellen (Guard `_currentPacket != null`; bei `null` Markierung überspringen, nur `LoadAsync`) | Offen | — |
| 9 | UI | `UnreadViewModel`: `MarkAllReadConfirmationText` als Getter (Paket-Anzahl `_packetUnreadCount` + Tageslabel `_packetDayLabel`, undatiert → „ohne Datum"); `MarkReadAsync` dekrementiert `_packetUnreadCount` (`Math.Max(0, …)`) und benachrichtigt `MarkAllReadConfirmationText`/`CardPositionText` | Offen | — |
| 9a | UI | `UnreadViewModel.CardPositionText`: Nenner auf Paket-Gesamtzahl umstellen (`Math.Max(_packetUnreadCount, _articles.Count)` statt `Math.Max(UnreadCount, _articles.Count)`) | Offen | — |
| 10 | UI | `UnreadPage.xaml.cs`: `OnMarkAllReadTapped` verwendet `MarkAllReadConfirmationText` als ActionSheet-Titel | Offen | — |
| 11 | UI | `AppResources.resx` + `AppResources.de.resx`: `MarkAllReadPacketTitleFormat` und `UnreadNoDateLabel` ergänzen; `AppResources.Designer.cs` neu generieren | Offen | — |
| 12 | Tests | `DelegatingItemRepository` um neue Member/Signatur erweitern; `RecordingItemRepository` (`LastPacket`) und `FailingItemRepository` anpassen | Offen | — |
| 13 | Tests | `ItemRepositoryTests`: Paket-Auflösung sortierneutral (Anker asc = ältester / desc = neuester Tag, <5-Randwerte, Folgetag = nächster belegter Tag, Pseudo-Tag asc erstes / desc letztes Paket, Kategoriefilter) — rot vor Implementierung | Offen | — |
| 14 | Tests | `ItemRepositoryTests`: Paket-Abfrage (`GetUnreadByDateAsync` mit/ohne Paket, Tagesgrenzen, Paging im Paket) | Offen | — |
| 15 | Tests | `ItemRepositoryTests`: `MarkUnreadPacketAsReadAsync` (nur Paket markiert, kompletter Paketbereich bei >`PageSize` unabhängig vom Ladezustand, Kategoriefilter, `IncludeUndated`) und `GetUnreadPacketCountAsync` (Gesamt-Paket-Anzahl) | Offen | — |
| 16 | Tests | `UnreadViewModelTests`: Paket-Bildung beim Laden, <5-Erweiterung, kein Nachrücken bei `MarkReadCommand`, Auto-Reload mit neuer Paket-Entscheidung beim Wegfall des letzten sichtbaren Artikels | Offen | — |
| 17 | Tests | `UnreadViewModelTests`: `MarkAllReadCommand` markiert nur Paket (inkl. erweitertem Paket), `SelectCategoryCommand` löst Paket neu auf, `LoadMoreCommand` endet am Paketende (inkl. Randfall volle letzte Seite), `MarkAllReadConfirmationText` (Paket-Umfang, Dekrement nach `MarkReadCommand`, Label-Komposition inkl. „ohne Datum"), `CardPositionText` mit Paket-Nenner | Offen | — |
| 18 | E2E-Tests | `src/Reporter.E2ETests/Fixtures/day-packet-feed.xml` anlegen (6+3+3 Artikel über 3 Kalendertage; `pubDate` jeweils zur Orts-Mitte des Tages, nicht nahe Mitternacht — zeitzonen-sicher) | Offen | — |
| 19 | E2E-Tests | `FeedDbAssertions`: `ItemHasReadFlagAsync` und `SetUnreadSortOrderAsync` ergänzen | Offen | — |
| 20 | E2E-Tests | `UnreadPacketTests`: Happy Path absteigend/Default (Paket des neuesten Tags → Doppelhaken → ActionSheet → Paket gelesen → Folgepaket) | Offen | — |
| 21 | E2E-Tests | `UnreadPacketTests`: Happy Path aufsteigend (`SetUnreadSortOrderAsync("asc")` → ältestes Tagespaket → Doppelhaken → nur dieses Paket gelesen) — Nachweis der Sortierneutralität | Offen | — |
| 22 | E2E-Tests | `UnreadPacketTests`: kein Folgetag-Nachrücken beim einzelnen Als-gelesen-Markieren (Swipe auf `paged-feed`); Fallback dokumentierte manuelle Abnahme | Offen | — |
| 23 | Verifikation | `dotnet test src/Reporter.Tests -c Release` vollständig grün; `scripts\Run-StaticChecks.ps1` Exit-Code 0; `scripts\Run-E2ETests.ps1` in interaktiver Session (sonst Eskalation an Anwender) | Offen | — |
