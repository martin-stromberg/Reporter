# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### SettingsPage.xaml / App.xaml.cs (Karte „Benachrichtigungen & Ruhezeiten", Berechtigungsanfrage)

- **Erreichbarkeit** — Die geforderte Interaktion „globalen Schalter `Settings.NotificationsEnabled` aktivieren, damit Benachrichtigungen kommen" scheitert für eine Laiin stillschweigend in zwei Situationen:
  1. **Verweigerte Systemberechtigung bleibt unsichtbar.** Die iOS-Berechtigungsanfrage läuft in `App.OnStart` (nur wenn der Schalter bereits an ist) oder lazy beim ersten Versand in `LocalNotificationService.ShowAsync`. Hat die Anwenderin den iOS-Dialog einmal abgelehnt (`Denied`), lässt sich der In-App-Schalter weiterhin einschalten und bleibt an — es kommt aber nie eine Benachrichtigung an, und die App zeigt weder einen Hinweis noch einen Weg in die iOS-Einstellungen. Eine nicht-technische Anwenderin kann nicht erkennen, warum „Benachrichtigungen an" wirkungslos bleibt.
  2. **Kontextloser Berechtigungsdialog beim App-Start.** Da `NotificationsEnabled` per Default `true` ist, wird der iOS-Systemdialog bereits beim ersten App-Start gezeigt — ohne dass die Anwenderin die Funktion je gesehen oder den Schalter bewusst aktiviert hat. Ablehnung aus Unklarheit führt direkt in Situation 1.

  Empfehlung: Beim Aktivieren des Schalters in den Einstellungen `RequestAuthorizationAsync` aufrufen (Dialog im Kontext der bewussten Aktivierung) und den Berechtigungsstatus auswerten. Bei `Denied` einen kurzen `DisplayAlert` mit verständlichem Text („Benachrichtigungen sind in den iOS-Einstellungen deaktiviert") und Button „Einstellungen öffnen" (`AppInfo.Current.ShowSettingsUI()` bzw. `UIApplication.SharedApplication.OpenUrl(UIApplication.OpenSettingsUrlString)`) anbieten; alternativ den Schalter mit Hinweislabel auf den tatsächlichen Systemstatus zurücksetzen. Dafür `ILocalNotificationService` um eine Status-Abfrage ergänzen.

### SettingsPage.xaml / AppResources (Karte „Benachrichtigungen & Ruhezeiten")

- **Erreichbarkeit (Beschriftung)** — Der Hauptschalter heißt „Push-Benachrichtigungen" mit dem Hinweis „Eilmeldungen & favorisierte Feeds" (`SettingsNotificationsLabel`/`SettingsNotificationsHint`). Die gebaute Funktion liefert aber **lokale** Benachrichtigungen zu neuen Artikeln — für alle Feeds, bei denen der Pro-Feed-Schalter an ist, nicht für „Eilmeldungen" oder „Favoriten". Eine Laiin, die in den iOS-Einstellungen die Berechtigung prüfen will, findet dort „Mitteilungen", nicht „Push"; und sie erwartet womöglich ein anderes Benachrichtigungsverhalten als das tatsächliche. Die Zeichenkette stammt aus dem Einstellungen-Arbeitspaket, trifft die gebaute Funktion aber erst jetzt real.

  Empfehlung: Label auf „Benachrichtigungen" und Hinweis auf den tatsächlichen Zweck ändern, z. B. „Bei neuen Artikeln benachrichtigen" (EN: „Notifications" / „Get notified about new articles").

### FeedsPage.xaml (Feed-Formular, Pro-Feed-Schalter)

- **Erreichbarkeit** — Der neue Switch „Benachrichtigungen" (Hinweis: „Bei neuen Artikeln dieses Feeds benachrichtigen") wirkt nur, wenn zusätzlich der globale Schalter in den Einstellungen an ist. Auf der Feeds-Seite ist diese Abhängigkeit nicht erkennbar: Ist der globale Schalter aus, kann die Anwenderin den Pro-Feed-Schalter einschalten, ohne dass etwas passiert oder erklärt wird, warum der Feed „stumm" bleibt.

  Empfehlung: Hinweistext erweitern, z. B. „Bei neuen Artikeln benachrichtigen (erfordert aktivierte Benachrichtigungen in den Einstellungen)", oder die Zeile per Binding ausgrauen, wenn der globale Schalter aus ist.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:

