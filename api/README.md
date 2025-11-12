# Finshark — ASP.NET Core Web API

This repository contains an ASP.NET Core 9.0 Web API for the Finshark project. The API exposes endpoints for stocks, comments and user portfolios and uses ASP.NET Core Identity + JWT for authentication. This README documents how the project is organized, how components connect, how to run it locally and explains key files and folders.

## Quick start

Prerequisites
- .NET 9 SDK
- SQL Server / LocalDB (configured in appsettings.json)
- Optional: Postman / curl / VS Code REST Client

Run locally
1. Restore and build:
   ```ps
   dotnet restore
   dotnet build
   ```
2. Apply migrations (if DB not created):
   ```ps
   dotnet tool install --global dotnet-ef --version 9.0.10
   dotnet ef database update --project api
   ```
3. Run:
   ```ps
   dotnet run --project api
   ```
4. API listens on the URLs specified in `api/Properties/launchSettings.json`. Use Swagger (in Development) or `api/api.http` to test.

## Configuration

Files
- `appsettings.json` — connection string, FMPKey, JWT settings (Issuer, Audience, SigningKey).
- `appsettings.Development.json` — development overrides.
- `Properties/launchSettings.json` — local launch URLs.

Update `JWT:SigningKey` and `Issuer/Audience` to match token generation. The app consumes configuration in `Program.cs`.

## Authentication & Authorization

- Uses ASP.NET Core Identity + JWT Bearer.
- Login endpoint returns a JWT created by `Service/TokenService.cs`.
- Protected endpoints require header `Authorization: Bearer <token>`.
- Controllers use `[Authorize]` where protection is required.

Authentication flow
1. POST `/api/account/login` with `LoginDto`.
2. Controller validates via `SignInManager` and `UserManager`.
3. `TokenService.CreateToken` generates JWT returned to client.
4. Include token in protected requests.

## Database & EF Core

- DbContext: `Data/ApplicationDBContext.cs`
  - Configures identity tables and application entities: `Stocks`, `Comments`, `Portfolios`.
  - Configures many-to-many relationship between users and stocks for portfolios.
- Design-time factory: `Data/ApplicationDBContextFactory.cs` (used by `dotnet ef`).
- Migrations: `Migrations/` folder (examples: `20251110025811_PortfolioManyToMany.cs`, ...).

## Folder & File Overview

Top-level
- `Program.cs` — app startup, service registration, authentication, middleware and routing.
- `appsettings.json` — environment configuration.
- `Properties/launchSettings.json` — local launch profiles.

Controllers (HTTP endpoints)
- `Controllers/AccountController.cs` — register & login.
  - Endpoints: `POST /api/account/register`, `POST /api/account/login`.
- `Controllers/StockController.cs` — stock CRUD.
  - Endpoints: `GET /api/stock`, `GET /api/stock/{id}`, `POST /api/stock`, `PUT /api/stock/{id}`, `DELETE /api/stock/{id}`.
- `Controllers/CommentController.cs` — comments management.
  - Endpoints: `GET /api/comment`, `GET /api/comment/{id}`, `POST /api/comment/{symbol}`, `PUT /api/comment/{id}`, `DELETE /api/comment/{id}`.
- `Controllers/PortfolioController.cs` — user portfolio (protected).
  - Endpoints: `GET /api/portfolio`, `POST /api/portfolio`, `DELETE /api/portfolio`.

Data
- `Data/ApplicationDBContext.cs` — EF Core DbContext, entity configuration.
- `Data/ApplicationDBContextFactory.cs` — design-time factory for migrations.

Models
- `Models/AppUser.cs` — extends IdentityUser for application-specific fields.
- `Models/Stock.cs` — stock entity.
- `Models/Comment.cs` — comment entity (one-to-one or one-to-many depending on design).
- `Models/Portfolio.cs` — join entity (user-stock portfolio).

DTOs
- `Dtos/Account/` — `LoginDto`, `RegisterDto`, `NewUserDto`.
- `Dtos/Stock/` — `CreateStockRequestDto`, `UpdateStockRequestDto`, `StockDto`, `FMPStock`.
- `Dtos/Comment/` — `CreateCommentDto`, `UpdateCommentRequestDto`, `CommentDto`.

Repositories (data access)
- `Repository/StockRepository.cs` — implements `Interfaces/IStockRespository`.
- `Repository/CommentRepository.cs` — implements `Interfaces/ICommentRepository`.
- `Repository/PortfolioRepository.cs` — implements `Interfaces/IPortfolioRepository`.

Services
- `Service/FMPService.cs` — external FinancialModelingPrep API client (uses `FMPKey`).
  - Fetches stock data by symbol, maps to local DTOs.
- `Service/TokenService.cs` — builds JWT tokens using configured signing key, issuer and audience.

Helpers & Extensions
- `Helpers/QueryObject.cs`, `Helpers/CommentQueryObject.cs` — query/filter DTOs used by listing endpoints.
- `Extensions/ClaimsExtensions.cs` — helper to read username/userid from claims.
- `Mappers/` — extension methods to map between models and DTOs (StockMappers, CommentMappers).

Other
- `api.http` — VS Code REST client sample requests.
- `Migrations/` — EF Core migrations folder.

## How major pieces connect

