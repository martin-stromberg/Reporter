<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme – Schritt 3

- 9x `dotnet-version: '10.0.x'` in pr-staging-ci, staging-ci, release,
  security-scan, build-and-package, package-android.
- package-ios: SDK-Band-Pin 10.0.1xx + global.json 10.0.100 +
  Cleanup-Glob net10.0_* + net10.0-ios; Workload-Pin 26.5.10318
  (net10-Band, nicht mehr passend).
- Harte TFMs: net10.0-windows10.0.19041.0 (build-and-package),
  net10.0-android (package-android).
- ci-instructions.md: 4x 10.0.x + 2x --framework net10.0.
- Lokal verifiziertes net11-iOS-Pack: Microsoft.iOS.Sdk.net11.0_27.2
  Version 27.2.11039 (MAUI-Workload GA 11.0.100).
