<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestand: Dokumentationsstruktur und Ablageorte

Detaildokument zur [Bestandsaufnahme](../inventory.md) — wo und wie Infrastruktur-
Themen dokumentiert werden; relevant für das Akzeptanzkriterium „vollständig
innerhalb des Repositories dokumentiert".

## Struktur von `docs/help/`

- Themenverzeichnisse mit je einer `index.md` (Übersicht + Inhaltsliste) und
  einzelnen Inhaltsseiten. Bestehende Bereiche:
  `anwendung/`, `benachrichtigungen/`, `einstellungen/`, `entwicklung/`,
  `ios-deployment/`, `release-management/`, `tests/`, `website/` sowie die
  Dachdatei `docs/help/index.md`.
- `docs/help/index.md` gruppiert die Bereiche in **Anwendung**, **Konfiguration**,
  **Systemverwaltung** und **Entwicklung** — neue Bereiche müssen dort verlinkt
  werden, damit sie in der Übersicht erscheinen.
- Seitenkonvention (Beispiel `release-management/beschreibung.md`): Lizenzheader
  als XML-Kommentar, dann `← [Zurück zur Übersicht](index.md)`, dann
  `# <Bereich> — <Seitentitel>`; inhaltliche Gliederung u. a. mit „Zweck",
  „Funktionsweise", „Beispiele", „Einschränkungen".
- Sprache der Doku ist durchgehend **Deutsch** (Ausnahme: `ci-instructions.md`
  im Root ist Englisch und diente als Entwurfsvorlage für die Pipeline).

## Passfaehige Ablageorte (Bestand)

| Option | Begründung im Bestand |
|--------|------------------------|
| `docs/help/release-management/` | Dokumentiert die komplette Automatisierungs-Pipeline inkl. der `gh`-CLI-/Label-Workflows (`ablauf-technisch.md:51-104`, `installation.md:18,24-33`). Die drei neuen Review-Workflows sind derselbe Automatisierungstyp (zeitgesteuerte Repo-Pflege). `architektur.md` enthält eine Komponenten-Tabelle aller Workflows — neue Workflows müssten dort nachgetragen werden. |
| `docs/help/entwicklung/` | Bisher nur `git-hooks.md` (lokale Qualitätssicherung für Beitragende). Eher für Contributor-Themen; die Review-Automatisierung ist Betrieb, nicht lokale Entwicklung. |
| Eigener Bereich unter `docs/help/` (z. B. `automatisierung/`) | Möglich, aber kleiner Bereich — die Bereiche `benachrichtigungen/` o. ä. zeigen, dass auch schmale Themen eigene Verzeichnisse mit `index.md` bekommen. |

`requirement.md` selbst nennt `docs/help/release-management/` oder
`docs/help/entwicklung/` als Kandidaten; die Entscheidung ist dem Planungsschritt
vorbehalten.

## Verlinkungs-Pflichten (Bestand)

- `docs/help/index.md` — neuer Eintrag unter der passenden Gruppe
  (voraussichtlich „Systemverwaltung" oder „Entwicklung").
- `README.md` — Abschnitt „Weitere Informationen → Betrieb und Entwicklung"
  (Zeilen 106–114) verlinkt alle `docs/help`-Bereiche; Abschnitt „CI/CD"
  (Zeile 83) beschreibt die Pipeline und verweist auf Release-Management.
- `docs/help/release-management/index.md` / `architektur.md` — falls die Seite
  dort einsortiert wird, gehört sie in die Inhaltsliste bzw. Komponententabelle.
- `docs/help/release-management/installation.md` — Abschnitt „Workflow-Permissions"
  (Zeilen 26–31) listet die Permissions aller Workflows als Tabelle; neue
  Workflows mit `issues: write` wären dort konsistent nachzutragen, wenn die
  Doku in diesem Bereich landet.

## Verwandte Referenzdokumente

- `ci-instructions.md` (Root, Englisch): Entwurfsvorlage der gesamten Pipeline;
  beschreibt das Label-/Dedup-Muster in den Schritten 8/9 (Zeilen ~880–950) und
  dokumentiert die beiden Automatisierungs-Labels als Voraussetzung (Zeile 39).
- `issue.md` (Root): Original-Issue-Text #165 mit den wörtlichen Body-Texten
  (Encoding teils defekt, `requirement.md` enthält die bereinigte Übersetzung).
- `AGENTS.md`: Projektkonventionen — Mobile-UI-Regeln (für diese Anforderung ohne
  UI-Bezug nicht einschlägig) und Pflicht zu `scripts/Run-StaticChecks.ps1`.
- `CONTRIBUTING.md` / `.github/pull_request_template.md`: PR-Prozess und lokale
  Prüfpflichten.
