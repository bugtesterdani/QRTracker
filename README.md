# QRTracker

Cross-platform .NET MAUI App (Android, iOS, Windows)

Diese Projektmappe enthält eine .NET MAUI-Anwendung, die auf Android, iOS und Windows läuft. Voraussetzung ist eine korrekt eingerichtete .NET/MAUI-Umgebung.

## Voraussetzungen

- .NET SDK 9 (oder kompatibel)
- MAUI-Workloads installiert: `dotnet workload list` sollte `android`, `ios`, `maccatalyst`, `maui-windows` anzeigen.
  - Falls nötig: `dotnet workload install maui`
- Windows: Visual Studio 2022 17.13+ mit „.NET Multi-platform App UI development“
- Android: Android SDK/Emulator oder ein angeschlossenes Gerät
- iOS: Mac-Buildhost/Xcode für Build & Deploy (Remote-Build von Windows aus möglich)

## Struktur

- Solution: `QRTracker.sln`
- Projekt: `src/QRTracker/QRTracker.csproj`

## Bauen & Starten

- Windows (WinUI):
  - Build: `dotnet build src/QRTracker/QRTracker.csproj -c Debug -f net9.0-windows10.0.19041.0`
  - Starten: `dotnet build src/QRTracker/QRTracker.csproj -t:Run -f net9.0-windows10.0.19041.0`

- Android:
  - Emulator/Device starten/verbinden
  - Starten: `dotnet build src/QRTracker/QRTracker.csproj -t:Run -f net9.0-android`

- iOS:
  - Erfordert Mac-Buildhost und Provisioning
  - Build: `dotnet build src/QRTracker/QRTracker.csproj -c Debug -f net9.0-ios`
  - Starten (mit angegebener Ziel-Geräte-ID, z.B. Simulator): `dotnet build src/QRTracker/QRTracker.csproj -t:Run -f net9.0-ios`

## Hinweise

- Die Standardvorlage zeigt eine einfache Startseite. 
- Paketbezeichner, App-Name/Icons und Berechtigungen können pro Plattform in `Platforms/*` und in `*.csproj` angepasst werden.

## Implementierte Funktionen (QRTracker)

- Einstellungen: `Einstellungen`-Tab. Speichert `settings.json` in `AppDataDirectory`.
  - Azure AD (TenantId, ClientId, optional UPN-Hint)
  - SharePoint/Excel-Ziel (SiteId, DriveId, ItemId, TableName) + Schalter `UseSharePoint`
- Auth mittels MSAL:
  - Silent-Login beim Start (wenn aktiviert und Cache vorhanden)
  - Interaktiver Login-Button auf der Scan-Seite
- Scan/Timer-Flow (Scan-Seite):
  - Station (S...) und Gerät (G...) eingeben oder mit Mobilscanner erfassen
  - Start: Timer läuft; Stop: erneutes Scannen von G oder Button "Stopp"
  - Abfrage Tätigkeit: W/R/P/S und optionale Notiz
  - Speicherung lokal (`history.json`), optional Upload als Excel-Zeile via Graph
- Historie-Ansicht: Liste und Summen pro Tag/Woche/Monat/Gesamt

### SharePoint/Excel Upload (Graph)

- Erforderliche Berechtigungen (Scopes): `Files.ReadWrite.All`, `Sites.ReadWrite.All`, `offline_access`.
- App-Registrierung in Entra ID (Azure AD):
  - `ClientId` notieren
  - Plattform-Redirect URIs setzen:
    - Android/iOS: `msal{ClientId}://auth`
    - Windows: Systembrowser genügt (kein spezielles Redirect notwendig)
  - Delegierte Berechtigungen für Microsoft Graph hinzufügen (oder App-Zugriff nach Bedarf)
- Einstellungen befüllen:
  - `SiteId`, `DriveId`, `ItemId` (Excel-Datei), `TableName` (z.B. `Table1`).
  - Hinweis: Die Tabelle in der Arbeitsmappe muss existieren.

### QR-Scanner auf Mobile

- Paket: `ZXing.Net.MAUI` ist referenziert. Der Scanner wird zur Laufzeit auf Android/iOS initialisiert.
- iOS: `NSCameraUsageDescription` ist hinterlegt.
- Windows: Kein integrierter Scanner; Codes können manuell eingegeben werden.

## CI/CD

- GitHub Actions Workflow: `.github/workflows/cicd.yml`
- Läuft auf Push nach `main`, auf Tags `v*` und manuell via `workflow_dispatch`.
- Schritte
  - `dotnet list package` für Vulnerability- und Deprecated-Checks
  - Lizenzprüfung mit Abbruch bei `GPL/AGPL/LGPL/UNKNOWN`
  - Secret-Scan via `gitleaks`
  - Release-Build (`dotnet publish`, self-contained, Single-File)
- Artefakte
  - GitHub Action lädt `QRTracker.exe` als Build-Artefakt hoch
  - Bei Tags (`v*`) wird automatisch ein Release mit der `.exe` erstellt (Dateiname enthält den Tag)
