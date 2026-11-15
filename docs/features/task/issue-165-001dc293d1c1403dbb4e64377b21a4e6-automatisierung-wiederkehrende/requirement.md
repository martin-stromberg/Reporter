# Übersetzte Anforderung: Automatisierung wiederkehrender Architektur-, Sicherheits- und Compliance-Reviews

Quelle: GitHub Issue #165 (martin-stromberg/Reporter) inkl. Auftraggeber-Kommentar.

## Fachliche Zusammenfassung

Es werden drei neue, eigenständige GitHub-Actions-Workflows unter `.github/workflows/` eingeführt, die zeitgesteuert (`schedule`/Cron) und zusätzlich manuell (`workflow_dispatch`) GitHub-Issues anlegen. Jedes Issue enthält im Body eine Erwähnung von `@softwarewerkstatt`, die die automatische Analyse und Bearbeitung durch den Automatisierungs-Account anstoßen soll. Vor dem Anlegen prüft jeder Workflow, ob bereits ein offenes Issue desselben Typs aus einem früheren Zyklus existiert, und erzeugt in diesem Fall kein neues Issue (Duplikatvermeidung). Die Änderungen gelangen per PR gegen `staging` über den bestehenden Promotions-Mechanismus (`staging-to-main-promotion.yml`) nach `main`; da Cron-Trigger ausschließlich auf dem Default-Branch (`main`) wirken, wird die monatliche Architekturprüfung nach dem Merge einmal manuell per `workflow_dispatch` angestoßen.

## Betroffene Klassen und Komponenten

Keine Änderungen am C#-Datenmodell, an Services, Interfaces, Enums oder UI-Komponenten — die Anforderung betrifft ausschließlich die CI/CD-Infrastruktur des Repositories.

Neu zu erstellende Artefakte:

- `.github/workflows/architecture-security-review.yml` — Workflow „Monatliches Architektur- und Sicherheitsreview" (Dateiname ist ein Vorschlag nach bestehender Namenskonvention in Kleinbuchstaben mit Bindestrichen)
- `.github/workflows/nuget-package-review.yml` — Workflow „Wöchentliche NuGet-Prüfung"
- `.github/workflows/compliance-copyright-review.yml` — Workflow „Jährliches Compliance- und Copyright-Review"
- Dokumentationsseite unter `docs/help/` (voraussichtlich `docs/help/release-management/` oder `docs/help/entwicklung/`; genaue Ablage im Planungsschritt festzulegen), die Zweck, Zeitpläne, Issue-Titel, Dedup-Mechanismus und manuelle Auslösung beschreibt — Akzeptanzkriterium „vollständig innerhalb des Repositories dokumentiert"

Bestehende Artefakte als Referenz/Wiederverwendung:

- `.github/workflows/security-scan.yml` — Vorlage für `schedule` + `workflow_dispatch`-Trigger und minimale `permissions`
- `.github/workflows/staging-to-main-promotion.yml` und `.github/workflows/sync-staging-with-main.yml` — Vorlage für `gh`-CLI-Nutzung mit `GH_TOKEN: ${{ github.token }}`, `jq`-Auswertung von `gh ... list --json` sowie das Muster „Label sicherstellen, dann prüfen, ob bereits ein offenes Objekt existiert, erst dann anlegen"
- Lizenzheader-Konvention: Jede neue Workflow-Datei erhält die Kopfzeile `# Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.` (wird vom License-Header-Check `node scripts/add-license-headers.mjs --check` in `staging-ci.yml` geprüft)

Tests: Es gibt keine automatisierbare Test-Infrastruktur für Workflows; Verifikation erfolgt manuell über `workflow_dispatch` (für die Architekturprüfung explizit vom Auftraggeber gefordert) und durch Review der YAML-Syntax.

## Implementierungsansatz

Alle drei Workflows folgen demselben Muster, das an die vorhandenen Automatisierungs-Workflows angelehnt ist:

