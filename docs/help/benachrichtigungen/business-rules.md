<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Benachrichtigungen — Business Rules

## Entscheidungskette pro Sync

**Beschreibung:** Ob neue Artikel eine Benachrichtigung auslösen, entscheidet eine feste Kette von Prüfungen; jede nicht erfüllte Bedingung verwirft die Kandidaten vollständig (Reihenfolge relevant).

**Bedingungen:**
- `feed.NotificationsEnabled` (Pro-Feed-Schalter) — wird zuerst geprüft, noch vor dem Settings-Ladevorgang.
- `settings.NotificationsEnabled` (globaler Hauptschalter).
- Ruhezeit aktiv (s. nächste Regel).
- Keyword-Filter pro Artikel (`Title` und `ContentHtml`, Teilwort + `OrdinalIgnoreCase` — identische Semantik wie die Löschregel; `Item.Link` wird nicht gematcht).

**Verhalten:**
- Feed-Schalter aus oder keine neuen Artikel → sofortiger Abbruch ohne Settings-Zugriff.
- Globaler Schalter aus oder Ruhezeit aktiv → alle Kandidaten verworfen.
- Keyword-Treffer werden einzeln entfernt; ist die Restliste leer → Abbruch.
- Im Sammelmodus zählen für Anzahl, Text und Identifier nur die überlebenden Artikel — gefilterte Items fließen nicht ein.

**Umsetzung:** `NotificationService.NotifyNewItemsAsync` (Reihenfolge: Feed-Flag → globaler Schalter → Ruhezeit → Keywords → Modus-Verzweigung).

## Ruhezeit-Auswertung (Wrap-around, leere und einseitige Intervalle)

**Beschreibung:** `QuietHoursStart`/`QuietHoursEnd` werden unvalidiert gespeichert (vgl. [Einstellungen — Business Rules](../einstellungen/business-rules.md)); die Auswertung gegen die lokale Gerätezeit erfolgt hier. Eine aktive Ruhezeit **verwirft** Benachrichtigungen — es gibt kein Nachholen.

**Verhalten:**
- `QuietHoursStart` oder `QuietHoursEnd` ist `null` → keine Ruhezeit (einseitige Werte werden ignoriert).
- `Start == End` → leeres Intervall → keine Ruhezeit.
- `Start < End` (z. B. 12:00–14:00) → aktiv, wenn `now >= Start && now < End`.
- `Start > End` (über Mitternacht, z. B. 22:00–07:00) → aktiv, wenn `now >= Start || now < End`.
- `End` ist stets exklusiv, `Start` inklusiv.

**Umsetzung:** `NotificationService.IsQuietHoursActive` — `now` aus `_timeProvider.GetLocalNow().TimeOfDay` (injizierbarer `TimeProvider`, lokale Gerätezeit).

## Pro Artikel höchstens eine Benachrichtigung (Dedup)

**Beschreibung:** Derselbe Artikel darf nie zweimal benachrichtigt werden — weder durch den Sync noch durch iOS-Dubletten im Mitteilungszentrum.

**Bedingungen:**
- `Item`s werden über den Unique-Index `(feed_id, guid_or_hash)` nur einmal gespeichert; Benachrichtigungskandidaten sind ausschließlich die im aktuellen Sync neu eingefügten Artikel (`newItemEntities` in `RunSyncAsync`).
- iOS ersetzt eine zugestellte oder ausstehende Benachrichtigung mit identischem `UNNotificationRequest`-Identifier, statt sie zu duplizieren.

**Verhalten:**
- Einzelmodus: Identifier = `Item.Id` (stabile GUID) → ein Artikel = ein Identifier = max. eine Benachrichtigung.
- Sammelmodus: Identifier = `{feedId}-{SHA256-Hex der sortierten Item-Ids}`. Identischer Artikelbestand → identischer Identifier → iOS ersetzt die alte Sammel-Benachrichtigung (keine Dopplung bei erneutem Aufruf). Veränderter Bestand → neuer Identifier → neue Benachrichtigung statt stiller Ersetzung.

**Umsetzung:** `NotificationService.BuildSummaryIdentifier` (Sortierung ordinal, Join mit `|`, SHA-256 über UTF-8), `LocalNotificationService.ShowAsync` (`UNNotificationRequest.FromIdentifier`).

## Benachrichtigungsmodus (einzeln vs. Sammel)

