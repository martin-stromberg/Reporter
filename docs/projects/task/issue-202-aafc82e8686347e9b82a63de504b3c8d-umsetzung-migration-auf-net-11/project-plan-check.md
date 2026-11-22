<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Planprüfung – Projekt .NET-11-Migration

## Ergebnis

**Status:** Projektplan vollständig

Jeder Task des Migrationsplans (1.1–5.9) ist einem Entwicklungsschritt,
einem Akzeptanzkriterium oder einem dokumentierten Restpunkt zugeordnet.
Lücken aus Durchlauf 1 (fehlende Zuordnung 5.1/2.4/5.2, veraltete
Zeilen-Enumerationen) und Durchlauf 2 (Task 5.8) sind geschlossen.

## Abgleich Anforderung ↔ Entwicklungsschritte

| Plan-Task | Schritt | Status | Kommentar |
|---|---|---|---|
| 0.1–0.4 | — | ja | Organisatorisch erledigt; keine CI-Trigger-Erweiterung (Nicht-Ziel). |
| 1.1–1.3, 1.5–1.7 | 1 | ja | TFM-Listen, UseInterpreter, Lib-TFMs, harte Strings, global.json — verifiziert. |
| 1.4 | 1 (+3) | ja | E2E-TFM; FrameworkReference via Task 4.5. |
| 1.8 | 3 | ja | MauiXamlInflator-Override bleibt (konservativ, dokumentiert). |
| 2.1–2.3 | 1 | ja | MauiVersion unverändert; Paketversionen konkret benannt und verifiziert. |
| 2.4 | 1 | ja | PRs #196/#200 CLOSED; dependabot.yml bleibt. |
| 2.5 | 1 + 3 | ja | .config/dotnet-tools.json neu; ReportGenerator via PR-Check. |
| 2.6 | 1 | ja | Test-Stack-Kompatibilität als Teil des Schritts. |
| 3.1–3.3, 3.5 | 2 | ja | Re-Grep, MigrateAsync/PRAGMA, EnsureCreated-vs-Migrate, v0.7.0-DB-Upgrade. |
| 3.4 | 2 | ja | Inhaltliche Reverifikation; Gerätetest = Restpunkt. |
| 3.6 | 2 + 3 | ja | Coverage-Ausschluss bleibt unverändert (verifiziert). |
| 4.1–4.2 | 3 | ja | 10× '10.0.x' verifiziert; package-ios-Pin-Trio auf GA 27.2.11039/11.0.1xx. |
| 4.3–4.5 | 3 | ja | workload restore/Coverage bleiben; Shared-Framework via setup-dotnet. |
| 4.6–4.7 | 3 (+1 AK) | ja | ci-instructions.md mitgezogen inkl. Z. ~1018; MTP unter .NET-11-SDK. |
| 5.1–5.3 | 1 (AK) + 4 | ja | StaticChecks Exit 0 + dotnet test + FlaUI-Versuch dokumentiert. |
| 5.4, 5.8 | Offene Punkte | ja | Anwender-Restpunkte dokumentiert. |
| 5.5 | 2 | ja | DB-Upgrade-Nachweis im Test-Artefakt. |
| 5.6–5.7, 5.9 | 4 | ja | Doku-Liste vollständig; Status-Hebung inkl. Zeilen-Korrektur. |

## Abhängigkeitsprüfung

Schritt 1 keine; 2 → 1; 3 → 1; 4 → 1, 2, 3. Nur Rückwärtsverweise,
zyklenfrei, fachlich korrekt.

## Fehlende oder unvollständige Punkte

Keine.

## Hinweise

- `workload restore` kommt je Workflow 3× vor (staging-ci Z. 191,
  pr-staging-ci Z. 194) — Plan nennt nur 2; beim Status-Heben des
  Migrationsdokuments mit korrigieren.
- Schluss-Grep über `docs/` liefert legitime historische Treffer
  (alte Feature-Artefakte, Projektartefakte, IST-Spalten des
  Migrationsdokuments) — als bewusst historisch begründen.
- Nicht repo-verifizierbare Behauptungen (PR-Status #196/#200,
  iOS-Manifest 27.2.11039 als GA, setup-dotnet-Live-Auflösung)
  werden über den PR-Check verifiziert.
