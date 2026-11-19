<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Anforderung – .NET 11 / MAUI 11 / EF Core 11 Migration planen

Quelle: Issue martin-stromberg/Reporter#180 (Folge-Issue aus dem Architektur- und
Sicherheitsreview 2026-11, #178) + Issue-Kommentar des Auftraggebers.

## Ausgangslage

- Projekt steht auf .NET 10 (LTS, Support bis 14.11.2028).
- Seit 10.11.2026 ist .NET 11 verfügbar (STS, Support bis 14.11.2028 — gleiches Supportende).
- Umstieg ist **nicht zeitkritisch**, aber die 11er-Paketwellen lassen sich nicht isoliert
  einspielen:
  - `Microsoft.Maui.Controls` 10.0.20 → 11.0.10 (erfordert net11.0-TFMs + neue MAUI-Workloads, inkl. CI)
  - `Microsoft.EntityFrameworkCore.*` / `Microsoft.Data.Sqlite` 10.0.12 → 11.0.0
    (EF Core 11 **erfordert zwingend .NET 11**)
  - `Microsoft.Extensions.Logging.Debug`, `System.ServiceModel.Syndication`,
    `Microsoft.Extensions.TimeProvider.Testing` 10.x → 11.x (nur konsistent mit der
    Runtime-Migration anheben)

## Vom Auftraggeber angefordert (Issue-Kommentar)

1. **Maintenance-Branch für die .NET-10-Version** erstellen — erledigt:
   `maintenance/net10` auf `main`-Head (`5492d71`, Release-Stand v0.7.0).
2. **Diese Aufgabe erledigen** = die Migration *planen* und die Entscheidung
   **im Issue festhalten** (nicht: die Migration selbst durchführen).

## Zu liefern

- Eine begründete **Entscheidung**: geplant auf .NET 11 (STS) migrieren **oder** bewusst auf
  .NET 10 LTS bleiben und direkt .NET 12 (LTS, Nov 2027) ansteuern.
- Ein **Migrationsplan** als dauerhaftes Dokument im Repo (`docs/help/entwicklung/`),
  der mindestens abdeckt:
  - TFM-Umstellung aller Projekte (net10.0* → net11.0*, inkl. plattformbedingter TFM-Listen
    in `src/Reporter/Reporter.csproj`)
  - Paketwelle (MAUI, EF Core/Sqlite, Microsoft.Extensions.*)
  - MAUI-Workloads 11er-Generation (ios 26.x, maccatalyst, maui-windows, android) inkl. CI
  - CPU-Mindestbasis .NET 11 (x64: x86-64-v2 / SSE4.2+POPCNT; Arm64-Windows: LSE) —
    Build-Agents und Zielgeräte prüfen
  - `UseInterpreter`-Hinweis für EF Core auf iOS erneut verifizieren
  - CI-Matrizen und Release-Pipeline (setup-dotnet, Workload-Installation)
  - EF Core 11 Breaking Changes / Migrations-Verhalten für Bestandsinstallationen
  - Rolle des Maintenance-Branch `maintenance/net10` während der Umstellung
- **Entscheidung als Kommentar im Issue #180** festhalten.
- Abschluss als **PR gegen `staging`** mit Closing-Tag auf #180.

## Scope

- Enthalten: Entscheidungsdokument + Migrationsplan + Maintenance-Branch + Issue-Kommentar.
- Nicht enthalten: die eigentliche Durchführung der Migration (eigenes Folge-Issue/PR),
  keine Code- oder TFM-Änderungen in diesem PR.
