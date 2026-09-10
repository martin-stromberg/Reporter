# Detaildokument: Bestehende UI-Elemente

## Pages
- `src/Reporter/Views/UnreadPage.xaml`
  - Zeigt `CollectionView` mit `ArticleCardView`.
  - `Shell.NavBarIsVisible="False"`.
  - Nutzt `RefreshView`, `Grid` mit `RowDefinitions="Auto,*"`.

- `src/Reporter/Views/ArticleCardView.xaml`
  - Card-Layout mit `Border`, `Grid`, `AppThemeBinding`.
  - Touch-Targets für Lesezeichen und Gelesen-Markierung bereits 44 × 44 (`WidthRequest="44" HeightRequest="44"`).
  - `TapGestureRecognizer` für `OpenArticleCommand`, `ToggleSavedCommand`, `MarkReadCommand`.

## ViewModels
- `src/Reporter.Core/ViewModels/UnreadViewModel.cs`
  - Implementiert `ToggleSavedCommand` und `MarkReadCommand`.
  - Bietet kein `OpenArticleCommand`/Navigation zur Detailansicht.

- `src/Reporter.Core/ViewModels/BaseViewModel.cs`
  - Grundlage für neue ViewModels (vermutlich `INotifyPropertyChanged` + `IsBusy`).

## Navigation / DI
- `src/Reporter/MauiProgram.cs`
  - Registriert `UnreadPage`, `LaterPage`, `FeedsPage`, `CategoriesPage`, `SettingsPage` sowie `AppShell`.
  - Keine Detail-Page/-ViewModel vorhanden.

- `src/Reporter/AppShell.xaml`
  - Leer (nur `<Shell .../>`); Navigation muss programmatisch oder über Routing-Registrierung erfolgen.

## Styles / Ressourcen
- `src/Reporter/Resources/Styles/Colors.xaml`
- `src/Reporter/Resources/Styles/Styles.xaml`
- Schriftarten: `Inter-*` und `Newsreader-*` sind in `MauiProgram.cs` bereits registriert.

## Feststellungen
- Eine Artikeldetailseite existiert noch nicht.
- Navigation von Karte zur Detailseite muss eingerichtet werden.
- Vorhandene Pattern (Card-View, 44-pt-Touch-Targets, `AppThemeBinding`) können wiederverwendet werden.
