# 15 - Observability: find slow queries

**Problem:** the endpoint is slow, but which of its SQL commands is responsible? Without instrumentation you guess.

## Run it

```bash
cd lessons/15-observability
dotnet run
dotnet run -- --sql      # also prints every SQL command
```

## Your turn

Write a `SlowQueryInterceptor` in `Fixed.cs` (instructions in the comment). It measures each command via `eventData.Duration` and records those over 25 ms.

## Other tools to know

- `LogTo(Console.WriteLine, LogLevel.Information)` and `EnableSensitiveDataLogging()` in development.
- `ToQueryString()` on any `IQueryable` shows its SQL without running it.
- `EXPLAIN (ANALYZE, BUFFERS)` in psql (see lesson 06).
- `pg_stat_statements` in Postgres, OpenTelemetry instrumentation for EF Core in production.

## Takeaways

- Measure first, then optimise the query that actually dominates.
- Interceptors are the extension point for timing, auditing, and retries.
- Never enable sensitive data logging in production.

## Interview questions

- How do you find the slowest query in a production EF Core app?
- What is an interceptor, and how is it different from a logger?
- What does `ToQueryString()` do?
