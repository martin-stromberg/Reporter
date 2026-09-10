# Review – Implementierungsplan Artikeldetailansicht

## Ausgangslage
Geprüft wurden:
- `plan.md`
- `inventory.md` und alle Detaildokumente unter `inventory/`
- Implementierungsdateien in `src/`
- `docs/help/anwendung/mobile-ui-design.md`

## Gesamtstatus

**Offene Aufgaben vorhanden**

Die Artikeldetailansicht ist funktional implementiert, weicht jedoch in einigen vom Plan geforderten Details ab.

## Offene Aufgaben / Abweichungen

| # | Plan-Anforderung | Status | Verweis |
|---|------------------|--------|---------|
| 1 | `OpenArticleCommand` in `UnreadViewModel` bzw. `ArticleCardView` ergänzen, das `GoToAsync($"articledetail?itemId={id}")` aufruft. | Nicht exakt umgesetzt. Navigation existiert, aber als `Command` im Code-Behind von `UnreadPage` (`src/Reporter/Views/UnreadPage.xaml.cs:22-75`) statt im `UnreadViewModel` oder in `ArticleCardView` wie gefordert. `ArticleCardView` bindet an `OpenArticleCommand, Source={x:Reference Card}` (`src/Reporter/Views/ArticleCardView.xaml:174`), das BindableProperty existiert aber nicht an zentraler Stelle im ViewModel. | `plan.md:64`, `src/Reporter/Views/UnreadPage.xaml.cs:22`, `src/Reporter/Views/ArticleCardView.xaml:174` |
| 2 | `FeedIconUrl` als optionales Property im `ArticleDetailViewModel` bereitstellen und im Header anzeigen. | Nicht umgesetzt. Es wird nur ein farbiger `BoxView` (`src/Reporter/Views/ArticleDetailPage.xaml:43-47`) statt eines Feed-Icons verwendet; `FeedIconUrl` fehlt sowohl im ViewModel als auch in der XAML-Bindung. | `plan.md:37`, `src/Reporter/Views/ArticleDetailPage.xaml:43-47` |
| 3 | Status-Pille mit Gelesen-Dot und `Auto-Gelesen`-Toggle gemäß Design-Draft. | Teilweise umgesetzt. Es gibt ein "Gelesen"-Label und einen `Switch` mit "Auto"-Label (`src/Reporter/Views/ArticleDetailPage.xaml:16-33`), aber nicht den im Design-Draft vorgesehenen "Auto-Gelesen (5s)"-Pill-Text sowie keinen expliziten grünen Gelesen-Dot. | `plan.md:54`, `inventory/01-design-draft.md:12-14`, `src/Reporter/Views/ArticleDetailPage.xaml:16-33` |
| 4 | Konfigurierbare Gelesen-Verzögerung aus dem Einstellungen-Arbeitspaket nutzen (Priorität 1). | Nicht umgesetzt. Es wird ausschließlich der hartcodierte 5-Sekunden-Fallback verwendet (`src/Reporter.Core/ViewModels/ArticleDetailViewModel.cs:18, 205, 287`). Der Settings-Integrationspunkt ist noch offen. | `plan.md:75-76`, `src/Reporter.Core/ViewModels/ArticleDetailViewModel.cs:18, 205, 287` |
| 5 | Manuelle UI-Verifikation mit Screenshots dokumentieren. | Teilweise. Die Prüfung ist in `docs/help/anwendung/mobile-ui-design.md` beschrieben, jedoch ohne tatsächliche Screenshot-Dokumentation im Projekt. | `plan.md:85`, `docs/help/anwendung/mobile-ui-design.md:63-69` |

## Hinweise
- Route `articledetail` ist in `AppShell.xaml.cs:20` registriert.
- `ArticleDetailViewModel` und `ArticleDetailPage` sind vollständig angelegt und funktional.
- HTML-Wrapper, Dark-Mode-CSS, Schriftart Newsreader, Schriftgrößen-Toggle und Floating Bottom Action Bar sind umgesetzt.
- Touch-Targets der Bottom-Bar sind 44 × 44 pt (`src/Reporter/Views/ArticleDetailPage.xaml:119-120` u.ä.).
