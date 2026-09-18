<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Dokumentation — Bestandsaufnahme

Bezug: Anforderung „Artikeldaten getrennt von Nutzerdaten speichern (iCloud-Backup)" (`requirement.md`, Abschnitt „Dokumentation"). Ist-Zustand der genannten Dokumente:

## `docs/app-store-review.md`

- Enthält die Review-Notizen zur App-Store-Einreichung (Privacy-Label-Antworten, Datenschutz-URL, Demo-Account-Hinweise).
- Aktuell **kein** Abschnitt zum iCloud-Backup-Ausschluss; die Aussagen zur Datenspeicherung lauten sinngemäß „alle Inhalte und Einstellungen liegen lokal in SQLite (`ReporterDbContext`)" und „All user content stays on the device (local SQLite storage)".

## `docs/privacy-policy.md`

- Abschnitt „Lokale Datenspeicherung" (Zeilen 26–28, deutsch; 104–106 englisch): beschreibt **eine** lokale SQLite-Datei `reporter.db`, die unter iOS vollständig vom iCloud-Backup ausgeschlossen ist („die Inhalte verlassen dein Gerät dadurch auch nicht indirekt über ein Cloud-Backup").

## `docs/help/anwendung/architektur.md`

- Zeile 49: beschreibt `MauiProgram.CreateMauiApp` inkl. `REPORTER_DB_PATH`, `REPORTER_FEEDSEARCH_ENDPOINT`, `REPORTER_DISABLE_DEMO_SEED`, `FirstRunState` und `DatabasePath` („für den iCloud-Backup-Ausschluss").
- Zeile 70: `DatabasePath` als DI-Träger für den Backup-Ausschluss.
- Zeile 71: `IBackupExclusionService`/`BackupExclusionService` — dokumentiert, dass `App.OnStart` den Ausschluss auf `reporter.db` **und** die `-wal`-/`-shm`-Sidecars anwendet (iOS Data Storage Guidelines).

## `docs/help/anwendung/datenmodell.md`

- Zeile 109: `Reporter.Data.Repositories` implementiert die Interfaces mit EF Core + SQLite (ein Speicher).
- Zeile 119: beschreibt den Erststart-Seed (`FirstRunState`, `DemoContentService`, `REPORTER_DISABLE_DEMO_SEED`).

## `docs/help/anwendung/aufbewahrung.md`

- Zeile 45: Ablaufdiagramm „App-Start → Datenbank-Migration" — beschreibt den Retention-Cleanup beim Start (ein Speicher).

## `docs/help/anwendung/offline.md`

- Beschreibt das Offline-Leseverhalten (lokal gespeicherte Artikel bleiben lesbar; Links/Bilder offline deaktiviert). Keine Aussage zum Speicherort/Backup — die Annahme „Artikel sind lokal vollständig vorhanden" steht implizit dahinter.

## `docs/help/tests/*`

- `docs/help/tests/ablauf-technisch.md` (Zeilen 9, 31, 33, 53, 91): beschreibt E2E-Ablauf, `ReporterAppFixture`, `REPORTER_DB_PATH` auf Temp-Verzeichnis, `FeedDbAssertions` gegen `feeds`/`categories`/`items`.
- `docs/help/tests/api.md` (Zeile 92): Tabelle der Env-Overrides inkl. `REPORTER_DB_PATH` („Vollständiger Dateipfad der SQLite-DB").
- `docs/help/tests/architektur.md` (Zeilen 20, 21, 29, 36, 38, 47, 51): `FeedDbAssertions` liest isolierte `reporter.db` (Mode=ReadOnly); `MauiProgram` wertet die drei Overrides aus.
- `docs/help/tests/business-rules.md` (Zeilen 58, 75): `REPORTER_DB_PATH` nur `IsNullOrWhiteSpace`-Validierung; `REPORTER_DISABLE_DEMO_SEED` nicht an `REPORTER_DB_PATH` koppeln.
- `docs/help/tests/installation.md` (Zeilen 53, 56): `REPORTER_DB_PATH`-Override dokumentiert; Warnung vor dauerhaftem Setzen.
- `docs/help/tests/troubleshooting.md` (Zeile 69): Fehlerbild bei dauerhaft gesetzten Overrides.

## Sonstige Fundstellen

- `docs/RELEASE_NOTES.md`, `README.md`, `changes.log` — keine funktionale Aussage zur Speicheraufteilung erforderlich (Release-/Änderungslisten).
- `AGENTS.md` — enthält projektweite Regeln (Mobile UI Review, `Run-StaticChecks.ps1`); kein Bezug zur Speicherung.
