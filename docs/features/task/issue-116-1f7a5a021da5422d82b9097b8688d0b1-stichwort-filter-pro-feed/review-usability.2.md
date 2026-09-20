<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedDetailPage.xaml (Bearbeiten-Sheet „Feed bearbeiten")

- **Erreichbarkeit** — Interaktion „feed-spezifische Stichworte hinzufügen und entfernen": Die Stichworte werden sofort beim Tippen auf „+ Hinzufügen" bzw. auf „×" am Chip dauerhaft gespeichert (`AddFeedKeywordAsync`/`RemoveFeedKeywordAsync` persistieren direkt über das Repository), während die übrigen Angaben im selben Sheet — Feed-URL und Benachrichtigungs-Schalter — erst mit „Speichern" wirksam werden und über „Abbrechen" (Button, abgedunkelter Hintergrund) verworfen werden. Eine Endanwenderin kann diese zwei unterschiedlichen Wirkungsweisen im selben Dialog nicht erkennen: Wer ein Stichwort hinzufügt und dann „Abbrechen" tippt, geht davon aus, alle Änderungen seien verworfen — das Stichwort bleibt aber gespeichert. Umgekehrt ist unklar, ob ein hinzugefügtes Stichwort ohne „Speichern" überhaupt aktiv ist.

  Empfehlung: Die Sofort-Wirkung im Sheet kenntlich machen — z. B. den vorhandenen Hinweistext `FeedKeywordsInfo` um einen Satz wie „Änderungen an Schlagworten wirken sofort, ein separates Speichern ist nicht nötig." ergänzen — oder den Stichwort-Bereich visuell vom speicherpflichtigen Teil abgrenzen (z. B. eigene Karte mit eigenem Hinweis). Alternativ die Stichwort-Eingaben ebenfalls erst beim „Speichern" übernehmen; das würde allerdings vom etablierten Muster der globalen Stichwort-Verwaltung abweichen.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Feed-Detailseite öffnen und über das Aktionsmenü („…" → „Bearbeiten") das Bearbeiten-Sheet aufrufen → unauffällig
- Stichwort in das Eingabefeld eingeben und über „+ Hinzufügen" bzw. die Eingabetaste hinzufügen → unauffällig
- Vorhandene Feed-Stichworte als Chips einsehen und per „×" entfernen (44×44-Touch-Target, Accessibility-Text „Schlagwort {0} entfernen") → unauffällig
- Validierungsfehler (leer, zu lang, Duplikat) sowie Fehler beim Speichern/Löschen eines Stichworts im Sheet lesen → unauffällig (eigenes Fehler-Label `FeedKeywordErrorMessage` direkt unter dem Eingabefeld)
- Tragweite des Feed-Filters verstehen (gilt nur für diesen Feed, ergänzt den globalen Schlagwort-Filter, gefilterte Artikel werden beim Abruf verworfen) → unauffällig (Hinweis-Banner `FeedKeywordsInfo` im Sheet)
- Verhältnis des sofort wirkenden Stichwort-Bereichs zu den Sheet-Schaltflächen „Speichern"/„Abbrechen" → Befund vorhanden

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `src/Reporter/Views/FeedDetailPage.xaml`
- `src/Reporter/Views/FeedDetailPage.xaml.cs` (Einstieg über das Feed-Aktionsmenü — unverändert, für die Erreichbarkeitsprüfung mitberücksichtigt)
- `src/Reporter.Core/ViewModels/FeedDetailViewModel.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs` (Referenzmuster der globalen Stichwort-Verwaltung)
- `src/Reporter.Core/Services/KeywordValidator.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
