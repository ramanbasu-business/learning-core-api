# 07 - Client vs server evaluation

**Scenario:** a report of "large delivered orders" (status Delivered, total at least 3,000).

```bash
cd lessons/07-client-vs-server
dotnet run
dotnet run -- --sql
```

## The problem

A LINQ query written against `IQueryable` is not executed by .NET - EF Core reads the **expression tree**, translates it to SQL,
and the database runs it (**server evaluation**). Anything EF cannot translate, such as your own C# method, can only run
**in your application** (**client evaluation**), on rows that have already been downloaded.

The naive code downloads the whole table and filters in memory:

```
database  ------ 200,000 rows ------>  .NET  ---- Where(IsLargeDeliveredOrder) ---->  a few thousand
```

All that network traffic, materialisation and memory exists only to be thrown away.

## Why does it compile and run at all?

Because `ToListAsync()` (or `AsEnumerable()`) switches from `IQueryable` (translated to SQL) to `IEnumerable` (plain LINQ-to-Objects).
After that point `Where` takes a normal C# delegate and runs in memory. The mistake is **where you put the switch** - too early.

> If you put the method **inside** the query (`db.Orders.Where(o => IsLargeDeliveredOrder(o))`), EF Core 3.0 and later throws
> `InvalidOperationException: The LINQ expression ... could not be translated` instead of silently loading everything.
> That error is a favour: it tells you the filter would not run in the database.

## The idea: keep the filter in the query

Write the condition with properties EF understands, and it becomes SQL:

```sql
SELECT ... FROM orders WHERE status = 'Delivered' AND total_amount >= 3000
```

## What the files do

| File | |
|---|---|
| `Naive.cs` | `ToListAsync()` first, then `Where(IsLargeDeliveredOrder)` in memory. |
| `Fixed.cs` | **Your turn.** The filter inside the query. |

## Your turn

1. Read `Naive.cs` and find the exact line where the data leaves the database.
2. In `Fixed.cs`, put the condition inline in `Where(...)` before `ToListAsync()`.
   <details>
   <summary>Hint 1</summary>
   <code>db.Orders.AsNoTracking().Where(o =&gt; o.Status == "Delivered" &amp;&amp; o.TotalAmount &gt;= 3000m).ToListAsync()</code>
   </details>
   <details>
   <summary>Hint 2 (reuse without copy-paste)</summary>
   If you want a reusable, named condition, make it an <code>Expression&lt;Func&lt;Order, bool&gt;&gt;</code> (not a plain method).
   Expressions can be translated; methods cannot.
   </details>
3. Optional experiment: temporarily write `.Where(o => Naive.IsLargeDeliveredOrder(o))` and read the exception.
4. Finish with `return Naive.Describe(large);`

## What to look for

- The **same orders** come back (`OK - same result as Naive`).
- **Queries: 1 -> 1** again - the difference is the **amount of data**: Naive downloads all 200,000 orders, Fixed downloads only matching ones. Compare with `--sql`: Naive has no `WHERE` at all.
- **Time** (and memory) drop. The effect grows with table size.

## Things to watch for

- **`ToList()` / `AsEnumerable()` too early** is the classic cause. Filter, sort, project and limit **before** you materialise.
- **Last-`Select` exception.** In the *final* `Select` EF will run an untranslatable method on the client (after fetching the columns it needs). That is fine for formatting a few values - not for filtering.
- **Translatable helpers**: `EF.Functions.Like/ILike`, `string.Contains/StartsWith`, `DateTime` arithmetic, `Math` functions, `Enumerable.Contains(ids)` and many more are supported by the Npgsql provider. For your own SQL function, map it with `HasDbFunction`.
- **Different semantics.** Client evaluation uses .NET rules (case sensitivity, culture, decimal rounding); server evaluation uses the database's. Check string comparisons.

## Rule of thumb

> **Filter, sort and page in the query; materialise last.** If the number of rows downloaded is bigger than the number you use, something is being evaluated on the client.

## Interview questions

1. **What is the difference between `IQueryable` and `IEnumerable` in EF Core?** `IQueryable` builds an expression tree translated to SQL; `IEnumerable` is LINQ-to-Objects running in memory.
2. **Why did EF Core stop silently doing client evaluation in `Where`?** (3.0) It hid performance disasters - a missing translation meant "download the table". Now it throws so you notice.
3. **Where is client evaluation still allowed?** In the final top-level projection (`Select`), after the needed columns have been fetched.
4. **How do you share a filter between queries?** A static `Expression<Func<T,bool>>`, an extension method that returns `IQueryable<T>`, or a global query filter - not a `Func<T,bool>`.
5. **How can you tell a query downloads too much?** Compare rows returned with rows used; log the SQL and look for a missing `WHERE`.
6. **What does `AsEnumerable()` do?** It ends the SQL part of the query; everything after runs in .NET.

## Bonus exercise (optional)

Try `db.Orders.Where(o => o.OrderNumber.PadLeft(12) == "...")` or `Where(o => Naive.IsLargeDeliveredOrder(o))` and read the translation errors.
Then rewrite one as `EF.Functions.ILike(...)` for a case-insensitive customer-name search.
