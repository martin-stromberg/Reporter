<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Website — Fehlerbehebung

## Website-Änderung erscheint nicht auf der veröffentlichten Site

**Symptom:** Ein Push mit Änderungen unter `website/` ist erfolgt, aber die Site unter `https://martin-stromberg.github.io/Reporter/` zeigt den alten Stand.

**Ursache:** Der Workflow triggert ausschließlich auf `push` nach `main` — Änderungen auf `staging` oder einem Feature-Branch lösen kein Deployment aus. Die Site geht erst mit dem `staging` → `main`-Promotion-Merge live.

**Lösung:**
1. Prüfen, ob der Workflow-Lauf unter „Actions" → „Deploy Pages" überhaupt gestartet wurde.
2. Falls nicht: Änderung erst per Promotion-PR nach `main` bringen oder den Workflow manuell per `workflow_dispatch` starten (dieser deployed den Stand von `main`).

> **Hinweis:** Der Pfadfilter umfasst nur `website/**` und `.github/workflows/deploy-pages.yml` — ein `main`-Push ohne diese Pfade startet den Workflow ebenfalls nicht.

## Deployment schlägt fehl oder Site liefert 404 auf der Startseite

**Symptom:** Der Workflow-Lauf ist rot, oder die Pages-URL liefert trotz grünem Lauf eine Fehlerseite.

**Ursache:** In den Repository-Einstellungen ist unter „Pages" als Source nicht „GitHub Actions" eingestellt — ohne diese einmalige Einstellung kann `actions/deploy-pages` nicht veröffentlichen.

**Lösung:**
1. Repository-Einstellungen → „Pages" öffnen.
2. Source auf „GitHub Actions" stellen.
3. Workflow erneut per `workflow_dispatch` ausführen.

## Bilder, Stylesheet oder Downloads liefern 404

**Symptom:** Seiten laden ohne Styling, Screenshots oder Press-Kit-Downloads brechen mit 404.

**Ursache:** Das Pages-Artefakt enthält nur den Ordner `website/`. Ein relativer Verweis auf eine Datei außerhalb von `website/` (z. B. direkt auf `docs/help/anwendung/screenshots/...`) ist auf der Site nicht auflösbar — oder die Asset-Kopie fehlt schlicht unter `website/assets/`.

**Lösung:**
1. Prüfen, ob die referenzierte Datei unter `website/assets/` existiert und versioniert ist.
2. Fehlende Dateien als Kopie unter `website/assets/` ablegen (nicht auf Repo-Pfade außerhalb von `website/` verlinken).
3. Lokal verifizieren: `website/` per statischem Server ausliefern und alle Links klicken.

## Datenschutz-Seite oder Pressetexte weichen vom Repository-Stand ab

**Symptom:** `docs/privacy-policy.md`, `README.md` oder `docs/help/anwendung/beschreibung.md` wurden geändert; die Website zeigt noch den alten Text.

**Ursache:** Die HTML-Seiten und Pressetexte sind abgeleitete Kopien — es gibt keinen automatischen Nachlauf (siehe [Business Rules](business-rules.md)).

**Lösung:**
1. Änderungen in `website/privacypolicy.html` bzw. `website/en/privacypolicy.html` bzw. den Pressetexten manuell spiegeln — beide Sprachversionen beachten.
2. Nach dem Merge nach `main` läuft das Deployment automatisch (Pfadfilter trifft `website/**`).

## Static-Checks schlagen auf neuen Website-Dateien fehl

**Symptom:** `scripts/add-license-headers.mjs --check` bzw. der CI-Job `static checks` meldet fehlende Lizenzheader auf Dateien unter `website/` oder `deploy-pages.yml`.

**Ursache:** Jede neue `.html`-, `.svg`- und `.yml`-Datei benötigt den PolyForm-Header (bei `.html` in Zeile 2 hinter `<!DOCTYPE ...>`, bei `.yml` in Zeile 1, bei `.svg` hinter der XML-Deklaration).

**Lösung:**
1. `node scripts/add-license-headers.mjs` ausführen — das Skript ergänzt fehlende Header automatisch.
2. Prüflauf `node scripts/add-license-headers.mjs --check` bzw. `.\scripts\Run-StaticChecks.ps1` wiederholen.
