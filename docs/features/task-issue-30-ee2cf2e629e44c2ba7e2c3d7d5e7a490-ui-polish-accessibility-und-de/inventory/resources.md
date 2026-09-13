<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Ressourcen / Design-Tokens / Lokalisierung

## `Colors.xaml` (`src/Reporter/Resources/Styles/Colors.xaml`)

Theme-fähiges Token-Set (jeweils `Light*`/`Dark*`-Schlüssel, Verwendung über `AppThemeBinding`). Kommentar in der Datei verweist auf die Übernahme aus den `DESIGN.md`-Dateien des Entwurfs.

| Token-Gruppe | Vorhandene Schlüssel (Light-Wert / Dark-Wert) |
|---|---|
| Surfaces | `Surface` (#f7f9fb/#0f131c), `SurfaceDim` (#d8dadc/#0f131c), `SurfaceBright` (#f7f9fb/#353942), `SurfaceContainerLowest` (#ffffff/#0a0e16), `SurfaceContainerLow` (#f2f4f6/#181c24), `SurfaceContainer` (#eceef0/#1c2028), `SurfaceContainerHigh` (#e6e8ea/#262a33), `SurfaceContainerHighest` (#e0e3e5/#31353e), `SurfaceVariant` (#e0e3e5/#31353e), `SurfaceCard` (#FFFFFF/#182234), `PaperBase` (#F9F9FB/#111827), `SurfaceSubtle` (#F1F5F9/#111827) |
| On-Surface | `OnSurface` (#191c1e/#dfe2ee), `OnSurfaceVariant` (#45474c/**#d8c3ad** — in der Anforderung zur Verifikation gegen den Entwurf markiert) |
| Primary | `Primary` (#091426/#ffc174), `OnPrimary` (#ffffff/#472a00), `PrimaryContainer` (#1e293b/#f59e0b), `OnPrimaryContainer` (#8590a6/#613b00), `PrimaryFixed`/`PrimaryFixedDim`/`OnPrimaryFixed`/`OnPrimaryFixedVariant` |
| Secondary | `Secondary` (#0051d5/#4edea3), `OnSecondary`, `SecondaryContainer`, `OnSecondaryContainer`, `SecondaryFixed`, `SecondaryFixedDim`, `OnSecondaryFixed`, `OnSecondaryFixedVariant` |
| Tertiary | `Tertiary`, `OnTertiary`, `TertiaryContainer`, `OnTertiaryContainer`, `TertiaryFixed`, `TertiaryFixedDim`, `OnTertiaryFixed`, `OnTertiaryFixedVariant` |
| Error | `Error` (#ba1a1a/#ffb4ab), `OnError`, `ErrorContainer`, `OnErrorContainer` |
| Background | `Background` (#f7f9fb/#0b0f17), `OnBackground` (#191c1e/#dfe2ee) |
| Outline/Border | `Outline` (#75777d/#a08e7a), `OutlineVariant` (#c5c6cd/#534434), `BorderSubtle` (#e2e8f0/#334155) — vorhanden, wird aber aktuell in den Views nicht referenziert (Karten nutzen `Outline`) |
| SurfaceTint | `SurfaceTint` (#545f73/#ffb95f) |
| Text | `TextPrimary` (#0f172a/#f1f5f9), `TextSecondary` (#64748b/#cbd5e1), `TextMuted` (**#94a3b8/#94a3b8** — in der Anforderung zur Kontrastprüfung markiert) |
| Inverse | `InverseSurface`, `InverseOnSurface`, `InversePrimary` |
| Status | `StatusOk` (#10b981/#10b981), `StatusWarning` (#f59e0b/#f59e0b), `StatusError` (#ef4444/#f43f5e) — **keine Tint-/Container-Varianten** für Badge-Hintergründe (10-%-Tint laut Entwurf) vorhanden |
| Bookmark | `BookmarkGold` (#D97706/#f59e0b) — wird nur in `ArticleDetailPage` verwendet; `ArticleCardView` nutzt `Primary` |
| Utility | `Transparent` |
| Brushes | `PrimaryBrush`, `SecondaryBrush`, `TertiaryBrush`, `SurfaceBrush`, `OnSurfaceBrush`, `SurfaceContainerBrush`, `OutlineBrush` (alle `AppThemeBinding`) |

## `Styles.xaml` (`src/Reporter/Resources/Styles/Styles.xaml`)

- Converter: `StringNotEmptyToBoolConverter` (`src/Reporter/Converters/StringNotEmptyToBoolConverter.cs`).
- Typografie-Styles (Named Styles für `Label`):

| Style | FontFamily | FontSize | TextColor |
|---|---|---|---|
| `DisplayStyle` | NewsreaderRegular | 40 | `TextPrimary` |
| `HeadlineStyle` | NewsreaderSemiBold | 28 | `TextPrimary` |
| `HeadlineSmallStyle` | NewsreaderSemiBold | 20 | `TextPrimary` |
| `BodyStyle` | NewsreaderRegular | 19 | `TextPrimary` |
| `BodySmallStyle` | InterRegular | 14 | `OnSurface` |
| `MetaStyle` | InterRegular | 12 | `TextMuted` |
| `UiLabelStyle` | InterSemiBold | 14 | `TextSecondary` |

  → Die Entwurfs-Typo-Skala (`display-lg`, `headline-lg/-md/-sm`, `body-reading`, `label-md`, `label-sm`, `label-meta` in `editorial_feed/DESIGN.md`) ist nur teilweise abgebildet; dedizierte `label-*`-/`body-reading`-Styles fehlen.

- Implizite Styles (`ApplyToDerivedTypes` bzw. `TargetType`): `Label` (Inter 14), `Page` (Hintergrund `Background`), `Shell` (incl. `TabBar*`-Farben), `NavigationPage`, `TabbedPage`, `Button` (`Primary`-Fläche, `CornerRadius 4`, `Padding 14,10`, `Minimum*Request 44`, Disabled-VSM), `Entry`, `Editor`, `SearchBar`, `Picker`, `DatePicker`, `TimePicker`, `Border` (`Stroke` = `Outline`, `StrokeShape Rectangle`, `StrokeThickness 1`, `Background` `SurfaceContainer` — greift überall dort, wo Views `Stroke`/`Background` nicht explizit setzen), `BoxView`, `ActivityIndicator`, `ProgressBar`, `RefreshView`, `Slider`, `Switch`, `CheckBox`, `RadioButton`, `IndicatorView`, `ImageButton` — alle farblich per `AppThemeBinding`.
- `FontAutoScalingEnabled` wird im gesamten `src/`-Baum nicht gesetzt (kein Vorkommen) — dynamische Schriftgrößen laufen damit über den MAUI-Default.
- Shape-Konstanten sind nicht als Ressourcen zentralisiert; Radien stehen literal in den Views (Karten 16 in `ArticleCardView`, 12 in `FeedsPage`/`CategoriesPage`/`SettingsPage`-Karten und `ArticleDetailPage`-Footer, 8 für Hinweis-Boxen, 22 für runde Buttons, 10 für die Kategorie-Pill).

## Programmsymbol / Splash

- `src/Reporter/Resources/AppIcon/appicon.svg` — 456×456-`rect` Vollfläche `#512BD4` (.NET-Default).
- `src/Reporter/Resources/AppIcon/appiconfg.svg` — weiße „NET"-Schriftzug-Pfade (.NET-Default).
- `src/Reporter/Reporter.csproj` — `<MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#512BD4" />` und `<MauiSplashScreen … Color="#512BD4" …/>`; `Resources\Images\dotnet_bot.png` noch vorhanden.
- Referenz-Logo: `design-draft/stitch_local_rss_feed_reader/take_the_existing_logo_design_from_the_reference_image_data_image_image_13_the/screen.png` (nur PNG, kein SVG-Ausgangsmaterial im Repo).
- Plattformdateien: `Platforms/iOS/Info.plist`, `Platforms/Android/AndroidManifest.xml` (Android-Target ist in `Reporter.csproj` standardmäßig deaktiviert: `IncludeAndroidTarget` default `false`).

## Fonts (`src/Reporter/Resources/Fonts/`)

Vorhanden und als `MauiFont` eingebunden: `Inter-Regular/Medium/SemiBold.ttf`, `Newsreader-Regular/Medium/SemiBold/Italic.ttf` — entsprechen den Entwurfs-Familien Inter/Newsreader.

## Lokalisierung (`src/Reporter.Core/Resources/Strings/`)

- `AppResources.resx` (neutral/en) + `AppResources.de.resx` (de) + generierter `AppResources.Designer.cs`; Sprachwahl über `Settings.Language` (`system`/`de`/`en`).
- Vorhandene Accessibility-/Schlüssel für Screenreader-Labels (in beiden resx): `AccessibilityBack`, `AccessibilityFontSize`, `AccessibilityShare`, `ArticleBookmarkSet`, `ArticleBookmarkRemove`, `ArticleMarkAsRead`, `ArticleAlreadyRead`.
- Es gibt **keine** Schlüssel für: Refresh-/Filter-/MarkAll-Buttons auf `UnreadPage`, `ArticleCardView`-Aktionsbuttons und Karten-Tap-Overlay, Feed-/Kategorie-/Suchtreffer-Karten, Sheet-Backdrop. Ebenso keine dedizierten Chip-/Badge-Texte (Chip-Texte kommen aus `CategoryFilterItem.Name`/`Count`; `FilterAll` existiert).
