# 14 - DbContext pooling

**Problem:** a `DbContext` is cheap but not free: each new instance builds internal services, a change tracker and more. In a hot API creating thousands per second adds allocations and GC work.

## Run it

```bash
cd lessons/14-dbcontext-pooling
dotnet run
```

## Your turn

Edit `Fixed.cs` and use `PooledDbContextFactory<AppDbContext>`.

## What you should see

Same result (`OK`). Time improves a little (often 5-20%); this is a **micro-optimisation**. Do it after the big wins (indexes, projections, N+1).

## Takeaways

- `AddDbContextPool` reuses instances; EF resets their state when returned.
- Pooled contexts must be stateless between requests: no per-request fields set on the context.
- Connection pooling (Npgsql) is separate and always on; this is about the `DbContext` object.
- `AddDbContextFactory` is for Blazor/background services that need to create contexts themselves.

## Interview questions

- DbContext pooling vs connection pooling?
- What state can't live in a pooled context?
- Is `DbContext` thread-safe? (No: one per unit of work.)
