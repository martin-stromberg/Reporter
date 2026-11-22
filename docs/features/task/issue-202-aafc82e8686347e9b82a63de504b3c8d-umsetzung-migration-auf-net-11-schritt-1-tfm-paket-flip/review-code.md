<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review – Schritt 1

## Ergebnis

**Status:** Keine Befunde

Geprüft (unabhängiger schreibgeschützter Review-Agent):

- **TFM-Konsistenz:** alle Strings exakt (`net11.0`,
  `net11.0-windows10.0.19041.0`, `net11.0-ios`, `net11.0-android`),
  keine Tippfehler/Fehlstellen.
- **Versionswahl:** EF/Sqlite 11.0.0, `Microsoft.Data.Sqlite` 11.0.0,
  `Logging.Debug` 11.0.0, `Syndication` 11.0.0,
  `TimeProvider.Testing` 11.0.1, `dotnet-ef` 11.0.0,
  `global.json` `11.0.100`+`latestPatch` — alle requirement-konform.
- **G5:** android 21.0→24.0 (XA4216-belegt), maccatalyst 15.0→17.0
  (Pack-Befund), iOS 15.0/Windows 17763 bewusst unverändert,
  `Package.appxmanifest` konsistent.
- **Scope:** `IncludeIosTarget`/`IncludeAndroidTarget`-Mechanik intakt;
  keine App-Code-, Migrations- oder Manifest-Änderungen; verbleibende
  `net10.0`-Strings nur EF-Migrations-Annotationen (historisch).
- **`dotnet-tools.json`:** Schema korrekt; Root-Ablage = neue
  .NET-Konvention, Resolver findet beide Orte.

## Hinweise

- CI-Sequenzierung: `global.json`-Pin auf 11.0.100 bedeutet, dass die
  noch auf `10.0.x` stehenden CI-Jobs erst in Schritt 3 lauffähig
  werden — Merge nach `staging` erst nach Schritt 3 (Projektplan).
- Verbleibende `net10.0`-Doku-Treffer (`README.md`, `docs/help/**`,
  `scripts/iOS-Deployment.md`, `ci-instructions.md`, `changes.log`)
  sind Projektschritt-4-Umfang.
