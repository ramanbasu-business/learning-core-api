# 09 - Filtered `Include` (and raw SQL)

**Scenario:** a screen of 1,000 customer cards. Each card shows the customer's **3 most recent orders**, not all of them.

```bash
cd lessons/09-filtered-include
dotnet run
dotnet run -- --sql
```

## The problem

A plain `Include(c => c.Orders)` loads **every** order of every customer (about 20 each = 20,000 orders). The code then keeps 3 per
customer and discards the rest - 85% of what travelled over the network and was materialised is wasted.

## The idea: filter inside the Include

EF Core can apply `Where`, `OrderBy`, `Skip` and `Take` inside an `Include`:

```csharp
.Include(c => c.Orders.OrderByDescending(o => o.OrderDate).Take(3))
```

The database returns only the 3 rows per customer (it uses window functions / lateral joins under the hood). You still get
entity objects with a populated navigation property, which `Select` would not give you.

## What the files do

| File | |
|---|---|
| `Naive.cs` | `Include` everything, trim in memory with a loop. |
| `Fixed.cs` | **Your turn.** Trim inside the `Include`. |

## Your turn

1. Read `Naive.cs` and note the ordering rule.
2. In `Fixed.cs`, put the ordering and `Take(3)` inside `Include(...)`.
   <details>
   <summary>Hint 1</summary>
   <code>.Include(c =&gt; c.Orders.OrderByDescending(o =&gt; o.OrderDate).ThenByDescending(o =&gt; o.OrderNumber).Take(3))</code>
   </details>
   <details>
   <summary>Hint 2</summary>
   Keep <code>OrderBy(c =&gt; c.Email).Take(1_000)</code> on the customers, and remember <code>AsNoTracking()</code>.
   </details>
3. Finish with `return Naive.Describe(customers);`
4. Run it, then run with `--sql` and compare the two statements.

## What to look for

- **Same result** - 3 orders per customer, same total value (`OK - same result as Naive`).
- **Queries: 1 -> 1**, but the Fixed statement is more complex (a sub-select with `ROW_NUMBER`/`LATERAL`) and returns ~85% fewer rows.
- **Time** is lower; the gain grows with the size of the child collection (imagine a customer with 5,000 orders).

## Things to watch for

- Allowed inside Include: `Where`, `OrderBy/ThenBy`, `Skip`, `Take`, `Distinct`. Not allowed: `Select`, `GroupBy`, `Join`.
- With a **tracking** query, a filtered include can make the tracked navigation look "complete" while it is not (only 3 orders are loaded). Use `AsNoTracking()` for display-only data.
- It does not replace `Select`: if the card only needs a few *columns* of those 3 orders, a projection is still narrower (lesson 02).

## Raw SQL: when LINQ is not enough

Some queries are easier or faster in SQL (window functions, `DISTINCT ON`, full-text search). EF Core lets you mix both:

```csharp
// entities: composable - EF can add Where/OrderBy/Include on top (global query filters still apply)
var customers = await db.Customers
    .FromSql($"SELECT * FROM customers WHERE country = {country}")   // {country} becomes a SQL parameter
    .AsNoTracking()
    .ToListAsync();

// any other shape: a plain DTO - column aliases must match property names AFTER snake_case conversion
var rows = await db.Database
    .SqlQuery<CountryTotal>($"SELECT country, count(*) AS customer_count FROM customers GROUP BY country")
    .ToListAsync();
```

- **`FromSql` (interpolated) is safe** - values are parameterised. **`FromSqlRaw` with string concatenation is a SQL-injection hole.**
- `FromSql` must return **all columns** of the entity (the right names); `SqlQuery<T>` maps any shape but is not composable like an entity query.
- Raw SQL against `ExecuteSqlAsync` is how lesson 06 creates an index.

## Rule of thumb

> **Need only some children? Filter inside the `Include`.** Need only some columns? `Select`. Need SQL features LINQ lacks? `FromSql` with parameters.

## Interview questions

1. **What is a filtered include and what can it contain?** An `Include` with `Where/OrderBy/Skip/Take` applied to the collection, so only matching children are loaded.
2. **Why is "Include everything, then trim in memory" bad?** The database sends and EF materialises rows you immediately discard.
3. **`FromSql` vs `FromSqlRaw`?** Both run raw SQL; `FromSql` takes an interpolated string and parameterises values, `FromSqlRaw` takes a plain string and parameters you must pass explicitly - concatenating user input into it is injection.
4. **Do global query filters apply to `FromSql`?** Yes, EF wraps the raw SQL in a subquery and applies the filter to it (it does *not* apply to `SqlQuery<T>` or `ExecuteSql`).
5. **When is raw SQL the right tool?** For window functions, CTEs, `DISTINCT ON`, bulk operations and database-specific features LINQ cannot express - while keeping it parameterised.
6. **Pitfall of raw SQL results into DTOs in this lab?** Column aliases must match the property names after the snake_case naming convention (`customer_count`, not `CustomerCount`).

## Bonus exercise (optional)

Write the same "3 latest orders per customer" as a `SqlQuery<T>` with a `ROW_NUMBER() OVER (PARTITION BY customer_id ORDER BY order_date DESC)` window function and compare the plan and time.
