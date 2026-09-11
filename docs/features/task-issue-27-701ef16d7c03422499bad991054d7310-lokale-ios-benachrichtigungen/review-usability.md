# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### SettingsPage.xaml / SettingsViewModel.cs (Einstellungen)

- **Erreichbarkeit** — Irreführender „deaktiviert"-Hinweis bei nie angefragter iOS-Berechtigung: `Settings.NotificationsEnabled` hat Default `true` (`src/Reporter.Data/Entities/Settings.cs:39`, DB-Default in `ReporterDbContext.cs:87`). Beim Öffnen der Einstellungen ruft `SettingsViewModel.RefreshNotificationPermissionAsync` (`SettingsViewModel.cs:480-497`) `IsAuthorizedAsync` auf; `LocalNotificationService.IsAuthorized` (`LocalNotificationService.cs:123-128`) liefert für den iOS-Status `NotDetermined` `false` → `NotificationPermissionDenied = true`. Da die Berechtigung erst beim Aktivieren des Schalters oder beim ersten Benachrichtigungsversand (`EnsureAuthorizedAsync`, `LocalNotificationService.cs:86-101`) angefragt wird — nie beim App-Start —, sieht ein Anwender auf einer Neuinstallation (oder nach App-Update mit bestehendem `true`) sofort die rote Zeile „Benachrichtigungen sind für diese App in den Systemeinstellungen deaktiviert. Aktiviere sie dort …" mit dem Button „Einstellungen öffnen". Das ist faktisch falsch (der Anwender hat nie etwas deaktiviert) und führt in eine Sackgasse: iOS zeigt in den App-Einstellungen erst dann einen Bereich „Mitteilungen", wenn die App einmal eine Berechtigung angefragt hat — dort gibt es also noch nichts zum Aktivieren. Der einzige funktionierende Weg (Schalter aus/ein, um den Systemdialog auszulösen) wird dem Anwender nicht vermittelt.

  Empfehlung: `NotDetermined` von `Denied` unterscheiden — z. B. den tatsächlichen Autorisierungsstatus über `ILocalNotificationService` exponieren und die Hinweiszeile nur bei `Denied` zeigen; bei `NotDetermined` stattdessen eine neutrale Zeile mit Button „Benachrichtigungen erlauben", der `RequestAuthorizationAsync` aufruft (löst den echten iOS-Dialog aus). Alternativ beim Öffnen der Einstellungen bei aktivem Schalter die Berechtigung direkt anfragen.

### SettingsPage.xaml / SettingsViewModel.cs (Einstellungen, Nicht-iOS-Plattformen)