**Beschreibung:** `Settings.NotificationSummaryEnabled` wählt zwischen einer Benachrichtigung pro Artikel (`false`, Default) und einer Sammel-Benachrichtigung pro Feed und Sync (`true`).

**Verhalten:**
- `false`: pro verbleibendem Artikel ein `ShowAsync` — Titel = `Feed.Title`, Body = `Item.Title`, `UserInfo` = `{ itemId, link? }` (`link` nur, wenn `Item.Link` gesetzt).
- `true`: genau ein `ShowAsync` — Titel = `Feed.Title`, Body = `AppResources.NotificationSummaryFormat` (de: „{0} neue Artikel: {1}") mit Anzahl und kommagetrennter Titelliste (gekürzt auf 160 Zeichen + „…"), `UserInfo` = `{ feedId }`.
- Ein Moduswechsel wirkt erst ab dem nächsten Sync; bereits zugestellte Benachrichtigungen werden nicht umgruppiert oder entfernt.

**Umsetzung:** `NotificationService.NotifyNewItemsAsync` (Modus-Verzweigung), `Truncate` (`MaxSummaryTitlesLength` = 160).

## Berechtigungsanfrage kontextuell, nicht beim App-Start

**Beschreibung:** iOS erlaubt den System-Berechtigungsdialog nur einmal pro Installation. Die Anfrage wird daher beim bewussten Einschalten des Hauptschalters gestellt — mit lazy Fallback vor jedem Versand.

**Verhalten:**
- `SettingsViewModel.NotificationsEnabled` → `true` (außerhalb `LoadAsync`) startet `RequestNotificationAuthorizationAsync`; der anschließend gelesene Status (`GetAuthorizationStatusAsync`) steuert die Anzeige: `Denied` → `NotificationPermissionDenied = true` + Ereignis `NotificationAuthorizationDenied` (Dialog „Einstellungen öffnen" / „Abbrechen"); `NotDetermined` → `NotificationPermissionNotDetermined = true` (neutrale Zeile mit Button „Benachrichtigungen erlauben" → `RequestNotificationPermissionCommand`).
- `LocalNotificationService.EnsureAuthorizedAsync` fragt zusätzlich vor jedem `ShowAsync` nach, solange der Status `NotDetermined` ist; `Denied`/`Authorized` werden nicht erneut angefragt.
- Als autorisiert gelten `Authorized`, `Provisional` und `Ephemeral`.
- `SettingsViewModel.LoadAsync` fragt den Status ohne Dialog nach (`GetAuthorizationStatusAsync`) und steuert die Hinweiszeilen; auf Nicht-iOS (`IsSupported == false` → `NotificationsSupported == false`) und bei ausgeschaltetem Hauptschalter bleiben beide Flags `false` — stattdessen sind die Benachrichtigungs-Schalter deaktiviert und ein Hinweis „derzeit nur auf iOS verfügbar" eingeblendet.
- `App.OnStart` enthält bewusst keine Anfrage — ein ungefragter Dialog beim ersten Start ohne Benutzerkontext wurde vermieden.

**Umsetzung:** `SettingsViewModel.RequestNotificationAuthorizationAsync`/`RefreshNotificationPermissionAsync`/`ApplyAuthorizationStatus`, `LocalNotificationService.EnsureAuthorizedAsync`/`MapStatus`/`IsAuthorized`, `SettingsPage` (MultiTrigger-Hinweiszeilen + `AppInfo.ShowSettingsUI` + `NotificationsSupported`-Deaktivierung).

## Tap-Navigation mit Browser-Fallback

**Beschreibung:** Das Antippen einer Benachrichtigung soll in die App führen; beim Kaltstart ist die Shell eventuell noch nicht bereit.

**Verhalten:**
- `itemId` im `UserInfo` → Route `articledetail?itemId={itemId}` (Artikeldetailansicht).
- Nur `feedId` (Sammel-Benachrichtigung) → Route `//unread` (Tab **Ungelesen**).
- Navigation nur, wenn `Shell.Current` bereit ist; schlägt sie fehl oder fehlt die Shell (Kaltstart) und ein `link` existiert → `Launcher.OpenAsync(link)` öffnet den Artikel im Browser.
- Exceptions werden vollständig geschluckt — ein Antippen darf die App niemals abstürzen lassen.

**Umsetzung:** `NotificationDelegate.DidReceiveNotificationResponse`; Routen in `AppShell` (`Routing.RegisterRoute("articledetail")`, `ShellContent.Route = "unread"`).
