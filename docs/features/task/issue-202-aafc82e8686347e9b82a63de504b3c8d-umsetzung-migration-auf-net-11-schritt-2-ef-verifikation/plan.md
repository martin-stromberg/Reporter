<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan – Schritt 2

1. Grep: EF-11-Breaking-Patterns im Quellcode (ausser obj/Migrations).
2. dotnet-ef 10.0.12 (tool-path) + Worktree `maintenance/net10`:
   v0.7.0-DBs erzeugen (Vollstand + Partiellstand).
3. Unter net11-Repo (dotnet-ef 11.0.0 Manifest): `database update`
   beider Kontexte auf beiden Staenden; History/Tabelle pruefen.
4. `dotnet test` (Migrations-/PRAGMA-Pfade via Service-Tests).
5. Dokumentation in test-results.md; kein Code-Diff erwartet.
Abweichung/Risiko: `__EFMigrationsLock`-Tabelle (EF-11-Infra) wird bei
Migrate angelegt – EF 10 ignoriert unbekannte Tabelle, kein
Rollback-Blocker; kein Schema-Migrationsfile.
