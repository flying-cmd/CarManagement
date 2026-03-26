# Car Management

A Web API for dealers to manage their car stocks.

The project uses FastEndpoints for HTTP endpoints, JWT bearer authentication for protected routes, SQLite for persistence, Dapper for data access, and xUnit-based unit and integration tests.

## What This Project Does

- dealer registration and login
- JWT-protected car management endpoints
- add a car with stock level and unit price
- remove a dealer's stock level for a car, and delete the car when no stock remains for any dealer
- list cars and stock levels with pagination
- update stock level for a dealer-owned car
- search cars by optional make and model filters

All car management endpoints are protected. A dealer can only manage cars that belong to their own account.

## Tech Stack

- .NET 9
- ASP.NET Core
- FastEndpoints
- FastEndpoints.Security
- FastEndpoints.Swagger
- SQLite
- Dapper
- xUnit
- FluentAssertions
- Moq

## Solution Structure

The main solution is in [`CarManagement.Solution`](./CarManagement.Solution).

- `CarManagement.API`
  API entry point, FastEndpoints endpoints, middleware, configuration
- `CarManagement.Common`
  shared constants, helpers, exceptions, auth and swagger extensions
- `CarManagement.DataAccess`
  SQLite connection factory and database initialization
- `CarManagement.Models`
  domain entities such as `Dealer`, `Car`, and `CarStock`
- `CarManagement.Repository`
  Dapper-based repositories and unit of work pattern implementation
- `CarManagement.Service`
  business logic, DTOs, validators, mappers, JWT token service, and user context
- `CarManagement.UnitTests`
  unit tests for business logic
- `CarManagement.IntegrationTests`
  integration tests for API and repository

## Architecture

The request flow is:

1. A request reaches a FastEndpoints endpoint in `CarManagement.API` layer.
2. Request DTO validation runs through validators in `CarManagement.Service/Validators`.
3. The endpoint delegates to a service in `CarManagement.Service/Services` layer.
4. The service layer applies business logic and calls repository to query data.
5. The repository layer runs SQL queries through Dapper against SQLite.
6. Responses aare wrapped in the generic ApiResponse<T> response.

Key design points:
- The RESTful APIs is built using a multi-layered architecture that clearly separates responsibilities across the API, Service, Repository, DataAccess, and Models layers. The API layer is responsible for handling HTTP requests and returning standardized responses, the Service layer implements business logic, the Repository layer uses Dapper for SQL-based data access, the DataAccess layer manages database connections and database initialization, and the Models layer defines domain entities. This separation of concerns significantly improves maintainability and testability. For example, when business logic or query requirements change, I can modify the corresponding layer in isolation without impacting the rest of the system
- The JWT authentication and authorization strategy is implemented to ensure that dealers could only access and modify their own cars. By extracting the UserId from the JWT token and querying the database, the system verifies whether a car belongs to the authenticated dealer before allowing any access or update operations. `JwtTokenService` issues bearer tokens containing the `Dealer` role and a `UserId` claim
- `UserContext` reads the authenticated dealer id from `HttpContext.User`
- Global Exception Handler converts exceptions into consistent JSON error responses and provide a fallback mechanism for capturing any unhandled exceptions.

## Business Rules

### Authentication

- registration requires `name`, `email`, `phoneNumber`, and `password`
- email is normalized to lowercase before persistence
- duplicate email registration is rejected
- login returns `401 Unauthorized` for unknown emails or invalid passwords
- JWT expiration is controlled by `Jwt:DurationInMinutes`

### Cars

- all car endpoints require an authenticated dealer
- add car: Add car and create car stock. If the car already exists, reuse it and create a new car stock for the current dealer. Otherwise, create a new car and a new car stock for the current dealer.
- remove car: Remove car's stock level. If there is no stock level left for all dealers, remove the car.
- list cars and stock levels: list cars and stock levels in pagination.
- update car stock levels: Update car stock level.
- search car by make and model:  search cars owned by the current dealer with optional make and model filters. The returned reult is in pagination.

## Prerequisites

- .NET 9 SDK
- optional: Docker, for containerized runs

Verify your SDK:

```powershell
dotnet --version
```

## Getting Started

From the repository root:

```powershell
cd .\CarManagement.Solution
dotnet restore
dotnet build
```

