# 08 - Compiled queries

**Scenario:** a hot path that is called thousands of times per second in production, here: find an order by its number, 5,000 times.

```bash
cd lessons/08-compiled-queries
dotnet run
```

## The problem

Every time EF Core runs a LINQ query it must (1) take the expression tree, (2) work out a cache key for it, (3) find the already
translated SQL in its **query cache** (translating it the first time only), and then (4) execute. EF caches the expensive part
(LINQ -> SQL), so repeated queries are *already* much cheaper than the first. But steps 1-3 still happen on every call.

On a hot path with a very cheap query, that fixed per-call work can become a measurable share of the total CPU.

## The idea: compile the query once

`EF.CompileAsyncQuery` does the preparation **once** and gives you a delegate you can call again and again, skipping the expression
tree walking and cache lookup:

```csharp
static readonly Func<AppDbContext, string, Task<Order?>> GetByNumber =
    EF.CompileAsyncQuery((AppDbContext db, string number) =>
        db.Orders.AsNoTracking().FirstOrDefault(o => o.OrderNumber == number));

var order = await GetByNumber(db, "ORD-0000042");
```

## What the files do

| File | |
|---|---|
| `Naive.cs` | Writes the LINQ query inside the loop. |
| `Fixed.cs` | **Your turn.** Fill in the static compiled query; the loop is already written. |

## Your turn

1. Read `Naive.cs`.
2. In `Fixed.cs`, replace `= null` on `GetOrderByNumber` with `EF.CompileAsyncQuery(...)`.
   <details>
   <summary>Hint 1</summary>
   The lambda takes the context and the parameters: <code>(AppDbContext db, string number) =&gt; db.Orders...</code>
   </details>
   <details>
   <summary>Hint 2</summary>
   Use <code>FirstOrDefault</code> (not Async) inside - <code>CompileAsyncQuery</code> turns it into a <code>Task&lt;Order?&gt;</code> for you. Keep <code>AsNoTracking()</code>.
   </details>
3. Run it.

## What to look for

- **Same rows**, `OK - same result as Naive`.
- **Queries: 5,000 -> 5,000.** The SQL and the round trips are unchanged.
- **Time: expect a small difference** - maybe a few percent to something like 10-20%, and sometimes within noise. Each call still does a network round trip to Postgres,
  which dominates a cheap query. This lesson is partly about **measuring before optimising**: compiled queries are a micro-optimisation, not a fix for slow SQL.
  Try `dotnet run -- --no-warmup` too.

## When it helps

- Hot paths executed very frequently (thousands per second) with a cheap query.
- Services that are CPU-bound rather than waiting for the database.
- Not when the query itself is slow (that is an index or query-shape problem - lessons 05-07).

## Things to watch for

- Compiled queries must be **static and reused**; compiling inside the loop defeats the point.
- Parameters become SQL parameters. The *shape* of the query is fixed at compile time (no conditionally appended `Where`s).
- They return `IAsyncEnumerable<T>` for multiple results (`EF.CompileAsyncQuery` with `ToList` is not supported - return the query itself and enumerate with `await foreach`).
- EF Core 8+ caches and parameterizes automatically, so the gap is smaller than in old EF versions. Always measure.

## Rule of thumb

> **Fix the query first (projection, indexes, round trips). Reach for `CompileAsyncQuery` only for proven hot paths.**

## Interview questions

1. **What does `EF.CompileAsyncQuery` do?** Precompiles a LINQ query into a delegate so repeated executions skip expression-tree processing and query-cache lookup.
2. **Does it make the SQL faster?** No. Same SQL, same round trip; it only removes client-side overhead.
3. **When would you use it?** Rarely - on proven, extremely hot paths where profiling shows EF overhead matters.
4. **What are the restrictions?** The query is static, parameters are explicit, and collection results come back as `IAsyncEnumerable`.
5. **How does EF Core already cache queries?** It caches the translation of a query shape keyed by the expression tree (with parameters extracted), so the second call skips the SQL generation.
6. **What would you try first when a hot path is slow?** Measure; check the SQL and the plan (indexes), reduce round trips, project fewer columns - then consider compiled queries.

## Bonus exercise (optional)

Change `Lookups` to 50,000 in `Naive.cs` (it is shared) and re-run. Does the relative difference change? Then compile a second query returning a
`List<Order>` using `await foreach` over an `IAsyncEnumerable` result.
