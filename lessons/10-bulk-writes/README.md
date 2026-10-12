# 10 - Bulk writes

**Problem:** you need to change many rows with the same rule ("cancel old pending orders").
The classic EF approach loads every row, edits it, and calls `SaveChanges`. That is
1 SELECT plus **one UPDATE per row**, and all those entities sit in the change tracker.

## Run it

```bash
cd lessons/10-bulk-writes
dotnet run
```

This lesson writes data. The runner restores the baseline database before each variant and afterwards, so you can run it as often as you like.

## Naive

`Naive.cs` loads the matching orders, loops, sets `Status = "Cancelled"`, then `SaveChanges()`.

## Your turn

Edit `Fixed.cs`. Use `ExecuteUpdateAsync`:

```csharp
var affected = await db.Orders
    .Where(...)
    .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, "Cancelled"));
```

## What you should see

- Same affected count (`OK`).
- Queries: thousands -> **1**.
- Time: seconds -> milliseconds.

## Takeaways

- `ExecuteUpdateAsync` / `ExecuteDeleteAsync` run **one SQL statement** and skip the change tracker.
- Trade-off: they bypass tracking, so no `SaveChanges` interceptors/auditing, no concurrency token checks, and tracked entities in memory become stale.
- Use them for set-based rules, not for editing a single entity you already loaded.

## Interview questions

- When would you NOT use `ExecuteUpdate`? (needs change tracking, audit hooks, concurrency checks, domain events)
- Does `SaveChanges` batch updates? (it batches commands in one round trip, but still one UPDATE per row)
