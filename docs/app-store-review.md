<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# App-Store-Review — Arbeitsnotizen zur Einreichung

Arbeitsdokument für die Einreichung der Reporter-App im iOS App Store. Es
bündelt die Antworten und Begründungen, die App Store Connect bzw. das
App-Review-Team verlangen. Die öffentliche Datenschutzerklärung liegt in
[`docs/privacy-policy.md`](privacy-policy.md).

## Datenschutz-URL

In App Store Connect als Datenschutz-URL (Pflichtfeld, auch bei „keine
Datensammlung") die GitHub-URL von `docs/privacy-policy.md` auf dem
**Default-Branch** des Repositories hinterlegen, z. B.:

```text
https://github.com/<owner>/<repo>/blob/<default-branch>/docs/privacy-policy.md
```

Die Erklärung ist zweisprachig (Deutsch primär, englische Sektion); Kontakt
ist die in der App hinterlegte Adresse `mstromberg84+reporter@gmail.com`
(`DebugReportRecipient` in `Directory.Build.props`).

## App-Datenschutz (Privacy Label) — Antworten

| Frage | Antwort | Begründung |
|-------|---------|------------|
| Sammelt die App Daten? | **Nein** | Alle Inhalte und Einstellungen liegen lokal in SQLite (`ReporterDbContext`); es gibt kein eigenes Backend, keine Telemetrie, kein Tracking. |
| Tracking im Sinne der ATT-Definition? | **Nein** | `NSPrivacyTracking = false` im Privacy-Manifest (`Platforms/iOS/Resources/PrivacyInfo.xcprivacy`); kein ATT-Prompt nötig. |
| Gesammelte Datentypen | **keine** | `NSPrivacyCollectedDataTypes` ist im Manifest ein leeres Array. |
| Required-Reason-APIs | deklariert | `NSPrivacyAccessedAPITypes`: FileTimestamp `C617.1`, SystemBootTime `35F9.1`, DiskSpace `E174.1`, UserDefaults `CA92.1` (.NET-Laufzeit/MAUI berühren diese APIs). |

Hinweis: Der vom Anwender ausgelöste **Debugbericht** (E-Mail an
`mstromberg84+reporter@gmail.com`) ist ein bewusster Versand durch den
Anwender selbst — er fällt unter Apples Ausnahme für „optionaler, vom
Anwender initiierter Kontakt" und ändert die Antwort „keine Datensammlung"
nicht. Inhalt des Berichts siehe Datenschutzerklärung.

## Datenspeicherung / iCloud-Backup (iOS Data Storage Guidelines)

Die App hält ihre lokale SQLite-Ablage in zwei Dateien getrennt:

- `reporter.db` — Nutzerdaten (Abonnements, Kategorien, Stichwort-Filter,
  Einstellungen, Lese- und Merkstatus). **Nicht** vom iCloud-Backup
  ausgeschlossen: Diese Daten sind nicht wiederherstellbar und müssen eine
  Geräte-Wiederherstellung überstehen.
- `reporter-content.db` — re-downloadbare Artikelinhalte und lokal
  gespeicherte Artikelbilder (`item_contents`-Tabelle, Spalten
  `content`/`image_data`/`image_content_type`/`image_url`; Bilder bis 5 MB).
  Vom iCloud-Backup ausgeschlossen
  (`NSUrl.IsExcludedFromBackupKey` inkl. `-wal`-/`-shm`-Sidecars, gesetzt
  durch `BackupExclusionService` in `App.OnStart`): Inhalte und Bilder sind
  online erneut abrufbar und würden sonst nur die iCloud-Quota belasten — das
  entspricht den iOS Data Storage Guidelines (nur nicht reproduzierbare
  Nutzerdaten gehören ins Backup). Nach einem Restore ohne Content-Datei
  lädt der Sync fehlende Inhalte und Bilder automatisch nach
  (Content-/Bild-Backfill im `FeedSyncService`).

## ATS-Begründung (`NSAllowsArbitraryLoads`)

`Platforms/iOS/Info.plist` setzt `NSAppTransportSecurity →
NSAllowsArbitraryLoads = true`. **Begründung für das Review-Team:**

> The app is a user-driven RSS/Atom reader. Users enter arbitrary feed URLs;
> some feed servers and images referenced inside articles are only reachable
> over plain HTTP. `NSExceptionDomains` cannot cover arbitrary, user-defined
> hosts, so `NSAllowsArbitraryLoads` is required for the app to fulfil its
> purpose. All user content stays on the device (local SQLite storage); the
> app itself runs no backend.

(Kurzfassung steht bereits in `docs/help/anwendung/architektur.md` — dieser
Abschnitt ist die für den Review-Dialog bestimmte konsolidierte Fassung.)

## Review-Hinweis: kein Login, Demo-Inhalt

> The app requires **no sign-in and no account**. On first launch it seeds a
> demo category „News" and the Apple Newsroom feed
> (`https://www.apple.com/newsroom/rss-feed.rss`) via `DemoContentService`,
> so the reviewer sees a populated reader immediately. All features can be
> exercised without credentials.

Hintergrund: `FirstRunState.ShouldSeedDemoContent` steuert den einmaligen
Seed in `App.OnStart` (Unterdrückung über `REPORTER_DISABLE_DEMO_SEED` in
E2E-/CI-Läufen).

## Altersfreigabe — Empfehlung

Empfehlung: **12+**, ggf. **17+** je nach Apples Auswertung der Fragen zu
nutzergenerierten/unkontrollierten Inhalten.

- „Unrestricted web access" entfällt: Externe Links im Artikel-WebView
  werden nicht in der App geöffnet, sondern an den System-Browser delegiert
  (`WebViewNavigationGuard.DecideAction` → `CancelAndOpenExternally` →
  `Browser.OpenAsync`, `BrowserLaunchMode.SystemPreferred`).
- Dennoch bleiben anwenderbestimmte Feed-Inhalte fachlich nicht
  kontrollierbar — die Fragen zu „user-generated content" /
  „uncontrolled content" wahrheitsgemäß **bejahen**.
- Die finale Einstufung obliegt dem Einreichenden.

## iPad-Entscheidung (beschlossen: Variante B)

**Beschluss:** iPad ist in dieser Einreichung **kein** Target —
`UIDeviceFamily` in `Platforms/iOS/Info.plist` steht auf `[1]` (nur iPhone)
und `UISupportedInterfaceOrientations~ipad` wurde entfernt.

**Begründung:** Ohne verifizierbaren macOS-/iPad-Zugriff wäre ungeprüfter
iPad-Support (v. a. `DisplayActionSheetAsync`-Popover in `FeedsPage`/
`CategoriesPage`) das größere Review-Risiko. Die App ist noch nicht
veröffentlicht; `2` kann mit durchgeführter iPad-Verifikation später
zurückkehren.

**Konsequenz in App Store Connect:** Es werden nur iPhone-Screenshots
benötigt (keine iPad-13″-Pflicht).

## App-Icon-Verifikation (ITMS-90717)

Apple lehnt App-Icons mit Alpha-Kanal/Transparenz ab (ITMS-90717).

- **Generiertes Asset:** Die Prüfung des generierten PNG in
  `Assets.xcassets` erfordert einen iOS-Build auf macOS und steht dort als
  Folgeschritt aus (`scripts/iOS-Deployment.ps1 -Action build`).
- **Statische Verifikation der Quellen (durchgeführt):**
  - `src/Reporter/Resources/AppIcon/appicon.svg` — einziges Element ist
    `<rect fill="#1e293b">` über die volle 456 × 456-Fläche: komplett
    opaker Hintergrund, kein Alpha-Kanal in der Quelle.
  - `src/Reporter/Resources/AppIcon/appiconfg.svg` — Vordergrund mit
    `fill="none"` am Root; transparente Bereiche werden von der
    `MauiIcon`-Fläche gedeckt.
  - `src/Reporter/Reporter.csproj` Zeile 60 —
    `<MauiIcon … Color="#1e293b" />` setzt den opaken Hintergrund beim
    Generieren der `Assets.xcassets`-PNGs.

**Ergebnis:** Die Quellen garantieren ein Alpha-freies Icon; die
Abschlussprüfung am generierten PNG ist dokumentiert als macOS-Folgeschritt.

## Upload-Tooling (`iTMSTransporter`)

`xcrun altool` (deprecated) wurde in `scripts/iOS-Deployment.ps1` durch
`iTMSTransporter` ersetzt: Upload per `-m upload`, Remote-Validierung
per `-m verify`. Beide nutzen dieselbe API-Key-Authentifizierung
(`-apiKey`/`-apiIssuer`) und denselben Schlüsselsuchpfad
`~/.appstoreconnect/private_keys/` — `Copy-ApiKeyToMac` bleibt unverändert.
Seit Xcode 16 liefert Xcode `iTMSTransporter` nicht mehr mit; das Skript
löst das Binary aus der Transporter-App
(`/Applications/Transporter.app/Contents/itms/bin/iTMSTransporter`) mit
`xcrun -f`-Fallback für ältere Xcode-Versionen auf. Fehlt das Werkzeug,
bricht der Upload mit Installationshinweis ab; `-m verify` entfällt dann
dokumentiert mit Warnung — die lokale
`codesign`-/`embedded.mobileprovision`-Prüfung läuft weiter, und der
Upload validiert serverseitig.
