# Azure AD (Entra ID) Registrierung für QRTracker

Diese Anleitung zeigt, wie du die erforderlichen Werte **Tenant ID** und **Client ID** für die QRTracker-App erhältst und welche Einstellungen in der Azure App-Registrierung vorgenommen werden sollten.

## Voraussetzungen

- Zugriff auf das Azure-Portal (<https://portal.azure.com/>)
- Rechte zum Erstellen und Verwalten von App-Registrierungen im gewünschten Mandanten (z. B. Rolle "Anwendungsadministrator" oder "Globaler Administrator")

## Schritt 1: Tenant ID ermitteln

1. Melde dich im Azure-Portal an und öffne **Microsoft Entra ID** (vormals Azure Active Directory).
2. Auf der Übersichtsseite findest du im Abschnitt **Mandanteninformationen** die **Mandanten-ID (Tenant ID)**.
3. Kopiere diesen GUID-Wert – er wird später in `TenantConfigProvider` bzw. den App-Einstellungen verwendet.

*Alternative:* Die URL `https://login.microsoftonline.com/<domain>/.well-known/openid-configuration` enthält ebenfalls den Tenant (Feld `issuer`).

## Schritt 2: App-Registrierung anlegen (Client ID)

1. Navigiere in Microsoft Entra ID zu **App-Registrierungen** → **Neue Registrierung**.
2. Vergib einen Namen, z. B. `QRTracker`.
3. Bei **Unterstützte Kontotypen** wähle in der Regel:
   - `Konten in diesem Organisationsverzeichnis (nur <Tenant>)`, wenn nur eure Organisation Zugriff haben soll.
   - `Konten in beliebigen Organisationsverzeichnissen`, falls mehrere Mandanten zugreifen dürfen.
4. Redirect-URI:
   - Für Public-/Desktop-Apps reicht `https://login.microsoftonline.com/common/oauth2/nativeclient`.
   - Für Mobile: später pro Plattform (siehe unten) `msal{ClientId}://auth` hinzufügen.
5. Klicke auf **Registrieren**.
6. Nach dem Anlegen findest du auf der Übersichtsseite die **Anwendungs-ID (Client ID)**. Kopiere diesen GUID-Wert.

## Schritt 3: Redirect URIs konfigurieren

1. In der App-Registrierung → **Authentifizierung**.
2. Unter **Plattformkonfigurationen** füge die benötigten Redirect-URIs hinzu:
   - **Mobile und Desktop** → `msal{ClientId}://auth` (ClientId durch die echte GUID ersetzen) – für Android/iOS.
   - Optional: Zusätzliche URI `https://login.microsoftonline.com/common/oauth2/nativeclient` (falls nicht bereits gesetzt).
3. Aktiviere **Öffentliche Clientflows zulassen (Ressourcenbesitzerpasswort/Device Code)** nur, wenn wirklich benötigt. Für MSAL reicht der Schalter **Mobile & Desktop** (Public Client) -> **Ja**.

## Schritt 4: Microsoft Graph Berechtigungen

1. Registerkarten **API-Berechtigungen** → **Berechtigung hinzufügen** → **Microsoft Graph** → **Delegierte Berechtigungen**.
2. Suche und füge folgende Berechtigungen hinzu (entsprechend den App-Scopes):
   - `Files.ReadWrite.All`
   - `Sites.ReadWrite.All`
   - `offline_access`
3. Danach **Adminzustimmung erteilen** (Schaltfläche „Administratorzustimmung für <Tenant> erteilen“).

## Schritt 5: Optional – SharePoint IDs ermitteln

Für den Excel-Upload benötigst du die IDs von SharePoint Site, Drive und Item (Datei). Das kann z. B. via Graph PowerShell/Graph Explorer erfolgen:

- **Site ID**: GET `https://graph.microsoft.com/v1.0/sites/<domain>:/sites/<SiteName>`
- **Drive ID**: GET `https://graph.microsoft.com/v1.0/sites/<SiteID>/drives`
- **Item ID** (Excel-Datei): GET `https://graph.microsoft.com/v1.0/sites/<SiteID>/drives/<DriveID>/root:/Pfad/zur/Datei.xlsx`

Diese Werte kannst du in `TenantConfigProvider` hinterlegen, sodass sie automatisch mit der Domain verbunden werden.

## Schritt 6: Werte in QRTracker eintragen

1. Öffne `src/QRTracker/Services/TenantConfigProvider.cs`.
2. Ersetze den Beispiel-Eintrag `example.com` durch eure Firmen-Domain und trage TenantId, ClientId sowie ggf. SharePoint-IDs ein:

   ```csharp
   ["firma.de"] = new TenantConfiguration(
       TenantId: "<TENANT_GUID>",
       ClientId: "<CLIENT_GUID>",
       PreferredUserHint: null,
       SiteId: "<SITE_ID>",
       DriveId: "<DRIVE_ID>",
       ItemId: "<ITEM_ID>",
       TableName: "Table1",
       UseSharePoint: true)
   ```

3. Wenn mehrere Domains bedient werden, füge zusätzliche Einträge hinzu.
4. Nutzer geben anschließend nur noch ihre Firmen-E-Mail im Login ein; die App wählt automatisch die passende Konfiguration.

## Sicherheit / Betrieb

- Gib die Client-List geheim nicht an Endnutzer weiter, sondern baue sie in die App ein (oder lade sie verschlüsselt zur Laufzeit nach).
- Admin-Zustimmungen regelmäßig prüfen und ggf. Mannschaftsbetrieb überwachen (Azure Portal → App-Registrierung → Logins).
- Bei Änderungen (z. B. neues Excel-Ziel) muss `TenantConfigProvider` aktualisiert und die App neu verteilt werden.

## Weiterführende Links

- [Microsoft-Dokumentation zu App-Registrierungen](https://learn.microsoft.com/azure/active-directory/develop/quickstart-register-app)
- [MSAL .NET Dokumentation](https://learn.microsoft.com/azure/active-directory/develop/msal-overview)
- [Microsoft Graph Explorer](https://developer.microsoft.com/graph/graph-explorer)
