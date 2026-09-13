<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# UI-Komponenten — Bestandsaufnahme (Issue #77)

## `ArticleCardView` (R1, R2)
Dateien: `src\Reporter\Views\ArticleCardView.xaml` + `.xaml.cs`

Wiederverwendbare Artikelkarte auf **Ungelesen** (`UnreadPage.xaml` Zeile 197–199) und **Später** (`LaterPage.xaml` Zeile 46–48).

- **Lesezeit** (Zeilen 105–127): `HorizontalStackLayout` mit Uhr-`Path` + `Label {Binding ReadingTimeText}`, Sichtbarkeit über `IsVisible="{Binding ReadingTimeText, Converter={StaticResource StringNotEmptyToBoolConverter}}"`. **R1 greift ohne XAML-Änderung**, sobald `ReadingTimeText` bei 1 Minute `null`/leer ist.
- **Thumbnail** (Zeilen 76–93): 80×80 `Border` (RoundRectangle 12) mit `Image Source="{Binding ImageUrl}" Aspect="AspectFill"`; ausgeblendet offline via `DataTrigger` auf `IsOnline` (BindableProperty, `x:Reference Card`). **Kein Fallback bei `ImageUrl == null`** — der leere `Border` bleibt sichtbar (kein Trigger auf `ImageUrl`), kein Favicon-`Image`, kein Initialen-Kreis (R2).
- Weitere Elemente: Ungelesen-Punkt (`IsRead`-Trigger), `FeedTitle`/`PublishedAt`-Zeile, `CategoryName`-Badge (`IsVisible`-Trigger auf leer/null), `Summary` (`StringNotEmptyToBoolConverter`), Bookmark-/Mark-read-`Border`s (44×44, `TapGestureRecognizer`), transparenter Vollflächen-Tap (`OpenArticleCommand` → `articledetail?itemId=…`).
- BindableProperties: `OpenArticleCommand`, `ToggleSavedCommand`, `MarkReadCommand`, `IsOnline` (Default `true`).

## `SettingsPage` (R3, R4, R5, R7)
Dateien: `src\Reporter\Views\SettingsPage.xaml` + `.xaml.cs`

Abschnitte (je `UiLabelStyle`-Überschrift + `Border`-Karte):
- **Aufbewahrungsdauer** (Zeilen 22–57): `RetentionDays`-Slider + `SaveRetentionCommand` (DragCompleted), Info-`Border`.
- **Keyword-Filter** (Zeilen 59–140): `Entry`/`Button` `AddKeywordCommand`, Fehler-Label, Chip-Liste mit Entfernen-Buttons, Match-Hinweis.
- **Synchronisation & Lesefluss** (Zeilen 142–232): `Switch AutoRefreshEnabled`, `Picker SelectedRefreshInterval` (deaktiviert/dimmed bei aus), `Switch AutoMarkReadEnabled`, `Picker SelectedAutoMarkReadDelay`. **R3-Schalter und R4-Picker würden hier einsortiert** — es gibt weder Start-Abruf- noch Sortier-Eintrag.
- **Benachrichtigungen & Ruhezeiten** (Zeilen 234–424): `Switch NotificationsEnabled` (deaktiviert via `NotificationsSupported`), iOS-only-Hinweis, `NotDetermined`-/`Denied`-Hinweiszeilen per `MultiTrigger`, Detail-`Border` (`NotificationControlsEnabled`) mit `NotificationSummaryEnabled`-`Switch` und Quiet-Hours-`TimePicker`s.
- **Erscheinungsbild** (Zeilen 426–447): `Picker SelectedTheme`.
- **Sprache** (Zeilen 449–480): `Picker SelectedLanguage`; darunter der **Neustart-Hinweis-`Border` (Zeilen 470–477) mit `AppResources.SettingsLanguageRestartHint` — ohne `IsVisible`-Binding, also immer sichtbar** (R5-Anker).

Code-Behind: `OnAppearing` abonniert `NotificationAuthorizationDenied` und führt `LoadCommand` aus; `OnDisappearing` deabonniert; `OnNotificationAuthorizationDenied` zeigt `DisplayAlertAsync` → `AppInfo.Current.ShowSettingsUI()`; `OnOpenNotificationSettingsClicked` ebenfalls `ShowSettingsUI`. **Kein E-Mail-/Debug-Versand** (R7).

## `ArticleDetailPage` (R1-/R2-Kontext)
Datei: `src\Reporter\Views\ArticleDetailPage.xaml`

Bindet `FeedIconUrl` in ein `Image` (Zeilen 66–67, Sichtbarkeit via `StringNotEmptyToBoolConverter`) — die Quelle wird im ViewModel immer `string.Empty` gesetzt, d. h. das Icon-Element existiert, wird aber nie angezeigt. `ReadingTime` (Zeilen 95–100) ebenfalls via `StringNotEmptyToBoolConverter` sichtbar — R1 betrifft die Detailansicht nicht.

## `UnreadPage` / `LaterPage` (R1, R4)
Dateien: `src\Reporter\Views\UnreadPage.xaml`, `LaterPage.xaml`

`CollectionView` mit `ArticleCardView`-ItemTemplate; `IsOnline` durchgereicht. `UnreadPage` zusätzlich: Kategorie-Chips, Pull-to-Refresh (`RefreshCommand`), Offline-Hinweis, „Alle als gelesen"-Aktion, `LoadMoreCommand`-Paging. Kein Sortier-UI (R4 kommt als reine Einstellung).

## `App.xaml` / `Styles.xaml`
`StringNotEmptyToBoolConverter` registriert in `src\Reporter\Resources\Styles\Styles.xaml` Zeile 13 — für R2-Fallback-Trigger direkt wiederverwendbar.