- `Program.cs` wires up services into DI:
  - Adds controllers, Swagger, JSON options, EF DbContext.
  - Registers implementations:
    - `IStockRespository -> StockRepository`
    - `ICommentRepository -> CommentRepository`
    - `IPortfolioRepository -> PortfolioRepository`
    - `IFMPService -> FMPService`
    - `ITokenService -> TokenService`
- Controllers receive repositories/services via constructor injection and orchestrate request handling:
  - Example: `PortfolioController` extracts the username from claims (via `ClaimsExtensions`) and asks `IPortfolioRepository` for user portfolio.
- External integration:
  - `FMPService` uses an injected `HttpClient` and `FMPKey` to call FinancialModelingPrep endpoints, map responses and return DTOs/entities.
- Authentication:
  - `AccountController` uses `UserManager`/`SignInManager` to validate credentials and `TokenService` to create JWTs.

## Endpoints (high level)

- Account
  - POST /api/account/register — register new user
  - POST /api/account/login — login and receive JWT
- Stock
  - GET /api/stock — list (may be protected)
  - GET /api/stock/{id}
  - POST /api/stock
  - PUT /api/stock/{id}
  - DELETE /api/stock/{id}
- Comment
  - GET /api/comment
  - GET /api/comment/{id}
  - POST /api/comment/{symbol}
  - PUT /api/comment/{id}
  - DELETE /api/comment/{id}
- Portfolio
  - GET /api/portfolio — returns authenticated user portfolio
  - POST /api/portfolio — add stock to authenticated user portfolio
  - DELETE /api/portfolio — remove stock from authenticated user portfolio

Note: Portfolio `POST`/`DELETE` currently accept a `string symbol` parameter in controller signatures. If calling from browser forms or AJAX, ensure model binding is correct — add route templates like `[HttpPost("{symbol}")]` or use `[FromBody]` DTO.

## Troubleshooting

Cannot access endpoints from browser
- Protected endpoints require JWT; unauthenticated requests return 401. Obtain token via `/api/account/login` and include header `Authorization: Bearer <token>`.
- Confirm `Program.cs` reads the same JWT settings (`Issuer`, `Audience`, `SigningKey`) as `appsettings.json`.
- Check that the app is listening on the expected port/URL from `Properties/launchSettings.json`.
- If calling from a different origin (frontend), configure CORS in `Program.cs`.
- If browser shows 404, verify route templates and parameter binding — some endpoints accept primitive parameters without route templates which may not bind from query/body as expected.

Database errors
- Verify connection string in `appsettings.json` and ensure SQL Server/LocalDB is running.
- Run migrations: `dotnet ef database update --project api`.

FMP API issues
- Ensure `FMPKey` is present and valid in configuration.
- Check `FMPService` for endpoint URLs and mapping logic.

## Useful commands / examples

Register, login and call protected endpoint (example using curl)
1. Register
   ```sh
   curl -X POST http://localhost:5000/api/account/register -H "Content-Type: application/json" -d '{"username":"test","email":"a@a.com","password":"P@ssw0rd!"}'
   ```
2. Login
   ```sh
   curl -X POST http://localhost:5000/api/account/login -H "Content-Type: application/json" -d '{"username":"test","password":"P@ssw0rd!"}'
   ```
   - Save `token` from response.
3. Call protected endpoint
   ```sh
   curl -H "Authorization: Bearer <token>" http://localhost:5000/api/portfolio
   ```

## Notes & next steps

- Consider changing portfolio endpoints to use explicit route templates (e.g. `[HttpPost("{symbol}")]`) to make calling them from a browser or simple curl commands straightforward.
- Confirm Swagger is enabled in Development for UI testing of protected endpoints (use Swagger "Authorize" button to set the JWT).
- Add sample `.env` or secrets guidance for JWT signing key and `FMPKey`.

## Key files quick links

- `Program.cs` — startup & DI
- `appsettings.json` — configuration
- `Controllers/` — API endpoints
- `Data/ApplicationDBContext.cs` — EF DbContext
- `Service/FMPService.cs` — external API client
- `Service/TokenService.cs` — JWT creation
- `Repository/` — data layer
- `Migrations/` — EF migrations

## Dependencies & Packages to install

Prerequisites (system)
- .NET 9 SDK — verify with:
  ```ps
  dotnet --version
  ```
- SQL Server / LocalDB accessible for the ConnectionStrings:DefaultConnection.
- Optional: Postman or VS Code REST Client extension to run `api/api.http`.

Global tools
- EF Core CLI:
  ```ps
  dotnet tool install --global dotnet-ef --version 9.0.10
  ```

Recommended NuGet packages (install into the `api` project if not present)
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.EntityFrameworkCore.Tools
- Microsoft.AspNetCore.Identity.EntityFrameworkCore
- Microsoft.AspNetCore.Authentication.JwtBearer
- Swashbuckle.AspNetCore (Swagger)
- Microsoft.Extensions.Http (for IHttpClientFactory)

Install commands (run from repo root)
```ps
dotnet restore
cd api
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore
dotnet add package Microsoft.Extensions.Http
```

Optional developer tooling
- VS Code (recommended) + C# extension
- REST Client extension (for `api/api.http`)
- SQL Server Management Studio or Azure Data Studio to inspect the DB
- (Optional) dotnet user-secrets for storing JWT signing key and FMPKey during development:
  ```ps
  dotnet user-secrets init
  dotnet user-secrets set "JWT:SigningKey" "<your-key>"
  dotnet user-secrets set "FMPKey" "<your-fmp-key>"
  ```

