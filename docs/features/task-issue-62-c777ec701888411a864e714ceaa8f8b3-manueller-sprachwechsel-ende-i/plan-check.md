<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Sprachwahl **System / Deutsch / English** in den Einstellungen wählbar | `SettingsViewModel`: `LanguageOptions` (Konstruktor, Reihenfolge System/Deutsch/English, lokalisierte Labels) + `SelectedLanguage` mit `PersistOnChange()`; `SettingsPage.xaml`: eigene Sektion „Sprache" mit Picker exakt nach Theme-Picker-Muster (Schritte 7, 8, 11; Designentscheidung „Platzierung") | `LanguageOptions_ExposePersistedValues` (Werte `["system","de","en"]`), `Load_PopulatesSelectedLanguage`, `E2E_ChangeLanguage_PersistRoundtrip`; sichtbare UI-Wirkung via dokumentierter manueller Verifikation (Schritt 16) | Abgedeckt |
| Persistierung als Feld `Settings.Language` (Entity + Domain-Modell, Default `"system"`, analog `Theme`) | Schritte 1–3: `SettingsValues.Language*`-Konstanten, `Language` in `Reporter.Data.Entities.Settings` und `Reporter.Core.Models.Settings` | Implizit über Roundtrip-Tests (`SaveAsync_PersistsLanguage`, E2E) | Abgedeckt |
| Spalte `language` (TEXT, `HasMaxLength`, nullable) via Migration `AddSettingsLanguage` inkl. `UpdateData`/Seed | Schritte 3–4: `ConfigureSettings`-Mapping + `dotnet ef migrations add AddSettingsLanguage`; Migrations-Tabelle mit Voraussetzungen (`dotnet-ef` 10.0.10, `ReporterDbContextFactory`, `Microsoft.EntityFrameworkCore.Design`) | Schema-Tests (`ReporterDbContextTests`) erfassen Spalte automatisch über das Modell; Inhaltlicher Nachweis via `SaveAsync_PersistsLanguage` | Abgedeckt |
| `SettingsRepository` mappt `Language` in `SaveAsync` und `MapToModel` | Schritt 5 mit Zeilenreferenzen (55–64, 71–84) | `SaveAsync_PersistsLanguage` (Roundtrip `SaveAsync`→`GetAsync`) | Abgedeckt |
| `LanguageOption`-Klasse (`Value` + `Label`, analog `ThemeOption`) | Schritt 7, Tabelle „Neue Klassen" | Indirekt über `LanguageOptions_ExposePersistedValues` | Abgedeckt |
| `SelectedLanguage` persistiert sofort; `Language` geht bei anderen Einstellungsänderungen nicht verloren | Schritt 8: `PersistOnChange()`-Muster + Pflicht-Ergänzung in `PersistAsync` (Zeilen 645–658); Risiko „Settings-Verlust-Gefahr" explizit benannt | `SelectedLanguage_Change_Persists` (via `TestWaitHelper.WaitUntilAsync`), `OtherChange_DoesNotLoseLanguage` | Abgedeckt |
| Neustart-Hinweis in der UI (keine Laufzeit-Umschaltung) | Schritt 11: statischer Info-`Border` `SettingsLanguageRestartHint` nach `SettingsRetentionInfo`-Muster; Designentscheidung „Neustart-Hinweis" + „Kein Service-Aufruf bei Sprachwechsel" | Manuelle UI-Verifikation (Sichtbarkeit des Hinweises, Schritt 16) | Abgedeckt |
| Kultur-Anwendung beim App-Start auf `CurrentUICulture` (vor `CreateWindow` wirksam) | Schritte 9–10: `AppCulture` (`ResolveCulture`/`Apply`, setzt `CurrentUICulture`, `CurrentCulture`, `DefaultThreadCurrent*`) + `ApplyPersistedLanguage(app)` in `MauiProgram.CreateMauiApp` nach `builder.Build()` — begründet über Inventory-Korrektur (`AppShell` instanziiert Pages/Tab-Titel eager in `CreateWindow`, vor `OnStart`) | `AppCultureTests.ResolveCulture_ReturnsExpected` (Theory inkl. `null`/`"fr"`/`""`→`null`), optional `Apply_SetsCultures` mit Kultur-Restore; Startpfad-Wirkung via manueller Verifikation (Neustart → Texte in gewählter Sprache) | Abgedeckt |
| Fallback: unbekannte/`null`-Werte → `system` | `LoadAsync`-Options-Fallback + `ResolveCulture` liefert `null` → `Apply` No-Op; Validierungstabelle + Designentscheidung „System-Verhalten" | `Load_InvalidLanguage_UsesSystemFallback`, `ResolveCulture_ReturnsExpected` | Abgedeckt |
| Neue `AppResources`-Schlüssel (en/de) + `AppResources.Designer.cs` | Schritt 6: sechs Schlüssel mit Übersetzungen; Designer-Regenerierung/manuelle Ergänzung als Offener Punkt 1 (vom Anwender bestätigt) | Kompilierung + manuelle UI-Verifikation | Abgedeckt |
| `TestSettingsHelper` um `language`-Parameter/Feld-Copy erweitern | Schritt 12; in „Betroffene bestehende Tests" mit Begründung | Hilfsmethoden-Voraussetzung für alle neuen Tests | Abgedeckt |
| Doku `docs/help/anwendung/sprache.md` aktualisieren | Schritt 16 (Doku-Punkt) + Risikoeintrag „Hilfedokumentation" | — | Abgedeckt |
| Offene Fragen der Anforderung (1–5) entschieden | Alle entschieden und begründet: beide Kulturen setzen (`CurrentUICulture`+`CurrentCulture`+Thread-Defaults); Zeitpunkt `MauiProgram` vor `CreateWindow` (statt `OnStart`); statischer Hinweis; eigene Sektion; Werte `system`/`de`/`en` | — | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Sprache im Picker wählen → sofort persistiert → nach „Neustart" (neue ViewModel-Instanz + `LoadCommand`) wieder selektiert | `SettingsViewModelTests_E2E.E2E_ChangeLanguage_PersistRoundtrip` (echte `SettingsRepository`-Instanz auf Shared-In-Memory-SQLite via `TestDbContextFactory`, Reload simuliert Neustart — Muster `E2E_ChangeSettings_PersistRoundtrip`) | Abgedeckt |
| Sprache „Deutsch"/„English" wählen → App neu starten → Tab-Titel, Einstellungstexte, Picker-Labels in gewählter Sprache; `system` folgt Gerätesprache; Neustart-Hinweis sichtbar | Kein UI-Testframework im Repo (nachvollziehbar im Plan begründet, Offener Punkt 2 — vom Anwender bestätigt). Stattdessen AGENTS.md-Pflicht konkret verankert: dokumentierte manuelle UI-Verifikation in Schritt 16 (Windows-Handysize 390×844 aus `App.CreateWindow`, Light+Dark via `AppThemeBinding`, Design-Vergleich `design-draft/stitch_local_rss_feed_reader/einstellungen_filter/screen.png`, Screenshot + Fenstergröße in `test-results.md`/`docs/help/anwendung/mobile-ui-design.md`, Aktualisierung des offenen Checklistenpunkts `test-results.md` Zeile 538) | Abgedeckt (manuelle Verifikation, AGENTS.md-konform) |
| Unbekannter persistierter `language`-Wert → UI zeigt „System", kein Crash, Kultur bleibt Systemkultur | `Load_InvalidLanguage_UsesSystemFallback` + `AppCultureTests.ResolveCulture_ReturnsExpected` (`"fr"`→`null` → No-Op); über geschlossenen Picker nicht per UI auslösbar — Service-/Logikebene korrekt abgedeckt | Abgedeckt |
| Fehler im Startpfad (Migration/Repository-Zugriff schlägt fehl) darf App-Start nicht verhindern | Try/catch-Schutz um `ApplyPersistedLanguage` im Plan beschrieben (Designentscheidung + Programmablauf); Fallback = bisheriges Systemverhalten | Abgedeckt |

