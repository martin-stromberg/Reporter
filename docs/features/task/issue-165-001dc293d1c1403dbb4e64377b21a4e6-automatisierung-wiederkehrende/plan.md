<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Automatisierung wiederkehrender Architektur-, Sicherheits- und Compliance-Reviews (GitHub Issue #165)

## Übersicht

Es werden drei eigenständige GitHub-Actions-Workflows unter `.github/workflows/` neu angelegt, die zeitgesteuert (`schedule`/Cron, UTC) und manuell (`workflow_dispatch`) GitHub-Issues mit fester `@softwarewerkstatt`-Erwähnung im Body erzeugen. Die Duplikatvermeidung erfolgt über je ein review-spezifisches Label nach dem etablierten Muster „Label sicherstellen → offene Objekte prüfen → nur bei leerer Liste anlegen" aus `staging-to-main-promotion.yml`. Ergänzend entsteht eine Dokumentationsseite unter `docs/help/release-management/` inklusive Verlinkungen. C#-Code, Datenmodell und UI sind nicht betroffen.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Workflow-Aufteilung | Drei eigenständige Workflow-Dateien: `review-architecture.yml`, `review-nuget.yml`, `review-compliance.yml` | Vorgabe des Auftraggebers; getrennte Zeitpläne, Concurrency-Gruppen und Labels — ein Fehler in einem Workflow blockiert die anderen nicht; entspricht der Dateinamenskonvention (Kleinbuchstaben, Bindestriche) |
| Duplikat-Erkennung | Je Review-Typ ein eigenes Label (`scheduled-architecture-review`, `scheduled-nuget-review`, `scheduled-compliance-review`); Label per `gh label create --force` idempotent sicherstellen, dann `gh issue list --label <label> --state open` | Vorgabe des Auftraggebers; deckt sich mit dem etablierten Label-/Dedup-Muster (`staging-to-main-promotion.yml:45-63`, `sync-staging-with-main.yml`); robuster als ein Titel-Präfix-Vergleich, der im Bestand nirgends vorkommt |
| Checkout | Kein `actions/checkout`-Schritt | Reine `gh`-CLI-Operationen (Label, Issue-Liste, Issue-Create) benötigen keinen Repository-Inhalt; `staging-to-main-promotion.yml` checkt nur für die Git-Diff-Ermittlung aus |
| Permissions | Nur `issues: write` auf Workflow-Ebene | Minimal-Prinzip; `gh label create`, `gh issue list` und `gh issue create` werden durch `issues: write` abgedeckt; ohne Checkout ist `contents: read` nicht erforderlich |
| Dedup bei `workflow_dispatch` | Duplikatprüfung gilt auch bei manueller Auslösung; kein `force`-Input | Verhindert Duplikate konsistent; ein gewollter neuer Zyklus wird durch Schließen des Vorgänger-Issues ausgelöst |
| Titel-Schema | Festes deutsches Präfix + ISO-Datumsangabe via `date -u` (`YYYY-MM`, `KW YYYY-Www`, `YYYY`) | Locale-unabhängig (Runner liefern englische Monatsnamen), eindeutig und sortierbar; erfüllt die Vorgabe „aussagekräftiger Titel" |
| `@softwarewerkstatt` | Nur Erwähnung im Issue-Body, kein `--assignee` | Die Anforderung schreibt nur die Erwähnung vor; eine darauf reagierende Automatisierung liegt außerhalb des Repositories |
| Doku-Ablage | Neue Seite `docs/help/release-management/wiederkehrende-reviews.md` | `release-management/` dokumentiert bereits alle `gh`-CLI-/Label-Automatisierungen; die Review-Workflows sind derselbe Automatisierungstyp (zeitgesteuerte Repo-Pflege) |

## Programmabläufe

### Zeitgesteuerter oder manueller Review-Lauf (identisch in allen drei Workflows)

1. Trigger: `schedule` (Cron-Ausdruck, UTC) oder `workflow_dispatch` (ohne Inputs). Genau ein Job `create-review-issue` auf `ubuntu-latest` mit `timeout-minutes: 10`; `concurrency`-Gruppe `<workflow-name>` mit `cancel-in-progress: false` verhindert parallele Doppelerzeugung.
2. Schritt „Ensure `<label>` label exists": `gh label create <label> --color "<RRGGBB>" --description "<text>" --force` mit `env: GH_TOKEN: ${{ github.token }}` — legt das Label beim ersten Lauf an, ist wiederholbar.
3. Schritt „Create review issue if none open" mit `env: GH_TOKEN: ${{ github.token }}` und mehrzeiligem Bash (`set -euo pipefail`):
   a. `existing="$(gh issue list --label <label> --state open --json number)"` ausführen.
   b. `echo "$existing" | jq length` auswerten (`jq` ist auf `ubuntu-latest` vorinstalliert).
   c. Liste leer → `gh issue create --title "<Präfix> <Datumsangabe>" --body "<fester Body-Text>" --label "<label>"`; die Datumsangabe wird im selben Schritt per `date -u` ermittelt.
   d. Liste nicht leer → Log-Ausgabe („offenes Issue existiert bereits"), kein neues Issue.

Beteiligte Komponenten: je eine Workflow-Datei, `gh`-CLI und `jq` (beide auf `ubuntu-latest` vorinstalliert), `github.token`.

### Parameter je Workflow

| Datei | `name:` | Cron (UTC) | Label | Farbe | Issue-Titel (Muster) |
|-------|---------|------------|-------|-------|----------------------|
| `.github/workflows/review-architecture.yml` | `Scheduled Architecture Review` | `0 6 1 * *` (monatlich, 1. Tag, 06:00) | `scheduled-architecture-review` | `5319E7` | `Architektur- und Sicherheitsreview $(date -u +'%Y-%m')` |
| `.github/workflows/review-nuget.yml` | `Scheduled NuGet Review` | `0 6 * * 1` (wöchentlich, Montag 06:00) | `scheduled-nuget-review` | `0075CA` | `NuGet-Paketprüfung KW $(date -u +'%G-W%V')` |
| `.github/workflows/review-compliance.yml` | `Scheduled Compliance Review` | `0 6 1 1 *` (jährlich, 1. Januar 06:00) | `scheduled-compliance-review` | `D93F0B` | `Compliance- und Copyright-Review $(date -u +'%Y')` |

### Issue-Body-Texte (wörtlich aus der Anforderung, jeweils als fester `--body`-Inhalt)

- `review-architecture.yml`: `@softwarewerkstatt Bitte führe ein Architektur- und Sicherheitsreview durch. Prüfe insbesondere: Neue Sicherheitsrisiken in verwendeten Frameworks und Bibliotheken; Bekannte Schwachstellen (CVEs) in NuGet-Abhängigkeiten; Neue Empfehlungen für ASP.NET Core, Blazor, .NET und verwandte Technologien; Änderungen bei Authentifizierung und Autorisierung; Neue oder inzwischen bevorzugte Design Patterns; Veraltete, abgekündigte oder problematische APIs; Verbesserungsmöglichkeiten hinsichtlich Wartbarkeit, Erweiterbarkeit und Testbarkeit; Aktuelle Empfehlungen zu Logging, Telemetrie und Monitoring; Relevante Erkenntnisse aus der Community, Microsoft-Dokumentation und Sicherheitsmeldungen. Erstelle eine priorisierte Liste konkreter Handlungsempfehlungen.`
- `review-nuget.yml`: `@softwarewerkstatt Bitte analysiere alle verwendeten NuGet-Pakete. Prüfe: Verfügbare Updates; Security-Updates; Pakete mit bekannt gewordenen Schwachstellen; Veraltete oder nicht mehr gepflegte Pakete; Empfohlene Nachfolgepakete; Mögliche Breaking Changes bei Updates. Erstelle konkrete Update-Empfehlungen inklusive Risikobewertung. Setze die unbedenklichen Empfehlungen (niedriges bis mittleres Risiko) direkt in einem Pull Request um. Risikoreiche oder aufgeschobene Empfehlungen dokumentiere im PR-Body bzw. als eigenes Folge-Issue.`
- `review-compliance.yml`: `@softwarewerkstatt Bitte führe das jährliche Compliance-Review durch. Prüfe: Copyright-Hinweise im Quellcode; Copyright-Angaben auf Webseiten; Copyright-Angaben in About-Dialogen; Impressum; Datenschutzerklärung; Drittanbieter-Lizenzen; Open-Source-Verpflichtungen; Unternehmens- und Kontaktinformationen; App-Store-Metadaten (falls vorhanden). Erstelle konkrete Änderungsvorschläge und liste erkannte Risiken oder Handlungsbedarfe auf.`

### Bereitstellung und Wirksamkeit

1. Änderungen kommen per Feature-Branch als PR gegen `staging` (Direkt-Commits auf `main`/`staging` blockt `.githooks/pre-commit`; `verify-pr-source.yml` erzwingt den Weg über `staging`).
2. Über `staging-to-main-promotion.yml` gelangen die Workflow-Dateien nach `main`.
3. `schedule`-Trigger wirken ausschließlich auf dem Default-Branch (`main`) — bis zur Promotion läuft kein Cron (bekannte, bereits dokumentierte Cold-Start-Problematik).
4. Nach dem Merge auf `main` wird `review-architecture.yml` einmal manuell per `workflow_dispatch` angestoßen (Auftraggeber-Vorgabe); dadurch entstehen Label und erstes Issue sofort statt erst zum nächsten Monatsersten.

## Neue Klassen

Keine — es entstehen keine C#-Artefakte. Neu zu erstellende Dateien:

| Datei | Typ | Zweck |
|-------|-----|-------|
| `.github/workflows/review-architecture.yml` | GitHub-Actions-Workflow | Monatliches Architektur- und Sicherheitsreview als Issue |
| `.github/workflows/review-nuget.yml` | GitHub-Actions-Workflow | Wöchentliche NuGet-Paketprüfung als Issue |
| `.github/workflows/review-compliance.yml` | GitHub-Actions-Workflow | Jährliches Compliance- und Copyright-Review als Issue |
| `docs/help/release-management/wiederkehrende-reviews.md` | Dokumentationsseite | Zweck, Zeitpläne, Titel-/Label-Schema, Dedup-Mechanismus, manuelle Auslösung, Cold-Start-Hinweis |

## Änderungen an bestehenden Klassen

Keine C#-Klassen betroffen. Änderungen an bestehenden Dateien:

### `docs/help/release-management/index.md`

- Inhaltsliste um den Eintrag „Wiederkehrende Reviews" → `wiederkehrende-reviews.md` ergänzen.

### `docs/help/release-management/architektur.md`

- Tabelle „Beteiligte Komponenten" um drei Zeilen für `review-architecture.yml`, `review-nuget.yml`, `review-compliance.yml` (Typ: GitHub-Actions-Workflow; Rolle: geplante Review-Issues mit Label-Dedup) ergänzen.

### `docs/help/release-management/installation.md`

- Tabelle „Workflow-Permissions" um drei Zeilen (`review-*.yml` → `issues: write`) ergänzen.
- „Installationsschritte": Hinweis ergänzen, dass die drei `scheduled-*-review`-Labels vom jeweiligen Workflow beim ersten Lauf per `gh label create --force` angelegt werden und dass `review-architecture.yml` nach dem ersten Merge auf `main` einmalig manuell per `workflow_dispatch` zu starten ist.

### `docs/help/index.md`

- Bestehenden Eintrag „Release-Management" in der Gruppe „Systemverwaltung" in der Kurzbeschreibung um die zeitgesteuerten Review-Workflows ergänzen.

### `README.md`

- Abschnitt „Weitere Informationen → Betrieb und Entwicklung": Release-Management-Bullet um den Verweis auf wiederkehrende Review-Issues ergänzen (bzw. CI/CD-Absatz kurz erwähnen).

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine fachlichen Eingabe-Validierungen. Als technische Prüfung gilt: Die YAML-Syntax der drei neuen Workflow-Dateien wird lokal verifiziert (z. B. `python -c "import yaml; yaml.safe_load(...)"` oder `yamllint`), bevor sie committet wird — siehe Umsetzungsreihenfolge.

## Konfigurationsänderungen

Keine App-/Laufzeit-Konfiguration. Zeitpläne (Cron), Label-Namen, Issue-Titel und Body-Texte sind fest in den Workflow-Dateien hinterlegt; es werden weder Repository-Variablen noch Secrets eingeführt — Authentifizierung ausschließlich über `GH_TOKEN: ${{ github.token }}`.

## Seiteneffekte und Risiken

- **Neue Labels im Repository:** `scheduled-architecture-review`, `scheduled-nuget-review` und `scheduled-compliance-review` werden beim ersten Lauf angelegt — konsistent zu `automated-promotion`/`automated-backmerge`, aber sichtbare Erweiterung des Label-Sets.
- **Zeitslot-Kollision mit `security-scan.yml`:** Die NuGet-Prüfung teilt sich Montag 06:00 UTC mit dem Security Scan — unkritisch (eigene Concurrency-Gruppen, unterschiedliche Jobs, Issue-Erzeugung dauert nur Sekunden).
- **Cold-Start:** `schedule` läuft nur auf `main`; auf `staging` und im Feature-Branch passiert nichts Zeitgesteuertes. Bis zur Promotion nach `main` ist lediglich `workflow_dispatch` verfügbar — sobald die Datei auf `main` liegt.
- **Issue-Verhalten:** Ein offenes Review-Issue blockiert den Folgezyklus (Dedup) — gewollt, aber in der Doku zu beschreiben: Wer ein neues Issue will, schließt das alte.
- **Lizenzheader-Gate:** Neue `.yml`-Dateien benötigen die Hash-Kopfzeile, neue `.md`-Dateien den XML-Kommentar — sonst schlägt `node scripts/add-license-headers.mjs --check` in den `static checks` fehl.
- **Externe Abhängigkeit `@softwarewerkstatt`:** Die Erwähnung ist reiner Issue-Text; ob eine Automatisierung darauf reagiert, liegt außerhalb des Repositories — in der Doku als Einschränkung zu vermerken.
- **Duplikate bei parallelen Läufen:** `gh issue list` + `gh issue create` ist nicht atomar; die `concurrency`-Gruppe je Workflow verhindert parallele Läufe desselben Typs — als ausreichend bewertet (gleiches Risiko tragen die bestehenden PR-Automatisierungen).

## Umsetzungsreihenfolge

1. **`.github/workflows/review-architecture.yml` anlegen**
   - Voraussetzungen: Keine — Referenzmuster `staging-to-main-promotion.yml` und `security-scan.yml` liegen im Repo vor.
   - Beschreibung: Lizenzheader (Hash-Kommentar, Zeile 1), `name: Scheduled Architecture Review`, Trigger `schedule` (`0 6 1 * *`) + `workflow_dispatch`, `permissions: issues: write`, `concurrency`-Gruppe `review-architecture` (`cancel-in-progress: false`), Job `create-review-issue` auf `ubuntu-latest` (`timeout-minutes: 10`), Schritte „Ensure label exists" und „Create review issue if none open" gemäß Programmablauf mit Label `scheduled-architecture-review` und dem Architektur-Body.

2. **`.github/workflows/review-nuget.yml` anlegen**
   - Voraussetzungen: Schritt 1 als direkte Vorlage.
   - Beschreibung: Gleiches Gerüst; `name: Scheduled NuGet Review`, Cron `0 6 * * 1`, Concurrency-Gruppe `review-nuget`, Label `scheduled-nuget-review`, Titel `NuGet-Paketprüfung KW <YYYY-Www>`, NuGet-Body.

3. **`.github/workflows/review-compliance.yml` anlegen**
   - Voraussetzungen: Schritt 1 als direkte Vorlage.
   - Beschreibung: Gleiches Gerüst; `name: Scheduled Compliance Review`, Cron `0 6 1 1 *`, Concurrency-Gruppe `review-compliance`, Label `scheduled-compliance-review`, Titel `Compliance- und Copyright-Review <YYYY>`, Compliance-Body.

4. **YAML-Syntax der neuen Workflows prüfen**
   - Voraussetzungen: Schritte 1–3.
   - Beschreibung: Lokale Syntaxprüfung aller drei Dateien, z. B. per `python -c "import yaml,sys; [yaml.safe_load(open(f, encoding='utf-8')) for f in sys.argv[1:]]" .github/workflows/review-*.yml` oder `yamllint`; zusätzlich Sichtprüfung der Cron-Ausdrücke und der wörtlichen Body-Texte gegen die Anforderung.

5. **`docs/help/release-management/wiederkehrende-reviews.md` anlegen**
   - Voraussetzungen: Schritte 1–3 (die Seite beschreibt das finale Verhalten).
   - Beschreibung: Seitenkonvention einhalten (XML-Lizenzheader, `← [Zurück zur Übersicht](index.md)`, Titel `# Release-Management — Wiederkehrende Reviews`); Inhalte: Zweck, Tabelle der drei Workflows (Zeitplan, Titelmuster, Label), Funktionsweise der Duplikatprüfung, manuelle Auslösung per `workflow_dispatch`, Hinweis „offenes Issue blockiert Folgezyklus — zum Neustart schließen", Cold-Start-Hinweis (Cron wirkt erst ab `main`; einmaliger manueller Start des Architektur-Workflows nach dem Merge), Einschränkung zur externen `@softwarewerkstatt`-Verarbeitung.

6. **Verlinkungen und Bestandsdoku nachtragen**
   - Voraussetzungen: Schritt 5.
   - Beschreibung: `docs/help/release-management/index.md` (Inhaltsliste), `docs/help/release-management/architektur.md` (Komponententabelle), `docs/help/release-management/installation.md` (Permissions-Tabelle + Installationsschritte), `docs/help/index.md` (Release-Management-Eintrag), `README.md` („Weitere Informationen → Betrieb und Entwicklung").

7. **Lokale statische Checks ausführen**
   - Voraussetzungen: Schritte 1–6.
   - Beschreibung: `.\scripts\Run-StaticChecks.ps1` muss mit Exit-Code 0 ohne Befund durchlaufen (AGENTS.md-Pflicht); für diese Änderung ist v. a. der `LicenseHeaders`-Check relevant — die übrigen Checks (Format, Security, Static-Analysis-Build) laufen über die Solution und bleiben unverändert.

8. **PR gegen `staging`; nach Promotion auf `main` manuell verifizieren**
   - Voraussetzungen: Schritt 7 grün.
   - Beschreibung: PR erstellen und mergen; nach dem Promotion-Lauf `staging` → `main` den Workflow `Scheduled Architecture Review` einmal per `workflow_dispatch` starten (Auftraggeber-Vorgabe), Ergebnis prüfen (Label angelegt, Issue mit korrektem Titel/Body/Label), anschließend Zweitlauf zur Dedup-Verifikation (kein neues Issue). Nacharbeit in der Doku bzw. `test-results.md` festhalten.

## Tests

### Neue Tests

Keine — es existiert keine automatisierbare Testinfrastruktur für GitHub-Actions-Workflows (weder YAML-Lint in der CI noch Workflow-Tests; `npm test` deckt nur die Node-Release-Skripte ab). Verifikation erfolgt manuell über `workflow_dispatch` und YAML-/Sichtprüfung (siehe Umsetzungsreihenfolge, Schritte 4 und 8).

### Betroffene bestehende Tests

Keine.

### E2E-Tests (Pflicht)

Keine neuen E2E-Tests erforderlich — die Anforderung ändert keine Benutzerinteraktion in der Reporter-App; die bestehende Windows-E2E-Smoke-Suite ist unberührt. Der Happy Path der neuen Automatisierung wird stattdessen manuell verifiziert:

| Szenario | Verifikationsweg | Abgedecktes Kriterium |
|----------|------------------|------------------------|
| Manueller Lauf erzeugt Review-Issue | `workflow_dispatch` auf `Scheduled Architecture Review` nach Merge auf `main` | Issue existiert mit Label `scheduled-architecture-review`, Titel „Architektur- und Sicherheitsreview JJJJ-MM", Body mit führender `@softwarewerkstatt`-Erwähnung und allen Prüfpunkten |
| Duplikatvermeidung | Zweiter `workflow_dispatch`-Lauf desselben Workflows | Kein zweites Issue; Log-Meldung „offenes Issue existiert bereits" |
| Label-Idempotenz | Label vorab löschen bzw. Lauf wiederholen | `gh label create --force` legt/aktualisiert das Label fehlerfrei |

Bestehende E2E-Tests betroffen: Keine.

## Offene Punkte

Keine.
