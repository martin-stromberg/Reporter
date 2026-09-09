← [Zurück zur Übersicht](index.md)

# Anwendung — Datenmodell

## Entitäten

### `Article`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `Guid` | Eindeutige Kennung des Artikels. |
| `Title` | `string` | Titel des Artikels. |
| `IsRead` | `bool` | Gibt an, ob der Artikel bereits gelesen wurde. |

## Beziehungen

Aktuell sind noch keine Beziehungen zu anderen Entitäten definiert. In späteren Versionen werden `Feed`- und `Source`-Entitäten hinzukommen.
