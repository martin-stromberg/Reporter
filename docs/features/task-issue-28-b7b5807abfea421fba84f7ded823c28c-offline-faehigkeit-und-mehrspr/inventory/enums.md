# Enums und statusbildende Konstanten — Bestandsaufnahme

Für die Anforderung relevante Enums bzw. enum-ähnliche Konstanten. **Ein Enum oder eine Konstantenklasse für den Netzwerkstatus existiert nicht** (`Connectivity`, `NetworkAccess`, `IConnectivity` kommen in `src/` nicht vor).

## `NotificationAuthorizationStatus`

Datei: `src/Reporter.Core/Models/NotificationAuthorizationStatus.cs`

Einziger echter Enum im relevanten Umfeld; zeigt das Muster für Status-Enums in `Reporter.Core.Models`.

| Wert | Bedeutung |
|------|-----------|
| `Unsupported` | Plattform unterstützt keine lokalen Benachrichtigungen |
| `NotDetermined` | Berechtigung noch nicht angefragt |
| `Denied` | In den Systemeinstellungen verweigert |
| `Authorized` | Erteilt (inkl. provisional/ephemeral) |

## `FeedHealth` (statische Konstantenklasse, kein Enum)

Datei: `src/Reporter.Core/Services/FeedHealth.cs`

String-Konstanten für den Sync-/Feed-Gesundheitsstatus (werden in `Feed.HealthStatus` und `SyncLog.Status` persistiert).

| Wert | Bedeutung |
|------|-----------|
| `Ok` (`"OK"`) | Feed/Sync fehlerfrei |
| `Warning` (`"Warning"`) | Warnung (z. B. deutlich weniger Items, >30 Tage ohne neue Items) |
| `Error` (`"Error"`) | Fehler (u. a. Netzwerkfehler beim Sync) |

Hilfsmethode: `Changed(string? current, string? next)` (Zeile 29) — Vergleich für `HealthLastChange`-Pflege.

## `SettingsValues` (statische Konstantenklasse, kein Enum)

Datei: `src/Reporter.Core/Models/SettingsValues.cs`

Persistierte String-Werte — siehe [models.md](models.md). Für den optionalen manuellen Sprachwechsel wären hier `Language*`-Konstanten anzulegen; aktuell existieren nur `AutoMarkRead*` und `Theme*`.
