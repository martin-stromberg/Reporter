<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Anforderung: Testklassifikation und lokale Härtung

## Originalanforderung

GitHub Issue #205 verlangt eine Härtung der Tests gegen Flakiness nach dem Ansatz aus Softwareschmiede#428/#432. Entwicklungsschritt 1 deckt die lokale Testklassifikation und die nicht-CI-seitige Härtung ab.

## Übersetzte Anforderung

Reporter erhält eine explizite Testklassifikation, mit der deterministische Tests von umgebungssensitiven Tests getrennt werden können. Umgebungssensitive Tests erhalten die xUnit-Kategorie `OsInterface`; eine spätere `Flaky`-Kennzeichnung darf nur innerhalb dieser Kategorie vergeben werden. Die FlaUI-/Prozess-/Temp-Verzeichnis-Tests in `Reporter.E2ETests` werden entsprechend markiert. `Reporter.Tests` erhält eine robuste Temp-Verzeichnis-Cleanup-Hilfe mit Wiederholung und nicht werfender Endbehandlung sowie Tests und Konventionstests für die neue Klassifikation.

## Fachlicher Scope

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Testmethoden, Testklassen und lokale Test-Hilfsmittel in `src/Reporter.Tests` und `src/Reporter.E2ETests` |
| Auslösende Ereignisse | Lokaler `dotnet test`-Lauf und spätere CI-Filterung |
| Erwartete Wirkung | `Category=OsInterface` trennt umgebungssensitive Tests; `Category=Flaky` ist nur mit `OsInterface` zulässig; Cleanup ist best effort und deadlinegebunden |
| Umgang mit fehlenden Objekten | Fehlende Temp-Verzeichnisse werden als bereits aufgeräumt behandelt; fehlende Prozesszuordnung bleibt best effort ohne Wurf |

## Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine Änderung an GitHub-Workflows | CI-Lanes, Retry-Auswertung und Statistik sind Schritt 2 | bestätigt |
| 2 | Kein globales Markieren aller Datei-/Temp-Zugriffe | Der Audit hat gezeigt, dass OS-Sensitivität an der Assertion bzw. am Ressourcenbesitz hängt, nicht am bloßen Path-Gebrauch | bestätigt |
| 3 | Keine Verlagerung der E2E-Suite in den normalen CI-Lauf | Die Suite ist bewusst lokal/on-demand und benötigt eine interaktive Windows-Desktop-Sitzung | bestätigt |

## Akzeptanzkriterien

1. Es gibt eine dokumentierte und maschinell filterbare `OsInterface`-Kategorie.
2. Alle umgebungssensitiven Tests in `Reporter.E2ETests` tragen `Category=OsInterface` zusätzlich zu ihrer bisherigen Kennzeichnung.
3. Eine `Flaky`-Kategorie existiert als explizite Ausnahme für umgebungssensitive Tests; Flaky-Markierungen ohne `OsInterface` werden durch einen Konventionstest verhindert.
4. `Reporter.Tests` besitzt eine deadlinegebundene, mehrfach versuchende Temp-Verzeichnis-Cleanup-Hilfe, die Fehler meldet und nicht wirft.
5. Die neue Cleanup-Hilfe ist durch Tests für Erfolg, fehlendes Verzeichnis, kurze Dateisperre und dauerhafte Sperre abgesichert.
6. Die lokale Filterung `Category=OsInterface` ist mit MTP/xUnit v3 nachweisbar.

## Kontext

- `Reporter.Tests` hatte zuletzt 798 bestandene Tests ohne Fehler oder Skips.
- `Reporter.E2ETests` enthält bereits `E2EProcessGuard` mit Windows-Job-Objekt, PID-basiertem Tree-Kill, Exit-Bestätigung und best-effort Temp-Cleanup.
- Die eigentlichen Desktop-Tests tragen bereits `Category=E2E`.
- `E2EProcessGuardTests` greifen auf reale Prozess-/Dateisystemressourcen zu und sind daher ebenfalls OS-sensitiv.
