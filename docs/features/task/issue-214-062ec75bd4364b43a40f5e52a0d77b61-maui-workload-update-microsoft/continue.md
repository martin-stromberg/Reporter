<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Offene Aufgaben

Erstellt am: 2026-11-23
Abbruchgrund: Implementierungsabbruch — externer Blocker (Publish-Lag Workload-Manifest)

## Blocker

Der Plan-Schritt 1 (`sdk.workloadVersion` in `global.json` mit `maui-windows ≥ 11.0.10`) ist technisch nicht umsetzbar: `Microsoft.Maui.Controls 11.0.10` existiert als NuGet-Paket (2026-11-11), aber **kein** Workload-Manifest/Workload-Set liefert `maui-windows ≥ 11.0.10`:

- `Microsoft.NET.Sdk.Maui.Manifest-11.0.100` auf nuget.org: nur `11.0.0`
- `Microsoft.NET.Workloads.11.0.100` (Sets): nur `11.100.0`, `11.100.1` — beide liefern `Microsoft.NET.Sdk.Maui 11.0.0`
- `dotnet workload update` auf der Dev-Maschine (Set-Modus): „kein Update gefunden" — bereits auf Set `11.0.100.1`
- dotnet11-Eng-Feed zeigt `11.0.20-ci.inflight.*` — nächstes stabiles Manifest erwartbar im Servicing-Zyklus

`$(MauiVersion)` wird aus dem installierten `Microsoft.Maui.Sdk`-Pack (lokal `11.0.0`) aufgelöst — ohne Manifest kein `dotnet workload`-Weg zu 11.0.10. Der Befund ist damit ein Publish-Lag zwischen NuGet-Paket und Workload-Manifest (gleiches Muster wie .NET 10: Pakete liefen dem Manifest voraus).

An den Anwender eskaliert per Issue-Kommentar auf #214 (2026-11-23) mit den Optionen:

1. Zurückstellen bis zum Manifest (Befund als bekannten Publish-Lag markieren/akzeptieren)
2. `<MauiVersion>11.0.10</MauiVersion>`-Override (offizieller Hotfix-Weg, aber Version-Mismatch-Risiko Paket 11.0.10 ↔ Workload-Packs 11.0.0; war ursprünglich Scope-Ausschluss 1)
3. Trotzdem `workloadVersion: 11.100.1` pinnen + Diagnose + Doku (friert 11.0.0 ein — schlechter als Floating)

## Noch offene Aufgaben (bei Fortsetzung)

- [ ] Anwender-Entscheidung zum Blocker abwarten (Kommentar auf Issue #214)
- [ ] Je nach Entscheidung Plan anpassen und Implementierung fortsetzen (Umsetzungsschritte 1–5 aus `plan.md`, ggf. re-scoped)
- [ ] Plan-Review, Code-Review, Tests (Schritte 7–10)
- [ ] Dokumentation (Schritte 12–12c), Commit `fix:` + `Closes #214`, PR gegen `staging`
- [ ] Post-Merge-Verifikation Pre-Release-Kette (verbindlicher Anwenderauftrag)

## Bereits verifizierte Fakten (für Fortsetzung)

- `dotnet workload list` nach `dotnet workload update`: `maui-windows 11.0.0/11.0.100`, `ios 27.2.11039`, `maccatalyst 27.2.11039`, `android 37.2.31` — kein iOS-Drift, CI-Pin intakt
- `dotnet list package --outdated` (Reporter.csproj): Befund `11.0.0 → 11.0.10` besteht weiterhin
- Repo-Arbeitsstand: nur Planungscommit `8e1e7c9`, keine Codeänderungen
