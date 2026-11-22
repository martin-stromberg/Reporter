<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# OS-sensible Bereiche

## Klassifikationsregel

Ein Test ist `OsInterface`, wenn seine Assertion oder sein stabiler Teardown von echten Betriebssystemressourcen abhängt. Beispiele: Prozesslebenszyklus, Windows-Job-Objekte, Fenster/UIA, Dateisperren, echtes Temp-Verzeichnisverhalten oder externe Netzwerk-/Desktop-Zustände.

Reine Datei-, Pfad-, Datenbank-, Zeit- oder Kulturverwendungen sind nicht automatisch OS-sensitiv. Entscheidend ist, ob das Betriebssystem das zu prüfende Verhalten oder den erfolgreichen Cleanup beeinflusst.

## Reporter.Tests

Der Projekt-Audit fand keine bestehende Testmethode, deren Assertion direkt an echten OS-Ressourcen hängt. In diesem Schritt wird nur die neue `TempDirectoryCleanup`-Infrastruktur selbst mit OS-sensitiven Tests abgesichert.

## Reporter.E2ETests

Alle acht Testklassen werden als `OsInterface` klassifiziert:

- UI-Tests: `SmokeTests`, `FeedDetailTests`, `UnreadPacketTests`, `ArticleTagsTests`, `ArticleLinkTests`, `ArticleImageTests`, `DemoSeedTests`
- Infrastrukturtests: `E2EProcessGuardTests`

`Category=E2E` bleibt unverändert als fachliche Suite-Kennzeichnung; `Category=OsInterface` ist eine zusätzliche Ausführungs-/Stabilitätsdimension.
