# ShipmentManagement (Hillebrand proxy)

Overview
- ASP.NET Core (.NET 8) API that proxies Hillebrand Gori v6 shipments endpoints.
- Uses OAuth 2.0 Resource Owner Password Credentials flow to obtain an access token.

Getting started (local, safe)
1. Copy example config:
   cp src/ShipmentManagement/appsettings.json.example src/ShipmentManagement/appsettings.json

2. Do not put credentials into the repo. Use __dotnet user-secrets__ or env vars:
   cd src/ShipmentManagement
   dotnet user-secrets init
   dotnet user-secrets set "Hillebrand:HGB_CLIENT_ID" "<client-id>"
   dotnet user-secrets set "Hillebrand:HGB_CLIENT_SECRET" "<client-secret>"
   dotnet user-secrets set "Hillebrand:HGB_USERNAME" "<username>"
   dotnet user-secrets set "Hillebrand:HGB_PASSWORD" "<password>"
   dotnet user-secrets set "Hillebrand:HGB_SCOPE" "openid"

3. Run the API:
   dotnet run

4. Run tests:
   cd tests/ShipmentManagement.Tests
   dotnet test

Repository contents to commit
- All source except files listed in .gitignore
- Keep `appsettings.json.example` and `launchSettings.json.example` committed