<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Projektplan: Flaky-Test-Härtung

## Übersicht

Das Projekt härtet die automatisierte Testausführung gegen Umgebungsflakiness, ohne die reguläre Merge-Gate-Lane zu schwächen. Es trennt OS-sensible Tests von der blockierenden Lane, etabliert Cleanup-, Deadline-, Retry- und Statistik-Konventionen und dokumentiert die lokale sowie CI-seitige Ausführung.

## Grobe Vorgehensentscheidungen

| Bereich | Entscheidung | Begründung |
|---------|--------------|------------|
| Klassifizierung | `OsInterface` als eigene Testkategorie; `Flaky` nur zusammen mit `OsInterface` | Ermöglicht präzise Filterung und verhindert Missbrauch von Retries. |
| `Reporter.Tests` | Keine bestehenden Tests pauschal auslagern; nur neue, bewusst OS-abhängige Infrastruktur-Tests markieren | Temp-SQLite/Temp-IO allein ist kein OS-Flake-Befund; die blockierende Abdeckung bleibt erhalten. |
| `Reporter.E2ETests` | Als `OsInterface` klassifizieren, aber keine CI-Lane hinzufügen | FlaUI benötigt interaktive Desktop-Session; der Aufbau einer solchen Runner-Infrastruktur ist außerhalb des Issuescopes. |
| CI | In beiden Staging-Workflows einen sichtbaren best-effort-OS-Job ergänzen | Ergebnisse bleiben sichtbar, können aber kein Merge-Gate blockieren. |
| Retry | Ein gefilterter Einmal-Retry nur für `Flaky`-Tests im OS-Job | Kein Blanket-Retry; die reguläre Lane bleibt streng. |
| Statistik | TRX-Auswertung mit dokumentiertem Quarantäne-Schwellwert | Verhindert Alarmmüdigkeit bei dauerhaft instabilen OS-Tests. |

## Entwicklungsschritte

### Schritt 1: Testklassifikation und lokale Härtung

**Beschreibung:** Führe das Audit-Ergebnis in eine umgesetzte Testklassifikation über: Umgebungssensitive Tests erhalten die Kategorie `OsInterface`; `Flaky` darf nur in Kombination mit `OsInterface` vorkommen und wird per Meta-Test abgesichert. Die lokalen E2E-/UI-Tests werden entsprechend markiert, ohne eine neue CI-Ausführung für sie einzuführen. Ergänze die nötige lokale Cleanup- und Deadline-Härtung: Prozessbäume werden vor Temp-Verzeichnisdelete beendet, Exit-Waits sind begrenzt, Verzeichnisdelete erfolgt mit Retry/Backoff, und Cleanup-Restfehler werden diagnostiziert statt den eigentlichen Assert zu überdecken. Dokumentiere das Audit mit Begründung, warum `Reporter.Tests` keine bestehenden Tests verliert und warum `Reporter.E2ETests` OS-sensitiv ist.

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** Testklassifikation, lokale Testinfrastruktur, E2E-Tests, Cleanup/Deadlines

### Schritt 2: CI-Lanes, Retry-/Statistik-Integration und Dokumentation

**Beschreibung:** Ergänze in beiden Staging-Workflows eine deterministische blockierende Lane ohne `OsInterface` sowie eine sichtbare, nicht blockierende OS-Lane mit `continue-on-error`, großzügigem Timeout, eigenen TRX-/Berichtsartefakten und gefiltertem Einmal-Retry für `Flaky`-Tests. Ergänze die Skripte für den gefilterten Retry und die TRX-Flake-Statistik inklusive Quarantäne-Schwellwert. Aktualisiere die Test-, CI- und Bedienungsdokumentation mit Klassifikationsregeln, lokalen Befehlen, Retry-/Quarantäne-Konvention, Deadline-/Cleanup-Verhalten und der Tatsache, dass `Reporter.E2ETests` weiterhin lokal/on-demand bleibt. Verifiziere die Filterbefehle und teste die Workflows so weit wie im PR-Kontext möglich; dokumentiere wiederholte CI-Nachweise und etwaige plattformbedingte Grenzen.

**Abhängigkeiten:** 1

**Betroffene Bereiche:** CI-Workflows, Testskripte, TRX-Statistik, Dokumentation, Verifikation

## Übersichtstabelle

| # | Titel | Abhängigkeiten | Betroffene Bereiche |
|---|-------|----------------|----------------------|
| 1 | Testklassifikation und lokale Härtung | Keine | Testklassifikation, lokale Testinfrastruktur, E2E-Tests, Cleanup/Deadlines |
| 2 | CI-Lanes, Retry-/Statistik-Integration und Dokumentation | 1 | CI-Workflows, Testskripte, TRX-Statistik, Dokumentation, Verifikation |

## Offene Punkte

Keine.