- **Erreichbarkeit** — Funktionsloser Schalter ohne Rückmeldung auf Windows: Die App wird auch für `net10.0-windows` gebaut (`Reporter.csproj`). Dort ist `ILocalNotificationService.IsSupported == false` (`LocalNotificationService.cs:20-26`): `RequestAuthorizationAsync` kehrt sofort zurück, `ShowAsync` ist No-Op, `NotificationPermissionDenied` bleibt `false`. Ein Anwender kann den Schalter „Benachrichtigungen — Bei neuen Artikeln benachrichtigen" einschalten, der Zustand wird persistiert, es kommen aber niemals Benachrichtigungen — ohne jeden Hinweis, dass die Funktion auf dieser Plattform nicht existiert. Gleiches gilt für die abhängigen Optionen „Sammel-Benachrichtigung" und „Ruhezeit" sowie für den Pro-Feed-Schalter auf `FeedsPage`. Die Kundenanforderung sieht iOS vor und No-Op auf anderen Plattformen — ein Schalter, der eine nicht vorhandene Funktion vorspiegelt, ist für Laien trotzdem nicht als solcher erkennbar.

  Empfehlung: Bei `!IsSupported` die Sektion „Benachrichtigungen & Ruhezeiten" ausblenden oder die Schalter deaktivieren und eine neutrale Zeile einblenden, z. B. „Benachrichtigungen sind derzeit nur auf iOS verfügbar" (Analogie: die bestehende Hinweis-Mechanik der Karte kann wiederverwendet werden). Entsprechend auf `FeedsPage` den Pro-Feed-Schalter mit Hinweis versehen oder ausblenden.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Globalen Benachrichtigungs-Schalter in den Einstellungen aktivieren/deaktivieren → Befund vorhanden (siehe oben: falscher „deaktiviert"-Hinweis bei `NotDetermined`; funktionsloser Schalter auf Nicht-iOS)
- Berechtigungsanfrage beim Aktivieren bzw. bei verweigerter Systemberechtigung (Hinweis + „Einstellungen öffnen") → Befund vorhanden (bei tatsächlich verweigerter Berechtigung ist Hinweis + System-Einstellungen-Button korrekt und gut beschriftet; der `NotDetermined`-Fall erzeugt denselben Hinweis fälschlich und der Button führt in eine Sackgasse)
- Ruhezeit konfigurieren (Schalter „Ruhezeit (Nicht stören)" + „VON"/„BIS"-TimePicker, inkl. Zeitraum über Mitternacht) → unauffällig (klare Beschriftung, Abdunklung/Deaktivierung konsistent zum Auto-Refresh-Muster, entspricht `design-draft/.../einstellungen_filter`)
- „Sammel-Benachrichtigung" ein-/ausschalten (Aggregation „n neue Artikel: Titel…" pro Feed) → unauffällig
- Pro-Feed-Benachrichtigungen im Feed-Formular schalten — Anlage und Bearbeiten über Feed-Karte → ActionSheet → „Bearbeiten", Vorbefüllung des Schalters → unauffällig (Abhängigkeit zum globalen Schalter ist im Hinweistext erklärt; Randnotiz ohne Befund-Charakter: In der Feed-Liste ist nicht erkennbar, für welche Feeds Benachrichtigungen stumm geschaltet sind — der Zustand ist nur über „Bearbeiten" je Feed sichtbar)
- Benachrichtigung antippen → In-App-Navigation zur Artikeldetailansicht (`articledetail?itemId=`) bzw. zur Liste „Ungelesen" (`//unread` bei Sammel-Benachrichtigung) → unauffällig (Randnotiz: Nur beim Kaltstart ohne bereite Shell wird stattdessen die Artikel-URL im externen Browser geöffnet; bei Sammel-Benachrichtigungen ohne `link` geschieht dann gar nichts — akzeptabler Fallback, aber abweichendes Verhalten)
- Keyword-Filter pflegen (bestehender Einstellungsbereich, unterdrückt Benachrichtigungen mit) → unauffällig

Mobile-Regeln (AGENTS.md) an den geänderten Oberflächen geprüft: keine horizontalen Datentabellen, keine mehreren Text-Buttons in einer Zeile (Hinweiszeile enthält genau einen Button), Touch-Targets ≥ 44 × 44 pt an allen neuen Switches/Pickern/Buttons, `AppThemeBinding` für alle neuen Farben, `SemanticProperties.Description` an neuen Bedienelementen, keine Scroll-Verschachtelung. Layout gegen `design-draft/stitch_local_rss_feed_reader/einstellungen_filter` verglichen — Karte „Benachrichtigungen & Ruhezeiten" entspricht dem Entwurf (Schalter oben, Ruhezeit-Block mit VON/BIS darunter); der „Aktiv"-Status-Badge des Entwurfs ist bereits als bekannte Scope-Abweichung in `mobile-ui-design.md` dokumentiert. Manuelle Verifikation (390 × 844 pt) in `test-results.md` und `test-results/issue-27/manual-*.png` dokumentiert; iOS-Simulator-Verifikation steht als dokumentierter macOS-Folgeschritt aus — die Befunde oben betreffen genau diesen iOS-Pfad und konnten daher nur statisch geprüft werden.

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/Views/SettingsPage.xaml.cs`
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/Services/NotificationService.cs` (Benachrichtigungstitel/-text, UserInfo-Payload)
- `src/Reporter/Services/LocalNotificationService.cs` (Berechtigungsstatus/Anzeige)
- `src/Reporter/Platforms/iOS/NotificationDelegate.cs` (Vordergrund-Darstellung, Tap-Navigation)
- `src/Reporter/Platforms/iOS/AppDelegate.cs` (Delegate-Registrierung)
- `src/Reporter/AppShell.xaml.cs` (Route `unread`, `articledetail`)
- `src/Reporter/Views/ArticleDetailPage.xaml.cs` und `src/Reporter/ViewModels/ArticleDetailViewModel.cs` (Navigationsziel `itemId`)
- `src/Reporter.Core/Resources/Strings/AppResources.resx` / `AppResources.de.resx` (alle neuen Beschriftungen, DE + EN vorhanden)
