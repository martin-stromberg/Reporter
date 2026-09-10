# Plan-Review – Artikeldetailansicht mit WebView und Lesestatus

## Geprüfte Eingaben

- `plan.md`
- `inventory.md`
- `inventory/01-design-draft.md`
- `inventory/02-domain-and-data.md`
- `inventory/03-existing-ui.md`
- `inventory/04-mobile-ui-review.md`
- `inventory/05-open-questions.md`
- Implementierungsdateien in `src/`
- `docs/help/anwendung/mobile-ui-design.md`

## Gesamtstatus

**Offene Aufgaben vorhanden**

Die Artikeldetailansicht ist funktional implementiert und deckt die überwiegende Mehrheit der Plan-Schritte ab. Es verbleiben zwei offene Punkte, die gegen den Plan noch nicht vollständig geschlossen sind.

## Umsetzungsstand pro Plan-Schritt

| # | Plan-Schritt | Status | Anmerkungen |
|---|--------------|--------|-------------|
| 1 | Domain-/Daten-Layer prüfen und ergänzen | Erledigt | `Item` enthält alle benötigten Felder (`ContentHtml`, `IsRead`, `IsSavedForLater`, `Link`, `PublishedAt`, `Title`). `IItemRepository.GetByIdAsync`, `MarkAsReadAsync` und `ToggleSavedForLaterAsync` sind vorhanden. |
| 2 | ViewModel erstellen | Erledigt | `src/Reporter/ViewModels/ArticleDetailViewModel.cs` ist angelegt mit allen geforderten Properties, Commands, automatischem Lesen-Markieren und Lesezeitberechnung. |
| 3 | Detailseite erstellen | Erledigt | `src/Reporter/Views/ArticleDetailPage.xaml` (inkl. Code-Behind) ist vorhanden und umfasst Status-Pille, Header, WebView, Quellen-Footer und Floating Bottom Action Bar. |
| 4 | Navigation und Routing einrichten | Erledigt | Route `articledetail` in `AppShell.xaml.cs:20` registriert; `ArticleCardView.xaml.cs:86` navigiert mit `GoToAsync($"articledetail?itemId={item.Id}")`. |
| 5 | HTML-/WebView-Styling | Erledigt | HTML-Wrapper in `ArticleDetailViewModel.cs:289-341` generiert; Dark-Mode-CSS (`prefers-color-scheme`), Schriftgrößen-Toggle und Newsreader-Font enthalten. |
| 6 | Automatisch als gelesen markieren | Erledigt | Verzögerungslogik verwendet `ISettingsRepository.GetAsync()` und `AutoMarkReadDelaySeconds` aus den Einstellungen, mit 5-Sekunden-Fallback. |
| 7 | Mobile UI Design Review durchführen | Erledigt | Manuelle UI-Verifikation ist in `docs/help/anwendung/mobile-ui-design.md` (Zeilen 63-71) dokumentiert. |
| 8 | Akzeptanzkriterien final prüfen | Erledigt | Artikelinhalt, Lesestatus, Lesezeichen, Teilen, Browser-Öffnen, Lesezeit und Quellen-Footer sind funktional vorhanden. |

## Offene Aufgaben

| # | Thema | Verweis | Beschreibung |
|---|-------|---------|--------------|
| 1 | Feed-Icon in der Detailansicht | `plan.md:31-33` <br> `src/Reporter/ViewModels/ArticleDetailViewModel.cs:235` <br> `src/Reporter.Core/Models/Feed.cs:1-42` <br> `src/Reporter/Views/ArticleDetailPage.xaml:54-59` | Der Plan fordert, dass `FeedName` / Feed-Icon über `FeedId` verfügbar gemacht werden. `ArticleDetailViewModel` bindet `FeedIconUrl`, setzt es aber auf `string.Empty`, weil das `Feed`-Domain-Modell kein Icon- oder Image-Property enthält. Im Header wird stattdessen ein farbiger `BoxView` angezeigt. Entweder muss `Feed` um ein Icon-/Image-Property ergänzt und befüllt werden, oder die Anforderung muss explizit als Out-of-Scope markiert werden. |
| 2 | Fire-and-Forget `LoadAsync` | `src/Reporter/Views/ArticleDetailPage.xaml.cs:39` | `LoadAsync(itemId)` wird mit `_ = ...` gestartet; unbehandelte Ausnahmen können verloren gehen. Das ist keine explizite Plan-Zeile, folgt aber aus `plan.md:65` (Query-Parameter korrekt entgegennehmen und an ViewModel übergeben), da eine robuste Übergabe/Initialisierung gewährleistet sein sollte. Empfohlene Korrektur: `LoadAsync` entweder `await`en oder ein explizites Fehlerhandling für den Fire-and-Forget-Aufruf ergänzen. |

## Hinweise

- `ArticleDetailPage` und `ArticleDetailViewModel` sind vollständig angelegt und funktional.
- Touch-Targets der Floating Bottom Action Bar und des `Switch` sind mindestens 44 × 44 pt.
- Dark Mode wird über `AppThemeBinding` in der XAML-UI und via `prefers-color-scheme` im WebView-CSS unterstützt.
- Die konfigurierbare Gelesen-Verzögerung ist an `ISettingsRepository` angebunden und nicht mehr hartcodiert.
- HTML-Sanitisierung entfernt `<script>`, `<iframe>`, `on*` Event-Handler und `javascript:`-URLs.
- DI-Registrierung für `ArticleDetailPage` und `ArticleDetailViewModel` in `MauiProgram.cs:56-57` vorhanden.