## Hinweise

- Die Platzierung der Kultur-Anwendung in `MauiProgram.CreateMauiApp` (vor `CreateWindow`) geht über die Anforderung hinaus (`OnStart` wäre zu spät, da `AppShell` Pages und Tab-Titel eager erzeugt) — korrekt aus der Inventory-Korrektur abgeleitet und sauber begründet, inkl. synchronem `Database.Migrate()` für den Upgrade-Fall (erste Spaltenexistenz).
- Beide offenen Punkte des Plans wurden vom Anwender bestätigt: manuelle Ergänzung der `AppResources.Designer.cs`-Properties (kein CLI-Generierungsweg im Repo) sowie dokumentierte manuelle UI-Verifikation statt UI-Testframework plus E2E-Roundtrip auf ViewModel-Ebene.
- Optionale Test-Erweiterungen sind als solche markiert (`Apply_SetsCultures` mit Kultur-Restore wegen prozessweiten `CultureInfo`-Zustands und xunit-Parallelität; Erweiterung von `E2E_ChangeSettings_PersistRoundtrip`/`Load_InvalidPersistedValues_UsesFallbacks` statt separater Tests) — keine Pflichtlücke.
- Keine bestehenden Tests brechen (Konstruktor- und Interface-Signaturen unverändert); betroffene Hilfsmethode `TestSettingsHelper` ist vollständig benannt.
- Schritt 15 (`dotnet build`, `dotnet test`, `Run-StaticChecks.ps1` mit Exit 0) deckt die AGENTS.md-Pflicht lokaler statischer Checks ab.
