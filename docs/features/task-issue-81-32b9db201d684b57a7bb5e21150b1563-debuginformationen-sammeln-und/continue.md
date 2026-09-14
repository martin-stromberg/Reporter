<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Offene Aufgaben

Erstellt am: 2026-09-14
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

Alle verbleibenden Punkte sind Umgebungslimitationen (kein registrierter
Mail-Client auf dem Prüf-PC, kein macOS für iOS) — keine Code-Mängel.
Alle automatisierten Tests sind grün (461/461), Code-Review und
Usability-Review tragen „Keine Befunde".

## Offene Planelemente

- [ ] Manuelle Pflicht-Verifikation: Versand mit echtem registriertem Mail-Client — vorbefüllter Entwurf (Empfänger/Betreff/7 Sektionen inkl. Session-Log) auf einem Gerät mit `mailto:`-Client prüfen (auf dem Prüf-PC wurde der Windows-Dialog „kein E-Mail-Programm zugeordnet" als Unsupported-Pfad verifiziert)
- [ ] Manuelle Pflicht-Verifikation: iOS-Verifikation (`net10.0-ios`, erfordert macOS) — Session-Log, Schalter und Versand auf iOS-Gerät/Simulator prüfen

## Code-Review-Befunde

Keine.

## Usability-Befunde

Keine.

## Fehlgeschlagene Tests

- [ ] Manuell: Mail-Client öffnet vorbefüllten Entwurf mit Empfänger/Betreff/Body — nicht ausgeführt (kein registrierter Mail-Client auf dem Prüf-PC)
- [ ] Manuell: iOS-Verifikation — nicht ausgeführt (macOS erforderlich)

## Hinweise für die Nachbearbeitung

- `DebugReportRecipient = "debug@example.com"` in `src/Reporter.Core/Services/DebugReportService.cs` ist ein bewusster Platzhalter (Anwender-Entscheidung) — die reale Entwickler-Adresse muss vor dem Release eingetragen werden.
