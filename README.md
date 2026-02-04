# WPFDemo

Sample solution that combines a WPF MVVM desktop client with an ASP.NET Core Web API.
It includes async CRUD operations with search/sort, plus unit and integration tests.

## Solution Structure
- `WebApi` — ASP.NET Core Web API (CRUD + search + sort, in-memory repository)
- `WpfClient` — WPF MVVM client that calls the API
- `Shared` — shared DTO/model (`Item`)
- `WebApi.Tests` — unit + integration tests

## Features
- **CRUD**: Create, read, update, delete items
- **Search**: filter by name/description
- **Filtering**: price range and created date range
- **Sorting**: by name, price, or created date
- **Pagination**: page + page size with total count headers
- **Async/await**: repository + HTTP client + view model
- **API versioning**: `/api/v1/...`
- **OpenAPI docs**: Swagger UI with examples
- **WPF UX**: validation, price input mask, loading indicator, error dialog, paging UI, grid column sorting
- **Security**: JWT auth with roles, protected endpoints
- **Feature flags**: App Configuration feature management (e.g., disable delete)
- **Rate limiting**: fixed window limiter with configurable limits
- **CORS**: configurable allowed origins
- **Testing**: repository unit tests + API integration tests
- **CI/CD**: GitHub Actions build/test + Azure App Service deploy
- **Azure config**: App Configuration + Key Vault hooks wired in

## Prerequisites
- .NET SDK 9.0+
- (Optional) Azure CLI for local Key Vault/App Configuration auth

## Run the API
```bash
dotnet run --project WebApi
```

The API uses the `https` profile by default (see `WebApi/Properties/launchSettings.json`).
Default HTTPS URL: `https://localhost:7042/`

### API Endpoints (v1)
- `GET /api/v1/items` (query: `search`, `sortBy`, `sortDir`, `minPrice`, `maxPrice`, `fromCreated`, `toCreated`, `page`, `pageSize`)
- `GET /api/v1/items/{id}`
- `POST /api/v1/items`
- `PUT /api/v1/items/{id}`
- `DELETE /api/v1/items/{id}`
- `POST /api/v1/auth/token` (issue demo JWT)

Example:
```
GET /api/v1/items?search=mouse&sortBy=price&sortDir=asc&minPrice=10&maxPrice=200&page=1&pageSize=20
```

### Pagination Headers
Each `GET /api/v1/items` response includes:
- `X-Total-Count`: total items after filtering
- `Page`: current page
- `PageSize`: page size

## Security (JWT + Roles)
The API uses JWT Bearer authentication.

Protected endpoints:
- `POST /api/v1/items`
- `PUT /api/v1/items/{id}`
- `DELETE /api/v1/items/{id}`

Roles:
- `AdminOnly` policy required for create/update/delete.

### Get a token (demo)
```bash
POST /api/v1/auth/token
{
  "userName": "admin",
  "password": "Admin@123"
}
```

Use the token in requests:
```
Authorization: Bearer <token>
```

### Dummy Users (for testing)
Configured in `appsettings*.json` under `Users`:
- `admin / Admin@123` (Role: Admin)
- `user / User@123` (Role: User)

## Run the WPF client
```bash
dotnet run --project WpfClient
```

The WPF app expects the API at `https://localhost:7042/`.
You can change this in `WpfClient/ViewModels/MainViewModel.cs` if needed.

### WPF UX Enhancements
- **Validation**: `IDataErrorInfo` for required name, non-negative price, and page size bounds.
- **Price mask**: restricts input to numeric values with up to 2 decimals.
- **Loading indicator**: progress bar shown while API calls are running.
- **Error dialog**: API errors pop a dialog (in addition to status bar).
- **Paging UI**: next/prev buttons, total count, and page size selector.
- **Grid sorting**: click column headers to sort server-side.
- **Login panel**: username + password to request/store JWT automatically.

## Tests
```bash
dotnet test
```

- Unit tests: `WebApi.Tests/RepositoryTests.cs`
- Integration tests: `WebApi.Tests/ItemsApiTests.cs`
- Integration tests use `appsettings.Test.json` with a non-placeholder `Jwt:Key`.

## Configuration
### Base settings
`WebApi/appsettings.json` holds defaults and the shape of configuration.

### Environment-specific settings
- `WebApi/appsettings.Development.json`
- `WebApi/appsettings.Test.json`
- `WebApi/appsettings.Production.json`

### Azure App Configuration + Key Vault
These are wired in `WebApi/Program.cs` using `DefaultAzureCredential`.

Recommended environment variables:
- `AzureAppConfiguration__ConnectionString`
- `Azure__KeyVaultUri`

Example (PowerShell):
```powershell
$env:AzureAppConfiguration__ConnectionString = "<connection-string>"
$env:Azure__KeyVaultUri = "https://<keyvault-name>.vault.azure.net/"
```

### Feature Flags
Feature flags are loaded from Azure App Configuration.
Example flag used in the API:
- `FeatureManagement:EnableDelete` (disables `DELETE /items` when false)

### CORS
Configured via `Cors:AllowedOrigins`:
```
"Cors": {
  "AllowedOrigins": [ "https://example.com" ]
}
```

### Rate Limiting
Fixed window limiter configured via:
```
"RateLimiting": {
  "PermitLimit": 60,
  "WindowSeconds": 60,
  "QueueLimit": 0
}
```

## OpenAPI / Swagger
In Development, Swagger UI is available at:
- `https://localhost:7042/swagger`

The API includes example requests/responses for `Item` and item lists.

## CI/CD (GitHub Actions)
Workflow: `.github/workflows/ci-cd.yml`

Pipeline:
- Restore, build, test
- Publish `WebApi`
- Deploy to Azure App Service (Dev/Test)
- Manual deploy to Prod (`workflow_dispatch`)

Required GitHub secrets:
- `AZURE_WEBAPP_NAME_DEV`
- `AZURE_WEBAPP_PUBLISH_PROFILE_DEV`
- `AZURE_WEBAPP_NAME_TEST`
- `AZURE_WEBAPP_PUBLISH_PROFILE_TEST`
- `AZURE_WEBAPP_NAME_PROD`
- `AZURE_WEBAPP_PUBLISH_PROFILE_PROD`

## Notes
- The API uses an in-memory repository; data resets on restart.
- `DefaultAzureCredential` works locally with `az login` and in Azure via Managed Identity.

