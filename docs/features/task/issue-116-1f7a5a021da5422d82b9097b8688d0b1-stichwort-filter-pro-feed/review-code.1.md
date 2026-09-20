<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedDetailViewModel.cs (FeedDetailViewModel)

- **Doppelter Code** — `AddFeedKeywordAsync` (Z. 799–832) wiederholt die komplette Validierungs- und Persistierlogik aus `SettingsViewModel.AddKeywordAsync` (`src/Reporter.Core/ViewModels/SettingsViewModel.cs:840-876`) nahezu identisch: Trim → Leer-Prüfung (`ErrorKeywordEmpty`) → Längenprüfung (`ErrorKeywordTooLong`) → `OrdinalIgnoreCase`-Duplikatprüfung (`ErrorKeywordDuplicate`) → `AddAsync` → Liste ergänzen → Eingabe leeren. Auch die Konstante `MaxKeywordLength = 500` ist jetzt an zwei Stellen definiert (`FeedDetailViewModel.cs:26`, `SettingsViewModel.cs:22`) — ein geändertes Limit müsste doppelt gepflegt werden und kann auseinanderlaufen.

  Empfehlung: Die gemeinsame Validierung in eine geteilte Stelle auslagern, z. B. eine statische Hilfsmethode `TryValidateKeywordText(string? text, IEnumerable<Keyword> existing, out string errorResourceKey)` oder das Limit als `public const` im `Keyword`-Modell bzw. einer gemeinsamen Stelle führen und in beiden ViewModels nutzen.

- **Fehlerbehandlung** — `AddFeedKeywordAsync` (Z. 828–831) und `RemoveFeedKeywordAsync` (Z. 846–849) schlucken Exceptions mit nur `Debug.WriteLine`; der Anwender erhält keinerlei Rückmeldung, wenn der Repository-Aufruf fehlschlägt (das Stichwort erscheint dann lautlos nicht bzw. bleibt lautlos in der Liste). `SaveEditAsync` in derselben Klasse setzt dagegen im Fehlerfall `ErrorMessage = AppResources.ErrorActionFailed` (Z. 745) — die Behandlung ist innerhalb der Datei inkonsistent. Zusätzlich fehlt ein Guard auf den geladenen Feed: Wird `AddFeedKeywordCommand` ohne vorheriges `LoadAsync` ausgelöst, wird ein `Keyword` mit `FeedId = Guid.Empty` erzeugt (Z. 820), was zu einer FK-Verletzung führt, die dann ebenfalls still geschluckt wird.

  Empfehlung: Im `catch`-Block `FeedKeywordErrorMessage = AppResources.ErrorActionFailed` (oder `ErrorMessage`) setzen, damit der Fehler im Sheet sichtbar wird; optional am Methodenanfang `if (!HasFeed) { return; }` ergänzen.

### RetentionCleanupService.cs (RetentionCleanupService)

- **Effizienz / fehlende Vorbedingungsprüfung** — `CleanupAsync` (Z. 54) ruft `GetExpiredKeywordCandidatesAsync` jetzt unbedingt auf; der frühere Frühausstieg (`keywordTexts.Count > 0`, siehe gelöschte Zeilen im Diff) ist entfallen. Bei einer Installation ohne jegliche Stichworte lädt damit jeder Cleanup-Lauf sämtliche abgelaufenen Kandidaten-Items inklusive `ContentHtml` in den Speicher, ohne dass je ein Match möglich wäre.

  Empfehlung: Vor dem Laden der Kandidaten eine günstige Existenzprüfung ergänzen (z. B. `AnyAsync`-Methode auf `IKeywordRepository` bzw. einmal `GetKeywordTextsAsync(null)` plus Abbruch nur, wenn auch keine Feed-Stichworte existieren können), damit der Fall „keine Stichworte konfiguriert" weiterhin ohne Kandidaten-Query auskommt.

## Geprüfte Dateien

Liste aller geprüften Dateien (Basisbranch `origin/staging`, Merge-Base `1bd324a`, inkl. uncommitteter Änderungen und untracked Migration):

- `src/Reporter.Core/Interfaces/IKeywordFilter.cs`
- `src/Reporter.Core/Interfaces/IKeywordRepository.cs`
- `src/Reporter.Core/Models/Keyword.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Services/FeedSyncService.cs`
- `src/Reporter.Core/Services/KeywordFilter.cs`
- `src/Reporter.Core/Services/NotificationService.cs`
- `src/Reporter.Core/Services/RetentionCleanupService.cs`
- `src/Reporter.Core/ViewModels/FeedDetailViewModel.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Data/Entities/Keyword.cs`
- `src/Reporter.Data/Migrations/20260920094729_AddKeywordFeedId.cs`
- `src/Reporter.Data/Migrations/20260920094729_AddKeywordFeedId.Designer.cs`
- `src/Reporter.Data/Migrations/ReporterDbContextModelSnapshot.cs`
- `src/Reporter.Data/ReporterDbContext.cs`
- `src/Reporter.Data/Repositories/KeywordRepository.cs`
- `src/Reporter.E2ETests/E2EPageHelpers.cs`
- `src/Reporter.E2ETests/FeedDbAssertions.cs`
- `src/Reporter.E2ETests/FeedDetailTests.cs`
- `src/Reporter.Tests/FeedDetailViewModelTests.cs`
- `src/Reporter.Tests/FeedRepositoryTests.cs`
- `src/Reporter.Tests/FeedSyncServiceTests.cs`
- `src/Reporter.Tests/KeywordFilterTests_E2E.cs`
- `src/Reporter.Tests/KeywordRepositoryTests.cs`
- `src/Reporter.Tests/NotificationServiceTests.cs`
- `src/Reporter.Tests/RetentionCleanupServiceTests.cs`
- `src/Reporter.Tests/SettingsViewModelTests_Keywords.cs`
- `src/Reporter.Tests/TestDataSeeder.cs`
- `src/Reporter/Views/FeedDetailPage.xaml`

## Zusatzprüfungen (Projektregeln)

- **`RaiseUiActionRequested`:** nicht im Diff und nirgends im Repository verwendet — nichts zu prüfen.
- **Mobile-UI-Regeln (`FeedDetailPage.xaml`):** erfüllt — keine horizontalen Datentabellen; Touch-Ziele der neuen Buttons ≥ 44 × 44 pt (`MinimumWidthRequest`/`MinimumHeightRequest` bzw. feste 44 × 44); alle neuen Farben über `AppThemeBinding`; keine verschachtelte `CollectionView`/`ScrollView` (Stichwort-Chips via `FlexLayout` + `BindableLayout` in der höhenbegrenzten Sheet-`ScrollView`, die Artikel-`CollectionView` ist ein Geschwister-Element). Manuelle/automatisierte UI-Verifikation ist in `docs/help/anwendung/mobile-ui-design.md` dokumentiert.
- **Build-Verifikation:** `dotnet build` für `Reporter.Tests` (inkl. `Reporter.Core`, `Reporter.Data`) und `Reporter` (`net10.0-windows10.0.19041.0`, inkl. XAML-Kompilierung) erfolgreich — 0 Warnungen, 0 Fehler.
