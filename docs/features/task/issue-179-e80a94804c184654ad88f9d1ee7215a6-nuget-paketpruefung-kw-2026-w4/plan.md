<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: NuGet-Paketprüfung KW 2026-W47 (Issue #179)

## Übersicht

Wartungs-/Audit-Aufgabe aus dem wiederkehrenden Workflow `.github/workflows/review-nuget.yml`: Bewertung aller 13 direkt referenzierten NuGet-Pakete der `Reporter.sln` mit Risikoeinstufung. Ergebnis der Bestandsaufnahme: keine Schwachstellen, keine Deprecated-Pakete, und **keine Patch-/Minor-Updates innerhalb der 10er-Linie** — sämtliche verfügbaren Updates sind Major-Sprünge auf die .NET-11-Welle. Konsequenz: Es gibt **keine direkt umsetzbaren Paket-Updates** (keine niedrig-/mittelrisikoreichen Empfehlungen). Der Auftrag reduziert sich auf: Ergebnis dokumentieren (Kommentar auf Issue #179 + PR-Body-Tabelle), ein Folge-Issue für die .NET-11-/Paketwellen-Migration anlegen und Issue #179 per `Closes #179` im Commit abschließen.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Direkte Paket-Updates | **Keine umsetzen.** Alle verfügbaren Updates sind Major-Sprünge auf 11.x (EF Core 10.0.12→11.0.0, `Microsoft.Maui.Controls` 10.0.20→11.0.10 via `$(MauiVersion)`, `Microsoft.Extensions.Logging.Debug` 10.0.0→11.0.0, `System.ServiceModel.Syndication` 10.0.11→11.0.0, `Microsoft.Extensions.TimeProvider.Testing` 10.1.0→11.0.0, `Microsoft.Data.Sqlite` 10.0.12→11.0.0). | .NET 11 ist sehr frisch (Release 11/2026) und die 11er-Paketwelle setzt faktisch eine TFM-/SDK-/Workload-Migration voraus (Projekte zielen auf `net10.0`; CI pinnt `dotnet-version: '10.0.x'`; `maui-windows`-Workload 10.0.20 liefert `$(MauiVersion)`). Einzelnes Hochsetzen würde Versionsmischungen 10.x/11.x erzeugen — Risiko hoch, Nutzen null. .NET 10 ist LTS → kein Handlungsdruck. |
| Transitive Pakete (`Microsoft.Testing.*` 2.5.2→2.5.3, `Microsoft.Web.WebView2`, `Microsoft.WindowsAppSDK`, `SQLitePCLRaw.*` u. a.) | **Nicht pinnen, nur dokumentieren.** | Entspricht der Annahme in `requirement.md`: aktives Pinnen transitiver Pakete nur bei tatsächlichen Schwachstellen — keine vorhanden. Das Test-Stack-Patch-Delta (2.5.2 vs. direktem 2.5.3) ist funktional unerheblich und löst sich mit dem nächsten `xunit.v3`-/`Microsoft.NET.Test.Sdk`-Update. |
| Ablage des Prüfergebnisses | **Issue-Kommentar auf #179 + Tabelle im PR-Body.** Kein dauerhaftes Audit-Dokument im Repo. | Der `review-nuget.yml`-Workflow erwartet Ergebnisse als Kommentar im Review-Issue und Folge-Issues für komplexe Befunde — kein Repo-Artefakt. Da der Workflow wöchentlich neue Review-Issues erzeugt, würde ein Audit-Dokument pro Lauf das Repo mit Kurzlebens-Dokumenten fluten. |
| Komplexer Befund „.NET-11-Welle" | **Ein Folge-Issue** „Migration auf .NET 11 und NuGet-Paketwelle 11.x" via `gh issue create`, verlinkt im Kommentar auf #179. | Der Workflow-Issue-Text verlangt für risikoreiche Updates/Breaking Changes eigene Folge-Issues. Die Migration ist kohärent (TFMs, SDK, Workloads, Pakete gehören zusammen) → ein Issue statt sechs Einzel-Issues. |
| Abschluss von Issue #179 | **`Closes #179` in der finalen Commit-Message** (PR gegen `staging`). | Die Prüfung selbst ist mit diesem Auftrag abgeschlossen; die .NET-11-Migration ist ein eigenständiges Folge-Issue. Der Workflow-Text („schließe dieses Issue nach dem Review, sodass nur die Folge-Issues offen bleiben") ist damit erfüllt. Faktische Schließung erfolgt erst bei Promotion `staging` → `main` — konsistent mit dem Staging-Workflow („ausgeliefert" = auf `main`). Kein manuelles Schließen per `gh` nötig. |
| Central Package Management (`Directory.Packages.props`) | **Nicht einführen; nur als Beobachtung im Folge-Issue/Report erwähnen.** | `requirement.md` grenzt das explizit als eigenständige Architekturentscheidung ab — allenfalls dokumentierbar. |

## Programmabläufe

### Verifikations-Prüfdurchlauf (Re-Run zum Umsetzungszeitpunkt)

Die Bestandsaufnahme ist vom 2026-11-17; vor der Dokumentation wird der Ist-Stand erneut verifiziert, da sich Paketstände und Dependabot-PRs seitdem geändert haben können.

1. `dotnet list Reporter.sln package --outdated --include-transitive` ausführen und mit `inventory/outdated.md` abgleichen.
2. `dotnet list Reporter.sln package --vulnerable --include-transitive` und `--deprecated --include-transitive` ausführen und mit `inventory/vulnerabilities.md` abgleichen.
3. `gh pr list --repo martin-stromberg/Reporter --author app/dependabot --state open` prüfen (zwischenzeitlich geöffnete Dependabot-PRs, insbesondere für die 11.x-Welle).
4. Für die 11.x-Zielpakete die TFM-Anforderungen aus den NuGet-Metadaten prüfen (z. B. `dotnet package search Microsoft.EntityFrameworkCore --exact-match --format json` bzw. nuget.org) und als Risikobegründung dokumentieren — Erwartung: `net11.0` erforderlich.
5. **Bedingter Pfad:** Meldet der neue Outdated-Scan ein Patch-/Minor-Update **innerhalb der 10er-Linie** für ein direktes Paket (in der Bestandsaufnahme nicht der Fall), wird dieses als niedrigrisikoreiche Empfehlung direkt in der betreffenden `.csproj` hochgesetzt und anschließend mit Schritt „Verifikation" abgesichert.
6. **Bedingter Pfad:** Zeigt der neue Vulnerability-/Deprecated-Scan Befunde, werden betroffene transitive Pakete gezielt direkt hochgepinnt bzw. das Paket aktualisiert — Abweichung vom Plan ist dann in der Ergebnistabelle zu begründen.

Beteiligte Komponenten: `Reporter.sln`, `dotnet`-CLI, `gh`-CLI, `inventory/outdated.md`, `inventory/vulnerabilities.md`.

### Ergebnistabelle und Risikobewertung

1. Für jedes der 13 direkten Pakete eine Zeile: Paket, Projekt(e), Ist-Version, neueste stabile Version, Befund (aktuell / veraltet / Schwachstelle / deprecated), Empfehlung, Risiko.
2. Transitive Ausreißer (`Microsoft.Testing.*` 2.5.2→2.5.3, `Microsoft.Web.WebView2`, `Microsoft.WindowsAppSDK`, `SQLitePCLRaw.*`, `Humanizer.Core`, `Microsoft.ApplicationInsights`) als kommentierter Abschnitt „transitiv — keine Aktion, siehe Folge-Issue/Dokumentation".
3. Bewertungsheuristik aus `requirement.md`: Patch/Minor in derselben Major-Linie = niedrig; Major-Sprung = hoch (hier: gesamte 11er-Welle).

Beteiligte Komponenten: Ergebnistabelle als Markdown (wird in Issue-Kommentar und PR-Body wiederverwendet).

### Folge-Issue „.NET-11-Migration" anlegen

1. `gh issue create` im Repo `martin-stromberg/Reporter` mit Titel „Migration auf .NET 11 und NuGet-Paketwelle 11.x".
2. Inhalt des Issue-Bodys:
   - Betroffene Pakete mit Zielversionen (alle 11.x-Sprünge aus `inventory/outdated.md`).
   - Notwendige Infrastrukturänderungen: TFMs `net10.0*` → `net11.0*` in allen fünf `.csproj`, .NET SDK 11.x lokal und in CI (`dotnet-version: '11.0.x'` in `pr-staging-ci.yml`/`staging-ci.yml`), `dotnet workload restore` mit `maui-windows` 11.x (impliziert `$(MauiVersion)` 11.x), ggf. Anpassungen in `scripts/iOS-Deployment.ps1`/E2E-Umgebung.
   - Timing-Empfehlung: Umsetzung nach Reifung der 11er-Welle (erste Servicing-Releases, stabile MAUI-11-Workloads für alle Zielplattformen); .NET 10 ist LTS mit Support bis 11/2028 — kein Zeitdruck.
   - Hinweis: Dependabot-PRs für 11.x-Major-Updates bis dahin auf dieses Issue verweisen/schließen; wöchentliche Review-Issues sollen auf dieses Folge-Issue verweisen statt die 11er-Welle erneut aufzugreifen.
   - Beobachtung: dezentrale Versionspflege je `.csproj` — `Directory.Packages.props` bei Migration erwägenswert (separate Entscheidung).
3. Vergabene Issue-Nummer notieren (wird im Kommentar auf #179 und im PR-Body verlinkt).

Beteiligte Komponenten: `gh`-CLI, GitHub-Repo `martin-stromberg/Reporter`.

### Dokumentation und Abschluss

1. Kommentar auf Issue #179 posten (`gh issue comment 179`): Ergebnistabelle, Aussage „keine einfachen Anpassungen notwendig", Verlinkung des Folge-Issues.
2. Finaler Commit auf dem Task-Branch mit `Closes #179` im Commit-Body (siehe Designentscheidung); Prozessartefakt `docs/features/task/issue-179-...-tasks.md` bleibt als einzige Repo-Änderung bestehen, das Feature-Unterverzeichnis wird wie üblich aufgeräumt.
3. PR gegen `staging` erstellen; PR-Body enthält dieselbe Ergebnistabelle, die Liste der aufgeschobenen Empfehlungen (gesamte 11er-Welle) und den Link zum Folge-Issue.
4. PR-Checks überwachen (`gh pr checks --watch --fail-fast` bzw. Polling mit Fail-Erkennung); bei `fail` sofort `gh run view --job <id> --log-failed` diagnostizieren.

Beteiligte Komponenten: `gh`-CLI, `docs/features/task/issue-179-...-tasks.md`, `git`.

## Neue Klassen

Keine — Wartungs-/Audit-Aufgabe ohne Codeänderung.

## Änderungen an bestehenden Klassen

Keine. Es sind nach aktuellem Stand keine `.csproj`-Änderungen vorgesehen (kein Paket-Update erfüllt das Kriterium „niedriges bis mittleres Risiko"). Nur im bedingten Pfad des Verifikations-Prüfdurchlaufs (neu gemeldetes Patch-/Minor-Update in der 10er-Linie) kämen `Version`-Attribute in `PackageReference`-Einträgen der betroffenen `.csproj` hinzu.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine. Bewusst nicht geändert: `.github/dependabot.yml` (keine `ignore`-Regeln für die 11er-Welle — Umgang mit Dependabot-PRs wird im Folge-Issue geregelt), `global.json` (kein SDK-Pinning), `Directory.Packages.props` (wird nicht eingeführt).

## Seiteneffekte und Risiken

- **Kein Code-Risiko:** Da keine Paketversionen geändert werden, entsteht kein Build-/Laufzeitrisiko; `Run-StaticChecks.ps1` und `dotnet test` dienen nur als Regressionssicherung des unveränderten Stands.
- **Wiederkehrende Meldungen:** Der wöchentliche `review-nuget.yml`-Lauf erzeugt weiterhin Review-Issues, die dieselben 11.x-Updates finden. Mitigation: Kommentar auf #179 und das Folge-Issue enthalten den Verweis, dass die 11er-Welle bereits erfasst ist — Folge-Reviews verlinken statt neu bewerten.
- **Dependabot:** Dependabot kann jederzeit Major-Update-PRs (z. B. EF Core 11.0.0) gegen `staging` öffnen. Diese sind bis zur .NET-11-Migration fachlich nicht mergebar. Mitigation: Hinweis im Folge-Issue; bei Vorliegen solcher PRs im Verifikations-Schritt entsprechend kommentieren/verlinken.
- **Versionsdrift zwischen Bestandsaufnahme und Umsetzung:** Durch den Re-Run der Scans abgedeckt (bedingte Pfade).

## Umsetzungsreihenfolge

1. **Verifikations-Prüfdurchlauf (Re-Run der Scans + Dependabot-PR-Check)**
   - Voraussetzungen: Keine (dotnet SDK 10.0.403 und `gh` sind lokal vorhanden und in der Bestandsaufnahme nachgewiesen).
   - Beschreibung: Drei `dotnet list`-Scans erneut ausführen, `gh pr list --author app/dependabot` prüfen, Ergebnis mit `inventory/`-Dokumenten abgleichen. Bei Abweichungen die bedingten Pfade aus dem Ablauf „Verifikations-Prüfdurchlauf" anwenden.

2. **TFM-Anforderungen der 11.x-Pakete verifizieren**
   - Voraussetzungen: Schritt 1 (aktuelle Zielversionen).
   - Beschreibung: NuGet-Metadaten der 11.x-Zielpakete auf unterstützte TFMs prüfen; Befund „erfordert net11.0 / .NET-11-SDK bzw. MAUI-11-Workload" als Risikobegründung in die Ergebnistabelle aufnehmen.

3. **Ergebnistabelle mit Risikobewertung finalisieren**
   - Voraussetzungen: Schritte 1–2.
   - Beschreibung: Tabelle aller 13 direkten Pakete + Abschnitt transitive Ausreißer erstellen (Markdown-Snippet, für Issue-Kommentar und PR-Body identisch).

4. **Folge-Issue „.NET-11-Migration" anlegen**
   - Voraussetzungen: Schritt 3 (Paketliste/Zielversionen stehen fest).
   - Beschreibung: `gh issue create` mit dem im Ablauf beschriebenen Body; Issue-Nummer notieren.

5. **Kommentar auf Issue #179 posten**
   - Voraussetzungen: Schritte 3–4 (Tabelle + Folge-Issue-Nummer).
   - Beschreibung: `gh issue comment 179` mit Ergebnistabelle, Fazit „keine einfachen Anpassungen notwendig" und Link zum Folge-Issue.

6. **Verifikation: Tests und statische Checks**
   - Voraussetzungen: Schritt 1 (finaler Paketstand); keine.
   - Beschreibung: `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj -c Release` ausführen (MTP-Runner gemäß `global.json`), danach `.\scripts\Run-StaticChecks.ps1` — muss mit Exit-Code 0 und ohne Findings durchlaufen (AGENTS.md-Vorgabe). Im bedingten Pfad mit Paketänderung vorher `dotnet restore Reporter.sln -r win-x64` und Build mit `-p:IncludeIosTarget=false -p:IncludeAndroidTarget=false`.

7. **Abschluss-Commit und PR gegen `staging`**
   - Voraussetzungen: Schritte 3–6; `docs/features/task/issue-179-...-tasks.md` liegt vor (Lifecycle-Artefakt).
   - Beschreibung: Feature-Unterverzeichnis aufräumen, finaler Commit mit `Closes #179` im Body, `gh pr create --base staging` mit PR-Body-Tabelle + Liste aufgeschobener Empfehlungen + Folge-Issue-Link. PR-Checks mit Fail-fast-Überwachung verfolgen.

## Tests

### Neue Tests

Keine — keine Funktionsänderung.

### Betroffene bestehende Tests

Keine — keine Paket- oder Codeänderung geplant. Im bedingten Pfad (Patch-/Minor-Update in der 10er-Linie) dient die vorhandene `Reporter.Tests`-Suite unverändert als Regressionsnachweis.

### E2E-Tests (Pflicht)

Keine — keine neue oder geänderte Benutzerinteraktion; die Mobile-UI-Design-Review-Regel greift nicht (kein UI-/Seiten-Change). `Reporter.E2ETests` (`FlaUI.UIA3`) wird nicht verändert und muss nicht lokal ausgeführt werden.

## Akzeptanzszenarien

| # | Szenario | Nachweis |
|---|----------|----------|
| 1 | Alle 13 direkten Pakete sind bewertet (Befund, Empfehlung, Risiko) und transitive Ausreißer sind dokumentiert | Ergebnistabelle im Kommentar auf Issue #179 und im PR-Body |
| 2 | Kein niedrig-/mittelrisikoreiches Update blieb unumgesetzt — begründet: es existiert kein Patch-/Minor-Update in der 10er-Linie; Schwachstellen-/Deprecated-Scans sind sauber | Scan-Ausgaben + Tabelle |
| 3 | Risikoreiche 11er-Welle ist als Folge-Issue erfasst und in #179 verlinkt | Folge-Issue existiert, Kommentar auf #179 verweist darauf |
| 4 | Issue #179 wird durch den PR geschlossen (faktisch bei Promotion `staging`→`main`); nur das Folge-Issue bleibt offen | `Closes #179` im Commit, PR gegen `staging` |
| 5 | `dotnet test` für `Reporter.Tests` grün und `.\scripts\Run-StaticChecks.ps1` endet mit Exit-Code 0 ohne Findings | Lokale Ausführung + CI-Jobs `static-checks`/`build-and-test` auf dem PR |

## Offene Punkte

Alle Punkte wurden mit den unten genannten Entscheidungen geklärt (automatisierte Bearbeitung: Vorschläge sind jeweils direkt aus dem Anforderungstext ableitbar bzw. branchenübliche Defaults; sie werden im Abschluss-Kommentar auf Issue #179 transparent gemacht und können dort korrigiert werden).

| # | Punkt | Entscheidung |
|---|-------|--------------|
| 1 | Pre-Release-/Preview-Versionen berücksichtigen? | **Nein — nur stabile Versionen.** Konsistent mit Dependabot-Verhalten und Wartungsziel; Preview-Updates erhöhen das Risiko ohne Wartungsnutzen. |
| 2 | Zeitpunkt/Priorisierung der .NET-11-Migration? | **Folge-Issue sofort anlegen, Umsetzung nicht terminieren.** Empfehlung: frühestens nach ersten 11.0.x-Servicing-Releases und stabilen MAUI-11-Workloads für alle Zielplattformen. .NET 10 ist LTS (Support bis 11/2028) — kein Zeitdruck; die wöchentlichen Reviews beobachten die Lage. Entspricht der Anforderung „risikoreiche Empfehlungen dokumentieren". |
| 3 | Umgang mit künftigen Dependabot-PRs für 11.x-Major-Updates? | **Keine Config-Änderung; prozedural lösen.** Dependabot-PRs für 11.x kommentieren („wartet auf .NET-11-Migration, siehe Folge-Issue #N") und schließen bzw. auf Eis legen; das Folge-Issue dokumentiert diese Regel für die Zeit bis zur Migration. |
