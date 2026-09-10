# iOS-Deployment (Notizen)

Dieses Dokument ergaenzt `iOS-Deployment.ps1` und fasst den aktuellen Stand,
offene Probleme sowie moegliche Weiterfuehrungen zusammen.

## Ziel

`iOS-Deployment.ps1` soll ausgehend von Windows (mit Pair-to-Mac) oder direkt
auf einem Mac folgende Aktionen ermoeglichen:

- `build`    : iOS-App bauen (optional mit Codesigning -> `.ipa`)
- `simulator`: iOS-App bauen und im iOS-Simulator starten
- `device`   : iOS-App bauen und auf einem physischen Geraet starten

## Was aktuell funktioniert

### `build` (Windows mit Pair-to-Mac)

Ein unsignierter iOS-Build laesst sich von Windows aus ueber Pair-to-Mac
bauen, sobald `_DotNetRootRemoteDirectory` auf den .NET-Cache der passenden
Visual-Studio-Pair-to-Mac-Version zeigt:

- Visual Studio 2022:
  `/Users/<macOS-Kurzname>/Library/Caches/Xamarin/XMA/SDKs/dotnet/`
- Neuere Pair-to-Mac-Versionen (z. B. VS 2026):
  `/Users/<macOS-Kurzname>/Library/Caches/maui/PairToMac/SDKs/dotnet/`

Der Pfad kann ueber `IOS_MAC_DOTNET_ROOT` oder `-DotNetRootRemoteDirectory`
uebersteuert werden.

Mit `-CodesignKey` und `-CodesignProvision` erzeugt `build` stattdessen
eine `.ipa` via `dotnet publish -p:ArchiveOnBuild=true`.

### `build` / `simulator` / `device` auf dem Mac

Wenn das Skript direkt auf einem Mac lauft, funktionieren alle Aktionen
(außer Pair-to-Mac-Parameter natuerlich nicht noetig).

## Bekannte Probleme und Limitierungen

### `simulator` / `device` auf Windows: `hostpolicy.dll` Fehler

`dotnet build -t:Run` fuer iOS/tvOS wird von Microsoft auf Windows **nicht**
unterstuetzt. Das bewirkt, dass `dotnet` versucht, die iOS-DLL lokal als
.NET-Programm zu starten:

```text
A fatal error was encountered. The library 'hostpolicy.dll' required to
execute the application was not found in '...\net10.0-ios\iossimulator-x64\'.
```

Dies ist ein bekanntes .NET-MAUI/.NET-iOS-Problem. Visual Studio umgeht es,
indem es eine eigene IDE-interne Deployment-Pipeline nutzt (Pair-to-Mac ->
`xcrun simctl` / `mlaunch` auf dem Mac).

### `Microsoft.iOS` wurde nicht gefunden

Wenn `_DotNetRootRemoteDirectory` fehlt, sucht der Build auf dem Mac unter
`/usr/local/share/dotnet/packs/...` nach `Microsoft.iOS.Sdk`. Dort ist das
Workload-Pack typischerweise aber nicht installiert; Visual Studio bzw.
Pair-to-Mac legt es in den oben genannten Cache-Pfaden ab.

### `ServerUser` muss der macOS-Kurzname sein

Pair-to-Mac erwartet den Kurznamen (z. B. `mstromberg`), nicht den
vollstaendigen Anzeigenamen (z. B. `Martin Stromberg`).

### Codesigning

- `build` ohne Codesigning: moeglich, erzeugt aber keine `.ipa`.
- `simulator`: sollte ohne Codesigning funktionieren.
- `device` und `.ipa`-Erzeugung: erfordern ein gueltiges Signing-Zertifikat
  (`CodesignKey`) und ein Provisioning-Profil (`CodesignProvision`).

### Sicherheit / Passwort

Waehrend der Fehlersuche wurde das Pair-to-Mac-Passwort im Terminal-Output
angezeigt. Das Skript maskiert es inzwischen (`***`), das Passwort sollte
trotzdem erneuert werden.

## Moegliche Weiterfuehrung (SSH-basiertes Deployment)

Damit `simulator` / `device` auch auf Windows funktionieren, koennte das
Skript nach einem erfolgreichen `dotnet build` selbst per SSH auf den Mac
wechseln und dort deployen:

1. `.app`-Bundle im Pair-to-Mac-Cache finden, z. B. unter
   `/Users/<user>/Library/Caches/Xamarin/mtbs/builds/<AppName>/<session>/...`
2. Simulator booten: `xcrun simctl boot <udid>`
3. App installieren: `xcrun simctl install booted <app-path>`
4. App starten: `xcrun simctl launch booted <bundle-id>`

Fuer physische Geraete koennte `xcrun devicectl` (Xcode 15+) oder
`ideviceinstaller`/`ios-deploy` genutzt werden.

Voraussetzungen dafuer:
- Passwortloser SSH vom Windows-Rechner zum Mac (Visual Studio legt beim
  ersten Pair-to-Mac SSH-Keys an).
- Oder ein Tool, um das Passwort an `ssh` zu uebergeben (`sshpass`, `plink`).
- Stabile Pfad- und UDID-Behandlung, da der Pair-to-Mac-Cache dynamische
  Session-IDs enthaelt.

Dieser Workaround ist **nicht offiziell unterstuetzt** und koennte bei
.NET-/Xcode-Updates wieder brechen.

## Hilfreiche Links

- [Pair to Mac for iOS development (.NET MAUI)](https://learn.microsoft.com/en-us/dotnet/maui/ios/pair-to-mac)
- [Build an iOS app on macOS with .NET CLI (.NET MAUI)](https://learn.microsoft.com/en-us/dotnet/maui/ios/cli)
- [.NET iOS Known Issues - Launching from the command line is awkward](https://github.com/dotnet/macios/wiki/Known-issues-in-.NET)
