# learning-core-api

.NET Core (net10.0) microservice — **single source of truth** for users, roles, and (eventually) reporting. Owns all business logic and is the only service that talks to PostgreSQL.

## Architecture

```
React (learning-client, :5000)
    │
    ▼
Node BFF (learning-server, :5001)
    │  HTTPS
    ▼
.NET Core API (learning-core-api, :5002 https only)   ← this repo
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
| API docs | `Microsoft.AspNetCore.OpenApi` (`AddOpenApi()` / `MapOpenApi()`) + Scalar UI at `/scalar/v1`, spec served live, not checked in |
| Logging | Serilog → console + daily rolling compact-JSON file (`Logs/app-log.txt`) |
| Health checks | `AspNetCore.HealthChecks.NpgSql` at `/health` |
| Database | PostgreSQL 16 (see root `docker-compose.yaml`) |

## Project structure

```
src/LearningUserService/
├── Controllers/      # UsersController, RolesController
├── Services/          # business logic (IUserService, IRoleService)
├── Repositories/      # EF Core data access (IUserRepository, IRoleRepository)
├── Data/               # AppDbContext
├── Migrations/         # EF Core migrations (source of truth for schema)
├── Models/              # Entities + DTOs
├── Infrastructure/      # GlobalExceptionHandler, etc.
└── Program.cs
tests/LearningUserService.Tests/
Makefile                  # migration/run shortcuts (see below)
Dockerfile
```

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
| GET | `/scalar/v1` | Browsable API docs UI (Development only) |
| GET | `/health` | DB-backed health check |

Called exclusively by `learning-server` (the BFF) — never directly by `learning-client`.

## Data model

- Entities: `User`, `Role`, `UserRole` (join table) — see `Data/AppDbContext.cs`
- Unique indexes on `users.email`, `users.username`, `roles.name`, and `(user_id, role_id)` on `user_roles`
- Schema is managed via **EF Core migrations** (`src/LearningUserService/Migrations/`). `Program.cs` calls `Database.MigrateAsync()` on startup in `Development`, so pending migrations are applied automatically when the app runs — no manual step needed for local dev.

## Configuration

`appsettings.Development.json` / `appsettings.json`:

```json
"ConnectionStrings": { "Postgres": "Host=localhost;Port=5432;Username=admin;Password=111;Database=learningdb" }
```

`Properties/launchSettings.json` ports:

| Profile | URL |
|---|---|
| `https` | `https://localhost:5002` |

## How to run

Prerequisites: .NET 10 SDK, PostgreSQL reachable at the configured connection string, [`dotnet-ef`](https://learn.microsoft.com/ef/core/cli/dotnet) tool (only needed for the migration commands below — `dotnet tool install --global dotnet-ef`).

```bash
# from the repo root, start Postgres (+ pgAdmin) locally
# docker compose up -d postgres pgadmin

# from the repo root
dotnet restore
dotnet run --project src/LearningUserService --launch-profile https
```

The API listens on `https://localhost:5002` (https-only — trust the local dev cert once via `dotnet dev-certs https --trust`). Callers running on Node (i.e. `learning-server`) also need to export this cert for Node's own CA store — see the "One-time HTTPS cert setup" section in `learning-server`'s README. On startup in `Development`, `Database.MigrateAsync()` creates/updates the schema automatically — there's no seed data, so create a user via `POST /api/users` (or insert one directly) before testing login.

Live OpenAPI spec: `https://localhost:5002/openapi/v1.json` — used by `learning-server`'s `npm run generate:types` to produce a typed TS client. Interactive docs at `https://localhost:5002/scalar/v1`.

## Database migrations

Schema changes are managed with EF Core migrations, tracked under `src/LearningUserService/Migrations/`. `Program.cs` applies pending migrations automatically on startup in `Development`, so most local workflows never need the commands below directly — but use them to author new migrations or manage the DB manually (e.g. non-Development environments, CI, or troubleshooting).

Run from the repo root:

```bash
# Add a new migration after changing entities/AppDbContext
dotnet ef migrations add <MigrationName> \
  --project src/LearningUserService \
  --startup-project src/LearningUserService

# Apply pending migrations to the database
dotnet ef database update \
  --project src/LearningUserService \
  --startup-project src/LearningUserService

# List migrations and applied status
dotnet ef migrations list \
  --project src/LearningUserService \
  --startup-project src/LearningUserService

# Roll back to a specific migration (re-applies down to and including <MigrationName>)
dotnet ef database update <MigrationName> \
  --project src/LearningUserService \
  --startup-project src/LearningUserService

# Remove the most recently added, not-yet-applied migration
dotnet ef migrations remove \
  --project src/LearningUserService \
  --startup-project src/LearningUserService

# Drop the database (destructive)
dotnet ef database drop \
  --project src/LearningUserService \
  --startup-project src/LearningUserService
```

**Workflow for a schema change:** edit the entity/`AppDbContext` → `dotnet ef migrations add <DescriptiveName> --project src/LearningUserService --startup-project src/LearningUserService` → review the generated migration in `Migrations/` → `dotnet ef database update --project src/LearningUserService --startup-project src/LearningUserService` (or let it apply automatically next time the app starts in `Development`) → commit the migration files.

## Logging

Serilog is configured in `Program.cs` / `appsettings.Development.json`:

- Writes to console and to `Logs/app-log.txt` (daily rolling, compact JSON via `Serilog.Formatting.Compact`)
- HTTP request/response logging via `UseSerilogRequestLogging()`
- Relevant packages: `Serilog.AspNetCore`, `Serilog.Sinks.File`, `Serilog.Settings.Configuration`, `Serilog.Formatting.Compact`

## Testing

```bash
dotnet test
```

Tests live in `tests/LearningUserService.Tests`.

## Roadmap

- RabbitMQ publishing for report/PDF generation (not yet implemented — will live here, not in the BFF)
- Salted password hashing (e.g. BCrypt) instead of plain SHA256
