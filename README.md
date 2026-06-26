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
| GET | `/health` | DB-backed health check (`AspNetCore.HealthChecks.NpgSql`) |

Called exclusively by `learning-server` (the BFF) — never directly by `learning-client`.

## Data

- EF Core (`Npgsql.EntityFrameworkCore.PostgreSQL`) with snake_case naming convention (`EFCore.NamingConventions`)
- Entities: `User`, `Role`, `UserRole` (join table) — see `Data/AppDbContext.cs`
- `Database.EnsureCreatedAsync()` runs automatically in `Development` (no migrations yet)

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

## Local development

```bash
dotnet run --launch-profile http
```

Requires PostgreSQL reachable at the configured connection string (see root `docker-compose.yaml` for a local Postgres + pgAdmin container).

## Roadmap

- RabbitMQ publishing for report/PDF generation (not yet implemented — will live here, not in the BFF)