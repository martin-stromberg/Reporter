# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### src/Reporter/Platforms/iOS/NotificationDelegate.cs (iOS-Benachrichtigungsverhalten)

- **Abweichendes Muster** — Interaktion „Benachrichtigung antippen": Bei einer Einzel-Benachrichtigung wird der Artikel per `Launcher.Default.OpenAsync(link)` im **externen Browser** geöffnet (`DidReceiveNotificationResponse`, Zeile 31–34). Die Anwenderin verlässt damit die App, obwohl der Artikel in der App vorhanden ist und eine etablierte In-App-Route existiert (`ArticleCardView.xaml.cs:86`: `Shell.Current.GoToAsync($"articledetail?itemId={item.Id}")`). Nebeneffekt aus Endanwendersicht: Der Artikel wird nicht als gelesen markiert (das Auto-Gelesen läuft nur in der In-App-Detailansicht), und die Leserin landet nicht auf der in der Anforderung diskutierten Zielseite (Ungelesen/Artikeldetail). Bei der **Sammel-Benachrichtigung** enthält `userInfo` nur `feedId`, keinen `link` (`NotificationService.cs:81`) — ein Antippen holt die App nur in den Vordergrund, ohne zu den neuen Artikeln zu führen. Das Antippen verhält sich also je nach Modus unterschiedlich bzw. wirkungslos; eine nicht-technische Person tippt „3 neue Artikel: …" an und sieht keine der genannten Artikel.

  Empfehlung: Einheitliche In-App-Navigation beim Antippen — Einzel-Benachrichtigung via `Shell.Current.GoToAsync("articledetail?itemId=…")` auf die bestehende Detailroute (itemId liegt bereits im `userInfo`), Sammel-Benachrichtigung mindestens auf den Tab „Ungelesen" bzw. eine feed-gefilterte Ansicht navigieren.

### src/Reporter/Views/SettingsPage.xaml / src/Reporter.Core/ViewModels/SettingsViewModel.cs (Einstellungen)

- **Erreichbarkeit** — Interaktion „Benachrichtigungen aktivieren" bei verweigerter iOS-Berechtigung: Beim Einschalten des Schalters wird die Berechtigung angefragt; bei Verweigerung erscheint der Alert mit „Einstellungen öffnen" / „Abbrechen" (`SettingsPage.xaml.cs:44–55`). Wählt die Anwenderin „Abbrechen", bleibt der Schalter dauerhaft auf „Ein" (`NotificationsEnabled` wird bereits vor der Prüfung persistiert, `SettingsViewModel.cs:281–295`), obwohl iOS alle Benachrichtigungen blockiert. Beim nächsten Öffnen der Einstellungen zeigt die Karte „Benachrichtigungen & Ruhezeiten" einen scheinbar aktiven Zustand — es gibt keinerlei sichtbaren Hinweis, dass die Systemberechtigung fehlt. Eine nicht-technische Person, die den Alert weggetippt hat, kann nicht erkennen, warum keine Benachrichtigungen ankommen.

  Empfehlung: Den System-Berechtigungsstatus beim Anzeigen der Einstellungsseite erneut prüfen (`UNUserNotificationCenter.GetNotificationSettingsAsync` bzw. eine `IsAuthorized`-Abfrage am `ILocalNotificationService`) und den Zustand sichtbar machen — z. B. Hinweiszeile „In den Systemeinstellungen deaktiviert" mit erneutem „Einstellungen öffnen"-Angebot, oder den Schalter auf „Aus" zurücksetzen, wenn die Berechtigung verweigert bleibt.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Globalen Benachrichtigungs-Schalter in den Einstellungen ein-/ausschalten → Befund vorhanden (verbleibender „Ein"-Zustand ohne Statushinweis bei verweigerter Systemberechtigung)
- Berechtigungsverweigerung: Weg in die Systemeinstellungen finden → unauffällig (Alert „Benachrichtigungen deaktiviert" mit „Einstellungen öffnen" via `AppInfo.ShowSettingsUI()`)
- Ruhezeit aktivieren und Von-/Bis-Uhrzeiten einstellen → unauffällig (Switch „Ruhezeit (Nicht stören)" + zwei `TimePicker` VON/BIS, Defaults 22:00–07:00, Abdunklung bei ausgeschalteter Option)
- Sammel-Benachrichtigung ein-/ausschalten → unauffällig (beschriftete Zeile „Sammel-Benachrichtigung" mit Hinweistext, wird mit dem globalen Schalter ausgegraut)
- Pro Feed Benachrichtigungen im Anlage-/Bearbeitungsformular schalten → unauffällig (Switch „Benachrichtigungen" mit Abhängigkeits-Hinweis; Bearbeiten über Feed-Karte → ActionSheet „Bearbeiten" füllt den Flag vor)
- Keyword-Filter pflegen, damit Treffer nicht benachrichtigen → unauffällig (bestehende, unveränderte Eingabe + Chips)
- Benachrichtigung antippen → Befund vorhanden (Einzel: externer Browser statt In-App-Route; Sammel: kein Navigationsziel)

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/Views/SettingsPage.xaml.cs`
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Services/NotificationService.cs` (Inhalt/userInfo der Benachrichtigungen)
- `src/Reporter/Services/LocalNotificationService.cs`
- `src/Reporter/Platforms/iOS/AppDelegate.cs`
- `src/Reporter/Platforms/iOS/NotificationDelegate.cs`
- `src/Reporter/MauiProgram.cs` (DI-Registrierung)
- `test-results.md` und `test-results/issue-27/` (manuelle Verifikation 390 × 844 pt, Screenshots `manual-01…07`, UIA-Skript)
- `design-draft/stitch_local_rss_feed_reader/einstellungen_filter/code.html` und `feeds_health_status/code.html` (Abgleich Benachrichtigungs-/Ruhezeiten-Karte und Feed-Formular)
