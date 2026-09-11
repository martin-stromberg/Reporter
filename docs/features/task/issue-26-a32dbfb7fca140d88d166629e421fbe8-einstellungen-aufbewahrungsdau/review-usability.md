# Usability-Review
Status: Befunde vorhanden

> **Nachtrag (2026-09-11):** Alle 6 Befunde wurden anschließend behoben —
> Description am Slider (1) und am Keyword-Entry (2), Fehlermeldung wird bei
> Textänderung zurückgesetzt (3), Slider rastet auf ganze Tage ein und die
> Skala zeigt nur noch „1 Tag – 365 Tage" (4), Sektion heißt jetzt einheitlich
> „Schlagwort-Filter" (5), verschachtelte Opacity per MultiTrigger aufgelöst
> (6). Verifikation: Build 0 Warnungen, 147/147 Tests, Static Checks Exit 0.

## Befund 1 — Slider ohne Screenreader-Beschreibung
- Datei: src/Reporter/Views/SettingsPage.xaml, Zeilen 36–40
- Schweregrad: mittel
- Beschreibung: Der Slider für die Aufbewahrungsdauer ist das einzige interaktive
  Element ohne `SemanticProperties.Description`. Alle Switches, Picker und
  TimePicker haben eine Beschreibung. Screenreader-Nutzer hören nur „Slider"
  ohne Kontext — der zugehörige Label „Gelesene Artikel aufbewahren" ist nicht
  mit dem Slider verknüpft. Verstoß gegen die AGENTS.md-Regel „semantische
  Beschreibungen".
- Empfehlung: `SemanticProperties.Description="{x:Static strings:AppResources.SettingsRetentionLabel}"`
  am Slider ergänzen.
- **Status: Behoben** (im Zuge der Code-Review-Fixes, Commit folgt).

