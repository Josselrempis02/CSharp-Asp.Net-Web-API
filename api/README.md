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

## Folder & File Overview with detailed explanations

### Top-level files
- **`Program.cs`** — app startup, service registration, authentication, middleware and routing.
  - Registers all services (repositories, services, DbContext).
  - Configures JWT authentication and Swagger.
  - Adds CORS, JSON serialization options.
  - Configures ASP.NET Core Identity.

- **`appsettings.json`** — environment configuration.
  - Connection string to SQL Server/LocalDB.
  - FMPKey for external API.
  - JWT signing key, issuer, audience.
  
- **`Properties/launchSettings.json`** — local launch profiles.
  - Defines URLs and ports (http://localhost:5000, https://localhost:5001).
  - Environment variables.

---

### Controllers folder
**Purpose:** HTTP endpoints that handle incoming requests.

Files
- **`Controllers/AccountController.cs`** — user registration & login.
  - `POST /api/account/register` — creates new user via `UserManager`.
  - `POST /api/account/login` — validates credentials, returns JWT via `TokenService`.
  - Uses `SignInManager` to authenticate.

- **`Controllers/StockController.cs`** — stock CRUD operations.
  - `GET /api/stock` — list all stocks (with optional filtering via `QueryObject`).
  - `GET /api/stock/{id}` — fetch single stock.
  - `POST /api/stock` — create new stock (accepts `CreateStockRequestDto`).
  - `PUT /api/stock/{id}` — update stock (accepts `UpdateStockRequestDto`).
  - `DELETE /api/stock/{id}` — delete stock.
  - Uses `IStockRespository` for data access.

- **`Controllers/CommentController.cs`** — comment management.
  - `GET /api/comment` — list comments (with filtering via `CommentQueryObject`).
  - `GET /api/comment/{id}` — fetch single comment.
  - `POST /api/comment/{symbol}` — create comment on a stock (associates with stock symbol).
  - `PUT /api/comment/{id}` — update comment.
  - `DELETE /api/comment/{id}` — delete comment.
  - Uses `ICommentRepository` for data access.

- **`Controllers/PortfolioController.cs`** — user portfolio endpoints (all protected with `[Authorize]`).
  - `GET /api/portfolio` — returns authenticated user's portfolio (stocks they own).
  - `POST /api/portfolio` — add stock to user's portfolio.
  - `DELETE /api/portfolio` — remove stock from user's portfolio.
  - Extracts username from JWT claims using `ClaimsExtensions`.
  - Uses `IPortfolioRepository` for data access.

---

### Data folder
**Purpose:** Entity Framework Core configuration and DbContext.

Files
- **`Data/ApplicationDBContext.cs`** — EF Core DbContext.
  - Defines `DbSet` for all entities: `AppUser`, `Stock`, `Comment`, `Portfolio`.
  - Configures relationships:
    - User-Portfolio-Stock many-to-many via `Portfolio` join table.
    - Comment-Stock relationship.
  - `OnModelCreating` method sets up fluent API constraints and indexes.
  - Example: Portfolio has composite key (UserId, StockId).

- **`Data/ApplicationDBContextFactory.cs`** — design-time DbContext factory.
  - Used by `dotnet ef` CLI for migrations without running the app.
  - Reads connection string from `appsettings.json`.
  - Allows migration commands like `dotnet ef migrations add MigrationName`.

---

### Models folder
**Purpose:** Core entity/domain classes representing database tables.

Files
- **`Models/AppUser.cs`** — extends ASP.NET Core Identity `IdentityUser`.
  - Inherits username, email, password hash from Identity.
  - May include custom fields (e.g., FirstName, LastName).
  - Navigation property to `Portfolios` (one user has many portfolios).

- **`Models/Stock.cs`** — represents a stock.
  - Properties: Symbol, CompanyName, Description, Industry, MarketCap, Price, etc.
  - Navigation property to `Comments` (one stock has many comments).
  - Navigation property to `Portfolios` (via join table).

- **`Models/Comment.cs`** — represents a comment on a stock.
  - Properties: Title, Content, CreatedOn, CreatedBy (userId).
  - Navigation properties: Stock (which stock is it on), AppUser (who wrote it).
  - May be one-to-one or one-to-many with stocks depending on business logic.

- **`Models/Portfolio.cs`** — join entity for many-to-many user-stock relationship.
  - Properties: UserId, StockId.
  - Navigation properties: AppUser (the owner), Stock (the stock).
  - Composite primary key: (UserId, StockId) ensures a user can own a stock only once.

---

### DTOs folder
**Purpose:** Data Transfer Objects — lightweight objects for API request/response payloads (not stored in DB).

Subfolder structure
- **`Dtos/Account/`**
  - `LoginDto.cs` — payload for login: username + password.
  - `RegisterDto.cs` — payload for registration: username + email + password.
  - `NewUserDto.cs` — response after successful registration or login (returns user info + token).

- **`Dtos/Stock/`**
  - `CreateStockRequestDto.cs` — payload to create a stock (Symbol, CompanyName, Industry, etc.).
  - `UpdateStockRequestDto.cs` — payload to update a stock.
  - `StockDto.cs` — response payload (read model returned to client).
  - `FMPStock.cs` — maps response from FinancialModelingPrep external API (deserializes JSON).

- **`Dtos/Comment/`**
  - `CreateCommentDto.cs` — payload to create comment (Title, Content, StockSymbol).
  - `UpdateCommentRequestDto.cs` — payload to update comment.
  - `CommentDto.cs` — response payload (read model).

**Why DTOs?**
- Decouple API contract from database models.
- Control which fields are exposed publicly.
- Flatten/reshape data for client consumption.
- Validate input independently of entities.

---

### Repositories folder (Repository Pattern)
**Purpose:** Data access layer — abstracts database operations.

Files
- **`Repository/StockRepository.cs`** — implements `IStockRespository`.
  - `GetAllAsync()` — fetch all stocks from DB (with optional pagination/filtering).
  - `GetByIdAsync(int id)` — fetch single stock.
  - `CreateAsync(Stock stock)` — insert new stock.
  - `UpdateAsync(Stock stock)` — update stock.
  - `DeleteAsync(Stock stock)` — delete stock.
  - Methods interact with `ApplicationDBContext` and return `Task<Stock>` or `IEnumerable<Stock>`.

- **`Repository/CommentRepository.cs`** — implements `ICommentRepository`.
  - Similar methods but for comments: `GetAllAsync()`, `GetByIdAsync()`, `CreateAsync()`, `UpdateAsync()`, `DeleteAsync()`, etc.
  - May include `GetByStockIdAsync()` to fetch comments for a specific stock.

- **`Repository/PortfolioRepository.cs`** — implements `IPortfolioRepository`.
  - `GetUserPortfolioAsync(string userId)` — fetch all stocks in a user's portfolio.
  - `AddStockToPortfolioAsync(string userId, int stockId)` — add stock to portfolio.
  - `DeleteStockFromPortfolioAsync(string userId, int stockId)` — remove stock from portfolio.
  - Operates on `Portfolio` join table.

**Why Repository Pattern?**
- Centralizes database queries in one place.
- Easy to swap implementations (e.g., switch to NoSQL).
- Simpler unit testing (mock repositories).

---

### Services folder
**Purpose:** Business logic and external integrations.

Files
- **`Service/FMPService.cs`** — implements `IFMPService`.
  - `FindStockBySymbolAsync(string symbol)` — calls FinancialModelingPrep API to fetch stock data.
  - Uses injected `HttpClient` and `FMPKey` from configuration.
  - Deserializes JSON response into `FMPStock` DTO.
  - Maps `FMPStock` to local `Stock` model for storage.
  - Example use: when creating a stock, optionally enrich it with real market data.

- **`Service/TokenService.cs`** — implements `ITokenService`.
  - `CreateToken(AppUser user)` — generates JWT bearer token.
  - Uses `SigningKey` from configuration to sign the token.
  - Sets `Issuer` and `Audience` claims.
  - Typically includes expiration (e.g., 7 days).
  - Returns token string passed back to client via login response.

**Why Services?**
- Encapsulates business rules and external calls.
- Reusable across multiple controllers.
- Easier to test and maintain.

---

### Helpers folder
**Purpose:** Utility classes and query/filter objects.

Files
- **`Helpers/QueryObject.cs`** — DTO for filtering/pagination on stock list.
  - Properties: `PageNumber`, `PageSize`, `Symbol`, `CompanyName`, `SortBy`, `IsDescending`.
  - Controllers use this to parse query parameters and pass to repositories.
  - Example: `GET /api/stock?symbol=AAPL&pageNumber=1&pageSize=10`.

- **`Helpers/CommentQueryObject.cs`** — DTO for filtering/pagination on comments list.
  - Similar structure: page number, size, filtering by stock symbol, etc.
  - Helps standardize query parameter parsing across list endpoints.

**Why Helpers?**
- Organize cross-cutting utilities.
- Standardize query object handling.
- Make pagination/filtering consistent.

---

### Extensions folder
**Purpose:** Extension methods to enhance existing classes.

Files
- **`Extensions/ClaimsExtensions.cs`** — extension methods on `ClaimsPrincipal`.
  - `GetUsername(this ClaimsPrincipal principal)` — extracts username from JWT claims.
  - `GetUserId(this ClaimsPrincipal principal)` — extracts userId from JWT claims.
  - Used by controllers (e.g., `PortfolioController`) to identify the authenticated user.
  - Example: `var username = User.GetUsername();` inside a controller action.

**Why Extensions?**
- Keep code DRY by avoiding repeated claim extraction.
- Readable, fluent syntax.

---

### Mappers folder
**Purpose:** Mapping logic between Models and DTOs (for data transformation).

Files
- **`Mappers/StockMappers.cs`** — extension methods to map `Stock` ↔ `StockDto`.
  - `ToStockDto(this Stock stock)` — converts `Stock` model to `StockDto` for API responses.
    - Example: hides internal fields, formats dates, includes derived data.
  - `ToStock(this CreateStockRequestDto dto)` — converts `CreateStockRequestDto` to `Stock` for saving.
  - Example:
    ```csharp
    public static StockDto ToStockDto(this Stock stock)
    {
        return new StockDto
        {
            Id = stock.Id,
            Symbol = stock.Symbol,
            CompanyName = stock.CompanyName,
            Industry = stock.Industry,
            MarketCap = stock.MarketCap,
            Price = stock.Price
        };
    }
    ```

- **`Mappers/CommentMappers.cs`** — extension methods to map `Comment` ↔ `CommentDto`.
  - `ToCommentDto(this Comment comment)` — converts `Comment` to `CommentDto`.
  - `ToComment(this CreateCommentDto dto)` — converts `CreateCommentDto` to `Comment`.

**How Mappers work:**
- Controllers call mappers when preparing responses:
  ```csharp
  var stock = await _repo.GetByIdAsync(id);
  return Ok(stock.ToStockDto()); // Maps Stock model to DTO
  ```
- Keep models separate from DTOs so schema changes don't leak to clients.
- Centralize transformation logic for consistency.

**Why Mappers?**
- Separation of concerns (models vs API contracts).
- Reusable, testable mapping logic.
- Flexibility to add calculated fields or hide sensitive data.

---

### Interfaces folder
**Purpose:** Contracts for services and repositories (enables dependency injection and mocking).

Files
- **`Interfaces/IStockRespository`** — contract for stock data access.
  - Methods: `GetAllAsync()`, `GetByIdAsync(int id)`, `CreateAsync()`, `UpdateAsync()`, `DeleteAsync()`.
  - Repository implements this interface.

- **`Interfaces/ICommentRepository`** — contract for comment data access.
  - Methods: `GetAllAsync()`, `GetByIdAsync()`, `CreateAsync()`, `UpdateAsync()`, `DeleteAsync()`, etc.

- **`Interfaces/IPortfolioRepository`** — contract for portfolio operations.
  - Methods: `GetUserPortfolioAsync()`, `AddStockToPortfolioAsync()`, `DeleteStockFromPortfolioAsync()`.

- **`Interfaces/IFMPService`** — contract for external API client.
  - Methods: `FindStockBySymbolAsync(string symbol)`.
  - Service implements this interface.

- **`Interfaces/ITokenService`** — contract for JWT token creation.
  - Methods: `CreateToken(AppUser user)`.
  - Service implements this interface.

**Why Interfaces?**
- Enable dependency injection in `Program.cs`.
- Make unit testing easier (mock implementations).
- Decouple implementation details from consumers.
- Example: `services.AddScoped<IStockRespository, StockRepository>();` in `Program.cs`.

---

### Migrations folder
**Purpose:** Entity Framework Core database schema change history.

Files (examples)
- **`Migrations/20251110025811_PortfolioManyToMany.cs`** — migration to add many-to-many portfolio relationship.
  - `Up()` method — creates tables/columns/indexes.
  - `Down()` method — rolls back the migration.

- **`Migrations/20251110091829_CommentOneToOne.cs`** — migration for comment-stock relationship.

- **`Migrations/ApplicationDBContextModelSnapshot.cs`** — snapshot of current schema (auto-generated).

**How Migrations work:**
1. Developer defines models and relationships in code.
2. Run `dotnet ef migrations add MigrationName`.
3. EF generates migration file with `Up()` and `Down()` methods.
4. Run `dotnet ef database update` to apply to DB.
5. Rollback with `dotnet ef database update PreviousMigration`.

**Why Migrations?**
- Version control for database schema.
- Reproducible deployments.
- Easy rollback if issues arise.

---

### Other files

- **`api.http`** — VS Code REST Client file.
  - Sample HTTP requests to test endpoints.
  - Define variables for token, base URL, etc.
  - Example:
    ```http
    POST http://localhost:5000/api/account/login
    Content-Type: application/json
    
    {
      "username": "testuser",
      "password": "P@ssw0rd!"
    }
    ```
  - Click "Send Request" to execute directly in VS Code.

---

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
  - `FMPService` uses an injected `HttpClient` and `FMPKey` to call FinancialModelingPrep endpoints, map responses to `FMPStock` DTO, and return mapped `Stock` entities.
  
- Authentication:
  - `AccountController` uses `UserManager`/`SignInManager` to validate credentials and `TokenService` to create JWTs.
  
- Mapping workflow:
  - Controller receives request DTO (e.g., `CreateStockRequestDto`).
  - Mapper converts DTO to model: `var stock = dto.ToStock();`.
  - Repository saves to DB.
  - On retrieval, mapper converts model to response DTO: `return stock.ToStockDto();`.

---

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

---

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

---

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

---

## Notes & next steps

- Consider changing portfolio endpoints to use explicit route templates (e.g. `[HttpPost("{symbol}")]`) to make calling them from a browser or simple curl commands straightforward.
- Confirm Swagger is enabled in Development for UI testing of protected endpoints (use Swagger "Authorize" button to set the JWT).
- Add sample `.env` or secrets guidance for JWT signing key and `FMPKey`.

---

## Key files quick links

- `Program.cs` — startup & DI
- `appsettings.json` — configuration
- `Controllers/` — API endpoints
- `Data/ApplicationDBContext.cs` — EF DbContext
- `Service/FMPService.cs` — external API client
- `Service/TokenService.cs` — JWT creation
- `Repository/` — data layer
- `Migrations/` — EF migrations

---

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

