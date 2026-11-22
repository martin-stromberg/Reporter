<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Anforderung – Schritt 3: CI/CD auf .NET 11 (Phase 4)

1. Alle `dotnet-version: '10.0.x'` → `'11.0.x'` in Workflows/Actions.
2. `package-ios/action.yml`: Workload-Pin `27.2.11039` (net11.0_27.2,
   Release-Xcode-kompatibel, lokal verifiziertes Pack), SDK-Band
   `11.0.1xx`, temporaere global.json `11.0.100`, Pack-Cleanup-Glob
   `Microsoft.iOS.Sdk.net11.0_*`, Framework `net11.0-ios`.
3. `build-and-package`/`package-android`: TFM net11.0.
4. ci-instructions.md: Beispiele auf net11.0.
5. setup-dotnet@v6 loest 11.0.x auf (GA-SDK vorhanden);
   E2E-ASP.NET-Shared-Framework: E2ETests projektieren net11.0-windows,
   AspNetCore-Abhaengigkeit nicht vorhanden (Pruefung);
   MTP-Flags unveraendert (in Schritt 1 verifiziert).
