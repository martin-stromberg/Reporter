<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Audit: Umgebungssensitive Tests

Regel: Ein Test wird nur dann als `OsInterface` markiert, wenn seine Assertions von echten OS-Ressourcen oder nicht kontrollierbarer Umgebung abhängen. Nutzt er nur ein lokales Temp-Verzeichnis oder eine lokale SQLite-Datei und räumt deterministisch auf, bleibt er in der blockierenden Lane.

| Bereich / Klasse | Befund | Deterministisch fakbar? | Entscheidung |
|------------------|--------|-------------------------|--------------|
| `Reporter.Tests` allgemein | Keine `Process.Start`, ConPTY/`PseudoConsole`, FlaUI/UIA, Clipboard, `HttpListener`, `TcpListener`, `Socket`-Treffer. | Nicht nötig | Keine bestehende Testmethode wird verschoben. |
| `FeedSyncServiceTests*` und Netzwerk-nahe Tests | HTTP geht über `FakeHttpMessageHandler` bzw. Stub-Handler; Zeit über `TimeProvider.Testing`. | Bereits gefakt | Blockierende Lane behalten. |
| Repository-/EF-/SQLite-Tests | Echte lokale Temp-SQLite-Dateien, aber kein externes System und keine Race-Bedingung gegen einen Kindprozess. | Nicht nötig | Blockierende Lane behalten; kein pauschales IO-Auslagern. |
| `ServiceCollectionTests` | Nur `Path.GetTempPath()` als konfigurierter Datenbankpfad, kein externer Dienst. | Nicht nötig | Blockierende Lane behalten. |
| `Reporter.E2ETests` UI-Testklassen (`SmokeTests`, `FeedDetailTests`, `Article*`, `UnreadPacketTests`, `DemoSeedTests`) | Starten echte `Reporter.exe`, verwenden FlaUI/UIA3, echte Desktop-Fokus-/Fensterzustände, Kestrel-Loopback und Temp-SQLite. | Nicht vollständig — echte UI ist Gegenstand der Tests | Als `OsInterface` markieren (zusätzlich zu `E2E`). |
| `E2EProcessGuardTests` | Prüft echte Prozessbeendigung/Temp-Cleanup. | Nicht sinnvoll — OS-Verhalten ist Assertion | Als `OsInterface` markieren. |
| `ReporterAppFixture`, `DemoSeedTests` | Starten Kindprozesse, warten auf Fenster/DB-Zustände, besitzen finally-/Dispose-Cleanup. | Nicht vollständig | Als OS-sensitiv dokumentiert; bestehende Cleanup-Infrastruktur erweitern/prüfen. |
| `UiRetry`, `FeedDbAssertions` | Polling auf UIA/SQLite mit 20 s bzw. ca. 15 s Budgets. | Nein | Deadline-Beziehungen dokumentieren und bei neuen OS-Tests einhalten. |

## Ergebnis

- `Reporter.Tests`: Keine bestehende Testmethode wird pauschal in die OS-Lane verschoben. Neue Infrastruktur-Tests, die bewusst OS-Verhalten prüfen (z. B. Temp-Delete unter Datei-Lock), werden mit `OsInterface` markiert.
- `Reporter.E2ETests`: Alle Testklassen, die echte App-/UI-/Prozesszustände prüfen, erhalten `Category=OsInterface`; `E2EProcessGuardTests` wird ebenfalls markiert. Die Suite bleibt außerhalb der regulären CI, weil sie eine interaktive Desktop-Session benötigt.
