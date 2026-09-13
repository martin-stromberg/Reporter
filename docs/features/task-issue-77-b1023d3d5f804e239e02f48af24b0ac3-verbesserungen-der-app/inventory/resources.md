<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Ressourcen und Plattform-Artefakte — Bestandsaufnahme (Issue #77)

## Programmsymbol / Splash (R6)

| Datei | Inhalt |
|-------|--------|
| `src\Reporter\Resources\AppIcon\appicon.svg` | 456×456 SVG, nur `<rect fill="#1e293b">` — **reine Hintergrundfläche ohne Motiv** (Placeholder) |
| `src\Reporter\Resources\AppIcon\appiconfg.svg` | 456×456 SVG, Zeitungs-/Newsletter-Icon (`Path` weiß `#ffffff` + amber Punkt `#f59e0b`), `translate(114,114) scale(9.5)` |
| `src\Reporter\Resources\Splash\splash.svg` | 456×456 SVG, **identisches Icon** wie `appiconfg.svg` (gleiche Pfade/Farben) — keine separate Variante mit App-Name |

`src\Reporter\Reporter.csproj` (Zeilen 52–57):
- `<MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#1e293b" />`
- `<MauiSplashScreen Include="Resources\Splash\splash.svg" Color="#1e293b" BaseSize="128,128" />`

`src\Reporter\Platforms\iOS\Info.plist` (Zeilen 31–32): `XSAppIconAssets` = `Assets.xcassets/appicon.appiconset` (Standard-Verweis auf das generierte Asset; kein `UIBackgroundModes`-Eintrag — auch für R8 relevant).

`src\Reporter\Platforms\Windows\Package.appxmanifest`: nur `$placeholder$`-Verweise (`WindowsPackageType=None` → unverpackter Windows-Build; Manifest greift dort nicht direkt).

Weitere Bild-Assets: `src\Reporter\Resources\Images\tab_*.svg` (Tab-Icons), `Resources\Fonts\*` (Inter, Newsreader), `Resources\Raw\**` als `MauiAsset`.

## Lokalisierte Strings (R3, R4, R5, R7)
Dateien: `src\Reporter.Core\Resources\Strings\AppResources.resx` (EN, mit `AppResources.Designer.cs`) + `AppResources.de.resx` (DE).

Relevant vorhandene Schlüssel:
- `SettingsLanguageRestartHint` (R5 — Text existiert, Sichtbarkeit nicht gesteuert)
- `SettingsAutoRefreshLabel`/`Hint`, `SettingsRefreshIntervalLabel`, `SettingsAutoMarkReadLabel`/`Hint`, `SettingsAutoMarkReadDelayLabel` (Muster für neue Sync-Einträge, R3/R4)
- `SettingsSectionRetention`/`Keywords`/`Sync`/`Notifications`/`Appearance`/`Language` (Sektions-Überschriften)
- `SettingsLanguageLabel`/`System`/`German`/`English`, `SettingsTheme*`, `SettingsInterval*`, `SettingsDelay*` (Options-Labels)
- `ArticleReadingTimeFormat` (Lesezeit-Format, R1)
- `Notification*`-Schlüssel (R8-Umfeld)

**Fehlt:** sämtliche neuen Schlüssel für Start-Abruf (R3), Sortierung (R4), Debug-Sektion/E-Mail (R7) — jeweils EN + DE (RESX-Konsistenz wird laut Anforderung vom `translation-check`-Hook geprüft).

## Projekt-Konventionen (Build-relevant)
- `src\Reporter\Reporter.csproj`: `net10.0-windows10.0.19041.0` (+`net10.0-ios` nur wenn `IncludeIosTarget=true`); `WarningsAsErrors=CS1591` (XML-Doku Pflicht), `MauiXamlInflator=SourceGen`.
- `src\Reporter.Core\Reporter.Core.csproj` und `src\Reporter.Data\Reporter.Data.csproj`: `net10.0` ohne MAUI-Referenz — R7-E-Mail muss über Gateway-Interface laufen (`Email.ComposeAsync` nur in `src\Reporter` nutzbar; `Microsoft.Maui.Essentials` ist Teil des MAUI-Frameworks, kein Extra-Paket).
