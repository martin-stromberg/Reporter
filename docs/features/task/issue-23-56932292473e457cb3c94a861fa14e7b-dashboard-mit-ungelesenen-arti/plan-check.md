# Plan-Prüfung

## Status
Plan vollständig.

## Prüfung gegen Anforderung
| Anforderung | Plan-Referenz |
|---|---|
| Liste ungelesener Artikel, absteigend nach Datum | `IItemRepository.GetUnreadByDateAsync` + ViewModel `Articles` |
| Kategoriefilter (Alle + Kategorien) | `CategoryFilterItem` + `SelectedCategory` |
| Pull-to-Refresh + Sync | `RefreshCommand` mit `IFeedSyncService.SyncAllAsync` |
| Infinity-Scroll | Paging `LoadMoreCommand` |
| "Alle als gelesen markieren" | `MarkAllReadCommand` |
| Tippen öffnet Detail | `OpenArticleCommand` / ActionSheet |

## Testabdeckung
- Repository-Tests für Paging, Filter, MarkAll, ToggleSaved, Count.
- ViewModel-Tests für Laden, Filter, LoadMore, Sync, MarkAll.
- Build und Test via `dotnet build` und `dotnet test`.

## Hinweis
Aufgrund fehlender Unteragenten-Unterstützung wurden Plan und Prüfung vom Hauptagenten selbst erstellt.
