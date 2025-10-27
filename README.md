# QRTracker

Cross-platform .NET MAUI App (Android, iOS, Windows)

## Voraussetzungen

- .NET SDK 9 (oder kompatibel)
- Installierte MAUI-Workloads (android, ios, maccatalyst, maui-windows)
- Windows: Visual Studio 2022 17.13+ mit MAUI-Workload
- Android: SDK, Emulator oder angeschlossenes Device
- iOS: Mac-Buildhost/Xcode oder Remote-Build

## Projektstruktur

- Solution: `QRTracker.sln`
- Hauptprojekt: `src/QRTracker/QRTracker.csproj`

## Bauen & Starten

- Windows (WinUI)
  - Build: `dotnet build src/QRTracker/QRTracker.csproj -c Debug -f net9.0-windows10.0.19041.0`
  - Start: `dotnet build src/QRTracker/QRTracker.csproj -t:Run -f net9.0-windows10.0.19041.0`
- Android: `dotnet build src/QRTracker/QRTracker.csproj -t:Run -f net9.0-android`
- iOS: `dotnet build src/QRTracker/QRTracker.csproj -t:Run -f net9.0-ios`

## Kernfunktionen

- **Einstellungen** (`settings.json` im AppData-Verzeichnis)
  - Firmen-E-Mail, TenantId, ClientId, optional User Hint
  - SharePoint/Excel Ziel (SiteId, DriveId, ItemId, TableName)
- **Authentifizierung (MSAL)**
  - Silent-Sign-In beim Start (falls Cache vorhanden)
  - Automatischer Login-Dialog beim Start ohne aktive Sitzung
  - Nutzer melden sich mit ihrer Firmen-E-Mail an; Tenant-/Client-ID werden anhand der Domain vorbelegt
  - Interaktiver Login-Button bleibt vorhanden
- **Scan/Timer-Flow**
  - Station (S...) und Geraet (G...) eingeben oder via mobilem QR-Scanner erfassen
  - Timer startet bei Scan, endet bei erneutem G-Scan oder Stop-Button
  - Aktivitaet (W/R/P/S) + Notiz abfragen
  - Lokale Historie (`history.json`) und optionaler Graph-Upload
- **Historie**: Liste aller Sessions inkl. Tages-/Wochen-/Monats-/Gesamtsumme

### Tenant-/E-Mail-Zuordnung

- Zuordnungstabelle in `src/QRTracker/Services/TenantConfigProvider.cs`
- Beispiel `example.com` durch echte Firmen-Domains plus TenantId/ClientId/SiteId/DriveId/ItemId/TableName ersetzen
- Nutzer geben nur ihre Firmenadresse ein; notwendige Einstellungen werden gefüllt
- Falls keine Zuordnung existiert, öffnet die App automatisch den Einstellungs-Tab zur manuellen Eingabe

### SharePoint/Excel Upload (Graph)

- Standard-Scopes: `Files.ReadWrite.All`, `Sites.ReadWrite.All`, `offline_access`
- App-Registrierung in Entra ID (Azure AD) notwendig
  - Redirect URIs konfigurieren (`msal{ClientId}://auth` auf Mobile, Systembrowser auf Windows)
  - Delegierte Graph-Berechtigungen hinzufügen
- Tabelle/Datei muss existieren (TableName default `Table1`)

### QR-Scanner auf Mobile

- Paket: `ZXing.Net.MAUI` + `ZXing.Net.MAUI.Controls`
- Android/iOS: Kamera-Scanner im UI eingebettet
- Windows: Codes manuell erfassen (kein Kamera-Scanner)

## CI/CD (GitHub Actions)

- Workflow: `.github/workflows/cicd.yml`
- Trigger: Push auf `main`, Tags `v*`, Pull-Requests, manuell
- Checks
  - `dotnet list package` (vulnerable/deprecated) inkl. JSON-Report als Artefakt
  - Trivy (CRITICAL/HIGH) + Semgrep (SAST) + CodeQL + Dependency Review
  - Gitleaks & Lizenzpruefung (Fehler bei GPL/UNKNOWN)
- Build & Release
  - Self-contained Publish, Upload des `.exe`
  - Automatisches GitHub-Release bei Tags `v*`

## Konfiguration in Kurzform

1. E-Mail-Domain zu Tenant/Client/SharePoint-Ziel in `TenantConfigProvider` hinterlegen
2. App-Registrierung in Entra ID anlegen (Public Client) und Werte eintragen
3. Nutzer starten App, geben Firmen-E-Mail ein und melden sich an
4. Optional: SharePoint IDs im Mapping pflegen (SiteId, DriveId, ItemId, TableName)

## Bekannte Hinweise

- XAML-Compiler warnt fuer Historien-View (fehlendes `x:DataType`); funktional unkritisch
- Bei fehlender Domain-Zuordnung wird der Tab "Einstellungen" geoeffnet, damit Werte manuell ergänzt werden können
