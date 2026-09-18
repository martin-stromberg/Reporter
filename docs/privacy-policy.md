<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenschutzerklärung — Reporter

**Stand:** September 2026

Diese Datenschutzerklärung beschreibt, welche Daten die App „Reporter"
(RSS-/Atom-Feed-Reader) verarbeitet, wohin sie fließen und welche Rechte du
hast. Sie gilt für die iOS-App; sie ist öffentlich über die GitHub-URL dieses
Dokuments auf dem Default-Branch des Repositories erreichbar und in App Store
Connect als Datenschutz-URL hinterlegt.

## 1. Verantwortlicher und Kontakt

Verantwortlich für die Datenverarbeitung in der App ist der Betreiber des
Repositories, erreichbar über die in der App für Debugberichte hinterlegte
Kontaktadresse:

**E-Mail:** mstromberg84+reporter@gmail.com

## 2. Grundsatz: Lokale Datenspeicherung

Reporter ist ein lokaler Feed-Reader **ohne eigenes Backend und ohne
Benutzerkonto**. Alle abonnierten Feeds, Kategorien, Artikel, Stichwort-Filter
und Einstellungen liegen ausschließlich in einer lokalen SQLite-Datenbank
(`reporter.db`) auf deinem Gerät. Unter iOS ist die Datenbankdatei vom
iCloud-Backup ausgeschlossen — die Inhalte verlassen dein Gerät dadurch auch
nicht indirekt über ein Cloud-Backup.

Die App erhebt **keine** Telemetrie, enthält **kein** Tracking, **keine**
Analyse-SDKs und **keine** Werbung. Es findet kein Profiling statt.

## 3. Netzwerkzugriffe beim normalen Betrieb

Damit die App ihre Aufgabe erfüllen kann, greift sie auf Server zu, die **du**
durch deine Feed-Auswahl bestimmst:

| Zweck | Empfänger | Übertragene Daten |
|-------|-----------|-------------------|
| Feed-Abruf (Synchronisation) | Die von dir abonnierten Feed-Server (beliebige Hosts, HTTP oder HTTPS) | Deine IP-Adresse; keine Inhalte von dir |
| Feed-Suche | `feedsearch.dev` (Verzeichnis-API) sowie die von dir eingegebene Website und deren Standard-Feedpfade (Autodiscovery) | Dein Suchbegriff / die eingegebene URL sowie deine IP-Adresse |
| Feed-Symbole (Favicons) | Die Websites der abonnierten Feeds | Deine IP-Adresse; keine Inhalte von dir |
| Artikelbilder und externe Links | Die in den Artikeln referenzierten Server der jeweiligen Anbieter | Deine IP-Adresse; keine Inhalte von dir |

Bei jedem dieser Abrufe erhält der jeweilige Fremdserver zwangsläufig deine
IP-Adresse — wie bei jedem Besuch dieser Server in einem Browser. Reporter
übermittelt darüber hinaus keine personenbezogenen Daten an diese Server.

## 4. Debugbericht (nur auf deine ausdrückliche Anfrage)

Auf der Seite **Einstellungen → Diagnose & Support** kannst du einen
Debugbericht erzeugen. Dieser wird als vorbefüllter **E-Mail-Entwurf** im
Mail-Client deines Geräts geöffnet und **nur versendet, wenn du ihn selbst
absendest** — an die Support-Adresse `mstromberg84+reporter@gmail.com`.

Der Bericht enthält: App- und Geräteinformationen (App-Version,
Gerätemodell, Hersteller, Plattform, OS-Version), den Online-Status, einen
Abzug deiner Einstellungen, die Liste deiner Feeds **inklusive deren URLs**
sowie die jüngsten Sync- und Session-Logeinträge. Vor dem Absenden siehst du
den vollständigen Inhalt im Mail-Entwurf und kannst ihn prüfen oder verwerfen.

Die optionale **Debug-Sammlung** (Schalter in den Einstellungen,
voreingestellt aus) schreibt Diagnoseeinträge in die lokale Datenbank; sie
werden nur mit einem von dir versendeten Debugbericht weitergegeben.

## 5. Datenübermittlung in Drittländer

Die in Abschnitt 3 genannten Server können weltweit verteilt stehen; durch die
Nutzung der jeweiligen Feed-Inhalte findet eine Übermittlung deiner IP-Adresse
an diese Server statt. Eine darüber hinausgehende Übermittlung personenbezogener
Daten findet nicht statt.

## 6. Deine Rechte

Da Reporter keine Daten auf eigenen Servern speichert, beschränken sich
Auskunft, Berichtigung und Löschung auf die lokale Datenbank deines Geräts:
Das Löschen der App entfernt alle lokal gespeicherten Daten. Gesendete
Debugbericht-E-Mails kannst du über die Kontaktadresse löschen lassen.

---

# Privacy Policy — Reporter (English summary)

**Effective:** September 2026

This privacy policy describes which data the “Reporter” app
(RSS/Atom feed reader) processes, where it flows, and which rights you have.
It is publicly reachable via the GitHub URL of this document on the
repository's default branch and registered in App Store Connect as the
privacy URL. For the full legal text, see the German section above; the
English version below is a faithful summary.

## 1. Controller and contact

The operator of the repository is responsible for data processing in the app
and can be reached at the contact address configured for debug reports:

**Email:** mstromberg84+reporter@gmail.com

## 2. Principle: local storage only

Reporter is a local feed reader **without its own backend and without a user
account**. All subscribed feeds, categories, articles, keyword filters and
settings are stored exclusively in a local SQLite database (`reporter.db`) on
your device. On iOS the database file is excluded from iCloud backup, so your
content does not leave your device via a cloud backup either.

The app collects **no** telemetry, contains **no** tracking, **no** analytics
SDKs and **no** advertising. No profiling takes place.

## 3. Network access during normal operation

To do its job the app contacts servers that **you** determine through your
feed selection:

| Purpose | Recipient | Data transmitted |
|---------|-----------|------------------|
| Feed sync | The feed servers you subscribed to (arbitrary hosts, HTTP or HTTPS) | Your IP address; none of your content |
| Feed search | `feedsearch.dev` (directory API) plus the website you typed and its standard feed paths (autodiscovery) | Your search term / the URL you entered, plus your IP address |
| Feed icons (favicons) | The websites of subscribed feeds | Your IP address; none of your content |
| Article images and external links | The servers referenced inside articles | Your IP address; none of your content |

Each of these requests inevitably exposes your IP address to the respective
third-party server — exactly as visiting those servers in a browser would.
Reporter transmits no personal data beyond that.

## 4. Debug report (only on your explicit request)

Under **Settings → Diagnostics & Support** you can generate a debug report.
It opens as a pre-filled **email draft** in your device's mail client and is
sent **only if you press send yourself** — to the support address
`mstromberg84+reporter@gmail.com`.

The report contains: app and device information (app version, device model,
manufacturer, platform, OS version), online status, a snapshot of your
settings, the list of your feeds **including their URLs**, and the latest
sync and session log entries. You can review the full content in the draft
before sending or discard it.

The optional **debug collection** switch (off by default) writes diagnostic
entries to the local database; they leave the device only inside a debug
report you send.

## 5. Transfers to third countries

The servers listed in section 3 may be located worldwide; using the
respective feed content transmits your IP address to those servers. No
further transfer of personal data takes place.

## 6. Your rights

Since Reporter stores no data on its own servers, access, rectification and
erasure are limited to the local database on your device: deleting the app
removes all locally stored data. Debug report emails you sent can be deleted
on request via the contact address.
