<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Testergebnisse – Schritt 3

- Rest-Grep `.github/` + ci-instructions.md: 0 Treffer fuer
  net10/10.0.x/10.0.1xx/10.0.100/26.5.10318/net10.0_.
- package-ios/action.yml Diff: Workload-Pin 27.2.11039, SDK-Band
  11.0.1xx, global.json 11.0.100, Cleanup-Glob net11.0_*,
  Framework net11.0-ios; Kommentare an neue Feed-Lage angepasst.
- Workflows: 9x dotnet-version 11.0.x; TFM net11.0-windows/android/ios.
- Lokale Verifikation: YAML reine Textedits (keine Syntaxaenderung an
  Struktur); endgueltige Validierung = CI-Lauf im PR (G3/G4-Gates:
  Workload-Restore + iOS/Android/Windows-Build remote).
- setup-dotnet@v6 + 11.0.x: GA-SDK, Aufloesung ok (11.0.100 installiert).
- E2E Shared-Framework: kein AspNetCore-Framework-Bezug in E2ETests;
  MTP-Flags bereits Schritt 1 verifiziert (805/805 + TRX).
