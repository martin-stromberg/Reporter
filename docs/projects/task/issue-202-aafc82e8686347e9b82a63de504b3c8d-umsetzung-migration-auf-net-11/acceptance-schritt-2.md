<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Abnahmepruefung - Entwicklungsschritt 2 (EF-Core-11-Verifikation)

## Ergebnis

**Status:** Anforderung vollstaendig erfuellt

## Abweichungen

Keine.

## Hinweise

- Breaking-Change-Grep: kein OwnsOne/OwnsMany/AsSplitQuery/Compiled-
  Model/AutoTransactionBehavior-Treffer im Produktivcode.
- v0.7.0-DBs (maintenance/net10, dotnet-ef 10.0.12): Vollstand unter
  EF 11 "already up to date", Partiellstand migrierte EF 11 die
  verbleibenden Migrationen fehlerfrei (AddItemTags,
  AddItemContentImageColumns). Tabellen/History verifiziert.
- `__EFMigrationsLock` wird von EF 11 angelegt - EF-10-Rollback
  unbedenklich (unbekannte Tabelle).
- MigrateAsync-Reihenfolge + PRAGMA-Pfad via Service-Tests gruen
  (805/805 unter net11).
- Keine neue Schema-Migration; historische ProductVersion-Annotationen
  unveraendert.
- UseInterpreter: Code-Pruefung ok; Geraeteverifikation U2-Restpunkt.
