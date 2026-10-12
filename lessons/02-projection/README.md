# 02 - Projection with `Select`

**Scenario:** an orders grid shows five values per row: order number, customer name, status, total and number of
lines. We need the first 20,000 rows for an export.

```bash
cd lessons/02-projection
dotnet run
dotnet run -- --sql       # compare the two SELECT statements!
```

## The problem

Lesson 01 removed the cost of *tracking*. The next cost is **fetching data you never use**.

The naive version loads whole entities: every column of `orders`, every column of `customers` (to read one name) and
every row of `order_products` (just to count them). Then it copies five values into a small object and throws the rest away.

Worse, the `Include` of a collection makes the database send **one row per order line** - the order's and the
customer's columns are repeated on each of those rows:

```
orders ⨝ customers ⨝ order_products          what the database sends back
─────────────────────────────────────         ──────────────────────────────────────────
order A | customer X | line 1                 order A + customer X + line 1   (all columns)
order A | customer X | line 2                 order A + customer X + line 2   (all columns again)
order A | customer X | line 3                 order A + customer X + line 3   (all columns again)
```

About 60,000 wide rows travel over the network to produce 20,000 list items.

## The idea: project in the query

`Select` inside the query tells EF Core *which values* you need, so the generated SQL selects only those:

```csharp
db.Orders.Select(o => new SomeDto { Name = o.Customer.Name, ... })
```

- You can navigate relationships (`o.Customer.Name`) without `Include` - EF turns it into a JOIN.
- You can aggregate (`o.OrderProducts.Count`) - EF turns it into a subquery, so the lines never leave the database.
- The result is a plain DTO, **not tracked** - so `AsNoTracking()` is not needed either.

## What the files do

| File | |
|---|---|
| `OrderListItem.cs` | The five values the grid shows (the DTO). |
| `Naive.cs` | `Include` + load entities, then copy the five values in memory. |
| `Fixed.cs` | **Your turn.** `Select` straight into `OrderListItem`. |

## Your turn

1. Read `Naive.cs` - note the two steps: load everything, then map.
2. In `Fixed.cs`, replace both steps with a single query that uses `Select(...)` to create `OrderListItem` objects.
   <details>
   <summary>Hint 1</summary>
   Keep <code>OrderBy(o =&gt; o.OrderNumber)</code> and <code>Take(20_000)</code>. Add <code>.Select(o =&gt; new OrderListItem { ... })</code> and fill in the five properties from <code>o</code>.
   </details>
   <details>
   <summary>Hint 2</summary>
   <code>CustomerName = o.Customer.Name</code> and <code>ItemCount = o.OrderProducts.Count</code> - no <code>Include</code>, no <code>AsNoTracking()</code>.
   </details>
   <details>
   <summary>Hint 3</summary>
   Put <code>Select</code> before <code>ToListAsync()</code>. If you call <code>ToListAsync()</code> first and <code>Select</code> afterwards, you are back to loading everything (that is the naive version again).
   </details>
3. Finish with `return Naive.Describe(items);`
4. Run it, then run it again with `--sql`.

## What to look for

- **Rows** are identical (20,000) and the runner prints `OK - same result as Naive`.
- **Queries: 1 -> 1.** Again the win is not the number of queries - it is the *size* of each one.
- **The SELECT list** (`--sql`): Naive selects around twenty columns across three tables and returns ~60,000 rows;
  Fixed selects five values and returns 20,000 rows. Less to read in Postgres, less to send, less to materialise in .NET.
- **Time** is lower for Fixed. The gap grows with wider tables and bigger collections (think of a customer row with
  a large `notes` column, or orders with 50 lines).

## Things to watch for

- **Don't project too early or too late.** `ToList()` / `AsEnumerable()` *before* `Select` loads full entities first.
  Put `Select` in the query, before the call that runs it.
- **Projecting into an entity type is still tracked.** `Select(o => new Order { ... })` is a different thing from a DTO;
  use a DTO, a record or an anonymous type.
- **Don't project when you are going to update the entity.** To change an order you need the real, tracked `Order`.
  Read with projections, write with entities.
- **Methods EF cannot translate.** In the *last* `Select`, EF fetches the columns it needs and runs your C# method on the
  client. In `Where` or `OrderBy` the same method throws an exception (lesson 07 explains why this matters).

## Rule of thumb

> **If the screen or API needs 5 values, ask the database for 5 values.** Reach for `Select` into a DTO first; load
> whole entities only when you intend to modify them.

## Interview questions

1. **`Include` vs `Select` - when would you use which?**
   `Include` loads whole related entities (needed when you will work with or modify them). `Select` loads only the values
   you need and is the better default for read-only screens, lists and APIs.
2. **Do I need `AsNoTracking()` with `Select` into a DTO?**
   No. Only entity types are tracked; DTOs and anonymous types never are.
3. **How does `o.Customer.Name` inside `Select` work without `Include`?**
   EF translates the navigation access into a JOIN (or a subquery for collections and aggregates) when it builds the SQL.
4. **What happens if the `Select` calls a C# method EF cannot translate?**
   In the final top-level projection EF fetches the needed columns and runs the method in memory. In `Where`/`OrderBy`
   it throws, because EF cannot filter or sort in the database with it (since EF Core 3.0, no silent client evaluation there).
5. **Why can a collection `Include` make a query slow even when you only want a count?**
   The join repeats every parent column for every child row, so the result set multiplies. `Select(o => o.Children.Count)`
   keeps the count in the database (lessons 03-04 go deeper into the related problems).
6. **Would you use AutoMapper's `ProjectTo`?**
   It is a convenient way to generate the `Select` expression from a mapping configuration. The benefit is exactly this
   lesson's: only the mapped columns are queried. Mapping *after* loading entities (`Map<Dto>(entity)`) does not have that benefit.

## Bonus exercise (optional)

Add `.Include(o => o.OrderNotes)` to `Naive.cs` and count the notes too. Watch how many rows the database now returns for 20,000
orders (lines x notes per order). Then add `NoteCount = o.OrderNotes.Count` to `Fixed.cs` - one more number in the
SELECT list, still ~20,000 rows. This explosion has a name and a fix: lesson 04.