## Befund 2 — Keyword-Eingabefeld ohne Screenreader-Beschreibung
- Datei: src/Reporter/Views/SettingsPage.xaml, Zeilen 66–68
- Schweregrad: niedrig
- Beschreibung: Das `Entry` für neue Schlagworte hat nur einen Placeholder
  („Schlagwort eingeben…"). Placeholder werden von Screenreadern nicht zuverlässig
  als Accessible Name verwendet und verschwinden, sobald Text eingegeben ist.
- Empfehlung: `SemanticProperties.Description` ergänzen (z. B. neuer Resource-
  String „Neues Schlagwort" oder Wiederverwendung des Placeholder-Strings).

## Befund 3 — Fehlermeldung bleibt während der Korrektur sichtbar
- Datei: src/Reporter.Core/ViewModels/SettingsViewModel.cs, Zeilen 164–168
  (`NewKeywordText`-Setter), 529–565 (`AddKeywordAsync`)
- Schweregrad: niedrig
- Beschreibung: Nach einem Validierungsfehler (leer/Duplikat/zu lang) bleibt
  `HasError = true`, bis der Nutzer erneut „+ Hinzufügen" auslöst. Während der
  Nutzer die Eingabe korrigiert, steht die rote Fehlermeldung weiter unter dem
  Eingabefeld — das wirkt, als wäre die neue Eingabe ebenfalls fehlerhaft.
- Empfehlung: Im `NewKeywordText`-Setter `HasError = false` setzen, sobald sich
  der Text ändert.

## Befund 4 — Skalen-Text unter dem Slider suggeriert Stufen, die es nicht gibt
- Datei: src/Reporter/Views/SettingsPage.xaml, Zeilen 41–43;
  src/Reporter.Core/Resources/Strings/AppResources.de.resx, Zeilen 207–209
  („1 Tag – 90 Tage – 180 Tage – 365 Tage")
- Schweregrad: niedrig
- Beschreibung: Der Slider ist stufenlos (Minimum=1, Maximum=365, double-Binding
  ohne Schrittweite). Der zentrierte Text mit vier Werten sieht aus wie
  Rasterpunkte bzw. einrastbare Marken — die Werte sind zudem ungleichmäßig
  verteilt (1→90→180→365). Nutzer könnten annehmen, nur diese Werte seien
  wählbar. Erschwerend: auf einem 1–365-Slider per Touch kaum ein exakter
  Tagesswert treffbar (~1 pt pro Tag).
- Empfehlung: Entweder die Skala positionskorrekt als Min-/Max-Beschriftung
  (nur „1 Tag" links, „365 Tage" rechts) darstellen, oder dem Slider
  `Step`-Verhalten geben, damit exakte Werte einstellbar sind.

## Befund 5 — Inkonsistente Terminologie: „Keyword"/„Blacklist" vs. „Schlagwort"
- Datei: src/Reporter.Core/Resources/Strings/AppResources.de.resx,
  Zeilen 187 („Keyword-Filter (Blacklist)"), 211 („Schlagwort eingeben…"),
  300–307 (Fehlermeldungen „Schlagwort"), 315–317 („Schlagwort {0} entfernen")
- Schweregrad: niedrig
- Beschreibung: Die Sektion heißt „Keyword-Filter (Blacklist)", der Rest der UI
  spricht durchgehend von „Schlagwort". Zwei verschiedene Begriffe für dasselbe
  Konzept; „Blacklist" ist zudem Fachjargon und ein zunehmend vermiedener Begriff.
- Empfehlung: Einheitlich „Schlagwort-Filter" (bzw. „Ausschlussliste" statt
  „Blacklist") verwenden.

## Befund 6 — Verschachtelte Disabled-Opacity erzeugt fast unsichtbare Elemente
- Datei: src/Reporter/Views/SettingsPage.xaml, Zeilen 253–317
  (äußerer Border Opacity 0.4 bei `NotificationsEnabled=False`;
  inneres Grid Opacity 0.4 bei `QuietHoursEnabled=False`)
- Schweregrad: niedrig
- Beschreibung: Wenn Benachrichtigungen aus UND Ruhezeit aus sind, multiplizieren
  sich die Opacity-Werte: Die Von/Bis-TimePicker werden effektiv auf ~0,16
  abgedunkelt und sind kaum noch lesbar.
- Empfehlung: Opacity-Trigger nur auf der jeweils direkt steuernden Ebene
  belassen (inneres Grid nicht zusätzlich abdunkeln, wenn der äußere Container
  bereits deaktiviert ist — z. B. via MultiTrigger auf beide Bindings).

## Geprüft und ohne Befund
- Vollständigkeit der Anforderung: Auto-Refresh Ein/Aus + Intervall (15/30 min,
  1 h, 4 h), Aufbewahrungsdauer 1–365 (Standard 30), Keyword-Filter mit
  Hinzufügen/Entfernen, Auto-Gelesen Ein/Aus + Verzögerung (0/1/3/5 s),
  Benachrichtigungen + Ruhezeiten Von/Bis (beliebige Zeiten, über Mitternacht
  möglich), Theme System/Hell/Dunkel — alles vorhanden.
- Touch-Targets ≥ 44 pt überall: explizit an Switch/Picker/TimePicker/Slider/
  Entfernen-Button; Button/Entry über implizite Styles.
- Lokalisierung: alle Texte aus AppResources (x:Static), de/en parallel
  gepflegt; einziger hartcodierter Text ist das Symbol „×" — akzeptabel, da
  sprachneutral und mit lokalisierter SemanticDescription.
- Konsistenz Anzeige↔Persistenz: `RetentionDaysText` rundet identisch wie
  `PersistAsync`/`SaveRetention`; Slider springt nach Loslassen auf den
  gespeicherten ganzzahligen Wert.
- Fehlerfälle: leer/Duplikat (case-insensitiv)/>500 Zeichen werden mit
  verständlichen, lokalisierten Meldungen quittiert.
- Layout: Kartenmuster, ein ScrollView, keine verschachtelten Scroll-Container,
  keine horizontalen Tabellen, durchgehend AppThemeBinding.
- Deaktivierte Zustände: Interval-/Verzögerungs-Picker und Ruhezeiten werden per
  IsEnabled + Opacity-Trigger abgedunkelt und sind nicht bedienbar.
- Sofort-Persistierung: jede Property-Änderung löst `PersistAsync` aus;
  Theme-Wechsel und Auto-Refresh wirken sofort.
