# 03 - The N+1 problem

**Scenario:** a report for the first 500 customers: name, number of orders, total spent.

```bash
cd lessons/03-n-plus-one
dotnet run
dotnet run -- --sql       # shows the first 3 statements - note how similar they are
```

## The problem

The naive code runs **one** query for the list of customers, then **one more query for every customer** to get that
customer's orders: 1 + N = 501 queries.

```
SELECT ... FROM customers ...                 <- 1 query
SELECT ... FROM orders WHERE customer_id = A  <- query 2
SELECT ... FROM orders WHERE customer_id = B  <- query 3
SELECT ... FROM orders WHERE customer_id = C  <- query 4
... 497 more ...
```

Each query is fast on its own. The cost is the **round trip**: every query pays for the network, for Postgres parsing and
planning it, and for EF Core building the command and materialising the result. 500 small queries are far slower than
one slightly bigger query, and the problem grows with N (5,000 customers = 5,001 queries).

N+1 is easy to write by accident:
- a `foreach` that awaits a query inside (this lesson);
- **lazy loading** - just touching `customer.Orders` inside a loop triggers a hidden query per customer;
- a mapper or serializer that calls the database once per item.

## The idea: ask for everything at once

| Approach | Use when |
|---|---|
| `Select` with `Count()` / `Sum()` inside the query | you only need numbers or a few values (this lesson - the best choice) |
| `Include(c => c.Orders)` | you really need the related entities |
| Two queries: customers, then `Where(o => ids.Contains(o.CustomerId))` | when a join is awkward; still 2 queries, not N+1 |

## What the files do

| File | |
|---|---|
| `CustomerSummary.cs` | One line of the report. |
| `Naive.cs` | A loop with a query inside it. |
| `Fixed.cs` | **Your turn.** One query. |

## Your turn

1. Read `Naive.cs` and count where queries are sent.
2. In `Fixed.cs`, build the same `CustomerSummary` list with a single query.
   <details>
   <summary>Hint 1</summary>
   Start from the customers query (AsNoTracking, OrderBy email, Take 500) and add <code>.Select(c =&gt; new CustomerSummary { ... })</code>.
   </details>
   <details>
   <summary>Hint 2</summary>
   <code>OrderCount = c.Orders.Count()</code> and <code>TotalSpent = c.Orders.Sum(o =&gt; o.TotalAmount)</code>. A customer with no orders gets 0, not an error.
   </details>
   <details>
   <summary>Hint 3</summary>
   Prefer the other approach (<code>Include</code>) as a second exercise: it works too, but loads every order column - compare the SQL.
   </details>
3. Finish with `return Naive.Describe(summaries);`
4. Run it, then run with `--sql`.

## What to look for

- **Queries: 501 -> 1.** This is the headline number.
- **Rows** are 500 in both, and the fingerprint (order count and total spent) matches: `OK - same result as Naive`.
- **Time** drops sharply. The gap depends on how far away the database is - on a real network (not localhost) each round trip is
  slower, so N+1 hurts even more in production than on your laptop.

## Things to watch for

- **Lazy loading hides N+1.** It is off in this lab (one `AppDbContext`, no proxies). If you see `UseLazyLoadingProxies()` in a
  project, search for loops that touch navigation properties.
- **`Include` fixes the count but not the width.** It loads whole entities; prefer `Select` (lesson 02) when you only need values.
- **Batching with `Contains(ids)`** is fine for hundreds of ids; for very large lists use a join or a temp table.
- Look for N+1 in tests: assert the **number of queries** (the lab's `QueryCounter` is exactly that).

## Rule of thumb

> **Never run a query inside a loop.** If the number of queries grows with the number of rows, you have an N+1.

## Interview questions

1. **What is the N+1 problem?** One query to load a list, then one more per item to load related data - 1 + N round trips instead of 1 or 2.
2. **How do you spot it?** Count the queries (logging, `QueryCounter`, a profiler): the count scales with the data. Also: a loop with `await` inside, or navigation properties touched in a loop with lazy loading on.
3. **How do you fix it in EF Core?** `Include`/`ThenInclude` (eager loading), a `Select` projection with aggregates, or batching with `Where(ids.Contains(...))`. Explicit loading and lazy loading both still issue extra queries.
4. **Is lazy loading always bad?** It is convenient for tiny, interactive screens but dangerous in loops, serializers and APIs, because the queries are invisible in the code.
5. **Why is 500 small queries slower than one big one even on localhost?** Fixed per-query overhead (EF command building, network round trip, Postgres parse/plan, result materialisation) is paid 500 times.

## Bonus exercise (optional)

Write a third variant (for yourself, not in the runner) with two queries: load the 500 customers, then load their orders with
`Where(o => ids.Contains(o.CustomerId))` and group in memory. Compare it with your `Select` solution.
