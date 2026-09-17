<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Entwicklung — Git-Hooks und lokale Prüfungen

## Übersicht

Das Repository enthält Git-Hooks im Ordner `.githooks` (aus dem [Pattern-Collection](https://github.com/martin-stromberg/Pattern-Collection/tree/main/Git-Hooks)-Repo). Die Hooks fangen typische Fehler lokal ab, bevor sie in der CI auffallen, und schützen die Integrationsbranches `main` und `staging` vor direkten Commits und Pushes.

## Aktivierung

```bash
git config --local core.hooksPath .githooks
```

Oder unter Windows `install-hooks.cmd` / unter macOS/Linux `install-hooks.sh` ausführen.

## Geprüfte Regeln

**`pre-commit`** (blockiert direkte Commits auf `main` und `staging`):

- Konsistenz der RESX-Lokalisierung (`translation-check.py`)
- XML-Dokumentation in `.cs`/`.csproj` (`csproj-xmldoc-check.py`)
- Razor-Lokalisierung und -Verwendung (`razor-l10n-check.py`; `razor-usage-check.py` nur als Warnung)
- Lizenz-Header der gestagten Dateien (`scripts/add-license-headers.mjs --staged`, sofern `node` verfügbar)
- Platzhalter-Implementierungen (`no-notimplemented-check.py`, nur Warnung)
- Enum-Testabdeckung (`enum-coverage-check.py`, nur Warnung)
- Sofern vorhanden, dynamisch gesucht: `SecretScan.csproj` und `MarkdownLinkCheck.csproj` werden per `dotnet run` ausgeführt

**`pre-push`** (blockiert direkte Pushes auf `main` und `staging`):

- `no-notimplemented-check.py --all --strict`
- `razor-usage-check.py --all --strict`
- `enum-coverage-check.py --all --strict`

## Statische Prüfungen

Die Projekte sind so konfiguriert, dass `GenerateDocumentationFile` aktiviert und `CS1591` als Fehler behandelt wird — neue öffentliche APIs benötigen daher XML-Dokumentation (`<summary>`, `<param>`, `<returns>`).

Vor Abschluss einer Änderung lassen sich die Prüfungen des CI-Jobs `static checks` lokal ausführen (Format, Security-Scan, Static-Analysis-Build):

```powershell
.\scripts\Run-StaticChecks.ps1
```

## Verwandte Bereiche

- [Tests](../tests/index.md) — Ausführung der Test-Suiten.
- [Release-Management](../release-management/index.md) — CI-Gates und Release-Pipeline.
