<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Anforderung: Flaky-Test-Härtung für Reporter

Quelle: [Issue #205](https://github.com/martin-stromberg/Reporter/issues/205), Vorlage [Softwareschmiede PR #432](https://github.com/martin-stromberg/Softwareschmiede/pull/432).

## Fachliche Zusammenfassung

Die Testinfrastruktur soll deterministische, blockierende CI-Tests von umgebungssensitiven OS-Schnittstellen-Tests trennen. Dazu sind Tests zu auditieren und zu markieren, die beiden Staging-Workflows um eine sichtbare, aber nicht blockierende OS-Lane zu erweitern, Prozess-/Temp-Cleanup und Polling-Deadlines zu härten, eine eng gefasste Retry-/Quarantäne-Konvention einzuführen und TRX-Flake-Statistik zu berichten.

## Fachlicher Scope

- **Verarbeitungseinheit:** Einzelner automatischer Test sowie CI-Testlauf-Lane (`deterministisch/blockierend` vs. `OsInterface/best-effort`).
- **Auslösende Ereignisse:** Push/PR gegen `staging`, lokale Testausführung, Fehlschlag eines als `Flaky` markierten OS-Tests, Auswertung von TRX-Berichten.
- **Erwartete Wirkung:** `Reporter.Tests` bleibt als blockierende Lane deterministisch; OS-sensitive Tests laufen sichtbar, aber nicht gate-relevant; Fehlschlag-dann-Retry-Verhalten und Flake-Statistik sind nachvollziehbar. `Reporter.E2ETests` ist einheitlich als OS/UI-sensitiv klassifiziert, bleibt aber mangels interaktiver Desktop-Session außerhalb der regulären CI.
- **Umgang mit fehlenden Objekten:** Fehlende Trait-Infrastruktur, Retry-/Analyse-Skripte und Dokumentationsartefakte werden erzeugt; vorhandene E2E-Cleanup-Infrastruktur wird wiederverwendet oder gespiegelt; fehlende CI-Artefaktnamen werden ergänzt.
- **Scope-Ausschlüsse:**
  - Kein neuer Self-hosted-/Interactive-Desktop-Runner und keine reguläre CI-Ausführung von `Reporter.E2ETests`.
  - Kein Blanket-Retry der Gesamtsuite und keine Wiederholung in der blockierenden Lane.
  - Keine automatische Auslagerung aller Dateisystem-/Temp-Tests; Fakten/Abstraktion hat Vorrang vor Markierung.

## Betroffene Klassen und Komponenten

- `src/Reporter.Tests` — Trait-/Konventionsinfrastruktur, Meta-Tests, Temp-Directory-Cleanup, Audit-Ergebnis.
- `src/Reporter.E2ETests` — OS-Klassifizierung der FlaUI-/Prozess-/Temp-DB-Tests, Deadline- und Cleanup-Prüfung.
- `.github/workflows/staging-ci.yml` und `.github/workflows/pr-staging-ci.yml` — Filterung und best-effort-OS-Job.
- `scripts/` — gefilterter Einmal-Retry und TRX-Flake-Auswertung.
- `docs/help/tests/`, `README.md`, `changes.log` — Konventionen, lokale Befehle, Lane-Verhalten, Cleanup-/Deadline-Regeln.

## Implementierungsansatz

- Einführung eines `Category=OsInterface`-Traits sowie einer `Flaky`-Konvention, die nur zusammen mit `OsInterface` zulässig ist und per Meta-Test abgesichert wird.
- MTP-kompatible Filterung mit `--filter-not-trait "Category=OsInterface"` in der blockierenden Lane und `--filter-trait "Category=OsInterface"` in der best-effort-Lane.
- Separater `continue-on-error`-Job in beiden Workflows, ohne Aufnahme in Gate-`needs`-Ketten und mit eigenen Artefaktnamen.
- Gefilterter Einmal-Retry ausschließlich für `Category=Flaky` in der OS-Lane; TRX-Auswertung berichtet Fehlschlag → Retry-grün-Rate und dokumentiert den Quarantäne-Schwellwert.
- Härtung/Wiederverwendung von Prozess-Kill, begrenztem Exit-Wait und Temp-Delete mit Retry/Backoff ohne Maskierung des eigentlichen Testfehlers.

## Konfiguration

- Keine produktive Konfiguration erforderlich. Testklassifikation erfolgt per xUnit-Traits; Retry-/Statistikverhalten erfolgt über CI-Schritte und Skriptparameter.
- Quarantäne-Schwellwert: ein Quarantäne-Issue soll erstellt werden, wenn ein Test innerhalb der letzten 20 ausgewerteten Läufe mindestens 2 Flake-Ereignisse hat **oder** bei mindestens 5 Ausführungen eine Fehlschlag-dann-Retry-grün-Rate über 20 % erreicht.

## Geklärte Issue-Fragen

| Frage | Entscheidung |
|-------|--------------|
| E2E-CI-Lane? | `Reporter.E2ETests` erhält keine neue CI-Lane, weil FlaUI eine interaktive Windows-Desktop-Session erfordert. Die Suite wird stattdessen als `OsInterface` klassifiziert und bleibt lokal/on-demand. |
| Filter-Syntax | Microsoft.Testing.Platform nutzt `--filter-trait` / `--filter-not-trait`; klassisches VSTest-`--filter Category!=...` wird nicht verwendet. Leere Treffermengen sind als Fehlschlag zu vermeiden bzw. explizit zu behandeln. |
| Retry-Mechanik | Ein zweiter, gefilterter `dotnet test`-Aufruf im OS-Job führt nur `Category=Flaky`-Tests einmal erneut aus. |
| Schwellwert | 2 Flake-Ereignisse oder >20 % bei mindestens 5 Ausführungen innerhalb der letzten 20 Läufe. |

## Offene Fragen

Keine.
