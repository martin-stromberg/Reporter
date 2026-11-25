<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Projektabschluss – .NET-11-Migration (Issue #202)

## Ergebnis

Alle vier Entwicklungsschritte umgesetzt, abgenommen und gemergt.
PR #208 gegen `staging` mit `Closes #202`; alle PR-Checks gruen
(build & test, os-interface tests, static checks).

## Verifikation

- Build net11.0-windows10.0.19041.0 + net11.0-android: gruen.
- `dotnet test`: 805/805 gruen (inkl. MTP --coverlet/--report-trx).
- `scripts/Run-StaticChecks.ps1`: Exit 0 (Format, Header, Audit,
  Static-Analysis-Build 0 Warnungen/Fehler).
- v0.7.0-Bestands-DBs (maintenance/net10, dotnet-ef 10.0.12) unter
  EF 11 fehlerfrei migriert; keine neue Schema-Migration.
- CI-Gates: alle gruen (PR #208).

## Offene Restpunkte (userseitig / umgebungsabhaengig)

1. **FlaUI-E2E (lokal):** 19/39 gruen, 20 Fehlschlaegde mit
   deterministischer Signatur „Element not found" fuer
   Overlay-/Dialog-Elemente (transparenter `Aktionen`-Button auf der
   Feed-Detailseite, `Ja`-Dialogbutton); `AppStarts_FeedListRenders`
   gruen. Muster deutet auf geaenderte UIA-Exposure unter MAUI 11
   (Windows-Testhost) hin — ob reine Automation-Sichtbarkeit oder
   echte UI-Regression, kann nur eine manuelle Sichtpruefung in einer
   interaktiven Session klaeren. Suite ist vollstaendig
   `OsInterface`-klassifiziert (best-effort, nicht Teil der
   mergeblockierenden Gates).
2. **iOS-TestFlight + Geraet:** `UseInterpreter`-Verhalten, App-Start,
   Migration und Feed-Refresh auf echtem Geraet — Task 5.4 des
   Migrationsplans, umgebungsabhaengig (CI liefert den iOS-Build).
3. **Release-Timing (Task 5.8):** Nicht direkt vor einem Release
   mergen; erste .NET-11-Auslieferung enthaelt keine neue
   EF-Schema-Migration (DB-Rollback bleibt moeglich).
