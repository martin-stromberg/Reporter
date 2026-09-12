← [Zurück zur Übersicht](index.md)

# Release-Management — Beschreibung

## Zweck

Das Release-Management automatisiert den Weg einer Änderung vom Integrationsbranch `staging` bis zum veröffentlichten, stabilen Release auf `main`. Es löst das Problem, dass Versionen, Tags, Release-Artefakte und die Synchronisation der beiden Branches bisher manuell gepflegt werden mussten. Nach dem Branch-Modell `staging` → `main` entstehen Vorabversionen (Release Candidates) und stabile Releases vollautomatisch; Maintainer müssen lediglich die automatisch geöffneten Pull Requests prüfen und mergen.

## Funktionsweise

- **Push auf `staging`:** Der Workflow **Pre-Release** prüft Format, Sicherheit und Tests. Danach wird die nächste Versionsnummer aus den Commit-Botschaften ermittelt und ein GitHub-Pre-Release mit dem Tag `vX.Y.Z-rc.N` angelegt. Das Pre-Release enthält die Pakete `release-win-x64.zip` (Windows) und `release-android.apk` (Android) sowie das Update-Manifest `update.json`. Ein iOS-Paket `release-ios.ipa` kommt hinzu, sobald die Signierung aktiviert ist. Enthält der Push keine releasefähigen Commits, entsteht kein neues Pre-Release.
- **Promotion nach `main`:** Nach jedem erfolgreichen Pre-Release-Lauf öffnet der Workflow **Staging to Main Promotion** automatisch einen Entwurfs-Pull-Request `staging` → `main` mit dem Label `automated-promotion`, sofern `staging` neue Änderungen enthält und noch kein solcher PR offen ist. Ein Maintainer prüft und mergt diesen PR manuell.
- **Push auf `main`:** Der Workflow **Release** erzeugt das stabile Release `vX.Y.Z` mit denselben Paketen und dem Update-Manifest. Ein manuell gepushter Tag `vX.Y.Z` löst denselben Ablauf aus. Existiert das Release bereits, aber ohne vollständige Pakete, werden die fehlenden Dateien nachgeladen statt ein neues Release anzulegen.
- **Backmerge nach `staging`:** Nach jedem Push auf `main` öffnet der Workflow **Backmerge Main to Staging** bei Bedarf einen Pull Request `main` → `staging` mit dem Label `automated-backmerge`, damit `staging` nicht zurückfällt. Dieser PR muss per „Create a merge commit" gemergt werden.
- **Schutzregeln:** Pull Requests gegen `staging` durchlaufen im Workflow **PR CI for Staging** dieselben Prüfungen. Reine Backmerge-PRs werden erkannt und überspringen die Prüfungen. Pull Requests nach `main` werden vom Workflow **Verify PR Source** abgelehnt, wenn sie nicht aus `staging` stammen.

## Beispiele

- Ein Bugfix-Commit (`fix: …`) landet per PR auf `staging` → es entsteht z. B. das Pre-Release `v1.4.1-rc.1` und ein Entwurfs-PR nach `main`. Nach dem Merge des Promotion-PRs erscheint das stabile Release `v1.4.1`, anschließend öffnet sich der Backmerge-PR.
- Ein erneuter Push auf `staging` vor dem Merge erzeugt `v1.4.1-rc.2` — die laufende Nummer zählt pro Zielversion weiter.
- Ein Release, dessen Paket-Upload abgebrochen ist, wird beim nächsten `main`-Lauf automatisch repariert: Die fehlenden Dateien werden dem bestehenden Release hinzugefügt.

## Einschränkungen

- Versionen entstehen ausschließlich aus Conventional-Commits-Botschaften; ohne `fix:`/`feat:`/`BREAKING CHANGE` gibt es kein neues Release.
- Das iOS-Paket ist Bestandteil der Pipeline, bleibt aber deaktiviert, bis die Signierungs-Secrets bereitstehen und die Repository-Variable `IOS_SIGNING_ENABLED` gesetzt ist (siehe [Installation & Konfiguration](installation.md)).
- Branch-Protection-Regeln für `main` und `staging` sind vorgesehen, konnten aber im privaten Repository auf dem Free-Plan nicht per API eingerichtet werden (HTTP 403) — die Einrichtung ist dokumentierte Admin-Nacharbeit. Bis dahin ist **Verify PR Source** die einzige automatisierte Absicherung der `main`-Branch-Regel.
- Der allererste Promotion-PR muss einmalig manuell geöffnet werden, weil die Automatisierung erst greift, nachdem die Workflow-Dateien auf dem Standardbranch liegen (Details siehe [Fehlerbehebung](troubleshooting.md)).
