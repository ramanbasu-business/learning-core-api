# 01 - Change tracking and `AsNoTracking()`

**Scenario:** a nightly export (or a read-only report page) loads 50,000 delivered orders and writes them out.
Nobody edits those orders. Does EF Core still need to keep track of them?

```bash
cd lessons/01-tracking
dotnet run
```

## The problem

By default, every entity a query returns is **tracked** by the `DbContext`'s *change tracker*:

```
SQL result row ──► new Order object ──► change tracker
                                         ├─ identity map: "Order with this Id is already loaded?"
                                         └─ snapshot: a copy of every original property value
```

Tracking exists so that this works:

```csharp
var order = await db.Orders.FirstAsync(...);
order.Status = "Shipped";
await db.SaveChangesAsync();     // EF compares order against its snapshot and writes only what changed
```

That is useful when you intend to change the data. When you only *read* it, you pay for:

- a lookup in the identity map for every row,
- a snapshot (a second copy of the values) for every entity - more memory and more garbage collection,
- extra work whenever EF scans for changes (`SaveChanges`, `ChangeTracker.Entries()`...).

For 50,000 rows that is real, completely wasted work.

## What the files do

| File | |
|---|---|
| `Naive.cs` | A plain query. EF tracks all 50,000 orders. |
| `Fixed.cs` | **Your turn.** The same query without tracking. |

## Your turn

1. Open `Naive.cs` and read the query.
2. Open `Fixed.cs`. Write the same query, but tell EF not to track the results. It is a single extra method call.
   <details>
   <summary>Hint 1</summary>
   The method name says exactly what you want: <code>No</code> + <code>Tracking</code>. It is called on the <code>IQueryable</code>, before <code>ToListAsync()</code>.
   </details>
   <details>
   <summary>Hint 2</summary>
   <code>AsNoTracking()</code> can go right after <code>db.Orders</code>. Its position in the chain does not change the SQL.
   </details>
3. Finish with `return Naive.Describe(orders, db);`
4. Run it: `dotnet run`

## What to look for

```
Variant   Queries    Time (ms)         Rows  Result
Naive           1          ...       50,000  baseline
Fixed           1          ...       50,000  OK - same result as Naive
  [Naive] entities tracked by EF: 50,000
  [Fixed] entities tracked by EF: 0
```

- **Queries: 1 -> 1.** `AsNoTracking()` does *not* change the SQL or the number of round trips. Run with
  `--sql` and compare: the statements are identical. It saves work **inside your application**, not in the database.
  (Fewer queries come from lessons 03 and 04.)
- **Time** is lower for Fixed, and so is memory use and garbage-collector pressure. The exact numbers depend on your machine -
  that is why the runner measures instead of promising. Try `dotnet run -- --no-warmup` and see how cold caches change the picture.
- **Entities tracked: 50,000 -> 0.** That is the whole difference.

## When *not* to use it

Do not use `AsNoTracking()` when you load an entity in order to **modify and save it**. Untracked entities are
invisible to `SaveChanges()`, so changes to them are silently ignored:

```csharp
var order = await db.Orders.AsNoTracking().FirstAsync(...);
order.Status = "Shipped";
await db.SaveChangesAsync();     // saves nothing - EF does not know about this order
```

(You *can* re-attach it with `db.Update(order)`, but that rewrites every column - usually not what you want.)

## Variations worth knowing

- **`AsNoTrackingWithIdentityResolution()`** - when you `Include` related data, plain `AsNoTracking()` creates a *separate*
  object for the same related row each time it appears (200 orders for one customer => 200 `Customer` objects).
  The identity-resolution version reuses one object per key within that query, still without tracking afterwards.
  Slightly slower than plain `AsNoTracking()`, still cheaper than full tracking.
- **Make no-tracking the default.** In read-heavy services you can set
  `options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)` once and opt in with `.AsTracking()`
  on the few write paths.
- **Projections are not tracked anyway.** `Select(o => new OrderDto { ... })` returns plain objects, so tracking never
  applies. That is lesson 02, and it also saves bandwidth because you only fetch the columns you need.

## Rule of thumb

> **Read-only query? Add `AsNoTracking()`. About to change and save what you loaded? Leave tracking on.**

## Interview questions

1. **What does the EF Core change tracker do?**
   It keeps an identity map (one object per entity key per context) and a snapshot of each tracked entity's original
   values. On `SaveChanges()` it compares current values with the snapshots to work out which INSERT/UPDATE/DELETE
   statements to send.
2. **Does `AsNoTracking()` make the SQL query faster?**
   No. The SQL and the round trips are identical. It removes client-side cost: identity-map lookups, snapshots,
   memory, and GC pressure.
3. **When would using `AsNoTracking()` be a bug?**
   When you load an entity to modify it and then call `SaveChanges()` - the changes are silently ignored.
4. **`AsNoTracking()` vs `AsNoTrackingWithIdentityResolution()`?**
   With `Include`, plain no-tracking materialises a new instance of the same related entity for every row that
   references it. The identity-resolution variant de-duplicates them within the query (so object graphs are shared
   correctly) but still does not track them in the context. It costs a bit more than plain `AsNoTracking()`.
5. **How would you apply this across a read-heavy application?**
   Default the context to `QueryTrackingBehavior.NoTracking` and call `AsTracking()` only where entities will be
   updated - or keep tracking on and put `AsNoTracking()` in the read-side repositories/query handlers.
6. **Are projected DTOs tracked?**
   No. Only entity types returned from a query are tracked, so `Select` into a DTO or anonymous type is already untracked.

## Bonus exercise (optional)

Change both `Naive.cs` and `Fixed.cs` to `.Include(o => o.Customer)`, then count the distinct `Customer` instances in
the results (`orders.Select(o => o.Customer).Distinct().Count()` counts distinct *objects*, `... .Select(c => c.Id).Distinct().Count()`
counts distinct *customers*). Compare `AsNoTracking()` with `AsNoTrackingWithIdentityResolution()`.