## Configuration

Main API settings are in [`CarManagement.Solution/CarManagement.API/appsettings.json`](./CarManagement.Solution/CarManagement.API/appsettings.json):

```json
{
  "Database": {
    "FilePath": "Database/CarManagement.db"
  },
  "Jwt": {
    "SigningKey": "replace-with-a-secure-secret-key",
    "DurationInMinutes": 60
  }
}
```

Important notes:

- replace the placeholder JWT signing key before using the app outside local development
- `Jwt:SigningKey` must not be empty
- `Jwt:DurationInMinutes` must be greater than `0`
- For development environment, you can store the `Jwt:SigningKey` and `Jwt:DurationInMinutes` in `appsettings.Development.json`. However, for production environment, please store them securely in a secret management service, such as Azure Key Vault or AWS Secrets Manager, instead of hardcoding them in configuration files.

## Running the API

From `CarManagement.Solution`:

```powershell
dotnet run --project .\CarManagement.API
```

Default local URLs from launch settings:

- `http://localhost:5177`
- `https://localhost:7036`

Swagger UI:

- `http://localhost:5177/swagger`
- `https://localhost:7036/swagger`

## Database

The default local SQLite file is:

- [`CarManagement.Solution/Database/CarManagement.db`](./CarManagement.Solution/Database/CarManagement.db)

On startup, the application:

- creates the database directory if needed
- creates the `Dealers`, `Cars`, and `CarStocks` tables if they do not exist
- creates the supporting indexes and uniqueness constraints
- seeds demo dealers and sample stock if they do not already exist

If you want a clean local database, stop the API and delete:

- [`CarManagement.Solution/Database/CarManagement.db`](./CarManagement.Solution/Database/CarManagement.db)

The database will be recreated on the next startup.

## Seed Data

The database initializer seeds these dealer accounts:

- email: `tom@example.com` / password: `Pass123$`
- email: `jack@example.com` / password: `Pass123$`

It also seeds example stock for `Car`:

- `Toyota Corolla 2022`
- `Mazda Mazda3 2023`
- `Honda Civic 2021`

## Run with Docker

The repository includes a root-level [`Dockerfile`](./Dockerfile).

Build the image from the repository root:

```powershell
docker build -t carmanagement-api .
```

Create a host folder for the SQLite database in your current working directory:

```powershell
New-Item -ItemType Directory -Force .\docker-data
```

Then run the container with a bind mount:

```powershell
docker run --rm -p 8080:8080 --mount type=bind,source="${PWD}\docker-data",target=/app/Database carmanagement-api
```

Container details:

- the API listens on `http://localhost:8080`
- Swagger is available at `http://localhost:8080/swagger`

Inside the container, SQLite is configured to use:

- `/app/Database/CarManagement.db`

This is set through the Dockerfile environment override:

```text
Database__FilePath=/app/Database/CarManagement.db
```

That means:

- inside the container, the database file is `/app/Database/CarManagement.db`
- on the host, the database file is created inside the folder you mounted to `/app/Database`

If you use the command above from the repository root, the host database file will be:

- `.\docker-data\CarManagement.db`

In absolute form on Windows, that will resolve to:

- `<current-working-directory>\docker-data\CarManagement.db`

If you run the container without a bind mount:

- the SQLite file exists only inside that container
- the data is lost when the container is removed

This solution includes:

- service unit tests in [`CarManagement.UnitTests`](./CarManagement.Solution/CarManagement.UnitTests)
- API integration tests in [`CarManagement.IntegrationTests/Api`](./CarManagement.Solution/CarManagement.IntegrationTests/Api)
- repository integration tests in [`CarManagement.IntegrationTests/Repositories`](./CarManagement.Solution/CarManagement.IntegrationTests/Repositories)

### Run All Tests

From `CarManagement.Solution`:

```powershell
dotnet test .\CarManagement.Solution.sln
```

### Run Unit Tests Only

```powershell
dotnet test .\CarManagement.UnitTests\CarManagement.UnitTests.csproj
```

### Run Integration Tests Only

```powershell
dotnet test .\CarManagement.IntegrationTests\CarManagement.IntegrationTests.csproj
```

### Test Strategy

Unit tests cover:

