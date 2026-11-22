<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Testklassifikation und lokale Härtung

## Übersicht

`Reporter.Tests` und `Reporter.E2ETests` erhalten gemeinsame Testkategorien für `OsInterface` und `Flaky`. Alle OS-sensitiven E2E- und Infrastrukturtests werden markiert; `Reporter.Tests` erhält eine robuste Temp-Verzeichnis-Cleanup-Hilfe und fokussierte Konventions-/Cleanup-Tests.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Testattribute, lokale Test-Hilfsklassen und Testmethoden |
| Auslösende Ereignisse | `dotnet test` und spätere Trait-Filter |
| Erwartete Wirkung | OS-sensitive Tests sind separat filterbar; Flaky ist nur innerhalb `OsInterface` möglich; Temp-Cleanup ist retry-basiert und wirft nicht |
| Umgang mit fehlenden Objekten | Fehlende Temp-Verzeichnisse gelten als aufgeräumt; ungültige Pfade werden protokolliert und nicht geworfen |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine Workflow-Änderungen | Schritt 2 behandelt CI-Lanes und Statistik | bestätigt |
| 2 | Keine neue Flaky-Markierung | Es gibt aktuell keinen bekannten Flaky-Test in `Reporter.Tests`; die Ausnahme-Kategorie wird nur konventionell vorbereitet | bestätigt |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|------------------|------------|
| Testklassifikation | `ITraitAttribute`-Attribute `OsInterface` und `Flaky` | Maschinell filterbar und besser lesbar als rohe `Trait("-")`-Duplikate |
| Kategorien | `TestCategories` pro Testprojekt | Keine neue Shared-Test-Assembly nötig; kleine Konstantenmenge |
| Temp-Cleanup | `TempDirectoryCleanup` als interne Hilfsklasse in `Reporter.Tests` | Übernimmt bewährtes best-effort Muster und erlaubt injizierbare Delays für deterministische Tests |
| E2E-Markierung | `[OsInterface]` direkt auf allen OS-sensitiven Methoden | Methodenebene ist mit MTP/xUnit zuverlässig filterbar und hängt nicht von Klassen-Vererbung von Traits ab |

## Programmabläufe

### OS-Interface-Klassifikation

1. `TestCategories` stellt `E2E`, `OsInterface` und `Flaky` als Konstanten bereit.
2. `OsInterfaceAttribute` und `FlakyAttribute` liefern über `ITraitAttribute.GetTraits()` den Wert `Category=<Name>`.
3. Alle OS-sensitiven Testmethoden in `Reporter.E2ETests` erhalten `[OsInterface]` zusätzlich zu vorhandenen Traits.
4. Neue OS-sensitive Cleanup-Tests in `Reporter.Tests` erhalten `[OsInterface]`.

### Flaky-Konvention

1. `TestClassificationConventionTests` liest alle Testmethoden und Klassenattribute des `Reporter.Tests`-Assembly.
2. Hat eine Methode `Category=Flaky`, muss dieselbe Methode oder ihre Klasse zusätzlich `Category=OsInterface` liefern.
3. Verstöße werden mit Methodennamen gemeldet.

### Temp-Verzeichnis-Cleanup

1. `TempDirectoryCleanup.TryDeleteDirectoryAsync` prüft `Directory.Exists` und löscht rekursiv.
2. Bei `IOException` oder `UnauthorizedAccessException` wiederholt die Hilfe bis zu fünf Versuche.
3. Nach dem letzten Versuch oder bei nicht wiederholbaren Fehlern wird eine Meldung über den optionalen Log-Callback ausgegeben; es wird nicht geworfen.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `TestCategories` | interne statische Klasse in beiden Testprojekten | Zentrale Kategorienamen |
| `OsInterfaceAttribute` | internes `ITraitAttribute` | Kennzeichnet OS-sensitive Tests |
| `FlakyAttribute` | internes `ITraitAttribute` | Kennzeichnet bekannte instabile OS-sensitive Tests |
| `TempDirectoryCleanup` | interne statische Klasse | Robuster rekursiver Temp-Cleanup mit Retry und Log-Callback |
| `TestClassificationConventionTests` | Testklasse | Prüft Trait-Ausgabe und Flaky/OsInterface-Konvention |
| `TempDirectoryCleanupTests` | Testklasse | Prüft Erfolgs-, Fehl-, Retry- und Best-Effort-Pfade des Cleanup-Helfers |

## Änderungen an bestehenden Klassen

### E2E-Testklassen (`SmokeTests`, `FeedDetailTests`, `UnreadPacketTests`, `ArticleTagsTests`, `ArticleLinkTests`, `ArticleImageTests`, `DemoSeedTests`, `E2EProcessGuardTests`)

