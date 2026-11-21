<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Entwicklung

Hinweise für Beitragende und Maintainer des Repositories: lokale Qualitätssicherung über Git-Hooks und die statischen Prüfungen, die auch der CI-Job `static checks` ausführt.

## Inhalt

- [Git-Hooks und lokale Prüfungen](git-hooks.md) — `.githooks` aktivieren, geprüfte Regeln, lokaler Lauf der CI-Static-Checks
- [.NET-11-Migration](dotnet-11-migration.md) — Entscheidung (geplante Migration auf .NET 11 STS) und Migrationsplan für .NET 11 / MAUI 11 / EF Core 11

## Verwandte Bereiche

- [Tests](../tests/index.md) — Unit-/Integrationstests, E2E-Smoke-Suite und die `REPORTER_*`-Umgebungsvariablen.
- [Release-Management](../release-management/index.md) — CI/CD-Pipeline mit den Gates `static checks` und `build & test`.