- `AuthService`
- `CarService`
- `JwtTokenService`
- `UserContext`

Integration tests cover:

- auth API endpoints
- car API endpoints
- dealer repository behavior against real SQLite
- car repository behavior against real SQLite

The integration test project uses:

- `WebApplicationFactory<Program>` for API-level testing
- temporary SQLite databases for isolation
- real repository implementations for query verification

## API Conventions

API endpoints return the generic `ApiResponse<T>` structure. For details, please see `CarManagement.Common/Helpers/ApiResponse` :

```json
{
  "success": true,
  "statusCode": 200,
  "message": "Success",
  "data": {},
  "errors": null,
  "traceId": "00-..."
}
```

Error responses include:

- `message`
- `statusCode`
- `errors` dictionary
- `traceId`

Note:
- `DELETE` and `PATCH` success responses return `204 No Content`

## API Endpoints
For more details, please access `http://localhost:5177/swagger`.

### Authentication

| Method | Route | Auth Required | Description |
| --- | --- | --- | --- |
| `POST` | `/api/auth/register` | No | Register a dealer and return a JWT |
| `POST` | `/api/auth/login` | No | Log in and return a JWT |

### Cars

| Method | Route | Auth Required | Description |
| --- | --- | --- | --- |
| `POST` | `/api/cars` | Yes | Add a car and create stock for the current dealer |
| `GET` | `/api/cars` | Yes | List current dealer cars with pagination |
| `GET` | `/api/cars/search` | Yes | Search current dealer cars by make and model |
| `PATCH` | `/api/cars/{id}/stock-level` | Yes | Update stock level for a dealer-owned car |
| `DELETE` | `/api/cars/{id}` | Yes | Remove the current dealer's stock entry for a car |

## Example Requests

### Register

```http
POST /api/auth/register
Content-Type: application/json

{
  "name": "DealerOne",
  "email": "dealer@example.com",
  "phoneNumber": "0412345678",
  "password": "Pass123$"
}
```

### Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "dealer@example.com",
  "password": "Pass123$"
}
```

### Add a Car

```http
POST /api/cars
Authorization: Bearer <jwt>
Content-Type: application/json

{
  "make": "Toyota",
  "model": "Corolla",
  "year": 2024,
  "stockLevel": 3,
  "unitPrice": 25000
}
```

### List Cars

```http
GET /api/cars?pageNumber=1&pageSize=10
Authorization: Bearer <jwt>
```

### Search Cars

```http
GET /api/cars/search?make=toy&model=cor&pageNumber=1&pageSize=10
Authorization: Bearer <jwt>
```

### Update Stock Level

```http
PATCH /api/cars/{id}/stock-level
Authorization: Bearer <jwt>
Content-Type: application/json

{
  "id": "00000000-0000-0000-0000-000000000000",
  "stockLevel": 7
}
```

### Remove a Car

```http
DELETE /api/cars/{id}
Authorization: Bearer <jwt>
```

## Validation Rules

### Register

- `Name` is required and has a maximum length of `20`
- `Email` is required and must be valid
- `PhoneNumber` is required and must be exactly `10` digits
- `Password` is required and must:
  - be at least `6` characters
  - be at most `20` characters
  - contain an uppercase letter
  - contain a lowercase letter
  - contain a number
  - contain a special character

### Login

- `Email` is required and must be valid
- `Password` is required

### Add Car

- `Make` is required and max length is `50`
- `Model` is required and max length is `50`
- `Year` must be greater than `0`
- `StockLevel` must be greater than or equal to `0`
- `UnitPrice` must be greater than or equal to `0`

### List and Search

- `PageNumber` must be greater than `0`
- `PageSize` must be between `1` and `100`
- `Make` and `Model` filters are optional and max length is `50`

### Update Stock Level

- `Id` is required
- `StockLevel` must be greater than or equal to `0`

## Troubleshooting

### JWT signing key startup error

If startup fails with a JWT signing key error, update [`CarManagement.Solution/CarManagement.API/appsettings.json`](./CarManagement.Solution/CarManagement.API/appsettings.json) or pass `Jwt__SigningKey` as an environment variable.

### Unauthorized car requests

Make sure:

- you registered or logged in first
- you send `Authorization: Bearer <token>`
- the token was generated using the same signing key the API is configured to validate
