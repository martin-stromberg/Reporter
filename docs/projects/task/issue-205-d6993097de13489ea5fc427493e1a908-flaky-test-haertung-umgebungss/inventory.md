<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Flaky-Test-Härtung

Analysiert wurden `src/Reporter.Tests`, `src/Reporter.E2ETests`, die beiden Staging-Workflows, lokale Testskripte und die Referenzumsetzung aus `Softwareschmiede` PR #432. Die Schritte wurden wegen der Umgebungsrestriktion gegen unaufgeforderte Subagenten direkt statt über Unteragenten erstellt.

## Zusammenfassung

- Die reguläre CI-Lane führt ausschließlich `src/Reporter.Tests` aus; `Reporter.E2ETests` ist bewusst lokal/on-demand.
- In `Reporter.Tests` wurden keine bestehenden Testmethoden gefunden, deren Assertions von echten Prozessen, UIA, ConPTY, Clipboard oder echten Netzwerk-Endpunkten abhängen. Temp-SQLite- und Temp-Dateipfade allein sind keine OS-Lane-Kandidaten.
- `Reporter.E2ETests` ist durchgehend umgebungssensitiv (`Process.Start`, FlaUI/UIA3, echte Desktop-Fokuslage, Kestrel-Loopback, Temp-SQLite, Prozessbaum-Cleanup).
- `E2EProcessGuard` bietet bereits Kill-on-Close-Job, Tree-Kill, begrenztes Exit-Wait und Temp-Delete mit Retry/Backoff; diese Struktur ist Referenz für `Reporter.Tests`.
- Bestehende E2E-Tests verwenden `Category=E2E`; `E2EProcessGuardTests` besitzt keine Kategorie. `OsInterface`/`Flaky` existieren noch nicht.
- Baseline vor Änderungen: `Reporter.Tests` 798/798 erfolgreich, 0 fehlgeschlagen, 0 übersprungen ([Nachweis](inventory/test-results/baseline-reporter-tests.log)).

## Details

- [Audit der Umgebungssensitivität](inventory/audit.md)
- [Test-Ausgangszustand und MTP-Prüfung](inventory/tests.md)
- [CI-Workflows und Gate-Struktur](inventory/workflows.md)
- [Cleanup- und Deadline-Bestand](inventory/cleanup-deadlines.md)
- [Referenzimplementierung](inventory/reference.md)
