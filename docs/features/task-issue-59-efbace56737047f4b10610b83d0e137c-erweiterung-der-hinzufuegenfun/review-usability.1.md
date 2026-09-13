<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedsPage.xaml / FeedsViewModel.cs (FeedsPage – Trefferansicht)

- **Erreichbarkeit** — Die Trefferansicht ersetzt die gesamte Feed-Liste (`ShowSearchResults` blendet die `RefreshView` mit der Feed-Liste komplett aus), bietet aber kein sichtbares Bedienelement, um sie wieder zu verlassen. Betroffen sind zwei aus der Anforderung vorgesehene Fälle: (a) Domain-Eingabe ohne Treffer — es erscheint nur der statische Leer-Text „Keine Feeds gefunden.", kein Dialog (der Direkt-Hinzufügen-Dialog kommt nur bei vollständiger URL); (b) Freitext-Eingabe — derselbe Leer-Zustand. Auch nach „Nein" im Abonnieren-Dialog bleibt die Trefferliste offen. Der einzige Rückweg ist, im URL-Feld erneut etwas zu tippen oder zu löschen (der `NewUrl`-Setter setzt `ShowSearchResults` zurück) — implizites Wissen, das eine nicht-technische Anwenderin nicht hat. Sie kann in diesem Zustand weder ihre Feeds sehen noch einen vorhandenen Feed bearbeiten, ohne den Trick zu kennen.

  Empfehlung: Ein sichtbares „Zurück"/„Schließen"/„Abbrechen"-Element in der Trefferansicht (z. B. Button unter der Attribution oder in der Kartenliste), das `ShowSearchResults` auf `false` setzt; alternativ den `EmptyView` um einen aktiven Rückweg-Hinweis/-Button ergänzen.

### FeedsViewModel.cs / AppResources.de.resx (FeedsPage – Fehlerhinweis Suche)

- **Erreichbarkeit** — Der Hinweis `FeedSearchUnavailable` („Die Feed-Suche ist nicht erreichbar. Du kannst die URL direkt hinzufügen.") wird auch dann angezeigt, wenn die Eingabe eine nackte Domain war (z. B. `tagesschau.de`), die gar nicht direkt gespeichert werden kann: Tippt die Anwenderin anschließend auf „Speichern", erscheint „Bitte gib eine gültige Feed-URL ein." — der Hinweis verspricht also einen Weg, der für diese Eingabe nicht existiert (der Direkt-Hinzufügen-Dialog wird korrekt nur bei `isDirectUrl` angeboten, der Fehlertext aber unabhängig davon).

  Empfehlung: Den Zusatz „Du kannst die URL direkt hinzufügen." nur anzeigen, wenn die Eingabe eine gültige URL war (`isDirectUrl`), oder einen eigenen Text für Domain-Eingaben verwenden, der zum erneuten Versuch bzw. zur Eingabe einer vollständigen Feed-URL rät.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Domain oder vollständige URL ins Such-/URL-Feld eingeben und Suche auslösen → unauffällig (Placeholder „Feed-URL oder Website-Adresse…", `Keyboard="Url"`, Button „Suchen", `ReturnCommand`)
- Treffer als scrollbare Kartenliste ansehen (Titel, Beschreibung, Site-Name/Host, Feed-URL) → unauffällig (`CollectionView` in `Grid`-Zeile `*`, Karten, Attribution „powered by feedsearch.dev")
- Treffer per Tap auswählen und nach Bestätigungsdialog abonnieren → unauffällig (`TapGestureRecognizer` + `DisplayAlertAsync` „„{0}" abonnieren?", Dublettenprüfung über `GetByUrlAsync`)
- Leeres Ergebnis bei gültiger URL → Confirm-Dialog „Kein Feed gefunden. Möchtest du die eingegebene URL direkt hinzufügen?" mit Titel-Vorbefüllung → unauffällig
- Domain-/Freitext-Eingabe ohne Treffer → Befund vorhanden (Sackgasse ohne Rückweg-Steuerelement)
- Suche nicht erreichbar → Hinweis + URL-Fallback → Befund vorhanden (irreführender Direkt-Hinzufügen-Hinweis bei Domain-Eingabe)
- Offline: Suche deaktiviert, Hinweis, direkte URL-Hinzufügung bleibt → unauffällig (`SearchCommand.CanExecute`, `FeedSearchOfflineHint`-Label, `OnConnectivityChanged` ruft `NotifyCanExecuteChanged`)
- Kategorie per Picker und Benachrichtigungs-Schalter beim Abonnieren → unauffällig (Klartext-Picker, Default „—" = keine Kategorie)
- Interne Kennungen (Ids, technische Schlüssel) eingeben müssen → unauffällig (keine erforderlich; Auswahl nur über Klartext)

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Models/FeedSearchResult.cs` (Kontext: welche Felder die Trefferkarte zeigt)
- `src/Reporter.Core/Services/FeedSearchService.cs` (Kontext: Trefferinhalte/-arten)
