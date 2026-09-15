<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Contributing

Danke für dein Interesse an Reporter! Beiträge sind willkommen — bitte beachte die folgenden Punkte.

## Lizenz der Beiträge

Reporter steht unter der [PolyForm Noncommercial License 1.0.0](LICENSE) (siehe die LICENSE-Datei im Projekt-Root). Mit dem Einreichen eines Beitrags (Pull Request, Patch, Code, Dokumentation o. ä.) bestätigst du, dass:

- du deinen Beitrag unter der **PolyForm Noncommercial License 1.0.0** lizenzierst,
- dein Beitrag damit ebenfalls nur **nicht-kommerziell** genutzt werden darf,
- du berechtigt bist, den Beitrag unter diesen Bedingungen einzureichen (eigenes Werk bzw. Einverständnis des Rechteinhabers).

Das Pull-Request-Template enthält eine entsprechende Bestätigungs-Checkbox; ohne diese Bestätigung können Pull Requests nicht gemergt werden.

Für eine kommerzielle Nutzung der Software (unabhängig von Beiträgen) ist eine separate, individuell vereinbarte kommerzielle Lizenz erforderlich — siehe [COMMERCIAL-LICENSE.md](COMMERCIAL-LICENSE.md).

## Entwicklung

- Build: `dotnet build Reporter.sln`
- Tests: `dotnet test Reporter.sln --filter "Category!=E2E"` und `npm test`
- E2E-Smoke-Tests (nur Windows, benötigt eine interaktive Desktop-Session): `.\scripts\Run-E2ETests.ps1`
- Lokale statische Prüfungen vor Abschluss: `.\scripts\Run-StaticChecks.ps1`
- Neue öffentliche APIs benötigen XML-Dokumentation (`CS1591` ist als Fehler konfiguriert).
- Neue Dateien erhalten den kurzen Lizenzheader (siehe vorhandene Dateien bzw. `scripts/add-license-headers.mjs`).
- Mobile-UI-Regeln und weitere Projektkonventionen siehe [AGENTS.md](AGENTS.md).