- Globalen Schalter „Benachrichtigungen" in den Einstellungen ein-/ausschalten → Befund vorhanden (kein Feedback bei verweigerter iOS-Berechtigung; kontextloser Berechtigungsdialog beim App-Start; ungenaue Beschriftung)
- „Sammel-Benachrichtigung" ein-/ausschalten → unauffällig (verständlich beschriftet, korrekt als Unteroption des globalen Schalters ausgegraut)
- Ruhezeit per Schalter + TimePicker „VON/BIS" konfigurieren → unauffällig (vorhandene Bedienung, unverändert)
- Pro-Feed „Benachrichtigungen" im Feed-Formular ein-/ausschalten (Neuanlage und Bearbeiten via Feed-Karte → „Feed-Aktionen" → „Bearbeiten") → Befund vorhanden (Abhängigkeit vom globalen Schalter nicht erkennbar); Auswahl des Feeds selbst erfolgt über benannte Karten — keine interne Kennung nötig
- Keyword-Filter pflegen (unterdrückt Benachrichtigungen für Treffer) → unauffällig (vorhandene Bedienung, unverändert)
- iOS-Berechtigungsdialog beantworten → Befund vorhanden (siehe erster Befund: Zeitpunkt/Kontext und fehlendes Feedback nach Ablehnung)
- Benachrichtigung antippen → unauffällig: kein `DidReceiveNotificationResponse` implementiert, iOS-Standardverhalten öffnet die App; die Anforderung lässt das Ziel bewusst offen (Offene Frage 4)

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:

- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/App.xaml.cs`
- `src/Reporter/Platforms/iOS/AppDelegate.cs`
- `src/Reporter/Platforms/iOS/NotificationDelegate.cs`
- `src/Reporter/Services/LocalNotificationService.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/Services/NotificationService.cs` (Inhalt der Benachrichtigungstexte)
- `src/Reporter.Core/Resources/Strings/AppResources.resx` / `AppResources.de.resx`
- `test-results.md`, `test-results/issue-27/manual-*.png` (manuelle Verifikationsnachweise)

## Hinweise

- **Diff-Breite:** Der kommittete Diff gegen `main` (`git merge-base HEAD main`) enthält auffällig viele Dateien aus früheren Issues (#25, #26). Die tatsächlichen Issue-27-Änderungen liegen zum Review-Zeitpunkt uncommitted im Working Tree vor (`git status`: u. a. `SettingsPage.xaml`, `FeedsPage.xaml`, `App.xaml.cs`, `LocalNotificationService.cs`, `NotificationDelegate.cs`, ViewModels, resx). Geprüft wurden gezielt diese Working-Tree-Änderungen; die breite committed Dateiliste wurde nicht als Fehlverhalten gewertet.
- **AGENTS.md Mobile-Regeln:** Keine horizontalen Datentabellen, keine mehreren Text-Buttons in einer Zeile; neue `Switch`-Zeilen verwenden das etablierte Karten-/Zeilenmuster (`Grid *,Auto`, Label + `MetaStyle`-Hinweis), `MinimumWidthRequest`/`MinimumHeightRequest="44"`, `SemanticProperties.Description`; Farben ausschließlich über `AppThemeBinding`. Manuelle Verifikation ist in `test-results.md` mit Screenshots unter `test-results/issue-27/` dokumentiert (390 × 844 pt, Windows-Target); die iOS-Simulator-Verifikation steht noch aus (dort vermerkt).
