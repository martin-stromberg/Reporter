<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Website — Business Rules

## Datenschutz: Repository-Datei bleibt Single Source of Truth

**Beschreibung:** Die Datenschutzerklärung existiert zweimal — als kanonische Markdown-Datei `docs/privacy-policy.md` (in App Store Connect als Datenschutz-URL hinterlegt) und als HTML-Spiegel `website/privacypolicy.html` bzw. `website/en/privacypolicy.html` auf der Website.

**Bedingungen:**
- Die Website-Seiten sind abgeleitete Kopien, kein Ersatz — die App-Store-Connect-URL bleibt die GitHub-Blob-URL von `docs/privacy-policy.md` auf dem Default-Branch.
- `website/privacypolicy.html` rendert den deutschen Haupttext, `website/en/privacypolicy.html` die englische Zusammenfassung.

**Verhalten:**
- Wird `docs/privacy-policy.md` geändert, müssen beide HTML-Spiegel manuell nachgezogen werden (Content-Drift).
- Jede HTML-Seite verweist im Text zusätzlich auf die kanonische Repo-Datei.

**Umsetzung:** `website/privacypolicy.html`, `website/en/privacypolicy.html` (Verweis-Zeilen auf `docs/privacy-policy.md` im Einleitungs- und Schlussabschnitt).

## Nur `website/` wird veröffentlicht

**Beschreibung:** Das Pages-Artefakt enthält ausschließlich den Ordner `website/` — interne Projektdokumentation (`docs/features/`, `docs/help/`, Arbeitsnotizen) wird nicht publiziert.

**Bedingungen:**
- Alle von der Site referenzierten Assets müssen physische Kopien unter `website/assets/` sein.
- Links auf Repository-Dateien außerhalb von `website/` (z. B. `changes.log`, `LICENSE`) erfolgen als externe GitHub-URLs, nicht als relative Links.

**Verhalten:**
- Referenziert eine Seite eine Datei außerhalb von `website/` relativ, ergibt das auf der veröffentlichten Site einen 404.
- Neue Screenshots oder Logos müssen als Kopie nach `website/assets/` gelegt und versioniert werden.

**Umsetzung:** `actions/upload-pages-artifact` mit `path: website/` in `.github/workflows/deploy-pages.yml`.

## Sprachparität und relative Links

**Beschreibung:** Deutsch ist die Default-Sprache im Root von `website/`; die englische Version spiegelt die Struktur unter `website/en/`. Alle internen Links sind relativ.

**Bedingungen:**
- Jede neue oder geänderte Seite muss in beiden Sprachen existieren; der Sprachumschalter im Kopfbereich verlinkt auf das jeweilige Pendant.
- Keine absoluten Domain-Links innerhalb der Site, kein `baseurl` — so bleibt die Site domain-unabhängig und lokal ohne Server-Konfiguration lauffähig.

**Verhalten:**
- Fehlt eine Sprachversion, führt der Sprachumschalter ins Leere.
- Absolute interne Links würden in der lokalen Vorschau und bei einem späteren Domain-Wechsel brechen.

**Umsetzung:** Seitenpaare `website/*.html` ↔ `website/en/*.html`; `lang-switch`-Block im `<header>` jeder Seite.

## Lizenzheader auf allen Site-Quellen

**Beschreibung:** Alle `.html`-, `.svg`- und `.yml`-Dateien der Website müssen den PolyForm-Lizenzheader tragen — der CI-Job `static checks` (`scripts/add-license-headers.mjs --check`) lässt den Build sonst fehlschlagen.

**Bedingungen:**
- Bei `.html` steht der Kommentar in Zeile 2 hinter `<!DOCTYPE ...>`; bei `.yml` als `#`-Kommentar in Zeile 1; bei `.svg` hinter der XML-Deklaration.
- `.css`, `.png` und `.gif` sind vom Check ausgenommen.

**Verhalten:**
- Neue Site-Datei ohne Header → roter Static-Checks-Lauf.
- Header werden automatisiert per `node scripts/add-license-headers.mjs` ergänzt.

**Umsetzung:** `scripts/add-license-headers.mjs`, CI-Job `static checks`.

## Veraltbare Kopien: Screenshots und Pressetexte

**Beschreibung:** Screenshots, Demo-GIF und die Kurz-/Langtexte des Press Kits sind Momentaufnahmen bzw. abgeleitete Kopien aus `README.md` und `docs/help/anwendung/beschreibung.md`.

**Bedingungen:**
- Bei UI-Änderungen der App veralten die kopierten PNGs und das GIF.
- Bei Änderungen an App-Positionierung oder Feature-Liste veralten die Pressetexte und die Feature-Liste der Landing-Page.

**Verhalten:**
- Kein automatischer Nachlauf — Aktualisierung erfolgt manuell durch neue Kopien in `website/assets/` bzw. angepasste HTML-Texte.

**Umsetzung:** `website/assets/img/screenshots/`, `website/assets/img/app-demo-ios.gif`, Textabschnitte in `website/index.html` und `website/press.html`.
