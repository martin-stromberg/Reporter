<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Planprüfung – Projekt .NET-11-Migration (Durchlauf 1, archiviert)

## Ergebnis

**Status:** Projektplan lückenhaft

Die vier Entwicklungsschritte bilden die Phasen 1–5 des Migrationsplans strukturell sauber ab, die Abhängigkeiten sind korrekt und die Nicht-Ziele werden eingehalten. Jedoch ist **Task 5.1 (`scripts/Run-StaticChecks.ps1` → Exit 0) keinem Entwicklungsschritt und keinem Akzeptanzkriterium zugeordnet**, obwohl die Anforderung und `AGENTS.md` ihn explizit fordern. Weitere Tasks (2.4, 3.6, 4.3, 4.5, 5.2, 5.3) sind nur implizit abgedeckt. Zudem enthält der verbindliche Migrationsplan veraltete Zeilen-Enumerationen (Task 4.1, 4.3, 4.6) — die grep-basierten Akzeptanzkriterien fangen diese auf.

## Fehlende oder unvollständige Punkte

1. Task 5.1 — `scripts/Run-StaticChecks.ps1` Exit 0: keinem Schritt/AK zugeordnet.
2. Task 2.4 (Dependabot-Koordination): kein Schritt; faktisch entfallen (PRs #196/#200 CLOSED), im Plan zu dokumentieren.
3. Task 5.2 (FlaUI-E2E): nicht als Akzeptanzkriterium verankert.
4. Stellen-Referenz-Fehler im Migrationsplan: `staging-ci.yml:188`, `pr-staging-ci.yml:191` (zusätzliche `dotnet-version '10.0.x'`), `ci-instructions.md:1018`, ReportGenerator-Zeilen 129/132 → tatsächlich 130/133.
5. Implizite Bleibt-Tasks (3.6, 4.3, 4.5, 5.3) bei strenger Abbildung nur über Grep/PR-Check erfasst.

## Abhängigkeitsprüfung / Hinweise

Abhängigkeiten zyklenfrei und korrekt. Stichprobe der Stellen-Referenzen überwiegend exakt verifiziert (Reporter.csproj, package-ios, Scripts, Fixtures, csproj-Paketzeilen).

(Vollständiger Bericht des Prüfdurchlaufs 1 — Lücken wurden in Durchlauf 2 bearbeitet.)
