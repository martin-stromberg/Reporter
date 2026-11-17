<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderungsbeschreibung – NuGet-Paketprüfung KW 2026-W47 (Issue #179)

## Fachliche Zusammenfassung

Es ist eine vollständige technische Bestandsaufnahme und Bewertung aller in `Reporter.sln` referenzierten NuGet-Pakete durchzuführen. Geprüft werden: verfügbare Updates, Security-Updates, Pakete mit bekannten Schwachstellen, veraltete bzw. nicht mehr gepflegte Pakete, empfohlene Nachfolgepakete sowie mögliche Breaking Changes bei Versionserhöhungen. Das Ergebnis ist ein konkreter Maßnahmenkatalog mit Risikobewertung je Empfehlung. Empfehlungen mit niedrigem bis mittlerem Risiko (v. a. einfache Versionshochsetzungen) werden direkt in einem Pull Request umgesetzt; risikoreiche oder aufgeschobene Empfehlungen werden im PR-Body dokumentiert bzw. als separate Folge-Issues angelegt und im Issue #179 verlinkt. Die Aufgabe stammt aus dem wiederkehrenden Review-Workflow `.github/workflows/review-nuget.yml` (Label `scheduled-nuget-review`, eingeführt mit Issue #165).

## Betroffene Klassen und Komponenten

Keine neuen fachlichen Klassen, Services, Interfaces, Enums oder UI-Komponenten – es handelt sich um eine Wartungs-/Audit-Aufgabe ohne Funktionsänderung. Betroffen sind die Projektdateien mit ihren `PackageReference`-Einträgen (keine zentrale Paketverwaltung, kein `Directory.Packages.props` vorhanden; Versionen stehen inline in den `.csproj`-Dateien):

- `src/Reporter/Reporter.csproj` (MAUI-App, `net10.0-windows10.0.19041.0` [+ `net10.0-ios`/`net10.0-android` über `IncludeIosTarget`/`IncludeAndroidTarget`]):
  - `Microsoft.Maui.Controls` `$(MauiVersion)` (Version aus dem installierten MAUI-Workload/SDK, nicht inline gepinnt)
  - `Microsoft.Extensions.Logging.Debug` `10.0.0`
  - `CommunityToolkit.Mvvm` `8.4.2`
- `src/Reporter.Core/Reporter.Core.csproj` (`net10.0`):
  - `CommunityToolkit.Mvvm` `8.4.2`
  - `System.ServiceModel.Syndication` `10.0.11`
- `src/Reporter.Data/Reporter.Data.csproj` (`net10.0`):
  - `Microsoft.EntityFrameworkCore.Design` `10.0.12`
  - `Microsoft.EntityFrameworkCore.Sqlite` `10.0.12`
- `src/Reporter.Tests/Reporter.Tests.csproj` (`net10.0`, Test-Stack auf `Microsoft.Testing.Platform`/`xunit.v3`, vgl. Issue #181):
  - `coverlet.MTP` `10.1.1`
  - `Microsoft.EntityFrameworkCore.Sqlite` `10.0.12`
  - `Microsoft.Extensions.TimeProvider.Testing` `10.1.0`
  - `Microsoft.NET.Test.Sdk` `18.12.0`
  - `Microsoft.Testing.Extensions.TrxReport` `2.5.3`
  - `xunit.v3` `4.0.3`
  - `xunit.runner.visualstudio` `4.0.1`
- `src/Reporter.E2ETests/Reporter.E2ETests.csproj` (`net10.0-windows10.0.19041.0`):
  - `FlaUI.UIA3` `5.0.0`
  - `Microsoft.Data.Sqlite` `10.0.12`
  - `Microsoft.NET.Test.Sdk` `18.12.0`
  - `xunit.v3` `4.0.3`
  - `xunit.runner.visualstudio` `4.0.1`

Weitere betroffene Artefakte:

- Transitive Pakete (nur relevant bei Schwachstellen/Deprecation-Befunden)
- `.github/dependabot.yml` (wöchentliche NuGet-Updates gegen `staging` – Abgleich mit offenen Dependabot-PRs nötig, um Doppelarbeit/Konflikte zu vermeiden)
- Pull Request gegen `staging` (Staging-Workflow: Feature-Branch → `staging` → `main`) mit Risikobewertung und Liste aufgeschobener Empfehlungen im PR-Body
- Ggf. neue GitHub-Folge-Issues für risikoreiche Befunde, verlinkt in Issue #179
- Tests: `Reporter.Tests` (Unit-/Integrationstests via `xunit.v3`) und `Reporter.E2ETests` (`FlaUI.UIA3`) als Nachweis, dass umgesetzte Updates nichts brechen; `scripts/Run-StaticChecks.ps1` als lokaler CI-Gate-Check

## Implementierungsansatz

1. **Inventur:** `dotnet list Reporter.sln package --outdated`, `dotnet list Reporter.sln package --vulnerable --include-transitive` und `dotnet list Reporter.sln package --deprecated --include-transitive` ausführen. `$(MauiVersion)` separat aus dem installierten Workload ermitteln (`dotnet workload list` bzw. NuGet-Metadaten), da diese Version nicht in der `.csproj` steht.
2. **Bewertung:** Je Paket Release Notes / NuGet.org-Lebenszyklus (Deprecated-Flag, letztes Release-Datum, offizielles Nachfolgepaket) sowie bekannte Breaking Changes (Major-Versionssprünge, .NET-10-/MAUI-Migrationshinweise) prüfen und ein Risiko (niedrig/mittel/hoch) vergeben. Heuristik: Patch/Minor innerhalb derselben Major = niedrig; Major-Sprünge und Test-Stack-Änderungen mit Verhalten-API-Brüchen = mittel bis hoch.
3. **Umsetzung:** Empfehlungen mit niedrigem bis mittlerem Risiko direkt als Versionsanpassungen in den `.csproj`-Dateien umsetzen, `dotnet restore` + Build für alle relevanten TFMs, Unit-Tests (`Reporter.Tests`) und `.\scripts\Run-StaticChecks.ps1` (muss Exit-Code 0 liefern) ausführen. Keine UI-Änderung → die Mobile-UI-Design-Review-Regel greift nicht.
4. **Dokumentation:** Im PR-Body eine Tabelle aller geprüften Pakete mit Befund, Empfehlung und Risikobewertung. Risikoreiche/aufgeschobene Punkte entweder nur im PR-Body dokumentieren oder – bei komplexen Befunden – als eigene Folge-Issues anlegen und in Issue #179 verlinken (entsprechend dem Original-Text des `review-nuget.yml`-Workflows).
5. **Abschluss:** Commit-Message mit Closing-Keyword (`Closes #179`), sofern der PR die Aufgabe abschließt und keine Folge-Issues offen bleiben; andernfalls nur Referenzierung, damit das Issue nicht vorschnell geschlossen wird. Schließung erfolgt ohnehin erst bei Promotion `staging` → `main`.

## Konfiguration

Kein konfigurierbares Verhalten – die Aufgabe ändert kein Laufzeitverhalten der Anwendung. Paketversionen werden dezentral je `.csproj` gepflegt; eine Einführung von Central Package Management (`Directory.Packages.props`) wäre eine eigenständige Architekturentscheidung und ist nicht Teil dieser Anforderung (allenfalls als Folge-Empfehlung dokumentierbar).

## Offene Fragen

- **Pre-Release-Versionen:** Sollen nur stabile Paketversionen berücksichtigt werden oder auch Previews/RCs? Annahme: nur stabile Versionen.
- **Transitive Pakete:** Sollen verwundbare/veraltete transitive Pakete aktiv über eine direkte `PackageReference` hochgepinnt werden oder nur dokumentiert werden? Annahme: aktives Pinnen nur bei tatsächlichen Schwachstellen, sonst Dokumentation.
- **Abstimmung mit Dependabot:** `.github/dependabot.yml` erzeugt bereits wöchentlich NuGet-Update-PRs gegen `staging`. Es ist zu klären, ob vorhandene offene Dependabot-PRs für dieselben Pakete geschlossen/zusammengeführt oder parallel belassen werden sollen.
- **`$(MauiVersion)`/Workload:** Ein Update von `Microsoft.Maui.Controls` impliziert ggf. ein MAUI-Workload-Update auf Entwickler- und CI-Maschinen (Android/iOS-Builds). Ist das im Rahmen dieser Prüfung umzusetzen oder als Folge-Issue aufzuschieben? Annahme: aufschieben und dokumentieren, da Build-Umgebungsabhängigkeit.
- **Test-Stack-Updates:** `xunit.v3`, `Microsoft.Testing.Platform`/`Microsoft.Testing.Extensions.TrxReport` und `coverlet.MTP` sind thematisch mit Issue #181 („Test-Stack modernisieren") verwandt – größere Test-Stack-Major-Updates ggf. dort koordinieren statt doppelt umzusetzen.
