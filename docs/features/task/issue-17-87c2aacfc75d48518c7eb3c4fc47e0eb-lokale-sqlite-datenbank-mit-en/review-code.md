# Code-Review

**Status:** Befunde behoben

Geprüft:
- Migrationen wurden ausschließlich über `dotnet ef migrations add` erzeugt.
- Kein `EnsureCreated` im Produktiv-Startup; `MigrateAsync` wird in `App.OnStart` verwendet.
- Alle öffentlichen Typen und Member sind mit XML-Dokumentation versehen.
- Build und Tests sind erfolgreich.

## Befunde

1. **Teststruktur nicht fokussiert** (behoben)
   - `ReporterDbContextTests` verband Schema-Erstellung, Persistenz und Include-Abfrage in einer einzigen Methode.
   - Lösung: `ReporterDbContextTests` ersetzt durch `ReporterDbContextTests_Schema` und `ReporterDbContextTests_Persistence`; jede Methode testet ein Verhalten.

2. **`Item.GuidOrHash` war nullable, Unique-Index damit wirkungslos** (behoben)
   - Der per-feed-Unique-Index auf `feed_id, guid_or_hash` würde bei `NULL`-Werten in SQLite Duplikate zulassen.
   - Zudem entspricht `guid_or_hash` (im Gegensatz zu `content_html`) nicht dem explizit als optional markierten Feld im Anforderungskatalog.
   - Lösung: `GuidOrHash` in `Item` auf `string` (nicht nullable) geändert, `IsRequired()` in `ReporterDbContext`, `datenmodell.md` aktualisiert und `InitialCreate`-Migration neu generiert.

## Offene Punkte

- `App.OnStart` verwendet `async void` für `MigrateAsync`. Für den MAUI-App-Start akzeptabel; bei späteren Anforderungen sollte ein explizites Fehler-Handling ergänzt werden.
- `ArticleRepository` ist weiterhin ein Stub; die Persistenzschicht für Artikel ist nicht Teil dieser Anforderung.