1. **Trigger:**
   - `schedule` mit Cron-Ausdruck (Annahme: UTC, Ausführungszeit ca. 06:00 Uhr analog zu `security-scan.yml` `cron: '0 6 * * 1'`):
     - Architektur-/Sicherheitsreview: monatlich am 1. Tag, z. B. `0 6 1 * *`
     - NuGet-Prüfung: wöchentlich, z. B. `0 6 * * 1` (Wochentag/Uhrzeit im Planungsschritt festlegen, ggf. versetzt zum bestehenden Security Scan)
     - Compliance-/Copyright-Review: jährlich am 1. Januar, z. B. `0 6 1 1 *`
   - `workflow_dispatch` für manuelle Auslösung (vom Auftraggeber explizit gefordert; für die monatliche Architekturprüfung nach dem Merge einmal auszuführen)
2. **Runner/Permissions:** `runs-on: ubuntu-latest` (reine `gh`-CLI-Logik, kein .NET-Build nötig); `permissions:` minimal auf `issues: write` (ggf. `contents: read`); `concurrency`-Gruppe pro Workflow zur Vermeidung paralleler Doppelerzeugung.
3. **Duplikatprüfung:** Vor dem `gh issue create` wird per `gh issue list --state open --json number,title,labels` geprüft, ob bereits ein offenes Issue desselben Typs existiert. Als Erkennungsmerkmal bietet sich ein dediziertes Label (z. B. `automated-review` oder ein Label je Review-Typ) an, das — analog zu `automated-promotion`/`automated-backmerge` — vorab per `gh label create --force` sichergestellt wird. Alternative: Abgleich über ein eindeutiges Titel-Präfix. Konkrete Wahl im Planungsschritt zu treffen (siehe Offene Fragen).
4. **Issue-Erzeugung:** `gh issue create` mit aussagekräftigem Titel (Vorschlag: festes Präfix + Datum, z. B. „Architektur- und Sicherheitsreview – YYYY-MM", „NuGet-Paketprüfung – KW YYYY-WW", „Compliance-Review YYYY") und dem exakt im Issue vorgegebenen Body-Text inkl. führender `@softwarewerkstatt`-Erwähnung.
5. **Body-Texte:** wörtlich aus der Anforderung übernehmen:
   - Architektur-/Sicherheitsreview: `@softwarewerkstatt Bitte führe ein Architektur- und Sicherheitsreview durch. Prüfe insbesondere: Neue Sicherheitsrisiken in verwendeten Frameworks und Bibliotheken; Bekannte Schwachstellen (CVEs) in NuGet-Abhängigkeiten; Neue Empfehlungen für ASP.NET Core, Blazor, .NET und verwandte Technologien; Änderungen bei Authentifizierung und Autorisierung; Neue oder inzwischen bevorzugte Design Patterns; Veraltete, abgekündigte oder problematische APIs; Verbesserungsmöglichkeiten hinsichtlich Wartbarkeit, Erweiterbarkeit und Testbarkeit; Aktuelle Empfehlungen zu Logging, Telemetrie und Monitoring; Relevante Erkenntnisse aus der Community, Microsoft-Dokumentation und Sicherheitsmeldungen. Erstelle eine priorisierte Liste konkreter Handlungsempfehlungen.`
   - NuGet-Prüfung: `@softwarewerkstatt Bitte analysiere alle verwendeten NuGet-Pakete. Prüfe: Verfügbare Updates; Security-Updates; Pakete mit bekannt gewordenen Schwachstellen; Veraltete oder nicht mehr gepflegte Pakete; Empfohlene Nachfolgepakete; Mögliche Breaking Changes bei Updates. Erstelle konkrete Update-Empfehlungen inklusive Risikobewertung. Setze die unbedenklichen Empfehlungen (niedriges bis mittleres Risiko) direkt in einem Pull Request um. Risikoreiche oder aufgeschobene Empfehlungen dokumentiere im PR-Body bzw. als eigenes Folge-Issue.`
   - Compliance-Review: `@softwarewerkstatt Bitte führe das jährliche Compliance-Review durch. Prüfe: Copyright-Hinweise im Quellcode; Copyright-Angaben auf Webseiten; Copyright-Angaben in About-Dialogen; Impressum; Datenschutzerklärung; Drittanbieter-Lizenzen; Open-Source-Verpflichtungen; Unternehmens- und Kontaktinformationen; App-Store-Metadaten (falls vorhanden). Erstelle konkrete Änderungsvorschläge und liste erkannte Risiken oder Handlungsbedarfe auf.`
6. **Bereitstellung:** PR gegen `staging`; Wirksamkeit der Cron-Trigger erst nach Promotion nach `main` (Schedule läuft nur auf dem Default-Branch). Manuelle Verifikation des Architektur-Workflows per `workflow_dispatch` nach dem Merge.
7. **Dokumentation:** Neue Seite unter `docs/help/` mit Zeitplänen, Titeln, Dedup-Logik und Hinweis auf manuelle Auslösbarkeit; ggf. Verlinkung aus `docs/help/index.md` bzw. `README.md`.

## Konfiguration

Keine Laufzeit-Konfiguration der App betroffen. Die Intervalle werden fest in den Cron-Ausdrücken der Workflow-Dateien hinterlegt (keine Repository-Variablen vorgesehen — Annahme, da die Anforderung keine Konfigurierbarkeit verlangt). Der Ziel-Account `@softwarewerkstatt` steht fest im Issue-Body; bei Bedarf könnte er als Repository-Variable ausgelagert werden (optional, im Planungsschritt zu entscheiden). Optional kann pro Workflow ein spezifisches Label (z. B. `automated-review`) für die Duplikat-Erkennung konfiguriert werden.

## Offene Fragen

- **Erkennungsmerkmal für Duplikate:** Soll die Prüfung auf offene Vorgänger-Issues über ein eigenes Label (z. B. `automated-review` oder drei typspezifische Labels) oder über einen festen Titel-Präfix erfolgen? Label-Variante ist robuster, erzeugt aber neue Labels im Repository.
- **Genauer Wochentag/Uhrzeit der wöchentlichen NuGet-Prüfung:** Annahme Montag 06:00 UTC analog `security-scan.yml`; ggf. bewusst auf einen anderen Tag legen, um die Läufe zu entzerren — vom Kunden nicht spezifiziert.
- **Zeitzoneninterpretation:** Cron läuft in UTC; „1. Januar" und „1. des Monats" fallen damit auf 00:00–06:00 UTC — für die Anforderung als ausreichend angenommen.
- **Assignee statt/zusätzlich zur Erwähnung:** Soll das Issue zusätzlich dem Account `softwarewerkstatt` zugewiesen (`gh issue create --assignee softwarewerkstatt`) oder reicht die `@`-Erwähnung im Body? Annahme: Erwähnung im Body genügt laut Anforderungstext.
- **Verhalten bei manueller Auslösung:** Soll `workflow_dispatch` die Duplikatprüfung ebenfalls berücksichtigen (Annahme: ja, damit auch manuell keine Duplikate entstehen) oder optional über einen Input-Parameter (`force`) übersteuerbar sein?
- **Abgrenzung zum bestehenden `security-scan.yml`:** Der wöchentliche Vulnerability-Scan lädt bereits Artefakte hoch; die neue NuGet-Prüfung erzeugt dagegen Issues für eine Gesamtanalyse — als komplementär und bewusst getrennt angenommen.
- **Reichweite der Erwähnung:** Annahme: `@softwarewerkstatt` ist ein GitHub-Account/Automation-Bot im Repository-Kontext, der auf Erwähnungen reagiert; im Repo selbst ist dafür kein weiterer Mechanismus zu bauen.
