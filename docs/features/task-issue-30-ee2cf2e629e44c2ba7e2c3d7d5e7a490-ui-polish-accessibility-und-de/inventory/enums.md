<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Enums / Konstanten

## `FeedHealth`
Datei: `src/Reporter.Core/Services/FeedHealth.cs`

Kein `enum`, sondern eine statische Klasse mit String-Konstanten (`Feed.HealthStatus`/`FeedListItem.HealthStatus` sind `string?`). Die `DataTrigger` auf `FeedsPage.xaml` vergleichen gegen die Literalwerte `"OK"`, `"Warning"`, `"Error"`.

| Wert | Bedeutung |
|------|-----------|
| `Ok = "OK"` | Feed fehlerfrei synchronisiert |
| `Warning = "Warning"` | Warnung (z. B. deutlich weniger Items abgerufen oder >30 Tage ohne neue Items — s. `FeedSyncService.DetermineStatus`) |
| `Error = "Error"` | Sync-Fehler (Exception, Feed nicht gefunden, offline) |

Hilfsmethode: `FeedHealth.Changed(current, next)` — Vergleich für `HealthLastChange`-Pflege in `FeedSyncService.UpdateFeedHealthAsync`.

## `SettingsValues`
Datei: `src/Reporter.Core/Models/SettingsValues.cs`

Statische Konstanten für persistierte Settings-Werte.

| Wert | Bedeutung |
|------|-----------|
| `AutoMarkReadOnOpen = "on_open"` | Auto-Mark beim Öffnen |
| `AutoMarkReadOnScroll = "on_scroll"` | Auto-Mark beim Scrollen (Legacy-Seed) |
| `AutoMarkReadOff = "off"` | Auto-Mark deaktiviert |
| `ThemeSystem = "system"` | Theme folgt OS |
| `ThemeLight = "light"` | Light-Theme |
| `ThemeDark = "dark"` | Dark-Theme |
| `LanguageSystem = "system"` | Sprache folgt OS |
| `LanguageGerman = "de"` | Deutsch |
| `LanguageEnglish = "en"` | Englisch |

Hilfsmethode: `IsAutoMarkReadEnabled(mode)` — `true` für alle Modi außer `off`.

## `FeedSearchMatchKind`
Datei: `src/Reporter.Core/Models/FeedSearchMatchKind.cs`

| Wert | Bedeutung |
|------|-----------|
| `ExactUrl` | Exakter URL-Treffer / eingegebene URL ist selbst ein Feed-Dokument |
| `Directory` | Treffer aus dem feedsearch.dev-Verzeichnis |
| `Discovered` | Autodiscovery über Link-Tags/well-known Pfade |

Die Deklarationsreihenfolge definiert die Sortierung der Suchtreffer-Karten auf `FeedsPage`.

## `NotificationAuthorizationStatus`
Datei: `src/Reporter.Core/Models/NotificationAuthorizationStatus.cs`

| Wert | Bedeutung |
|------|-----------|
| `Unsupported` | Plattform unterstützt keine lokalen Benachrichtigungen |
| `NotDetermined` | Nutzer wurde noch nicht gefragt |
| `Denied` | Abgelehnt |
| `Authorized` | Erteilt (inkl. provisional/ephemeral) |

Wird in `SettingsPage`/`SettingsViewModel` für die Permission-Banner verwendet.
