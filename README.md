# learning-core-api

.NET Core (net10.0) microservice — **single source of truth** for users, roles, and (eventually) reporting. Owns all business logic and is the only service that talks to PostgreSQL.

## Architecture

```
React (learning-client, :5000)
    │
    ▼
Node BFF (learning-server, :5001)
    │  HTTP
    ▼
.NET Core API (learning-core-api, :5002 http / :5003 https)   ← this repo
    │  EF Core
    ▼
PostgreSQL (users, roles, user_roles)
```

Layering: `Controllers` → `Services` (`IUserService`/`IRoleService`, business logic, return `FluentResults.Result<T>`) → `Repositories` (`IUserRepository`/`IRoleRepository`, EF Core data access) → `AppDbContext`.

- DTOs in `Models/DTOs` are the only types that cross the controller boundary; entities (`Models/Entitites`) never leak out.
- Not-found vs. server-error is distinguished via `Result` error metadata (`UserService.NotFoundMetadataKey` / `RoleService.NotFoundMetadataKey`), which controllers translate to `404` vs `500`.
- `GlobalExceptionHandler` (`Infrastructure/`) + `AddProblemDetails()` provide consistent RFC 7807 error responses for unhandled exceptions.
- Passwords are hashed with SHA256 (`UserService.HashPassword`) and compared as plain strings — no salting yet (see Roadmap).

## Tech stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10 (`net10.0`), ASP.NET Core Web API |
| ORM | EF Core via `Npgsql.EntityFrameworkCore.PostgreSQL`, snake_case columns via `EFCore.NamingConventions` |
| Result/error handling | `FluentResults` |
| API docs | `Microsoft.AspNetCore.OpenApi` (`AddOpenApi()` / `MapOpenApi()`), spec served live, not checked in |
| Health checks | `AspNetCore.HealthChecks.NpgSql` at `/health` |
| Database | PostgreSQL 16 (see root `docker-compose.yaml`) |

## Endpoints

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/users` | List users with roles |
| GET | `/api/users/{id}` | Get user by id |
| GET | `/api/users/{id}/roles` | Get a user's roles |
| POST | `/api/users` | Create user |
| POST | `/api/users/auth` | Verify username/password, return `UserDto` |
| DELETE | `/api/users/{id}` | Delete user |
| GET/POST/DELETE | `/api/roles`, `/api/roles/{id}` | Role CRUD |
| POST | `/api/roles/assign` | Assign a role to a user |
| GET | `/openapi/v1.json` | Live OpenAPI 3.1 spec (Development only) |
| GET | `/health` | DB-backed health check |

Called exclusively by `learning-server` (the BFF) — never directly by `learning-client`.

## Data model

- Entities: `User`, `Role`, `UserRole` (join table) — see `Data/AppDbContext.cs`
- Unique indexes on `users.email`, `users.username`, `roles.name`, and `(user_id, role_id)` on `user_roles`
- `Database.EnsureCreatedAsync()` runs automatically on startup in `Development` (no EF migrations yet — schema changes require a DB reset in dev)

## Configuration

`appsettings.Development.json` / `appsettings.json`:

```json
"ConnectionStrings": { "Postgres": "Host=localhost;Port=5432;Username=admin;Password=111;Database=learningdb" }
```

`Properties/launchSettings.json` ports:

| Profile | URL |
|---|---|
| `http` | `http://localhost:5002` |
| `https` | `https://localhost:5003;http://localhost:5002` |

## How to run

Prerequisites: .NET 10 SDK, PostgreSQL reachable at the configured connection string.

```bash
# from the repo root, start Postgres (+ pgAdmin) locally
docker compose up -d postgres pgadmin

# from learning-core-api/
dotnet restore
dotnet run --launch-profile http
```

The API listens on `http://localhost:5002`. On first run in `Development`, `EnsureCreatedAsync()` creates the schema if it doesn't exist — there's no seed data, so create a user via `POST /api/users` (or insert one directly) before testing login.

Live OpenAPI spec: `http://localhost:5002/openapi/v1.json` — used by `learning-server`'s `npm run generate:types` to produce a typed TS client.

## Roadmap

- RabbitMQ publishing for report/PDF generation (not yet implemented — will live here, not in the BFF)
- EF Core migrations to replace `EnsureCreatedAsync`
- Salted password hashing (e.g. BCrypt) instead of plain SHA256