- **Neue Attribute:** `[OsInterface]` auf allen OS-sensitiven `[Fact]`-Methoden.
- **Geänderte Kommentare:** Veraltete Aussage in `E2EProcessGuardTests`, dass bewusst keine Kategorie gesetzt ist, wird auf die neue Klassifikation angepasst.

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `path` in `TempDirectoryCleanup` | Leer oder null ist nicht zulässig | `ArgumentException` beim Aufruf mit ungültigem Pfad ist zulässig und wird nicht als Cleanup-Fehler behandelt |
| `Category=Flaky` | Nur mit `Category=OsInterface` | Konventionstest schlägt fehl |

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Filterung:** Zusätzliche `Category=OsInterface`-Traits dürfen bestehende `Category=E2E`-Filter nicht verdrängen; beide Kategorien können nebeneinander existieren.
- **E2E-Testlauf:** Die Markierung verändert keine Ausführung ohne Filter; lokale/on-demand-Läufe bleiben unverändert.
- **Cleanup-Tests:** Tests mit Dateisperren sind bewusst `OsInterface` und laufen damit später in der OS-Lane statt im deterministischen Pflichtlauf.

## Umsetzungsreihenfolge

1. **Kategorien und Attribute in `Reporter.Tests` anlegen**
   - Voraussetzungen: xUnit v3 bereits vorhanden.
   - Beschreibung: `TestCategories`, `OsInterfaceAttribute`, `FlakyAttribute` erstellen.

2. **Kategorien und Attribute in `Reporter.E2ETests` anlegen**
   - Voraussetzungen: xUnit v3 bereits vorhanden.
   - Beschreibung: dieselbe Konstanten-/Attributstruktur im E2E-Namespace erstellen.

3. **`TempDirectoryCleanup` implementieren**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: best-effort rekursives Löschen mit maximal fünf Versuchen, retrybarer IO-/Zugriffsfehlerbehandlung und optionalem Log-Callback.

4. **Neue fokussierte Tests schreiben**
   - Voraussetzungen: Schritte 1 und 3.
   - Beschreibung: `TestClassificationConventionTests` und `TempDirectoryCleanupTests` nach Topic-Regeln erstellen.

5. **Alle OS-sensitiven E2E-Methoden markieren**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: `[OsInterface]` zu allen `[Fact]`-Methoden in den acht Testklassen hinzufügen und den veralteten Kommentar in `E2EProcessGuardTests` aktualisieren.

6. **Filter und Tests lokal verifizieren**
   - Voraussetzungen: Schritte 1–5.
   - Beschreibung: Build, gefilterte `OsInterface`-Liste und kompletter `Reporter.Tests`-Lauf.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `OsInterfaceAttribute_GetTraits_ReturnsOsInterfaceCategory` | `TestClassificationConventionTests` | Trait `Category=OsInterface` |
| `FlakyAttribute_GetTraits_ReturnsFlakyCategory` | `TestClassificationConventionTests` | Trait `Category=Flaky` |
| `FlakyTests_AlwaysDeclareOsInterface` | `TestClassificationConventionTests` | Flaky ohne OsInterface ist verboten |
| `TryDeleteDirectoryAsync_ExistingDirectory_RemovesIt` | `TempDirectoryCleanupTests` | Rekursiver Erfolgspfad |
| `TryDeleteDirectoryAsync_MissingDirectory_DoesNotThrow` | `TempDirectoryCleanupTests` | Fehlendes Verzeichnis ist Erfolg |
| `TryDeleteDirectoryAsync_BrieflyLockedFile_RetriesAndRemoves` | `TempDirectoryCleanupTests` | Retry nach kurzer Dateisperre |
| `TryDeleteDirectoryAsync_PersistentLockedFile_LogsAndDoesNotThrow` | `TempDirectoryCleanupTests` | Best-effort Fehlschlag mit Log, kein Wurf |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| Alle OS-sensitiven `Reporter.E2ETests`-Methoden | Zusätzliches `[OsInterface]`-Attribut |

### E2E-Tests (primärer Funktionsnachweis)

Keine neuen E2E-Tests erforderlich: Der Schritt ändert keine UI-Funktion und keinen Benutzerfluss. Die bestehende FlaUI-Suite wird nur klassifiziert; ihr lokal/on-demand Ausführungsmodell bleibt unverändert.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Nicht erforderlich | Kein geänderter Benutzerfluss | — | — | Keine UI-/Produktverhaltensänderung |

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | OS-Lane ist filterbar | `dotnet test -- --filter-trait "Category=OsInterface"` | Neue Attribute existieren noch nicht | Build, Liste gefilterter Tests | Nur OS-sensitive Tests erscheinen | `--list-tests` |
| 2 | Flaky-Konvention schützt Klassifikation | Neuer Test mit `Flaky`, aber ohne `OsInterface` | Conventionstest vorhanden | Reflection über Testmethoden | Test schlägt mit Methodennamen fehl | `TestClassificationConventionTests` |
| 3 | Cleanup räumt kurz gesperrtes Verzeichnis | `TempDirectoryCleanup.TryDeleteDirectoryAsync` | Temp-Verzeichnis mit gesperrter Datei | Erster Löschversuch, Sperre freigegeben, Retry | Verzeichnis entfernt | `TempDirectoryCleanupTests` |
| 4 | Cleanup bleibt best effort | `TempDirectoryCleanup.TryDeleteDirectoryAsync` | Temp-Verzeichnis mit dauerhaft gesperrter Datei | Wiederholte Löschversuche | Logmeldung, keine Exception | `TempDirectoryCleanupTests` |

## Offene Punkte

Keine.
