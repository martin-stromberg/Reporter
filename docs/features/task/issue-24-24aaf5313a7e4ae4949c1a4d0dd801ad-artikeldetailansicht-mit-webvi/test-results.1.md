# Test-Ergebnisse

## Ergebnis

**Status:** Fehler vorhanden

## Fehlgeschlagene Tests

### E2E-/UI-Fluss-Szenarien

- **Navigation zur Artikeldetailansicht** — Fehlend: Keine automatisierte E2E-Testabdeckung im Repository vorhanden.
- **WebView stellt Artikelinhalt dar** — Fehlend: Keine automatisierte E2E-Testabdeckung vorhanden.
- **Automatisches Gelesen-Markieren beim Öffnen** — Fehlend: Keine automatisierte E2E-Testabdeckung vorhanden.
- **Lesezeichen-Button toggelt gespeichert-für-später** — Fehlend: Keine automatisierte E2E-Testabdeckung vorhanden.
- **Teilen-Button öffnet System-Share-Dialog** — Fehlend: Keine automatisierte E2E-Testabdeckung vorhanden.
- **Im Browser öffnen** — Fehlend: Keine automatisierte E2E-Testabdeckung vorhanden.

## E2E-Abdeckung

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Navigation zur Artikeldetailansicht | `n/a` | Fehlend |
| WebView stellt Artikelinhalt dar | `n/a` | Fehlend |
| Automatisches Gelesen-Markieren beim Öffnen | `n/a` | Fehlend |
| Lesezeichen-Button toggelt `is_saved_for_later` | `n/a` | Fehlend |
| Teilen-Button öffnet System-Share-Dialog | `n/a` | Fehlend |
| Im Browser öffnen | `n/a` | Fehlend |

## Zusammenfassung

- Gesamt (dotnet test Reporter.sln --no-build): 70
- Bestanden: 70
- Fehlgeschlagen: 0
- Übersprungen: 0
- Build (`dotnet build Reporter.sln`): 0 Warnungen, 0 Fehler

## Testabdeckung

**Abdeckung:** 53.4 % (lines-covered / lines-valid); 65.4 % branch-rate

| Datei | Abdeckung |
|-------|-----------|
| Reporter.Core\ViewModels\FeedsViewModel.cs | 57.2 % |
| Reporter.Core\ViewModels\LaterViewModel.cs | 0 % |
| Reporter.Core\ViewModels\SettingsViewModel.cs | 0 % |
| Reporter.Core\ViewModels\UnreadViewModel.cs | 58.6 % |

## Fehlende Tests

Quelle: Dateinamen-Konvention

- `src/Reporter/ViewModels/ArticleDetailViewModel.cs` — Keine Testdatei `ArticleDetailViewModelTests.cs` gefunden.
- `src/Reporter/Views/ArticleDetailPage.xaml.cs` — Keine Code-Behind-Testdatei gefunden.

---

Hinweis: Die Unit-/Integrationstests bestanden, aber die geplanten E2E-Szenarien des Benutzerflusses sind im Repository nicht automatisiert vorhanden und wurden nicht ausgeführt.
