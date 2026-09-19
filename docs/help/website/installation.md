<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Website — Installation und Konfiguration

## Voraussetzungen

- Die Workflow-Datei `.github/workflows/deploy-pages.yml` und der Ordner `website/` liegen auf dem `main`-Branch (über den regulären `staging` → `main`-Promotion-Merge).
- Der ausführende Account hat Maintainer-Rechte am Repository `martin-stromberg/Reporter`.
- Für die lokale Vorschau genügt ein Browser — optional ein beliebiger statischer Dateiserver (z. B. `npx serve website`).

## Installationsschritte

1. **Einmalig — Repository-Einstellung:** In den Repository-Einstellungen unter „Pages" die Source auf **„GitHub Actions"** stellen. Ohne diesen Schritt kann der Workflow nicht veröffentlichen. Der Schritt ist nicht im Code versionierbar und muss vom Maintainer in der GitHub-Oberfläche erfolgen.
2. **Deployment auslösen:** Ein Push auf `main`, der Dateien unter `website/` oder `.github/workflows/deploy-pages.yml` ändert, startet den Workflow automatisch. Alternativ lässt er sich unter „Actions" → „Deploy Pages" manuell per `workflow_dispatch` starten.
3. **Ergebnis prüfen:** Nach grünem Lauf ist die Site unter `https://martin-stromberg.github.io/Reporter/` erreichbar; die konkrete URL steht auch in der Umgebung `github-pages`.

## Konfiguration

| Parameter | Typ | Standardwert | Beschreibung |
|-----------|-----|--------------|--------------|
| GitHub Pages „Source" | Repository-Einstellung (nicht im Code) | — | Muss einmalig auf „GitHub Actions" stehen, damit `deploy-pages.yml` veröffentlichen kann. |
| Workflow-`permissions` | `deploy-pages.yml` | `contents: read`, `pages: write`, `id-token: write` | Technische Voraussetzung des Pages-Actions-Stacks. |
| `concurrency`-Gruppe `pages` | `deploy-pages.yml` | `cancel-in-progress: false` | Serialisiert Deployments; laufende Deployments werden nicht abgebrochen. |
| Artefakt-Pfad | `deploy-pages.yml` | `website/` | `actions/upload-pages-artifact` packt ausschließlich diesen Ordner — nur sein Inhalt wird öffentlich. |
| Trigger-Pfadfilter | `deploy-pages.yml` | `website/**`, `.github/workflows/deploy-pages.yml` auf `main` | Änderungen außerhalb dieser Pfade lösen kein Deployment aus. |

## Umgebungsvariablen

Keine — der Workflow liest keine Umgebungsvariablen oder Secrets; die Authentifizierung gegenüber GitHub Pages erfolgt über `id-token: write` (OIDC).

## Lokale Vorschau

Die Site ist statisch und verwendet ausschließlich relative Links — für eine Vorschau genügt es, `website/index.html` direkt im Browser zu öffnen oder den Ordner über einen statischen Server auszuliefern (z. B. `npx serve website`). Sichtprüfung beider Sprachversionen in mobilem und Desktop-Viewport.

## Überprüfung

1. Workflow-Lauf unter „Actions" → „Deploy Pages" ist grün.
2. `https://martin-stromberg.github.io/Reporter/` liefert die Landing-Page; der Sprachumschalter führt zur englischen Version.
3. Alle vier Seiten pro Sprache sind erreichbar; Bilder, GIF und Download-Links des Press Kits laden ohne 404.
4. `node scripts/add-license-headers.mjs --check` bzw. `.\scripts\Run-StaticChecks.ps1` läuft ohne Befund — alle `.html`-, `.svg`- und `.yml`-Dateien der Site tragen den PolyForm-Lizenzheader.
