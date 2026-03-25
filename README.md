# Car Management

A Web API for dealers to manage their car stocks.

The solution uses FastEndpoints for HTTP endpoints, JWT bearer authentication for protected routes, SQLite for persistence, Dapper for data access, and xUnit-based unit and integration tests.

## What This Project Does

The API allows a dealer to:

- register and login
- add cars to their own inventory
- remove their own cars
- list their cars with pagination
- update stock level for their own cars
- search their cars by make and model

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
- WebApplicationFactory for API integration tests

## Solution Structure

The main solution lives in [`CarManagement.Solution`](./CarManagement.Solution).

Projects:

- `CarManagement.API`
  - application entry point, endpoint definitions, middleware
- `CarManagement.Common`
  - shared constants, helpers, exceptions, auth and swagger extensions
- `CarManagement.DataAccess`
  - SQLite connection factory and database initialization
- `CarManagement.Models`
  - domain entities such as `Dealer` and `Car`
- `CarManagement.Repository`
  - Dapper-based repository implementations
- `CarManagement.Service`
  - business logic, DTOs, validators, mapper, JWT token service, user context
- `CarManagement.UnitTests`
  - unit tests for business logic
- `CarManagement.IntegrationTests`
  - integration tests for API and repository

## Architecture Overview

The request flow is:

1. HTTP request reaches a FastEndpoints endpoint in `CarManagement.API` layer
2. DTO validation runs through validators in `CarManagement.Service/Validators`
3. The endpoint delegates to a service in `CarManagement.Service/Services` layer
4. The service layer enforces business logic and calls repository to query data
5. Repository layer execute SQL using Dapper
6. Results are wrapped in the generic `ApiResponse<T>` response

Key design points:
- The RESTful APIs is built using a multi-layered architecture that clearly separates responsibilities across the API, Service, Repository, DataAccess, and Models layers. The API layer is responsible for handling HTTP requests and returning standardized responses, the Service layer implements business logic, the Repository layer uses Dapper for SQL-based data access, the DataAccess layer manages database connections and database initialization, and the Models layer defines domain entities. This separation of concerns significantly improves maintainability and testability. For example, when business logic or query requirements change, I can modify the corresponding layer in isolation without impacting the rest of the system
- The JWT authentication and authorization strategy is implemented to ensure that dealers could only access and modify their own cars. authentication and authorization to ensure that dealers could only access and modify their own cars. `JwtTokenService` issues bearer tokens containing the `Dealer` role and a `UserId` claim
- `UserContext` reads the authenticated dealer id from `HttpContext.User`
- Global Exception Handler converts exceptions into consistent JSON error responses and provide a fallback mechanism for capturing any unhandled exceptions.

## Features and Business Rules

### Authentication

- registration requires `name`, `email`, and `password`
- email is normalized to lowercase before persistence
- duplicate email registration is rejected
- login rejects unknown emails and invalid passwords with `401 Unauthorized`
- JWT expiration is controlled by `Jwt:DurationInMinutes`

### Car

- cars are scoped to the authenticated dealer
- duplicate cars for the same dealer are rejected based on:
  - `make`
  - `model`
  - `year`
  - `colour`
- listing is paginated and ordered by `Make`, `Model`, `Year`, `Colour`
- search is paginated and supports optional `make` and `model` filters
- search matching is case-insensitive
- stock level cannot be negative
- a dealer cannot update or remove another dealer's car

## Prerequisites

Install:

- .NET 9 SDK

You can verify with:

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

### Configure the Application

Open [`appsettings.json`](./CarManagement.Solution/CarManagement.API/appsettings.json) and make sure the JWT signing key is set to a strong secret:

```json
"Jwt": {
  "SigningKey": "replace-with-a-secure-secret-key",
  "DurationInMinutes": 60
}
```

Important:

- replace the placeholder signing key before running
- the signing key must not be empty
- the `DurationInMinutes` must be greater than `0`

### Run the API

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

## Run with Docker

The repository includes a root-level [`Dockerfile`](./Dockerfile) for containerized deployment.

### Build the Image

From the repository root:

```powershell
docker build -t carmanagement-api .
```

### Run the Container

```powershell
docker run --rm -p 8080:8080 carmanagement-api
```

The API will then be available at:

- `http://localhost:8080`
- `http://localhost:8080/swagger`

### SQLite Location in Docker

Inside the container, SQLite is configured to use:

- `/app/Database/CarManagement.db`

This is set through the Dockerfile environment override:

```text
Database__FilePath=/app/Database/CarManagement.db
```

If you run the container without a volume mount:

- the SQLite file exists only inside that container
- the data is lost when the container is removed

If you want the database to persist on your machine, mount a host folder to `/app/Database`.

Example:

```powershell
docker run --rm -p 8080:8080 -v ${PWD}\docker-data:/app/Database carmanagement-api
```

With that command:

- SQLite is still located at `/app/Database/CarManagement.db` inside the container
- the actual file is stored on your host in `.\docker-data\CarManagement.db`

## Database

The application uses SQLite.

Default database configuration in [`appsettings.json`](./CarManagement.Solution/CarManagement.API/appsettings.json):

```json
"Database": {
  "FilePath": "Database/CarManagement.db"
}
```

When running the API locally, this resolves to:

- [`CarManagement.Solution/Database/CarManagement.db`](./CarManagement.Solution/Database/CarManagement.db)

On startup, the app automatically:

- creates the database directory if needed
- creates the `Dealers` table
- creates the `Cars` table
- seeds demo dealers if they do not already exist

## Seeded Demo Accounts

The database initializer seeds these dealer accounts on startup:

- `tom@example.com` / `Pass123$`
- `jack@example.com` / `Pass123$`

These are useful for quick local testing.

## API Conventions

API endpoints return the generic `ApiResponse<T>` structure. For details, please see CarManagement.Common/Helpers/ApiResponse :

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
| `POST` | `/api/auth/register` | No | Register a new dealer and return a JWT |
| `POST` | `/api/auth/login` | No | Log in and return a JWT |

### Cars

| Method | Route | Auth Required | Description |
| --- | --- | --- | --- |
| `POST` | `/api/cars` | Yes | Add a car for the current dealer |
| `GET` | `/api/cars` | Yes | List current dealer cars with pagination |
| `GET` | `/api/cars/search` | Yes | Search current dealer cars by make and model |
| `PATCH` | `/api/cars/{id}/stock-level` | Yes | Update stock level for a car owned by the current dealer |
| `DELETE` | `/api/cars/{id}` | Yes | Remove a car owned by the current dealer |

## Example Requests

### Register

```http
POST /api/auth/register
Content-Type: application/json

{
  "name": "DealerOne",
  "email": "dealer@example.com",
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

Example login response:

```json
{
  "success": true,
  "statusCode": 200,
  "message": "Login successfully",
  "data": {
    "name": "DealerOne",
    "email": "dealer@example.com",
    "accessToken": "<jwt>",
    "expiresAtUtc": "2026-03-25T10:15:00+00:00"
  },
  "traceId": "00-..."
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
  "colour": "Blue",
  "price": 25000,
  "stockLevel": 3
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

- `Name` is required and max length is `20`
- `Email` is required and must be valid
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
- `Colour` is required and max length is `50`
- `Price` must be greater than or equal to `0`
- `StockLevel` must be greater than or equal to `0`

### List and Search

- `PageNumber` must be greater than `0`
- `PageSize` must be between `1` and `100`
- `Make` and `Model` search fields are optional, max length `50`

### Update and Remove

- `Id` is required
- `StockLevel` must be greater than or equal to `0` for stock updates

## Testing

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

## Useful Commands

Restore packages:

```powershell
dotnet restore .\CarManagement.Solution\CarManagement.Solution.sln
```

Build solution:

```powershell
dotnet build .\CarManagement.Solution\CarManagement.Solution.sln
```

Run API:

```powershell
dotnet run --project .\CarManagement.Solution\CarManagement.API
```

Run Swagger-backed API locally and explore the endpoints in a browser:

- `http://localhost:5177/swagger`

## Troubleshooting

### JWT signing key error on startup

If the app throws an error about `Jwt:SigningKey`, update [`appsettings.json`](./CarManagement.Solution/CarManagement.API/appsettings.json) with a non-empty signing key.

### Database reset

If you want a clean local database, stop the API and delete:

- [`CarManagement.Solution/Database/CarManagement.db`](./CarManagement.Solution/Database/CarManagement.db)

The database will be recreated automatically on next startup.

### Unauthorized requests to car endpoints

Make sure:

- you called `/api/auth/register` or `/api/auth/login` first
- you send `Authorization: Bearer <token>`
- the token was generated using the same signing key the API is configured to validate
